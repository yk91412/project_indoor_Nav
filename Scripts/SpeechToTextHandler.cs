using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using System;
using UnityEngine.Android;  // Permission Ŭ���� ����� ���� �߰�

public class SpeechToTextHandler : MonoBehaviour
{
    public TextMeshProUGUI debugText;
    private string apiKey = "";  // Google API Ű
    private bool isListening = false;

    public void StartVoiceRecognition()
    {
        // ������ �ο����� ���� ��� ���� ��û
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            Permission.RequestUserPermission(Permission.Microphone);
            debugText.text = "Requesting microphone permission...";
            return;  // ������ �㰡�� ������ ���
        }

        // ������ �̹� �ο��� ��� ���� �ν� ����
        if (!isListening)
        {
            isListening = true;
            debugText.text = "Voice Recognition Started";
            StartCoroutine(RecordAudioAndSendToGoogle());
        }
    }

    private IEnumerator RecordAudioAndSendToGoogle()
    {
        // ����ũ ��ġ Ȯ��
        if (Microphone.devices.Length == 0)
        {
            debugText.text = "No microphones found.";
            yield break;
        }

        // Step 1: Start recording
        AudioClip audioClip = Microphone.Start(null, false, 5, 44100);
        if (Microphone.IsRecording(null))
        {
            debugText.text = "Microphone is recording...";
        }
        else
        {
            debugText.text = "Microphone failed to start recording. Please check microphone settings and permissions.";
            yield break;
        }

        // 5�ʰ� ���� ���
        yield return new WaitForSeconds(5);

        // Step 2: Stop recording and process audio
        if (Microphone.IsRecording(null))
        {
            Microphone.End(null);
            debugText.text = "Microphone stopped, preparing to send audio...";

            // Convert AudioClip to byte array (WAV format)
            byte[] audioData = ConvertAudioClipToWav(audioClip);

            if (audioData != null)
            {
                debugText.text = $"Audio data size: {audioData.Length} bytes. Sending to Google...";
                yield return SendAudioToGoogle(audioData);
            }
            else
            {
                debugText.text = "Failed to convert audio to WAV format.";
            }

            // Reset the listening state after the process is completed
            isListening = false;
        }
        else
        {
            debugText.text = "Failed to stop recording.";
        }
    }

    private IEnumerator SendAudioToGoogle(byte[] audioData)
    {
        if (audioData == null || audioData.Length == 0)
        {
            debugText.text = "Audio data is empty or null.";
            yield break;
        }

        // Convert byte array to Base64
        string base64Audio = Convert.ToBase64String(audioData);

        // JSON payload ���� (�ڷ��� ������ �������� ����)
        string jsonPayload = "{\"config\":{\"encoding\":\"LINEAR16\",\"sampleRateHertz\":44100,\"languageCode\":\"ko-KR\"},\"audio\":{\"content\":\"" + base64Audio + "\"}}";

        // JSON ���̷ε� Ȯ���� ���� ����� �α� �߰�
        Debug.Log("JSON Payload: " + jsonPayload);

        UnityWebRequest request = new UnityWebRequest("https://speech.googleapis.com/v1/speech:recognize?key=" + apiKey, "POST");
        byte[] bodyRaw = new System.Text.UTF8Encoding().GetBytes(jsonPayload);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        // ��û ���� �� ���� ���
        yield return request.SendWebRequest();

        // API ���� Ȯ��
        if (request.result == UnityWebRequest.Result.Success)
        {
            debugText.text = "Google response received. Parsing response...";
            string recognizedText = ParseResponse(request.downloadHandler.text);
            debugText.text = $"Recognized Text: {recognizedText}";

            // LLMHandler�� �ؽ�Ʈ�� ����
            FindObjectOfType<LLMHandler>().GetTargetFromText(recognizedText);
        }
        else
        {
            // ���� �޽����� ���� ���� ���
            debugText.text = $"Error: {request.error}, Status Code: {request.responseCode}";
            Debug.LogError($"Google API Request Error: {request.error}, Response Code: {request.responseCode}, Detailed Response: {request.downloadHandler.text}");
        }
    }

    // AudioClip�� WAV ���Ϸ� ��ȯ�ϴ� �Լ� (�޸� �󿡼� ó��)
    private byte[] ConvertAudioClipToWav(AudioClip clip)
    {
        try
        {
            using (var memoryStream = new System.IO.MemoryStream())
            {
                byte[] header = WriteWavHeader(clip);
                memoryStream.Write(header, 0, header.Length);

                byte[] audioData = ConvertAudioClipToPCM(clip);
                memoryStream.Write(audioData, 0, audioData.Length);

                return memoryStream.ToArray();
            }
        }
        catch (Exception ex)
        {
            debugText.text = $"Error converting audio to WAV: {ex.Message}";
            return null;
        }
    }

    private byte[] WriteWavHeader(AudioClip clip)
    {
        int fileSize = clip.samples * clip.channels * 2 + 44; // 2 bytes per sample
        int sampleRate = clip.frequency;

        byte[] header = new byte[44];

        // ChunkID "RIFF"
        header[0] = (byte)'R';
        header[1] = (byte)'I';
        header[2] = (byte)'F';
        header[3] = (byte)'F';

        byte[] chunkSize = BitConverter.GetBytes(fileSize - 8);
        Array.Copy(chunkSize, 0, header, 4, 4);

        // Format "WAVE"
        header[8] = (byte)'W';
        header[9] = (byte)'A';
        header[10] = (byte)'V';
        header[11] = (byte)'E';

        byte[] subChunk1Size = BitConverter.GetBytes(16);
        Array.Copy(subChunk1Size, 0, header, 16, 4);

        byte[] audioFormat = BitConverter.GetBytes((short)1);
        Array.Copy(audioFormat, 0, header, 20, 2);

        byte[] numChannels = BitConverter.GetBytes((short)clip.channels);
        Array.Copy(numChannels, 0, header, 22, 2);

        byte[] sampleRateBytes = BitConverter.GetBytes(sampleRate);
        Array.Copy(sampleRateBytes, 0, header, 24, 4);

        byte[] byteRate = BitConverter.GetBytes(sampleRate * clip.channels * 2);
        Array.Copy(byteRate, 0, header, 28, 4);

        byte[] blockAlign = BitConverter.GetBytes((short)(clip.channels * 2));
        Array.Copy(blockAlign, 0, header, 32, 2);

        byte[] bitsPerSample = BitConverter.GetBytes((short)16);
        Array.Copy(bitsPerSample, 0, header, 34, 2);

        // Subchunk2ID "data"
        header[36] = (byte)'d';
        header[37] = (byte)'a';
        header[38] = (byte)'t';
        header[39] = (byte)'a';

        byte[] subChunk2Size = BitConverter.GetBytes(clip.samples * clip.channels * 2);
        Array.Copy(subChunk2Size, 0, header, 40, 4);

        return header;
    }

    private byte[] ConvertAudioClipToPCM(AudioClip clip)
    {
        float[] samples = new float[clip.samples * clip.channels];
        clip.GetData(samples, 0);

        byte[] pcmData = new byte[samples.Length * 2]; // 16-bit PCM, 2 bytes per sample

        for (int i = 0; i < samples.Length; i++)
        {
            short sampleValue = (short)(samples[i] * short.MaxValue);  // Float to 16-bit PCM conversion
            byte[] sampleBytes = BitConverter.GetBytes(sampleValue);
            pcmData[i * 2] = sampleBytes[0];
            pcmData[i * 2 + 1] = sampleBytes[1];
        }

        return pcmData;
    }

    // Google Speech-to-Text ���� �Ľ� �޼���
    private string ParseResponse(string jsonResponse)
    {
        int startIndex = jsonResponse.IndexOf("\"transcript\": \"") + 15;
        int endIndex = jsonResponse.IndexOf("\"", startIndex);
        return jsonResponse.Substring(startIndex, endIndex - startIndex);
    }
}

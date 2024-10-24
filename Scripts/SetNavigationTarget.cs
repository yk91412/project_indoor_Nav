using TMPro;  // TextMeshProUGUI�� ����ϱ� ���� �ʿ�
using System.Collections;
using System.Collections.Generic;
using UnityEngine.XR.ARFoundation;
using UnityEngine;
using UnityEngine.AI;


public class SetNavigationTarget : MonoBehaviour
{
    public TextMeshProUGUI debugText;  // ����� �޽����� ǥ���� TextMeshProUGUI �ʵ�
    [SerializeField]
    private List<GameObject> navTargetObjects;  // ���� Ÿ�� ������Ʈ��
    [SerializeField]
    private LineRenderer line;  // ���� ������
    [SerializeField]
    private GameObject indicator;  // �ε������� ��ü
    [SerializeField]
    private Camera topDownCamera;  // TopDownCamera �ʵ� �߰�

    private NavMeshPath path;  // ��� ����
    private GameObject currentTarget;  // ���� Ÿ��
    private Vector3 lastIndicatorPosition;  // ���� �ε������� ��ġ
    private Vector3 lastTargetPosition;  // ���� Ÿ�� ��ġ

    private void Start()
    {
        path = new NavMeshPath();
        line.positionCount = 0;
        lastIndicatorPosition = indicator.transform.position;
    }

    public void SetTarget(string targetName)
    {
        foreach (GameObject target in navTargetObjects)
        {
            if (target.name.Equals(targetName))
            {
                currentTarget = target;
                lastTargetPosition = currentTarget.transform.position;
                // debugText.text += "Target found: " + targetName;
                UpdatePath();
                break;
            }
        }
    }

    private void UpdatePath()
    {
        if (currentTarget != null)
        {
            if (indicator.transform.position != lastIndicatorPosition || currentTarget.transform.position != lastTargetPosition)
            {
                // debugText.text += $"\nCalculating path from Indicator: {indicator.transform.position} to Target: {currentTarget.transform.position}";

                NavMesh.CalculatePath(indicator.transform.position, currentTarget.transform.position, NavMesh.AllAreas, path);

                if (path.status == NavMeshPathStatus.PathComplete)
                {
                    // debugText.text += "\nPath successfully calculated.";
                    line.positionCount = path.corners.Length;
                    line.SetPositions(path.corners);
                    line.enabled = true;

                    lastIndicatorPosition = indicator.transform.position;
                    lastTargetPosition = currentTarget.transform.position;

                    // TopDownCamera�� �ε������͸� ���󰡵��� ����
                    topDownCamera.transform.position = new Vector3(indicator.transform.position.x, topDownCamera.transform.position.y, indicator.transform.position.z);
                }
                else
                {
                    // debugText.text += "\nFailed to calculate path.";
                }
            }
        }
    }

    private void Update()
    {
        if (currentTarget != null)
        {
            UpdatePath();
        }
    }
}

using TMPro;  // TextMeshProUGUI를 사용하기 위해 필요
using System.Collections;
using System.Collections.Generic;
using UnityEngine.XR.ARFoundation;
using UnityEngine;
using UnityEngine.AI;


public class SetNavigationTarget : MonoBehaviour
{
    public TextMeshProUGUI debugText;  // 디버깅 메시지를 표시할 TextMeshProUGUI 필드
    [SerializeField]
    private List<GameObject> navTargetObjects;  // 여러 타겟 오브젝트들
    [SerializeField]
    private LineRenderer line;  // 라인 렌더러
    [SerializeField]
    private GameObject indicator;  // 인디케이터 객체
    [SerializeField]
    private Camera topDownCamera;  // TopDownCamera 필드 추가

    private NavMeshPath path;  // 경로 정보
    private GameObject currentTarget;  // 현재 타겟
    private Vector3 lastIndicatorPosition;  // 이전 인디케이터 위치
    private Vector3 lastTargetPosition;  // 이전 타겟 위치

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

                    // TopDownCamera가 인디케이터를 따라가도록 설정
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

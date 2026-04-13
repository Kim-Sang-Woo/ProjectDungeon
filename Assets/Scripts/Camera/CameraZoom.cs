// ============================================================
// CameraZoom.cs — 마우스 휠 카메라 줌 시스템
// 위치: Assets/Scripts/Camera/CameraZoom.cs
// ============================================================
// [개요]
//   마우스 휠 업/다운으로 Orthographic Camera의 Size를 조절한다.
//   Min / Max 값은 Inspector에서 직접 수정 가능.
//
// [씬 배치]
//   Main Camera에 부착
// ============================================================
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraZoom : MonoBehaviour
{
    [Header("줌 범위")]
    [Tooltip("카메라 최소 Size (최대 확대)")]
    public float minSize = 10f;

    [Tooltip("카메라 최대 Size (최대 축소)")]
    public float maxSize = 25f;

    [Header("줌 속도")]
    [Tooltip("휠 한 칸당 줌 변화량")]
    public float zoomStep = 1.5f;

    [Tooltip("줌 보간 속도 (높을수록 즉각 반응)")]
    public float zoomSpeed = 10f;

    private Camera cam;
    private float targetSize;
    private float defaultSize;
    private bool zoomEnabled = true;

    private void Awake()
    {
        cam        = GetComponent<Camera>();
        targetSize = cam.orthographicSize;
        defaultSize = cam.orthographicSize;
    }

    private void Update()
    {
        if (zoomEnabled)
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");

            if (scroll != 0f)
            {
                targetSize -= scroll * zoomStep * (maxSize - minSize);
                targetSize  = Mathf.Clamp(targetSize, minSize, maxSize);
            }
        }

        cam.orthographicSize = Mathf.Lerp(
            cam.orthographicSize,
            targetSize,
            Time.deltaTime * zoomSpeed
        );
    }

    public void SetZoomEnabled(bool enabled)
    {
        zoomEnabled = enabled;
    }

    public void ResetToDefaultSize(bool immediate = false)
    {
        targetSize = Mathf.Clamp(defaultSize, minSize, maxSize);
        if (immediate && cam != null)
            cam.orthographicSize = targetSize;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        minSize = Mathf.Max(0.1f, minSize);
        maxSize = Mathf.Max(minSize + 0.1f, maxSize);
    }
#endif
}

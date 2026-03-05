// ============================================================
// CameraBackground.cs — 카메라 배경 검정 설정
// 위치: Assets/Scripts/Camera/CameraBackground.cs
// ============================================================
// [용도]
//   Unity 기본 파란 배경(Skybox/Solid Color 파란색)을 검정으로 바꾼다.
//   Main Camera에 부착하면 Play 시작 시 자동으로 설정된다.
//
// [씬 세팅 대안]
//   코드 없이 처리하려면:
//   Main Camera → Inspector → Camera 컴포넌트
//     → Clear Flags : Solid Color
//     → Background  : 검정 (R=0, G=0, B=0, A=255)
//   으로 직접 설정해도 동일하다.
// ============================================================
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraBackground : MonoBehaviour
{
    [Tooltip("배경 색상 (기본: 완전 검정)")]
    public Color backgroundColor = Color.black;

    private Camera cam;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        Apply();
    }

    private void Apply()
    {
        cam.clearFlags       = CameraClearFlags.SolidColor;
        cam.backgroundColor  = backgroundColor;
    }

#if UNITY_EDITOR
    // 에디터에서 색상 변경 시 즉시 반영
    private void OnValidate()
    {
        if (cam == null) cam = GetComponent<Camera>();
        if (cam != null) Apply();
    }
#endif
}

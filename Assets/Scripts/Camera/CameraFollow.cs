// ============================================================
// CameraFollow.cs — 카메라 플레이어 추적
// 기획서 Ch.5.2 참조
// 위치: Assets/Scripts/Camera/CameraFollow.cs
// ============================================================
// [v2 변경사항]
//   - 프레임 독립적 보간으로 수정
//     기존 Lerp(pos, target, smoothSpeed)는 프레임 레이트에 따라
//     추적 속도가 달라지는 문제가 있었음.
//     Mathf.Pow 기반 감쇠로 60fps/30fps 모두 동일한 추적 느낌 보장.
// ============================================================
using UnityEngine;

/// <summary>
/// 카메라가 플레이어를 부드럽게 따라가는 컴포넌트.
/// Main Camera 오브젝트에 부착한다.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("설정")]
    [Tooltip("추적 대상 (Player 오브젝트)")]
    public Transform target;

    [Tooltip("추적 부드러움 (0~1, 낮을수록 부드러움)")]
    [Range(0.01f, 1f)]
    public float smoothSpeed = 0.125f;

    [Tooltip("카메라 Z축 오프셋 (2D이므로 -10 유지)")]
    public float zOffset = -10f;

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPosition = new Vector3(
            target.position.x,
            target.position.y,
            zOffset
        );

        // 프레임 독립적 감쇠 보간
        // smoothSpeed가 동일하면 30fps든 144fps든 같은 속도로 추적
        float damping = 1f - Mathf.Pow(1f - smoothSpeed, Time.deltaTime * 60f);
        Vector3 smoothedPosition = Vector3.Lerp(
            transform.position,
            desiredPosition,
            damping
        );

        transform.position = smoothedPosition;
    }
}

// ============================================================
// CameraFollow.cs — 카메라 플레이어 추적
// 기획서 Ch.5.2 참조
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

        Vector3 smoothedPosition = Vector3.Lerp(
            transform.position,
            desiredPosition,
            smoothSpeed
        );

        transform.position = smoothedPosition;
    }
}

// ============================================================
// StairSystem.cs — 계단 상호작용 시스템
// 위치: Assets/Scripts/Events/StairSystem.cs
// ============================================================
// 현재 계단 상호작용은 ObjectEventTrigger -> EventPopup 흐름으로 통일한다.
// 직접 클릭으로 즉시 층 이동하던 예전 로직은 이벤트 팝업 UX와 충돌하므로 비활성화.
// ============================================================
using UnityEngine;

public class StairSystem : MonoBehaviour
{
    [Header("참조")]
    public DungeonManager dungeonManager;
    public MovementSystem movementSystem;
}

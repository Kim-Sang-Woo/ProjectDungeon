// ============================================================
// EventTriggerSystem.cs — 이벤트 트리거 시스템
// 기획서 Ch.3 참조
// 위치: Assets/Scripts/Events/EventTriggerSystem.cs
// ============================================================
// [v2 변경사항]
//   - DungeonGenerator 직접 참조 → DungeonManager 참조
//   - TileData가 class로 변경되어 직접 필드 수정이 원본에 반영됨
//     → SetTileData() 호출 불필요
// ============================================================
using UnityEngine;

/// <summary>
/// 플레이어 이동 시 타일의 이벤트를 감지하고 처리하는 시스템.
/// MovementSystem.OnTileEntered 이벤트를 구독하여 동작한다.
/// </summary>
public class EventTriggerSystem : MonoBehaviour
{
    [Header("참조")]
    [Tooltip("Player의 MovementSystem 참조")]
    public MovementSystem movementSystem;
    [Tooltip("이벤트 발생 시 표시할 팝업 UI")]
    public EventPopupUI eventPopupUI;
    [Tooltip("던전 데이터 접근용")]
    public DungeonManager dungeonManager;

    private void Start()
    {
        if (movementSystem != null)
        {
            movementSystem.OnTileEntered += OnPlayerTileEntered;
            Debug.Log("[EventTriggerSystem] MovementSystem.OnTileEntered 구독 완료.");
        }
        else
        {
            Debug.LogError("[EventTriggerSystem] MovementSystem 참조가 설정되지 않았습니다!");
        }
    }

    private void OnDestroy()
    {
        if (movementSystem != null)
        {
            movementSystem.OnTileEntered -= OnPlayerTileEntered;
        }
    }

    private void OnPlayerTileEntered(Vector2Int tilePos)
    {
        CheckAndTriggerEvent(tilePos);
    }

    /// <summary>
    /// 타일의 이벤트 여부를 확인하고 이벤트를 트리거한다.
    /// 이벤트 발생 시 이동을 즉시 중단하고 GameManager에 통보한다.
    /// </summary>
    public void CheckAndTriggerEvent(Vector2Int tilePos)
    {
        if (dungeonManager == null) return;

        TileData tile = dungeonManager.GetTile(tilePos.x, tilePos.y);

        // 이벤트가 없거나 이미 소진된 경우 → 이동 계속
        if (tile.eventData == null)
            return;

        if (tile.isEventConsumed && !tile.eventData.isRepeatable)
            return;

        // ─── 이벤트 발생! ───

        // 1. 이동 즉시 중단
        movementSystem.StopMovement();
        Debug.Log($"[EventTriggerSystem] 이벤트 발생! 위치: {tilePos}, 타입: {tile.eventData.eventType}, 이름: {tile.eventData.displayName}");

        // 2. 이벤트 소진 처리 — TileData가 class이므로 직접 수정이 원본에 반영됨
        tile.isEventConsumed = true;

        // 3. GameManager에 이벤트 통보
        if (GameManager.Instance != null)
        {
            GameManager.Instance.HandleDungeonEvent(tile.eventData);
        }

        // 4. UI 팝업 표시
        if (eventPopupUI != null)
        {
            eventPopupUI.ShowEvent(tile.eventData);
        }
    }
}

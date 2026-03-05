// ============================================================
// EventTriggerSystem.cs — 이벤트 트리거 시스템
// 기획서 Ch.3 참조
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
    public DungeonGenerator dungeonGenerator;

    private void Start()
    {
        // 기획서 5.2: Start()에서 OnTileEntered 이벤트 구독
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
        // 이벤트 구독 해제
        if (movementSystem != null)
        {
            movementSystem.OnTileEntered -= OnPlayerTileEntered;
        }
    }

    /// <summary>
    /// 플레이어가 타일에 진입했을 때 호출되는 콜백
    /// </summary>
    private void OnPlayerTileEntered(Vector2Int tilePos)
    {
        CheckAndTriggerEvent(tilePos);
    }

    /// <summary>
    /// 기획서 Ch.3: 타일의 이벤트 여부를 확인하고 이벤트를 트리거한다.
    /// 이벤트 발생 시 이동을 즉시 중단하고 GameManager에 통보한다.
    /// </summary>
    public void CheckAndTriggerEvent(Vector2Int tilePos)
    {
        if (dungeonGenerator == null) return;

        TileData tile = dungeonGenerator.GetTileData(tilePos.x, tilePos.y);

        // 이벤트가 없거나 이미 소진된 경우 → 이동 계속
        if (tile.eventData == null)
            return;

        if (tile.isEventConsumed && !tile.eventData.isRepeatable)
            return;

        // ─── 이벤트 발생! ───

        // 1. 이동 즉시 중단 (기획서 3.2: STOP)
        movementSystem.StopMovement();
        Debug.Log($"[EventTriggerSystem] ⚡ 이벤트 발생! 위치: {tilePos}, 타입: {tile.eventData.eventType}, 이름: {tile.eventData.displayName}");

        // 2. 이벤트 소진 처리 (기획서 3.2)
        tile.isEventConsumed = true;
        dungeonGenerator.SetTileData(tilePos.x, tilePos.y, tile);

        // 3. GameManager에 이벤트 통보 (기획서 0.6)
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

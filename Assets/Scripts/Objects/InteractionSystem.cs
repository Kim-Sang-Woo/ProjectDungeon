// ============================================================
// InteractionSystem.cs — 오브젝트 인터렉션 시스템
// 위치: Assets/Scripts/Objects/InteractionSystem.cs
// ============================================================
// [v1.4 변경사항]
//   - ShowInteraction() 콜백을 Action → Action<string>으로 변경
//     actionId를 받아 처리 분기
//   - objectType 기반 switch → actionId 기반 switch로 변경
//     DungeonObjectData.actions에서 정의한 actionId로 동작 결정
//
// [표준 actionId 목록]
//   "open"     — 보물 상자 열기
//   "go_down"  — 내려가는 계단
//   "go_up"    — 올라가는 계단
//   "ignore"   — 아무것도 하지 않고 UI 닫기
//   (새 액션 추가 시 HandleAction() switch에 case 추가)
// ============================================================
using UnityEngine;

public class InteractionSystem : MonoBehaviour
{
    [Header("참조")]
    public MovementSystem       movementSystem;
    public DungeonManager       dungeonManager;
    public InteractionUI        interactionUI;
    public DungeonObjectSpawner dungeonObjectSpawner;

    private Vector2Int currentInteractTile;

    private void Start()
    {
        if (movementSystem == null)
        {
            Debug.LogError("[InteractionSystem] MovementSystem 참조 없음!");
            return;
        }
        if (dungeonManager == null)
        {
            Debug.LogError("[InteractionSystem] DungeonManager 참조 없음!");
            return;
        }

        movementSystem.OnTileEntered  += OnPlayerMoved;
        dungeonManager.OnFloorChanged += OnFloorChanged;
    }

    private void OnDestroy()
    {
        if (movementSystem != null)
            movementSystem.OnTileEntered  -= OnPlayerMoved;
        if (dungeonManager != null)
            dungeonManager.OnFloorChanged -= OnFloorChanged;
    }

    // ─── 이벤트 핸들러 ───

    private void OnFloorChanged(int floorIndex)
    {
        if (interactionUI != null) interactionUI.Hide();
        CheckCurrentTile();
    }

    private void OnPlayerMoved(Vector2Int tilePos)
    {
        if (dungeonManager == null) return;

        TileData tile = dungeonManager.GetTile(tilePos.x, tilePos.y);

        if (tile.HasObject)
        {
            currentInteractTile = tilePos;
            ShowInteraction(tilePos, tile);
        }
        else
        {
            if (interactionUI != null) interactionUI.Hide();
        }
    }

    private void CheckCurrentTile()
    {
        if (dungeonManager == null || movementSystem == null) return;
        if (dungeonManager.Grid == null) return;
        OnPlayerMoved(movementSystem.CurrentTilePosition);
    }

    // ─── UI 표시 ───

    private void ShowInteraction(Vector2Int tilePos, TileData tile)
    {
        if (interactionUI == null) return;

        // actionId를 받아 처리하는 콜백 전달
        interactionUI.ShowInteraction(
            tile.placedObject,
            (actionId) => HandleAction(tilePos, actionId)
        );
    }

    // ─── actionId 기반 처리 분기 ───

    /// <summary>
    /// 액션 링크 클릭 시 actionId로 동작을 결정한다.
    /// 새 actionId 추가 시 case를 추가한다.
    /// </summary>
    private void HandleAction(Vector2Int tilePos, string actionId)
    {
        Debug.Log($"[InteractionSystem] 액션 실행 — tilePos:{tilePos} actionId:{actionId}");

        switch (actionId)
        {
            case "open":
                HandleOpen(tilePos);
                break;

            case "go_down":
                HandleGoDown();
                break;

            case "go_up":
                HandleGoUp();
                break;

            case "ignore":
                // 아무것도 하지 않음 — UI는 ShowInteraction 콜백에서 이미 Hide() 호출됨
                Debug.Log("[InteractionSystem] 내버려 두기 — 아무 행동 없음");
                break;

            default:
                Debug.LogWarning($"[InteractionSystem] 처리되지 않은 actionId: '{actionId}'");
                break;
        }
    }

    // ─── 액션별 처리 ───

    private void HandleOpen(Vector2Int tilePos)
    {
        TileData tile = dungeonManager.GetTile(tilePos.x, tilePos.y);
        if (tile.placedObject == null) return;

        DungeonObjectData data = tile.placedObject;
        Debug.Log($"[InteractionSystem] 열기 — {data.displayName}");

        tile.isObjectInteracted = true;

        if (data.isOneTime && dungeonObjectSpawner != null)
            dungeonObjectSpawner.RemoveAt(tilePos);

        // 보상 아이템 지급
        if (data.rewardItems != null && Inventory.Instance != null)
        {
            for (int i = 0; i < data.rewardItems.Length; i++)
            {
                ItemData item = data.rewardItems[i];
                if (item == null) continue;

                int qty = (data.rewardQuantities != null && i < data.rewardQuantities.Length)
                    ? Mathf.Max(1, data.rewardQuantities[i])
                    : 1;

                bool added = Inventory.Instance.AddItem(item, qty);
                if (added)
                    Debug.Log($"[InteractionSystem] 아이템 획득: {item.itemName} x{qty}");
                else
                    Debug.Log($"[InteractionSystem] 인벤토리 가득 참 — {item.itemName} 획득 실패");
            }
        }
    }

    private void HandleGoDown()
    {
        Debug.Log("[InteractionSystem] 내려가는 계단 → 다음 층");
        movementSystem.LockInput();
        dungeonManager.GoToNextFloor();
    }

    private void HandleGoUp()
    {
        if (dungeonManager.CurrentFloorIndex <= 0)
        {
            Debug.Log("[InteractionSystem] 최상위 층 — 더 올라갈 수 없습니다.");
            return;
        }
        Debug.Log("[InteractionSystem] 올라가는 계단 → 이전 층");
        movementSystem.LockInput();
        dungeonManager.GoToPreviousFloor();
    }
}

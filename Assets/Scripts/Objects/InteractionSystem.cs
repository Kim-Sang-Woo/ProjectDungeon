// ============================================================
// InteractionSystem.cs — 오브젝트 인터렉션 시스템
// 위치: Assets/Scripts/Objects/InteractionSystem.cs
// ============================================================
// [v1.5 변경사항]
//   - HandleOpen(): AddItemResult로 획득 결과 분기
//   - 획득 성공/실패/감속 발생 시 FloatingTextUI 호출
//   - 감속 최초 발생 감지: 획득 전후 IsOverweightSlow 비교
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

        interactionUI.ShowInteraction(
            tile.placedObject,
            (actionId) => HandleAction(tilePos, actionId)
        );
    }

    // ─── actionId 기반 처리 분기 ───

    private void HandleAction(Vector2Int tilePos, string actionId)
    {
        switch (actionId)
        {
            case "open":     HandleOpen(tilePos);  break;
            case "go_down":  HandleGoDown();        break;
            case "go_up":    HandleGoUp();          break;
            case "ignore":                          break;
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
        if (data.rewardItems == null || Inventory.Instance == null) return;

        // ── 1단계: 획득 가능 여부를 먼저 검사 ──
        // 모든 아이템이 획득 가능한지 사전 확인
        for (int i = 0; i < data.rewardItems.Length; i++)
        {
            ItemData item = data.rewardItems[i];
            if (item == null) continue;

            int qty = (data.rewardQuantities != null && i < data.rewardQuantities.Length)
                ? Mathf.Max(1, data.rewardQuantities[i])
                : 1;

            // 슬롯 초과 사전 체크
            bool needsNewSlot = !item.isStackable ||
                                !Inventory.Instance.Slots.Exists(
                                    s => s.item.itemId == item.itemId &&
                                         s.quantity < item.maxStack);
            if (needsNewSlot && Inventory.Instance.Slots.Count >= Inventory.Instance.maxItemCount)
            {
                FloatingTextUI.Instance?.ShowNoSpace();
                return; // 보물상자 소모 없이 취소
            }

            // 무게 초과 사전 체크
            if (Inventory.Instance.CurrentWeight + item.weight * qty > Inventory.Instance.maxWeight)
            {
                FloatingTextUI.Instance?.ShowTooHeavy();
                return; // 보물상자 소모 없이 취소
            }
        }

        // ── 2단계: 모든 아이템 획득 가능 확인 후 보물상자 소모 ──
        tile.isObjectInteracted = true;
        if (data.isOneTime && dungeonObjectSpawner != null)
            dungeonObjectSpawner.RemoveAt(tilePos);

        // ── 3단계: 아이템 지급 및 연출 ──
        for (int i = 0; i < data.rewardItems.Length; i++)
        {
            ItemData item = data.rewardItems[i];
            if (item == null) continue;

            int qty = (data.rewardQuantities != null && i < data.rewardQuantities.Length)
                ? Mathf.Max(1, data.rewardQuantities[i])
                : 1;

            bool wasSlowBefore   = Inventory.Instance.IsOverweightSlow;
            AddItemResult result = Inventory.Instance.AddItem(item, qty);

            if (result == AddItemResult.Success)
            {
                FloatingTextUI.Instance?.ShowAcquire(item.itemName, qty);

                if (!wasSlowBefore && Inventory.Instance.IsOverweightSlow)
                    FloatingTextUI.Instance?.ShowSlowDown();
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

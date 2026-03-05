// ============================================================
// InteractionSystem.cs — 오브젝트 인터렉션 시스템
// 위치: Assets/Scripts/Objects/InteractionSystem.cs
// ============================================================
// [v1.3 변경사항]
//   - 계단 인터렉션 추가 (StairSystem.cs 대체)
//     STAIRS_DOWN: LockInput → GoToNextFloor
//     STAIRS_UP  : LockInput → GoToPreviousFloor
//   - StairSystem.cs는 씬에서 제거해도 됨
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
            () => HandleInteraction(tilePos)
        );
    }

    // ─── 인터렉션 처리 ───

    private void HandleInteraction(Vector2Int tilePos)
    {
        TileData tile = dungeonManager.GetTile(tilePos.x, tilePos.y);
        if (tile.placedObject == null) return;

        switch (tile.placedObject.objectType)
        {
            case DungeonObjectType.TREASURE_CHEST:
                HandleTreasureChest(tilePos, tile, tile.placedObject);
                break;
            case DungeonObjectType.STAIRS_DOWN:
                HandleStairsDown();
                break;
            case DungeonObjectType.STAIRS_UP:
                HandleStairsUp();
                break;
            default:
                Debug.LogWarning($"[InteractionSystem] 처리되지 않은 타입: {tile.placedObject.objectType}");
                break;
        }
    }

    private void HandleTreasureChest(Vector2Int tilePos, TileData tile, DungeonObjectData data)
    {
        Debug.Log($"[InteractionSystem] 보물 상자 열기! 위치:{tilePos} 이름:{data.displayName}");

        tile.isObjectInteracted = true;

        if (data.isOneTime && dungeonObjectSpawner != null)
            dungeonObjectSpawner.RemoveAt(tilePos);

        // TODO: 보상 처리
    }

    private void HandleStairsDown()
    {
        Debug.Log("[InteractionSystem] 내려가는 계단 → 다음 층");
        movementSystem.LockInput();
        dungeonManager.GoToNextFloor();
    }

    private void HandleStairsUp()
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

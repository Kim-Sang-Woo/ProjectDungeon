// ============================================================
// InteractionSystem.cs — 오브젝트 인터렉션 시스템
// 위치: Assets/Scripts/Objects/InteractionSystem.cs
// ============================================================
// [v1.2 수정]
//   최초 오브젝트 접촉 시 UI 미출력 근본 원인 수정
//   - 원인: Start() 실행 순서 불확정
//     InteractionSystem.Start()가 DungeonManager.Start()보다
//     먼저 실행되면 Grid가 null이라 CheckCurrentTile()이 실패함
//   - 해결: DungeonManager.OnFloorChanged 이벤트 구독
//     던전 로드가 완전히 완료된 시점에 현재 타일을 체크하므로
//     Start() 순서에 무관하게 항상 정상 동작
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

        // OnFloorChanged 구독
        // DungeonManager가 던전 로드를 완전히 마친 뒤 이 이벤트를 발신하므로
        // Grid와 플레이어 위치가 모두 확정된 상태에서 체크 가능
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

    /// <summary>
    /// 층 로드 완료 시 호출된다.
    /// 플레이어 시작 위치에 오브젝트가 있으면 즉시 UI를 표시한다.
    /// </summary>
    private void OnFloorChanged(int floorIndex)
    {
        // UI 초기화 (이전 층의 UI가 남아있을 수 있음)
        if (interactionUI != null) interactionUI.Hide();

        // 던전 로드 완료 시점 → Grid, 플레이어 위치 모두 확정됨
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

    /// <summary>
    /// 현재 플레이어 위치의 타일을 직접 조회한다.
    /// </summary>
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
}

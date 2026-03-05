// ============================================================
// StairSystem.cs — 계단 상호작용 시스템
// 위치: Assets/Scripts/Events/StairSystem.cs
// ============================================================
// [신규 스크립트]
//   플레이어가 계단 타일 위에 서 있을 때 자기 타일을 클릭하면
//   해당 계단의 방향에 따라 층을 이동한다.
//
//   - 내려가는 계단(EXIT) 위에서 클릭 → 다음 층으로
//   - 올라가는 계단(START) 위에서 클릭 → 이전 층으로
// ============================================================
using UnityEngine;

public class StairSystem : MonoBehaviour
{
    [Header("참조")]
    [Tooltip("DungeonManager 참조")]
    public DungeonManager dungeonManager;

    [Tooltip("MovementSystem 참조")]
    public MovementSystem movementSystem;

    private Camera mainCamera;

    private void Start()
    {
        mainCamera = Camera.main;
    }

    private void Update()
    {
        if (!Input.GetMouseButtonDown(0)) return;
        if (mainCamera == null) return;
        if (movementSystem == null || dungeonManager == null) return;

        // 이동 중이면 무시 (MovementSystem이 정지 처리)
        if (movementSystem.IsMoving) return;

        Vector3 worldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        Vector2Int clickedTile = new Vector2Int(
            Mathf.FloorToInt(worldPos.x),
            Mathf.FloorToInt(worldPos.y)
        );

        // 플레이어가 있는 타일을 클릭했는지 확인
        if (clickedTile != movementSystem.CurrentTilePosition)
            return;

        // 현재 타일이 계단인지 확인
        Vector2Int playerPos = movementSystem.CurrentTilePosition;

        if (playerPos == dungeonManager.ExitPosition)
        {
            // 내려가는 계단 → 다음 층
            Debug.Log("[StairSystem] 내려가는 계단 사용! 다음 층으로 이동합니다.");
            dungeonManager.GoToNextFloor();
        }
        else if (playerPos == dungeonManager.StartPosition)
        {
            // 올라가는 계단 → 이전 층
            if (dungeonManager.CurrentFloorIndex <= 0)
            {
                Debug.Log("[StairSystem] 최상위 층입니다. 더 올라갈 수 없습니다.");
                return;
            }

            Debug.Log("[StairSystem] 올라가는 계단 사용! 이전 층으로 이동합니다.");
            dungeonManager.GoToPreviousFloor();
        }
    }
}

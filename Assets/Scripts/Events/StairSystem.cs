// ============================================================
// StairSystem.cs — 계단 상호작용 시스템
// 위치: Assets/Scripts/Events/StairSystem.cs
// ============================================================
// [수정] 층 이동 전 movementSystem.LockInput() 호출 추가
//   - GoToNextFloor / GoToPreviousFloor 호출 직전에 LockInput()
//   - 같은 프레임에 MovementSystem.HandleClickInput()이
//     동일 클릭을 이동 명령으로 처리하는 것을 차단
//   - UnlockInput()은 DungeonManager.GenerateAndLoadFloor() 완료 시 자동 호출
// ============================================================
using UnityEngine;

public class StairSystem : MonoBehaviour
{
    [Header("참조")]
    public DungeonManager dungeonManager;
    public MovementSystem movementSystem;

    private Camera mainCamera;

    private void Start()
    {
        mainCamera = Camera.main;
    }

    private void Update()
    {
        if (!Input.GetMouseButtonDown(0)) return;
        if (mainCamera    == null)        return;
        if (movementSystem == null || dungeonManager == null) return;
        if (movementSystem.IsMoving)      return;

        Vector3 worldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        Vector2Int clickedTile = new Vector2Int(
            Mathf.FloorToInt(worldPos.x),
            Mathf.FloorToInt(worldPos.y)
        );

        // 플레이어가 서 있는 타일을 클릭했는지 확인
        if (clickedTile != movementSystem.CurrentTilePosition) return;

        Vector2Int playerPos = movementSystem.CurrentTilePosition;

        if (playerPos == dungeonManager.ExitPosition)
        {
            Debug.Log("[StairSystem] 내려가는 계단 — 다음 층으로 이동");
            // 층 이동 전 입력 잠금: 같은 프레임 MovementSystem 이동 차단
            movementSystem.LockInput();
            dungeonManager.GoToNextFloor();
        }
        else if (playerPos == dungeonManager.StartPosition)
        {
            if (dungeonManager.CurrentFloorIndex <= 0)
            {
                Debug.Log("[StairSystem] 최상위 층 — 더 올라갈 수 없습니다.");
                return;
            }

            Debug.Log("[StairSystem] 올라가는 계단 — 이전 층으로 이동");
            movementSystem.LockInput();
            dungeonManager.GoToPreviousFloor();
        }
    }
}

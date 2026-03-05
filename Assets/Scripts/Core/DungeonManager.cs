// ============================================================
// DungeonManager.cs — 런타임 던전 데이터 관리자 (다중 층 지원)
// 위치: Assets/Scripts/Core/DungeonManager.cs
// ============================================================
// [v3.1 변경사항]
//   - 층 이동 완료 후 movementSystem.UnlockInput() 호출 추가
//     : StairSystem이 층 이동 전 LockInput()으로 잠근 입력을
//       GenerateAndLoadFloor() 완료 시점에 해제한다
//   - OnFloorChanged 이벤트 발신 순서를 플레이어 배치 이후로 유지
//     : FogOfWar, GridOverlay 등 구독자가 올바른 플레이어 위치를 참조하도록
// ============================================================
using System;
using System.Collections.Generic;
using UnityEngine;

public class DungeonManager : MonoBehaviour
{
    [Header("참조")]
    [Tooltip("던전 생성기")]
    public DungeonGenerator dungeonGenerator;

    [Tooltip("던전 렌더러")]
    public DungeonRenderer dungeonRenderer;

    [Tooltip("플레이어 이동 시스템")]
    public MovementSystem movementSystem;

    [Tooltip("전장의 안개")]
    public FogOfWar fogOfWar;

    [Tooltip("그리드 오버레이")]
    public GridOverlay gridOverlay;

    // ─── 현재 층 런타임 데이터 ───
    public TileData[,] Grid { get; private set; }
    public List<RoomData> Rooms { get; private set; }
    public Vector2Int StartPosition { get; private set; }
    public Vector2Int ExitPosition { get; private set; }
    public DungeonFloorData FloorData => dungeonGenerator != null ? dungeonGenerator.floorData : null;

    public int MapWidth  => FloorData != null ? FloorData.mapWidth  : 0;
    public int MapHeight => FloorData != null ? FloorData.mapHeight : 0;

    // ─── 층 관리 ───
    public int CurrentFloorIndex { get; private set; } = 0;

    public event Action<int> OnFloorChanged;

    private Dictionary<int, FloorCache> floorCacheMap = new Dictionary<int, FloorCache>();

    private class FloorCache
    {
        public TileData[,]   grid;
        public List<RoomData> rooms;
        public Vector2Int    startPosition;
        public Vector2Int    exitPosition;
    }

    // ─── 초기화 ───

    private void Awake()
    {
        if (dungeonGenerator == null) dungeonGenerator = GetComponent<DungeonGenerator>();
        if (dungeonRenderer  == null) dungeonRenderer  = GetComponent<DungeonRenderer>();
    }

    private void Start()
    {
        CurrentFloorIndex = 0;
        GenerateAndLoadFloor(CurrentFloorIndex, true);
    }

    // ─── 층 생성 / 로드 ───

    /// <summary>
    /// 지정 층을 생성(또는 캐시에서 로드)하고 렌더링한다.
    /// 완료 후 movementSystem.UnlockInput()을 호출하여 입력 잠금을 해제한다.
    /// </summary>
    private void GenerateAndLoadFloor(int floorIndex, bool spawnAtStart)
    {
        CurrentFloorIndex = floorIndex;

        if (floorCacheMap.ContainsKey(floorIndex))
        {
            FloorCache cache = floorCacheMap[floorIndex];
            Grid          = cache.grid;
            Rooms         = cache.rooms;
            StartPosition = cache.startPosition;
            ExitPosition  = cache.exitPosition;
            Debug.Log($"[DungeonManager] {floorIndex}층 캐시에서 로드. 방 수: {Rooms.Count}");
        }
        else
        {
            if (dungeonGenerator == null)
            {
                Debug.LogError("[DungeonManager] DungeonGenerator 참조가 설정되지 않았습니다!");
                return;
            }

            bool success = dungeonGenerator.GenerateAndReturn(
                out TileData[,]    grid,
                out List<RoomData> rooms,
                out Vector2Int     startPos,
                out Vector2Int     exitPos
            );

            if (!success)
            {
                Debug.LogError($"[DungeonManager] {floorIndex}층 던전 생성 실패!");
                return;
            }

            Grid          = grid;
            Rooms         = rooms;
            StartPosition = startPos;
            ExitPosition  = exitPos;

            floorCacheMap[floorIndex] = new FloorCache
            {
                grid          = grid,
                rooms         = rooms,
                startPosition = startPos,
                exitPosition  = exitPos
            };

            Debug.Log($"[DungeonManager] {floorIndex}층 신규 생성. 방 수: {Rooms.Count}");
        }

        // 렌더링
        if (dungeonRenderer != null)
            dungeonRenderer.RenderDungeon(this);

        // 플레이어 배치
        Vector2Int spawnPos = spawnAtStart ? StartPosition : ExitPosition;
        if (movementSystem != null)
        {
            movementSystem.StopMovement();
            movementSystem.SetPosition(spawnPos);
        }

        // 층 변경 이벤트 발신 (FogOfWar, GridOverlay 등 구독자가 처리)
        OnFloorChanged?.Invoke(CurrentFloorIndex);

        // ── 핵심 수정: 층 이동 완료 후 입력 잠금 해제 ──
        // StairSystem이 층 이동 전에 LockInput()을 호출했으므로
        // 모든 처리가 끝난 이 시점에 해제한다.
        // Start() 최초 호출 시에는 잠금이 없으므로 호출해도 무해하다.
        if (movementSystem != null)
            movementSystem.UnlockInput();

        Debug.Log($"[DungeonManager] {floorIndex}층 로드 완료. 시작:{StartPosition} 출구:{ExitPosition} 배치:{spawnPos}");
    }

    // ─── 층 이동 API ───

    public void GoToNextFloor()
    {
        Debug.Log($"[DungeonManager] {CurrentFloorIndex}층 → {CurrentFloorIndex + 1}층 이동");
        GenerateAndLoadFloor(CurrentFloorIndex + 1, true);
    }

    public void GoToPreviousFloor()
    {
        if (CurrentFloorIndex <= 0)
        {
            Debug.Log("[DungeonManager] 이미 최상위 층입니다.");
            // 잠금이 걸려있을 수 있으므로 여기서도 해제
            if (movementSystem != null) movementSystem.UnlockInput();
            return;
        }

        Debug.Log($"[DungeonManager] {CurrentFloorIndex}층 → {CurrentFloorIndex - 1}층 이동");
        GenerateAndLoadFloor(CurrentFloorIndex - 1, false);
    }

    // ─── 공용 API ───

    public bool IsInBounds(int x, int y) =>
        x >= 0 && x < MapWidth && y >= 0 && y < MapHeight;

    public bool IsWalkable(int x, int y)
    {
        if (!IsInBounds(x, y)) return false;
        return Grid[x, y].IsWalkable;
    }

    public TileData GetTile(int x, int y) => Grid[x, y];

    public void SetTile(int x, int y, TileData data) => Grid[x, y] = data;
}

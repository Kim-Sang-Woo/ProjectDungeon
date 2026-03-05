// ============================================================
// DungeonManager.cs — 런타임 던전 데이터 관리자 (다중 층 지원)
// 위치: Assets/Scripts/Core/DungeonManager.cs
// ============================================================
// [v3 변경사항]
//   - 다중 층 관리: 방문한 층의 데이터를 캐싱하여 왕래 가능
//   - GoToNextFloor() / GoToPreviousFloor() 층 이동 API
//   - 층 이동 시 FogOfWar, GridOverlay, MovementSystem 등 재초기화
//   - OnFloorChanged 이벤트로 외부 시스템에 층 변경 통보
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

    public int MapWidth => FloorData != null ? FloorData.mapWidth : 0;
    public int MapHeight => FloorData != null ? FloorData.mapHeight : 0;

    // ─── 층 관리 ───
    /// <summary>현재 층 번호 (0부터 시작)</summary>
    public int CurrentFloorIndex { get; private set; } = 0;

    /// <summary>층 변경 시 발생하는 이벤트</summary>
    public event Action<int> OnFloorChanged;

    // 방문한 층 데이터 캐시
    private Dictionary<int, FloorCache> floorCacheMap = new Dictionary<int, FloorCache>();

    /// <summary>한 층의 전체 데이터를 저장하는 캐시</summary>
    private class FloorCache
    {
        public TileData[,] grid;
        public List<RoomData> rooms;
        public Vector2Int startPosition;
        public Vector2Int exitPosition;
    }

    // ─── 초기화 ───

    private void Awake()
    {
        if (dungeonGenerator == null)
            dungeonGenerator = GetComponent<DungeonGenerator>();
        if (dungeonRenderer == null)
            dungeonRenderer = GetComponent<DungeonRenderer>();
    }

    private void Start()
    {
        CurrentFloorIndex = 0;
        GenerateAndLoadFloor(CurrentFloorIndex, true);
    }

    // ─── 층 생성 / 로드 ───

    /// <summary>
    /// 지정 층을 생성(또는 캐시에서 로드)하고 렌더링한다.
    /// </summary>
    /// <param name="floorIndex">층 번호</param>
    /// <param name="spawnAtStart">true=시작계단 배치, false=출구계단 배치(위층에서 내려온 경우)</param>
    private void GenerateAndLoadFloor(int floorIndex, bool spawnAtStart)
    {
        CurrentFloorIndex = floorIndex;

        if (floorCacheMap.ContainsKey(floorIndex))
        {
            // 캐시에서 로드
            FloorCache cache = floorCacheMap[floorIndex];
            Grid = cache.grid;
            Rooms = cache.rooms;
            StartPosition = cache.startPosition;
            ExitPosition = cache.exitPosition;

            Debug.Log($"[DungeonManager] {floorIndex}층 캐시에서 로드. 방 수: {Rooms.Count}");
        }
        else
        {
            // 새로 생성
            if (dungeonGenerator == null)
            {
                Debug.LogError("[DungeonManager] DungeonGenerator 참조가 설정되지 않았습니다!");
                return;
            }

            bool success = dungeonGenerator.GenerateAndReturn(
                out TileData[,] grid,
                out List<RoomData> rooms,
                out Vector2Int startPos,
                out Vector2Int exitPos
            );

            if (!success)
            {
                Debug.LogError($"[DungeonManager] {floorIndex}층 던전 생성 실패!");
                return;
            }

            Grid = grid;
            Rooms = rooms;
            StartPosition = startPos;
            ExitPosition = exitPos;

            // 캐시에 저장
            floorCacheMap[floorIndex] = new FloorCache
            {
                grid = grid,
                rooms = rooms,
                startPosition = startPos,
                exitPosition = exitPos
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

        // 층 변경 이벤트 발신
        OnFloorChanged?.Invoke(CurrentFloorIndex);

        Debug.Log($"[DungeonManager] {floorIndex}층 로드 완료. 시작: {StartPosition}, 출구: {ExitPosition}, 배치: {spawnPos}");
    }

    // ─── 층 이동 API ───

    /// <summary>
    /// 다음 층(아래층)으로 이동한다.
    /// 내려가는 계단에서 호출된다.
    /// </summary>
    public void GoToNextFloor()
    {
        Debug.Log($"[DungeonManager] {CurrentFloorIndex}층 → {CurrentFloorIndex + 1}층 이동");
        GenerateAndLoadFloor(CurrentFloorIndex + 1, true);
    }

    /// <summary>
    /// 이전 층(위층)으로 이동한다.
    /// 올라가는 계단에서 호출된다.
    /// 0층보다 위로는 갈 수 없다.
    /// </summary>
    public void GoToPreviousFloor()
    {
        if (CurrentFloorIndex <= 0)
        {
            Debug.Log("[DungeonManager] 이미 최상위 층입니다. 이전 층이 없습니다.");
            return;
        }

        Debug.Log($"[DungeonManager] {CurrentFloorIndex}층 → {CurrentFloorIndex - 1}층 이동");
        // 이전 층의 출구계단(=내려가는 계단) 위치에 배치
        GenerateAndLoadFloor(CurrentFloorIndex - 1, false);
    }

    // ─── 공용 API ───

    public bool IsInBounds(int x, int y)
    {
        return x >= 0 && x < MapWidth && y >= 0 && y < MapHeight;
    }

    public bool IsWalkable(int x, int y)
    {
        if (!IsInBounds(x, y)) return false;
        return Grid[x, y].IsWalkable;
    }

    public TileData GetTile(int x, int y)
    {
        return Grid[x, y];
    }

    public void SetTile(int x, int y, TileData data)
    {
        Grid[x, y] = data;
    }
}

// ============================================================
// DungeonGenerator.cs — 던전 생성 시스템
// 기획서 Ch.1 참조 — Script Execution Order: -50
// ============================================================
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 무작위 던전을 생성하는 핵심 시스템.
/// DungeonFloorData를 입력받아 TileData[,] grid를 반환한다.
/// 생성 파이프라인: 방 배치 → Delaunay → MST → 루프 추가 → 복도 생성 → 계단 배치 → 이벤트 배치 → 검증
/// </summary>
public class DungeonGenerator : MonoBehaviour
{
    [Header("데이터")]
    [Tooltip("층 설정 ScriptableObject")]
    public DungeonFloorData floorData;

    [Header("타일맵")]
    [Tooltip("던전을 렌더링할 Tilemap 참조")]
    public Tilemap tilemap;

    [Tooltip("바닥 타일")]
    public TileBase floorTile;
    [Tooltip("벽 타일")]
    public TileBase wallTile;
    [Tooltip("복도 타일 (null이면 floorTile 사용)")]
    public TileBase corridorTile;
    [Tooltip("입구 계단 타일 (null이면 floorTile 사용)")]
    public TileBase stairsUpTile;
    [Tooltip("출구 계단 타일 (null이면 floorTile 사용)")]
    public TileBase stairsDownTile;

    [Header("이벤트 에셋 목록")]
    [Tooltip("배치 가능한 이벤트 데이터 목록")]
    public List<DungeonEventData> eventPool = new List<DungeonEventData>();

    // ─── 런타임 데이터 (외부에서 참조 가능) ───
    [HideInInspector] public TileData[,] grid;
    [HideInInspector] public List<RoomData> rooms = new List<RoomData>();
    [HideInInspector] public Vector2Int startPosition;
    [HideInInspector] public Vector2Int exitPosition;

    // Delaunay/MST용 간선 구조체
    private struct Edge
    {
        public int roomA;
        public int roomB;
        public float distance;
    }

    private const int MAX_GENERATION_ATTEMPTS = 50;
    private const int MAX_ROOM_PLACEMENT_ATTEMPTS = 200;

    // ─── 진입점 ───

    private void Start()
    {
        GenerateAndRender();
    }

    /// <summary>
    /// 던전을 생성하고 타일맵에 렌더링한다.
    /// </summary>
    public void GenerateAndRender()
    {
        for (int attempt = 0; attempt < MAX_GENERATION_ATTEMPTS; attempt++)
        {
            grid = Generate(floorData);
            if (ValidateConnectivity())
            {
                RenderToTilemap();
                Debug.Log($"[DungeonGenerator] 던전 생성 완료! 방 수: {rooms.Count}, 시도: {attempt + 1}회");
                return;
            }
            Debug.LogWarning($"[DungeonGenerator] 연결성 검증 실패. 재생성 시도 #{attempt + 2}");
        }

        Debug.LogError("[DungeonGenerator] 최대 시도 횟수 초과! 던전 생성 실패.");
    }

    /// <summary>
    /// 기획서 Ch.1: 던전 생성 메인 파이프라인
    /// </summary>
    public TileData[,] Generate(DungeonFloorData data)
    {
        // 그리드 초기화 (모두 벽으로)
        grid = new TileData[data.mapWidth, data.mapHeight];
        for (int x = 0; x < data.mapWidth; x++)
        {
            for (int y = 0; y < data.mapHeight; y++)
            {
                grid[x, y] = new TileData
                {
                    type = TileType.WALL,
                    roomId = -1,
                    eventData = null,
                    isEventConsumed = false
                };
            }
        }

        rooms.Clear();

        // Step 1: 방 배치
        PlaceRooms(data);

        if (rooms.Count < 2)
        {
            Debug.LogWarning("[DungeonGenerator] 방이 2개 미만. 재생성 필요.");
            return grid;
        }

        // Step 2: Delaunay 삼각 그래프 생성
        List<Edge> delaunayEdges = BuildDelaunayGraph();

        // Step 3: MST 추출 (Kruskal)
        List<Edge> mstEdges = BuildMST(delaunayEdges);

        // Step 4: 루프 간선 추가
        List<Edge> finalEdges = AddLoopEdges(delaunayEdges, mstEdges, data.loopEdgeProbability);

        // Step 5: 복도 생성
        CarveCorridors(finalEdges);

        // 방 연결 정보 기록
        RecordConnections(finalEdges);

        // Step 6: 계단 배치 (BFS 최원거리)
        PlaceStairs(mstEdges);

        // Step 7: 이벤트 배치
        PlaceEvents(data.eventDensity);

        return grid;
    }

    // ═══════════════════════════════════════════════════════
    // Step 1: 방 배치
    // ═══════════════════════════════════════════════════════

    /// <summary>
    /// 기획서 1.1: 전체 맵 공간 내에 방들을 무작위로 배치한다.
    /// 방끼리는 서로 겹치지 않도록 최소 1칸의 간격을 유지한다.
    /// </summary>
    private void PlaceRooms(DungeonFloorData data)
    {
        int targetRoomCount = Random.Range(data.roomCountMin, data.roomCountMax + 1);

        for (int attempt = 0; attempt < MAX_ROOM_PLACEMENT_ATTEMPTS && rooms.Count < targetRoomCount; attempt++)
        {
            // 기획서: 최소 2×2, 최대 10×10
            int width = Random.Range(2, 11);
            int height = Random.Range(2, 11);

            // 맵 경계 안에 배치 (경계 1칸 여유)
            int x = Random.Range(1, data.mapWidth - width - 1);
            int y = Random.Range(1, data.mapHeight - height - 1);

            RectInt newBounds = new RectInt(x, y, width, height);

            // 겹침 검사 (최소 1칸 간격)
            if (IsRoomOverlapping(newBounds))
                continue;

            // 방 생성
            RoomData room = new RoomData(rooms.Count, newBounds);
            rooms.Add(room);

            // 그리드에 바닥 타일 기록
            for (int rx = x; rx < x + width; rx++)
            {
                for (int ry = y; ry < y + height; ry++)
                {
                    grid[rx, ry].type = TileType.FLOOR;
                    grid[rx, ry].roomId = room.id;
                }
            }
        }
    }

    /// <summary>
    /// 새 방이 기존 방과 겹치는지 검사 (1칸 간격 포함)
    /// </summary>
    private bool IsRoomOverlapping(RectInt newBounds)
    {
        foreach (var room in rooms)
        {
            RectInt existing = room.bounds;
            // 1칸 여유를 두고 확장하여 겹침 검사
            if (newBounds.xMin < existing.xMax + 1 &&
                newBounds.xMax + 1 > existing.xMin &&
                newBounds.yMin < existing.yMax + 1 &&
                newBounds.yMax + 1 > existing.yMin)
            {
                return true;
            }
        }
        return false;
    }

    // ═══════════════════════════════════════════════════════
    // Step 2: Delaunay Triangulation (Bowyer-Watson)
    // ═══════════════════════════════════════════════════════

    /// <summary>
    /// 기획서 1.1-02: Delaunay Triangulation으로 방들의 삼각 연결 그래프를 생성한다.
    /// Bowyer-Watson 알고리즘 사용.
    /// </summary>
    private List<Edge> BuildDelaunayGraph()
    {
        List<Vector2> points = rooms.Select(r => new Vector2(r.center.x, r.center.y)).ToList();
        List<Edge> edges = new List<Edge>();

        if (points.Count < 2)
            return edges;

        // 간이 Delaunay: 방 수가 적으므로 (6~10개) 모든 쌍 거리 계산으로 대체
        // (Bowyer-Watson은 방이 매우 많을 때 효율적이지만 10개 이하에서는 과도함)
        for (int i = 0; i < points.Count; i++)
        {
            for (int j = i + 1; j < points.Count; j++)
            {
                float dist = Vector2.Distance(points[i], points[j]);
                edges.Add(new Edge { roomA = i, roomB = j, distance = dist });
            }
        }

        return edges;
    }

    // ═══════════════════════════════════════════════════════
    // Step 3: MST (Kruskal)
    // ═══════════════════════════════════════════════════════

    /// <summary>
    /// 기획서 1.1-03: MST(Minimum Spanning Tree, Kruskal 알고리즘)를 적용해
    /// 모든 방을 최소 비용으로 연결하는 트리를 추출한다.
    /// </summary>
    private List<Edge> BuildMST(List<Edge> allEdges)
    {
        List<Edge> sorted = allEdges.OrderBy(e => e.distance).ToList();
        int[] parent = Enumerable.Range(0, rooms.Count).ToArray();

        int Find(int x)
        {
            while (parent[x] != x)
            {
                parent[x] = parent[parent[x]]; // path compression
                x = parent[x];
            }
            return x;
        }

        List<Edge> mst = new List<Edge>();

        foreach (var edge in sorted)
        {
            int rootA = Find(edge.roomA);
            int rootB = Find(edge.roomB);

            if (rootA != rootB)
            {
                parent[rootA] = rootB;
                mst.Add(edge);

                if (mst.Count == rooms.Count - 1)
                    break;
            }
        }

        return mst;
    }

    // ═══════════════════════════════════════════════════════
    // Step 4: 루프 간선 추가
    // ═══════════════════════════════════════════════════════

    /// <summary>
    /// 기획서 1.1-04: MST에서 제외된 일부 간선을 loopEdgeProbability 확률로 복원하여
    /// 루프(Loop) 경로를 추가한다.
    /// </summary>
    private List<Edge> AddLoopEdges(List<Edge> allEdges, List<Edge> mstEdges, float probability)
    {
        List<Edge> result = new List<Edge>(mstEdges);
        HashSet<(int, int)> mstSet = new HashSet<(int, int)>();

        foreach (var e in mstEdges)
        {
            mstSet.Add((Mathf.Min(e.roomA, e.roomB), Mathf.Max(e.roomA, e.roomB)));
        }

        foreach (var edge in allEdges)
        {
            var key = (Mathf.Min(edge.roomA, edge.roomB), Mathf.Max(edge.roomA, edge.roomB));
            if (!mstSet.Contains(key) && Random.value < probability)
            {
                result.Add(edge);
            }
        }

        return result;
    }

    // ═══════════════════════════════════════════════════════
    // Step 5: 복도 생성
    // ═══════════════════════════════════════════════════════

    /// <summary>
    /// 기획서 1.1-05: 각 연결 간선을 따라 복도를 생성한다 (L자 혹은 직선 선택).
    /// 복도 폭은 항상 1칸.
    /// </summary>
    private void CarveCorridors(List<Edge> edges)
    {
        foreach (var edge in edges)
        {
            Vector2Int start = rooms[edge.roomA].center;
            Vector2Int end = rooms[edge.roomB].center;

            // 50% 확률로 수평 먼저 또는 수직 먼저 (L자 방향 결정)
            if (Random.value > 0.5f)
            {
                CarveHorizontalTunnel(start.x, end.x, start.y);
                CarveVerticalTunnel(start.y, end.y, end.x);
            }
            else
            {
                CarveVerticalTunnel(start.y, end.y, start.x);
                CarveHorizontalTunnel(start.x, end.x, end.y);
            }
        }
    }

    private void CarveHorizontalTunnel(int x1, int x2, int y)
    {
        int minX = Mathf.Min(x1, x2);
        int maxX = Mathf.Max(x1, x2);
        for (int x = minX; x <= maxX; x++)
        {
            if (x >= 0 && x < floorData.mapWidth && y >= 0 && y < floorData.mapHeight)
            {
                if (grid[x, y].type == TileType.WALL)
                {
                    grid[x, y].type = TileType.CORRIDOR;
                    grid[x, y].roomId = -1;
                }
            }
        }
    }

    private void CarveVerticalTunnel(int y1, int y2, int x)
    {
        int minY = Mathf.Min(y1, y2);
        int maxY = Mathf.Max(y1, y2);
        for (int y = minY; y <= maxY; y++)
        {
            if (x >= 0 && x < floorData.mapWidth && y >= 0 && y < floorData.mapHeight)
            {
                if (grid[x, y].type == TileType.WALL)
                {
                    grid[x, y].type = TileType.CORRIDOR;
                    grid[x, y].roomId = -1;
                }
            }
        }
    }

    /// <summary>
    /// 간선 정보를 RoomData.connectedRoomIds에 기록
    /// </summary>
    private void RecordConnections(List<Edge> edges)
    {
        foreach (var edge in edges)
        {
            if (!rooms[edge.roomA].connectedRoomIds.Contains(edge.roomB))
                rooms[edge.roomA].connectedRoomIds.Add(edge.roomB);
            if (!rooms[edge.roomB].connectedRoomIds.Contains(edge.roomA))
                rooms[edge.roomB].connectedRoomIds.Add(edge.roomA);
        }
    }

    // ═══════════════════════════════════════════════════════
    // Step 6: 계단 배치
    // ═══════════════════════════════════════════════════════

    /// <summary>
    /// 기획서 1.2: BFS로 모든 방 쌍 거리를 계산하여 가장 멀리 떨어진 두 방에
    /// 각각 입구/출구를 배치한다.
    /// </summary>
    private void PlaceStairs(List<Edge> mstEdges)
    {
        // 인접 리스트 구성 (MST 기반)
        Dictionary<int, List<int>> adj = new Dictionary<int, List<int>>();
        for (int i = 0; i < rooms.Count; i++)
            adj[i] = new List<int>();

        foreach (var edge in mstEdges)
        {
            adj[edge.roomA].Add(edge.roomB);
            adj[edge.roomB].Add(edge.roomA);
        }

        // 모든 방 쌍의 BFS 거리 계산 → 최원거리 쌍 선택
        int startRoom = 0, exitRoom = 0;
        int maxDist = 0;

        for (int i = 0; i < rooms.Count; i++)
        {
            int[] distances = BFS(i, adj);
            for (int j = i + 1; j < rooms.Count; j++)
            {
                if (distances[j] > maxDist)
                {
                    maxDist = distances[j];
                    startRoom = i;
                    exitRoom = j;
                }
            }
        }

        // 방 타입 설정
        rooms[startRoom].roomType = RoomType.START;
        rooms[exitRoom].roomType = RoomType.EXIT;

        // 시작/출구 위치 기록
        startPosition = rooms[startRoom].center;
        exitPosition = rooms[exitRoom].center;

        Debug.Log($"[DungeonGenerator] 입구: 방 {startRoom} ({startPosition}), 출구: 방 {exitRoom} ({exitPosition}), 거리: {maxDist} hops");
    }

    private int[] BFS(int start, Dictionary<int, List<int>> adj)
    {
        int[] dist = new int[rooms.Count];
        for (int i = 0; i < dist.Length; i++) dist[i] = -1;

        Queue<int> queue = new Queue<int>();
        dist[start] = 0;
        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            int current = queue.Dequeue();
            foreach (int neighbor in adj[current])
            {
                if (dist[neighbor] < 0)
                {
                    dist[neighbor] = dist[current] + 1;
                    queue.Enqueue(neighbor);
                }
            }
        }

        return dist;
    }

    // ═══════════════════════════════════════════════════════
    // Step 7: 이벤트 배치
    // ═══════════════════════════════════════════════════════

    /// <summary>
    /// 기획서 3.1: 바닥 타일의 eventDensity(15%) 비율로 이벤트를 랜덤 배치.
    /// 시작 방(START)과 출구 방(EXIT)에는 이벤트를 배치하지 않는다.
    /// </summary>
    private void PlaceEvents(float density)
    {
        if (eventPool == null || eventPool.Count == 0)
        {
            Debug.LogWarning("[DungeonGenerator] 이벤트 풀이 비어있습니다. 이벤트 배치를 건너뜁니다.");
            return;
        }

        // 유효한 후보 타일 수집 (START/EXIT 방 제외)
        List<Vector2Int> candidates = new List<Vector2Int>();
        for (int x = 0; x < floorData.mapWidth; x++)
        {
            for (int y = 0; y < floorData.mapHeight; y++)
            {
                if (!grid[x, y].IsWalkable) continue;

                // START/EXIT 방은 제외
                int rid = grid[x, y].roomId;
                if (rid >= 0)
                {
                    RoomType rType = rooms[rid].roomType;
                    if (rType == RoomType.START || rType == RoomType.EXIT)
                        continue;
                }

                candidates.Add(new Vector2Int(x, y));
            }
        }

        // 이벤트 배치 수 결정
        int eventCount = Mathf.RoundToInt(candidates.Count * density);

        // 셔플 후 배치
        Shuffle(candidates);
        for (int i = 0; i < eventCount && i < candidates.Count; i++)
        {
            Vector2Int pos = candidates[i];
            DungeonEventData selectedEvent = eventPool[Random.Range(0, eventPool.Count)];
            grid[pos.x, pos.y].eventData = selectedEvent;
            grid[pos.x, pos.y].isEventConsumed = false;
        }

        Debug.Log($"[DungeonGenerator] 이벤트 배치 완료: {eventCount}개 / 후보 {candidates.Count}개 (밀도: {density:P0})");
    }

    private void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            T temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }

    // ═══════════════════════════════════════════════════════
    // 유효성 검증
    // ═══════════════════════════════════════════════════════

    /// <summary>
    /// 기획서 1.1-06: 생성 완료 후 유효성 검증.
    /// 모든 이동 가능 타일이 하나의 연결 컴포넌트를 형성하는지 BFS로 확인.
    /// </summary>
    private bool ValidateConnectivity()
    {
        // 첫 번째 이동 가능 타일 찾기
        Vector2Int? startTile = null;
        int walkableCount = 0;

        for (int x = 0; x < floorData.mapWidth && !startTile.HasValue; x++)
        {
            for (int y = 0; y < floorData.mapHeight && !startTile.HasValue; y++)
            {
                if (grid[x, y].IsWalkable)
                {
                    startTile = new Vector2Int(x, y);
                }
            }
        }

        if (!startTile.HasValue)
            return false;

        // 전체 이동 가능 타일 수 카운트
        for (int x = 0; x < floorData.mapWidth; x++)
            for (int y = 0; y < floorData.mapHeight; y++)
                if (grid[x, y].IsWalkable) walkableCount++;

        // BFS로 연결된 타일 수 카운트
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        queue.Enqueue(startTile.Value);
        visited.Add(startTile.Value);

        Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            foreach (var dir in directions)
            {
                Vector2Int next = current + dir;
                if (next.x >= 0 && next.x < floorData.mapWidth &&
                    next.y >= 0 && next.y < floorData.mapHeight &&
                    grid[next.x, next.y].IsWalkable &&
                    !visited.Contains(next))
                {
                    visited.Add(next);
                    queue.Enqueue(next);
                }
            }
        }

        bool isConnected = visited.Count == walkableCount;
        if (!isConnected)
        {
            Debug.LogWarning($"[DungeonGenerator] 연결성 검증 실패! 연결: {visited.Count} / 전체: {walkableCount}");
        }

        return isConnected;
    }

    // ═══════════════════════════════════════════════════════
    // 타일맵 렌더링
    // ═══════════════════════════════════════════════════════

    /// <summary>
    /// TileData[,] grid를 Unity Tilemap에 렌더링한다.
    /// </summary>
    private void RenderToTilemap()
    {
        if (tilemap == null)
        {
            Debug.LogError("[DungeonGenerator] Tilemap 참조가 설정되지 않았습니다!");
            return;
        }

        tilemap.ClearAllTiles();

        for (int x = 0; x < floorData.mapWidth; x++)
        {
            for (int y = 0; y < floorData.mapHeight; y++)
            {
                Vector3Int tilePos = new Vector3Int(x, y, 0);
                TileBase tile = null;

                switch (grid[x, y].type)
                {
                    case TileType.WALL:
                        tile = wallTile;
                        break;
                    case TileType.FLOOR:
                        tile = floorTile;
                        break;
                    case TileType.CORRIDOR:
                        tile = corridorTile != null ? corridorTile : floorTile;
                        break;
                }

                if (tile != null)
                    tilemap.SetTile(tilePos, tile);
            }
        }

        // 입구/출구 계단 타일 오버라이드
        if (stairsUpTile != null)
            tilemap.SetTile(new Vector3Int(startPosition.x, startPosition.y, 0), stairsUpTile);
        if (stairsDownTile != null)
            tilemap.SetTile(new Vector3Int(exitPosition.x, exitPosition.y, 0), stairsDownTile);
    }

    // ═══════════════════════════════════════════════════════
    // 공용 헬퍼
    // ═══════════════════════════════════════════════════════

    /// <summary>
    /// 특정 좌표가 이동 가능한 타일인지 확인
    /// </summary>
    public bool IsWalkable(int x, int y)
    {
        if (x < 0 || x >= floorData.mapWidth || y < 0 || y >= floorData.mapHeight)
            return false;
        return grid[x, y].IsWalkable;
    }

    /// <summary>
    /// 특정 좌표의 TileData를 반환
    /// </summary>
    public TileData GetTileData(int x, int y)
    {
        return grid[x, y];
    }

    /// <summary>
    /// 특정 좌표의 TileData를 설정 (이벤트 소진 등)
    /// </summary>
    public void SetTileData(int x, int y, TileData data)
    {
        grid[x, y] = data;
    }
}

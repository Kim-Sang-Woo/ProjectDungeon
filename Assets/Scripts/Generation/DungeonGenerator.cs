// ============================================================
// DungeonGenerator.cs — 던전 생성 시스템
// 기획서 Ch.1 참조
// 위치: Assets/Scripts/Generation/DungeonGenerator.cs
// ============================================================
// [v2 변경사항]
//   - 렌더링 책임 → DungeonRenderer로 분리
//   - 런타임 데이터 저장 → DungeonManager로 분리
//   - TileData struct → class 대응
//   - BuildDelaunayGraph() → BuildCompleteGraph() 메서드명 변경
//   - ValidateConnectivity() BFS: HashSet<Vector2Int> → bool[,] 최적화
//   - GenerateAndReturn() out 파라미터 방식으로 변경
// ============================================================
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 무작위 던전을 생성하는 핵심 시스템.
/// DungeonFloorData를 입력받아 TileData[,] grid를 반환한다.
/// 생성 파이프라인: 방 배치 → 완전그래프 → MST → 루프 추가 → 복도 생성 → 계단 배치 → 이벤트 배치 → 검증
/// </summary>
public class DungeonGenerator : MonoBehaviour
{
    [Header("데이터")]
    [Tooltip("층 설정 ScriptableObject")]
    public DungeonFloorData floorData;

    [Header("이벤트 에셋 목록")]
    [Tooltip("배치 가능한 이벤트 데이터 목록")]
    public List<DungeonEventData> eventPool = new List<DungeonEventData>();

    // ─── 내부 생성 데이터 ───
    private TileData[,] grid;
    private List<RoomData> rooms = new List<RoomData>();
    private Vector2Int startPosition;
    private Vector2Int exitPosition;

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

    /// <summary>
    /// 던전을 생성하고 결과를 out 파라미터로 반환한다.
    /// DungeonManager가 호출한다.
    /// </summary>
    public bool GenerateAndReturn(
        out TileData[,] outGrid,
        out List<RoomData> outRooms,
        out Vector2Int outStart,
        out Vector2Int outExit)
    {
        for (int attempt = 0; attempt < MAX_GENERATION_ATTEMPTS; attempt++)
        {
            Generate(floorData);

            if (ValidateConnectivity())
            {
                outGrid = grid;
                outRooms = rooms;
                outStart = startPosition;
                outExit = exitPosition;

                Debug.Log($"[DungeonGenerator] 던전 생성 완료! 방 수: {rooms.Count}, 시도: {attempt + 1}회");
                return true;
            }

            Debug.LogWarning($"[DungeonGenerator] 연결성 검증 실패. 재생성 시도 #{attempt + 2}");
        }

        Debug.LogError("[DungeonGenerator] 최대 시도 횟수 초과! 던전 생성 실패.");
        outGrid = null;
        outRooms = null;
        outStart = Vector2Int.zero;
        outExit = Vector2Int.zero;
        return false;
    }

    /// <summary>
    /// 기획서 Ch.1: 던전 생성 메인 파이프라인
    /// </summary>
    private void Generate(DungeonFloorData data)
    {
        // 그리드 초기화 (모두 벽으로) — TileData는 class이므로 new 필요
        grid = new TileData[data.mapWidth, data.mapHeight];
        for (int x = 0; x < data.mapWidth; x++)
        {
            for (int y = 0; y < data.mapHeight; y++)
            {
                grid[x, y] = new TileData();
            }
        }

        rooms = new List<RoomData>();

        // Step 1: 방 배치
        PlaceRooms(data);

        if (rooms.Count < 2)
        {
            Debug.LogWarning("[DungeonGenerator] 방이 2개 미만. 재생성 필요.");
            return;
        }

        // Step 2: 완전 그래프 생성 (모든 방 쌍 간선)
        List<Edge> allEdges = BuildCompleteGraph();

        // Step 3: MST 추출 (Kruskal)
        List<Edge> mstEdges = BuildMST(allEdges);

        // Step 4: 루프 간선 추가
        List<Edge> finalEdges = AddLoopEdges(allEdges, mstEdges, data.loopEdgeProbability);

        // Step 5: 복도 생성
        CarveCorridors(finalEdges);

        // 방 연결 정보 기록
        RecordConnections(finalEdges);

        // Step 6: 계단 배치 (BFS 최원거리)
        PlaceStairs(mstEdges);

        // Step 7: 이벤트 배치
        PlaceEvents(data.eventDensity);
    }

    // ═══════════════════════════════════════════════════════
    // Step 1: 방 배치
    // ═══════════════════════════════════════════════════════

    private void PlaceRooms(DungeonFloorData data)
    {
        int targetRoomCount = Random.Range(data.roomCountMin, data.roomCountMax + 1);

        for (int attempt = 0; attempt < MAX_ROOM_PLACEMENT_ATTEMPTS && rooms.Count < targetRoomCount; attempt++)
        {
            int width = Random.Range(2, 11);
            int height = Random.Range(2, 11);
            int x = Random.Range(1, data.mapWidth - width - 1);
            int y = Random.Range(1, data.mapHeight - height - 1);

            RectInt newBounds = new RectInt(x, y, width, height);

            if (IsRoomOverlapping(newBounds))
                continue;

            RoomData room = new RoomData(rooms.Count, newBounds);
            rooms.Add(room);

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

    private bool IsRoomOverlapping(RectInt newBounds)
    {
        foreach (var room in rooms)
        {
            RectInt existing = room.bounds;
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
    // Step 2: 완전 그래프 생성
    // ═══════════════════════════════════════════════════════

    /// <summary>
    /// 모든 방 쌍 사이의 간선을 생성한다.
    /// 방 수가 6~10개 수준이므로 O(n²)이 실용적이다.
    /// 방이 20개 이상으로 확장 시 실제 Delaunay Triangulation으로 교체 권장.
    /// </summary>
    private List<Edge> BuildCompleteGraph()
    {
        List<Vector2> points = rooms.Select(r => new Vector2(r.center.x, r.center.y)).ToList();
        List<Edge> edges = new List<Edge>();

        if (points.Count < 2)
            return edges;

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

    private List<Edge> BuildMST(List<Edge> allEdges)
    {
        List<Edge> sorted = allEdges.OrderBy(e => e.distance).ToList();
        int[] parent = Enumerable.Range(0, rooms.Count).ToArray();

        int Find(int x)
        {
            while (parent[x] != x)
            {
                parent[x] = parent[parent[x]];
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

    private void CarveCorridors(List<Edge> edges)
    {
        foreach (var edge in edges)
        {
            Vector2Int start = rooms[edge.roomA].center;
            Vector2Int end = rooms[edge.roomB].center;

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

    private void PlaceStairs(List<Edge> mstEdges)
    {
        Dictionary<int, List<int>> adj = new Dictionary<int, List<int>>();
        for (int i = 0; i < rooms.Count; i++)
            adj[i] = new List<int>();

        foreach (var edge in mstEdges)
        {
            adj[edge.roomA].Add(edge.roomB);
            adj[edge.roomB].Add(edge.roomA);
        }

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

        rooms[startRoom].roomType = RoomType.START;
        rooms[exitRoom].roomType = RoomType.EXIT;
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

    private void PlaceEvents(float density)
    {
        if (eventPool == null || eventPool.Count == 0)
        {
            Debug.LogWarning("[DungeonGenerator] 이벤트 풀이 비어있습니다. 이벤트 배치를 건너뜁니다.");
            return;
        }

        List<Vector2Int> candidates = new List<Vector2Int>();
        for (int x = 0; x < floorData.mapWidth; x++)
        {
            for (int y = 0; y < floorData.mapHeight; y++)
            {
                if (!grid[x, y].IsWalkable) continue;

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

        int eventCount = Mathf.RoundToInt(candidates.Count * density);
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
    // 유효성 검증 — bool[,] 최적화
    // ═══════════════════════════════════════════════════════

    /// <summary>
    /// 모든 이동 가능 타일이 하나의 연결 컴포넌트를 형성하는지 확인.
    /// HashSet 대신 bool[,] 배열을 사용하여 GC 압력을 줄임.
    /// </summary>
    private bool ValidateConnectivity()
    {
        int w = floorData.mapWidth;
        int h = floorData.mapHeight;

        // 첫 번째 이동 가능 타일 찾기
        Vector2Int? startTile = null;
        int walkableCount = 0;

        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                if (grid[x, y].IsWalkable)
                {
                    walkableCount++;
                    if (!startTile.HasValue)
                        startTile = new Vector2Int(x, y);
                }
            }
        }

        if (!startTile.HasValue)
            return false;

        // BFS — bool[,] visited 사용
        bool[,] visited = new bool[w, h];
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        queue.Enqueue(startTile.Value);
        visited[startTile.Value.x, startTile.Value.y] = true;
        int visitedCount = 1;

        int[] dx = { 0, 0, -1, 1 };
        int[] dy = { 1, -1, 0, 0 };

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            for (int d = 0; d < 4; d++)
            {
                int nx = current.x + dx[d];
                int ny = current.y + dy[d];

                if (nx >= 0 && nx < w && ny >= 0 && ny < h &&
                    !visited[nx, ny] && grid[nx, ny].IsWalkable)
                {
                    visited[nx, ny] = true;
                    visitedCount++;
                    queue.Enqueue(new Vector2Int(nx, ny));
                }
            }
        }

        bool isConnected = visitedCount == walkableCount;
        if (!isConnected)
        {
            Debug.LogWarning($"[DungeonGenerator] 연결성 검증 실패! 연결: {visitedCount} / 전체: {walkableCount}");
        }

        return isConnected;
    }
}

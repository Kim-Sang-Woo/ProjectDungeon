// ============================================================
// DungeonGenerator.cs — 던전 생성 시스템
// 위치: Assets/Scripts/Generation/DungeonGenerator.cs
// ============================================================
// [v2.2 변경사항]
//   - objectPool 필드 추가: 오브젝트(보물 상자 등) 배치 풀
//   - PlaceObjects() 메서드 추가: 가중치 기반 오브젝트 배치
//     이벤트와 오브젝트는 같은 타일에 중복 배치되지 않음
// ============================================================
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DungeonGenerator : MonoBehaviour
{
    [Header("데이터")]
    [Tooltip("층 설정 ScriptableObject")]
    public DungeonFloorData floorData;

    [Header("이벤트 에셋 목록")]
    [Tooltip("배치 가능한 이벤트 데이터 목록")]
    public List<DungeonEventData> eventPool = new List<DungeonEventData>();

    [Header("오브젝트 에셋 목록")]
    [Tooltip("배치 가능한 오브젝트 데이터 목록 (보물 상자 등)")]
    public List<DungeonObjectData> objectPool = new List<DungeonObjectData>();

    [Tooltip("오브젝트 배치 밀도 (walkable 타일 대비 비율, 0.0~1.0)")]
    [Range(0f, 0.3f)]
    public float objectDensity = 0.03f;

    private TileData[,] grid;
    private List<RoomData> rooms = new List<RoomData>();
    private Vector2Int startPosition;
    private Vector2Int exitPosition;

    private struct Edge
    {
        public int roomA;
        public int roomB;
        public float distance;
    }

    private const int MAX_GENERATION_ATTEMPTS = 50;
    private const int MAX_ROOM_PLACEMENT_ATTEMPTS = 200;

    // ─── 진입점 ───

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

    private void Generate(DungeonFloorData data)
    {
        grid = new TileData[data.mapWidth, data.mapHeight];
        for (int x = 0; x < data.mapWidth; x++)
            for (int y = 0; y < data.mapHeight; y++)
                grid[x, y] = new TileData();

        rooms = new List<RoomData>();

        PlaceRooms(data);
        if (rooms.Count < 2) return;

        List<Edge> allEdges = BuildCompleteGraph();
        List<Edge> mstEdges = BuildMST(allEdges);
        List<Edge> finalEdges = AddLoopEdges(allEdges, mstEdges, data.loopEdgeProbability);

        CarveCorridors(finalEdges);
        RecordConnections(finalEdges);
        PlaceStairs(mstEdges);
        PlaceEvents(data.eventDensity);
        PlaceObjects();
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
            if (IsRoomOverlapping(newBounds)) continue;

            RoomData room = new RoomData(rooms.Count, newBounds);
            rooms.Add(room);

            for (int rx = x; rx < x + width; rx++)
                for (int ry = y; ry < y + height; ry++)
                {
                    grid[rx, ry].type = TileType.FLOOR;
                    grid[rx, ry].roomId = room.id;
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
                return true;
        }
        return false;
    }

    // ═══════════════════════════════════════════════════════
    // Step 2~6: 그래프/복도/계단 (기존과 동일)
    // ═══════════════════════════════════════════════════════

    private List<Edge> BuildCompleteGraph()
    {
        List<Vector2> points = rooms.Select(r => new Vector2(r.center.x, r.center.y)).ToList();
        List<Edge> edges = new List<Edge>();
        if (points.Count < 2) return edges;

        for (int i = 0; i < points.Count; i++)
            for (int j = i + 1; j < points.Count; j++)
                edges.Add(new Edge { roomA = i, roomB = j, distance = Vector2.Distance(points[i], points[j]) });

        return edges;
    }

    private List<Edge> BuildMST(List<Edge> allEdges)
    {
        List<Edge> sorted = allEdges.OrderBy(e => e.distance).ToList();
        int[] parent = Enumerable.Range(0, rooms.Count).ToArray();

        int Find(int x) { while (parent[x] != x) { parent[x] = parent[parent[x]]; x = parent[x]; } return x; }

        List<Edge> mst = new List<Edge>();
        foreach (var edge in sorted)
        {
            int rootA = Find(edge.roomA), rootB = Find(edge.roomB);
            if (rootA != rootB) { parent[rootA] = rootB; mst.Add(edge); if (mst.Count == rooms.Count - 1) break; }
        }
        return mst;
    }

    private List<Edge> AddLoopEdges(List<Edge> allEdges, List<Edge> mstEdges, float probability)
    {
        List<Edge> result = new List<Edge>(mstEdges);
        HashSet<(int, int)> mstSet = new HashSet<(int, int)>();
        foreach (var e in mstEdges) mstSet.Add((Mathf.Min(e.roomA, e.roomB), Mathf.Max(e.roomA, e.roomB)));
        foreach (var edge in allEdges)
        {
            var key = (Mathf.Min(edge.roomA, edge.roomB), Mathf.Max(edge.roomA, edge.roomB));
            if (!mstSet.Contains(key) && Random.value < probability) result.Add(edge);
        }
        return result;
    }

    private void CarveCorridors(List<Edge> edges)
    {
        foreach (var edge in edges)
        {
            Vector2Int s = rooms[edge.roomA].center, e = rooms[edge.roomB].center;
            if (Random.value > 0.5f) { CarveH(s.x, e.x, s.y); CarveV(s.y, e.y, e.x); }
            else { CarveV(s.y, e.y, s.x); CarveH(s.x, e.x, e.y); }
        }
    }

    private void CarveH(int x1, int x2, int y)
    {
        for (int x = Mathf.Min(x1, x2); x <= Mathf.Max(x1, x2); x++)
            if (x >= 0 && x < floorData.mapWidth && y >= 0 && y < floorData.mapHeight && grid[x, y].type == TileType.WALL)
            { grid[x, y].type = TileType.CORRIDOR; grid[x, y].roomId = -1; }
    }

    private void CarveV(int y1, int y2, int x)
    {
        for (int y = Mathf.Min(y1, y2); y <= Mathf.Max(y1, y2); y++)
            if (x >= 0 && x < floorData.mapWidth && y >= 0 && y < floorData.mapHeight && grid[x, y].type == TileType.WALL)
            { grid[x, y].type = TileType.CORRIDOR; grid[x, y].roomId = -1; }
    }

    private void RecordConnections(List<Edge> edges)
    {
        foreach (var edge in edges)
        {
            if (!rooms[edge.roomA].connectedRoomIds.Contains(edge.roomB)) rooms[edge.roomA].connectedRoomIds.Add(edge.roomB);
            if (!rooms[edge.roomB].connectedRoomIds.Contains(edge.roomA)) rooms[edge.roomB].connectedRoomIds.Add(edge.roomA);
        }
    }

    private void PlaceStairs(List<Edge> mstEdges)
    {
        Dictionary<int, List<int>> adj = new Dictionary<int, List<int>>();
        for (int i = 0; i < rooms.Count; i++) adj[i] = new List<int>();
        foreach (var edge in mstEdges) { adj[edge.roomA].Add(edge.roomB); adj[edge.roomB].Add(edge.roomA); }

        int startRoom = 0, exitRoom = 0, maxDist = 0;
        for (int i = 0; i < rooms.Count; i++)
        {
            int[] distances = BFS(i, adj);
            for (int j = i + 1; j < rooms.Count; j++)
                if (distances[j] > maxDist) { maxDist = distances[j]; startRoom = i; exitRoom = j; }
        }

        rooms[startRoom].roomType = RoomType.START;
        rooms[exitRoom].roomType = RoomType.EXIT;
        startPosition = rooms[startRoom].center;
        exitPosition = rooms[exitRoom].center;
    }

    private int[] BFS(int start, Dictionary<int, List<int>> adj)
    {
        int[] dist = new int[rooms.Count];
        for (int i = 0; i < dist.Length; i++) dist[i] = -1;
        Queue<int> queue = new Queue<int>();
        dist[start] = 0; queue.Enqueue(start);
        while (queue.Count > 0)
        {
            int cur = queue.Dequeue();
            foreach (int nb in adj[cur]) if (dist[nb] < 0) { dist[nb] = dist[cur] + 1; queue.Enqueue(nb); }
        }
        return dist;
    }

    // ═══════════════════════════════════════════════════════
    // Step 7: 이벤트 배치 (가중치 기반)
    // ═══════════════════════════════════════════════════════

    /// <summary>
    /// 바닥 타일의 eventDensity 비율로 이벤트를 배치한다.
    /// 각 이벤트의 spawnWeight에 따라 가중치 기반으로 선택한다.
    /// weight가 높을수록 자주 선택됨.
    /// weight가 0인 이벤트는 배치되지 않음.
    /// </summary>
    private void PlaceEvents(float density)
    {
        if (eventPool == null || eventPool.Count == 0)
        {
            Debug.LogWarning("[DungeonGenerator] 이벤트 풀이 비어있습니다.");
            return;
        }

        // 가중치가 0보다 큰 이벤트만 필터링
        List<DungeonEventData> validEvents = new List<DungeonEventData>();
        List<int> weights = new List<int>();
        int totalWeight = 0;

        foreach (var evt in eventPool)
        {
            if (evt != null && evt.spawnWeight > 0)
            {
                validEvents.Add(evt);
                weights.Add(evt.spawnWeight);
                totalWeight += evt.spawnWeight;
            }
        }

        if (validEvents.Count == 0 || totalWeight <= 0)
        {
            Debug.LogWarning("[DungeonGenerator] 유효한 이벤트(spawnWeight > 0)가 없습니다.");
            return;
        }

        // 후보 타일 수집
        List<Vector2Int> candidates = new List<Vector2Int>();
        for (int x = 0; x < floorData.mapWidth; x++)
        {
            for (int y = 0; y < floorData.mapHeight; y++)
            {
                if (!grid[x, y].IsWalkable) continue;
                int rid = grid[x, y].roomId;
                if (rid >= 0 && (rooms[rid].roomType == RoomType.START || rooms[rid].roomType == RoomType.EXIT))
                    continue;
                candidates.Add(new Vector2Int(x, y));
            }
        }

        int eventCount = Mathf.RoundToInt(candidates.Count * density);
        Shuffle(candidates);

        // 가중치 기반 이벤트 배치
        for (int i = 0; i < eventCount && i < candidates.Count; i++)
        {
            Vector2Int pos = candidates[i];
            DungeonEventData selectedEvent = SelectWeightedRandom(validEvents, weights, totalWeight);
            grid[pos.x, pos.y].eventData = selectedEvent;
            grid[pos.x, pos.y].isEventConsumed = false;
        }

        // 로그: 이벤트별 배치 수 카운트
        Dictionary<string, int> countMap = new Dictionary<string, int>();
        for (int i = 0; i < eventCount && i < candidates.Count; i++)
        {
            var evt = grid[candidates[i].x, candidates[i].y].eventData;
            if (evt == null) continue;
            string key = evt.displayName ?? evt.eventId;
            if (!countMap.ContainsKey(key)) countMap[key] = 0;
            countMap[key]++;
        }

        string breakdown = string.Join(", ", countMap.Select(kv => $"{kv.Key}:{kv.Value}"));
        Debug.Log($"[DungeonGenerator] 이벤트 배치 완료: {eventCount}개 [{breakdown}]");
    }

    /// <summary>
    /// 가중치 기반 랜덤 선택.
    /// 예: weights = [10, 5, 3] → 10/18 확률로 첫 번째, 5/18 두 번째, 3/18 세 번째
    /// </summary>
    private DungeonEventData SelectWeightedRandom(List<DungeonEventData> events, List<int> weights, int totalWeight)
    {
        int roll = Random.Range(0, totalWeight);
        int cumulative = 0;

        for (int i = 0; i < events.Count; i++)
        {
            cumulative += weights[i];
            if (roll < cumulative)
                return events[i];
        }

        return events[events.Count - 1];
    }

    private void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            T temp = list[i]; list[i] = list[j]; list[j] = temp;
        }
    }

    // ═══════════════════════════════════════════════════════
    // Step 8: 오브젝트 배치 (보물 상자 등)
    // ═══════════════════════════════════════════════════════

    /// <summary>
    /// walkable 타일에 오브젝트를 가중치 기반으로 배치한다.
    /// - START/EXIT 방 타일 제외
    /// - 이미 이벤트가 배치된 타일 제외 (이벤트와 오브젝트 중복 없음)
    /// - spawnWeight 0인 오브젝트는 배치 안 됨
    /// </summary>
    private void PlaceObjects()
    {
        if (objectPool == null || objectPool.Count == 0) return;

        List<DungeonObjectData> validObjects = new List<DungeonObjectData>();
        List<int> weights   = new List<int>();
        int totalWeight     = 0;

        foreach (var obj in objectPool)
        {
            if (obj != null && obj.spawnWeight > 0)
            {
                validObjects.Add(obj);
                weights.Add(obj.spawnWeight);
                totalWeight += obj.spawnWeight;
            }
        }

        if (validObjects.Count == 0 || totalWeight <= 0) return;

        // 후보 타일: walkable + START/EXIT 아님 + 이벤트 없음
        List<Vector2Int> candidates = new List<Vector2Int>();
        for (int x = 0; x < floorData.mapWidth; x++)
        {
            for (int y = 0; y < floorData.mapHeight; y++)
            {
                TileData t = grid[x, y];
                if (!t.IsWalkable)     continue;
                if (t.eventData != null) continue;  // 이벤트 타일 제외
                int rid = t.roomId;
                if (rid >= 0 && (rooms[rid].roomType == RoomType.START ||
                                 rooms[rid].roomType == RoomType.EXIT)) continue;
                candidates.Add(new Vector2Int(x, y));
            }
        }

        int count = Mathf.RoundToInt(candidates.Count * objectDensity);
        Shuffle(candidates);

        for (int i = 0; i < count && i < candidates.Count; i++)
        {
            Vector2Int pos = candidates[i];
            DungeonObjectData selected = SelectObjectWeightedRandom(validObjects, weights, totalWeight);
            grid[pos.x, pos.y].placedObject      = selected;
            grid[pos.x, pos.y].isObjectInteracted = false;
        }

        Debug.Log($"[DungeonGenerator] 오브젝트 배치 완료: {Mathf.Min(count, candidates.Count)}개");
    }

    private DungeonObjectData SelectObjectWeightedRandom(
        List<DungeonObjectData> objects, List<int> weights, int totalWeight)
    {
        int roll       = Random.Range(0, totalWeight);
        int cumulative = 0;
        for (int i = 0; i < objects.Count; i++)
        {
            cumulative += weights[i];
            if (roll < cumulative) return objects[i];
        }
        return objects[objects.Count - 1];
    }

    // ═══════════════════════════════════════════════════════
    // 유효성 검증 (bool[,] 최적화)
    // ═══════════════════════════════════════════════════════

    private bool ValidateConnectivity()
    {
        int w = floorData.mapWidth, h = floorData.mapHeight;
        Vector2Int? startTile = null;
        int walkableCount = 0;

        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
                if (grid[x, y].IsWalkable) { walkableCount++; if (!startTile.HasValue) startTile = new Vector2Int(x, y); }

        if (!startTile.HasValue) return false;

        bool[,] visited = new bool[w, h];
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        queue.Enqueue(startTile.Value);
        visited[startTile.Value.x, startTile.Value.y] = true;
        int count = 1;

        int[] dx = { 0, 0, -1, 1 }, dy = { 1, -1, 0, 0 };
        while (queue.Count > 0)
        {
            Vector2Int cur = queue.Dequeue();
            for (int d = 0; d < 4; d++)
            {
                int nx = cur.x + dx[d], ny = cur.y + dy[d];
                if (nx >= 0 && nx < w && ny >= 0 && ny < h && !visited[nx, ny] && grid[nx, ny].IsWalkable)
                { visited[nx, ny] = true; count++; queue.Enqueue(new Vector2Int(nx, ny)); }
            }
        }

        return count == walkableCount;
    }
}

// ============================================================
// MovementSystem.cs — A* 이동 시스템 (8방향 대각선 + 부드러운 이동)
// 기획서 Ch.2 참조
// 위치: Assets/Scripts/Movement/MovementSystem.cs
// ============================================================
// [v3 변경사항]
//   1. 4방향 → 8방향 이동 (대각선 포함)
//   2. A* 휴리스틱: Manhattan → Chebyshev (8방향 최적)
//   3. 이동 비용: 직선 10, 대각선 14 (√2 ≈ 1.414 정수 근사)
//   4. 대각선 벽 끼기 방지 (Wall Cutting 방지)
//      - 대각선 이동 시 인접 두 직교 타일이 모두 walkable이어야 허용
//      - 예: (0,0)→(1,1)로 이동하려면 (1,0)과 (0,1) 둘 다 walkable
//   5. 대각선 이동 시 시각적 속도 보정 (√2배 거리 반영)
// ============================================================
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovementSystem : MonoBehaviour
{
    [Header("속도 설정")]
    [Tooltip("기본 이동 속도 (칸/sec)")]
    public float baseSpeed = 5f;
    [Tooltip("최대 이동 속도 (칸/sec)")]
    public float maxSpeed = 10f;
    [Tooltip("가속 시작 잔여 거리 (칸)")]
    public int accelerationThreshold = 10;

    [Header("참조")]
    [Tooltip("DungeonManager 참조 (던전 맵 데이터 접근용)")]
    public DungeonManager dungeonManager;

    // ─── 이벤트 ───
    public event Action<Vector2Int> OnTileEntered;

    // ─── 상태 ───
    public Vector2Int CurrentTilePosition { get; private set; }
    public bool IsMoving { get; private set; }

    private List<Vector2Int> currentPath;
    private List<Vector2Int> pendingPath;
    private Coroutine moveCoroutine;
    private Camera mainCamera;

    // ─── 8방향 정의 ───
    // 직선 4방향 + 대각선 4방향
    private static readonly Vector2Int[] AllDirections = {
        new Vector2Int( 0,  1), // 상
        new Vector2Int( 0, -1), // 하
        new Vector2Int(-1,  0), // 좌
        new Vector2Int( 1,  0), // 우
        new Vector2Int( 1,  1), // 우상
        new Vector2Int(-1,  1), // 좌상
        new Vector2Int( 1, -1), // 우하
        new Vector2Int(-1, -1), // 좌하
    };

    // A* 비용 상수 (정수 연산으로 부동소수점 오차 방지)
    private const int COST_STRAIGHT = 10;
    private const int COST_DIAGONAL = 14; // √2 * 10 ≈ 14.14

    private static readonly float SQRT2 = Mathf.Sqrt(2f);

    // ─── 초기화 ───

    private void Start()
    {
        mainCamera = Camera.main;

        if (dungeonManager != null)
        {
            CurrentTilePosition = dungeonManager.StartPosition;
            transform.position = TileToWorld(CurrentTilePosition);
            Debug.Log($"[MovementSystem] 플레이어 시작 위치: {CurrentTilePosition}");
        }
    }

    private void Update()
    {
        HandleClickInput();
    }

    // ─── 입력 처리 ───

    private void HandleClickInput()
    {
        if (!Input.GetMouseButtonDown(0)) return;
        if (mainCamera == null) return;

        Vector3 worldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        Vector2Int targetTile = WorldToTile(worldPos);

        if (dungeonManager == null || !dungeonManager.IsWalkable(targetTile.x, targetTile.y))
            return;

        if (targetTile == CurrentTilePosition)
            return;

        MoveTo(targetTile);
    }

    // ─── 공용 메서드 ───

    public void MoveTo(Vector2Int target)
    {
        List<Vector2Int> path = FindPath(CurrentTilePosition, target);

        if (path == null || path.Count < 2)
        {
            Debug.Log("[MovementSystem] 경로를 찾을 수 없습니다.");
            return;
        }

        if (IsMoving)
        {
            pendingPath = path;
            return;
        }

        currentPath = path;
        moveCoroutine = StartCoroutine(MoveAlongPath());
    }

    public void StopMovement()
    {
        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
            moveCoroutine = null;
        }
        IsMoving = false;
        currentPath = null;
        pendingPath = null;

        transform.position = TileToWorld(CurrentTilePosition);
    }

    // ─── 이동 코루틴 (부드러운 보간, 대각선 속도 보정) ───

    private IEnumerator MoveAlongPath()
    {
        IsMoving = true;
        int pathIndex = 1;

        while (pathIndex < currentPath.Count)
        {
            Vector2Int prevTile = currentPath[pathIndex - 1];
            Vector2Int nextTile = currentPath[pathIndex];

            Vector3 startWorldPos = TileToWorld(prevTile);
            Vector3 endWorldPos = TileToWorld(nextTile);

            // 대각선 여부 판정
            Vector2Int delta = nextTile - prevTile;
            bool isDiagonal = (delta.x != 0 && delta.y != 0);

            // 대각선은 실제 월드 거리가 √2배이므로 이동 시간을 보정
            // speed는 "칸/sec"이므로, 대각선 1칸의 월드 거리가 √2인 것을 반영
            float tileDistance = isDiagonal ? SQRT2 : 1f;

            // 속도 계산
            int remainingTiles = currentPath.Count - pathIndex;
            float currentSpeed = CalculateSpeed(remainingTiles);

            // 보간 이동: progress는 0→1, 실제 이동 속도는 tileDistance를 반영
            float progress = 0f;
            while (progress < 1f)
            {
                progress += (currentSpeed / tileDistance) * Time.deltaTime;
                progress = Mathf.Min(progress, 1f);

                transform.position = Vector3.Lerp(startWorldPos, endWorldPos, progress);
                yield return null;
            }

            // 타일 도착
            CurrentTilePosition = nextTile;
            transform.position = endWorldPos;

            OnTileEntered?.Invoke(CurrentTilePosition);

            if (!IsMoving)
                yield break;

            if (pendingPath != null)
            {
                currentPath = pendingPath;
                pendingPath = null;
                pathIndex = 1;
                continue;
            }

            pathIndex++;
        }

        IsMoving = false;
        currentPath = null;
    }

    private float CalculateSpeed(int remainingTiles)
    {
        if (remainingTiles < accelerationThreshold)
            return baseSpeed;

        float t = Mathf.Clamp01((remainingTiles - accelerationThreshold) / 20f);
        return Mathf.Lerp(baseSpeed, maxSpeed, t);
    }

    // ═══════════════════════════════════════════════════════
    // A* Pathfinding (Chebyshev Distance, 8방향)
    // ═══════════════════════════════════════════════════════
    //
    // 8방향 A*:
    //   - 직선 비용: 10, 대각선 비용: 14
    //   - 휴리스틱: Chebyshev Distance (8방향 최적)
    //   - 대각선 벽 끼기 방지 (Wall Cutting Prevention):
    //     (x,y)에서 (x+dx, y+dy)로 대각선 이동 시
    //     (x+dx, y)와 (x, y+dy) 둘 다 walkable이어야 허용
    //
    // ═══════════════════════════════════════════════════════

    private List<Vector2Int> FindPath(Vector2Int start, Vector2Int end)
    {
        if (dungeonManager == null) return null;

        var openList = new List<AStarNode>();
        var closedSet = new HashSet<Vector2Int>();
        var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        var gScore = new Dictionary<Vector2Int, int>();

        gScore[start] = 0;
        openList.Add(new AStarNode(start, 0, ChebyshevHeuristic(start, end)));

        while (openList.Count > 0)
        {
            // fScore가 가장 작은 노드 추출
            int bestIdx = 0;
            for (int i = 1; i < openList.Count; i++)
            {
                if (openList[i].fScore < openList[bestIdx].fScore ||
                    (openList[i].fScore == openList[bestIdx].fScore &&
                     openList[i].gScore < openList[bestIdx].gScore))
                {
                    bestIdx = i;
                }
            }

            AStarNode current = openList[bestIdx];
            openList.RemoveAt(bestIdx);

            if (closedSet.Contains(current.position))
                continue;

            if (current.position == end)
                return ReconstructPath(cameFrom, end, start);

            closedSet.Add(current.position);

            // 8방향 이웃 탐색
            foreach (var dir in AllDirections)
            {
                Vector2Int neighbor = current.position + dir;

                if (closedSet.Contains(neighbor))
                    continue;

                if (!dungeonManager.IsWalkable(neighbor.x, neighbor.y))
                    continue;

                bool isDiagonal = (dir.x != 0 && dir.y != 0);

                // ─── 대각선 벽 끼기 방지 ───
                // 대각선 이동 시 양쪽 직교 타일이 모두 walkable이어야 허용
                // 이를 통해 벽 모서리를 대각선으로 빠져나가는 것을 방지
                if (isDiagonal)
                {
                    bool sideAWalkable = dungeonManager.IsWalkable(
                        current.position.x + dir.x, current.position.y);
                    bool sideBWalkable = dungeonManager.IsWalkable(
                        current.position.x, current.position.y + dir.y);

                    if (!sideAWalkable || !sideBWalkable)
                        continue;
                }

                int moveCost = isDiagonal ? COST_DIAGONAL : COST_STRAIGHT;
                int tentativeG = gScore[current.position] + moveCost;

                if (!gScore.ContainsKey(neighbor) || tentativeG < gScore[neighbor])
                {
                    cameFrom[neighbor] = current.position;
                    gScore[neighbor] = tentativeG;
                    int f = tentativeG + ChebyshevHeuristic(neighbor, end);
                    openList.Add(new AStarNode(neighbor, tentativeG, f));
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Chebyshev Distance 휴리스틱 (8방향 이동에 최적).
    /// 직선 10, 대각선 14 비용 체계에 맞춘 공식:
    /// h = COST_STRAIGHT * max(dx, dy) + (COST_DIAGONAL - COST_STRAIGHT) * min(dx, dy)
    /// 
    /// 이유: max(dx,dy) 칸만큼 이동해야 하는데, 그 중 min(dx,dy) 칸은
    /// 대각선으로 갈 수 있어서 비용 차이(14-10=4)를 더해준다.
    /// </summary>
    private int ChebyshevHeuristic(Vector2Int a, Vector2Int b)
    {
        int dx = Mathf.Abs(a.x - b.x);
        int dy = Mathf.Abs(a.y - b.y);
        return COST_STRAIGHT * Mathf.Max(dx, dy) + (COST_DIAGONAL - COST_STRAIGHT) * Mathf.Min(dx, dy);
    }

    private List<Vector2Int> ReconstructPath(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int current, Vector2Int start)
    {
        List<Vector2Int> path = new List<Vector2Int> { current };
        while (current != start)
        {
            current = cameFrom[current];
            path.Add(current);
        }
        path.Reverse();
        return path;
    }

    // ─── A* 내부 구조체 ───

    private struct AStarNode
    {
        public Vector2Int position;
        public int gScore;
        public int fScore;

        public AStarNode(Vector2Int pos, int g, int f)
        {
            position = pos;
            gScore = g;
            fScore = f;
        }
    }

    // ═══════════════════════════════════════════════════════
    // 좌표 변환 유틸리티
    // ═══════════════════════════════════════════════════════

    private Vector3 TileToWorld(Vector2Int tilePos)
    {
        return new Vector3(tilePos.x + 0.5f, tilePos.y + 0.5f, 0f);
    }

    private Vector2Int WorldToTile(Vector3 worldPos)
    {
        return new Vector2Int(Mathf.FloorToInt(worldPos.x), Mathf.FloorToInt(worldPos.y));
    }
}

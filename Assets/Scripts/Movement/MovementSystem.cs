// ============================================================
// MovementSystem.cs — A* 이동 시스템 (8방향 대각선 + 부드러운 이동)
// 위치: Assets/Scripts/Movement/MovementSystem.cs
// ============================================================
// [v3.2 변경사항]
//   - SetPosition() 메서드 추가: 층 이동 시 플레이어 강제 배치용
//   - 기존 v3.1의 이동/정지 로직은 그대로 유지
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
    private Coroutine moveCoroutine;
    private Camera mainCamera;
    private bool stopRequested;

    // ─── 8방향 정의 ───
    private static readonly Vector2Int[] AllDirections = {
        new Vector2Int( 0,  1),
        new Vector2Int( 0, -1),
        new Vector2Int(-1,  0),
        new Vector2Int( 1,  0),
        new Vector2Int( 1,  1),
        new Vector2Int(-1,  1),
        new Vector2Int( 1, -1),
        new Vector2Int(-1, -1),
    };

    private const int COST_STRAIGHT = 10;
    private const int COST_DIAGONAL = 14;
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

        if (IsMoving)
        {
            stopRequested = true;
            return;
        }

        Vector3 worldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        Vector2Int targetTile = WorldToTile(worldPos);

        if (dungeonManager == null || !dungeonManager.IsWalkable(targetTile.x, targetTile.y))
            return;

        // 현재 위치 클릭은 MoveTo에서 처리하지 않음 (StairSystem이 처리)
        if (targetTile == CurrentTilePosition)
            return;

        MoveTo(targetTile);
    }

    // ─── 공용 메서드 ───

    public void MoveTo(Vector2Int target)
    {
        if (IsMoving) return;

        List<Vector2Int> path = FindPath(CurrentTilePosition, target);

        if (path == null || path.Count < 2)
        {
            Debug.Log("[MovementSystem] 경로를 찾을 수 없습니다.");
            return;
        }

        currentPath = path;
        stopRequested = false;
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
        stopRequested = false;

        transform.position = TileToWorld(CurrentTilePosition);
    }

    /// <summary>
    /// 플레이어를 지정 타일로 강제 배치한다.
    /// 층 이동 시 DungeonManager가 호출한다.
    /// </summary>
    public void SetPosition(Vector2Int tilePos)
    {
        StopMovement();
        CurrentTilePosition = tilePos;
        transform.position = TileToWorld(tilePos);

        // 배치 후 타일 진입 이벤트 발신 (FogOfWar 갱신 등)
        OnTileEntered?.Invoke(CurrentTilePosition);

        Debug.Log($"[MovementSystem] 플레이어 강제 배치: {tilePos}");
    }

    // ─── 이동 코루틴 ───

    private IEnumerator MoveAlongPath()
    {
        IsMoving = true;
        int pathIndex = 1;

        while (pathIndex < currentPath.Count)
        {
            if (stopRequested)
            {
                stopRequested = false;
                break;
            }

            Vector2Int prevTile = currentPath[pathIndex - 1];
            Vector2Int nextTile = currentPath[pathIndex];

            Vector3 startWorldPos = TileToWorld(prevTile);
            Vector3 endWorldPos = TileToWorld(nextTile);

            Vector2Int delta = nextTile - prevTile;
            bool isDiagonal = (delta.x != 0 && delta.y != 0);
            float tileDistance = isDiagonal ? SQRT2 : 1f;

            int remainingTiles = currentPath.Count - pathIndex;
            float currentSpeed = CalculateSpeed(remainingTiles);

            float progress = 0f;
            bool stoppingThisTile = false;

            while (progress < 1f)
            {
                if (stopRequested && !stoppingThisTile)
                    stoppingThisTile = true;

                float speed = stoppingThisTile ? (baseSpeed * 3f) : currentSpeed;

                progress += (speed / tileDistance) * Time.deltaTime;
                progress = Mathf.Min(progress, 1f);

                transform.position = Vector3.Lerp(startWorldPos, endWorldPos, progress);
                yield return null;
            }

            CurrentTilePosition = nextTile;
            transform.position = endWorldPos;

            OnTileEntered?.Invoke(CurrentTilePosition);

            if (!IsMoving)
                yield break;

            if (stoppingThisTile || stopRequested)
            {
                stopRequested = false;
                break;
            }

            pathIndex++;
        }

        IsMoving = false;
        currentPath = null;
        stopRequested = false;
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

            foreach (var dir in AllDirections)
            {
                Vector2Int neighbor = current.position + dir;

                if (closedSet.Contains(neighbor))
                    continue;

                if (!dungeonManager.IsWalkable(neighbor.x, neighbor.y))
                    continue;

                bool isDiagonal = (dir.x != 0 && dir.y != 0);

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

    private Vector3 TileToWorld(Vector2Int tilePos)
    {
        return new Vector3(tilePos.x + 0.5f, tilePos.y + 0.5f, 0f);
    }

    private Vector2Int WorldToTile(Vector3 worldPos)
    {
        return new Vector2Int(Mathf.FloorToInt(worldPos.x), Mathf.FloorToInt(worldPos.y));
    }
}

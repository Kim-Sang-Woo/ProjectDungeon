// ============================================================
// MovementSystem.cs — A* 이동 시스템 (8방향 대각선 + 부드러운 이동)
// 기획서 Ch.2 참조
// 위치: Assets/Scripts/Movement/MovementSystem.cs
// ============================================================
// [v3.1 변경사항]
//   - 이동 중 클릭 시 즉시 정지 (새 이동 명령 수행하지 않음)
//   - 완전히 정지된 후에만 새 이동 명령을 수락
//   - pendingPath 로직 완전 제거 (빠른 클릭 시 텔레포트 버그 원인)
//   - 정지 요청 시 현재 보간 중인 다음 타일까지 이동 완료 후 정지
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

    // 이동 중 정지 요청 플래그
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

        // ─── 이동 중이면 정지 요청만 하고 새 명령은 무시 ───
        if (IsMoving)
        {
            stopRequested = true;
            return;
        }

        Vector3 worldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        Vector2Int targetTile = WorldToTile(worldPos);

        if (dungeonManager == null || !dungeonManager.IsWalkable(targetTile.x, targetTile.y))
            return;

        if (targetTile == CurrentTilePosition)
            return;

        MoveTo(targetTile);
    }

    // ─── 공용 메서드 ───

    /// <summary>
    /// 목적지로 A* 경로를 계산하고 자동 이동을 시작한다.
    /// 이동 중에는 무시된다. 완전 정지 후에만 동작한다.
    /// </summary>
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

    /// <summary>
    /// 이벤트 발생 시 호출되는 즉시 정지.
    /// 코루틴을 강제 중단하고 현재 논리 타일 위치로 스냅한다.
    /// </summary>
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

    // ─── 이동 코루틴 ───

    private IEnumerator MoveAlongPath()
    {
        IsMoving = true;
        int pathIndex = 1;

        while (pathIndex < currentPath.Count)
        {
            // 다음 칸 이동 시작 전 정지 요청 확인
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

            // 타일 간 보간 이동
            float progress = 0f;
            bool stoppingThisTile = false;

            while (progress < 1f)
            {
                // 정지 요청 감지 → 현재 칸까지는 빠르게 완료
                if (stopRequested && !stoppingThisTile)
                {
                    stoppingThisTile = true;
                }

                float speed = stoppingThisTile
                    ? (baseSpeed * 3f)  // 빠르게 현재 칸 완료
                    : currentSpeed;

                progress += (speed / tileDistance) * Time.deltaTime;
                progress = Mathf.Min(progress, 1f);

                transform.position = Vector3.Lerp(startWorldPos, endWorldPos, progress);
                yield return null;
            }

            // 타일 도착
            CurrentTilePosition = nextTile;
            transform.position = endWorldPos;

            // 타일 진입 이벤트
            OnTileEntered?.Invoke(CurrentTilePosition);

            // StopMovement()가 호출되었을 수 있음 (이벤트 시스템에 의해)
            if (!IsMoving)
                yield break;

            // 정지 요청이 있었으면 이 타일에서 멈춤
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

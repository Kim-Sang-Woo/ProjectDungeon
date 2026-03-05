// ============================================================
// MovementSystem.cs — A* 이동 시스템 (8방향 대각선 + 부드러운 이동)
// 위치: Assets/Scripts/Movement/MovementSystem.cs
// ============================================================
// [v3.3] 가속 로직 제거 — 항상 일정한 속도로 이동
// ============================================================
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovementSystem : MonoBehaviour
{
    [Header("속도 설정")]
    [Tooltip("이동 속도 (칸/sec)")]
    public float moveSpeed = 5f;

    [Header("참조")]
    public DungeonManager dungeonManager;

    public event Action<Vector2Int> OnTileEntered;

    public Vector2Int CurrentTilePosition { get; private set; }
    public bool IsMoving { get; private set; }

    private List<Vector2Int> currentPath;
    private Coroutine moveCoroutine;
    private Camera mainCamera;
    private bool stopRequested;

    private static readonly Vector2Int[] AllDirections = {
        new Vector2Int( 0,  1), new Vector2Int( 0, -1),
        new Vector2Int(-1,  0), new Vector2Int( 1,  0),
        new Vector2Int( 1,  1), new Vector2Int(-1,  1),
        new Vector2Int( 1, -1), new Vector2Int(-1, -1),
    };

    private const int COST_STRAIGHT = 10;
    private const int COST_DIAGONAL = 14;
    private static readonly float SQRT2 = Mathf.Sqrt(2f);

    private void Start()
    {
        mainCamera = Camera.main;
        if (dungeonManager != null)
        {
            CurrentTilePosition = dungeonManager.StartPosition;
            transform.position = TileToWorld(CurrentTilePosition);
        }
    }

    private void Update() { HandleClickInput(); }

    private void HandleClickInput()
    {
        if (!Input.GetMouseButtonDown(0)) return;
        if (mainCamera == null) return;

        if (IsMoving) { stopRequested = true; return; }

        Vector3 worldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        Vector2Int targetTile = WorldToTile(worldPos);

        if (dungeonManager == null || !dungeonManager.IsWalkable(targetTile.x, targetTile.y)) return;
        if (targetTile == CurrentTilePosition) return;

        MoveTo(targetTile);
    }

    public void MoveTo(Vector2Int target)
    {
        if (IsMoving) return;
        List<Vector2Int> path = FindPath(CurrentTilePosition, target);
        if (path == null || path.Count < 2) return;

        currentPath = path;
        stopRequested = false;
        moveCoroutine = StartCoroutine(MoveAlongPath());
    }

    public void StopMovement()
    {
        if (moveCoroutine != null) { StopCoroutine(moveCoroutine); moveCoroutine = null; }
        IsMoving = false;
        currentPath = null;
        stopRequested = false;
        transform.position = TileToWorld(CurrentTilePosition);
    }

    public void SetPosition(Vector2Int tilePos)
    {
        StopMovement();
        CurrentTilePosition = tilePos;
        transform.position = TileToWorld(tilePos);
        OnTileEntered?.Invoke(CurrentTilePosition);
    }

    private IEnumerator MoveAlongPath()
    {
        IsMoving = true;
        int pathIndex = 1;

        while (pathIndex < currentPath.Count)
        {
            if (stopRequested) { stopRequested = false; break; }

            Vector2Int prevTile = currentPath[pathIndex - 1];
            Vector2Int nextTile = currentPath[pathIndex];
            Vector3 startPos = TileToWorld(prevTile);
            Vector3 endPos = TileToWorld(nextTile);

            Vector2Int delta = nextTile - prevTile;
            bool isDiag = (delta.x != 0 && delta.y != 0);
            float tileDist = isDiag ? SQRT2 : 1f;

            float progress = 0f;
            bool stoppingThisTile = false;

            while (progress < 1f)
            {
                if (stopRequested && !stoppingThisTile) stoppingThisTile = true;
                float speed = stoppingThisTile ? (moveSpeed * 3f) : moveSpeed;
                progress += (speed / tileDist) * Time.deltaTime;
                progress = Mathf.Min(progress, 1f);
                transform.position = Vector3.Lerp(startPos, endPos, progress);
                yield return null;
            }

            CurrentTilePosition = nextTile;
            transform.position = endPos;
            OnTileEntered?.Invoke(CurrentTilePosition);

            if (!IsMoving) yield break;
            if (stoppingThisTile || stopRequested) { stopRequested = false; break; }
            pathIndex++;
        }

        IsMoving = false;
        currentPath = null;
        stopRequested = false;
    }

    // ═══════════════════════════════════════════════════════
    // A* (Chebyshev, 8방향, 벽 끼기 방지)
    // ═══════════════════════════════════════════════════════

    private List<Vector2Int> FindPath(Vector2Int start, Vector2Int end)
    {
        if (dungeonManager == null) return null;

        var openList = new List<AStarNode>();
        var closedSet = new HashSet<Vector2Int>();
        var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        var gScore = new Dictionary<Vector2Int, int>();

        gScore[start] = 0;
        openList.Add(new AStarNode(start, 0, Heuristic(start, end)));

        while (openList.Count > 0)
        {
            int bestIdx = 0;
            for (int i = 1; i < openList.Count; i++)
                if (openList[i].fScore < openList[bestIdx].fScore ||
                    (openList[i].fScore == openList[bestIdx].fScore && openList[i].gScore < openList[bestIdx].gScore))
                    bestIdx = i;

            AStarNode current = openList[bestIdx];
            openList.RemoveAt(bestIdx);

            if (closedSet.Contains(current.position)) continue;
            if (current.position == end) return ReconstructPath(cameFrom, end, start);
            closedSet.Add(current.position);

            foreach (var dir in AllDirections)
            {
                Vector2Int nb = current.position + dir;
                if (closedSet.Contains(nb)) continue;
                if (!dungeonManager.IsWalkable(nb.x, nb.y)) continue;

                bool isDiag = (dir.x != 0 && dir.y != 0);
                if (isDiag)
                {
                    if (!dungeonManager.IsWalkable(current.position.x + dir.x, current.position.y) ||
                        !dungeonManager.IsWalkable(current.position.x, current.position.y + dir.y))
                        continue;
                }

                int cost = isDiag ? COST_DIAGONAL : COST_STRAIGHT;
                int tentG = gScore[current.position] + cost;

                if (!gScore.ContainsKey(nb) || tentG < gScore[nb])
                {
                    cameFrom[nb] = current.position;
                    gScore[nb] = tentG;
                    openList.Add(new AStarNode(nb, tentG, tentG + Heuristic(nb, end)));
                }
            }
        }
        return null;
    }

    private int Heuristic(Vector2Int a, Vector2Int b)
    {
        int dx = Mathf.Abs(a.x - b.x), dy = Mathf.Abs(a.y - b.y);
        return COST_STRAIGHT * Mathf.Max(dx, dy) + (COST_DIAGONAL - COST_STRAIGHT) * Mathf.Min(dx, dy);
    }

    private List<Vector2Int> ReconstructPath(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int cur, Vector2Int start)
    {
        var path = new List<Vector2Int> { cur };
        while (cur != start) { cur = cameFrom[cur]; path.Add(cur); }
        path.Reverse();
        return path;
    }

    private struct AStarNode
    {
        public Vector2Int position; public int gScore; public int fScore;
        public AStarNode(Vector2Int p, int g, int f) { position = p; gScore = g; fScore = f; }
    }

    private Vector3 TileToWorld(Vector2Int p) => new Vector3(p.x + 0.5f, p.y + 0.5f, 0f);
    private Vector2Int WorldToTile(Vector3 p) => new Vector2Int(Mathf.FloorToInt(p.x), Mathf.FloorToInt(p.y));
}

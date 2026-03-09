// ============================================================
// MovementSystem.cs — A* 이동 시스템
// 위치: Assets/Scripts/Movement/MovementSystem.cs
// ============================================================
// [v3.6 변경사항]
//   - ApplyWeightSlow(): 무게 감속 적용/해제 메서드 추가
//   - Inventory.OnWeightChanged 구독하여 자동 감속 처리
//   - baseSpeed 필드 추가: 감속 전 원래 속도 보존
// ============================================================
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class MovementSystem : MonoBehaviour
{
    [Header("디버그")]
    public bool debugLogs = false;

    [Header("속도 설정")]
    [Tooltip("기본 이동 속도 (칸/sec)")]
    public float moveSpeed = 5f;

    [Header("참조")]
    public DungeonManager dungeonManager;

    public event Action<Vector2Int> OnTileEntered;

    public Vector2Int CurrentTilePosition { get; private set; }
    public bool IsMoving { get; private set; }

    private int   inputLockCount = 0;
    private float baseSpeed;          // 감속 전 원본 속도

    private List<Vector2Int> currentPath;
    private Coroutine moveCoroutine;
    private Camera mainCamera;
    private bool stopRequested;

    private static readonly Vector2Int[] AllDirections =
    {
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
        baseSpeed  = moveSpeed;

        if (dungeonManager != null)
        {
            CurrentTilePosition = dungeonManager.StartPosition;
            transform.position  = TileToWorld(CurrentTilePosition);
        }

        // 무게 변화 구독
        if (Inventory.Instance != null)
            Inventory.Instance.OnWeightChanged += ApplyWeightSlow;
    }

    private void OnDestroy()
    {
        if (Inventory.Instance != null)
            Inventory.Instance.OnWeightChanged -= ApplyWeightSlow;
    }

    private void Update() { HandleClickInput(); }

    // ─── 무게 감속 ───

    /// <summary>
    /// 무게 감속 구간 진입/해제 시 호출된다.
    /// Inventory.weightSlowRatio 비율만큼 속도를 감소시킨다.
    /// </summary>
    public void ApplyWeightSlow(bool isSlow)
    {
        if (isSlow)
            moveSpeed = baseSpeed * (1f - Inventory.Instance.weightSlowRatio);
        else
            moveSpeed = baseSpeed;

        if (debugLogs) Debug.Log($"[MovementSystem] 이동속도 변경: {moveSpeed:F2} (감속:{isSlow})");
    }

    // ─── 입력 처리 ───

    private void HandleClickInput()
    {
        if (IsInputLocked) return;
        if (!Input.GetMouseButtonDown(0)) return;
        if (mainCamera    == null) return;
        if (dungeonManager == null) return;

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        if (IsMoving) { stopRequested = true; return; }

        Vector3    worldPos   = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        Vector2Int targetTile = WorldToTile(worldPos);

        if (!dungeonManager.IsWalkable(targetTile.x, targetTile.y))
        {
            targetTile = FindNearestWalkable(targetTile);
            if (targetTile.x == -1) return;
        }

        if (targetTile == CurrentTilePosition) return;
        MoveTo(targetTile);
    }

    // ─── 입력 잠금 ───

    public bool IsInputLocked => inputLockCount > 0;

    public void LockInput()
    {
        inputLockCount++;
        StopMovement();
        if (debugLogs) Debug.Log($"[MovementSystem] 입력 잠금 (count={inputLockCount})");
    }

    public void UnlockInput()
    {
        inputLockCount = Mathf.Max(0, inputLockCount - 1);
        if (debugLogs) Debug.Log($"[MovementSystem] 입력 잠금 해제 (count={inputLockCount})");
    }

    /// <summary>긴급 복구용: 모든 입력 잠금을 해제한다.</summary>
    public void UnlockAllInputLocks()
    {
        inputLockCount = 0;
        if (debugLogs) Debug.Log("[MovementSystem] 입력 잠금 전체 해제 (count=0)");
    }

    // ─── 이동 API ───

    private Vector2Int FindNearestWalkable(Vector2Int origin)
    {
        const int MAX = 10;
        for (int r = 1; r <= MAX; r++)
            for (int dx = -r; dx <= r; dx++)
                for (int dy = -r; dy <= r; dy++)
                {
                    if (Mathf.Abs(dx) != r && Mathf.Abs(dy) != r) continue;
                    int nx = origin.x + dx, ny = origin.y + dy;
                    if (dungeonManager.IsWalkable(nx, ny)) return new Vector2Int(nx, ny);
                }
        return new Vector2Int(-1, -1);
    }

    public void MoveTo(Vector2Int target)
    {
        if (IsMoving)    return;
        if (IsInputLocked) return;

        List<Vector2Int> path = FindPath(CurrentTilePosition, target);
        if (path == null || path.Count < 2) return;

        currentPath   = path;
        stopRequested = false;
        moveCoroutine = StartCoroutine(MoveAlongPath());
    }

    public void StopMovement()
    {
        if (moveCoroutine != null) { StopCoroutine(moveCoroutine); moveCoroutine = null; }
        IsMoving      = false;
        currentPath   = null;
        stopRequested = false;
        transform.position = TileToWorld(CurrentTilePosition);
    }

    public void SetPosition(Vector2Int tilePos)
    {
        StopMovement();
        CurrentTilePosition = tilePos;
        transform.position  = TileToWorld(tilePos);
        OnTileEntered?.Invoke(CurrentTilePosition);
    }

    // ─── 이동 코루틴 ───

    private IEnumerator MoveAlongPath()
    {
        IsMoving = true;
        int idx  = 1;

        while (idx < currentPath.Count)
        {
            if (stopRequested) { stopRequested = false; break; }

            Vector2Int prev = currentPath[idx - 1];
            Vector2Int next = currentPath[idx];
            Vector3    sPos = TileToWorld(prev);
            Vector3    ePos = TileToWorld(next);
            bool       diag = (next.x - prev.x != 0 && next.y - prev.y != 0);
            float      dist = diag ? SQRT2 : 1f;

            float progress = 0f;
            while (progress < 1f)
            {
                progress += (moveSpeed / dist) * Time.deltaTime;
                progress  = Mathf.Min(progress, 1f);
                transform.position = Vector3.Lerp(sPos, ePos, progress);
                yield return null;
            }

            CurrentTilePosition = next;
            transform.position  = ePos;
            OnTileEntered?.Invoke(CurrentTilePosition);

            if (!IsMoving) yield break;
            if (stopRequested) { stopRequested = false; break; }
            idx++;
        }

        IsMoving      = false;
        currentPath   = null;
        stopRequested = false;
    }

    // ═══════════════════════════════════════════════════════
    // A*
    // ═══════════════════════════════════════════════════════

    private List<Vector2Int> FindPath(Vector2Int start, Vector2Int end)
    {
        if (dungeonManager == null) return null;

        var open   = new List<AStarNode>();
        var closed = new HashSet<Vector2Int>();
        var from   = new Dictionary<Vector2Int, Vector2Int>();
        var g      = new Dictionary<Vector2Int, int>();

        g[start] = 0;
        open.Add(new AStarNode(start, 0, Heuristic(start, end)));

        while (open.Count > 0)
        {
            int bi = 0;
            for (int i = 1; i < open.Count; i++)
                if (open[i].fScore < open[bi].fScore ||
                   (open[i].fScore == open[bi].fScore && open[i].gScore < open[bi].gScore))
                    bi = i;

            AStarNode cur = open[bi];
            open.RemoveAt(bi);

            if (closed.Contains(cur.position)) continue;
            if (cur.position == end) return Reconstruct(from, end, start);
            closed.Add(cur.position);

            foreach (var dir in AllDirections)
            {
                Vector2Int nb = cur.position + dir;
                if (closed.Contains(nb)) continue;
                if (!dungeonManager.IsWalkable(nb.x, nb.y)) continue;

                bool isDiag = dir.x != 0 && dir.y != 0;
                if (isDiag &&
                   (!dungeonManager.IsWalkable(cur.position.x + dir.x, cur.position.y) ||
                    !dungeonManager.IsWalkable(cur.position.x, cur.position.y + dir.y)))
                    continue;

                int cost  = isDiag ? COST_DIAGONAL : COST_STRAIGHT;
                int tentG = g[cur.position] + cost;

                if (!g.ContainsKey(nb) || tentG < g[nb])
                {
                    from[nb] = cur.position;
                    g[nb]    = tentG;
                    open.Add(new AStarNode(nb, tentG, tentG + Heuristic(nb, end)));
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

    private List<Vector2Int> Reconstruct(Dictionary<Vector2Int, Vector2Int> from, Vector2Int cur, Vector2Int start)
    {
        var path = new List<Vector2Int> { cur };
        while (cur != start) { cur = from[cur]; path.Add(cur); }
        path.Reverse();
        return path;
    }

    private struct AStarNode
    {
        public Vector2Int position; public int gScore; public int fScore;
        public AStarNode(Vector2Int p, int g, int f) { position = p; gScore = g; fScore = f; }
    }

    private Vector3    TileToWorld(Vector2Int p) => new Vector3(p.x + 0.5f, p.y + 0.5f, 0f);
    private Vector2Int WorldToTile(Vector3 p)    => new Vector2Int(Mathf.FloorToInt(p.x), Mathf.FloorToInt(p.y));
}

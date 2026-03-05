// ============================================================
// MovementSystem.cs — A* 이동 시스템
// 기획서 Ch.2 참조
// ============================================================
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어의 자동 이동을 담당하는 시스템.
/// 클릭 시 A*(Manhattan) 경로를 계산하고 코루틴으로 타일 단위 이동한다.
/// </summary>
public class MovementSystem : MonoBehaviour
{
    [Header("속도 설정 (기획서 2.1)")]
    [Tooltip("기본 이동 속도 (칸/sec)")]
    public float baseSpeed = 5f;
    [Tooltip("최대 이동 속도 (칸/sec)")]
    public float maxSpeed = 10f;
    [Tooltip("가속 시작 잔여 거리 (칸)")]
    public int accelerationThreshold = 10;

    [Header("참조")]
    [Tooltip("DungeonGenerator 참조 (던전 맵 데이터 접근용)")]
    public DungeonGenerator dungeonGenerator;

    // ─── 이벤트 ───
    /// <summary>
    /// 타일 진입 시 발생하는 이벤트. EventTriggerSystem이 구독한다.
    /// 기획서 Ch.2: OnTileEntered(Vector2Int pos)
    /// </summary>
    public event Action<Vector2Int> OnTileEntered;

    // ─── 상태 ───
    /// <summary>현재 플레이어의 타일 좌표</summary>
    public Vector2Int CurrentTilePosition { get; private set; }

    /// <summary>현재 이동 중인지 여부</summary>
    public bool IsMoving { get; private set; }

    private List<Vector2Int> currentPath;
    private List<Vector2Int> pendingPath; // 재클릭 시 대기 경로
    private Coroutine moveCoroutine;
    private Camera mainCamera;

    // ─── 초기화 ───

    private void Start()
    {
        mainCamera = Camera.main;

        // 플레이어를 입구 위치로 초기 배치
        if (dungeonGenerator != null)
        {
            CurrentTilePosition = dungeonGenerator.startPosition;
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

        // 마우스 → 월드 → 타일 좌표 변환
        Vector3 worldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        Vector2Int targetTile = WorldToTile(worldPos);

        // 이동 가능 여부 확인
        if (dungeonGenerator == null || !dungeonGenerator.IsWalkable(targetTile.x, targetTile.y))
            return;

        // 현재 위치와 같으면 무시
        if (targetTile == CurrentTilePosition)
            return;

        // 이동 시작
        MoveTo(targetTile);
    }

    // ─── 공용 메서드 ───

    /// <summary>
    /// 목적지로 A* 경로를 계산하고 자동 이동을 시작한다.
    /// 이동 중 호출 시 현재 칸 이동을 완료한 후 새 경로로 자연스럽게 전환한다.
    /// </summary>
    public void MoveTo(Vector2Int target)
    {
        // 경로 계산
        List<Vector2Int> path = FindPath(CurrentTilePosition, target);

        if (path == null || path.Count < 2)
        {
            Debug.Log("[MovementSystem] 경로를 찾을 수 없습니다.");
            return;
        }

        if (IsMoving)
        {
            // 이동 중이면 현재 칸 완료 후 전환하도록 대기 경로에 저장
            pendingPath = path;
            return;
        }

        // 새 이동 시작
        currentPath = path;
        moveCoroutine = StartCoroutine(MoveAlongPath());
    }

    /// <summary>
    /// 기획서 Ch.3.2: 이동 즉시 중단.
    /// 이벤트 발생 시 호출된다.
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
        pendingPath = null;

        // 현재 타일 위치로 스냅
        transform.position = TileToWorld(CurrentTilePosition);
    }

    // ─── 이동 코루틴 ───

    /// <summary>
    /// 경로를 따라 타일 단위로 이동하는 코루틴.
    /// 칸마다 딱딱 스냅 이동. 기획서 2.1: 기본 5칸/sec, 10칸 이상 장거리 시 가속.
    /// </summary>
    private IEnumerator MoveAlongPath()
    {
        IsMoving = true;
        int pathIndex = 1; // 0번은 현재 위치

        while (pathIndex < currentPath.Count)
        {
            Vector2Int nextTile = currentPath[pathIndex];

            // 속도 계산 → 대기 시간 결정
            int remainingTiles = currentPath.Count - pathIndex;
            float currentSpeed = CalculateSpeed(remainingTiles);
            float stepDelay = 1f / currentSpeed;

            // 대기 후 칸 단위로 즉시 이동 (딱딱 이동)
            yield return new WaitForSeconds(stepDelay);

            // 타일 도착 — 즉시 스냅
            CurrentTilePosition = nextTile;
            transform.position = TileToWorld(CurrentTilePosition);

            // 타일 진입 이벤트 발신 (EventTriggerSystem이 수신)
            OnTileEntered?.Invoke(CurrentTilePosition);

            // 이벤트로 인해 이동이 중단되었을 수 있으므로 확인
            if (!IsMoving)
                yield break;

            // 대기 경로가 있으면 현재 칸에서 새 경로로 전환
            if (pendingPath != null)
            {
                currentPath = pendingPath;
                pendingPath = null;
                pathIndex = 1; // 새 경로의 첫 번째 이동 칸부터
                continue;
            }

            pathIndex++;
        }

        // 이동 완료
        IsMoving = false;
        currentPath = null;
    }

    /// <summary>
    /// 기획서 2.1 가속 공식:
    /// currentSpeed = Mathf.Lerp(baseSpeed, maxSpeed, (remainingTiles - 10) / 20f)
    /// 잔여 거리 10칸 미만이면 기본 속도 유지.
    /// </summary>
    private float CalculateSpeed(int remainingTiles)
    {
        if (remainingTiles < accelerationThreshold)
            return baseSpeed;

        float t = Mathf.Clamp01((remainingTiles - accelerationThreshold) / 20f);
        return Mathf.Lerp(baseSpeed, maxSpeed, t);
    }

    // ═══════════════════════════════════════════════════════
    // A* Pathfinding (Manhattan Distance, 4방향)
    // ═══════════════════════════════════════════════════════

    /// <summary>
    /// A* 알고리즘으로 최단 경로를 계산한다.
    /// 기획서 Ch.2.1: 휴리스틱은 Manhattan Distance (4방향 이동).
    /// </summary>
    private List<Vector2Int> FindPath(Vector2Int start, Vector2Int end)
    {
        if (dungeonGenerator == null) return null;

        var openSet = new SortedSet<AStarNode>(new AStarNodeComparer());
        var closedSet = new HashSet<Vector2Int>();
        var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        var gScore = new Dictionary<Vector2Int, int>();

        AStarNode startNode = new AStarNode(start, 0, ManhattanDistance(start, end));
        openSet.Add(startNode);
        gScore[start] = 0;

        Vector2Int[] directions = {
            Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
        };

        while (openSet.Count > 0)
        {
            AStarNode current = openSet.Min;
            openSet.Remove(current);

            if (current.position == end)
            {
                // 경로 역추적
                return ReconstructPath(cameFrom, end, start);
            }

            closedSet.Add(current.position);

            foreach (var dir in directions)
            {
                Vector2Int neighbor = current.position + dir;

                if (closedSet.Contains(neighbor))
                    continue;

                if (!dungeonGenerator.IsWalkable(neighbor.x, neighbor.y))
                    continue;

                int tentativeG = gScore[current.position] + 1;

                if (!gScore.ContainsKey(neighbor) || tentativeG < gScore[neighbor])
                {
                    cameFrom[neighbor] = current.position;
                    gScore[neighbor] = tentativeG;
                    int f = tentativeG + ManhattanDistance(neighbor, end);

                    openSet.Add(new AStarNode(neighbor, tentativeG, f));
                }
            }
        }

        return null; // 경로 없음
    }

    private int ManhattanDistance(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
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

    private class AStarNodeComparer : IComparer<AStarNode>
    {
        public int Compare(AStarNode a, AStarNode b)
        {
            int result = a.fScore.CompareTo(b.fScore);
            if (result == 0) result = a.gScore.CompareTo(b.gScore);
            if (result == 0) result = a.position.x.CompareTo(b.position.x);
            if (result == 0) result = a.position.y.CompareTo(b.position.y);
            return result;
        }
    }

    // ═══════════════════════════════════════════════════════
    // 좌표 변환 유틸리티
    // ═══════════════════════════════════════════════════════

    /// <summary>
    /// 타일 좌표 → 월드 좌표 변환 (타일 중심)
    /// </summary>
    private Vector3 TileToWorld(Vector2Int tilePos)
    {
        return new Vector3(tilePos.x + 0.5f, tilePos.y + 0.5f, 0f);
    }

    /// <summary>
    /// 월드 좌표 → 타일 좌표 변환
    /// </summary>
    private Vector2Int WorldToTile(Vector3 worldPos)
    {
        return new Vector2Int(Mathf.FloorToInt(worldPos.x), Mathf.FloorToInt(worldPos.y));
    }
}

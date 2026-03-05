// ============================================================
// MovementSystem.cs — A* 이동 시스템 (부드러운 이동)
// 기획서 Ch.2 참조
// 위치: Assets/Scripts/Movement/MovementSystem.cs
// ============================================================
// [v2 변경사항]
//   1. 딱딱한 스냅 이동 → 부드러운 Lerp 보간 이동
//      - 논리적 타일 위치(CurrentTilePosition)와 시각적 위치(transform.position) 분리
//      - 이동 중 시각적으로 부드럽게 슬라이드하되, 타일 도착 판정은 정확히 유지
//   2. A* 버그 수정: SortedSet → Dictionary + BinaryHeap 대안
//      - 같은 position에 더 나은 gScore가 발견되었을 때 갱신 보장
//   3. DungeonGenerator 직접 참조 → DungeonManager 참조로 변경
//   4. 가속 공식 프레임 독립적으로 보정
// ============================================================
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어의 자동 이동을 담당하는 시스템.
/// 클릭 시 A*(Manhattan) 경로를 계산하고 코루틴으로 타일 단위 부드러운 이동을 수행한다.
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
    [Tooltip("DungeonManager 참조 (던전 맵 데이터 접근용)")]
    public DungeonManager dungeonManager;

    // ─── 이벤트 ───
    /// <summary>
    /// 타일 진입 시 발생하는 이벤트. EventTriggerSystem이 구독한다.
    /// 기획서 Ch.2: OnTileEntered(Vector2Int pos)
    /// </summary>
    public event Action<Vector2Int> OnTileEntered;

    // ─── 상태 ───
    /// <summary>현재 플레이어의 논리적 타일 좌표</summary>
    public Vector2Int CurrentTilePosition { get; private set; }

    /// <summary>현재 이동 중인지 여부</summary>
    public bool IsMoving { get; private set; }

    private List<Vector2Int> currentPath;
    private List<Vector2Int> pendingPath;
    private Coroutine moveCoroutine;
    private Camera mainCamera;

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

    /// <summary>
    /// 목적지로 A* 경로를 계산하고 자동 이동을 시작한다.
    /// 이동 중 호출 시 현재 칸 이동을 완료한 후 새 경로로 자연스럽게 전환한다.
    /// </summary>
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

    /// <summary>
    /// 이동 즉시 중단. 이벤트 발생 시 호출된다.
    /// 현재 논리 타일 위치로 즉시 스냅한다.
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

        transform.position = TileToWorld(CurrentTilePosition);
    }

    // ─── 이동 코루틴 (부드러운 보간 이동) ───

    /// <summary>
    /// 경로를 따라 타일 단위로 부드럽게 이동하는 코루틴.
    /// 각 타일 사이를 Lerp로 보간하여 슬라이드하며,
    /// 타일 중심에 도달하면 논리적 진입 이벤트를 발생시킨다.
    /// </summary>
    private IEnumerator MoveAlongPath()
    {
        IsMoving = true;
        int pathIndex = 1; // 0번은 현재 위치

        while (pathIndex < currentPath.Count)
        {
            Vector2Int prevTile = currentPath[pathIndex - 1];
            Vector2Int nextTile = currentPath[pathIndex];

            Vector3 startWorldPos = TileToWorld(prevTile);
            Vector3 endWorldPos = TileToWorld(nextTile);

            // 속도 계산
            int remainingTiles = currentPath.Count - pathIndex;
            float currentSpeed = CalculateSpeed(remainingTiles);

            // 타일 간 보간 이동
            float progress = 0f;
            while (progress < 1f)
            {
                progress += currentSpeed * Time.deltaTime;
                progress = Mathf.Min(progress, 1f);

                transform.position = Vector3.Lerp(startWorldPos, endWorldPos, progress);
                yield return null;
            }

            // 타일 도착 — 논리적 위치 갱신
            CurrentTilePosition = nextTile;
            transform.position = endWorldPos; // 정확히 스냅

            // 타일 진입 이벤트 발신 (EventTriggerSystem, FogOfWar 등이 수신)
            OnTileEntered?.Invoke(CurrentTilePosition);

            // 이벤트로 인해 이동이 중단되었을 수 있으므로 확인
            if (!IsMoving)
                yield break;

            // 대기 경로가 있으면 현재 칸에서 새 경로로 전환
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

    /// <summary>
    /// 가속 공식:
    /// 잔여 거리가 accelerationThreshold 미만이면 기본 속도,
    /// 이상이면 baseSpeed ~ maxSpeed 사이에서 보간.
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
    // [v2 수정] SortedSet 기반 → Dictionary + List 기반
    //   이전 구현에서 같은 position에 더 나은 gScore가 발견되어도
    //   SortedSet에서 이전 노드를 제거하지 못하는 버그가 있었음.
    //   새 구현은 openList에 중복 삽입을 허용하되,
    //   closedSet으로 이미 확정된 노드를 건너뛰는 방식.
    // ═══════════════════════════════════════════════════════

    private List<Vector2Int> FindPath(Vector2Int start, Vector2Int end)
    {
        if (dungeonManager == null) return null;

        // openList: fScore 기준 정렬된 리스트 (중복 position 허용)
        var openList = new List<AStarNode>();
        var closedSet = new HashSet<Vector2Int>();
        var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        var gScore = new Dictionary<Vector2Int, int>();

        gScore[start] = 0;
        openList.Add(new AStarNode(start, 0, ManhattanDistance(start, end)));

        Vector2Int[] directions = {
            Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
        };

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

            // 이미 확정된 노드면 건너뛰기 (중복 삽입 대응)
            if (closedSet.Contains(current.position))
                continue;

            if (current.position == end)
            {
                return ReconstructPath(cameFrom, end, start);
            }

            closedSet.Add(current.position);

            foreach (var dir in directions)
            {
                Vector2Int neighbor = current.position + dir;

                if (closedSet.Contains(neighbor))
                    continue;

                if (!dungeonManager.IsWalkable(neighbor.x, neighbor.y))
                    continue;

                int tentativeG = gScore[current.position] + 1;

                if (!gScore.ContainsKey(neighbor) || tentativeG < gScore[neighbor])
                {
                    cameFrom[neighbor] = current.position;
                    gScore[neighbor] = tentativeG;
                    int f = tentativeG + ManhattanDistance(neighbor, end);
                    // 중복 삽입 허용 — closedSet에서 걸러짐
                    openList.Add(new AStarNode(neighbor, tentativeG, f));
                }
            }
        }

        return null;
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

    // ═══════════════════════════════════════════════════════
    // 좌표 변환 유틸리티
    // ═══════════════════════════════════════════════════════

    /// <summary>타일 좌표 → 월드 좌표 변환 (타일 중심)</summary>
    private Vector3 TileToWorld(Vector2Int tilePos)
    {
        return new Vector3(tilePos.x + 0.5f, tilePos.y + 0.5f, 0f);
    }

    /// <summary>월드 좌표 → 타일 좌표 변환</summary>
    private Vector2Int WorldToTile(Vector3 worldPos)
    {
        return new Vector2Int(Mathf.FloorToInt(worldPos.x), Mathf.FloorToInt(worldPos.y));
    }
}

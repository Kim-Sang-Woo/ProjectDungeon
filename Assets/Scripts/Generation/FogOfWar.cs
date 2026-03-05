// ============================================================
// FogOfWar.cs — 전장의 안개 시스템 v3
// [수정] 복도 시야 수정 — Bresenham Ray Casting 방식으로 교체
// ============================================================
// [씬 배치]
//   Hierarchy: DungeonManager 하위 FogOfWar 빈 GameObject
//   컴포넌트: FogOfWar.cs 부착
//
// [Inspector 연결 필수]
//   movementSystem   → Player/MovementSystem
//   dungeonGenerator → DungeonManager/DungeonGenerator
//
// [v3 변경 사항]
//   Shadowcasting → Bresenham Ray Casting 방식으로 교체
//   - 복도(폭 1칸)에서 전방이 1칸만 보이던 문제 수정
//   - 원점에서 가시 범위 경계의 모든 타일로 Ray를 쏘아
//     첫 번째 벽 타일에서 차단, 그 이전까지만 밝힘
//   - 복도에서 전방 3타일, 방 안에서 사방 3타일 정상 동작
// ============================================================

using System.Collections.Generic;
using UnityEngine;

public class FogOfWar : MonoBehaviour
{
    // ─── Inspector ───

    [Header("필수 참조")]
    public MovementSystem movementSystem;
    public DungeonGenerator dungeonGenerator;

    [Header("안개 설정")]
    [Tooltip("완전 가시 반경 (0~visibleRadius 타일 완전 투명)")]
    public int visibleRadius = 2;

    [Tooltip("반투명 경계 알파 (기획서: 50%)")]
    [Range(0f, 1f)]
    public float edgeAlpha = 0.5f;

    [Tooltip("안개 Mesh Z축 (타일맵보다 앞, UI보다 뒤)")]
    public float fogZDepth = -0.5f;

    // ─── 내부 상태 ───

    private enum FogState : byte
    {
        Unexplored = 0,
        Explored   = 1,
        Visible    = 2
    }

    private FogState[,] fogMap;
    private HashSet<Vector2Int> currentVisible = new HashSet<Vector2Int>();
    private HashSet<Vector2Int> currentEdge    = new HashSet<Vector2Int>();

    private Texture2D    fogTexture;
    private MeshRenderer meshRenderer;
    private MeshFilter   meshFilter;
    private Material     fogMaterial;

    private int mapWidth, mapHeight;
    private int edgeRadius;

    private Vector2Int lastPlayerTile = new Vector2Int(-999, -999);

    // ─── 초기화 ───

    private void Start()
    {
        edgeRadius = visibleRadius + 1;

        if (dungeonGenerator == null || dungeonGenerator.grid == null)
        {
            Debug.LogError("[FogOfWar] DungeonGenerator 참조 없음 또는 던전 미생성!");
            return;
        }
        if (movementSystem == null)
        {
            Debug.LogError("[FogOfWar] MovementSystem 참조 없음!");
            return;
        }

        mapWidth  = dungeonGenerator.floorData.mapWidth;
        mapHeight = dungeonGenerator.floorData.mapHeight;

        InitFogMap();
        BuildMesh();
        BuildTexture();

        movementSystem.OnTileEntered += OnPlayerMoved;
        UpdateFog(dungeonGenerator.startPosition);

        Debug.Log($"[FogOfWar] 초기화 완료 ({mapWidth}x{mapHeight}), 가시:{visibleRadius} 경계:{edgeRadius}");
    }

    private void OnDestroy()
    {
        if (movementSystem != null) movementSystem.OnTileEntered -= OnPlayerMoved;
        if (fogTexture  != null) Destroy(fogTexture);
        if (fogMaterial != null) Destroy(fogMaterial);
    }

    private void OnPlayerMoved(Vector2Int pos)
    {
        if (pos == lastPlayerTile) return;
        UpdateFog(pos);
    }

    // ─── FOV 갱신 ───

    private void UpdateFog(Vector2Int origin)
    {
        lastPlayerTile = origin;

        // 1. VISIBLE → EXPLORED
        for (int x = 0; x < mapWidth; x++)
            for (int y = 0; y < mapHeight; y++)
                if (fogMap[x, y] == FogState.Visible)
                    fogMap[x, y] = FogState.Explored;

        // 2. Ray Casting
        currentVisible.Clear();
        currentEdge.Clear();
        CastFieldOfView(origin);

        // 3. fogMap 갱신
        foreach (var t in currentVisible)
            fogMap[t.x, t.y] = FogState.Visible;

        // 4. 텍스처 반영
        ApplyTexture();
    }

    // ═══════════════════════════════════════════════════════
    // Bresenham Ray Casting FOV
    //
    // 원리:
    //   마름모 경계(edgeRadius) 위의 모든 타일을 목표점으로 삼아
    //   원점 → 목표점 방향으로 Bresenham 직선 Ray를 발사한다.
    //   Ray가 지나는 각 타일:
    //     - FLOOR / CORRIDOR : 가시 처리 후 계속 진행
    //     - WALL             : 가시 처리(벽 자체는 보임) 후 Ray 중단
    //   복도처럼 좁은 통로에서도 Ray가 직선으로 뚫리므로
    //   전방 edgeRadius 타일까지 정상적으로 밝혀진다.
    // ═══════════════════════════════════════════════════════

    private void CastFieldOfView(Vector2Int origin)
    {
        // 원점 항상 가시
        MarkVisible(origin, 0);

        // 마름모 경계 위의 모든 점을 향해 Ray 발사
        for (int dx = -edgeRadius; dx <= edgeRadius; dx++)
        {
            int dyMax = edgeRadius - Mathf.Abs(dx);
            for (int dy = -dyMax; dy <= dyMax; dy++)
            {
                // 경계(dist == edgeRadius)인 타일만 목표점으로 사용
                // 내부 타일은 경계를 향한 Ray가 지나면서 자동으로 밝혀짐
                if (Mathf.Abs(dx) + Mathf.Abs(dy) != edgeRadius) continue;
                CastRay(origin, origin + new Vector2Int(dx, dy));
            }
        }
    }

    /// <summary>
    /// origin → target 방향으로 Bresenham 직선을 따라
    /// 타일을 확인하고 벽에서 시야를 차단한다.
    /// </summary>
    private void CastRay(Vector2Int origin, Vector2Int target)
    {
        int x0 = origin.x, y0 = origin.y;
        int x1 = target.x, y1 = target.y;

        int dx  = Mathf.Abs(x1 - x0);
        int dy  = Mathf.Abs(y1 - y0);
        int sx  = x0 < x1 ? 1 : -1;
        int sy  = y0 < y1 ? 1 : -1;
        int err = dx - dy;
        int cx  = x0, cy = y0;

        while (true)
        {
            // 원점은 CastFieldOfView에서 이미 처리
            if (cx != x0 || cy != y0)
            {
                if (cx < 0 || cx >= mapWidth || cy < 0 || cy >= mapHeight) break;

                int dist = Mathf.Abs(cx - x0) + Mathf.Abs(cy - y0);
                MarkVisible(new Vector2Int(cx, cy), dist);

                // 벽: 이 타일에서 시야 차단
                if (dungeonGenerator.grid[cx, cy].type == TileType.WALL) break;
            }

            if (cx == x1 && cy == y1) break;

            int e2 = 2 * err;
            if (e2 > -dy) { err -= dy; cx += sx; }
            if (e2 <  dx) { err += dx; cy += sy; }
        }
    }

    private void MarkVisible(Vector2Int pos, int dist)
    {
        currentVisible.Add(pos);
        if (dist >= edgeRadius)
            currentEdge.Add(pos);
    }

    // ─── 텍스처 적용 ───

    private void ApplyTexture()
    {
        Color[] pixels = fogTexture.GetPixels();

        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                float alpha;
                switch (fogMap[x, y])
                {
                    case FogState.Unexplored:
                        alpha = 1.0f;
                        break;
                    case FogState.Explored:
                        alpha = edgeAlpha;
                        break;
                    case FogState.Visible:
                        alpha = currentEdge.Contains(new Vector2Int(x, y))
                            ? edgeAlpha : 0.0f;
                        break;
                    default:
                        alpha = 1.0f;
                        break;
                }
                pixels[y * mapWidth + x] = new Color(0f, 0f, 0f, alpha);
            }
        }

        fogTexture.SetPixels(pixels);
        fogTexture.Apply();
    }

    // ─── 공개 API ───

    /// <summary>탐험된 타일(Explored 또는 Visible)이면 true.</summary>
    public bool IsTileRevealed(int x, int y)
    {
        if (x < 0 || x >= mapWidth || y < 0 || y >= mapHeight) return false;
        return fogMap[x, y] != FogState.Unexplored;
    }

    /// <summary>현재 완전 가시 상태이면 true.</summary>
    public bool IsTileFullyVisible(int x, int y)
    {
        if (x < 0 || x >= mapWidth || y < 0 || y >= mapHeight) return false;
        if (fogMap[x, y] != FogState.Visible) return false;
        return !currentEdge.Contains(new Vector2Int(x, y));
    }

    // ─── Mesh / Texture ───

    private void InitFogMap()
    {
        fogMap = new FogState[mapWidth, mapHeight];
    }

    private void BuildMesh()
    {
        meshFilter   = gameObject.AddComponent<MeshFilter>();
        meshRenderer = gameObject.AddComponent<MeshRenderer>();

        Mesh mesh = new Mesh { name = "FogOfWarMesh" };
        mesh.vertices = new Vector3[]
        {
            new Vector3(0f,       0f,        fogZDepth),
            new Vector3(mapWidth, 0f,        fogZDepth),
            new Vector3(mapWidth, mapHeight, fogZDepth),
            new Vector3(0f,       mapHeight, fogZDepth)
        };
        mesh.triangles = new int[] { 0, 2, 1, 0, 3, 2 };
        mesh.uv = new Vector2[]
        {
            new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(1f, 1f), new Vector2(0f, 1f)
        };
        mesh.RecalculateNormals();

        meshFilter.mesh      = mesh;
        meshRenderer.sortingOrder = 10;
    }

    private void BuildTexture()
    {
        fogTexture = new Texture2D(mapWidth, mapHeight, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode   = TextureWrapMode.Clamp
        };

        Color[] init = new Color[mapWidth * mapHeight];
        for (int i = 0; i < init.Length; i++)
            init[i] = new Color(0f, 0f, 0f, 1f);
        fogTexture.SetPixels(init);
        fogTexture.Apply();

        Shader shader = Shader.Find("Sprites/Default")
                     ?? Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
        {
            Debug.LogError("[FogOfWar] 호환 쉐이더를 찾을 수 없습니다!");
            return;
        }

        fogMaterial = new Material(shader)
        {
            mainTexture = fogTexture,
            renderQueue = 3000
        };
        fogMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        fogMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        fogMaterial.SetInt("_ZWrite",   0);

        meshRenderer.material = fogMaterial;
    }

    // ─── Gizmo ───

    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying || movementSystem == null) return;
        Vector2Int p = movementSystem.CurrentTilePosition;

        Gizmos.color = new Color(0f, 1f, 0f, 0.2f);
        for (int x = -visibleRadius; x <= visibleRadius; x++)
            for (int y = -(visibleRadius - Mathf.Abs(x)); y <= (visibleRadius - Mathf.Abs(x)); y++)
                Gizmos.DrawCube(new Vector3(p.x + x + 0.5f, p.y + y + 0.5f, 0), Vector3.one);

        Gizmos.color = new Color(1f, 1f, 0f, 0.2f);
        for (int x = -edgeRadius; x <= edgeRadius; x++)
            for (int y = -(edgeRadius - Mathf.Abs(x)); y <= (edgeRadius - Mathf.Abs(x)); y++)
                if (Mathf.Abs(x) + Mathf.Abs(y) == edgeRadius)
                    Gizmos.DrawCube(new Vector3(p.x + x + 0.5f, p.y + y + 0.5f, 0), Vector3.one);
    }
}

// ============================================================
// FogOfWar.cs — 전장의 안개 시스템 v4.2
// 위치: Assets/Scripts/Rendering/FogOfWar.cs
// ============================================================
// [v4.2 변경사항]
//   - 시야 범위: 마름모(Manhattan) → 사각형(Chebyshev) 거리 기반
//     visibleRadius=2일 때 5x5 사각형이 완전 가시, 7x7 사각형 경계가 반투명
//     → 위/아래/좌/우 모서리가 검정으로 남는 문제 완전 해결
//   - Ray 목표점도 사각형 경계로 변경
//   - Color32 전체 갱신 방식 유지 (성능 + 안정성)
//   - DungeonManager 참조
// ============================================================

using System.Collections.Generic;
using UnityEngine;

public class FogOfWar : MonoBehaviour
{
    // ─── Inspector ───

    [Header("필수 참조")]
    public MovementSystem movementSystem;
    public DungeonManager dungeonManager;

    [Header("안개 설정")]
    [Tooltip("완전 가시 반경 (Chebyshev 거리 기준, 정사각형 범위)")]
    public int visibleRadius = 2;

    [Tooltip("반투명 경계 알파 (기획서: 50%)")]
    [Range(0f, 1f)]
    public float edgeAlpha = 0.5f;

    [Tooltip("안개 Mesh Z축 (타일맵보다 앞, UI보다 뒤)")]
    public float fogZDepth = -0.5f;

    [Header("셰이더 (선택)")]
    [Tooltip("직접 할당하면 Shader.Find를 건너뜁니다. 빌드 시 안정적.")]
    public Material fogMaterialOverride;

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
    private Color32[]    pixelBuffer;

    private int mapWidth, mapHeight;
    private int edgeRadius; // visibleRadius + 1

    private Vector2Int lastPlayerTile = new Vector2Int(-999, -999);

    // ─── 초기화 ───

    private void Start()
    {
        edgeRadius = visibleRadius + 1;

        if (dungeonManager == null || dungeonManager.Grid == null)
        {
            Debug.LogError("[FogOfWar] DungeonManager 참조 없음 또는 던전 미생성!");
            return;
        }
        if (movementSystem == null)
        {
            Debug.LogError("[FogOfWar] MovementSystem 참조 없음!");
            return;
        }

        mapWidth  = dungeonManager.MapWidth;
        mapHeight = dungeonManager.MapHeight;

        InitFogMap();
        BuildMesh();
        BuildTexture();

        movementSystem.OnTileEntered += OnPlayerMoved;
        UpdateFog(dungeonManager.StartPosition);

        Debug.Log($"[FogOfWar] 초기화 완료 ({mapWidth}x{mapHeight}), 가시:{visibleRadius} 경계:{edgeRadius} (Chebyshev/사각형)");
    }

    private void OnDestroy()
    {
        if (movementSystem != null) movementSystem.OnTileEntered -= OnPlayerMoved;
        if (fogTexture  != null) Destroy(fogTexture);
        if (fogMaterial != null && fogMaterialOverride == null) Destroy(fogMaterial);
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
    // Bresenham Ray Casting FOV (사각형 경계)
    //
    // 원리:
    //   edgeRadius 크기의 사각형 경계 위의 모든 타일을 목표점으로 삼아
    //   원점 → 목표점 방향으로 Bresenham 직선 Ray를 발사한다.
    //   사각형 경계 = Chebyshev 거리(max(|dx|,|dy|)) == edgeRadius
    //
    //   마름모(Manhattan) 대신 사각형을 사용하면:
    //   - 상/하/좌/우 대각선 모서리까지 빈틈없이 Ray가 도달
    //   - visibleRadius=2일 때 5x5 완전 가시, 7x7 테두리 반투명
    // ═══════════════════════════════════════════════════════

    private void CastFieldOfView(Vector2Int origin)
    {
        // 원점 항상 가시
        MarkVisible(origin, 0);

        // 사각형 경계(Chebyshev 거리 == edgeRadius) 위의 모든 점을 향해 Ray 발사
        for (int dx = -edgeRadius; dx <= edgeRadius; dx++)
        {
            for (int dy = -edgeRadius; dy <= edgeRadius; dy++)
            {
                // 사각형 경계만 (테두리 한 줄)
                if (Mathf.Abs(dx) != edgeRadius && Mathf.Abs(dy) != edgeRadius)
                    continue;

                CastRay(origin, origin + new Vector2Int(dx, dy));
            }
        }
    }

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
            if (cx != x0 || cy != y0)
            {
                if (cx < 0 || cx >= mapWidth || cy < 0 || cy >= mapHeight) break;

                // Chebyshev 거리 = max(|dx|, |dy|)
                int dist = Mathf.Max(Mathf.Abs(cx - x0), Mathf.Abs(cy - y0));
                MarkVisible(new Vector2Int(cx, cy), dist);

                // 벽: 이 타일에서 시야 차단 (벽 자체는 보임)
                if (dungeonManager.Grid[cx, cy].type == TileType.WALL) break;
            }

            if (cx == x1 && cy == y1) break;

            int e2 = 2 * err;
            if (e2 > -dy) { err -= dy; cx += sx; }
            if (e2 <  dx) { err += dx; cy += sy; }
        }
    }

    /// <summary>
    /// dist <= visibleRadius → 완전 투명 (가시)
    /// dist > visibleRadius → 반투명 (경계)
    /// </summary>
    private void MarkVisible(Vector2Int pos, int dist)
    {
        currentVisible.Add(pos);
        if (dist > visibleRadius)
            currentEdge.Add(pos);
    }

    // ─── 텍스처 적용 (Color32 전체 갱신) ───

    private void ApplyTexture()
    {
        byte edgeByte = (byte)(edgeAlpha * 255);

        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                byte alpha;
                switch (fogMap[x, y])
                {
                    case FogState.Unexplored:
                        alpha = 255;
                        break;
                    case FogState.Explored:
                        alpha = edgeByte;
                        break;
                    case FogState.Visible:
                        alpha = currentEdge.Contains(new Vector2Int(x, y))
                            ? edgeByte : (byte)0;
                        break;
                    default:
                        alpha = 255;
                        break;
                }
                pixelBuffer[y * mapWidth + x] = new Color32(0, 0, 0, alpha);
            }
        }

        fogTexture.SetPixels32(pixelBuffer);
        fogTexture.Apply();
    }

    // ─── 공개 API ───

    public bool IsTileRevealed(int x, int y)
    {
        if (x < 0 || x >= mapWidth || y < 0 || y >= mapHeight) return false;
        return fogMap[x, y] != FogState.Unexplored;
    }

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

        pixelBuffer = new Color32[mapWidth * mapHeight];
        for (int i = 0; i < pixelBuffer.Length; i++)
            pixelBuffer[i] = new Color32(0, 0, 0, 255);
        fogTexture.SetPixels32(pixelBuffer);
        fogTexture.Apply();

        if (fogMaterialOverride != null)
        {
            fogMaterial = fogMaterialOverride;
        }
        else
        {
            Shader shader = Shader.Find("Sprites/Default")
                         ?? Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                Debug.LogError("[FogOfWar] 호환 셰이더를 찾을 수 없습니다!");
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
        }

        fogMaterial.mainTexture = fogTexture;
        meshRenderer.material = fogMaterial;
    }

    // ─── Gizmo ───

    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying || movementSystem == null) return;
        Vector2Int p = movementSystem.CurrentTilePosition;

        // 완전 가시 범위 (사각형)
        Gizmos.color = new Color(0f, 1f, 0f, 0.2f);
        for (int x = -visibleRadius; x <= visibleRadius; x++)
            for (int y = -visibleRadius; y <= visibleRadius; y++)
                Gizmos.DrawCube(new Vector3(p.x + x + 0.5f, p.y + y + 0.5f, 0), Vector3.one);

        // 경계 범위 (사각형 테두리)
        Gizmos.color = new Color(1f, 1f, 0f, 0.2f);
        for (int x = -edgeRadius; x <= edgeRadius; x++)
            for (int y = -edgeRadius; y <= edgeRadius; y++)
                if (Mathf.Abs(x) == edgeRadius || Mathf.Abs(y) == edgeRadius)
                    Gizmos.DrawCube(new Vector3(p.x + x + 0.5f, p.y + y + 0.5f, 0), Vector3.one);
    }
}

// ============================================================
// FogOfWar.cs — 전장의 안개 시스템 v5
// 위치: Assets/Scripts/Rendering/FogOfWar.cs
// ============================================================
// [v5 변경사항]
//   - 층별 안개 상태 캐싱: 층 이동 시 현재 fogMap을 저장하고
//     돌아왔을 때 복원 (이전에 탐험한 영역이 유지됨)
//   - DungeonManager.OnFloorChanged 이벤트 구독
//   - 층 변경 시 안개 완전 초기화 또는 캐시에서 복원
//   - Mesh/Texture는 최초 1회만 생성 (맵 크기가 동일하므로 재사용)
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
    private int edgeRadius;

    private Vector2Int lastPlayerTile = new Vector2Int(-999, -999);
    private bool isInitialized = false;

    // ─── 층별 안개 캐시 ───
    private Dictionary<int, FogState[,]> fogCacheMap = new Dictionary<int, FogState[,]>();
    private int currentCachedFloor = -1;

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

        // 이벤트 구독
        movementSystem.OnTileEntered += OnPlayerMoved;
        dungeonManager.OnFloorChanged += OnFloorChanged;

        currentCachedFloor = dungeonManager.CurrentFloorIndex;
        UpdateFog(dungeonManager.StartPosition);
        isInitialized = true;

        Debug.Log($"[FogOfWar] 초기화 완료 ({mapWidth}x{mapHeight}), 가시:{visibleRadius} 경계:{edgeRadius}");
    }

    private void OnDestroy()
    {
        if (movementSystem != null) movementSystem.OnTileEntered -= OnPlayerMoved;
        if (dungeonManager != null) dungeonManager.OnFloorChanged -= OnFloorChanged;
        if (fogTexture  != null) Destroy(fogTexture);
        if (fogMaterial != null && fogMaterialOverride == null) Destroy(fogMaterial);
    }

    // ─── 이벤트 핸들러 ───

    private void OnPlayerMoved(Vector2Int pos)
    {
        if (pos == lastPlayerTile) return;
        UpdateFog(pos);
    }

    /// <summary>
    /// 층이 변경되었을 때 호출된다.
    /// 현재 안개 상태를 캐시에 저장하고, 새 층의 안개를 복원 또는 초기화한다.
    /// </summary>
    private void OnFloorChanged(int newFloorIndex)
    {
        if (!isInitialized) return;

        // 1. 현재 층 안개 상태를 캐시에 저장
        SaveCurrentFogToCache();

        // 2. 새 층의 안개 복원 또는 초기화
        currentCachedFloor = newFloorIndex;

        if (fogCacheMap.ContainsKey(newFloorIndex))
        {
            // 이전에 방문한 층 → 캐시에서 복원
            RestoreFogFromCache(newFloorIndex);
            Debug.Log($"[FogOfWar] {newFloorIndex}층 안개 캐시에서 복원.");
        }
        else
        {
            // 처음 방문하는 층 → 완전 초기화 (모두 미탐험)
            ResetFogMap();
            Debug.Log($"[FogOfWar] {newFloorIndex}층 안개 새로 초기화.");
        }

        // 3. 플레이어 현재 위치 기준으로 안개 갱신
        lastPlayerTile = new Vector2Int(-999, -999); // 강제 갱신
        UpdateFog(movementSystem.CurrentTilePosition);
    }

    // ─── 캐시 관리 ───

    private void SaveCurrentFogToCache()
    {
        if (currentCachedFloor < 0) return;

        // fogMap 깊은 복사
        FogState[,] copy = new FogState[mapWidth, mapHeight];
        for (int x = 0; x < mapWidth; x++)
            for (int y = 0; y < mapHeight; y++)
                copy[x, y] = fogMap[x, y];

        fogCacheMap[currentCachedFloor] = copy;
    }

    private void RestoreFogFromCache(int floorIndex)
    {
        FogState[,] cached = fogCacheMap[floorIndex];
        for (int x = 0; x < mapWidth; x++)
            for (int y = 0; y < mapHeight; y++)
                fogMap[x, y] = cached[x, y];
    }

    private void ResetFogMap()
    {
        for (int x = 0; x < mapWidth; x++)
            for (int y = 0; y < mapHeight; y++)
                fogMap[x, y] = FogState.Unexplored;

        currentVisible.Clear();
        currentEdge.Clear();
    }

    public void ResetAllFogState()
    {
        fogCacheMap.Clear();
        currentCachedFloor = dungeonManager != null ? dungeonManager.CurrentFloorIndex : -1;
        lastPlayerTile = new Vector2Int(-999, -999);

        if (fogMap != null)
            ResetFogMap();

        if (movementSystem != null && dungeonManager != null && dungeonManager.Grid != null && fogMap != null)
            UpdateFog(movementSystem.CurrentTilePosition);

        Debug.Log("[FogOfWar] 모든 안개 캐시 초기화 완료");
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
    // ═══════════════════════════════════════════════════════

    private void CastFieldOfView(Vector2Int origin)
    {
        MarkVisible(origin, 0);

        for (int dx = -edgeRadius; dx <= edgeRadius; dx++)
        {
            for (int dy = -edgeRadius; dy <= edgeRadius; dy++)
            {
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

                int dist = Mathf.Max(Mathf.Abs(cx - x0), Mathf.Abs(cy - y0));
                MarkVisible(new Vector2Int(cx, cy), dist);

                if (dungeonManager.Grid[cx, cy].type == TileType.WALL) break;
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
        if (dist > visibleRadius)
            currentEdge.Add(pos);
    }

    // ─── 텍스처 적용 ───

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

    // ─── Mesh / Texture (최초 1회 생성, 층 변경 시 재사용) ───

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

        Gizmos.color = new Color(0f, 1f, 0f, 0.2f);
        for (int x = -visibleRadius; x <= visibleRadius; x++)
            for (int y = -visibleRadius; y <= visibleRadius; y++)
                Gizmos.DrawCube(new Vector3(p.x + x + 0.5f, p.y + y + 0.5f, 0), Vector3.one);

        Gizmos.color = new Color(1f, 1f, 0f, 0.2f);
        for (int x = -edgeRadius; x <= edgeRadius; x++)
            for (int y = -edgeRadius; y <= edgeRadius; y++)
                if (Mathf.Abs(x) == edgeRadius || Mathf.Abs(y) == edgeRadius)
                    Gizmos.DrawCube(new Vector3(p.x + x + 0.5f, p.y + y + 0.5f, 0), Vector3.one);
    }
}

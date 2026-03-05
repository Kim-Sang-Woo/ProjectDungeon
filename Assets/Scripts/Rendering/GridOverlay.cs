// ============================================================
// GridOverlay.cs — 그리드 오버레이 v4
// 위치: Assets/Scripts/Rendering/GridOverlay.cs
// ============================================================
// [v4 변경사항]
//   - DungeonGenerator 직접 참조 → DungeonManager 참조
//   - Update() 매 프레임 폴링 → MovementSystem.OnTileEntered 이벤트 구독
//   - 불필요한 매 프레임 체크 제거, 이벤트 기반으로 성능 개선
// ============================================================

using System.Collections.Generic;
using UnityEngine;

public class GridOverlay : MonoBehaviour
{
    [Header("그리드 설정")]
    [Tooltip("그리드 선 색상")]
    public Color gridColor = new Color(1f, 1f, 1f, 0.3f);

    [Tooltip("이동 가능 타일 위에만 그리드 표시")]
    public bool onlyWalkableTiles = true;

    [Header("참조")]
    public DungeonManager dungeonManager;

    [Tooltip("FogOfWar 참조 — 탐험 여부 확인. 비워두면 전체 표시.")]
    public FogOfWar fogOfWar;

    [Tooltip("MovementSystem 참조 — 이동 이벤트 구독용")]
    public MovementSystem movementSystem;

    // 런타임 Mesh
    private GameObject lineObject;
    private Mesh       gridMesh;
    private Material   lineMaterial;

    private void Start()
    {
        if (dungeonManager == null || dungeonManager.Grid == null)
        {
            Debug.LogError("[GridOverlay] DungeonManager 참조 없음 또는 던전 미생성!");
            return;
        }

        BuildMaterial();
        BuildGridMesh();

        // 이벤트 구독 — Update 폴링 대신 타일 진입 시에만 재빌드
        if (movementSystem != null)
        {
            movementSystem.OnTileEntered += OnPlayerMoved;
        }
    }

    private void OnDestroy()
    {
        if (movementSystem != null)
            movementSystem.OnTileEntered -= OnPlayerMoved;

        if (gridMesh     != null) Destroy(gridMesh);
        if (lineMaterial != null) Destroy(lineMaterial);
    }

    private void OnPlayerMoved(Vector2Int pos)
    {
        // 플레이어가 새 타일에 진입했을 때만 그리드 재빌드
        BuildGridMesh();
    }

    // ─── Mesh 생성 ───

    private void BuildMaterial()
    {
        Shader shader = Shader.Find("Sprites/Default")
                     ?? Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
        {
            Debug.LogError("[GridOverlay] 호환 셰이더를 찾을 수 없습니다!");
            return;
        }

        lineMaterial = new Material(shader);
        lineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        lineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        lineMaterial.SetInt("_ZWrite",   0);
        lineMaterial.renderQueue = 3001;
    }

    private void BuildGridMesh()
    {
        if (dungeonManager == null || dungeonManager.Grid == null) return;
        if (lineMaterial == null) return;

        int w = dungeonManager.MapWidth;
        int h = dungeonManager.MapHeight;
        TileData[,] grid = dungeonManager.Grid;

        List<Vector3> verts   = new List<Vector3>();
        List<int>     indices = new List<int>();
        List<Color>   colors  = new List<Color>();

        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                if (onlyWalkableTiles && !grid[x, y].IsWalkable)
                    continue;

                if (!IsTileRevealed(x, y)) continue;

                float x0 = x, x1 = x + 1f;
                float y0 = y, y1 = y + 1f;
                float z  = -1f;

                if (ShouldDrawEdge(x, y, x, y + 1, w, h, grid))
                    AddLine(verts, indices, colors, new Vector3(x0, y1, z), new Vector3(x1, y1, z));

                if (ShouldDrawEdge(x, y, x, y - 1, w, h, grid))
                    AddLine(verts, indices, colors, new Vector3(x0, y0, z), new Vector3(x1, y0, z));

                if (ShouldDrawEdge(x, y, x + 1, y, w, h, grid))
                    AddLine(verts, indices, colors, new Vector3(x1, y0, z), new Vector3(x1, y1, z));

                if (ShouldDrawEdge(x, y, x - 1, y, w, h, grid))
                    AddLine(verts, indices, colors, new Vector3(x0, y0, z), new Vector3(x0, y1, z));
            }
        }

        if (lineObject == null)
        {
            lineObject = new GameObject("GridMesh");
            lineObject.transform.SetParent(transform);
            lineObject.transform.localPosition = Vector3.zero;

            var mf = lineObject.AddComponent<MeshFilter>();
            var mr = lineObject.AddComponent<MeshRenderer>();
            mr.material      = lineMaterial;
            mr.sortingOrder  = 11;
            mr.shadowCastingMode  = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows     = false;

            gridMesh = new Mesh { name = "GridOverlayMesh" };
            mf.mesh  = gridMesh;
        }

        gridMesh.Clear();

        if (verts.Count == 0) return;

        gridMesh.SetVertices(verts);
        gridMesh.SetColors(colors);
        gridMesh.SetIndices(indices.ToArray(), MeshTopology.Lines, 0);
        gridMesh.RecalculateBounds();
    }

    // ─── 헬퍼 ───

    private void AddLine(
        List<Vector3> verts, List<int> indices, List<Color> colors,
        Vector3 a, Vector3 b)
    {
        int i = verts.Count;
        verts.Add(a); verts.Add(b);
        indices.Add(i); indices.Add(i + 1);
        colors.Add(gridColor); colors.Add(gridColor);
    }

    private bool ShouldDrawEdge(int x, int y, int nx, int ny, int w, int h, TileData[,] grid)
    {
        if (nx < 0 || nx >= w || ny < 0 || ny >= h) return true;

        if (onlyWalkableTiles)
        {
            if (!grid[nx, ny].IsWalkable) return true;
            if (!IsTileRevealed(nx, ny)) return true;
            return true;
        }

        return true;
    }

    private bool IsTileRevealed(int x, int y)
    {
        if (fogOfWar == null) return true;
        return fogOfWar.IsTileRevealed(x, y);
    }
}

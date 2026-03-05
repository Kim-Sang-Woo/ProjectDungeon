// ============================================================
// GridOverlay.cs — 그리드 오버레이 v3
// [수정] URP에서 GL 렌더링 미표시 문제 → LineRenderer Mesh 방식으로 교체
// ============================================================
// [씬 배치]
//   Main Camera에 부착 (RequireComponent 제거됨 → 어디든 부착 가능)
//   단, 실제로는 DungeonManager 하위 빈 GameObject에 부착 권장
//
// [Inspector 연결 필수]
//   dungeonGenerator → DungeonManager
//   fogOfWar         → FogOfWar
//
// [v3 변경 사항]
//   GL.Lines(OnPostRender) 방식 → GL 방식은 URP에서 FogOfWar Mesh보다
//   렌더 순서 제어가 불안정하여 완전히 교체.
//   대신 동적 Mesh(GL_LINES 대신 MeshTopology.Lines)를 사용하여
//   SpriteRenderer와 동일한 렌더 파이프라인에서 FogOfWar 위에 그린다.
//   - sortingOrder = 11 (FogOfWar = 10 보다 위)
//   - FogOfWar.IsTileRevealed()로 탐험된 타일만 그리드 표시
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
    public DungeonGenerator dungeonGenerator;

    [Tooltip("FogOfWar 참조 — 탐험 여부 확인. 비워두면 전체 표시.")]
    public FogOfWar fogOfWar;

    // 런타임 Mesh
    private GameObject lineObject;
    private Mesh       gridMesh;
    private Material   lineMaterial;

    private void Start()
    {
        if (dungeonGenerator == null)
        {
            Debug.LogError("[GridOverlay] DungeonGenerator 참조 없음!");
            return;
        }

        BuildMaterial();
        BuildGridMesh();
    }

    // ─── FogOfWar 갱신 시 그리드 재빌드 ───
    // FogOfWar가 타일을 밝힐 때마다 그리드도 갱신되어야 한다.
    // Update()에서 매 프레임 체크하는 대신
    // FogOfWar의 OnPlayerMoved와 같은 타이밍에 재빌드한다.

    private void Update()
    {
        // 매 프레임 재빌드는 비용이 크므로
        // fogOfWar가 있을 때만 플레이어 위치 변경 감지 후 재빌드
        if (fogOfWar != null && dungeonGenerator != null && dungeonGenerator.grid != null)
        {
            // MovementSystem이 있으면 위치 변경 감지
            if (dungeonGenerator != null)
                RebuildIfNeeded();
        }
    }

    private Vector2Int lastKnownPlayerPos = new Vector2Int(-999, -999);

    private void RebuildIfNeeded()
    {
        // FogOfWar와 연동된 MovementSystem 위치 확인
        if (fogOfWar == null || fogOfWar.movementSystem == null) return;

        Vector2Int cur = fogOfWar.movementSystem.CurrentTilePosition;
        if (cur == lastKnownPlayerPos) return;

        lastKnownPlayerPos = cur;
        BuildGridMesh();
    }

    // ─── Mesh 생성 ───

    private void BuildMaterial()
    {
        Shader shader = Shader.Find("Sprites/Default")
                     ?? Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
        {
            Debug.LogError("[GridOverlay] 호환 쉐이더를 찾을 수 없습니다!");
            return;
        }

        lineMaterial = new Material(shader);
        lineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        lineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        lineMaterial.SetInt("_ZWrite",   0);
        lineMaterial.renderQueue = 3001; // FogOfWar(3000)보다 1 위
    }

    /// <summary>
    /// 탐험된 walkable 타일의 경계선을 Lines Mesh로 빌드한다.
    /// 타일이 밝혀질 때마다 재호출된다.
    /// </summary>
    private void BuildGridMesh()
    {
        if (dungeonGenerator == null || dungeonGenerator.grid == null) return;
        if (lineMaterial == null) return;

        int w = dungeonGenerator.floorData.mapWidth;
        int h = dungeonGenerator.floorData.mapHeight;

        List<Vector3> verts   = new List<Vector3>();
        List<int>     indices = new List<int>();
        List<Color>   colors  = new List<Color>();

        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                // walkable 필터
                if (onlyWalkableTiles && !dungeonGenerator.grid[x, y].IsWalkable)
                    continue;

                // 탐험 여부 필터
                if (!IsTileRevealed(x, y)) continue;

                float x0 = x, x1 = x + 1f;
                float y0 = y, y1 = y + 1f;
                float z  = -1f; // FogOfWar(fogZDepth=-0.5) 보다 앞

                // 상단선
                if (ShouldDrawEdge(x, y, x, y + 1, w, h))
                    AddLine(verts, indices, colors, new Vector3(x0, y1, z), new Vector3(x1, y1, z));

                // 하단선
                if (ShouldDrawEdge(x, y, x, y - 1, w, h))
                    AddLine(verts, indices, colors, new Vector3(x0, y0, z), new Vector3(x1, y0, z));

                // 우측선
                if (ShouldDrawEdge(x, y, x + 1, y, w, h))
                    AddLine(verts, indices, colors, new Vector3(x1, y0, z), new Vector3(x1, y1, z));

                // 좌측선
                if (ShouldDrawEdge(x, y, x - 1, y, w, h))
                    AddLine(verts, indices, colors, new Vector3(x0, y0, z), new Vector3(x0, y1, z));
            }
        }

        // Mesh 생성 또는 갱신
        if (lineObject == null)
        {
            lineObject = new GameObject("GridMesh");
            lineObject.transform.SetParent(transform);
            lineObject.transform.localPosition = Vector3.zero;

            var mf = lineObject.AddComponent<MeshFilter>();
            var mr = lineObject.AddComponent<MeshRenderer>();
            mr.material      = lineMaterial;
            mr.sortingOrder  = 11; // FogOfWar(10) 위
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

    /// <summary>
    /// (x,y) 타일에서 (nx,ny) 방향의 경계선을 그려야 하는지 판단.
    /// nx,ny가 맵 밖이거나 벽이거나 미탐험이면 경계선 표시.
    /// nx,ny가 탐험된 walkable이면 내부 구분선 표시.
    /// </summary>
    private bool ShouldDrawEdge(int x, int y, int nx, int ny, int w, int h)
    {
        // 인접이 맵 밖
        if (nx < 0 || nx >= w || ny < 0 || ny >= h) return true;

        if (onlyWalkableTiles)
        {
            // 인접이 walkable이 아님 (벽)
            if (!dungeonGenerator.grid[nx, ny].IsWalkable) return true;
            // 인접이 walkable이지만 미탐험 → 경계 표시
            if (!IsTileRevealed(nx, ny)) return true;
            // 둘 다 탐험된 walkable → 내부 구분선
            return true;
        }

        return true;
    }

    private bool IsTileRevealed(int x, int y)
    {
        if (fogOfWar == null) return true;
        return fogOfWar.IsTileRevealed(x, y);
    }

    private void OnDestroy()
    {
        if (gridMesh     != null) Destroy(gridMesh);
        if (lineMaterial != null) Destroy(lineMaterial);
    }
}

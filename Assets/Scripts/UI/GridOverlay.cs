// ============================================================
// GridOverlay.cs — 맵 위에 그리드 선을 표시하는 오버레이
// URP 호환 — Main Camera에 부착
// ============================================================
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 던전 맵 위에 타일 경계선(그리드)을 렌더링한다.
/// URP 환경에서도 동작하도록 RenderPipelineManager 이벤트를 사용한다.
/// </summary>
[RequireComponent(typeof(Camera))]
public class GridOverlay : MonoBehaviour
{
    [Header("그리드 설정")]
    [Tooltip("그리드 선 색상")]
    public Color gridColor = new Color(1f, 1f, 1f, 0.3f);

    [Tooltip("이동 가능 타일 위에만 그리드 표시")]
    public bool onlyWalkableTiles = true;

    [Header("참조")]
    [Tooltip("DungeonGenerator 참조 (맵 크기 접근용)")]
    public DungeonGenerator dungeonGenerator;

    private Material lineMaterial;

    private void OnEnable()
    {
        RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
    }

    private void OnDisable()
    {
        RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
    }

    private void OnEndCameraRendering(ScriptableRenderContext context, Camera cam)
    {
        // 이 카메라에서만 렌더링
        if (cam != GetComponent<Camera>()) return;
        DrawGrid();
    }

    // Standard RP 호환 (URP가 아닌 경우)
    private void OnPostRender()
    {
        DrawGrid();
    }

    private void DrawGrid()
    {
        if (dungeonGenerator == null || dungeonGenerator.grid == null) return;

        CreateLineMaterial();
        lineMaterial.SetPass(0);

        int width = dungeonGenerator.floorData.mapWidth;
        int height = dungeonGenerator.floorData.mapHeight;

        GL.PushMatrix();
        GL.MultMatrix(Matrix4x4.identity);
        GL.Begin(GL.LINES);
        GL.Color(gridColor);

        if (onlyWalkableTiles)
        {
            DrawWalkableGrid(width, height);
        }
        else
        {
            DrawFullGrid(width, height);
        }

        GL.End();
        GL.PopMatrix();
    }

    private void CreateLineMaterial()
    {
        if (lineMaterial != null) return;

        Shader shader = Shader.Find("Hidden/Internal-Colored");
        lineMaterial = new Material(shader);
        lineMaterial.hideFlags = HideFlags.HideAndDontSave;
        lineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        lineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        lineMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
        lineMaterial.SetInt("_ZWrite", 0);
    }

    private void DrawFullGrid(int width, int height)
    {
        for (int x = 0; x <= width; x++)
        {
            GL.Vertex3(x, 0, 0);
            GL.Vertex3(x, height, 0);
        }
        for (int y = 0; y <= height; y++)
        {
            GL.Vertex3(0, y, 0);
            GL.Vertex3(width, y, 0);
        }
    }

    private void DrawWalkableGrid(int width, int height)
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (!dungeonGenerator.grid[x, y].IsWalkable) continue;

                float x0 = x, x1 = x + 1;
                float y0 = y, y1 = y + 1;

                // 외곽 경계선 (벽과의 경계)
                if (y + 1 >= height || !dungeonGenerator.grid[x, y + 1].IsWalkable)
                { GL.Vertex3(x0, y1, 0); GL.Vertex3(x1, y1, 0); }
                if (y - 1 < 0 || !dungeonGenerator.grid[x, y - 1].IsWalkable)
                { GL.Vertex3(x0, y0, 0); GL.Vertex3(x1, y0, 0); }
                if (x + 1 >= width || !dungeonGenerator.grid[x + 1, y].IsWalkable)
                { GL.Vertex3(x1, y0, 0); GL.Vertex3(x1, y1, 0); }
                if (x - 1 < 0 || !dungeonGenerator.grid[x - 1, y].IsWalkable)
                { GL.Vertex3(x0, y0, 0); GL.Vertex3(x0, y1, 0); }

                // 내부 칸 구분선
                if (x + 1 < width && dungeonGenerator.grid[x + 1, y].IsWalkable)
                { GL.Vertex3(x1, y0, 0); GL.Vertex3(x1, y1, 0); }
                if (y + 1 < height && dungeonGenerator.grid[x, y + 1].IsWalkable)
                { GL.Vertex3(x0, y1, 0); GL.Vertex3(x1, y1, 0); }
            }
        }
    }
}

// ============================================================
// DungeonRenderer.cs — 타일맵 렌더링 전담
// 위치: Assets/Scripts/Rendering/DungeonRenderer.cs
// ============================================================
// [v3.1 변경사항]
//   - 계단 스프라이트 자동 생성 제거
//   - 계단 타일은 Inspector에서 직접 할당
//   - 할당하지 않으면 바닥 타일이 표시됨 (경고 로그 출력)
// ============================================================
using UnityEngine;
using UnityEngine.Tilemaps;

public class DungeonRenderer : MonoBehaviour
{
    [Header("타일맵")]
    [Tooltip("던전을 렌더링할 Tilemap 참조")]
    public Tilemap tilemap;

    [Header("기본 타일")]
    [Tooltip("바닥 타일")]
    public TileBase floorTile;
    [Tooltip("벽 타일")]
    public TileBase wallTile;
    [Tooltip("복도 타일 (null이면 floorTile 사용)")]
    public TileBase corridorTile;

    [Header("계단 타일")]
    [Tooltip("올라가는 계단 타일 (내려온 계단 / 시작 지점)")]
    public TileBase stairsUpTile;
    [Tooltip("내려가는 계단 타일 (출구 / 끝 지점)")]
    public TileBase stairsDownTile;

    public void RenderDungeon(DungeonManager manager)
    {
        if (tilemap == null)
        {
            Debug.LogError("[DungeonRenderer] Tilemap 참조가 설정되지 않았습니다!");
            return;
        }

        if (floorTile == null)
            Debug.LogError("[DungeonRenderer] floorTile이 할당되지 않았습니다!");
        if (wallTile == null)
            Debug.LogWarning("[DungeonRenderer] wallTile이 할당되지 않았습니다.");
        if (stairsUpTile == null)
            Debug.LogWarning("[DungeonRenderer] stairsUpTile이 할당되지 않았습니다. 시작 지점에 바닥 타일이 표시됩니다.");
        if (stairsDownTile == null)
            Debug.LogWarning("[DungeonRenderer] stairsDownTile이 할당되지 않았습니다. 출구 지점에 바닥 타일이 표시됩니다.");

        tilemap.ClearAllTiles();

        int w = manager.MapWidth;
        int h = manager.MapHeight;
        TileData[,] grid = manager.Grid;

        int floorCount = 0, wallCount = 0, corridorCount = 0, nullCount = 0;

        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                Vector3Int tilePos = new Vector3Int(x, y, 0);
                TileBase tile = null;

                switch (grid[x, y].type)
                {
                    case TileType.WALL:
                        tile = wallTile;
                        if (tile != null) wallCount++;
                        break;
                    case TileType.FLOOR:
                        tile = floorTile;
                        if (tile != null) floorCount++;
                        break;
                    case TileType.CORRIDOR:
                        tile = corridorTile != null ? corridorTile : floorTile;
                        if (tile != null) corridorCount++;
                        break;
                }

                if (tile != null)
                    tilemap.SetTile(tilePos, tile);
                else
                    nullCount++;
            }
        }

        // 계단 타일 오버라이드
        Vector2Int startPos = manager.StartPosition;
        Vector2Int exitPos = manager.ExitPosition;

        if (stairsUpTile != null)
            tilemap.SetTile(new Vector3Int(startPos.x, startPos.y, 0), stairsUpTile);
        if (stairsDownTile != null)
            tilemap.SetTile(new Vector3Int(exitPos.x, exitPos.y, 0), stairsDownTile);

        tilemap.RefreshAllTiles();

        Debug.Log($"[DungeonRenderer] 렌더링 완료. 바닥:{floorCount} 벽:{wallCount} 복도:{corridorCount} 계단: 시작({startPos}) 출구({exitPos})");
    }
}

// ============================================================
// DungeonRenderer.cs — 타일맵 렌더링 전담
// 위치: Assets/Scripts/Rendering/DungeonRenderer.cs
// ============================================================
// [v3.2 변경사항]
//   - 계단 타일 오버라이드 제거
//     계단은 이제 DungeonObjectSpawner가 스프라이트 오브젝트로 표시하므로
//     Tilemap에 별도 계단 타일을 올릴 필요가 없음
//   - stairsUpTile / stairsDownTile 필드 제거
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

        tilemap.ClearAllTiles();

        int w = manager.MapWidth;
        int h = manager.MapHeight;
        TileData[,] grid = manager.Grid;

        int floorCount = 0, wallCount = 0, corridorCount = 0;

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
            }
        }

        tilemap.RefreshAllTiles();

        Debug.Log($"[DungeonRenderer] 렌더링 완료. 바닥:{floorCount} 벽:{wallCount} 복도:{corridorCount}");
    }
}

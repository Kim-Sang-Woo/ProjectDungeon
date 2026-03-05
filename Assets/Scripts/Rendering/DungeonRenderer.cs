// ============================================================
// DungeonRenderer.cs — 타일맵 렌더링 전담
// 위치: Assets/Scripts/Rendering/DungeonRenderer.cs
// ============================================================
// [v2.1 변경사항]
//   - 타일 배치 카운트 디버그 로그 추가
//   - floorTile/wallTile null 경고 추가
//   - 렌더링 후 RefreshAllTiles() 호출 (일부 Unity 버전 대응)
// ============================================================
using UnityEngine;
using UnityEngine.Tilemaps;

public class DungeonRenderer : MonoBehaviour
{
    [Header("타일맵")]
    [Tooltip("던전을 렌더링할 Tilemap 참조")]
    public Tilemap tilemap;

    [Tooltip("바닥 타일")]
    public TileBase floorTile;
    [Tooltip("벽 타일")]
    public TileBase wallTile;
    [Tooltip("복도 타일 (null이면 floorTile 사용)")]
    public TileBase corridorTile;
    [Tooltip("입구 계단 타일 (null이면 floorTile 사용)")]
    public TileBase stairsUpTile;
    [Tooltip("출구 계단 타일 (null이면 floorTile 사용)")]
    public TileBase stairsDownTile;

    public void RenderDungeon(DungeonManager manager)
    {
        if (tilemap == null)
        {
            Debug.LogError("[DungeonRenderer] Tilemap 참조가 설정되지 않았습니다!");
            return;
        }

        // 타일 에셋 null 체크
        if (floorTile == null)
            Debug.LogError("[DungeonRenderer] floorTile이 할당되지 않았습니다! Inspector를 확인하세요.");
        if (wallTile == null)
            Debug.LogWarning("[DungeonRenderer] wallTile이 할당되지 않았습니다. 벽이 표시되지 않습니다.");

        tilemap.ClearAllTiles();

        int w = manager.MapWidth;
        int h = manager.MapHeight;
        TileData[,] grid = manager.Grid;

        int floorCount = 0;
        int wallCount = 0;
        int corridorCount = 0;
        int nullCount = 0;

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

        // 입구/출구 계단 타일 오버라이드
        Vector2Int startPos = manager.StartPosition;
        Vector2Int exitPos = manager.ExitPosition;

        if (stairsUpTile != null)
            tilemap.SetTile(new Vector3Int(startPos.x, startPos.y, 0), stairsUpTile);
        if (stairsDownTile != null)
            tilemap.SetTile(new Vector3Int(exitPos.x, exitPos.y, 0), stairsDownTile);

        // 일부 Unity 버전에서 런타임 SetTile 후 렌더링 갱신이 안 되는 경우 대응
        tilemap.RefreshAllTiles();

        Debug.Log($"[DungeonRenderer] 타일맵 렌더링 완료. 바닥:{floorCount} 벽:{wallCount} 복도:{corridorCount} null(미배치):{nullCount}");
    }
}

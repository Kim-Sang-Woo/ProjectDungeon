// ============================================================
// TileData.cs — 타일 단위 데이터 (런타임 2D 배열)
// 기획서 Ch.0.3 참조
// 위치: Assets/Scripts/Data/TileData.cs
// ============================================================
// [v2 변경사항]
//   struct → class 변경
//   - struct일 때 GetTileData() 반환값 수정이 원본에 반영되지 않는
//     값 복사 버그를 근본적으로 해결
//   - 이제 grid[x,y]를 꺼내 수정하면 원본에 바로 반영됨
// ============================================================

/// <summary>
/// 맵의 각 타일 셀에 대한 런타임 데이터.
/// TileData[,] grid 형태로 DungeonGenerator가 생성하고 DungeonManager가 소유한다.
/// </summary>
[System.Serializable]
public class TileData
{
    /// <summary>타일 유형 (WALL / FLOOR / CORRIDOR)</summary>
    public TileType type;

    /// <summary>소속 방 ID. -1이면 복도이거나 벽</summary>
    public int roomId;

    /// <summary>이 타일에 배치된 이벤트. null이면 이벤트 없음</summary>
    public DungeonEventData eventData;

    /// <summary>이벤트 소진 여부. true면 이미 처리된 이벤트</summary>
    public bool isEventConsumed;

    /// <summary>이동 가능 여부 (FLOOR 또는 CORRIDOR)</summary>
    public bool IsWalkable => type == TileType.FLOOR || type == TileType.CORRIDOR;

    public TileData()
    {
        type = TileType.WALL;
        roomId = -1;
        eventData = null;
        isEventConsumed = false;
    }
}

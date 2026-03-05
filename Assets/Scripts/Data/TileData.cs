// ============================================================
// TileData.cs — 타일 단위 데이터 (런타임 2D 배열)
// 기획서 Ch.0.3 참조
// ============================================================

/// <summary>
/// 맵의 각 타일 셀에 대한 런타임 데이터.
/// TileData[,] grid 형태로 DungeonGenerator가 생성하고 DungeonManager가 소유한다.
/// </summary>
[System.Serializable]
public struct TileData
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
}

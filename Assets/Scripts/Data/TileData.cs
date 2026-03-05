// ============================================================
// TileData.cs — 타일 단위 데이터 (런타임 2D 배열)
// 기획서 Ch.0.3 참조
// 위치: Assets/Scripts/Data/TileData.cs
// ============================================================
// [v3 변경사항]
//   - placedObject 필드 추가: 타일 위에 배치된 오브젝트 데이터 참조
//   - isObjectInteracted 필드 추가: 오브젝트 상호작용 완료 여부
//   - HasObject 프로퍼티 추가: 유효한 오브젝트 존재 여부 간편 확인
// ============================================================

[System.Serializable]
public class TileData
{
    /// <summary>타일 유형 (WALL / FLOOR / CORRIDOR)</summary>
    public TileType type;

    /// <summary>소속 방 ID. -1이면 복도이거나 벽</summary>
    public int roomId;

    /// <summary>이 타일에 배치된 이벤트. null이면 이벤트 없음</summary>
    public DungeonEventData eventData;

    /// <summary>이벤트 소진 여부</summary>
    public bool isEventConsumed;

    /// <summary>이 타일에 배치된 오브젝트. null이면 오브젝트 없음</summary>
    public DungeonObjectData placedObject;

    /// <summary>오브젝트 상호작용 완료 여부 (isOneTime 오브젝트가 사용됨)</summary>
    public bool isObjectInteracted;

    /// <summary>이동 가능 여부</summary>
    public bool IsWalkable => type == TileType.FLOOR || type == TileType.CORRIDOR;

    /// <summary>상호작용 가능한 오브젝트가 있는지 여부</summary>
    public bool HasObject => placedObject != null &&
                             !(placedObject.isOneTime && isObjectInteracted);

    public TileData()
    {
        type              = TileType.WALL;
        roomId            = -1;
        eventData         = null;
        isEventConsumed   = false;
        placedObject      = null;
        isObjectInteracted = false;
    }
}

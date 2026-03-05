// ============================================================
// Enums.cs — 던전 시스템 공용 열거형 정의
// 기획서 Ch.0 참조
// 위치: Assets/Scripts/Core/Enums.cs
// ============================================================

/// <summary>타일의 기본 유형</summary>
public enum TileType
{
    WALL,       // 벽 (이동 불가)
    FLOOR,      // 방 내부 바닥 타일
    CORRIDOR    // 복도 타일
}

/// <summary>방의 역할 유형</summary>
public enum RoomType
{
    NORMAL,     // 일반 방
    START,      // 시작 방 (입구 계단)
    EXIT        // 출구 방 (내려가는 계단)
}

/// <summary>던전 이벤트 타입 (기획서 0.5)</summary>
public enum DungeonEventType
{
    COMBAT,     // 전투
    TRAP,       // 함정
    TREASURE,   // 보물
    NPC,        // NPC 조우
    SHRINE,     // 제단
    SPECIAL     // 특수
}

/// <summary>
/// 던전 오브젝트 타입.
/// DungeonObjectData ScriptableObject에서 타입을 지정하여
/// 인터렉션 방식 및 표시 텍스트를 결정한다.
/// </summary>
public enum DungeonObjectType
{
    TREASURE_CHEST, // 보물 상자 — 열기
    STAIRS_DOWN,    // 내려가는 계단 — 다음 층으로
    STAIRS_UP,      // 올라가는 계단 — 이전 층으로
    // 추후 확장: NPC_MERCHANT, ALTAR, SIGN, ...
}

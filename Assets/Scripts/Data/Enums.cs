// ============================================================
// Enums.cs — 던전 시스템 공용 열거형 정의
// 기획서 Ch.0 참조
// ============================================================

/// <summary>
/// 타일의 기본 유형
/// </summary>
public enum TileType
{
    WALL,       // 벽 (이동 불가)
    FLOOR,      // 방 내부 바닥 타일
    CORRIDOR    // 복도 타일
}

/// <summary>
/// 방의 역할 유형
/// </summary>
public enum RoomType
{
    NORMAL,     // 일반 방
    START,      // 시작 방 (입구 계단)
    EXIT        // 출구 방 (내려가는 계단)
}

/// <summary>
/// 던전 이벤트 타입 (기획서 0.5)
/// </summary>
public enum DungeonEventType
{
    COMBAT,     // 전투 — 몬스터 전투 이벤트
    TRAP,       // 함정 — 즉발형 피해/디버프
    TREASURE,   // 보물 — 아이템/골드 획득
    NPC,        // NPC 조우 — 대화/거래
    SHRINE,     // 제단 — HP/버프 회복
    SPECIAL     // 특수 — 추후 확장용 // TODO: 추후 확장
}

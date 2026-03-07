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
/// 캐릭터 스탯 타입.
/// CharacterStats에서 스탯을 식별하는 키로 사용된다.
/// </summary>
public enum StatType
{
    MaxHP,        // 최대 체력
    HPGen,        // 턴당 체력 회복
    BaseMana,     // 최대 지구력 (턴 시작 시 획득)
    MaxHand,      // 행동력 (턴당 받는 카드 수)
    BaseShield,   // 기본 방어력 (전투 시작 시 1회)
    DamagePer,    // 피해량 증가 (%)
    DamageConst,  // 피해량 증가 (+ 상수)
    BaseDodge,    // 기본 회피 (전투 시작 시 1회)
}

/// <summary>장비 타입 — 슬롯 식별자</summary>
public enum EquipType
{
    Weapon,    // 무기
    Armor,     // 갑옷
    Gloves,    // 장갑
    Boots,     // 신발
    Ring,      // 반지
    Necklace,  // 목걸이
    Amulet,    // 장신구
    Bag,       // 가방 (MaxItemSlot / MaxCarryWeight 증가)
}


public enum DungeonObjectType
{
    TREASURE_CHEST, // 보물 상자 — 열기
    STAIRS_DOWN,    // 내려가는 계단 — 다음 층으로
    STAIRS_UP,      // 올라가는 계단 — 이전 층으로
    // 추후 확장: NPC_MERCHANT, ALTAR, SIGN, ...
}

// ============================================================
// Enums.cs — 던전 시스템 공용 열거형 정의
// 위치: Assets/Scripts/Core/Enums.cs
// ============================================================
// [변경사항]
//   기존 열거형 유지, 이벤트 시스템용 열거형 추가:
//   - ChoiceType : 선택지 종류
//   - EventPhase : 팝업 단계 (Choice / Result)
// ============================================================

/// <summary>타일의 기본 유형</summary>
public enum TileType { WALL, FLOOR, CORRIDOR }

/// <summary>방의 역할 유형</summary>
public enum RoomType { NORMAL, START, EXIT }

/// <summary>던전 이벤트 타입</summary>
public enum DungeonEventType { COMBAT, TRAP, TREASURE, NPC, SHRINE, SPECIAL }

/// <summary>캐릭터 스탯 타입</summary>
public enum StatType
{
    MaxHP, HPGen, BaseMana, MaxHand,
    BaseShield, DamagePer, DamageConst, BaseDodge,
}

/// <summary>장비 타입 — 슬롯 식별자</summary>
public enum EquipType { Weapon, Helmet, Necklace, Gloves, Armor, Ring, Amulet, Boots, Bag }

public enum DungeonObjectType { TREASURE_CHEST, STAIRS_DOWN, STAIRS_UP }

// ────────────────────────────────────────────────────────
// 이벤트 시스템
// ────────────────────────────────────────────────────────

/// <summary>
/// 선택지 종류.
/// UI 뱃지 색상 및 조건 평가 방식을 결정한다.
/// </summary>
public enum ChoiceType
{
    Default,      // 확률 기반 (성공률 뱃지: 초록/주황/빨강)
    SpecialItem,  // 아이템 보유 조건 (파랑 뱃지)
    SpecialEquip, // 장착 슬롯 조건 (보라 뱃지)
    SpecialStat,  // 능력치 조건 (노랑 뱃지)
    Close,        // 이벤트 효과 없이 팝업 닫기
}

/// <summary>이벤트 팝업의 현재 단계</summary>
public enum EventPhase { Choice, Result }

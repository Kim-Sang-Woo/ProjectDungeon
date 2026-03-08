// ============================================================
// EquipData.cs — 장비 아이템 데이터 (ScriptableObject)
// 위치: Assets/Scripts/Data/EquipData.cs
// ============================================================
// [에셋 생성]
//   Project 창 우클릭 → Create → Dungeon → Equip Data
//   Assets/Data/Equips/ 폴더에 저장 권장
//
// [특징]
//   - 장비는 스택 불가 (isStackable = false 고정)
//   - 모든 StatType에 대해 보정치 설정 가능
//   - 장착 시 EquipmentManager가 CharacterStats에 보정치 적용
// ============================================================
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Equip_New", menuName = "Dungeon/Equip Data")]
public class EquipData : ItemData
{
    [Header("장비 타입")]
    public EquipType equipType;

    [Header("전투 카드 (Deck 기여)")]
    [Tooltip("전투 시작/라운드 드로우 시 이 장비가 덱 풀에 추가하는 카드 목록")]
    public List<BattleCardData> battleCards = new List<BattleCardData>();

    [Header("장비 능력치 보정치")]
    [Tooltip("최대 체력 증가")]
    public float statMaxHP;

    [Tooltip("턴당 체력 회복 증가")]
    public float statHPGen;

    [Tooltip("최대 지구력 증가")]
    public float statBaseMana;

    [Tooltip("행동력 증가")]
    public float statMaxHand;

    [Tooltip("기본 방어력 증가")]
    public float statBaseShield;

    [Tooltip("피해량 % 증가")]
    public float statDamagePer;

    [Tooltip("피해량 상수 증가")]
    public float statDamageConst;

    [Tooltip("기본 회피 증가")]
    public float statBaseDodge;

    [Tooltip("최대 아이템 슬롯 증가 (Bag 타입 권장)")]
    public int statMaxItemSlot;

    [Tooltip("최대 무게 증가 (Bag 타입 권장)")]
    public float statMaxCarryWeight;

    /// <summary>능력치 보정치가 하나라도 있는지 여부 (UI 표시용)</summary>
    public bool HasStats =>
        statMaxHP != 0 || statHPGen != 0 || statBaseMana != 0 ||
        statMaxHand != 0 || statBaseShield != 0 || statDamagePer != 0 ||
        statDamageConst != 0 || statBaseDodge != 0 ||
        statMaxItemSlot != 0 || statMaxCarryWeight != 0;
}

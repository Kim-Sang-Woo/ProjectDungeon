// ============================================================
// EventSession.cs — 이벤트 팝업 1회 실행 런타임 상태
// 위치: Assets/Scripts/Events/EventSession.cs
// ============================================================
// [개요]
//   팝업이 열린 동안만 존재하는 휘발성 상태 오브젝트.
//   UI는 이 구조만 읽는다 — SO 에셋을 직접 참조하지 않는다.
//
//   ResolvedChoice: 조건 평가가 완료된 선택지 뷰모델.
//     - isVisible  : SetActive 기준
//     - badgeText  : 뱃지 표시 문자열 (미리 계산)
//     - badgeType  : 뱃지 색상 결정
//   팝업이 열릴 때 1회 계산되며, 열려 있는 동안 재평가하지 않는다.
// ============================================================
using System.Collections.Generic;
using UnityEngine;

public class EventSession
{
    // ── 에셋 참조 (읽기 전용) ─────────────────────────────
    public EventData sourceData;

    // ── 현재 UI 단계 ──────────────────────────────────────
    public EventPhase phase = EventPhase.Choice;

    // ── ChoiceView 데이터 ─────────────────────────────────
    /// <summary>조건 평가 완료된 선택지 뷰모델 리스트</summary>
    public List<ResolvedChoice> resolvedChoices = new List<ResolvedChoice>();

    // ── ResultView 데이터 ─────────────────────────────────
    /// <summary>현재 표시 중인 결과 SO</summary>
    public EventResult currentResult;

    /// <summary>효과 요약 텍스트 (Rich Text 태그 포함, TMP 직접 표시용)</summary>
    public string effectSummaryText;

    // ─────────────────────────────────────────────────────
    /// <summary>
    /// 이 이벤트 세션을 초기화한다.
    /// EventData.choices를 순회하여 ResolvedChoice 리스트를 구성한다.
    /// </summary>
    public void Initialize(EventData data)
    {
        sourceData      = data;
        phase           = EventPhase.Choice;
        currentResult   = null;
        effectSummaryText = "";
        resolvedChoices.Clear();

        if (data == null || data.choices == null) return;

        for (int i = 0; i < data.choices.Length && i < 5; i++)
        {
            EventChoice choice = data.choices[i];
            if (choice == null) continue;

            ResolvedChoice rc = ResolveChoice(choice, i + 1);
            resolvedChoices.Add(rc);
        }
    }

    /// <summary>
    /// 선택지 1개를 평가하여 ResolvedChoice를 반환한다.
    /// </summary>
    private ResolvedChoice ResolveChoice(EventChoice choice, int keyIndex)
    {
        var rc = new ResolvedChoice
        {
            source    = choice,
            label     = choice.label,
            badgeType = choice.choiceType,
            keyIndex  = keyIndex,
        };

        switch (choice.choiceType)
        {
            case ChoiceType.Default:
                rc.isVisible  = true;
                rc.badgeText  = $"{choice.successRate}%";
                break;

            case ChoiceType.SpecialItem:
                rc.isVisible  = EvaluateItemCondition(choice, out string foundItemName);
                rc.badgeText  = foundItemName;
                break;

            case ChoiceType.SpecialEquip:
                rc.isVisible  = EvaluateEquipCondition(choice, out string slotName);
                rc.badgeText  = slotName;
                break;

            case ChoiceType.SpecialStat:
                rc.isVisible  = EvaluateStatCondition(choice);
                rc.badgeText  = $"{GetStatLabel(choice.requiredStatType)} {choice.requiredStatValue}";
                break;

            case ChoiceType.Close:
                rc.isVisible  = true;
                rc.badgeText  = "";
                break;
        }

        return rc;
    }

    // ── 조건 평가 ─────────────────────────────────────────

    private bool EvaluateItemCondition(EventChoice choice, out string foundName)
    {
        foundName = "";
        if (choice.requiredItems == null || choice.requiredItems.Length == 0) return false;
        if (Inventory.Instance == null) return false;

        foreach (ItemData required in choice.requiredItems)
        {
            if (required == null) continue;
            foreach (var slot in Inventory.Instance.Slots)
            {
                if (slot.item == required)
                {
                    foundName = required.itemName;
                    return true;
                }
            }
        }
        return false;
    }

    private bool EvaluateEquipCondition(EventChoice choice, out string slotName)
    {
        slotName = GetEquipSlotLabel(choice.requiredSlot);
        if (EquipmentManager.Instance == null) return false;
        return EquipmentManager.Instance.GetEquipped(choice.requiredSlot) != null;
    }

    private bool EvaluateStatCondition(EventChoice choice)
    {
        if (CharacterStats.Instance == null) return false;
        float val = GetStatValue(choice.requiredStatType);
        return val >= choice.requiredStatValue;
    }

    // ── 스탯 값 조회 헬퍼 ────────────────────────────────

    private float GetStatValue(StatType type)
    {
        var s = CharacterStats.Instance;
        switch (type)
        {
            case StatType.MaxHP:       return s.maxHP.FinalValue;
            case StatType.HPGen:       return s.hpGen.FinalValue;
            case StatType.BaseMana:    return s.baseMana.FinalValue;
            case StatType.MaxHand:     return s.maxHand.FinalValue;
            case StatType.BaseShield:  return s.baseShield.FinalValue;
            case StatType.DamagePer:   return s.damagePer.FinalValue;
            case StatType.DamageConst: return s.damageConst.FinalValue;
            case StatType.BaseDodge:   return s.baseDodge.FinalValue;
            default:                   return 0;
        }
    }

    private string GetStatLabel(StatType type)
    {
        switch (type)
        {
            case StatType.MaxHP:       return "체력";
            case StatType.HPGen:       return "회복력";
            case StatType.BaseMana:    return "지구력";
            case StatType.MaxHand:     return "행동력";
            case StatType.BaseShield:  return "방어력";
            case StatType.DamagePer:   return "피해%";
            case StatType.DamageConst: return "피해량";
            case StatType.BaseDodge:   return "회피";
            default:                   return type.ToString();
        }
    }

    private string GetEquipSlotLabel(EquipType slot)
    {
        switch (slot)
        {
            case EquipType.Weapon:   return "무기";
            case EquipType.Armor:    return "갑옷";
            case EquipType.Gloves:   return "장갑";
            case EquipType.Boots:    return "신발";
            case EquipType.Ring:     return "반지";
            case EquipType.Necklace: return "목걸이";
            case EquipType.Amulet:   return "장신구";
            case EquipType.Bag:      return "가방";
            default:                 return slot.ToString();
        }
    }
}

// ────────────────────────────────────────────────────────────
// 선택지 뷰모델
// ────────────────────────────────────────────────────────────

/// <summary>
/// 조건 평가가 완료된 선택지 뷰모델.
/// EventSession 생성 시 1회 계산된다.
/// </summary>
public class ResolvedChoice
{
    /// <summary>원본 ScriptableObject</summary>
    public EventChoice source;

    /// <summary>조건 충족 여부 → UI SetActive 기준</summary>
    public bool isVisible;

    /// <summary>
    /// 뱃지 표시 문자열 (미리 계산됨).
    /// Default: "72%" / SpecialItem: "낡은 열쇠" / SpecialEquip: "무기" / SpecialStat: "공격 10"
    /// </summary>
    public string badgeText;

    /// <summary>뱃지 색상 결정용 타입</summary>
    public ChoiceType badgeType;

    /// <summary>선택지 본문 텍스트</summary>
    public string label;

    /// <summary>숫자 키 인덱스 (1~5)</summary>
    public int keyIndex;
}

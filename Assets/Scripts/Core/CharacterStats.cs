// ============================================================
// CharacterStats.cs — 캐릭터 스테이터스 데이터 및 로직
// 위치: Assets/Scripts/Stats/CharacterStats.cs
// ============================================================
// [개요]
//   캐릭터의 모든 스탯을 관리하는 싱글턴 컴포넌트.
//   기본값은 Inspector에서 설정.
//   장비 착용/해제 시 AddModifier/RemoveModifier로 보정치 적용.
//   스탯 변경 시 OnStatsChanged 이벤트 발신 → UI 자동 갱신.
//
// [스탯 구조]
//   BaseValue  : Inspector에서 설정하는 기본값
//   Modifier   : 장비/버프로 추가되는 보정치
//   FinalValue : BaseValue + Modifier (실제 적용값)
//
// [씬 배치]
//   Hierarchy → 빈 GameObject "CharacterStats"
//   CharacterStats.cs 부착
// ============================================================
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>단일 스탯 데이터</summary>
[System.Serializable]
public class StatEntry
{
    [Tooltip("기본값 (레벨업/장비 없는 순수 기본 수치)")]
    public float baseValue;

    // 장비/버프 보정치 합산 (런타임 전용)
    [NonSerialized] public float modifier;

    public float FinalValue => baseValue + modifier;
}

public class CharacterStats : MonoBehaviour
{
    public static CharacterStats Instance { get; private set; }

    [Header("인벤토리 제한")]
    [Tooltip("보유 가능한 최대 아이템 슬롯 수")]
    public int   maxItemSlot = 30;

    [Tooltip("보유 가능한 최대 무게 (kg)")]
    public float maxCarryWeight = 30f;

    [Header("스탯 캡 (어떠한 요인으로도 초과 불가)")]
    [Tooltip("0 이하 = 제한 없음")]
    public float capMaxHP        = 0;
    public float capHPGen        = 0;
    public float capBaseMana     = 0;
    public float capMaxHand      = 0;
    public float capBaseShield   = 0;
    public float capDamagePer    = 0;
    public float capDamageConst  = 0;
    public float capBaseDodge    = 0;
    public int   capMaxItemSlot  = 0;
    public float capMaxCarryWeight = 0;

    [Header("체력")]
    public StatEntry maxHP        = new StatEntry { baseValue = 100 };
    public StatEntry hpGen        = new StatEntry { baseValue = 0   };

    [Header("지구력")]
    public StatEntry baseMana     = new StatEntry { baseValue = 3   };

    [Header("행동력")]
    public StatEntry maxHand      = new StatEntry { baseValue = 5   };

    [Header("방어")]
    public StatEntry baseShield   = new StatEntry { baseValue = 0   };

    [Header("피해량")]
    public StatEntry damagePer    = new StatEntry { baseValue = 0   }; // %
    public StatEntry damageConst  = new StatEntry { baseValue = 0   }; // 상수

    [Header("회피")]
    public StatEntry baseDodge    = new StatEntry { baseValue = 0   };

    // ─── 런타임 전용 현재값 ───
    [NonSerialized] public float currentHP;
    [NonSerialized] public float currentMana;
    [NonSerialized] public float currentShield;
    [NonSerialized] public float currentDodge;

    /// <summary>스탯 변경 시 발신 (UI 갱신용)</summary>
    public event Action OnStatsChanged;

    /// <summary>외부에서 OnStatsChanged 발신용 (EquipmentManager 등)</summary>
    public void NotifyStatsChanged() => OnStatsChanged?.Invoke();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        InitRuntimeValues();
    }

    /// <summary>런타임 현재값 초기화 (게임 시작 / 층 이동 후 호출)</summary>
    public void InitRuntimeValues()
    {
        currentHP     = maxHP.FinalValue;
        currentMana   = baseMana.FinalValue;
        currentShield = 0f;
        currentDodge  = 0f;
        OnStatsChanged?.Invoke();
    }

    // ─── 스탯 조회 ───

    public float Get(StatType type)
    {
        return GetEntry(type).FinalValue;
    }

    // ─── 장비 보정치 ───

    /// <summary>장비 착용 시 보정치 추가</summary>
    public void AddModifier(StatType type, float value)
    {
        var entry = GetEntry(type);
        entry.modifier += value;
        entry.modifier  = ApplyCap(type, entry.modifier, entry.baseValue);
        OnStatsChanged?.Invoke();
    }

    /// <summary>장비 해제 시 보정치 제거</summary>
    public void RemoveModifier(StatType type, float value)
    {
        GetEntry(type).modifier -= value;
        OnStatsChanged?.Invoke();
    }

    /// <summary>스탯 캡 적용 — baseValue + modifier가 캡을 초과하지 않도록 modifier 보정</summary>
    private float ApplyCap(StatType type, float modifier, float baseValue)
    {
        float cap = GetCap(type);
        if (cap <= 0) return modifier; // 캡 없음
        float total = baseValue + modifier;
        if (total > cap) modifier = cap - baseValue;
        return modifier;
    }

    private float GetCap(StatType type)
    {
        switch (type)
        {
            case StatType.MaxHP:       return capMaxHP;
            case StatType.HPGen:       return capHPGen;
            case StatType.BaseMana:    return capBaseMana;
            case StatType.MaxHand:     return capMaxHand;
            case StatType.BaseShield:  return capBaseShield;
            case StatType.DamagePer:   return capDamagePer;
            case StatType.DamageConst: return capDamageConst;
            case StatType.BaseDodge:   return capBaseDodge;
            default: return 0;
        }
    }

    // ─── 현재값 조작 (전투용) ───

    public void TakeDamage(float amount)
    {
        // 회피 우선 적용
        if (currentDodge > 0)
        {
            currentDodge = Mathf.Max(0, currentDodge - 1);
            Debug.Log($"[CharacterStats] 회피! 남은 회피:{currentDodge}");
            OnStatsChanged?.Invoke();
            return;
        }

        // 방어력 우선 흡수
        float remaining = amount;
        if (currentShield > 0)
        {
            float absorbed = Mathf.Min(currentShield, remaining);
            currentShield -= absorbed;
            remaining     -= absorbed;
        }

        currentHP = Mathf.Max(0, currentHP - remaining);
        OnStatsChanged?.Invoke();
        Debug.Log($"[CharacterStats] 피해:{amount} → HP:{currentHP}/{maxHP.FinalValue}");
    }

    public void Heal(float amount)
    {
        currentHP = Mathf.Min(maxHP.FinalValue, currentHP + amount);
        OnStatsChanged?.Invoke();
    }

    public void AddShield(float amount)
    {
        currentShield += amount;
        OnStatsChanged?.Invoke();
    }

    public void AddDodge(float amount)
    {
        currentDodge += amount;
        OnStatsChanged?.Invoke();
    }

    public bool IsDead => currentHP <= 0;

    // ─── 내부 유틸 ───

    private StatEntry GetEntry(StatType type)
    {
        switch (type)
        {
            case StatType.MaxHP:       return maxHP;
            case StatType.HPGen:       return hpGen;
            case StatType.BaseMana:    return baseMana;
            case StatType.MaxHand:     return maxHand;
            case StatType.BaseShield:  return baseShield;
            case StatType.DamagePer:   return damagePer;
            case StatType.DamageConst: return damageConst;
            case StatType.BaseDodge:   return baseDodge;
            default:
                Debug.LogWarning($"[CharacterStats] 정의되지 않은 StatType: {type}");
                return new StatEntry();
        }
    }
}

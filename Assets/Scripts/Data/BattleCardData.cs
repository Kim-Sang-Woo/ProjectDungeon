using System;
using System.Collections.Generic;
using UnityEngine;

public enum BattleCardEffectType
{
    Attack,
    GainShield,
    Heal,
    GainDodge,
    GainMana,
    GainDamagePer,
    DrawCard,
}

public enum BattleCardTargetType
{
    None,
    EnemySingle,
    EnemyAll,
    EnemySingleAdjacent,
    Self,
}

[Serializable]
public class BattleCardEffectEntry
{
    public BattleCardEffectType effectType = BattleCardEffectType.Attack;
    public BattleCardTargetType targetType = BattleCardTargetType.EnemySingle;

    [Tooltip("Attack일 때 배율(최종 피해에 곱함)")]
    [Min(0f)] public float attackMultiplier = 1f;

    [Tooltip("효과 수치. Attack=추가 기본 피해, GainShield=방어 획득, Heal=회복, GainDodge=회피, GainMana=마나, GainDamagePer=피해%")]
    public float amount = 0f;

    [Header("피격 연출")]
    [Tooltip("Attack 효과 적중 시 재생할 피격 스프라이트")]
    public Sprite hitEffectSprite;
}

[CreateAssetMenu(fileName = "BattleCard_New", menuName = "Battle/Battle Card Data")]
public class BattleCardData : ScriptableObject
{
    [Header("식별")]
    public string cardId;
    public string cardName = "공격";

    [Header("표시")]
    public Sprite artwork;
    [TextArea(2, 4)] public string description = "몬스터 1체에게 피해를 줍니다.";

    [Header("코스트")]
    [Min(0)] public int costMana = 1;

    [Header("효과")]
    public bool useEffectLimit = true;
    [Min(1)] public int maxEffects = 4;
    public List<BattleCardEffectEntry> effects = new List<BattleCardEffectEntry>();

    [Header("Legacy (자동 이관용)")]
    public BattleCardEffectType effectType = BattleCardEffectType.Attack;
    public BattleCardTargetType targetType = BattleCardTargetType.EnemySingle;
    [Min(0f)] public float attackMultiplier = 1f;
    public float amount = 0f;

    private void OnValidate()
    {
        if (!useEffectLimit || effects == null) return;

        int limit = Mathf.Max(1, maxEffects);
        if (effects.Count > limit)
            effects.RemoveRange(limit, effects.Count - limit);
    }

    public IReadOnlyList<BattleCardEffectEntry> GetEffects()
    {
        if (effects != null && effects.Count > 0)
            return effects;

        // 레거시 카드 데이터 호환
        if (effects == null) effects = new List<BattleCardEffectEntry>();
        effects.Clear();
        effects.Add(new BattleCardEffectEntry
        {
            effectType = effectType,
            targetType = targetType,
            attackMultiplier = attackMultiplier,
            amount = amount,
            hitEffectSprite = null,
        });
        return effects;
    }
}

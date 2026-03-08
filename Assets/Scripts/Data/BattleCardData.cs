using UnityEngine;

public enum BattleCardEffectType
{
    Attack,
    GainShield,
    Heal,
    GainDodge,
    GainMana,
}

public enum BattleCardTargetType
{
    None,
    EnemySingle,
    EnemyAll,
    EnemySingleAdjacent,
    Self,
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
    public BattleCardEffectType effectType = BattleCardEffectType.Attack;
    public BattleCardTargetType targetType = BattleCardTargetType.EnemySingle;

    [Tooltip("Attack일 때 배율(최종 피해에 곱함)")]
    [Min(0f)] public float attackMultiplier = 1f;

    [Tooltip("효과 수치. Attack=추가 기본 피해, GainShield=방어 획득, Heal=회복, GainDodge=회피, GainMana=마나")]
    public float amount = 0f;
}

using UnityEngine;

public enum ConsumableEffectType
{
    HealHP,
    EscapeToTown
}

[CreateAssetMenu(fileName = "Consumable_New", menuName = "Dungeon/Consumable Item Data")]
public class ConsumableItemData : ItemData
{
    [Header("소모 아이템 효과")]
    public ConsumableEffectType effectType;

    [Min(0f)]
    public float effectValue = 0f;
}

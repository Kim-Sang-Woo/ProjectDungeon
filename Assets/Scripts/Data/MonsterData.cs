using UnityEngine;

[CreateAssetMenu(fileName = "Monster_New", menuName = "Battle/Monster Data")]
public class MonsterData : ScriptableObject
{
    [Header("식별")]
    public string monsterId;
    public string monsterName;

    [Header("표시")]
    public Sprite image;

    [Header("전투 수치")]
    [Min(1)] public int maxHP = 10;
    [Min(0f)] public float damageConst = 3f;
    public float damagePer = 0f;

    [Header("전투 승리 시 추가 보상")]
    public RewardData rewardData;
}

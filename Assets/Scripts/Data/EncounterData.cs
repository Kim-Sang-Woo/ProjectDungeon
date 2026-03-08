using System;
using UnityEngine;

[Serializable]
public class EncounterMonsterEntry
{
    public MonsterData monster;
    [Min(0)] public int rate = 100;
}

[Serializable]
public class MonsterGroupData
{
    [Tooltip("그룹 내 몬스터가 등장하지 않을 확률 가중치")]
    [Min(0)] public int emptyRate = 0;

    public EncounterMonsterEntry[] monsters;
}

[CreateAssetMenu(fileName = "Encounter_New", menuName = "Battle/Encounter Data")]
public class EncounterData : ScriptableObject
{
    [Header("식별")]
    public string encounterId;
    public string displayName;

    [Header("전투 승리 보상 (선택)")]
    public RewardData victoryReward;

    [Header("몬스터 그룹")]
    public MonsterGroupData[] groups;
}

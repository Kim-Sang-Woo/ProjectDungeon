// ============================================================
// RewardGroupData.cs — GroupID ScriptableObject
// 위치: Assets/Scripts/Reward/RewardGroupData.cs
// ============================================================
// [개요]
//   하나의 그룹은 자신에게 속한 아이템 중 1개를 확률 계산으로 선정한다.
//   확률 계산: emptyRate(꽝) + 각 entry.rate 합산 → 랜덤으로 1개 선택.
//
// [Inspector 예시]
//   Group_WeaponDrop  emptyRate=20
//     entries[0]: 낡은 검  value=1  rate=60
//     entries[1]: 철 단검  value=1  rate=30
//     entries[2]: 마법 검  value=1  rate=10
//   → 총 120 중 20은 꽝, 60:30:10 비율로 1개 선정
// ============================================================
using UnityEngine;

[CreateAssetMenu(fileName = "Group_New", menuName = "Reward/RewardGroup")]
public class RewardGroupData : ScriptableObject
{
    [Header("식별")]
    public string groupId;

    [Header("꽝 확률 (가중치)")]
    [Tooltip("아이템을 드롭하지 않을 가중치. 0이면 반드시 아이템 드롭.")]
    public int emptyRate = 0;

    [Header("아이템 목록 (확률 가중치)")]
    public RewardItemEntry[] entries;

    // ─────────────────────────────────────────────────────
    /// <summary>
    /// 확률 계산으로 아이템 1개를 선정해 반환한다.
    /// 꽝이거나 entries가 비어 있으면 null 반환.
    /// </summary>
    public RewardItemEntry Roll()
    {
        if (entries == null || entries.Length == 0) return null;

        // emptyRate + 유효 entry.rate 합산
        int total = emptyRate;
        foreach (var e in entries)
            if (e != null && e.item != null) total += e.rate;

        if (total <= 0) return null;

        int roll = Random.Range(0, total);

        // 꽝 구간 (0 ~ emptyRate-1)
        if (roll < emptyRate) return null;

        int acc = emptyRate;
        foreach (var e in entries)
        {
            if (e == null || e.item == null) continue;
            acc += e.rate;
            if (roll < acc) return e;
        }

        return null;
    }
}

// ============================================================
// RewardItemEntry.cs — GroupID 내 아이템 1개 엔트리
// 위치: Assets/Scripts/Reward/RewardItemEntry.cs
// ============================================================
// [개요]
//   RewardGroupData 안에 [Serializable]로 인라인 배열로 사용.
//   ItemID(ItemData SO), Value(수량), Rate(확률 가중치).
//   Rate는 절대값이 아닌 가중치 — GroupID 내 합산 후 백분율 계산.
// ============================================================
using System;
using UnityEngine;

[Serializable]
public class RewardItemEntry
{
    [Tooltip("지급할 아이템 SO")]
    public ItemData item;

    [Tooltip("지급 수량")]
    [Min(1)]
    public int value = 1;

    [Tooltip("확률 가중치 (그룹 내 합산 후 백분율 계산)")]
    [Min(1)]
    public int rate = 100;
}

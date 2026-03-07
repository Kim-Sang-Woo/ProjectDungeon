// ============================================================
// RewardData.cs — RewardID ScriptableObject
// 위치: Assets/Scripts/Reward/RewardData.cs
// ============================================================
// [개요]
//   하나의 드롭 이벤트를 정의한다.
//   groups 배열의 각 GroupData가 독립적으로 Roll()을 실행해
//   아이템을 1개씩 확정 → 최대 16개(4×4 그리드)까지 지급 가능.
//
// [Inspector 예시]
//   Reward_TreasureChest
//     groups[0]: Group_WeaponDrop
//     groups[1]: Group_PotionDrop
//     groups[2]: Group_GoldDrop
//   → 3개 그룹 → 아이템 최대 3개 지급
// ============================================================
using UnityEngine;

[CreateAssetMenu(fileName = "Reward_New", menuName = "Reward/RewardData")]
public class RewardData : ScriptableObject
{
    [Header("식별")]
    public string rewardId;

    [Tooltip("표시 이름 (Reward UI 헤더에 표시, 비면 'REWARD' 고정)")]
    public string displayName;

    [Header("그룹 목록 (최대 16개 — 4×4 그리드)")]
    [Tooltip("각 그룹에서 아이템 1개가 확률 선정됨. 순서대로 슬롯에 배치.")]
    public RewardGroupData[] groups;
}

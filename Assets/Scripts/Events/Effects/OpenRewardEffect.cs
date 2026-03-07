// ============================================================
// OpenRewardEffect.cs — EventEffect 구현: 보상 UI 열기
// 위치: Assets/Scripts/Events/EventEffect/OpenRewardEffect.cs
// ============================================================
// [개요]
//   EventResult.effects 배열에 추가하면
//   Execute() 호출 시 RewardManager.Open(rewardData)를 실행한다.
//
// [Inspector 설정]
//   EventResult SO → effects → OpenRewardEffect
//     rewardData: Reward_TreasureChest (RewardData SO 연결)
//
// [실행 순서]
//   EventResult.effects를 순서대로 실행하므로
//   GainItemEffect 등 다른 효과 이후에 배치하면
//   아이템 획득 → Reward UI 열기 순으로 진행된다.
// ============================================================
using System;
using UnityEngine;

[Serializable]
public class OpenRewardEffect : EventEffect
{
    [Tooltip("열 보상 데이터 SO. RewardManager.Open()에 전달된다.")]
    public RewardData rewardData;

    public override void Execute()
    {
        if (rewardData == null)
        {
            Debug.LogWarning("[OpenRewardEffect] rewardData가 null입니다. Inspector에서 RewardData SO를 연결해 주세요.");
            return;
        }

        if (RewardManager.Instance == null)
        {
            Debug.LogError("[OpenRewardEffect] RewardManager.Instance가 null입니다. 씬에 RewardPopupPanel이 있는지 확인하세요.");
            return;
        }

        RewardManager.Instance.Open(rewardData);
    }

    public override string GetEffectText() => ""; // ResultView 효과 텍스트에 표시 안 함
}

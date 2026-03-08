// ============================================================
// RewardManager.cs — 리워드 시스템 싱글톤
// 위치: Assets/Scripts/Reward/RewardManager.cs
// ============================================================
// [개요]
//   RewardData SO를 받아 각 GroupID에서 확률 계산 후
//   RewardPopupUI에 결과 아이템 목록을 전달한다.
//
// [씬 배치]
//   RewardPopupPanel 오브젝트에 RewardManager + RewardPopupUI 함께 배치
//
// [호출]
//   RewardManager.Instance.Open(rewardData)   ← RewardData SO 직접
//   RewardManager.Instance.Open(rewardId)     ← ID 문자열로 조회 (RewardTable 필요 시 확장)
// ============================================================
using System.Collections.Generic;
using UnityEngine;

public class RewardManager : MonoBehaviour
{
    public static RewardManager Instance { get; private set; }

    [Header("참조")]
    public RewardPopupUI popupUI;

    // ─────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ─────────────────────────────────────────────────────
    /// <summary>
    /// RewardData SO를 받아 확률 계산 후 Reward UI를 연다.
    /// EventEffect(OpenRewardEffect) 또는 코드에서 직접 호출.
    /// </summary>
    public void Open(RewardData data)
    {
        if (data == null)
        {
            Debug.LogWarning("[RewardManager] RewardData가 null입니다.");
            return;
        }
        if (popupUI == null)
        {
            Debug.LogError("[RewardManager] popupUI가 연결되지 않았습니다.");
            return;
        }

        var results = RollEntries(data);
        Debug.Log($"[RewardManager] Open: {data.rewardId} — {results.Count}개 아이템 지급");
        popupUI.Show(data.displayName, results);
    }

    /// <summary>여러 RewardData를 합산하여 단일 팝업으로 연다.</summary>
    public void OpenCombined(string displayName, IEnumerable<RewardData> rewards)
    {
        if (popupUI == null)
        {
            Debug.LogError("[RewardManager] popupUI가 연결되지 않았습니다.");
            return;
        }

        List<RewardItemEntry> merged = new List<RewardItemEntry>();
        if (rewards != null)
        {
            foreach (var r in rewards)
            {
                if (r == null) continue;
                var entries = RollEntries(r);
                foreach (var e in entries)
                {
                    merged.Add(e);
                    if (merged.Count >= 16) break;
                }
                if (merged.Count >= 16) break;
            }
        }

        popupUI.Show(displayName, merged);
    }

    private List<RewardItemEntry> RollEntries(RewardData data)
    {
        var results = new List<RewardItemEntry>();
        if (data == null) return results;

        if (data.groups != null)
        {
            foreach (var group in data.groups)
            {
                if (group == null) continue;
                RewardItemEntry entry = group.Roll();
                if (entry != null) results.Add(entry);
                if (results.Count >= 16) break;
            }
        }

        return results;
    }

    /// <summary>RewardPopupPanel을 닫는다. EventPopup 닫힐 때 연동 호출.</summary>
    public void ClosePopup()
    {
        popupUI?.Close();
    }
}

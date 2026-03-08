using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class BattleSfxEntry
{
    public string cue;
    public AudioClip clip;
    [Range(0f, 1f)] public float volume = 1f;
}

/// <summary>
/// BattleManager.OnSfxCue(string) 이벤트를 AudioSource 재생으로 라우팅한다.
/// 예시 cue: round_start, enemy_turn, attack, enemy_hit, victory, defeat
/// </summary>
public class BattleSfxRouter : MonoBehaviour
{
    [Header("참조")]
    public BattleManager battleManager;
    public AudioSource audioSource;

    [Header("SFX 매핑")]
    public BattleSfxEntry[] entries;

    private Dictionary<string, BattleSfxEntry> map = new Dictionary<string, BattleSfxEntry>();

    private void Awake()
    {
        if (battleManager == null) battleManager = BattleManager.Instance;
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        RebuildMap();
    }

    private void OnEnable()
    {
        if (battleManager == null) battleManager = BattleManager.Instance;
        if (battleManager != null)
            battleManager.OnSfxCue += HandleCue;
    }

    private void OnDisable()
    {
        if (battleManager != null)
            battleManager.OnSfxCue -= HandleCue;
    }

    public void RebuildMap()
    {
        map.Clear();
        if (entries == null) return;

        foreach (var e in entries)
        {
            if (e == null || string.IsNullOrWhiteSpace(e.cue)) continue;
            map[e.cue.Trim()] = e;
        }
    }

    private void HandleCue(string cue)
    {
        if (string.IsNullOrWhiteSpace(cue)) return;
        if (audioSource == null) return;

        if (!map.TryGetValue(cue, out BattleSfxEntry e)) return;
        if (e == null || e.clip == null) return;

        audioSource.PlayOneShot(e.clip, Mathf.Clamp01(e.volume));
    }
}

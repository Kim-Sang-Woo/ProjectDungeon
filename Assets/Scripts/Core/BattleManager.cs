using System;
using System.Collections.Generic;
using UnityEngine;

public enum BattleState
{
    None,
    BattleStart,
    RoundStart,
    PlayerTurn,
    EnemyTurn,
    Victory,
    Defeat,
    BattleEnd,
}

[Serializable]
public class RuntimeMonster
{
    public MonsterData data;
    public int currentHP;

    public RuntimeMonster(MonsterData data)
    {
        this.data = data;
        currentHP = data != null ? data.maxHP : 0;
    }

    public bool IsDead => currentHP <= 0;
}

[Serializable]
public class RuntimeBattleCard
{
    public BattleCardData data;

    public RuntimeBattleCard(BattleCardData data)
    {
        this.data = data;
    }
}

public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance { get; private set; }

    [Header("연동")]
    public CharacterStats characterStats;
    public MovementSystem movementSystem;

    [Header("디버그")]
    public bool debugLog = true;

    [Header("초기 전투 설정")]
    [Min(0)] public int defaultAttackCostMana = 1;

    [Header("전투 카드 설정")]
    public BattleCardData fallbackAttackCard;
    public List<BattleCardData> debugBattleDeck = new List<BattleCardData>();

    [Header("종료 연출")]
    [Min(0f)] public float endStateDuration = 1.2f;

    public BattleState State { get; private set; } = BattleState.None;
    public EncounterData CurrentEncounter { get; private set; }
    public List<RuntimeMonster> Monsters { get; private set; } = new List<RuntimeMonster>();

    public int RoundIndex { get; private set; } = 0;
    public int CurrentMana { get; private set; } = 0;
    /// <summary>라운드 기준 Hand 수치(최대 지급 목표치)</summary>
    public int CurrentHandCount { get; private set; } = 0;
    /// <summary>현재 실제 손패 카드 수</summary>
    public int CurrentHandCardCount => CurrentHandCards.Count;
    public int PredictedEnemyDamage { get; private set; } = 0; // 다음 적 턴 예상 합산 피해

    public List<RuntimeBattleCard> CurrentHandCards { get; private set; } = new List<RuntimeBattleCard>();

    public event Action<BattleState> OnBattleStateChanged;
    public event Action OnBattleStarted;
    public event Action OnBattleEnded;
    public event Action<bool> OnBattleFinished; // true=victory, false=defeat
    public event Action OnBattleValuesChanged;
    public event Action<string> OnSfxCue; // "round_start", "enemy_turn", "victory", "defeat", "attack", "enemy_hit"

    private const int MaxRoundHandCards = 10;

    private System.Random rng = new System.Random();
    private bool battleEndRequested = false;
    private float tempBattleDamagePerBonus = 0f;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void StartBattle(EncounterData encounter)
    {
        if (encounter == null)
        {
            Debug.LogWarning("[BattleManager] encounter is null");
            return;
        }

        if (characterStats == null) characterStats = CharacterStats.Instance;
        if (characterStats == null)
        {
            Debug.LogWarning("[BattleManager] CharacterStats를 찾을 수 없습니다.");
            return;
        }

        if (movementSystem == null) movementSystem = FindFirstObjectByType<MovementSystem>();
        movementSystem?.LockInput();

        if (InventoryUI.Instance != null)
            InventoryUI.Instance.Hide();

        CurrentEncounter = encounter;
        BuildMonstersFromEncounter(encounter);

        // 전투 시작 시 기본 방어/회피를 현재값에 반영 (장비 보정 포함 FinalValue 사용)
        characterStats.currentShield = Mathf.Max(0f, characterStats.baseShield.FinalValue);
        characterStats.currentDodge  = Mathf.Max(0f, characterStats.baseDodge.FinalValue);
        characterStats.NotifyStatsChanged();

        battleEndRequested = false;
        RemoveTempBattleDamagePerBonus();
        RoundIndex = 0;
        SetState(BattleState.BattleStart);
        OnBattleStarted?.Invoke();

        if (Monsters.Count == 0)
        {
            Debug.LogWarning("[BattleManager] 생성된 몬스터가 없습니다. 즉시 승리 처리합니다.");
            EndBattle(true);
            return;
        }

        RecalculateEnemyIntent();
        BeginRound();

        if (debugLog)
            Debug.Log($"[BattleManager] StartBattle: {encounter.displayName} / monsters={Monsters.Count}");
    }

    public void EndBattle(bool isVictory)
    {
        if (battleEndRequested) return;
        battleEndRequested = true;

        StopAllCoroutines();
        StartCoroutine(CoEndBattle(isVictory));
    }

    private System.Collections.IEnumerator CoEndBattle(bool isVictory)
    {
        SetState(isVictory ? BattleState.Victory : BattleState.Defeat);

        if (isVictory)
            OpenVictoryRewards();

        if (!isVictory)
        {
            FloatingTextUI.Instance?.Show("전투 패배", FloatingTextUI.ColorFail);
            OnSfxCue?.Invoke("defeat");
        }
        else
        {
            FloatingTextUI.Instance?.Show("전투 승리", FloatingTextUI.ColorAcquire);
            OnSfxCue?.Invoke("victory");
        }

        if (endStateDuration > 0f)
            yield return new WaitForSeconds(endStateDuration);

        PredictedEnemyDamage = 0;
        OnBattleValuesChanged?.Invoke();

        SetState(BattleState.BattleEnd);

        RemoveTempBattleDamagePerBonus();
        movementSystem?.UnlockAllInputLocks();

        if (debugLog)
            Debug.Log($"[BattleManager] EndBattle: {(isVictory ? "Victory" : "Defeat")}");

        OnBattleEnded?.Invoke();
        OnBattleFinished?.Invoke(isVictory);
    }

    /// <summary>하위 호환: 첫 번째 카드 사용 시도로 연결</summary>
    public bool TryUseDefaultAttack(int targetIndex)
    {
        return TryUseCard(0, targetIndex);
    }

    public bool TryUseCard(int cardOrder, int targetIndex = -1)
    {
        if (State != BattleState.PlayerTurn) return false;
        if (cardOrder < 0 || cardOrder >= CurrentHandCards.Count) return false;

        RuntimeBattleCard runtimeCard = CurrentHandCards[cardOrder];
        BattleCardData card = runtimeCard?.data;
        if (card == null) return false;

        if (CurrentMana < card.costMana)
            return false;

        var effects = card.GetEffects();
        if (effects == null || effects.Count == 0) return false;

        bool consumed = false;

        for (int ei = 0; ei < effects.Count; ei++)
        {
            BattleCardEffectEntry e = effects[ei];
            if (e == null) continue;

            switch (e.effectType)
            {
                case BattleCardEffectType.Attack:
                {
                    int damage = CalcCardAttackDamage(e);
                    bool hitAny = false;

                    if (e.targetType == BattleCardTargetType.EnemyAll)
                    {
                        foreach (var m in Monsters)
                        {
                            if (m == null || m.IsDead) continue;
                            ApplyDamageToMonster(m, damage);
                            hitAny = true;
                        }
                    }
                    else if (e.targetType == BattleCardTargetType.EnemySingleAdjacent)
                    {
                        if (targetIndex < 0 || targetIndex >= Monsters.Count) return false;
                        for (int i = targetIndex - 1; i <= targetIndex + 1; i++)
                        {
                            if (i < 0 || i >= Monsters.Count) continue;
                            RuntimeMonster m = Monsters[i];
                            if (m == null || m.IsDead) continue;
                            ApplyDamageToMonster(m, damage);
                            hitAny = true;
                        }
                    }
                    else // EnemySingle 기본
                    {
                        if (targetIndex < 0 || targetIndex >= Monsters.Count) return false;
                        RuntimeMonster target = Monsters[targetIndex];

                        // 앞선 다중 효과 타격으로 대상/전원이 이미 죽었을 수 있음
                        if (target == null || target.IsDead)
                        {
                            if (AllMonstersDead())
                            {
                                consumed = true;
                                break;
                            }
                            return false;
                        }

                        ApplyDamageToMonster(target, damage);
                        hitAny = true;

                        if (debugLog)
                            Debug.Log($"[BattleManager] [PlayerTurn] 카드사용({card.cardName})[{ei}] -> {target.data?.monsterName} / dmg={damage} / mana={CurrentMana - card.costMana}");
                    }

                    if (!hitAny)
                    {
                        // 다중 효과 카드에서 앞선 타격으로 전원이 죽었다면
                        // 후속 타격 효과는 no-op로 처리하고 정상 종료 흐름을 유지한다.
                        if (AllMonstersDead())
                        {
                            consumed = true;
                            break;
                        }
                        return false;
                    }

                    OnSfxCue?.Invoke("attack");
                    OnSfxCue?.Invoke("enemy_hit");
                    consumed = true;
                    break;
                }
                case BattleCardEffectType.GainShield:
                {
                    if (characterStats == null) return false;
                    characterStats.AddShield(e.amount);
                    consumed = true;
                    break;
                }
                case BattleCardEffectType.Heal:
                {
                    if (characterStats == null) return false;
                    characterStats.Heal(e.amount);
                    consumed = true;
                    break;
                }
                case BattleCardEffectType.GainDodge:
                {
                    if (characterStats == null) return false;
                    characterStats.AddDodge(e.amount);
                    consumed = true;
                    break;
                }
                case BattleCardEffectType.GainMana:
                {
                    CurrentMana += Mathf.FloorToInt(e.amount);
                    consumed = true;
                    break;
                }
                case BattleCardEffectType.GainDamagePer:
                {
                    AddTempBattleDamagePerBonus(e.amount);
                    consumed = true;
                    break;
                }
                case BattleCardEffectType.DrawCard:
                {
                    int draw = Mathf.Max(0, Mathf.FloorToInt(e.amount));
                    DrawCardsToHand(draw, allowOverMaxHand: true);
                    consumed = true;
                    break;
                }
            }
        }

        if (!consumed) return false;

        CurrentMana -= card.costMana;
        CurrentMana = Mathf.Max(0, CurrentMana);
        CurrentHandCards.RemoveAt(cardOrder);
        OnBattleValuesChanged?.Invoke();

        RecalculateEnemyIntent();

        if (AllMonstersDead())
        {
            EndBattle(true);
            return true;
        }

        if (ShouldAutoEndPlayerTurn())
            GoToEnemyTurn();

        return true;
    }

    public void EndPlayerTurnByButton()
    {
        if (State != BattleState.PlayerTurn) return;
        GoToEnemyTurn();
    }

    public void GoToEnemyTurn()
    {
        if (State != BattleState.PlayerTurn) return;
        SetState(BattleState.EnemyTurn);
        FloatingTextUI.Instance?.Show("적 턴", FloatingTextUI.ColorWarning);
        OnSfxCue?.Invoke("enemy_turn");

        ExecuteEnemyTurn();

        if (characterStats != null && characterStats.IsDead)
        {
            EndBattle(false);
            return;
        }

        BeginRound();
    }

    public void GoToNextRound()
    {
        // 하위 호환: 외부 호출이 남아 있어도 라운드 시작으로 연결
        BeginRound();
    }

    private void BeginRound()
    {
        if (State == BattleState.BattleEnd || State == BattleState.Victory || State == BattleState.Defeat)
            return;

        RoundIndex++;
        SetState(BattleState.RoundStart);

        // 라운드 시작 효과
        if (characterStats != null)
        {
            if (characterStats.hpGen.FinalValue > 0)
                characterStats.Heal(characterStats.hpGen.FinalValue);

            CurrentMana = Mathf.FloorToInt(characterStats.baseMana.FinalValue);
            CurrentHandCount = Mathf.Clamp(Mathf.FloorToInt(characterStats.maxHand.FinalValue), 0, MaxRoundHandCards);
            BuildRoundHand(CurrentHandCount);
        }
        else
        {
            CurrentMana = 0;
            CurrentHandCount = 0;
            CurrentHandCards.Clear();
        }

        RecalculateEnemyIntent();
        OnBattleValuesChanged?.Invoke();
        SetState(BattleState.PlayerTurn);
        FloatingTextUI.Instance?.Show($"라운드 {RoundIndex}", FloatingTextUI.ColorAcquire);
        OnSfxCue?.Invoke("round_start");

        if (debugLog)
            Debug.Log($"[BattleManager] [RoundStart] R{RoundIndex} / mana={CurrentMana} hand={CurrentHandCount} / predictedEnemy={PredictedEnemyDamage}");
    }

    private int CalcCardAttackDamage(BattleCardEffectEntry effect)
    {
        float dmgConst = characterStats != null ? characterStats.damageConst.FinalValue : 0f;
        float dmgPer = characterStats != null ? characterStats.damagePer.FinalValue : 0f;
        int baseDamage = BattleMath.CalcFinalDamage(dmgConst, dmgPer);

        float scaled = baseDamage * Mathf.Max(0f, effect != null ? effect.attackMultiplier : 1f);
        float plus = effect != null ? effect.amount : 0f;
        return Mathf.Max(0, Mathf.FloorToInt(scaled + plus));
    }

    private void AddTempBattleDamagePerBonus(float amount)
    {
        if (characterStats == null || amount == 0f) return;
        characterStats.AddModifier(StatType.DamagePer, amount);
        tempBattleDamagePerBonus += amount;
    }

    private void RemoveTempBattleDamagePerBonus()
    {
        if (characterStats == null || tempBattleDamagePerBonus == 0f) return;
        characterStats.RemoveModifier(StatType.DamagePer, tempBattleDamagePerBonus);
        tempBattleDamagePerBonus = 0f;
    }

    private void ApplyDamageToMonster(RuntimeMonster monster, int damage)
    {
        if (monster == null || monster.IsDead) return;
        monster.currentHP = Mathf.Max(0, monster.currentHP - Mathf.Max(0, damage));
    }

    private void ExecuteEnemyTurn()
    {
        if (characterStats == null) return;

        foreach (var m in Monsters)
        {
            if (m == null || m.IsDead || m.data == null) continue;

            int damage = BattleMath.CalcFinalDamage(m.data.damageConst, m.data.damagePer);
            characterStats.TakeDamage(damage);

            if (debugLog)
                Debug.Log($"[BattleManager] [EnemyTurn] {m.data.monsterName} -> player / dmg={damage}");

            if (characterStats.IsDead)
                break;
        }

        OnBattleValuesChanged?.Invoke();
    }

    private void BuildRoundHand(int maxHand)
    {
        CurrentHandCards.Clear();

        int drawCount = Mathf.Max(0, maxHand);
        DrawCardsToHand(drawCount, allowOverMaxHand: false);
    }

    private void DrawCardsToHand(int drawCount, bool allowOverMaxHand)
    {
        if (drawCount <= 0) return;

        List<BattleCardData> sourceDeck = BuildDeckSourcePool();
        if (sourceDeck.Count == 0) return;

        int maxDraw = drawCount;

        int handLimit = Mathf.Clamp(CurrentHandCount, 0, MaxRoundHandCards);
        int room = Mathf.Max(0, handLimit - CurrentHandCards.Count);
        maxDraw = Mathf.Min(maxDraw, room);

        if (!allowOverMaxHand)
        {
            maxDraw = Mathf.Min(maxDraw, sourceDeck.Count);
        }

        for (int i = 0; i < maxDraw; i++)
        {
            int pick = rng.Next(0, sourceDeck.Count);
            BattleCardData cd = sourceDeck[pick];

            if (!allowOverMaxHand)
                sourceDeck.RemoveAt(pick); // 라운드 기본 드로우는 중복 비허용

            CurrentHandCards.Add(new RuntimeBattleCard(cd));
        }
    }

    private List<BattleCardData> BuildDeckSourcePool()
    {
        // 1순위: 장착 장비 기반 덱 풀
        List<BattleCardData> sourceDeck = BuildDeckPoolFromEquips();

        // 2순위(하위호환): debugBattleDeck
        if (sourceDeck.Count == 0 && debugBattleDeck != null && debugBattleDeck.Count > 0)
        {
            for (int i = 0; i < debugBattleDeck.Count; i++)
                if (debugBattleDeck[i] != null)
                    sourceDeck.Add(debugBattleDeck[i]);
        }

        // 3순위: fallback 1장
        if (sourceDeck.Count == 0 && fallbackAttackCard != null)
            sourceDeck.Add(fallbackAttackCard);

        return sourceDeck;
    }

    private List<BattleCardData> BuildDeckPoolFromEquips()
    {
        List<BattleCardData> pool = new List<BattleCardData>();
        EquipmentManager em = EquipmentManager.Instance;
        if (em == null) return pool;

        foreach (var kv in em.GetAllEquipped())
        {
            EquipData eq = kv.Value;
            if (eq == null || eq.battleCards == null) continue;

            for (int i = 0; i < eq.battleCards.Count; i++)
            {
                BattleCardData card = eq.battleCards[i];
                if (card != null) pool.Add(card);
            }
        }

        return pool;
    }

    private bool ShouldAutoEndPlayerTurn()
    {
        if (CurrentMana <= 0)
        {
            if (debugLog) Debug.Log("[BattleManager] [PlayerTurn] 자동 턴 종료: 마나 0");
            return true;
        }

        if (CurrentHandCardCount <= 0)
        {
            if (debugLog) Debug.Log("[BattleManager] [PlayerTurn] 자동 턴 종료: 손패 0");
            return true;
        }

        bool hasUsable = false;
        for (int i = 0; i < CurrentHandCards.Count; i++)
        {
            BattleCardData card = CurrentHandCards[i]?.data;
            if (card == null) continue;
            if (card.costMana <= CurrentMana)
            {
                hasUsable = true;
                break;
            }
        }

        if (!hasUsable)
        {
            if (debugLog) Debug.Log("[BattleManager] [PlayerTurn] 자동 턴 종료: 사용 가능한 카드 없음(코스트 부족)");
            return true;
        }

        return false;
    }

    private bool AllMonstersDead()
    {
        if (Monsters == null || Monsters.Count == 0) return false;
        foreach (var m in Monsters)
            if (m != null && !m.IsDead) return false;
        return true;
    }

    private void RecalculateEnemyIntent()
    {
        int sum = 0;
        if (Monsters != null)
        {
            foreach (var m in Monsters)
            {
                if (m == null || m.IsDead || m.data == null) continue;
                sum += BattleMath.CalcFinalDamage(m.data.damageConst, m.data.damagePer);
            }
        }

        PredictedEnemyDamage = Mathf.Max(0, sum);
    }

    private void OpenVictoryRewards()
    {
        RewardManager rm = RewardManager.Instance;
        if (rm == null) return;

        List<RewardData> rewards = new List<RewardData>();
        if (CurrentEncounter != null && CurrentEncounter.victoryReward != null)
            rewards.Add(CurrentEncounter.victoryReward);

        foreach (var m in Monsters)
        {
            if (m?.data?.rewardData != null)
                rewards.Add(m.data.rewardData);
        }

        if (rewards.Count == 0) return;

        string display = CurrentEncounter != null && !string.IsNullOrEmpty(CurrentEncounter.displayName)
            ? $"{CurrentEncounter.displayName} 전리품"
            : "BATTLE REWARD";

        rm.OpenCombined(display, rewards);
    }

    private void BuildMonstersFromEncounter(EncounterData encounter)
    {
        Monsters.Clear();
        if (encounter.groups == null) return;

        foreach (var group in encounter.groups)
        {
            MonsterData picked = PickFromGroup(group);
            if (picked != null)
                Monsters.Add(new RuntimeMonster(picked));
        }
    }

    private MonsterData PickFromGroup(MonsterGroupData group)
    {
        if (group == null) return null;

        int total = Mathf.Max(0, group.emptyRate);
        if (group.monsters != null)
        {
            foreach (var m in group.monsters)
                total += Mathf.Max(0, m != null ? m.rate : 0);
        }

        if (total <= 0) return null;

        int roll = rng.Next(0, total);
        if (roll < group.emptyRate) return null;

        int acc = group.emptyRate;
        if (group.monsters != null)
        {
            foreach (var m in group.monsters)
            {
                if (m == null || m.monster == null) continue;
                int r = Mathf.Max(0, m.rate);
                acc += r;
                if (roll < acc)
                    return m.monster;
            }
        }

        return null;
    }

    private void SetState(BattleState next)
    {
        State = next;
        OnBattleStateChanged?.Invoke(next);
    }
}

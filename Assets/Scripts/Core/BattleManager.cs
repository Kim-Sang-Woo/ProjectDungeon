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

    [Header("종료 연출")]
    [Min(0f)] public float endStateDuration = 1.2f;

    public BattleState State { get; private set; } = BattleState.None;
    public EncounterData CurrentEncounter { get; private set; }
    public List<RuntimeMonster> Monsters { get; private set; } = new List<RuntimeMonster>();

    public int RoundIndex { get; private set; } = 0;
    public int CurrentMana { get; private set; } = 0;
    public int CurrentHandCount { get; private set; } = 0; // 초기 개발: 디폴트 공격 카드 수량으로 사용
    public int PredictedEnemyDamage { get; private set; } = 0; // 다음 적 턴 예상 합산 피해

    public event Action<BattleState> OnBattleStateChanged;
    public event Action OnBattleStarted;
    public event Action OnBattleEnded;
    public event Action OnBattleValuesChanged;

    private System.Random rng = new System.Random();

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

        CurrentEncounter = encounter;
        BuildMonstersFromEncounter(encounter);

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
        StopAllCoroutines();
        StartCoroutine(CoEndBattle(isVictory));
    }

    private System.Collections.IEnumerator CoEndBattle(bool isVictory)
    {
        SetState(isVictory ? BattleState.Victory : BattleState.Defeat);

        if (isVictory)
            OpenVictoryRewards();

        if (!isVictory)
            FloatingTextUI.Instance?.Show("전투 패배", FloatingTextUI.ColorFail);
        else
            FloatingTextUI.Instance?.Show("전투 승리", FloatingTextUI.ColorAcquire);

        if (endStateDuration > 0f)
            yield return new WaitForSeconds(endStateDuration);

        PredictedEnemyDamage = 0;
        OnBattleValuesChanged?.Invoke();

        SetState(BattleState.BattleEnd);

        movementSystem?.UnlockAllInputLocks();

        if (debugLog)
            Debug.Log($"[BattleManager] EndBattle: {(isVictory ? "Victory" : "Defeat")}");

        OnBattleEnded?.Invoke();
    }

    /// <summary>디폴트 공격 카드 사용 (초기 개발용)</summary>
    public bool TryUseDefaultAttack(int targetIndex)
    {
        if (State != BattleState.PlayerTurn) return false;
        if (targetIndex < 0 || targetIndex >= Monsters.Count) return false;

        RuntimeMonster target = Monsters[targetIndex];
        if (target == null || target.IsDead) return false;

        if (CurrentHandCount <= 0 || CurrentMana < defaultAttackCostMana)
            return false;

        float dmgConst = characterStats != null ? characterStats.damageConst.FinalValue : 0f;
        float dmgPer   = characterStats != null ? characterStats.damagePer.FinalValue : 0f;
        int damage = BattleMath.CalcFinalDamage(dmgConst, dmgPer);

        target.currentHP = Mathf.Max(0, target.currentHP - damage);
        CurrentMana -= defaultAttackCostMana;
        CurrentHandCount = Mathf.Max(0, CurrentHandCount - 1);
        OnBattleValuesChanged?.Invoke();

        if (debugLog)
            Debug.Log($"[BattleManager] [PlayerTurn] 기본공격 -> {target.data?.monsterName} / dmg={damage} / hp={target.currentHP} / mana={CurrentMana} hand={CurrentHandCount}");

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
            CurrentHandCount = Mathf.FloorToInt(characterStats.maxHand.FinalValue);
        }
        else
        {
            CurrentMana = 0;
            CurrentHandCount = 0;
        }

        RecalculateEnemyIntent();
        OnBattleValuesChanged?.Invoke();
        SetState(BattleState.PlayerTurn);
        FloatingTextUI.Instance?.Show($"라운드 {RoundIndex}", FloatingTextUI.ColorAcquire);

        if (debugLog)
            Debug.Log($"[BattleManager] [RoundStart] R{RoundIndex} / mana={CurrentMana} hand={CurrentHandCount} / predictedEnemy={PredictedEnemyDamage}");
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

    private bool ShouldAutoEndPlayerTurn()
    {
        if (CurrentMana <= 0)
        {
            if (debugLog) Debug.Log("[BattleManager] [PlayerTurn] 자동 턴 종료: 마나 0");
            return true;
        }

        if (CurrentHandCount <= 0)
        {
            if (debugLog) Debug.Log("[BattleManager] [PlayerTurn] 자동 턴 종료: 손패 0");
            return true;
        }

        if (CurrentMana < defaultAttackCostMana)
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

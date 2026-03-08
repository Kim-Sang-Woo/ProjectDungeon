// ============================================================
// EventEffect.cs — 이벤트 결과 효과 기반 클래스 및 구현체
// 위치: Assets/Scripts/Events/Effects/EventEffect.cs
// ============================================================
// [구조]
//   EventEffect (abstract)          — 기반
//     GainItemEffect                — 아이템 N개 획득
//     RemoveItemEffect              — 아이템 N개 제거
//     GainGoldEffect                — 골드 N 증가  (추후 GoldSystem 연동)
//     LoseGoldEffect                — 골드 N 감소
//     ApplyStatusEffect             — 상태이상 부여 (추후 구현)
// ============================================================
using System;
using UnityEngine;

/// <summary>
/// 이벤트 결과로 실행되는 게임 효과 기반 클래스.
/// EventResult.effects 배열의 원소로 사용된다.
/// </summary>
[Serializable]
public abstract class EventEffect
{
    /// <summary>효과 실행. 각 서브클래스에서 구현한다.</summary>
    public abstract void Execute();

    /// <summary>
    /// ResultView 효과 텍스트용 문자열 반환.
    /// Rich Text 태그 포함 가능 (획득: #78c058 초록 / 제거: #e06060 빨강).
    /// </summary>
    public abstract string GetEffectText();
}

// ────────────────────────────────────────────────────────────
// 아이템 효과
// ────────────────────────────────────────────────────────────

/// <summary>아이템 N개를 인벤토리에 추가한다.</summary>
[Serializable]
public class GainItemEffect : EventEffect
{
    [Tooltip("획득할 아이템 SO")]
    public ItemData item;

    [Tooltip("획득 수량")]
    [Min(1)] public int amount = 1;

    public override void Execute()
    {
        if (item == null || Inventory.Instance == null) return;

        AddItemResult result = Inventory.Instance.AddItem(item, amount);

        switch (result)
        {
            case AddItemResult.Success:
                FloatingTextUI.Instance?.Show(
                    $"{item.itemName} ×{amount} 획득",
                    FloatingTextUI.ColorAcquire);
                break;
            case AddItemResult.FailSlotFull:
                FloatingTextUI.Instance?.Show("인벤토리가 가득 찼습니다.", FloatingTextUI.ColorFail);
                break;
            case AddItemResult.FailTooHeavy:
                FloatingTextUI.Instance?.Show("너무 무겁습니다.", FloatingTextUI.ColorFail);
                break;
        }
    }

    public override string GetEffectText()
    {
        if (item == null) return "";
        string qty = amount > 1 ? $" ×{amount}" : "";
        return $"<color=#78c058>{item.itemName}{qty} 획득.</color>";
    }
}

/// <summary>아이템 N개를 인벤토리에서 제거한다. 수량 부족 시 가능한 만큼 제거한다.</summary>
[Serializable]
public class RemoveItemEffect : EventEffect
{
    [Tooltip("제거할 아이템 SO")]
    public ItemData item;

    [Tooltip("제거 수량")]
    [Min(1)] public int amount = 1;

    public override void Execute()
    {
        if (item == null || Inventory.Instance == null) return;

        int remaining = amount;
        var slots = Inventory.Instance.Slots;

        // 뒤에서부터 순회하여 슬롯 인덱스 변동 방지
        for (int i = slots.Count - 1; i >= 0 && remaining > 0; i--)
        {
            if (slots[i].item != item) continue;

            if (slots[i].quantity <= remaining)
            {
                // 슬롯 전체 제거
                remaining -= slots[i].quantity;
                Inventory.Instance.RemoveAt(i);
            }
            else
            {
                // 부분 제거: 슬롯 전체 제거 후 남은 수량 재추가
                int leftover = slots[i].quantity - remaining;
                remaining = 0;
                Inventory.Instance.RemoveAt(i);
                Inventory.Instance.AddItem(item, leftover);
            }
        }
    }

    public override string GetEffectText()
    {
        if (item == null) return "";
        string qty = amount > 1 ? $" ×{amount}" : "";
        return $"<color=#e06060>{item.itemName}{qty} 소모.</color>";
    }
}

// ────────────────────────────────────────────────────────────
// 골드 효과 (추후 GoldSystem 연동 시 Execute 내용 채울 것)
// ────────────────────────────────────────────────────────────

/// <summary>골드 N을 획득한다.</summary>
[Serializable]
public class GainGoldEffect : EventEffect
{
    [Tooltip("획득 골드량")]
    [Min(1)] public int amount = 100;

    public override void Execute()
    {
        // TODO: GoldSystem.Instance?.Add(amount);
        FloatingTextUI.Instance?.Show($"골드 +{amount}", FloatingTextUI.ColorAcquire);
        Debug.Log($"[GainGoldEffect] 골드 +{amount} (GoldSystem 미구현)");
    }

    public override string GetEffectText() =>
        $"<color=#78c058>골드 +{amount} 획득.</color>";
}

/// <summary>골드 N을 소모한다.</summary>
[Serializable]
public class LoseGoldEffect : EventEffect
{
    [Tooltip("소모 골드량")]
    [Min(1)] public int amount = 100;

    public override void Execute()
    {
        // TODO: GoldSystem.Instance?.Remove(amount);
        FloatingTextUI.Instance?.Show($"골드 -{amount}", FloatingTextUI.ColorFail);
        Debug.Log($"[LoseGoldEffect] 골드 -{amount} (GoldSystem 미구현)");
    }

    public override string GetEffectText() =>
        $"<color=#e06060>골드 -{amount} 소모.</color>";
}

/// <summary>상태이상을 부여한다. (추후 StatusSystem 구현 시 완성)</summary>
[Serializable]
public class ApplyStatusEffect : EventEffect
{
    [Tooltip("부여할 상태이상 ID")]
    public string statusId;

    public override void Execute()
    {
        // TODO: StatusSystem.Instance?.Apply(statusId);
        Debug.Log($"[ApplyStatusEffect] 상태이상 '{statusId}' (StatusSystem 미구현)");
    }

    public override string GetEffectText() =>
        $"<color=#e8a040>상태이상 '{statusId}' 부여.</color>";
}

// ────────────────────────────────────────────────────────────
// 오브젝트 시스템 연동 효과
// ────────────────────────────────────────────────────────────

/// <summary>
/// 보물 상자 등 일회성 오브젝트를 소진(제거)한다.
/// GainItemEffect 이후 실행되어 오브젝트를 씬에서 삭제한다.
/// </summary>
[Serializable]
public class ConsumeObjectEffect : EventEffect
{
    public Vector2Int         tilePos;
    public TileData           tile;
    public DungeonObjectSpawner spawner;

    public override void Execute()
    {
        if (tile != null)
            tile.isObjectInteracted = true;

        if (spawner != null)
            spawner.RemoveAt(tilePos);

        Debug.Log($"[ConsumeObjectEffect] 오브젝트 소진: {tilePos}");
    }

    public override string GetEffectText() => "";
}

/// <summary>
/// 계단 이동 효과. 층을 이동하고 이동 잠금을 해제한다.
/// </summary>
[Serializable]
public class StairsMoveEffect : EventEffect
{
    public bool              isDown;
    public DungeonManager    dungeonManager;
    public MovementSystem    movementSystem;

    public override void Execute()
    {
        if (dungeonManager == null) return;

        if (isDown)
        {
            Debug.Log("[StairsMoveEffect] 다음 층으로 이동");
            dungeonManager.GoToNextFloor();
        }
        else
        {
            if (dungeonManager.CurrentFloorIndex <= 0)
            {
                Debug.Log("[StairsMoveEffect] 최상위 층");
                movementSystem?.UnlockInput();
                return;
            }
            Debug.Log("[StairsMoveEffect] 이전 층으로 이동");
            dungeonManager.GoToPreviousFloor();
        }
        // 층 전환 시 입력 잠금 해제는 DungeonManager.OnFloorChanged → ObjectEventTrigger가 처리
    }

    public override string GetEffectText() => "";
}

/// <summary>
/// 이벤트에서 전투를 시작한다.
/// EventChoice.directEffects 또는 EventResult.effects에서 사용.
/// </summary>
[Serializable]
public class StartBattleEffect : EventEffect
{
    [Tooltip("전투에 사용할 인카운트 데이터")]
    public EncounterData encounter;

    [Tooltip("명시하지 않으면 BattleManager.Instance 사용")]
    public BattleManager battleManager;

    public override void Execute()
    {
        BattleManager bm = battleManager != null ? battleManager : BattleManager.Instance;
        if (bm == null)
        {
            Debug.LogWarning("[StartBattleEffect] BattleManager를 찾을 수 없습니다.");
            return;
        }

        if (encounter == null)
        {
            Debug.LogWarning("[StartBattleEffect] encounter가 비어 있습니다.");
            return;
        }

        bm.StartBattle(encounter);
    }

    public override string GetEffectText()
    {
        string n = encounter != null && !string.IsNullOrEmpty(encounter.displayName)
            ? encounter.displayName
            : "미지의 인카운트";
        return $"<color=#e8a040>전투 시작: {n}</color>";
    }
}

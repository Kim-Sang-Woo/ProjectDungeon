// ============================================================
// EquipmentManager.cs — 장비 장착/해제 로직
// 위치: Assets/Scripts/Core/EquipmentManager.cs
// ============================================================
// [개요]
//   장비 장착/해제를 관리하는 싱글턴.
//   장착 시 CharacterStats에 보정치 적용.
//   장착된 아이템은 인벤토리 무게/슬롯 계산에서 제외.
//   같은 타입 장비가 이미 있으면 자동 교체.
//
// [씬 배치]
//   Hierarchy → 빈 GameObject "EquipmentManager"
//   EquipmentManager.cs 부착
//
// [Inspector 연결]
//   characterStats → CharacterStats 오브젝트
//   startEquips    → 슬롯별 초기 장비 설정
// ============================================================
using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class StartEquipEntry
{
    public EquipType slot;
    public EquipData equip;
}

public class EquipmentManager : MonoBehaviour
{
    public static EquipmentManager Instance { get; private set; }

    [Header("연동")]
    public CharacterStats characterStats;

    [Header("초기 장비 (Start Equip)")]
    public List<StartEquipEntry> startEquips = new List<StartEquipEntry>();

    // 슬롯별 현재 장착 장비
    private Dictionary<EquipType, EquipData> equipped = new Dictionary<EquipType, EquipData>();

    /// <summary>장착 상태 변경 시 발신 → EquipmentUI 갱신용</summary>
    public event Action OnEquipChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        // 초기 장비 자동 장착
        foreach (var entry in startEquips)
            if (entry.equip != null)
                Equip(entry.equip);
    }

    // ─── 공개 API ───

    /// <summary>장비 장착. 같은 슬롯에 장비가 있으면 인벤토리로 반환 후 교체.</summary>
    public void Equip(EquipData equip)
    {
        if (equip == null) return;

        // 같은 슬롯에 이미 장착된 장비 → 스탯 제거 후 인벤토리로 반환
        if (equipped.TryGetValue(equip.equipType, out var prev) && prev != null)
        {
            UnequipInternal(prev);
            if (Inventory.Instance != null)
            {
                var result = Inventory.Instance.AddItem(prev, 1);
                if (result != AddItemResult.Success)
                    Debug.LogWarning($"[EquipmentManager] 교체 중 반환 실패({result}): {prev.itemName}");
            }
        }

        equipped[equip.equipType] = equip;
        ApplyStats(equip, +1);
        OnEquipChanged?.Invoke();
        Debug.Log($"[EquipmentManager] 장착: {equip.itemName} ({equip.equipType})");
    }

    /// <summary>슬롯의 장비 해제 후 인벤토리로 반환.</summary>
    public void Unequip(EquipType slot)
    {
        if (!equipped.TryGetValue(slot, out var equip) || equip == null) return;

        // 장비 해제 시 인벤토리 슬롯 여유 체크
        // (해제된 장비가 인벤토리로 반환되므로 빈 슬롯 1개 이상 필요)
        if (equip.equipType == EquipType.Bag && equip.statMaxItemSlot > 0)
        {
            // 가방: 해제 후 슬롯 감소 + 가방 자체 반환 슬롯 필요
            int currentItems = Inventory.Instance != null ? Inventory.Instance.CurrentItemCount : 0;
            int currentMax   = Inventory.Instance != null ? Inventory.Instance.MaxItemCount : 0;
            int afterMax     = currentMax - equip.statMaxItemSlot;

            if (currentItems > afterMax - 1)
            {
                FloatingTextUI.Instance?.Show("가방을 해제할 수 없습니다.", FloatingTextUI.ColorFail);
                Debug.Log($"[EquipmentManager] 가방 해제 불가 — 현재 아이템:{currentItems} 해제 후 슬롯:{afterMax - 1}");
                return;
            }
        }
        else if (equip.equipType != EquipType.Bag)
        {
            // 가방 외 장비: 반환 슬롯 1개만 필요
            int currentItems = Inventory.Instance != null ? Inventory.Instance.CurrentItemCount : 0;
            int currentMax   = Inventory.Instance != null ? Inventory.Instance.MaxItemCount : 0;

            if (currentItems >= currentMax)
            {
                FloatingTextUI.Instance?.Show("장비를 해제할 수 없습니다.", FloatingTextUI.ColorFail);
                Debug.Log($"[EquipmentManager] 장비 해제 불가 — 현재 아이템:{currentItems} 최대 슬롯:{currentMax}");
                return;
            }
        }

        UnequipInternal(equip);
        equipped.Remove(slot);

        // 인벤토리로 반환
        if (Inventory.Instance != null)
        {
            var result = Inventory.Instance.AddItem(equip, 1);
            if (result != AddItemResult.Success)
                Debug.LogWarning($"[EquipmentManager] 인벤토리 반환 실패({result}): {equip.itemName}");
        }

        OnEquipChanged?.Invoke();
        Debug.Log($"[EquipmentManager] 해제: {equip.itemName} ({slot})");
    }

    /// <summary>슬롯에 장착된 장비 반환. 없으면 null.</summary>
    public EquipData GetEquipped(EquipType slot)
    {
        equipped.TryGetValue(slot, out var equip);
        return equip;
    }

    /// <summary>장착된 모든 장비 반환.</summary>
    public IEnumerable<KeyValuePair<EquipType, EquipData>> GetAllEquipped()
        => equipped;

    /// <summary>해당 장비가 현재 장착 중인지 여부.</summary>
    public bool IsEquipped(EquipData equip)
    {
        if (equip == null) return false;
        return equipped.TryGetValue(equip.equipType, out var cur) && cur == equip;
    }

    // ─── 내부 ───

    private void UnequipInternal(EquipData equip)
    {
        ApplyStats(equip, -1);
    }

    /// <summary>sign: +1 = 장착, -1 = 해제</summary>
    private void ApplyStats(EquipData e, int sign)
    {
        if (characterStats == null) return;

        void Apply(StatType type, float val)
        {
            if (val == 0) return;
            if (sign > 0) characterStats.AddModifier(type, val);
            else          characterStats.RemoveModifier(type, val);
        }

        Apply(StatType.MaxHP,       e.statMaxHP);
        Apply(StatType.HPGen,       e.statHPGen);
        Apply(StatType.BaseMana,    e.statBaseMana);
        Apply(StatType.MaxHand,     e.statMaxHand);
        Apply(StatType.BaseShield,  e.statBaseShield);
        Apply(StatType.DamagePer,   e.statDamagePer);
        Apply(StatType.DamageConst, e.statDamageConst);
        Apply(StatType.BaseDodge,   e.statBaseDodge);

        // 인벤토리 제한 보정
        if (e.statMaxItemSlot != 0)
        {
            characterStats.maxItemSlot += sign * e.statMaxItemSlot;
            if (characterStats.capMaxItemSlot > 0)
                characterStats.maxItemSlot = Mathf.Min(
                    characterStats.maxItemSlot, characterStats.capMaxItemSlot);
        }
        if (e.statMaxCarryWeight != 0)
        {
            characterStats.maxCarryWeight += sign * e.statMaxCarryWeight;
            if (characterStats.capMaxCarryWeight > 0)
                characterStats.maxCarryWeight = Mathf.Min(
                    characterStats.maxCarryWeight, characterStats.capMaxCarryWeight);
        }

        characterStats.NotifyStatsChanged();
    }
}

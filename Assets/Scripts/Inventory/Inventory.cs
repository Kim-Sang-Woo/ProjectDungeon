// ============================================================
// Inventory.cs — 인벤토리 데이터 및 로직
// 위치: Assets/Scripts/Inventory/Inventory.cs
// ============================================================
// [v1.2 변경사항]
//   - AddItemResult enum 추가: 성공/슬롯초과/무게초과 구분
//   - AddItem() 반환값을 bool → AddItemResult로 변경
//   - 무게 감속 관련 설정 추가 (weightSlowThreshold, weightSlowRatio)
//   - OnWeightChanged 이벤트 추가: MovementSystem이 구독하여 속도 조정
// ============================================================
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>아이템 추가 결과</summary>
public enum AddItemResult
{
    Success,        // 획득 성공
    FailSlotFull,   // 슬롯(종류) 초과
    FailTooHeavy,   // 무게 초과
}

[System.Serializable]
public class InventorySlot
{
    public ItemData item;
    public int      quantity;

    public InventorySlot(ItemData item, int quantity)
    {
        this.item     = item;
        this.quantity = quantity;
    }
}

public class Inventory : MonoBehaviour
{
    public static Inventory Instance { get; private set; }

    [Header("스탯 연동")]
    [Tooltip("CharacterStats에서 수량/무게 제한을 가져옴. 없으면 아래 기본값 사용")]
    public CharacterStats characterStats;

    [Header("수량/무게 제한 (CharacterStats 미연결 시 사용)")]
    public int   maxItemCount = 30;
    public float maxWeight    = 30f;

    /// <summary>실제 적용되는 최대 슬롯 수</summary>
    public int MaxItemCount => characterStats != null
        ? characterStats.maxItemSlot
        : maxItemCount;

    /// <summary>실제 적용되는 최대 무게</summary>
    public float MaxWeight => characterStats != null
        ? characterStats.maxCarryWeight
        : maxWeight;

    [Header("무게 감속 설정")]
    [Tooltip("이 비율 이상 무게가 차면 이동속도 감소 (0.0~1.0)")]
    [Range(0f, 1f)]
    public float weightSlowThreshold = 0.7f;

    [Tooltip("감속 비율 (0.3 = 30% 감속)")]
    [Range(0f, 0.9f)]
    public float weightSlowRatio = 0.3f;

    public List<InventorySlot> Slots { get; private set; } = new List<InventorySlot>();

    /// <summary>현재 점유 슬롯 수 (스택 아이템은 1슬롯)</summary>
    public int CurrentItemCount => Slots.Count;

    public float CurrentWeight
    {
        get { float t = 0f; foreach (var s in Slots) t += s.item.weight * s.quantity; return t; }
    }

    /// <summary>현재 무게가 감속 기준을 초과하는지 여부</summary>
    public bool IsOverweightSlow => CurrentWeight >= MaxWeight * weightSlowThreshold;

    public event Action OnInventoryChanged;

    /// <summary>무게 변화 시 발신 — MovementSystem이 구독하여 속도 조정</summary>
    public event Action<bool> OnWeightChanged; // true = 감속 구간, false = 정상

    private bool wasSlowBefore = false;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ─── 아이템 추가 ───

    public AddItemResult AddItem(ItemData item, int quantity = 1)
    {
        if (item == null || quantity <= 0) return AddItemResult.FailSlotFull;

        // 슬롯 제한 체크
        bool needsNewSlot = !item.isStackable ||
                            !Slots.Exists(s => s.item.itemId == item.itemId &&
                                               s.quantity < item.maxStack);
        if (needsNewSlot && Slots.Count >= MaxItemCount)
        {
            Debug.Log($"[Inventory] 슬롯 초과! 현재:{Slots.Count} 최대:{MaxItemCount}");
            return AddItemResult.FailSlotFull;
        }

        // 무게 제한 체크
        if (CurrentWeight + item.weight * quantity > MaxWeight)
        {
            Debug.Log($"[Inventory] 무게 초과! 현재:{CurrentWeight:F1} 추가:{item.weight * quantity:F1} 최대:{MaxWeight}");
            return AddItemResult.FailTooHeavy;
        }

        int remaining = quantity;

        if (item.isStackable)
        {
            foreach (var slot in Slots)
            {
                if (slot.item.itemId != item.itemId) continue;
                if (remaining <= 0) break;
                int addable    = item.maxStack - slot.quantity;
                if (addable <= 0) continue;
                int toAdd      = Mathf.Min(remaining, addable);
                slot.quantity += toAdd;
                remaining     -= toAdd;
            }
        }

        while (remaining > 0)
        {
            int toAdd = item.isStackable ? Mathf.Min(remaining, item.maxStack) : 1;
            Slots.Add(new InventorySlot(item, toAdd));
            remaining -= toAdd;
        }

        Debug.Log($"[Inventory] 아이템 추가: {item.itemName} x{quantity}");
        OnInventoryChanged?.Invoke();
        NotifyWeightChanged();
        return AddItemResult.Success;
    }

    // ─── 아이템 제거 ───

    public void RemoveAt(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= Slots.Count) return;
        Debug.Log($"[Inventory] 아이템 버리기: {Slots[slotIndex].item.itemName}");
        Slots.RemoveAt(slotIndex);
        OnInventoryChanged?.Invoke();
        NotifyWeightChanged();
    }

    /// <summary>
    /// 슬롯 위치 변경 (드래그 정렬용)
    /// - 대상이 아이템 슬롯이면 서로 스왑
    /// - 대상이 빈 슬롯이면 아이템을 맨 뒤(가장 마지막 위치)로 이동
    /// </summary>
    public void MoveSlot(int fromIndex, int toIndex)
    {
        if (Slots == null || Slots.Count == 0) return;
        if (fromIndex < 0 || fromIndex >= Slots.Count) return;

        // UI 기준 open slot index를 받으므로 MaxItemCount 범위로 제한
        toIndex = Mathf.Clamp(toIndex, 0, Mathf.Max(0, MaxItemCount - 1));
        if (fromIndex == toIndex) return;

        // 대상 슬롯에 아이템이 있으면 "스왑"
        if (toIndex < Slots.Count)
        {
            InventorySlot temp = Slots[fromIndex];
            Slots[fromIndex] = Slots[toIndex];
            Slots[toIndex] = temp;

            Debug.Log($"[Inventory] 슬롯 스왑: {fromIndex} <-> {toIndex}");
            OnInventoryChanged?.Invoke();
            return;
        }

        // 대상 슬롯이 비어 있으면 끝으로 이동(현재 구조상 빈 슬롯은 뒤쪽 연속 구간)
        int lastIndex = Slots.Count - 1;
        if (fromIndex == lastIndex) return;

        InventorySlot moving = Slots[fromIndex];
        Slots.RemoveAt(fromIndex);
        Slots.Add(moving);

        Debug.Log($"[Inventory] 빈 슬롯으로 이동: {fromIndex} -> {Slots.Count - 1}");
        OnInventoryChanged?.Invoke();
    }

    // ─── 무게 감속 알림 ───

    private void NotifyWeightChanged()
    {
        bool isSlow = IsOverweightSlow;
        OnWeightChanged?.Invoke(isSlow);
        wasSlowBefore = isSlow;
    }
}

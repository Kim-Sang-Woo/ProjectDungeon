// ============================================================
// Inventory.cs — 인벤토리 데이터 및 로직
// 위치: Assets/Scripts/Inventory/Inventory.cs
// ============================================================
// [v1.1 수정]
//   스택 버그 수정
//   - 기존: Slots.Find()로 첫 슬롯만 탐색 → 꽉 차면 새 슬롯 추가
//   - 변경: 모든 슬롯을 순회하여 빈 공간을 순서대로 채운 뒤
//     남은 수량이 있을 때만 새 슬롯 추가
//   예) maxStack=99, 치유의 물약 98개 보유 중 3개 추가 시
//     기존 슬롯에 1개 채우고 → 새 슬롯에 2개 추가
// ============================================================
using System;
using System.Collections.Generic;
using UnityEngine;

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

    [Header("수량/무게 제한")]
    public int   maxItemCount = 100;
    public float maxWeight    = 100f;

    public List<InventorySlot> Slots { get; private set; } = new List<InventorySlot>();

    /// <summary>
    /// 현재 점유 슬롯 수 (스택 아이템은 수량과 무관하게 1슬롯으로 계산)
    /// </summary>
    public int CurrentItemCount => Slots.Count;

    public float CurrentWeight
    {
        get { float t = 0f; foreach (var s in Slots) t += s.item.weight * s.quantity; return t; }
    }

    public event Action OnInventoryChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ─── 아이템 추가 ───

    public bool AddItem(ItemData item, int quantity = 1)
    {
        if (item == null || quantity <= 0) return false;

        // 슬롯 수량 제한 체크 (스택 여부와 무관하게 슬롯 1개 기준)
        // 스택 가능 아이템은 기존 슬롯에 합산되므로 새 슬롯이 생기는 경우만 체크
        bool needsNewSlot = !item.isStackable ||
                            !Slots.Exists(s => s.item.itemId == item.itemId &&
                                               s.quantity < item.maxStack);
        if (needsNewSlot && Slots.Count >= maxItemCount)
        {
            Debug.Log($"[Inventory] 슬롯 초과! 현재:{Slots.Count} 최대:{maxItemCount}");
            return false;
        }

        if (CurrentWeight + item.weight * quantity > maxWeight)
        {
            Debug.Log($"[Inventory] 무게 초과! 현재:{CurrentWeight:F1} 추가:{item.weight * quantity:F1} 최대:{maxWeight}");
            return false;
        }

        int remaining = quantity;

        // 스택 가능 아이템 — 기존 슬롯들을 순회하며 빈 공간 채우기
        if (item.isStackable)
        {
            foreach (var slot in Slots)
            {
                if (slot.item.itemId != item.itemId) continue;
                if (remaining <= 0) break;

                int addable = item.maxStack - slot.quantity;
                if (addable <= 0) continue;

                int toAdd       = Mathf.Min(remaining, addable);
                slot.quantity  += toAdd;
                remaining      -= toAdd;
            }
        }

        // 남은 수량 → 새 슬롯에 maxStack 단위로 추가
        while (remaining > 0)
        {
            int toAdd  = item.isStackable ? Mathf.Min(remaining, item.maxStack) : 1;
            Slots.Add(new InventorySlot(item, toAdd));
            remaining -= toAdd;
        }

        OnInventoryChanged?.Invoke();
        Debug.Log($"[Inventory] 아이템 추가: {item.itemName} x{quantity}");
        return true;
    }

    // ─── 아이템 제거 ───

    public void RemoveAt(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= Slots.Count) return;
        Debug.Log($"[Inventory] 아이템 버리기: {Slots[slotIndex].item.itemName}");
        Slots.RemoveAt(slotIndex);
        OnInventoryChanged?.Invoke();
    }
}

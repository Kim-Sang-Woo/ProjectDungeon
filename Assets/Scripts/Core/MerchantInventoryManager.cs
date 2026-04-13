using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class MerchantStockEntry
{
    public ItemData item;
    [Range(0f, 1f)] public float appearChance = 1f;
    [Min(1)] public int quantity = 1;
}

[System.Serializable]
public class MerchantInventorySlot
{
    public ItemData item;
    public int quantity;

    public MerchantInventorySlot(ItemData item, int quantity)
    {
        this.item = item;
        this.quantity = quantity;
    }
}

public class MerchantInventoryManager : MonoBehaviour
{
    public static MerchantInventoryManager Instance { get; private set; }

    [Header("상점 표시")]
    public int columns = 6;
    public int rows = 4;

    [Header("상점 판매 후보")]
    public List<MerchantStockEntry> stockEntries = new List<MerchantStockEntry>();

    public List<MerchantInventorySlot> Slots { get; private set; } = new List<MerchantInventorySlot>();
    public int Capacity => Mathf.Max(1, columns * rows);

    public event System.Action OnMerchantInventoryChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void RefreshStock()
    {
        Slots.Clear();

        foreach (var entry in stockEntries)
        {
            if (entry == null || entry.item == null) continue;
            if (Random.value > Mathf.Clamp01(entry.appearChance)) continue;
            if (Slots.Count >= Capacity) break;

            Slots.Add(new MerchantInventorySlot(entry.item, Mathf.Max(1, entry.quantity)));
        }

        OnMerchantInventoryChanged?.Invoke();
    }

    public void EnsureStockReady()
    {
        if (Slots.Count == 0)
            RefreshStock();
    }

    public void RemoveAt(int index)
    {
        if (index < 0 || index >= Slots.Count) return;
        Slots.RemoveAt(index);
        OnMerchantInventoryChanged?.Invoke();
    }
}

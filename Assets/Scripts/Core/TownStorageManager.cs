using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class TownStorageSlot
{
    public ItemData item;
    public int quantity;

    public TownStorageSlot(ItemData item, int quantity)
    {
        this.item = item;
        this.quantity = quantity;
    }
}

public class TownStorageManager : MonoBehaviour
{
    public static TownStorageManager Instance { get; private set; }

    [Header("보관함")]
    public int columns = 10;
    public int rows = 10;

    public List<TownStorageSlot> Slots { get; private set; } = new List<TownStorageSlot>();
    public int Capacity => Mathf.Max(1, columns * rows);

    public event System.Action OnStorageChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public bool TryStore(ItemData item, int quantity)
    {
        if (item == null || quantity <= 0) return false;

        TownStorageSlot existing = Slots.Find(s => s.item == item);
        if (existing != null)
        {
            existing.quantity += quantity;
            OnStorageChanged?.Invoke();
            return true;
        }

        if (Slots.Count >= Capacity)
            return false;

        Slots.Add(new TownStorageSlot(item, quantity));
        OnStorageChanged?.Invoke();
        return true;
    }

    public void RemoveAt(int index)
    {
        if (index < 0 || index >= Slots.Count) return;
        Slots.RemoveAt(index);
        OnStorageChanged?.Invoke();
    }
}

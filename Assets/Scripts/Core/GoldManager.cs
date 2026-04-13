using UnityEngine;

public class GoldManager : MonoBehaviour
{
    public static GoldManager Instance { get; private set; }

    [Header("골드")]
    [Min(0)] public int startGold = 0;

    public int CurrentGold { get; private set; }

    public event System.Action<int> OnGoldChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        CurrentGold = startGold;
    }

    public void SetGold(int value)
    {
        CurrentGold = Mathf.Max(0, value);
        OnGoldChanged?.Invoke(CurrentGold);
    }

    public void AddGold(int amount)
    {
        if (amount <= 0) return;
        CurrentGold += amount;
        OnGoldChanged?.Invoke(CurrentGold);
    }

    public bool TrySpendGold(int amount)
    {
        if (amount <= 0) return true;
        if (CurrentGold < amount) return false;
        CurrentGold -= amount;
        OnGoldChanged?.Invoke(CurrentGold);
        return true;
    }
}

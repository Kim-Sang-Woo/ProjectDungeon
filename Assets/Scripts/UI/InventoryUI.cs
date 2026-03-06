// ============================================================
// InventoryUI.cs — 인벤토리 패널 UI
// 위치: Assets/Scripts/UI/InventoryUI.cs
// ============================================================
// [v1.1 수정]
//   아이템 클릭 → ItemDetailUI 미호출 문제 수정
//   - 원인: Text의 raycastTarget이 꺼져 있거나
//     InventoryPanel Image가 클릭을 가로채는 경우
//   - 해결: 코드에서 raycastTarget을 강제로 true 설정
//     InventoryPanel의 Image.raycastTarget을 false로 설정하여
//     패널 배경이 클릭을 가로채지 않도록 처리
// ============================================================
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class InventoryUI : MonoBehaviour
{
    [Header("UI 요소")]
    [Tooltip("단일 모노스페이스 텍스트 (Courier New 폰트 적용)")]
    public Text         statsText;
    public Transform    itemListContainer;
    public Text         itemEntryPrefab;
    public ItemDetailUI itemDetailUI;

    [Header("표시 제어")]
    public CanvasGroup canvasGroup;

    [Header("아이템 텍스트 색상")]
    public Color itemNormalColor = Color.white;
    public Color itemHoverColor  = new Color(0.29f, 0.62f, 1.00f);

    [Header("아이템 높이")]
    public float itemLineHeight = 30f;

    private List<GameObject> spawnedEntries = new List<GameObject>();
    private bool isVisible = false;

    private void Awake()
    {
        HideImmediate();

        // ── 수정: InventoryPanel 자체 Image가 클릭을 가로채지 않도록 ──
        Image panelImage = GetComponent<Image>();
        if (panelImage != null) panelImage.raycastTarget = false;
    }

    [Header("연동")]
    [Tooltip("CharacterStats를 직접 연결 (자동 탐색 실패 시 사용)")]
    public CharacterStats characterStats;

    private CharacterStats cachedStats;

    private void Start()
    {
        if (Inventory.Instance != null)
            Inventory.Instance.OnInventoryChanged += RefreshIfVisible;

        // Inspector 직접 연결 → Instance → FindObjectOfType 순으로 탐색
        cachedStats = characterStats
                      ?? CharacterStats.Instance
                      ?? FindObjectOfType<CharacterStats>();

        if (cachedStats != null)
            cachedStats.OnStatsChanged += RefreshIfVisible;
        else
            Debug.LogWarning("[InventoryUI] CharacterStats를 찾을 수 없습니다. Inspector에서 직접 연결해주세요.");
    }

    private void OnDestroy()
    {
        if (Inventory.Instance != null)
            Inventory.Instance.OnInventoryChanged -= RefreshIfVisible;
        if (cachedStats != null)
            cachedStats.OnStatsChanged -= RefreshIfVisible;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
            Toggle();
    }

    // ─── 표시/숨김 ───

    public void Toggle()
    {
        if (isVisible) Hide();
        else           Show();
    }

    public void Show()
    {
        // CharacterStats 재탐색 (Start 시점에 못 찾은 경우 대비)
        if (cachedStats == null)
        {
            cachedStats = CharacterStats.Instance ?? FindObjectOfType<CharacterStats>();
            if (cachedStats != null)
                cachedStats.OnStatsChanged += RefreshIfVisible;
        }

        isVisible = true;
        Refresh();
        ShowImmediate();
    }

    public void Hide()
    {
        isVisible = false;
        HideImmediate();
        if (itemDetailUI != null) itemDetailUI.Hide();
    }

    private void RefreshIfVisible()
    {
        if (isVisible) Refresh();
    }

    // ─── 목록 갱신 ───

    private void Refresh()
    {
        RefreshStats();
        RefreshItemList();
    }

    private void RefreshStats()
    {
        if (statsText == null) return;

        var inv = Inventory.Instance;
        var s   = cachedStats;

        string itemCount = inv != null ? $"{inv.CurrentItemCount} / {inv.MaxItemCount}"                   : "-";
        string weight    = inv != null ? $"{inv.CurrentWeight:F1} / {inv.MaxWeight:F1} kg"                : "-";
        string hp        = s   != null ? $"{s.currentHP:F0} / {s.maxHP.FinalValue:F0}"                    : "-";
        string hpGen     = s   != null ? $"{s.hpGen.FinalValue:F0}"                                       : "-";
        string mana      = s   != null ? $"{s.baseMana.FinalValue:F0}"                                    : "-";
        string hand      = s   != null ? $"{s.maxHand.FinalValue:F0}"                                     : "-";
        string shield    = s   != null ? $"{s.baseShield.FinalValue:F0}"                                  : "-";
        string damage    = s   != null ? $"{s.damageConst.FinalValue:F0} (+{s.damagePer.FinalValue:F0}%)" : "-";
        string dodge     = s   != null ? $"{s.baseDodge.FinalValue:F0}"                                   : "-";

        statsText.text =
            Pad("아이템", 9) + itemCount + "\n" +
            Pad("무게",   9) + weight    + "\n" +
            "─────────────────────\n"           +
            Pad("체력",   9) + hp        + "\n" +
            Pad("회복력", 9) + hpGen     + "\n" +
            Pad("지구력", 9) + mana      + "\n" +
            Pad("행동력", 9) + hand      + "\n" +
            Pad("방어력", 9) + shield    + "\n" +
            Pad("피해량", 9) + damage    + "\n" +
            Pad("회피",   9) + dodge     + "\n" +
            "─────────────────────";
    }

    /// <summary>모노스페이스 정렬 — 한글 1자=2칸, 영문=1칸</summary>
    private string Pad(string label, int totalWidth)
    {
        int len = 0;
        foreach (char c in label)
            len += c > 127 ? 2 : 1;
        int spaces = Mathf.Max(0, totalWidth - len);
        return label + new string(' ', spaces);
    }

    private void RefreshItemList()
    {
        ClearEntries();
        if (Inventory.Instance == null || itemListContainer == null || itemEntryPrefab == null)
        {
            Debug.LogWarning($"[InventoryUI] RefreshItemList 실패 — " +
                $"Inventory:{Inventory.Instance != null} " +
                $"Container:{itemListContainer != null} " +
                $"Prefab:{itemEntryPrefab != null}");
            return;
        }

        var slots = Inventory.Instance.Slots;
        Debug.Log($"[InventoryUI] 목록 갱신 — 슬롯 수:{slots.Count}");

        itemEntryPrefab.gameObject.SetActive(false);

        if (slots.Count == 0)
        {
            GameObject emptyGo = Instantiate(itemEntryPrefab.gameObject, itemListContainer);
            Text emptyText = emptyGo.GetComponent<Text>();
            if (emptyText != null)
            {
                emptyText.text           = "아이템이 없습니다.";
                emptyText.color          = new Color(0.6f, 0.6f, 0.6f);
                emptyText.raycastTarget  = false;
            }
            SetLayoutElement(emptyGo);
            emptyGo.SetActive(true);
            spawnedEntries.Add(emptyGo);
            return;
        }

        for (int i = 0; i < slots.Count; i++)
        {
            InventorySlot slot = slots[i];
            GameObject go = Instantiate(itemEntryPrefab.gameObject, itemListContainer);
            Text t = go.GetComponent<Text>();

            if (t != null)
            {
                t.text  = slot.item.isStackable && slot.quantity > 1
                    ? $"· {slot.item.itemName}({slot.quantity})"
                    : $"· {slot.item.itemName}";
                t.color          = itemNormalColor;
                t.raycastTarget  = true; // ── 수정: 클릭 감지를 위해 강제 활성화
            }

            SetLayoutElement(go);
            go.SetActive(true);

            int capturedIndex = i;
            SetupEntryEvents(go, t, slot, capturedIndex);
            spawnedEntries.Add(go);
        }
    }

    private void SetupEntryEvents(GameObject go, Text t, InventorySlot slot, int index)
    {
        EventTrigger trigger = go.GetComponent<EventTrigger>();
        if (trigger == null) trigger = go.AddComponent<EventTrigger>();
        trigger.triggers.Clear();

        var click = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
        click.callback.AddListener(_ =>
        {
            if (itemDetailUI != null)
                itemDetailUI.Show(slot, index, OnItemAction);
        });
        trigger.triggers.Add(click);

        var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enter.callback.AddListener(_ => { if (t) t.color = itemHoverColor; });
        trigger.triggers.Add(enter);

        var exitE = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exitE.callback.AddListener(_ => { if (t) t.color = itemNormalColor; });
        trigger.triggers.Add(exitE);
    }

    // ─── 아이템 액션 처리 ───

    private void OnItemAction(string actionId, int slotIndex)
    {
        switch (actionId)
        {
            case "delete":
                Inventory.Instance?.RemoveAt(slotIndex);
                break;
            default:
                Debug.LogWarning($"[InventoryUI] 처리되지 않은 actionId: {actionId}");
                break;
        }
    }

    // ─── 유틸 ───

    private void SetLayoutElement(GameObject go)
    {
        LayoutElement le = go.GetComponent<LayoutElement>();
        if (le == null) le = go.AddComponent<LayoutElement>();
        le.preferredHeight = itemLineHeight;
        le.minHeight       = itemLineHeight;
    }

    private void ClearEntries()
    {
        foreach (var go in spawnedEntries)
            if (go != null) Destroy(go);
        spawnedEntries.Clear();
    }

    private void ShowImmediate()
    {
        if (canvasGroup == null) return;
        canvasGroup.alpha          = 1f;
        canvasGroup.interactable   = true;
        canvasGroup.blocksRaycasts = true;
    }

    private void HideImmediate()
    {
        if (canvasGroup == null) return;
        canvasGroup.alpha          = 0f;
        canvasGroup.interactable   = false;
        canvasGroup.blocksRaycasts = false;
    }
}

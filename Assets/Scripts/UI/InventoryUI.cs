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

    private void Start()
    {
        if (Inventory.Instance != null)
            Inventory.Instance.OnInventoryChanged += RefreshIfVisible;
    }

    private void OnDestroy()
    {
        if (Inventory.Instance != null)
            Inventory.Instance.OnInventoryChanged -= RefreshIfVisible;
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
        if (statsText == null || Inventory.Instance == null) return;
        statsText.text =
            $"아이템  {Inventory.Instance.CurrentItemCount} / {Inventory.Instance.maxItemCount}\n" +
            $"무게    {Inventory.Instance.CurrentWeight:F1} / {Inventory.Instance.maxWeight:F1} kg";
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

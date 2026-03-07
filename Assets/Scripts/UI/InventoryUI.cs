// ============================================================
// InventoryUI.cs — 인벤토리 패널 UI v4.2
// 위치: Assets/Scripts/UI/InventoryUI.cs
// ============================================================
// [v4.2 변경사항] 큰 프리셋 적용
//   panelWidth      380 → 420
//   slotSize         72 → 80
//   slotSpacing       5 → 6
//   headerHeight     44 → 48
//   bagHeaderHeight  32 → 34
//   paddingH/V       18 → 20
//   MAX_SLOTS     36(4×9) → 40(4×10)
// ============================================================
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class InventoryUI : MonoBehaviour
{
    public static InventoryUI Instance { get; private set; }

    [Header("표시 제어")]
    public CanvasGroup canvasGroup;

    [Header("레이아웃 대상 오브젝트")]
    public RectTransform header;
    public RectTransform topSection;
    public RectTransform itemListContainer;

    [Header("Header 요소")]
    public Text titleText;
    public Text hintKeyText;

    [Header("ItemList 요소")]
    public Text       bagInfoText;
    public Transform  itemGrid;
    public GameObject itemSlotPrefab;

    [Header("레이아웃 수치")]
    public float panelWidth      = 420f;
    public float headerHeight    = 48f;
    public float bagHeaderHeight = 34f;
    public float paddingH        = 20f;
    public float paddingV        = 20f;
    public float slotSize        = 80f;
    public float slotSpacing     = 6f;

    [Header("슬롯 색상")]
    public Color colorEmpty  = new Color(0.055f, 0.047f, 0.035f, 1f);
    public Color colorFilled = new Color(0.094f, 0.078f, 0.063f, 1f);
    public Color colorHover  = new Color(0.157f, 0.125f, 0.063f, 1f);
    public Color colorLocked   = new Color(0.035f, 0.031f, 0.027f, 1f);

    [Header("연동")]
    public EquipmentUI   equipmentUI;
    public ItemTooltipUI tooltipUI;

    private const int GRID_COLUMNS = 4;
    private const int MAX_SLOTS    = 40; // 4×10

    private List<GameObject> spawnedSlots = new List<GameObject>();
    private bool             isVisible    = false;

    // ────────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        HideImmediate();
    }

    private void Start()
    {
        if (Inventory.Instance        != null)
            Inventory.Instance.OnInventoryChanged    += RefreshIfVisible;
        if (EquipmentManager.Instance != null)
            EquipmentManager.Instance.OnEquipChanged += RefreshIfVisible;

        ApplyPanelLayout();
        ApplyGridLayout();
    }

    private void OnDestroy()
    {
        if (Inventory.Instance        != null)
            Inventory.Instance.OnInventoryChanged    -= RefreshIfVisible;
        if (EquipmentManager.Instance != null)
            EquipmentManager.Instance.OnEquipChanged -= RefreshIfVisible;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab)) Toggle();
    }

    // ────────────────────────────────────────────────────────
    public void Toggle() { if (isVisible) Hide(); else Show(); }

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
        if (tooltipUI != null) tooltipUI.Hide();
    }

    // ────────────────────────────────────────────────────────
    public void ApplyPanelLayout()
    {
        float equipSectionH = CalcEquipSectionHeight();
        float itemGridH     = CalcItemGridHeight();
        float itemSectionH  = paddingV + bagHeaderHeight + slotSpacing + itemGridH + paddingV;
        float totalH        = headerHeight + equipSectionH + itemSectionH;

        // InventoryPanel
        RectTransform panelRT = GetComponent<RectTransform>();
        if (panelRT != null)
        {
            panelRT.anchorMin        = new Vector2(0f, 1f);
            panelRT.anchorMax        = new Vector2(0f, 1f);
            panelRT.pivot            = new Vector2(0f, 1f);
            panelRT.anchoredPosition = Vector2.zero;
            panelRT.sizeDelta        = new Vector2(panelWidth, totalH);
        }

        float y = 0f;

        // Header
        if (header != null)
        {
            header.anchorMin = new Vector2(0f, 1f);
            header.anchorMax = new Vector2(1f, 1f);
            header.pivot     = new Vector2(0.5f, 1f);
            header.offsetMin = new Vector2(0f, y - headerHeight);
            header.offsetMax = new Vector2(0f, y);

            if (titleText != null)
            {
                RectTransform rt = titleText.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0f,   0f);
                rt.anchorMax = new Vector2(0.7f, 1f);
                rt.pivot     = new Vector2(0f, 0.5f);
                rt.offsetMin = new Vector2(paddingH, 0f);
                rt.offsetMax = new Vector2(0f,       0f);
            }
            if (hintKeyText != null)
            {
                RectTransform rt = hintKeyText.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.7f, 0f);
                rt.anchorMax = new Vector2(1f,   1f);
                rt.pivot     = new Vector2(1f, 0.5f);
                rt.offsetMin = new Vector2(0f,        0f);
                rt.offsetMax = new Vector2(-paddingH, 0f);
            }
        }
        y -= headerHeight;

        // TopSection
        if (topSection != null)
        {
            topSection.anchorMin = new Vector2(0f, 1f);
            topSection.anchorMax = new Vector2(1f, 1f);
            topSection.pivot     = new Vector2(0.5f, 1f);
            topSection.offsetMin = new Vector2(0f, y - equipSectionH);
            topSection.offsetMax = new Vector2(0f, y);
        }
        y -= equipSectionH;

        // ItemListContainer
        if (itemListContainer != null)
        {
            itemListContainer.anchorMin = new Vector2(0f, 1f);
            itemListContainer.anchorMax = new Vector2(1f, 1f);
            itemListContainer.pivot     = new Vector2(0.5f, 1f);
            itemListContainer.offsetMin = new Vector2(0f, y - itemSectionH);
            itemListContainer.offsetMax = new Vector2(0f, y);

            if (bagInfoText != null)
            {
                RectTransform rt = bagInfoText.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot     = new Vector2(0.5f, 1f);
                rt.offsetMin = new Vector2(paddingH,  -paddingV - bagHeaderHeight);
                rt.offsetMax = new Vector2(-paddingH, -paddingV);
            }

            if (itemGrid != null)
            {
                RectTransform rt = itemGrid.GetComponent<RectTransform>();
                float gridW      = slotSize * GRID_COLUMNS + slotSpacing * (GRID_COLUMNS - 1);
                float gridY      = -(paddingV + bagHeaderHeight + slotSpacing);
                rt.anchorMin        = new Vector2(0.5f, 1f);
                rt.anchorMax        = new Vector2(0.5f, 1f);
                rt.pivot            = new Vector2(0.5f, 1f);
                rt.anchoredPosition = new Vector2(0f, gridY);
                rt.sizeDelta        = new Vector2(gridW, itemGridH);
            }
        }

        Debug.Log($"[InventoryUI] 레이아웃 확정 — 패널:{panelWidth}×{totalH} " +
                  $"(헤더:{headerHeight} 장비섹션:{equipSectionH} 아이템섹션:{itemSectionH})");
    }

    private float CalcEquipSectionHeight()
    {
        float gridH = slotSize * 3f + slotSpacing * 2f;
        return paddingV + 24f + 10f + gridH + paddingV;
    }

    private float CalcItemGridHeight()
    {
        int rows = Mathf.CeilToInt((float)MAX_SLOTS / GRID_COLUMNS); // 10
        return slotSize * rows + slotSpacing * (rows - 1);
    }

    private void ApplyGridLayout()
    {
        if (itemGrid == null) return;
        GridLayoutGroup g = itemGrid.GetComponent<GridLayoutGroup>();
        if (g == null) g  = itemGrid.gameObject.AddComponent<GridLayoutGroup>();

        g.cellSize        = new Vector2(slotSize, slotSize);
        g.spacing         = new Vector2(slotSpacing, slotSpacing);
        g.startCorner     = GridLayoutGroup.Corner.UpperLeft;
        g.startAxis       = GridLayoutGroup.Axis.Horizontal;
        g.childAlignment  = TextAnchor.UpperLeft;
        g.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
        g.constraintCount = GRID_COLUMNS;
    }

    // ────────────────────────────────────────────────────────
    private void RefreshIfVisible() { if (isVisible) Refresh(); }

    private void Refresh()
    {
        RefreshBagHeader();
        RefreshGrid();
        if (equipmentUI != null) equipmentUI.Refresh();
    }

    private void RefreshBagHeader()
    {
        if (bagInfoText == null) return;
        var inv = Inventory.Instance;
        if (inv == null) { bagInfoText.text = ""; return; }
        bagInfoText.text =
            $"슬롯 {inv.CurrentItemCount} / {inv.MaxItemCount}    " +
            $"무게 {inv.CurrentWeight:F1} / {inv.MaxWeight:F1} kg";
    }

    private void RefreshGrid()
    {
        foreach (var go in spawnedSlots)
            if (go != null) Destroy(go);
        spawnedSlots.Clear();

        if (itemGrid == null || itemSlotPrefab == null) return;

        var inv       = Inventory.Instance;
        var slots     = inv?.Slots;
        int openSlots = inv != null ? Mathf.Min(inv.MaxItemCount, MAX_SLOTS) : 0;

        for (int i = 0; i < MAX_SLOTS; i++)
        {
            bool          isOpen = i < openSlots;
            InventorySlot slot   = (isOpen && slots != null && i < slots.Count) ? slots[i] : null;
            GameObject    go     = Instantiate(itemSlotPrefab, itemGrid);
            // 잠긴 슬롯은 오브젝트 자체를 숨김
            go.SetActive(isOpen);
            if (isOpen) SetupSlot(go, slot, i, isOpen);
            spawnedSlots.Add(go);
        }
    }

    // ────────────────────────────────────────────────────────
    private void SetupSlot(GameObject go, InventorySlot slot, int index, bool isOpen)
    {
        Image bg = go.GetComponent<Image>();
        if (bg == null) bg = go.AddComponent<Image>();

        Image iconImg  = FindChildImage(go, "Icon");
        // 이름 매칭 우선, 실패 시 인덱스 순서로 fallback
        Text  qtyText  = FindChildText(go, "Quantity") ?? FindChildTextByIndex(go, 0);
        Text  nameText = FindChildText(go, "Name")     ?? FindChildTextByIndex(go, 1);

        bg.raycastTarget = true;

        if (slot == null)
        {
            bg.color = colorEmpty;
            SetActive(iconImg, false); SetActive(qtyText, false); SetActive(nameText, false);
            return;
        }

        bg.color = colorFilled;

        if (iconImg != null)
        {
            iconImg.gameObject.SetActive(true);
            if (slot.item.icon != null) { iconImg.sprite = slot.item.icon; iconImg.color = Color.white; }
            else                        { iconImg.sprite = null; iconImg.color = new Color(0.5f, 0.5f, 0.5f); }
        }
        if (qtyText != null)
        {
            bool show = slot.item.isStackable && slot.quantity > 1;
            Debug.Log($"[InvUI] {slot.item.itemName} isStackable={slot.item.isStackable} qty={slot.quantity} show={show} qtyObj={qtyText.gameObject.name}");
            qtyText.gameObject.SetActive(show);
            if (show) qtyText.text = slot.quantity.ToString();
        }
        if (nameText != null)
        {
            bool noIcon = slot.item.icon == null;
            nameText.gameObject.SetActive(noIcon);
            if (noIcon) { nameText.text = slot.item.itemName; nameText.fontSize = Mathf.RoundToInt(slotSize / 5f); }
        }

        int ci = index; InventorySlot cs = slot;
        RectTransform rt = go.GetComponent<RectTransform>();

        EventTrigger trigger = go.GetComponent<EventTrigger>() ?? go.AddComponent<EventTrigger>();
        trigger.triggers.Clear();

        var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enter.callback.AddListener(_ => { bg.color = colorHover; tooltipUI?.ShowForSlot(cs, rt); });
        trigger.triggers.Add(enter);

        var exitE = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exitE.callback.AddListener(_ => { bg.color = colorFilled; tooltipUI?.Hide(); });
        trigger.triggers.Add(exitE);

        var click = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
        click.callback.AddListener(evt =>
        {
            var pe = evt as PointerEventData;
            if (pe != null && pe.button == PointerEventData.InputButton.Right)
                OnRightClick(cs, ci);
        });
        trigger.triggers.Add(click);
    }

    private void OnRightClick(InventorySlot slot, int index)
    {
        if (slot == null) return;
        EquipData equip = slot.item as EquipData;
        // 장착 가능한 아이템만 처리, 그 외는 무응답
        if (equip == null) return;
        tooltipUI?.Hide();
        Inventory.Instance?.RemoveAt(index);
        EquipmentManager.Instance?.Equip(equip);
    }

    // ────────────────────────────────────────────────────────
    private Image FindChildImage(GameObject go, string n)
    {
        Transform t = go.transform.Find(n);
        if (t != null) return t.GetComponent<Image>();
        foreach (Image c in go.GetComponentsInChildren<Image>(true))
            if (c.gameObject.name == n) return c;
        return null;
    }

    private Text FindChildText(GameObject go, string n)
    {
        Transform t = go.transform.Find(n);
        if (t != null) return t.GetComponent<Text>();
        foreach (Text c in go.GetComponentsInChildren<Text>(true))
            if (c.gameObject.name == n) return c;
        return null;
    }

    /// <summary>이름 매칭 실패 시 인덱스 순서로 Text 자식 반환</summary>
    private Text FindChildTextByIndex(GameObject go, int index)
    {
        Text[] texts = go.GetComponentsInChildren<Text>(true);
        return index < texts.Length ? texts[index] : null;
    }

    private void SetActive(Component c, bool active)
    { if (c != null) c.gameObject.SetActive(active); }

    private void ShowImmediate()
    {
        if (canvasGroup == null) return;
        canvasGroup.alpha = 1f; canvasGroup.interactable = true; canvasGroup.blocksRaycasts = true;
    }

    private void HideImmediate()
    {
        if (canvasGroup == null) return;
        canvasGroup.alpha = 0f; canvasGroup.interactable = false; canvasGroup.blocksRaycasts = false;
    }
}

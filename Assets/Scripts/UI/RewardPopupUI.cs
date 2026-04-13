using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class RewardPopupUI : MonoBehaviour
{
    public static RewardPopupUI Instance { get; private set; }
    public enum PanelMode
    {
        Reward,
        Storage,
        Merchant
    }

    [Header("레이아웃")]
    public float panelWidth   = 280f;
    public float headerHeight = 44f;
    public float footerHeight = 36f;
    public float slotSize     = 58f;
    public float slotSpacing  = 4f;
    public float paddingH     = 14f;
    public float paddingV     = 14f;

    [Header("색상")]
    public Color colorPanelBg   = new Color(0.071f, 0.063f, 0.051f, 1f);
    public Color colorHeaderBg  = new Color(0.125f, 0.110f, 0.075f, 1f);
    public Color colorGridBg    = new Color(0.102f, 0.086f, 0.071f, 1f);
    public Color colorFooterBg  = new Color(0.071f, 0.063f, 0.051f, 1f);
    public Color colorSlotBg    = new Color(0.055f, 0.047f, 0.035f, 1f);
    public Color colorSlotEmpty = new Color(0.055f, 0.047f, 0.035f, 0.4f);
    public Color colorGold      = new Color(0.784f, 0.659f, 0.294f, 1f);
    public Color colorDim       = new Color(0.478f, 0.416f, 0.294f, 1f);

    private struct SlotState
    {
        public GameObject root;
        public Image icon;
        public Text count;
        public ItemData item;
        public int quantity;
    }

    private readonly List<SlotState> slots = new List<SlotState>();
    private RectTransform panelRT;
    private RectTransform gridRT;
    private Canvas popupCanvas;
    private GraphicRaycaster popupRaycaster;
    private Text titleText;
    private Text itemCountText;
    private Text hintText;
    private Button closeButton;
    private bool initialized;
    private int currentCols = 4;
    private int currentRows = 4;
    private PanelMode currentMode = PanelMode.Reward;
    private TownStorageManager storageManager;
    private TownStorageManager subscribedStorageManager;
    private MerchantInventoryManager merchantManager;
    private MerchantInventoryManager subscribedMerchantManager;
    private RectTransform headerRT;
    private bool isDragging;
    private int dragSourceIndex = -1;
    private Image dragIconImage;
    private RectTransform dragIconRect;
    private Canvas rootCanvas;

    private int SlotCount => Mathf.Max(1, currentCols * currentRows);

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        gameObject.SetActive(false);
    }

    private void Update()
    {
        if (currentMode == PanelMode.Storage) return;
        if (Input.GetKeyDown(KeyCode.Escape)) Close();
    }

    private void EnsureInitialized()
    {
        if (initialized) return;
        initialized = true;

        panelRT = GetComponent<RectTransform>() ?? gameObject.AddComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0.5f, 0.5f);
        panelRT.anchorMax = new Vector2(0.5f, 0.5f);
        panelRT.pivot = new Vector2(0.5f, 0.5f);

        if (!TryGetComponent(out popupCanvas))
            popupCanvas = gameObject.AddComponent<Canvas>();
        popupCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        popupCanvas.overrideSorting = true;
        popupCanvas.sortingOrder = 4700;

        if (!TryGetComponent(out popupRaycaster))
            popupRaycaster = gameObject.AddComponent<GraphicRaycaster>();

        rootCanvas = popupCanvas;
        SetBg(gameObject, colorPanelBg);
        RebuildPanel();
    }

    private void RebuildPanel()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
            DestroyImmediate(transform.GetChild(i).gameObject);
        slots.Clear();

        float gridH = paddingV + currentRows * slotSize + (currentRows - 1) * slotSpacing + paddingV;
        float totalH = headerHeight + gridH + footerHeight;
        float gridW = paddingH + currentCols * slotSize + (currentCols - 1) * slotSpacing + paddingH;
        float finalWidth = Mathf.Max(panelWidth, gridW);

        panelRT.sizeDelta = new Vector2(finalWidth, totalH);
        panelRT.anchoredPosition = Vector2.zero;

        GameObject header = MakeBlock("Header", panelRT, 0f, headerHeight, colorHeaderBg);
        headerRT = header.GetComponent<RectTransform>();
        titleText = MakeText(header, "TitleText",
            new Vector2(0f, 0f), new Vector2(0.75f, 1f),
            new Vector4(paddingH, 0f, 0f, 0f),
            "REWARD", 12, FontStyle.Bold, colorGold, TextAnchor.MiddleLeft);

        closeButton = MakeButton(header, "CloseButton",
            new Vector2(0.78f, 0.15f), new Vector2(1f, 0.85f),
            new Vector4(0f, 0f, paddingH, 0f),
            "CLOSE", 9, colorDim);
        closeButton.onClick.RemoveAllListeners();
        closeButton.onClick.AddListener(Close);

        GameObject gridSection = MakeBlock("GridSection", panelRT, headerHeight, gridH, colorGridBg);
        GameObject gridGo = new GameObject("RewardGrid", typeof(RectTransform), typeof(GridLayoutGroup));
        gridGo.transform.SetParent(gridSection.transform, false);
        gridRT = gridGo.GetComponent<RectTransform>();
        gridRT.anchorMin = Vector2.zero;
        gridRT.anchorMax = Vector2.one;
        gridRT.offsetMin = Vector2.zero;
        gridRT.offsetMax = Vector2.zero;

        GridLayoutGroup g = gridGo.GetComponent<GridLayoutGroup>();
        g.cellSize = new Vector2(slotSize, slotSize);
        g.spacing = new Vector2(slotSpacing, slotSpacing);
        g.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        g.constraintCount = currentCols;
        g.padding = new RectOffset((int)paddingH, (int)paddingH, (int)paddingV, (int)paddingV);
        g.childAlignment = TextAnchor.UpperLeft;

        BuildSlots(gridRT, SlotCount);

        GameObject footer = MakeBlock("Footer", panelRT, headerHeight + gridH, footerHeight, colorFooterBg);
        hintText = MakeText(footer, "HintText",
            new Vector2(0f, 0f), new Vector2(0.7f, 1f),
            new Vector4(paddingH, 0f, 0f, 0f),
            "우클릭 — 가방으로 이동", 10, FontStyle.Italic, colorDim, TextAnchor.MiddleLeft);

        itemCountText = MakeText(footer, "ItemCountText",
            new Vector2(0.7f, 0f), new Vector2(1f, 1f),
            new Vector4(0f, 0f, paddingH, 0f),
            $"0 / {SlotCount}", 10, FontStyle.Normal, colorDim, TextAnchor.MiddleRight);
    }

    private void BuildSlots(RectTransform parent, int count)
    {
        for (int i = 0; i < count; i++)
        {
            GameObject go = new GameObject($"RewardSlot_{i}", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(slotSize, slotSize);

            Image bg = go.GetComponent<Image>();
            bg.color = colorSlotEmpty;

            Button btn = go.GetComponent<Button>();
            btn.transition = Selectable.Transition.None;

            GameObject iconGo = new GameObject("IconImage", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(go.transform, false);
            RectTransform iconRT = iconGo.GetComponent<RectTransform>();
            iconRT.anchorMin = new Vector2(0.08f, 0.08f);
            iconRT.anchorMax = new Vector2(0.92f, 0.92f);
            iconRT.offsetMin = Vector2.zero;
            iconRT.offsetMax = Vector2.zero;
            Image iconImg = iconGo.GetComponent<Image>();
            iconImg.preserveAspect = true;
            iconImg.enabled = false;

            GameObject countGo = new GameObject("CountText", typeof(RectTransform), typeof(Text));
            countGo.transform.SetParent(go.transform, false);
            RectTransform countRT = countGo.GetComponent<RectTransform>();
            countRT.anchorMin = new Vector2(0f, 0f);
            countRT.anchorMax = new Vector2(1f, 0.38f);
            countRT.offsetMin = new Vector2(2f, 2f);
            countRT.offsetMax = new Vector2(-2f, 0f);
            Text countTxt = countGo.GetComponent<Text>();
            countTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            countTxt.fontSize = 10;
            countTxt.color = colorGold;
            countTxt.alignment = TextAnchor.LowerRight;
            countTxt.enabled = false;

            SlotState state = new SlotState { root = go, icon = iconImg, count = countTxt };
            slots.Add(state);

            int idx = i;
            AddStorageSlotDragHandlers(go, idx);
            AddRightClickHandler(go, () => OnSlotRightClick(idx));
            AddHoverHandler(go, idx);
        }
    }

    public void Show(string displayName, List<RewardItemEntry> items)
    {
        UnsubscribeStorageEvents();
        currentMode = PanelMode.Reward;
        currentCols = 4;
        currentRows = 4;
        storageManager = null;

        EnsureInitialized();
        RebuildPanel();
        gameObject.SetActive(true);
        transform.SetAsLastSibling();

        if (titleText != null)
            titleText.text = string.IsNullOrEmpty(displayName) ? "REWARD" : displayName.ToUpper();
        if (hintText != null)
            hintText.text = "우클릭 — 가방으로 이동";
        if (closeButton != null)
            closeButton.gameObject.SetActive(true);

        ClearSlots();
        for (int i = 0; i < items.Count && i < slots.Count; i++)
            SetItem(i, items[i].item, items[i].value);

        UpdateFooterCount();
    }

    public void ShowStorage(string header, TownStorageManager manager, int columns, int rows, bool hideCloseButton)
    {
        UnsubscribeStorageEvents();
        UnsubscribeMerchantEvents();
        currentMode = PanelMode.Storage;
        currentCols = Mathf.Max(1, columns);
        currentRows = Mathf.Max(1, rows);
        storageManager = manager;
        subscribedStorageManager = manager;
        if (subscribedStorageManager != null)
            subscribedStorageManager.OnStorageChanged += RefreshStorageView;

        EnsureInitialized();
        RebuildPanel();
        gameObject.SetActive(true);
        transform.SetAsLastSibling();

        if (titleText != null)
            titleText.text = string.IsNullOrEmpty(header) ? "보관함" : header;
        if (hintText != null)
            hintText.text = "우클릭 — 가방으로 이동";
        if (closeButton != null)
            closeButton.gameObject.SetActive(!hideCloseButton);

        RefreshStorageView();
    }

    public void ShowMerchant(string header, MerchantInventoryManager manager, int columns, int rows, bool hideCloseButton)
    {
        UnsubscribeStorageEvents();
        UnsubscribeMerchantEvents();
        currentMode = PanelMode.Merchant;
        currentCols = Mathf.Max(1, columns);
        currentRows = Mathf.Max(1, rows);
        merchantManager = manager;
        subscribedMerchantManager = manager;
        if (subscribedMerchantManager != null)
            subscribedMerchantManager.OnMerchantInventoryChanged += RefreshMerchantView;

        EnsureInitialized();
        RebuildPanel();
        gameObject.SetActive(true);
        transform.SetAsLastSibling();

        if (titleText != null)
            titleText.text = string.IsNullOrEmpty(header) ? "상점" : header;
        if (hintText != null)
            hintText.text = "구매/판매 [우클릭]";
        if (closeButton != null)
            closeButton.gameObject.SetActive(!hideCloseButton);

        RefreshMerchantView();
    }

    public void RefreshStorageView()
    {
        if (currentMode != PanelMode.Storage) return;

        ClearSlots();
        if (storageManager != null)
        {
            var entries = storageManager.Slots;
            for (int i = 0; i < entries.Count && i < slots.Count; i++)
                SetItem(i, entries[i].item, entries[i].quantity);
        }
        UpdateFooterCount();
    }

    public void RefreshMerchantView()
    {
        if (currentMode != PanelMode.Merchant) return;

        ClearSlots();
        if (merchantManager != null)
        {
            var entries = merchantManager.Slots;
            for (int i = 0; i < entries.Count && i < slots.Count; i++)
                SetItem(i, entries[i].item, entries[i].quantity);
        }
        UpdateFooterCount();
    }

    public void Close()
    {
        UnsubscribeStorageEvents();
        UnsubscribeMerchantEvents();
        CancelStorageDrag();
        ItemTooltipUI.Instance?.Hide();
        ClearSlots();
        gameObject.SetActive(false);
    }

    public bool CanAcceptInventoryDrag(PointerEventData eventData)
    {
        return currentMode == PanelMode.Storage && gameObject.activeInHierarchy && GetStorageDropSlotIndex(eventData) >= 0;
    }

    public bool TryStoreFromInventoryDrag(int inventoryIndex, PointerEventData eventData)
    {
        if (currentMode != PanelMode.Storage || storageManager == null || Inventory.Instance == null) return false;
        if (inventoryIndex < 0 || inventoryIndex >= Inventory.Instance.CurrentItemCount) return false;
        if (GetStorageDropSlotIndex(eventData) < 0) return false;

        InventorySlot slot = Inventory.Instance.Slots[inventoryIndex];
        if (slot == null || slot.item == null || slot.quantity <= 0) return false;

        if (!storageManager.TryStore(slot.item, slot.quantity))
        {
            FloatingTextUI.Instance?.Show("보관함이 가득 찼습니다.", FloatingTextUI.ColorFail);
            return true;
        }

        ItemTooltipUI.Instance?.Hide();
        FloatingTextUI.Instance?.Show($"{slot.item.itemName} ×{slot.quantity} 보관", FloatingTextUI.ColorAcquire);
        Inventory.Instance.RemoveAt(inventoryIndex);
        RefreshStorageView();
        return true;
    }

    private void SetItem(int index, ItemData item, int qty)
    {
        if (index < 0 || index >= slots.Count) return;
        SlotState s = slots[index];
        s.item = item;
        s.quantity = qty;
        SetSlotBg(s.root, colorSlotBg);
        if (s.icon != null)
        {
            s.icon.sprite = item != null ? item.icon : null;
            s.icon.color = Color.white;
            s.icon.enabled = item != null;
        }
        if (s.count != null)
        {
            s.count.text = qty > 1 ? qty.ToString() : "";
            s.count.enabled = qty > 1;
        }
        slots[index] = s;
    }

    private void SetEmpty(int index)
    {
        if (index < 0 || index >= slots.Count) return;
        SlotState s = slots[index];
        s.item = null;
        s.quantity = 0;
        SetSlotBg(s.root, colorSlotEmpty);
        if (s.icon != null) s.icon.enabled = false;
        if (s.count != null) s.count.enabled = false;
        slots[index] = s;
    }

    private void ClearSlots()
    {
        for (int i = 0; i < slots.Count; i++) SetEmpty(i);
    }

    private void SetSlotBg(GameObject go, Color color)
    {
        if (go == null) return;
        Image img = go.GetComponent<Image>();
        if (img != null) img.color = color;
    }

    private int GetStorageDropSlotIndex(PointerEventData eventData)
    {
        if (eventData == null || currentMode != PanelMode.Storage) return -1;

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current?.RaycastAll(eventData, results);
        foreach (var r in results)
        {
            if (r.gameObject == null) continue;
            int raycastIndex = GetStorageSlotIndexFromObject(r.gameObject);
            if (raycastIndex >= 0)
                return raycastIndex;
        }

        if (gridRT == null) return -1;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                gridRT,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 local))
        {
            float width = gridRT.rect.width;
            float ox = local.x + width * 0.5f - paddingH;
            float oy = -local.y - paddingV;

            float pitchX = slotSize + slotSpacing;
            float pitchY = slotSize + slotSpacing;

            int col = Mathf.FloorToInt(ox / pitchX);
            int row = Mathf.FloorToInt(oy / pitchY);
            if (col >= 0 && col < currentCols && row >= 0 && row < currentRows)
            {
                int index = row * currentCols + col;
                if (index >= 0 && index < SlotCount)
                    return index;
            }
        }

        return -1;
    }

    private int GetStorageSlotIndexFromObject(GameObject go)
    {
        if (go == null) return -1;

        Transform t = go.transform;
        while (t != null)
        {
            string n = t.gameObject.name;
            if (!string.IsNullOrEmpty(n) && n.StartsWith("RewardSlot_"))
            {
                string suffix = n.Substring("RewardSlot_".Length);
                if (int.TryParse(suffix, out int index) && index >= 0 && index < SlotCount)
                    return index;
            }
            t = t.parent;
        }

        return -1;
    }

    private void AddHoverHandler(GameObject go, int idx)
    {
        EventTrigger trigger = go.GetComponent<EventTrigger>() ?? go.AddComponent<EventTrigger>();

        var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enter.callback.AddListener(_ =>
        {
            if (idx < 0 || idx >= slots.Count || slots[idx].item == null) return;
            RectTransform rt = slots[idx].root.GetComponent<RectTransform>();
            int priceMultiplier = currentMode == PanelMode.Merchant ? 2 : 1;
            ItemTooltipUI.Instance?.ShowForItem(slots[idx].item, slots[idx].quantity, rt, priceMultiplier);
        });
        trigger.triggers.Add(enter);

        var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exit.callback.AddListener(_ => ItemTooltipUI.Instance?.Hide());
        trigger.triggers.Add(exit);
    }

    private void OnSlotRightClick(int idx)
    {
        if (idx < 0 || idx >= slots.Count || slots[idx].item == null) return;
        ItemData item = slots[idx].item;
        int qty = slots[idx].quantity;

        if (Inventory.Instance == null)
        {
            Debug.LogWarning("[RewardPopupUI] Inventory null");
            return;
        }

        if (currentMode == PanelMode.Merchant && merchantManager != null)
        {
            int buyPrice = item.price * 2 * Mathf.Max(1, qty);
            if (GoldManager.Instance == null || !GoldManager.Instance.TrySpendGold(buyPrice))
            {
                FloatingTextUI.Instance?.Show("골드가 부족합니다.", FloatingTextUI.ColorFail);
                return;
            }

            switch (Inventory.Instance.AddItem(item, qty))
            {
                case AddItemResult.Success:
                    merchantManager.RemoveAt(idx);
                    FloatingTextUI.Instance?.Show($"{item.itemName} ×{qty} 구매", FloatingTextUI.ColorAcquire);
                    break;
                case AddItemResult.FailSlotFull:
                    GoldManager.Instance?.AddGold(buyPrice);
                    FloatingTextUI.Instance?.Show("인벤토리가 가득 찼습니다.", FloatingTextUI.ColorFail);
                    break;
                case AddItemResult.FailTooHeavy:
                    GoldManager.Instance?.AddGold(buyPrice);
                    FloatingTextUI.Instance?.Show("너무 무겁습니다.", FloatingTextUI.ColorFail);
                    break;
            }
            return;
        }

        switch (Inventory.Instance.AddItem(item, qty))
        {
            case AddItemResult.Success:
                if (currentMode == PanelMode.Storage && storageManager != null)
                {
                    storageManager.RemoveAt(idx);
                    RefreshStorageView();
                }
                else
                {
                    SetEmpty(idx);
                    UpdateFooterCount();
                    if (GetFilledCount() == 0) Close();
                }
                FloatingTextUI.Instance?.Show($"{item.itemName} ×{qty} 획득", FloatingTextUI.ColorAcquire);
                break;
            case AddItemResult.FailSlotFull:
                FloatingTextUI.Instance?.Show("인벤토리가 가득 찼습니다.", FloatingTextUI.ColorFail);
                break;
            case AddItemResult.FailTooHeavy:
                FloatingTextUI.Instance?.Show("너무 무겁습니다.", FloatingTextUI.ColorFail);
                break;
        }
    }

    private int GetFilledCount()
    {
        int filled = 0;
        for (int i = 0; i < slots.Count; i++)
            if (slots[i].item != null) filled++;
        return filled;
    }

    private void UpdateFooterCount()
    {
        if (itemCountText == null) return;
        itemCountText.text = $"{GetFilledCount()} / {slots.Count}";
    }

    private void BeginStorageDrag(int slotIndex, PointerEventData eventData)
    {
        if (currentMode != PanelMode.Storage) return;
        if (slotIndex < 0 || slotIndex >= slots.Count) return;
        if (slots[slotIndex].item == null) return;

        isDragging = true;
        dragSourceIndex = slotIndex;
        ItemTooltipUI.Instance?.Hide();
        EnsureDragIcon();

        if (dragIconImage != null)
        {
            dragIconImage.enabled = true;
            dragIconImage.sprite = slots[slotIndex].item.icon;
            dragIconImage.color = slots[slotIndex].item.icon != null ? Color.white : new Color(1f, 1f, 1f, 0.6f);
        }

        UpdateStorageDrag(eventData);
    }

    private void UpdateStorageDrag(PointerEventData eventData)
    {
        if (!isDragging || dragIconRect == null || rootCanvas == null) return;

        RectTransform canvasRect = rootCanvas.transform as RectTransform;
        if (canvasRect == null) return;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, eventData.position, eventData.pressEventCamera, out Vector2 localPos))
            dragIconRect.anchoredPosition = localPos;
    }

    private void EndStorageDrag(PointerEventData eventData)
    {
        if (!isDragging) return;

        bool handled = false;
        if (InventoryUI.Instance != null)
            handled = InventoryUI.Instance.TryHandleStorageDrop(dragSourceIndex, eventData);

        if (!handled)
            CancelStorageDrag();
        else
        {
            isDragging = false;
            dragSourceIndex = -1;
            FinishStorageDragVisual();
        }
    }

    private void CancelStorageDrag()
    {
        isDragging = false;
        dragSourceIndex = -1;
        FinishStorageDragVisual();
    }

    private void FinishStorageDragVisual()
    {
        if (dragIconImage != null)
        {
            dragIconImage.enabled = false;
            dragIconImage.sprite = null;
        }
    }

    private GameObject MakeBlock(string name, RectTransform parent, float offsetFromTop, float height, Color bgColor)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -offsetFromTop);
        rt.sizeDelta = new Vector2(0f, height);
        SetBg(go, bgColor);
        return go;
    }

    private Text MakeText(GameObject parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector4 offset, string text, int size, FontStyle style, Color color, TextAnchor alignment)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent.transform, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = new Vector2(offset.x, offset.y);
        rt.offsetMax = new Vector2(-offset.z, -offset.w);
        Text t = go.GetComponent<Text>();
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.text = text;
        t.fontSize = size;
        t.fontStyle = style;
        t.color = color;
        t.alignment = alignment;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        return t;
    }

    private Button MakeButton(GameObject parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector4 offset, string label, int fontSize, Color labelColor)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent.transform, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = new Vector2(offset.x, offset.y);
        rt.offsetMax = new Vector2(-offset.z, -offset.w);
        SetBg(go, new Color(0.1f, 0.09f, 0.06f, 1f));
        Button btn = go.GetComponent<Button>();
        btn.transition = Selectable.Transition.ColorTint;
        MakeText(go, "Text", Vector2.zero, Vector2.one, Vector4.zero, label, fontSize, FontStyle.Normal, labelColor, TextAnchor.MiddleCenter);
        return btn;
    }

    private void SetBg(GameObject go, Color color)
    {
        Image img = go.GetComponent<Image>() ?? go.AddComponent<Image>();
        img.type = Image.Type.Simple;
        img.sprite = null;
        img.color = color;
    }

    private void AddStorageSlotDragHandlers(GameObject go, int idx)
    {
        EventTrigger trigger = go.GetComponent<EventTrigger>() ?? go.AddComponent<EventTrigger>();

        var beginDrag = new EventTrigger.Entry { eventID = EventTriggerType.BeginDrag };
        beginDrag.callback.AddListener(evt =>
        {
            var pe = evt as PointerEventData;
            if (pe != null && pe.button == PointerEventData.InputButton.Left)
                BeginStorageDrag(idx, pe);
        });
        trigger.triggers.Add(beginDrag);

        var drag = new EventTrigger.Entry { eventID = EventTriggerType.Drag };
        drag.callback.AddListener(evt =>
        {
            var pe = evt as PointerEventData;
            if (pe != null)
                UpdateStorageDrag(pe);
        });
        trigger.triggers.Add(drag);

        var endDrag = new EventTrigger.Entry { eventID = EventTriggerType.EndDrag };
        endDrag.callback.AddListener(evt =>
        {
            var pe = evt as PointerEventData;
            if (pe != null)
                EndStorageDrag(pe);
        });
        trigger.triggers.Add(endDrag);
    }

    private void AddRightClickHandler(GameObject go, System.Action onRightClick)
    {
        EventTrigger trigger = go.GetComponent<EventTrigger>() ?? go.AddComponent<EventTrigger>();
        var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
        entry.callback.AddListener(data =>
        {
            if (((PointerEventData)data).button == PointerEventData.InputButton.Right)
                onRightClick?.Invoke();
        });
        trigger.triggers.Add(entry);
    }

    private void EnsureDragIcon()
    {
        if (dragIconImage != null) return;
        if (rootCanvas == null) rootCanvas = GetComponentInParent<Canvas>();
        if (rootCanvas == null) return;

        GameObject go = new GameObject("DraggedStorageItemIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(rootCanvas.transform, false);

        dragIconRect = go.GetComponent<RectTransform>();
        dragIconRect.sizeDelta = new Vector2(slotSize, slotSize);

        dragIconImage = go.GetComponent<Image>();
        dragIconImage.raycastTarget = false;
        dragIconImage.preserveAspect = true;
        dragIconImage.enabled = false;
    }

    private void UnsubscribeStorageEvents()
    {
        if (subscribedStorageManager != null)
        {
            subscribedStorageManager.OnStorageChanged -= RefreshStorageView;
            subscribedStorageManager = null;
        }
    }

    private void UnsubscribeMerchantEvents()
    {
        if (subscribedMerchantManager != null)
        {
            subscribedMerchantManager.OnMerchantInventoryChanged -= RefreshMerchantView;
            subscribedMerchantManager = null;
        }
    }
}

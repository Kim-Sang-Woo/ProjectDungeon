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
    public Color colorLocked = new Color(0.035f, 0.031f, 0.027f, 1f);
    public Color colorDragTarget = new Color(0.25f, 0.36f, 0.15f, 1f);

    [Header("연동")]
    public EquipmentUI   equipmentUI;
    public ItemTooltipUI tooltipUI;

    private const int GRID_COLUMNS = 4;
    private const int MAX_SLOTS    = 40; // 4×10
    private const string DRAG_BUILD_TAG = "INV_DRAG_e7d0ee3";

    private List<GameObject> spawnedSlots = new List<GameObject>();
    private Dictionary<int, Image> slotBackgroundByIndex = new Dictionary<int, Image>();
    private bool             isVisible    = false;

    // 드래그 정렬 상태
    private bool           isDragging;
    private int            dragSourceIndex = -1;
    private int            dragTargetIndex = -1;
    private Image          dragIconImage;
    private RectTransform  dragIconRect;
    private Canvas         rootCanvas;

    /// <summary>가방 열림/닫힘 시 발신 — true=열림, false=닫힘</summary>
    public event System.Action<bool> OnInventoryToggled;

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

        rootCanvas = GetComponentInParent<Canvas>();
        Debug.Log($"[InventoryUI] Drag build: {DRAG_BUILD_TAG}");
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
        UpdateHintText();
        OnInventoryToggled?.Invoke(true);
    }

    public void Hide()
    {
        isVisible = false;
        isDragging = false;
        dragSourceIndex = -1;
        ClearDragTargetHighlight();

        HideImmediate();
        if (tooltipUI != null) tooltipUI.Hide();
        if (dragIconImage != null)
        {
            dragIconImage.enabled = false;
            dragIconImage.sprite = null;
        }

        UpdateHintText();
        OnInventoryToggled?.Invoke(false);
    }

    private void UpdateHintText()
    {
        if (hintKeyText == null) return;
        if (isVisible)
            hintKeyText.text = "가방 닫기 [Tab]　　장비 장착/해제 [우클릭]　　아이템 버리기 [Ctrl+우클릭]";
        else
            hintKeyText.text = "가방 [Tab]";
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
        slotBackgroundByIndex.Clear();
        dragTargetIndex = -1;

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
        slotBackgroundByIndex[index] = bg;

        InventorySlotView slotView = go.GetComponent<InventorySlotView>() ?? go.AddComponent<InventorySlotView>();
        slotView.slotIndex = index;
        slotView.hasItem   = slot != null;
        slotView.isOpen    = isOpen;

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
        enter.callback.AddListener(_ =>
        {
            if (!isDragging)
            {
                bg.color = colorHover;
                tooltipUI?.ShowForSlot(cs, rt);
            }
        });
        trigger.triggers.Add(enter);

        var exitE = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exitE.callback.AddListener(_ =>
        {
            if (!isDragging)
            {
                bg.color = colorFilled;
                tooltipUI?.Hide();
            }
        });
        trigger.triggers.Add(exitE);

        var beginDrag = new EventTrigger.Entry { eventID = EventTriggerType.BeginDrag };
        beginDrag.callback.AddListener(evt =>
        {
            var pe = evt as PointerEventData;
            if (pe != null && pe.button == PointerEventData.InputButton.Left)
                BeginItemDrag(ci, cs, pe);
        });
        trigger.triggers.Add(beginDrag);

        var drag = new EventTrigger.Entry { eventID = EventTriggerType.Drag };
        drag.callback.AddListener(evt =>
        {
            var pe = evt as PointerEventData;
            if (pe != null) UpdateItemDrag(pe);
        });
        trigger.triggers.Add(drag);

        var endDrag = new EventTrigger.Entry { eventID = EventTriggerType.EndDrag };
        endDrag.callback.AddListener(evt =>
        {
            var pe = evt as PointerEventData;
            EndItemDrag(pe);
        });
        trigger.triggers.Add(endDrag);

        var click = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
        click.callback.AddListener(evt =>
        {
            var pe = evt as PointerEventData;
            if (pe != null && pe.button == PointerEventData.InputButton.Right)
            {
                if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                    OnCtrlRightClick(cs, ci);
                else
                    OnRightClick(cs, ci);
            }
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

    private void OnCtrlRightClick(InventorySlot slot, int index)
    {
        if (slot == null) return;
        tooltipUI?.Hide();
        Inventory.Instance?.RemoveAt(index);
        Debug.Log($"[InventoryUI] 아이템 버리기: {slot.item.itemName}");
    }

    private void BeginItemDrag(int slotIndex, InventorySlot slot, PointerEventData eventData)
    {
        if (slot == null || slot.item == null) return;

        isDragging      = true;
        dragSourceIndex = slotIndex;
        dragTargetIndex = -1;
        tooltipUI?.Hide();

        EnsureDragIcon();
        if (dragIconImage != null)
        {
            dragIconImage.enabled = true;
            dragIconImage.sprite  = slot.item.icon;
            dragIconImage.color   = slot.item.icon != null ? Color.white : new Color(1f, 1f, 1f, 0.6f);
        }

        UpdateItemDrag(eventData);
    }

    private void UpdateItemDrag(PointerEventData eventData)
    {
        if (!isDragging || dragIconRect == null || rootCanvas == null) return;

        RectTransform canvasRect = rootCanvas.transform as RectTransform;
        if (canvasRect == null) return;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 localPos))
        {
            dragIconRect.anchoredPosition = localPos;
        }

        UpdateDragTargetHighlight(eventData);
    }

    private void EndItemDrag(PointerEventData eventData)
    {
        if (!isDragging) return;

        bool handled = TryDropToEquipSlot(eventData);

        if (!handled)
        {
            // 마지막 드래그 프레임에서 계산된 타겟 우선 사용
            int targetIndex = dragTargetIndex >= 0 ? dragTargetIndex : GetDropTargetIndex(eventData);

            if (targetIndex >= 0)
            {
                Inventory.Instance?.MoveSlot(dragSourceIndex, targetIndex);
            }
        }

        isDragging      = false;
        dragSourceIndex = -1;
        ClearDragTargetHighlight();

        if (dragIconImage != null)
        {
            dragIconImage.enabled = false;
            dragIconImage.sprite  = null;
        }
    }

    private bool TryDropToEquipSlot(PointerEventData eventData)
    {
        if (eventData == null) return false;
        Inventory inv = Inventory.Instance;
        if (inv == null || dragSourceIndex < 0 || dragSourceIndex >= inv.CurrentItemCount) return false;

        InventorySlot srcSlot = inv.Slots[dragSourceIndex];
        EquipData dragEquip = srcSlot?.item as EquipData;
        if (dragEquip == null) return false;

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current?.RaycastAll(eventData, results);

        foreach (var r in results)
        {
            if (r.gameObject == null) continue;
            EquipmentSlotView equipView = r.gameObject.GetComponentInParent<EquipmentSlotView>();
            if (equipView == null) continue;

            if (dragEquip.equipType != equipView.slotType)
            {
                // 타입이 다른 장비 슬롯에는 장착하지 않음
                return true;
            }

            inv.RemoveAt(dragSourceIndex);
            EquipmentManager.Instance?.Equip(dragEquip);
            return true;
        }

        return false;
    }

    private int GetDropTargetIndex(PointerEventData eventData)
    {
        if (eventData == null || itemGrid == null) return -1;

        // 1) 우선 Grid 좌표 기반으로 인덱스 계산 (좌↔우 드래그 오차 방지)
        RectTransform gridRT = itemGrid as RectTransform;
        if (gridRT != null)
        {
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    gridRT,
                    eventData.position,
                    eventData.pressEventCamera,
                    out Vector2 local))
            {
                float width = gridRT.rect.width;
                float height = gridRT.rect.height;

                // pivot(0.5, 1) 기준 local -> 좌상단 원점 좌표로 변환
                float ox = local.x + width * 0.5f;
                float oy = -local.y;

                float pitchX = slotSize + slotSpacing;
                float pitchY = slotSize + slotSpacing;

                int col = Mathf.FloorToInt(ox / pitchX);
                int row = Mathf.FloorToInt(oy / pitchY);

                int openSlots = Inventory.Instance != null ? Mathf.Min(Inventory.Instance.MaxItemCount, MAX_SLOTS) : 0;
                int maxRows = Mathf.CeilToInt((float)MAX_SLOTS / GRID_COLUMNS);

                if (col >= 0 && col < GRID_COLUMNS && row >= 0 && row < maxRows)
                {
                    int index = row * GRID_COLUMNS + col;
                    if (index >= 0 && index < openSlots)
                        return index;
                }
            }
        }

        // 2) 보조: 레이캐스트 결과 기반
        if (eventData.pointerCurrentRaycast.gameObject != null)
        {
            InventorySlotView currentView =
                eventData.pointerCurrentRaycast.gameObject.GetComponentInParent<InventorySlotView>();
            if (currentView != null && currentView.isOpen)
                return currentView.slotIndex;
        }

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current?.RaycastAll(eventData, results);

        foreach (var r in results)
        {
            if (r.gameObject == null) continue;
            InventorySlotView view = r.gameObject.GetComponentInParent<InventorySlotView>();
            if (view != null && view.isOpen)
                return view.slotIndex;
        }

        return -1;
    }

    private void UpdateDragTargetHighlight(PointerEventData eventData)
    {
        int candidate = GetDropTargetIndex(eventData);
        if (candidate == dragSourceIndex) candidate = -1;

        if (candidate == dragTargetIndex) return;

        ClearDragTargetHighlight();

        dragTargetIndex = candidate;
        if (dragTargetIndex >= 0 && slotBackgroundByIndex.TryGetValue(dragTargetIndex, out Image bg) && bg != null)
        {
            bg.color = colorDragTarget;
        }
    }

    private void ClearDragTargetHighlight()
    {
        if (dragTargetIndex >= 0 && slotBackgroundByIndex.TryGetValue(dragTargetIndex, out Image oldBg) && oldBg != null)
        {
            oldBg.color = GetSlotBaseColor(dragTargetIndex);
        }
        dragTargetIndex = -1;
    }

    private Color GetSlotBaseColor(int slotIndex)
    {
        Inventory inv = Inventory.Instance;
        if (inv != null && slotIndex < inv.CurrentItemCount) return colorFilled;
        return colorEmpty;
    }

    private void EnsureDragIcon()
    {
        if (dragIconImage != null) return;

        if (rootCanvas == null) rootCanvas = GetComponentInParent<Canvas>();
        if (rootCanvas == null) return;

        GameObject go = new GameObject("DraggedItemIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(rootCanvas.transform, false);

        dragIconRect = go.GetComponent<RectTransform>();
        dragIconRect.sizeDelta = new Vector2(slotSize, slotSize);

        dragIconImage = go.GetComponent<Image>();
        dragIconImage.raycastTarget = false;
        dragIconImage.preserveAspect = true;
        dragIconImage.enabled = false;
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

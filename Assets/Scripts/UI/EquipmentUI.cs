// ============================================================
// EquipmentUI.cs — 장비 슬롯 UI v3.2
// 위치: Assets/Scripts/UI/EquipmentUI.cs
// ============================================================
// [v3.2 변경사항] 큰 프리셋 적용 (InventoryUI v4.2와 동일)
//   slotSize    72 → 80
//   slotSpacing  5 → 6
//   paddingH/V  18 → 20
// ============================================================
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class EquipmentUI : MonoBehaviour
{
    public static EquipmentUI Instance { get; private set; }

    [Header("레이아웃 대상")]
    public RectTransform sectionLabel;

    [Header("그리드")]
    public Transform  equipGrid;
    public GameObject equipSlotPrefab;

    [Header("레이아웃 수치")]
    public float slotSize    = 80f;
    public float slotSpacing = 6f;
    public float paddingH    = 20f;
    public float paddingV    = 20f;
    public float labelHeight = 24f;
    public float labelGap    = 10f;

    [Header("색상")]
    public Color colorEmpty    = new Color(0.055f, 0.047f, 0.035f, 1f);
    public Color colorEquipped = new Color(0.125f, 0.110f, 0.075f, 1f);
    public Color colorHover    = new Color(0.180f, 0.149f, 0.094f, 1f);

    [Header("연동")]
    public ItemTooltipUI tooltipUI;

    private static readonly EquipType?[] SLOT_ORDER = new EquipType?[]
    {
        EquipType.Weapon,  EquipType.Helmet,  EquipType.Necklace,
        EquipType.Amulet,  EquipType.Armor,   EquipType.Ring,
        EquipType.Gloves,  EquipType.Boots,   EquipType.Bag,
    };

    private List<GameObject> spawnedSlots = new List<GameObject>();

    // 드래그 상태 (장비 -> 인벤토리)
    private bool isDragging;
    private EquipType dragSourceType;
    private Image dragIconImage;
    private RectTransform dragIconRect;
    private Canvas rootCanvas;

    // ────────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (EquipmentManager.Instance != null)
            EquipmentManager.Instance.OnEquipChanged += Refresh;

        ApplyLayout();
        ApplyGridLayout();
        rootCanvas = GetComponentInParent<Canvas>();
        Refresh();
    }

    private void OnDestroy()
    {
        if (EquipmentManager.Instance != null)
            EquipmentManager.Instance.OnEquipChanged -= Refresh;
    }

    // ────────────────────────────────────────────────────────
    public void ApplyLayout()
    {
        float gridSize = slotSize * 3f + slotSpacing * 2f;

        if (sectionLabel != null)
        {
            sectionLabel.anchorMin = new Vector2(0f, 1f);
            sectionLabel.anchorMax = new Vector2(1f, 1f);
            sectionLabel.pivot     = new Vector2(0.5f, 1f);
            sectionLabel.offsetMin = new Vector2(paddingH,  -(paddingV + labelHeight));
            sectionLabel.offsetMax = new Vector2(-paddingH, -paddingV);
        }

        if (equipGrid != null)
        {
            RectTransform rt = equipGrid.GetComponent<RectTransform>();
            if (rt != null)
            {
                float gridTopY      = -(paddingV + labelHeight + labelGap);
                rt.anchorMin        = new Vector2(0.5f, 1f);
                rt.anchorMax        = new Vector2(0.5f, 1f);
                rt.pivot            = new Vector2(0.5f, 1f);
                rt.anchoredPosition = new Vector2(0f, gridTopY);
                rt.sizeDelta        = new Vector2(gridSize, gridSize);
            }
        }

        Debug.Log($"[EquipmentUI] ApplyLayout 완료 — gridSize:{slotSize * 3f + slotSpacing * 2f}");
    }

    private void ApplyGridLayout()
    {
        if (equipGrid == null) return;
        GridLayoutGroup g = equipGrid.GetComponent<GridLayoutGroup>();
        if (g == null) g  = equipGrid.gameObject.AddComponent<GridLayoutGroup>();

        g.cellSize        = new Vector2(slotSize, slotSize);
        g.spacing         = new Vector2(slotSpacing, slotSpacing);
        g.startCorner     = GridLayoutGroup.Corner.UpperLeft;
        g.startAxis       = GridLayoutGroup.Axis.Horizontal;
        g.childAlignment  = TextAnchor.UpperLeft;
        g.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
        g.constraintCount = 3;
    }

    // ────────────────────────────────────────────────────────
    public void Refresh()
    {
        foreach (var go in spawnedSlots)
            if (go != null) Destroy(go);
        spawnedSlots.Clear();

        if (equipGrid == null || equipSlotPrefab == null) return;

        foreach (var slotType in SLOT_ORDER)
        {
            GameObject go = Instantiate(equipSlotPrefab, equipGrid);
            go.SetActive(true);
            SetupSlot(go, slotType);
            spawnedSlots.Add(go);
        }
    }

    // ────────────────────────────────────────────────────────
    private void SetupSlot(GameObject go, EquipType? slotType)
    {
        Image bg = go.GetComponent<Image>();
        if (bg == null) bg = go.AddComponent<Image>();

        Image iconImg   = FindChildImage(go, "Icon");
        Text  labelText = FindChildText(go,  "Label");

        if (slotType == null)
        {
            bg.color = Color.clear; bg.raycastTarget = false;
            SetActive(iconImg, false); SetActive(labelText, false);
            return;
        }

        EquipType type  = slotType.Value;
        EquipData equip = EquipmentManager.Instance?.GetEquipped(type);
        bool hasItem    = equip != null;

        bg.color         = hasItem ? colorEquipped : colorEmpty;
        bg.raycastTarget = true;

        EquipmentSlotView slotView = go.GetComponent<EquipmentSlotView>() ?? go.AddComponent<EquipmentSlotView>();
        slotView.slotType = type;
        slotView.hasItem  = hasItem;

        if (iconImg != null)
        {
            if (hasItem && equip.icon != null)
            { iconImg.gameObject.SetActive(true); iconImg.sprite = equip.icon; iconImg.color = Color.white; }
            else
            { iconImg.gameObject.SetActive(false); }
        }

        if (labelText != null)
        {
            bool showLabel     = !hasItem || equip.icon == null;
            labelText.gameObject.SetActive(showLabel);
            labelText.text     = hasItem && equip.icon == null ? equip.itemName : SlotShortLabel(type);
            labelText.fontSize = hasItem
                ? Mathf.RoundToInt(slotSize / 6f)
                : Mathf.RoundToInt(slotSize / 7f);
        }

        if (!hasItem) return;

        EquipType ct = type; EquipData ce = equip;
        RectTransform rt = go.GetComponent<RectTransform>();

        EventTrigger trigger = go.GetComponent<EventTrigger>() ?? go.AddComponent<EventTrigger>();
        trigger.triggers.Clear();

        var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enter.callback.AddListener(_ => { bg.color = colorHover; tooltipUI?.ShowForEquip(ce, rt); });
        trigger.triggers.Add(enter);

        var exitE = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exitE.callback.AddListener(_ => { bg.color = colorEquipped; tooltipUI?.Hide(); });
        trigger.triggers.Add(exitE);

        var beginDrag = new EventTrigger.Entry { eventID = EventTriggerType.BeginDrag };
        beginDrag.callback.AddListener(evt =>
        {
            var pe = evt as PointerEventData;
            if (pe != null && pe.button == PointerEventData.InputButton.Left)
                BeginEquipDrag(ct, ce, pe);
        });
        trigger.triggers.Add(beginDrag);

        var drag = new EventTrigger.Entry { eventID = EventTriggerType.Drag };
        drag.callback.AddListener(evt =>
        {
            var pe = evt as PointerEventData;
            if (pe != null) UpdateEquipDrag(pe);
        });
        trigger.triggers.Add(drag);

        var endDrag = new EventTrigger.Entry { eventID = EventTriggerType.EndDrag };
        endDrag.callback.AddListener(evt =>
        {
            var pe = evt as PointerEventData;
            EndEquipDrag(pe);
        });
        trigger.triggers.Add(endDrag);

        var click = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
        click.callback.AddListener(evt =>
        {
            var pe = evt as PointerEventData;
            if (pe != null && pe.button == PointerEventData.InputButton.Right)
                OnRightClick(ct);
        });
        trigger.triggers.Add(click);
    }

    private void BeginEquipDrag(EquipType slotType, EquipData equip, PointerEventData eventData)
    {
        if (equip == null) return;

        isDragging = true;
        dragSourceType = slotType;
        tooltipUI?.Hide();

        EnsureDragIcon();
        if (dragIconImage != null)
        {
            dragIconImage.enabled = true;
            dragIconImage.sprite = equip.icon;
            dragIconImage.color = equip.icon != null ? Color.white : new Color(1f, 1f, 1f, 0.6f);
        }

        UpdateEquipDrag(eventData);
    }

    private void UpdateEquipDrag(PointerEventData eventData)
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
    }

    private void EndEquipDrag(PointerEventData eventData)
    {
        if (!isDragging) return;

        TryDropEquipToInventory(eventData);

        isDragging = false;
        if (dragIconImage != null)
        {
            dragIconImage.enabled = false;
            dragIconImage.sprite = null;
        }
    }

    private void TryDropEquipToInventory(PointerEventData eventData)
    {
        Inventory inv = Inventory.Instance;
        EquipmentManager em = EquipmentManager.Instance;
        if (inv == null || em == null || eventData == null) return;

        EquipData draggedEquip = em.GetEquipped(dragSourceType);
        if (draggedEquip == null) return;

        int targetIndex = GetInventoryDropIndex(eventData);
        if (targetIndex < 0) return;

        bool targetHasItem = targetIndex < inv.CurrentItemCount;
        if (!targetHasItem)
        {
            em.Unequip(dragSourceType);
            return;
        }

        InventorySlot targetSlot = inv.Slots[targetIndex];
        EquipData targetEquip = targetSlot?.item as EquipData;

        // 같은 타입 장비 아이템 위에 드롭하면 교체
        if (targetEquip != null && targetEquip.equipType == dragSourceType)
        {
            inv.RemoveAt(targetIndex);

            em.Unequip(dragSourceType);
            if (em.GetEquipped(dragSourceType) != null)
            {
                // 해제 실패 시 최소 롤백 시도
                inv.AddItem(targetEquip, 1);
                return;
            }

            em.Equip(targetEquip);

            int movedIndex = inv.CurrentItemCount - 1;
            if (movedIndex >= 0)
                inv.MoveSlot(movedIndex, targetIndex);
            return;
        }

        // 다른 타입 아이템 위에 드롭: 대상은 유지, 장비는 빈칸으로 해제 시도
        em.Unequip(dragSourceType);
    }

    private int GetInventoryDropIndex(PointerEventData eventData)
    {
        InventoryUI invUI = InventoryUI.Instance;
        Inventory inv = Inventory.Instance;
        if (invUI == null || inv == null || invUI.itemGrid == null) return -1;

        RectTransform gridRT = invUI.itemGrid as RectTransform;
        if (gridRT == null) return -1;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                gridRT,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 local))
        {
            float width = gridRT.rect.width;
            float ox = local.x + width * 0.5f;
            float oy = -local.y;

            float pitchX = invUI.slotSize + invUI.slotSpacing;
            float pitchY = invUI.slotSize + invUI.slotSpacing;

            int col = Mathf.FloorToInt(ox / pitchX);
            int row = Mathf.FloorToInt(oy / pitchY);

            int maxRows = Mathf.CeilToInt(40f / 4f); // InventoryUI 고정 4x10
            int openSlots = Mathf.Min(inv.MaxItemCount, 40);

            if (col >= 0 && col < 4 && row >= 0 && row < maxRows)
            {
                int index = row * 4 + col;
                if (index >= 0 && index < openSlots)
                    return index;
            }
        }

        return -1;
    }

    private void EnsureDragIcon()
    {
        if (dragIconImage != null) return;

        if (rootCanvas == null) rootCanvas = GetComponentInParent<Canvas>();
        if (rootCanvas == null) return;

        GameObject go = new GameObject("DraggedEquipIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(rootCanvas.transform, false);

        dragIconRect = go.GetComponent<RectTransform>();
        dragIconRect.sizeDelta = new Vector2(slotSize, slotSize);

        dragIconImage = go.GetComponent<Image>();
        dragIconImage.raycastTarget = false;
        dragIconImage.preserveAspect = true;
        dragIconImage.enabled = false;
    }

    private void OnRightClick(EquipType slotType)
    {
        tooltipUI?.Hide();
        EquipmentManager.Instance?.Unequip(slotType);
    }

    // ────────────────────────────────────────────────────────
    private string SlotShortLabel(EquipType t)
    {
        switch (t)
        {
            case EquipType.Weapon:   return "무기";
            case EquipType.Helmet:   return "투구";
            case EquipType.Necklace: return "목걸이";
            case EquipType.Gloves:   return "장갑";
            case EquipType.Armor:    return "갑옷";
            case EquipType.Ring:     return "반지";
            case EquipType.Amulet:   return "보조 장비";
            case EquipType.Boots:    return "신발";
            case EquipType.Bag:      return "가방";
            default: return t.ToString();
        }
    }

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

    private void SetActive(Component c, bool active)
    { if (c != null) c.gameObject.SetActive(active); }
}

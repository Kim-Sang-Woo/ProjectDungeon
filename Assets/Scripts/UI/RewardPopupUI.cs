// ============================================================
// RewardPopupUI.cs — 보상 획득 UI (4×4 그리드)
// 위치: Assets/Scripts/UI/RewardPopupUI.cs
// ============================================================
// [씬 배치]
//   RewardPopupPanel (GameObject)
//     컴포넌트: RewardPopupUI + CanvasGroup
//     자식 오브젝트 불필요 — 모두 코드로 생성
//
// [Inspector 설정]
//   없음 — 모든 UI/레이아웃/색상을 코드로 제어
//   수치 조정이 필요한 경우 하단 [Header("레이아웃")] 섹션 참조
//
// [동작]
//   Show()  : 아이템 목록을 받아 슬롯에 배치 후 패널 표시
//   우클릭  : 해당 슬롯 아이템 → Inventory.AddItem() → 슬롯 비우기
//   Close() : 남은 슬롯 아이템 조용히 버림 → 패널 닫기
// ============================================================
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class RewardPopupUI : MonoBehaviour
{
    // ── 레이아웃 수치 (Inspector에서 조정 가능) ──────────
    [Header("레이아웃")]
    public float panelWidth   = 280f;
    public float headerHeight = 44f;
    public float footerHeight = 36f;
    public float slotSize     = 58f;
    public float slotSpacing  = 4f;
    public float paddingH     = 14f;
    public float paddingV     = 14f;

    // ── 색상 ──────────────────────────────────────────────
    [Header("색상")]
    public Color colorPanelBg   = new Color(0.071f, 0.063f, 0.051f, 1f);
    public Color colorHeaderBg  = new Color(0.125f, 0.110f, 0.075f, 1f);
    public Color colorGridBg    = new Color(0.102f, 0.086f, 0.071f, 1f);
    public Color colorFooterBg  = new Color(0.071f, 0.063f, 0.051f, 1f);
    public Color colorSlotBg    = new Color(0.055f, 0.047f, 0.035f, 1f);
    public Color colorSlotEmpty = new Color(0.055f, 0.047f, 0.035f, 0.4f);
    public Color colorGold      = new Color(0.784f, 0.659f, 0.294f, 1f);
    public Color colorDim       = new Color(0.478f, 0.416f, 0.294f, 1f);

    // ── 내부 상태 ─────────────────────────────────────────
    private const int COLS       = 4;
    private const int ROWS       = 4;
    private const int SLOT_COUNT = COLS * ROWS;

    private struct SlotState
    {
        public GameObject root;
        public Image      icon;
        public Text       count;
        public ItemData   item;
        public int        quantity;
    }

    private SlotState[] slots        = new SlotState[SLOT_COUNT];
    private Text        titleText;
    private Text        itemCountText;
    private bool        initialized  = false;

    // ─────────────────────────────────────────────────────
    private void Awake()
    {
        gameObject.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            Close();
    }

    private void EnsureInitialized()
    {
        if (initialized) return;
        initialized = true;
        BuildPanel();
    }

    // ── 패널 전체 빌드 ────────────────────────────────────
    private void BuildPanel()
    {
        float gridH  = paddingV + ROWS * slotSize + (ROWS - 1) * slotSpacing + paddingV;
        float totalH = headerHeight + gridH + footerHeight;

        // 패널 RectTransform — 화면 중앙 고정
        RectTransform panelRT = GetComponent<RectTransform>()
                                ?? gameObject.AddComponent<RectTransform>();
        panelRT.anchorMin        = new Vector2(0.5f, 0.5f);
        panelRT.anchorMax        = new Vector2(0.5f, 0.5f);
        panelRT.pivot            = new Vector2(0.5f, 0.5f);
        panelRT.sizeDelta        = new Vector2(panelWidth, totalH);
        panelRT.anchoredPosition = new Vector2(410f, 0f);
        SetBg(gameObject, colorPanelBg);

        // ── Header ────────────────────────────────────
        GameObject header = MakeBlock("Header", panelRT, 0f, headerHeight, colorHeaderBg);

        titleText = MakeText(header, "TitleText",
            new Vector2(0f, 0f), new Vector2(0.75f, 1f),
            new Vector4(paddingH, 0, 0, 0),
            "REWARD", 12, FontStyle.Bold, colorGold, TextAnchor.MiddleLeft);

        Button closeBtn = MakeButton(header, "CloseButton",
            new Vector2(0.78f, 0.15f), new Vector2(1f, 0.85f),
            new Vector4(0, 0, paddingH, 0),
            "CLOSE", 9, colorDim);
        closeBtn.onClick.AddListener(Close);

        // ── Grid Section ──────────────────────────────
        GameObject gridSection = MakeBlock("GridSection", panelRT, headerHeight, gridH, colorGridBg);

        GameObject gridGo = new GameObject("RewardGrid");
        gridGo.transform.SetParent(gridSection.transform, false);
        RectTransform gridRT = gridGo.AddComponent<RectTransform>();
        gridRT.anchorMin = Vector2.zero;
        gridRT.anchorMax = Vector2.one;
        gridRT.offsetMin = Vector2.zero;
        gridRT.offsetMax = Vector2.zero;

        GridLayoutGroup g = gridGo.AddComponent<GridLayoutGroup>();
        g.cellSize        = new Vector2(slotSize, slotSize);
        g.spacing         = new Vector2(slotSpacing, slotSpacing);
        g.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
        g.constraintCount = COLS;
        g.padding         = new RectOffset((int)paddingH, (int)paddingH, (int)paddingV, (int)paddingV);
        g.childAlignment  = TextAnchor.UpperLeft;

        BuildSlots(gridRT);

        // ── Footer ────────────────────────────────────
        GameObject footer = MakeBlock("Footer", panelRT, headerHeight + gridH, footerHeight, colorFooterBg);

        MakeText(footer, "HintText",
            new Vector2(0f, 0f), new Vector2(0.6f, 1f),
            new Vector4(paddingH, 0, 0, 0),
            "우클릭 — 가방으로 이동", 10, FontStyle.Italic, colorDim, TextAnchor.MiddleLeft);

        itemCountText = MakeText(footer, "ItemCountText",
            new Vector2(0.6f, 0f), new Vector2(1f, 1f),
            new Vector4(0, 0, paddingH, 0),
            $"0 / {SLOT_COUNT}", 10, FontStyle.Normal, colorDim, TextAnchor.MiddleRight);
    }

    // ── 슬롯 생성 ─────────────────────────────────────────
    private void BuildSlots(RectTransform parent)
    {
        for (int i = 0; i < SLOT_COUNT; i++)
        {
            GameObject go = new GameObject($"RewardSlot_{i}");
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>().sizeDelta = new Vector2(slotSize, slotSize);

            Image bg  = go.AddComponent<Image>();
            bg.type   = Image.Type.Simple;
            bg.sprite = null;
            bg.color  = colorSlotEmpty;
            go.AddComponent<Button>().transition = Selectable.Transition.None;

            // 아이콘
            GameObject iconGo = new GameObject("IconImage");
            iconGo.transform.SetParent(go.transform, false);
            RectTransform iconRT = iconGo.AddComponent<RectTransform>();
            iconRT.anchorMin = new Vector2(0.08f, 0.08f);
            iconRT.anchorMax = new Vector2(0.92f, 0.92f);
            iconRT.offsetMin = Vector2.zero;
            iconRT.offsetMax = Vector2.zero;
            Image iconImg = iconGo.AddComponent<Image>();
            iconImg.preserveAspect = true;
            iconImg.enabled        = false;

            // 수량
            GameObject countGo = new GameObject("CountText");
            countGo.transform.SetParent(go.transform, false);
            RectTransform countRT = countGo.AddComponent<RectTransform>();
            countRT.anchorMin = new Vector2(0f, 0f);
            countRT.anchorMax = new Vector2(1f, 0.38f);
            countRT.offsetMin = new Vector2(2f, 2f);
            countRT.offsetMax = new Vector2(-2f, 0f);
            Text countTxt    = countGo.AddComponent<Text>();
            countTxt.font    = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            countTxt.fontSize  = 10;
            countTxt.color     = colorGold;
            countTxt.alignment = TextAnchor.LowerRight;
            countTxt.enabled   = false;

            slots[i] = new SlotState { root = go, icon = iconImg, count = countTxt };

            int idx = i;
            AddRightClickHandler(go, () => OnSlotRightClick(idx));
            AddHoverHandler(go, idx);
        }
    }

    // ── 공개 API ──────────────────────────────────────────

    public void Show(string displayName, List<RewardItemEntry> items)
    {
        gameObject.SetActive(true);
        EnsureInitialized();

        if (titleText != null)
            titleText.text = string.IsNullOrEmpty(displayName) ? "REWARD" : displayName.ToUpper();

        for (int i = 0; i < SLOT_COUNT; i++) SetEmpty(ref slots[i]);
        for (int i = 0; i < items.Count && i < SLOT_COUNT; i++)
            SetItem(ref slots[i], items[i].item, items[i].value);

        UpdateFooterCount();
    }

    public void Close()
    {
        ItemTooltipUI.Instance?.Hide();
        for (int i = 0; i < SLOT_COUNT; i++) slots[i].item = null;
        gameObject.SetActive(false);
    }

    // ── 슬롯 상태 ─────────────────────────────────────────

    private void SetItem(ref SlotState s, ItemData item, int qty)
    {
        s.item = item; s.quantity = qty;
        SetSlotBg(s.root, colorSlotBg);
        if (s.icon  != null) { s.icon.sprite = item?.icon; s.icon.color = Color.white; s.icon.enabled = item != null; }
        if (s.count != null) { s.count.text = qty > 1 ? qty.ToString() : ""; s.count.enabled = qty > 1; }
    }

    private void SetEmpty(ref SlotState s)
    {
        s.item = null; s.quantity = 0;
        SetSlotBg(s.root, colorSlotEmpty);
        if (s.icon  != null) s.icon.enabled  = false;
        if (s.count != null) s.count.enabled = false;
    }

    private void SetSlotBg(GameObject go, Color color)
    {
        if (go == null) return;
        Image img = go.GetComponent<Image>();
        if (img != null) img.color = color;
    }

    // ── 툴팁 ──────────────────────────────────────────────

    private void AddHoverHandler(GameObject go, int idx)
    {
        EventTrigger trigger = go.GetComponent<EventTrigger>() ?? go.AddComponent<EventTrigger>();

        var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enter.callback.AddListener(_ =>
        {
            if (slots[idx].item == null) return;
            // slots[idx].root는 슬롯 루트 GameObject — 호버 시점에 참조해도 안전
            // (GridLayoutGroup 레이아웃은 Show() → EnsureInitialized() 완료 후 확정됨)
            RectTransform rt = slots[idx].root.GetComponent<RectTransform>();
            ItemTooltipUI.Instance?.ShowForItem(slots[idx].item, slots[idx].quantity, rt);
        });
        trigger.triggers.Add(enter);

        var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exit.callback.AddListener(_ => ItemTooltipUI.Instance?.Hide());
        trigger.triggers.Add(exit);
    }

    // ── 우클릭 ────────────────────────────────────────────

    private void OnSlotRightClick(int idx)
    {
        if (slots[idx].item == null) return;
        ItemData item = slots[idx].item;
        int      qty  = slots[idx].quantity;

        if (Inventory.Instance == null) { Debug.LogWarning("[RewardPopupUI] Inventory null"); return; }

        switch (Inventory.Instance.AddItem(item, qty))
        {
            case AddItemResult.Success:
                SetEmpty(ref slots[idx]);
                FloatingTextUI.Instance?.Show($"{item.itemName} ×{qty} 획득", FloatingTextUI.ColorAcquire);
                UpdateFooterCount();
                break;
            case AddItemResult.FailSlotFull:
                FloatingTextUI.Instance?.Show("인벤토리가 가득 찼습니다.", FloatingTextUI.ColorFail);
                break;
            case AddItemResult.FailTooHeavy:
                FloatingTextUI.Instance?.Show("너무 무겁습니다.", FloatingTextUI.ColorFail);
                break;
        }
    }

    // ── 푸터 카운트 ───────────────────────────────────────

    private void UpdateFooterCount()
    {
        if (itemCountText == null) return;
        int filled = 0;
        foreach (var s in slots) if (s.item != null) filled++;
        itemCountText.text = $"{filled} / {SLOT_COUNT}";

        // 남은 아이템이 0개면 자동 닫기
        if (filled == 0) Close();
    }

    // ── UI 생성 헬퍼 ──────────────────────────────────────

    private GameObject MakeBlock(string name, RectTransform parent,
        float offsetFromTop, float height, Color bgColor)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0f, 1f);
        rt.anchorMax        = new Vector2(1f, 1f);
        rt.pivot            = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -offsetFromTop);
        rt.sizeDelta        = new Vector2(0f, height);
        SetBg(go, bgColor);
        return go;
    }

    private Text MakeText(GameObject parent, string name,
        Vector2 anchorMin, Vector2 anchorMax, Vector4 offset,
        string text, int size, FontStyle style, Color color, TextAnchor alignment)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.offsetMin = new Vector2(offset.x, offset.y);
        rt.offsetMax = new Vector2(-offset.z, -offset.w);
        Text t = go.AddComponent<Text>();
        t.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.text      = text; t.fontSize = size; t.fontStyle = style;
        t.color     = color; t.alignment = alignment;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow   = VerticalWrapMode.Overflow;
        return t;
    }

    private Button MakeButton(GameObject parent, string name,
        Vector2 anchorMin, Vector2 anchorMax, Vector4 offset,
        string label, int fontSize, Color labelColor)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.offsetMin = new Vector2(offset.x, offset.y);
        rt.offsetMax = new Vector2(-offset.z, -offset.w);
        SetBg(go, new Color(0.1f, 0.09f, 0.06f, 1f));
        Button btn = go.AddComponent<Button>();
        btn.transition = Selectable.Transition.ColorTint;
        MakeText(go, "Text", Vector2.zero, Vector2.one, Vector4.zero,
            label, fontSize, FontStyle.Normal, labelColor, TextAnchor.MiddleCenter);
        return btn;
    }

    private void SetBg(GameObject go, Color color)
    {
        Image img = go.GetComponent<Image>() ?? go.AddComponent<Image>();
        img.type = Image.Type.Simple; img.sprite = null; img.color = color;
    }

    private void AddRightClickHandler(GameObject go, System.Action onRightClick)
    {
        EventTrigger trigger = go.GetComponent<EventTrigger>() ?? go.AddComponent<EventTrigger>();
        var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
        entry.callback.AddListener((data) =>
        {
            if (((PointerEventData)data).button == PointerEventData.InputButton.Right)
                onRightClick?.Invoke();
        });
        trigger.triggers.Add(entry);
    }
}

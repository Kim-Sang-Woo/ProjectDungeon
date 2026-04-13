using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class TownScreenUI : MonoBehaviour
{
    public enum TownPage
    {
        Dungeon,
        Storage,
        Merchant
    }

    [Header("참조")]
    public DungeonManager dungeonManager;

    [Header("배경")]
    public Sprite dungeonPageSprite;
    public Sprite storagePageSprite;
    public Sprite merchantPageSprite;

    [Header("버튼")]
    public string dungeonLabel = "던전";
    public string storageLabel = "보관함";
    public string merchantLabel = "상인";
    public string enterDungeonLabel = "던전 출발";

    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private Font uiFont;
    private RewardPopupUI storagePopupUI;
    private Image backgroundImage;
    private Image goldIconImage;
    private Text goldValueText;
    private Button dungeonButton;
    private Button storageButton;
    private Button merchantButton;
    private RectTransform enterDungeonWrap;
    private Button enterDungeonButton;
    private Text dungeonButtonText;
    private Text storageButtonText;
    private Text merchantButtonText;
    private Text enterDungeonText;
    private TownPage currentPage = TownPage.Dungeon;
    private bool isBuilt;

    private readonly Color colorTopBar = new Color(0.03f, 0.025f, 0.016f, 0.92f);
    private readonly Color colorTopBarLine = new Color(0.78f, 0.66f, 0.29f, 0.20f);
    private readonly Color colorBorder = new Color(0.35f, 0.29f, 0.16f, 0.85f);
    private readonly Color colorAccent = new Color(0.78f, 0.66f, 0.29f, 1f);
    private readonly Color colorTextMain = new Color(0.83f, 0.77f, 0.60f, 1f);
    private readonly Color colorTextDim = new Color(0.48f, 0.42f, 0.29f, 1f);
    private readonly Color colorEnterTop = new Color(0.23f, 0.17f, 0.06f, 1f);
    private readonly Color colorEnterBottom = new Color(0.14f, 0.10f, 0.03f, 1f);

    private void OnEnable()
    {
        if (GoldManager.Instance != null)
            GoldManager.Instance.OnGoldChanged += HandleGoldChanged;
    }

    private void OnDisable()
    {
        if (GoldManager.Instance != null)
            GoldManager.Instance.OnGoldChanged -= HandleGoldChanged;
    }

    private void Awake()
    {
        BuildIfNeeded();
        HideImmediate();
    }

    public void Show(DungeonManager owner)
    {
        if (owner != null)
            dungeonManager = owner;

        BuildIfNeeded();
        if (canvasGroup == null)
        {
            Debug.LogError("[TownScreenUI] Show 실패: canvasGroup이 초기화되지 않았습니다.");
            return;
        }

        SetPage(TownPage.Dungeon);
        RefreshGoldUI();

        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;
        gameObject.SetActive(true);
    }

    public void HideImmediate()
    {
        ExitStorageMode();
        ExitMerchantMode();
        if (canvasGroup == null) return;
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
    }

    public void SetPage(TownPage page)
    {
        ExitStorageMode();
        ExitMerchantMode();
        currentPage = page;

        if (backgroundImage != null)
        {
            Sprite sprite = GetPageSprite(page);
            backgroundImage.sprite = sprite;
            backgroundImage.color = sprite != null ? Color.white : new Color(0.07f, 0.06f, 0.04f, 1f);
        }

        ApplyTabState(dungeonButtonText, page == TownPage.Dungeon);
        ApplyTabState(storageButtonText, page == TownPage.Storage);
        ApplyTabState(merchantButtonText, page == TownPage.Merchant);

        if (enterDungeonWrap != null)
            enterDungeonWrap.gameObject.SetActive(page == TownPage.Dungeon);

        if (page == TownPage.Storage)
            EnterStorageMode();
        else if (page == TownPage.Merchant)
            EnterMerchantMode();
    }

    private Sprite GetPageSprite(TownPage page)
    {
        switch (page)
        {
            case TownPage.Storage: return storagePageSprite != null ? storagePageSprite : dungeonPageSprite;
            case TownPage.Merchant: return merchantPageSprite != null ? merchantPageSprite : dungeonPageSprite;
            default: return dungeonPageSprite;
        }
    }

    private void HandleEnterDungeon()
    {
        ExitStorageMode();
        ExitMerchantMode();
        dungeonManager?.BeginFreshDungeonRunFromTown();
    }

    private void EnterStorageMode()
    {
        InventoryUI.Instance?.SetTownStorageForcedOpen(true);

        storagePopupUI = null;
        if (RewardManager.Instance != null && RewardManager.Instance.popupUI != null)
            storagePopupUI = RewardManager.Instance.popupUI;
        if (storagePopupUI == null)
            storagePopupUI = FindFirstObjectByType<RewardPopupUI>(FindObjectsInactive.Include);

        if (storagePopupUI != null)
        {
            storagePopupUI.ShowStorage("보관함", TownStorageManager.Instance, TownStorageManager.Instance != null ? TownStorageManager.Instance.columns : 10, TownStorageManager.Instance != null ? TownStorageManager.Instance.rows : 10, true);
            storagePopupUI.transform.SetAsLastSibling();
        }
        else
        {
            Debug.LogError("[TownScreenUI] RewardPopupUI를 찾지 못해 보관함 패널을 열 수 없습니다.");
        }
    }

    private void ExitStorageMode()
    {
        InventoryUI.Instance?.SetTownStorageForcedOpen(false);
        storagePopupUI?.Close();
    }

    private void EnterMerchantMode()
    {
        InventoryUI.Instance?.SetTownMerchantForcedOpen(true);

        storagePopupUI = null;
        if (RewardManager.Instance != null && RewardManager.Instance.popupUI != null)
            storagePopupUI = RewardManager.Instance.popupUI;
        if (storagePopupUI == null)
            storagePopupUI = FindFirstObjectByType<RewardPopupUI>(FindObjectsInactive.Include);

        MerchantInventoryManager merchantManager = MerchantInventoryManager.Instance;
        if (merchantManager == null)
            merchantManager = FindFirstObjectByType<MerchantInventoryManager>(FindObjectsInactive.Include);

        merchantManager?.EnsureStockReady();

        if (storagePopupUI != null && merchantManager != null)
        {
            storagePopupUI.ShowMerchant("상점", merchantManager, merchantManager.columns, merchantManager.rows, true);
            storagePopupUI.transform.SetAsLastSibling();
        }
        else if (merchantManager == null)
        {
            Debug.LogError("[TownScreenUI] MerchantInventoryManager를 찾지 못해 상점 패널을 열 수 없습니다.");
        }
        else
        {
            Debug.LogError("[TownScreenUI] RewardPopupUI를 찾지 못해 상점 패널을 열 수 없습니다.");
        }
    }

    private void ExitMerchantMode()
    {
        InventoryUI.Instance?.SetTownMerchantForcedOpen(false);
        storagePopupUI?.Close();
    }

    private void HandleGoldChanged(int amount)
    {
        RefreshGoldUI();
    }

    private void BuildIfNeeded()
    {
        if (isBuilt && canvasGroup != null && backgroundImage != null) return;

        canvas = GetComponent<Canvas>();
        if (canvas == null) canvas = gameObject.AddComponent<Canvas>();
        uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = -100;

        CanvasScaler scaler = GetComponent<CanvasScaler>();
        if (scaler == null) scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        if (GetComponent<GraphicRaycaster>() == null)
            gameObject.AddComponent<GraphicRaycaster>();

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        RectTransform root = GetComponent<RectTransform>();
        if (root == null) root = gameObject.AddComponent<RectTransform>();
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;

        for (int i = transform.childCount - 1; i >= 0; i--)
            DestroyImmediate(transform.GetChild(i).gameObject);

        RectTransform bg = CreateRect("Background", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        backgroundImage = bg.gameObject.AddComponent<Image>();
        backgroundImage.color = new Color(0.07f, 0.06f, 0.04f, 1f);
        backgroundImage.preserveAspect = false;

        RectTransform overlay = CreateRect("BackgroundShade", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Image overlayImage = overlay.gameObject.AddComponent<Image>();
        overlayImage.color = new Color(0f, 0f, 0f, 0.18f);
        overlayImage.raycastTarget = false;

        RectTransform topBar = CreateRect("TopBar", root, new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
        topBar.pivot = new Vector2(0.5f, 1f);
        topBar.sizeDelta = new Vector2(0f, 50f);
        topBar.anchoredPosition = Vector2.zero;
        Image topBarImage = topBar.gameObject.AddComponent<Image>();
        topBarImage.color = colorTopBar;

        RectTransform bottomLine = CreateRect("TopBarLine", topBar, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(0f, 1f));
        Image lineImage = bottomLine.gameObject.AddComponent<Image>();
        lineImage.color = colorTopBarLine;
        lineImage.raycastTarget = false;

        RectTransform navGroupRt = CreateRect("NavGroup", topBar, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(-210f, 0f), new Vector2(210f, 0f));
        navGroupRt.pivot = new Vector2(0.5f, 0.5f);
        HorizontalLayoutGroup navGroup = navGroupRt.gameObject.AddComponent<HorizontalLayoutGroup>();
        navGroup.childForceExpandWidth = false;
        navGroup.childForceExpandHeight = true;
        navGroup.childControlWidth = true;
        navGroup.childControlHeight = true;
        navGroup.spacing = 0f;
        navGroup.padding = new RectOffset(0, 0, 0, 0);

        dungeonButton = CreateNavButton(navGroup.transform, dungeonLabel, out dungeonButtonText, () => SetPage(TownPage.Dungeon));
        storageButton = CreateNavButton(navGroup.transform, storageLabel, out storageButtonText, () => SetPage(TownPage.Storage));
        merchantButton = CreateNavButton(navGroup.transform, merchantLabel, out merchantButtonText, () => SetPage(TownPage.Merchant));

        BuildGoldDisplay(topBar);

        RectTransform enterWrap = CreateRect("EnterButtonWrap", root, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-300f, 32f), new Vector2(-32f, 96f));
        enterWrap.pivot = new Vector2(1f, 0f);
        enterDungeonWrap = enterWrap;

        Image enterFrame = enterWrap.gameObject.AddComponent<Image>();
        enterFrame.color = colorAccent;

        RectTransform enterButtonRt = CreateRect("EnterDungeonButton", enterWrap, Vector2.zero, Vector2.one, new Vector2(2f, 2f), new Vector2(-2f, -2f));
        Image enterButtonImage = enterButtonRt.gameObject.AddComponent<Image>();
        enterButtonImage.color = colorEnterBottom;

        enterDungeonButton = enterButtonRt.gameObject.AddComponent<Button>();
        enterDungeonButton.transition = Selectable.Transition.ColorTint;
        ColorBlock cb = enterDungeonButton.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1f, 1f, 1f, 0.95f);
        cb.pressedColor = new Color(0.90f, 0.90f, 0.90f, 0.95f);
        cb.selectedColor = cb.highlightedColor;
        enterDungeonButton.colors = cb;
        enterDungeonButton.targetGraphic = enterButtonImage;
        enterDungeonButton.onClick.AddListener(HandleEnterDungeon);

        RectTransform enterGloss = CreateRect("EnterGloss", enterButtonRt, new Vector2(0f, 0.5f), new Vector2(1f, 1f), new Vector2(0f, 0f), new Vector2(0f, 0f));
        Image glossImage = enterGloss.gameObject.AddComponent<Image>();
        glossImage.color = new Color(colorAccent.r, colorAccent.g, colorAccent.b, 0.10f);
        glossImage.raycastTarget = false;

        enterDungeonText = CreateText("EnterDungeonText", enterButtonRt, enterDungeonLabel, 18, colorAccent, FontStyle.Bold);
        enterDungeonText.alignment = TextAnchor.MiddleCenter;
        enterDungeonText.raycastTarget = false;
        enterDungeonText.resizeTextForBestFit = true;
        enterDungeonText.resizeTextMinSize = 14;
        enterDungeonText.resizeTextMaxSize = 22;

        RectTransform enterTextRt = enterDungeonText.rectTransform;
        enterTextRt.anchorMin = Vector2.zero;
        enterTextRt.anchorMax = Vector2.one;
        enterTextRt.offsetMin = new Vector2(12f, 0f);
        enterTextRt.offsetMax = new Vector2(-12f, 0f);

        SetPage(TownPage.Dungeon);
        RefreshGoldUI();
        isBuilt = true;
    }

    private Button CreateNavButton(Transform parent, string label, out Text labelText, UnityAction onClick)
    {
        RectTransform rt = CreateRect(label + "Button", parent, new Vector2(0f, 0f), new Vector2(0f, 1f), Vector2.zero, new Vector2(120f, 0f));
        rt.pivot = new Vector2(0f, 0.5f);
        LayoutElement layout = rt.gameObject.AddComponent<LayoutElement>();
        layout.preferredWidth = 120f;

        Image bg = rt.gameObject.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0f);

        Button button = rt.gameObject.AddComponent<Button>();
        button.transition = Selectable.Transition.ColorTint;
        button.targetGraphic = bg;
        var colors = button.colors;
        colors.normalColor = new Color(1f, 1f, 1f, 0f);
        colors.highlightedColor = new Color(colorAccent.r, colorAccent.g, colorAccent.b, 0.08f);
        colors.pressedColor = new Color(colorAccent.r, colorAccent.g, colorAccent.b, 0.12f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;
        button.onClick.AddListener(onClick);

        RectTransform rightLine = CreateRect("RightLine", rt, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-1f, 0f), Vector2.zero);
        Image line = rightLine.gameObject.AddComponent<Image>();
        line.color = new Color(colorBorder.r, colorBorder.g, colorBorder.b, 0.3f);
        line.raycastTarget = false;

        RectTransform activeLine = CreateRect("ActiveLine", rt, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(0f, 2f));
        Image activeLineImage = activeLine.gameObject.AddComponent<Image>();
        activeLineImage.color = colorAccent;
        activeLineImage.raycastTarget = false;

        labelText = CreateText("Label", rt, label, 14, colorTextDim, FontStyle.Bold);
        labelText.alignment = TextAnchor.MiddleCenter;
        labelText.raycastTarget = false;
        labelText.resizeTextForBestFit = true;
        labelText.resizeTextMinSize = 10;
        labelText.resizeTextMaxSize = 16;

        RectTransform labelRt = labelText.rectTransform;
        labelRt.anchorMin = Vector2.zero;
        labelRt.anchorMax = Vector2.one;
        labelRt.offsetMin = new Vector2(8f, 0f);
        labelRt.offsetMax = new Vector2(-8f, 0f);

        activeLine.gameObject.SetActive(false);
        rt.gameObject.name = label + "NavButton";
        return button;
    }

    private void ApplyTabState(Text label, bool active)
    {
        if (label == null) return;
        label.color = active ? colorAccent : colorTextDim;
        Transform line = label.transform.parent.Find("ActiveLine");
        if (line != null) line.gameObject.SetActive(active);
    }

    private void BuildGoldDisplay(RectTransform topBar)
    {
        RectTransform goldRoot = CreateRect("GoldDisplay", topBar, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-180f, 0f), new Vector2(-20f, 0f));
        goldRoot.pivot = new Vector2(1f, 0.5f);

        RectTransform iconRt = CreateRect("GoldIcon", goldRoot, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, -10f), new Vector2(20f, 10f));
        goldIconImage = iconRt.gameObject.AddComponent<Image>();
        goldIconImage.sprite = Resources.Load<Sprite>("Sprites/CoinGold");
        goldIconImage.preserveAspect = true;
        goldIconImage.color = Color.white;

        goldValueText = CreateText("GoldValue", goldRoot, "0", 14, colorTextMain, FontStyle.Bold);
        goldValueText.alignment = TextAnchor.MiddleLeft;
        goldValueText.raycastTarget = false;
        RectTransform valueRt = goldValueText.rectTransform;
        valueRt.anchorMin = new Vector2(0f, 0f);
        valueRt.anchorMax = new Vector2(1f, 1f);
        valueRt.offsetMin = new Vector2(28f, 0f);
        valueRt.offsetMax = Vector2.zero;
    }

    private void RefreshGoldUI()
    {
        if (goldValueText != null)
            goldValueText.text = GoldManager.Instance != null ? $"{GoldManager.Instance.CurrentGold} G" : "0 G";
        if (goldIconImage != null && goldIconImage.sprite == null)
            goldIconImage.sprite = Resources.Load<Sprite>("Sprites/CoinGold");
    }

    private RectTransform CreateRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
        return rt;
    }

    private Text CreateText(string name, Transform parent, string content, int fontSize, Color color, FontStyle fontStyle)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Text text = go.GetComponent<Text>();
        text.text = content;
        text.font = uiFont != null ? uiFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.color = color;
        text.fontStyle = fontStyle;
        text.supportRichText = false;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StatPanelUI : MonoBehaviour
{
    public static StatPanelUI Instance { get; private set; }

    [Header("표시 제어")]
    public CanvasGroup canvasGroup;

    [Header("레이아웃")]
    public float panelWidth = 280f;
    public float headerHeight = 42f;
    public float statusSectionHeight = 210f;
    public float paddingX = 14f;
    public float paddingY = 10f;
    public float bottomHintReserve = 72f;

    [Header("색상")]
    public Color panelColor = new Color(0.07f, 0.062f, 0.05f, 1f);
    public Color borderColor = new Color(0.35f, 0.29f, 0.16f, 1f);
    public Color accentGold = new Color(0.78f, 0.66f, 0.29f, 1f);
    public Color textMain = new Color(0.83f, 0.77f, 0.60f, 1f);
    public Color textDim = new Color(0.48f, 0.42f, 0.29f, 1f);
    public Color hpColor = new Color(0.83f, 0.35f, 0.35f, 1f);
    public Color manaColor = new Color(0.35f, 0.54f, 0.83f, 1f);
    public Color handColor = new Color(0.78f, 0.66f, 0.29f, 1f);
    public Color shieldColor = new Color(0.54f, 0.83f, 0.83f, 1f);
    public Color damageColor = new Color(0.83f, 0.56f, 0.35f, 1f);
    public Color dodgeColor = new Color(0.63f, 0.83f, 0.48f, 1f);

    public event System.Action<bool> OnStatPanelToggled;

    private bool isVisible;

    private Text hpValueText;
    private Text manaValueText;
    private Text handValueText;
    private Text shieldValueText;
    private Text damageValueText;
    private Text dodgeValueText;
    private Text deckListText;
    private Text deckTotalText;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        EnsureCanvasGroup();
        BuildLayout();
        HideImmediate();
        isVisible = false;
    }

    private void Start()
    {
        if (CharacterStats.Instance != null)
            CharacterStats.Instance.OnStatsChanged += RefreshIfVisible;
        if (EquipmentManager.Instance != null)
            EquipmentManager.Instance.OnEquipChanged += RefreshIfVisible;
    }

    private void OnDestroy()
    {
        if (CharacterStats.Instance != null)
            CharacterStats.Instance.OnStatsChanged -= RefreshIfVisible;
        if (EquipmentManager.Instance != null)
            EquipmentManager.Instance.OnEquipChanged -= RefreshIfVisible;
    }

    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.C)) return;
        if (IsBattleBlockingStatPanel()) return;
        if (IsTownBlockingStatPanel()) return;
        Toggle();
    }

    public bool IsVisible => isVisible;

    private bool IsTownBlockingStatPanel()
    {
        DungeonManager dm = FindFirstObjectByType<DungeonManager>();
        return dm != null && !dm.IsDungeonMode;
    }

    public void Toggle()
    {
        if (isVisible) Hide();
        else Show();
    }

    public void Show()
    {
        if (IsBattleBlockingStatPanel()) return;

        if (InventoryUI.Instance != null)
            InventoryUI.Instance.Hide();

        isVisible = true;
        Refresh();
        ShowImmediate();
        transform.SetAsLastSibling();
        OnStatPanelToggled?.Invoke(true);
    }

    public void Hide()
    {
        isVisible = false;
        HideImmediate();
        OnStatPanelToggled?.Invoke(false);
    }

    private void RefreshIfVisible()
    {
        if (isVisible)
            Refresh();
    }

    public void Refresh()
    {
        CharacterStats stats = CharacterStats.Instance;
        if (stats != null)
        {
            SetValueText(hpValueText, $"{Mathf.FloorToInt(stats.currentHP)} / {Mathf.FloorToInt(stats.maxHP.FinalValue)}", hpColor);
            SetValueText(manaValueText, Mathf.FloorToInt(stats.baseMana.FinalValue).ToString(), manaColor);
            SetValueText(handValueText, Mathf.FloorToInt(stats.maxHand.FinalValue).ToString(), handColor);
            SetValueText(shieldValueText, Mathf.FloorToInt(stats.baseShield.FinalValue).ToString(), shieldColor);
            SetValueText(damageValueText, BattleMath.CalcFinalDamage(stats.damageConst.FinalValue, stats.damagePer.FinalValue).ToString(), damageColor);
            SetValueText(dodgeValueText, Mathf.FloorToInt(stats.baseDodge.FinalValue).ToString(), dodgeColor);
        }

        RefreshDeckList();
    }

    private void RefreshDeckList()
    {
        if (deckListText == null || deckTotalText == null) return;

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        int totalCards = 0;

        EquipmentManager em = EquipmentManager.Instance;
        if (em != null)
        {
            foreach (var kv in em.GetAllEquipped())
            {
                EquipData equip = kv.Value;
                if (equip == null || equip.battleCards == null || equip.battleCards.Count == 0)
                    continue;

                Dictionary<string, int> counts = new Dictionary<string, int>();
                for (int i = 0; i < equip.battleCards.Count; i++)
                {
                    BattleCardData card = equip.battleCards[i];
                    if (card == null) continue;

                    string cardName = string.IsNullOrEmpty(card.cardName) ? "Unnamed Card" : card.cardName;
                    counts[cardName] = counts.TryGetValue(cardName, out int old) ? old + 1 : 1;
                    totalCards++;
                }

                if (counts.Count == 0) continue;

                if (sb.Length > 0)
                    sb.AppendLine().AppendLine();

                sb.Append("<size=10><color=#7A6A4A>").Append(equip.itemName).AppendLine("</color></size>");
                foreach (var pair in counts)
                {
                    sb.Append("<color=#D4C49A>").Append(pair.Key).Append("</color>")
                      .Append("  ")
                      .Append("<color=#C8A84B>x").Append(pair.Value).AppendLine("</color>");
                }
            }
        }

        if (sb.Length == 0)
            sb.Append("<color=#7A6A4A>표시할 카드가 없습니다.</color>");

        deckListText.supportRichText = true;
        deckListText.text = sb.ToString().TrimEnd();
        deckTotalText.text = $"TOTAL   {totalCards} cards";
    }

    private void BuildLayout()
    {
        RectTransform root = GetComponent<RectTransform>();
        if (root == null) root = gameObject.AddComponent<RectTransform>();
        root.anchorMin = new Vector2(0f, 0f);
        root.anchorMax = new Vector2(0f, 1f);
        root.pivot = new Vector2(0f, 1f);
        root.offsetMin = new Vector2(0f, bottomHintReserve);
        root.offsetMax = new Vector2(panelWidth, 0f);

        Image bg = GetComponent<Image>() ?? gameObject.AddComponent<Image>();
        bg.color = panelColor;

        Outline outline = GetComponent<Outline>() ?? gameObject.AddComponent<Outline>();
        outline.effectColor = borderColor;
        outline.effectDistance = new Vector2(1f, -1f);

        for (int i = transform.childCount - 1; i >= 0; i--)
            DestroyImmediate(transform.GetChild(i).gameObject);

        CreatePanelLine("TopAccent", root, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -2f), new Vector2(0f, 0f), new Color(accentGold.r, accentGold.g, accentGold.b, 0.5f));
        CreatePanelLine("HeaderLine", root, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -headerHeight - 1f), new Vector2(0f, -headerHeight), new Color(borderColor.r, borderColor.g, borderColor.b, 0.8f));

        RectTransform header = CreateRect("Header", root, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -headerHeight), new Vector2(0f, 0f));
        CreateText("HeaderTitle", header, "CHARACTER", 12, accentGold, TextAnchor.MiddleLeft, FontStyle.Bold,
            new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(paddingX, 0f), new Vector2(-54f, 0f));
        Text keyText = CreateText("HeaderKey", header, "C", 10, textDim, TextAnchor.MiddleCenter, FontStyle.Bold,
            new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-34f, -10f), new Vector2(-10f, 10f));
        Outline keyOutline = keyText.gameObject.AddComponent<Outline>();
        keyOutline.effectColor = new Color(borderColor.r, borderColor.g, borderColor.b, 0.6f);
        keyOutline.effectDistance = new Vector2(1f, -1f);

        RectTransform statusSection = CreateRect("StatusSection", root, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -(headerHeight + statusSectionHeight)), new Vector2(0f, -headerHeight));
        CreateSectionHeader(statusSection, "STATUS");

        RectTransform statusRowsRoot = CreateRect("StatusRowsRoot", statusSection, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(paddingX, paddingY), new Vector2(-paddingX, -26f));
        CreatePanelLine("StatusBottomLine", root, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(paddingX, -(headerHeight + statusSectionHeight + 1f)), new Vector2(-paddingX, -(headerHeight + statusSectionHeight)), new Color(borderColor.r, borderColor.g, borderColor.b, 0.35f));

        hpValueText = CreateStatRow(statusRowsRoot, 0, "체력", "HP");
        manaValueText = CreateStatRow(statusRowsRoot, 1, "지구력", "Mana");
        handValueText = CreateStatRow(statusRowsRoot, 2, "행동력", "Hand");
        shieldValueText = CreateStatRow(statusRowsRoot, 3, "방어", "Shield");
        damageValueText = CreateStatRow(statusRowsRoot, 4, "피해량", "Damage");
        dodgeValueText = CreateStatRow(statusRowsRoot, 5, "회피", "Dodge");

        RectTransform deckSection = CreateRect("DeckSection", root, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0f), new Vector2(0f, -(headerHeight + statusSectionHeight)));
        CreateSectionHeader(deckSection, "CARD DECK");
        deckListText = CreateText("DeckList", deckSection, "", 13, textMain, TextAnchor.UpperLeft, FontStyle.Normal,
            new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(paddingX, 44f), new Vector2(-paddingX, -34f));
        deckListText.lineSpacing = 1.05f;
        deckTotalText = CreateText("DeckTotal", deckSection, "TOTAL   0 cards", 12, accentGold, TextAnchor.LowerRight, FontStyle.Bold,
            new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(paddingX, 36f), new Vector2(-paddingX, 54f));
        CreatePanelLine("DeckTotalLine", deckSection, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(paddingX, 58f), new Vector2(-paddingX, 59f), new Color(borderColor.r, borderColor.g, borderColor.b, 0.35f));
    }

    private Text CreateStatRow(RectTransform parent, int rowIndex, string label, string subLabel)
    {
        float rowHeight = 28f;
        float top = rowIndex * rowHeight;

        RectTransform row = CreateRect(label + "Row", parent, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -top - rowHeight), new Vector2(0f, -top));
        if (rowIndex > 0)
            CreatePanelLine(label + "RowLine", row, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 0f), new Vector2(0f, 1f), new Color(borderColor.r, borderColor.g, borderColor.b, 0.16f));

        CreateText(label + "Label", row, label, 12, textDim, TextAnchor.MiddleLeft, FontStyle.Bold,
            new Vector2(0f, 0f), new Vector2(0.55f, 1f), new Vector2(0f, 0f), new Vector2(0f, 0f));
        CreateText(label + "SubLabel", row, subLabel, 10, textDim, TextAnchor.MiddleLeft, FontStyle.Italic,
            new Vector2(0.40f, 0f), new Vector2(0.78f, 1f), new Vector2(4f, 0f), new Vector2(0f, 0f));
        Text valueText = CreateText(label + "Value", row, "0", 13, textMain, TextAnchor.MiddleRight, FontStyle.Bold,
            new Vector2(0.60f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0f), new Vector2(0f, 0f));
        return valueText;
    }

    private void CreateSectionHeader(RectTransform parent, string title)
    {
        CreateText(title + "Label", parent, title, 10, textDim, TextAnchor.UpperLeft, FontStyle.Bold,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(paddingX, -18f), new Vector2(-paddingX, 0f));
        CreatePanelLine(title + "DecorLine", parent, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(92f, -11f), new Vector2(-paddingX, -10f), new Color(borderColor.r, borderColor.g, borderColor.b, 0.35f));
    }

    private RectTransform CreateRect(string name, RectTransform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
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

    private Text CreateText(string name, RectTransform parent, string text, int fontSize, Color color, TextAnchor anchor, FontStyle style,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;

        Text txt = go.GetComponent<Text>();
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.text = text;
        txt.fontSize = fontSize;
        txt.color = color;
        txt.alignment = anchor;
        txt.fontStyle = style;
        txt.horizontalOverflow = HorizontalWrapMode.Wrap;
        txt.verticalOverflow = VerticalWrapMode.Overflow;
        return txt;
    }

    private Image CreatePanelLine(string name, RectTransform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, Color color)
    {
        RectTransform rt = CreateRect(name, parent, anchorMin, anchorMax, offsetMin, offsetMax);
        Image img = rt.gameObject.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    private void SetValueText(Text txt, string value, Color color)
    {
        if (txt == null) return;
        txt.text = value;
        txt.color = color;
    }

    private void EnsureCanvasGroup()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
    }

    private bool IsBattleBlockingStatPanel()
    {
        BattleManager bm = BattleManager.Instance;
        if (bm == null) return false;

        bool inBattleState = bm.State == BattleState.BattleStart
            || bm.State == BattleState.RoundStart
            || bm.State == BattleState.PlayerTurn
            || bm.State == BattleState.EnemyTurn
            || bm.State == BattleState.Victory
            || bm.State == BattleState.Defeat;

        if (!inBattleState) return false;
        if (BattleUI.Instance == null || BattleUI.Instance.canvasGroup == null) return false;

        CanvasGroup cg = BattleUI.Instance.canvasGroup;
        return cg.alpha > 0.01f && cg.interactable && cg.blocksRaycasts;
    }

    private void ShowImmediate()
    {
        if (canvasGroup == null) return;
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }

    private void HideImmediate()
    {
        if (canvasGroup == null) return;
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }
}

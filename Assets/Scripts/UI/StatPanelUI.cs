using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class StatPanelUI : MonoBehaviour
{
    public static StatPanelUI Instance { get; private set; }

    [Header("표시 제어")]
    public CanvasGroup canvasGroup;

    [Header("레이아웃")]
    public float panelWidth = 280f;

    [Header("색상")]
    public Color panelColor = new Color(0.07f, 0.062f, 0.05f, 1f);
    public Color borderColor = new Color(0.35f, 0.29f, 0.16f, 1f);
    public Color accentGold = new Color(0.78f, 0.66f, 0.29f, 1f);
    public Color textMain = new Color(0.83f, 0.77f, 0.60f, 1f);
    public Color textDim = new Color(0.48f, 0.42f, 0.29f, 1f);

    public event System.Action<bool> OnStatPanelToggled;

    private bool isVisible;
    private Text headerTitleText;
    private Text headerKeyText;
    private Text statusText;
    private Text deckText;
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
    }

    private void Start()
    {
        if (CharacterStats.Instance != null)
            CharacterStats.Instance.OnStatsChanged += RefreshIfVisible;
        if (EquipmentManager.Instance != null)
            EquipmentManager.Instance.OnEquipChanged += RefreshIfVisible;

        Hide();
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
        Toggle();
    }

    public bool IsVisible => isVisible;

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
        if (stats != null && statusText != null)
        {
            int hpCurrent = Mathf.FloorToInt(stats.currentHP);
            int hpMax = Mathf.FloorToInt(stats.maxHP.FinalValue);
            int mana = Mathf.FloorToInt(stats.baseMana.FinalValue);
            int hand = Mathf.FloorToInt(stats.maxHand.FinalValue);
            int shield = Mathf.FloorToInt(stats.baseShield.FinalValue);
            int damage = BattleMath.CalcFinalDamage(stats.damageConst.FinalValue, stats.damagePer.FinalValue);
            int dodge = Mathf.FloorToInt(stats.baseDodge.FinalValue);

            statusText.text =
                $"체력   HP\n{hpCurrent} / {hpMax}\n\n" +
                $"지구력   Mana\n{mana}\n\n" +
                $"행동력   Hand\n{hand}\n\n" +
                $"방어   Shield\n{shield}\n\n" +
                $"피해량   Damage\n{damage}\n\n" +
                $"회피   Dodge\n{dodge}";
        }

        RefreshDeck();
    }

    private void RefreshDeck()
    {
        if (deckText == null || deckTotalText == null) return;

        StringBuilder sb = new StringBuilder();
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
                    counts[cardName] = counts.TryGetValue(cardName, out int oldCount) ? oldCount + 1 : 1;
                    totalCards++;
                }

                if (counts.Count <= 0) continue;

                if (sb.Length > 0)
                    sb.AppendLine().AppendLine();

                sb.Append('[').Append(equip.itemName).AppendLine("]");
                foreach (var pair in counts)
                    sb.Append(pair.Key).Append(" x").Append(pair.Value).AppendLine();
            }
        }

        if (sb.Length == 0)
            sb.Append("표시할 카드가 없습니다.");

        deckText.text = sb.ToString().TrimEnd();
        deckTotalText.text = $"Total  {totalCards} cards";
    }

    private void EnsureCanvasGroup()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
    }

    private void BuildLayout()
    {
        RectTransform root = GetComponent<RectTransform>();
        if (root == null) root = gameObject.AddComponent<RectTransform>();
        root.anchorMin = new Vector2(0f, 0f);
        root.anchorMax = new Vector2(0f, 1f);
        root.pivot = new Vector2(0f, 1f);
        root.offsetMin = new Vector2(0f, 0f);
        root.offsetMax = new Vector2(panelWidth, 0f);

        Image bg = GetComponent<Image>() ?? gameObject.AddComponent<Image>();
        bg.color = panelColor;

        Outline outline = GetComponent<Outline>() ?? gameObject.AddComponent<Outline>();
        outline.effectColor = borderColor;
        outline.effectDistance = new Vector2(1f, -1f);

        for (int i = transform.childCount - 1; i >= 0; i--)
            DestroyImmediate(transform.GetChild(i).gameObject);

        RectTransform header = CreateRect("Header", root, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -44f), new Vector2(0f, 0f));
        CreateImage(header, new Color(0f, 0f, 0f, 0f));
        headerTitleText = CreateText("Title", header, "CHARACTER", 12, accentGold, TextAnchor.MiddleLeft, FontStyle.Bold,
            new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(14f, 0f), new Vector2(-50f, 0f));
        headerKeyText = CreateText("Key", header, "C", 10, textDim, TextAnchor.MiddleCenter, FontStyle.Bold,
            new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-34f, -10f), new Vector2(-8f, 10f));

        RectTransform statusSection = CreateRect("StatusSection", root, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -220f), new Vector2(0f, -44f));
        CreateSectionHeader(statusSection, "STATUS");
        statusText = CreateText("StatusText", statusSection, "", 14, textMain, TextAnchor.UpperLeft, FontStyle.Normal,
            new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(14f, 12f), new Vector2(-14f, -24f));
        statusText.lineSpacing = 1.1f;

        RectTransform deckSection = CreateRect("DeckSection", root, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0f), new Vector2(0f, -220f));
        CreateSectionHeader(deckSection, "CARD DECK");
        deckText = CreateText("DeckText", deckSection, "", 13, textMain, TextAnchor.UpperLeft, FontStyle.Normal,
            new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(14f, 32f), new Vector2(-14f, -34f));
        deckText.lineSpacing = 1.05f;
        deckTotalText = CreateText("DeckTotal", deckSection, "Total  0 cards", 12, accentGold, TextAnchor.LowerRight, FontStyle.Bold,
            new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(14f, 8f), new Vector2(-14f, 26f));
    }

    private void CreateSectionHeader(RectTransform parent, string title)
    {
        CreateText(title + "Label", parent, title, 10, textDim, TextAnchor.UpperLeft, FontStyle.Bold,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(14f, -18f), new Vector2(-14f, 0f));
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

    private Image CreateImage(RectTransform parent, Color color)
    {
        Image img = parent.GetComponent<Image>() ?? parent.gameObject.AddComponent<Image>();
        img.color = color;
        return img;
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

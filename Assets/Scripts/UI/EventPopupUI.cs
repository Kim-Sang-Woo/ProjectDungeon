// ============================================================
// EventPopupUI.cs — 이벤트 팝업 UI 바인딩 + 레이아웃
// 위치: Assets/Scripts/UI/EventPopupUI.cs
// ============================================================
using UnityEngine;
using UnityEngine.UI;

public class EventPopupUI : MonoBehaviour
{
    [Header("공통")]
    public RectTransform header;
    public Text          eventNameText;

    [Header("설명 블록")]
    public RectTransform descBlock;
    public Text          eventDescText;

    [Header("이미지 블록")]
    public RectTransform illustBlock;
    public Image         eventImage;

    [Header("결과 블록 (ResultView 전용)")]
    public RectTransform resultBlock;
    public Text          eventResultText;

    [Header("효과 블록")]
    public RectTransform effectsBlock;
    public Text          effectsText;

    [Header("선택지 컨테이너")]
    public RectTransform     choicesContainer;
    public EventChoiceItemUI choiceItemPrefab;

    [Header("레이아웃 수치")]
    public float popupWidth    = 520f;
    public float headerHeight  = 44f;
    public float illustHeight  = 200f;
    public float blockPaddingV = 18f;
    public float paddingH      = 18f;
    public float choiceHeight  = 30f;
    public float choiceSpacing = 4f;

    [Header("색상")]
    public Color colorPopupBg    = new Color(0.067f, 0.063f, 0.035f, 1f);
    public Color colorHeaderBg   = new Color(0.078f, 0.071f, 0.031f, 1f);
    public Color colorDescBg     = new Color(0.051f, 0.047f, 0.035f, 1f);
    public Color colorIllustBg   = new Color(0.031f, 0.027f, 0.020f, 1f);
    public Color colorResultBg   = new Color(0.059f, 0.051f, 0.035f, 1f);
    public Color colorEffectBg   = new Color(0.051f, 0.047f, 0.035f, 1f);
    public Color colorChoiceBg   = new Color(0.051f, 0.047f, 0.035f, 1f);
    public Color colorNameText   = new Color(0.910f, 0.784f, 0.439f, 1f);
    public Color colorDescText   = new Color(0.847f, 0.784f, 0.596f, 1f);
    public Color colorResultText = new Color(0.627f, 0.565f, 0.439f, 1f);
    public Color colorEffectText = new Color(0.847f, 0.784f, 0.596f, 1f);

    private EventPopup          popup;
    private EventChoiceItemUI[] choiceItems = new EventChoiceItemUI[5];
    private bool                initialized = false;

    // ─────────────────────────────────────────────────────
    internal void EnsureInitialized()
    {
        if (initialized) return;
        initialized = true;

        popup = GetComponent<EventPopup>() ?? GetComponentInParent<EventPopup>();

        if (choicesContainer == null) { Debug.LogError("[EventPopupUI] choicesContainer 미연결"); return; }
        if (choiceItemPrefab == null) { Debug.LogError("[EventPopupUI] choiceItemPrefab 미연결"); return; }
        if (eventNameText    == null) { Debug.LogError("[EventPopupUI] eventNameText 미연결");    return; }
        if (eventDescText    == null) { Debug.LogError("[EventPopupUI] eventDescText 미연결");    return; }

        for (int i = 0; i < 5; i++)
        {
            var item = Instantiate(choiceItemPrefab, choicesContainer);
            item.gameObject.SetActive(false);
            choiceItems[i] = item;
        }

        ApplyStaticStyle();
        SetupChoicesContainer();
    }

    // ── 고정 스타일 (1회) ─────────────────────────────────
    private void ApplyStaticStyle()
    {
        SetBg(gameObject, colorPopupBg);

        if (header != null) SetBg(header.gameObject, colorHeaderBg);
        if (eventNameText != null)
        {
            eventNameText.fontSize  = 13;
            eventNameText.fontStyle = FontStyle.Bold;
            eventNameText.color     = colorNameText;
            eventNameText.alignment = TextAnchor.MiddleCenter;
        }

        if (descBlock != null) SetBg(descBlock.gameObject, colorDescBg);
        if (eventDescText != null)
        {
            eventDescText.fontSize    = 13;
            eventDescText.color       = colorDescText;
            eventDescText.alignment   = TextAnchor.UpperLeft;
            eventDescText.lineSpacing = 1.6f;
            eventDescText.horizontalOverflow = HorizontalWrapMode.Wrap;
            eventDescText.verticalOverflow   = VerticalWrapMode.Overflow;
        }

        if (illustBlock != null) SetBg(illustBlock.gameObject, colorIllustBg);
        if (eventImage != null)
        {
            eventImage.type           = Image.Type.Simple;
            eventImage.preserveAspect = true;
            RectTransform rt = eventImage.rectTransform;
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        if (resultBlock != null) SetBg(resultBlock.gameObject, colorResultBg);
        if (eventResultText != null)
        {
            eventResultText.fontSize    = 13;
            eventResultText.fontStyle   = FontStyle.Italic;
            eventResultText.color       = colorResultText;
            eventResultText.alignment   = TextAnchor.UpperLeft;
            eventResultText.lineSpacing = 1.6f;
            eventResultText.horizontalOverflow = HorizontalWrapMode.Wrap;
            eventResultText.verticalOverflow   = VerticalWrapMode.Overflow;
        }

        if (effectsBlock != null) SetBg(effectsBlock.gameObject, colorEffectBg);
        if (effectsText != null)
        {
            effectsText.fontSize        = 13;
            effectsText.color           = colorEffectText;
            effectsText.alignment       = TextAnchor.UpperLeft;
            effectsText.lineSpacing     = 1.6f;
            effectsText.supportRichText = true;
            effectsText.horizontalOverflow = HorizontalWrapMode.Wrap;
            effectsText.verticalOverflow   = VerticalWrapMode.Overflow;
        }

        if (choicesContainer != null) SetBg(choicesContainer.gameObject, colorChoiceBg);

        foreach (var item in choiceItems)
        {
            if (item == null) continue;
            SetBg(item.gameObject, new Color(1f, 1f, 1f, 0.018f));
            LayoutElement le = item.GetComponent<LayoutElement>() ?? item.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = choiceHeight;
            le.minHeight       = choiceHeight;
            if (item.numText   != null) { item.numText.fontSize = 13; item.numText.fontStyle = FontStyle.Bold; item.numText.color = new Color(0.784f, 0.627f, 0.251f); }
            if (item.badgeText != null) { item.badgeText.fontSize = 11; item.badgeText.fontStyle = FontStyle.Bold; }
            if (item.labelText != null) { item.labelText.fontSize = 12; item.labelText.color = new Color(0.847f, 0.784f, 0.596f); }
            item.ApplyLayout(choiceHeight, paddingH);
        }
    }

    private void SetupChoicesContainer()
    {
        VerticalLayoutGroup vlg = choicesContainer.GetComponent<VerticalLayoutGroup>()
                                  ?? choicesContainer.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.spacing                = choiceSpacing;
        vlg.padding                = new RectOffset((int)paddingH, (int)paddingH, (int)blockPaddingV, (int)blockPaddingV);
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth      = true;
        vlg.childControlHeight     = true;
    }

    // ── 동적 레이아웃 (뷰 전환마다) ──────────────────────
    private void RebuildLayout()
    {
        RectTransform panelRT = GetComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0.5f, 0.5f);
        panelRT.anchorMax = new Vector2(0.5f, 0.5f);
        panelRT.pivot     = new Vector2(0.5f, 0.5f);

        float totalH = 0f;

        if (header != null)
        {
            PlaceBlock(header, totalH, headerHeight);
            FillText(eventNameText, header);
            totalH += headerHeight;
        }

        if (descBlock != null)
        {
            float h = CalcTextHeight(eventDescText) + blockPaddingV * 2f;
            h = Mathf.Max(h, 30f);
            PlaceBlock(descBlock, totalH, h);
            FillText(eventDescText, descBlock);
            totalH += h;
        }

        if (illustBlock != null && illustBlock.gameObject.activeSelf)
        {
            PlaceBlock(illustBlock, totalH, illustHeight);
            totalH += illustHeight;
        }

        if (resultBlock != null && resultBlock.gameObject.activeSelf)
        {
            float h = CalcTextHeight(eventResultText) + blockPaddingV * 2f;
            h = Mathf.Max(h, 30f);
            PlaceBlock(resultBlock, totalH, h);
            FillText(eventResultText, resultBlock);
            totalH += h;
        }

        if (effectsBlock != null && effectsBlock.gameObject.activeSelf)
        {
            float h = CalcTextHeight(effectsText) + blockPaddingV * 2f;
            h = Mathf.Max(h, 30f);
            PlaceBlock(effectsBlock, totalH, h);
            FillText(effectsText, effectsBlock);
            totalH += h;
        }

        if (choicesContainer != null)
        {
            int visible = 0;
            foreach (var item in choiceItems)
                if (item != null && item.gameObject.activeSelf) visible++;

            float h = visible * (choiceHeight + choiceSpacing) - choiceSpacing + blockPaddingV * 2f;
            h = Mathf.Max(h, 40f);
            PlaceBlock(choicesContainer, totalH, h);
            totalH += h;
        }

        panelRT.sizeDelta        = new Vector2(popupWidth, totalH);
        panelRT.anchoredPosition = Vector2.zero;

        // 색상 재적용 (SetActive 이후 확실히 반영)
        SetBg(gameObject, colorPopupBg);
        if (header           != null) SetBg(header.gameObject,           colorHeaderBg);
        if (descBlock        != null) SetBg(descBlock.gameObject,        colorDescBg);
        if (illustBlock      != null) SetBg(illustBlock.gameObject,      colorIllustBg);
        if (resultBlock      != null) SetBg(resultBlock.gameObject,      colorResultBg);
        if (effectsBlock     != null) SetBg(effectsBlock.gameObject,     colorEffectBg);
        if (choicesContainer != null) SetBg(choicesContainer.gameObject, colorChoiceBg);

        LayoutRebuilder.ForceRebuildLayoutImmediate(panelRT);
    }

    // ── 블록 배치 헬퍼 ────────────────────────────────────

    private void PlaceBlock(RectTransform rt, float offsetFromTop, float height)
    {
        rt.anchorMin        = new Vector2(0f, 1f);
        rt.anchorMax        = new Vector2(1f, 1f);
        rt.pivot            = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -offsetFromTop);
        rt.sizeDelta        = new Vector2(0f, height);
    }

    private void FillText(Text t, RectTransform block)
    {
        if (t == null) return;
        RectTransform rt = t.rectTransform;
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.offsetMin = new Vector2(paddingH,  blockPaddingV);
        rt.offsetMax = new Vector2(-paddingH, -blockPaddingV);
    }

    private float CalcTextHeight(Text t)
    {
        if (t == null || string.IsNullOrEmpty(t.text)) return 0f;
        float lineH       = t.fontSize * t.lineSpacing;
        float innerWidth  = popupWidth - paddingH * 2f;
        int   charPerLine = Mathf.Max(1, Mathf.FloorToInt(innerWidth / (t.fontSize * 0.55f)));
        int   lines       = Mathf.CeilToInt((float)t.text.Length / charPerLine);
        lines = Mathf.Max(lines, t.text.Split('\n').Length);
        return lines * lineH;
    }

    // ── 뷰 바인딩 ─────────────────────────────────────────

    public void BindChoiceView(EventSession session)
    {
        SetName(session.sourceData.eventName);
        SetDesc(session.sourceData.desc);
        SetImage(session.sourceData.image);
        if (resultBlock  != null) resultBlock.gameObject.SetActive(false);
        if (effectsBlock != null) effectsBlock.gameObject.SetActive(false);
        BindChoices(session);
        RebuildLayout();
    }

    public void BindResultView(EventSession session)
    {
        EventResult result = session.currentResult;
        SetName(session.sourceData.eventName);
        SetDesc(session.sourceData.desc);
        if (result.resultImage != null) SetImage(result.resultImage);

        bool hasResult = !string.IsNullOrEmpty(result.resultDesc);
        if (resultBlock     != null) resultBlock.gameObject.SetActive(hasResult);
        if (eventResultText != null) eventResultText.text = result.resultDesc;

        bool hasEffect = !string.IsNullOrEmpty(session.effectSummaryText);
        if (effectsBlock != null) effectsBlock.gameObject.SetActive(hasEffect);
        if (effectsText  != null) effectsText.text = session.effectSummaryText;

        BindChoices(session);
        RebuildLayout();
    }

    // ── 내부 헬퍼 ─────────────────────────────────────────

    private void SetName(string n) { if (eventNameText != null) eventNameText.text = n; }
    private void SetDesc(string d) { if (eventDescText != null) eventDescText.text = d; }

    private void SetImage(Sprite s)
    {
        if (illustBlock != null) illustBlock.gameObject.SetActive(s != null);
        if (eventImage  != null && s != null) eventImage.sprite = s;
    }

    private void BindChoices(EventSession session)
    {
        var list = session.resolvedChoices;
        for (int i = 0; i < 5; i++)
        {
            if (choiceItems[i] == null) continue;
            if (i < list.Count) { int c = i; choiceItems[i].Bind(list[i], () => popup?.SelectChoice(c)); }
            else                { choiceItems[i].gameObject.SetActive(false); }
        }
    }

    private void SetBg(GameObject go, Color color)
    {
        Image img = go.GetComponent<Image>() ?? go.AddComponent<Image>();
        img.type   = Image.Type.Simple;
        img.sprite = null;
        img.color  = color;
    }
}

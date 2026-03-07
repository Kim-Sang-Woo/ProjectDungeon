// ============================================================
// EventChoiceItemUI.cs — 선택지 1개 프리팹 UI 제어
// 위치: Assets/Scripts/UI/EventChoiceItemUI.cs
// ============================================================
// [프리팹 구조]
//   ChoiceItem (Image + EventChoiceItemUI)
//     ├─ NumText          (Text) — "1."
//     ├─ BadgeGroup       (GameObject) — 확률/조건 뱃지 루트
//     │    └─ BadgeText   (Text) — "100%" / "낡은 열쇠" / "무기" / "공격 10"
//     ├─ LabelText        (Text) — 선택지 본문
//     └─ KeyHintText      (Text) — "1" ~ "5"
//
// [뱃지 색상]
//   Default 100%    : #78c058 (초록)
//   Default 50~99%  : #e8a040 (주황)
//   Default 0~49%   : #e06060 (빨강)
//   SpecialItem     : #70b8e8 (파랑)
//   SpecialEquip    : #d898e8 (보라)
//   SpecialStat     : #e8c860 (노랑)
//   Close           : 뱃지 없음, 본문 흐린 이탤릭 처리
// ============================================================
using UnityEngine;
using UnityEngine.UI;

public class EventChoiceItemUI : MonoBehaviour
{
    [Header("UI 요소")]
    public Text       numText;
    public GameObject badgeGroup;
    public Text       badgeText;
    public Text       labelText;
    // keyHintText 제거

    [Header("뱃지 색상")]
    public Color colorPctHigh   = new Color(0.47f, 0.75f, 0.35f); // 100%
    public Color colorPctMid    = new Color(0.91f, 0.63f, 0.25f); // 50~99%
    public Color colorPctLow    = new Color(0.88f, 0.38f, 0.38f); // 0~49%
    public Color colorItem      = new Color(0.44f, 0.72f, 0.91f); // SpecialItem
    public Color colorEquip     = new Color(0.85f, 0.60f, 0.91f); // SpecialEquip
    public Color colorStat      = new Color(0.91f, 0.78f, 0.38f); // SpecialStat
    public Color colorCloseText = new Color(0.48f, 0.42f, 0.29f); // Close 본문

    // ── 콜백 (EventPopupUI에서 주입) ─────────────────────
    private System.Action onClickCallback;

    // ─────────────────────────────────────────────────────

    /// <summary>ResolvedChoice 데이터를 바인딩하고 클릭 콜백을 연결한다.</summary>
    public void Bind(ResolvedChoice rc, System.Action onClick)
    {
        onClickCallback = onClick;

        // 조건 미충족 시 숨기기
        gameObject.SetActive(rc.isVisible);
        if (!rc.isVisible) return;

        // Button에 클릭 리스너 연결 (프리팹 Inspector 연결 불필요)
        Button btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => onClickCallback?.Invoke());
        }

        // 번호 텍스트
        if (numText != null)
            numText.text = $"{rc.keyIndex}.";

        // 본문
        if (labelText != null)
        {
            labelText.text  = rc.label;
            labelText.color = rc.badgeType == ChoiceType.Close
                ? colorCloseText
                : Color.white;
            labelText.fontStyle = rc.badgeType == ChoiceType.Close
                ? FontStyle.Italic
                : FontStyle.Normal;
        }

        // 뱃지
        bool showBadge = rc.badgeType != ChoiceType.Close && !string.IsNullOrEmpty(rc.badgeText);
        if (badgeGroup != null) badgeGroup.SetActive(showBadge);

        if (showBadge && badgeText != null)
        {
            badgeText.text  = rc.badgeText;
            badgeText.color = GetBadgeColor(rc);
        }
    }

    // ── 클릭 버튼 연결용 (Button.onClick 또는 EventTrigger) ──
    public void OnClick()
    {
        onClickCallback?.Invoke();
    }

    // ── 레이아웃 초기화 (EventPopupUI.ApplyChoiceItemLayout에서 호출) ──
    public void ApplyLayout(float itemHeight, float paddingH)
    {
        // ── 레이아웃: [번호(20)] [뱃지(56)] [본문(나머지)] ──
        float numW   = 20f;
        float badgeW = 56f;
        float gap    = 6f;

        // 번호 "1." — 좌측 고정
        if (numText != null)
        {
            RectTransform rt = numText.GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0f, 0f);
            rt.anchorMax        = new Vector2(0f, 1f);
            rt.pivot            = new Vector2(0f, 0.5f);
            rt.anchoredPosition = new Vector2(paddingH, 0f);
            rt.sizeDelta        = new Vector2(numW, 0f);
            numText.alignment   = TextAnchor.MiddleLeft;
            numText.fontSize    = 13;
            numText.fontStyle   = FontStyle.Bold;
            numText.color       = new Color(0.784f, 0.627f, 0.251f); // 골드
        }

        // 뱃지 그룹 — 번호 오른쪽
        if (badgeGroup != null)
        {
            RectTransform rt = badgeGroup.GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0f, 0.5f);
            rt.anchorMax        = new Vector2(0f, 0.5f);
            rt.pivot            = new Vector2(0f, 0.5f);
            rt.anchoredPosition = new Vector2(paddingH + numW + gap, 0f);
            rt.sizeDelta        = new Vector2(badgeW, 22f);
            if (badgeText != null) badgeText.alignment = TextAnchor.MiddleCenter;
        }

        // 본문 — 뱃지 오른쪽 ~ 우측 끝
        if (labelText != null)
        {
            RectTransform rt = labelText.GetComponent<RectTransform>();
            float left  = paddingH + numW + gap + badgeW + gap;
            float right = paddingH;
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot     = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(left,  4f);
            rt.offsetMax = new Vector2(-right, -4f);
            labelText.alignment = TextAnchor.MiddleLeft;
        }
    }

    // ─────────────────────────────────────────────────────

    private Color GetBadgeColor(ResolvedChoice rc)
    {
        switch (rc.badgeType)
        {
            case ChoiceType.Default:
                if (rc.source.successRate >= 100) return colorPctHigh;
                if (rc.source.successRate >= 50)  return colorPctMid;
                return colorPctLow;

            case ChoiceType.SpecialItem:  return colorItem;
            case ChoiceType.SpecialEquip: return colorEquip;
            case ChoiceType.SpecialStat:  return colorStat;
            default:                      return Color.white;
        }
    }
}

// ============================================================
// EventPopupUI.cs — 이벤트 팝업 전체 UI 바인딩
// 위치: Assets/Scripts/UI/EventPopupUI.cs
// ============================================================
// [개요]
//   기존 EventPopupUI를 완전 대체한다.
//   EventPopup(로직)이 세션 데이터를 넘기면 이 컴포넌트가 UI에 반영한다.
//
// [씬 계층 구조]
//   EventPopupPanel (CanvasGroup + EventPopup + EventPopupUI)
//     ├─ Header
//     │    └─ EventNameText        (Text)
//     ├─ DescBlock
//     │    └─ EventDescText        (Text + ContentSizeFitter)
//     ├─ IllustBlock               (GameObject — 이미지 없으면 SetActive false)
//     │    └─ EventImage           (Image)
//     ├─ ResultBlock               (GameObject — ResultView 전용)
//     │    └─ EventResultText      (Text + ContentSizeFitter)
//     ├─ EffectsBlock              (GameObject — 효과 있을 때만 표시)
//     │    └─ EffectsText          (Text + ContentSizeFitter)
//     └─ ChoicesContainer          (VerticalLayoutGroup)
//          └─ (ChoiceItem 프리팹 × N, 런타임 Instantiate)
// ============================================================
using UnityEngine;
using UnityEngine.UI;

public class EventPopupUI : MonoBehaviour
{
    [Header("공통")]
    public Text eventNameText;
    public Text eventDescText;

    [Header("이미지 (없으면 오브젝트 비활성)")]
    public GameObject illustBlock;
    public Image      eventImage;

    [Header("결과 블록 (ResultView 전용)")]
    public GameObject resultBlock;
    public Text       eventResultText;

    [Header("효과 블록 (효과 있을 때만 표시)")]
    public GameObject effectsBlock;
    public Text       effectsText;

    [Header("선택지 컨테이너")]
    public Transform           choicesContainer;
    public EventChoiceItemUI   choiceItemPrefab;

    // 현재 인스턴스 풀
    private EventChoiceItemUI[] choiceItems = new EventChoiceItemUI[5];

    // EventPopup 역참조 (선택지 클릭 콜백용)
    private EventPopup popup;

    // ─────────────────────────────────────────────────────
    private void Awake()
    {
        popup = GetComponent<EventPopup>();
        if (popup == null) popup = GetComponentInParent<EventPopup>();

        // 선택지 슬롯 미리 생성 (5개 고정 풀)
        for (int i = 0; i < 5; i++)
        {
            if (choiceItemPrefab == null) break;
            var item = Instantiate(choiceItemPrefab, choicesContainer);
            item.gameObject.SetActive(false);
            choiceItems[i] = item;
        }
    }

    // ── 뷰 바인딩 ─────────────────────────────────────────

    /// <summary>ChoiceView 데이터를 UI에 반영한다.</summary>
    public void BindChoiceView(EventSession session)
    {
        // 공통
        SetEventName(session.sourceData.eventName);
        SetDesc(session.sourceData.desc);
        SetImage(session.sourceData.image);

        // ResultView 전용 블록 숨기기
        if (resultBlock  != null) resultBlock.SetActive(false);
        if (effectsBlock != null) effectsBlock.SetActive(false);

        // 선택지 바인딩
        BindChoices(session);
    }

    /// <summary>ResultView 데이터를 UI에 반영한다.</summary>
    public void BindResultView(EventSession session)
    {
        EventResult result = session.currentResult;

        // 공통 (이름·설명 유지)
        SetEventName(session.sourceData.eventName);
        SetDesc(session.sourceData.desc);

        // 이미지: resultImage 우선, null이면 기존 유지
        if (result.resultImage != null)
            SetImage(result.resultImage);

        // 결과 설명
        if (resultBlock != null) resultBlock.SetActive(!string.IsNullOrEmpty(result.resultDesc));
        if (eventResultText != null) eventResultText.text = result.resultDesc;

        // 효과 텍스트
        bool hasEffect = !string.IsNullOrEmpty(session.effectSummaryText);
        if (effectsBlock != null) effectsBlock.SetActive(hasEffect);
        if (effectsText  != null) effectsText.text = session.effectSummaryText;

        // 선택지 바인딩
        BindChoices(session);

        // ContentSizeFitter 강제 갱신
        ForceLayoutRefresh();
    }

    // ── 내부 헬퍼 ─────────────────────────────────────────

    private void SetEventName(string name)
    {
        if (eventNameText != null) eventNameText.text = name;
    }

    private void SetDesc(string desc)
    {
        if (eventDescText != null)
        {
            eventDescText.text = desc;
            ForceLayoutRefresh(eventDescText.rectTransform);
        }
    }

    private void SetImage(Sprite sprite)
    {
        if (illustBlock != null) illustBlock.SetActive(sprite != null);
        if (eventImage  != null && sprite != null) eventImage.sprite = sprite;
    }

    private void BindChoices(EventSession session)
    {
        var list = session.resolvedChoices;

        for (int i = 0; i < 5; i++)
        {
            if (choiceItems[i] == null) continue;

            if (i < list.Count)
            {
                int captured = i; // 클로저 캡처용
                choiceItems[i].Bind(list[i], () => popup?.SelectChoice(captured));
            }
            else
            {
                choiceItems[i].gameObject.SetActive(false);
            }
        }
    }

    private void ForceLayoutRefresh(RectTransform target = null)
    {
        RectTransform rt = target ?? GetComponent<RectTransform>();
        if (rt != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
    }
}

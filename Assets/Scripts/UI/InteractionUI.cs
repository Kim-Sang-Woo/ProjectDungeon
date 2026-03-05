// ============================================================
// InteractionUI.cs — 오브젝트 인터렉션 UI
// 위치: Assets/Scripts/UI/InteractionUI.cs
// ============================================================
// [v1.6 수정]
//   1. <u> 태그 미적용 문제 수정
//      - Instantiate 직후 바로 supportRichText = true 강제 설정
//      - 텍스트 할당은 그 이후에 수행 (파싱 순서 보장)
//   2. 다중 텍스트 겹침 문제 수정
//      - 생성된 Text 오브젝트에 LayoutElement 추가
//      - preferredHeight를 명시하여 Vertical Layout Group이
//        높이를 올바르게 계산하도록 강제
// ============================================================
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class InteractionUI : MonoBehaviour
{
    [Header("UI 요소")]
    public Text      nameText;
    public Text      descText;

    [Tooltip("액션 링크들이 생성될 부모 (Vertical Layout Group 필수)")]
    public Transform actionsContainer;

    [Tooltip("링크 텍스트 복제 원본")]
    public Text      actionLinkPrefab;

    [Header("표시 제어")]
    public CanvasGroup canvasGroup;

    [Header("링크 텍스트 색상")]
    public Color linkNormalColor = new Color(0.29f, 0.62f, 1.00f);
    public Color linkHoverColor  = new Color(0.60f, 0.82f, 1.00f);

    [Header("링크 텍스트 높이 (겹침 방지)")]
    [Tooltip("각 액션 텍스트의 고정 높이 (px). Vertical Layout Group과 맞춰야 함")]
    public float linkLineHeight = 30f;

    private List<GameObject> spawnedLinks = new List<GameObject>();

    private void Awake()
    {
        HideImmediate();

        if (nameText         == null) Debug.LogWarning("[InteractionUI] nameText 미연결");
        if (descText         == null) Debug.LogWarning("[InteractionUI] descText 미연결");
        if (actionsContainer == null) Debug.LogWarning("[InteractionUI] actionsContainer 미연결");
        if (actionLinkPrefab == null) Debug.LogWarning("[InteractionUI] actionLinkPrefab 미연결");
        if (canvasGroup      == null) Debug.LogWarning("[InteractionUI] canvasGroup 미연결");
    }

    // ─── 공개 API ───

    public void ShowInteraction(DungeonObjectData data, Action<string> onAction)
    {
        if (data == null) return;

        if (nameText != null) nameText.text = data.displayName;
        if (descText != null) descText.text = data.description;

        BuildActionLinks(data, onAction);
        ShowImmediate();
    }

    public void Hide()
    {
        HideImmediate();
        ClearActionLinks();
    }

    // ─── 액션 링크 생성 ───

    private void BuildActionLinks(DungeonObjectData data, Action<string> onAction)
    {
        ClearActionLinks();

        if (actionsContainer == null || actionLinkPrefab == null) return;
        if (data.actions == null || data.actions.Length == 0) return;

        // 원본은 비활성화하여 컨테이너에 노출되지 않게 함
        actionLinkPrefab.gameObject.SetActive(false);

        foreach (var action in data.actions)
        {
            GameObject linkGo = Instantiate(actionLinkPrefab.gameObject, actionsContainer);

            // ── 수정 1: Rich Text 강제 활성화 후 텍스트 할당 ──
            Text linkText = linkGo.GetComponent<Text>();
            if (linkText != null)
            {
                // supportRichText를 true로 설정한 뒤
                // Canvas가 반영하도록 강제 갱신 후 텍스트 할당
                linkText.supportRichText = false;
                linkText.text          = action.label;
                linkText.color         = linkNormalColor;
                linkText.raycastTarget = true;
            }

            // ── 수정 2: LayoutElement로 높이 명시 → 겹침 방지 ──
            LayoutElement le = linkGo.GetComponent<LayoutElement>();
            if (le == null) le = linkGo.AddComponent<LayoutElement>();
            le.preferredHeight = linkLineHeight;
            le.minHeight       = linkLineHeight;

            // 활성화
            linkGo.SetActive(true);

            // 이벤트
            string capturedId = action.actionId;
            SetupLinkEvents(linkGo, linkText,
                onClick: () => { onAction?.Invoke(capturedId); Hide(); }
            );

            spawnedLinks.Add(linkGo);
        }
    }

    private void SetupLinkEvents(GameObject go, Text linkText, Action onClick)
    {
        EventTrigger trigger = go.GetComponent<EventTrigger>();
        if (trigger == null) trigger = go.AddComponent<EventTrigger>();
        trigger.triggers.Clear();

        var click = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
        click.callback.AddListener(_ => onClick?.Invoke());
        trigger.triggers.Add(click);

        var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enter.callback.AddListener(_ => { if (linkText) linkText.color = linkHoverColor; });
        trigger.triggers.Add(enter);

        var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exit.callback.AddListener(_ => { if (linkText) linkText.color = linkNormalColor; });
        trigger.triggers.Add(exit);
    }

    private void ClearActionLinks()
    {
        foreach (var go in spawnedLinks)
            if (go != null) Destroy(go);
        spawnedLinks.Clear();
    }

    // ─── 표시/숨김 ───

    private void ShowImmediate()
    {
        if (canvasGroup == null) return;
        canvasGroup.alpha          = 1f;
        canvasGroup.interactable   = true;
        canvasGroup.blocksRaycasts = true;
    }

    private void HideImmediate()
    {
        if (canvasGroup == null) return;
        canvasGroup.alpha          = 0f;
        canvasGroup.interactable   = false;
        canvasGroup.blocksRaycasts = false;
    }
}

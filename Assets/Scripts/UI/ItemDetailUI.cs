// ============================================================
// ItemDetailUI.cs — 아이템 상세 UI
// 위치: Assets/Scripts/UI/ItemDetailUI.cs
// ============================================================
// [개요]
//   인벤토리에서 아이템 클릭 시 표시되는 상세 UI.
//   이름/설명 표시 + 버리기 액션 제공.
//   InteractionUI와 동일한 CanvasGroup 방식 사용.
//
// [Canvas 계층]
//   Canvas
//   └── ItemDetailPanel       ← ItemDetailUI.cs 부착 + CanvasGroup
//         ├── ItemNameText
//         ├── ItemDescText
//         ├── ItemPriceText
//         ├── ItemWeightText
//         └── ActionsContainer (Vertical Layout Group)
//
// [Inspector 연결]
//   itemNameText   → ItemNameText
//   itemDescText   → ItemDescText
//   itemPriceText  → ItemPriceText
//   itemWeightText → ItemWeightText
//   actionsContainer→ ActionsContainer
//   actionLinkPrefab→ ActionLinkText (복제 원본)
//   canvasGroup    → CanvasGroup
// ============================================================
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ItemDetailUI : MonoBehaviour
{
    [Header("UI 요소")]
    public Text      itemNameText;
    public Text      itemDescText;
    public Text      itemPriceText;
    public Text      itemWeightText;
    public Transform actionsContainer;
    public Text      actionLinkPrefab;

    [Header("표시 제어")]
    public CanvasGroup canvasGroup;

    [Header("링크 색상")]
    public Color linkNormalColor = new Color(0.29f, 0.62f, 1.00f);
    public Color linkHoverColor  = new Color(0.60f, 0.82f, 1.00f);

    [Header("링크 높이")]
    public float linkLineHeight = 30f;

    private List<GameObject> spawnedLinks = new List<GameObject>();

    private void Awake()
    {
        HideImmediate();
    }

    // ─── 공개 API ───

    /// <summary>
    /// 아이템 상세 UI를 표시한다.
    /// </summary>
    /// <param name="slot">표시할 슬롯</param>
    /// <param name="slotIndex">버리기 처리용 슬롯 인덱스</param>
    /// <param name="onAction">actionId 콜백</param>
    public void Show(InventorySlot slot, int slotIndex, Action<string, int> onAction)
    {
        if (slot == null) return;

        if (itemNameText   != null) itemNameText.text   = slot.item.itemName;
        if (itemDescText   != null) itemDescText.text   = slot.item.description;
        if (itemPriceText  != null) itemPriceText.text  = $"가격: {slot.item.price}G";
        if (itemWeightText != null) itemWeightText.text = $"무게: {slot.item.weight:F1}kg";

        BuildActionLinks(slotIndex, onAction);
        ShowImmediate();
    }

    public void Hide()
    {
        HideImmediate();
        ClearLinks();
    }

    // ─── 액션 링크 생성 ───

    private void BuildActionLinks(int slotIndex, Action<string, int> onAction)
    {
        ClearLinks();
        if (actionsContainer == null || actionLinkPrefab == null) return;

        actionLinkPrefab.gameObject.SetActive(false);

        // 버리기
        SpawnLink("버리기", "delete", slotIndex, onAction);
    }

    private void SpawnLink(string label, string actionId, int slotIndex, Action<string, int> onAction)
    {
        GameObject go = Instantiate(actionLinkPrefab.gameObject, actionsContainer);
        Text t = go.GetComponent<Text>();
        if (t != null)
        {
            t.supportRichText = false;
            t.text            = label;
            t.color           = linkNormalColor;
            t.raycastTarget   = true;
        }

        LayoutElement le = go.GetComponent<LayoutElement>();
        if (le == null) le = go.AddComponent<LayoutElement>();
        le.preferredHeight = linkLineHeight;
        le.minHeight       = linkLineHeight;

        go.SetActive(true);

        EventTrigger trigger = go.GetComponent<EventTrigger>();
        if (trigger == null) trigger = go.AddComponent<EventTrigger>();
        trigger.triggers.Clear();

        string capturedId  = actionId;
        int    capturedIdx = slotIndex;

        var click = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
        click.callback.AddListener(_ => { onAction?.Invoke(capturedId, capturedIdx); Hide(); });
        trigger.triggers.Add(click);

        var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enter.callback.AddListener(_ => { if (t) t.color = linkHoverColor; });
        trigger.triggers.Add(enter);

        var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exit.callback.AddListener(_ => { if (t) t.color = linkNormalColor; });
        trigger.triggers.Add(exit);

        spawnedLinks.Add(go);
    }

    private void ClearLinks()
    {
        foreach (var go in spawnedLinks)
            if (go != null) Destroy(go);
        spawnedLinks.Clear();
    }

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

// ============================================================
// ItemDetailUI.cs — 아이템 상세 UI
// 위치: Assets/Scripts/UI/ItemDetailUI.cs
// ============================================================
// [Canvas 계층]
//   Canvas
//   └── ItemDetailPanel       ← ItemDetailUI.cs 부착 + CanvasGroup
//         ├── ItemNameText
//         ├── ItemEquipStatsText  ← 장비 능력치 (장비 아이템만 표시)
//         ├── ItemDescText
//         ├── ItemPriceText
//         ├── ItemWeightText
//         └── ActionsContainer (Vertical Layout Group)
//
// [Inspector 연결]
//   itemNameText        → ItemNameText
//   itemEquipStatsText  → ItemEquipStatsText
//   itemDescText        → ItemDescText
//   itemPriceText       → ItemPriceText
//   itemWeightText      → ItemWeightText
//   actionsContainer    → ActionsContainer
//   actionLinkPrefab    → ActionLinkText (복제 원본)
//   canvasGroup         → CanvasGroup
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
    public Text      itemEquipStatsText;  // 장비 능력치 (장비 아이템만)
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

    private void Awake() => HideImmediate();

    // ─── 공개 API ───

    /// <summary>인벤토리 일반 아이템 표시</summary>
    public void Show(InventorySlot slot, int slotIndex, Action<string, int> onAction)
    {
        if (slot == null) return;

        SetBasicInfo(slot.item.itemName, slot.item.description,
                     slot.item.price, slot.item.weight);

        // 장비 아이템인지 확인
        EquipData equip = slot.item as EquipData;
        SetEquipStats(equip);

        BuildInventoryActions(slotIndex, equip, onAction);
        ShowImmediate();
    }

    /// <summary>장착된 장비 슬롯 클릭 시 표시</summary>
    public void ShowEquipped(EquipData equip, EquipType slot)
    {
        if (equip == null) return;

        SetBasicInfo(equip.itemName, equip.description, equip.price, equip.weight);
        SetEquipStats(equip);

        BuildEquippedActions(slot);
        ShowImmediate();
    }

    public void Hide()
    {
        HideImmediate();
        ClearLinks();
    }

    // ─── 내부 UI 구성 ───

    private void SetBasicInfo(string name, string desc, int price, float weight)
    {
        if (itemNameText   != null) itemNameText.text   = name;
        if (itemDescText   != null) itemDescText.text   = desc;
        if (itemPriceText  != null) itemPriceText.text  = $"가격: {price}G";
        if (itemWeightText != null) itemWeightText.text = $"무게: {weight:F1}kg";
    }

    private void SetEquipStats(EquipData equip)
    {
        if (itemEquipStatsText == null) return;

        if (equip == null || !equip.HasStats)
        {
            itemEquipStatsText.gameObject.SetActive(false);
            return;
        }

        itemEquipStatsText.gameObject.SetActive(true);

        var sb = new System.Text.StringBuilder();
        void Add(string label, float val)
        {
            if (val == 0) return;
            sb.AppendLine($"{label}: {(val > 0 ? "+" : "")}{val}");
        }
        void AddInt(string label, int val)
        {
            if (val == 0) return;
            sb.AppendLine($"{label}: {(val > 0 ? "+" : "")}{val}");
        }

        Add("체력",       equip.statMaxHP);
        Add("회복력",     equip.statHPGen);
        Add("지구력",     equip.statBaseMana);
        Add("행동력",     equip.statMaxHand);
        Add("방어력",     equip.statBaseShield);
        Add("피해량%",    equip.statDamagePer);
        Add("피해량+",    equip.statDamageConst);
        Add("회피",       equip.statBaseDodge);
        AddInt("슬롯+",   equip.statMaxItemSlot);
        Add("무게한도+",  equip.statMaxCarryWeight);

        itemEquipStatsText.text = sb.ToString().TrimEnd();
    }

    // ─── 액션 링크 ───

    /// <summary>인벤토리 아이템 액션 — 장비면 '장착', 항상 '버리기'</summary>
    private void BuildInventoryActions(int slotIndex, EquipData equip,
                                       Action<string, int> onAction)
    {
        ClearLinks();
        if (actionsContainer == null || actionLinkPrefab == null) return;
        actionLinkPrefab.gameObject.SetActive(false);

        if (equip != null)
            SpawnLink("장착", "equip", slotIndex, onAction);

        SpawnLink("버리기", "delete", slotIndex, onAction);
    }

    /// <summary>장착된 장비 액션 — '장착 해제'만 표시 (버리기 불가)</summary>
    private void BuildEquippedActions(EquipType slot)
    {
        ClearLinks();
        if (actionsContainer == null || actionLinkPrefab == null) return;
        actionLinkPrefab.gameObject.SetActive(false);

        EquipType capturedSlot = slot;
        SpawnLinkDirect("장착 해제", () =>
        {
            EquipmentManager.Instance?.Unequip(capturedSlot);
            Hide();
        });
    }

    private void SpawnLink(string label, string actionId, int slotIndex,
                           Action<string, int> onAction)
    {
        SpawnLinkDirect(label, () =>
        {
            onAction?.Invoke(actionId, slotIndex);
            Hide();
        });
    }

    private void SpawnLinkDirect(string label, Action onClick)
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

        var click = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
        click.callback.AddListener(_ => onClick?.Invoke());
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
        canvasGroup.alpha = 1f; canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }

    private void HideImmediate()
    {
        if (canvasGroup == null) return;
        canvasGroup.alpha = 0f; canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }
}


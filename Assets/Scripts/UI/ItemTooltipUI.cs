// ============================================================
// ItemTooltipUI.cs — v1.5 Final
// 위치: Assets/Scripts/UI/ItemTooltipUI.cs
// ============================================================
// [개요]
//   장비/아이템 슬롯 호버 시 슬롯 오른쪽 아래에 툴팁 표시.
//   화면 오른쪽/아래쪽 밖으로 나가면 자동으로 반대쪽에 배치.
//
// [Canvas 계층]
//   Canvas
//   └── ItemTooltipPanel  ← ItemTooltipUI.cs + CanvasGroup
//         ├── TooltipNameText
//         ├── TooltipTypeText
//         ├── TooltipStatsText
//         ├── TooltipDescText
//         └── TooltipPriceWeightText
//
// [Inspector 연결]
//   tooltipNameText        → TooltipNameText
//   tooltipTypeText        → TooltipTypeText
//   tooltipStatsText       → TooltipStatsText
//   tooltipDescText        → TooltipDescText
//   tooltipPriceWeightText → TooltipPriceWeightText
//   canvasGroup            → ItemTooltipPanel > CanvasGroup
//   tooltipRect            → ItemTooltipPanel > RectTransform
// ============================================================
using UnityEngine;
using UnityEngine.UI;

public class ItemTooltipUI : MonoBehaviour
{
    public static ItemTooltipUI Instance { get; private set; }

    [Header("UI 요소")]
    public Text tooltipNameText;
    public Text tooltipTypeText;
    public Text tooltipStatsText;
    public Text tooltipDescText;
    public Text tooltipPriceWeightText;

    [Header("표시 제어")]
    public CanvasGroup   canvasGroup;
    public RectTransform tooltipRect;

    [Header("여백")]
    [Tooltip("슬롯과 툴팁 사이 간격 (픽셀)")]
    public float offset = 8f;

    private Canvas rootCanvas;

    // ────────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        rootCanvas = GetComponentInParent<Canvas>();
        if (rootCanvas != null) rootCanvas = rootCanvas.rootCanvas;

        // anchor 중앙(0.5, 0.5) 고정 — anchoredPosition을 Canvas 중앙 기준으로 사용
        // pivot (0, 1) — 툴팁 박스는 좌상단 기준으로 펼쳐짐
        if (tooltipRect != null)
        {
            tooltipRect.anchorMin = new Vector2(0.5f, 0.5f);
            tooltipRect.anchorMax = new Vector2(0.5f, 0.5f);
            tooltipRect.pivot     = new Vector2(0f,   1f);
        }

        SetupAutoLayout();
        HideImmediate();
    }

    // ────────────────────────────────────────────────────────
    // 공개 API
    // ────────────────────────────────────────────────────────

    public void ShowForSlot(InventorySlot slot, RectTransform slotRect)
    {
        if (slot == null || slot.item == null) return;
        PopulateInventoryItem(slot);
        ShowImmediate();
        RefreshLayout();
        PositionAtSlot(slotRect);
    }

    public void ShowForEquip(EquipData equip, RectTransform slotRect)
    {
        if (equip == null) return;
        PopulateEquipItem(equip);
        ShowImmediate();
        RefreshLayout();
        PositionAtSlot(slotRect);
    }

    public void Hide() => HideImmediate();

    // ────────────────────────────────────────────────────────
    // 위치 계산 — 슬롯 기준
    // ────────────────────────────────────────────────────────

    private void PositionAtSlot(RectTransform slotRect)
    {
        if (tooltipRect == null || tooltipRect.parent == null) return;

        RectTransform parentRT = tooltipRect.parent as RectTransform;
        if (parentRT == null) return;

        Camera cam = (rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            ? rootCanvas.worldCamera : null;

        // 슬롯 네 꼭짓점 스크린 좌표
        // ScreenSpaceOverlay: GetWorldCorners() = 스크린 픽셀 좌표 그대로
        Vector3[] corners = new Vector3[4];
        slotRect.GetWorldCorners(corners);
        // [0]=좌하 [1]=좌상 [2]=우상 [3]=우하

        Vector2 screenRightBottom = new Vector2(corners[3].x, corners[0].y);
        Vector2 screenRightTop    = new Vector2(corners[2].x, corners[1].y);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRT, screenRightBottom, cam, out Vector2 localRB);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRT, screenRightTop,    cam, out Vector2 localRT2);

        float tw = tooltipRect.rect.width;
        float th = tooltipRect.rect.height;
        float pw = parentRT.rect.width;
        float ph = parentRT.rect.height;

        // 기본: 슬롯 오른쪽 아래
        float x = localRB.x + offset;
        float y = localRB.y - offset;

        // 오른쪽 밖 → 슬롯 왼쪽
        if (x + tw > pw * 0.5f)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRT, new Vector2(corners[0].x, corners[0].y), cam, out Vector2 localLB);
            x = localLB.x - tw - offset;
        }

        // 아래쪽 밖 → 슬롯 위
        if (y - th < -ph * 0.5f)
            y = localRT2.y + offset;

        tooltipRect.anchoredPosition = new Vector2(x, y);
    }

    // ────────────────────────────────────────────────────────
    // 내용 채우기
    // ────────────────────────────────────────────────────────

    private void PopulateInventoryItem(InventorySlot slot)
    {
        ItemData item = slot.item;
        if (tooltipNameText != null)
        {
            string nameWithQty = (item.isStackable && slot.quantity > 1)
                ? $"{item.itemName}  x{slot.quantity}"
                : item.itemName;
            tooltipNameText.text = nameWithQty;
        }

        if (tooltipTypeText != null)
        {
            EquipData e = item as EquipData;
            tooltipTypeText.text = e != null ? EquipTypeLabel(e.equipType) : "소모품";
        }

        if (tooltipStatsText != null)
        {
            EquipData e = item as EquipData;
            bool hasStats = e != null && e.HasStats;
            tooltipStatsText.gameObject.SetActive(hasStats);
            tooltipStatsText.text = hasStats ? BuildEquipStats(e) : "";
        }

        if (tooltipDescText != null) tooltipDescText.text = item.description;

        if (tooltipPriceWeightText != null)
        {
            string qty = item.isStackable && slot.quantity > 1 ? $"(x{slot.quantity})" : "";
            tooltipPriceWeightText.text = $"{item.price}G{qty}  [{item.weight:F1} Kg]";
        }
    }

    private void PopulateEquipItem(EquipData equip)
    {
        if (tooltipNameText  != null) tooltipNameText.text  = equip.itemName;
        if (tooltipTypeText  != null) tooltipTypeText.text  = EquipTypeLabel(equip.equipType) + "  [장착 중]";

        if (tooltipStatsText != null)
        {
            tooltipStatsText.gameObject.SetActive(equip.HasStats);
            tooltipStatsText.text = equip.HasStats ? BuildEquipStats(equip) : "";
        }

        if (tooltipDescText != null) tooltipDescText.text = equip.description;
        if (tooltipPriceWeightText != null)
            tooltipPriceWeightText.text = $"{equip.price}G  [{equip.weight:F1} Kg]";
    }

    // ────────────────────────────────────────────────────────
    // 표시/숨김
    // ────────────────────────────────────────────────────────

    // ────────────────────────────────────────────────────────
    // 자동 레이아웃 설정
    // ────────────────────────────────────────────────────────

    private void SetupAutoLayout()
    {
        if (tooltipRect == null) return;
        GameObject panel = tooltipRect.gameObject;

        // ── Vertical Layout Group ──
        VerticalLayoutGroup vlg = panel.GetComponent<VerticalLayoutGroup>()
                                ?? panel.AddComponent<VerticalLayoutGroup>();
        vlg.padding             = new RectOffset(10, 10, 10, 10);
        vlg.spacing             = 8f;
        vlg.childAlignment      = TextAnchor.UpperLeft;
        vlg.childControlWidth   = true;
        vlg.childControlHeight  = true;   // 자식 높이를 내용에 맞게 제어
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;

        // ── Content Size Fitter ──
        ContentSizeFitter csf = panel.GetComponent<ContentSizeFitter>()
                              ?? panel.AddComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        csf.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;

        // ── 각 Text 자식: Height 고정 해제 + 자동 줄바꿈 ──
        Text[] texts = { tooltipNameText, tooltipTypeText,
                         tooltipStatsText, tooltipDescText, tooltipPriceWeightText };
        foreach (Text t in texts)
        {
            if (t == null) continue;

            // 텍스트 줄바꿈 설정
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow   = VerticalWrapMode.Overflow;

            // Layout Element — 고정 Height 해제, preferred height는 텍스트가 자동 계산
            LayoutElement le = t.GetComponent<LayoutElement>()
                             ?? t.gameObject.AddComponent<LayoutElement>();
            le.minHeight        = -1;   // 제한 없음
            le.preferredHeight  = -1;   // 텍스트 내용에 따라 자동
            le.flexibleWidth    = 1f;
            le.flexibleHeight   = 0f;   // 세로 강제 확장 금지 (내용 크기만큼만)
        }
    }

    /// <summary>내용 변경 후 패널 크기를 즉시 갱신</summary>
    private void RefreshLayout()
    {
        if (tooltipRect == null) return;
        LayoutRebuilder.ForceRebuildLayoutImmediate(tooltipRect);
    }

    private void ShowImmediate()
    {
        if (canvasGroup == null) return;
        canvasGroup.alpha          = 1f;
        canvasGroup.interactable   = false;
        canvasGroup.blocksRaycasts = false;
    }

    private void HideImmediate()
    {
        if (canvasGroup == null) return;
        canvasGroup.alpha          = 0f;
        canvasGroup.interactable   = false;
        canvasGroup.blocksRaycasts = false;
    }

    // ────────────────────────────────────────────────────────
    // 유틸
    // ────────────────────────────────────────────────────────

    private string BuildEquipStats(EquipData e)
    {
        var sb = new System.Text.StringBuilder();
        void Add(string label, float val)
        {
            if (val != 0) sb.AppendLine($"{label}  {(val > 0 ? "+" : "")}{val:F0}");
        }
        Add("체력",   e.statMaxHP);
        Add("회복력", e.statHPGen);
        Add("지구력", e.statBaseMana);
        Add("행동력", e.statMaxHand);
        Add("방어력", e.statBaseShield);
        Add("피해량", e.statDamageConst);
        Add("피해%",  e.statDamagePer);
        Add("회피",   e.statBaseDodge);
        // 소지량 — 정수 표시
        if (e.statMaxItemSlot != 0)
            sb.AppendLine($"소지량  {(e.statMaxItemSlot > 0 ? "+" : "")}{e.statMaxItemSlot:F0}");
        // 무게한도 — 소수점 1자리 + kg 단위
        if (e.statMaxCarryWeight != 0)
            sb.AppendLine($"무게  {(e.statMaxCarryWeight > 0 ? "+" : "")}{e.statMaxCarryWeight:F1}kg");
        return sb.ToString().TrimEnd();
    }

    private string EquipTypeLabel(EquipType t)
    {
        switch (t)
        {
            case EquipType.Weapon:   return "무기";
            case EquipType.Armor:    return "갑옷";
            case EquipType.Gloves:   return "장갑";
            case EquipType.Boots:    return "신발";
            case EquipType.Ring:     return "반지";
            case EquipType.Necklace: return "목걸이";
            case EquipType.Amulet:   return "장신구";
            case EquipType.Bag:      return "가방";
            default: return t.ToString();
        }
    }
}

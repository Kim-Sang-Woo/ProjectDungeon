// ============================================================
// InventoryHintUI.cs — 하단 힌트 표시
// 위치: Assets/Scripts/UI/InventoryHintUI.cs
// ============================================================
// [개요]
//   화면 하단에 단축키 힌트를 항상 표시한다.
//   가방 [Tab]  능력치 [C]
// ============================================================
using UnityEngine;
using UnityEngine.UI;

public class InventoryHintUI : MonoBehaviour
{
    private const string DefaultHintClosed = "가방 [Tab], 능력치 [C]";
    private const string DefaultHintInventoryOpen = "가방 닫기 [Tab], 능력치 [C], 장착/해제 [우클릭], 버리기 [Ctrl+우클릭]";
    private const string DefaultHintStatOpen = "가방 [Tab], 능력치 닫기 [C]";

    [Tooltip("힌트 텍스트 오브젝트")]
    public Text hintText;

    private bool isInventoryOpen;
    private bool isStatOpen;

    private void Awake()
    {
        EnsureStatPanelExists();
    }

    private void Start()
    {
        RefreshHint();

        if (InventoryUI.Instance != null)
            InventoryUI.Instance.OnInventoryToggled += HandleInventoryToggled;
        if (StatPanelUI.Instance != null)
            StatPanelUI.Instance.OnStatPanelToggled += HandleStatToggled;

        if (hintText != null)
            hintText.text = DefaultHintClosed;
    }

    private void Update()
    {
        bool inventoryVisible = InventoryUI.Instance != null && InventoryUI.Instance.canvasGroup != null && InventoryUI.Instance.canvasGroup.alpha > 0.01f;
        bool statVisible = StatPanelUI.Instance != null && StatPanelUI.Instance.IsVisible;

        if (inventoryVisible != isInventoryOpen || statVisible != isStatOpen)
        {
            isInventoryOpen = inventoryVisible;
            isStatOpen = statVisible;
            RefreshHint();
        }
    }

    private void OnDestroy()
    {
        if (InventoryUI.Instance != null)
            InventoryUI.Instance.OnInventoryToggled -= HandleInventoryToggled;
        if (StatPanelUI.Instance != null)
            StatPanelUI.Instance.OnStatPanelToggled -= HandleStatToggled;
    }

    private void HandleInventoryToggled(bool isOpen)
    {
        isInventoryOpen = isOpen;
        if (isOpen) isStatOpen = false;
        RefreshHint();
    }

    private void HandleStatToggled(bool isOpen)
    {
        isStatOpen = isOpen;
        if (isOpen) isInventoryOpen = false;
        RefreshHint();
    }

    private void RefreshHint()
    {
        if (hintText == null) return;

        bool inventoryVisible = InventoryUI.Instance != null && InventoryUI.Instance.canvasGroup != null
            && InventoryUI.Instance.canvasGroup.alpha > 0.01f && InventoryUI.Instance.canvasGroup.blocksRaycasts;
        bool statVisible = StatPanelUI.Instance != null && StatPanelUI.Instance.IsVisible;

        if (inventoryVisible)
            hintText.text = DefaultHintInventoryOpen;
        else if (statVisible)
            hintText.text = DefaultHintStatOpen;
        else
            hintText.text = DefaultHintClosed;
    }

    private void EnsureStatPanelExists()
    {
        if (StatPanelUI.Instance != null) return;

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return;

        GameObject go = new GameObject("StatPanelUI", typeof(RectTransform), typeof(CanvasGroup), typeof(UnityEngine.UI.Image), typeof(StatPanelUI));
        go.transform.SetParent(canvas.transform, false);
        go.transform.SetAsFirstSibling();
    }
}

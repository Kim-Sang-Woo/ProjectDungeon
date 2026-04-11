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
    [Tooltip("힌트 텍스트 오브젝트")]
    public Text hintText;

    [Tooltip("기본 힌트")]
    public string hintClosed = "가방 [Tab]    능력치 [C]";

    [Tooltip("가방 열린 상태 힌트")]
    public string hintInventoryOpen = "가방 닫기 [Tab]    능력치 [C]";

    [Tooltip("능력치 패널 열린 상태 힌트")]
    public string hintStatOpen = "능력치 닫기 [C]";

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
            hintText.text = hintClosed;
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

        if (isStatOpen)
            hintText.text = hintStatOpen;
        else if (isInventoryOpen)
            hintText.text = hintInventoryOpen;
        else
            hintText.text = hintClosed;
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

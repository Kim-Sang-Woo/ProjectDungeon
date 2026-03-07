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

    [Tooltip("가방 닫힌 상태 힌트")]
    public string hintClosed = "가방 [Tab]";

    [Tooltip("가방 열린 상태 힌트")]
    public string hintOpen   = "가방 닫기 [Tab]";

    private void Start()
    {
        UpdateHint(false);

        // InventoryUI 이벤트 구독
        if (InventoryUI.Instance != null)
            InventoryUI.Instance.OnInventoryToggled += UpdateHint;
    }

    private void OnDestroy()
    {
        if (InventoryUI.Instance != null)
            InventoryUI.Instance.OnInventoryToggled -= UpdateHint;
    }

    private void UpdateHint(bool isOpen)
    {
        if (hintText == null) return;
        hintText.text = isOpen ? hintOpen : hintClosed;
    }
}

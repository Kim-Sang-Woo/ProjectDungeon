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

    [Tooltip("힌트 문구")]
    public string hintMessage = "가방 [Tab]";

    private void Start()
    {
        if (hintText != null)
            hintText.text = hintMessage;
    }
}

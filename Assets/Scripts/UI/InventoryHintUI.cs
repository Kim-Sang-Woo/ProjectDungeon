// ============================================================
// InventoryHintUI.cs — 하단 인벤토리 힌트 표시
// 위치: Assets/Scripts/UI/InventoryHintUI.cs
// ============================================================
// [개요]
//   화면 하단에 "가방 [Tab]" 텍스트를 항상 표시한다.
//   별도 로직 없이 표시 전용.
//
// [씬 배치]
//   Canvas 하위에 Text 오브젝트 생성 후
//   Anchor를 하단 중앙으로 설정
//   이 스크립트는 불필요 시 생략하고
//   Text 오브젝트만 배치해도 동일한 효과
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

// ============================================================
// EventPopupUI.cs — 이벤트 팝업 UI
// 기획서 Ch.5.1 참조
// 위치: Assets/Scripts/UI/EventPopupUI.cs
// ============================================================
// [참고] TextMeshPro(TMPro) 전환을 권장하지만,
//        TMPro 패키지 설치 여부에 따라 달라지므로 현재는 UnityEngine.UI.Text 유지.
//        전환 시 using TMPro; 추가 및 Text → TextMeshProUGUI 변경.
// ============================================================
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 이벤트 발생 시 화면에 표시되는 팝업 UI.
/// 이벤트 타입과 이름을 표시하고, 확인 버튼으로 닫을 수 있다.
/// Canvas → EventPopup 오브젝트에 부착한다.
/// </summary>
public class EventPopupUI : MonoBehaviour
{
    [Header("UI 요소")]
    [Tooltip("이벤트 타입 표시 텍스트")]
    public Text eventTypeText;
    [Tooltip("이벤트 이름 표시 텍스트")]
    public Text eventNameText;
    [Tooltip("이벤트 아이콘 이미지")]
    public Image eventIcon;
    [Tooltip("확인 버튼")]
    public Button confirmButton;

    private void Awake()
    {
        gameObject.SetActive(false);

        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(HideEvent);
        }
    }

    /// <summary>
    /// 이벤트 발생 시 팝업을 표시한다.
    /// </summary>
    public void ShowEvent(DungeonEventData eventData)
    {
        if (eventData == null) return;

        gameObject.SetActive(true);

        if (eventTypeText != null)
        {
            eventTypeText.text = GetEventTypeDisplayName(eventData.eventType);
        }

        if (eventNameText != null)
        {
            eventNameText.text = eventData.displayName;
        }

        if (eventIcon != null)
        {
            if (eventData.iconSprite != null)
            {
                eventIcon.sprite = eventData.iconSprite;
                eventIcon.gameObject.SetActive(true);
            }
            else
            {
                eventIcon.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// 팝업을 닫는다.
    /// </summary>
    public void HideEvent()
    {
        gameObject.SetActive(false);
    }

    private string GetEventTypeDisplayName(DungeonEventType type)
    {
        switch (type)
        {
            case DungeonEventType.COMBAT:   return "⚔️ 전투";
            case DungeonEventType.TRAP:     return "💥 함정";
            case DungeonEventType.TREASURE: return "💰 보물";
            case DungeonEventType.NPC:      return "💬 NPC";
            case DungeonEventType.SHRINE:   return "✨ 제단";
            case DungeonEventType.SPECIAL:  return "🔮 특수";
            default:                        return "❓ 알 수 없음";
        }
    }
}

// ============================================================
// InteractionUI.cs — 오브젝트 인터렉션 UI
// 위치: Assets/Scripts/UI/InteractionUI.cs
// ============================================================
// [v1.3 수정]
//   코루틴 방식 제거 → CanvasGroup 방식으로 교체
//   - 원인: 비활성 오브젝트에서는 StartCoroutine 불가
//   - 해결: SetActive 대신 CanvasGroup.alpha / interactable / blocksRaycasts
//     로 표시/숨김을 처리. 오브젝트는 항상 활성 상태이므로
//     코루틴 문제가 발생하지 않음
//
// [Inspector 추가 작업]
//   InteractionPanel에 CanvasGroup 컴포넌트 추가 필요
//   (Add Component → UI → Canvas Group)
//   canvasGroup 필드에 해당 CanvasGroup 연결
// ============================================================
using System;
using UnityEngine;
using UnityEngine.UI;

public class InteractionUI : MonoBehaviour
{
    [Header("UI 요소")]
    public Text   nameText;
    public Text   descText;
    public Button actionButton;
    public Text   actionBtnText;

    [Header("표시 제어")]
    [Tooltip("InteractionPanel의 CanvasGroup 컴포넌트")]
    public CanvasGroup canvasGroup;

    private Action onActionClicked;

    private void Awake()
    {
        // SetActive 대신 CanvasGroup으로 숨김 처리
        // 오브젝트는 항상 활성 상태 유지
        HideImmediate();

        if (actionButton != null)
            actionButton.onClick.AddListener(OnActionButtonClicked);

        if (nameText      == null) Debug.LogWarning("[InteractionUI] nameText 미연결");
        if (descText      == null) Debug.LogWarning("[InteractionUI] descText 미연결");
        if (actionButton  == null) Debug.LogWarning("[InteractionUI] actionButton 미연결");
        if (actionBtnText == null) Debug.LogWarning("[InteractionUI] actionBtnText 미연결");
        if (canvasGroup   == null) Debug.LogWarning("[InteractionUI] canvasGroup 미연결 — Inspector에서 CanvasGroup을 연결해주세요.");
    }

    // ─── 공개 API ───

    public void ShowInteraction(DungeonObjectData data, Action onAction)
    {
        if (data == null) return;

        onActionClicked = onAction;

        // 텍스트 먼저 설정 후 표시
        if (nameText      != null) nameText.text      = data.displayName;
        if (descText      != null) descText.text      = data.description;
        if (actionBtnText != null) actionBtnText.text = data.GetActionLabel();

        ShowImmediate();
    }

    public void Hide()
    {
        HideImmediate();
        onActionClicked = null;
    }

    // ─── 내부 ───

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

    private void OnActionButtonClicked()
    {
        onActionClicked?.Invoke();
        Hide();
    }
}

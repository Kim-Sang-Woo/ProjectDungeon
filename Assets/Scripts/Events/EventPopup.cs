// ============================================================
// EventPopup.cs — 이벤트 팝업 로직 컨트롤러
// 위치: Assets/Scripts/Events/EventPopup.cs
// ============================================================
// [개요]
//   기존 EventPopupUI를 대체하는 이벤트 팝업 컨트롤러.
//   - EventSession 소유 및 관리
//   - 선택지 실행: 조건 충족 확인 → 확률 판정 → 효과 실행 → ResultView 전환
//   - 팝업 열기/닫기 시 MovementSystem 입력 잠금/해제
//   - 키 입력 처리 (숫자 1~5, ESC)
//
// [씬 배치]
//   Canvas → EventPopupPanel (이 스크립트 부착)
//   EventPopupUI.cs 와 동일 오브젝트 또는 자식에 함께 배치
// ============================================================
using System.Text;
using UnityEngine;

public class EventPopup : MonoBehaviour
{
    [Header("참조")]
    [Tooltip("UI 바인딩 컴포넌트")]
    public EventPopupUI ui;

    [Tooltip("이동 잠금/해제용")]
    public MovementSystem movementSystem;

    // ── 런타임 ─────────────────────────────────────────────
    private EventSession session;
    private bool         isOpen = false;

    // ─────────────────────────────────────────────────────
    private void Awake()
    {
        gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!isOpen) return;
        HandleKeyInput();
    }

    // ── 열기 / 닫기 ───────────────────────────────────────

    /// <summary>이벤트 팝업을 열고 ChoiceView를 표시한다.</summary>
    public void Open(EventData data)
    {
        if (data == null) return;

        session = new EventSession();
        session.Initialize(data);

        isOpen = true;
        gameObject.SetActive(true);   // SetActive(true) 먼저
        movementSystem?.LockInput();

        ui?.EnsureInitialized();      // 활성화 후 초기화
        ui?.BindChoiceView(session);
    }

    /// <summary>팝업을 닫고 이동 입력을 해제한다.</summary>
    public void Close()
    {
        isOpen = false;
        gameObject.SetActive(false);
        movementSystem?.UnlockInput();
        session = null;
    }

    // ── 선택지 실행 (UI → 콜백) ───────────────────────────

    /// <summary>
    /// 선택지 인덱스로 실행한다.
    /// EventChoiceItemUI 버튼 클릭 및 숫자 키 입력 모두 이 메서드를 호출한다.
    /// </summary>
    public void SelectChoice(int index)
    {
        if (session == null) return;
        if (index < 0 || index >= session.resolvedChoices.Count) return;

        ResolvedChoice rc = session.resolvedChoices[index];
        if (!rc.isVisible) return;

        ExecuteChoice(rc);
    }

    // ── 내부 로직 ─────────────────────────────────────────

    private void ExecuteChoice(ResolvedChoice rc)
    {
        if (rc == null) { Debug.LogError("[EventPopup] rc가 null"); return; }

        EventChoice choice = rc.source;
        if (choice == null) { Debug.LogError("[EventPopup] rc.source(EventChoice)가 null — Inspector에서 Choice SO 연결을 확인하세요."); Close(); return; }

        // 닫기 선택지
        if (choice.choiceType == ChoiceType.Close)
        {
            Close();
            return;
        }

        // 확률 판정 (Default만 적용, 특수 선택지는 조건 충족 시 항상 성공)
        bool success;
        if (choice.choiceType == ChoiceType.Default)
            success = Random.Range(0, 100) < choice.successRate;
        else
            success = true;

        EventResult result = success ? choice.onSuccess : choice.onFailure;

        // 결과 없으면 directEffects 실행 후 팝업 닫기
        if (result == null)
        {
            if (choice.directEffects != null)
                foreach (var effect in choice.directEffects)
                    effect?.Execute();
            Close();
            return;
        }

        // 효과 실행
        if (result.effects != null && result.effects.Length > 0)
            foreach (var effect in result.effects)
            {
                if (effect == null) { Debug.LogWarning("[EventPopup] effect가 null — EventResult.effects 배열을 확인하세요."); continue; }
                effect.Execute();
            }

        // 효과 텍스트 빌드
        session.effectSummaryText = BuildEffectText(result);

        // 후속 선택지 세션 갱신
        session.currentResult = result;
        session.phase         = EventPhase.Result;

        // 후속 선택지가 있으면 세션에 반영
        if (result.nextChoices != null && result.nextChoices.Length > 0)
        {
            session.resolvedChoices.Clear();
            for (int i = 0; i < result.nextChoices.Length && i < 5; i++)
            {
                var next = result.nextChoices[i];
                if (next == null) continue;
                // 후속 선택지는 조건 재평가 없이 그대로 표시
                session.resolvedChoices.Add(new ResolvedChoice
                {
                    source    = next,
                    isVisible = true,
                    badgeText = next.choiceType == ChoiceType.Default ? $"{next.successRate}%" : "",
                    badgeType = next.choiceType,
                    label     = next.label,
                    keyIndex  = i + 1,
                });
            }
        }
        else
        {
            // 후속 선택지 없으면 닫기 선택지만 자동 추가
            session.resolvedChoices.Clear();
            session.resolvedChoices.Add(new ResolvedChoice
            {
                source    = null,
                isVisible = true,
                badgeText = "",
                badgeType = ChoiceType.Close,
                label     = "자리를 떠난다.",
                keyIndex  = 1,
            });
        }

        ui?.BindResultView(session);
    }

    private string BuildEffectText(EventResult result)
    {
        if (result.effects == null || result.effects.Length == 0) return "";
        var sb = new StringBuilder();
        foreach (var effect in result.effects)
        {
            if (effect == null) continue;
            string line = effect.GetEffectText();
            if (!string.IsNullOrEmpty(line))
                sb.AppendLine(line);
        }
        return sb.ToString().TrimEnd();
    }

    // ── 키 입력 ───────────────────────────────────────────

    private void HandleKeyInput()
    {
        // ESC → ResultView에서만 닫기 허용
        // ChoiceView에서는 ESC 무시 — 유저가 반드시 선택지를 골라야 함
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (session != null && session.phase == EventPhase.Result)
                Close();
            return;
        }

        // 숫자 1~5 → 선택지 실행
        if (Input.GetKeyDown(KeyCode.Alpha1)) SelectChoice(0);
        else if (Input.GetKeyDown(KeyCode.Alpha2)) SelectChoice(1);
        else if (Input.GetKeyDown(KeyCode.Alpha3)) SelectChoice(2);
        else if (Input.GetKeyDown(KeyCode.Alpha4)) SelectChoice(3);
        else if (Input.GetKeyDown(KeyCode.Alpha5)) SelectChoice(4);
    }
}

using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 전투 종료 시 승/패 흐름을 Inspector 이벤트로 라우팅한다.
/// - onVictory: 승리 후 실행 (Event Result 출력 등 연결)
/// - onDefeat: 패배 후 실행
/// </summary>
public class BattleResultRouter : MonoBehaviour
{
    [Header("연동")]
    public BattleManager battleManager;

    [Header("결과 이벤트")]
    public UnityEvent onVictory;
    public UnityEvent onDefeat;

    private void Awake()
    {
        if (battleManager == null) battleManager = BattleManager.Instance;
    }

    private void OnEnable()
    {
        if (battleManager == null) battleManager = BattleManager.Instance;
        if (battleManager != null)
            battleManager.OnBattleFinished += HandleBattleFinished;
    }

    private void OnDisable()
    {
        if (battleManager != null)
            battleManager.OnBattleFinished -= HandleBattleFinished;
    }

    private void HandleBattleFinished(bool isVictory)
    {
        if (isVictory) onVictory?.Invoke();
        else onDefeat?.Invoke();
    }
}

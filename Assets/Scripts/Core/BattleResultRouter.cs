using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

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

    [Header("BattleResult 텍스트 연출")]
    public Text resultText;
    public string victoryText = "승리";
    public float resultTextDuration = 1.2f;
    public Color victoryColor = new Color(0.95f, 0.88f, 0.62f, 1f);

    private Coroutine resultTextCoroutine;

    private void Awake()
    {
        if (battleManager == null) battleManager = BattleManager.Instance;
        if (resultText != null)
        {
            resultText.text = string.Empty;
            resultText.gameObject.SetActive(false);
        }
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
        if (isVictory)
        {
            PlayResultText(victoryText, victoryColor);
            onVictory?.Invoke();
        }
        else
        {
            onDefeat?.Invoke();
        }
    }

    private void PlayResultText(string text, Color color)
    {
        if (resultText == null) return;

        if (resultTextCoroutine != null)
            StopCoroutine(resultTextCoroutine);

        resultTextCoroutine = StartCoroutine(CoPlayResultText(text, color));
    }

    private IEnumerator CoPlayResultText(string text, Color color)
    {
        resultText.gameObject.SetActive(true);
        resultText.text = text;

        Color c = color;
        c.a = 0f;
        resultText.color = c;

        RectTransform rt = resultText.rectTransform;
        Vector2 basePos = rt.anchoredPosition;

        float fadeIn = 0.2f;
        float hold = Mathf.Max(0f, resultTextDuration - 0.45f);
        float fadeOut = 0.25f;

        float t = 0f;
        while (t < fadeIn)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / fadeIn);
            c.a = k;
            resultText.color = c;
            rt.anchoredPosition = basePos + new Vector2(0f, 8f * k);
            yield return null;
        }

        c.a = 1f;
        resultText.color = c;
        yield return new WaitForSeconds(hold);

        t = 0f;
        while (t < fadeOut)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / fadeOut);
            c.a = 1f - k;
            resultText.color = c;
            rt.anchoredPosition = basePos + new Vector2(0f, 8f + 6f * k);
            yield return null;
        }

        resultText.text = string.Empty;
        resultText.gameObject.SetActive(false);
        rt.anchoredPosition = basePos;
        resultTextCoroutine = null;
    }
}

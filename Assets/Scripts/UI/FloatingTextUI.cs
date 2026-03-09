// ============================================================
// FloatingTextUI.cs — 떠오르는 텍스트 연출
// 위치: Assets/Scripts/UI/FloatingTextUI.cs
// ============================================================
// [개요]
//   플레이어 위치 상단에 텍스트가 천천히 올라가며 사라지는 연출.
//   다수 아이템 획득 시 겹치지 않게 순차적으로 출력.
//
// [씬 배치]
//   Hierarchy → 빈 GameObject "FloatingTextUI"
//   FloatingTextUI.cs 부착
//
// [Inspector 연결]
//   floatingTextPrefab → FloatingText 프리팹 (Text + CanvasGroup)
//   canvas             → 메인 Canvas
//   playerTransform    → Player 오브젝트
// ============================================================
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FloatingTextUI : MonoBehaviour
{
    public static FloatingTextUI Instance { get; private set; }

    [Header("참조")]
    [Tooltip("떠오르는 텍스트 프리팹 (Text + CanvasGroup)")]
    public GameObject floatingTextPrefab;

    [Tooltip("메인 Canvas")]
    public Canvas canvas;

    [Tooltip("플레이어 Transform")]
    public Transform playerTransform;

    [Header("디버그")]
    public bool debugLogs = false;

    [Header("연출 설정")]
    [Tooltip("텍스트가 올라가는 거리 (월드 유닛)")]
    public float riseDistance = 1.5f;

    [Tooltip("연출 지속 시간 (초)")]
    public float duration = 1.8f;

    [Tooltip("다수 텍스트 간격 (초)")]
    public float queueInterval = 0.3f;

    [Tooltip("플레이어 기준 초기 오프셋 (월드 유닛)")]
    public float startOffsetY = 1.2f;

    // 대기 중인 텍스트 큐
    private Queue<(string text, Color color)> textQueue = new Queue<(string, Color)>();
    private bool isProcessing = false;

    // 색상 프리셋
    public static readonly Color ColorAcquire  = new Color(1.0f, 0.95f, 0.6f); // 노란색 — 획득
    public static readonly Color ColorFail     = new Color(1.0f, 0.4f,  0.4f); // 빨간색 — 실패
    public static readonly Color ColorWarning  = new Color(1.0f, 0.65f, 0.2f); // 주황색 — 경고

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ─── 공개 API ───

    public void Show(string text, Color color)
    {
        textQueue.Enqueue((text, color));
        if (!isProcessing)
            StartCoroutine(ProcessQueue());
    }

    // 자주 쓰는 메시지 단축 메서드
    public void ShowAcquire(string itemName, int quantity)
        => Show($"{itemName} x{quantity} 획득", ColorAcquire);

    public void ShowTooHeavy()
        => Show("너무 무겁습니다.", ColorFail);

    public void ShowNoSpace()
        => Show("가방 공간이 부족합니다.", ColorFail);

    public void ShowSlowDown()
        => Show("너무 무거워 발이 느려집니다.", ColorWarning);

    // ─── 큐 처리 ───

    private IEnumerator ProcessQueue()
    {
        isProcessing = true;

        while (textQueue.Count > 0)
        {
            var (text, color) = textQueue.Dequeue();
            StartCoroutine(ShowFloatingText(text, color));
            yield return new WaitForSeconds(queueInterval);
        }

        isProcessing = false;
    }

    private IEnumerator ShowFloatingText(string text, Color color)
    {
        if (floatingTextPrefab == null || canvas == null || playerTransform == null)
            yield break;

        Camera cam = Camera.main;
        RectTransform canvasRect = canvas.transform as RectTransform;

        if (cam == null || canvasRect == null)
        {
            if (debugLogs)
                Debug.LogWarning($"[FloatingText] 참조 없음 — cam:{cam != null} canvasRect:{canvasRect != null}");
            yield break;
        }

        Vector3 worldPos  = playerTransform.position + Vector3.up * startOffsetY;
        Vector3 screenPos = cam.WorldToScreenPoint(worldPos);

        Camera uiCam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : cam;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, screenPos, uiCam, out Vector2 localPos);

        if (debugLogs)
            Debug.Log($"[FloatingText] worldPos:{worldPos} screenPos:{screenPos} localPos:{localPos} canvasSize:{canvasRect.sizeDelta}");

        if (debugLogs)
            Debug.Log($"[FloatingText] localPos:{localPos} / prefab:{floatingTextPrefab != null} / 생성 시도");

        // 텍스트 오브젝트 생성 — canvasRect.transform으로 부모 명시
        GameObject go = Instantiate(floatingTextPrefab, canvasRect.transform);
        if (go == null)
        {
            if (debugLogs) Debug.LogWarning("[FloatingText] Instantiate 실패");
            yield break;
        }

        if (debugLogs)
            Debug.Log($"[FloatingText] 생성됨 — name:{go.name} active:{go.activeSelf} parent:{go.transform.parent?.name}");

        RectTransform rect = go.GetComponent<RectTransform>();
        if (rect == null) rect = go.AddComponent<RectTransform>();

        rect.anchoredPosition = localPos;
        rect.sizeDelta        = new Vector2(300f, 50f);

        Text t = go.GetComponent<Text>();
        if (t != null) { t.text = text; t.color = color; }

        CanvasGroup cg = go.GetComponent<CanvasGroup>();
        if (cg == null) cg = go.AddComponent<CanvasGroup>();
        cg.alpha = 1f;

        // 상승 거리 계산
        Vector3 riseWorldTarget = worldPos + Vector3.up * riseDistance;
        Vector3 riseScreenPos   = cam.WorldToScreenPoint(riseWorldTarget);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, riseScreenPos, uiCam, out Vector2 riseLocalPos);
        float riseAmount = riseLocalPos.y - localPos.y;

        // 떠오르며 사라지는 연출
        float elapsed  = 0f;
        Vector2 startPos = rect.anchoredPosition;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            if (rect == null) yield break;  // 오브젝트가 파괴된 경우 중단
            float t01 = elapsed / duration;

            rect.anchoredPosition = new Vector2(
                startPos.x,
                startPos.y + riseAmount * t01);

            cg.alpha = t01 < 0.6f ? 1f : 1f - (t01 - 0.6f) / 0.4f;
            yield return null;
        }

        Destroy(go);
    }
}

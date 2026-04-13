using UnityEngine;
using UnityEngine.UI;

public class TitleScreenUI : MonoBehaviour
{
    [Header("참조")]
    public DungeonManager dungeonManager;

    [Header("타이틀 화면")]
    public Sprite backgroundSprite;
    public Sprite startButtonSprite;
    public Vector2 startButtonSize = new Vector2(220f, 72f);
    public bool pulseStartButton = true;
    public float pulseSpeed = 1.2f;
    [Range(0f, 1f)] public float pulseMinAlpha = 0.45f;
    [Range(0f, 1f)] public float pulseMaxAlpha = 1f;

    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private Button startButton;
    private Image startButtonImage;
    private bool isBuilt;

    private void Awake()
    {
        BuildIfNeeded();
        HideImmediate();
    }

    public void Show(DungeonManager owner)
    {
        if (owner != null)
            dungeonManager = owner;

        BuildIfNeeded();

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
        }

        gameObject.SetActive(true);
    }

    public void HideImmediate()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }
    }

    private void Update()
    {
        if (!pulseStartButton || startButtonImage == null || canvasGroup == null || canvasGroup.alpha < 0.01f)
            return;

        float t = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * pulseSpeed * Mathf.PI * 2f);
        float alpha = Mathf.Lerp(pulseMinAlpha, pulseMaxAlpha, t);

        Color c = Color.white;
        c.a = alpha;
        startButtonImage.color = c;
    }

    private void HandleStartPressed()
    {
        dungeonManager?.BeginRunFromTitleScreen();
    }

    private void BuildIfNeeded()
    {
        if (isBuilt) return;
        isBuilt = true;

        canvas = GetComponent<Canvas>();
        if (canvas == null) canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5000;

        CanvasScaler scaler = GetComponent<CanvasScaler>();
        if (scaler == null) scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        if (GetComponent<GraphicRaycaster>() == null)
            gameObject.AddComponent<GraphicRaycaster>();

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        RectTransform root = GetComponent<RectTransform>();
        if (root == null) root = gameObject.AddComponent<RectTransform>();
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;

        for (int i = transform.childCount - 1; i >= 0; i--)
            DestroyImmediate(transform.GetChild(i).gameObject);

        RectTransform bg = CreateRect("Background", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Image bgImage = bg.gameObject.AddComponent<Image>();
        bgImage.sprite = backgroundSprite;
        bgImage.color = Color.white;
        bgImage.preserveAspect = false;

        RectTransform dim = CreateRect("Dim", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Image dimImage = dim.gameObject.AddComponent<Image>();
        dimImage.color = new Color(0f, 0f, 0f, 0f);

        RectTransform startRt = CreateRect(
            "StartButton",
            root,
            new Vector2(0.5f, 0.333f),
            new Vector2(0.5f, 0.333f),
            new Vector2(-startButtonSize.x * 0.5f, -startButtonSize.y * 0.5f),
            new Vector2(startButtonSize.x * 0.5f, startButtonSize.y * 0.5f));

        Image clickAreaImage = startRt.gameObject.AddComponent<Image>();
        clickAreaImage.color = new Color(1f, 1f, 1f, 0.001f);

        startButton = startRt.gameObject.AddComponent<Button>();
        startButton.transition = Selectable.Transition.None;
        startButton.targetGraphic = clickAreaImage;
        startButton.onClick.AddListener(HandleStartPressed);

        RectTransform spriteRt = CreateRect("StartSprite", startRt, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Image startImage = spriteRt.gameObject.AddComponent<Image>();
        startImage.sprite = startButtonSprite;
        startImage.type = startButtonSprite != null ? Image.Type.Sliced : Image.Type.Simple;
        startImage.color = Color.white;
        startImage.raycastTarget = false;
        startButtonImage = startImage;
    }

    private RectTransform CreateRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
        return rt;
    }


}

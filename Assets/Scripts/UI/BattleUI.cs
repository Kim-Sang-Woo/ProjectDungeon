using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BattleUI : MonoBehaviour
{
    public static BattleUI Instance { get; private set; }

    [Header("표시")]
    public CanvasGroup canvasGroup;

    [Header("레이아웃 대상")]
    public RectTransform topBar;
    public RectTransform battleField;
    public RectTransform leftHud;
    public RectTransform rightHud;
    public RectTransform handArea;

    [Header("TopBar")]
    public Text stageText;
    public Text goldText;

    [Header("Hand")]
    public RectTransform handCardContainer;
    public BattleCardItemUI attackCardPrefab;

    [Header("Monster")]
    public RectTransform monsterContainer;
    public BattleMonsterItemUI monsterItemPrefab;
    public Sprite intentAttackSprite;
    public Sprite battleFieldBackgroundSprite;

    [Header("리소스 그래픽")]
    public Sprite hpGaugeFrameSprite;
    public Sprite hpGaugeFillSprite;
    public Sprite hpStageFullSprite;
    public Sprite hpStageHighSprite;
    public Sprite hpStageMidSprite;
    public Sprite hpStageLowSprite;
    public Sprite hpStageCriticalSprite;
    public Sprite manaGaugeFrameSprite;
    public Sprite manaGaugeFillSprite;

    [Header("디버그")]
    public bool debugLogs = false;

    [Header("레이아웃 수치")]
    public float width = 1280f;
    public float height = 720f;
    public float topBarHeight = 48f;
    public float handAreaHeight = 220f;
    public float sideHudWidth = 180f;
    public float padding = 16f;

    [Header("몬스터 패널")]
    public Vector2 monsterPanelSize = new Vector2(192f, 240f);

    [Header("색상")]
    public Color colorTopBar = new Color(0.10f, 0.08f, 0.05f, 1f);
    public Color colorBattleField = new Color(0.16f, 0.12f, 0.08f, 1f);
    public Color colorSideHud = new Color(0.10f, 0.08f, 0.05f, 1f);
    public Color colorHand = new Color(0.07f, 0.06f, 0.045f, 1f);

    private Button endTurnButton;
    private bool isAttackArmed;
    private int selectedCardOrder = -1;

    private Text leftStatsText;
    private Text rightStatsText;
    private Text leftRegenValueText;
    private Text leftDamageValueText;
    private Text rightStateValueText;
    private Text rightRoundValueText;
    private Text rightManaValueText;
    private Text rightHandValueText;
    private Text rightEnemiesValueText;
    private Text rightExpDamageValueText;
    private Text resultBannerText;
    private Text battleFieldNoticeText;
    private Coroutine battleFieldNoticeCoroutine;
    [Header("연출 타이밍")]
    [Min(0f)] public float battleFieldNoticeTotalDuration = 0.76f;

    [Header("적 공격 타격감")]
    [Min(0f)] public float enemyAttackDownDuration = 0.08f;
    [Min(0f)] public float enemyAttackHoldDuration = 0.05f;
    [Min(0f)] public float enemyAttackUpDuration = 0.10f;
    [Range(0f, 0.8f)] public float enemyAttackSpeedVariance = 0.25f;
    [Min(0f)] public float hitShakeDuration = 0.10f;
    [Min(0f)] public float hitShakeStrength = 5f;
    public Vector2 monsterDamageTextOffset = Vector2.zero;

    [Header("몬스터 피격 연출")]
    [Min(0f)] public float monsterHitFlashDuration = 0.10f;
    [Min(0f)] public float monsterHitRecoilUpDuration = 0.08f;
    [Min(0f)] public float monsterHitRecoilDownDuration = 0.10f;
    [Min(0f)] public float monsterHitRecoilDistance = 12f;
    [Range(0.5f, 1f)] public float monsterHitRecoilScale = 0.92f;
    [Min(0f)] public float monsterHitEffectDuration = 0.06f;
    [Min(0f)] public float monsterHitEffectScale = 1.03f;

    private Text handHintText;
    private Text hpBlockText;
    private Text hpStatusText;
    private Text manaBlockText;
    private Image hpGaugeFillImage;
    private Image manaGaugeFillImage;
    private RectTransform hpBlockRoot;
    private Image hpGaugeFrameImage;
    private Image manaGaugeFrameImage;
    private bool hpUseStageSprite;
    private float hpFillInset = 5f;
    private static Sprite runtimeFallbackUiSprite;


    private readonly List<GameObject> spawnedMonsterButtons = new List<GameObject>();
    private readonly Dictionary<int, Image> monsterButtonBgByIndex = new Dictionary<int, Image>();
    private readonly Dictionary<int, RectTransform> monsterRootByIndex = new Dictionary<int, RectTransform>();
    private readonly Dictionary<int, RectTransform> monsterPortraitByIndex = new Dictionary<int, RectTransform>();
    private readonly Dictionary<int, Image> monsterPortraitImageByIndex = new Dictionary<int, Image>();
    private readonly Dictionary<int, float> monsterDamageNextTime = new Dictionary<int, float>();
    private readonly HashSet<int> flashingMonsterIndices = new HashSet<int>();
    private readonly List<BattleCardItemUI> spawnedCards = new List<BattleCardItemUI>();
    private readonly List<int> spawnedCardOrders = new List<int>();
    private readonly Dictionary<BattleCardItemUI, RectTransform> cardRootByUI = new Dictionary<BattleCardItemUI, RectTransform>();
    private BattleCardItemUI hoveredCard;

    private int selectedTargetIndex = -1;

    private readonly Color colorMonsterNormal = new Color(0f, 0f, 0f, 0f);
    private readonly Color colorMonsterHover = new Color(0f, 0f, 0f, 0f);
    private readonly Color colorMonsterTarget = new Color(0f, 0f, 0f, 0f);
    private readonly Color colorMonsterSelected = new Color(0f, 0f, 0f, 0f);

    private int lastHandSignature = int.MinValue;
    private int lastMonsterSignature = int.MinValue;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        HideImmediate();
    }

    private void Start()
    {
        ValidatePrefabSetup();
        ApplyLayout();

        if (BattleManager.Instance != null)
        {
            BattleManager.Instance.OnBattleStateChanged += OnBattleStateChanged;
            BattleManager.Instance.OnBattleValuesChanged += RefreshTexts;
            BattleManager.Instance.OnMonsterDamaged += HandleMonsterDamaged;
        }
    }

    private void Update()
    {
        BattleManager bm = BattleManager.Instance;

        if (bm != null && bm.State == BattleState.PlayerTurn && Input.GetKeyDown(KeyCode.Space))
        {
            bm.EndPlayerTurnByButton();
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
        {
            isAttackArmed = false;
            selectedCardOrder = -1;
            selectedTargetIndex = -1;
            RefreshTexts();
            return;
        }

        // 숫자 단축키: 1~0
        KeyCode[] keys =
        {
            KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4, KeyCode.Alpha5,
            KeyCode.Alpha6, KeyCode.Alpha7, KeyCode.Alpha8, KeyCode.Alpha9, KeyCode.Alpha0,
        };

        // 카드 미선택 상태: 1~0으로 카드 선택
        if (!isAttackArmed)
        {
            int maxShortcut = Mathf.Min(10, spawnedCards.Count);
            for (int i = 0; i < maxShortcut; i++)
            {
                if (!Input.GetKeyDown(keys[i])) continue;

                int actualCardOrder = (i < spawnedCardOrders.Count) ? spawnedCardOrders[i] : i;
                selectedCardOrder = actualCardOrder;

                RuntimeBattleCard runtime = (bm != null && actualCardOrder < bm.CurrentHandCards.Count) ? bm.CurrentHandCards[actualCardOrder] : null;
                BattleCardData cardData = runtime != null ? runtime.data : null;

                if (bm == null || cardData == null) return;
                if (bm.CurrentMana < cardData.costMana)
                {
                    RefreshTexts();
                    return;
                }

                if (RequiresEnemyTarget(cardData))
                {
                    isAttackArmed = true;
                    RefreshTexts();
                    return;
                }

                bool used = bm.TryUseCard(actualCardOrder, -1);
                if (used)
                {
                    isAttackArmed = false;
                    selectedCardOrder = -1;
                    selectedTargetIndex = -1;
                }
                RefreshTexts();
                return;
            }

            return;
        }

        // 카드 선택 상태: 1~0으로 몬스터 타겟 선택
        int maxTargetShortcut = 10;
        for (int i = 0; i < maxTargetShortcut; i++)
        {
            if (!Input.GetKeyDown(keys[i])) continue;

            int targetIndex = GetAliveMonsterIndexByOrder(i);
            if (targetIndex < 0) return;

            int cardOrder = selectedCardOrder >= 0 ? selectedCardOrder : 0;
            RuntimeBattleCard runtime = (bm != null && cardOrder < bm.CurrentHandCards.Count) ? bm.CurrentHandCards[cardOrder] : null;
            BattleCardData cardData = runtime != null ? runtime.data : null;

            bool used = bm != null && bm.TryUseCard(cardOrder, targetIndex);
            if (used)
            {
                isAttackArmed = false;
                selectedCardOrder = -1;
                selectedTargetIndex = -1;
            }
            RefreshTexts();
            return;
        }
    }

    private void OnDestroy()
    {
        if (BattleManager.Instance != null)
        {
            BattleManager.Instance.OnBattleStateChanged -= OnBattleStateChanged;
            BattleManager.Instance.OnBattleValuesChanged -= RefreshTexts;
            BattleManager.Instance.OnMonsterDamaged -= HandleMonsterDamaged;
        }
    }

    private void ApplyMockupPanelPalette()
    {
        // 인스펙터 직렬화 값과 무관하게 시안 팔레트를 강제 적용
        colorTopBar = new Color(0.10f, 0.08f, 0.05f, 1f);
        colorSideHud = new Color(0.10f, 0.08f, 0.05f, 1f);
        colorBattleField = new Color(0.16f, 0.12f, 0.08f, 1f);
        colorHand = new Color(0.07f, 0.06f, 0.045f, 1f);
    }

    public void ApplyLayout()
    {
        ApplyMockupPanelPalette();

        RectTransform panelRT = GetComponent<RectTransform>();
        if (panelRT != null)
        {
            panelRT.anchorMin = new Vector2(0.5f, 0.5f);
            panelRT.anchorMax = new Vector2(0.5f, 0.5f);
            panelRT.pivot = new Vector2(0.5f, 0.5f);
            panelRT.sizeDelta = new Vector2(width, height);
            panelRT.anchoredPosition = Vector2.zero;
        }

        Outline rootBorder = gameObject.GetComponent<Outline>();
        if (rootBorder != null) Destroy(rootBorder);

        if (topBar != null)
        {
            topBar.anchorMin = new Vector2(0f, 1f);
            topBar.anchorMax = new Vector2(1f, 1f);
            topBar.pivot = new Vector2(0.5f, 1f);
            topBar.offsetMin = new Vector2(0f, -topBarHeight);
            topBar.offsetMax = new Vector2(0f, 0f);
            SetBg(topBar.gameObject, colorTopBar);
            Outline topBorder = topBar.gameObject.GetComponent<Outline>();
            if (topBorder != null) Destroy(topBorder);

            if (stageText != null)
            {
                RectTransform rt = stageText.rectTransform;
                rt.anchorMin = new Vector2(0f, 0f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.offsetMin = new Vector2(0f, 0f);
                rt.offsetMax = new Vector2(0f, 0f);
                stageText.alignment = TextAnchor.MiddleCenter;
                stageText.fontStyle = FontStyle.Bold;
                stageText.fontSize = 18;
                stageText.color = new Color(0.96f, 0.84f, 0.45f, 1f);
                stageText.text = "BATTLE";
            }

            if (goldText != null)
            {
                goldText.text = "";
                goldText.gameObject.SetActive(false);
            }
        }

        float bodyTop = -topBarHeight;
        float bodyBottom = handAreaHeight;

        if (handArea != null)
        {
            handArea.anchorMin = new Vector2(0f, 0f);
            handArea.anchorMax = new Vector2(1f, 0f);
            handArea.pivot = new Vector2(0.5f, 0f);
            handArea.offsetMin = new Vector2(0f, 0f);
            handArea.offsetMax = new Vector2(0f, handAreaHeight);
            SetBg(handArea.gameObject, colorHand);
        }

        if (leftHud != null)
        {
            leftHud.anchorMin = new Vector2(0f, 0f);
            leftHud.anchorMax = new Vector2(0f, 1f);
            leftHud.pivot = new Vector2(0f, 1f);
            leftHud.offsetMin = new Vector2(0f, bodyBottom);
            leftHud.offsetMax = new Vector2(sideHudWidth, bodyTop);
            SetBg(leftHud.gameObject, colorSideHud);
            BuildLeftHud();
        }

        if (rightHud != null)
        {
            rightHud.anchorMin = new Vector2(1f, 0f);
            rightHud.anchorMax = new Vector2(1f, 1f);
            rightHud.pivot = new Vector2(1f, 1f);
            rightHud.offsetMin = new Vector2(-sideHudWidth, bodyBottom);
            rightHud.offsetMax = new Vector2(0f, bodyTop);
            SetBg(rightHud.gameObject, colorSideHud);
            BuildRightHud();
        }

        if (battleField != null)
        {
            battleField.anchorMin = new Vector2(0f, 0f);
            battleField.anchorMax = new Vector2(1f, 1f);
            battleField.pivot = new Vector2(0.5f, 1f);
            battleField.offsetMin = new Vector2(sideHudWidth, bodyBottom);
            battleField.offsetMax = new Vector2(-sideHudWidth, bodyTop);
            SetBg(battleField.gameObject, colorBattleField);
            BuildBattleFieldBackground();
            BuildMonsterContainer();
            BuildBattleFieldNotice();
            BuildResultBanner();
        }

        BuildHandControls();

        // 최초 1회 강제 리빌드
        lastHandSignature = int.MinValue;
        lastMonsterSignature = int.MinValue;
        RebuildMonsterButtons();

        if (debugLogs) Debug.Log("[BattleUI] ApplyLayout 완료");
    }

    private void OnBattleStateChanged(BattleState state)
    {
        if (state == BattleState.BattleStart || state == BattleState.RoundStart || state == BattleState.PlayerTurn || state == BattleState.EnemyTurn || state == BattleState.Victory || state == BattleState.Defeat)
            ShowImmediate();

        if (state == BattleState.BattleEnd)
            HideImmediate();

        if (state == BattleState.RoundStart)
        {
            BattleManager bm = BattleManager.Instance;
            int round = bm != null ? bm.RoundIndex : 0;
            ShowBattleFieldNotice($"라운드 {round}", new Color(0.95f, 0.88f, 0.62f, 1f));
        }
        else if (state == BattleState.EnemyTurn)
        {
            ShowBattleFieldNotice("적 턴", new Color(1f, 0.78f, 0.42f, 1f));
        }

        if (state != BattleState.PlayerTurn)
        {
            isAttackArmed = false;
            selectedCardOrder = -1;
            selectedTargetIndex = -1;
        }

        RefreshTexts();
    }

    private void RefreshTexts()
    {
        BattleManager bm = BattleManager.Instance;
        if (bm == null) return;

        if (stageText != null)
            stageText.text = "BATTLE";

        if (goldText != null)
            goldText.gameObject.SetActive(false);

        CharacterStats stats = bm.characterStats != null ? bm.characterStats : CharacterStats.Instance;
        if (stats != null)
        {
            if (leftStatsText != null)
                leftStatsText.text = "PLAYER";
            if (leftRegenValueText != null)
                leftRegenValueText.text = $"{stats.hpGen.FinalValue:0}%";
            if (leftDamageValueText != null)
                leftDamageValueText.text = $"{stats.damagePer.FinalValue:0}%";

        }

        int alive = 0;
        if (bm.Monsters != null)
            foreach (var m in bm.Monsters)
                if (m != null && !m.IsDead) alive++;

        if (rightStatsText != null) rightStatsText.text = "COMBAT";
        if (rightStateValueText != null) rightStateValueText.text = bm.State.ToString();
        if (rightRoundValueText != null) rightRoundValueText.text = bm.RoundIndex.ToString();
        if (rightManaValueText != null) rightManaValueText.text = bm.CurrentMana.ToString();
        if (rightHandValueText != null) rightHandValueText.text = $"{bm.CurrentHandCardCount}/{bm.CurrentHandCount}";
        if (rightEnemiesValueText != null) rightEnemiesValueText.text = alive.ToString();
        if (rightExpDamageValueText != null) rightExpDamageValueText.text = bm.PredictedEnemyDamage.ToString();

        if (hpBlockText != null && stats != null)
        {
            hpBlockText.text =
                $"HP\n{stats.currentHP:0}/{stats.maxHP.FinalValue:0}";

            float hpRatio = stats.maxHP.FinalValue > 0f ? stats.currentHP / stats.maxHP.FinalValue : 0f;
            hpBlockText.color = hpRatio > 0.5f
                ? new Color(1f, 0.95f, 0.92f, 1f)
                : (hpRatio > 0.2f ? new Color(1f, 0.78f, 0.65f, 1f) : new Color(1f, 0.55f, 0.55f, 1f));

            UpdateHpGaugeVisual(Mathf.Clamp01(hpRatio));
        }

        if (hpStatusText != null && stats != null)
            hpStatusText.text = $"SHEILD : {stats.currentShield:0}\nDODGE : {stats.currentDodge:0}";

        int maxMana = Mathf.Max(1, Mathf.FloorToInt(stats != null ? stats.baseMana.FinalValue : 1f));
        if (manaBlockText != null)
            manaBlockText.text = $"MANA\n{bm.CurrentMana}/{maxMana}";
        UpdateManaGaugeVisual(Mathf.Clamp01((float)bm.CurrentMana / maxMana));

        if (resultBannerText != null)
        {
            if (bm.State == BattleState.Victory)
            {
                resultBannerText.enabled = true;
                resultBannerText.text = "VICTORY";
                resultBannerText.color = new Color(0.95f, 0.88f, 0.62f, 1f);
            }
            else if (bm.State == BattleState.Defeat)
            {
                resultBannerText.enabled = true;
                resultBannerText.text = "DEFEAT";
                resultBannerText.color = new Color(1f, 0.52f, 0.52f, 1f);
            }
            else
            {
                resultBannerText.enabled = false;
                resultBannerText.text = "";
            }
        }

        int handSig = ComputeHandSignature(bm);
        if (handSig != lastHandSignature)
        {
            RebuildHandCards();
            lastHandSignature = handSig;
        }

        if (handHintText != null)
        {
            int minCost = 999;
            for (int i = 0; i < bm.CurrentHandCards.Count; i++)
            {
                BattleCardData cd = bm.CurrentHandCards[i]?.data;
                if (cd == null) continue;
                minCost = Mathf.Min(minCost, cd.costMana);
            }
            if (minCost == 999) minCost = 0;

            if (bm.State != BattleState.PlayerTurn)
                handHintText.text = "";
            else if (bm.CurrentHandCardCount <= 0)
                handHintText.text = "사용 가능한 카드가 없습니다.";
            else if (bm.CurrentMana < minCost)
                handHintText.text = "마나가 부족합니다.";
            else if (isAttackArmed)
                handHintText.text = "대상을 선택하세요. (1~0), 취소 (ESC/우클릭)";
            else
                handHintText.text = "카드를 선택하세요. (1~0)";
        }

        if (endTurnButton != null)
            endTurnButton.interactable = (bm.State == BattleState.PlayerTurn);

        if (selectedTargetIndex >= 0)
        {
            if (bm.Monsters == null || selectedTargetIndex >= bm.Monsters.Count || bm.Monsters[selectedTargetIndex] == null || bm.Monsters[selectedTargetIndex].IsDead)
                selectedTargetIndex = -1;
        }

        int monsterSig = ComputeMonsterSignature(bm);
        if (monsterSig != lastMonsterSignature)
        {
            RebuildMonsterButtons();
            lastMonsterSignature = monsterSig;
        }
        ApplyMonsterTargetVisual();
        ApplyCardSelectionVisual();
    }

    private int ComputeHandSignature(BattleManager bm)
    {
        unchecked
        {
            int h = 17;
            h = h * 31 + (int)bm.State;
            h = h * 31 + bm.CurrentMana;
            h = h * 31 + bm.CurrentHandCount;
            h = h * 31 + bm.CurrentHandCardCount;
            h = h * 31 + (isAttackArmed ? 1 : 0);
            h = h * 31 + selectedCardOrder;

            for (int i = 0; i < bm.CurrentHandCards.Count; i++)
            {
                var c = bm.CurrentHandCards[i]?.data;
                h = h * 31 + (c != null ? c.GetInstanceID() : 0);
            }

            return h;
        }
    }

    private int ComputeMonsterSignature(BattleManager bm)
    {
        unchecked
        {
            int h = 23;
            h = h * 31 + bm.Monsters.Count;

            for (int i = 0; i < bm.Monsters.Count; i++)
            {
                RuntimeMonster m = bm.Monsters[i];
                h = h * 31 + (m != null && m.IsDead ? 1 : 0);
                h = h * 31 + (m != null && m.data != null ? m.data.GetInstanceID() : 0);
            }

            return h;
        }
    }

    private void BuildBattleFieldBackground()
    {
        if (battleField == null) return;
        Transform existing = battleField.Find("BattleFieldBackground");
        Image img;
        RectTransform rt;

        if (existing != null)
        {
            img = existing.GetComponent<Image>();
            rt = existing.GetComponent<RectTransform>();
        }
        else
        {
            GameObject go = new GameObject("BattleFieldBackground", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(battleField, false);
            go.transform.SetAsFirstSibling();
            rt = go.GetComponent<RectTransform>();
            img = go.GetComponent<Image>();
        }

        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        img.sprite = battleFieldBackgroundSprite;
        img.type = Image.Type.Sliced;
        img.preserveAspect = false;
        img.color = battleFieldBackgroundSprite != null
            ? new Color(1f, 1f, 1f, 0.95f)
            : new Color(0.08f, 0.07f, 0.05f, 0.35f);
    }

    private void BuildBattleFieldNotice()
    {
        if (battleField == null || battleFieldNoticeText != null) return;

        GameObject go = new GameObject("BattleFieldNoticeText", typeof(RectTransform), typeof(Text));
        go.transform.SetParent(battleField, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(520f, 96f);
        rt.anchoredPosition = new Vector2(0f, 0f);

        battleFieldNoticeText = go.GetComponent<Text>();
        battleFieldNoticeText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        battleFieldNoticeText.alignment = TextAnchor.MiddleCenter;
        battleFieldNoticeText.fontSize = 44;
        battleFieldNoticeText.fontStyle = FontStyle.Bold;
        battleFieldNoticeText.color = new Color(0.95f, 0.88f, 0.62f, 0f);
        battleFieldNoticeText.enabled = false;
    }

    private void ShowBattleFieldNotice(string message, Color color)
    {
        if (battleFieldNoticeText == null)
            BuildBattleFieldNotice();

        if (battleFieldNoticeText == null) return;

        if (battleFieldNoticeCoroutine != null)
            StopCoroutine(battleFieldNoticeCoroutine);

        battleFieldNoticeCoroutine = StartCoroutine(CoShowBattleFieldNotice(message, color));
    }

    private IEnumerator CoShowBattleFieldNotice(string message, Color color)
    {
        battleFieldNoticeText.enabled = true;
        battleFieldNoticeText.text = message;

        Color c = color;
        c.a = 0f;
        battleFieldNoticeText.color = c;

        Vector2 basePos = new Vector2(0f, 4f);
        RectTransform rt = battleFieldNoticeText.rectTransform;
        rt.anchoredPosition = basePos;

        float fadeIn = 0.16f;
        float hold = 0.36f;
        float fadeOut = 0.24f;

        float t = 0f;
        while (t < fadeIn)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / fadeIn);
            c.a = k;
            battleFieldNoticeText.color = c;
            rt.anchoredPosition = basePos + new Vector2(0f, 10f * k);
            yield return null;
        }

        c.a = 1f;
        battleFieldNoticeText.color = c;
        yield return new WaitForSeconds(hold);

        t = 0f;
        while (t < fadeOut)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / fadeOut);
            c.a = 1f - k;
            battleFieldNoticeText.color = c;
            rt.anchoredPosition = basePos + new Vector2(0f, 10f + 8f * k);
            yield return null;
        }

        battleFieldNoticeText.enabled = false;
        battleFieldNoticeText.text = "";
        battleFieldNoticeCoroutine = null;
    }

    private void BuildResultBanner()
    {
        if (battleField == null || resultBannerText != null) return;

        GameObject go = new GameObject("ResultBannerText", typeof(RectTransform), typeof(Text));
        go.transform.SetParent(battleField, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(420f, 90f);
        rt.anchoredPosition = new Vector2(0f, 120f);

        resultBannerText = go.GetComponent<Text>();
        resultBannerText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        resultBannerText.alignment = TextAnchor.MiddleCenter;
        resultBannerText.fontSize = 36;
        resultBannerText.fontStyle = FontStyle.Bold;
        resultBannerText.color = new Color(0.95f, 0.88f, 0.62f, 1f);
        resultBannerText.enabled = false;
    }

    private void BuildMonsterContainer()
    {
        if (battleField == null || monsterContainer != null) return;

        GameObject go = new GameObject("MonsterContainer", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        go.transform.SetParent(battleField, false);

        monsterContainer = go.GetComponent<RectTransform>();
        monsterContainer.anchorMin = new Vector2(0.5f, 0.5f);
        monsterContainer.anchorMax = new Vector2(0.5f, 0.5f);
        monsterContainer.pivot = new Vector2(0.5f, 0.5f);
        monsterContainer.sizeDelta = new Vector2(900f, 360f);
        monsterContainer.anchoredPosition = new Vector2(0f, 40f);

        HorizontalLayoutGroup hlg = go.GetComponent<HorizontalLayoutGroup>();
        hlg.spacing = 24f;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
    }

    private void BuildLeftHud()
    {
        if (leftHud == null || leftStatsText != null) return;

        leftStatsText = CreateHudHeader(leftHud, "LeftHudHeader", "PLAYER");

        RectTransform regenRow = CreateHudRowRoot(leftHud, "RegenRow", 44f);
        CreateHudRowLabel(regenRow, "Icon", "✦", 12, new Color(0.45f, 0.82f, 0.45f, 1f));
        CreateHudRowLabel(regenRow, "Name", "HP REGEN", 10, new Color(0.78f, 0.72f, 0.56f, 1f), 22f, 0.65f);
        leftRegenValueText = CreateHudRowValue(regenRow, "Value", "0%", 11, new Color(0.86f, 0.92f, 0.72f, 1f));

        RectTransform damageRow = CreateHudRowRoot(leftHud, "DamageRow", 68f);
        CreateHudRowLabel(damageRow, "Icon", "⚔", 12, new Color(0.90f, 0.66f, 0.45f, 1f));
        CreateHudRowLabel(damageRow, "Name", "DAMAGE", 10, new Color(0.78f, 0.72f, 0.56f, 1f), 22f, 0.65f);
        leftDamageValueText = CreateHudRowValue(damageRow, "Value", "0%", 11, new Color(0.95f, 0.82f, 0.62f, 1f));
    }

    private void BuildRightHud()
    {
        if (rightHud == null || rightStatsText != null) return;

        rightStatsText = CreateHudHeader(rightHud, "RightHudHeader", "COMBAT");

        float y = 44f;
        rightStateValueText = CreateCombatInfoRow(rightHud, "State", "STATE", y); y += 24f;
        rightRoundValueText = CreateCombatInfoRow(rightHud, "Round", "ROUND", y); y += 24f;
        rightManaValueText = CreateCombatInfoRow(rightHud, "Mana", "MANA", y); y += 24f;
        rightHandValueText = CreateCombatInfoRow(rightHud, "Hand", "HAND", y); y += 24f;
        rightEnemiesValueText = CreateCombatInfoRow(rightHud, "Enemies", "ENEMIES", y); y += 24f;
        rightExpDamageValueText = CreateCombatInfoRow(rightHud, "ExpDamage", "EXP DMG", y);
    }

    private void BuildHandControls()
    {
        if (handArea == null) return;

        RectTransform hpBlock = EnsurePanel("HPBlock", handArea, new Color(0f, 0f, 0f, 0f));
        hpBlockRoot = hpBlock;
        hpBlock.anchorMin = new Vector2(0f, 0f);
        hpBlock.anchorMax = new Vector2(0f, 1f);
        hpBlock.pivot = new Vector2(0f, 0.5f);
        hpBlock.sizeDelta = new Vector2(sideHudWidth, 0f);
        hpBlock.anchoredPosition = Vector2.zero;

        BuildResourceGauge(
            hpBlock,
            "HPGaugeRoot",
            "HPGaugeFill",
            "HPBlockText",
            new Color(0.22f, 0.06f, 0.06f, 0.95f),
            new Color(0.85f, 0.16f, 0.16f, 0.98f),
            hpGaugeFrameSprite,
            hpGaugeFillSprite,
            true,
            out hpGaugeFrameImage,
            out hpGaugeFillImage,
            out hpBlockText,
            new Vector2(0f, 8f));
        if (hpBlockText != null) hpBlockText.color = new Color(1f, 0.95f, 0.92f, 1f);

        if (hpStatusText == null)
        {
            GameObject go = new GameObject("HPStatusText", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(hpBlock, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.sizeDelta = new Vector2(sideHudWidth - 16f, 30f);
            rt.anchoredPosition = new Vector2(0f, 20f);

            hpStatusText = go.GetComponent<Text>();
            hpStatusText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            hpStatusText.alignment = TextAnchor.UpperCenter;
            hpStatusText.fontSize = 10;
            hpStatusText.lineSpacing = 1.05f;
            hpStatusText.color = new Color(0.86f, 0.80f, 0.67f, 1f);
            hpStatusText.text = "SHEILD : 0\nDODGE : 0";
        }

        RectTransform manaBlock = EnsurePanel("ManaBlock", handArea, new Color(0f, 0f, 0f, 0f));
        manaBlock.anchorMin = new Vector2(1f, 0f);
        manaBlock.anchorMax = new Vector2(1f, 1f);
        manaBlock.pivot = new Vector2(1f, 0.5f);
        manaBlock.sizeDelta = new Vector2(sideHudWidth, 0f);
        manaBlock.anchoredPosition = Vector2.zero;

        BuildResourceGauge(
            manaBlock,
            "ManaGaugeRoot",
            "ManaGaugeFill",
            "ManaBlockText",
            new Color(0.05f, 0.10f, 0.18f, 0.95f),
            new Color(0.25f, 0.55f, 0.95f, 0.98f),
            manaGaugeFrameSprite,
            manaGaugeFillSprite,
            false,
            out manaGaugeFrameImage,
            out manaGaugeFillImage,
            out manaBlockText,
            new Vector2(0f, 8f));
        if (manaBlockText != null) manaBlockText.color = new Color(0.86f, 0.94f, 1f, 1f);

        if (handCardContainer == null)
        {
            GameObject go = new GameObject("HandCardContainer", typeof(RectTransform));
            go.transform.SetParent(handArea, false);
            handCardContainer = go.GetComponent<RectTransform>();
        }

        handCardContainer.anchorMin = new Vector2(0f, 0f);
        handCardContainer.anchorMax = new Vector2(1f, 1f);
        handCardContainer.pivot = new Vector2(0.5f, 0.5f);
        handCardContainer.offsetMin = new Vector2(sideHudWidth + 12f, 42f);
        handCardContainer.offsetMax = new Vector2(-(sideHudWidth + 12f), -10f);

        RectMask2D mask = handCardContainer.GetComponent<RectMask2D>();
        if (mask != null)
            Destroy(mask);

        HorizontalLayoutGroup hlg = handCardContainer.GetComponent<HorizontalLayoutGroup>();
        if (hlg != null)
            Destroy(hlg);

        if (endTurnButton == null)
        {
            GameObject go = CreateButton("EndTurnButton", manaBlock, "END TURN\n<size=10><color=#8A7A58>Space</color></size>", new Vector2(140f, 50f));
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, 20f);

            Image bg = go.GetComponent<Image>();
            if (bg != null) bg.color = new Color(0.24f, 0.19f, 0.12f, 1f);

            Outline ol = go.GetComponent<Outline>() ?? go.AddComponent<Outline>();
            ol.effectColor = new Color(0.52f, 0.43f, 0.25f, 1f);
            ol.effectDistance = new Vector2(1f, -1f);

            Button btn = go.GetComponent<Button>();
            btn.transition = Selectable.Transition.ColorTint;
            btn.targetGraphic = bg;
            ColorBlock cb = btn.colors;
            cb.normalColor = new Color(0.24f, 0.19f, 0.12f, 1f);
            cb.highlightedColor = new Color(0.7f, 0.2f, 0.2f, 1f);
            cb.pressedColor = new Color(0.18f, 0.14f, 0.09f, 1f);
            cb.selectedColor = new Color(0.7f, 0.2f, 0.2f, 1f);
            cb.disabledColor = new Color(0.10f, 0.10f, 0.10f, 1f);
            cb.colorMultiplier = 1f;
            cb.fadeDuration = 0.08f;
            btn.colors = cb;

            Text txt = go.GetComponentInChildren<Text>();
            if (txt != null)
            {
                txt.alignment = TextAnchor.MiddleCenter;
                txt.fontStyle = FontStyle.Bold;
                txt.fontSize = 11;
                txt.supportRichText = true;
                txt.lineSpacing = 1.0f;
                txt.color = new Color(0.87f, 0.74f, 0.46f, 1f);
            }

            endTurnButton = go.GetComponent<Button>();
            endTurnButton.onClick.AddListener(() => BattleManager.Instance?.EndPlayerTurnByButton());
        }

        if (handHintText == null)
        {
            GameObject go = new GameObject("HandHintText", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(handArea, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.sizeDelta = new Vector2(620f, 24f);
            rt.anchoredPosition = new Vector2(0f, 10f);

            handHintText = go.GetComponent<Text>();
            handHintText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            handHintText.alignment = TextAnchor.MiddleCenter;
            handHintText.fontSize = 12;
            handHintText.color = new Color(0.72f, 0.64f, 0.45f, 1f);
            handHintText.text = "";
        }
    }

    private void RebuildHandCards()
    {
        foreach (var c in spawnedCards)
            if (c != null) Destroy(c.gameObject);
        spawnedCards.Clear();
        spawnedCardOrders.Clear();
        cardRootByUI.Clear();
        hoveredCard = null;

        BattleManager bm = BattleManager.Instance;
        if (bm == null || handCardContainer == null) return;

        int count = Mathf.Max(0, bm.CurrentHandCardCount);
        for (int i = 0; i < count; i++)
        {
            RuntimeBattleCard runtime = bm.CurrentHandCards[i];
            BattleCardData cardData = runtime != null ? runtime.data : null;
            if (cardData == null) continue;

            BattleCardItemUI card = CreateAttackCard();
            if (card == null) continue;

            ConfigureCardVisualLayout(card);

            bool canUse = (bm.State == BattleState.PlayerTurn && bm.CurrentMana >= cardData.costMana);
            if (card.button != null)
            {
                card.button.interactable = canUse;
                ColorBlock cb = card.button.colors;
                cb.disabledColor = new Color(0.4f, 0.4f, 0.4f, 0.8f);
                cb.colorMultiplier = 1f;
                card.button.colors = cb;
            }

            Outline ol = card.GetComponent<Outline>() ?? card.gameObject.AddComponent<Outline>();
            ol.effectDistance = new Vector2(1f, -1f);
            ol.effectColor = new Color(0.75f, 0.62f, 0.32f, 0.45f);

            Image bg = card.GetComponent<Image>();
            if (bg != null)
                bg.color = canUse ? new Color(0.16f, 0.13f, 0.09f, 1f) : new Color(0.4f, 0.4f, 0.4f, 0.8f);

            Transform topBgT = card.transform.Find("TopBarBg");
            if (topBgT != null)
            {
                Image topImg = topBgT.GetComponent<Image>();
                if (topImg != null)
                    topImg.color = canUse ? new Color(0.08f, 0.08f, 0.08f, 1f) : new Color(0.02f, 0.02f, 0.02f, 1f);
            }

            Transform descBgT = card.transform.Find("DescBg");
            if (descBgT != null)
            {
                Image descImg = descBgT.GetComponent<Image>();
                if (descImg != null)
                    descImg.color = canUse ? new Color(0.08f, 0.08f, 0.08f, 1f) : new Color(0.015f, 0.015f, 0.015f, 1f);
            }

            Transform costBgT = card.transform.Find("CostBg");
            if (costBgT != null)
            {
                Image costImg = costBgT.GetComponent<Image>();
                if (costImg != null)
                    costImg.color = canUse ? new Color(0.25f, 0.55f, 0.95f, 1f) : new Color(0.10f, 0.14f, 0.20f, 1f);
            }

            if (card.artworkImage != null)
                card.artworkImage.color = canUse ? new Color(1f, 1f, 1f, 1f) : new Color(0.30f, 0.30f, 0.30f, 0.45f);

            if (card.titleText != null) card.titleText.color = canUse ? new Color(1f, 0.92f, 0.72f, 1f) : new Color(0.42f, 0.42f, 0.42f, 1f);
            if (card.costText != null)  card.costText.color  = canUse ? Color.white : new Color(0.60f, 0.60f, 0.60f, 1f);
            if (card.descText != null)  card.descText.color  = canUse ? new Color(0.93f, 0.86f, 0.65f, 1f) : new Color(0.40f, 0.40f, 0.40f, 1f);

            bool selected = (i == selectedCardOrder);
            string title = cardData.cardName;
            string desc = string.IsNullOrEmpty(cardData.description) ? BuildCardAutoDescription(cardData) : cardData.description;
            int cardOrder = i;
            card.Bind(title, cardData.costMana, desc, cardData.artwork, () =>
            {
                selectedCardOrder = cardOrder;
                selectedTargetIndex = -1;

                if (RequiresEnemyTarget(cardData))
                {
                    isAttackArmed = true;
                    RefreshTexts();
                    return;
                }

                // Self/None 타겟 카드는 즉시 사용
                bool used = BattleManager.Instance != null && BattleManager.Instance.TryUseCard(cardOrder, -1);
                if (used)
                {
                    isAttackArmed = false;
                    selectedCardOrder = -1;
                    selectedTargetIndex = -1;
                }
                RefreshTexts();
            });

            RectTransform cardRT = card.GetComponent<RectTransform>();
            if (cardRT != null)
            {
                cardRootByUI[card] = cardRT;
                AddCardHoverEvents(card, cardRT);
            }

            spawnedCards.Add(card);
            spawnedCardOrders.Add(cardOrder);
        }

        ApplyCardSelectionVisual();
    }

    private void AddCardHoverEvents(BattleCardItemUI card, RectTransform rt)
    {
        if (card == null || rt == null) return;

        EventTrigger trigger = card.GetComponent<EventTrigger>() ?? card.gameObject.AddComponent<EventTrigger>();
        trigger.triggers.Clear();

        var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enter.callback.AddListener(_ =>
        {
            hoveredCard = card;
            ApplyCardSelectionVisual();
        });
        trigger.triggers.Add(enter);

        var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exit.callback.AddListener(_ =>
        {
            if (hoveredCard == card) hoveredCard = null;
            ApplyCardSelectionVisual();
        });
        trigger.triggers.Add(exit);
    }

    private void ApplyCardSelectionVisual()
    {
        int count = spawnedCards.Count;
        if (count <= 0 || handCardContainer == null) return;

        int focusIndex = -1;
        if (hoveredCard != null)
            focusIndex = spawnedCards.IndexOf(hoveredCard);

        if (focusIndex < 0 && selectedCardOrder >= 0)
        {
            for (int i = 0; i < spawnedCardOrders.Count; i++)
            {
                if (spawnedCardOrders[i] == selectedCardOrder)
                {
                    focusIndex = i;
                    break;
                }
            }
        }

        int clampedCount = Mathf.Min(10, count);
        float containerW = handCardContainer.rect.width;
        if (containerW < 100f && handArea != null)
            containerW = handArea.rect.width - (sideHudWidth * 2f) - 16f;
        containerW = Mathf.Max(320f, containerW);

        const float cardW = 140f;
        const float sideMargin = 6f;
        float usableW = Mathf.Max(cardW, containerW - sideMargin * 2f);

        float step;
        if (clampedCount <= 1)
        {
            step = 0f;
        }
        else if (clampedCount <= 5)
        {
            // 카드가 적을 때는 "붙지 않게" 카드폭 + 소간격 유지
            const float gapSmall = 12f;
            step = cardW + gapSmall;
        }
        else
        {
            step = (usableW - cardW) / (clampedCount - 1);
            step = Mathf.Clamp(step, 28f, cardW + 28f);
        }

        float totalW = cardW + Mathf.Max(0, clampedCount - 1) * step;
        float startX = -totalW * 0.5f + cardW * 0.5f;

        const float pushX = 22f;

        RectTransform focusedRt = null;

        for (int i = 0; i < count; i++)
        {
            BattleCardItemUI card = spawnedCards[i];
            if (card == null || !cardRootByUI.TryGetValue(card, out RectTransform rt) || rt == null) continue;

            int order = i < spawnedCardOrders.Count ? spawnedCardOrders[i] : i;
            bool selected = (order == selectedCardOrder) && isAttackArmed;
            bool hover = (card == hoveredCard);
            bool focused = (i == focusIndex);

            rt.localScale = Vector3.one;

            float x = startX + i * step;
            if (focusIndex >= 0 && i != focusIndex)
                x += i < focusIndex ? -pushX : pushX;

            float y = (selected || hover) ? 10f : 0f;
            rt.anchoredPosition = new Vector2(x, y);

            rt.SetSiblingIndex(i);
            if (focused)
                focusedRt = rt;

            Image bg = card.GetComponent<Image>();
            if (bg != null)
            {
                bool canUseCard = card.button != null && card.button.interactable;
                if (!canUseCard)
                    bg.color = new Color(0.4f, 0.4f, 0.4f, 0.8f);
                else if (selected)
                    bg.color = new Color(0.24f, 0.19f, 0.10f, 1f);
                else
                    bg.color = new Color(0.16f, 0.13f, 0.09f, 1f);
            }
        }

        if (focusedRt != null)
            focusedRt.SetAsLastSibling();
    }

    private void ConfigureCardVisualLayout(BattleCardItemUI card)
    {
        if (card == null) return;

        RectTransform root = card.GetComponent<RectTransform>();
        if (root != null)
        {
            root.anchorMin = new Vector2(0.5f, 0f);
            root.anchorMax = new Vector2(0.5f, 0f);
            root.sizeDelta = new Vector2(140f, 180f);
            root.pivot = new Vector2(0.5f, 0f);
        }

        LayoutElement le = card.GetComponent<LayoutElement>() ?? card.gameObject.AddComponent<LayoutElement>();
        le.preferredWidth = 140f;
        le.preferredHeight = 180f;

        Canvas cardCanvas = card.GetComponent<Canvas>();
        if (cardCanvas != null)
            Destroy(cardCanvas);

        Transform topBarBgT = card.transform.Find("TopBarBg");
        GameObject topBarBg = topBarBgT != null ? topBarBgT.gameObject : new GameObject("TopBarBg", typeof(RectTransform), typeof(Image));
        if (topBarBgT == null) topBarBg.transform.SetParent(card.transform, false);
        RectTransform topBarRT = topBarBg.GetComponent<RectTransform>();
        topBarRT.anchorMin = new Vector2(0f, 1f);
        topBarRT.anchorMax = new Vector2(1f, 1f);
        topBarRT.offsetMin = new Vector2(4f, -34f);
        topBarRT.offsetMax = new Vector2(-4f, -4f);
        Image topBarImg = topBarBg.GetComponent<Image>();
        topBarImg.color = new Color(0.08f, 0.08f, 0.08f, 1f);
        topBarRT.SetSiblingIndex(0);

        Transform costBgT = card.transform.Find("CostBg");
        GameObject costBg = costBgT != null ? costBgT.gameObject : new GameObject("CostBg", typeof(RectTransform), typeof(Image));
        if (costBgT == null) costBg.transform.SetParent(card.transform, false);
        RectTransform costBgRT = costBg.GetComponent<RectTransform>();
        costBgRT.anchorMin = new Vector2(0f, 1f);
        costBgRT.anchorMax = new Vector2(0f, 1f);
        costBgRT.offsetMin = new Vector2(8f, -31f);
        costBgRT.offsetMax = new Vector2(30f, -9f);
        Image costBgImg = costBg.GetComponent<Image>();
        Color manaLike = manaGaugeFillImage != null ? manaGaugeFillImage.color : new Color(0.25f, 0.55f, 0.95f, 1f);
        manaLike.a = 1f;
        costBgImg.color = manaLike;
        costBgRT.SetSiblingIndex(2);

        Transform descBgT = card.transform.Find("DescBg");
        GameObject descBg = descBgT != null ? descBgT.gameObject : new GameObject("DescBg", typeof(RectTransform), typeof(Image));
        if (descBgT == null) descBg.transform.SetParent(card.transform, false);
        RectTransform descBarRT = descBg.GetComponent<RectTransform>();
        descBarRT.anchorMin = new Vector2(0f, 0f);
        descBarRT.anchorMax = new Vector2(1f, 0.40f);
        descBarRT.offsetMin = new Vector2(4f, 4f);
        descBarRT.offsetMax = new Vector2(-4f, -4f);
        Image descBarImg = descBg.GetComponent<Image>();
        descBarImg.color = new Color(0.08f, 0.08f, 0.08f, 1f);
        descBarRT.SetSiblingIndex(0);

        if (card.artworkImage != null)
        {
            RectTransform art = card.artworkImage.rectTransform;
            art.anchorMin = new Vector2(0f, 0.42f);
            art.anchorMax = new Vector2(1f, 0.82f);
            art.offsetMin = new Vector2(8f, 0f);
            art.offsetMax = new Vector2(-8f, 0f);
            art.SetSiblingIndex(1);
        }

        if (card.costText != null)
        {
            RectTransform rt = card.costText.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.offsetMin = new Vector2(8f, -31f);
            rt.offsetMax = new Vector2(30f, -9f);
            card.costText.alignment = TextAnchor.MiddleCenter;
            card.costText.resizeTextForBestFit = false;
            card.costText.horizontalOverflow = HorizontalWrapMode.Overflow;
            card.costText.verticalOverflow = VerticalWrapMode.Overflow;
            card.costText.fontStyle = FontStyle.Bold;
            card.costText.color = Color.white;
            rt.SetAsLastSibling();
        }

        if (card.titleText != null)
        {
            RectTransform rt = card.titleText.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.offsetMin = new Vector2(62f, -31f);
            rt.offsetMax = new Vector2(-8f, -9f);
            card.titleText.alignment = TextAnchor.MiddleLeft;
            card.titleText.resizeTextForBestFit = false;
            card.titleText.horizontalOverflow = HorizontalWrapMode.Overflow;
            card.titleText.verticalOverflow = VerticalWrapMode.Overflow;
            card.titleText.fontStyle = FontStyle.Bold;
            card.titleText.color = new Color(1f, 0.92f, 0.72f, 1f);
            Outline titleOl = card.titleText.GetComponent<Outline>() ?? card.titleText.gameObject.AddComponent<Outline>();
            titleOl.effectColor = new Color(0f, 0f, 0f, 0.7f);
            titleOl.effectDistance = new Vector2(1f, -1f);
            rt.SetAsLastSibling();
        }

        if (card.descText != null)
        {
            RectTransform rt = card.descText.rectTransform;
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0.40f);
            rt.offsetMin = new Vector2(8f, 8f);
            rt.offsetMax = new Vector2(-8f, -6f);
            rt.SetAsLastSibling();
        }
    }

    private BattleCardItemUI CreateAttackCard()
    {
        if (attackCardPrefab != null)
            return Instantiate(attackCardPrefab, handCardContainer);

        // 프리팹 미지정 시 런타임 생성 fallback
        GameObject go = new GameObject("AttackCard", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement), typeof(BattleCardItemUI));
        go.transform.SetParent(handCardContainer, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(140f, 180f);
        rt.pivot = new Vector2(0.5f, 0f);

        Image bg = go.GetComponent<Image>();
        bg.color = new Color(0.16f, 0.13f, 0.09f, 1f);

        LayoutElement le = go.GetComponent<LayoutElement>();
        le.preferredWidth = 140f;
        le.preferredHeight = 180f;

        BattleCardItemUI ui = go.GetComponent<BattleCardItemUI>();
        ui.button = go.GetComponent<Button>();

        GameObject artGo = new GameObject("Artwork", typeof(RectTransform), typeof(Image));
        artGo.transform.SetParent(go.transform, false);
        RectTransform artRt = artGo.GetComponent<RectTransform>();
        artRt.anchorMin = new Vector2(0f, 0.42f);
        artRt.anchorMax = new Vector2(1f, 0.82f);
        artRt.offsetMin = new Vector2(8f, 0f);
        artRt.offsetMax = new Vector2(-8f, 0f);
        Image artImg = artGo.GetComponent<Image>();
        artImg.enabled = false;
        artImg.preserveAspect = true;
        ui.artworkImage = artImg;

        ui.costText  = CreateCardText(go.transform, "Cost",  new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(8f, -31f), new Vector2(30f, -9f), TextAnchor.MiddleCenter, 12);
        ui.titleText = CreateCardText(go.transform, "Title", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(62f, -31f), new Vector2(-8f, -9f), TextAnchor.MiddleLeft, 12);
        ui.descText  = CreateCardText(go.transform, "Desc",  new Vector2(0f, 0f), new Vector2(1f, 0.40f), new Vector2(8f, 8f), new Vector2(-8f, -6f), TextAnchor.UpperLeft, 11);

        return ui;
    }

    private Text CreateCardText(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, TextAnchor align, int fontSize)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;

        Text t = go.GetComponent<Text>();
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.alignment = align;
        t.fontSize = fontSize;
        t.color = new Color(0.93f, 0.86f, 0.65f, 1f);
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        return t;
    }

    private void RebuildMonsterButtons()
    {
        foreach (var go in spawnedMonsterButtons)
            if (go != null) Destroy(go);
        spawnedMonsterButtons.Clear();
        monsterButtonBgByIndex.Clear();
        monsterRootByIndex.Clear();
        monsterPortraitByIndex.Clear();
        monsterPortraitImageByIndex.Clear();
        monsterDamageNextTime.Clear();
        flashingMonsterIndices.Clear();

        BattleManager bm = BattleManager.Instance;
        if (bm == null || monsterContainer == null || bm.Monsters == null) return;

        for (int i = 0; i < bm.Monsters.Count; i++)
        {
            RuntimeMonster m = bm.Monsters[i];
            if (m == null || m.IsDead) continue;

            GameObject go;
            BattleMonsterItemUI monsterUI = null;

            if (monsterItemPrefab != null)
            {
                monsterUI = Instantiate(monsterItemPrefab, monsterContainer);
                go = monsterUI.gameObject;
            }
            else
            {
                go = CreateButton($"Monster_{i}", monsterContainer, "", monsterPanelSize);
                monsterUI = go.GetComponent<BattleMonsterItemUI>() ?? go.AddComponent<BattleMonsterItemUI>();
                monsterUI.button = go.GetComponent<Button>();

                // 이미지 전용 몬스터 슬롯
                if (monsterUI.portraitImage == null)
                {
                    GameObject portraitGo = new GameObject("PortraitImage", typeof(RectTransform), typeof(Image));
                    portraitGo.transform.SetParent(go.transform, false);
                    RectTransform prt = portraitGo.GetComponent<RectTransform>();
                    prt.anchorMin = new Vector2(0f, 0f);
                    prt.anchorMax = new Vector2(1f, 1f);
                    prt.offsetMin = new Vector2(6f, 6f);
                    prt.offsetMax = new Vector2(-6f, -6f);

                    Image portrait = portraitGo.GetComponent<Image>();
                    portrait.preserveAspect = true;
                    monsterUI.portraitImage = portrait;
                }

                // fallback intent text 생성
                if (monsterUI.intentIcon == null)
                {
                    GameObject ii = new GameObject("IntentIcon", typeof(RectTransform), typeof(Image));
                    ii.transform.SetParent(go.transform, false);
                    RectTransform iirt = ii.GetComponent<RectTransform>();
                    iirt.anchorMin = new Vector2(0f, 1f);
                    iirt.anchorMax = new Vector2(0f, 1f);
                    iirt.pivot = new Vector2(0f, 1f);
                    iirt.sizeDelta = new Vector2(18f, 18f);
                    iirt.anchoredPosition = new Vector2(6f, -6f);

                    Image icon = ii.GetComponent<Image>();
                    icon.color = new Color(0.95f, 0.70f, 0.45f, 1f);
                    icon.sprite = intentAttackSprite;
                    icon.preserveAspect = true;
                    monsterUI.intentIcon = icon;
                }

                if (monsterUI.intentText == null)
                {
                    GameObject it = new GameObject("IntentText", typeof(RectTransform), typeof(Text));
                    it.transform.SetParent(go.transform, false);
                    RectTransform irt = it.GetComponent<RectTransform>();
                    irt.anchorMin = new Vector2(0f, 1f);
                    irt.anchorMax = new Vector2(1f, 1f);
                    irt.offsetMin = new Vector2(28f, -28f);
                    irt.offsetMax = new Vector2(-6f, -6f);

                    Text itt = it.GetComponent<Text>();
                    itt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    itt.fontSize = 16;
                    itt.fontStyle = FontStyle.Bold;
                    itt.alignment = TextAnchor.UpperLeft;
                    itt.color = new Color(0.98f, 0.86f, 0.48f, 1f);
                    monsterUI.intentText = itt;
                }
            }

            RectTransform slotRT = go.GetComponent<RectTransform>();
            if (slotRT != null)
                slotRT.sizeDelta = monsterPanelSize;

            LayoutElement monsterLE = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
            monsterLE.preferredWidth = monsterPanelSize.x;
            monsterLE.preferredHeight = monsterPanelSize.y;

            if (monsterUI.intentIcon != null && intentAttackSprite != null)
                monsterUI.intentIcon.sprite = intentAttackSprite;

            int targetIndex = i;
            int intentDamage = (m.data != null) ? BattleMath.CalcFinalDamage(m.data.damageConst, m.data.damagePer) : 0;
            monsterUI.Bind(m, intentDamage, () =>
            {
                selectedTargetIndex = targetIndex;

                if (!isAttackArmed)
                {
                    RefreshTexts();
                    return;
                }

                BattleManager bmgr = BattleManager.Instance;
                int cardOrder = selectedCardOrder >= 0 ? selectedCardOrder : 0;
                RuntimeBattleCard runtime = (bmgr != null && cardOrder < bmgr.CurrentHandCards.Count) ? bmgr.CurrentHandCards[cardOrder] : null;
                BattleCardData cardData = runtime != null ? runtime.data : null;

                bool used = bmgr != null && bmgr.TryUseCard(cardOrder, targetIndex);
                if (used)
                {
                    isAttackArmed = false;
                    selectedCardOrder = -1;
                    selectedTargetIndex = -1;
                    // TryUseCard 내부 OnBattleValuesChanged에서 RefreshTexts가 이미 호출됨.
                    // 여기서 추가 Refresh를 호출하면 즉시 재생성되어 연출이 사라질 수 있음.
                    return;
                }

                RefreshTexts();
            });

            RectTransform rootRT = go.GetComponent<RectTransform>();
            if (rootRT != null) monsterRootByIndex[targetIndex] = rootRT;
            if (monsterUI != null && monsterUI.portraitImage != null)
            {
                RectTransform prt = monsterUI.portraitImage.rectTransform;
                if (prt != null) monsterPortraitByIndex[targetIndex] = prt;
                monsterPortraitImageByIndex[targetIndex] = monsterUI.portraitImage;
            }

            Image bg = go.GetComponent<Image>();
            if (bg != null) monsterButtonBgByIndex[targetIndex] = bg;

            EventTrigger trigger = go.GetComponent<EventTrigger>() ?? go.AddComponent<EventTrigger>();
            trigger.triggers.Clear();

            var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enter.callback.AddListener(_ =>
            {
                if (bg == null) return;
                if (flashingMonsterIndices.Contains(targetIndex)) return;
                if (targetIndex == selectedTargetIndex) return;
                bg.color = isAttackArmed ? colorMonsterTarget : colorMonsterHover;
            });
            trigger.triggers.Add(enter);

            var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exit.callback.AddListener(_ =>
            {
                if (bg == null) return;
                if (flashingMonsterIndices.Contains(targetIndex)) return;
                if (targetIndex == selectedTargetIndex)
                    bg.color = colorMonsterSelected;
                else
                    bg.color = colorMonsterNormal;
            });
            trigger.triggers.Add(exit);

            spawnedMonsterButtons.Add(go);
        }
    }

    private void ApplyMonsterTargetVisual()
    {
        foreach (var kv in monsterButtonBgByIndex)
        {
            int idx = kv.Key;
            Image bg = kv.Value;
            if (bg == null) continue;

            if (idx == selectedTargetIndex)
                bg.color = colorMonsterSelected;
            else
                bg.color = isAttackArmed ? colorMonsterTarget : colorMonsterNormal;
        }
    }

    private string BuildCardAutoDescription(BattleCardData card)
    {
        if (card == null) return "-";

        var effects = card.GetEffects();
        if (effects == null || effects.Count == 0) return "-";

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        for (int i = 0; i < effects.Count; i++)
        {
            var e = effects[i];
            if (e == null) continue;

            if (sb.Length > 0) sb.Append("\n");

            switch (e.effectType)
            {
                case BattleCardEffectType.Attack:
                    switch (e.targetType)
                    {
                        case BattleCardTargetType.EnemyAll: sb.Append("모든 적에게 피해를 줍니다."); break;
                        case BattleCardTargetType.EnemySingleAdjacent: sb.Append("대상과 인접한 적에게 피해를 줍니다."); break;
                        default: sb.Append("적 1체에게 피해를 줍니다."); break;
                    }
                    break;
                case BattleCardEffectType.GainShield:
                    sb.Append($"방어를 {e.amount:0} 얻습니다.");
                    break;
                case BattleCardEffectType.Heal:
                    sb.Append($"체력을 {e.amount:0} 회복합니다.");
                    break;
                case BattleCardEffectType.GainDodge:
                    sb.Append($"회피를 {e.amount:0} 얻습니다.");
                    break;
                case BattleCardEffectType.GainMana:
                    sb.Append($"마나를 {e.amount:0} 얻습니다.");
                    break;
                case BattleCardEffectType.GainDamagePer:
                    sb.Append($"피해%를 {e.amount:0} 얻습니다. (전투 종료까지)");
                    break;
                case BattleCardEffectType.DrawCard:
                    sb.Append($"카드를 {e.amount:0}장 뽑습니다.");
                    break;
            }
        }

        return sb.Length > 0 ? sb.ToString() : "-";
    }

    private bool RequiresEnemyTarget(BattleCardData card)
    {
        if (card == null) return false;
        var effects = card.GetEffects();
        if (effects == null) return false;

        for (int i = 0; i < effects.Count; i++)
        {
            var e = effects[i];
            if (e == null || e.effectType != BattleCardEffectType.Attack) continue;

            if (e.targetType == BattleCardTargetType.EnemySingle
                || e.targetType == BattleCardTargetType.EnemySingleAdjacent)
                return true;
        }

        return false;
    }

    private int GetAliveMonsterIndexByOrder(int order)
    {
        BattleManager bm = BattleManager.Instance;
        if (bm == null || bm.Monsters == null || order < 0) return -1;

        int aliveOrder = 0;
        for (int i = 0; i < bm.Monsters.Count; i++)
        {
            RuntimeMonster m = bm.Monsters[i];
            if (m == null || m.IsDead) continue;

            if (aliveOrder == order)
                return i;

            aliveOrder++;
        }

        return -1;
    }

    private void ValidatePrefabSetup()
    {
        if (attackCardPrefab != null)
            attackCardPrefab.ValidateReferences(true);

        if (monsterItemPrefab != null)
            monsterItemPrefab.ValidateReferences(true);
    }

    public IEnumerator PlayEnemyAttackFx(int monsterIndex, Action onImpact, int damage)
    {
        if (!monsterRootByIndex.TryGetValue(monsterIndex, out RectTransform rt) || rt == null)
        {
            onImpact?.Invoke();
            PlayPlayerHitFx(damage);
            StartHitShake();
            yield break;
        }

        Vector2 basePos = rt.anchoredPosition;
        Vector2 hitPos = basePos + new Vector2(0f, -10f);

        float speedMul = 1f + UnityEngine.Random.Range(-enemyAttackSpeedVariance, enemyAttackSpeedVariance);
        speedMul = Mathf.Max(0.35f, speedMul);

        float downDur = enemyAttackDownDuration / speedMul;
        float holdDur = enemyAttackHoldDuration / speedMul;
        float upDur = enemyAttackUpDuration / speedMul;

        float t = 0f;
        while (t < downDur)
        {
            t += Time.deltaTime;
            float k = downDur > 0f ? Mathf.Clamp01(t / downDur) : 1f;
            rt.anchoredPosition = Vector2.Lerp(basePos, hitPos, k);
            yield return null;
        }

        rt.anchoredPosition = hitPos;
        onImpact?.Invoke();
        PlayPlayerHitFx(damage);
        StartHitShake();

        if (holdDur > 0f)
            yield return new WaitForSeconds(holdDur);

        t = 0f;
        while (t < upDur)
        {
            t += Time.deltaTime;
            float k = upDur > 0f ? Mathf.Clamp01(t / upDur) : 1f;
            rt.anchoredPosition = Vector2.Lerp(hitPos, basePos, k);
            yield return null;
        }

        rt.anchoredPosition = basePos;
    }

    private void HandleMonsterDamaged(int targetIndex, int damage, Sprite hitEffectSprite)
    {
        if (damage <= 0) return;

        if (monsterButtonBgByIndex.TryGetValue(targetIndex, out Image bg) && bg != null)
            StartCoroutine(CoFlashMonster(targetIndex, bg));

        if (monsterPortraitImageByIndex.TryGetValue(targetIndex, out Image portraitImage) && portraitImage != null)
            StartCoroutine(CoFlashMonsterPortrait(portraitImage));

        if (monsterRootByIndex.TryGetValue(targetIndex, out RectTransform rootRt) && rootRt != null)
            StartCoroutine(CoMonsterHitRecoil(rootRt, targetIndex));

        RectTransform anchorRt = rootRt;
        if (monsterPortraitByIndex.TryGetValue(targetIndex, out RectTransform portraitRt) && portraitRt != null)
            anchorRt = portraitRt;

        if (anchorRt == null)
            return;

        if (!TryResolveCanvasLocalFromRectCenter(anchorRt, out Vector2 localPos))
            return;

        localPos += monsterDamageTextOffset;

        float now = Time.time;
        float nextAt = monsterDamageNextTime.TryGetValue(targetIndex, out float queuedAt) ? queuedAt : now;
        float delay = Mathf.Max(0f, nextAt - now);
        monsterDamageNextTime[targetIndex] = now + delay + 0.06f;

        StartCoroutine(CoDamageFloatDelayedOnCanvas(localPos, damage, delay));

        if (hitEffectSprite != null)
            StartCoroutine(CoMonsterHitSprite(anchorRt, hitEffectSprite, delay));
    }

    private IEnumerator CoDamageFloatDelayedOnCanvas(Vector2 canvasLocalPos, int damage, float delay)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        yield return CoDamageFloatOnCanvas(canvasLocalPos, damage);
    }

    private bool TryResolveCanvasLocalFromRectCenter(RectTransform target, out Vector2 local)
    {
        local = Vector2.zero;
        if (target == null) return false;

        Canvas rootCanvas = GetComponentInParent<Canvas>();
        if (rootCanvas == null) return false;

        RectTransform canvasRT = rootCanvas.transform as RectTransform;
        if (canvasRT == null) return false;

        Camera cam = null;
        if (rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            cam = rootCanvas.worldCamera;

        Vector3[] corners = new Vector3[4];
        target.GetWorldCorners(corners);
        Vector3 worldCenter = (corners[0] + corners[2]) * 0.5f;
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(cam, worldCenter);

        return RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRT, screenPoint, cam, out local);
    }

    private void PlayPlayerHitFx(int damage)
    {
        if (handArea != null)
        {
            Image handBg = handArea.GetComponent<Image>();
            if (handBg != null) StartCoroutine(CoFlashHud(handBg, colorHand));
        }

        if (hpBlockRoot != null)
        {
            Image hpBg = hpBlockRoot.GetComponent<Image>();
            if (hpBg != null) StartCoroutine(CoFlashHud(hpBg, new Color(0f, 0f, 0f, 0f)));
            StartCoroutine(CoDamageFloat(hpBlockRoot, damage));
        }
    }

    private IEnumerator CoFlashMonster(int targetIndex, Image bg)
    {
        flashingMonsterIndices.Add(targetIndex);

        Color flash = new Color(1f, 0.55f, 0.55f, 0.45f);
        bg.color = flash;
        yield return new WaitForSeconds(monsterHitFlashDuration);

        flashingMonsterIndices.Remove(targetIndex);

        if (targetIndex == selectedTargetIndex)
            bg.color = colorMonsterSelected;
        else
            bg.color = isAttackArmed ? colorMonsterTarget : colorMonsterNormal;
    }

    private IEnumerator CoFlashHud(Image bg, Color baseColor)
    {
        bg.color = new Color(1f, 0.45f, 0.45f, 1f);
        yield return new WaitForSeconds(0.1f);
        bg.color = baseColor;
    }

    private IEnumerator CoFlashMonsterPortrait(Image portraitImage)
    {
        if (portraitImage == null) yield break;

        Color baseColor = portraitImage.color;
        portraitImage.color = new Color(1f, 0.45f, 0.45f, baseColor.a);
        yield return new WaitForSeconds(monsterHitFlashDuration);

        if (portraitImage != null)
            portraitImage.color = baseColor;
    }

    private IEnumerator CoMonsterHitRecoil(RectTransform rootRt, int targetIndex)
    {
        if (rootRt == null) yield break;

        RectTransform portraitRt = null;
        monsterPortraitByIndex.TryGetValue(targetIndex, out portraitRt);

        Vector2 rootBasePos = rootRt.anchoredPosition;
        Vector3 rootBaseScale = rootRt.localScale;
        Vector2 portraitBasePos = portraitRt != null ? portraitRt.anchoredPosition : Vector2.zero;
        Vector3 portraitBaseScale = portraitRt != null ? portraitRt.localScale : Vector3.one;

        Vector2 recoilOffset = new Vector2(0f, monsterHitRecoilDistance);
        Vector3 recoilScale = new Vector3(monsterHitRecoilScale, monsterHitRecoilScale, 1f);

        float upDur = Mathf.Max(0.001f, monsterHitRecoilUpDuration);
        float downDur = Mathf.Max(0.001f, monsterHitRecoilDownDuration);

        float t = 0f;
        while (t < upDur)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / upDur);
            if (rootRt != null)
            {
                rootRt.anchoredPosition = Vector2.Lerp(rootBasePos, rootBasePos + recoilOffset, k);
                rootRt.localScale = Vector3.Lerp(rootBaseScale, recoilScale, k);
            }
            if (portraitRt != null)
            {
                portraitRt.anchoredPosition = Vector2.Lerp(portraitBasePos, portraitBasePos + recoilOffset * 0.35f, k);
                portraitRt.localScale = Vector3.Lerp(portraitBaseScale, recoilScale, k);
            }
            yield return null;
        }

        t = 0f;
        while (t < downDur)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / downDur);
            if (rootRt != null)
            {
                rootRt.anchoredPosition = Vector2.Lerp(rootBasePos + recoilOffset, rootBasePos, k);
                rootRt.localScale = Vector3.Lerp(recoilScale, rootBaseScale, k);
            }
            if (portraitRt != null)
            {
                portraitRt.anchoredPosition = Vector2.Lerp(portraitBasePos + recoilOffset * 0.35f, portraitBasePos, k);
                portraitRt.localScale = Vector3.Lerp(recoilScale, portraitBaseScale, k);
            }
            yield return null;
        }

        if (rootRt != null)
        {
            rootRt.anchoredPosition = rootBasePos;
            rootRt.localScale = rootBaseScale;
        }
        if (portraitRt != null)
        {
            portraitRt.anchoredPosition = portraitBasePos;
            portraitRt.localScale = portraitBaseScale;
        }
    }

    private IEnumerator CoMonsterHitSprite(RectTransform anchorRt, Sprite hitEffectSprite, float delay)
    {
        if (anchorRt == null || hitEffectSprite == null) yield break;
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        Canvas rootCanvas = GetComponentInParent<Canvas>();
        if (rootCanvas == null) yield break;

        RectTransform canvasRT = rootCanvas.transform as RectTransform;
        if (canvasRT == null) yield break;

        if (!TryResolveCanvasLocalFromRectCenter(anchorRt, out Vector2 localPos))
            yield break;

        GameObject go = new GameObject("MonsterHitEffect", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(canvasRT, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = localPos;
        rt.sizeDelta = new Vector2(hitEffectSprite.rect.width, hitEffectSprite.rect.height) * monsterHitEffectScale;
        rt.SetAsLastSibling();

        Image img = go.GetComponent<Image>();
        img.sprite = hitEffectSprite;
        img.preserveAspect = true;
        img.color = new Color(1f, 1f, 1f, 1f);

        float dur = Mathf.Max(0.01f, monsterHitEffectDuration);
        float elapsed = 0f;
        Vector3 startScale = Vector3.one * monsterHitEffectScale * 0.96f;
        Vector3 flashScale = Vector3.one * monsterHitEffectScale * 1.06f;
        rt.localScale = startScale;

        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float k = Mathf.Clamp01(elapsed / dur);
            float scaleK = Mathf.Clamp01(k / 0.18f);
            rt.localScale = Vector3.Lerp(startScale, flashScale, scaleK);

            float alpha = k < 0.12f ? 1f : 1f - Mathf.Clamp01((k - 0.12f) / 0.88f);
            img.color = new Color(1f, 1f, 1f, alpha);
            yield return null;
        }

        if (go != null) Destroy(go);
    }

    private void StartHitShake()
    {
        if (battleField != null)
            StartCoroutine(CoShakeRect(battleField, hitShakeDuration, hitShakeStrength));
        if (handArea != null)
            StartCoroutine(CoShakeRect(handArea, hitShakeDuration, hitShakeStrength * 0.5f));
    }

    private IEnumerator CoShakeRect(RectTransform rt, float duration, float strength)
    {
        if (rt == null || duration <= 0f || strength <= 0f) yield break;

        Vector2 origin = rt.anchoredPosition;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float fade = 1f - Mathf.Clamp01(t / duration);
            Vector2 offset = UnityEngine.Random.insideUnitCircle * strength * fade;
            rt.anchoredPosition = origin + offset;
            yield return null;
        }

        rt.anchoredPosition = origin;
    }

    private IEnumerator CoDamageFloat(RectTransform target, int damage)
    {
        if (target == null) yield break;

        GameObject go = new GameObject("HitDamageText", typeof(RectTransform), typeof(Text));
        go.transform.SetParent(target, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0f, 0f);
        rt.sizeDelta = new Vector2(100f, 30f);

        Text t = go.GetComponent<Text>();
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.alignment = TextAnchor.MiddleCenter;
        t.fontSize = 20;
        t.fontStyle = FontStyle.Bold;
        t.color = new Color(1f, 0.40f, 0.40f, 1f);
        t.text = $"-{Mathf.Max(0, damage)}";

        float dur = 0.35f;
        float elapsed = 0f;
        Vector2 start = rt.anchoredPosition;

        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float k = Mathf.Clamp01(elapsed / dur);
            rt.anchoredPosition = start + new Vector2(0f, 26f * k);
            t.color = new Color(t.color.r, t.color.g, t.color.b, 1f - k * 0.9f);
            yield return null;
        }

        if (go != null) Destroy(go);
    }

    private IEnumerator CoDamageFloatOnCanvas(Vector2 canvasLocalPos, int damage)
    {
        Canvas rootCanvas = GetComponentInParent<Canvas>();
        if (rootCanvas == null) yield break;

        RectTransform canvasRT = rootCanvas.transform as RectTransform;
        if (canvasRT == null) yield break;

        GameObject go = new GameObject("HitDamageText", typeof(RectTransform), typeof(Text));
        go.transform.SetParent(canvasRT, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = canvasLocalPos;
        rt.sizeDelta = new Vector2(120f, 36f);
        rt.SetAsLastSibling();

        Text t = go.GetComponent<Text>();
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.alignment = TextAnchor.MiddleCenter;
        t.fontSize = 22;
        t.fontStyle = FontStyle.Bold;
        t.color = new Color(1f, 0.40f, 0.40f, 1f);
        t.text = $"-{Mathf.Max(0, damage)}";

        float dur = 0.35f;
        float elapsed = 0f;
        Vector2 start = rt.anchoredPosition;

        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float k = Mathf.Clamp01(elapsed / dur);
            rt.anchoredPosition = start + new Vector2(0f, 26f * k);
            t.color = new Color(t.color.r, t.color.g, t.color.b, 1f - k * 0.9f);
            yield return null;
        }

        if (go != null) Destroy(go);
    }

    private Text CreateHudHeader(RectTransform parent, string name, string label)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(0f, 22f);
        rt.anchoredPosition = new Vector2(0f, -10f);
        rt.offsetMin = new Vector2(8f, rt.offsetMin.y);
        rt.offsetMax = new Vector2(-6f, rt.offsetMax.y);

        Text t = go.GetComponent<Text>();
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.alignment = TextAnchor.MiddleLeft;
        t.fontSize = 11;
        t.fontStyle = FontStyle.Bold;
        t.color = new Color(0.62f, 0.56f, 0.43f, 1f);
        t.text = label;
        return t;
    }

    private RectTransform CreateHudRowRoot(RectTransform parent, string name, float topY)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(-12f, 24f);
        rt.anchoredPosition = new Vector2(0f, -topY);

        Image bg = go.GetComponent<Image>();
        bg.color = new Color(0.11f, 0.09f, 0.065f, 1f);

        return rt;
    }

    private Text CreateHudRowLabel(RectTransform parent, string name, string text, int fontSize, Color color, float left = 4f, float right = 0.35f)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(right, 1f);
        rt.offsetMin = new Vector2(left, 0f);
        rt.offsetMax = new Vector2(0f, 0f);

        Text t = go.GetComponent<Text>();
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.alignment = TextAnchor.MiddleLeft;
        t.fontSize = fontSize;
        t.color = color;
        t.text = text;
        return t;
    }

    private Text CreateHudRowValue(RectTransform parent, string name, string text, int fontSize, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.55f, 0f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.offsetMin = new Vector2(0f, 0f);
        rt.offsetMax = new Vector2(-6f, 0f);

        Text t = go.GetComponent<Text>();
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.alignment = TextAnchor.MiddleRight;
        t.fontSize = fontSize;
        t.fontStyle = FontStyle.Bold;
        t.color = color;
        t.text = text;
        return t;
    }

    private Text CreateCombatInfoRow(RectTransform parent, string name, string key, float topY)
    {
        RectTransform row = CreateHudRowRoot(parent, $"CombatRow_{name}", topY);
        CreateHudRowLabel(row, "Key", key, 9, new Color(0.64f, 0.57f, 0.43f, 1f), 6f, 0.58f);
        return CreateHudRowValue(row, "Value", "-", 10, new Color(0.89f, 0.82f, 0.66f, 1f));
    }

    private void BuildResourceGauge(
        RectTransform parent,
        string gaugeRootName,
        string fillName,
        string textName,
        Color baseColor,
        Color fillColor,
        Sprite frameSprite,
        Sprite fillSprite,
        bool useHpStage,
        out Image frameImage,
        out Image fillImage,
        out Text valueText,
        Vector2 centerOffset)
    {
        Transform gaugeT = parent.Find(gaugeRootName);
        GameObject gaugeGo = gaugeT != null ? gaugeT.gameObject : new GameObject(gaugeRootName, typeof(RectTransform), typeof(Image), typeof(Outline));
        if (gaugeT == null) gaugeGo.transform.SetParent(parent, false);

        RectTransform grt = gaugeGo.GetComponent<RectTransform>();
        grt.anchorMin = new Vector2(0.5f, 0.5f);
        grt.anchorMax = new Vector2(0.5f, 0.5f);
        grt.pivot = new Vector2(0.5f, 0.5f);
        grt.sizeDelta = new Vector2(70f, 70f);
        grt.anchoredPosition = centerOffset;

        Image baseImg = gaugeGo.GetComponent<Image>() ?? gaugeGo.AddComponent<Image>();
        Sprite defaultSprite = GetDefaultUiSprite();
        baseImg.sprite = frameSprite != null ? frameSprite : (baseImg.sprite != null ? baseImg.sprite : defaultSprite);
        baseImg.color = baseColor;
        baseImg.type = Image.Type.Simple;
        baseImg.preserveAspect = true;
        frameImage = baseImg;

        Outline outline = gaugeGo.GetComponent<Outline>() ?? gaugeGo.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.5f);
        outline.effectDistance = new Vector2(1f, -1f);

        Transform fillT = gaugeGo.transform.Find(fillName);
        GameObject fillGo = fillT != null ? fillT.gameObject : new GameObject(fillName, typeof(RectTransform), typeof(Image));
        if (fillT == null) fillGo.transform.SetParent(gaugeGo.transform, false);

        RectTransform frt = fillGo.GetComponent<RectTransform>();
        frt.anchorMin = new Vector2(0f, 0f);
        frt.anchorMax = new Vector2(1f, 1f);
        frt.offsetMin = new Vector2(hpFillInset, hpFillInset);
        frt.offsetMax = new Vector2(-hpFillInset, -hpFillInset);

        fillImage = fillGo.GetComponent<Image>() ?? fillGo.AddComponent<Image>();
        fillImage.sprite = fillSprite != null ? fillSprite : (fillImage.sprite != null ? fillImage.sprite : defaultSprite);
        fillImage.color = fillColor;
        fillImage.preserveAspect = false;
        fillImage.fillAmount = 1f;

        if (useHpStage)
        {
            hpUseStageSprite = HasHpStageSprites();
            fillImage.type = Image.Type.Simple;
        }
        else
        {
            fillImage.type = Image.Type.Simple;
        }

        Transform textT = gaugeGo.transform.Find(textName);
        GameObject textGo = textT != null ? textT.gameObject : new GameObject(textName, typeof(RectTransform), typeof(Text));
        if (textT == null) textGo.transform.SetParent(gaugeGo.transform, false);

        RectTransform trt = textGo.GetComponent<RectTransform>();
        trt.anchorMin = new Vector2(0f, 0f);
        trt.anchorMax = new Vector2(1f, 1f);
        trt.offsetMin = new Vector2(0f, 0f);
        trt.offsetMax = new Vector2(0f, 0f);

        valueText = textGo.GetComponent<Text>() ?? textGo.AddComponent<Text>();
        valueText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        valueText.alignment = TextAnchor.MiddleCenter;
        valueText.fontSize = 13;
        valueText.fontStyle = FontStyle.Bold;
        valueText.lineSpacing = 1.1f;
    }

    private bool HasHpStageSprites()
    {
        return hpStageFullSprite != null
            || hpStageHighSprite != null
            || hpStageMidSprite != null
            || hpStageLowSprite != null
            || hpStageCriticalSprite != null;
    }

    private Sprite GetHpStageSprite(float hpRatio)
    {
        if (hpRatio > 0.8f)
            return hpStageFullSprite ?? hpStageHighSprite ?? hpStageMidSprite ?? hpStageLowSprite ?? hpStageCriticalSprite;
        if (hpRatio > 0.6f)
            return hpStageHighSprite ?? hpStageFullSprite ?? hpStageMidSprite ?? hpStageLowSprite ?? hpStageCriticalSprite;
        if (hpRatio > 0.35f)
            return hpStageMidSprite ?? hpStageHighSprite ?? hpStageLowSprite ?? hpStageFullSprite ?? hpStageCriticalSprite;
        if (hpRatio > 0.15f)
            return hpStageLowSprite ?? hpStageMidSprite ?? hpStageCriticalSprite ?? hpStageHighSprite ?? hpStageFullSprite;
        return hpStageCriticalSprite ?? hpStageLowSprite ?? hpStageMidSprite ?? hpStageHighSprite ?? hpStageFullSprite;
    }

    private void UpdateHpGaugeVisual(float hpRatio)
    {
        if (hpGaugeFillImage == null) return;

        if (hpUseStageSprite)
        {
            Sprite s = GetHpStageSprite(hpRatio);
            if (s != null)
                hpGaugeFillImage.sprite = s;
        }

        // 위 -> 아래로 줄어드는 방식: 상단을 깎아서 높이 비율만 남긴다.
        RectTransform frt = hpGaugeFillImage.rectTransform;
        RectTransform parentRt = hpGaugeFrameImage != null ? hpGaugeFrameImage.rectTransform : frt.parent as RectTransform;
        float parentH = parentRt != null ? parentRt.rect.height : 70f;
        float innerH = Mathf.Max(1f, parentH - hpFillInset * 2f);
        float hiddenFromTop = (1f - hpRatio) * innerH;

        frt.offsetMin = new Vector2(hpFillInset, hpFillInset);
        frt.offsetMax = new Vector2(-hpFillInset, -hpFillInset - hiddenFromTop);
    }

    private void UpdateManaGaugeVisual(float manaRatio)
    {
        if (manaGaugeFillImage == null) return;

        RectTransform frt = manaGaugeFillImage.rectTransform;
        RectTransform parentRt = manaGaugeFrameImage != null ? manaGaugeFrameImage.rectTransform : frt.parent as RectTransform;
        float parentH = parentRt != null ? parentRt.rect.height : 70f;
        float innerH = Mathf.Max(1f, parentH - hpFillInset * 2f);
        float hiddenFromTop = (1f - manaRatio) * innerH;

        frt.offsetMin = new Vector2(hpFillInset, hpFillInset);
        frt.offsetMax = new Vector2(-hpFillInset, -hpFillInset - hiddenFromTop);
    }

    private Sprite GetDefaultUiSprite()
    {
        if (runtimeFallbackUiSprite != null) return runtimeFallbackUiSprite;

        Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        Color c = Color.white;
        tex.SetPixels(new[] { c, c, c, c });
        tex.Apply();
        tex.name = "BattleUI_RuntimeFallbackSpriteTex";

        runtimeFallbackUiSprite = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 100f);
        runtimeFallbackUiSprite.name = "BattleUI_RuntimeFallbackSprite";
        return runtimeFallbackUiSprite;
    }

    private RectTransform EnsurePanel(string name, RectTransform parent, Color color)
    {
        Transform t = parent.Find(name);
        GameObject go = t != null ? t.gameObject : new GameObject(name, typeof(RectTransform), typeof(Image));
        if (t == null) go.transform.SetParent(parent, false);

        Image bg = go.GetComponent<Image>() ?? go.AddComponent<Image>();
        bg.color = color;
        return go.GetComponent<RectTransform>();
    }

    private Text EnsureBlockText(string name, RectTransform parent, TextAnchor anchor, int fontSize, Color color)
    {
        Transform t = parent.Find(name);
        GameObject go = t != null ? t.gameObject : new GameObject(name, typeof(RectTransform), typeof(Text));
        if (t == null) go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.offsetMin = new Vector2(6f, 6f);
        rt.offsetMax = new Vector2(-6f, -6f);

        Text txt = go.GetComponent<Text>() ?? go.AddComponent<Text>();
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.alignment = anchor;
        txt.fontSize = fontSize;
        txt.color = color;
        return txt;
    }

    private GameObject CreateButton(string name, RectTransform parent, string label, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = size;

        Image bg = go.GetComponent<Image>();
        bg.color = colorMonsterNormal;

        GameObject textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
        textGo.transform.SetParent(go.transform, false);
        RectTransform trt = textGo.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(6f, 6f);
        trt.offsetMax = new Vector2(-6f, -6f);

        Text txt = textGo.GetComponent<Text>();
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.text = label;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = new Color(0.93f, 0.86f, 0.65f, 1f);
        txt.fontSize = 14;

        return go;
    }

    private void SetBg(GameObject go, Color color)
    {
        Image img = go.GetComponent<Image>() ?? go.AddComponent<Image>();
        img.type = Image.Type.Simple;
        img.sprite = null;
        img.color = color;
    }

    private void ShowImmediate()
    {
        if (canvasGroup == null) return;
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }

    private void HideImmediate()
    {
        if (canvasGroup == null) return;
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }
}

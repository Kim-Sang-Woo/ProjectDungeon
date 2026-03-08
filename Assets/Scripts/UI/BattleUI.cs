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

    [Header("레이아웃 수치")]
    public float width = 1280f;
    public float height = 720f;
    public float topBarHeight = 48f;
    public float handAreaHeight = 220f;
    public float sideHudWidth = 180f;
    public float padding = 16f;

    [Header("색상")]
    public Color colorTopBar = new Color(0.08f, 0.07f, 0.05f, 0.92f);
    public Color colorBattleField = new Color(0.04f, 0.04f, 0.04f, 0.72f);
    public Color colorSideHud = new Color(0.06f, 0.055f, 0.04f, 0.85f);
    public Color colorHand = new Color(0.07f, 0.06f, 0.045f, 0.92f);

    private Button endTurnButton;
    private bool isAttackArmed;
    private int selectedCardOrder = -1;

    private Text leftStatsText;
    private Text rightStatsText;
    private Text resultBannerText;
    private Text handHintText;

    private float prevPlayerHP = -1f;

    private readonly List<GameObject> spawnedMonsterButtons = new List<GameObject>();
    private readonly Dictionary<int, Image> monsterButtonBgByIndex = new Dictionary<int, Image>();
    private readonly Dictionary<int, RectTransform> monsterRootByIndex = new Dictionary<int, RectTransform>();
    private readonly HashSet<int> flashingMonsterIndices = new HashSet<int>();
    private readonly List<BattleCardItemUI> spawnedCards = new List<BattleCardItemUI>();

    private int selectedTargetIndex = -1;

    private readonly Color colorMonsterNormal = new Color(0.15f, 0.13f, 0.09f, 0.95f);
    private readonly Color colorMonsterHover = new Color(0.24f, 0.18f, 0.10f, 0.98f);
    private readonly Color colorMonsterTarget = new Color(0.32f, 0.24f, 0.12f, 1f);
    private readonly Color colorMonsterSelected = new Color(0.38f, 0.30f, 0.14f, 1f);

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
            for (int i = 0; i < keys.Length; i++)
            {
                if (!Input.GetKeyDown(keys[i])) continue;
                if (i < 0 || i >= spawnedCards.Count) return;

                selectedCardOrder = i;

                RuntimeBattleCard runtime = (bm != null && i < bm.CurrentHandCards.Count) ? bm.CurrentHandCards[i] : null;
                BattleCardData cardData = runtime != null ? runtime.data : null;

                if (cardData != null && RequiresEnemyTarget(cardData))
                {
                    isAttackArmed = true;
                    RefreshTexts();
                    return;
                }

                bool used = bm != null && bm.TryUseCard(i, -1);
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
        for (int i = 0; i < keys.Length; i++)
        {
            if (!Input.GetKeyDown(keys[i])) continue;

            int targetIndex = GetAliveMonsterIndexByOrder(i);
            if (targetIndex < 0) return;

            int cardOrder = selectedCardOrder >= 0 ? selectedCardOrder : 0;
            RuntimeBattleCard runtime = (bm != null && cardOrder < bm.CurrentHandCards.Count) ? bm.CurrentHandCards[cardOrder] : null;
            BattleCardData cardData = runtime != null ? runtime.data : null;

            CharacterStats stats = bm != null ? (bm.characterStats != null ? bm.characterStats : CharacterStats.Instance) : CharacterStats.Instance;
            int predictedHit = stats != null
                ? BattleMath.CalcFinalDamage(stats.damageConst.FinalValue, stats.damagePer.FinalValue)
                : 0;
            if (cardData != null)
                predictedHit = Mathf.FloorToInt(predictedHit * Mathf.Max(0f, cardData.attackMultiplier) + cardData.amount);

            bool used = bm != null && bm.TryUseCard(cardOrder, targetIndex);
            if (used)
            {
                PlayMonsterHitFx(targetIndex, predictedHit);
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
        }
    }

    public void ApplyLayout()
    {
        RectTransform panelRT = GetComponent<RectTransform>();
        if (panelRT != null)
        {
            panelRT.anchorMin = new Vector2(0.5f, 0.5f);
            panelRT.anchorMax = new Vector2(0.5f, 0.5f);
            panelRT.pivot = new Vector2(0.5f, 0.5f);
            panelRT.sizeDelta = new Vector2(width, height);
            panelRT.anchoredPosition = Vector2.zero;
        }

        if (topBar != null)
        {
            topBar.anchorMin = new Vector2(0f, 1f);
            topBar.anchorMax = new Vector2(1f, 1f);
            topBar.pivot = new Vector2(0.5f, 1f);
            topBar.offsetMin = new Vector2(0f, -topBarHeight);
            topBar.offsetMax = new Vector2(0f, 0f);
            SetBg(topBar.gameObject, colorTopBar);

            if (stageText != null)
            {
                RectTransform rt = stageText.rectTransform;
                rt.anchorMin = new Vector2(0f, 0f);
                rt.anchorMax = new Vector2(0.5f, 1f);
                rt.offsetMin = new Vector2(padding, 0f);
                rt.offsetMax = new Vector2(0f, 0f);
                stageText.alignment = TextAnchor.MiddleLeft;
                stageText.text = "전투";
            }

            if (goldText != null)
            {
                RectTransform rt = goldText.rectTransform;
                rt.anchorMin = new Vector2(0.5f, 0f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.offsetMin = new Vector2(0f, 0f);
                rt.offsetMax = new Vector2(-padding, 0f);
                goldText.alignment = TextAnchor.MiddleRight;
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

            BuildMonsterContainer();
            BuildResultBanner();
        }

        BuildHandControls();
        RebuildMonsterButtons();

        Debug.Log("[BattleUI] ApplyLayout 완료");
    }

    private void OnBattleStateChanged(BattleState state)
    {
        if (state == BattleState.BattleStart || state == BattleState.RoundStart || state == BattleState.PlayerTurn || state == BattleState.EnemyTurn || state == BattleState.Victory || state == BattleState.Defeat)
            ShowImmediate();

        if (state == BattleState.BattleEnd)
            HideImmediate();

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
        {
            string stateLabel = bm.State.ToString();
            if (bm.State == BattleState.Victory) stateLabel = "승리";
            else if (bm.State == BattleState.Defeat) stateLabel = "패배";
            else if (bm.State == BattleState.BattleEnd) stateLabel = "종료";
            stageText.text = $"전투 R{bm.RoundIndex} / {stateLabel}";
        }

        if (goldText != null)
            goldText.text = $"Mana {bm.CurrentMana}  Hand {bm.CurrentHandCount}";

        CharacterStats stats = bm.characterStats != null ? bm.characterStats : CharacterStats.Instance;
        if (leftStatsText != null && stats != null)
        {
            leftStatsText.text =
                $"플레이어\n\n" +
                $"HP {stats.currentHP:0}/{stats.maxHP.FinalValue:0}\n" +
                $"Shield {stats.currentShield:0}\n" +
                $"Dodge {stats.currentDodge:0}\n" +
                $"HP Regen {stats.hpGen.FinalValue:0}";

            if (prevPlayerHP >= 0f)
            {
                float delta = stats.currentHP - prevPlayerHP;
                if (delta < 0f)
                    PlayPlayerHitFx(Mathf.FloorToInt(-delta));
            }
            prevPlayerHP = stats.currentHP;
        }

        if (rightStatsText != null)
        {
            int alive = 0;
            if (bm.Monsters != null)
                foreach (var m in bm.Monsters)
                    if (m != null && !m.IsDead) alive++;

            rightStatsText.text =
                $"전투 정보\n\n" +
                $"State {bm.State}\n" +
                $"Round {bm.RoundIndex}\n" +
                $"Mana {bm.CurrentMana}\n" +
                $"Hand {bm.CurrentHandCount}\n" +
                $"적 생존 {alive}\n" +
                $"예상 피해 {bm.PredictedEnemyDamage}";
        }

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

        RebuildHandCards();

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
            else if (bm.CurrentHandCount <= 0)
                handHintText.text = "사용 가능한 카드가 없습니다.";
            else if (bm.CurrentMana < minCost)
                handHintText.text = "마나가 부족합니다.";
            else if (isAttackArmed)
                handHintText.text = "대상을 선택하세요. (1~0 / ESC / 우클릭)";
            else
                handHintText.text = "카드를 선택하세요. (1~0, Space: 턴 종료)";
        }

        if (endTurnButton != null)
            endTurnButton.interactable = (bm.State == BattleState.PlayerTurn);

        if (selectedTargetIndex >= 0)
        {
            if (bm.Monsters == null || selectedTargetIndex >= bm.Monsters.Count || bm.Monsters[selectedTargetIndex] == null || bm.Monsters[selectedTargetIndex].IsDead)
                selectedTargetIndex = -1;
        }

        RebuildMonsterButtons();
        ApplyMonsterTargetVisual();
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
        monsterContainer.sizeDelta = new Vector2(760f, 220f);
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

        GameObject go = new GameObject("LeftStatsText", typeof(RectTransform), typeof(Text));
        go.transform.SetParent(leftHud, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.offsetMin = new Vector2(12f, 12f);
        rt.offsetMax = new Vector2(-12f, -12f);

        leftStatsText = go.GetComponent<Text>();
        leftStatsText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        leftStatsText.alignment = TextAnchor.UpperLeft;
        leftStatsText.fontSize = 14;
        leftStatsText.lineSpacing = 1.25f;
        leftStatsText.color = new Color(0.93f, 0.86f, 0.65f, 1f);
    }

    private void BuildRightHud()
    {
        if (rightHud == null || rightStatsText != null) return;

        GameObject go = new GameObject("RightStatsText", typeof(RectTransform), typeof(Text));
        go.transform.SetParent(rightHud, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.offsetMin = new Vector2(12f, 12f);
        rt.offsetMax = new Vector2(-12f, -12f);

        rightStatsText = go.GetComponent<Text>();
        rightStatsText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        rightStatsText.alignment = TextAnchor.UpperLeft;
        rightStatsText.fontSize = 14;
        rightStatsText.lineSpacing = 1.25f;
        rightStatsText.color = new Color(0.93f, 0.86f, 0.65f, 1f);
    }

    private void BuildHandControls()
    {
        if (handArea == null) return;

        if (handCardContainer == null)
        {
            GameObject go = new GameObject("HandCardContainer", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            go.transform.SetParent(handArea, false);
            handCardContainer = go.GetComponent<RectTransform>();

            handCardContainer.anchorMin = new Vector2(0.5f, 0.5f);
            handCardContainer.anchorMax = new Vector2(0.5f, 0.5f);
            handCardContainer.pivot = new Vector2(0.5f, 0.5f);
            handCardContainer.sizeDelta = new Vector2(700f, 160f);
            handCardContainer.anchoredPosition = new Vector2(0f, 0f);

            HorizontalLayoutGroup hlg = go.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 16f;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
        }

        if (endTurnButton == null)
        {
            GameObject go = CreateButton("EndTurnButton", handArea, "턴 종료", new Vector2(140f, 56f));
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 0.5f);
            rt.anchorMax = new Vector2(1f, 0.5f);
            rt.pivot = new Vector2(1f, 0.5f);
            rt.anchoredPosition = new Vector2(-padding, 0f);

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
            rt.sizeDelta = new Vector2(520f, 28f);
            rt.anchoredPosition = new Vector2(0f, 10f);

            handHintText = go.GetComponent<Text>();
            handHintText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            handHintText.alignment = TextAnchor.MiddleCenter;
            handHintText.fontSize = 13;
            handHintText.color = new Color(0.95f, 0.72f, 0.50f, 1f);
            handHintText.text = "";
        }
    }

    private void RebuildHandCards()
    {
        foreach (var c in spawnedCards)
            if (c != null) Destroy(c.gameObject);
        spawnedCards.Clear();

        BattleManager bm = BattleManager.Instance;
        if (bm == null || handCardContainer == null) return;

        int count = Mathf.Max(0, bm.CurrentHandCount);
        for (int i = 0; i < count; i++)
        {
            RuntimeBattleCard runtime = bm.CurrentHandCards[i];
            BattleCardData cardData = runtime != null ? runtime.data : null;
            if (cardData == null) continue;

            BattleCardItemUI card = CreateAttackCard();
            if (card == null) continue;

            bool canUse = (bm.State == BattleState.PlayerTurn && bm.CurrentMana >= cardData.costMana);
            if (card.button != null)
                card.button.interactable = canUse;

            Image bg = card.GetComponent<Image>();
            if (bg != null)
                bg.color = canUse ? new Color(0.16f, 0.13f, 0.09f, 0.98f) : new Color(0.10f, 0.10f, 0.10f, 0.85f);

            if (card.titleText != null) card.titleText.color = canUse ? new Color(0.93f, 0.86f, 0.65f, 1f) : new Color(0.62f, 0.62f, 0.62f, 1f);
            if (card.costText != null)  card.costText.color  = canUse ? new Color(0.93f, 0.86f, 0.65f, 1f) : new Color(0.62f, 0.62f, 0.62f, 1f);
            if (card.descText != null)  card.descText.color  = canUse ? new Color(0.93f, 0.86f, 0.65f, 1f) : new Color(0.62f, 0.62f, 0.62f, 1f);

            bool selected = (i == selectedCardOrder);
            string title = selected ? $"[선택중] {cardData.cardName}" : cardData.cardName;
            string desc = string.IsNullOrEmpty(cardData.description) ? "-" : cardData.description;
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

            spawnedCards.Add(card);
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
        rt.sizeDelta = new Vector2(180f, 140f);

        Image bg = go.GetComponent<Image>();
        bg.color = new Color(0.16f, 0.13f, 0.09f, 0.98f);

        LayoutElement le = go.GetComponent<LayoutElement>();
        le.preferredWidth = 180f;
        le.preferredHeight = 140f;

        BattleCardItemUI ui = go.GetComponent<BattleCardItemUI>();
        ui.button = go.GetComponent<Button>();

        GameObject artGo = new GameObject("Artwork", typeof(RectTransform), typeof(Image));
        artGo.transform.SetParent(go.transform, false);
        RectTransform artRt = artGo.GetComponent<RectTransform>();
        artRt.anchorMin = new Vector2(0f, 0.35f);
        artRt.anchorMax = new Vector2(1f, 0.78f);
        artRt.offsetMin = new Vector2(8f, 0f);
        artRt.offsetMax = new Vector2(-8f, 0f);
        Image artImg = artGo.GetComponent<Image>();
        artImg.enabled = false;
        artImg.preserveAspect = true;
        ui.artworkImage = artImg;

        ui.titleText = CreateCardText(go.transform, "Title", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(8f, -8f), new Vector2(-8f, -32f), TextAnchor.UpperLeft, 14);
        ui.costText  = CreateCardText(go.transform, "Cost",  new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-36f, -8f), new Vector2(-8f, -32f), TextAnchor.UpperRight, 16);
        ui.descText  = CreateCardText(go.transform, "Desc",  new Vector2(0f, 0f), new Vector2(1f, 0.34f), new Vector2(8f, 8f), new Vector2(-8f, -6f), TextAnchor.UpperLeft, 11);

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
                go = CreateButton($"Monster_{i}", monsterContainer, "", new Vector2(140f, 120f));
                monsterUI = go.GetComponent<BattleMonsterItemUI>() ?? go.AddComponent<BattleMonsterItemUI>();
                monsterUI.button = go.GetComponent<Button>();
                monsterUI.nameText = go.GetComponentInChildren<Text>();
                monsterUI.hpText = monsterUI.nameText;

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
                    iirt.anchoredPosition = new Vector2(8f, -8f);

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
                    irt.offsetMin = new Vector2(30f, -30f);
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

                CharacterStats stats = bmgr != null ? (bmgr.characterStats != null ? bmgr.characterStats : CharacterStats.Instance) : CharacterStats.Instance;
                int predictedHit = stats != null
                    ? BattleMath.CalcFinalDamage(stats.damageConst.FinalValue, stats.damagePer.FinalValue)
                    : 0;
                if (cardData != null)
                    predictedHit = Mathf.FloorToInt(predictedHit * Mathf.Max(0f, cardData.attackMultiplier) + cardData.amount);

                bool used = bmgr != null && bmgr.TryUseCard(cardOrder, targetIndex);
                if (used)
                {
                    PlayMonsterHitFx(targetIndex, predictedHit);
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

    private bool RequiresEnemyTarget(BattleCardData card)
    {
        if (card == null) return false;
        if (card.effectType != BattleCardEffectType.Attack) return false;

        return card.targetType == BattleCardTargetType.EnemySingle
            || card.targetType == BattleCardTargetType.EnemySingleAdjacent;
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

    private void PlayMonsterHitFx(int targetIndex, int damage)
    {
        if (monsterButtonBgByIndex.TryGetValue(targetIndex, out Image bg) && bg != null)
            StartCoroutine(CoFlashMonster(targetIndex, bg));

        if (monsterRootByIndex.TryGetValue(targetIndex, out RectTransform rt) && rt != null)
            StartCoroutine(CoDamageFloat(rt, damage));
    }

    private void PlayPlayerHitFx(int damage)
    {
        if (leftHud != null)
        {
            Image bg = leftHud.GetComponent<Image>();
            if (bg != null) StartCoroutine(CoFlashHud(bg, colorSideHud));

            StartCoroutine(CoDamageFloat(leftHud, damage));
        }
    }

    private IEnumerator CoFlashMonster(int targetIndex, Image bg)
    {
        flashingMonsterIndices.Add(targetIndex);

        Color flash = new Color(1f, 0.55f, 0.55f, 1f);
        bg.color = flash;
        yield return new WaitForSeconds(0.1f);

        flashingMonsterIndices.Remove(targetIndex);

        // 플래시 종료 후 현재 상태 기준 색으로 복구
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

// ============================================================
// EventData.cs — 이벤트 최상위 ScriptableObject + 팩토리
// 위치: Assets/Scripts/Events/Data/EventData.cs
// ============================================================
// [개요]
//   이벤트 한 건의 모든 정적 데이터를 담는 SO.
//   FromTreasureChest / FromStairs 팩토리 메서드로
//   DungeonObjectData를 EventData로 런타임 변환한다.
// ============================================================
using UnityEngine;

[CreateAssetMenu(fileName = "Event_New", menuName = "Event/EventData")]
public class EventData : ScriptableObject
{
    [Header("식별")]
    [Tooltip("고유 이벤트 ID (영문+언더바). 예) ev_rusty_lock")]
    public string eventId;

    [Tooltip("UI 헤더에 표시되는 이벤트 이름")]
    public string eventName;

    [Header("내용")]
    [TextArea(2, 6)]
    [Tooltip("ChoiceView에 표시되는 이벤트 상황 설명")]
    public string desc;

    [Tooltip("이벤트 이미지. null이면 이미지 영역 비활성")]
    public Sprite image;

    [Header("선택지 (최대 5개)")]
    [Tooltip("순서 = 숫자키 1~5. 최대 5개.")]
    public EventChoice[] choices;

    [Header("반복 여부")]
    [Tooltip("true = 이미 소진된 타일에서도 재발생")]
    public bool isRepeatable = false;

    // ────────────────────────────────────────────────────────
    // 런타임 팩토리 — DungeonObjectData → EventData 변환
    // ScriptableObject.CreateInstance로 휘발성 SO를 생성한다.
    // 팝업이 닫히면 GC가 수거한다 (에셋 등록 불필요).
    // ────────────────────────────────────────────────────────

    /// <summary>
    /// 보물 상자 DungeonObjectData를 EventData로 변환한다.
    /// 선택지: [열기] → GainItemEffect 실행 / [그냥 지나친다] → 닫기
    /// </summary>
    public static EventData FromTreasureChest(
        DungeonObjectData obj,
        Vector2Int        tilePos,
        TileData          tile,
        DungeonObjectSpawner spawner)
    {
        var data = CreateInstance<EventData>();
        data.eventId   = $"obj_{obj.objectId}_{tilePos.x}_{tilePos.y}";
        data.eventName = obj.displayName;
        data.desc      = obj.description;
        data.image     = obj.sprite;

        // ── 결과: 열기 성공 ──────────────────────────────
        var openResult = CreateInstance<EventResult>();
        openResult.resultId   = "open_success";
        openResult.resultDesc = "상자가 열렸다.";

        // GainItemEffect 목록 생성
        var effects = new System.Collections.Generic.List<EventEffect>();
        if (obj.rewardItems != null)
        {
            for (int i = 0; i < obj.rewardItems.Length; i++)
            {
                if (obj.rewardItems[i] == null) continue;
                int qty = (obj.rewardQuantities != null && i < obj.rewardQuantities.Length)
                    ? Mathf.Max(1, obj.rewardQuantities[i]) : 1;

                effects.Add(new GainItemEffect { item = obj.rewardItems[i], amount = qty });
            }
        }

        // 보물상자 소진 효과 (isOneTime)
        if (obj.isOneTime)
            effects.Add(new ConsumeObjectEffect { tilePos = tilePos, tile = tile, spawner = spawner });

        openResult.effects     = effects.ToArray();
        openResult.nextChoices = new EventChoice[0]; // 닫기 선택지 자동 추가

        // 기본 문구 — DungeonObjectData.choiceActionLabel/choiceCloseLabel 우선 사용
        string actionLabel = !string.IsNullOrEmpty(obj.choiceActionLabel) ? obj.choiceActionLabel : "상자를 연다.";
        string closeLabel  = !string.IsNullOrEmpty(obj.choiceCloseLabel)  ? obj.choiceCloseLabel  : "그냥 지나친다.";

        // ── 선택지 1: 열기 ──────────────────────────────
        var openChoice = CreateInstance<EventChoice>();
        openChoice.choiceId     = "open";
        openChoice.choiceType   = ChoiceType.Default;
        openChoice.label        = actionLabel;
        openChoice.successRate  = 100;
        openChoice.onSuccess    = openResult;

        // ── 선택지 2: 닫기 ──────────────────────────────
        var closeChoice = CreateInstance<EventChoice>();
        closeChoice.choiceId   = "ignore";
        closeChoice.choiceType = ChoiceType.Close;
        closeChoice.label      = closeLabel;

        data.choices = new EventChoice[] { openChoice, closeChoice };
        return data;
    }

    /// <summary>
    /// 계단 DungeonObjectData를 EventData로 변환한다.
    /// 선택지: [이동] → 층 이동 실행 / [머무른다] → 닫기
    /// </summary>
    public static EventData FromStairs(
        DungeonObjectData obj,
        bool              isDown,
        DungeonManager    dungeonManager,
        MovementSystem    movementSystem)
    {
        var data = CreateInstance<EventData>();
        data.eventId   = isDown ? "stairs_down" : "stairs_up";
        data.eventName = obj.displayName;
        data.desc      = obj.description;
        data.image     = obj.sprite;

        // 기본 문구 — DungeonObjectData.choiceActionLabel/choiceCloseLabel 우선 사용
        string defaultAction = isDown ? "계단을 내려간다." : "계단을 올라간다.";
        string defaultClose  = "머무른다.";
        string actionLabel   = !string.IsNullOrEmpty(obj.choiceActionLabel) ? obj.choiceActionLabel : defaultAction;
        string closeLabel    = !string.IsNullOrEmpty(obj.choiceCloseLabel)  ? obj.choiceCloseLabel  : defaultClose;

        // ── 선택지 1: 이동 — onSuccess=null, directEffects로 즉시 실행 ──
        var goChoice = CreateInstance<EventChoice>();
        goChoice.choiceId       = isDown ? "go_down" : "go_up";
        goChoice.choiceType     = ChoiceType.Default;
        goChoice.label          = actionLabel;
        goChoice.successRate    = 100;
        goChoice.onSuccess      = null; // ResultView 없이 즉시 실행
        goChoice.directEffects  = new EventEffect[]
        {
            new StairsMoveEffect
            {
                isDown         = isDown,
                dungeonManager = dungeonManager,
                movementSystem = movementSystem,
            }
        };

        // ── 선택지 2: 닫기 ──────────────────────────────
        var stayChoice = CreateInstance<EventChoice>();
        stayChoice.choiceId   = "stay";
        stayChoice.choiceType = ChoiceType.Close;
        stayChoice.label      = closeLabel;

        data.choices = new EventChoice[] { goChoice, stayChoice };
        return data;
    }
}

// ============================================================
// GameManager.cs — 싱글톤 게임 매니저
// 기획서 Ch.5.1 참조 — Script Execution Order: -100
// 위치: Assets/Scripts/Core/GameManager.cs
// ============================================================
// [v2 변경사항]
//   - switch-case → Dictionary<DungeonEventType, Action> 핸들러 등록
//   - 새 이벤트 타입 추가 시 RegisterHandler()만 호출하면 됨
// ============================================================
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 게임 전체를 총괄하는 싱글톤 매니저.
/// 이벤트 발생 시 타입별 분기 처리의 중앙 허브 역할.
/// </summary>
public class GameManager : MonoBehaviour
{
    // ─── 싱글톤 ───
    public static GameManager Instance { get; private set; }

    // ─── 이벤트 핸들러 레지스트리 ───
    private Dictionary<DungeonEventType, Action<DungeonEventData>> eventHandlers;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitDefaultHandlers();
    }

    // ─── 핸들러 초기화 ───

    private void InitDefaultHandlers()
    {
        eventHandlers = new Dictionary<DungeonEventType, Action<DungeonEventData>>
        {
            { DungeonEventType.COMBAT,   HandleCombatEvent },
            { DungeonEventType.TRAP,     HandleTrapEvent },
            { DungeonEventType.TREASURE, HandleTreasureEvent },
            { DungeonEventType.NPC,      HandleNPCEvent },
            { DungeonEventType.SHRINE,   HandleShrineEvent },
            { DungeonEventType.SPECIAL,  HandleSpecialEvent },
        };
    }

    /// <summary>
    /// 외부 시스템에서 커스텀 핸들러를 등록할 수 있다.
    /// 기존 핸들러가 있으면 덮어쓴다.
    /// </summary>
    public void RegisterHandler(DungeonEventType type, Action<DungeonEventData> handler)
    {
        eventHandlers[type] = handler;
    }

    // ─── 이벤트 처리 ───

    /// <summary>
    /// EventTriggerSystem이 호출하는 이벤트 타입별 분기 처리 메서드.
    /// 기획서 Ch.0.6: GameManager.OnEventTriggered(DungeonEventData) → 이벤트 타입별 처리 분기
    /// </summary>
    public void HandleDungeonEvent(DungeonEventData eventData)
    {
        if (eventData == null)
        {
            Debug.LogWarning("[GameManager] HandleDungeonEvent: eventData가 null입니다.");
            return;
        }

        Debug.Log($"[GameManager] 이벤트 발생 — 타입: {eventData.eventType}, 이름: {eventData.displayName}");

        if (eventHandlers.TryGetValue(eventData.eventType, out var handler))
        {
            handler.Invoke(eventData);
        }
        else
        {
            Debug.LogWarning($"[GameManager] 등록되지 않은 이벤트 타입: {eventData.eventType}");
        }
    }

    // ─── 이벤트 타입별 기본 핸들러 (v1.0: 콘솔 출력만 수행) ───

    private void HandleCombatEvent(DungeonEventData data)
    {
        // TODO: 전투 시스템으로 전환 (별도 기획서)
        Debug.Log($"[EventTrigger] COMBAT — {data.displayName} ({data.eventId})");
    }

    private void HandleTrapEvent(DungeonEventData data)
    {
        // TODO: 즉발형 피해/디버프 처리
        Debug.Log($"[EventTrigger] TRAP — {data.displayName} ({data.eventId})");
    }

    private void HandleTreasureEvent(DungeonEventData data)
    {
        // TODO: 인벤토리 시스템 연결
        Debug.Log($"[EventTrigger] TREASURE — {data.displayName} ({data.eventId})");
    }

    private void HandleNPCEvent(DungeonEventData data)
    {
        // TODO: 대화 시스템 연결 (TBD)
        Debug.Log($"[EventTrigger] NPC — {data.displayName} ({data.eventId})");
    }

    private void HandleShrineEvent(DungeonEventData data)
    {
        // TODO: HP/버프 회복 처리
        Debug.Log($"[EventTrigger] SHRINE — {data.displayName} ({data.eventId})");
    }

    private void HandleSpecialEvent(DungeonEventData data)
    {
        // TODO: 추후 확장
        Debug.Log($"[EventTrigger] SPECIAL — {data.displayName} ({data.eventId})");
    }
}

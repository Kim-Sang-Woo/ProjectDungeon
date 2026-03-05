// ============================================================
// GameManager.cs — 싱글톤 게임 매니저
// 기획서 Ch.5.1 참조 — Script Execution Order: -100
// ============================================================
using UnityEngine;

/// <summary>
/// 게임 전체를 총괄하는 싱글톤 매니저.
/// 이벤트 발생 시 타입별 분기 처리의 중앙 허브 역할.
/// </summary>
public class GameManager : MonoBehaviour
{
    // ─── 싱글톤 ───
    public static GameManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
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

        switch (eventData.eventType)
        {
            case DungeonEventType.COMBAT:
                HandleCombatEvent(eventData);
                break;
            case DungeonEventType.TRAP:
                HandleTrapEvent(eventData);
                break;
            case DungeonEventType.TREASURE:
                HandleTreasureEvent(eventData);
                break;
            case DungeonEventType.NPC:
                HandleNPCEvent(eventData);
                break;
            case DungeonEventType.SHRINE:
                HandleShrineEvent(eventData);
                break;
            case DungeonEventType.SPECIAL:
                HandleSpecialEvent(eventData);
                break;
            default:
                Debug.LogWarning($"[GameManager] 알 수 없는 이벤트 타입: {eventData.eventType}");
                break;
        }
    }

    // ─── 이벤트 타입별 핸들러 (v1.0: 콘솔 출력만 수행) ───

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

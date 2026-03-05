// ============================================================
// DungeonEventData.cs — 이벤트 정의 (ScriptableObject)
// 기획서 Ch.0.4 참조
// 위치: Assets/Scripts/Data/DungeonEventData.cs
// ============================================================
// [v2 변경사항]
//   - spawnWeight 필드 추가: 이벤트 배치 시 가중치 기반 확률 선택
//     weight가 높을수록 자주 배치됨
//     예: COMBAT(10), TRAP(5), TREASURE(3) → 전투가 가장 자주 발생
// ============================================================
using UnityEngine;

[CreateAssetMenu(fileName = "Event_New", menuName = "Dungeon/Event Data")]
public class DungeonEventData : ScriptableObject
{
    [Header("이벤트 식별")]
    [Tooltip("고유 이벤트 식별자 (예: trap_fire, monster_goblin)")]
    public string eventId;

    [Tooltip("이벤트 타입")]
    public DungeonEventType eventType;

    [Header("이벤트 속성")]
    [Tooltip("재발생 여부 (true = 재생 함정 등)")]
    public bool isRepeatable = false;

    [Tooltip("이벤트 UI 표시명")]
    public string displayName;

    [Tooltip("이벤트 발생 시 표시 아이콘 (선택)")]
    public Sprite iconSprite;

    [Header("배치 확률")]
    [Tooltip("배치 가중치 (높을수록 자주 배치됨). 0이면 배치되지 않음.")]
    [Min(0)]
    public int spawnWeight = 10;
}

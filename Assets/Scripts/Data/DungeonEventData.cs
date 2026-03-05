// ============================================================
// DungeonEventData.cs — 이벤트 정의 (ScriptableObject)
// 기획서 Ch.0.4 참조
// ============================================================
using UnityEngine;

/// <summary>
/// 던전 내 개별 이벤트를 정의하는 ScriptableObject.
/// Unity 에디터에서 Assets/Data/Events/ 폴더에 에셋으로 생성하여 사용한다.
/// </summary>
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

    // TODO: 이벤트별 세부 파라미터는 서브클래스 또는 별도 기획서에서 확장
}

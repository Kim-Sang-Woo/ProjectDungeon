// ============================================================
// DungeonObjectData.cs — 던전 오브젝트 데이터 (ScriptableObject)
// 위치: Assets/Scripts/Data/DungeonObjectData.cs
// ============================================================
// [v2 변경사항]
//   - ObjectAction 구조체 추가: 액션 ID + 표시 라벨을 쌍으로 정의
//   - actions 배열: Inspector에서 직접 액션 목록을 구성
//   - GetActionLabel() 제거 → actions 배열로 완전 대체
//
// [Inspector 설정 예시 — 보물 상자]
//   Actions:
//     [0] actionId: open    / label: 열기
//     [1] actionId: ignore  / label: 내버려 두기
//
// [Inspector 설정 예시 — 계단]
//   Actions:
//     [0] actionId: go_down / label: 내려가기
//     [1] actionId: ignore  / label: 그냥 있기
// ============================================================
using UnityEngine;

/// <summary>
/// 인터렉션 UI에 표시되는 단일 액션.
/// actionId는 InteractionSystem의 처리 분기 키로 사용된다.
/// </summary>
[System.Serializable]
public class ObjectAction
{
    [Tooltip("처리 분기용 ID (예: open, ignore, go_down, go_up)")]
    public string actionId;

    [Tooltip("UI에 표시되는 텍스트 (예: 열기, 내버려 두기)")]
    public string label;
}

[CreateAssetMenu(fileName = "Object_New", menuName = "Dungeon/Object Data")]
public class DungeonObjectData : ScriptableObject
{
    [Header("오브젝트 식별")]
    [Tooltip("고유 ID (예: treasure_chest_basic)")]
    public string objectId;

    [Tooltip("오브젝트 타입 — 인터렉션 방식 결정")]
    public DungeonObjectType objectType;

    [Header("표시 정보")]
    [Tooltip("인터렉션 UI에 표시되는 이름")]
    public string displayName;

    [Tooltip("인터렉션 UI에 표시되는 설명")]
    [TextArea(2, 4)]
    public string description;

    [Tooltip("타일 위에 렌더링할 스프라이트")]
    public Sprite sprite;

    [Tooltip("픽셀 단위 스프라이트 크기 (타일 1칸 기준 100)")]
    public float pixelsPerUnit = 100f;

    [Header("배치 설정")]
    [Tooltip("한 번 상호작용하면 오브젝트가 사라지는가")]
    public bool isOneTime = true;

    [Tooltip("배치 가중치 (높을수록 자주 배치). 0이면 자동 배치 안 됨")]
    [Min(0)]
    public int spawnWeight = 5;

    [Header("보상 아이템 (보물 상자 등)")]
    [Tooltip("상호작용 시 지급할 아이템 목록")]
    public ItemData[] rewardItems;

    [Tooltip("각 아이템의 지급 수량 (rewardItems와 같은 인덱스)")]
    public int[] rewardQuantities;

    [Header("액션 목록")]
    [Tooltip("인터렉션 UI에 표시할 액션 목록. 순서대로 표시된다.")]
    public ObjectAction[] actions;

    [Header("이벤트 연결")]
    [Tooltip("직접 구성한 EventData SO를 연결하면 팩토리 자동 생성 대신 이 데이터를 사용한다.\n" +
             "null이면 rewardItems / objectType 기반으로 자동 생성.")]
    public EventData eventOverride;

    [Header("팩토리 선택지 문구 (eventOverride가 null일 때 적용)")]
    [Tooltip("선택지 1번 문구. 비어 있으면 기본값 사용.\n" +
             "보물상자 기본: '상자를 연다.' / 계단 기본: '계단을 내려간다.' 또는 '계단을 올라간다.'")]
    public string choiceActionLabel;

    [Tooltip("선택지 2번(닫기) 문구. 비어 있으면 기본값 사용.\n" +
             "보물상자 기본: '그냥 지나친다.' / 계단 기본: '머무른다.'")]
    public string choiceCloseLabel;
}

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

    [Header("액션 목록")]
    [Tooltip("인터렉션 UI에 표시할 액션 목록. 순서대로 표시된다.")]
    public ObjectAction[] actions;
}

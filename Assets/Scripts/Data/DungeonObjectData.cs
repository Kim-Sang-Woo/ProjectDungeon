// ============================================================
// DungeonObjectData.cs — 던전 오브젝트 데이터 (ScriptableObject)
// 위치: Assets/Scripts/Data/DungeonObjectData.cs
// ============================================================
// [개요]
//   던전 안에 배치되는 상호작용 오브젝트(보물 상자 등)의
//   정의 데이터를 담는 ScriptableObject.
//
// [에셋 생성]
//   Project 창 우클릭
//   → Create → Dungeon → Object Data
//   → Assets/Data/Objects/ 폴더에 저장 권장
//
// [Inspector 설정 예시 — 보물 상자]
//   objectId   : treasure_chest_basic
//   objectType : TREASURE_CHEST
//   displayName: 낡은 보물 상자
//   description: 오래된 나무 상자다. 무언가 들어있을 것 같다.
//   sprite     : (보물상자 스프라이트 할당)
//   isOneTime  : true  (한 번 열면 사라짐)
// ============================================================
using UnityEngine;

[CreateAssetMenu(fileName = "Object_New", menuName = "Dungeon/Object Data")]
public class DungeonObjectData : ScriptableObject
{
    [Header("오브젝트 식별")]
    [Tooltip("고유 ID (예: treasure_chest_basic, treasure_chest_rare)")]
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

    /// <summary>
    /// 오브젝트 타입에 따라 인터렉션 UI에 표시할 액션 텍스트를 반환한다.
    /// 새 타입 추가 시 여기에 case를 추가한다.
    /// </summary>
    public string GetActionLabel()
    {
        switch (objectType)
        {
            case DungeonObjectType.TREASURE_CHEST: return "열기";
            case DungeonObjectType.STAIRS_DOWN:    return "내려가기";
            case DungeonObjectType.STAIRS_UP:      return "올라가기";
            default:                               return "상호작용";
        }
    }
}

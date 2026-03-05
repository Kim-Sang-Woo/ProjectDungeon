// ============================================================
// DungeonFloorData.cs — 층 전체 설정 (ScriptableObject)
// 기획서 Ch.0.1 참조
// ============================================================
using UnityEngine;

/// <summary>
/// 던전 한 층의 전체 생성 파라미터를 정의하는 ScriptableObject.
/// Unity 에디터에서 Assets/Data/Dungeons/ 폴더에 에셋으로 생성하여 사용한다.
/// </summary>
[CreateAssetMenu(fileName = "Floor_01", menuName = "Dungeon/Floor Data")]
public class DungeonFloorData : ScriptableObject
{
    [Header("층 기본 정보")]
    [Tooltip("층 번호 (1부터 시작)")]
    public int floorIndex = 1;

    [Header("방 생성 설정")]
    [Tooltip("최소 방 수")]
    public int roomCountMin = 6;
    [Tooltip("최대 방 수")]
    public int roomCountMax = 10;

    [Header("맵 크기")]
    [Tooltip("맵 가로 타일 수")]
    public int mapWidth = 60;
    [Tooltip("맵 세로 타일 수")]
    public int mapHeight = 45;

    [Header("이벤트 설정")]
    [Tooltip("이벤트 타일 밀도 (바닥 타일 대비 비율, 0.0~1.0)")]
    [Range(0f, 1f)]
    public float eventDensity = 0.15f;

    [Header("연결 설정")]
    [Tooltip("MST 이후 루프 간선 추가 확률 (0.0~1.0)")]
    [Range(0f, 1f)]
    public float loopEdgeProbability = 0.2f;
}

// ============================================================
// EventResult.cs — 결과 ScriptableObject
// 위치: Assets/Scripts/Events/Data/EventResult.cs
// ============================================================
// [개요]
//   선택지 실행 결과 1개를 표현한다.
//   ResultView에 표시되는 설명, 실행할 효과, 후속 선택지를 담는다.
// ============================================================
using UnityEngine;

[CreateAssetMenu(fileName = "Result_New", menuName = "Event/EventResult")]
public class EventResult : ScriptableObject
{
    [Header("식별")]
    public string resultId;

    [Header("결과 설명")]
    [TextArea(2, 6)]
    [Tooltip("ResultView에 이탤릭 표시되는 결과 설명 텍스트")]
    public string resultDesc;

    [Tooltip("결과 전용 이미지. null이면 EventData.image 유지.")]
    public Sprite resultImage;

    [Header("효과")]
    [Tooltip("실행할 효과 배열. 빈 배열 허용.")]
    [SerializeReference]          // 추상 타입 직렬화를 위해 필수
    public EventEffect[] effects;

    [Header("후속 선택지")]
    [Tooltip("결과 화면 선택지. 비어 있으면 닫기 선택지만 자동 추가.")]
    public EventChoice[] nextChoices;
}

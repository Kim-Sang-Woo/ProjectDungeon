// ============================================================
// EventChoice.cs — 선택지 ScriptableObject
// 위치: Assets/Scripts/Events/Data/EventChoice.cs
// ============================================================
// [개요]
//   선택지 1개를 표현한다.
//   choiceType으로 기본/특수(아이템·장착·능력치)/닫기를 구분한다.
//   조건 미충족 시 UI에서 SetActive(false) 처리된다.
// ============================================================
using UnityEngine;

[CreateAssetMenu(fileName = "Choice_New", menuName = "Event/EventChoice")]
public class EventChoice : ScriptableObject
{
    [Header("식별")]
    public string choiceId;

    [Header("선택지 종류")]
    public ChoiceType choiceType = ChoiceType.Default;

    [Header("본문")]
    [TextArea(1, 3)]
    [Tooltip("선택지 본문 텍스트. 예) 자물쇠를 연다.")]
    public string label;

    // ── 기본 선택지 (Default) ────────────────────────────
    [Header("확률 (Default 전용)")]
    [Range(0, 100)]
    [Tooltip("성공 확률 %. 0~49=빨강, 50~99=주황, 100=초록")]
    public int successRate = 100;

    // ── 특수 선택지: 아이템 보유 (SpecialItem) ───────────
    [Header("아이템 조건 (SpecialItem 전용)")]
    [Tooltip("조건 아이템 SO 목록 (OR 조건). 하나라도 보유 시 표시.")]
    public ItemData[] requiredItems;

    // ── 특수 선택지: 장착 슬롯 (SpecialEquip) ────────────
    [Header("장착 슬롯 조건 (SpecialEquip 전용)")]
    [Tooltip("지정 슬롯에 장착 아이템이 있을 때 표시.")]
    public EquipType requiredSlot;

    // ── 특수 선택지: 능력치 (SpecialStat) ────────────────
    [Header("능력치 조건 (SpecialStat 전용)")]
    [Tooltip("해당 스탯이 requiredStatValue 이상일 때 표시.")]
    public StatType  requiredStatType;
    [Tooltip("요구 최소 스탯 수치")]
    public int       requiredStatValue;

    // ── 결과 연결 ─────────────────────────────────────────
    [Header("결과 연결")]
    [Tooltip("성공 결과. null이면 효과 실행 후 팝업 닫기.")]
    public EventResult onSuccess;

    [Tooltip("실패 결과. null이면 실패 시 팝업 닫기.")]
    public EventResult onFailure;

    // ── 직접 효과 (Result 없이 즉시 실행) ─────────────────
    [Header("직접 효과 (onSuccess가 null일 때 실행)")]
    [Tooltip("onSuccess가 null인 경우 여기 지정한 효과를 즉시 실행하고 팝업을 닫는다.\n" +
             "계단 이동 등 ResultView가 필요 없는 경우에 사용.")]
    [SerializeReference]
    public EventEffect[] directEffects;
}

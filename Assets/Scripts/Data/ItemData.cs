// ============================================================
// ItemData.cs — 아이템 데이터 (ScriptableObject)
// 위치: Assets/Scripts/Data/ItemData.cs
// ============================================================
// [에셋 생성]
//   Project 창 우클릭 → Create → Dungeon → Item Data
//   Assets/Data/Items/ 폴더에 저장 권장
//
// [Inspector 설정 예시]
//   itemId    : healing_potion
//   itemName  : 치유의 물약
//   description: 마시면 체력이 회복된다.
//   price     : 50
//   weight    : 0.5
//   isStackable: true
//   maxStack  : 99
// ============================================================
using UnityEngine;

[CreateAssetMenu(fileName = "Item_New", menuName = "Dungeon/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("아이템 식별")]
    [Tooltip("고유 ID (예: healing_potion, iron_sword)")]
    public string itemId;

    [Tooltip("표시 이름")]
    public string itemName;

    [Header("표시 정보")]
    [Tooltip("아이템 설명")]
    [TextArea(2, 4)]
    public string description;

    [Header("수치")]
    [Tooltip("판매 가격 (골드)")]
    [Min(0)]
    public int price;

    [Tooltip("무게 (kg)")]
    [Min(0f)]
    public float weight;

    [Header("스택 설정")]
    [Tooltip("겹치기 가능 여부")]
    public bool isStackable = false;

    [Tooltip("최대 스택 수량 (isStackable = true 일 때만 유효)")]
    [Min(1)]
    public int maxStack = 1;
}

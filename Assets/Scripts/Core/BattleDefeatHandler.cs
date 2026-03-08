using UnityEngine;

/// <summary>
/// 전투 패배 후 복구 처리:
/// 1) 인벤토리/장비 초기화
/// 2) Start Equips 재장착
/// 3) 임시 마을(0층 START=StairUP) 복귀
/// </summary>
public class BattleDefeatHandler : MonoBehaviour
{
    [Header("연동")]
    public Inventory inventory;
    public EquipmentManager equipmentManager;
    public DungeonManager dungeonManager;

    public void HandleDefeat()
    {
        if (inventory == null) inventory = Inventory.Instance;
        if (equipmentManager == null) equipmentManager = EquipmentManager.Instance;
        if (dungeonManager == null) dungeonManager = FindFirstObjectByType<DungeonManager>();

        // 1) 장비/인벤토리 초기화
        equipmentManager?.ClearAllEquipped(false);
        inventory?.ClearAll();

        // 2) 시작 장비 재착용
        equipmentManager?.ReapplyStartEquips();

        // 2-1) 런타임 스탯 완전 회복 (HP/Mana/Shield/Dodge)
        CharacterStats stats = CharacterStats.Instance;
        stats?.InitRuntimeValues();

        // 3) 0층 시작 지점 복귀 (임시 마을)
        dungeonManager?.ReturnToTownSpawn();

        Debug.Log("[BattleDefeatHandler] 패배 복구 완료: 인벤토리 초기화 + 시작장비 재착용 + 스탯 초기화 + 0층 복귀");
    }
}

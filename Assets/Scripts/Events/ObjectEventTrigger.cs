// ============================================================
// ObjectEventTrigger.cs — 오브젝트/이벤트 통합 트리거
// 위치: Assets/Scripts/Events/ObjectEventTrigger.cs
// ============================================================
// [개요]
//   기존 EventTriggerSystem + InteractionSystem을 완전 대체한다.
//   플레이어가 타일에 진입하면 두 가지를 순서대로 확인한다.
//
//   우선순위:
//     1. tile.eventSO  — 이벤트 SO가 있으면 EventPopup 열기
//     2. tile.HasObject — 오브젝트가 있으면 오브젝트 타입별 처리
//          TREASURE_CHEST → 보물상자 EventData 자동 생성 후 EventPopup 열기
//          STAIRS_DOWN    → 계단 EventData 자동 생성 후 EventPopup 열기
//          STAIRS_UP      → 계단 EventData 자동 생성 후 EventPopup 열기
//
// [제거된 파일]
//   - EventTriggerSystem.cs  (역할 흡수)
//   - InteractionSystem.cs   (역할 흡수)
//   - InteractionUI.cs       (EventPopupUI로 대체)
//
// [씬 배치]
//   기존 EventTriggerSystem, InteractionSystem 오브젝트 제거 후
//   빈 GameObject에 이 컴포넌트 단독 배치
// ============================================================
using System.Collections.Generic;
using UnityEngine;

public class ObjectEventTrigger : MonoBehaviour
{
    [Header("참조")]
    public MovementSystem       movementSystem;
    public EventPopup           eventPopup;
    public DungeonManager       dungeonManager;
    public DungeonObjectSpawner dungeonObjectSpawner;


    // ─────────────────────────────────────────────────────
    private void Start()
    {
        if (movementSystem != null)
            movementSystem.OnTileEntered += OnTileEntered;
        else
            Debug.LogError("[ObjectEventTrigger] MovementSystem 참조 없음!");

        if (dungeonManager != null)
            dungeonManager.OnFloorChanged += OnFloorChanged;
    }

    private void OnDestroy()
    {
        if (movementSystem != null)
            movementSystem.OnTileEntered -= OnTileEntered;
        if (dungeonManager != null)
            dungeonManager.OnFloorChanged -= OnFloorChanged;
    }

    // ─────────────────────────────────────────────────────

    private void OnFloorChanged(int floorIndex)
    {
        // 층 전환 시 열려 있던 팝업 닫기
        eventPopup?.Close();
    }

    private void OnTileEntered(Vector2Int tilePos)
    {
        if (dungeonManager == null) return;

        TileData tile = dungeonManager.GetTile(tilePos.x, tilePos.y);
        if (tile == null) return;

        // ── 우선순위 1: 이벤트 SO ──────────────────────────
        if (tile.eventSO != null)
        {
            if (tile.isEventConsumed && !tile.eventSO.isRepeatable) return;

            tile.isEventConsumed = true;
            movementSystem.LockInput();
            Debug.Log($"[ObjectEventTrigger] 이벤트: {tile.eventSO.eventName} ({tilePos})");
            eventPopup?.Open(tile.eventSO);
            return;
        }

        // ── 우선순위 2: 던전 오브젝트 ─────────────────────
        if (!tile.HasObject) return;

        DungeonObjectData obj = tile.placedObject;

        switch (obj.objectType)
        {
            case DungeonObjectType.TREASURE_CHEST:
                HandleTreasureChest(tilePos, tile, obj);
                break;

            case DungeonObjectType.STAIRS_DOWN:
                HandleStairs(tilePos, obj, isDown: true);
                break;

            case DungeonObjectType.STAIRS_UP:
                HandleStairs(tilePos, obj, isDown: false);
                break;
        }
    }

    // ── 보물 상자 ─────────────────────────────────────────

    private void HandleTreasureChest(Vector2Int tilePos, TileData tile, DungeonObjectData obj)
    {
        movementSystem.LockInput();
        Debug.Log($"[ObjectEventTrigger] 보물 상자: {obj.displayName} ({tilePos})");

        // eventOverride 우선 사용, null이면 팩토리 자동 생성
        EventData data = obj.eventOverride != null
            ? obj.eventOverride
            : EventData.FromTreasureChest(obj, tilePos, tile, dungeonObjectSpawner);
        // desc가 비어 있으면 DungeonObjectData.description 폴백
        ApplyDescFallback(data, obj);
        eventPopup?.Open(data);
    }

    // ── 계단 ──────────────────────────────────────────────

    private void HandleStairs(Vector2Int tilePos, DungeonObjectData obj, bool isDown)
    {
        if (!isDown && dungeonManager.CurrentFloorIndex <= 0)
        {
            Debug.Log("[ObjectEventTrigger] 최상위 층 — 더 올라갈 수 없습니다.");
            return;
        }

        movementSystem.LockInput();
        Debug.Log($"[ObjectEventTrigger] 계단 ({(isDown ? "하행" : "상행")}): {obj.displayName} ({tilePos})");

        // eventOverride 우선 사용, null이면 팩토리 자동 생성
        EventData data = obj.eventOverride != null
            ? obj.eventOverride
            : EventData.FromStairs(obj, isDown, dungeonManager, movementSystem);
        // desc가 비어 있으면 DungeonObjectData.description 폴백
        ApplyDescFallback(data, obj);
        // ── 계단 전용 이미지 하드코딩 ────────────────────────
        // obj.sprite(100x100 아이콘)와 별도로 팝업용 고해상도 이미지(480x200)를 사용한다.
        // 이미지 경로: Assets/Resources/Sprites/Object_StairsDown.png
        //              Assets/Resources/Sprites/Object_StairsUp.png
        // * Resources 폴더에 없으면 obj.sprite(아이콘)를 그대로 사용한다.
        // * 계단 외 오브젝트에는 적용하지 않는다 (보물상자 등은 별도 처리 예정).
        string spriteName = isDown ? "Object_StairsDown" : "Object_StairsUp";
        Sprite loadedSprite = Resources.Load<Sprite>($"Sprites/{spriteName}");
        if (loadedSprite != null) data.image = loadedSprite;
        // ─────────────────────────────────────────────────
        eventPopup?.Open(data);
    }

    /// <summary>
    /// EventData.desc가 비어 있으면 DungeonObjectData.description을 대신 사용한다.
    /// eventOverride SO 원본을 수정하지 않도록 팩토리 생성본과 구분 없이 런타임에서만 적용.
    /// </summary>
    private void ApplyDescFallback(EventData data, DungeonObjectData obj)
    {
        if (data == null) return;
        if (string.IsNullOrEmpty(data.desc) && !string.IsNullOrEmpty(obj.description))
            data.desc = obj.description;
    }
}

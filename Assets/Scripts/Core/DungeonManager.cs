// ============================================================
// DungeonManager.cs — 런타임 던전 데이터 관리자
// 위치: Assets/Scripts/Core/DungeonManager.cs
// ============================================================
// [신규 스크립트]
//   DungeonGenerator에서 분리된 런타임 데이터 소유/API 계층.
//   다른 시스템(MovementSystem, EventTriggerSystem, FogOfWar 등)은
//   DungeonGenerator가 아닌 DungeonManager를 참조한다.
//
//   Script Execution Order: -50 (DungeonGenerator와 동일 또는 직후)
// ============================================================
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 던전 런타임 데이터를 소유하고 외부 시스템에 API를 제공하는 관리자.
/// DungeonGenerator가 생성한 데이터를 받아 저장하며,
/// 다른 시스템은 이 클래스를 통해 맵 데이터에 접근한다.
/// </summary>
public class DungeonManager : MonoBehaviour
{
    [Header("참조")]
    [Tooltip("던전 생성기")]
    public DungeonGenerator dungeonGenerator;

    [Tooltip("던전 렌더러")]
    public DungeonRenderer dungeonRenderer;

    // ─── 런타임 데이터 ───
    public TileData[,] Grid { get; private set; }
    public List<RoomData> Rooms { get; private set; }
    public Vector2Int StartPosition { get; private set; }
    public Vector2Int ExitPosition { get; private set; }
    public DungeonFloorData FloorData => dungeonGenerator != null ? dungeonGenerator.floorData : null;

    public int MapWidth => FloorData != null ? FloorData.mapWidth : 0;
    public int MapHeight => FloorData != null ? FloorData.mapHeight : 0;

    // ─── 초기화 ───

    private void Awake()
    {
        // Inspector 참조 누락 시 같은 GameObject에서 자동 검색
        if (dungeonGenerator == null)
            dungeonGenerator = GetComponent<DungeonGenerator>();
        if (dungeonRenderer == null)
            dungeonRenderer = GetComponent<DungeonRenderer>();
    }

    private void Start()
    {
        GenerateDungeon();
    }

    /// <summary>
    /// 던전을 생성하고 데이터를 저장한 뒤 렌더링한다.
    /// </summary>
    public void GenerateDungeon()
    {
        if (dungeonGenerator == null)
        {
            Debug.LogError("[DungeonManager] DungeonGenerator 참조가 설정되지 않았습니다!");
            return;
        }

        bool success = dungeonGenerator.GenerateAndReturn(
            out TileData[,] grid,
            out List<RoomData> rooms,
            out Vector2Int startPos,
            out Vector2Int exitPos
        );

        if (!success)
        {
            Debug.LogError("[DungeonManager] 던전 생성 실패!");
            return;
        }

        Grid = grid;
        Rooms = rooms;
        StartPosition = startPos;
        ExitPosition = exitPos;

        Debug.Log($"[DungeonManager] 던전 데이터 로드 완료. 방 수: {Rooms.Count}, 시작: {StartPosition}, 출구: {ExitPosition}");

        // 렌더링
        if (dungeonRenderer != null)
        {
            dungeonRenderer.RenderDungeon(this);
        }
        else
        {
            Debug.LogError("[DungeonManager] DungeonRenderer 참조가 없습니다! 타일맵이 렌더링되지 않습니다.");
        }
    }

    // ─── 공용 API ───

    /// <summary>특정 좌표가 맵 범위 내인지 확인</summary>
    public bool IsInBounds(int x, int y)
    {
        return x >= 0 && x < MapWidth && y >= 0 && y < MapHeight;
    }

    /// <summary>특정 좌표가 이동 가능한 타일인지 확인</summary>
    public bool IsWalkable(int x, int y)
    {
        if (!IsInBounds(x, y)) return false;
        return Grid[x, y].IsWalkable;
    }

    /// <summary>
    /// 특정 좌표의 TileData를 반환.
    /// TileData는 class이므로 반환된 참조를 직접 수정해도 원본에 반영된다.
    /// </summary>
    public TileData GetTile(int x, int y)
    {
        return Grid[x, y];
    }

    /// <summary>
    /// 특정 좌표의 TileData를 교체한다 (전체 교체가 필요한 경우).
    /// </summary>
    public void SetTile(int x, int y, TileData data)
    {
        Grid[x, y] = data;
    }
}

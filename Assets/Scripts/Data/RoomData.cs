// ============================================================
// RoomData.cs — 방 인스턴스 데이터 (런타임 생성)
// 기획서 Ch.0.2 참조
// ============================================================
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 던전 생성 시 런타임으로 생성되는 방(Room) 인스턴스 데이터.
/// ScriptableObject가 아니며 DungeonGenerator가 생성하여 리스트로 관리한다.
/// </summary>
[System.Serializable]
public class RoomData
{
    /// <summary>방 고유 인덱스</summary>
    public int id;

    /// <summary>방의 위치 및 크기 (x, y, width, height)</summary>
    public RectInt bounds;

    /// <summary>방 중심 좌표</summary>
    public Vector2Int center;

    /// <summary>연결된 방 ID 목록</summary>
    public List<int> connectedRoomIds = new List<int>();

    /// <summary>방 유형 — NORMAL / START / EXIT</summary>
    public RoomType roomType = RoomType.NORMAL;

    /// <summary>플레이어 방문 여부</summary>
    public bool isVisited = false;

    /// <summary>
    /// 생성자
    /// </summary>
    public RoomData(int id, RectInt bounds)
    {
        this.id = id;
        this.bounds = bounds;
        this.center = new Vector2Int(
            bounds.x + bounds.width / 2,
            bounds.y + bounds.height / 2
        );
    }
}

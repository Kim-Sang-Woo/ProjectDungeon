// ============================================================
// DungeonObjectSpawner.cs — 던전 오브젝트 씬 배치 시스템
// 위치: Assets/Scripts/Objects/DungeonObjectSpawner.cs
// ============================================================
// [개요]
//   TileData.placedObject가 설정된 타일에 스프라이트 오브젝트를 생성한다.
//   층 변경 시 기존 오브젝트를 전부 제거하고 새 층의 오브젝트를 생성한다.
//
// [씬 배치]
//   Hierarchy: DungeonManager 하위 빈 GameObject "DungeonObjectSpawner"
//   컴포넌트: DungeonObjectSpawner.cs 부착
//
// [Inspector 연결]
//   dungeonManager → DungeonManager
//
// [렌더링 구조]
//   각 오브젝트 타일마다 빈 GameObject를 생성하고
//   SpriteRenderer를 부착하여 타일 중앙에 스프라이트를 표시한다.
//   sortingOrder = 5 (타일맵(0) 위, FogOfWar(10) 아래)
// ============================================================
using System.Collections.Generic;
using UnityEngine;

public class DungeonObjectSpawner : MonoBehaviour
{
    [Header("참조")]
    public DungeonManager dungeonManager;

    [Header("렌더링")]
    [Tooltip("오브젝트 스프라이트 Sorting Order (타일맵보다 위, 안개보다 아래)")]
    public int sortingOrder = 5;

    // 현재 씬에 생성된 오브젝트 GameObject 목록 (타일 좌표 → GameObject)
    private Dictionary<Vector2Int, GameObject> spawnedObjects
        = new Dictionary<Vector2Int, GameObject>();

    private void Start()
    {
        if (dungeonManager == null)
        {
            Debug.LogError("[DungeonObjectSpawner] DungeonManager 참조 없음!");
            return;
        }

        dungeonManager.OnFloorChanged += OnFloorChanged;
        SpawnObjects();
    }

    private void OnDestroy()
    {
        if (dungeonManager != null)
            dungeonManager.OnFloorChanged -= OnFloorChanged;
    }

    private void OnFloorChanged(int floorIndex)
    {
        ClearObjects();
        SpawnObjects();
    }

    // ─── 오브젝트 생성 ───

    /// <summary>
    /// 현재 Grid를 순회하여 placedObject가 있는 타일에 스프라이트를 생성한다.
    /// </summary>
    public void SpawnObjects()
    {
        if (dungeonManager == null || dungeonManager.Grid == null) return;

        int w = dungeonManager.MapWidth;
        int h = dungeonManager.MapHeight;

        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                TileData tile = dungeonManager.Grid[x, y];
                if (!tile.HasObject) continue;

                SpawnAt(new Vector2Int(x, y), tile.placedObject);
            }
        }

        Debug.Log($"[DungeonObjectSpawner] {spawnedObjects.Count}개 오브젝트 배치 완료.");
    }

    /// <summary>
    /// 지정 타일 좌표에 오브젝트 스프라이트를 생성한다.
    /// </summary>
    private void SpawnAt(Vector2Int tilePos, DungeonObjectData data)
    {
        if (data.sprite == null)
        {
            Debug.LogWarning($"[DungeonObjectSpawner] {data.objectId}의 스프라이트가 없습니다. 건너뜀.");
            return;
        }

        // 타일 중앙 월드 좌표 (TileToWorld와 동일)
        Vector3 worldPos = new Vector3(tilePos.x + 0.5f, tilePos.y + 0.5f, 0f);

        GameObject go = new GameObject($"Obj_{data.objectId}_{tilePos.x}_{tilePos.y}");
        go.transform.SetParent(transform);
        go.transform.position = worldPos;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite       = data.sprite;
        sr.sortingOrder = sortingOrder;

        // 픽셀 단위 설정에 따라 스케일 조정
        // Unity 기본 pixelsPerUnit=100이면 1타일에 딱 맞음
        // sprite.pixelsPerUnit과 data.pixelsPerUnit이 다를 때 보정
        float scale = 100f / data.pixelsPerUnit;
        go.transform.localScale = Vector3.one * scale;

        spawnedObjects[tilePos] = go;
    }

    // ─── 오브젝트 제거 ───

    /// <summary>
    /// 특정 타일의 오브젝트 스프라이트를 제거한다.
    /// isOneTime 오브젝트 상호작용 완료 후 호출된다.
    /// </summary>
    public void RemoveAt(Vector2Int tilePos)
    {
        if (!spawnedObjects.TryGetValue(tilePos, out GameObject go)) return;

        Destroy(go);
        spawnedObjects.Remove(tilePos);
    }

    /// <summary>
    /// 현재 씬의 모든 오브젝트를 제거한다.
    /// 층 변경 시 호출된다.
    /// </summary>
    public void ClearObjects()
    {
        foreach (var go in spawnedObjects.Values)
            if (go != null) Destroy(go);

        spawnedObjects.Clear();
    }
}

/*
 * 맵 매니저 (MapManager)
 *
 * 역할:
 * 1. 현재 활성 맵 (IMap) 관리
 * 2. 맵 컴포넌트 (BOMB, HARVEST 등) 관리
 * 3. 맵 관련 이벤트 및 상호작용 처리
 * 4. Managers.Map으로 전역 접근
 *
 * 접근 예시:
 * - Managers.Map.CurrentMap.SetBlockOwner(...)
 * - Managers.Map.Components.Register(...)
 */

using System;
using UnityEngine;

public class MapManager
{
    /// <summary>
    /// 현재 활성 맵 (IMap 구현체)
    /// </summary>
    public IMap CurrentMap { get; private set; }

    /// <summary>
    /// 맵 컴포넌트 관리자 (BOMB, HARVEST 등)
    /// </summary>
    public MapComponentManager Components { get; private set; }

    // 기존 필드 (하위 호환성)
    public GameObject Map { get; private set; }
    public string MapName { get; private set; }
    public Grid CellGrid { get; private set; }

    public MapManager()
    {
        Components = new MapComponentManager(null); // 초기에는 맵 없이 생성
        Debug.Log("<color=magenta>[MapManager]</color> 생성됨");
    }

    /// <summary>
    /// 현재 맵 설정 (씬 로드 시 호출)
    /// </summary>
    public void SetCurrentMap(IMap map)
    {
        CurrentMap = map;

        // ComponentManager에 맵 참조 업데이트
        Components = new MapComponentManager(map);

        Debug.Log($"<color=magenta>[MapManager]</color> CurrentMap 설정: {map?.GetType().Name ?? "null"}");
    }

    /// <summary>
    /// 맵 해제 (씬 언로드 시 호출)
    /// </summary>
    public void ClearCurrentMap()
    {
        Components?.Clear();
        CurrentMap = null;

        Debug.Log("<color=magenta>[MapManager]</color> CurrentMap 해제됨");
    }

    #region 편의 메서드 (CurrentMap 위임)

    /// <summary>
    /// 블록 소유권 변경 (CurrentMap 위임)
    /// </summary>
    public bool SetBlockOwner(GameObject block, int playerID, Color color)
    {
        if (CurrentMap == null)
        {
            Debug.LogWarning("[MapManager] CurrentMap이 설정되지 않음");
            return false;
        }
        return CurrentMap.SetBlockOwner(block, playerID, color);
    }

    /// <summary>
    /// 블록 소유자 조회 (CurrentMap 위임)
    /// </summary>
    public int GetBlockOwner(GameObject block)
    {
        return CurrentMap?.GetBlockOwner(block) ?? -1;
    }

    /// <summary>
    /// 플레이어 색상 조회 (CurrentMap 위임)
    /// </summary>
    public Color GetPlayerColor(int playerID)
    {
        return CurrentMap?.GetPlayerColor(playerID) ?? Color.white;
    }

    /// <summary>
    /// 플레이어 블록 수 (CurrentMap 위임)
    /// </summary>
    public int GetBlockCountByPlayer(int playerID)
    {
        return CurrentMap?.GetBlockCountByPlayer(playerID) ?? 0;
    }

    #endregion
}

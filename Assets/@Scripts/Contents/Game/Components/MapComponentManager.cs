using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 맵 컴포넌트 관리자 (순수 C# 클래스)
/// IMap의 하위로 계층적으로 관리됨
///
/// 접근 방법:
/// - IsometricGridGenerator.Instance.Components.Register(...)
/// - map.Components.GetPlayerComponents(playerID)
/// </summary>
public class MapComponentManager
{
    private readonly IMap _map;
    private readonly List<IMapComponent> _components = new List<IMapComponent>();
    private readonly Dictionary<int, List<IMapComponent>> _playerComponents = new Dictionary<int, List<IMapComponent>>();

    /// <summary>
    /// 생성자 - IMap에서 생성
    /// </summary>
    public MapComponentManager(IMap map)
    {
        _map = map;
        Debug.Log("<color=cyan>[MapComponentManager] 초기화 완료</color>");
    }

    /// <summary>
    /// 매 프레임 호출 (IMap의 Update에서 호출)
    /// </summary>
    public void Tick(float deltaTime)
    {
        foreach (var component in _components)
        {
            if (component.IsActive)
            {
                component.OnTick(deltaTime);
            }
        }
    }

    #region 컴포넌트 등록/해제

    /// <summary>
    /// 컴포넌트 등록
    /// </summary>
    public void Register(IMapComponent component, int ownerPlayerID = -1)
    {
        if (component == null || _components.Contains(component))
            return;

        component.Initialize(_map, ownerPlayerID);
        _components.Add(component);

        // 플레이어별 목록에 추가
        if (!_playerComponents.ContainsKey(ownerPlayerID))
        {
            _playerComponents[ownerPlayerID] = new List<IMapComponent>();
        }
        _playerComponents[ownerPlayerID].Add(component);

        Debug.Log($"<color=cyan>[MapComponentManager] 등록: {component.DisplayName} (Player {ownerPlayerID})</color>");
    }

    /// <summary>
    /// 컴포넌트 해제
    /// </summary>
    public void Unregister(IMapComponent component)
    {
        if (component == null || !_components.Contains(component))
            return;

        component.Deactivate();
        _components.Remove(component);

        int ownerID = component.OwnerPlayerID;
        if (_playerComponents.ContainsKey(ownerID))
        {
            _playerComponents[ownerID].Remove(component);
        }

        Debug.Log($"<color=yellow>[MapComponentManager] 해제: {component.DisplayName}</color>");
    }

    /// <summary>
    /// 모든 컴포넌트 해제
    /// </summary>
    public void Clear()
    {
        foreach (var component in _components)
        {
            component.Deactivate();
        }
        _components.Clear();
        _playerComponents.Clear();
    }

    #endregion

    #region 컴포넌트 조회

    /// <summary>
    /// ID로 컴포넌트 찾기
    /// </summary>
    public IMapComponent GetByID(string componentID)
    {
        return _components.Find(c => c.ComponentID == componentID);
    }

    /// <summary>
    /// 플레이어의 특정 컴포넌트 찾기
    /// </summary>
    public IMapComponent GetByID(string componentID, int playerID)
    {
        return _components.Find(c => c.ComponentID == componentID && c.OwnerPlayerID == playerID);
    }

    /// <summary>
    /// 플레이어의 모든 컴포넌트
    /// </summary>
    public List<IMapComponent> GetPlayerComponents(int playerID)
    {
        if (_playerComponents.TryGetValue(playerID, out var list))
        {
            return new List<IMapComponent>(list);
        }
        return new List<IMapComponent>();
    }

    /// <summary>
    /// 모든 컴포넌트
    /// </summary>
    public List<IMapComponent> GetAll()
    {
        return new List<IMapComponent>(_components);
    }

    /// <summary>
    /// 컴포넌트 수
    /// </summary>
    public int Count => _components.Count;

    #endregion

    #region 이벤트 전파

    /// <summary>
    /// 블록 점령 이벤트 전파
    /// </summary>
    public void NotifyBlockCaptured(GameObject block, int oldOwnerID, int newOwnerID)
    {
        foreach (var component in _components)
        {
            if (component.IsActive)
            {
                component.OnBlockCaptured(block, oldOwnerID, newOwnerID);
            }
        }
    }

    #endregion

    #region 컴포넌트 사용

    /// <summary>
    /// 플레이어가 컴포넌트 사용
    /// </summary>
    public bool Use(int playerID, string componentID)
    {
        var component = GetByID(componentID, playerID);

        if (component != null && component.CanUse)
        {
            component.Use();
            Debug.Log($"<color=green>[MapComponentManager] 사용: {component.DisplayName} (Player {playerID})</color>");
            return true;
        }

        return false;
    }

    #endregion
}

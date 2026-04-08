using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

/// <summary>
/// 총알 풀링 시스템 (MonoBehaviour 기반)
/// INetworkPrefabInstanceHandler를 구현하여 Netcode와 통합
///
/// ✅ NetworkBehaviour가 아님 — Spawn 불필요, NetworkManager 상태는 전역으로 확인
/// </summary>
public class NetworkBulletPool : MonoBehaviour, INetworkPrefabInstanceHandler
{
    #region Settings
    [Header("풀링 설정")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private int initialPoolSize = 500;
    [SerializeField] private int maxPoolSize = 3000;
    [SerializeField] private Transform poolRoot;
    #endregion

    #region Pool State
    private Queue<NetworkObject> _availablePool = new Queue<NetworkObject>();
    private HashSet<NetworkObject> _activeObjects = new HashSet<NetworkObject>();
    private bool _isInitialized = false;
    private bool _handlerRegistered = false;
    #endregion

    #region Singleton
    public static NetworkBulletPool Instance { get; private set; }
    #endregion

    #region Properties
    public int AvailableCount => _availablePool.Count;
    public int ActiveCount => _activeObjects.Count;
    public int TotalCount => AvailableCount + ActiveCount;
    public GameObject BulletPrefab => bulletPrefab;

    public void SetBulletPrefab(GameObject prefab)
    {
        if (prefab == null)
        {
            GameLogger.Error("NetworkBulletPool", "SetBulletPrefab: prefab이 null입니다!");
            return;
        }
        if (prefab.GetComponent<NetworkObject>() == null)
        {
            GameLogger.Error("NetworkBulletPool", "SetBulletPrefab: prefab에 NetworkObject가 없습니다!");
            return;
        }
        bulletPrefab = prefab;
        GameLogger.Success("NetworkBulletPool", $"BulletPrefab 설정됨: {prefab.name}");
    }
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            GameLogger.Warning("NetworkBulletPool", "중복 인스턴스 감지 - 파괴됨");
            Destroy(gameObject);
            return;
        }

        if (poolRoot == null)
        {
            var rootGO = new GameObject("@BulletPool");
            poolRoot = rootGO.transform;
        }
    }

    private void Update()
    {
        // NetworkManager가 준비되면 PrefabHandler 등록 + 풀 초기화
        if (!_handlerRegistered && NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            RegisterHandler();
        }
    }

    private void OnDestroy()
    {
        UnregisterHandler();
        ClearPool();

        if (Instance == this)
            Instance = null;
    }
    #endregion

    #region Handler Registration
    private void RegisterHandler()
    {
        if (_handlerRegistered) return;
        if (bulletPrefab == null) return;

        NetworkManager.Singleton.PrefabHandler.AddHandler(bulletPrefab, this);
        _handlerRegistered = true;
        GameLogger.Success("NetworkBulletPool", $"PrefabHandler 등록 완료: {bulletPrefab.name}");

        InitializePool();
    }

    private void UnregisterHandler()
    {
        if (!_handlerRegistered) return;
        if (bulletPrefab != null && NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.PrefabHandler.RemoveHandler(bulletPrefab);
        }
        _handlerRegistered = false;
    }
    #endregion

    #region Pool Initialization
    private void InitializePool()
    {
        if (_isInitialized) return;
        if (bulletPrefab == null) return;

        GameLogger.Info("NetworkBulletPool", $"풀 초기화 시작 (크기: {initialPoolSize})");

        for (int i = 0; i < initialPoolSize; i++)
        {
            CreatePooledObject();
        }

        _isInitialized = true;
        GameLogger.Success("NetworkBulletPool", $"풀 초기화 완료! (생성: {_availablePool.Count}개)");
    }

    private NetworkObject CreatePooledObject()
    {
        GameObject go = Instantiate(bulletPrefab, poolRoot);
        go.name = $"{bulletPrefab.name}_Pooled";
        go.SetActive(false);

        NetworkObject netObj = go.GetComponent<NetworkObject>();
        if (netObj == null)
        {
            GameLogger.Error("NetworkBulletPool", "프리팹에 NetworkObject가 없습니다!");
            Destroy(go);
            return null;
        }

        _availablePool.Enqueue(netObj);
        return netObj;
    }

    private void ClearPool()
    {
        while (_availablePool.Count > 0)
        {
            var netObj = _availablePool.Dequeue();
            if (netObj != null) Destroy(netObj.gameObject);
        }
        _activeObjects.Clear();
        _isInitialized = false;
    }
    #endregion

    #region INetworkPrefabInstanceHandler
    public NetworkObject Instantiate(ulong ownerClientId, Vector3 position, Quaternion rotation)
    {
        NetworkObject netObj = GetFromPool();
        if (netObj != null)
        {
            netObj.transform.position = position;
            netObj.transform.rotation = rotation;
            netObj.gameObject.SetActive(true);
        }
        return netObj;
    }

    /// <summary>
    /// Despawn 시 Netcode가 호출하는 콜백 — 풀로 반환
    /// </summary>
    public void Destroy(NetworkObject networkObject)
    {
        if (networkObject == null) return;
        networkObject.gameObject.SetActive(false);
        _activeObjects.Remove(networkObject);

        if (!_availablePool.Contains(networkObject))
        {
            _availablePool.Enqueue(networkObject);
        }
    }
    #endregion

    #region Public API
    public NetworkObject GetNetworkObject(Vector3 position, Quaternion rotation)
    {
        NetworkObject netObj = GetFromPool();
        if (netObj != null)
        {
            var bullet = netObj.GetComponent<CannonBullet>();
            if (bullet != null)
                bullet.ResetForReuse();

            netObj.transform.position = position;
            netObj.transform.rotation = rotation;
            netObj.gameObject.SetActive(true);
            _activeObjects.Add(netObj);
        }
        return netObj;
    }

    /// <summary>
    /// 총알 Despawn + 풀 반환 (Server 전용)
    /// Despawn(false) → Netcode Destroy 콜백이 자동으로 풀 반환 처리
    /// </summary>
    public void DespawnAndReturn(NetworkObject netObj)
    {
        if (netObj == null) return;
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;

        _activeObjects.Remove(netObj);

        // Despawn → Netcode가 INetworkPrefabInstanceHandler.Destroy 콜백 호출 → 풀 반환
        // Despawn 실행
        if (netObj.IsSpawned)
        {
            netObj.Despawn(false);
        }

        // ✅ 서버에서는 INetworkPrefabInstanceHandler.Destroy 콜백이 안 불림
        // 직접 비활성화 + 풀 반환 처리
        netObj.gameObject.SetActive(false);
        if (!_availablePool.Contains(netObj))
        {
            _availablePool.Enqueue(netObj);
        }
    }
    #endregion

    #region Private
    private NetworkObject GetFromPool()
    {
        if (_availablePool.Count == 0)
        {
            if (TotalCount < maxPoolSize)
            {
                GameLogger.Warning("NetworkBulletPool", $"풀 확장! (현재: {TotalCount}, 최대: {maxPoolSize})");
                return CreatePooledObject();
            }
            GameLogger.Error("NetworkBulletPool", $"풀 최대 크기 초과! ({maxPoolSize})");
            return null;
        }
        return _availablePool.Dequeue();
    }
    #endregion
}

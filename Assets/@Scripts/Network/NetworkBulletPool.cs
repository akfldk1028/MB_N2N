using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

/// <summary>
/// NetworkObject 총알 풀링 시스템
/// INetworkPrefabInstanceHandler를 구현하여 Netcode와 통합
///
/// 사용법:
/// 1. 씬에 빈 오브젝트 생성 후 이 컴포넌트 추가
/// 2. BulletPrefab에 총알 프리팹 할당
/// 3. InitialPoolSize로 초기 풀 크기 설정
///
/// 참고: Unity Netcode for GameObjects 공식 문서
/// https://docs-multiplayer.unity3d.com/netcode/current/advanced-topics/object-pooling/
/// </summary>
public class NetworkBulletPool : NetworkBehaviour, INetworkPrefabInstanceHandler
{
    #region Settings
    [Header("풀링 설정")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private int initialPoolSize = 500;      // ✅ 초기 풀 크기 (500개 미리 생성)
    [SerializeField] private int maxPoolSize = 3000;         // ✅ 최대 풀 크기 (대량 발사 대비)
    [SerializeField] private Transform poolRoot;             // 풀 오브젝트 부모
    #endregion

    #region Pool State
    private Queue<NetworkObject> _availablePool = new Queue<NetworkObject>();
    private HashSet<NetworkObject> _activeObjects = new HashSet<NetworkObject>();
    private bool _isInitialized = false;
    #endregion

    #region Singleton
    public static NetworkBulletPool Instance { get; private set; }
    #endregion

    #region Properties
    public int AvailableCount => _availablePool.Count;
    public int ActiveCount => _activeObjects.Count;
    public int TotalCount => AvailableCount + ActiveCount;
    public GameObject BulletPrefab => bulletPrefab;

    /// <summary>
    /// 동적 생성 시 bulletPrefab 설정 (씬에 배치하지 않고 코드로 생성할 때 사용)
    /// </summary>
    public void SetBulletPrefab(GameObject prefab)
    {
        if (prefab == null)
        {
            GameLogger.Error("NetworkBulletPool", "SetBulletPrefab: prefab이 null입니다!");
            return;
        }

        // NetworkObject 확인
        var netObj = prefab.GetComponent<NetworkObject>();
        if (netObj == null)
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

        // 풀 루트 생성
        if (poolRoot == null)
        {
            var rootGO = new GameObject("@BulletPool");
            poolRoot = rootGO.transform;
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Prefab Handler에 등록
        if (bulletPrefab != null)
        {
            NetworkManager.Singleton.PrefabHandler.AddHandler(bulletPrefab, this);
            GameLogger.Success("NetworkBulletPool", $"PrefabHandler 등록 완료: {bulletPrefab.name}");
        }
        else
        {
            GameLogger.Error("NetworkBulletPool", "bulletPrefab이 null입니다!");
        }

        // 초기 풀 생성 (Server/Host/Client 모두)
        InitializePool();
    }

    public override void OnNetworkDespawn()
    {
        // Prefab Handler에서 해제
        if (bulletPrefab != null && NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.PrefabHandler.RemoveHandler(bulletPrefab);
        }

        // 모든 활성 오브젝트 Despawn
        foreach (var netObj in _activeObjects)
        {
            if (netObj != null && netObj.IsSpawned)
            {
                netObj.Despawn();
            }
        }

        // 풀 정리
        ClearPool();

        base.OnNetworkDespawn();
    }

    public override void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
        base.OnDestroy();
    }
    #endregion

    #region Pool Initialization
    /// <summary>
    /// 초기 풀 생성
    /// </summary>
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

    /// <summary>
    /// 새 풀 오브젝트 생성
    /// </summary>
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

    /// <summary>
    /// 풀 정리
    /// </summary>
    private void ClearPool()
    {
        while (_availablePool.Count > 0)
        {
            var netObj = _availablePool.Dequeue();
            if (netObj != null)
            {
                Destroy(netObj.gameObject);
            }
        }

        _activeObjects.Clear();
        _isInitialized = false;
    }
    #endregion

    #region INetworkPrefabInstanceHandler Implementation
    /// <summary>
    /// Client에서 NetworkObject 생성 시 호출
    /// (Server/Host에서는 호출되지 않음)
    /// </summary>
    public NetworkObject Instantiate(ulong ownerClientId, Vector3 position, Quaternion rotation)
    {
        NetworkObject netObj = GetFromPool();

        if (netObj != null)
        {
            netObj.transform.position = position;
            netObj.transform.rotation = rotation;
            netObj.gameObject.SetActive(true);

            GameLogger.DevLog("NetworkBulletPool", $"[Client] 풀에서 꺼냄: {netObj.name}, pos={position}");
        }

        return netObj;
    }

    /// <summary>
    /// NetworkObject 파괴 시 호출 (Client/Server 모두)
    /// </summary>
    public void Destroy(NetworkObject networkObject)
    {
        ReturnToPool(networkObject);
        GameLogger.DevLog("NetworkBulletPool", $"[Destroy] 풀로 반환: {networkObject?.name}");
    }
    #endregion

    #region Public API (Server에서 사용)
    /// <summary>
    /// [Server] 풀에서 총알 가져오기
    /// Spawn() 호출 전에 사용
    /// </summary>
    public NetworkObject GetNetworkObject(Vector3 position, Quaternion rotation)
    {
        NetworkObject netObj = GetFromPool();

        if (netObj != null)
        {
            netObj.transform.position = position;
            netObj.transform.rotation = rotation;
            netObj.gameObject.SetActive(true);
            _activeObjects.Add(netObj);

            GameLogger.DevLog("NetworkBulletPool", $"[Server] 풀에서 꺼냄: {netObj.name}, 남은 풀: {_availablePool.Count}");
        }

        return netObj;
    }

    /// <summary>
    /// [Server] 총알을 풀로 반환
    /// Despawn() 호출 후 사용
    /// </summary>
    public void ReturnNetworkObject(NetworkObject netObj)
    {
        if (netObj == null) return;

        _activeObjects.Remove(netObj);
        ReturnToPool(netObj);

        GameLogger.DevLog("NetworkBulletPool", $"[Server] 풀로 반환: {netObj.name}, 사용 가능: {_availablePool.Count}");
    }

    /// <summary>
    /// 총알 Despawn 및 풀 반환 (Server 전용)
    /// </summary>
    public void DespawnAndReturn(NetworkObject netObj)
    {
        if (netObj == null) return;
        if (!IsServer) return;

        // ✅ NetworkObject는 reparent 하면 안됨!
        // - Spawned 상태: NetworkObject 부모만 허용 (InvalidParentException)
        // - Despawned 상태: reparent 불가 (SpawnStateException)
        // → SetActive(false)만 사용하고, 위치는 그대로 둠

        // 1. 비활성화
        netObj.gameObject.SetActive(false);

        // 2. 활성 목록에서 제거
        _activeObjects.Remove(netObj);

        // 3. Despawn (Client에 동기화)
        if (netObj.IsSpawned)
        {
            netObj.Despawn(false); // destroy = false (풀링이므로)
        }

        // 4. 풀에 반환 (중복 방지)
        if (!_availablePool.Contains(netObj))
        {
            _availablePool.Enqueue(netObj);
        }

        GameLogger.DevLog("NetworkBulletPool", $"[DespawnAndReturn] 풀 반환 완료: {netObj.name}, 사용 가능: {_availablePool.Count}");
    }
    #endregion

    #region Private Methods
    /// <summary>
    /// 풀에서 오브젝트 가져오기
    /// </summary>
    private NetworkObject GetFromPool()
    {
        // 풀이 비어있으면 새로 생성
        if (_availablePool.Count == 0)
        {
            if (TotalCount < maxPoolSize)
            {
                GameLogger.Warning("NetworkBulletPool", $"풀 확장! (현재: {TotalCount}, 최대: {maxPoolSize})");
                return CreatePooledObject();
            }
            else
            {
                GameLogger.Error("NetworkBulletPool", $"풀 최대 크기 초과! ({maxPoolSize})");
                return null;
            }
        }

        return _availablePool.Dequeue();
    }

    /// <summary>
    /// 오브젝트를 풀로 반환 (INetworkPrefabInstanceHandler.Destroy에서 호출됨)
    /// </summary>
    private void ReturnToPool(NetworkObject netObj)
    {
        if (netObj == null) return;

        // ✅ NetworkObject는 reparent 하면 안됨! SetActive(false)만 사용
        netObj.gameObject.SetActive(false);

        // 이미 풀에 있는지 확인 (중복 방지)
        if (!_availablePool.Contains(netObj))
        {
            _availablePool.Enqueue(netObj);
        }

        _activeObjects.Remove(netObj);
    }
    #endregion

    #region Debug
    /// <summary>
    /// 풀 상태 출력 (디버그용)
    /// </summary>
    [ContextMenu("Print Pool Status")]
    public void PrintPoolStatus()
    {
        GameLogger.Info("NetworkBulletPool",
            $"풀 상태: 사용 가능={AvailableCount}, 활성={ActiveCount}, 총={TotalCount}");
    }
    #endregion
}

using UnityEngine;
using Unity.Netcode;
using System;
using System.Collections;
using System.Collections.Generic;
using MB.Infrastructure.Messages;

/// <summary>
/// 중앙 맵(땅따먹기) 총알 발사 네트워크 컨트롤러
///
/// 역할:
/// 1. 각 플레이어의 점수만큼 총알 발사 (키 입력 시)
/// 2. Server에서 총알 Spawn → Client 동기화
/// 3. 대포 파괴 시 게임 오버 처리
///
/// 사용법:
/// - 씬에 NetworkObject로 배치
/// - 플레이어가 Fire 키 누르면 RequestFireServerRpc 호출
/// </summary>
public class CentralMapBulletController : NetworkBehaviour
{
    #region Settings
    [Header("총알 설정")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float bulletSpeed = 30f;           // ✅ 빠른 속도!
    [SerializeField] private int bulletsPerFrame = 1;           // ✅ 1발씩 발사 (기관총 효과: 다다다다닥!)
    #endregion

    #region NetworkVariables (게임 오버 동기화)
    /// <summary>
    /// 게임 종료 여부
    /// </summary>
    private NetworkVariable<bool> _isGameOver = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    /// <summary>
    /// 승자 플레이어 ID (-1 = 아직 없음)
    /// </summary>
    private NetworkVariable<int> _winnerPlayerId = new NetworkVariable<int>(
        -1,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    #endregion

    #region State
    private Dictionary<int, Cannon> _playerCannons = new Dictionary<int, Cannon>();
    private Dictionary<ulong, bool> _isFiring = new Dictionary<ulong, bool>(); // 발사 중 여부
    private IDisposable _bulletFiredSubscription;
    private IDisposable _mapComponentSubscription;  // 맵 컴포넌트 구독
    #endregion

    #region Properties
    public bool IsGameOver => _isGameOver.Value;
    public int WinnerPlayerId => _winnerPlayerId.Value;
    #endregion

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        InitializeController();
    }

    /// <summary>
    /// 수동 초기화 (이미 Spawn된 NetworkObject에 AddComponent한 경우 호출)
    /// OnNetworkSpawn()이 자동 호출되지 않을 때 사용
    /// </summary>
    public void ManualInitialize()
    {
        // 이미 초기화되었으면 스킵
        if (_bulletFiredSubscription != null)
        {
            GameLogger.Info("CentralMapBulletController", "이미 초기화됨 - ManualInitialize 스킵");
            return;
        }

        GameLogger.Info("CentralMapBulletController", "ManualInitialize 호출됨");
        InitializeController();
    }

    /// <summary>
    /// 실제 초기화 로직 (OnNetworkSpawn, ManualInitialize 공통)
    /// </summary>
    private void InitializeController()
    {
        // 이미 초기화되었으면 스킵
        if (_bulletFiredSubscription != null)
        {
            return;
        }

        // 대포 찾기
        CacheCannons();

        // 대포 파괴 이벤트 구독
        Cannon.OnCannonDestroyed += HandleCannonDestroyed;

        // 게임 오버 변경 콜백
        _isGameOver.OnValueChanged += OnGameOverChanged;

        // ActionBus 구독: Input_CentralMapFire 이벤트 (Enter키)
        _bulletFiredSubscription = Managers.Subscribe(
            ActionId.Input_CentralMapFire,
            OnCentralMapFireInput
        );

        // ActionBus 구독: Input_UseMapComponent 이벤트 (B키=BOMB, H키=HARVEST)
        _mapComponentSubscription = Managers.Subscribe(
            ActionId.Input_UseMapComponent,
            OnMapComponentInput
        );

        // 총알 프리팹 생성 (없으면)
        if (bulletPrefab == null)
        {
            CreateBulletPrefab();
        }

        GameLogger.Success("CentralMapBulletController", $"초기화 완료 - 대포 {_playerCannons.Count}개, ActionBus 구독됨");
    }

    /// <summary>
    /// 총알 Prefab 로드 (원본 프리팹 직접 사용 - Instantiate 금지!)
    ///
    /// ⚠️ 중요: Instantiate()로 런타임 복제본을 만들면 globalObjectIdHash=0이 되어
    ///          CLIENT에서 NetworkObject를 찾을 수 없습니다.
    ///          반드시 원본 프리팹을 직접 참조해야 합니다.
    ///
    /// 📌 Addressables 등록 필요:
    ///    1. Unity Editor에서 Bullet.prefab 선택
    ///    2. Inspector에서 Addressable 체크
    ///    3. Address: "bullet" 또는 "GameScene/Model/Bullet"
    /// </summary>
    private void CreateBulletPrefab()
    {
        GameObject loadedPrefab = null;

        // ✅ 1. Addressables에서 로드 시도 (여러 경로)
        string[] addressPaths = new string[]
        {
            "bullet",                       // 간단한 주소
            "Bullet",                       // 대문자
            "GameScene/Model/Bullet",       // 전체 경로
            "GameScene/Model/Bullet.prefab" // 확장자 포함
        };

        foreach (var path in addressPaths)
        {
            loadedPrefab = Managers.Resource.Load<GameObject>(path);
            if (loadedPrefab != null)
            {
                GameLogger.Info("CentralMapBulletController", $"Bullet 프리팹 로드 성공 (Addressable: {path})");
                break;
            }
        }

        // ✅ 2. Addressables 실패 시 Resources.Load 시도
        if (loadedPrefab == null)
        {
            loadedPrefab = Resources.Load<GameObject>("GameScene/Model/Bullet");
            if (loadedPrefab != null)
            {
                GameLogger.Info("CentralMapBulletController", "Bullet 프리팹 로드 성공 (Resources.Load)");
            }
        }

        // ✅ 3. 프리팹 검증
        if (loadedPrefab != null)
        {
            // 원본 프리팹 직접 참조 (Instantiate 금지!)
            bulletPrefab = loadedPrefab;

            // NetworkObject 확인
            var networkObject = bulletPrefab.GetComponent<NetworkObject>();
            if (networkObject == null)
            {
                GameLogger.Error("CentralMapBulletController", "Bullet 프리팹에 NetworkObject가 없습니다! Unity Editor에서 추가해주세요.");
            }
            else
            {
                GameLogger.Success("CentralMapBulletController", "Bullet 프리팹 로드 완료 (NetworkObject 있음)");
            }

            // CannonBullet 확인
            var cannonBullet = bulletPrefab.GetComponent<CannonBullet>();
            if (cannonBullet == null)
            {
                GameLogger.Warning("CentralMapBulletController", "Bullet 프리팹에 CannonBullet이 없음");
            }
        }
        else
        {
            // ❌ 로드 실패
            GameLogger.Error("CentralMapBulletController",
                "❌ Bullet 프리팹을 찾을 수 없습니다!\n" +
                "   해결 방법:\n" +
                "   1. Unity Editor에서 Assets/@Resources/GameScene/Model/Bullet.prefab 선택\n" +
                "   2. Inspector에서 'Addressable' 체크박스 활성화\n" +
                "   3. Address를 'bullet'으로 설정");
            return;
        }

        // ✅ 4. NetworkManager에 Prefab 등록 (HOST/CLIENT 모두, 중복 방지)
        var networkManager = NetworkManager.Singleton;
        if (networkManager != null && bulletPrefab != null)
        {
            // 이미 등록되어 있는지 확인 (duplicate PrefabIdHash 방지)
            var networkObject = bulletPrefab.GetComponent<NetworkObject>();
            bool alreadyRegistered = false;
            if (networkObject != null)
            {
                foreach (var prefab in networkManager.NetworkConfig.Prefabs.Prefabs)
                {
                    if (prefab.Prefab != null && prefab.Prefab.GetComponent<NetworkObject>()?.PrefabIdHash == networkObject.PrefabIdHash)
                    {
                        alreadyRegistered = true;
                        break;
                    }
                }
            }

            if (!alreadyRegistered)
            {
                try
                {
                    networkManager.AddNetworkPrefab(bulletPrefab);
                    GameLogger.Success("CentralMapBulletController", "총알 Prefab NetworkManager 등록 완료");
                }
                catch (System.Exception e)
                {
                    GameLogger.Info("CentralMapBulletController", $"NetworkPrefab 등록 (이미 등록됨): {e.Message}");
                }
            }
            else
            {
                GameLogger.Info("CentralMapBulletController", "총알 Prefab 이미 NetworkManager에 등록됨 - 스킵");
            }
        }
    }

    /// <summary>
    /// 간단한 구체 메시 생성
    /// </summary>
    private Mesh CreateSphereMesh()
    {
        var tempSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        var mesh = tempSphere.GetComponent<MeshFilter>().sharedMesh;
        Destroy(tempSphere);
        return mesh;
    }

    public override void OnNetworkDespawn()
    {
        Cannon.OnCannonDestroyed -= HandleCannonDestroyed;
        _isGameOver.OnValueChanged -= OnGameOverChanged;

        _bulletFiredSubscription?.Dispose();
        _bulletFiredSubscription = null;

        _mapComponentSubscription?.Dispose();
        _mapComponentSubscription = null;

        base.OnNetworkDespawn();
    }

    /// <summary>
    /// ActionBus에서 Input_CentralMapFire 이벤트 수신 (Enter 키)
    /// </summary>
    private void OnCentralMapFireInput(ActionMessage message)
    {
        if (_isGameOver.Value)
        {
            GameLogger.Warning("CentralMapBulletController", "게임 오버 - 발사 불가");
            return;
        }

        // 로컬 플레이어 ID
        ulong localClientId = NetworkManager.Singleton.LocalClientId;

        GameLogger.Info("CentralMapBulletController",
            $"[Enter키] Player {localClientId} 발사 요청 (블록 개수는 SERVER에서 확인)");

        // ✅ BrickGameMultiplayerSpawner의 ServerRpc 호출 (런타임 AddComponent된 NetworkBehaviour는 RPC 불가)
        var spawner = FindObjectOfType<BrickGameMultiplayerSpawner>();
        if (spawner != null)
        {
            spawner.RequestCentralMapFireServerRpc(localClientId);
        }
        else
        {
            GameLogger.Error("CentralMapBulletController", "BrickGameMultiplayerSpawner를 찾을 수 없습니다!");
        }
    }

    /// <summary>
    /// ActionBus에서 Input_UseMapComponent 이벤트 수신 (B키=BOMB, H키=HARVEST)
    /// </summary>
    private void OnMapComponentInput(ActionMessage message)
    {
        if (_isGameOver.Value)
        {
            GameLogger.Warning("CentralMapBulletController", "게임 오버 - 컴포넌트 사용 불가");
            return;
        }

        // Payload에서 컴포넌트 ID 추출
        if (!message.TryGetPayload<MapComponentPayload>(out var payload))
        {
            GameLogger.Error("CentralMapBulletController", "MapComponentPayload 추출 실패");
            return;
        }

        string componentID = payload.ComponentID;
        ulong localClientId = NetworkManager.Singleton.LocalClientId;
        int playerID = (int)localClientId;

        GameLogger.Info("CentralMapBulletController",
            $"[{componentID.ToUpper()}] Player {playerID} 컴포넌트 사용 요청");

        // ✅ BrickGameMultiplayerSpawner의 ServerRpc 호출
        var spawner = FindObjectOfType<BrickGameMultiplayerSpawner>();
        if (spawner != null)
        {
            spawner.RequestUseMapComponentServerRpc(playerID, componentID);
        }
        else
        {
            GameLogger.Error("CentralMapBulletController", "BrickGameMultiplayerSpawner를 찾을 수 없습니다!");
        }
    }

    /// <summary>
    /// 플레이어의 블록 개수 가져오기
    /// </summary>
    private int GetPlayerBlockCount(int playerId)
    {
        if (IsometricGridGenerator.Instance != null)
        {
            return IsometricGridGenerator.Instance.GetBlockCountByPlayer(playerId);
        }
        return 0;
    }

    /// <summary>
    /// 플레이어의 점수(총알) 가져오기
    /// ✅ 점수 = 총알 개수 규칙
    /// ✅ 멀티플레이어: 플레이어별 BrickGameManager에서 가져오기
    /// </summary>
    private int GetPlayerScore(int playerId)
    {
        // ✅ 멀티플레이어: 플레이어별 BrickGameManager 사용!
        if (MultiplayerUtil.IsMultiplayer())
        {
            var playerGame = Managers.Game?.GetPlayerGame((ulong)playerId);
            if (playerGame != null)
            {
                int score = playerGame.Score;
                GameLogger.Info("CentralMapBulletController", $"[GetPlayerScore] Player {playerId} 점수(멀티): {score}");
                return score;
            }
            GameLogger.Warning("CentralMapBulletController", $"[GetPlayerScore] Player {playerId} BrickGame 없음!");
            return 0;
        }

        // ✅ 싱글플레이어: 전역 BrickGameManager 사용
        if (Managers.Game?.BrickGame != null)
        {
            int score = Managers.Game.BrickGame.Score;
            GameLogger.Info("CentralMapBulletController", $"[GetPlayerScore] Player {playerId} 점수(싱글): {score}");
            return score;
        }

        GameLogger.Warning("CentralMapBulletController", $"[GetPlayerScore] BrickGameManager null - Player {playerId}");
        return 0;
    }

    /// <summary>
    /// 플레이어의 점수 차감 (총알 발사 시)
    /// ✅ 멀티플레이어: 플레이어별 BrickGameManager에서 차감
    /// </summary>
    private void SubtractPlayerScore(int playerId, int amount)
    {
        if (amount <= 0) return;

        // ✅ 멀티플레이어: 플레이어별 BrickGameManager 사용!
        if (MultiplayerUtil.IsMultiplayer())
        {
            var playerGame = Managers.Game?.GetPlayerGame((ulong)playerId);
            if (playerGame != null)
            {
                playerGame.SubtractScore(amount);
                GameLogger.Info("CentralMapBulletController", $"[SubtractPlayerScore] Player {playerId} 점수 차감(멀티): -{amount}, 남은 점수: {playerGame.Score}");
            }
            else
            {
                GameLogger.Error("CentralMapBulletController", $"[SubtractPlayerScore] Player {playerId} BrickGame 없음 - 점수 차감 실패!");
            }
            return;
        }

        // ✅ 싱글플레이어: 전역 BrickGameManager 사용
        if (Managers.Game?.BrickGame != null)
        {
            Managers.Game.BrickGame.SubtractScore(amount);
            GameLogger.Info("CentralMapBulletController", $"[SubtractPlayerScore] Player {playerId} 점수 차감(싱글): -{amount}, 남은 점수: {Managers.Game.BrickGame.Score}");
        }
        else
        {
            GameLogger.Error("CentralMapBulletController", $"[SubtractPlayerScore] BrickGameManager null - 점수 차감 실패!");
        }
    }

    /// <summary>
    /// 발사 요청 처리 (Server-side)
    /// </summary>
    private void HandleFireRequest(ulong clientId, int bulletCount)
    {
        if (_isGameOver.Value) return;

        // 이미 발사 중이면 무시
        if (_isFiring.TryGetValue(clientId, out bool firing) && firing)
        {
            return;
        }

        int playerIndex = (int)clientId;

        if (!_playerCannons.TryGetValue(playerIndex, out Cannon cannon))
        {
            GameLogger.Error("CentralMapBulletController", $"Player {playerIndex} 대포를 찾을 수 없음");
            return;
        }

        StartCoroutine(FireBulletsCoroutine(clientId, cannon, bulletCount));
    }

    /// <summary>
    /// 씬의 모든 대포 캐시
    /// </summary>
    private void CacheCannons()
    {
        _playerCannons.Clear();

        if (IsometricGridGenerator.Instance != null)
        {
            // ✅ CLIENT에서는 _cannons 리스트가 비어있을 수 있음 - RefreshCannonsFromScene() 먼저 호출
            IsometricGridGenerator.Instance.RefreshCannonsFromScene();

            var cannons = IsometricGridGenerator.Instance.GetAllCannons();
            foreach (var cannon in cannons)
            {
                if (cannon != null)
                {
                    _playerCannons[cannon.playerID] = cannon;
                    GameLogger.Info("CentralMapBulletController", $"대포 캐시: Player {cannon.playerID}");
                }
            }

            GameLogger.Info("CentralMapBulletController", $"IsometricGridGenerator에서 대포 {_playerCannons.Count}개 캐시됨");
        }

        // ✅ 여전히 비어있으면 씬에서 직접 검색
        if (_playerCannons.Count == 0)
        {
            GameLogger.Warning("CentralMapBulletController", "IsometricGridGenerator에서 대포 없음 - 씬에서 직접 검색...");
            var cannons = FindObjectsOfType<Cannon>();
            foreach (var cannon in cannons)
            {
                if (cannon != null && cannon.playerID >= 0)
                {
                    _playerCannons[cannon.playerID] = cannon;
                    GameLogger.Info("CentralMapBulletController", $"대포 캐시 (직접 검색): Player {cannon.playerID}");
                }
            }
            GameLogger.Info("CentralMapBulletController", $"직접 검색으로 대포 {_playerCannons.Count}개 캐시됨");
        }
    }

    #region Public API (외부에서 호출)
    /// <summary>
    /// 총알 발사 요청 (Client가 호출) - 블록 개수는 SERVER에서 확인
    /// </summary>
    public void RequestFire()
    {
        if (_isGameOver.Value) return;

        ulong localClientId = NetworkManager.Singleton.LocalClientId;

        // ✅ BrickGameMultiplayerSpawner의 ServerRpc 호출
        var spawner = FindObjectOfType<BrickGameMultiplayerSpawner>();
        if (spawner != null)
        {
            spawner.RequestCentralMapFireServerRpc(localClientId);
        }
    }

    /// <summary>
    /// [Server] BrickGameMultiplayerSpawner에서 호출 - 실제 발사 로직
    /// (런타임 AddComponent된 NetworkBehaviour는 ServerRpc 사용 불가하므로 우회)
    /// </summary>
    public void HandleFireRequestFromServer(ulong clientId)
    {
        GameLogger.Info("CentralMapBulletController", $"HandleFireRequestFromServer 시작 - clientId={clientId}, IsServer={IsServer}, bulletPrefab={(bulletPrefab != null ? bulletPrefab.name : "NULL")}");

        // Server에서만 실행
        if (!IsServer && NetworkManager.Singleton != null && !NetworkManager.Singleton.IsServer)
        {
            GameLogger.Warning("CentralMapBulletController", $"HandleFireRequestFromServer는 Server에서만 실행 가능 (IsServer={IsServer}, NetworkManager.IsServer={NetworkManager.Singleton?.IsServer})");
            return;
        }

        if (_isGameOver.Value)
        {
            GameLogger.Warning("CentralMapBulletController", "게임 오버 - 발사 불가");
            return;
        }

        // 이미 발사 중이면 무시
        if (_isFiring.TryGetValue(clientId, out bool firing) && firing)
        {
            GameLogger.Warning("CentralMapBulletController", $"Player {clientId} 이미 발사 중");
            return;
        }

        // 플레이어 인덱스 결정 (clientId → playerIndex)
        int playerIndex = (int)clientId; // 0 또는 1

        // ✅ 점수 = 총알 개수! (블록 개수 대신 점수 사용)
        int bulletCount = GetPlayerScore(playerIndex);
        if (bulletCount <= 0)
        {
            GameLogger.Warning("CentralMapBulletController", $"[Server] Player {playerIndex} 점수(총알) 없음 - 발사 불가");
            return;
        }

        // ✅ 점수 차감 (발사 전에 미리 차감!)
        SubtractPlayerScore(playerIndex, bulletCount);
        GameLogger.Info("CentralMapBulletController", $"[Server] Player {playerIndex} 점수 차감: -{bulletCount}");

        // ✅ 대포 캐시가 비어있으면 갱신 (대포가 늦게 생성된 경우 대비)
        if (_playerCannons.Count == 0)
        {
            GameLogger.Warning("CentralMapBulletController", "대포 캐시가 비어있음 - 갱신 시도...");
            CacheCannons();
        }

        // 대포 확인
        if (!_playerCannons.TryGetValue(playerIndex, out Cannon cannon))
        {
            // ✅ 그래도 없으면 다시 한번 캐시 갱신 후 재시도
            GameLogger.Warning("CentralMapBulletController", $"Player {playerIndex} 대포 없음 - 캐시 강제 갱신...");
            CacheCannons();

            if (!_playerCannons.TryGetValue(playerIndex, out cannon))
            {
                GameLogger.Error("CentralMapBulletController", $"Player {playerIndex} 대포를 찾을 수 없음 (캐시 갱신 후에도)");
                return;
            }
        }

        GameLogger.Info("CentralMapBulletController", $"[Server] Player {clientId} 총알 {bulletCount}발 발사 시작");

        // 발사 코루틴 시작
        StartCoroutine(FireBulletsCoroutine(clientId, cannon, bulletCount));
    }
    #endregion

    #region 총알 발사 로직 (Server에서 실행)

    /// <summary>
    /// 총알 발사 코루틴 (Server에서 실행)
    /// </summary>
    private IEnumerator FireBulletsCoroutine(ulong clientId, Cannon cannon, int bulletCount)
    {
        _isFiring[clientId] = true;

        if (bulletPrefab == null)
        {
            GameLogger.Error("CentralMapBulletController", "총알 프리팹이 없습니다!");
            _isFiring[clientId] = false;
            yield break;
        }

        for (int i = 0; i < bulletCount; i++)
        {
            if (_isGameOver.Value) break;

            // 발사 위치 (null 체크 추가)
            if (cannon == null || cannon.turretBarrel == null)
            {
                GameLogger.Error("CentralMapBulletController", "❌ cannon 또는 turretBarrel이 NULL!");
                yield break;
            }

            Transform firePoint = cannon.firePoint != null ? cannon.firePoint : cannon.turretBarrel;
            Vector3 spawnPos = firePoint.position;

            // ✅ 총알 Y 높이: 블록과 동일한 높이 (Y=0)에서 발사해야 충돌 감지됨!
            spawnPos.y = 0f;

            // ✅ 스폰 위치 랜덤 오프셋 (총알 겹침 방지)
            spawnPos.x += UnityEngine.Random.Range(-0.3f, 0.3f);
            spawnPos.z += UnityEngine.Random.Range(-0.3f, 0.3f);

            // ✅ FireDirection 사용 (centerRotation 기반 - Server/Client 동일)
            // turretBarrel.forward는 sweeping으로 인해 Server/Client가 다를 수 있음!
            Vector3 direction = cannon.FireDirection;
            // 방향도 수평으로 (Y=0)
            direction.y = 0;
            direction = direction.normalized;

            // ✅ 방향 랜덤 퍼짐 (샷건 효과 - 부와아악!)
            float spreadAngle = UnityEngine.Random.Range(-15f, 15f);
            direction = Quaternion.Euler(0, spreadAngle, 0) * direction;

            // ✅ 총알 생성 (풀링 우선, 폴백으로 Instantiate)
            NetworkObject netObj = null;
            GameObject bullet = null;
            bool fromPool = false;

            // ✅ 풀링 사용 (NetworkBulletPool 있으면)
            if (NetworkBulletPool.Instance != null)
            {
                netObj = NetworkBulletPool.Instance.GetNetworkObject(spawnPos, Quaternion.identity);
                if (netObj != null)
                {
                    bullet = netObj.gameObject;
                    fromPool = true;
                }
            }

            // ✅ 폴백: 풀에서 가져오기 실패 시
            if (netObj == null)
            {
                GameObject prefabToUse = NetworkBulletPool.Instance != null
                    ? NetworkBulletPool.Instance.BulletPrefab
                    : bulletPrefab;

                if (prefabToUse == null) continue;

                bullet = Instantiate(prefabToUse, spawnPos, Quaternion.identity);
                netObj = bullet.GetComponent<NetworkObject>();
            }

            // ✅ NetworkObject Spawn
            if (netObj == null) continue;
            netObj.Spawn();

            // ✅ Spawn 후에 상태 리셋 및 SetOwner/Fire 호출
            CannonBullet bulletScript = bullet.GetComponent<CannonBullet>();
            if (bulletScript != null)
            {
                if (fromPool) bulletScript.ResetForReuse();
                bulletScript.SetOwner(cannon, cannon.playerColor, cannon.playerID);
                bulletScript.Fire(direction, bulletSpeed);
            }

            // ✅ 프레임당 N발 발사 후 다음 프레임 대기 (기관총 효과: 다다다다닥!)
            // - 매 프레임 소량씩 Spawn → 네트워크 부하 분산 → 렉 없음!
            if ((i + 1) % bulletsPerFrame == 0)
            {
                yield return null; // 다음 프레임까지 대기
            }
        }

        _isFiring[clientId] = false;
        float totalFrames = bulletCount / (float)bulletsPerFrame;
        GameLogger.Success("CentralMapBulletController", $"[Server] Player {clientId} 발사 완료! 🔫 {bulletCount}발 (~{totalFrames:F0}프레임)");
    }
    #endregion

    #region Game Over (대포 파괴)
    /// <summary>
    /// 대포 파괴 처리 (Server에서 실행)
    /// </summary>
    private void HandleCannonDestroyed(int destroyedPlayerId)
    {
        if (!IsServer) return;
        if (_isGameOver.Value) return;

        GameLogger.Warning("CentralMapBulletController", $"★★★ Player {destroyedPlayerId} 대포 파괴! ★★★");

        // 승자 결정 (파괴된 플레이어가 아닌 쪽)
        int winnerId = (destroyedPlayerId == 0) ? 1 : 0;

        // NetworkVariable 업데이트
        _winnerPlayerId.Value = winnerId;
        _isGameOver.Value = true;

        // 게임 오버 ClientRpc (추가 처리용)
        NotifyGameOverClientRpc(winnerId, destroyedPlayerId);
    }

    /// <summary>
    /// [ClientRpc] 게임 오버 알림
    /// </summary>
    [ClientRpc]
    private void NotifyGameOverClientRpc(int winnerId, int loserId)
    {
        GameLogger.Warning("CentralMapBulletController",
            $"🎮 게임 오버! 승자: Player {winnerId}, 패자: Player {loserId}");

        // ActionBus에 발행 (UI 업데이트용)
        Managers.PublishAction(MB.Infrastructure.Messages.ActionId.BrickGame_GameOver,
            new GameOverPayload(winnerId, loserId));
    }

    /// <summary>
    /// NetworkVariable 변경 콜백
    /// </summary>
    private void OnGameOverChanged(bool previousValue, bool newValue)
    {
        if (newValue)
        {
            GameLogger.Warning("CentralMapBulletController", $"게임 오버 감지 (동기화됨) - 승자: {_winnerPlayerId.Value}");
        }
    }
    #endregion

    #region Utility
    /// <summary>
    /// 특정 플레이어의 대포 가져오기
    /// </summary>
    public Cannon GetPlayerCannon(int playerId)
    {
        _playerCannons.TryGetValue(playerId, out Cannon cannon);
        return cannon;
    }
    #endregion
}

/// <summary>
/// 게임 오버 페이로드 (ActionBus용)
/// </summary>
public readonly struct GameOverPayload : IActionPayload
{
    public int WinnerId { get; }
    public int LoserId { get; }

    public GameOverPayload(int winnerId, int loserId)
    {
        WinnerId = winnerId;
        LoserId = loserId;
    }
}

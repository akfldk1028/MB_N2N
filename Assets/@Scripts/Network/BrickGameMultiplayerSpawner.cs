using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Unity.Assets.Scripts.Objects;

/// <summary>
/// 블록깨기 멀티플레이 스폰 매니저
/// 플레이어가 접속하면 자동으로 Ball과 Plank를 생성
/// Inspector 없이 코드로만 동작
/// </summary>
public class BrickGameMultiplayerSpawner : NetworkBehaviour
{
    #region 설정 (코드로 자동 설정)
    private GameObject _ballPrefab;
    private GameObject _plankPrefab;
    private Camera _mainCamera;

    // 플레이어별 스폰 위치 오프셋
    private float _plankYPosition = -4f; // 패들 Y 위치
    private float _plankSpacing = 3f;     // 플레이어간 간격

    // 경계 Transform (코드로 찾기)
    private Transform _leftBoundary;
    private Transform _rightBoundary;
    private Transform _topBoundary;
    #endregion

    #region 플레이어 추적
    private Dictionary<ulong, PlayerObjects> _playerObjects = new Dictionary<ulong, PlayerObjects>();

    private class PlayerObjects
    {
        public GameObject Ball;
        public GameObject Plank;
        public int PlayerIndex;
        public ObjectPlacement ObjectPlacement; // ✅ 플레이어별 ObjectPlacement
    }
    #endregion

    void Start()
    {
        // ✅ 수동 Spawn (NetworkObject가 자동 Spawn 안 될 경우)
        var networkObject = GetComponent<NetworkObject>();
        if (networkObject != null && !networkObject.IsSpawned)
        {
            var networkManager = NetworkManager.Singleton;
            if (networkManager != null && networkManager.IsListening && networkManager.IsServer)
            {
                networkObject.Spawn();
                GameLogger.Success("BrickGameMultiplayerSpawner", "수동 Spawn 완료");
            }
        }
    }

    /// <summary>
    /// Prefab 자동 로드 (ResourceManager에서 가져오기)
    /// </summary>
    private void LoadPrefabs()
    {
        // ✅ ResourceManager에서 로드 (Address 이름: "ball")
        _ballPrefab = Managers.Resource.Load<GameObject>("ball");
        if (_ballPrefab == null)
        {
            GameLogger.Error("BrickGameMultiplayerSpawner", "ball 프리팹을 찾을 수 없습니다!");
        }
        else
        {
            GameLogger.Success("BrickGameMultiplayerSpawner", $"Ball 프리팹 로드 완료: {_ballPrefab.name}");
        }

        // ✅ Plank 로드 - 씬에서 찾기 (프리팹 없을 수 있음)
        PhysicsPlank existingPlank = FindObjectOfType<PhysicsPlank>();
        if (existingPlank != null)
        {
            _plankPrefab = existingPlank.gameObject;
            GameLogger.Success("BrickGameMultiplayerSpawner", $"Plank 템플릿 발견: {_plankPrefab.name}");
        }
        else
        {
            GameLogger.Error("BrickGameMultiplayerSpawner", "씬에서 PhysicsPlank를 찾을 수 없습니다!");
        }
    }

    /// <summary>
    /// 씬에서 경계 Transform 찾기
    /// </summary>
    private void FindBoundaries()
    {
        // "LeftEnd", "RightEnd", "TopBorder" GameObject 찾기 (BrickGame 씬 구조)
        _leftBoundary = GameObject.Find("LeftEnd")?.transform;
        _rightBoundary = GameObject.Find("RightEnd")?.transform;
        _topBoundary = GameObject.Find("TopBorder")?.transform;

        if (_leftBoundary == null || _rightBoundary == null)
        {
            GameLogger.Warning("BrickGameMultiplayerSpawner", "경계(LeftEnd/RightEnd)를 찾을 수 없습니다. 기본값 사용");
            // 기본값 생성
            var left = new GameObject("LeftEnd_Auto");
            left.transform.position = new Vector3(-8f, _plankYPosition, 0);
            _leftBoundary = left.transform;

            var right = new GameObject("RightEnd_Auto");
            right.transform.position = new Vector3(8f, _plankYPosition, 0);
            _rightBoundary = right.transform;
        }

        if (_topBoundary == null)
        {
            GameLogger.Warning("BrickGameMultiplayerSpawner", "TopBorder를 찾을 수 없습니다. 기본값 사용");
            var top = new GameObject("TopBorder_Auto");
            top.transform.position = new Vector3(0, 4f, 0);
            _topBoundary = top.transform;
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // ✅ 경계 및 카메라 초기화 (ObjectPlacement 생성 전에 필요)
        if (_mainCamera == null)
        {
            _mainCamera = Camera.main;
            if (_mainCamera == null)
            {
                GameLogger.Error("BrickGameMultiplayerSpawner", "Main Camera를 찾을 수 없습니다!");
            }
        }

        if (_leftBoundary == null || _rightBoundary == null)
        {
            FindBoundaries();
        }

        // ✅ 프리팹 로드 (Addressables 로드 완료 후)
        LoadPrefabs();

        if (IsServer)
        {
            // 서버: 클라이언트 연결 이벤트 구독
            NetworkManager.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.OnClientDisconnectCallback += OnClientDisconnected;

            GameLogger.Success("BrickGameMultiplayerSpawner", "서버 모드 - 클라이언트 연결 대기 중");

            // ✅ 씬 전환 후 이미 연결된 클라이언트들 초기화
            // (로비에서 연결된 후 GameScene으로 넘어온 경우, 이미 지나간 OnClientConnected 이벤트를 놓쳤으므로)
            GameLogger.Info("BrickGameMultiplayerSpawner", $"이미 연결된 클라이언트 수: {NetworkManager.ConnectedClientsIds.Count}");
            foreach (var clientId in NetworkManager.ConnectedClientsIds)
            {
                GameLogger.Info("BrickGameMultiplayerSpawner", $"[씬 전환 후] 클라이언트 {clientId} 초기화 시작");
                OnClientConnected(clientId);
            }
        }

        // ✅ Client-side: 카메라 설정 (Host 포함 모든 Client)
        if (IsClient)
        {
            SetupClientSideCameras();
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            NetworkManager.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.OnClientDisconnectCallback -= OnClientDisconnected;
        }

        base.OnNetworkDespawn();
    }

    /// <summary>
    /// 클라이언트 연결 시 Ball과 Plank 생성 (서버에서 실행)
    /// </summary>
    private void OnClientConnected(ulong clientId)
    {
        if (!IsServer) return;

        GameLogger.Info("BrickGameMultiplayerSpawner", $"🎮 플레이어 {clientId} 연결됨 - Ball & Plank 생성 중...");

        int playerIndex = _playerObjects.Count;

        // 1. Plank 생성
        GameObject plankObject = SpawnPlankForPlayer(clientId, playerIndex);

        // 2. Ball 생성
        GameObject ballObject = SpawnBallForPlayer(clientId, playerIndex, plankObject);

        // 3. ObjectPlacement 생성 (플레이어별 벽돌 영역)
        ObjectPlacement objectPlacement = CreateObjectPlacementForPlayer(clientId, playerIndex);

        // 4. 플레이어 오브젝트 추적
        _playerObjects[clientId] = new PlayerObjects
        {
            Ball = ballObject,
            Plank = plankObject,
            PlayerIndex = playerIndex,
            ObjectPlacement = objectPlacement
        };

        // ✅ 5. 플레이어별 BrickGameManager 생성 (Server-side)
        // 카메라는 각 Client에서 설정하므로 여기서는 Main Camera 임시 사용
        PhysicsPlank plank = plankObject.GetComponent<PhysicsPlank>();
        if (plank != null)
        {
            Managers.Game.InitializePlayerGame(
                clientId,
                objectPlacement,  // ✅ 플레이어별 ObjectPlacement 전달
                plank,
                _mainCamera,      // 임시로 Main Camera 전달 (Client에서 각자 설정)
                null              // 기본 설정 사용
            );
            GameLogger.Success("BrickGameMultiplayerSpawner", $"[Player {clientId}] BrickGameManager 생성 완료!");

            // ✅ 6. 게임 시작 (벽돌 생성)
            var playerGame = Managers.Game.GetPlayerGame(clientId);
            if (playerGame != null)
            {
                playerGame.StartGame();
                GameLogger.Success("BrickGameMultiplayerSpawner", $"[Player {clientId}] 게임 시작 및 벽돌 생성 완료!");
            }
        }
        else
        {
            GameLogger.Error("BrickGameMultiplayerSpawner", $"[Player {clientId}] PhysicsPlank 컴포넌트를 찾을 수 없습니다!");
        }

        GameLogger.Success("BrickGameMultiplayerSpawner", $"✅ 플레이어 {clientId} 스폰 완료 (Index: {playerIndex})");
    }

    /// <summary>
    /// 플레이어별 Plank 생성
    /// </summary>
    private GameObject SpawnPlankForPlayer(ulong clientId, int playerIndex)
    {
        if (_plankPrefab == null)
        {
            GameLogger.Error("BrickGameMultiplayerSpawner", "Plank 프리팹이 없습니다!");
            return null;
        }

        // 플레이어별 위치 계산
        float xPosition = CalculatePlayerXPosition(playerIndex);
        Vector3 spawnPosition = new Vector3(xPosition, _plankYPosition, 0);

        // Plank 생성 (기존 씬 Plank 복제 또는 프리팹 인스턴스화)
        GameObject plankObject;

        if (_plankPrefab.scene.IsValid())
        {
            // 씬 오브젝트인 경우 복제
            plankObject = Instantiate(_plankPrefab);
        }
        else
        {
            // 프리팹인 경우 그대로 인스턴스화
            plankObject = Instantiate(_plankPrefab);
        }

        plankObject.name = $"Plank_Player{clientId}";
        plankObject.transform.position = spawnPosition;

        // NetworkObject 설정
        NetworkObject networkObject = plankObject.GetComponent<NetworkObject>();
        if (networkObject == null)
        {
            networkObject = plankObject.AddComponent<NetworkObject>();
        }

        // 스폰 (Owner 지정)
        networkObject.SpawnWithOwnership(clientId);

        // PhysicsPlank 설정
        PhysicsPlank plank = plankObject.GetComponent<PhysicsPlank>();
        if (plank != null)
        {
            // 경계 설정
            plank.leftEnd = _leftBoundary;
            plank.rightEnd = _rightBoundary;
            plank.mainCamera = _mainCamera;
        }

        GameLogger.Info("BrickGameMultiplayerSpawner", $"  📍 Plank 스폰: {spawnPosition}");

        return plankObject;
    }

    /// <summary>
    /// 플레이어별 Ball 생성
    /// </summary>
    private GameObject SpawnBallForPlayer(ulong clientId, int playerIndex, GameObject plankObject)
    {
        if (_ballPrefab == null)
        {
            GameLogger.Error("BrickGameMultiplayerSpawner", "Ball 프리팹이 없습니다!");
            return null;
        }

        // Ball 위치: Plank 위에
        Vector3 spawnPosition = plankObject.transform.position + Vector3.up * 1f;

        // Ball 생성
        GameObject ballObject = Instantiate(_ballPrefab, spawnPosition, Quaternion.identity);
        ballObject.name = $"Ball_Player{clientId}";

        // NetworkObject 설정
        NetworkObject networkObject = ballObject.GetComponent<NetworkObject>();
        if (networkObject == null)
        {
            networkObject = ballObject.AddComponent<NetworkObject>();
        }

        // 스폰 (Owner 지정)
        networkObject.SpawnWithOwnership(clientId);

        // PhysicsBall 설정
        PhysicsBall ball = ballObject.GetComponent<PhysicsBall>();
        if (ball != null)
        {
            // Plank 참조 설정
            PhysicsPlank plank = plankObject.GetComponent<PhysicsPlank>();
            if (plank != null)
            {
                // Reflection 또는 public field로 plank 설정
                var field = typeof(PhysicsBall).GetField("plank",
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.Instance);

                if (field != null)
                {
                    field.SetValue(ball, plank);
                }
            }
        }

        GameLogger.Info("BrickGameMultiplayerSpawner", $"  ⚽ Ball 스폰: {spawnPosition}");

        return ballObject;
    }

    /// <summary>
    /// 플레이어별 ObjectPlacement 생성 (벽돌 생성 담당)
    /// </summary>
    private ObjectPlacement CreateObjectPlacementForPlayer(ulong clientId, int playerIndex)
    {
        // 1. 새 GameObject 생성
        GameObject placementObj = new GameObject($"ObjectPlacement_Player{clientId}");

        // 2. ObjectPlacement 컴포넌트 추가
        ObjectPlacement placement = placementObj.AddComponent<ObjectPlacement>();

        // 3. xOffset 계산 (플레이어별 영역 분리)
        float xOffset = CalculatePlayerXOffset(playerIndex);

        // 4. 플레이어별 경계 설정 (기존 경계 사용)
        placement.InitializeForPlayer(
            clientId,
            xOffset,
            _leftBoundary,
            _rightBoundary,
            _topBoundary
        );

        GameLogger.Success("BrickGameMultiplayerSpawner", $"[Player {clientId}] ObjectPlacement 생성 완료 (xOffset: {xOffset})");

        return placement;
    }

    /// <summary>
    /// 플레이어별 X 오프셋 계산 (벽돌 영역 분리)
    /// CalculatePlayerXPosition()과 동일한 로직 사용하여 Plank와 벽돌 영역 일치
    /// </summary>
    private float CalculatePlayerXOffset(int playerIndex)
    {
        int totalPlayers = _playerObjects.Count + 1;

        if (totalPlayers == 1)
        {
            return 0; // 1인: 중앙
        }
        else if (totalPlayers == 2)
        {
            // Plank 위치와 동일하게 _plankSpacing 사용
            return playerIndex == 0 ? -_plankSpacing : _plankSpacing;
        }
        else
        {
            // 3인 이상: Plank 위치와 동일한 로직
            float totalWidth = _plankSpacing * (totalPlayers - 1);
            float startX = -totalWidth / 2f;
            return startX + (playerIndex * _plankSpacing);
        }
    }

    /// <summary>
    /// 플레이어 X 위치 계산 (플레이어 인덱스 기반)
    /// </summary>
    private float CalculatePlayerXPosition(int playerIndex)
    {
        // 2인 플레이: 왼쪽(-2), 오른쪽(+2)
        // 3인 플레이: 왼쪽(-3), 중앙(0), 오른쪽(+3)
        // 4인 플레이: -4.5, -1.5, +1.5, +4.5

        int totalPlayers = _playerObjects.Count + 1;

        if (totalPlayers == 1)
        {
            return 0; // 1인: 중앙
        }
        else if (totalPlayers == 2)
        {
            return playerIndex == 0 ? -_plankSpacing : _plankSpacing;
        }
        else
        {
            // 3인 이상: 균등 배치
            float totalWidth = _plankSpacing * (totalPlayers - 1);
            float startX = -totalWidth / 2f;
            return startX + (playerIndex * _plankSpacing);
        }
    }

    /// <summary>
    /// 클라이언트 연결 해제 시 오브젝트 제거
    /// </summary>
    private void OnClientDisconnected(ulong clientId)
    {
        if (!IsServer) return;

        if (_playerObjects.TryGetValue(clientId, out PlayerObjects objects))
        {
            // Ball & Plank 제거
            if (objects.Ball != null)
            {
                NetworkObject ballNetObj = objects.Ball.GetComponent<NetworkObject>();
                if (ballNetObj != null && ballNetObj.IsSpawned)
                {
                    ballNetObj.Despawn();
                }
                Destroy(objects.Ball);
            }

            if (objects.Plank != null)
            {
                NetworkObject plankNetObj = objects.Plank.GetComponent<NetworkObject>();
                if (plankNetObj != null && plankNetObj.IsSpawned)
                {
                    plankNetObj.Despawn();
                }
                Destroy(objects.Plank);
            }

            // ObjectPlacement 제거
            if (objects.ObjectPlacement != null)
            {
                Destroy(objects.ObjectPlacement.gameObject);
            }

            _playerObjects.Remove(clientId);

            // ✅ 플레이어 BrickGameManager 정리
            Managers.Game.CleanupPlayerGame(clientId);

            GameLogger.Warning("BrickGameMultiplayerSpawner", $"🔌 플레이어 {clientId} 연결 해제 - 오브젝트 제거됨");
        }
    }

    /// <summary>
    /// Client-side 카메라 Viewport 설정 (OnNetworkSpawn에서 호출)
    /// Host: Main(왼쪽) + Sub(오른쪽)
    /// Client: Sub(왼쪽) + Main(오른쪽)
    /// </summary>
    private void SetupClientSideCameras()
    {
        var networkManager = NetworkManager.Singleton;
        if (networkManager == null)
        {
            GameLogger.Error("BrickGameMultiplayerSpawner", "[Client] NetworkManager를 찾을 수 없습니다!");
            return;
        }

        ulong localClientId = networkManager.LocalClientId;

        GameLogger.Info("BrickGameMultiplayerSpawner",
            $"[Client {localClientId}] 카메라 Viewport 설정 시작");

        // CameraManager를 통해 Viewport만 조정
        Managers.Camera.SetupViewportsForLocalPlayer(localClientId);

        GameLogger.Success("BrickGameMultiplayerSpawner",
            $"[Client {localClientId}] 카메라 Viewport 설정 완료!");
    }
}

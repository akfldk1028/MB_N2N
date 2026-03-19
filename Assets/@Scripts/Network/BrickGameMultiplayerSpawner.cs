using System;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using Unity.Assets.Scripts.Objects;
using MB.Infrastructure.Messages;
using MB.Network.Factories;

/// <summary>
/// 블록깨기 멀티플레이 스폰 매니저
/// - 플레이어가 접속하면 자동으로 Ball과 Plank를 생성
/// - 플레이어별 점수 NetworkVariable 동기화 (땅따먹기 포함)
/// - Inspector 없이 코드로만 동작
/// </summary>
public class BrickGameMultiplayerSpawner : NetworkBehaviour
{
    #region 설정 (코드로 자동 설정)
    private GameObject _ballPrefab;
    private GameObject _plankPrefab;
    private Camera _mainCamera;

    // 플레이어별 스폰 위치 오프셋
    private float _plankYPosition = -4f; // 패들 Y 위치
    private float _plankSpacing = 15f;    // ✅ 플레이어간 간격 (3→15: 카메라 영역 침범 방지)

    // 경계 Transform (코드로 찾기)
    private Transform _leftBoundary;
    private Transform _rightBoundary;
    private Transform _topBoundary;
    #endregion

    #region Factories (모듈화된 생성 로직)
    private BrickGameBordersFactory _bordersFactory = new BrickGameBordersFactory();
    private BrickGameSpawnFactory _spawnFactory;
    #endregion

    #region NetworkVariables (점수 동기화 - Server Write, Everyone Read)
    /// <summary>
    /// Player 0 (Host) 점수
    /// </summary>
    private NetworkVariable<int> _player0Score = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    /// <summary>
    /// Player 1 (Client) 점수
    /// </summary>
    private NetworkVariable<int> _player1Score = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    /// <summary>
    /// 땅따먹기 영역 비율 (0.0 ~ 1.0)
    /// 0.0 = Player 0 완전 승리, 0.5 = 중립, 1.0 = Player 1 완전 승리
    /// </summary>
    private NetworkVariable<float> _territoryRatio = new NetworkVariable<float>(
        0.5f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    // 점수 구독 관리
    private Dictionary<ulong, Action<int>> _scoreHandlers = new Dictionary<ulong, Action<int>>();
    #endregion

    #region Public Properties (점수 조회)
    public int Player0Score => _player0Score.Value;
    public int Player1Score => _player1Score.Value;
    public float TerritoryRatio => _territoryRatio.Value;
    #endregion

    #region 플레이어 추적
    private Dictionary<ulong, PlayerObjects> _playerObjects = new Dictionary<ulong, PlayerObjects>();

    private class PlayerObjects
    {
        public GameObject Ball;
        public GameObject Plank;
        public int PlayerIndex;
        public ObjectPlacement ObjectPlacement; // ✅ 플레이어별 ObjectPlacement
        public GameObject LeftBoundary;  // ✅ 플레이어별 왼쪽 경계
        public GameObject RightBoundary; // ✅ 플레이어별 오른쪽 경계
        public GameObject BordersContainer; // ✅ 플레이어별 물리 벽 (BoxCollider2D 포함)
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
    /// Prefab 자동 로드 (ResourceManager에서 가져오기) - Server만 호출
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

        // ✅ Plank 로드 (이미 템플릿이 있으면 스킵)
        if (_plankPrefab == null)
        {
            // 템플릿이 없으면 Addressable로 로드 시도
            _plankPrefab = Managers.Resource.Load<GameObject>("plank");
            if (_plankPrefab == null)
            {
                GameLogger.Error("BrickGameMultiplayerSpawner", "❌ Plank 프리팹을 찾을 수 없습니다! 씬에 Plank가 없고 Addressable도 없습니다.");
            }
            else
            {
                GameLogger.Success("BrickGameMultiplayerSpawner", $"Plank 프리팹 로드 완료 (Addressable): {_plankPrefab.name}");
            }
        }
        else
        {
            GameLogger.Success("BrickGameMultiplayerSpawner", $"Plank 템플릿 이미 있음: {_plankPrefab.name}");
        }

        // ✅ SpawnFactory 초기화 (컨텍스트 주입)
        InitializeSpawnFactory();
    }

    /// <summary>
    /// SpawnFactory 초기화 (컨텍스트 주입 방식)
    /// </summary>
    private void InitializeSpawnFactory()
    {
        var context = new BrickGameSpawnFactory.SpawnContext
        {
            BallPrefab = _ballPrefab,
            PlankPrefab = _plankPrefab,
            MainCamera = _mainCamera,
            LeftBoundary = _leftBoundary,
            RightBoundary = _rightBoundary,
            PlankYPosition = _plankYPosition,
            PlankSpacing = _plankSpacing
        };

        _spawnFactory = new BrickGameSpawnFactory(context);
        GameLogger.Success("BrickGameMultiplayerSpawner", "SpawnFactory 초기화 완료 (컨텍스트 주입)");
    }

    /// <summary>
    /// 씬에 존재하는 기존 Ball/Plank 제거 (중복 스폰 방지)
    /// 모든 클라이언트에서 실행 (NetworkObject Spawn 전의 씬 오브젝트만 제거)
    /// </summary>
    private void RemoveExistingSceneObjects()
    {
        // 1. Ball 제거
        var existingBalls = FindObjectsOfType<Unity.Assets.Scripts.Objects.PhysicsBall>();
        if (existingBalls.Length > 0)
        {
            GameLogger.Warning("BrickGameMultiplayerSpawner", $"씬에 {existingBalls.Length}개의 Ball 발견 - 제거 중...");
            foreach (var ball in existingBalls)
            {
                // ✅ NetworkObject가 Spawn되지 않은 씬 오브젝트만 제거
                var netObj = ball.GetComponent<NetworkObject>();
                if (netObj == null || !netObj.IsSpawned)
                {
                    Destroy(ball.gameObject);
                }
            }
            GameLogger.Success("BrickGameMultiplayerSpawner", "씬의 기존 Ball 제거 완료");
        }

        // 2. Plank 제거 (템플릿으로 복제 후 제거)
        var existingPlanks = FindObjectsOfType<PhysicsPlank>();
        if (existingPlanks.Length > 0)
        {
            GameLogger.Warning("BrickGameMultiplayerSpawner", $"씬에 {existingPlanks.Length}개의 Plank 발견 - 제거 중...");

            // Server만 템플릿으로 복제 저장 (원본은 Destroy될 것이므로)
            if (IsServer && _plankPrefab == null && existingPlanks.Length > 0)
            {
                // ✅ Instantiate로 복제본 생성 (원본이 Destroy되어도 템플릿은 살아있음)
                GameObject plankTemplate = Instantiate(existingPlanks[0].gameObject);
                plankTemplate.name = "PlankTemplate";
                plankTemplate.SetActive(false); // 보이지 않게
                DontDestroyOnLoad(plankTemplate); // 씬 전환 시 보존
                _plankPrefab = plankTemplate;

                GameLogger.Success("BrickGameMultiplayerSpawner", $"Plank 템플릿 복제 저장: {_plankPrefab.name}");
            }

            foreach (var plank in existingPlanks)
            {
                // ✅ NetworkObject가 Spawn되지 않은 씬 오브젝트만 제거
                var netObj = plank.GetComponent<NetworkObject>();
                if (netObj == null || !netObj.IsSpawned)
                {
                    Destroy(plank.gameObject);
                }
            }
            GameLogger.Success("BrickGameMultiplayerSpawner", "씬의 기존 Plank 제거 완료");
        }
    }

    /// <summary>
    /// 씬에서 경계 Transform 찾기
    /// </summary>
    private void FindBoundaries()
    {
        // "LeftEnd", "RightEnd", "TopEnd" GameObject 찾기 (BrickGame 씬 구조)
        var sceneLeft = GameObject.Find("LeftEnd")?.transform;
        var sceneRight = GameObject.Find("RightEnd")?.transform;
        var sceneTop = GameObject.Find("TopEnd")?.transform;

        if (sceneLeft != null && sceneRight != null)
        {
            // ✅ 씬 경계에서 게임 영역 크기 계산 후, 원점 중심으로 재배치
            // (씬의 BrickGame이 원점에서 벗어나 있어도 멀티플레이어 카메라와 정렬됨)
            float gameWidth = sceneRight.position.x - sceneLeft.position.x;
            float halfWidth = gameWidth / 2f;
            float boundaryY = sceneLeft.position.y;

            var left = new GameObject("LeftBound_MP");
            left.transform.position = new Vector3(-halfWidth, boundaryY, 0);
            _leftBoundary = left.transform;

            var right = new GameObject("RightBound_MP");
            right.transform.position = new Vector3(halfWidth, boundaryY, 0);
            _rightBoundary = right.transform;

            GameLogger.Success("BrickGameMultiplayerSpawner",
                $"경계 원점 중심 재배치: 씬({sceneLeft.position.x:F1}~{sceneRight.position.x:F1}) → MP({-halfWidth:F1}~{halfWidth:F1}), Y={boundaryY:F1}");
        }
        else
        {
            GameLogger.Warning("BrickGameMultiplayerSpawner", "경계(LeftEnd/RightEnd)를 찾을 수 없습니다. 기본값 사용");
            var left = new GameObject("LeftEnd_Auto");
            left.transform.position = new Vector3(-3.6f, _plankYPosition, 0);
            _leftBoundary = left.transform;

            var right = new GameObject("RightEnd_Auto");
            right.transform.position = new Vector3(3.6f, _plankYPosition, 0);
            _rightBoundary = right.transform;
        }

        if (sceneTop != null)
        {
            // ✅ Top도 원점 중심으로 재배치 (Y만 사용)
            var top = new GameObject("TopBound_MP");
            top.transform.position = new Vector3(0, sceneTop.position.y, 0);
            _topBoundary = top.transform;
        }
        else
        {
            GameLogger.Warning("BrickGameMultiplayerSpawner", "TopEnd를 찾을 수 없습니다. 기본값 사용");
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

        // ✅ Server에서만 씬의 기존 Ball/Plank 제거 (중복 방지)
        // Client에서 실행하면 Server가 Spawn한 오브젝트를 삭제할 수 있음!
        if (IsServer)
        {
            RemoveExistingSceneObjects();
        }

        if (IsServer)
        {
            // ✅ Server만 프리팹 로드 (Client는 Server가 Spawn한 것 받기만 함)
            LoadPrefabs();

            // 서버: 클라이언트 연결 이벤트 구독
            NetworkManager.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.OnClientDisconnectCallback += OnClientDisconnected;

            GameLogger.Success("BrickGameMultiplayerSpawner", "서버 모드 - 클라이언트 연결 대기 중 (점수 동기화 내장)");

            // ✅ 씬 전환 후 이미 연결된 클라이언트들 초기화
            // (로비에서 연결된 후 GameScene으로 넘어온 경우, 이미 지나간 OnClientConnected 이벤트를 놓쳤으므로)
            GameLogger.Info("BrickGameMultiplayerSpawner", $"이미 연결된 클라이언트 수: {NetworkManager.ConnectedClientsIds.Count}");
            foreach (var clientId in NetworkManager.ConnectedClientsIds)
            {
                GameLogger.Info("BrickGameMultiplayerSpawner", $"[씬 전환 후] 클라이언트 {clientId} 초기화 시작");
                OnClientConnected(clientId);
            }
        }

        // ✅ Client-side: NetworkVariable 변경 구독 + 카메라 설정
        if (IsClient)
        {
            // 점수 변경 콜백 구독
            _player0Score.OnValueChanged += OnPlayer0ScoreChanged;
            _player1Score.OnValueChanged += OnPlayer1ScoreChanged;
            _territoryRatio.OnValueChanged += OnTerritoryRatioChanged;

            SetupClientSideCameras();
            GameLogger.Success("BrickGameMultiplayerSpawner", "[Client] 점수 NetworkVariable 구독 완료");
        }

        // ✅ HOST/CLIENT 모두: CentralMapBulletController 추가 (ActionBus 구독 + ServerRpc 호출용)
        // 타이밍 문제 해결: GameScene.Start()보다 OnNetworkSpawn()이 더 확실함
        InitializeCentralMapBulletController();

        // ✅ Server: WinConditionManager 초기화 및 이벤트 구독 (대포 파괴 시 승리 조건 동기화)
        if (IsServer)
        {
            // 1. GameManager.WinCondition 초기화 (Cannon.OnCannonDestroyed 구독)
            Managers.Game?.InitializeWinCondition();

            // 2. BrickGameMultiplayerSpawner가 WinCondition.OnGameEnded 구독 (ClientRpc 동기화용)
            SubscribeWinConditionEvents();
        }
    }

    /// <summary>
    /// CentralMapBulletController 초기화 (땅따먹기 총알 발사용)
    /// HOST/CLIENT 모두 필요 (ActionBus 구독 + ServerRpc 호출)
    /// </summary>
    private void InitializeCentralMapBulletController()
    {
        // 이미 있는지 확인
        var bulletController = GetComponent<CentralMapBulletController>();
        if (bulletController != null)
        {
            GameLogger.Info("BrickGameMultiplayerSpawner", "CentralMapBulletController 이미 존재함");
            return;
        }

        // AddComponent
        bulletController = gameObject.AddComponent<CentralMapBulletController>();

        // ✅ 이미 Spawn된 NetworkObject에 AddComponent하면 OnNetworkSpawn() 자동 호출 안됨!
        // 수동 초기화 호출
        bulletController.ManualInitialize();

        GameLogger.Success("BrickGameMultiplayerSpawner",
            $"[{(IsServer ? "HOST" : "CLIENT")}] CentralMapBulletController 추가 및 초기화 완료");
    }

    #region WinCondition Network Sync
    // ✅ WinConditionManager가 GameManager 레벨로 이동됨 (게임 전역에서 하나)
    // → 더 이상 플레이어별 핸들러 불필요
    private bool _winConditionSubscribed = false;

    /// <summary>
    /// [Server] GameManager.WinCondition 이벤트 구독 (한번만)
    /// </summary>
    private void SubscribeWinConditionEvents()
    {
        if (_winConditionSubscribed) return;

        var winCondition = Managers.Game?.WinCondition;
        if (winCondition == null)
        {
            GameLogger.Warning("BrickGameMultiplayerSpawner", "[Server] WinConditionManager가 null!");
            return;
        }

        winCondition.OnGameEnded += HandleGameEnded;
        _winConditionSubscribed = true;

        GameLogger.Success("BrickGameMultiplayerSpawner", "[Server] WinCondition 이벤트 구독 완료 (GameManager 레벨)");
    }

    /// <summary>
    /// [Server] GameManager.WinCondition 이벤트 구독 해제
    /// </summary>
    private void UnsubscribeWinConditionEvents()
    {
        if (!_winConditionSubscribed) return;

        var winCondition = Managers.Game?.WinCondition;
        if (winCondition != null)
        {
            winCondition.OnGameEnded -= HandleGameEnded;
        }
        _winConditionSubscribed = false;

        GameLogger.Info("BrickGameMultiplayerSpawner", "[Server] WinCondition 이벤트 구독 해제 완료");
    }

    /// <summary>
    /// [Server] 게임 종료 처리 → ClientRpc로 동기화
    /// </summary>
    private void HandleGameEnded(int winnerID, int loserID)
    {
        GameLogger.Success("BrickGameMultiplayerSpawner",
            $"[Server] 게임 종료! 승자: Player {winnerID}, 패자: Player {loserID} → ClientRpc 전송");

        // 모든 클라이언트에 동기화
        NotifyGameEndedClientRpc(winnerID, loserID);
    }

    /// <summary>
    /// [ClientRpc] 모든 클라이언트에 게임 종료 알림
    /// </summary>
    [ClientRpc]
    private void NotifyGameEndedClientRpc(int winnerID, int loserID)
    {
        // Host는 이미 처리함
        if (IsServer)
        {
            GameLogger.Info("BrickGameMultiplayerSpawner", "[ClientRpc] Host는 이미 처리됨 - 스킵");
            return;
        }

        GameLogger.Success("BrickGameMultiplayerSpawner",
            $"[ClientRpc] 게임 종료 수신! 승자: Player {winnerID}, 패자: Player {loserID}");

        // 클라이언트의 GameManager.WinCondition에 알림
        Managers.Game?.WinCondition?.ProcessGameEndedFromServer(winnerID, loserID);
    }
    #endregion

    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            NetworkManager.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.OnClientDisconnectCallback -= OnClientDisconnected;

            // 점수 구독 해제
            foreach (var handler in _scoreHandlers)
            {
                var playerGame = Managers.Game.GetPlayerGame(handler.Key);
                if (playerGame != null)
                {
                    playerGame.OnScoreChanged -= handler.Value;
                }
            }
            _scoreHandlers.Clear();

            // WinCondition 구독 해제 (GameManager 레벨)
            UnsubscribeWinConditionEvents();

            // WinConditionManager 정리 (Cannon.OnCannonDestroyed 구독 해제)
            Managers.Game?.CleanupWinCondition();
        }

        // Client: NetworkVariable 콜백 해제
        if (IsClient)
        {
            _player0Score.OnValueChanged -= OnPlayer0ScoreChanged;
            _player1Score.OnValueChanged -= OnPlayer1ScoreChanged;
            _territoryRatio.OnValueChanged -= OnTerritoryRatioChanged;
        }

        base.OnNetworkDespawn();
    }

    /// <summary>
    /// 클라이언트 연결 시 Ball과 Plank 생성 (서버에서 실행)
    /// </summary>
    private void OnClientConnected(ulong clientId)
    {
        if (!IsServer) return;

        int playerIndex = _playerObjects.Count;
        int totalPlayers = NetworkManager.ConnectedClientsIds.Count;

        GameLogger.Info("BrickGameMultiplayerSpawner", $"🎮 플레이어 {clientId} 연결됨 - Ball & Plank 생성 중...");
        GameLogger.Warning("BrickGameMultiplayerSpawner", $"[DEBUG] clientId={clientId}, playerIndex={playerIndex}, totalPlayers={totalPlayers}");

        // 0. xOffset 계산 (모든 스폰에서 공통 사용)
        float xOffset = CalculatePlayerXOffset(clientId);

        // 1. Plank 생성 (경계 포함)
        var (plankObject, leftBound, rightBound) = SpawnPlankForPlayer(clientId, playerIndex);

        // 2. Ball 생성
        GameObject ballObject = SpawnBallForPlayer(clientId, playerIndex, plankObject);

        // 3. ObjectPlacement 생성 (플레이어별 벽돌 영역)
        ObjectPlacement objectPlacement = CreateObjectPlacementForPlayer(clientId, playerIndex);

        // ✅ 4. 플레이어별 물리 벽(borders) 생성 - BoxCollider2D + SpriteRenderer 포함!
        float leftX = leftBound.transform.position.x;
        float rightX = rightBound.transform.position.x;
        GameObject bordersContainer = SpawnBordersForPlayer(clientId, leftBound, rightBound);

        // ✅ 5. Client들에게도 벽 생성 요청 (ClientRpc)
        SpawnBordersClientRpc(clientId, leftX, rightX, _plankYPosition);

        // 6. 플레이어 오브젝트 추적
        _playerObjects[clientId] = new PlayerObjects
        {
            Ball = ballObject,
            Plank = plankObject,
            PlayerIndex = playerIndex,
            ObjectPlacement = objectPlacement,
            LeftBoundary = leftBound,   // ✅ 플레이어별 경계 저장
            RightBoundary = rightBound, // ✅ 플레이어별 경계 저장
            BordersContainer = bordersContainer // ✅ 물리 벽 컨테이너
        };

        // ✅ 7. 플레이어별 BrickGameManager 생성 (Server-side)
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

                // ✅ 7. 점수 변경 이벤트 구독 (NetworkVariable로 동기화)
                ConnectPlayerScoreSync(clientId, playerGame);

                // ✅ 8. 승리 조건은 GameManager 레벨에서 처리 (SubscribeWinConditionEvents에서 구독됨)
            }
        }
        else
        {
            GameLogger.Error("BrickGameMultiplayerSpawner", $"[Player {clientId}] PhysicsPlank 컴포넌트를 찾을 수 없습니다!");
        }

        GameLogger.Success("BrickGameMultiplayerSpawner", $"✅ 플레이어 {clientId} 스폰 완료 (Index: {playerIndex})");
    }

    /// <summary>
    /// 플레이어별 Plank 생성 - Factory 패턴 사용
    /// </summary>
    /// <returns>(Plank, LeftBoundary, RightBoundary)</returns>
    private (GameObject plank, GameObject leftBound, GameObject rightBound) SpawnPlankForPlayer(ulong clientId, int playerIndex)
    {
        // ✅ Factory를 통해 Plank 생성 (순수 GameObject 생성)
        var result = _spawnFactory.CreatePlank(clientId);
        if (result == null) return (null, null, null);

        // ✅ 활성화 후 NetworkObject 스폰 (네트워크 로직은 Spawner가 담당)
        _spawnFactory.ActivatePlank(result);

        // NetworkObject 설정
        NetworkObject networkObject = result.PlankObject.GetComponent<NetworkObject>();
        if (networkObject == null)
        {
            networkObject = result.PlankObject.AddComponent<NetworkObject>();
        }

        // 스폰 (Owner 지정)
        networkObject.SpawnWithOwnership(clientId);

        // ✅ NetworkVariable로 경계 동기화 (Spawn 후에 설정해야 함)
        if (result.PlankComponent != null)
        {
            result.PlankComponent.SetBoundaries(
                result.LeftBoundary.transform.position.x,
                result.RightBoundary.transform.position.x
            );
        }

        GameLogger.Info("BrickGameMultiplayerSpawner",
            $"  📍 Plank 네트워크 스폰 완료: {result.PlankObject.transform.position}");

        return (result.PlankObject, result.LeftBoundary, result.RightBoundary);
    }

    /// <summary>
    /// 플레이어별 Ball 생성 - Factory 패턴 사용
    /// </summary>
    private GameObject SpawnBallForPlayer(ulong clientId, int playerIndex, GameObject plankObject)
    {
        // ✅ Plank 결과 객체 생성 (Factory 호환용)
        var plankResult = new BrickGameSpawnFactory.PlankSpawnResult
        {
            PlankObject = plankObject,
            PlankComponent = plankObject.GetComponent<PhysicsPlank>()
        };

        // ✅ Factory를 통해 Ball 생성 (순수 GameObject 생성)
        var result = _spawnFactory.CreateBall(clientId, plankResult);
        if (result == null) return null;

        // ✅ 활성화 후 NetworkObject 스폰 (네트워크 로직은 Spawner가 담당)
        _spawnFactory.ActivateBall(result);

        // NetworkObject 설정 및 Spawn
        NetworkObject networkObject = result.BallObject.GetComponent<NetworkObject>();
        if (networkObject == null)
        {
            networkObject = result.BallObject.AddComponent<NetworkObject>();
        }

        // 스폰 (Owner 지정)
        networkObject.SpawnWithOwnership(clientId);

        GameLogger.Info("BrickGameMultiplayerSpawner",
            $"  ⚽ Ball 네트워크 스폰 완료: {result.BallObject.transform.position}");

        return result.BallObject;
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

        // 3. xOffset 계산 (clientId 기반)
        float xOffset = CalculatePlayerXOffset(clientId);

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
    /// 플레이어별 물리 벽(borders) 생성 - Factory 패턴 사용
    /// </summary>
    private GameObject SpawnBordersForPlayer(ulong clientId, GameObject leftBound, GameObject rightBound)
    {
        float leftX = leftBound.transform.position.x;
        float rightX = rightBound.transform.position.x;

        // ✅ Factory를 통해 Borders 생성 (모듈화)
        return _bordersFactory.CreateBorders(clientId, leftX, rightX, _plankYPosition);
    }

    /// <summary>
    /// [ClientRpc] 모든 Client에서 벽 생성 (시각적 + 물리)
    /// </summary>
    [ClientRpc]
    private void SpawnBordersClientRpc(ulong clientId, float leftX, float rightX, float plankY)
    {
        // ✅ Host(Server)는 이미 SpawnBordersForPlayer에서 생성했으므로 스킵
        if (IsServer)
        {
            GameLogger.Info("BrickGameMultiplayerSpawner", $"[ClientRpc] Host는 이미 벽 생성됨 - 스킵");
            return;
        }

        GameLogger.Info("BrickGameMultiplayerSpawner", $"[ClientRpc] Client에서 Player {clientId} 벽 생성 중...");

        // Client에서도 같은 위치에 벽 생성
        CreateBordersOnClient(clientId, leftX, rightX, plankY);
    }

    /// <summary>
    /// Client-side 벽 생성 - Factory 패턴 사용
    /// </summary>
    private void CreateBordersOnClient(ulong clientId, float leftX, float rightX, float plankY)
    {
        // ✅ Factory를 통해 Client용 Borders 생성 (모듈화)
        _bordersFactory.CreateBordersForClient(clientId, leftX, rightX, plankY);
    }

    /// <summary>
    /// 플레이어별 X 오프셋 계산 (벽돌 영역 분리)
    /// ✅ clientId 기반으로 고정 (totalPlayers 의존 제거)
    /// - clientId 0 (Host) → 왼쪽 (-3)
    /// - clientId 1 (Client) → 오른쪽 (+3)
    /// </summary>
    private float CalculatePlayerXOffset(ulong clientId)
    {
        // ✅ clientId 기반 고정 오프셋 (2인 멀티플레이어 기준)
        // totalPlayers로 계산하면 Host 연결 시점에 1명이라서 xOffset=0이 되는 버그 발생!
        float xOffset = (clientId == 0) ? -_plankSpacing : _plankSpacing;

        GameLogger.Warning("BrickGameMultiplayerSpawner", $"[DEBUG] CalculatePlayerXOffset: clientId={clientId}, xOffset={xOffset}");
        return xOffset;
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

            // ✅ 플레이어별 경계 제거
            if (objects.LeftBoundary != null)
            {
                Destroy(objects.LeftBoundary);
            }

            if (objects.RightBoundary != null)
            {
                Destroy(objects.RightBoundary);
            }

            // ✅ 플레이어별 물리 벽(borders) 제거
            if (objects.BordersContainer != null)
            {
                Destroy(objects.BordersContainer);
            }

            _playerObjects.Remove(clientId);

            // ✅ 점수 구독 해제
            DisconnectPlayerScoreSync(clientId);

            // ✅ 승리 조건은 GameManager 레벨에서 처리 (OnNetworkDespawn에서 해제됨)

            // ✅ 플레이어 BrickGameManager 정리
            Managers.Game.CleanupPlayerGame(clientId);

            GameLogger.Warning("BrickGameMultiplayerSpawner", $"🔌 플레이어 {clientId} 연결 해제 - 오브젝트 제거됨");
        }
    }

    #region Server: 점수 동기화 (NetworkVariable 직접 관리)
    /// <summary>
    /// [Server] 플레이어 게임의 점수 변경 이벤트 구독
    /// </summary>
    private void ConnectPlayerScoreSync(ulong clientId, BrickGameManager playerGame)
    {
        if (!IsServer) return;

        // 기존 핸들러 해제
        DisconnectPlayerScoreSync(clientId);

        // 점수 변경 핸들러 생성
        Action<int> scoreHandler = (score) => HandlePlayerScoreChanged(clientId, score);
        playerGame.OnScoreChanged += scoreHandler;
        _scoreHandlers[clientId] = scoreHandler;

        GameLogger.Success("BrickGameMultiplayerSpawner", $"[Server] Player {clientId} 점수 동기화 연결 완료");
    }

    /// <summary>
    /// [Server] 플레이어 게임의 점수 변경 이벤트 구독 해제
    /// </summary>
    private void DisconnectPlayerScoreSync(ulong clientId)
    {
        if (_scoreHandlers.TryGetValue(clientId, out var handler))
        {
            var playerGame = Managers.Game.GetPlayerGame(clientId);
            if (playerGame != null)
            {
                playerGame.OnScoreChanged -= handler;
            }
            _scoreHandlers.Remove(clientId);
            GameLogger.Info("BrickGameMultiplayerSpawner", $"[Server] Player {clientId} 점수 동기화 해제");
        }
    }

    /// <summary>
    /// [Server] 플레이어 점수 변경 처리 → NetworkVariable 업데이트
    /// </summary>
    private void HandlePlayerScoreChanged(ulong clientId, int newScore)
    {
        if (!IsServer) return;

        // 플레이어별 점수 업데이트
        if (clientId == 0)
        {
            _player0Score.Value = newScore;
        }
        else if (clientId == 1)
        {
            _player1Score.Value = newScore;
        }

        // 땅따먹기 영역 비율 계산
        CalculateTerritoryRatio();

        GameLogger.DevLog("BrickGameMultiplayerSpawner",
            $"[Server] Player {clientId} Score: {newScore}, Territory: {_territoryRatio.Value:F2}");
    }

    /// <summary>
    /// [Server] 점수 차이에 따른 땅따먹기 영역 비율 계산
    /// </summary>
    private void CalculateTerritoryRatio()
    {
        int totalScore = _player0Score.Value + _player1Score.Value;

        if (totalScore == 0)
        {
            _territoryRatio.Value = 0.5f; // 중립
            return;
        }

        // Player 1 점수 비율 (0.0 ~ 1.0)
        // Player 0이 이기면 0에 가까움, Player 1이 이기면 1에 가까움
        float ratio = (float)_player1Score.Value / totalScore;

        // 약간의 스무딩 (급격한 변화 방지)
        _territoryRatio.Value = Mathf.Lerp(_territoryRatio.Value, ratio, 0.3f);
    }
    #endregion

    #region Client: NetworkVariable 변경 콜백
    private void OnPlayer0ScoreChanged(int previousValue, int newValue)
    {
        // ActionBus에 발행 (UI 업데이트용)
        Managers.PublishAction(ActionId.BrickGame_ScoreChanged,
            new MultiplayerScorePayload(0, newValue, _player1Score.Value, _territoryRatio.Value));

        GameLogger.DevLog("BrickGameMultiplayerSpawner", $"[Client] Player 0 Score: {newValue}");
    }

    private void OnPlayer1ScoreChanged(int previousValue, int newValue)
    {
        // ActionBus에 발행 (UI 업데이트용)
        Managers.PublishAction(ActionId.BrickGame_ScoreChanged,
            new MultiplayerScorePayload(1, _player0Score.Value, newValue, _territoryRatio.Value));

        GameLogger.DevLog("BrickGameMultiplayerSpawner", $"[Client] Player 1 Score: {newValue}");
    }

    private void OnTerritoryRatioChanged(float previousValue, float newValue)
    {
        // ActionBus에 발행 (땅따먹기 UI 업데이트용)
        Managers.PublishAction(ActionId.BrickGame_TerritoryChanged,
            new TerritoryPayload(newValue));

        GameLogger.DevLog("BrickGameMultiplayerSpawner", $"[Client] Territory Ratio: {newValue:F2}");
    }
    #endregion

    #region Public API (점수 조회)
    /// <summary>
    /// 특정 플레이어 점수 조회
    /// </summary>
    public int GetPlayerScore(ulong clientId)
    {
        return clientId == 0 ? _player0Score.Value : _player1Score.Value;
    }

    /// <summary>
    /// 현재 이기고 있는 플레이어 반환 (null = 동점)
    /// </summary>
    public ulong? GetWinningPlayer()
    {
        if (_player0Score.Value > _player1Score.Value) return 0;
        if (_player1Score.Value > _player0Score.Value) return 1;
        return null; // 동점
    }
    #endregion

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

    #region CentralMap ServerRpc (땅따먹기 총알 발사)
    /// <summary>
    /// [ServerRpc] 땅따먹기 총알 발사 요청
    /// CentralMapBulletController에서 호출 (런타임 AddComponent된 NetworkBehaviour는 RPC 불가)
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void RequestCentralMapFireServerRpc(ulong clientId, ServerRpcParams rpcParams = default)
    {
        GameLogger.Info("BrickGameMultiplayerSpawner", $"[ServerRpc] RequestCentralMapFireServerRpc 수신 - clientId={clientId}");

        // CentralMapBulletController에 위임
        var bulletController = GetComponent<CentralMapBulletController>();
        if (bulletController != null)
        {
            GameLogger.Info("BrickGameMultiplayerSpawner", $"CentralMapBulletController 찾음, HandleFireRequestFromServer 호출");
            bulletController.HandleFireRequestFromServer(clientId);
        }
        else
        {
            GameLogger.Error("BrickGameMultiplayerSpawner", "❌ CentralMapBulletController를 찾을 수 없습니다!");
        }
    }

    /// <summary>
    /// [ClientRpc] 블록 소유권 변경을 모든 클라이언트에 동기화
    /// CannonBullet에서 호출 (SERVER에서 충돌 처리 후)
    /// </summary>
    [ClientRpc]
    public void ChangeBlockOwnerClientRpc(string blockName, int newOwnerID, float r, float g, float b)
    {
        if (IsometricGridGenerator.Instance == null)
        {
            GameLogger.Warning("BrickGameMultiplayerSpawner", "IsometricGridGenerator.Instance가 null입니다");
            return;
        }

        // 블록 이름으로 찾기
        var block = IsometricGridGenerator.Instance.FindBlockByName(blockName);
        if (block != null)
        {
            Color newColor = new Color(r, g, b);

            // 렌더러 색상 변경
            var renderer = block.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = newColor;
            }

            // 소유권 데이터 변경
            IsometricGridGenerator.Instance.SetBlockOwnerLocal(block, newOwnerID, newColor);

            GameLogger.Info("BrickGameMultiplayerSpawner", $"[ClientRpc] 블록 {blockName} 소유권 변경: Player {newOwnerID}");
        }
        else
        {
            GameLogger.Warning("BrickGameMultiplayerSpawner", $"[ClientRpc] 블록 {blockName}을 찾을 수 없습니다");
        }
    }

    /// <summary>
    /// [ClientRpc] 시각적 총알 생성 (NetworkObject 없음 - 네트워크 부하 최소화!)
    /// 모든 클라이언트에서 동일한 랜덤 시드로 로컬 총알 생성
    /// </summary>
    [ClientRpc]
    public void SpawnVisualBulletsClientRpc(
        Vector3 firePosition,
        Vector3 baseDirection,
        int bulletCount,
        float bulletSpeed,
        float spreadAngle,
        float positionSpread,
        float colorR, float colorG, float colorB,
        int randomSeed)
    {
        // 클라이언트에서 시각적 총알 생성 (코루틴으로 분산)
        StartCoroutine(SpawnVisualBulletsCoroutine(
            firePosition, baseDirection, bulletCount, bulletSpeed,
            spreadAngle, positionSpread,
            new Color(colorR, colorG, colorB), randomSeed));
    }

    /// <summary>
    /// 시각적 총알 생성 코루틴 (로컬 전용)
    /// </summary>
    private System.Collections.IEnumerator SpawnVisualBulletsCoroutine(
        Vector3 firePosition,
        Vector3 baseDirection,
        int bulletCount,
        float bulletSpeed,
        float spreadAngle,
        float positionSpread,
        Color bulletColor,
        int randomSeed)
    {
        // 동일한 랜덤 시드로 모든 클라이언트에서 같은 패턴 생성
        UnityEngine.Random.InitState(randomSeed);

        int spawned = 0;
        int bulletsPerBatch = 30;  // 한 프레임당 생성할 총알 수

        GameLogger.Info("BrickGameMultiplayerSpawner",
            $"[VisualBullets] 시각적 총알 {bulletCount}개 생성 시작 (seed={randomSeed})");

        while (spawned < bulletCount)
        {
            int batchSize = Mathf.Min(bulletsPerBatch, bulletCount - spawned);

            for (int i = 0; i < batchSize; i++)
            {
                // 스프레드 각도 계산
                float angleOffset = UnityEngine.Random.Range(-spreadAngle / 2f, spreadAngle / 2f);
                Vector3 spreadDirection = Quaternion.Euler(0, angleOffset, 0) * baseDirection;

                // 발사 위치에 랜덤 오프셋
                Vector3 spawnPos = firePosition;
                spawnPos.x += UnityEngine.Random.Range(-positionSpread, positionSpread);
                spawnPos.z += UnityEngine.Random.Range(-positionSpread, positionSpread);

                // 속도 변화
                float speedVariation = bulletSpeed * UnityEngine.Random.Range(0.9f, 1.1f);

                // 시각적 총알 생성 (VisualBullet 풀 사용)
                VisualBullet bullet = VisualBullet.CreateSimple(spawnPos, Quaternion.identity, 0.25f);
                if (bullet != null)
                {
                    bullet.Fire(spreadDirection, speedVariation, bulletColor);
                }

                spawned++;
            }

            // 프레임 분산 (30발마다 한 프레임 쉬기)
            yield return null;
        }

        GameLogger.Info("BrickGameMultiplayerSpawner",
            $"[VisualBullets] 시각적 총알 {spawned}개 생성 완료!");
    }
    #endregion

    #region MapComponent ServerRpc (BOMB, HARVEST 등)
    /// <summary>
    /// [ServerRpc] 맵 컴포넌트 사용 요청
    /// 클라이언트에서 BOMB 등 컴포넌트 사용 시 호출
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void RequestUseMapComponentServerRpc(int playerID, string componentID, ServerRpcParams rpcParams = default)
    {
        GameLogger.Info("BrickGameMultiplayerSpawner", $"[ServerRpc] RequestUseMapComponentServerRpc - Player {playerID}, Component: {componentID}");

        // MapComponentManager에서 컴포넌트 찾아서 실행
        if (Managers.Map?.Components != null)
        {
            bool success = Managers.Map.Components.Use(playerID, componentID);
            if (success)
            {
                GameLogger.Success("BrickGameMultiplayerSpawner", $"Player {playerID} - {componentID} 사용 성공");
            }
            else
            {
                GameLogger.Warning("BrickGameMultiplayerSpawner", $"Player {playerID} - {componentID} 사용 실패 (쿨다운 또는 미등록)");
            }
        }
        else
        {
            GameLogger.Warning("BrickGameMultiplayerSpawner", "MapComponentManager가 없습니다");
        }
    }

    /// <summary>
    /// [ClientRpc] 맵 컴포넌트 이펙트 동기화
    /// 서버에서 컴포넌트 사용 후 모든 클라이언트에 이펙트 재생 요청
    /// </summary>
    [ClientRpc]
    public void PlayComponentEffectClientRpc(string componentID, float x, float y, float z, int playerID)
    {
        GameLogger.Info("BrickGameMultiplayerSpawner", $"[ClientRpc] PlayComponentEffectClientRpc - {componentID} at ({x}, {y}, {z})");

        // 컴포넌트 타입별 이펙트 재생
        Vector3 position = new Vector3(x, y, z);

        switch (componentID)
        {
            case "bomb":
                var bomb = Managers.Map?.Components?.GetByID("bomb", playerID) as BombComponent;
                bomb?.PlayEffectLocal(position);
                break;

            case "harvest":
                var harvest = Managers.Map?.Components?.GetByID("harvest", playerID) as HarvestComponent;
                harvest?.PlayEffectLocal(position);
                break;

            default:
                GameLogger.Warning("BrickGameMultiplayerSpawner", $"알 수 없는 컴포넌트: {componentID}");
                break;
        }
    }
    #endregion
}

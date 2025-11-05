using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using MB.Infrastructure.Messages;
using MB.Infrastructure.State;
using Unity.Assets.Scripts.Network;
using Unity.Assets.Scripts.UnityServices.Lobbies;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Core.Environments;
using UnityEngine;

// Main Managers class - Service Locator Pattern
public class Managers : MonoBehaviour
{
    public static bool Initialized { get; private set; } = false;

    private static Managers s_instance;
    private static Managers Instance
    {
        get
        {
            if (s_instance == null)
            {
                Init();
            }
            return s_instance;
        }
    }

    #region Network Components
    // 네트워크 관련 컴포넌트들
    private NetworkManager _networkManager;
    private ConnectionManagerEx _connectionManager;
    private AuthManager _authManager;
    private UpdateRunnerEx _updateRunner;
    private LobbyServiceFacadeEx _lobbyServiceFacade;
    private LocalLobbyEx _localLobby;
    private LocalLobbyUserEx _localUser;
    private SessionManager _sessionManager;
    private DebugClassFacadeEx _debugFacade;

    // 네트워크 컴포넌트 public 접근자
    public static NetworkManager Network => Instance?._networkManager;
    public static ConnectionManagerEx Connection => Instance?._connectionManager;
    public static AuthManager Auth => Instance?._authManager;
    public static UpdateRunnerEx UpdateRunner => Instance?._updateRunner;
    public static LobbyServiceFacadeEx Lobby => Instance?._lobbyServiceFacade;
    public static LocalLobbyEx LocalLobby => Instance?._localLobby;
    public static LocalLobbyUserEx LocalUser => Instance?._localUser;
    public static SessionManager Session => Instance?._sessionManager;
    public static DebugClassFacadeEx Debug => Instance?._debugFacade;
    #endregion

    #region Contents
    private GameManager _game = new GameManager();
    private ObjectManager _object = new ObjectManager();
    private MapManager _map = new MapManager();
    private GameModeService _gameMode = new GameModeService();

    public static GameManager Game { get { return Instance?._game; } }
    public static ObjectManager Object { get { return Instance?._object; } }
    public static MapManager Map { get { return Instance?._map; } }
    public static GameModeService GameMode { get { return Instance?._gameMode; } }
    #endregion

    #region Core
    private readonly MB.Infrastructure.Messages.ActionMessageBus _actionBus = new MB.Infrastructure.Messages.ActionMessageBus();
    private MB.Infrastructure.Messages.ActionDispatcher _actionDispatcher;
    private StateMachine _stateMachine;

    private DataManager _data = new DataManager();
    private PoolManager _pool = new PoolManager();
    private ResourceManager _resource = new ResourceManager();
    private SceneManagerEx _scene = new SceneManagerEx();
    private UIManager _ui = new UIManager();

    public static MB.Infrastructure.Messages.ActionMessageBus ActionBus { get { return Instance?._actionBus; } }
    public static StateMachine StateMachine { get { return Instance?._stateMachine; } }
    public static DataManager Data { get { return Instance?._data; } }
    public static PoolManager Pool { get { return Instance?._pool; } }
    public static ResourceManager Resource { get { return Instance?._resource; } }
    public static SceneManagerEx Scene { get { return Instance?._scene; } }
    public static UIManager UI { get { return Instance?._ui; } }
    #endregion

    public static void Init()
    {
        if (s_instance == null && Initialized == false)
        {
            Initialized = true;

            GameObject go = GameObject.Find("@Managers");
            if (go == null)
            {
                go = new GameObject { name = "@Managers" };
                go.AddComponent<Managers>();
            }

            DontDestroyOnLoad(go);
            s_instance = go.GetComponent<Managers>();
        }
    }

    private async void Awake()
    {
        // 중복 체크
        if (s_instance != null && s_instance != this)
        {
            Destroy(gameObject);
            return;
        }

        s_instance = this;
        DontDestroyOnLoad(gameObject);

        // 메시지 시스템 초기화
        GameLogger.Progress("Managers", "Infrastructure 시스템 초기화 중...");
        if (_actionDispatcher == null)
        {
            _actionDispatcher = new MB.Infrastructure.Messages.ActionDispatcher(_actionBus);
            GameLogger.Success("Managers", "ActionDispatcher 생성됨");
        }

        if (_stateMachine == null)
        {
            _stateMachine = new StateMachine(_actionBus);
            GameLogger.Success("Managers", "StateMachine 생성됨");
        }

        // 네트워크 컴포넌트 초기화
        await InitializeNetworkComponents();
    }

    private async Task InitializeNetworkComponents()
    {
        GameLogger.SystemStart("Managers", "네트워크 컴포넌트 초기화 시작");

        // 1. Unity Services 초기화
        if (UnityServices.State == ServicesInitializationState.Uninitialized)
        {
            try
            {
                GameLogger.Progress("Managers", "Unity Services 초기화 중...");
                var options = new InitializationOptions()
                    .SetEnvironmentName("production");
                await UnityServices.InitializeAsync(options);
                GameLogger.Success("Managers", "Unity Services 초기화 성공");
            }
            catch (Exception e)
            {
                GameLogger.Error("Managers", $"Unity Services 초기화 실패: {e.Message}");
                return;
            }
        }
        else
        {
            GameLogger.Success("Managers", "Unity Services 이미 초기화됨");
        }

        // 2. 단일 GameObject에 모든 네트워크 컴포넌트 추가
        GameLogger.Progress("Managers", "네트워크 시스템 GameObject 생성 중...");
        var networkGo = new GameObject("@NetworkSystems");
        DontDestroyOnLoad(networkGo);
        
        _networkManager = networkGo.AddComponent<NetworkManager>();
        _connectionManager = networkGo.AddComponent<ConnectionManagerEx>();
        _authManager = networkGo.AddComponent<AuthManager>();
        _updateRunner = networkGo.AddComponent<UpdateRunnerEx>();
        
        GameLogger.Success("Managers", "모든 네트워크 컴포넌트가 @NetworkSystems에 생성됨");

        // 2-1. NetworkManager 설정 (Transport, NetworkConfig, NetworkPrefabs)
        await ConfigureNetworkManager(networkGo);
        GameLogger.Success("Managers", "NetworkManager 설정 완료 (Transport, Config, Prefabs)");

        // 3. Non-MonoBehaviour 객체들 초기화
        GameLogger.Info("Managers", "서비스 객체들 초기화 중...");
        _debugFacade = new DebugClassFacadeEx();
        _localLobby = new LocalLobbyEx();
        _localUser = new LocalLobbyUserEx();
        _lobbyServiceFacade = new LobbyServiceFacadeEx();
        _sessionManager = new SessionManager();

        // 4. LobbyServiceFacade 의존성 주입
        GameLogger.Progress("Managers", "LobbyServiceFacade 의존성 주입 중...");
        _lobbyServiceFacade.Initialize(
            _debugFacade,
            _updateRunner,
            _localLobby,
            _localUser,
            _scene,
            _networkManager
        );

        // 5. 멀티플레이어 기능 검증
        await ValidateMultiplayerCapabilities();

        GameLogger.Success("Managers", "네트워크 컴포넌트 초기화 완료!");
    }

    /// <summary>
    /// NetworkManager를 코드로 완전히 설정 (Transport, NetworkConfig, NetworkPrefabs)
    /// Addressable 시스템을 활용한 비동기 로딩
    /// </summary>
    private async Task ConfigureNetworkManager(GameObject networkGo)
    {
        GameLogger.Progress("Managers", "NetworkManager 설정 시작...");

        // 1. UnityTransport 추가 및 설정
        var transport = networkGo.AddComponent<UnityTransport>();
        transport.ConnectionData.Address = "127.0.0.1"; // 로컬호스트 (Relay 사용 시 자동 변경됨)
        transport.ConnectionData.Port = 7777;
        transport.ConnectionData.ServerListenAddress = "0.0.0.0";
        GameLogger.Info("Managers", "UnityTransport 설정 완료 (127.0.0.1:7777)");

        // 2. NetworkConfig 생성 및 설정
        var config = new NetworkConfig
        {
            NetworkTransport = transport,
            TickRate = 60,
            ClientConnectionBufferTimeout = 10,
            ConnectionApproval = false, // 일단 false, 나중에 승인 로직 추가 가능
            EnableSceneManagement = true,
            ForceSamePrefabs = true
        };
        GameLogger.Info("Managers", "NetworkConfig 설정 완료 (TickRate: 60)");

        // 3. NetworkPrefabsList 로드 (ResourceManager 위임)
        var prefabsList = await _resource.LoadNetworkPrefabsListAsync();
        if (prefabsList != null && prefabsList.PrefabList.Count > 0)
        {
            // NetworkPrefabsList를 NetworkPrefabs로 복사
            foreach (var networkPrefab in prefabsList.PrefabList)
            {
                config.Prefabs.Add(networkPrefab);
            }
            GameLogger.Success("Managers", $"NetworkPrefabs 등록 완료 ({prefabsList.PrefabList.Count}개)");
        }
        else
        {
            GameLogger.Warning("Managers", "NetworkPrefabsList 없음. 빈 리스트로 진행");
        }

        // 4. NetworkManager에 설정 적용
        _networkManager.NetworkConfig = config;
        GameLogger.Success("Managers", "NetworkManager.NetworkConfig 할당 완료");
    }


    /// <summary>
    /// 멀티플레이어 기능이 제대로 설정되었는지 검증
    /// </summary>
    private Task ValidateMultiplayerCapabilities()
    {
        UnityEngine.Debug.Log("<color=cyan>[Managers] 🔍 멀티플레이어 기능 검증 시작</color>");

        // Unity Services 상태 확인
        bool servicesReady = UnityServices.State == ServicesInitializationState.Initialized;
        UnityEngine.Debug.Log($"<color={(servicesReady ? "green" : "red")}>[Managers] Unity Services: {(servicesReady ? "✅ 준비됨" : "❌ 미준비")}</color>");

        // Authentication 서비스 확인
        bool authReady = _authManager != null;
        UnityEngine.Debug.Log($"<color={(authReady ? "green" : "red")}>[Managers] Authentication: {(authReady ? "✅ 준비됨" : "❌ 미준비")}</color>");

        // NetworkManager 확인
        bool networkReady = _networkManager != null;
        UnityEngine.Debug.Log($"<color={(networkReady ? "green" : "red")}>[Managers] NetworkManager: {(networkReady ? "✅ 준비됨" : "❌ 미준비")}</color>");

        // ConnectionManager 확인
        bool connectionReady = _connectionManager != null;
        UnityEngine.Debug.Log($"<color={(connectionReady ? "green" : "red")}>[Managers] ConnectionManager: {(connectionReady ? "✅ 준비됨" : "❌ 미준비")}</color>");

        // Lobby 서비스 확인
        bool lobbyReady = _lobbyServiceFacade != null && _localLobby != null && _localUser != null;
        UnityEngine.Debug.Log($"<color={(lobbyReady ? "green" : "red")}>[Managers] Lobby System: {(lobbyReady ? "✅ 준비됨" : "❌ 미준비")}</color>");

        // Session 관리 확인
        bool sessionReady = _sessionManager != null;
        UnityEngine.Debug.Log($"<color={(sessionReady ? "green" : "red")}>[Managers] Session Manager: {(sessionReady ? "✅ 준비됨" : "❌ 미준비")}</color>");

        // 전체 멀티플레이어 준비 상태
        bool allReady = servicesReady && authReady && networkReady && connectionReady && lobbyReady && sessionReady;
        
        if (allReady)
        {
            UnityEngine.Debug.Log("<color=green>[Managers] 🎉 멀티플레이어 시스템 모든 준비 완료!</color>");
            UnityEngine.Debug.Log("<color=lime>[Managers] 📡 로비 생성/참가 가능</color>");
            UnityEngine.Debug.Log("<color=lime>[Managers] 🔗 클라이언트/호스트 연결 가능</color>");
            UnityEngine.Debug.Log("<color=lime>[Managers] 💾 세션 데이터 동기화 가능</color>");
        }
        else
        {
            UnityEngine.Debug.LogWarning("<color=orange>[Managers] ⚠️ 일부 멀티플레이어 기능이 준비되지 않았습니다</color>");
        }

        // 인터넷 연결 상태 확인
        var internetReachability = Application.internetReachability;
        string connectionStatus = internetReachability switch
        {
            NetworkReachability.NotReachable => "<color=red>❌ 인터넷 연결 없음</color>",
            NetworkReachability.ReachableViaCarrierDataNetwork => "<color=green>✅ 모바일 데이터 연결</color>",
            NetworkReachability.ReachableViaLocalAreaNetwork => "<color=green>✅ WiFi/LAN 연결</color>",
            _ => "<color=yellow>⚠️ 연결 상태 불명</color>"
        };
        UnityEngine.Debug.Log($"<color=cyan>[Managers] 🌐 인터넷 상태: {connectionStatus}</color>");
        
        return Task.CompletedTask;
    }

    private void Update()
    {
        PublishAction(ActionId.System_Update);
    }

    private void LateUpdate()
    {
        PublishAction(ActionId.System_LateUpdate);
    }

    private void FixedUpdate()
    {
        PublishAction(ActionId.System_FixedUpdate);
    }

    private void OnDestroy()
    {
        _stateMachine?.Dispose();
        _actionDispatcher?.Dispose();
        _actionBus?.Dispose();
    }

    #region Public Methods
    public static IDisposable Subscribe(ActionId actionId, Action handler)
    {
        return ActionBus?.Subscribe(actionId, handler);
    }

    public static IDisposable Subscribe(ActionId actionId, Action<ActionMessage> handler)
    {
        return ActionBus?.Subscribe(actionId, handler);
    }

    public static IDisposable SubscribeMultiple(Action<ActionMessage> handler, params ActionId[] actionIds)
    {
        return ActionBus?.Subscribe(handler, actionIds);
    }

    public static void RegisterAction(IAction action)
    {
        Instance?._actionDispatcher?.Register(action);
    }

    public static void UnregisterAction(IAction action)
    {
        Instance?._actionDispatcher?.Unregister(action);
    }

    public static void PublishAction(ActionId actionId)
    {
        ActionBus?.Publish(ActionMessage.From(actionId));
    }

    public static void PublishAction(ActionId actionId, IActionPayload payload)
    {
        ActionBus?.Publish(ActionMessage.From(actionId, payload));
    }

    public static void RegisterState(IState state)
    {
        Instance?._stateMachine?.RegisterState(state);
    }

    public static void SetState(StateId stateId)
    {
        Instance?._stateMachine?.SetState(stateId);
    }

    public static StateId CurrentStateId
    {
        get { return Instance?._stateMachine?.CurrentId ?? StateId.None; }
    }
    #endregion

}

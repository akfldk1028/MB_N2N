using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Assets.Scripts.Network.Interfaces;
using Unity.Netcode;
using UnityEngine;

    /// <summary>
    /// 앱의 연결 모드를 정의하는 열거형
    /// </summary>
    public enum ConnectionMode
    {
        OfflineOnly,    // 오프라인 전용
        OnlineRequired, // 온라인 필수
        Hybrid          // 혼합 모드
    }
    /// <summary>
    /// 네트워크 연결 상태를 나타내는 열거형
    /// 클라이언트와 호스트의 다양한 연결 상태를 정의
    /// </summary>
    public enum ConnectStatus
    {
        Undefined,               // 초기 상태
        Success,                // 연결 성공
        ServerFull,            // 서버가 가득 참
        LoggedInAgain,         // 다른 곳에서 로그인됨
        UserRequestedDisconnect, // 사용자가 연결 종료 요청
        GenericDisconnect,     // 일반적인 연결 종료
        Reconnecting,          // 재연결 시도 중
        IncompatibleBuildType, // 빌드 타입 불일치
        HostEndedSession,      // 호스트가 세션 종료
        StartHostFailed,       // 호스트 시작 실패
        StartClientFailed,      // 클라이언트 시작 실패
          Disconnected,
        Connecting,
        Connected,
        Failed,

    }

    /// <summary>
    /// 재연결 시도 정보를 담는 구조체
    /// 현재 시도 횟수와 최대 시도 횟수를 포함
    /// </summary>
    public struct ReconnectMessage
    {
        public int CurrentAttempt;  // 현재 재연결 시도 횟수
        public int MaxAttempt;      // 최대 재연결 시도 횟수

        public ReconnectMessage(int currentAttempt, int maxAttempt)
        {
            CurrentAttempt = currentAttempt;
            MaxAttempt = maxAttempt;
        }
    }


/// <summary>
/// 네트워크 연결 관리자 클래스 - VContainer 의존성을 제거하고 Managers 패턴으로 리팩토링됨
/// Unity NGO(Netcode for GameObjects)를 사용한 네트워크 연결 관리
/// 상태 패턴을 사용하여 다양한 연결 상태 처리
///
/// NetworkBehaviour를 상속받아 RPC 기능을 제공하며, 상태 패턴과 통합되어 있습니다.
/// 각 상태 클래스는 이 클래스의 RPC 메서드를 간접적으로 호출하여 네트워크 통신을 수행합니다.
///
/// 주요 기능:
/// 1. 네트워크 연결 상태 관리 (상태 패턴)
/// 2. 서버-클라이언트 간 통신 (RPC)
/// 3. 연결 승인 및 재연결 처리
/// 4. 네트워크 이벤트 처리
/// </summary>

namespace Unity.Assets.Scripts.Network
{

    public class ConnectionManagerEx : NetworkBehaviour, IConnectionService
    {
        // 연결 상태 변경 이벤트
        /// <summary>
        /// 최대 연결 플레이어 수 (GameModeService에서 자동 관리)
        /// </summary>
        public int MaxConnectedPlayers => Managers.GameMode?.MaxPlayers ?? 1;

        public event System.Action<ConnectStatus> OnConnectionStatusChanged;

        /// <summary>
        /// 연결 상태 변경 이벤트를 발생시키는 public 메서드
        /// ConnectionStateEx 클래스에서 이벤트를 발생시킬 때 사용
        /// </summary>
        public void RaiseConnectionStatusChanged(ConnectStatus status)
        {
            OnConnectionStatusChanged?.Invoke(status);
        }

        [SerializeField]
        private ConnectionMode m_ConnectionMode = ConnectionMode.OnlineRequired;  // 기본값은 혼합 모드

        // 현재 연결 상태를 관리하는 상태 객체
        ConnectionStateEx m_CurrentState;

        // 의존성 참조 - Managers를 통해 획득
        private DebugClassFacadeEx m_DebugClassFacade;
        private NetworkManager m_NetworkManager;

        // NetworkManager public 접근자 - ConnectionStateEx에서 사용
        public new NetworkManager NetworkManager => m_NetworkManager;

        [SerializeField]
        int m_NbReconnectAttempts = 2;
        public int NbReconnectAttempts => m_NbReconnectAttempts;

        // 상태 패턴을 위한 상태 객체들
        internal readonly OfflineStateEx m_Offline = new OfflineStateEx();
        internal readonly LobbyConnectingStateEx m_LobbyConnecting = new LobbyConnectingStateEx();
        internal readonly ClientConnectingStateEx m_ClientConnecting = new ClientConnectingStateEx();
        internal readonly ClientConnectedStateEx m_ClientConnected = new ClientConnectedStateEx();
        internal readonly ClientReconnectingStateEx m_ClientReconnecting = new ClientReconnectingStateEx();
        internal readonly StartingHostStateEx m_StartingHost = new StartingHostStateEx();
        internal readonly HostingStateEx m_Hosting = new HostingStateEx();

        // 네트워크 이벤트 구독/해제 상태 추적
        private bool m_IsNetworkCallbacksRegistered = false;

        // 연결 상태 체크 - UpdateRunner 사용
        private bool m_IsConnectionStatusCheckActive = false;

        void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        void Start()
        {
            // Managers 초기화 대기
            StartCoroutine(WaitForManagersAndInitialize());
        }

        private IEnumerator WaitForManagersAndInitialize()
        {
            Debug.Log("<color=yellow>[ConnectionManagerEx] 🕐 Managers 초기화 대기 중...</color>");
            
            // Managers가 완전히 초기화될 때까지 대기
            while (Managers.Network == null || Managers.Debug == null)
            {
                yield return null;
            }

            Debug.Log("<color=green>[ConnectionManagerEx] ✅ Managers 초기화 완료, ConnectionManager 시작</color>");

            // Managers를 통해 NetworkManager 참조 획득
            m_NetworkManager = Managers.Network;
            if (m_NetworkManager == null)
            {
                Debug.LogError("<color=red>[ConnectionManagerEx] ❌ Managers.Network가 초기화되지 않음</color>");
                yield break;
            }

            // DebugClassFacadeEx를 Managers에서 가져오기
            m_DebugClassFacade = Managers.Debug;

            Debug.Log("<color=cyan>[ConnectionManagerEx] 🔧 네트워크 매니저 초기화 시작</color>");
            List<ConnectionStateEx> states = new()
            {
                m_Offline,
                m_ClientConnecting,
                m_ClientConnected,
                m_ClientReconnecting,
                m_StartingHost,
                m_Hosting,
                m_LobbyConnecting
            };

            Debug.Log("<color=magenta>[ConnectionManagerEx] 📊 초기 상태 설정: Offline</color>");
            m_CurrentState = m_Offline;

            // 이벤트 핸들러 등록
            RegisterNetworkCallbacks();

            // ConnectionState 상태 객체들에 종속성 주입 - VContainer 대신 직접 설정
            foreach (var connectionState in states)
            {
                connectionState.Initialize(this);
            }

            // 연결 상태 체크 코루틴 시작
            StartConnectionStatusCheck();

            Debug.Log("[ConnectionManagerEx] 종료: Start");
        }

        // 네트워크 콜백 등록을 별도 메서드로 분리
        private void RegisterNetworkCallbacks()
        {
            if (m_IsNetworkCallbacksRegistered)
            {
                Debug.Log("[ConnectionManagerEx] 네트워크 이벤트 핸들러가 이미 등록되어 있습니다.");
                return;
            }

            Debug.Log("[ConnectionManagerEx] 네트워크 이벤트 핸들러 등록 시작");
            
            // NetworkConfig는 Managers.cs에서 이미 설정됨 - 확인만 수행
            if (!NetworkManager.NetworkConfig.ConnectionApproval)
            {
                GameLogger.Warning("ConnectionManagerEx", "ConnectionApproval이 비활성화되어 있습니다");
            }
            if (!NetworkManager.NetworkConfig.EnableSceneManagement)
            {
                GameLogger.Warning("ConnectionManagerEx", "SceneManagement가 비활성화되어 있습니다");
            }
            
            // 이벤트 핸들러 등록
            NetworkManager.OnClientConnectedCallback += OnClientConnectedCallback;
            Debug.Log("[ConnectionManagerEx] OnClientConnectedCallback 핸들러 등록됨");
            NetworkManager.ConnectionApprovalCallback += ApprovalCheck;

            NetworkManager.OnClientDisconnectCallback += OnClientDisconnectCallback;
            Debug.Log("[ConnectionManagerEx] OnClientDisconnectCallback 핸들러 등록됨");

            NetworkManager.OnServerStarted += OnServerStarted;
            Debug.Log("[ConnectionManagerEx] OnServerStarted 핸들러 등록됨");

            NetworkManager.OnTransportFailure += OnTransportFailure;
            NetworkManager.OnServerStopped += OnServerStopped;

            Debug.Log("[ConnectionManagerEx] 네트워크 이벤트 핸들러 등록 완료");

            m_IsNetworkCallbacksRegistered = true;
        }

        private void UnregisterNetworkCallbacks()
        {
            if (!m_IsNetworkCallbacksRegistered)
                return;

            NetworkManager.OnClientConnectedCallback -= OnClientConnectedCallback;
            NetworkManager.OnClientDisconnectCallback -= OnClientDisconnectCallback;
            NetworkManager.OnServerStarted -= OnServerStarted;
            NetworkManager.OnTransportFailure -= OnTransportFailure;
            NetworkManager.OnServerStopped -= OnServerStopped;
            NetworkManager.ConnectionApprovalCallback -= ApprovalCheck;

            m_IsNetworkCallbacksRegistered = false;
            Debug.Log("[ConnectionManagerEx] 네트워크 이벤트 핸들러 등록 해제");
        }

        // ✅ UpdateRunner를 사용한 연결 상태 주기적 체크
        private void StartConnectionStatusCheck()
        {
            if (!m_IsConnectionStatusCheckActive)
            {
                GameLogger.Info("ConnectionManagerEx", "🔄 연결 상태 모니터링 시작 (UpdateRunner, 5초 주기)");
                Managers.UpdateRunner.Subscribe(ConnectionStatusCheck, 5.0f);
                m_IsConnectionStatusCheckActive = true;
            }
        }

        private void ConnectionStatusCheck(float deltaTime)
        {
            if (NetworkManager == null) return;

            bool isClient = NetworkManager.IsClient;
            bool isConnected = NetworkManager.IsConnectedClient;
            bool isHost = NetworkManager.IsHost;
            bool isServer = NetworkManager.IsServer;
            string currentStateName = m_CurrentState?.GetType().Name ?? "null";

            // ✅ Offline 상태일 때만 로그 출력 (불필요한 로그 제거)
            if (m_CurrentState is OfflineStateEx)
            {
                GameLogger.DevLog("ConnectionManagerEx", 
                    $"📊 상태: {currentStateName} | 인터넷: {Application.internetReachability} | " +
                    $"Netcode: Client={isClient}, Connected={isConnected}, Host={isHost}, Server={isServer}");
            }
            // 연결 중이거나 연결됨 상태는 간략하게
            else if (isClient || isHost || isServer)
            {
                GameLogger.DevLog("ConnectionManagerEx", 
                    $"✅ 활성: {currentStateName} | Client={isClient}, Host={isHost}, Clients={NetworkManager.ConnectedClientsIds.Count}명");
            }

            // 상태와 실제 연결 상태의 불일치 감지
            if (isClient && isConnected && m_CurrentState is ClientConnectingStateEx)
            {
                GameLogger.Warning("ConnectionManagerEx", "⚠️ 연결 완료 but 상태=ClientConnecting → ClientConnected로 전환");
                ChangeState(m_ClientConnected);
            }

            // 콜백 등록 확인 및 필요시 재등록
            if (!m_IsNetworkCallbacksRegistered)
            {
                GameLogger.Warning("ConnectionManagerEx", "⚠️ 네트워크 콜백 미등록 → 재등록 시도");
                RegisterNetworkCallbacks();
            }
        }

        public override void OnDestroy()
        {
            UnregisterNetworkCallbacks();

            // ✅ UpdateRunner 구독 해제
            if (m_IsConnectionStatusCheckActive)
            {
                Managers.UpdateRunner?.Unsubscribe(ConnectionStatusCheck);
                m_IsConnectionStatusCheckActive = false;
                GameLogger.Info("ConnectionManagerEx", "🔄 연결 상태 모니터링 종료");
            }
            
            base.OnDestroy();
        }

        internal void ChangeState(ConnectionStateEx nextState)
        {
            Debug.Log($"[ConnectionManagerEx] Changed connection state from {m_CurrentState?.GetType().Name} to {nextState.GetType().Name}.");

            if (m_CurrentState != null)
            {
                m_CurrentState.Exit();
            }
            m_CurrentState = nextState;
            m_CurrentState.Enter();
        }

        // NGO 이벤트 핸들러들
        void OnClientDisconnectCallback(ulong clientId)
        {
            m_CurrentState.OnClientDisconnect(clientId);
        }

        void OnClientConnectedCallback(ulong clientId)
        {
            m_CurrentState.OnClientConnected(clientId);
            
            // 랜덤 매칭: Host가 필요한 인원 달성 시 즉시 게임 시작
            int requiredPlayers = Managers.GameMode?.MinPlayersToStart ?? MaxConnectedPlayers;
            int currentPlayers = NetworkManager.ConnectedClientsIds.Count;
            
            if (NetworkManager.IsHost && currentPlayers >= requiredPlayers)
            {
                GameLogger.Success("ConnectionManagerEx", $"매칭 완료! (현재 {currentPlayers}명 / 필요 {requiredPlayers}명) 즉시 게임 시작");
                StartGame();
            }
            else if (NetworkManager.IsHost)
            {
                GameLogger.Info("ConnectionManagerEx", $"매칭 대기 중... (현재 {currentPlayers}명 / 필요 {requiredPlayers}명)");
            }
        }
        
        /// <summary>
        /// 게임 시작 (Host만 호출, NetworkManager SceneManagement가 모든 Client 동기화)
        /// </summary>
        private void StartGame()
        {
            if (!NetworkManager.IsHost)
            {
                GameLogger.Warning("ConnectionManagerEx", "Host가 아니므로 게임 시작 불가");
                return;
            }
            
            // ✅ SessionManager에 세션 시작 알림
            Managers.Session.OnSessionStarted();
            GameLogger.Success("ConnectionManagerEx", "세션 시작됨 - 이제 플레이어 재연결 데이터 유지");
            
            GameLogger.Network("ConnectionManagerEx", $"Host가 게임 씬 로드 시작 (Client 자동 동기화)");
            NetworkManager.SceneManager.LoadScene(Define.EScene.GameScene.ToString(), UnityEngine.SceneManagement.LoadSceneMode.Single);
        }
        void OnServerStarted()
        {
            Debug.Log("[ConnectionManagerEx] 매치 서버 시작됨");

            if (m_CurrentState != null)
            {
                m_CurrentState.OnServerStarted();
            }
            else
            {
                Debug.LogError("[ConnectionManagerEx] m_CurrentState가 null입니다 - OnServerStarted 처리 불가");
            }
        }

       void OnTransportFailure()
       {
           m_CurrentState.OnTransportFailure();
       }

        void OnServerStopped(bool _)
        {
            m_CurrentState.OnServerStopped();
        }

        public async Task<bool> CheckNetworkStatusAsync()
        {
            try
            {
                // 1. 인터넷 연결 상태 확인
                bool isOnline = Application.internetReachability != NetworkReachability.NotReachable;
                if (!isOnline)
                {
                    Debug.LogWarning("[ConnectionManagerEx] 인터넷 연결이 없습니다.");
                    return false;
                }

                // 2. 현재 연결 상태 확인
                if (m_CurrentState is OfflineStateEx)
                {
                    // 오프라인 상태인 경우, 로비 연결 상태로 전환
                    Debug.Log("[ConnectionManagerEx] 오프라인 상태에서 로비 연결 상태로 전환");
                    ChangeState(m_LobbyConnecting);
                    await System.Threading.Tasks.Task.Delay(1000); // 상태 전환 대기
                    return m_CurrentState is not OfflineStateEx;
                }

                // 3. 현재 상태가 오프라인이 아닌 경우
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[ConnectionManagerEx] 네트워크 상태 확인 중 오류 발생: {e.Message}");
                return false;
            }
        }

        public void StartClientLobby(string playerName)
        {
            m_CurrentState.StartClientLobby(playerName);
        }

        public void StartHostLobby(string playerName)
        {
            m_CurrentState.StartHostLobby(playerName);
        }

      // 추가: 연결 승인 콜백
       void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
       {
           m_CurrentState.ApprovalCheck(request, response);
       }

        public void RequestShutdown()
        {
            m_CurrentState.OnUserRequestedShutdown();
        }

        [ClientRpc]
        public void LoadSceneClientRpc(string sceneName)
        {
            Debug.Log($"[ConnectionManagerEx] 씬 전환 RPC 수신: {sceneName}");

            // 클라이언트는 이 RPC를 수신만 하고 직접 씬을 로드하지 않음
            // 서버가 NetworkSceneManager를 통해 씬을 로드하면 자동으로 클라이언트에도 적용됨
            Debug.Log($"[ConnectionManagerEx] 씬 전환 RPC 수신 완료. 서버의 NetworkSceneManager 씬 전환을 기다립니다.");
        }

        #region IConnectionService Implementation

        public bool IsInitialized { get; private set; } = false;

        public async Task InitializeAsync()
        {
            if (!IsInitialized)
            {
                await Task.Yield(); // Start에서 초기화됨
                IsInitialized = true;
            }
        }

        public void Shutdown()
        {
            RequestShutdown();
        }

        public void StartClient(string playerName)
        {
            StartClientLobby(playerName);
        }

        public void StartHost(string playerName)
        {
            StartHostLobby(playerName);
        }

        public void Disconnect()
        {
            RequestShutdown();
        }

        /// <summary>
        /// MPPM 로컬 테스트용 직접 Host 시작
        /// Unity Services 인증 없이 127.0.0.1:7778로 직접 호스팅
        /// ConnectionPayload를 설정하여 ApprovalCheck 통과
        /// </summary>
        public void StartHostDirect(string playerName)
        {
            GameLogger.Progress("ConnectionManagerEx", $"[MPPM] 직접 Host 시작: {playerName}");

            if (m_NetworkManager == null)
            {
                GameLogger.Error("ConnectionManagerEx", "[MPPM] NetworkManager가 아직 초기화되지 않음");
                return;
            }

            // UnityTransport 설정 — Host는 0.0.0.0에서 리스닝
            var transport = m_NetworkManager.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
            if (transport == null)
                transport = UnityEngine.Object.FindAnyObjectByType<Unity.Netcode.Transports.UTP.UnityTransport>();
            if (transport != null)
            {
                transport.SetConnectionData("127.0.0.1", 7778, "0.0.0.0");
                GameLogger.Info("ConnectionManagerEx", "[MPPM] Transport 설정: Listen 0.0.0.0:7778");
            }
            else
            {
                GameLogger.Error("ConnectionManagerEx", "[MPPM] UnityTransport를 찾을 수 없음!");
            }

            // ConnectionPayload 설정 (ApprovalCheck 통과용)
            string playerId = System.Guid.NewGuid().ToString();
            var payload = JsonUtility.ToJson(new ConnectionPayload(
                playerName,
                playerId,
                1,
                UnityEngine.Debug.isDebugBuild
            ));
            NetworkManager.NetworkConfig.ConnectionData = System.Text.Encoding.UTF8.GetBytes(payload);

            // 상태를 StartingHost로 전환 후 직접 StartHost 호출
            m_ConnectionManager_DirectStartHost(playerName, playerId);
        }

        private void m_ConnectionManager_DirectStartHost(string playerName, string playerId)
        {
            try
            {
                // 현재 상태를 Exit 처리 후 m_Hosting으로 직접 이동할 준비
                // OnServerStarted 이벤트가 발생하면 m_CurrentState.OnServerStarted()가 호출됨
                // m_Offline.OnServerStarted()는 빈 메서드이므로, 직접 m_StartingHost 상태로 설정
                // (단, Enter()는 호출하지 않음 - ConnectionMethod 없이 직접 StartHost 호출)
                m_CurrentState?.Exit();
                m_CurrentState = m_StartingHost; // OnServerStarted에서 m_Hosting으로 전환하기 위해

                // MPPM 로컬 테스트: ForceSamePrefabs 비활성화 (런타임 프리팹 등록 허용)
                NetworkManager.NetworkConfig.ForceSamePrefabs = false;
                GameLogger.Info("ConnectionManagerEx", "[MPPM] ForceSamePrefabs 비활성화");

                bool started = NetworkManager.StartHost();
                if (started)
                {
                    GameLogger.Success("ConnectionManagerEx", $"[MPPM] Host 직접 시작 성공: {playerName}");
                    // OnServerStarted 이벤트 → m_StartingHost.OnServerStarted() → ChangeState(m_Hosting)
                }
                else
                {
                    GameLogger.Error("ConnectionManagerEx", "[MPPM] Host 직접 시작 실패");
                    ChangeState(m_Offline);
                }
            }
            catch (Exception e)
            {
                GameLogger.Error("ConnectionManagerEx", $"[MPPM] Host 직접 시작 중 오류: {e.Message}");
                ChangeState(m_Offline);
            }
        }

        /// <summary>
        /// MPPM 로컬 테스트용 직접 Client 시작
        /// Unity Services 인증 없이 127.0.0.1:7778로 직접 연결
        /// ConnectionPayload를 설정하여 ApprovalCheck 통과
        /// </summary>
        public void StartClientDirect(string playerName)
        {
            GameLogger.Progress("ConnectionManagerEx", $"[MPPM] 직접 Client 시작: {playerName}");

            if (m_NetworkManager == null)
            {
                GameLogger.Error("ConnectionManagerEx", "[MPPM] NetworkManager가 아직 초기화되지 않음");
                return;
            }

            // UnityTransport 설정 — Client는 127.0.0.1:7778로 연결
            var transport = m_NetworkManager.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
            if (transport == null)
                transport = UnityEngine.Object.FindAnyObjectByType<Unity.Netcode.Transports.UTP.UnityTransport>();
            if (transport != null)
            {
                transport.SetConnectionData("127.0.0.1", 7778);
                GameLogger.Info("ConnectionManagerEx", "[MPPM] Transport 설정: Connect 127.0.0.1:7778");
            }
            else
            {
                GameLogger.Error("ConnectionManagerEx", "[MPPM] UnityTransport를 찾을 수 없음!");
            }

            // ConnectionPayload 설정 (ApprovalCheck 통과용)
            string playerId = System.Guid.NewGuid().ToString();
            var payload = JsonUtility.ToJson(new ConnectionPayload(
                playerName,
                playerId,
                1,
                UnityEngine.Debug.isDebugBuild
            ));
            NetworkManager.NetworkConfig.ConnectionData = System.Text.Encoding.UTF8.GetBytes(payload);

            try
            {
                // 이전 연결 상태 정리 — Shutdown 필요 시 호출자에서 대기 후 재호출
                if (NetworkManager.IsListening)
                {
                    GameLogger.Info("ConnectionManagerEx", "[MPPM] 기존 연결 활성 — Shutdown 후 재시도 필요");
                    NetworkManager.Shutdown();
                    // Shutdown 직후 StartClient 호출하면 실패함 — 호출자에서 대기 후 재시도
                    return;
                }

                // Transport 설정
                if (transport != null)
                {
                    transport.SetConnectionData("127.0.0.1", 7778);
                    GameLogger.Info("ConnectionManagerEx", "[MPPM] Transport 설정: Connect 127.0.0.1:7778");
                }

                // MPPM 로컬 테스트: ForceSamePrefabs 비활성화
                NetworkManager.NetworkConfig.ForceSamePrefabs = false;

                m_CurrentState?.Exit();
                m_CurrentState = m_ClientConnecting;
                bool started = NetworkManager.StartClient();
                if (started)
                {
                    GameLogger.Success("ConnectionManagerEx", $"[MPPM] Client 직접 시작 성공: {playerName}");
                }
                else
                {
                    GameLogger.Error("ConnectionManagerEx", $"[MPPM] Client 직접 시작 실패 (IsListening={NetworkManager.IsListening}, Transport={transport != null})");
                    ChangeState(m_Offline);
                }
            }
            catch (Exception e)
            {
                GameLogger.Error("ConnectionManagerEx", $"[MPPM] Client 직접 시작 중 오류: {e.Message}");
                ChangeState(m_Offline);
            }
        }

        public ConnectStatus CurrentStatus
        {
            get
            {
                if (m_CurrentState == null) return ConnectStatus.Undefined;

                if (m_CurrentState is OfflineStateEx) return ConnectStatus.Disconnected;
                if (m_CurrentState is ClientConnectingStateEx) return ConnectStatus.Connecting;
                if (m_CurrentState is ClientConnectedStateEx) return ConnectStatus.Connected;
                if (m_CurrentState is HostingStateEx) return ConnectStatus.Success;
                if (m_CurrentState is ClientReconnectingStateEx) return ConnectStatus.Reconnecting;

                return ConnectStatus.Undefined;
            }
        }

        public bool IsConnected => m_NetworkManager != null && m_NetworkManager.IsConnectedClient;

        public new bool IsHost => m_NetworkManager != null && m_NetworkManager.IsHost;

        public new bool IsClient => m_NetworkManager != null && m_NetworkManager.IsClient;

        #endregion
    }
}
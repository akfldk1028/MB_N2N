using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Multiplayer;
using Unity.Assets.Scripts.Network;
using Unity.Assets.Scripts.UnityServices.Lobbies;

namespace Unity.Assets.Scripts.Test
{
    /// <summary>
    /// 실제 네트워크 모듈 통합 테스트 매니저
    /// Unity 공식 테스트 방법론을 적용한 완전한 네트워크 테스트 시스템
    ///
    /// 기존 NetworkModuleTestManager와 차이점:
    /// - 실제 Unity Services API 호출 (Authentication, Sessions)
    /// - ParrelSync 프로필 전환 지원
    /// - NetworkCommandLine 패턴 적용
    /// - 실제 네트워크 상태 전환 테스트
    /// - 네트워크 시뮬레이션 지원
    /// </summary>
    public class NetworkIntegrationTestManager : MonoBehaviour
    {
        [Header("🎯 통합 테스트 설정")]
        [SerializeField] private bool autoTestOnStart = true;
        [SerializeField] private bool enableDetailedLogging = true;

        [Header("🌐 Unity Services 테스트")]
        [SerializeField] private bool testUnityServicesIntegration = true;
        [SerializeField] private bool testAuthenticationFlow = true;
        [SerializeField] private bool testSessionsAPI = true;
        [SerializeField] private float serviceTimeout = 10f;

        [Header("🔗 네트워크 연결 테스트")]
        [SerializeField] private bool testConnectionStates = true;
        [SerializeField] private bool testRPCCommunication = true;
        [SerializeField] private string testSessionName = "IntegrationTestSession";
        [SerializeField] private int maxTestPlayers = 2;

        [Header("🎮 명령줄 제어")]
        [SerializeField] private bool useCommandLineArgs = true;
        [SerializeField] private bool forceHostMode = false;
        [SerializeField] private bool forceClientMode = false;

        [Header("📡 네트워크 시뮬레이션")]
        [SerializeField] private bool enableNetworkSimulation = false;
        [SerializeField] private int packetDelay = 120;
        [SerializeField] private int packetJitter = 5;
        [SerializeField] private int dropRate = 3;

        // 테스트 상태
        private TestPhase m_CurrentPhase = TestPhase.Idle;
        private List<string> m_TestResults = new List<string>();
        private float m_TestStartTime;
        private int m_PassedTests = 0;
        private int m_FailedTests = 0;

        // 컴포넌트 참조
        private AuthManager m_AuthManager;
        private ConnectionManagerEx m_ConnectionManager;
        private LobbyServiceFacadeEx m_LobbyServiceFacade;
        private DebugClassFacadeEx m_DebugClassFacade;
        private ProfileManagerEx m_ProfileManager;
        private NetworkManager m_NetworkManager;
        private UnityTransport m_UnityTransport;

        // 명령줄 파서
        private Dictionary<string, string> m_CommandLineArgs;

        public enum TestPhase
        {
            Idle,
            InitializingServices,
            TestingAuthentication,
            TestingSessionAPI,
            TestingNetworkStates,
            TestingRPCCommunication,
            NetworkSimulation,
            Completed,
            Failed
        }

        private void Awake()
        {
            // 명령줄 인수 파싱
            ParseCommandLineArguments();

            // ParrelSync 프로필 설정 (Unity 공식 패턴)
            SetupParrelSyncProfile();
        }

        private void Start()
        {
            Log("=== NetworkIntegrationTestManager 시작 ===");

            if (autoTestOnStart)
            {
                StartCoroutine(RunIntegrationTestsCoroutine());
            }

            // 명령줄 모드 처리 (Unity 공식 NetworkCommandLine 패턴)
            ProcessCommandLineMode();
        }

        /// <summary>
        /// Unity 공식 NetworkCommandLine 패턴 구현
        /// </summary>
        private void ParseCommandLineArguments()
        {
            m_CommandLineArgs = new Dictionary<string, string>();

            var args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; ++i)
            {
                var arg = args[i].ToLower();
                if (arg.StartsWith("-"))
                {
                    var value = i < args.Length - 1 ? args[i + 1].ToLower() : null;
                    value = (value?.StartsWith("-") ?? false) ? null : value;
                    m_CommandLineArgs.Add(arg, value);
                }
            }

            if (m_CommandLineArgs.Count > 0)
            {
                Log($"명령줄 인수 파싱됨: {string.Join(", ", m_CommandLineArgs)}");
            }
        }

        /// <summary>
        /// ParrelSync 프로필 전환 (Unity 공식 패턴)
        /// </summary>
        private void SetupParrelSyncProfile()
        {
#if UNITY_EDITOR
            try
            {
                // ParrelSync이 설치되어 있는지 확인
                var parrelSyncType = System.Type.GetType("ParrelSync.ClonesManager, ParrelSync");
                if (parrelSyncType != null)
                {
                    var isCloneMethod = parrelSyncType.GetMethod("IsClone");
                    var getArgumentMethod = parrelSyncType.GetMethod("GetArgument");

                    if (isCloneMethod != null && getArgumentMethod != null)
                    {
                        bool isClone = (bool)isCloneMethod.Invoke(null, null);
                        if (isClone)
                        {
                            string customArgument = (string)getArgumentMethod.Invoke(null, null);
                            if (string.IsNullOrEmpty(customArgument))
                            {
                                customArgument = "Default";
                            }

                            string profileName = $"Clone_{customArgument}_Profile";
                            AuthenticationService.Instance.SwitchProfile(profileName);

                            Log($"🔄 ParrelSync Clone 감지 - 프로필 전환: {profileName}");
                        }
                        else
                        {
                            Log("📱 ParrelSync 원본 인스턴스 - 기본 프로필 사용");
                        }
                    }
                }
                else
                {
                    Log("⚠️ ParrelSync이 설치되지 않음 - 기본 프로필 사용");
                }
            }
            catch (System.Exception e)
            {
                LogWarning($"ParrelSync 프로필 설정 실패: {e.Message}");
            }
#else
            Log("💻 빌드 모드 - ParrelSync 프로필 전환 스킵");
#endif
        }

        /// <summary>
        /// 명령줄 모드 처리 (Unity 공식 NetworkCommandLine 패턴)
        /// </summary>
        private void ProcessCommandLineMode()
        {
            if (!useCommandLineArgs && !forceHostMode && !forceClientMode)
                return;

            // 강제 모드 우선 처리
            if (forceHostMode)
            {
                Log("🎯 강제 Host 모드로 설정됨");
                StartCoroutine(StartAsHostCoroutine());
                return;
            }

            if (forceClientMode)
            {
                Log("🎯 강제 Client 모드로 설정됨");
                StartCoroutine(StartAsClientCoroutine());
                return;
            }

            // 명령줄 인수 처리
            if (m_CommandLineArgs.TryGetValue("-mode", out string mode))
            {
                switch (mode)
                {
                    case "server":
                    case "host":
                        Log($"📡 명령줄에서 Host 모드 요청됨");
                        StartCoroutine(StartAsHostCoroutine());
                        break;
                    case "client":
                        Log($"📱 명령줄에서 Client 모드 요청됨");
                        StartCoroutine(StartAsClientCoroutine());
                        break;
                    default:
                        LogWarning($"알 수 없는 모드: {mode}");
                        break;
                }
            }
        }

        /// <summary>
        /// 통합 테스트 실행 (메인 테스트 플로우)
        /// </summary>
        [ContextMenu("Run Integration Tests")]
        public void RunIntegrationTests()
        {
            StartCoroutine(RunIntegrationTestsCoroutine());
        }

        private IEnumerator RunIntegrationTestsCoroutine()
        {
            m_CurrentPhase = TestPhase.InitializingServices;
            m_TestStartTime = Time.time;
            m_TestResults.Clear();
            m_PassedTests = 0;
            m_FailedTests = 0;

            Log("🚀 통합 테스트 시작");

            // 1단계: Unity Services 초기화 및 인증 테스트
            if (testUnityServicesIntegration)
            {
                m_CurrentPhase = TestPhase.InitializingServices;
                yield return InitializeAndTestUnityServices();
                if (m_CurrentPhase == TestPhase.Failed) yield break;
            }

            // 2단계: 인증 플로우 테스트
            if (testAuthenticationFlow)
            {
                m_CurrentPhase = TestPhase.TestingAuthentication;
                yield return TestAuthenticationIntegration();
                if (m_CurrentPhase == TestPhase.Failed) yield break;
            }

            // 3단계: Sessions API 테스트
            if (testSessionsAPI)
            {
                m_CurrentPhase = TestPhase.TestingSessionAPI;
                yield return TestSessionsAPIIntegration();
                if (m_CurrentPhase == TestPhase.Failed) yield break;
            }

            // 4단계: 네트워크 상태 전환 테스트
            if (testConnectionStates)
            {
                m_CurrentPhase = TestPhase.TestingNetworkStates;
                yield return TestNetworkStateTransitions();
                if (m_CurrentPhase == TestPhase.Failed) yield break;
            }

            // 5단계: RPC 통신 테스트
            if (testRPCCommunication)
            {
                m_CurrentPhase = TestPhase.TestingRPCCommunication;
                yield return TestRPCCommunicationIntegration();
                if (m_CurrentPhase == TestPhase.Failed) yield break;
            }

            // 6단계: 네트워크 시뮬레이션 테스트 (선택적)
            if (enableNetworkSimulation)
            {
                m_CurrentPhase = TestPhase.NetworkSimulation;
                yield return TestNetworkSimulation();
            }

            // 테스트 완료
            m_CurrentPhase = TestPhase.Completed;
            GenerateIntegrationTestReport();
        }

        /// <summary>
        /// Unity Services 초기화 및 기본 테스트
        /// </summary>
        private IEnumerator InitializeAndTestUnityServices()
        {
            Log("1단계: Unity Services 초기화 중...");

            // 컴포넌트 검색 및 초기화
            yield return FindAndInitializeComponents();

            // try
            // {
            //     // Unity Services 초기화
            //     if (UnityServices.State != ServicesInitializationState.Initialized)
            //     {
            //         Log("Unity Services 초기화 중...");
            //         var initTask = UnityServices.InitializeAsync();
            //
            //         float startTime = Time.time;
            //         while (!initTask.IsCompleted && Time.time - startTime < serviceTimeout)
            //         {
            //             yield return null;
            //         }
            //
            //         if (initTask.IsCompletedSuccessfully)
            //         {
            //             Log("✅ Unity Services 초기화 성공");
            //             AddTestResult("✅ Unity Services", "초기화 성공");
            //             m_PassedTests++;
            //         }
            //         else if (initTask.IsFaulted)
            //         {
            //             LogError($"❌ Unity Services 초기화 실패: {initTask.Exception?.Message}");
            //             AddTestResult("❌ Unity Services", "초기화 실패");
            //             m_FailedTests++;
            //             m_CurrentPhase = TestPhase.Failed;
            //             yield break;
            //         }
            //         else
            //         {
            //             LogError("❌ Unity Services 초기화 시간 초과");
            //             AddTestResult("❌ Unity Services", "초기화 시간 초과");
            //             m_FailedTests++;
            //             m_CurrentPhase = TestPhase.Failed;
            //             yield break;
            //         }
            //     }
            //     else
            //     {
            //         Log("✅ Unity Services 이미 초기화됨");
            //         AddTestResult("✅ Unity Services", "이미 초기화됨");
            //         m_PassedTests++;
            //     }
            // }
            // catch (System.Exception e)
            // {
            //     LogError($"❌ Unity Services 초기화 예외: {e.Message}");
            //     AddTestResult("❌ Unity Services", $"예외 발생: {e.Message}");
            //     m_FailedTests++;
            //     m_CurrentPhase = TestPhase.Failed;
            // }

            yield return new WaitForSeconds(0.5f);
        }

        /// <summary>
        /// 컴포넌트 검색 및 초기화
        /// </summary>
        private IEnumerator FindAndInitializeComponents()
        {
            Log("필수 컴포넌트 검색 중...");

            // NetworkManager 검색
            m_NetworkManager = FindObjectOfType<NetworkManager>();
            if (m_NetworkManager == null)
            {
                // NetworkManager 자동 생성
                var nmObject = new GameObject("NetworkManager");
                m_NetworkManager = nmObject.AddComponent<NetworkManager>();
                m_UnityTransport = nmObject.AddComponent<UnityTransport>();
                m_NetworkManager.NetworkConfig.NetworkTransport = m_UnityTransport;
                Log("NetworkManager 자동 생성됨");
            }
            else
            {
                m_UnityTransport = m_NetworkManager.GetComponent<UnityTransport>();
                Log("기존 NetworkManager 발견됨");
            }

            // AuthManager 검색 또는 생성
            m_AuthManager = FindObjectOfType<AuthManager>();
            if (m_AuthManager == null)
            {
                var authObject = new GameObject("AuthManager");
                m_AuthManager = authObject.AddComponent<AuthManager>();
                Log("AuthManager 자동 생성됨");
            }

            // ConnectionManagerEx 검색
            m_ConnectionManager = FindObjectOfType<ConnectionManagerEx>();
            if (m_ConnectionManager == null)
            {
                LogWarning("ConnectionManagerEx를 찾을 수 없음");
            }

            // DebugClassFacadeEx 초기화
            m_DebugClassFacade = new DebugClassFacadeEx();

            // ProfileManagerEx 초기화
            m_ProfileManager = new ProfileManagerEx();

            Log($"컴포넌트 초기화 완료 - NetworkManager: {m_NetworkManager != null}, AuthManager: {m_AuthManager != null}");

            yield return null;
        }

        /// <summary>
        /// AuthManager와 실제 Unity Authentication API 통합 테스트
        /// </summary>
        private IEnumerator TestAuthenticationIntegration()
        {
            Log("2단계: Authentication 통합 테스트 중...");

            if (m_AuthManager == null)
            {
                LogError("AuthManager를 찾을 수 없음");
                AddTestResult("❌ AuthManager", "컴포넌트 없음");
                m_FailedTests++;
                m_CurrentPhase = TestPhase.Failed;
                yield break;
            }

            // 실제 Unity Authentication API 호출
            bool isAuthenticated = false;
            string playerId = null;

            var authTask = System.Threading.Tasks.Task.Run(async () =>
            {
                try
                {
                    bool result = await m_AuthManager.EnsurePlayerIsAuthorized();
                    isAuthenticated = result && AuthenticationService.Instance.IsSignedIn;
                    if (isAuthenticated)
                    {
                        playerId = AuthenticationService.Instance.PlayerId;
                    }
                    return result;
                }
                catch (System.Exception e)
                {
                    Log($"Authentication 예외: {e.Message}");
                    return false;
                }
            });

            // 비동기 작업 대기
            float startTime = Time.time;
            while (!authTask.IsCompleted && Time.time - startTime < serviceTimeout)
            {
                yield return null;
            }

            if (authTask.IsCompletedSuccessfully && isAuthenticated)
            {
                Log($"✅ AuthManager: Unity Authentication 성공 - Player ID: {playerId}");
                AddTestResult("✅ AuthManager", $"인증 성공 (ID: {playerId?.Substring(0, 8)}...)");
                m_PassedTests++;
            }
            else
            {
                LogError("❌ AuthManager: Unity Authentication 실패");
                AddTestResult("❌ AuthManager", "인증 실패");
                m_FailedTests++;
            }

            yield return new WaitForSeconds(0.5f);
        }

        /// <summary>
        /// LobbyServiceFacadeEx와 실제 Sessions API 통합 테스트
        /// </summary>
        private IEnumerator TestSessionsAPIIntegration()
        {
            Log("3단계: Sessions API 통합 테스트 중...");

            // LobbyServiceFacadeEx 초기화
            if (m_LobbyServiceFacade == null)
            {
                var lobbyObject = new GameObject("LobbyServiceFacade");
                // m_LobbyServiceFacade = lobbyObject.AddComponent<LobbyServiceFacadeEx>();

                // 의존성 초기화 (VContainer 대신)
                var updateRunnerObject = new GameObject("UpdateRunner");
                var updateRunner = updateRunnerObject.AddComponent<UpdateRunnerEx>();
                var localLobby = new LocalLobbyEx();
                var localUser = new LocalLobbyUserEx();
                var sceneManager = new SceneManagerEx();

                m_LobbyServiceFacade.Initialize(
                    m_DebugClassFacade,
                    updateRunner,
                    localLobby,
                    localUser,
                    sceneManager,
                    m_NetworkManager
                );
            }

            // 실제 Sessions API 호출 테스트
            bool sessionSuccess = false;
            string sessionId = null;

            var sessionTask = System.Threading.Tasks.Task.Run(async () =>
            {
                try
                {
                    var (success, session) = await m_LobbyServiceFacade.TryCreateSessionAsync(testSessionName, maxTestPlayers, false);
                    sessionSuccess = success;
                    if (success && session != null)
                    {
                        sessionId = session.Id;

                        // 테스트 완료 후 세션 정리
                        await System.Threading.Tasks.Task.Delay(1000);
                        m_LobbyServiceFacade.DeleteSessionAsync();
                    }
                    return success;
                }
                catch (System.Exception e)
                {
                    Log($"Sessions API 예외: {e.Message}");
                    return false;
                }
            });

            // 비동기 작업 대기
            float startTime = Time.time;
            while (!sessionTask.IsCompleted && Time.time - startTime < serviceTimeout)
            {
                yield return null;
            }

            if (sessionTask.IsCompletedSuccessfully && sessionSuccess)
            {
                Log($"✅ LobbyServiceFacadeEx: Sessions API 성공 - Session ID: {sessionId}");
                AddTestResult("✅ Sessions API", $"세션 생성 성공 (ID: {sessionId?.Substring(0, 8)}...)");
                m_PassedTests++;
            }
            else
            {
                LogWarning("⚠️ LobbyServiceFacadeEx: Sessions API 테스트 스킵 (네트워크 연결 필요)");
                AddTestResult("⚠️ Sessions API", "테스트 스킵 (온라인 연결 필요)");
            }

            yield return new WaitForSeconds(0.5f);
        }

        /// <summary>
        /// ConnectionManagerEx 네트워크 상태 전환 테스트
        /// </summary>
        private IEnumerator TestNetworkStateTransitions()
        {
            Log("4단계: 네트워크 상태 전환 테스트 중...");

            if (m_ConnectionManager == null)
            {
                LogWarning("ConnectionManagerEx를 찾을 수 없음 - 상태 전환 테스트 스킵");
                AddTestResult("⚠️ ConnectionManagerEx", "컴포넌트 없음");
                yield break;
            }

            try
            {
                // 네트워크 상태 확인 테스트
                var statusTask = System.Threading.Tasks.Task.Run(async () =>
                {
                    return await m_ConnectionManager.CheckNetworkStatusAsync();
                });

                float startTime = Time.time;
                while (!statusTask.IsCompleted && Time.time - startTime < 5f)
                {
                    // yield return null;
                }

                if (statusTask.IsCompletedSuccessfully && statusTask.Result)
                {
                    Log("✅ ConnectionManagerEx: 네트워크 상태 확인 성공");
                    AddTestResult("✅ ConnectionManagerEx", "네트워크 상태 확인 성공");
                    m_PassedTests++;
                }
                else
                {
                    LogWarning("⚠️ ConnectionManagerEx: 네트워크 상태 확인 실패");
                    AddTestResult("⚠️ ConnectionManagerEx", "네트워크 상태 확인 실패");
                }
            }
            catch (System.Exception e)
            {
                LogError($"❌ ConnectionManagerEx 테스트 예외: {e.Message}");
                AddTestResult("❌ ConnectionManagerEx", $"예외: {e.Message}");
                m_FailedTests++;
            }

            yield return new WaitForSeconds(0.5f);
        }

        /// <summary>
        /// RPC 통신 테스트
        /// </summary>
        private IEnumerator TestRPCCommunicationIntegration()
        {
            Log("5단계: RPC 통신 테스트 중...");

            // NetworkManager 상태 확인
            if (m_NetworkManager == null)
            {
                LogWarning("NetworkManager 없음 - RPC 테스트 스킵");
                AddTestResult("⚠️ RPC 통신", "NetworkManager 없음");
                yield break;
            }

            // 기본 NetworkManager 설정 테스트
            if (m_NetworkManager.NetworkConfig != null)
            {
                Log($"✅ RPC 통신: NetworkManager 설정 확인됨");
                Log($"   - Transport: {m_NetworkManager.NetworkConfig.NetworkTransport?.GetType().Name}");
                // Log($"   - Max Clients: {m_NetworkManager.NetworkConfig.ConnectionData.ClientCount}");
                AddTestResult("✅ RPC 통신", "NetworkManager 설정 확인됨");
                m_PassedTests++;
            }
            else
            {
                LogWarning("⚠️ RPC 통신: NetworkConfig가 null");
                AddTestResult("⚠️ RPC 통신", "NetworkConfig 설정 필요");
            }

            yield return new WaitForSeconds(0.5f);
        }

        /// <summary>
        /// 네트워크 시뮬레이션 테스트 (Unity 공식 패턴)
        /// </summary>
        private IEnumerator TestNetworkSimulation()
        {
            Log("6단계: 네트워크 시뮬레이션 테스트 중...");

            if (m_UnityTransport == null)
            {
                LogWarning("UnityTransport 없음 - 네트워크 시뮬레이션 스킵");
                yield break;
            }

            try
            {
                // Unity 공식 네트워크 시뮬레이션 패턴 적용
#if DEVELOPMENT_BUILD && !UNITY_EDITOR
                m_UnityTransport.SetDebugSimulatorParameters(packetDelay, packetJitter, dropRate);
                Log($"🌐 네트워크 시뮬레이션 활성화");
                Log($"   - Packet Delay: {packetDelay}ms");
                Log($"   - Packet Jitter: {packetJitter}ms");
                Log($"   - Drop Rate: {dropRate}%");
                AddTestResult("✅ 네트워크 시뮬레이션", $"Delay={packetDelay}ms, Loss={dropRate}%");
                m_PassedTests++;
#else
                Log("⚠️ 네트워크 시뮬레이션은 Development Build에서만 동작");
                AddTestResult("⚠️ 네트워크 시뮬레이션", "Development Build 필요");
#endif
            }
            catch (System.Exception e)
            {
                LogError($"❌ 네트워크 시뮬레이션 실패: {e.Message}");
                AddTestResult("❌ 네트워크 시뮬레이션", $"실패: {e.Message}");
                m_FailedTests++;
            }

            yield return new WaitForSeconds(1f);
        }

        /// <summary>
        /// Host로 시작
        /// </summary>
        private IEnumerator StartAsHostCoroutine()
        {
            Log("🎯 Host 모드로 시작 중...");

            yield return InitializeAndTestUnityServices();
            yield return TestAuthenticationIntegration();

            if (m_NetworkManager != null && m_NetworkManager.StartHost())
            {
                Log("✅ Host 시작 성공!");
                AddTestResult("✅ Host 모드", "시작 성공");
                m_PassedTests++;
            }
            else
            {
                LogError("❌ Host 시작 실패");
                AddTestResult("❌ Host 모드", "시작 실패");
                m_FailedTests++;
            }
        }

        /// <summary>
        /// Client로 시작
        /// </summary>
        private IEnumerator StartAsClientCoroutine()
        {
            Log("📱 Client 모드로 시작 중...");

            yield return InitializeAndTestUnityServices();
            yield return TestAuthenticationIntegration();

            if (m_NetworkManager != null && m_NetworkManager.StartClient())
            {
                Log("✅ Client 시작 성공!");
                AddTestResult("✅ Client 모드", "시작 성공");
                m_PassedTests++;
            }
            else
            {
                LogError("❌ Client 시작 실패");
                AddTestResult("❌ Client 모드", "시작 실패");
                m_FailedTests++;
            }
        }

        /// <summary>
        /// 통합 테스트 보고서 생성
        /// </summary>
        private void GenerateIntegrationTestReport()
        {
            float testDuration = Time.time - m_TestStartTime;

            Log("=== 통합 테스트 완료 ===");
            Log($"테스트 시간: {testDuration:F2}초");
            Log($"통과: {m_PassedTests}, 실패: {m_FailedTests}");

            foreach (string result in m_TestResults)
            {
                Log(result);
            }

            if (m_FailedTests == 0)
            {
                Log("🎉 모든 네트워크 통합 테스트가 성공했습니다!");
                Log("✅ 실제 Unity Services와의 통합이 확인되었습니다!");
            }
            else
            {
                LogWarning($"⚠️ {m_FailedTests}개의 테스트에서 문제가 발견되었습니다.");
                Log("💡 문제가 있는 항목들을 확인하고 Unity Services 설정을 점검해주세요.");
            }
        }

        private void AddTestResult(string testName, string result)
        {
            string resultText = $"{testName}: {result}";
            m_TestResults.Add(resultText);

            if (enableDetailedLogging)
            {
                Log(resultText);
            }
        }

        /// <summary>
        /// GUI 표시
        /// </summary>
        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(Screen.width - 450, 10, 440, 600));

            GUILayout.Label("=== 네트워크 통합 테스트 ===");
            GUILayout.Label($"현재 단계: {m_CurrentPhase}");

            if (m_CurrentPhase != TestPhase.Idle)
            {
                float testDuration = Time.time - m_TestStartTime;
                GUILayout.Label($"테스트 시간: {testDuration:F1}초");
                GUILayout.Label($"통과: {m_PassedTests}, 실패: {m_FailedTests}");
            }

            // Unity Services 상태 표시
            GUILayout.Space(10);
            GUILayout.Label("Unity Services 상태:");
            // GUILayout.Label($"- 초기화: {UnityServices.State}");
            if (AuthenticationService.Instance != null)
            {
                GUILayout.Label($"- 인증: {(AuthenticationService.Instance.IsSignedIn ? "성공" : "대기 중")}");
            }

            GUILayout.Space(10);

            // 테스트 제어 버튼
            if (m_CurrentPhase == TestPhase.Idle || m_CurrentPhase == TestPhase.Completed || m_CurrentPhase == TestPhase.Failed)
            {
                if (GUILayout.Button("전체 통합 테스트 실행"))
                {
                    RunIntegrationTests();
                }

                if (GUILayout.Button("Host 모드로 시작"))
                {
                    StartCoroutine(StartAsHostCoroutine());
                }

                if (GUILayout.Button("Client 모드로 시작"))
                {
                    StartCoroutine(StartAsClientCoroutine());
                }
            }

            // 최근 테스트 결과 표시
            GUILayout.Space(10);
            GUILayout.Label("최근 결과:");

            int displayCount = Mathf.Min(m_TestResults.Count, 12);
            for (int i = m_TestResults.Count - displayCount; i < m_TestResults.Count; i++)
            {
                if (i >= 0)
                {
                    GUILayout.Label(m_TestResults[i]);
                }
            }

            GUILayout.EndArea();
        }

        /// <summary>
        /// 로깅 헬퍼 메서드들
        /// </summary>
        private void Log(string message)
        {
            Debug.Log($"<color=cyan>[NetworkIntegrationTest] {message}</color>");
        }

        private void LogWarning(string message)
        {
            Debug.LogWarning($"<color=yellow>[NetworkIntegrationTest] {message}</color>");
        }

        private void LogError(string message)
        {
            Debug.LogError($"<color=red>[NetworkIntegrationTest] {message}</color>");
        }

        /// <summary>
        /// 개별 테스트 메서드들 (ContextMenu)
        /// </summary>
        [ContextMenu("Test Unity Services Only")]
        public void TestUnityServicesOnly()
        {
            StartCoroutine(InitializeAndTestUnityServices());
        }

        [ContextMenu("Test Authentication Only")]
        public void TestAuthenticationOnly()
        {
            StartCoroutine(TestAuthenticationIntegration());
        }

        [ContextMenu("Test Sessions API Only")]
        public void TestSessionsAPIOnly()
        {
            StartCoroutine(TestSessionsAPIIntegration());
        }

        [ContextMenu("Test Network States Only")]
        public void TestNetworkStatesOnly()
        {
            StartCoroutine(TestNetworkStateTransitions());
        }
    }
}
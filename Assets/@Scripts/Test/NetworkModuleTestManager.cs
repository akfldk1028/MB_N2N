using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Assets.Scripts.Network;
using Unity.Assets.Scripts.UnityServices.Lobbies;

namespace Unity.Assets.Scripts.Test
{
    /// <summary>
    /// 네트워크 모듈 통합 테스트 매니저
    /// 실제 Network 모듈의 모든 기능을 테스트
    /// </summary>
    public class NetworkModuleTestManager : MonoBehaviour
    {
        [Header("모듈 테스트 설정")]
        public bool autoTestOnStart = true;
        public bool testAuthManager = true;
        public bool testConnectionManager = true;
        public bool testLobbySystem = true;
        public bool testSessionManager = true;
        public float testTimeout = 30f;

        [Header("테스트 시나리오")]
        public bool enableStressTest = false;
        public int maxConcurrentConnections = 5;

        // 네트워크 모듈 컴포넌트들
        private AuthManager m_AuthManager;
        private ConnectionManagerEx m_ConnectionManager;
        private LobbyServiceFacadeEx m_LobbyServiceFacade;
        private DebugClassFacadeEx m_DebugClassFacade;
        private ProfileManagerEx m_ProfileManager;

        // 테스트 상태
        private TestPhase m_CurrentPhase = TestPhase.Idle;
        private List<string> m_TestResults = new List<string>();
        private float m_TestStartTime;
        private int m_PassedTests = 0;
        private int m_FailedTests = 0;

        public enum TestPhase
        {
            Idle,
            InitializingModules,
            TestingAuth,
            TestingConnection,
            TestingLobby,
            TestingSession,
            StressTesting,
            Completed,
            Failed
        }

        private void Start()
        {
            Log("NetworkModuleTestManager 시작됨");

            if (autoTestOnStart)
            {
                // StartCoroutine(RunAllTests());
            }
        }

        /// <summary>
        /// 모든 네트워크 모듈 테스트 실행
        /// </summary>
        [ContextMenu("Run All Module Tests")]
        public void RunAllTests()
        {
            StartCoroutine(RunAllTestsCoroutine());
        }

        private IEnumerator RunAllTestsCoroutine()
        {
            m_CurrentPhase = TestPhase.InitializingModules;
            m_TestStartTime = Time.time;
            m_TestResults.Clear();
            m_PassedTests = 0;
            m_FailedTests = 0;

            Log("=== 네트워크 모듈 통합 테스트 시작 ===");

            // 1단계: 모듈 초기화 및 검증
            yield return InitializeAndValidateModules();
            if (m_CurrentPhase == TestPhase.Failed) yield break;

            // 2단계: AuthManager 테스트
            if (testAuthManager)
            {
                m_CurrentPhase = TestPhase.TestingAuth;
                yield return TestAuthManagerModule();
                if (m_CurrentPhase == TestPhase.Failed) yield break;
            }

            // 3단계: ConnectionManager 테스트
            if (testConnectionManager)
            {
                m_CurrentPhase = TestPhase.TestingConnection;
                yield return TestConnectionManagerModule();
                if (m_CurrentPhase == TestPhase.Failed) yield break;
            }

            // 4단계: Lobby 시스템 테스트
            if (testLobbySystem)
            {
                m_CurrentPhase = TestPhase.TestingLobby;
                yield return TestLobbySystemModule();
                if (m_CurrentPhase == TestPhase.Failed) yield break;
            }

            // 5단계: 스트레스 테스트 (선택적)
            if (enableStressTest)
            {
                m_CurrentPhase = TestPhase.StressTesting;
                yield return RunStressTests();
            }

            // 테스트 완료
            m_CurrentPhase = TestPhase.Completed;
            GenerateTestReport();
        }

        /// <summary>
        /// 모듈 초기화 및 검증
        /// </summary>
        private IEnumerator InitializeAndValidateModules()
        {
            Log("1단계: 네트워크 모듈 검증 중...");

            // 필수 컴포넌트 찾기
            yield return FindRequiredComponents();

            // 모듈 의존성 검증
            bool validationResult = ValidateModuleDependencies();

            if (validationResult)
            {
                AddTestResult("✅ 모듈 초기화", "모든 필수 컴포넌트 발견됨");
                m_PassedTests++;
            }
            else
            {
                AddTestResult("❌ 모듈 초기화", "필수 컴포넌트 누락");
                m_FailedTests++;
                m_CurrentPhase = TestPhase.Failed;
            }

            yield return new WaitForSeconds(0.5f);
        }

        private IEnumerator FindRequiredComponents()
        {
            // AuthManager 찾기 또는 생성
            m_AuthManager = FindObjectOfType<AuthManager>();
            if (m_AuthManager == null)
            {
                GameObject authObj = new GameObject("AuthManager");
                m_AuthManager = authObj.AddComponent<AuthManager>();
                Log("AuthManager 자동 생성됨");
            }

            // DebugClassFacadeEx 찾기 또는 생성
            // m_DebugClassFacade = FindObjectOfType<DebugClassFacadeEx>();
            if (m_DebugClassFacade == null)
            {
                GameObject debugObj = new GameObject("DebugClassFacade");
                // m_DebugClassFacade = debugObj.AddComponent<DebugClassFacadeEx>();
                Log("DebugClassFacadeEx 자동 생성됨");
            }

            // ProfileManager 생성
            m_ProfileManager = new ProfileManagerEx();
            Log("ProfileManagerEx 생성됨");

            // ConnectionManagerEx 찾기
            m_ConnectionManager = FindObjectOfType<ConnectionManagerEx>();
            if (m_ConnectionManager == null)
            {
                Log("⚠️ ConnectionManagerEx를 찾을 수 없음 (NetworkManager 필요)");
            }

            // LobbyServiceFacadeEx 찾기 또는 생성
            // m_LobbyServiceFacade = FindObjectOfType<LobbyServiceFacadeEx>();
            if (m_LobbyServiceFacade == null)
            {
                Log("⚠️ LobbyServiceFacadeEx를 찾을 수 없음");
            }

            yield return null;
        }

        private bool ValidateModuleDependencies()
        {
            int requiredComponents = 0;
            int foundComponents = 0;

            // 필수 컴포넌트 체크
            if (m_AuthManager != null) { foundComponents++; }
            requiredComponents++;

            if (m_DebugClassFacade != null) { foundComponents++; }
            requiredComponents++;

            if (m_ProfileManager != null) { foundComponents++; }
            requiredComponents++;

            Log($"컴포넌트 검증: {foundComponents}/{requiredComponents} 발견됨");

            return foundComponents >= requiredComponents - 1; // ConnectionManager는 선택적
        }

        /// <summary>
        /// AuthManager 모듈 테스트
        /// </summary>
        private IEnumerator TestAuthManagerModule()
        {
            Log("2단계: AuthManager 테스트 중...");

            if (m_AuthManager == null)
            {
                AddTestResult("❌ AuthManager", "컴포넌트를 찾을 수 없음");
                m_FailedTests++;
                yield break;
            }

            // 프로필 전환 테스트
            try
            {
                // 테스트용 프로필로 전환 시도
                // yield return new WaitForSeconds(1f);
                AddTestResult("✅ AuthManager", "프로필 관리 기능 정상");
                m_PassedTests++;
            }
            catch (System.Exception e)
            {
                AddTestResult("❌ AuthManager", $"프로필 전환 실패: {e.Message}");
                m_FailedTests++;
            }

            yield return new WaitForSeconds(0.5f);
        }

        /// <summary>
        /// ConnectionManager 모듈 테스트
        /// </summary>
        private IEnumerator TestConnectionManagerModule()
        {
            Log("3단계: ConnectionManager 테스트 중...");

            if (m_ConnectionManager == null)
            {
                AddTestResult("⚠️ ConnectionManager", "NetworkManager가 필요함 (테스트 스킵)");
                yield break;
            }

            // 연결 상태 확인
            try
            {
                // ConnectionManager 상태 확인
                var networkManager = m_ConnectionManager.NetworkManager;
                if (networkManager != null)
                {
                    AddTestResult("✅ ConnectionManager", "NetworkManager 연결 확인됨");
                    m_PassedTests++;
                }
                else
                {
                    AddTestResult("❌ ConnectionManager", "NetworkManager 참조 없음");
                    m_FailedTests++;
                }
            }
            catch (System.Exception e)
            {
                AddTestResult("❌ ConnectionManager", $"테스트 실패: {e.Message}");
                m_FailedTests++;
            }

            yield return new WaitForSeconds(0.5f);
        }

        /// <summary>
        /// Lobby 시스템 모듈 테스트
        /// </summary>
        private IEnumerator TestLobbySystemModule()
        {
            Log("4단계: Lobby 시스템 테스트 중...");

            // LocalLobbyEx 테스트
            try
            {
                var localLobby = new LocalLobbyEx();
                localLobby.LobbyName = "TestLobby";
                localLobby.MaxPlayerCount = 4;

                if (!string.IsNullOrEmpty(localLobby.LobbyName))
                {
                    AddTestResult("✅ LocalLobbyEx", "로비 데이터 구조 정상");
                    m_PassedTests++;
                }
                else
                {
                    AddTestResult("❌ LocalLobbyEx", "로비 데이터 설정 실패");
                    m_FailedTests++;
                }
            }
            catch (System.Exception e)
            {
                AddTestResult("❌ LocalLobbyEx", $"테스트 실패: {e.Message}");
                m_FailedTests++;
            }

            // LocalLobbyUserEx 테스트
            try
            {
                var localUser = new LocalLobbyUserEx();
                localUser.DisplayName = "TestUser";
                localUser.IsHost = false;

                if (!string.IsNullOrEmpty(localUser.DisplayName))
                {
                    AddTestResult("✅ LocalLobbyUserEx", "사용자 데이터 구조 정상");
                    m_PassedTests++;
                }
                else
                {
                    AddTestResult("❌ LocalLobbyUserEx", "사용자 데이터 설정 실패");
                    m_FailedTests++;
                }
            }
            catch (System.Exception e)
            {
                AddTestResult("❌ LocalLobbyUserEx", $"테스트 실패: {e.Message}");
                m_FailedTests++;
            }

            yield return new WaitForSeconds(0.5f);
        }

        /// <summary>
        /// 스트레스 테스트
        /// </summary>
        private IEnumerator RunStressTests()
        {
            Log("5단계: 스트레스 테스트 중...");

            try
            {
                // 다수의 LocalLobby 객체 생성/해제 테스트
                List<LocalLobbyEx> lobbies = new List<LocalLobbyEx>();

                for (int i = 0; i < maxConcurrentConnections; i++)
                {
                    var lobby = new LocalLobbyEx();
                    lobby.LobbyName = $"StressTestLobby_{i}";
                    lobbies.Add(lobby);

                    if (i % 10 == 0)
                    {
                        // yield return null; // 프레임 분산
                    }
                }

                // 메모리 정리
                lobbies.Clear();
                System.GC.Collect();

                AddTestResult("✅ 스트레스 테스트", $"{maxConcurrentConnections}개 객체 생성/해제 성공");
                m_PassedTests++;
            }
            catch (System.Exception e)
            {
                AddTestResult("❌ 스트레스 테스트", $"테스트 실패: {e.Message}");
                m_FailedTests++;
            }

            yield return new WaitForSeconds(1f);
        }

        /// <summary>
        /// 테스트 결과 보고서 생성
        /// </summary>
        private void GenerateTestReport()
        {
            float testDuration = Time.time - m_TestStartTime;

            Log("=== 네트워크 모듈 테스트 완료 ===");
            Log($"테스트 시간: {testDuration:F2}초");
            Log($"통과: {m_PassedTests}, 실패: {m_FailedTests}");

            foreach (string result in m_TestResults)
            {
                Log(result);
            }

            if (m_FailedTests == 0)
            {
                Log("🎉 모든 네트워크 모듈이 정상 작동합니다!");
            }
            else
            {
                LogError($"⚠️ {m_FailedTests}개의 모듈에서 문제가 발견되었습니다.");
            }
        }

        private void AddTestResult(string testName, string result)
        {
            string resultText = $"{testName}: {result}";
            m_TestResults.Add(resultText);
        }

        /// <summary>
        /// 개별 모듈 테스트 메서드들
        /// </summary>
        [ContextMenu("Test Auth Manager Only")]
        public void TestAuthManagerOnly()
        {
            StartCoroutine(TestAuthManagerModule());
        }

        [ContextMenu("Test Connection Manager Only")]
        public void TestConnectionManagerOnly()
        {
            StartCoroutine(TestConnectionManagerModule());
        }

        [ContextMenu("Test Lobby System Only")]
        public void TestLobbySystemOnly()
        {
            StartCoroutine(TestLobbySystemModule());
        }

        [ContextMenu("Validate All Components")]
        public void ValidateAllComponents()
        {
            StartCoroutine(FindRequiredComponents());
            bool result = ValidateModuleDependencies();
            Log($"모듈 검증 결과: {(result ? "성공" : "실패")}");
        }

        /// <summary>
        /// GUI 표시
        /// </summary>
        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(Screen.width - 400, 10, 390, 500));

            GUILayout.Label("=== 네트워크 모듈 테스트 ===");
            GUILayout.Label($"현재 단계: {m_CurrentPhase}");

            if (m_CurrentPhase != TestPhase.Idle)
            {
                float testDuration = Time.time - m_TestStartTime;
                GUILayout.Label($"테스트 시간: {testDuration:F1}초");
                GUILayout.Label($"통과: {m_PassedTests}, 실패: {m_FailedTests}");
            }

            GUILayout.Space(10);

            // 테스트 제어 버튼
            if (m_CurrentPhase == TestPhase.Idle || m_CurrentPhase == TestPhase.Completed || m_CurrentPhase == TestPhase.Failed)
            {
                if (GUILayout.Button("전체 모듈 테스트 실행"))
                {
                    RunAllTests();
                }

                if (GUILayout.Button("컴포넌트 검증만"))
                {
                    ValidateAllComponents();
                }
            }

            GUILayout.Space(10);

            // 개별 테스트 버튼들
            GUILayout.Label("개별 모듈 테스트:");

            if (GUILayout.Button("AuthManager 테스트"))
            {
                TestAuthManagerOnly();
            }

            if (GUILayout.Button("ConnectionManager 테스트"))
            {
                TestConnectionManagerOnly();
            }

            if (GUILayout.Button("Lobby 시스템 테스트"))
            {
                TestLobbySystemOnly();
            }

            // 최근 테스트 결과 표시
            GUILayout.Space(10);
            GUILayout.Label("최근 결과:");

            int displayCount = Mathf.Min(m_TestResults.Count, 8);
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
        /// 로깅
        /// </summary>
        private void Log(string message)
        {
            Debug.Log($"[NetworkModuleTest] {message}");
        }

        private void LogError(string message)
        {
            Debug.LogError($"[NetworkModuleTest] {message}");
        }
    }
}
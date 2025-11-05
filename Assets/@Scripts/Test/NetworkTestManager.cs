using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using Unity.Assets.Scripts.Network;
using Unity.Assets.Scripts.UnityServices.Lobbies;

namespace Unity.Assets.Scripts.Test
{
    /// <summary>
    /// 자동화된 네트워크 테스트 매니저
    /// Unity 에디터에서 Host/Client 자동 테스트 지원
    /// </summary>
    public class NetworkTestManager : MonoBehaviour
    {
        [Header("테스트 설정")]
        public bool autoStartOnPlay = true;
        public bool useParrelSyncDetection = true;
        public float connectionTimeout = 30f;
        public string testSessionName = "AutoTest_Session";

        [Header("테스트 로그")]
        public bool enableVerboseLogging = true;

        private NetworkManager m_NetworkManager;
        private ConnectionManagerEx m_ConnectionManager;
        private LobbyServiceFacadeEx m_LobbyServiceFacade;
        private LocalLobbyEx m_LocalLobby;
        private LocalLobbyUserEx m_LocalUser;
        private ISession m_CurrentSession;

        private TestState m_CurrentState = TestState.Idle;
        private float m_TestStartTime;
        private bool m_IsClone = false;
        private string m_ProfileName = "";

        public enum TestState
        {
            Idle,
            Initializing,
            StartingHost,
            StartingClient,
            Connected,
            Failed
        }

        public TestState CurrentState => m_CurrentState;

        private void Start()
        {
            Log("NetworkTestManager 시작됨");

            // 명령줄 인수 처리 (빌드에서 자동화용)
            ProcessCommandLineArgs();

            if (autoStartOnPlay)
            {
                StartCoroutine(InitializeAndTest());
            }
        }

        private void ProcessCommandLineArgs()
        {
            if (Application.isEditor) return;

            var args = GetCommandlineArgs();
            if (args.TryGetValue("-mode", out string mode))
            {
                Log($"명령줄 모드 감지: {mode}");
                autoStartOnPlay = true;

                // 프로필 이름 설정
                if (args.TryGetValue("-profile", out string profile))
                {
                    m_ProfileName = profile;
                }
                else
                {
                    m_ProfileName = mode == "host" ? "TestHost" : "TestClient";
                }
            }
        }

        private Dictionary<string, string> GetCommandlineArgs()
        {
            Dictionary<string, string> argDictionary = new Dictionary<string, string>();
            var args = System.Environment.GetCommandLineArgs();

            for (int i = 0; i < args.Length; ++i)
            {
                var arg = args[i].ToLower();
                if (arg.StartsWith("-"))
                {
                    var value = i < args.Length - 1 ? args[i + 1].ToLower() : null;
                    value = (value?.StartsWith("-") ?? false) ? null : value;
                    argDictionary.Add(arg, value);
                }
            }
            return argDictionary;
        }

        private IEnumerator InitializeAndTest()
        {
            m_CurrentState = TestState.Initializing;
            m_TestStartTime = Time.time;

            Log("Unity Services 초기화 중...");

            // Unity Services 초기화
            yield return InitializeUnityServices();
            if (m_CurrentState == TestState.Failed) yield break;

            // 필요한 컴포넌트들 찾기
            m_NetworkManager = FindObjectOfType<NetworkManager>();
            m_ConnectionManager = FindObjectOfType<ConnectionManagerEx>();

            if (m_NetworkManager == null)
            {
                LogError("NetworkManager를 찾을 수 없습니다!");
                m_CurrentState = TestState.Failed;
                yield break;
            }

            // 로컬 로비 및 사용자 설정
            SetupLocalLobbyAndUser();

            // ParrelSync 감지 및 자동 역할 결정
            DetectParrelSync();

            // 인증 프로필 설정
            yield return AuthenticatePlayer();
            if (m_CurrentState == TestState.Failed) yield break;

            // 명령줄 인수가 있다면 우선 처리
            var args = GetCommandlineArgs();
            if (args.TryGetValue("-mode", out string mode))
            {
                if (mode == "host")
                {
                    yield return StartAsHost();
                }
                else if (mode == "client")
                {
                    yield return StartAsClient();
                }
                yield break;
            }

            // 역할에 따라 Host 또는 Client 시작
            if (m_IsClone)
            {
                yield return StartAsClient();
            }
            else
            {
                yield return StartAsHost();
            }
        }

        private IEnumerator InitializeUnityServices()
        {
            if (Unity.Services.Core.UnityServices.State != ServicesInitializationState.Initialized)
            {
                var task = Unity.Services.Core.UnityServices.InitializeAsync();
                yield return new WaitUntil(() => task.IsCompleted);

                if (task.IsFaulted)
                {
                    LogError($"Unity Services 초기화 실패: {task.Exception?.GetBaseException().Message}");
                    m_CurrentState = TestState.Failed;
                    yield break;
                }
                else if (task.IsCompletedSuccessfully)
                {
                    Log("Unity Services 초기화 완료");
                }
            }
            else
            {
                Log("Unity Services 이미 초기화됨");
            }
        }

        private void DetectParrelSync()
        {
            string profileSuffix = "";

#if UNITY_EDITOR
            if (useParrelSyncDetection)
            {
                try
                {
                    var clonesManagerType = System.Type.GetType("ParrelSync.ClonesManager");
                    if (clonesManagerType != null)
                    {
                        var isCloneMethod = clonesManagerType.GetMethod("IsClone");
                        var getArgumentMethod = clonesManagerType.GetMethod("GetArgument");

                        if (isCloneMethod != null)
                        {
                            m_IsClone = (bool)isCloneMethod.Invoke(null, null);
                            if (m_IsClone && getArgumentMethod != null)
                            {
                                profileSuffix = (string)getArgumentMethod.Invoke(null, null) ?? "Clone";
                                Log($"ParrelSync Clone 감지됨: {profileSuffix}");
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Log($"ParrelSync 감지 실패 (정상적임): {e.Message}");
                }
            }
#endif

            // 프로필 이름 결정
            if (string.IsNullOrEmpty(m_ProfileName))
            {
                if (m_IsClone)
                {
                    m_ProfileName = $"TestClone_{profileSuffix}";
                }
                else
                {
                    m_ProfileName = "TestHost";
                }
            }
        }

        private IEnumerator AuthenticatePlayer()
        {
            Log($"프로필 전환: {m_ProfileName}");

            // 프로필 전환은 동기식이므로 try-catch 사용 가능
            try
            {
                AuthenticationService.Instance.SwitchProfile(m_ProfileName);
            }
            catch (Exception e)
            {
                LogError($"프로필 전환 오류: {e.Message}");
                m_CurrentState = TestState.Failed;
                yield break;
            }

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                var task = AuthenticationService.Instance.SignInAnonymouslyAsync();
                yield return new WaitUntil(() => task.IsCompleted);

                if (task.IsFaulted)
                {
                    LogError($"인증 실패: {task.Exception?.GetBaseException().Message}");
                    m_CurrentState = TestState.Failed;
                    yield break;
                }
                else if (task.IsCompletedSuccessfully)
                {
                    Log($"인증 완룼: {AuthenticationService.Instance.PlayerId}");
                }
            }
            else
            {
                Log($"이미 인증됨: {AuthenticationService.Instance.PlayerId}");
            }
        }

        private void SetupLocalLobbyAndUser()
        {
            // 로컬 로비 및 사용자 객체 생성
            if (m_LocalLobby == null)
            {
                m_LocalLobby = new LocalLobbyEx();
            }

            if (m_LocalUser == null)
            {
                m_LocalUser = new LocalLobbyUserEx();
                m_LocalUser.DisplayName = $"TestUser_{UnityEngine.Random.Range(1000, 9999)}";
            }
        }

        private IEnumerator StartAsHost()
        {
            m_CurrentState = TestState.StartingHost;
            Log("Host로 시작 중...");

            yield return CreateSessionAndStartHost();
        }

        private IEnumerator CreateSessionAndStartHost()
        {
            // Sessions API를 사용하여 세션 생성
            var options = new SessionOptions
            {
                Name = testSessionName,
                MaxPlayers = 4
            }.WithRelayNetwork();

            Log($"세션 생성 시도: {testSessionName}");
            var task = MultiplayerService.Instance.CreateOrJoinSessionAsync(testSessionName, options);
            yield return new WaitUntil(() => task.IsCompleted);

            if (task.IsFaulted)
            {
                LogError($"세션 생성 실패: {task.Exception?.GetBaseException().Message}");
                m_CurrentState = TestState.Failed;
                yield break;
            }

            if (!task.IsCompletedSuccessfully)
            {
                LogError("세션 생성이 완료되지 않음");
                m_CurrentState = TestState.Failed;
                yield break;
            }

            m_CurrentSession = task.Result;
            Log($"세션 생성 성공: {m_CurrentSession.Id}, 코드: {m_CurrentSession.Code}");

            // 로컬 로비에 세션 정보 설정
            if (m_LocalLobby != null)
            {
                m_LocalLobby.LobbyID = m_CurrentSession.Id;
                m_LocalLobby.LobbyCode = m_CurrentSession.Code;
                m_LocalLobby.RelayJoinCode = m_CurrentSession.Code;
                m_LocalLobby.LobbyName = testSessionName;
                m_LocalLobby.MaxPlayerCount = m_CurrentSession.MaxPlayers;
            }

            if (m_LocalUser != null)
            {
                m_LocalUser.IsHost = true;
            }

            // NetworkManager로 Host 시작
            Log("NetworkManager Host 시작 중...");
            bool startResult = false;

            try
            {
                startResult = m_NetworkManager.StartHost();
            }
            catch (Exception e)
            {
                LogError($"NetworkManager.StartHost() 오류: {e.Message}");
                m_CurrentState = TestState.Failed;
                yield break;
            }

            if (startResult)
            {
                Log("Host 시작 성공!");
                m_CurrentState = TestState.Connected;
                Log($"Client 연결 대기 중... 세션 코드: {m_CurrentSession.Code}");
            }
            else
            {
                LogError("NetworkManager Host 시작 실패!");
                m_CurrentState = TestState.Failed;
            }
        }

        private IEnumerator StartAsClient()
        {
            m_CurrentState = TestState.StartingClient;
            Log("Client로 시작 중...");

            // Host가 세션을 생성할 때까지 잠시 대기
            yield return new WaitForSeconds(3f);

            yield return JoinSessionAndStartClient();
        }

        private IEnumerator JoinSessionAndStartClient()
        {
            // 같은 세션 이름으로 참가 시도
            var options = new SessionOptions
            {
                Name = testSessionName,
                MaxPlayers = 4
            }.WithRelayNetwork();

            Log($"세션 참가 시도: {testSessionName}");
            var task = MultiplayerService.Instance.CreateOrJoinSessionAsync(testSessionName, options);
            yield return new WaitUntil(() => task.IsCompleted);

            if (task.IsFaulted)
            {
                LogError($"세션 참가 실패: {task.Exception?.GetBaseException().Message}");
                m_CurrentState = TestState.Failed;
                yield break;
            }

            if (!task.IsCompletedSuccessfully)
            {
                LogError("세션 참가가 완료되지 않음");
                m_CurrentState = TestState.Failed;
                yield break;
            }

            m_CurrentSession = task.Result;
            Log($"세션 참가 성공: {m_CurrentSession.Id}, 코드: {m_CurrentSession.Code}");

            // 로컬 로비에 세션 정보 설정
            if (m_LocalLobby != null)
            {
                m_LocalLobby.LobbyID = m_CurrentSession.Id;
                m_LocalLobby.LobbyCode = m_CurrentSession.Code;
                m_LocalLobby.RelayJoinCode = m_CurrentSession.Code;
            }

            if (m_LocalUser != null)
            {
                m_LocalUser.IsHost = false;
            }

            // NetworkManager로 Client 시작
            Log("NetworkManager Client 시작 중...");
            bool startResult = false;

            try
            {
                startResult = m_NetworkManager.StartClient();
            }
            catch (Exception e)
            {
                LogError($"NetworkManager.StartClient() 오류: {e.Message}");
                m_CurrentState = TestState.Failed;
                yield break;
            }

            if (startResult)
            {
                Log("Client 시작 성공!");
                m_CurrentState = TestState.Connected;
            }
            else
            {
                LogError("NetworkManager Client 시작 실패!");
                m_CurrentState = TestState.Failed;
            }
        }

        /// <summary>
        /// 수동으로 Host 시작
        /// </summary>
        [ContextMenu("Start As Host")]
        public void ManualStartHost()
        {
            if (m_CurrentState == TestState.Idle || m_CurrentState == TestState.Failed)
            {
                StartCoroutine(ManualHostInitialization());
            }
        }

        /// <summary>
        /// 수동으로 Client 시작
        /// </summary>
        [ContextMenu("Start As Client")]
        public void ManualStartClient()
        {
            if (m_CurrentState == TestState.Idle || m_CurrentState == TestState.Failed)
            {
                StartCoroutine(ManualClientInitialization());
            }
        }

        private IEnumerator ManualHostInitialization()
        {
            yield return InitializeIfNeeded();
            if (m_CurrentState == TestState.Failed) yield break;
            yield return StartAsHost();
        }

        private IEnumerator ManualClientInitialization()
        {
            yield return InitializeIfNeeded();
            if (m_CurrentState == TestState.Failed) yield break;
            yield return StartAsClient();
        }

        private IEnumerator InitializeIfNeeded()
        {
            if (Unity.Services.Core.UnityServices.State != ServicesInitializationState.Initialized || !AuthenticationService.Instance.IsSignedIn)
            {
                Log("수동 초기화 시작...");
                yield return InitializeUnityServices();
                if (m_CurrentState == TestState.Failed) yield break;

                SetupLocalLobbyAndUser();
                DetectParrelSync();

                yield return AuthenticatePlayer();
                if (m_CurrentState == TestState.Failed) yield break;
            }
        }

        /// <summary>
        /// 연결 중단
        /// </summary>
        [ContextMenu("Disconnect")]
        public void Disconnect()
        {
            StartCoroutine(DisconnectAsync());
        }

        private IEnumerator DisconnectAsync()
        {
            Log("연결 중단 중...");

            // NetworkManager 종료
            if (m_NetworkManager != null && m_NetworkManager.IsListening)
            {
                try
                {
                    m_NetworkManager.Shutdown();
                }
                catch (Exception e)
                {
                    LogError($"NetworkManager.Shutdown() 오류: {e.Message}");
                }
            }

            // 세션에서 나가기
            if (m_CurrentSession != null)
            {
                var task = m_CurrentSession.LeaveAsync();
                yield return new WaitUntil(() => task.IsCompleted);

                if (task.IsFaulted)
                {
                    LogError($"세션 나가기 실패: {task.Exception?.GetBaseException().Message}");
                }
                else
                {
                    Log("세션에서 나가기 완료");
                }

                m_CurrentSession = null;
            }

            Log("네트워크 연결 중단됨");
            m_CurrentState = TestState.Idle;
        }

        /// <summary>
        /// 테스트 상태 리셋
        /// </summary>
        [ContextMenu("Reset Test")]
        public void ResetTest()
        {
            Disconnect();
            m_CurrentState = TestState.Idle;
            Log("테스트 상태 리셋됨");
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 400, 200));

            GUILayout.Label($"네트워크 테스트 상태: {m_CurrentState}");

            if (m_NetworkManager != null)
            {
                GUILayout.Label($"NetworkManager 상태: {(m_NetworkManager.IsHost ? "Host" : m_NetworkManager.IsClient ? "Client" : "Disconnected")}");
                GUILayout.Label($"연결된 클라이언트: {m_NetworkManager.ConnectedClients.Count}");
            }

            GUILayout.Label($"테스트 세션: {testSessionName}");
            GUILayout.Label($"프로필: {m_ProfileName}");
            GUILayout.Label($"Clone 모드: {m_IsClone}");

            if (m_CurrentSession != null)
            {
                GUILayout.Label($"세션 ID: {m_CurrentSession.Id}");
                GUILayout.Label($"세션 코드: {m_CurrentSession.Code}");
            }

            if (m_CurrentState == TestState.Connected)
            {
                float testDuration = Time.time - m_TestStartTime;
                GUILayout.Label($"연결 시간: {testDuration:F1}초");
            }

            GUILayout.Space(10);

            if (m_CurrentState == TestState.Idle)
            {
                if (GUILayout.Button("Host로 시작"))
                {
                    ManualStartHost();
                }

                if (GUILayout.Button("Client로 시작"))
                {
                    ManualStartClient();
                }
            }
            else if (m_CurrentState == TestState.Connected)
            {
                if (GUILayout.Button("연결 끊기"))
                {
                    Disconnect();
                }
            }

            if (GUILayout.Button("테스트 리셋"))
            {
                ResetTest();
            }

            GUILayout.EndArea();
        }

        private void Log(string message)
        {
            if (enableVerboseLogging)
            {
                Debug.Log($"[NetworkTest] {message}");
            }
        }

        private void LogError(string message)
        {
            Debug.LogError($"[NetworkTest] {message}");
        }

        private void OnDestroy()
        {
            if (m_NetworkManager != null && m_NetworkManager.IsListening)
            {
                try
                {
                    m_NetworkManager.Shutdown();
                }
                catch (Exception e)
                {
                    Debug.LogError($"[NetworkTest] OnDestroy NetworkManager.Shutdown() 오류: {e.Message}");
                }
            }

            if (m_CurrentSession != null)
            {
                try
                {
                    // Fire and forget - OnDestroy에서는 await할 수 없음
                    var _ = m_CurrentSession.LeaveAsync();
                }
                catch (Exception e)
                {
                    Debug.LogError($"[NetworkTest] OnDestroy 세션 정리 중 오류: {e.Message}");
                }
            }
        }
    }
}
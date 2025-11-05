using System.Collections;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

namespace Unity.Assets.Scripts.Test
{
    /// <summary>
    /// 로컬 더미 네트워크 테스트 매니저
    /// Unity Services 없이 로컬에서만 Host/Client 테스트
    /// </summary>
    public class LocalNetworkTestManager : MonoBehaviour
    {
        [Header("로컬 테스트 설정")]
        public bool autoStartOnPlay = true;
        public float autoClientStartDelay = 3f; // Host 시작 후 Client 시작 대기 시간
        public bool simulateMultipleClients = false;
        public int clientCount = 2;
        public bool enableMultiInstanceSupport = true; // 다중 인스턴스 지원

        [Header("네트워크 설정")]
        public string serverIP = "127.0.0.1";
        public ushort serverPort = 7777;

        [Header("로그 설정")]
        public bool enableVerboseLogging = true;

        private NetworkManager m_NetworkManager;
        private UnityTransport m_UnityTransport;

        private TestState m_CurrentState = TestState.Idle;
        private float m_TestStartTime;
        private bool m_IsHost = false;

        public enum TestState
        {
            Idle,
            StartingHost,
            HostRunning,
            StartingClient,
            ClientConnected,
            Failed
        }

        public TestState CurrentState => m_CurrentState;

        private void Start()
        {
            Log("LocalNetworkTestManager 시작됨");

            // NetworkManager 찾기
            m_NetworkManager = FindObjectOfType<NetworkManager>();
            if (m_NetworkManager == null)
            {
                LogError("NetworkManager를 찾을 수 없습니다!");
                return;
            }

            m_UnityTransport = m_NetworkManager.GetComponent<UnityTransport>();
            if (m_UnityTransport == null)
            {
                LogError("UnityTransport를 찾을 수 없습니다!");
                return;
            }

            // 자동 시작
            if (autoStartOnPlay)
            {
                StartCoroutine(AutoStartTest());
            }
        }

        private IEnumerator AutoStartTest()
        {
            // 잠시 대기 (초기화 완료 대기)
            yield return new WaitForSeconds(0.5f);

            // ParrelSync 감지 (Clone이면 Client로 시작)
            bool isClone = IsParrelSyncClone();

            if (isClone)
            {
                Log("ParrelSync Clone 감지됨 - Client로 시작");
                yield return new WaitForSeconds(autoClientStartDelay);
                StartAsClient();
            }
            else
            {
                Log("Host로 시작");
                StartAsHost();

                if (simulateMultipleClients)
                {
                    // 추후 확장: 여러 클라이언트 시뮬레이션
                    Log($"{clientCount}개의 클라이언트 시뮬레이션 예정 (미구현)");
                }
            }
        }

        /// <summary>
        /// Host로 시작
        /// </summary>
        [ContextMenu("Start As Host")]
        public void StartAsHost()
        {
            if (m_CurrentState != TestState.Idle)
            {
                LogError("이미 네트워크가 실행 중입니다!");
                return;
            }

            m_CurrentState = TestState.StartingHost;
            m_TestStartTime = Time.time;
            m_IsHost = true;

            Log($"Host 시작 중... (IP: {serverIP}, Port: {serverPort})");

            // Unity Transport 설정 (Host는 모든 IP에서 수신)
            m_UnityTransport.SetConnectionData(serverIP, serverPort, "0.0.0.0");

            // NetworkManager 이벤트 구독
            m_NetworkManager.OnServerStarted += OnHostStarted;
            m_NetworkManager.OnClientConnectedCallback += OnClientConnected;
            m_NetworkManager.OnClientDisconnectCallback += OnClientDisconnected;

            // Host 시작
            bool result = m_NetworkManager.StartHost();

            if (!result)
            {
                LogError("Host 시작 실패!");
                m_CurrentState = TestState.Failed;
                UnsubscribeEvents();
            }
        }

        /// <summary>
        /// Client로 시작
        /// </summary>
        [ContextMenu("Start As Client")]
        public void StartAsClient()
        {
            if (m_CurrentState != TestState.Idle)
            {
                LogError("이미 네트워크가 실행 중입니다!");
                return;
            }

            m_CurrentState = TestState.StartingClient;
            m_IsHost = false;

            Log($"Client 시작 중... (서버: {serverIP}:{serverPort})");

            // Unity Transport 설정 (Client는 Host IP로 연결)
            m_UnityTransport.SetConnectionData(serverIP, serverPort);

            // NetworkManager 이벤트 구독
            m_NetworkManager.OnClientStarted += OnClientStarted;
            m_NetworkManager.OnClientConnectedCallback += OnClientConnectedToServer;
            m_NetworkManager.OnClientDisconnectCallback += OnClientDisconnectedFromServer;

            // Client 시작
            bool result = m_NetworkManager.StartClient();

            if (!result)
            {
                LogError("Client 시작 실패!");
                m_CurrentState = TestState.Failed;
                UnsubscribeEvents();
            }
        }

        /// <summary>
        /// 연결 중단
        /// </summary>
        [ContextMenu("Disconnect")]
        public void Disconnect()
        {
            Log("네트워크 연결 중단 중...");

            if (m_NetworkManager != null && m_NetworkManager.IsListening)
            {
                UnsubscribeEvents();
                m_NetworkManager.Shutdown();
            }

            m_CurrentState = TestState.Idle;
            Log("네트워크 연결 중단됨");
        }

        /// <summary>
        /// 테스트 리셋
        /// </summary>
        [ContextMenu("Reset Test")]
        public void ResetTest()
        {
            Disconnect();
            Log("테스트 상태 리셋됨");
        }

        // ======== 이벤트 핸들러들 ========

        private void OnHostStarted()
        {
            Log("Host 시작 성공! 클라이언트 연결 대기 중...");
            m_CurrentState = TestState.HostRunning;
        }

        private void OnClientStarted()
        {
            Log("Client 시작됨, 서버 연결 시도 중...");
        }

        private void OnClientConnected(ulong clientId)
        {
            if (m_IsHost)
            {
                Log($"클라이언트 연결됨: ClientID {clientId}");
                Log($"총 연결된 클라이언트: {m_NetworkManager.ConnectedClients.Count}");
            }
        }

        private void OnClientConnectedToServer(ulong clientId)
        {
            if (!m_IsHost && clientId == m_NetworkManager.LocalClientId)
            {
                Log($"서버에 성공적으로 연결됨! ClientID: {clientId}");
                m_CurrentState = TestState.ClientConnected;
            }
        }

        private void OnClientDisconnected(ulong clientId)
        {
            if (m_IsHost)
            {
                Log($"클라이언트 연결 해제됨: ClientID {clientId}");
                Log($"남은 연결된 클라이언트: {m_NetworkManager.ConnectedClients.Count}");
            }
        }

        private void OnClientDisconnectedFromServer(ulong clientId)
        {
            if (!m_IsHost && clientId == m_NetworkManager.LocalClientId)
            {
                Log("서버와의 연결이 해제됨");
                m_CurrentState = TestState.Idle;
            }
        }

        // ======== 유틸리티 메서드들 ========

        private bool IsParrelSyncClone()
        {
#if UNITY_EDITOR
            try
            {
                var clonesManagerType = System.Type.GetType("ParrelSync.ClonesManager");
                if (clonesManagerType != null)
                {
                    var isCloneMethod = clonesManagerType.GetMethod("IsClone");
                    if (isCloneMethod != null)
                    {
                        return (bool)isCloneMethod.Invoke(null, null);
                    }
                }
            }
            catch
            {
                // ParrelSync가 없으면 무시
            }
#endif
            return false;
        }

        private void UnsubscribeEvents()
        {
            if (m_NetworkManager != null)
            {
                m_NetworkManager.OnServerStarted -= OnHostStarted;
                m_NetworkManager.OnClientStarted -= OnClientStarted;
                m_NetworkManager.OnClientConnectedCallback -= OnClientConnected;
                m_NetworkManager.OnClientConnectedCallback -= OnClientConnectedToServer;
                m_NetworkManager.OnClientDisconnectCallback -= OnClientDisconnected;
                m_NetworkManager.OnClientDisconnectCallback -= OnClientDisconnectedFromServer;
            }
        }

        // ======== GUI ========

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 400, 300));

            GUILayout.Label("=== 로컬 네트워크 테스트 ===");
            GUILayout.Label($"상태: {m_CurrentState}");
            GUILayout.Label($"역할: {(m_IsHost ? "Host" : "Client")}");

            if (m_NetworkManager != null)
            {
                GUILayout.Label($"NetworkManager: {(m_NetworkManager.IsHost ? "Host" : m_NetworkManager.IsClient ? "Client" : "Disconnected")}");
                if (m_NetworkManager.IsListening)
                {
                    GUILayout.Label($"연결된 클라이언트: {m_NetworkManager.ConnectedClients.Count}");
                    if (m_NetworkManager.IsClient)
                    {
                        GUILayout.Label($"내 ClientID: {m_NetworkManager.LocalClientId}");
                    }
                }
            }

            GUILayout.Label($"서버 주소: {serverIP}:{serverPort}");

            if (m_CurrentState == TestState.HostRunning || m_CurrentState == TestState.ClientConnected)
            {
                float duration = Time.time - m_TestStartTime;
                GUILayout.Label($"연결 시간: {duration:F1}초");
            }

            GUILayout.Space(10);

            // 버튼들
            GUI.enabled = m_CurrentState == TestState.Idle;

            if (GUILayout.Button("Host로 시작"))
            {
                StartAsHost();
            }

            if (GUILayout.Button("Client로 시작"))
            {
                StartAsClient();
            }

            GUI.enabled = m_CurrentState != TestState.Idle;

            if (GUILayout.Button("연결 끊기"))
            {
                Disconnect();
            }

            GUI.enabled = true;

            if (GUILayout.Button("테스트 리셋"))
            {
                ResetTest();
            }

            GUILayout.EndArea();
        }

        // ======== 로깅 ========

        private void Log(string message)
        {
            if (enableVerboseLogging)
            {
                Debug.Log($"[LocalNetworkTest] {message}");
            }
        }

        private void LogError(string message)
        {
            Debug.LogError($"[LocalNetworkTest] {message}");
        }

        // ======== 정리 ========

        private void OnDestroy()
        {
            UnsubscribeEvents();

            if (m_NetworkManager != null && m_NetworkManager.IsListening)
            {
                m_NetworkManager.Shutdown();
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;
using UnityEngine;
using Unity.Netcode;
using Unity.Assets.Scripts.UnityServices.Lobbies;

namespace Unity.Assets.Scripts.Network
{
    /// <summary>
    /// 오프라인 상태를 나타내는 클래스 - VContainer 의존성을 제거하고 Managers 패턴으로 리팩토링됨
    /// 연결 상태 패턴의 일부로, 오프라인 상태에서의 동작을 정의
    ///
    /// 이 상태에서는 네트워크 연결이 없는 상태를 관리하며,
    /// 클라이언트나 호스트 연결을 시작하기 위한 진입점 역할을 합니다.
    /// NetworkBehaviour 관련 기능은 비활성화된 상태입니다.
    /// </summary>
    class OfflineStateEx : ConnectionStateEx
    {
        // VContainer 의존성 제거 - 직접 참조로 변경
        private LobbyServiceFacadeEx m_LobbyServiceFacade;

        // 씬 변경 이벤트 구독 여부를 추적
        private bool m_IsSceneEventSubscribed = false;

        // 온라인 연결 가능 여부 확인 주기 (초)
        private const float k_OnlineCheckInterval = 5.0f;

        // 마지막 온라인 연결 확인 시간
        private float m_LastOnlineCheckTime = 0f;

        // 온라인 연결 가능 여부
        private bool m_IsOnlineAvailable = false;

        // 온라인 연결 확인 코루틴
        private Coroutine m_OnlineCheckCoroutine;

        public override void Initialize(ConnectionManagerEx connectionManager)
        {
            base.Initialize(connectionManager);

            // Managers 패턴을 통해 참조 획득
            m_LobbyServiceFacade = Managers.Lobby;
        }

        /// <summary>
        /// 상태 진입 시 호출되는 메서드
        ///
        /// 오프라인 상태로 진입할 때 네트워크 상태를 확인하고,
        /// 필요한 경우 메인 메뉴 씬으로 전환합니다.
        /// 씬 변경 이벤트를 구독하여 MainMenu 씬 로드 시 LobbyConnectingState로 자동 전환합니다.
        /// </summary>
        public override void Enter()
        {
            Debug.Log("[OfflineStateEx] 시작: Enter");

            // 네트워크 상태 확인
            if (m_ConnectionManager.NetworkManager.IsListening)
            {
                Debug.Log("[OfflineStateEx] NetworkManager가 아직 활성 상태입니다. 이전 연결을 정리합니다.");
                m_ConnectionManager.NetworkManager.Shutdown();
            }
            else
            {
                // Debug.Log("[OfflineStateEx] NetworkManager가 이미 비활성 상태입니다.");
            }

            // 로비 서비스 추적 종료 (필요한 경우)
            m_LobbyServiceFacade?.EndTracking();
        }

        /// <summary>
        /// 인터넷 연결 가능 여부를 확인하는 메서드
        /// </summary>
        private bool CheckInternetConnection()
        {
            return Application.internetReachability != NetworkReachability.NotReachable;
        }

        public override void Exit()
        {
            Debug.Log("[OfflineStateEx] 종료: Exit");
        }

        public override void StartClientLobby(string playerName)
        {
            GameLogger.Progress("OfflineStateEx", $"Client 연결 요청: {playerName}");
            
            // ✅ 1단계: 인터넷 연결 확인
            if (!CheckInternetConnection())
            {
                GameLogger.Error("OfflineStateEx", "❌ 인터넷 연결 없음! Client 연결 중단");
                PublishConnectStatus(ConnectStatus.StartClientFailed);
                return;
            }
            
            GameLogger.Info("OfflineStateEx", "✅ 인터넷 연결 확인됨, LobbyConnecting 상태로 전환");
            
            // ✅ 2단계: Offline → LobbyConnecting 상태로 전환
            m_ConnectionManager.ChangeState(m_ConnectionManager.m_LobbyConnecting);
            
            // ✅ 3단계: LobbyConnecting 상태에서 StartClientLobby 다시 호출
            m_ConnectionManager.StartClientLobby(playerName);
        }

        public override void StartHostLobby(string playerName)
        {
            GameLogger.Progress("OfflineStateEx", $"Host 시작 요청: {playerName}");
            
            // ✅ 1단계: 인터넷 연결 확인
            if (!CheckInternetConnection())
            {
                GameLogger.Error("OfflineStateEx", "❌ 인터넷 연결 없음! Host 시작 중단");
                PublishConnectStatus(ConnectStatus.StartHostFailed);
                return;
            }
            
            GameLogger.Info("OfflineStateEx", "✅ 인터넷 연결 확인됨, LobbyConnecting 상태로 전환");
            
            // ✅ 2단계: Offline → LobbyConnecting 상태로 전환
            m_ConnectionManager.ChangeState(m_ConnectionManager.m_LobbyConnecting);
            
            // ✅ 3단계: LobbyConnecting 상태에서 StartHostLobby 다시 호출
            m_ConnectionManager.StartHostLobby(playerName);
        }
    }
}
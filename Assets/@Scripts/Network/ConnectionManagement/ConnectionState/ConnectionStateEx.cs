using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;


namespace Unity.Assets.Scripts.Network
{
    /// <summary>
    /// 연결 상태를 나타내는 추상 클래스 - VContainer 의존성을 제거하고 Managers 패턴으로 리팩토링됨
    /// 상태 패턴의 기본 클래스로, 모든 연결 상태는 이 클래스를 상속받음
    ///
    /// 이 클래스는 NetworkBehaviour를 직접 상속하지 않지만, ConnectionManagerEx를 통해
    /// 간접적으로 NetworkBehaviour의 RPC 기능을 사용합니다.
    ///
    /// 각 상태 클래스는 이 클래스를 상속하여 특정 연결 상태에서의 동작을 구현합니다.
    /// 상태 전환은 ConnectionManagerEx.ChangeState() 메서드를 통해 이루어집니다.
    /// </summary>
    public abstract class ConnectionStateEx
    {
        // 의존성 참조 - Managers를 통해 획득
        protected ConnectionManagerEx m_ConnectionManager;
        protected NetworkManager m_NetworkManager;
        protected DebugClassFacadeEx m_DebugClassFacade;

        // VContainer의 [Inject] 제거하고 직접 초기화
        public virtual void Initialize(ConnectionManagerEx connectionManager)
        {
            m_ConnectionManager = connectionManager;

            // Managers를 통해 NetworkManager 직접 참조
            m_NetworkManager = Managers.Network;

            // DebugClassFacade를 Managers에서 가져오기
            m_DebugClassFacade = Managers.Debug;
        }

        /// <summary>
        /// 상태 진입 시 호출되는 메서드
        /// 각 상태 클래스는 이 메서드를 구현하여 상태 진입 시 필요한 작업을 수행합니다.
        /// </summary>
        public virtual void Enter() { }

        /// <summary>
        /// 상태 종료 시 호출되는 메서드
        /// 각 상태 클래스는 이 메서드를 구현하여 상태 종료 시 필요한 정리 작업을 수행합니다.
        /// </summary>
        public virtual void Exit() { }

        /// <summary>
        /// 클라이언트 연결 시 호출되는 메서드
        /// </summary>
        public virtual void OnClientConnected(ulong clientId) { }

        /// <summary>
        /// 클라이언트 연결 해제 시 호출되는 메서드
        /// </summary>
        public virtual void OnClientDisconnect(ulong clientId) { }

        /// <summary>
        /// 서버 시작 시 호출되는 메서드
        /// </summary>
        public virtual void OnServerStarted() { }

        /// <summary>
        /// IP 주소를 통한 클라이언트 연결 시작 메서드
        /// </summary>
        public virtual void StartClientIP(string playerName, string ipaddress, int port) { }

        /// <summary>
        /// 로비를 통한 클라이언트 연결 시작 메서드
        /// </summary>
        public virtual void StartClientLobby(string playerName) { }

        /// <summary>
        /// IP 주소를 통한 호스트 시작 메서드
        /// </summary>
        public virtual void StartHostIP(string playerName, string ipaddress, int port) { }

        /// <summary>
        /// 로비를 통한 호스트 시작 메서드
        /// </summary>
        public virtual void StartHostLobby(string playerName) { }

        /// <summary>
        /// 사용자가 종료 요청 시 호출되는 메서드
        /// </summary>
        public virtual void OnUserRequestedShutdown() { }

        /// <summary>
        /// 연결 승인 검사 메서드
        /// </summary>
        public virtual void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response) { }

        /// <summary>
        /// 전송 실패 시 호출되는 메서드
        /// </summary>
        public virtual void OnTransportFailure() { }

        /// <summary>
        /// 서버 중지 시 호출되는 메서드
        /// </summary>
        public virtual void OnServerStopped() { }

        /// <summary>
        /// 릴레이 연결 시작 메서드
        /// </summary>

        /// <summary>
        /// 플레이어가 게임에 참여할 때 호출되는 메서드
        /// </summary>

        // Managers의 ActionMessageBus를 통한 이벤트 발행
        protected void PublishConnectStatus(ConnectStatus status)
        {
            Debug.Log($"[ConnectionStateEx] ConnectStatus: {status}");

            // ConnectionManagerEx의 이벤트 발행
            if (m_ConnectionManager != null)
            {
                m_ConnectionManager.RaiseConnectionStatusChanged(status);
            }
        }

        protected void PublishConnectionEvent(ConnectionEventMessage message)
        {
            Debug.Log($"[ConnectionStateEx] ConnectionEvent: ClientId={message.ClientId}, Status={message.ConnectStatus}");

            // Managers의 ActionBus를 통해 이벤트 발행
            // ActionId를 정의해야 할 수도 있음
            // 현재는 로그만 출력
        }
    }
}
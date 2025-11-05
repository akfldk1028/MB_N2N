using System;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Netcode;
using Unity.Assets.Scripts.UnityServices.Lobbies;

namespace Unity.Assets.Scripts.Network
{
    /// <summary>
    /// 호스트 시작 상태 클래스 - VContainer 의존성을 제거하고 Managers 패턴으로 리팩토링됨
    ///
    /// 네트워크 호스트 시작 상태를 관리하며, 호스트 초기화 및 연결 승인을 처리합니다.
    /// </summary>
    class StartingHostStateEx : OnlineStateEx
    {
        // VContainer 의존성 제거
        private LobbyServiceFacadeEx m_LobbyServiceFacade;
        private LocalLobbyEx m_LocalLobby;

        ConnectionMethodBaseEx m_ConnectionMethod;

        public override void Initialize(ConnectionManagerEx connectionManager)
        {
            base.Initialize(connectionManager);

            // Managers 패턴을 통해 참조 획득
            m_LobbyServiceFacade = Managers.Lobby;
            m_LocalLobby = Managers.LocalLobby;
        }

        public StartingHostStateEx Configure(ConnectionMethodBaseEx baseConnectionMethod)
        {
            m_ConnectionMethod = baseConnectionMethod;
            return this;
        }

        public override void Enter()
        {
            Debug.Log("[StartingHostStateEx] 호스트 시작 상태 진입");
            StartHostAsync();
        }

        public override void Exit()
        {
            Debug.Log("[StartingHostStateEx] 호스트 시작 상태 종료");
        }

        private async void StartHostAsync()
        {
            try
            {
                if (m_ConnectionMethod != null)
                {
                    GameLogger.Progress("StartingHostStateEx", "ConnectionMethod를 사용하여 호스트 연결 설정 시작");
                    
                    // ✅ Sessions API가 내부적으로 NetworkManager.StartHost()를 자동 호출합니다!
                    // SetupHostConnectionAsync()는:
                    // 1. MultiplayerService.CreateSessionAsync() 호출
                    // 2. 내부에서 transport.SetRelayServerData() 설정 (WebSocket)
                    // 3. 자동으로 NetworkManager.StartHost() 호출
                    await m_ConnectionMethod.SetupHostConnectionAsync();
                    
                    GameLogger.Success("StartingHostStateEx", "Sessions API가 호스트 시작 완료 (자동)");
                    
                    // ✅ NetworkManager가 이미 시작되었는지 확인
                    if (m_ConnectionManager.NetworkManager.IsHost)
                    {
                        GameLogger.Success("StartingHostStateEx", "호스트 상태 확인 완료");
                        PublishConnectStatus(ConnectStatus.Success);
                    }
                    else
                    {
                        GameLogger.Error("StartingHostStateEx", "Sessions API 호출 후에도 호스트 상태가 아님");
                        PublishConnectStatus(ConnectStatus.StartHostFailed);
                        m_ConnectionManager.ChangeState(m_ConnectionManager.m_Offline);
                    }
                }
                else
                {
                    GameLogger.Error("StartingHostStateEx", "ConnectionMethod가 null입니다");
                    PublishConnectStatus(ConnectStatus.StartHostFailed);
                    m_ConnectionManager.ChangeState(m_ConnectionManager.m_Offline);
                }
            }
            catch (Exception e)
            {
                GameLogger.Error("StartingHostStateEx", $"호스트 시작 중 오류 발생: {e.Message}\n{e.StackTrace}");
                PublishConnectStatus(ConnectStatus.StartHostFailed);
                m_ConnectionManager.ChangeState(m_ConnectionManager.m_Offline);
            }
        }

        public override void OnServerStarted()
        {
            Debug.Log("[StartingHostStateEx] 서버 시작 완료, HostingState로 전환");
            m_ConnectionManager.ChangeState(m_ConnectionManager.m_Hosting);
        }

        public override void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
        {
            var connectionData = request.Payload;
            var clientId = request.ClientNetworkId;

            if (connectionData?.Length > 0)
            {
                var payload = System.Text.Encoding.UTF8.GetString(connectionData);
                var connectionPayload = JsonUtility.FromJson<ConnectionPayload>(payload);

                GameLogger.Network("StartingHostStateEx", 
                    $"Client {clientId} 연결 승인 요청: {connectionPayload.playerName} (GUID: {connectionPayload.playerGuid})");

                // ✅ 최대 연결 수 체크
                if (m_ConnectionManager.NetworkManager.ConnectedClients.Count >= m_ConnectionManager.MaxConnectedPlayers)
                {
                    GameLogger.Warning("StartingHostStateEx", $"Client {clientId} 연결 거부: 서버 만원 (정원 {m_ConnectionManager.MaxConnectedPlayers}명)");
                    response.Approved = false;
                    response.Reason = JsonUtility.ToJson(ConnectStatus.ServerFull);
                    return;
                }

                // ✅ SessionManager에 플레이어 중복 연결 체크
                if (Managers.Session.IsDuplicateConnection(connectionPayload.playerGuid))
                {
                    GameLogger.Warning("StartingHostStateEx", 
                        $"Client {clientId} 연결 거부: 중복 연결 (GUID: {connectionPayload.playerGuid})");
                    response.Approved = false;
                    response.Reason = JsonUtility.ToJson(ConnectStatus.GenericDisconnect);
                    return;
                }

                // ✅ SessionManager에 플레이어 데이터 등록
                var sessionData = new SessionPlayerDataEx(
                    clientId, 
                    connectionPayload.playerName, 
                    new NetworkGuid(), // Avatar GUID는 나중에 설정
                    currentHitPoints: 100, 
                    isConnected: true,
                    hasCharacterSpawned: false
                );
                
                Managers.Session.SetupConnectingPlayerSessionData(
                    clientId, 
                    connectionPayload.playerGuid, 
                    sessionData
                );

                GameLogger.Success("StartingHostStateEx", 
                    $"Client {clientId} 연결 승인 및 세션 등록 완료: {connectionPayload.playerName}");

                response.Approved = true;
                response.CreatePlayerObject = true;
            }
            else
            {
                GameLogger.Warning("StartingHostStateEx", $"Client {clientId} 연결 거부: 페이로드 없음");
                response.Approved = false;
                response.Reason = JsonUtility.ToJson(ConnectStatus.GenericDisconnect);
            }
        }
    }
}
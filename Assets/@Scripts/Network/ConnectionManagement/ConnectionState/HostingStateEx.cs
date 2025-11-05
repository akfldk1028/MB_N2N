using System;
using Unity.Netcode;
using UnityEngine;
using Unity.Assets.Scripts.UnityServices.Lobbies;
// using Unity.Assets.Scripts.Scene;
using System.Collections;
using Unity.Assets.Scripts.Network;

/// <summary>
/// Connection state corresponding to a listening host - VContainer 의존성을 제거하고 Managers 패턴으로 리팩토링됨
/// Handles incoming client connections. When shutting down or being timed out, transitions to the Offline state.
/// </summary>
class HostingStateEx : OnlineStateEx
{
    // Managers를 통해 참조 획득
    protected LocalLobbyEx m_LocalLobby;
    private SceneManagerEx _sceneManagerEx;
    private LobbyServiceFacadeEx m_LobbyServiceFacade;

    // 연결 상태 체크 코루틴
    private Coroutine m_ConnectionStatusCheckCoroutine;
    private float lastSceneLoadAttemptTime = 0f;

    public override void Initialize(ConnectionManagerEx connectionManager)
    {
        base.Initialize(connectionManager);

        // Managers를 통해 참조 획득
        m_LocalLobby = Managers.LocalLobby;
        _sceneManagerEx = Managers.Scene;
        m_LobbyServiceFacade = Managers.Lobby;
        // DebugClassFacade는 이미 base.Initialize에서 설정됨
    }

    public override void Enter()
    {
        Debug.Log("[HostingStateEx] 호스트 상태 진입");

        if (m_LobbyServiceFacade?.CurrentUnityLobby != null)
        {
            m_LobbyServiceFacade.BeginTracking();
        }

        m_ConnectionStatusCheckCoroutine = m_ConnectionManager.StartCoroutine(ConnectionStatusCheck());
    }

    public override void Exit()
    {
        Debug.Log("[HostingStateEx] 호스트 상태 종료");

        if (m_ConnectionStatusCheckCoroutine != null)
        {
            m_ConnectionManager.StopCoroutine(m_ConnectionStatusCheckCoroutine);
            m_ConnectionStatusCheckCoroutine = null;
        }
    }

    private IEnumerator ConnectionStatusCheck()
    {
        while (true)
        {
            yield return new WaitForSeconds(3.0f);

            // if (m_LocalLobby != null && m_LocalLobby.LobbyUsers.Count >= m_ConnectionManager.MaxConnectedPlayers)
            // {
            //     if (Time.time - lastSceneLoadAttemptTime > 5.0f)
            //     {
            //         Debug.Log("[HostingStateEx] 플레이어 수 충족 - 게임 씬으로 전환");
            //         _sceneManagerEx?.LoadScene(EScene.BasicGame.ToString(), useNetworkSceneManager: true);
            //         lastSceneLoadAttemptTime = Time.time;
            //         yield break;
            //     }
            // }
        }
    }

    public override void OnClientConnected(ulong clientId)
    {
        Debug.Log($"[HostingStateEx] 클라이언트 {clientId} 연결됨");

        var connectionEventMessage = new ConnectionEventMessage()
        {
            ClientId = clientId,
            ConnectStatus = ConnectStatus.Success
        };

        PublishConnectionEvent(connectionEventMessage);
    }

    public override void OnClientDisconnect(ulong clientId)
    {
        GameLogger.Warning("HostingStateEx", $"Client {clientId} 연결 해제");

        // ✅ SessionManager에 플레이어 연결 해제 알림
        Managers.Session.DisconnectClient(clientId);

        var connectionEventMessage = new ConnectionEventMessage()
        {
            ClientId = clientId,
            ConnectStatus = ConnectStatus.GenericDisconnect
        };

        PublishConnectionEvent(connectionEventMessage);
    }

    public override void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        var connectionData = request.Payload;
        var clientId = request.ClientNetworkId;

        if (connectionData?.Length > 0)
        {
            var payload = System.Text.Encoding.UTF8.GetString(connectionData);
            var connectionPayload = JsonUtility.FromJson<ConnectionPayload>(payload);

            GameLogger.Network("HostingStateEx", 
                $"Client {clientId} 연결 승인 요청: {connectionPayload.playerName} (GUID: {connectionPayload.playerGuid})");

            // ✅ SessionManager에 플레이어 중복 연결 체크
            if (Managers.Session.IsDuplicateConnection(connectionPayload.playerGuid))
            {
                GameLogger.Warning("HostingStateEx", 
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

            GameLogger.Success("HostingStateEx", 
                $"Client {clientId} 연결 승인 및 세션 등록 완료: {connectionPayload.playerName}");

            response.Approved = true;
            response.CreatePlayerObject = true;
        }
        else
        {
            GameLogger.Warning("HostingStateEx", $"Client {clientId} 연결 거부: 페이로드 없음");
            response.Approved = false;
        }
    }
}
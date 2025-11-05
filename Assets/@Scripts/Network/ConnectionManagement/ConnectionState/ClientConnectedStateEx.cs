using Unity.Assets.Scripts.Network;
using Unity.Assets.Scripts.UnityServices.Lobbies;
using UnityEngine;

/// <summary>
/// Connection state corresponding to a connected client - VContainer 의존성을 제거하고 Managers 패턴으로 리팩토링됨
/// When being disconnected, transitions to the ClientReconnecting state if no reason is given, or to the Offline state.
/// </summary>
class ClientConnectedStateEx : OnlineStateEx
{
    // VContainer 의존성 제거 - 직접 참조로 변경
    protected LobbyServiceFacadeEx m_LobbyServiceFacade;
    private SceneManagerEx _sceneManagerEx;
    private LocalLobbyEx m_LocalLobby;

    public override void Initialize(ConnectionManagerEx connectionManager)
    {
        base.Initialize(connectionManager);

        // Managers 패턴을 통해 참조 획득
        m_LobbyServiceFacade = Managers.Lobby;
        _sceneManagerEx = Managers.Scene;
        m_LocalLobby = Managers.LocalLobby;
    }

    public override void Enter()
    {
        Debug.Log("[ClientConnectedStateEx] 클라이언트 연결 상태 진입");
        if (m_LobbyServiceFacade?.CurrentUnityLobby != null)
        {
            m_LobbyServiceFacade.BeginTracking();
        }
        // if (m_LocalLobby.LobbyUsers.Count >= m_ConnectionManager.MaxConnectedPlayers)
        // {
        //     Debug.Log("[ClientConnectedStateEx] 플레이어 수 충족 - 게임 씬으로 전환");
        //     _sceneManagerEx.LoadScene(EScene.BasicGame.ToString(), useNetworkSceneManager: true);
        // }
    }

    public override void Exit() { }

    public override void OnClientDisconnect(ulong _)
    {
        var disconnectReason = m_ConnectionManager.NetworkManager.DisconnectReason;
        if (string.IsNullOrEmpty(disconnectReason) ||
            disconnectReason == "Disconnected due to host shutting down.")
        {
            PublishConnectStatus(ConnectStatus.Reconnecting);
            m_ConnectionManager.ChangeState(m_ConnectionManager.m_ClientReconnecting);
        }
        else
        {
            var connectStatus = JsonUtility.FromJson<ConnectStatus>(disconnectReason);
            PublishConnectStatus(connectStatus);
            m_ConnectionManager.ChangeState(m_ConnectionManager.m_Offline);
        }
    }
}
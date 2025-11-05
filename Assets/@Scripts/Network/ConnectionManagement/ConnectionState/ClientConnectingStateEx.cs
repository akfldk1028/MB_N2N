using System;
using System.Collections;
using System.Threading.Tasks;
using Unity.Assets.Scripts.Network;
using Unity.Assets.Scripts.UnityServices.Lobbies;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

/// <summary>
/// Connection state corresponding to when a client is attempting to connect to a server - VContainer 의존성을 제거하고 Managers 패턴으로 리팩토링됨
/// Starts the client when entering. If successful, transitions to the ClientConnected state. If not, transitions to the Offline state.
/// </summary>
class ClientConnectingStateEx : OnlineStateEx
{
    protected ConnectionMethodBaseEx m_ConnectionMethod;

    // Managers를 통해 참조 획득
    protected LocalLobbyEx m_LocalLobby;
    private SceneManagerEx _sceneManagerEx;
    private LobbyServiceFacadeEx m_LobbyServiceFacade;

    // 연결 시도 타임아웃을 관리하기 위한 필드 추가
    private Coroutine m_ConnectionTimeoutCoroutine;
    private const float CONNECTION_TIMEOUT = 30.0f; // 30초 타임아웃
    private bool m_ConnectionTimeoutTriggered = false;

    public override void Initialize(ConnectionManagerEx connectionManager)
    {
        base.Initialize(connectionManager);

        // Managers를 통해 참조 획득
        m_LocalLobby = Managers.LocalLobby;
        _sceneManagerEx = Managers.Scene;
        m_LobbyServiceFacade = Managers.Lobby;
    }

    public ClientConnectingStateEx Configure(ConnectionMethodBaseEx baseConnectionMethod)
    {
        m_ConnectionMethod = baseConnectionMethod;
        return this;
    }

    public override void Enter()
    {
        m_ConnectionTimeoutTriggered = false;
        Debug.Log($"<color=green>[ClientConnectingStateEx] Enter 호출됨</color>");

        ConnectClientAsync();
        m_ConnectionTimeoutCoroutine = m_ConnectionManager.StartCoroutine(ConnectionTimeoutCheck());
    }

    public override void Exit()
    {
        if (m_ConnectionTimeoutCoroutine != null)
        {
            m_ConnectionManager.StopCoroutine(m_ConnectionTimeoutCoroutine);
            m_ConnectionTimeoutCoroutine = null;
        }
        Debug.Log($"<color=green>[ClientConnectingStateEx] Exit 호출됨</color>");
    }

    private IEnumerator ConnectionTimeoutCheck()
    {
        Debug.Log($"<color=green>[ClientConnectingStateEx] 연결 타임아웃 체크 시작: {CONNECTION_TIMEOUT}초</color>");
        yield return new WaitForSeconds(CONNECTION_TIMEOUT);

        if (!m_ConnectionTimeoutTriggered)
        {
            Debug.LogWarning($"<color=red>[ClientConnectingStateEx] 연결 타임아웃 발생 ({CONNECTION_TIMEOUT}초)</color>");
            m_ConnectionTimeoutTriggered = true;
            PublishConnectStatus(ConnectStatus.StartClientFailed);
            m_ConnectionManager.ChangeState(m_ConnectionManager.m_Offline);
        }
    }

    private async void ConnectClientAsync()
    {
        try
        {
            if (m_ConnectionMethod != null)
            {
                GameLogger.Progress("ClientConnectingStateEx", "ConnectionMethod를 사용하여 클라이언트 연결 설정 시작");
                
                // ✅ Sessions API가 내부적으로 NetworkManager.StartClient()를 자동 호출합니다!
                // SetupClientConnectionAsync()는:
                // 1. Relay 세션 정보 가져오기
                // 2. transport.SetRelayServerData() 설정 (WebSocket)
                // 3. 자동으로 NetworkManager.StartClient() 호출
                await m_ConnectionMethod.SetupClientConnectionAsync();
                
                GameLogger.Success("ClientConnectingStateEx", "Sessions API가 클라이언트 시작 완료 (자동)");
                
                // ✅ NetworkManager가 이미 시작되었는지 확인
                if (!m_ConnectionTimeoutTriggered)
                {
                    if (m_ConnectionManager.NetworkManager.IsClient)
                    {
                        GameLogger.Success("ClientConnectingStateEx", "클라이언트 상태 확인 완료");
                    }
                    else
                    {
                        GameLogger.Error("ClientConnectingStateEx", "Sessions API 호출 후에도 클라이언트 상태가 아님");
                        PublishConnectStatus(ConnectStatus.StartClientFailed);
                        m_ConnectionManager.ChangeState(m_ConnectionManager.m_Offline);
                    }
                }
            }
            else
            {
                GameLogger.Error("ClientConnectingStateEx", "ConnectionMethod가 null입니다");
                PublishConnectStatus(ConnectStatus.StartClientFailed);
                m_ConnectionManager.ChangeState(m_ConnectionManager.m_Offline);
            }
        }
        catch (Exception e)
        {
            GameLogger.Error("ClientConnectingStateEx", $"클라이언트 연결 중 오류 발생: {e.Message}\n{e.StackTrace}");
            PublishConnectStatus(ConnectStatus.StartClientFailed);
            m_ConnectionManager.ChangeState(m_ConnectionManager.m_Offline);
        }
    }

    public override void OnClientConnected(ulong clientId)
    {
        Debug.Log($"<color=green>[ClientConnectingStateEx] 클라이언트 {clientId} 연결됨</color>");
        if (clientId == m_ConnectionManager.NetworkManager.LocalClientId)
        {
            m_ConnectionTimeoutTriggered = true;
            PublishConnectStatus(ConnectStatus.Success);
            m_ConnectionManager.ChangeState(m_ConnectionManager.m_ClientConnected);
        }
    }

    public override void OnClientDisconnect(ulong clientId)
    {
        Debug.Log($"<color=red>[ClientConnectingStateEx] 클라이언트 {clientId} 연결 해제됨</color>");
        if (clientId == m_ConnectionManager.NetworkManager.LocalClientId)
        {
            var disconnectReason = m_ConnectionManager.NetworkManager.DisconnectReason;
            Debug.Log($"<color=red>[ClientConnectingStateEx] 연결 해제 이유: {disconnectReason}</color>");

            if (string.IsNullOrEmpty(disconnectReason))
            {
                PublishConnectStatus(ConnectStatus.StartClientFailed);
            }
            else
            {
                var connectStatus = JsonUtility.FromJson<ConnectStatus>(disconnectReason);
                PublishConnectStatus(connectStatus);
            }

            m_ConnectionManager.ChangeState(m_ConnectionManager.m_Offline);
        }
    }
}
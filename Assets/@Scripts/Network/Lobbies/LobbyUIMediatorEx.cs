using System;
using TMPro;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;
// using VContainer;
using Unity.Assets.Scripts.UnityServices.Lobbies;
using Unity.Assets.Scripts.Network;
using System.Threading.Tasks;
using System.Collections;

public class LobbyUIMediatorEx : MonoBehaviour
{
    // 로비 생성 완료 이벤트
    public event Action OnLobbyCreated;

    // 로비 참가 완료 이벤트
    public event Action OnLobbyJoined;

    // VContainer 의존성 제거 - Initialize 패턴으로 변경
    private AuthManager m_AuthManager;
    private LobbyServiceFacadeEx m_LobbyServiceFacade;
    private LocalLobbyUserEx m_LocalUser;
    private LocalLobbyEx m_LocalLobby;
    private ConnectionManagerEx m_ConnectionManager;
    private DebugClassFacadeEx m_DebugClassFacadeEx;
    private SceneManagerEx m_SceneManagerEx;

    // VContainer ISubscriber 대신 직접 이벤트 구독
    public event Action<ConnectStatus> OnConnectStatusReceived;

    const string k_DefaultLobbyName = "no-name";

    // VContainer 의존성 제거 - Initialize 패턴 구현
    public virtual void Initialize(
        AuthManager authManager,
        LobbyServiceFacadeEx lobbyServiceFacade,
        LocalLobbyUserEx localUser,
        LocalLobbyEx localLobby,
        ConnectionManagerEx connectionManager,
        SceneManagerEx sceneManagerEx,
        DebugClassFacadeEx debugClassFacade)
    {
        Debug.Log("<color=green>[LobbyUIMediatorEx] Initialize 시작</color>");

        m_AuthManager = authManager;
        m_LobbyServiceFacade = lobbyServiceFacade;
        m_LocalUser = localUser;
        m_LocalLobby = localLobby;
        m_ConnectionManager = connectionManager;
        m_SceneManagerEx = sceneManagerEx;
        m_DebugClassFacadeEx = debugClassFacade;

        RegenerateName();

        // VContainer ISubscriber 대신 직접 이벤트 구독
        OnConnectStatusReceived += OnConnectStatus;

        Debug.Log("<color=green>[LobbyUIMediatorEx] Initialize 완료</color>");
    }

    void OnConnectStatus(ConnectStatus status)
    {
        Debug.Log($"<color=green>[LobbyUIMediatorEx] OnConnectStatus: {status}</color>");
        if (status is ConnectStatus.GenericDisconnect or ConnectStatus.StartClientFailed)
        {
            UnblockUIAfterLoadingIsComplete();
        }
    }

    public async void ClosedLobbyByHost()
    {
        Debug.Log("<color=green>[LobbyUIMediatorEx] ClosedLobbyByHost 호출</color>");
        // await m_LobbyServiceFacade.DeleteLobbyAsync();
        UnblockUIAfterLoadingIsComplete();

        // Legacy Lobby API 코드 제거됨 - Sessions API로 대체됨
    }

    void OnDestroy()
    {
        Debug.Log("<color=green>[LobbyUIMediatorEx] OnDestroy 호출</color>");
        if (OnConnectStatusReceived != null)
        {
            OnConnectStatusReceived -= OnConnectStatus;
        }
    }

    //Lobby and Relay calls done from UI

    public async void CreateLobbyRequest(string lobbyName, bool isPrivate)
    {
        Debug.Log($"<color=green>[LobbyUIMediatorEx] CreateLobbyRequest 호출 - 로비명: {lobbyName}, 비공개: {isPrivate}</color>");

        // before sending request to lobby service, populate an empty lobby name, if necessary
        if (string.IsNullOrEmpty(lobbyName))
        {
            lobbyName = k_DefaultLobbyName;
        }

        BlockUIWhileLoadingIsInProgress();

        bool playerIsAuthorized = await m_AuthManager.EnsurePlayerIsAuthorized();

        if (!playerIsAuthorized)
        {
            UnblockUIAfterLoadingIsComplete();
            return;
        }

        var sessionCreationAttempt = await m_LobbyServiceFacade.TryCreateSessionAsync(lobbyName, m_LobbyServiceFacade.MaxConnectedPlayers, isPrivate);

        if (sessionCreationAttempt.Success)
        {
            m_LocalUser.IsHost = true;
            m_LobbyServiceFacade.SetRemoteSession(sessionCreationAttempt.Session);

            m_DebugClassFacadeEx.LogInfo(GetType().Name, $"Created lobby with ID: {m_LocalLobby.LobbyID} and code {m_LocalLobby.LobbyCode}");
            m_ConnectionManager.StartHostLobby(m_LocalUser.DisplayName);

            // 로비 생성 완료 이벤트 발생
            OnLobbyCreated?.Invoke();
        }
        // else
        // {
        //     UnblockUIAfterLoadingIsComplete();
        // }
    }

    public async Task<(bool Success, ISession Session)> QueryLobbiesRequest(bool blockUI)
    {
        m_DebugClassFacadeEx.LogInfo(GetType().Name, $"로비 검색 요청 - UI 블록: {blockUI}");
        if (Unity.Services.Core.UnityServices.State != ServicesInitializationState.Initialized)
        {
            m_DebugClassFacadeEx.LogWarning(GetType().Name, $"Unity Services가 초기화되지 않음");
            return (false, null);
        }

        bool playerIsAuthorized = await m_AuthManager.EnsurePlayerIsAuthorized();

        if (blockUI && !playerIsAuthorized)
        {
            m_DebugClassFacadeEx.LogWarning(GetType().Name, $"플레이어 인증 실패");
            UnblockUIAfterLoadingIsComplete();
            return (false, null);
        }

        var (success, session) = await m_LobbyServiceFacade.FindAvailableLobby();
        m_DebugClassFacadeEx.LogInfo(GetType().Name, $"세션 검색 완료 - 성공: {success}");

        return (success, session);
    }

    public async void JoinLobbyWithCodeRequest(string lobbyCode)
    {
        Debug.Log($"<color=green>[LobbyUIMediatorEx] JoinLobbyWithCodeRequest 호출 - 로비 코드: {lobbyCode}</color>");

        BlockUIWhileLoadingIsInProgress();

        bool playerIsAuthorized = await m_AuthManager.EnsurePlayerIsAuthorized();
        m_DebugClassFacadeEx.LogInfo(GetType().Name, $"JoinLobbyWithCodeRequest 요청 - 플레이어 인증 결과: {playerIsAuthorized}");
        m_DebugClassFacadeEx.LogInfo(GetType().Name, $"JoinLobbyWithCodeRequest 요청 - 로비 코드: {lobbyCode}");
        if (!playerIsAuthorized)
        {
            UnblockUIAfterLoadingIsComplete();
            return;
        }

        m_DebugClassFacadeEx.LogInfo(GetType().Name, $"세션 참가 요청 - 세션 코드: {lobbyCode}");
        var (success, session) = await m_LobbyServiceFacade.TryJoinSessionAsync(null, lobbyCode);
        m_DebugClassFacadeEx.LogInfo(GetType().Name, $"세션 참가 요청 결과 - 성공: {success}");
        m_DebugClassFacadeEx.LogInfo(GetType().Name, $"JoinLobbyWithCodeRequest 요청 - 세션 코드: {lobbyCode}");

        if (success == true && session != null)
        {
            OnJoinedLobby(session);
        }
        else
        {
            UnblockUIAfterLoadingIsComplete();
        }
    }

    public async void JoinLobbyRequest(LocalLobbyEx lobby)
    {
        Debug.Log($"<color=green>[LobbyUIMediatorEx] JoinLobbyRequest 호출</color>");

        BlockUIWhileLoadingIsInProgress();

        bool playerIsAuthorized = await m_AuthManager.EnsurePlayerIsAuthorized();

        if (!playerIsAuthorized)
        {
            UnblockUIAfterLoadingIsComplete();
            return;
        }

        var result = await m_LobbyServiceFacade.TryJoinSessionAsync(lobby.LobbyID, lobby.LobbyCode);

        if (result.Success)
        {
            OnJoinedLobby(result.Session);
        }
        else
        {
            UnblockUIAfterLoadingIsComplete();
        }
    }

    public async void QuickJoinRequest()
    {
        Debug.Log($"<color=green>[LobbyUIMediatorEx] QuickJoinRequest 호출</color>");

        BlockUIWhileLoadingIsInProgress();

        bool playerIsAuthorized = await m_AuthManager.EnsurePlayerIsAuthorized();
        m_DebugClassFacadeEx.LogInfo(GetType().Name, $"플레이어 인증 결과: {playerIsAuthorized}아 시방방");
        if (!playerIsAuthorized)
        {
            UnblockUIAfterLoadingIsComplete();
            return;
        }

        var result = await m_LobbyServiceFacade.TryQuickJoinSessionAsync();
        m_DebugClassFacadeEx.LogInfo(GetType().Name, $"QuickJoinRequest() 세션 참가 요청 결과 - 성공: {result.Success}");
        if (result.Success)
        {
            OnJoinedLobby(result.Session);
        }
        else
        {
            UnblockUIAfterLoadingIsComplete();
        }
    }

    void OnJoinedLobby(ISession remoteSession)
    {
        Debug.Log($"<color=green>[LobbyUIMediatorEx] OnJoinedLobby 호출</color>");

        m_LobbyServiceFacade.SetRemoteSession(remoteSession);
        m_DebugClassFacadeEx.LogInfo(GetType().Name, $" Joined session with code: {m_LocalLobby.LobbyCode}");

        // 로비 참가 완료 이벤트 발생
        OnLobbyJoined?.Invoke();

        if (m_LocalUser.IsHost)
        {
            m_ConnectionManager.StartHostLobby(m_LocalUser.DisplayName);
        }
        else
        {
            m_ConnectionManager.StartClientLobby(m_LocalUser.DisplayName);
        }
    }

    public void RegenerateName()
    {
        Debug.Log($"<color=green>[LobbyUIMediatorEx] RegenerateName 호출</color>");
        m_LocalUser.DisplayName = "Player";
    }

    public static event Action<bool> OnWaitingStateChanged; // true: 대기 시작, false: 대기 종료

    public void BlockUIWhileLoadingIsInProgress()
    {
        Debug.Log($"<color=green>[LobbyUIMediatorEx] BlockUIWhileLoadingIsInProgress 호출</color>");
        OnWaitingStateChanged?.Invoke(true);
        StartCoroutine(WaitForNetworkConnection());
    }

    private IEnumerator WaitForNetworkConnection()
    {
        // 네트워크 연결이 완료될 때까지 대기
        while (m_ConnectionManager.NetworkManager != null && !m_ConnectionManager.NetworkManager.IsConnectedClient)
        {
            m_DebugClassFacadeEx.LogInfo(GetType().Name, $"네트워크 연결 대기 중...");
            yield return new WaitForSeconds(0.5f);
        }

        // 네트워크 연결이 실패하거나 타임아웃된 경우 처리
        if (m_ConnectionManager.NetworkManager == null || !m_ConnectionManager.NetworkManager.IsConnectedClient)
        {
            m_DebugClassFacadeEx.LogWarning(GetType().Name, $"네트워크 연결 실패 또는 타임아웃");
            UnblockUIAfterLoadingIsComplete();
        }
        else
        {
            m_DebugClassFacadeEx.LogInfo(GetType().Name, $"네트워크 연결 완료");
            // 연결이 완료된 후에도 UI는 차단된 상태로 유지
            // 실제 작업(로비 생성/참가 등)이 완료된 후 UnblockUIAfterLoadingIsComplete()가 호출될 것임
        }
    }

    void UnblockUIAfterLoadingIsComplete()
    {
        Debug.Log($"<color=green>[LobbyUIMediatorEx] UnblockUIAfterLoadingIsComplete 호출</color>");
        //this callback can happen after we've already switched to a different scene
        //in that case the canvas group would be null
        // if (m_CanvasGroup != null)
        // {
        //     m_CanvasGroup.interactable = true;
        //     m_LoadingSpinner.SetActive(false);
        // }
        OnWaitingStateChanged?.Invoke(false);
    }
}
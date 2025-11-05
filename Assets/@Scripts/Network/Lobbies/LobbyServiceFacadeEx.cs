using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Multiplayer;
using UnityEngine;
// using VContainer;
// using VContainer.Unity;
using Unity.Assets.Scripts.Network;
using Unity.Assets.Scripts.Network.Interfaces;
using Unity.Netcode;

/// <summary>
/// An abstraction layer between the direct calls into the Sessions API and the outcomes you actually want.
/// Migrated from legacy Lobby API to Sessions API - VContainer 의존성을 제거하고 Managers 패턴으로 리팩토링됨
/// </summary>

namespace Unity.Assets.Scripts.UnityServices.Lobbies{
    public class LobbyServiceFacadeEx : ILobbyService, IDisposable
    {
        // VContainer 의존성 제거 - Initialize 패턴으로 변경
        // TODO: [개선필요] 생성자 기반 초기화로 변경
        // Unity Services Sessions API 베스트 프랙티스:
        // 서비스는 생성 시점에 모든 의존성을 받아야 함
        //
        // 현재 문제점:
        // 1. Initialize() 메서드를 수동으로 호출해야 함
        // 2. 초기화 전까지 서비스 사용 불가
        // 3. 초기화 상태 추적 어려움
        //
        // 개선 방안:
        // public LobbyServiceFacadeEx() {
        //     m_DebugClassFacadeEx = Managers.Debug;
        //     m_UpdateRunner = Managers.UpdateRunner;
        //     m_LocalLobby = Managers.LocalLobby;
        //     m_LocalUser = Managers.LocalUser;
        //     m_SceneManagerEx = Managers.Scene;
        //     m_NetworkManager = Managers.Network;
        //     InitializeInternal();
        // }
        //
        // Managers에서:
        // _lobbyServiceFacade = new LobbyServiceFacadeEx();
        // (자동 초기화됨)

        private DebugClassFacadeEx m_DebugClassFacadeEx;
        private UpdateRunnerEx m_UpdateRunner;
        private LocalLobbyEx m_LocalLobby;
        private LocalLobbyUserEx m_LocalUser;
        private SceneManagerEx m_SceneManagerEx;
        private NetworkManager m_NetworkManager;

        const float k_HeartbeatPeriod = 8; // Sessions API manages heartbeat automatically, but keeping for compatibility
        float m_HeartbeatTime = 0;
        
        /// <summary>
        /// 랜덤 매칭 플레이어 수 (항상 2명 고정)
        /// </summary>
        public int MaxConnectedPlayers = Define.MATCHMAKING_PLAYER_COUNT;

        LobbyAPIInterfaceEx m_LobbyApiInterface;

        RateLimitCooldown m_RateLimitQuery;
        RateLimitCooldown m_RateLimitJoin;
        RateLimitCooldown m_RateLimitQuickJoin;
        RateLimitCooldown m_RateLimitHost;

        public ISession CurrentUnityLobby { get; private set; }

        bool m_IsTracking = false;

        // VContainer의 IPublisher 대신 직접 이벤트 발행
        public event Action<LobbyListFetchedMessageEx> OnLobbyListFetched;

        // VContainer 의존성 제거 - Initialize 패턴 구현
        public virtual void Initialize(
            DebugClassFacadeEx debugClassFacade,
            UpdateRunnerEx updateRunner,
            LocalLobbyEx localLobby,
            LocalLobbyUserEx localUser,
            SceneManagerEx sceneManagerEx,
            NetworkManager networkManager)
        {
            Debug.Log("<color=cyan>[LobbyServiceFacadeEx] Initialize 시작</color>");

            m_DebugClassFacadeEx = debugClassFacade;
            m_UpdateRunner = updateRunner;
            m_LocalLobby = localLobby;
            m_LocalUser = localUser;
            m_SceneManagerEx = sceneManagerEx;
            m_NetworkManager = networkManager;

            m_LobbyApiInterface = new LobbyAPIInterfaceEx();

            //See https://docs.unity.com/lobby/rate-limits.html
            m_RateLimitQuery = new RateLimitCooldown(1f);
            m_RateLimitJoin = new RateLimitCooldown(3f);
            m_RateLimitQuickJoin = new RateLimitCooldown(10f);
            m_RateLimitHost = new RateLimitCooldown(3f);

            Debug.Log("<color=cyan>[LobbyServiceFacadeEx] Initialize 완료</color>");
        }

        public void Dispose()
        {
            Debug.Log("<color=cyan>[LobbyServiceFacadeEx] Dispose 호출</color>");
            EndTracking();
        }

        public void SetRemoteSession(ISession session)
        {
            Debug.Log("<color=cyan>[LobbyServiceFacadeEx] SetRemoteSession 호출</color>");
            CurrentUnityLobby = session;
            m_LocalLobby.ApplyRemoteData(session);
        }

        /// <summary>
        /// Initiates tracking of joined lobby's events. The host also starts sending heartbeat pings here.
        /// </summary>
        public void BeginTracking()
        {
            if (!m_IsTracking)
            {
                Debug.Log("<color=cyan>[LobbyServiceFacadeEx] BeginTracking 시작</color>");
                m_IsTracking = true;
                SubscribeToJoinedSessionAsync();

                // Only the host sends heartbeat pings to the service to keep the lobby alive
                if (m_LocalUser.IsHost)
                {
                    m_HeartbeatTime = 0;
                    m_UpdateRunner.Subscribe(DoLobbyHeartbeat, 1.5f);
                }
            }
        }

        /// <summary>
        /// Ends tracking of joined lobby's events and leaves or deletes the lobby. The host also stops sending heartbeat pings here.
        /// </summary>
        public void EndTracking()
        {
            if (m_IsTracking)
            {
                Debug.Log("<color=cyan>[LobbyServiceFacadeEx] EndTracking 시작</color>");
                m_IsTracking = false;
                UnsubscribeToJoinedSessionAsync();

                // Only the host sends heartbeat pings to the service to keep the lobby alive
                if (m_LocalUser.IsHost)
                {
                    m_UpdateRunner.Unsubscribe(DoLobbyHeartbeat);
                }
            }

            if (CurrentUnityLobby != null)
            {
                if (m_LocalUser.IsHost)
                {
                    DeleteSessionAsync();
                }
                else
                {
                    LeaveLobbyAsync();
                }
            }
        }

        /// <summary>
        /// Attempt to create a new session and then join it.
        /// </summary>
        // Legacy 호환 메서드
        public Task<ISession> CreateLobbyAsync(string lobbyName, int maxPlayers, bool isPrivate = false) =>
            TryCreateSessionAsync(lobbyName, maxPlayers, isPrivate).ContinueWith(t => t.Result.Session);

        public async Task<(bool Success, ISession Session)> TryCreateSessionAsync(string sessionName, int maxPlayers, bool isPrivate)
        {
            if (!m_RateLimitHost.CanCall)
            {
                Debug.LogWarning("[LobbyServiceFacadeEx] Create Session hit the rate limit.");
                return (false, null);
            }

            try
            {
                var session = await m_LobbyApiInterface.CreateSession(AuthenticationService.Instance.PlayerId, sessionName, maxPlayers, isPrivate, m_LocalUser.GetDataForUnityServices(), null);
                return (true, session);
            }
            catch (Exception e)
            {
                Debug.LogError($"[LobbyServiceFacadeEx] Create Session failed: {e.Message}");
                m_RateLimitHost.PutOnCooldown();
                PublishError(e);
            }

            return (false, null);
        }

        /// <summary>
        /// Attempt to join an existing session. Will try to join via code, if code is null - will try to join via ID.
        /// </summary>
        public async Task<(bool Success, ISession Session)> TryJoinSessionAsync(string sessionId, string sessionCode)
        {
            if (!m_RateLimitJoin.CanCall ||
                (sessionId == null && sessionCode == null))
            {
                Debug.LogWarning("[LobbyServiceFacadeEx] Join Session hit the rate limit.");
                return (false, null);
            }
            Debug.Log($"[LobbyServiceFacadeEx] 세션 참가 요청 - 세션 아이디: {sessionId}, 세션 코드: {sessionCode}");
            try
            {
                if (!string.IsNullOrEmpty(sessionCode))
                {
                    var session = await m_LobbyApiInterface.JoinSessionByCode(AuthenticationService.Instance.PlayerId, sessionCode, m_LocalUser.GetDataForUnityServices());
                    Debug.Log($"[LobbyServiceFacadeEx] 세션 {session.Id}에 참가합니다");
                    return (true, session);
                }
                else
                {
                    var session = await m_LobbyApiInterface.JoinSessionById(AuthenticationService.Instance.PlayerId, sessionId, m_LocalUser.GetDataForUnityServices());
                    Debug.Log($"[LobbyServiceFacadeEx] 세션 {session.Id}에 참가합니다");
                    return (true, session);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[LobbyServiceFacadeEx] 세션 참가 중 오류 발생 - 세션 아이디: {sessionId}, 세션 코드: {sessionCode}, 오류: {e.Message}");
                m_RateLimitJoin.PutOnCooldown();
                PublishError(e);
            }

            return (false, null);
        }

        /// <summary>
        /// Attempt to join the first session among the available sessions.
        /// </summary>
        // Legacy 호환 메서드
        public Task<ISession> QuickJoinLobbyAsync() => TryQuickJoinSessionAsync().ContinueWith(t => t.Result.Session);

        public async Task<(bool Success, ISession Session)> TryQuickJoinSessionAsync()
        {
            if (!m_RateLimitQuickJoin.CanCall)
            {
                Debug.LogWarning("[LobbyServiceFacadeEx] Quick Join Session hit the rate limit.");
                return (false, null);
            }
            Debug.Log($"[LobbyServiceFacadeEx] 세션 빠르게 참가 요청");
            try
            {
                Debug.Log($"[LobbyServiceFacadeEx] Quick join attempt - PlayerID: {AuthenticationService.Instance.PlayerId}");

                var session = await m_LobbyApiInterface.QuickJoinSession(AuthenticationService.Instance.PlayerId, m_LocalUser.GetDataForUnityServices());
                Debug.Log($"[LobbyServiceFacadeEx] 세션 빠르게 참가 요청 성공");
                return (true, session);
            }
            catch (Exception e)
            {
                Debug.LogError($"[LobbyServiceFacadeEx] 세션 빠르게 참가 요청 실패: {e.Message}");
                m_RateLimitQuickJoin.PutOnCooldown();
                PublishError(e);
            }

            return (false, null);
        }

        void ResetLobby()
        {
            Debug.Log("<color=cyan>[LobbyServiceFacadeEx] ResetLobby 호출</color>");
            CurrentUnityLobby = null;
            if (m_LocalUser != null)
            {
                m_LocalUser.ResetState();
            }
            if (m_LocalLobby != null && m_LocalUser != null)
            {
                m_LocalLobby.Reset(m_LocalUser);
            }

            // no need to disconnect Netcode, it should already be handled by Netcode's callback to disconnect
        }

        // ❌ 제거됨: OnSessionChanged()는 사용하지 않음
        // 이유: Unity Sessions API의 이벤트 구독이 구현되지 않았고,
        //       실제 게임 시작은 ConnectionManagerEx의 OnClientConnectedCallback에서 처리함
        // 참고: NetworkManager.OnClientConnectedCallback → 2명 확인 → 씬 전환

        void OnKickedFromSession()
        {
            Debug.Log("[LobbyServiceFacadeEx] Kicked from Session");
            ResetLobby();
            EndTracking();
        }

        async void SubscribeToJoinedSessionAsync()
        {
            // Sessions API에서는 이벤트 구독이 자동으로 처리됨
            // 필요시 CurrentUnityLobby의 이벤트를 직접 구독할 수 있음
            if (CurrentUnityLobby != null)
            {
                // ISession 이벤트 구독 (Sessions API에서 제공되는 경우)
                Debug.Log("[LobbyServiceFacadeEx] Sessions API 이벤트 구독 설정됨");
            }
        }

        async void UnsubscribeToJoinedSessionAsync()
        {
            // Sessions API에서는 자동으로 정리됨
            Debug.Log("[LobbyServiceFacadeEx] Sessions API 이벤트 구독 해제됨");
        }

        /// <summary>
        /// Used for getting the list of all active lobbies, without needing full info for each.
        /// </summary>
        public async Task<(bool Success, ISession Session)> FindAvailableLobby()
        {
            if (!m_RateLimitQuery.CanCall)
            {
                Debug.LogWarning("[LobbyServiceFacadeEx] Retrieve Lobby list hit the rate limit. Will try again soon...");
                return (false, null);
            }

            try
            {
                var sessions = await m_LobbyApiInterface.QueryAllSessions();

                // sessions가 null인 경우 처리
                if (sessions == null)
                {
                    Debug.Log("[LobbyServiceFacadeEx] 세션 쿼리 결과가 null입니다");
                    return (false, null);
                }

                // 세션 목록이 비어있는 경우 처리
                if (sessions.Count == 0)
                {
                    Debug.Log("[LobbyServiceFacadeEx] 사용 가능한 세션이 없습니다");
                    return (false, null);
                }

                // VContainer IPublisher 대신 직접 이벤트 발행
                OnLobbyListFetched?.Invoke(new LobbyListFetchedMessageEx(LocalLobbyEx.CreateLocalLobbies(sessions)));
                Debug.Log($"[LobbyServiceFacadeEx] {sessions.Count}개의 세션을 찾았습니다");

                // 첫 번째 사용 가능한 세션을 반환
                return (true, sessions.FirstOrDefault());
            }
            catch (Exception e)
            {
                Debug.LogError($"[LobbyServiceFacadeEx] 세션 검색 중 예외 발생: {e.Message}");
                m_RateLimitQuery.PutOnCooldown();
                PublishError(e);
                return (false, null);
            }
        }

        public async Task<ISession> ReconnectToSessionAsync()
        {
            try
            {
                return await m_LobbyApiInterface.ReconnectToSession(m_LocalLobby.LobbyID);
            }
            catch (Exception e)
            {
                // If Session is not found and if we are not the host, it has already been deleted. No need to publish the error here.
                if (!m_LocalUser.IsHost)
                {
                    PublishError(e);
                }
            }

            return null;
        }

        /// <summary>
        /// Attempt to leave a lobby
        /// </summary>
        async void LeaveLobbyAsync()
        {
            string uasId = AuthenticationService.Instance.PlayerId;
            try
            {
                await m_LobbyApiInterface.RemovePlayerFromSession(uasId, m_LocalLobby.LobbyID);
            }
            catch (Exception e)
            {
                // If Lobby is not found and if we are not the host, it has already been deleted. No need to publish the error here.
                if (!e.Message.Contains("not found") && !m_LocalUser.IsHost)
                {
                    PublishError(e);
                }
            }
            finally
            {
                ResetLobby();
            }

        }

        public async void RemovePlayerFromSessionAsync(string uasId)
        {
            if (m_LocalUser.IsHost)
            {
                try
                {
                    await m_LobbyApiInterface.RemovePlayerFromSession(uasId, m_LocalLobby.LobbyID);
                }
                catch (Exception e)
                {
                    PublishError(e);
                }
            }
            else
            {
                Debug.LogError("[LobbyServiceFacadeEx] Only the host can remove other players from the lobby.");
            }
        }

        public async void DeleteSessionAsync()
        {
            if (m_LocalUser != null && m_LocalUser.IsHost)
            {
                try
                {
                    if (m_LocalLobby != null && !string.IsNullOrEmpty(m_LocalLobby.LobbyID))
                    {
                        await m_LobbyApiInterface.DeleteSession(m_LocalLobby.LobbyID);
                    }
                }
                catch (Exception e)
                {
                    PublishError(e);
                }
                finally
                {
                    ResetLobby();
                }
            }
            else
            {
                Debug.LogError("[LobbyServiceFacadeEx] Only the host can delete a lobby.");
            }
        }

        /// <summary>
        /// Attempt to push a set of key-value pairs associated with the local player which will overwrite any existing
        /// data for these keys. Lobby can be provided info about Relay (or any other remote allocation) so it can add
        /// automatic disconnect handling.
        /// </summary>
        public async Task UpdatePlayerDataAsync(string allocationId, string connectionInfo)
        {
            if (!m_RateLimitQuery.CanCall)
            {
                return;
            }

            try
            {
                var result = await m_LobbyApiInterface.UpdatePlayer(CurrentUnityLobby.Id, AuthenticationService.Instance.PlayerId, m_LocalUser.GetDataForUnityServices(), allocationId, connectionInfo);
                Debug.Log($"<color=red>[LobbyServiceFacadeEx] UpdatePlayerDataAsync 호출됨 - 결과: {result}</color>");
                if (result != null)
                {
                    CurrentUnityLobby = result; // Store the most up-to-date lobby now since we have it, instead of waiting for the next heartbeat.
                    Debug.Log($"<color=red>[LobbyServiceFacadeEx] UpdatePlayerDataAsync 호출됨 - 결과: {result}</color>");
                }

            }
            catch (Exception e)
            {
                if (e.Message.Contains("rate") || e.Message.Contains("limit"))
                {
                    m_RateLimitQuery.PutOnCooldown();
                }
                else if (!e.Message.Contains("not found") && !m_LocalUser.IsHost) // If Lobby is not found and if we are not the host, it has already been deleted. No need to publish the error here.
                {
                    PublishError(e);
                }
            }
        }

        /// <summary>
        /// Attempt to update the set of key-value pairs associated with a given lobby and unlocks it so clients can see it.
        /// </summary>
        public async Task UpdateSessionDataAndUnlockAsync()
        {
            if (!m_RateLimitQuery.CanCall)
            {
                return;
            }

            var localData = m_LocalLobby.GetDataForUnityServices();

            // Sessions API에서는 Data 속성이 없음 - 빈 딕셔너리 사용
            var dataCurr = new Dictionary<string, string>();
            if (dataCurr == null)
            {
                dataCurr = new Dictionary<string, string>();
            }

            foreach (var dataNew in localData)
            {
                if (dataCurr.ContainsKey(dataNew.Key))
                {
                    dataCurr[dataNew.Key] = dataNew.Value;
                }
                else
                {
                    dataCurr.Add(dataNew.Key, dataNew.Value);
                }
            }

            try
            {
                var result = await m_LobbyApiInterface.UpdateSession(CurrentUnityLobby.Id, dataCurr);

                if (result != null)
                {
                    CurrentUnityLobby = result;
                }
            }
            catch (Exception e)
            {
                if (e.Message.Contains("rate") || e.Message.Contains("limit"))
                {
                    m_RateLimitQuery.PutOnCooldown();
                }
                else
                {
                    PublishError(e);
                }
            }
        }

        /// <summary>
        /// Lobby requires a periodic ping to detect rooms that are still active, in order to mitigate "zombie" lobbies.
        /// </summary>
        void DoLobbyHeartbeat(float dt)
        {
            m_HeartbeatTime += dt;
            if (m_HeartbeatTime > k_HeartbeatPeriod)
            {
                m_HeartbeatTime -= k_HeartbeatPeriod;
                try
                {
                    m_LobbyApiInterface.SendHeartbeatPing(CurrentUnityLobby.Id);


                }
                catch (Exception e)
                {
                    // If Lobby is not found and if we are not the host, it has already been deleted. No need to publish the error here.
                    if (!e.Message.Contains("not found") && !m_LocalUser.IsHost)
                    {
                        PublishError(e);
                    }
                }
            }
        }

        void PublishError(Exception e)
        {
            var reason = e.InnerException == null ? e.Message : $"{e.Message} ({e.InnerException.Message})"; // Session error type, then HTTP error type.
            Debug.LogError($"[LobbyServiceFacadeEx] Session Error: {reason}");
            // VContainer IPublisher 대신 직접 에러 로깅
        }

        // Legacy compatibility wrapper methods for easier transition
        [System.Obsolete("Use SetRemoteSession instead")]
        public void SetRemoteLobby(ISession session) => SetRemoteSession(session);

        [System.Obsolete("Use TryCreateSessionAsync instead")]
        public async Task<(bool Success, ISession Lobby)> TryCreateLobbyAsync(string lobbyName, int maxPlayers, bool isPrivate) =>
            await TryCreateSessionAsync(lobbyName, maxPlayers, isPrivate);

        [System.Obsolete("Use TryJoinSessionAsync instead")]
        public async Task<(bool Success, ISession Lobby)> TryJoinLobbyAsync(string lobbyId, string lobbyCode) =>
            await TryJoinSessionAsync(lobbyId, lobbyCode);

        [System.Obsolete("Use TryQuickJoinSessionAsync instead")]
        public async Task<(bool Success, ISession Lobby)> TryQuickJoinLobbyAsync() =>
            await TryQuickJoinSessionAsync();

        #region ILobbyService Implementation

        public bool IsInitialized { get; private set; } = false;

        public async Task InitializeAsync()
        {
            if (IsInitialized) return;

            // Managers를 통해 의존성 획득
            Initialize(
                Managers.Debug,
                Managers.UpdateRunner,
                Managers.LocalLobby,
                Managers.LocalUser,
                Managers.Scene,
                Managers.Network
            );

            IsInitialized = true;
            await Task.CompletedTask;
        }

        public void Shutdown()
        {
            Dispose();
            IsInitialized = false;
        }

        public async Task<bool> CreateLobbyAsync(string lobbyName, int maxPlayers)
        {
            var result = await TryCreateSessionAsync(lobbyName, maxPlayers, false);
            if (result.Success && result.Session != null)
            {
                SetRemoteSession(result.Session);
                BeginTracking();
            }
            return result.Success;
        }

        public async Task<bool> JoinLobbyByCodeAsync(string lobbyCode)
        {
            var result = await TryJoinSessionAsync(null, lobbyCode);
            if (result.Success && result.Session != null)
            {
                SetRemoteSession(result.Session);
                BeginTracking();
            }
            return result.Success;
        }

        public async Task<bool> JoinLobbyByIdAsync(string lobbyId)
        {
            var result = await TryJoinSessionAsync(lobbyId, null);
            if (result.Success && result.Session != null)
            {
                SetRemoteSession(result.Session);
                BeginTracking();
            }
            return result.Success;
        }

        public async Task<bool> QuickJoinAsync()
        {
            var result = await TryQuickJoinSessionAsync();
            if (result.Success && result.Session != null)
            {
                SetRemoteSession(result.Session);
                BeginTracking();
            }
            return result.Success;
        }

        public void LeaveLobby()
        {
            EndTracking();
        }

        public LocalLobbyEx CurrentLobby => m_LocalLobby;

        #endregion
    }
}
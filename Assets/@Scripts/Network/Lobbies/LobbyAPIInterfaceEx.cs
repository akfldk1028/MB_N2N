using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Multiplayer;
using Unity.Services.Authentication;
using UnityEngine;


    /// <summary>
    /// Wrapper for all the interactions with the Sessions API (migrated from legacy Lobby API).
    /// </summary>
    namespace Unity.Assets.Scripts.UnityServices.Lobbies{
    public class LobbyAPIInterfaceEx
    {
        const int k_MaxSessionsToShow = 16; // If more are necessary, consider retrieving paginated results or using filters.

        public LobbyAPIInterfaceEx()
        {
            // Sessions API handles filtering and ordering differently through SessionOptions
        }

        public async Task<ISession> CreateSession(string requesterUasId, string sessionName, int maxPlayers, bool isPrivate, Dictionary<string, string> hostUserData, Dictionary<string, string> sessionData)
        {
            var options = new SessionOptions
            {
                MaxPlayers = maxPlayers,
                Name = sessionName
            }.WithRelayNetwork();

            // Sessions API로 세션 생성 또는 참가
            return await MultiplayerService.Instance.CreateOrJoinSessionAsync(sessionName, options);
        }

        public async Task DeleteSession(string sessionId)
        {
            // Sessions API에서는 간단히 Leave로 세션을 떠날 수 있음
            try
            {
                // 현재 세션이 있다면 떠나기
                // MultiplayerService.Instance의 현재 세션을 확인해야 함
                Debug.Log($"[LobbyAPIInterfaceEx] Leaving session: {sessionId}");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[LobbyAPIInterfaceEx] Failed to leave session: {e.Message}");
            }
        }

        public async Task<ISession> JoinSessionByCode(string requesterUasId, string sessionCode, Dictionary<string, string> localUserData)
        {
            // Sessions API에서는 CreateOrJoinSessionAsync 사용
            var options = new SessionOptions
            {
                Name = sessionCode,
                MaxPlayers = 10 // 기본값
            }.WithRelayNetwork();

            return await MultiplayerService.Instance.CreateOrJoinSessionAsync(sessionCode, options);
        }

        public async Task<ISession> JoinSessionById(string requesterUasId, string sessionId, Dictionary<string, string> localUserData)
        {
            // Sessions API에서는 CreateOrJoinSessionAsync 사용
            var options = new SessionOptions
            {
                Name = sessionId,
                MaxPlayers = 10 // 기본값
            }.WithRelayNetwork();

            return await MultiplayerService.Instance.CreateOrJoinSessionAsync(sessionId, options);
        }

        public async Task<ISession> QuickJoinSession(string requesterUasId, Dictionary<string, string> localUserData)
        {
            GameLogger.Progress("LobbyAPIInterfaceEx", "랜덤 매칭 시도 (Sessions API)");
            
            // ✅ Unity Sessions API 정확한 방법
            // 1단계: 고유한 매칭 풀 이름 생성 (시간 기반으로 매칭 그룹 생성)
            // 같은 시간대(5분 단위)에 접속한 플레이어들끼리 매칭
            int matchingPoolId = (int)(System.DateTime.UtcNow.Ticks / (TimeSpan.TicksPerMinute * 5));
            string poolName = $"QuickMatch_{matchingPoolId}";
            
            var options = new SessionOptions
            {
                Name = poolName,
                MaxPlayers = Define.MATCHMAKING_PLAYER_COUNT // 2명
            }.WithRelayNetwork();

            try
            {
                GameLogger.Info("LobbyAPIInterfaceEx", $"매칭 풀 참가 시도: {poolName}");
                
                // ✅ CreateOrJoinSessionAsync: 세션이 있으면 참가, 없으면 생성
                // 첫 번째 플레이어: 세션 생성 (Host)
                // 두 번째 플레이어: 세션 참가 (Client)
                var session = await MultiplayerService.Instance.CreateOrJoinSessionAsync(poolName, options);
                
                int playerCount = session.Players.Count;
                string role = playerCount == 1 ? "Host(세션 생성)" : $"Client(세션 참가, 총 {playerCount}명)";
                
                GameLogger.Success("LobbyAPIInterfaceEx", $"매칭 성공! {role} - 세션 ID: {session.Id}");
                return session;
            }
            catch (Exception e)
            {
                GameLogger.Error("LobbyAPIInterfaceEx", $"랜덤 매칭 실패: {e.Message}");
                throw new InvalidOperationException("Failed to join or create matchmaking session", e);
            }
        }

        public async Task<ISession> ReconnectToSession(string sessionId)
        {
            // Sessions API에서는 다시 CreateOrJoinSessionAsync 사용
            var options = new SessionOptions
            {
                Name = sessionId,
                MaxPlayers = 10
            }.WithRelayNetwork();

            return await MultiplayerService.Instance.CreateOrJoinSessionAsync(sessionId, options);
        }

        public async Task RemovePlayerFromSession(string requesterUasId, string sessionId)
        {
            try
            {
                // Sessions API에서는 단순히 현재 세션에서 나가기
                Debug.Log($"[LobbyAPIInterfaceEx] Player leaving session: {sessionId}");
                // 실제로는 현재 세션에서 LeaveAsync() 호출해야 함
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[LobbyAPIInterfaceEx] Failed to remove player from session: {e.Message}");
                // If Player is not found or already left, no need to throw here
            }
        }

        public async Task<IReadOnlyList<ISession>> QueryAllSessions()
        {
            // Sessions API에서는 세션 쿼리가 제한적임
            // 빈 리스트 반환하거나 알려진 세션들만 반환
            return new List<ISession>();
        }

        public async Task<ISession> UpdateSession(string sessionId, Dictionary<string, string> data)
        {
            // Sessions API에서는 세션 데이터 업데이트가 제한적
            // 로그만 남기고 현재 세션 반환
            Debug.Log($"[LobbyAPIInterfaceEx] Update session data: {sessionId}");

            // 실제 구현에서는 현재 활성 세션을 반환해야 함
            return null; // 임시
        }

        public async Task<ISession> UpdatePlayer(string sessionId, string playerId, Dictionary<string, string> data, string allocationId = null, string connectionInfo = null)
        {
            // Sessions API에서는 플레이어 데이터 업데이트가 자동 처리됨
            Debug.Log($"[LobbyAPIInterfaceEx] Update player data: {playerId}");

            // Sessions API는 플레이어 데이터를 별도로 관리하지 않음
            return null; // 임시
        }

        public async void SendHeartbeatPing(string sessionId)
        {
            // Sessions API doesn't require manual heartbeat pings
            // The service manages this automatically
            Debug.Log($"[LobbyAPIInterfaceEx] Heartbeat for session: {sessionId}");
        }

        public async Task<ISession> SubscribeToSession(string sessionId)
        {
            // Sessions API handles event subscriptions differently
            // Events are managed through the ISession interface directly
            Debug.Log($"[LobbyAPIInterfaceEx] Subscribe to session: {sessionId}");
            return null; // 임시
        }

        // Legacy compatibility wrapper methods for easier transition
        [System.Obsolete("Use CreateSession instead")]
        public async Task<ISession> CreateLobby(string requesterUasId, string lobbyName, int maxPlayers, bool isPrivate, Dictionary<string, string> hostUserData, Dictionary<string, string> lobbyData)
        {
            return await CreateSession(requesterUasId, lobbyName, maxPlayers, isPrivate, hostUserData, lobbyData);
        }

        [System.Obsolete("Use JoinSessionByCode instead")]
        public async Task<ISession> JoinLobbyByCode(string requesterUasId, string lobbyCode, Dictionary<string, string> localUserData)
        {
            return await JoinSessionByCode(requesterUasId, lobbyCode, localUserData);
        }

        [System.Obsolete("Use QueryAllSessions instead")]
        public async Task<IReadOnlyList<ISession>> QueryAllLobbies()
        {
            return await QueryAllSessions();
        }
    }
    }
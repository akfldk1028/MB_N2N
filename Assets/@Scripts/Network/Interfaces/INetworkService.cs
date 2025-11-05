using System;
using System.Threading.Tasks;
using Unity.Assets.Scripts.UnityServices.Lobbies;
using Unity.Netcode;

namespace Unity.Assets.Scripts.Network.Interfaces
{
    /// <summary>
    /// 네트워크 서비스의 기본 인터페이스
    /// 모든 네트워크 관련 서비스가 구현해야 하는 기본 계약
    /// </summary>
    public interface INetworkService
    {
        /// <summary>
        /// 서비스 초기화
        /// </summary>
        Task InitializeAsync();

        /// <summary>
        /// 서비스 종료
        /// </summary>
        void Shutdown();

        /// <summary>
        /// 서비스 초기화 여부
        /// </summary>
        bool IsInitialized { get; }
    }

    /// <summary>
    /// 연결 관리 서비스 인터페이스
    /// </summary>
    public interface IConnectionService : INetworkService
    {
        /// <summary>
        /// 연결 상태 변경 이벤트
        /// </summary>
        event Action<ConnectStatus> OnConnectionStatusChanged;

        /// <summary>
        /// 클라이언트로 연결
        /// </summary>
        void StartClient(string playerName);

        /// <summary>
        /// 호스트로 시작
        /// </summary>
        void StartHost(string playerName);

        /// <summary>
        /// 연결 종료
        /// </summary>
        void Disconnect();

        /// <summary>
        /// 현재 연결 상태
        /// </summary>
        ConnectStatus CurrentStatus { get; }

        /// <summary>
        /// 연결 여부
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// 호스트 여부
        /// </summary>
        bool IsHost { get; }

        /// <summary>
        /// 클라이언트 여부
        /// </summary>
        bool IsClient { get; }
    }

    /// <summary>
    /// 로비 서비스 인터페이스
    /// </summary>
    public interface ILobbyService : INetworkService
    {
        /// <summary>
        /// 로비 생성
        /// </summary>
        Task<bool> CreateLobbyAsync(string lobbyName, int maxPlayers);

        /// <summary>
        /// 로비 참가 (코드)
        /// </summary>
        Task<bool> JoinLobbyByCodeAsync(string lobbyCode);

        /// <summary>
        /// 로비 참가 (ID)
        /// </summary>
        Task<bool> JoinLobbyByIdAsync(string lobbyId);

        /// <summary>
        /// 빠른 참가
        /// </summary>
        Task<bool> QuickJoinAsync();

        /// <summary>
        /// 로비 나가기
        /// </summary>
        void LeaveLobby();

        /// <summary>
        /// 현재 로비 정보
        /// </summary>
        Unity.Assets.Scripts.UnityServices.Lobbies.LocalLobbyEx CurrentLobby { get; }
    }

    /// <summary>
    /// 세션 관리 서비스 인터페이스
    /// </summary>
    public interface ISessionService : INetworkService
    {
        /// <summary>
        /// 플레이어 데이터 설정
        /// </summary>
        void SetPlayerData(ulong clientId, SessionPlayerDataEx data);

        /// <summary>
        /// 플레이어 데이터 가져오기
        /// </summary>
        SessionPlayerDataEx? GetPlayerData(ulong clientId);

        /// <summary>
        /// 플레이어 연결 해제
        /// </summary>
        void DisconnectPlayer(ulong clientId);

        /// <summary>
        /// 세션 시작
        /// </summary>
        void OnSessionStarted();

        /// <summary>
        /// 세션 종료
        /// </summary>
        void OnSessionEnded();
    }

    /// <summary>
    /// 인증 서비스 인터페이스
    /// </summary>
    public interface IAuthenticationService : INetworkService
    {
        /// <summary>
        /// 플레이어 인증 확인
        /// </summary>
        Task<bool> EnsurePlayerIsAuthorized();

        /// <summary>
        /// 로그아웃
        /// </summary>
        void SignOut();

        /// <summary>
        /// 현재 플레이어 ID
        /// </summary>
        string PlayerId { get; }

        /// <summary>
        /// 인증 여부
        /// </summary>
        bool IsSignedIn { get; }
    }
}
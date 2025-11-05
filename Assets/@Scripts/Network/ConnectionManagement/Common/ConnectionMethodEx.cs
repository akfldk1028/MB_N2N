using System;
using System.Threading.Tasks;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;
using Unity.Assets.Scripts.Network;
using Unity.Assets.Scripts.UnityServices.Lobbies;
using Unity.Netcode;


/// <summary>
/// ConnectionMethodEx contains all setup needed to setup NGO to be ready to start a connection, either host or client side.
/// VContainer 의존성을 제거하고 Managers 패턴으로 리팩토링됨
/// Please override this abstract class to add a new transport or way of connecting.
/// </summary>

public abstract class ConnectionMethodBaseEx
{
    protected ConnectionManagerEx m_ConnectionManager;

    readonly ProfileManagerEx m_ProfileManager;

    protected readonly string m_PlayerName;
    protected const string k_DtlsConnType = "dtls";
    /// <summary>
    /// Setup the host connection prior to starting the NetworkManager
    /// </summary>
    /// <returns></returns>
    public abstract Task SetupHostConnectionAsync();


    /// <summary>
    /// Setup the client connection prior to starting the NetworkManager
    /// </summary>
    /// <returns></returns>
    public abstract Task SetupClientConnectionAsync();

    /// <summary>
    /// Setup the client for reconnection prior to reconnecting
    /// </summary>
    /// <returns>
    /// success = true if succeeded in setting up reconnection, false if failed.
    /// shouldTryAgain = true if we should try again after failing, false if not.
    /// </returns>
    public abstract Task<(bool success, bool shouldTryAgain)> SetupClientReconnectionAsync();

    public ConnectionMethodBaseEx(ConnectionManagerEx connectionManager, ProfileManagerEx profileManager, string playerName)
    {
        m_ConnectionManager = connectionManager;
        m_ProfileManager = profileManager;
        m_PlayerName = playerName;
    }

    protected void SetConnectionPayload(string playerId, string playerName)
    {
        var payload = JsonUtility.ToJson(new ConnectionPayload(
            playerName,
            playerId,
            1,  // buildVersion
            Debug.isDebugBuild
        ));

        var payloadBytes = System.Text.Encoding.UTF8.GetBytes(payload);

        m_ConnectionManager.NetworkManager.NetworkConfig.ConnectionData = payloadBytes;
    }

    /// Using authentication, this makes sure your session is associated with your account and not your device. This means you could reconnect
    /// from a different device for example. A playerId is also a bit more permanent than player prefs. In a browser for example,
    /// player prefs can be cleared as easily as cookies.
    /// The forked flow here is for debug purposes and to make UGS optional in Boss Room. This way you can study the sample without
    /// setting up a UGS account. It's recommended to investigate your own initialization and IsSigned flows to see if you need
    /// those checks on your own and react accordingly. We offer here the option for offline access for debug purposes, but in your own game you
    /// might want to show an error popup and ask your player to connect to the internet.
  protected string GetPlayerId()
  {
      if (Unity.Services.Core.UnityServices.State != ServicesInitializationState.Initialized)
      {
          return ClientPrefs.GetGuid() + m_ProfileManager.Profile;
      }

      return AuthenticationService.Instance.IsSignedIn ? AuthenticationService.Instance.PlayerId : ClientPrefs.GetGuid() + m_ProfileManager.Profile;
  }
}


/// <summary>
/// UTP's Relay connection setup using the Lobby integration - VContainer 의존성 제거됨
/// </summary>
class ConnectionMethodRelayEx : ConnectionMethodBaseEx
{
    LobbyServiceFacadeEx m_LobbyServiceFacade;
    LocalLobbyEx m_LocalLobby;

   public ConnectionMethodRelayEx(LobbyServiceFacadeEx lobbyServiceFacade, LocalLobbyEx localLobby, ConnectionManagerEx connectionManager, ProfileManagerEx profileManager, string playerName)
       : base(connectionManager, profileManager, playerName)
   {
       m_LobbyServiceFacade = lobbyServiceFacade;
       m_LocalLobby = localLobby;
       m_ConnectionManager = connectionManager;
   }

    public override async Task SetupClientConnectionAsync()
    {
        Debug.Log("<color=red>[ConnectionMethodRelayEx] Setting up Unity Relay client</color>");

        SetConnectionPayload(GetPlayerId(), m_PlayerName);

        // ✅ QuickJoin에서 이미 세션 참가됨 - 기존 세션 사용!
        var session = m_LobbyServiceFacade.CurrentUnityLobby;
        
        if (session == null)
        {
            GameLogger.Error("ConnectionMethodRelayEx", "❌ 세션 정보 없음! QuickJoin이 먼저 완료되어야 함");
            throw new Exception("No session available. QuickJoin must be called first.");
        }

        GameLogger.Success("ConnectionMethodRelayEx", $"✅ Client 기존 세션 사용: {session.Id}, Code: {session.Code}");

        m_LocalLobby.RelayJoinCode = session.Code;

        // ✅ Unity Transport는 Sessions API가 자동으로 Relay 설정 처리
        
        GameLogger.Success("ConnectionMethodRelayEx", "✅ Client 연결 설정 완료");
    }

    public override async Task<(bool success, bool shouldTryAgain)> SetupClientReconnectionAsync()
    {
        if (m_LobbyServiceFacade.CurrentUnityLobby == null)
        {
            Debug.Log("[ConnectionMethodRelayEx] Lobby does not exist anymore, stopping reconnection attempts.");
            return (false, false);
        }

        // When using Lobby with Relay, if a user is disconnected from the Relay server, the server will notify the
        // Lobby service and mark the user as disconnected, but will not remove them from the lobby. They then have
        // some time to attempt to reconnect (defined by the "Disconnect removal time" parameter on the dashboard),
        // after which they will be removed from the lobby completely.
        // See https://docs.unity.com/lobby/reconnect-to-lobby.html
        var session = await m_LobbyServiceFacade.ReconnectToSessionAsync();
        var success = session != null;
        Debug.Log(success ? "[ConnectionMethodRelayEx] Successfully reconnected to Lobby." : "[ConnectionMethodRelayEx] Failed to reconnect to Lobby.");
        return (success, true); // return a success if reconnecting to lobby returns a lobby
    }

    public override async Task SetupHostConnectionAsync()
    {
        GameLogger.Progress("ConnectionMethodRelayEx", "Host 연결 설정 시작");

        // ✅ QuickJoin에서 이미 세션 생성됨 - 기존 세션 사용!
        var session = m_LobbyServiceFacade.CurrentUnityLobby;
        
        if (session == null)
        {
            GameLogger.Error("ConnectionMethodRelayEx", "❌ 세션 정보 없음! QuickJoin이 먼저 완료되어야 함");
            throw new System.Exception("No session available. QuickJoin must be called first.");
        }

        GameLogger.Success("ConnectionMethodRelayEx", $"✅ 기존 세션 사용: {session.Id}, Code: {session.Code}");

        m_LocalLobby.RelayJoinCode = session.Code;

        // Sessions API에서 세션 데이터 업데이트 (필요시)
        await m_LobbyServiceFacade.UpdateSessionDataAndUnlockAsync();

        // ✅ Unity Transport는 Sessions API가 자동으로 Relay 설정 처리
        // Set connection payload for host
        SetConnectionPayload(GetPlayerId(), m_PlayerName);

        GameLogger.Success("ConnectionMethodRelayEx", $"✅ Host 연결 설정 완료: {session.Code}");
    }
}
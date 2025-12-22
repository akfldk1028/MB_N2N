using UnityEngine;
using Unity.Netcode;

/// <summary>
/// 멀티플레이어 유틸리티 (Util.cs 패턴 따름)
/// - 네트워크 상태 체크
/// - 플레이어 ID/오프셋 계산
/// - 단일 인스턴스 구조 유지하면서 Host/Client 구분
/// </summary>
public static class MultiplayerUtil
{
    #region 네트워크 상태 체크
    /// <summary>
    /// NetworkManager 싱글톤 (캐싱)
    /// </summary>
    private static NetworkManager NetworkManager => NetworkManager.Singleton;

    /// <summary>
    /// 멀티플레이어 모드인지 확인
    /// </summary>
    public static bool IsMultiplayer()
    {
        return NetworkManager != null && NetworkManager.IsListening;
    }

    /// <summary>
    /// 현재 인스턴스가 Host(Server)인지 확인
    /// </summary>
    public static bool IsHost()
    {
        return NetworkManager != null && NetworkManager.IsListening && NetworkManager.IsServer;
    }

    /// <summary>
    /// 현재 인스턴스가 Client(비서버)인지 확인
    /// </summary>
    public static bool IsClient()
    {
        return NetworkManager != null && NetworkManager.IsListening && !NetworkManager.IsServer;
    }

    /// <summary>
    /// 현재 인스턴스가 Server인지 확인 (Host 포함)
    /// </summary>
    public static bool IsServer()
    {
        return NetworkManager != null && NetworkManager.IsServer;
    }

    /// <summary>
    /// 싱글플레이어 모드인지 확인
    /// </summary>
    public static bool IsSinglePlayer()
    {
        return !IsMultiplayer();
    }
    #endregion

    #region 플레이어 ID
    /// <summary>
    /// 로컬 클라이언트 ID 반환 (싱글플레이어: 0)
    /// </summary>
    public static ulong GetLocalClientId()
    {
        if (!IsMultiplayer()) return 0;
        return NetworkManager.LocalClientId;
    }

    /// <summary>
    /// 상대방 클라이언트 ID 반환 (2인 멀티플레이어 기준)
    /// </summary>
    public static ulong GetOpponentClientId()
    {
        return GetLocalClientId() == 0 ? 1UL : 0UL;
    }

    /// <summary>
    /// 특정 clientId가 로컬 플레이어인지 확인
    /// </summary>
    public static bool IsLocalPlayer(ulong clientId)
    {
        return GetLocalClientId() == clientId;
    }
    #endregion

    #region 플레이어 오프셋 계산
    /// <summary>
    /// 플레이어별 X 오프셋 계산 (2인 멀티플레이어 기준)
    /// - clientId 0 (Host) → 왼쪽 (-spacing)
    /// - clientId 1 (Client) → 오른쪽 (+spacing)
    /// </summary>
    public static float GetPlayerXOffset(ulong clientId, float spacing = 15f)
    {
        return (clientId == 0) ? -spacing : spacing;
    }

    /// <summary>
    /// 로컬 플레이어의 X 오프셋 반환
    /// </summary>
    public static float GetLocalPlayerXOffset(float spacing = 15f)
    {
        return GetPlayerXOffset(GetLocalClientId(), spacing);
    }

    /// <summary>
    /// 상대방 플레이어의 X 오프셋 반환
    /// </summary>
    public static float GetOpponentXOffset(float spacing = 15f)
    {
        return GetPlayerXOffset(GetOpponentClientId(), spacing);
    }
    #endregion

    #region 권한 체크
    /// <summary>
    /// 특정 오브젝트의 소유자인지 확인
    /// </summary>
    public static bool IsOwner(NetworkObject networkObject)
    {
        if (networkObject == null) return false;
        return networkObject.IsOwner;
    }

    /// <summary>
    /// Server Authority 체크 (Server만 실행해야 하는 로직에 사용)
    /// </summary>
    public static bool HasServerAuthority()
    {
        // 싱글플레이어: 항상 true
        if (IsSinglePlayer()) return true;
        // 멀티플레이어: Server만 true
        return IsServer();
    }

    /// <summary>
    /// Client에서 실행해야 하는 로직 체크 (UI 업데이트 등)
    /// </summary>
    public static bool ShouldRunOnClient()
    {
        // 싱글플레이어: 항상 true
        if (IsSinglePlayer()) return true;
        // 멀티플레이어: 모든 클라이언트에서 실행
        return IsMultiplayer();
    }
    #endregion

    #region 연결 상태
    /// <summary>
    /// 현재 연결된 클라이언트 수 반환
    /// </summary>
    public static int GetConnectedClientCount()
    {
        if (!IsMultiplayer()) return 1;
        return NetworkManager.ConnectedClientsIds.Count;
    }

    /// <summary>
    /// 게임 시작 가능 상태인지 확인 (2인 멀티플레이어 기준)
    /// </summary>
    public static bool IsReadyToStart(int requiredPlayers = 2)
    {
        if (IsSinglePlayer()) return true;
        return GetConnectedClientCount() >= requiredPlayers;
    }
    #endregion

    #region 디버그
    /// <summary>
    /// 현재 네트워크 상태 문자열 반환 (디버깅용)
    /// </summary>
    public static string GetNetworkStatusString()
    {
        if (IsSinglePlayer())
            return "[SinglePlayer]";

        string role = IsHost() ? "Host" : "Client";
        ulong localId = GetLocalClientId();
        int connected = GetConnectedClientCount();

        return $"[{role}] ClientId={localId}, Connected={connected}";
    }
    #endregion
}

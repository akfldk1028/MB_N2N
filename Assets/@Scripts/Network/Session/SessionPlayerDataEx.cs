using UnityEngine;

/// <summary>
/// 세션 플레이어 데이터 - VContainer 의존성을 제거하고 Managers 패턴으로 리팩토링됨
/// </summary>
public struct SessionPlayerDataEx : ISessionPlayerDataEx
{
    public string PlayerName;
    public int PlayerNumber;
    public Vector3 PlayerPosition;
    public Quaternion PlayerRotation;
    /// Instead of using a NetworkGuid (two ulongs) we could just use an int or even a byte-sized index into an array of possible avatars defined in our game data source
    public NetworkGuid AvatarNetworkGuid;
    public int CurrentHitPoints;
    public bool HasCharacterSpawned;

    public SessionPlayerDataEx(ulong clientID, string name, NetworkGuid avatarNetworkGuid, int currentHitPoints = 0, bool isConnected = false, bool hasCharacterSpawned = false)
    {
        // 클라이언트 ID에 따라 다른 색상 사용 - 정적 메서드로 변경
        string logColor = GetColorForClientStatic(clientID);

        Debug.Log($"<color={logColor}>[SessionPlayerDataEx] 클라이언트 {clientID} 세션 데이터 생성 시작</color>");
        ClientID = clientID;
        Debug.Log($"<color={logColor}>[SessionPlayerDataEx] 클라이언트 {clientID} - ClientID 설정: {clientID}</color>");
        PlayerName = name;
        Debug.Log($"<color={logColor}>[SessionPlayerDataEx] 클라이언트 {clientID} - PlayerName 설정: {name}</color>");
        PlayerNumber = -1;
        Debug.Log($"<color={logColor}>[SessionPlayerDataEx] 클라이언트 {clientID} - PlayerNumber 설정: -1</color>");
        PlayerPosition = Vector3.zero;
        Debug.Log($"<color={logColor}>[SessionPlayerDataEx] 클라이언트 {clientID} - PlayerPosition 설정: {Vector3.zero}</color>");
        PlayerRotation = Quaternion.identity;
        Debug.Log($"<color={logColor}>[SessionPlayerDataEx] 클라이언트 {clientID} - PlayerRotation 설정: {Quaternion.identity}</color>");
        AvatarNetworkGuid = avatarNetworkGuid;
        Debug.Log($"<color={logColor}>[SessionPlayerDataEx] 클라이언트 {clientID} - AvatarNetworkGuid 설정: {avatarNetworkGuid}</color>");
        CurrentHitPoints = currentHitPoints;
        Debug.Log($"<color={logColor}>[SessionPlayerDataEx] 클라이언트 {clientID} - CurrentHitPoints 설정: {currentHitPoints}</color>");
        IsConnected = isConnected;
        Debug.Log($"<color={logColor}>[SessionPlayerDataEx] 클라이언트 {clientID} - IsConnected 설정: {isConnected}</color>");
        HasCharacterSpawned = hasCharacterSpawned;
        Debug.Log($"<color={logColor}>[SessionPlayerDataEx] 클라이언트 {clientID} - HasCharacterSpawned 설정: {hasCharacterSpawned}</color>");
        Debug.Log($"<color={logColor}>[SessionPlayerDataEx] 클라이언트 {clientID} 세션 데이터 생성 완료</color>");
    }

    // 클라이언트 ID에 따라 다른 색상을 반환하는 정적 메서드
    private static string GetColorForClientStatic(ulong clientID)
    {
        // 호스트는 일반적으로 0번 클라이언트
        if (clientID == 0)
        {
            return "green"; // 호스트는 녹색
        }

        // 나머지 클라이언트는 ID에 따라 다른 색상 할당
        switch (clientID % 5)
        {
            case 1: return "green";
            case 2: return "cyan";
            case 3: return "magenta";
            case 4: return "orange";
            default: return "green";
        }
    }

    // 인스턴스 메서드 버전 - 이미 초기화된 후 사용
    private string GetColorForClient()
    {
        return GetColorForClientStatic(this.ClientID);
    }

    public bool IsConnected { get; set; }
    public ulong ClientID { get; set; }

    public void Reinitialize()
    {
        string logColor = GetColorForClientStatic(ClientID);
        Debug.Log($"<color={logColor}>[SessionPlayerDataEx] 클라이언트 {ClientID} 세션 데이터 재초기화</color>");
        HasCharacterSpawned = false;
    }
}
using Unity.Netcode;

/// <summary>
/// BrickGame 멀티플레이어 네트워크 메시지 정의
/// NetworkedMessageChannel에서 사용 (INetworkSerializeByMemcpy 구현 필수)
/// </summary>

/// <summary>
/// 점수 및 레벨 동기화 메시지
/// Server → All Clients
/// </summary>
public struct BrickGameScoreMessage : INetworkSerializeByMemcpy
{
    public int Score;
    public int Level;
    public int RowsSpawned;

    public BrickGameScoreMessage(int score, int level, int rowsSpawned)
    {
        Score = score;
        Level = level;
        RowsSpawned = rowsSpawned;
    }
}

/// <summary>
/// 게임 상태 동기화 메시지
/// Server → All Clients
/// </summary>
public struct BrickGameStateMessage : INetworkSerializeByMemcpy
{
    public GamePhase Phase;

    public BrickGameStateMessage(GamePhase phase)
    {
        Phase = phase;
    }
}

/// <summary>
/// 행 생성 알림 메시지
/// Server → All Clients
/// </summary>
public struct BrickGameRowSpawnedMessage : INetworkSerializeByMemcpy
{
    public int RowIndex;

    public BrickGameRowSpawnedMessage(int rowIndex)
    {
        RowIndex = rowIndex;
    }
}

using System;
using MB.Infrastructure.Messages;

/// <summary>
/// 멀티플레이어 점수 관련 Payload 클래스들
/// BrickGameMultiplayerSpawner에서 ActionBus로 발행할 때 사용
/// </summary>

#region Payload Classes (ActionBus용)
/// <summary>
/// 멀티플레이어 점수 Payload
/// </summary>
public class MultiplayerScorePayload : IActionPayload
{
    public ulong ChangedClientId { get; }
    public int Player0Score { get; }
    public int Player1Score { get; }
    public float TerritoryRatio { get; }

    public MultiplayerScorePayload(ulong changedClientId, int player0Score, int player1Score, float territoryRatio)
    {
        ChangedClientId = changedClientId;
        Player0Score = player0Score;
        Player1Score = player1Score;
        TerritoryRatio = territoryRatio;
    }
}

/// <summary>
/// 땅따먹기 영역 Payload
/// </summary>
public class TerritoryPayload : IActionPayload
{
    public float Ratio { get; }

    public TerritoryPayload(float ratio)
    {
        Ratio = ratio;
    }
}
#endregion

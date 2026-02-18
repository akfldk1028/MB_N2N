using System;

/// <summary>
/// 벽돌깨기 게임 저장 데이터 모델
/// PlayerPrefs + JSON 직렬화를 통해 영구 저장되는 데이터
/// </summary>
[Serializable]
public class BrickGameSaveData
{
    #region 점수 및 진행도
    /// <summary>
    /// 최고 점수
    /// </summary>
    public int HighScore;

    /// <summary>
    /// 달성한 최고 레벨
    /// </summary>
    public int MaxLevel;
    #endregion

    #region 통계
    /// <summary>
    /// 총 게임 플레이 횟수
    /// </summary>
    public int TotalGamesPlayed;

    /// <summary>
    /// 총 파괴한 벽돌 수
    /// </summary>
    public int TotalBricksDestroyed;

    /// <summary>
    /// 총 승리 횟수
    /// </summary>
    public int TotalVictories;
    #endregion

    #region 메타 정보
    /// <summary>
    /// 마지막 플레이 날짜 (yyyy-MM-dd HH:mm:ss 형식)
    /// </summary>
    public string LastPlayDate;
    #endregion
}

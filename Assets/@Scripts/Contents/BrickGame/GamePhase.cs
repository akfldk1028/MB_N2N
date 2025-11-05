/// <summary>
/// 벽돌깨기 게임의 게임 단계 (Phase)
/// 게임의 전체 흐름을 명확하게 관리
/// </summary>
public enum GamePhase
{
    /// <summary>
    /// 게임 시작 전 대기 상태
    /// </summary>
    Idle,
    
    /// <summary>
    /// 게임 진행 중 (공이 움직이고 벽돌을 깨는 상태)
    /// </summary>
    Playing,
    
    /// <summary>
    /// 게임 일시정지
    /// </summary>
    Paused,
    
    /// <summary>
    /// 게임 오버 (벽돌이 바닥에 닿음)
    /// </summary>
    GameOver,
    
    /// <summary>
    /// 스테이지 클리어 (모든 벽돌 파괴)
    /// </summary>
    StageClear
}


using UnityEngine;

/// <summary>
/// 게임 모드 관리 서비스
/// - 멀티플레이어 (2인 매칭)
/// - 로컬 테스트 (1인 플레이)
/// </summary>
public class GameModeService
{
    #region Enums
    public enum GameMode
    {
        Multiplayer,    // 2인 랜덤 매칭
        LocalTest       // 1인 로컬 테스트
    }
    #endregion
    
    #region Properties
    private GameMode _currentMode = GameMode.LocalTest;  // 1인 테스트 기본값
    
    public GameMode CurrentMode
    {
        get => _currentMode;
        set
        {
            if (_currentMode != value)
            {
                _currentMode = value;
                GameLogger.Info("GameModeService", $"게임 모드 변경: {value}");
            }
        }
    }
    
    /// <summary>
    /// 현재 모드의 최대 플레이어 수
    /// </summary>
    public int MaxPlayers => _currentMode == GameMode.Multiplayer ? 2 : 1;
    
    /// <summary>
    /// 현재 모드의 최소 플레이어 수 (게임 시작 조건)
    /// </summary>
    public int MinPlayersToStart => _currentMode == GameMode.Multiplayer ? 2 : 1;
    
    /// <summary>
    /// 로컬 테스트 모드 여부
    /// </summary>
    public bool IsLocalTest => _currentMode == GameMode.LocalTest;
    
    /// <summary>
    /// 멀티플레이어 모드 여부
    /// </summary>
    public bool IsMultiplayer => _currentMode == GameMode.Multiplayer;
    #endregion
    
    #region Constructor
    public GameModeService()
    {
        // 기본값: LocalTest (1인 테스트)
        _currentMode = GameMode.LocalTest;
        GameLogger.SystemStart("GameModeService", $"게임 모드 서비스 생성 (기본: {_currentMode})");
    }
    #endregion
    
    #region Public Methods
    /// <summary>
    /// 멀티플레이어 모드로 전환
    /// </summary>
    public void SetMultiplayerMode()
    {
        CurrentMode = GameMode.Multiplayer;
        GameLogger.Success("GameModeService", "멀티플레이어 모드 활성화 (2인 매칭)");
    }
    
    /// <summary>
    /// 로컬 테스트 모드로 전환
    /// </summary>
    public void SetLocalTestMode()
    {
        CurrentMode = GameMode.LocalTest;
        GameLogger.Success("GameModeService", "로컬 테스트 모드 활성화 (1인 플레이)");
    }
    
    /// <summary>
    /// 현재 모드에서 게임 시작 가능 여부
    /// </summary>
    public bool CanStartGame(int currentPlayerCount)
    {
        bool canStart = currentPlayerCount >= MinPlayersToStart;
        
        if (!canStart)
        {
            GameLogger.Warning("GameModeService", 
                $"게임 시작 불가: 현재 {currentPlayerCount}명, 필요 {MinPlayersToStart}명");
        }
        
        return canStart;
    }
    
    /// <summary>
    /// 현재 모드 정보 출력
    /// </summary>
    public void LogCurrentMode()
    {
        GameLogger.Info("GameModeService", 
            $"현재 모드: {CurrentMode}, 최대: {MaxPlayers}명, 최소: {MinPlayersToStart}명");
    }
    #endregion
}


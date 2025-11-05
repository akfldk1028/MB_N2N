using System;
using UnityEngine;

/// <summary>
/// 벽돌깨기 게임 메인 매니저 (Non-MonoBehaviour)
/// Managers.Game.BrickGame 으로 접근
/// </summary>
public class BrickGameManager
{
    #region 의존성 (Dependency Injection)
    private IBrickPlacer _brickPlacer;
    #endregion
    
    #region 설정 및 상태
    private BrickGameSettings _settings;
    private BrickGameState _state;
    #endregion
    
    #region Sub-Managers
    // ✅ InputManager 제거: 전역 Managers.Input 사용
    private PlankManager _plankManager;
    private BallManager _ballManager;
    private BrickManager _brickManager;
    
    /// <summary>
    /// 패들(Plank) 관리 매니저 접근자
    /// Managers.Game.BrickGame.Plank 형태로 사용
    /// </summary>
    public PlankManager Plank => _plankManager;
    
    /// <summary>
    /// 공(Ball) 관리 매니저 접근자
    /// Managers.Game.BrickGame.Ball 형태로 사용
    /// </summary>
    public BallManager Ball => _ballManager;
    
    /// <summary>
    /// 벽돌(Brick) 관리 매니저 접근자
    /// Managers.Game.BrickGame.Brick 형태로 사용
    /// </summary>
    public BrickManager Brick => _brickManager;
    #endregion
    
    #region 이벤트
    public event Action OnGameStart;
    public event Action OnGamePause;
    public event Action OnGameResume;
    public event Action OnGameOver;
    public event Action OnRowSpawn;
    public event Action<int> OnLevelUp;
    public event Action<int> OnScoreChanged;
    #endregion
    
    #region 생성자
    public BrickGameManager()
    {
        _state = new BrickGameState();
        // ✅ InputManager 제거: 전역 Managers.Input 사용
        _plankManager = new PlankManager();
        _ballManager = new BallManager();
        _brickManager = new BrickManager();
        
        // Sub-Manager 이벤트 구독
        _ballManager.OnAllBallsReturned += HandleAllBallsReturned;
        _brickManager.OnAllBricksDestroyed += HandleAllBricksDestroyed;
        
        GameLogger.SystemStart("BrickGameManager", "벽돌깨기 게임 매니저 생성됨");
    }
    #endregion
    
    #region 초기화
    /// <summary>
    /// BrickGameManager 초기화 (의존성 주입)
    /// </summary>
    public void Initialize(
        IBrickPlacer brickPlacer,
        PhysicsPlank plank,
        Camera mainCamera,
        BrickGameSettings settings)
    {
        // brickPlacer는 선택적 (1인 테스트에서는 불필요)
        _brickPlacer = brickPlacer;
        _settings = settings ?? BrickGameSettings.CreateDefault();
        
        if (_brickPlacer == null)
        {
            GameLogger.Warning("BrickGameManager", "BrickPlacer가 null입니다. 벽돌 자동 생성 불가");
        }
        
        // Sub-Managers 초기화
        // ✅ InputManager 제거: 전역 Managers.Input이 ActionBus를 통해 입력 발행
        _plankManager.Initialize(plank, mainCamera);
        _ballManager.Initialize();
        _brickManager.Initialize();
        
        GameLogger.Success("BrickGameManager", "초기화 완료 (의존성 주입됨)");
    }
    #endregion
    
    #region Public Methods - 게임 제어
    /// <summary>
    /// 게임 시작
    /// </summary>
    public void StartGame()
    {
        if (_brickPlacer == null)
        {
            GameLogger.Warning("BrickGameManager", "BrickPlacer가 null입니다. 벽돌 자동 생성 생략 (멀티플레이어 모드는 OK)");
            // return 제거 - 게임은 계속 진행
        }

        // ✅ 먼저 상태 초기화 (Reset()이 CurrentPhase를 Idle로 되돌림)
        _state.Reset();
        _state.ResetRowsSpawned();
        _state.ResetScore();
        
        // 이벤트 발생 (UI가 구독하여 점수 업데이트)
        OnScoreChanged?.Invoke(_state.CurrentScore);
        
        // ✅ 그 다음 게임 시작 상태로 설정
        _state.CurrentPhase = GamePhase.Playing;
        _state.CurrentLevel = _settings.initialLevel;
        _state.CurrentSpawnInterval = _settings.spawnInterval;
        _state.NextSpawnTime = Time.time + _settings.initialSpawnDelay;
        
        // Sub-Managers 초기화
        // ✅ 전역 InputManager는 GameScene에서 GameMode로 제어됨
        _plankManager.Enabled = true;
        _plankManager.ResetPosition();
        _ballManager.Initialize();
        _brickManager.Initialize();
        
        // 초기 행 생성 (BrickPlacer가 있을 경우만)
        if (_brickPlacer != null)
        {
            _brickPlacer.PlaceMultipleRows(_settings.initialRowCount);
        }
        else
        {
            GameLogger.Info("BrickGameManager", "BrickPlacer 없음 - 벽돌 자동 생성 생략");
        }
        
        // 이벤트 발생
        OnGameStart?.Invoke();
        
        GameLogger.Success("BrickGameManager", $"게임 시작! (초기 레벨: {_settings.initialLevel})");
        GameLogger.Warning("BrickGameManager", $"🔥 StartGame() 완료! CurrentPhase: {_state.CurrentPhase}, IsGameActive: {_state.IsGameActive}");
    }
    
    /// <summary>
    /// 게임 일시정지
    /// </summary>
    public void PauseGame()
    {
        _state.CurrentPhase = GamePhase.Paused;
        // ✅ 전역 InputManager는 GameMode로 제어 (필요 시 Managers.Input.SetGameMode(None) 호출)
        _plankManager.Enabled = false;
        OnGamePause?.Invoke();
        GameLogger.Info("BrickGameManager", "게임 일시정지");
    }
    
    /// <summary>
    /// 게임 재개
    /// </summary>
    public void ResumeGame()
    {
        _state.CurrentPhase = GamePhase.Playing;
        // ✅ 전역 InputManager는 GameMode로 제어됨
        _plankManager.Enabled = true;
        OnGameResume?.Invoke();
        GameLogger.Info("BrickGameManager", "게임 재개");
    }
    
    /// <summary>
    /// 게임 오버
    /// </summary>
    public void GameOver()
    {
        _state.CurrentPhase = GamePhase.GameOver;
        OnGameOver?.Invoke();
        GameLogger.Warning("BrickGameManager", $"게임 오버! 최종 점수: {_state.CurrentScore}");
    }
    #endregion
    
    #region Public Methods - 점수 관리
    /// <summary>
    /// 벽돌 파괴 시 점수 추가 (외부에서 호출)
    /// </summary>
    public void AddScore(int waveValue)
    {
        _state.AddScore(waveValue);
        
        // 이벤트 발생
        OnScoreChanged?.Invoke(_state.CurrentScore);
    }
    #endregion
    
    #region Public Methods - 게임 상태 조회
    /// <summary>
    /// 게임 활성화 상태 반환
    /// </summary>
    public bool IsGameActive() => _state.IsGameActive;
    
    /// <summary>
    /// 현재 스폰 간격 반환
    /// </summary>
    public float GetCurrentSpawnInterval() => _state.CurrentSpawnInterval;
    
    /// <summary>
    /// 현재 레벨 반환
    /// </summary>
    public int GetCurrentLevel() => _state.CurrentLevel;
    
    /// <summary>
    /// 현재 점수 반환
    /// </summary>
    public int GetCurrentScore() => _state.CurrentScore;
    
    /// <summary>
    /// 생성된 행 수 반환
    /// </summary>
    public int GetRowsSpawned() => _state.RowsSpawned;
    #endregion
    
    #region Update Logic (ActionBus에서 호출됨)
    /// <summary>
    /// 매 프레임 호출되는 업데이트 로직
    /// Managers.Subscribe(ActionId.System_Update, OnUpdate) 형태로 등록
    /// </summary>
    public void OnUpdate()
    {
        // ✅ 디버깅: OnUpdate가 호출되는지 확인 (매 60프레임마다)
        if (Time.frameCount % 60 == 0)
        {
            GameLogger.Info("BrickGameManager", $"OnUpdate 호출됨! (프레임: {Time.frameCount}, IsGameActive: {_state.IsGameActive})");
        }

        if (!_state.IsGameActive)
        {
            if (Time.frameCount % 60 == 0)
            {
                GameLogger.Warning("BrickGameManager", "게임이 활성화되지 않아 OnUpdate 스킵");
            }
            return;
        }

        // ✅ 입력 처리: 전역 Managers.Input이 ActionBus를 통해 방향키 입력 발행
        // PlankManager가 ActionBus의 Input_ArrowKey 이벤트 구독하여 자동 처리됨

        // 패들 이동 처리
        if (_plankManager == null)
        {
            GameLogger.Error("BrickGameManager", "❌ _plankManager가 null입니다!");
            return;
        }
        _plankManager.UpdateMovement(Time.deltaTime);

        // BallManager 파워 타이머 업데이트
        _ballManager.UpdatePowerTimer(Time.deltaTime);

        // 시간 체크하여 새 행 생성 여부 결정
        if (Time.time >= _state.NextSpawnTime)
        {
            SpawnNewRow();
            AdjustDifficulty();
        }
    }
    #endregion
    
    #region Private Methods - 게임 로직
    /// <summary>
    /// 새 행 생성
    /// </summary>
    private void SpawnNewRow()
    {
        if (_brickPlacer != null)
        {
            // 한 줄씩 생성
            _brickPlacer.PlaceMultipleRows(1);
            
            // 행 생성 카운터 증가
            _state.IncrementRowsSpawned();
            
            // 레벨업 처리
            IncreaseLevel();
            
            // 이벤트 발생
            OnRowSpawn?.Invoke();
            
            // 새 블록 웨이브 플래그 (CommonVars 대체)
            _state.NewWaveOfBricks = true;
            
            GameLogger.DevLog("BrickGameManager", $"새 행 생성 (총 {_state.RowsSpawned}행)");
        }
    }
    
    /// <summary>
    /// 레벨 증가 및 난이도 조정
    /// </summary>
    private void IncreaseLevel()
    {
        // 최대 레벨 체크
        if (_state.CurrentLevel < _settings.maxLevel)
        {
            _state.CurrentLevel++;
            
            // 레벨에 따른 난이도 조정
            AdjustDifficultyByLevel();
            
            // 이벤트 발생
            OnLevelUp?.Invoke(_state.CurrentLevel);
            
            GameLogger.Info("BrickGameManager", $"레벨 업! 현재 레벨: {_state.CurrentLevel}");
        }
    }
    
    /// <summary>
    /// 레벨에 따른 난이도 조정
    /// </summary>
    private void AdjustDifficultyByLevel()
    {
        // 레벨당 5% 추가 감소
        float levelFactor = 1f - (0.05f * (_state.CurrentLevel - 1));
        _state.CurrentSpawnInterval *= levelFactor;
        _state.CurrentSpawnInterval = Mathf.Max(_state.CurrentSpawnInterval, _settings.minSpawnInterval);
        
        // 다음 스폰 시간 재설정
        _state.NextSpawnTime = Time.time + _state.CurrentSpawnInterval;
    }
    
    /// <summary>
    /// 기본 난이도 조정 (매 스폰마다)
    /// </summary>
    private void AdjustDifficulty()
    {
        // 난이도 증가 (간격 감소)
        _state.CurrentSpawnInterval *= _settings.spawnIntervalDecreaseRate;
        _state.CurrentSpawnInterval = Mathf.Max(_state.CurrentSpawnInterval, _settings.minSpawnInterval);
        
        // 다음 스폰 시간 설정
        _state.NextSpawnTime = Time.time + _state.CurrentSpawnInterval;
    }
    #endregion
    
    #region 이벤트 핸들러
    /// <summary>
    /// 모든 공이 바닥에 떨어졌을 때 호출
    /// </summary>
    private void HandleAllBallsReturned()
    {
        GameLogger.Progress("BrickGameManager", "모든 공이 바닥에 떨어짐 - 다음 턴 준비");
        // TODO: 다음 턴 로직 (플랭크 이동, 공 재발사 등)
    }
    
    /// <summary>
    /// 모든 벽돌이 파괴되었을 때 호출 (스테이지 클리어)
    /// </summary>
    private void HandleAllBricksDestroyed()
    {
        GameLogger.Success("BrickGameManager", "스테이지 클리어! 모든 벽돌 파괴 완료");
        
        // 게임 상태 변경
        _state.CurrentPhase = GamePhase.StageClear;
        
        // TODO: 스테이지 클리어 로직 (다음 스테이지 로드, 보상 등)
    }
    #endregion
}


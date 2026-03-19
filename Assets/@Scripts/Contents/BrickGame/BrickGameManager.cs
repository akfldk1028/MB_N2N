using System;
using UnityEngine;
using Newtonsoft.Json;
using MB.Infrastructure.Messages;

/// <summary>
/// 벽돌깨기 게임 메인 매니저 (Non-MonoBehaviour)
/// Managers.Game.BrickGame 으로 접근
/// </summary>
public class BrickGameManager
{
    #region 의존성 (Dependency Injection)
    private IBrickPlacer _brickPlacer;
    private BrickGameNetworkSync _networkSync;
    #endregion

    #region 설정 및 상태
    private BrickGameSettings _settings;
    private BrickGameState _state;

    /// <summary>
    /// 현재 세션에서 파괴한 벽돌 수 (게임 시작 시 리셋, 저장 시 누적)
    /// </summary>
    private int _sessionBricksDestroyed;

    /// <summary>
    /// 일시정지 전 게임 단계 (Resume 시 복원용)
    /// </summary>
    private GamePhase _previousPhase = GamePhase.Idle;
    #endregion

    #region 저장 데이터 (Save/Load)
    private const string PREF_BRICKGAME_SAVE = "BrickGame_Save";
    private BrickGameSaveData _saveData;

    /// <summary>
    /// 현재 저장 데이터 접근자 (UI에서 최고 점수 등 표시용)
    /// Managers.Game.BrickGame.SaveData로 접근
    /// </summary>
    public BrickGameSaveData SaveData => _saveData;

    /// <summary>
    /// 현재 게임에서 최고 기록을 갱신했는지 여부 (캐싱됨)
    /// 자동 저장 핸들러에서 SaveProgress() 호출 전에 확인하여 캐싱
    /// StartGame() 호출 시 리셋됨
    /// </summary>
    private bool _isNewRecord;
    public bool IsNewRecord => _isNewRecord;
    #endregion

    #region Network 접근 (Managers.Game.BrickGame.Network)
    /// <summary>
    /// 멀티플레이어 네트워크 동기화 매니저
    /// Managers.Game.BrickGame.Network로 접근
    /// </summary>
    public BrickGameNetworkSync Network => _networkSync;

    /// <summary>
    /// 동기화된 점수 (멀티플레이어에서는 NetworkSync 우선)
    /// ✅ MultiplayerUtil 사용
    /// </summary>
    public int Score
    {
        get
        {
            if (_networkSync != null && MultiplayerUtil.IsMultiplayer())
                return _networkSync.Score;
            return _state.CurrentScore;
        }
    }

    /// <summary>
    /// 동기화된 레벨 (멀티플레이어에서는 NetworkSync 우선)
    /// ✅ MultiplayerUtil 사용
    /// </summary>
    public int Level
    {
        get
        {
            if (_networkSync != null && MultiplayerUtil.IsMultiplayer())
                return _networkSync.Level;
            return _state.CurrentLevel;
        }
    }

    /// <summary>
    /// 동기화된 게임 상태 (멀티플레이어에서는 NetworkSync 우선)
    /// ✅ MultiplayerUtil 사용
    /// </summary>
    public GamePhase Phase
    {
        get
        {
            if (_networkSync != null && MultiplayerUtil.IsMultiplayer())
                return _networkSync.Phase;
            return _state.CurrentPhase;
        }
    }
    #endregion
    
    #region Sub-Managers
    // ✅ InputManager 제거: 전역 Managers.Input 사용
    // ✅ WinConditionManager 제거: GameManager 레벨로 이동 (Managers.Game.WinCondition)
    private PlankManager _plankManager;
    private BallManager _ballManager;
    private BrickManager _brickManager;
    private PowerUpDropManager _powerUpDropManager;
    private ComponentChargeManager _chargeManager;

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

    /// <summary>
    /// 파워업 드롭 관리 매니저 접근자
    /// Managers.Game.BrickGame.PowerUpDrop 형태로 사용
    /// </summary>
    public PowerUpDropManager PowerUpDrop => _powerUpDropManager;

    /// <summary>
    /// 컴포넌트 게이지 충전 매니저 접근자
    /// Managers.Game.BrickGame.Charge 형태로 사용
    /// </summary>
    public ComponentChargeManager Charge => _chargeManager;
    #endregion
    
    #region 이벤트
    public event Action OnGameStart;
    public event Action OnGamePause;
    public event Action OnGameResume;
    public event Action OnGameOver;
    public event Action OnStageClear;
    public event Action OnVictory;
    public event Action OnRowSpawn;
    public event Action<int> OnLevelUp;
    public event Action<int> OnScoreChanged;
    #endregion
    
    #region 생성자
    public BrickGameManager()
    {
        _state = new BrickGameState();
        // ✅ InputManager 제거: 전역 Managers.Input 사용
        // ✅ WinConditionManager 제거: GameManager 레벨로 이동
        _plankManager = new PlankManager();
        _ballManager = new BallManager();
        _brickManager = new BrickManager();
        _powerUpDropManager = new PowerUpDropManager();
        _chargeManager = new ComponentChargeManager();

        // Sub-Manager 이벤트 구독
        _ballManager.OnAllBallsReturned += HandleAllBallsReturned;
        _brickManager.OnAllBricksDestroyed += HandleAllBricksDestroyed;

        // 자동 저장 이벤트 구독
        OnGameOver += HandleSaveOnGameOver;
        OnStageClear += HandleSaveOnStageClear;
        OnVictory += HandleSaveOnVictory;

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
        // brickPlacer는 선택적 (Client에서는 null)
        _brickPlacer = brickPlacer;
        _settings = settings ?? BrickGameSettings.CreateDefault();

        // ✅ NetworkManager 참조 제거: MultiplayerUtil 사용

        if (_brickPlacer == null)
        {
            GameLogger.Warning("BrickGameManager", "BrickPlacer가 null입니다. 벽돌 자동 생성 불가 (Client 모드 OK)");
        }

        // Sub-Managers 초기화
        // ✅ InputManager 제거: 전역 Managers.Input이 ActionBus를 통해 입력 발행
        // ✅ WinConditionManager 제거: GameManager 레벨에서 초기화 (Managers.Game.WinCondition)
        _plankManager.Initialize(plank, mainCamera);
        _ballManager.Initialize();
        _brickManager.Initialize();
        _powerUpDropManager.Initialize();

        // 저장 데이터 로드 (Unity API 사용하므로 생성자가 아닌 Initialize에서 호출)
        LoadProgress();

        GameLogger.Success("BrickGameManager", "초기화 완료 (의존성 주입됨)");
    }

    /// <summary>
    /// 멀티플레이어 네트워크 동기화 연결
    /// GameScene에서 BrickGameNetworkSync를 찾아서 연결
    /// </summary>
    public void ConnectNetworkSync(BrickGameNetworkSync networkSync)
    {
        _networkSync = networkSync;
        _networkSync.Initialize(this);

        GameLogger.Success("BrickGameManager", "BrickGameNetworkSync 연결 완료!");
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

        // 세션 벽돌 파괴 카운터 리셋
        _sessionBricksDestroyed = 0;

        // 신기록 플래그 리셋
        _isNewRecord = false;

        // 컴포넌트 게이지 초기화
        _chargeManager.Reset();

        // ✅ GameRule 상태 리셋 (CannonBulletRule의 _lastKnownScore 등)
        Managers.Game?.Rules?.Reset();

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
    /// Time.timeScale = 0 으로 물리/애니메이션 완전 정지
    /// </summary>
    public void PauseGame()
    {
        // ✅ 이중 일시정지 방지
        if (_state.CurrentPhase == GamePhase.Paused)
        {
            GameLogger.Warning("BrickGameManager", "이미 일시정지 상태입니다. 중복 호출 무시.");
            return;
        }

        // ✅ 이전 단계 저장 (Resume 시 복원용)
        _previousPhase = _state.CurrentPhase;

        // ✅ Time.timeScale 정지 (물리/애니메이션 완전 정지)
        Time.timeScale = 0f;

        _state.CurrentPhase = GamePhase.Paused;
        // ✅ 전역 InputManager는 GameMode로 제어 (필요 시 Managers.Input.SetGameMode(None) 호출)
        _plankManager.Enabled = false;

        // ✅ ActionBus를 통해 게임 상태 변경 알림
        Managers.PublishAction(MB.Infrastructure.Messages.ActionId.BrickGame_GameStateChanged,
            new MB.Infrastructure.Messages.BrickGameStatePayload(GamePhase.Paused));

        OnGamePause?.Invoke();
        GameLogger.Info("BrickGameManager", $"게임 일시정지 (이전 상태: {_previousPhase})");
    }
    
    /// <summary>
    /// 게임 재개
    /// Time.timeScale = 1 로 복원, 이전 GamePhase 복귀
    /// </summary>
    public void ResumeGame()
    {
        // ✅ 일시정지 상태가 아니면 무시
        if (_state.CurrentPhase != GamePhase.Paused)
        {
            GameLogger.Warning("BrickGameManager", "일시정지 상태가 아닙니다. Resume 호출 무시.");
            return;
        }

        // ✅ Time.timeScale 복원
        Time.timeScale = 1f;

        // ✅ 이전 단계로 복원 (Playing이 아닐 수 있음)
        _state.CurrentPhase = _previousPhase;
        // ✅ 전역 InputManager는 GameMode로 제어됨
        _plankManager.Enabled = (_previousPhase == GamePhase.Playing);

        // ✅ ActionBus를 통해 게임 상태 변경 알림
        Managers.PublishAction(MB.Infrastructure.Messages.ActionId.BrickGame_GameStateChanged,
            new MB.Infrastructure.Messages.BrickGameStatePayload(_previousPhase));

        OnGameResume?.Invoke();
        GameLogger.Info("BrickGameManager", $"게임 재개 (복원 상태: {_previousPhase})");
    }
    
    /// <summary>
    /// 게임 오버
    /// </summary>
    public void GameOver()
    {
        // ✅ Time.timeScale 안전망: 일시정지 중 게임 오버 시에도 복원
        Time.timeScale = 1f;

        _state.CurrentPhase = GamePhase.GameOver;
        OnGameOver?.Invoke();
        GameLogger.Warning("BrickGameManager", $"게임 오버! 최종 점수: {_state.CurrentScore}");
    }
    #endregion
    
    #region Public Methods - 점수 관리
    /// <summary>
    /// 벽돌 파괴 시 점수 추가 (외부에서 호출)
    /// ✅ 멀티플레이어: BrickGameNetworkSync가 OnScoreChanged 구독 → ActionBus 발행
    /// ✅ 싱글플레이어: 직접 ActionBus 발행
    /// </summary>
    public void AddScore(int waveValue)
    {
        _state.AddScore(waveValue);

        // 세션 벽돌 파괴 카운터 증가 (점수 추가 = 벽돌 파괴)
        _sessionBricksDestroyed++;

        // 이벤트 발생 (BrickGameNetworkSync가 구독 → NetworkVariable 업데이트)
        OnScoreChanged?.Invoke(_state.CurrentScore);

        // ✅ 싱글플레이어: 직접 ActionBus 발행 (BrickGameNetworkSync가 없으면)
        if (_networkSync == null && MultiplayerUtil.IsSinglePlayer())
        {
            Managers.PublishAction(ActionId.BrickGame_ScoreChanged,
                new BrickGameScorePayload(_state.CurrentScore, _state.CurrentLevel));
            GameLogger.DevLog("BrickGameManager", $"[싱글플레이어] ActionBus 점수 발행: {_state.CurrentScore}");
        }

        // ✅ 컴포넌트 게이지 충전 (벽돌 파괴 점수의 10%)
        // 싱글플레이어: playerID=0, 멀티플레이어: 각 플레이어별 BrickGameManager 인스턴스에서 호출
        _chargeManager.OnBrickDestroyed(waveValue, 0);
    }

    /// <summary>
    /// 점수 차감 (총알 발사 시 호출)
    /// ✅ 점수 = 총알 개수이므로, 발사 시 점수 차감
    /// </summary>
    public void SubtractScore(int amount)
    {
        if (amount <= 0) return;

        // 로컬 점수 차감 (멀티/싱글 공통 - _state 동기화 유지)
        int currentScore = _state.CurrentScore;
        int newScore = UnityEngine.Mathf.Max(0, currentScore - amount);
        _state.SetScore(newScore);

        // 멀티플레이어: NetworkSync에도 동기화
        if (_networkSync != null && MultiplayerUtil.IsMultiplayer())
        {
            _networkSync.SubtractScore(amount);
            GameLogger.Info("BrickGameManager", $"[멀티플레이어] 점수 차감: {currentScore} → {newScore} (-{amount})");
            return;
        }

        // 싱글플레이어: 이벤트 + ActionBus 발행
        OnScoreChanged?.Invoke(_state.CurrentScore);

        Managers.PublishAction(ActionId.BrickGame_ScoreChanged,
            new BrickGameScorePayload(_state.CurrentScore, _state.CurrentLevel));

        GameLogger.Info("BrickGameManager", $"[싱글플레이어] 점수 차감: {currentScore} → {newScore} (-{amount})");
    }
    #endregion
    
    #region Public Methods - 게임 상태 조회
    /// <summary>
    /// 게임 일시정지 여부
    /// </summary>
    public bool IsPaused => _state.CurrentPhase == GamePhase.Paused;

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
    /// ✅ MultiplayerUtil 사용
    /// </summary>
    public void OnUpdate()
    {
        // ✅ Plank 이동: 멀티플레이어에서는 PhysicsPlank.Update()가 직접 처리
        // 싱글플레이어에서만 PlankManager 사용
        if (MultiplayerUtil.IsSinglePlayer() && _plankManager != null)
        {
            _plankManager.UpdateMovement(Time.deltaTime);
        }

        // 🔒 게임 로직은 Server만 실행 (Server Authority)
        if (!MultiplayerUtil.HasServerAuthority())
        {
            // Client는 PhysicsPlank가 직접 입력 처리
            return;
        }

        // ===== 이하 Server 전용 로직 =====

        // ✅ 디버깅: OnUpdate가 호출되는지 확인 (매 60프레임마다)
        if (Time.frameCount % 60 == 0)
        {
            GameLogger.Info("BrickGameManager", $"[Server] OnUpdate 호출됨! (프레임: {Time.frameCount}, IsGameActive: {_state.IsGameActive})");
        }

        if (!_state.IsGameActive)
        {
            if (Time.frameCount % 60 == 0)
            {
                GameLogger.Warning("BrickGameManager", "[Server] 게임이 활성화되지 않아 OnUpdate 스킵");
            }
            return;
        }

        // BallManager 파워 타이머 업데이트
        _ballManager.UpdatePowerTimer(Time.deltaTime);

        // 시간 체크하여 새 행 생성 여부 결정 (Server만)
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
    /// 이것은 게임 오버가 아니라 단순히 현재 턴이 끝난 것을 의미
    /// </summary>
    private void HandleAllBallsReturned()
    {
        GameLogger.Progress("BrickGameManager", "모든 공이 바닥에 떨어짐 - 다음 턴 준비");

        // 🔒 Server만 처리
        if (!MultiplayerUtil.HasServerAuthority())
        {
            GameLogger.DevLog("BrickGameManager", "Client에서 HandleAllBallsReturned 무시 (Server 권한 필요)");
            return;
        }

        // 게임이 활성화되어 있지 않으면 처리하지 않음 (이미 GameOver나 StageClear 상태)
        if (!_state.IsGameActive)
        {
            GameLogger.Info("BrickGameManager", "게임이 활성화되지 않음 - 턴 종료 처리 생략");
            return;
        }

        // 다음 턴 준비: 공 상태 리셋 (BallManager가 자동으로 처리)
        // 플랭크는 현재 위치 유지
        GameLogger.Info("BrickGameManager", "다음 턴 준비 완료");
    }

    /// <summary>
    /// 모든 벽돌이 파괴되었을 때 호출 (스테이지 클리어)
    /// </summary>
    private void HandleAllBricksDestroyed()
    {
        GameLogger.Success("BrickGameManager", "스테이지 클리어! 모든 벽돌 파괴 완료");

        // 🔒 Server만 처리
        if (!MultiplayerUtil.HasServerAuthority())
        {
            GameLogger.DevLog("BrickGameManager", "Client에서 HandleAllBricksDestroyed 무시 (Server 권한 필요)");
            return;
        }

        // 게임 상태를 StageClear로 변경
        _state.CurrentPhase = GamePhase.StageClear;

        // 이벤트 발생
        OnStageClear?.Invoke();

        // ActionBus를 통해 게임 상태 변경 알림 (StageClear)
        Managers.PublishAction(MB.Infrastructure.Messages.ActionId.BrickGame_GameStateChanged,
            new MB.Infrastructure.Messages.BrickGameStatePayload(GamePhase.StageClear));

        GameLogger.Info("BrickGameManager", $"현재 레벨: {_state.CurrentLevel}, 최대 레벨: {_settings.maxLevel}");

        // 최대 레벨 도달 체크
        if (_state.CurrentLevel >= _settings.maxLevel)
        {
            // 게임 승리!
            HandleVictory();
        }
        else
        {
            // 다음 레벨로 진행
            PrepareNextStage();
        }
    }

    /// <summary>
    /// 게임 승리 처리 (최대 레벨 도달)
    /// </summary>
    private void HandleVictory()
    {
        GameLogger.Success("BrickGameManager", $"🎉 게임 승리! 최대 레벨({_settings.maxLevel}) 도달!");

        // 게임 상태를 Victory로 변경
        _state.CurrentPhase = GamePhase.Victory;

        // 이벤트 발생
        OnVictory?.Invoke();

        // ActionBus를 통해 게임 상태 변경 알림 (Victory)
        Managers.PublishAction(MB.Infrastructure.Messages.ActionId.BrickGame_GameStateChanged,
            new MB.Infrastructure.Messages.BrickGameStatePayload(GamePhase.Victory));

        // Sub-Managers 비활성화
        _plankManager.Enabled = false;
    }

    /// <summary>
    /// 다음 스테이지 준비 (레벨 증가 및 리셋)
    /// </summary>
    private void PrepareNextStage()
    {
        GameLogger.Info("BrickGameManager", "다음 스테이지 준비 중...");

        // 레벨 증가
        _state.CurrentLevel++;
        _state.ResetRowsSpawned();

        // 레벨업 이벤트 발생
        OnLevelUp?.Invoke(_state.CurrentLevel);

        // 난이도 조정
        AdjustDifficultyByLevel();

        // 게임 상태를 Playing으로 변경 (다시 진행)
        _state.CurrentPhase = GamePhase.Playing;

        // ActionBus를 통해 게임 상태 변경 알림
        Managers.PublishAction(MB.Infrastructure.Messages.ActionId.BrickGame_GameStateChanged,
            new MB.Infrastructure.Messages.BrickGameStatePayload(GamePhase.Playing));

        // BallManager 및 BrickManager 초기화
        _ballManager.Initialize();
        _brickManager.Initialize();

        // Plank 위치 리셋
        _plankManager.ResetPosition();

        // 새로운 벽돌 행 생성 (BrickPlacer가 있을 경우만)
        if (_brickPlacer != null)
        {
            _brickPlacer.PlaceMultipleRows(_settings.initialRowCount);
        }

        GameLogger.Success("BrickGameManager", $"다음 스테이지 시작! 레벨: {_state.CurrentLevel}");
    }
    #endregion

    #region 자동 저장 핸들러 (Auto-Save Handlers)
    /// <summary>
    /// 신기록 여부를 SaveProgress() 호출 전에 캐싱
    /// SaveProgress()가 HighScore를 갱신하면 비교가 불가능해지므로 사전 체크 필요
    /// 한 번 true로 설정되면 StartGame()까지 유지 (중복 저장에도 안전)
    /// </summary>
    private void CheckNewRecord()
    {
        if (_state.CurrentScore > _saveData.HighScore)
            _isNewRecord = true;
    }

    /// <summary>
    /// 게임 오버 시 자동 저장
    /// TotalGamesPlayed 증가 + HighScore/MaxLevel 갱신 + 세션 벽돌 수 누적
    /// </summary>
    private void HandleSaveOnGameOver()
    {
        CheckNewRecord();
        _saveData.TotalGamesPlayed++;
        _saveData.TotalBricksDestroyed += _sessionBricksDestroyed;
        SaveProgress();
        GameLogger.Info("BrickGameManager", $"[자동 저장] 게임 오버 - TotalGamesPlayed: {_saveData.TotalGamesPlayed}, 세션 벽돌: {_sessionBricksDestroyed}");
    }

    /// <summary>
    /// 스테이지 클리어 시 자동 저장
    /// MaxLevel 갱신 + 세션 벽돌 수 누적
    /// </summary>
    private void HandleSaveOnStageClear()
    {
        CheckNewRecord();
        _saveData.TotalBricksDestroyed += _sessionBricksDestroyed;
        _sessionBricksDestroyed = 0; // 스테이지 클리어 후 세션 카운터 리셋 (중복 누적 방지)
        SaveProgress();
        GameLogger.Info("BrickGameManager", $"[자동 저장] 스테이지 클리어 - MaxLevel: {_saveData.MaxLevel}");
    }

    /// <summary>
    /// 게임 승리 시 자동 저장
    /// TotalGamesPlayed + TotalVictories 증가 + HighScore/MaxLevel 갱신 + 세션 벽돌 수 누적
    /// </summary>
    private void HandleSaveOnVictory()
    {
        CheckNewRecord();
        _saveData.TotalGamesPlayed++;
        _saveData.TotalVictories++;
        _saveData.TotalBricksDestroyed += _sessionBricksDestroyed;
        SaveProgress();
        GameLogger.Info("BrickGameManager", $"[자동 저장] 승리! - TotalVictories: {_saveData.TotalVictories}, TotalGamesPlayed: {_saveData.TotalGamesPlayed}");
    }
    #endregion

    #region 저장/로드 (Save/Load Progress)
    /// <summary>
    /// 현재 게임 진행 상태를 PlayerPrefs에 저장
    /// _saveData 필드를 _state 기반으로 업데이트 후 JSON 직렬화하여 저장
    /// </summary>
    public void SaveProgress()
    {
        // 초기화 전이면 저장 스킵
        if (_state == null || _saveData == null) return;

        // 최고 점수 갱신
        if (_state.CurrentScore > _saveData.HighScore)
            _saveData.HighScore = _state.CurrentScore;

        // 최고 레벨 갱신
        if (_state.CurrentLevel > _saveData.MaxLevel)
            _saveData.MaxLevel = _state.CurrentLevel;

        // 마지막 플레이 날짜 업데이트
        _saveData.LastPlayDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        // JSON 직렬화 후 PlayerPrefs에 저장
        string json = JsonConvert.SerializeObject(_saveData);
        PlayerPrefs.SetString(PREF_BRICKGAME_SAVE, json);
        PlayerPrefs.Save();

        GameLogger.Info("BrickGameManager", $"진행 상태 저장 완료 (HighScore: {_saveData.HighScore}, MaxLevel: {_saveData.MaxLevel})");
    }

    /// <summary>
    /// PlayerPrefs에서 저장된 게임 진행 상태를 로드
    /// 저장 데이터가 없으면 기본값으로 초기화
    /// </summary>
    public void LoadProgress()
    {
        string json = PlayerPrefs.GetString(PREF_BRICKGAME_SAVE, "");

        if (!string.IsNullOrEmpty(json))
        {
            try
            {
                _saveData = JsonConvert.DeserializeObject<BrickGameSaveData>(json);
                GameLogger.Info("BrickGameManager", $"진행 상태 로드 완료 (HighScore: {_saveData.HighScore}, MaxLevel: {_saveData.MaxLevel})");
            }
            catch (Exception e)
            {
                GameLogger.Warning("BrickGameManager", $"저장 데이터 파싱 실패, 기본값으로 초기화: {e.Message}");
                _saveData = new BrickGameSaveData();
            }
        }
        else
        {
            _saveData = new BrickGameSaveData();
            GameLogger.Info("BrickGameManager", "저장 데이터 없음, 기본값으로 초기화");
        }
    }
    #endregion

    #region 정리 (Cleanup)
    /// <summary>
    /// BrickGameManager 정리 - 이벤트 구독 해제 및 SubManager 정리
    /// GameManager에서 BrickGame을 교체하거나 씬 전환 시 호출
    /// </summary>
    public void Cleanup()
    {
        // 자동 저장 이벤트 구독 해제
        OnGameOver -= HandleSaveOnGameOver;
        OnStageClear -= HandleSaveOnStageClear;
        OnVictory -= HandleSaveOnVictory;

        // Sub-Manager 이벤트 구독 해제
        if (_ballManager != null)
            _ballManager.OnAllBallsReturned -= HandleAllBallsReturned;
        if (_brickManager != null)
            _brickManager.OnAllBricksDestroyed -= HandleAllBricksDestroyed;

        // PowerUpDropManager 정리
        _powerUpDropManager?.Dispose();

        GameLogger.Info("BrickGameManager", "정리 완료 (이벤트 구독 해제)");
    }
    #endregion

    #region 게임 재시작
    /// <summary>
    /// 게임 재시작 (GameOver 또는 Victory 후)
    /// </summary>
    public void RestartGame()
    {
        GameLogger.Info("BrickGameManager", "게임 재시작 중...");

        // ✅ Time.timeScale 안전망: 일시정지 중 재시작 시에도 복원
        Time.timeScale = 1f;

        // 🔒 Server만 처리
        if (!MultiplayerUtil.HasServerAuthority())
        {
            GameLogger.Warning("BrickGameManager", "Client는 게임 재시작 불가 (Server 권한 필요)");
            return;
        }

        // 게임 상태 완전 초기화
        _state.Reset();
        _state.ResetRowsSpawned();
        _state.ResetScore();

        // ✅ 컴포넌트 게이지 초기화
        _chargeManager.Reset();

        // GameRule 상태 리셋
        Managers.Game?.Rules?.Reset();

        // Sub-Managers 초기화
        _ballManager.Initialize();
        _brickManager.Initialize();
        _plankManager.ResetPosition();
        _plankManager.Enabled = true;

        // 게임 시작
        StartGame();

        GameLogger.Success("BrickGameManager", "게임 재시작 완료!");
    }
    #endregion
}


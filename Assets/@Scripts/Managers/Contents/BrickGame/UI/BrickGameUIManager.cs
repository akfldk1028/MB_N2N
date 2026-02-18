using System;
using UnityEngine;
using MB.Infrastructure.Messages;

/// <summary>
/// BrickGame UI 통합 매니저
/// Managers.UI.BrickGame 으로 접근
///
/// 계층 구조:
/// - Score: 점수 표시 담당
/// - Territory: 땅따먹기 바 담당
/// - GameResult: 게임 결과 표시 담당
/// </summary>
public class BrickGameUIManager
{
    #region Sub-Controllers
    private ScoreUIController _score;
    private TerritoryUIController _territory;
    private GameResultUIController _gameResult;

    /// <summary>
    /// 점수 UI 컨트롤러
    /// Managers.UI.BrickGame.Score 로 접근
    /// </summary>
    public ScoreUIController Score => _score;

    /// <summary>
    /// 땅따먹기 UI 컨트롤러
    /// Managers.UI.BrickGame.Territory 로 접근
    /// </summary>
    public TerritoryUIController Territory => _territory;

    /// <summary>
    /// 게임 결과 UI 컨트롤러
    /// Managers.UI.BrickGame.GameResult 로 접근
    /// </summary>
    public GameResultUIController GameResult => _gameResult;
    #endregion

    #region State
    private bool _initialized = false;
    public bool IsInitialized => _initialized;
    #endregion

    #region 생성자
    public BrickGameUIManager()
    {
        _score = new ScoreUIController();
        _territory = new TerritoryUIController();
        _gameResult = new GameResultUIController();

        GameLogger.SystemStart("BrickGameUIManager", "생성됨 (Score + Territory + GameResult)");
    }
    #endregion

    #region 초기화
    /// <summary>
    /// UI 컨트롤러들 초기화 및 ActionBus 구독
    /// GameScene 진입 시 호출
    /// </summary>
    public void Initialize()
    {
        if (_initialized)
        {
            GameLogger.Warning("BrickGameUIManager", "이미 초기화됨 - 스킵");
            return;
        }

        // Sub-Controllers 초기화
        _score.Initialize();
        _territory.Initialize();
        _gameResult.Initialize();

        // ActionBus 이벤트 구독
        SubscribeToEvents();

        // 초기 점수 동기화 (UI가 늦게 바인딩되어도 현재 점수 표시)
        SyncInitialScores();

        _initialized = true;
        GameLogger.Success("BrickGameUIManager", "초기화 완료");
    }

    /// <summary>
    /// 초기 점수 동기화 (BrickGameMultiplayerSpawner에서 현재 값 가져오기)
    /// </summary>
    private void SyncInitialScores()
    {
        // 멀티플레이어 모드일 때만 필요
        if (!MultiplayerUtil.IsMultiplayer()) return;

        var spawner = GameObject.FindObjectOfType<BrickGameMultiplayerSpawner>();
        if (spawner != null)
        {
            int p0Score = spawner.Player0Score;
            int p1Score = spawner.Player1Score;
            float territory = spawner.TerritoryRatio;

            _score.UpdateScores(p0Score, p1Score);
            _territory.UpdateRatio(territory);

            GameLogger.Info("BrickGameUIManager",
                $"초기 점수 동기화: P0={p0Score}, P1={p1Score}, Territory={territory:F2}");
        }
        else
        {
            GameLogger.DevLog("BrickGameUIManager", "BrickGameMultiplayerSpawner 없음 - 이벤트 구독만 대기");
        }
    }

    /// <summary>
    /// 정리 (GameScene 종료 시 호출)
    /// </summary>
    public void Cleanup()
    {
        if (!_initialized) return;

        // ActionBus 구독 해제
        UnsubscribeFromEvents();

        // Sub-Controllers 정리
        _score.Cleanup();
        _territory.Cleanup();
        _gameResult.Cleanup();

        _initialized = false;
        GameLogger.Info("BrickGameUIManager", "정리 완료");
    }
    #endregion

    #region ActionBus 구독
    private IDisposable _scoreSubscription;
    private IDisposable _territorySubscription;
    private IDisposable _gameEndedSubscription;
    private IDisposable _gameStateSubscription;

    private void SubscribeToEvents()
    {
        // 점수 변경 이벤트 → Score 컨트롤러로 전달
        _scoreSubscription = Managers.Subscribe(ActionId.BrickGame_ScoreChanged, OnScoreChanged);

        // 땅따먹기 영역 변경 이벤트 → Territory 컨트롤러로 전달
        _territorySubscription = Managers.Subscribe(ActionId.BrickGame_TerritoryChanged, OnTerritoryChanged);

        // 게임 종료 이벤트 → GameResult 컨트롤러로 전달
        _gameEndedSubscription = Managers.Subscribe(ActionId.BrickGame_GameEnded, OnGameEnded);

        // 게임 상태 변경 이벤트 → GameResult 컨트롤러로 전달
        _gameStateSubscription = Managers.Subscribe(ActionId.BrickGame_GameStateChanged, OnGameStateChanged);

        GameLogger.Info("BrickGameUIManager", "ActionBus 구독 완료");
    }

    private void UnsubscribeFromEvents()
    {
        _scoreSubscription?.Dispose();
        _scoreSubscription = null;

        _territorySubscription?.Dispose();
        _territorySubscription = null;

        _gameEndedSubscription?.Dispose();
        _gameEndedSubscription = null;

        _gameStateSubscription?.Dispose();
        _gameStateSubscription = null;

        GameLogger.Info("BrickGameUIManager", "ActionBus 구독 해제");
    }

    /// <summary>
    /// 점수 변경 콜백 → Score 컨트롤러로 위임
    /// 멀티플레이어: MultiplayerScorePayload (양쪽 점수 + Territory)
    /// 싱글플레이어: BrickGameScorePayload (단일 점수 + 레벨)
    /// </summary>
    private void OnScoreChanged(ActionMessage message)
    {
        // 멀티플레이어 Payload (BrickGameMultiplayerSpawner에서 발행)
        if (message.TryGetPayload<MultiplayerScorePayload>(out var mpPayload))
        {
            _score.UpdateScores(mpPayload.Player0Score, mpPayload.Player1Score);
            _territory.UpdateRatio(mpPayload.TerritoryRatio);

            GameLogger.DevLog("BrickGameUIManager",
                $"Score 업데이트 [MP]: P0={mpPayload.Player0Score}, P1={mpPayload.Player1Score}, Territory={mpPayload.TerritoryRatio:F2}");
            return;
        }

        // 싱글플레이어 Payload (BrickGameManager/BrickGameNetworkSync에서 발행)
        if (message.TryGetPayload<BrickGameScorePayload>(out var spPayload))
        {
            _score.UpdateScores(spPayload.Score, 0);

            GameLogger.DevLog("BrickGameUIManager",
                $"Score 업데이트 [SP]: Score={spPayload.Score}, Level={spPayload.Level}");
        }
    }

    /// <summary>
    /// 땅따먹기 영역 변경 콜백 → Territory 컨트롤러로 위임
    /// </summary>
    private void OnTerritoryChanged(ActionMessage message)
    {
        if (message.TryGetPayload<TerritoryPayload>(out var payload))
        {
            _territory.UpdateRatio(payload.Ratio);

            GameLogger.DevLog("BrickGameUIManager", $"Territory 업데이트: {payload.Ratio:F2}");
        }
    }

    /// <summary>
    /// 게임 종료 콜백 → GameResult 컨트롤러로 위임
    /// </summary>
    private void OnGameEnded(ActionMessage message)
    {
        if (message.TryGetPayload<GameEndedPayload>(out var payload))
        {
            _gameResult.ShowGameEnded(payload.WinnerPlayerID, payload.LoserPlayerID);

            GameLogger.DevLog("BrickGameUIManager",
                $"GameEnded: Winner={payload.WinnerPlayerID}, Loser={payload.LoserPlayerID}");
        }
    }

    /// <summary>
    /// 게임 상태 변경 콜백 → GameResult 컨트롤러로 위임
    /// </summary>
    private void OnGameStateChanged(ActionMessage message)
    {
        if (message.TryGetPayload<MB.Infrastructure.Messages.BrickGameStatePayload>(out var payload))
        {
            _gameResult.ShowStateResult(payload.Phase);

            GameLogger.DevLog("BrickGameUIManager", $"GameState 변경: {payload.Phase}");
        }
    }
    #endregion

    #region Public API
    /// <summary>
    /// 전체 UI 새로고침
    /// </summary>
    public void RefreshAll()
    {
        _score.Refresh();
        _territory.Refresh();
        _gameResult.Refresh();
    }

    /// <summary>
    /// UI 초기 상태로 리셋
    /// </summary>
    public void Reset()
    {
        _score.Reset();
        _territory.Reset();
        _gameResult.Reset();
    }

    /// <summary>
    /// 현재 점수 상태 동기화 요청 (외부에서 호출 가능)
    /// UI 바인딩 직후, 또는 재접속 시 호출
    /// </summary>
    public void RequestSync()
    {
        SyncInitialScores();
    }
    #endregion
}

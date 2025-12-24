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
/// </summary>
public class BrickGameUIManager
{
    #region Sub-Controllers
    private ScoreUIController _score;
    private TerritoryUIController _territory;

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

        GameLogger.SystemStart("BrickGameUIManager", "생성됨 (Score + Territory)");
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

        _initialized = false;
        GameLogger.Info("BrickGameUIManager", "정리 완료");
    }
    #endregion

    #region ActionBus 구독
    private IDisposable _scoreSubscription;
    private IDisposable _territorySubscription;

    private void SubscribeToEvents()
    {
        // 점수 변경 이벤트 → Score 컨트롤러로 전달
        _scoreSubscription = Managers.Subscribe(ActionId.BrickGame_ScoreChanged, OnScoreChanged);

        // 땅따먹기 영역 변경 이벤트 → Territory 컨트롤러로 전달
        _territorySubscription = Managers.Subscribe(ActionId.BrickGame_TerritoryChanged, OnTerritoryChanged);

        GameLogger.Info("BrickGameUIManager", "ActionBus 구독 완료");
    }

    private void UnsubscribeFromEvents()
    {
        _scoreSubscription?.Dispose();
        _scoreSubscription = null;

        _territorySubscription?.Dispose();
        _territorySubscription = null;

        GameLogger.Info("BrickGameUIManager", "ActionBus 구독 해제");
    }

    /// <summary>
    /// 점수 변경 콜백 → Score 컨트롤러로 위임
    /// </summary>
    private void OnScoreChanged(ActionMessage message)
    {
        if (message.TryGetPayload<MultiplayerScorePayload>(out var payload))
        {
            _score.UpdateScores(payload.Player0Score, payload.Player1Score);
            _territory.UpdateRatio(payload.TerritoryRatio);

            GameLogger.DevLog("BrickGameUIManager",
                $"Score 업데이트: P0={payload.Player0Score}, P1={payload.Player1Score}, Territory={payload.TerritoryRatio:F2}");
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
    #endregion

    #region Public API
    /// <summary>
    /// 전체 UI 새로고침
    /// </summary>
    public void RefreshAll()
    {
        _score.Refresh();
        _territory.Refresh();
    }

    /// <summary>
    /// UI 초기 상태로 리셋
    /// </summary>
    public void Reset()
    {
        _score.Reset();
        _territory.Reset();
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

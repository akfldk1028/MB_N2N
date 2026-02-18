using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 게임 결과 UI 컨트롤러
/// Managers.UI.BrickGame.GameResult 로 접근
///
/// 역할:
/// - 게임 결과 표시 (승리/패배/스테이지클리어)
/// - 최종 점수 표시
/// - 재시작/로비 버튼 처리
/// </summary>
public class GameResultUIController
{
    #region State
    private bool _isShowing = false;
    private GamePhase _resultPhase = GamePhase.Idle;
    private int _winnerPlayerID = -1;
    private int _loserPlayerID = -1;

    public bool IsShowing => _isShowing;
    public GamePhase ResultPhase => _resultPhase;
    #endregion

    #region UI References
    private TMP_Text _resultTitleText;   // "승리!" or "패배..." or "스테이지 클리어!"
    private TMP_Text _resultScoreText;   // 최종 점수
    private TMP_Text _newRecordText;     // "New Record!" 표시
    private Button _restartButton;
    private Button _lobbyButton;
    private GameObject _resultPanel;
    #endregion

    #region Events
    public event Action OnRestartRequested;
    public event Action OnLobbyRequested;
    #endregion

    #region 생성자
    public GameResultUIController()
    {
        GameLogger.Info("GameResultUIController", "생성됨");
    }
    #endregion

    #region 초기화
    public void Initialize()
    {
        _isShowing = false;
        _resultPhase = GamePhase.Idle;
        GameLogger.Success("GameResultUIController", "초기화 완료");
    }

    public void Cleanup()
    {
        _resultTitleText = null;
        _resultScoreText = null;
        _newRecordText = null;
        _restartButton = null;
        _lobbyButton = null;
        _resultPanel = null;
        OnRestartRequested = null;
        OnLobbyRequested = null;

        GameLogger.Info("GameResultUIController", "정리 완료");
    }
    #endregion

    #region UI 바인딩
    public void BindUI(TMP_Text titleText, TMP_Text scoreText, TMP_Text newRecordText, Button restartBtn, Button lobbyBtn, GameObject panel)
    {
        _resultTitleText = titleText;
        _resultScoreText = scoreText;
        _newRecordText = newRecordText;
        _restartButton = restartBtn;
        _lobbyButton = lobbyBtn;
        _resultPanel = panel;

        if (_restartButton != null)
            _restartButton.onClick.AddListener(() => OnRestartRequested?.Invoke());
        if (_lobbyButton != null)
            _lobbyButton.onClick.AddListener(() => OnLobbyRequested?.Invoke());

        // 초기에는 숨김
        Hide();

        GameLogger.Success("GameResultUIController", "UI 바인딩 완료");
    }

    public void UnbindUI()
    {
        if (_restartButton != null)
            _restartButton.onClick.RemoveAllListeners();
        if (_lobbyButton != null)
            _lobbyButton.onClick.RemoveAllListeners();

        _resultTitleText = null;
        _resultScoreText = null;
        _newRecordText = null;
        _restartButton = null;
        _lobbyButton = null;
        _resultPanel = null;

        GameLogger.Info("GameResultUIController", "UI 바인딩 해제");
    }
    #endregion

    #region Public API
    /// <summary>
    /// 게임 종료 결과 표시 (승리/패배)
    /// </summary>
    public void ShowGameEnded(int winnerID, int loserID)
    {
        _winnerPlayerID = winnerID;
        _loserPlayerID = loserID;

        bool isLocalWinner = IsLocalPlayerWinner(winnerID);
        _resultPhase = isLocalWinner ? GamePhase.Victory : GamePhase.GameOver;

        string title = isLocalWinner ? "Victory!" : "Defeat...";
        ShowResult(title, _resultPhase);
    }

    /// <summary>
    /// 게임 상태 변경에 따른 결과 표시 (스테이지 클리어, 승리, 게임오버)
    /// </summary>
    public void ShowStateResult(GamePhase phase)
    {
        if (phase != GamePhase.Victory && phase != GamePhase.GameOver && phase != GamePhase.StageClear)
            return;

        _resultPhase = phase;

        string title = phase switch
        {
            GamePhase.Victory => "Victory!",
            GamePhase.GameOver => "Game Over",
            GamePhase.StageClear => "Stage Clear!",
            _ => ""
        };

        ShowResult(title, phase);
    }

    public void Hide()
    {
        _isShowing = false;
        if (_resultPanel != null)
            _resultPanel.SetActive(false);
        if (_newRecordText != null)
            _newRecordText.gameObject.SetActive(false);
    }

    public void Refresh()
    {
        // 현재 표시 중이면 UI 갱신
        if (_isShowing)
            RefreshUI();
    }

    public void Reset()
    {
        _isShowing = false;
        _resultPhase = GamePhase.Idle;
        _winnerPlayerID = -1;
        _loserPlayerID = -1;
        Hide();
        GameLogger.Info("GameResultUIController", "리셋됨");
    }
    #endregion

    #region Private Methods
    private void ShowResult(string title, GamePhase phase)
    {
        _isShowing = true;

        if (_resultPanel != null)
            _resultPanel.SetActive(true);

        RefreshUI();

        if (_resultTitleText != null)
            _resultTitleText.text = title;

        // New Record 표시 (SaveProgress 호출 전에 체크해야 함)
        if (_newRecordText != null)
        {
            bool isNewRecord = Managers.Game.BrickGame?.IsNewRecord ?? false;
            _newRecordText.gameObject.SetActive(isNewRecord);
            if (isNewRecord)
                _newRecordText.text = "New Record!";
        }

        GameLogger.Success("GameResultUIController", $"결과 표시: {title} (Phase: {phase})");
    }

    private void RefreshUI()
    {
        if (_resultScoreText != null)
        {
            int myScore = Managers.UI.BrickGame.Score.MyScore;
            int opponentScore = Managers.UI.BrickGame.Score.OpponentScore;
            _resultScoreText.text = $"{myScore} vs {opponentScore}";
        }
    }

    private bool IsLocalPlayerWinner(int winnerID)
    {
        var networkManager = Unity.Netcode.NetworkManager.Singleton;
        if (networkManager == null) return true; // 싱글플레이어
        return (int)networkManager.LocalClientId == winnerID;
    }
    #endregion
}

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
    private int _previousHighScore = 0;  // 게임 시작 시점의 최고 점수 (New Record 판별용)

    public bool IsShowing => _isShowing;
    public GamePhase ResultPhase => _resultPhase;
    #endregion

    #region UI References
    private TMP_Text _resultTitleText;   // "승리!" or "패배..." or "스테이지 클리어!"
    private TMP_Text _resultScoreText;   // 최종 점수
    private TMP_Text _newRecordText;     // "New Record!" 표시 텍스트
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

        // 게임 시작 시점의 최고 점수를 스냅샷 (New Record 판별 기준)
        var saveData = BrickGameSaveData.Load();
        _previousHighScore = saveData.HighScore;

        GameLogger.Success("GameResultUIController", $"초기화 완료 (이전 최고 점수: {_previousHighScore})");
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
    public void BindUI(TMP_Text titleText, TMP_Text scoreText, Button restartBtn, Button lobbyBtn, GameObject panel)
    {
        _resultTitleText = titleText;
        _resultScoreText = scoreText;
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

    /// <summary>
    /// New Record 텍스트 UI 바인딩 (별도 바인딩, 선택적)
    /// UI_BrickGameScene에서 호출
    /// </summary>
    public void BindNewRecordUI(TMP_Text newRecordText)
    {
        _newRecordText = newRecordText;

        // 초기에는 숨김
        if (_newRecordText != null)
            _newRecordText.gameObject.SetActive(false);

        GameLogger.Info("GameResultUIController", "New Record UI 바인딩 완료");
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

        // New Record 판별: 현재 점수가 게임 시작 시점의 최고 점수를 초과했는지 확인
        bool isNewRecord = IsNewRecord();
        if (_newRecordText != null)
        {
            _newRecordText.gameObject.SetActive(isNewRecord);
            _newRecordText.text = "New Record!";
        }

        GameLogger.Success("GameResultUIController",
            $"결과 표시: {title} (Phase: {phase}, NewRecord: {isNewRecord})");
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

    /// <summary>
    /// 현재 점수가 이전 최고 점수를 초과했는지 판별
    /// Initialize() 시점에 스냅샷한 _previousHighScore와 비교하여 타이밍 문제 회피
    /// </summary>
    private bool IsNewRecord()
    {
        int currentScore = Managers.UI.BrickGame.Score.MyScore;
        return currentScore > 0 && currentScore > _previousHighScore;
    }

    private bool IsLocalPlayerWinner(int winnerID)
    {
        var networkManager = Unity.Netcode.NetworkManager.Singleton;
        if (networkManager == null) return true; // 싱글플레이어
        return (int)networkManager.LocalClientId == winnerID;
    }
    #endregion
}

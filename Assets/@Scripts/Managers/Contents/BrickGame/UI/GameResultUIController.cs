using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 게임 결과 UI 컨트롤러
/// Managers.UI.BrickGame.GameResult 로 접근
///
/// 역할:
/// - 게임 결과 표시 (승리/패배/스테이지클리어)
/// - 최종 점수 표시 (카운팅 애니메이션)
/// - 등장 애니메이션 (페이드 인 + 타이틀 스케일 펀치 + 점수 카운팅)
/// - 재시작/로비 버튼 처리
///
/// 주의: MonoBehaviour가 아니므로 UIAnimationHelper를 통해 코루틴을 실행합니다.
/// </summary>
public class GameResultUIController
{
    #region Animation Constants
    private const float FADE_IN_DURATION = 0.3f;
    private const float TITLE_PUNCH_SCALE = 1.5f;
    private const float TITLE_PUNCH_DURATION = 0.3f;
    private const float SCORE_COUNT_DURATION = 1.0f;
    #endregion

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
    private Button _restartButton;
    private Button _lobbyButton;
    private GameObject _resultPanel;
    private CanvasGroup _canvasGroup;    // 패널 페이드 효과용
    #endregion

    #region Animation State
    private Coroutine _entranceCoroutine;
    private Coroutine _fadeCoroutine;
    private Coroutine _scalePunchCoroutine;
    private Coroutine _scoreCountCoroutine;
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
        StopEntranceAnimation();

        _resultTitleText = null;
        _resultScoreText = null;
        _restartButton = null;
        _lobbyButton = null;
        _resultPanel = null;
        _canvasGroup = null;
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

        // CanvasGroup 가져오기 또는 추가 (페이드 효과용)
        if (_resultPanel != null)
        {
            _canvasGroup = UIAnimationHelper.GetOrAddCanvasGroup(_resultPanel);
        }

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
        StopEntranceAnimation();

        if (_restartButton != null)
            _restartButton.onClick.RemoveAllListeners();
        if (_lobbyButton != null)
            _lobbyButton.onClick.RemoveAllListeners();

        _resultTitleText = null;
        _resultScoreText = null;
        _restartButton = null;
        _lobbyButton = null;
        _resultPanel = null;
        _canvasGroup = null;

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
        StopEntranceAnimation();
        _isShowing = false;

        // CanvasGroup 알파 리셋
        if (_canvasGroup != null)
            _canvasGroup.alpha = 0f;

        if (_resultPanel != null)
            _resultPanel.SetActive(false);
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

        // 진행 중인 애니메이션 중지
        StopEntranceAnimation();

        // 패널 활성화 및 초기 상태 설정
        if (_resultPanel != null)
            _resultPanel.SetActive(true);

        if (_canvasGroup != null)
            _canvasGroup.alpha = 0f;

        if (_resultTitleText != null)
        {
            _resultTitleText.text = title;
            _resultTitleText.transform.localScale = Vector3.one;
        }

        if (_resultScoreText != null)
            _resultScoreText.text = "";

        // UIAnimationHelper를 통해 등장 애니메이션 실행 (MonoBehaviour가 아니므로 코루틴 호스팅 필요)
        _entranceCoroutine = UIAnimationHelper.Instance.StartAnimCoroutine(
            AnimateResultEntrance(title, phase));

        GameLogger.Success("GameResultUIController", $"결과 표시: {title} (Phase: {phase})");
    }

    /// <summary>
    /// 결과 화면 등장 애니메이션 코루틴
    /// 1) 패널 페이드 인 (0->1, 0.3초) + 타이틀 스케일 펀치 (1.5->1.0, 0.3초) 동시 실행
    /// 2) 점수 카운팅 (0->최종점수, 1.0초) - 페이드 완료 후 실행
    /// 패턴: AchievementUnlocked.cs의 FadeCanvasGroup 방식
    /// </summary>
    private IEnumerator AnimateResultEntrance(string title, GamePhase phase)
    {
        // 1) 패널 페이드 인 + 타이틀 스케일 펀치 동시 실행
        if (_canvasGroup != null)
        {
            _fadeCoroutine = UIAnimationHelper.Instance.StartAnimCoroutine(
                UIAnimationHelper.FadeCanvasGroup(_canvasGroup, 0f, 1f, FADE_IN_DURATION));
        }

        if (_resultTitleText != null)
        {
            _scalePunchCoroutine = UIAnimationHelper.Instance.StartAnimCoroutine(
                UIAnimationHelper.ScalePunch(_resultTitleText.transform, TITLE_PUNCH_SCALE, TITLE_PUNCH_DURATION));
        }

        // 페이드 인 완료 대기
        if (_fadeCoroutine != null)
            yield return _fadeCoroutine;
        _fadeCoroutine = null;

        // 스케일 펀치 완료 대기 (페이드와 동일 시간이지만 안전하게 대기)
        if (_scalePunchCoroutine != null)
            yield return _scalePunchCoroutine;
        _scalePunchCoroutine = null;

        // 2) 점수 카운팅 애니메이션
        if (_resultScoreText != null)
        {
            int myScore = Managers.UI.BrickGame.Score.MyScore;
            int opponentScore = Managers.UI.BrickGame.Score.OpponentScore;

            _scoreCountCoroutine = UIAnimationHelper.Instance.StartAnimCoroutine(
                AnimateScoreCounting(myScore, opponentScore, SCORE_COUNT_DURATION));

            yield return _scoreCountCoroutine;
            _scoreCountCoroutine = null;
        }

        _entranceCoroutine = null;
    }

    /// <summary>
    /// 점수 카운팅 애니메이션 (두 점수를 동시에 0에서 최종값까지 카운팅)
    /// UIAnimationHelper.CountTo 패턴을 따르되, 두 값을 동시에 처리합니다.
    /// </summary>
    private IEnumerator AnimateScoreCounting(int targetMyScore, int targetOpponentScore, float duration)
    {
        if (_resultScoreText == null)
            yield break;

        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);
            int currentMyScore = Mathf.RoundToInt(Mathf.Lerp(0, targetMyScore, t));
            int currentOpponentScore = Mathf.RoundToInt(Mathf.Lerp(0, targetOpponentScore, t));
            _resultScoreText.text = $"{currentMyScore} vs {currentOpponentScore}";
            yield return null;
        }

        _resultScoreText.text = $"{targetMyScore} vs {targetOpponentScore}";
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
    /// 진행 중인 등장 애니메이션을 모두 중지합니다.
    /// 각 애니메이션은 UIAnimationHelper에서 개별 코루틴으로 실행되므로 모두 개별 중지해야 합니다.
    /// </summary>
    private void StopEntranceAnimation()
    {
        if (_fadeCoroutine != null)
        {
            UIAnimationHelper.Instance.StopAnimCoroutine(_fadeCoroutine);
            _fadeCoroutine = null;
        }

        if (_scalePunchCoroutine != null)
        {
            UIAnimationHelper.Instance.StopAnimCoroutine(_scalePunchCoroutine);
            _scalePunchCoroutine = null;
        }

        if (_scoreCountCoroutine != null)
        {
            UIAnimationHelper.Instance.StopAnimCoroutine(_scoreCountCoroutine);
            _scoreCountCoroutine = null;
        }

        if (_entranceCoroutine != null)
        {
            UIAnimationHelper.Instance.StopAnimCoroutine(_entranceCoroutine);
            _entranceCoroutine = null;
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

using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 점수 UI 컨트롤러
/// Managers.UI.BrickGame.Score 로 접근
///
/// 역할:
/// - 내 점수 / 상대 점수 표시 (LocalClientId 기반)
/// - 점수 애니메이션 (선택적)
/// - UI 요소 바인딩 관리
/// </summary>
public class ScoreUIController
{
    #region State
    private int _player0Score = 0;
    private int _player1Score = 0;

    public int Player0Score => _player0Score;
    public int Player1Score => _player1Score;
    public int TotalScore => _player0Score + _player1Score;

    /// <summary>
    /// 내 점수 (LocalClientId 기반)
    /// </summary>
    public int MyScore => MultiplayerUtil.GetLocalClientId() == 0 ? _player0Score : _player1Score;

    /// <summary>
    /// 상대방 점수 (OpponentClientId 기반)
    /// </summary>
    public int OpponentScore => MultiplayerUtil.GetLocalClientId() == 0 ? _player1Score : _player0Score;
    #endregion

    #region UI References
    private TMP_Text _myScoreText;      // 내 점수 (왼쪽 또는 지정된 위치)
    private TMP_Text _opponentScoreText; // 상대 점수 (오른쪽 또는 지정된 위치)
    #endregion

    #region Events
    /// <summary>
    /// 점수 변경 시 발생 (UI 외부에서 구독 가능)
    /// </summary>
    public event Action<int, int> OnScoreUpdated;
    #endregion

    #region 생성자
    public ScoreUIController()
    {
        GameLogger.Info("ScoreUIController", "생성됨");
    }
    #endregion

    #region 초기화
    /// <summary>
    /// 초기화 (BrickGameUIManager에서 호출)
    /// </summary>
    public void Initialize()
    {
        // UI 요소는 BindUI()에서 나중에 바인딩
        GameLogger.Success("ScoreUIController", "초기화 완료");
    }

    /// <summary>
    /// 정리
    /// </summary>
    public void Cleanup()
    {
        _myScoreText = null;
        _opponentScoreText = null;
        OnScoreUpdated = null;

        GameLogger.Info("ScoreUIController", "정리 완료");
    }
    #endregion

    #region UI 바인딩
    /// <summary>
    /// UI 요소 바인딩 (UI_BrickGameScene에서 호출)
    /// 첫 번째 = 내 점수, 두 번째 = 상대 점수
    /// </summary>
    public void BindUI(TMP_Text myScoreText, TMP_Text opponentScoreText)
    {
        _myScoreText = myScoreText;
        _opponentScoreText = opponentScoreText;

        GameLogger.Success("ScoreUIController",
            $"UI 바인딩 완료 (LocalClient={MultiplayerUtil.GetLocalClientId()})");

        // 바인딩 직후 현재 점수 표시
        Refresh();
    }

    /// <summary>
    /// UI 바인딩 해제
    /// </summary>
    public void UnbindUI()
    {
        _myScoreText = null;
        _opponentScoreText = null;

        GameLogger.Info("ScoreUIController", "UI 바인딩 해제");
    }
    #endregion

    #region Public API
    /// <summary>
    /// 점수 업데이트 (ActionBus에서 호출됨)
    /// </summary>
    public void UpdateScores(int player0Score, int player1Score)
    {
        bool changed = (_player0Score != player0Score) || (_player1Score != player1Score);

        _player0Score = player0Score;
        _player1Score = player1Score;

        // UI 업데이트
        RefreshUI();

        // 이벤트 발생
        if (changed)
        {
            OnScoreUpdated?.Invoke(_player0Score, _player1Score);
        }
    }

    /// <summary>
    /// 특정 플레이어 점수만 업데이트
    /// </summary>
    public void UpdatePlayerScore(ulong clientId, int score)
    {
        if (clientId == 0)
        {
            _player0Score = score;
        }
        else
        {
            _player1Score = score;
        }

        RefreshUI();
        OnScoreUpdated?.Invoke(_player0Score, _player1Score);
    }

    /// <summary>
    /// UI 새로고침 (현재 상태 반영)
    /// </summary>
    public void Refresh()
    {
        RefreshUI();
    }

    /// <summary>
    /// 점수 초기화
    /// </summary>
    public void Reset()
    {
        _player0Score = 0;
        _player1Score = 0;
        RefreshUI();

        GameLogger.Info("ScoreUIController", "점수 리셋됨");
    }
    #endregion

    #region Private Methods
    private void RefreshUI()
    {
        // 내 점수 표시
        if (_myScoreText != null)
        {
            _myScoreText.text = FormatScore(MyScore);
        }

        // 상대 점수 표시
        if (_opponentScoreText != null)
        {
            _opponentScoreText.text = FormatScore(OpponentScore);
        }
    }

    /// <summary>
    /// 점수 포맷팅 (확장 가능)
    /// </summary>
    private string FormatScore(int score)
    {
        // 1000 이상이면 콤마 추가
        if (score >= 1000)
        {
            return score.ToString("N0");
        }
        return score.ToString();
    }
    #endregion
}

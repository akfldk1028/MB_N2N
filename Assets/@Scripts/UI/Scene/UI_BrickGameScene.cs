using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MB.Infrastructure.Messages;

/// <summary>
/// 블록깨기 멀티플레이어 게임 UI (점수 + 땅따먹기)
///
/// 화면 구조:
/// - 왼쪽 30%: 내 게임 영역 (카메라 렌더링)
/// - 중앙 40%: 땅따먹기 Territory Bar (UI)
/// - 오른쪽 30%: 상대 게임 영역 (카메라 렌더링)
/// </summary>
public class UI_BrickGameScene : UI_Scene
{
    #region Enum Bindings
    enum Texts
    {
        Player0ScoreText,   // Player 0 점수
        Player1ScoreText,   // Player 1 점수
    }

    enum Images
    {
        TerritoryBarFill,   // 땅따먹기 바 채움 (Player 0 = 왼쪽, Player 1 = 오른쪽)
    }

    enum Objects
    {
        TerritoryBarContainer,  // 땅따먹기 바 컨테이너
    }
    #endregion

    #region 상태
    private int _player0Score = 0;
    private int _player1Score = 0;
    private float _territoryRatio = 0.5f;
    #endregion

    public override bool Init()
    {
        if (base.Init() == false)
            return false;

        // UI 바인딩
        BindTexts(typeof(Texts));
        BindImages(typeof(Images));
        BindObjects(typeof(Objects));

        // ActionBus 구독
        SubscribeToEvents();

        // 초기 UI 설정
        RefreshUI();

        GameLogger.Success("UI_BrickGameScene", "초기화 완료");
        return true;
    }

    private void OnDestroy()
    {
        // ActionBus 구독 해제
        UnsubscribeFromEvents();
    }

    #region ActionBus 구독
    private void SubscribeToEvents()
    {
        // 점수 변경 이벤트 구독
        Managers.Subscribe(ActionId.BrickGame_ScoreChanged, OnScoreChanged);

        // 땅따먹기 영역 변경 이벤트 구독
        Managers.Subscribe(ActionId.BrickGame_TerritoryChanged, OnTerritoryChanged);

        GameLogger.Info("UI_BrickGameScene", "ActionBus 이벤트 구독 완료");
    }

    private void UnsubscribeFromEvents()
    {
        // 점수 변경 이벤트 구독 해제
        Managers.Unsubscribe(ActionId.BrickGame_ScoreChanged, OnScoreChanged);

        // 땅따먹기 영역 변경 이벤트 구독 해제
        Managers.Unsubscribe(ActionId.BrickGame_TerritoryChanged, OnTerritoryChanged);

        GameLogger.Info("UI_BrickGameScene", "ActionBus 이벤트 구독 해제");
    }

    /// <summary>
    /// 점수 변경 콜백
    /// </summary>
    private void OnScoreChanged(ActionMessage message)
    {
        if (message.TryGetPayload<MultiplayerScorePayload>(out var payload))
        {
            _player0Score = payload.Player0Score;
            _player1Score = payload.Player1Score;
            _territoryRatio = payload.TerritoryRatio;

            RefreshScoreUI();
            RefreshTerritoryUI();

            GameLogger.DevLog("UI_BrickGameScene",
                $"점수 업데이트: P0={_player0Score}, P1={_player1Score}, Territory={_territoryRatio:F2}");
        }
    }

    /// <summary>
    /// 땅따먹기 영역 변경 콜백
    /// </summary>
    private void OnTerritoryChanged(ActionMessage message)
    {
        if (message.TryGetPayload<TerritoryPayload>(out var payload))
        {
            _territoryRatio = payload.Ratio;
            RefreshTerritoryUI();

            GameLogger.DevLog("UI_BrickGameScene", $"Territory 업데이트: {_territoryRatio:F2}");
        }
    }
    #endregion

    #region UI 업데이트
    /// <summary>
    /// 전체 UI 새로고침
    /// </summary>
    private void RefreshUI()
    {
        RefreshScoreUI();
        RefreshTerritoryUI();
    }

    /// <summary>
    /// 점수 UI 새로고침
    /// </summary>
    private void RefreshScoreUI()
    {
        var player0Text = GetText((int)Texts.Player0ScoreText);
        var player1Text = GetText((int)Texts.Player1ScoreText);

        if (player0Text != null)
        {
            player0Text.text = $"{_player0Score}";
        }

        if (player1Text != null)
        {
            player1Text.text = $"{_player1Score}";
        }
    }

    /// <summary>
    /// 땅따먹기 바 UI 새로고침
    /// </summary>
    private void RefreshTerritoryUI()
    {
        var fillImage = GetImage((int)Images.TerritoryBarFill);
        if (fillImage == null) return;

        // Territory Ratio: 0.0 = Player 0 완전 승리, 0.5 = 중립, 1.0 = Player 1 완전 승리
        // Fill Amount로 표현: Player 0 영역 = 1 - ratio, Player 1 영역 = ratio

        // Fill Direction이 Left-to-Right라면:
        // fillAmount = territoryRatio → Player 1이 이기면 바가 오른쪽으로 채워짐

        fillImage.fillAmount = _territoryRatio;

        // 색상 변경 (누가 이기고 있는지에 따라)
        if (_territoryRatio < 0.45f)
        {
            // Player 0 우세 (파란색)
            fillImage.color = Color.Lerp(Color.blue, Color.white, _territoryRatio / 0.45f);
        }
        else if (_territoryRatio > 0.55f)
        {
            // Player 1 우세 (빨간색)
            fillImage.color = Color.Lerp(Color.white, Color.red, (_territoryRatio - 0.55f) / 0.45f);
        }
        else
        {
            // 중립 (흰색/회색)
            fillImage.color = Color.white;
        }
    }
    #endregion

    #region Public API
    /// <summary>
    /// 외부에서 점수 직접 설정 (테스트용)
    /// </summary>
    public void SetScores(int player0Score, int player1Score)
    {
        _player0Score = player0Score;
        _player1Score = player1Score;
        RefreshScoreUI();
    }

    /// <summary>
    /// 외부에서 Territory 비율 직접 설정 (테스트용)
    /// </summary>
    public void SetTerritoryRatio(float ratio)
    {
        _territoryRatio = Mathf.Clamp01(ratio);
        RefreshTerritoryUI();
    }
    #endregion
}

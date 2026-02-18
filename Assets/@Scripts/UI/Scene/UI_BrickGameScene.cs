using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 블록깨기 멀티플레이어 게임 UI (레이아웃 + 바인딩만 담당)
///
/// 역할:
/// - UI 요소 바인딩 (Enum Binding)
/// - Managers.UI.BrickGame 컨트롤러에 UI 연결
/// - 레이아웃 관리
///
/// 로직 담당:
/// - Score → Managers.UI.BrickGame.Score
/// - Territory → Managers.UI.BrickGame.Territory
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
        MyScoreText,        // 내 점수 (LocalClient)
        OpponentScoreText,  // 상대 점수 (Opponent)
        ResultTitleText,    // 게임 결과 제목 ("Victory!" / "Game Over" / "Stage Clear!")
        ResultScoreText,    // 게임 결과 점수
    }

    enum Images
    {
        TerritoryBarFill,   // 땅따먹기 바 채움
    }

    enum Buttons
    {
        RestartButton,      // 재시작 버튼
        LobbyButton,        // 로비 버튼
    }

    enum Objects
    {
        TerritoryBarContainer,  // 땅따먹기 바 컨테이너
        GameResultPanel,        // 게임 결과 패널 (Victory/GameOver/StageClear)
    }
    #endregion

    public override bool Init()
    {
        if (base.Init() == false)
            return false;

        // 1. UI 바인딩
        BindTexts(typeof(Texts));
        BindImages(typeof(Images));
        BindButtons(typeof(Buttons));
        BindObjects(typeof(Objects));

        // 2. BrickGameUIManager 초기화
        Managers.UI.BrickGame.Initialize();

        // 3. 컨트롤러에 UI 요소 바인딩
        BindUIToControllers();

        GameLogger.Success("UI_BrickGameScene", "초기화 완료 (UIManager 연동)");
        return true;
    }

    private void OnDestroy()
    {
        // 컨트롤러 UI 바인딩 해제
        UnbindUIFromControllers();

        // BrickGameUIManager 정리
        Managers.UI.BrickGame.Cleanup();

        GameLogger.Info("UI_BrickGameScene", "정리 완료");
    }

    #region Controller UI 바인딩
    /// <summary>
    /// 컨트롤러에 UI 요소 전달
    /// </summary>
    private void BindUIToControllers()
    {
        // Score 컨트롤러에 Text 바인딩 (내 점수, 상대 점수)
        var myScoreText = GetText((int)Texts.MyScoreText);
        var opponentScoreText = GetText((int)Texts.OpponentScoreText);
        Managers.UI.BrickGame.Score.BindUI(myScoreText, opponentScoreText);

        // Territory 컨트롤러에 Image 바인딩
        var fillImage = GetImage((int)Images.TerritoryBarFill);
        var container = GetObject((int)Objects.TerritoryBarContainer);
        Managers.UI.BrickGame.Territory.BindUI(fillImage, container);

        // GameResult 컨트롤러에 UI 바인딩
        var resultTitle = GetText((int)Texts.ResultTitleText);
        var resultScore = GetText((int)Texts.ResultScoreText);
        var restartBtn = GetButton((int)Buttons.RestartButton);
        var lobbyBtn = GetButton((int)Buttons.LobbyButton);
        var resultPanel = GetObject((int)Objects.GameResultPanel);
        Managers.UI.BrickGame.GameResult.BindUI(resultTitle, resultScore, restartBtn, lobbyBtn, resultPanel);

        // GameResult 이벤트 구독 (재시작/로비 버튼)
        Managers.UI.BrickGame.GameResult.OnRestartRequested += OnRestartRequested;
        Managers.UI.BrickGame.GameResult.OnLobbyRequested += OnLobbyRequested;

        // 초기 점수 동기화 요청 (UI가 바인딩된 후 현재 상태 반영)
        Managers.UI.BrickGame.RequestSync();

        GameLogger.Success("UI_BrickGameScene", "컨트롤러에 UI 바인딩 완료");
    }

    /// <summary>
    /// 컨트롤러 UI 바인딩 해제
    /// </summary>
    private void UnbindUIFromControllers()
    {
        Managers.UI.BrickGame.Score.UnbindUI();
        Managers.UI.BrickGame.Territory.UnbindUI();
        Managers.UI.BrickGame.GameResult.OnRestartRequested -= OnRestartRequested;
        Managers.UI.BrickGame.GameResult.OnLobbyRequested -= OnLobbyRequested;
        Managers.UI.BrickGame.GameResult.UnbindUI();

        GameLogger.Info("UI_BrickGameScene", "컨트롤러 UI 바인딩 해제");
    }
    #endregion

    #region GameResult 이벤트 핸들러
    private void OnRestartRequested()
    {
        GameLogger.Info("UI_BrickGameScene", "재시작 요청");
        Managers.UI.BrickGame.GameResult.Hide();

        if (MultiplayerUtil.IsSinglePlayer())
        {
            Managers.Game.BrickGame?.RestartGame();
        }
    }

    private void OnLobbyRequested()
    {
        GameLogger.Info("UI_BrickGameScene", "로비 복귀 요청");
        Managers.Scene.LoadScene(Define.EScene.TitleScene);
    }
    #endregion

    #region Public API (테스트용)
    /// <summary>
    /// 외부에서 점수 직접 설정 (테스트용)
    /// </summary>
    public void SetScores(int player0Score, int player1Score)
    {
        Managers.UI.BrickGame.Score.UpdateScores(player0Score, player1Score);
    }

    /// <summary>
    /// 외부에서 Territory 비율 직접 설정 (테스트용)
    /// </summary>
    public void SetTerritoryRatio(float ratio)
    {
        Managers.UI.BrickGame.Territory.UpdateRatio(ratio);
    }
    #endregion
}

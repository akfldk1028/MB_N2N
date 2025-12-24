using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 땅따먹기 UI 컨트롤러
/// Managers.UI.BrickGame.Territory 로 접근
///
/// 역할:
/// - 땅따먹기 바 표시 (fillAmount)
/// - 비율에 따른 색상 변경
/// - 승리 상태 표시
/// </summary>
public class TerritoryUIController
{
    #region State
    private float _ratio = 0.5f;  // 0.0 ~ 1.0 (0.5 = 중립)

    public float Ratio => _ratio;

    /// <summary>
    /// 누가 이기고 있는지 (null = 동점)
    /// </summary>
    public ulong? WinningPlayer
    {
        get
        {
            if (_ratio < 0.45f) return 0;  // Player 0 우세
            if (_ratio > 0.55f) return 1;  // Player 1 우세
            return null;  // 동점
        }
    }
    #endregion

    #region UI References
    private Image _fillImage;
    private GameObject _container;
    #endregion

    #region 색상 설정
    private Color _player0Color = Color.blue;    // Player 0 우세 색상
    private Color _player1Color = Color.red;     // Player 1 우세 색상
    private Color _neutralColor = Color.white;   // 중립 색상
    #endregion

    #region Events
    /// <summary>
    /// 영역 비율 변경 시 발생
    /// </summary>
    public event Action<float> OnRatioChanged;

    /// <summary>
    /// 승리 상태 변경 시 발생 (null = 동점)
    /// </summary>
    public event Action<ulong?> OnWinningPlayerChanged;
    #endregion

    #region 생성자
    public TerritoryUIController()
    {
        GameLogger.Info("TerritoryUIController", "생성됨");
    }
    #endregion

    #region 초기화
    /// <summary>
    /// 초기화 (BrickGameUIManager에서 호출)
    /// </summary>
    public void Initialize()
    {
        GameLogger.Success("TerritoryUIController", "초기화 완료");
    }

    /// <summary>
    /// 정리
    /// </summary>
    public void Cleanup()
    {
        _fillImage = null;
        _container = null;
        OnRatioChanged = null;
        OnWinningPlayerChanged = null;

        GameLogger.Info("TerritoryUIController", "정리 완료");
    }
    #endregion

    #region UI 바인딩
    /// <summary>
    /// UI 요소 바인딩 (UI_BrickGameScene에서 호출)
    /// </summary>
    public void BindUI(Image fillImage, GameObject container = null)
    {
        _fillImage = fillImage;
        _container = container;

        GameLogger.Success("TerritoryUIController", "UI 바인딩 완료");

        // 바인딩 직후 현재 상태 표시
        Refresh();
    }

    /// <summary>
    /// UI 바인딩 해제
    /// </summary>
    public void UnbindUI()
    {
        _fillImage = null;
        _container = null;

        GameLogger.Info("TerritoryUIController", "UI 바인딩 해제");
    }
    #endregion

    #region Public API
    /// <summary>
    /// 영역 비율 업데이트 (ActionBus에서 호출됨)
    /// </summary>
    public void UpdateRatio(float ratio)
    {
        ulong? previousWinner = WinningPlayer;
        float previousRatio = _ratio;

        _ratio = Mathf.Clamp01(ratio);

        // UI 업데이트
        RefreshUI();

        // 이벤트 발생
        if (!Mathf.Approximately(previousRatio, _ratio))
        {
            OnRatioChanged?.Invoke(_ratio);
        }

        // 승리 상태 변경 체크
        ulong? currentWinner = WinningPlayer;
        if (previousWinner != currentWinner)
        {
            OnWinningPlayerChanged?.Invoke(currentWinner);
        }
    }

    /// <summary>
    /// UI 새로고침 (현재 상태 반영)
    /// </summary>
    public void Refresh()
    {
        RefreshUI();
    }

    /// <summary>
    /// 초기 상태로 리셋
    /// </summary>
    public void Reset()
    {
        _ratio = 0.5f;
        RefreshUI();

        GameLogger.Info("TerritoryUIController", "영역 리셋됨 (0.5)");
    }

    /// <summary>
    /// 색상 설정 변경
    /// </summary>
    public void SetColors(Color player0Color, Color player1Color, Color neutralColor)
    {
        _player0Color = player0Color;
        _player1Color = player1Color;
        _neutralColor = neutralColor;
        RefreshUI();
    }
    #endregion

    #region Private Methods
    private void RefreshUI()
    {
        if (_fillImage == null) return;

        // Fill Amount 업데이트
        _fillImage.fillAmount = _ratio;

        // 색상 업데이트
        _fillImage.color = CalculateColor();
    }

    /// <summary>
    /// 비율에 따른 색상 계산
    /// </summary>
    private Color CalculateColor()
    {
        if (_ratio < 0.45f)
        {
            // Player 0 우세 (파란색으로 전환)
            float t = _ratio / 0.45f;
            return Color.Lerp(_player0Color, _neutralColor, t);
        }
        else if (_ratio > 0.55f)
        {
            // Player 1 우세 (빨간색으로 전환)
            float t = (_ratio - 0.55f) / 0.45f;
            return Color.Lerp(_neutralColor, _player1Color, t);
        }
        else
        {
            // 중립 (흰색)
            return _neutralColor;
        }
    }
    #endregion
}

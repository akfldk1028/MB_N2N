using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 컴포넌트 게이지 UI 컨트롤러
/// Managers.UI.BrickGame.ComponentGauge 로 접근
///
/// 역할:
/// - Bomb/Harvest 게이지 바 표시 (fillAmount)
/// - 게이지 충전 비율에 따른 색상 변경
/// - 사용 가능 상태 아이콘 표시
/// </summary>
public class ComponentGaugeUIController
{
    #region State
    private float _bombRatio = 0f;      // 0.0 ~ 1.0
    private float _harvestRatio = 0f;   // 0.0 ~ 1.0
    private bool _bombUsable = false;
    private bool _harvestUsable = false;

    public float BombRatio => _bombRatio;
    public float HarvestRatio => _harvestRatio;
    public bool BombUsable => _bombUsable;
    public bool HarvestUsable => _harvestUsable;
    #endregion

    #region UI References
    private Image _bombFillImage;
    private Image _harvestFillImage;
    private GameObject _bombUsableIcon;
    private GameObject _harvestUsableIcon;
    #endregion

    #region 색상 설정
    private Color _bombColor = new Color(1f, 0.5f, 0f, 1f);       // 주황색 (BombComponent explosionColor)
    private Color _harvestColor = new Color(0.2f, 0.8f, 0.2f, 1f); // 초록색 (HarvestComponent harvestColor)
    private Color _usableColor = Color.white;                       // 풀 충전 색상
    #endregion

    #region Events
    /// <summary>
    /// Bomb 게이지 변경 시 발생 (ratio, usable)
    /// </summary>
    public event Action<float, bool> OnBombGaugeChanged;

    /// <summary>
    /// Harvest 게이지 변경 시 발생 (ratio, usable)
    /// </summary>
    public event Action<float, bool> OnHarvestGaugeChanged;
    #endregion

    #region 생성자
    public ComponentGaugeUIController()
    {
        GameLogger.Info("ComponentGaugeUIController", "생성됨");
    }
    #endregion

    #region 초기화
    /// <summary>
    /// 초기화 (BrickGameUIManager에서 호출)
    /// </summary>
    public void Initialize()
    {
        GameLogger.Success("ComponentGaugeUIController", "초기화 완료");
    }

    /// <summary>
    /// 정리
    /// </summary>
    public void Cleanup()
    {
        _bombFillImage = null;
        _harvestFillImage = null;
        _bombUsableIcon = null;
        _harvestUsableIcon = null;
        OnBombGaugeChanged = null;
        OnHarvestGaugeChanged = null;

        GameLogger.Info("ComponentGaugeUIController", "정리 완료");
    }
    #endregion

    #region UI 바인딩
    /// <summary>
    /// UI 요소 바인딩 (UI_BrickGameScene에서 호출)
    /// </summary>
    public void BindUI(Image bombFillImage, Image harvestFillImage, GameObject bombUsableIcon = null, GameObject harvestUsableIcon = null)
    {
        _bombFillImage = bombFillImage;
        _harvestFillImage = harvestFillImage;
        _bombUsableIcon = bombUsableIcon;
        _harvestUsableIcon = harvestUsableIcon;

        GameLogger.Success("ComponentGaugeUIController", "UI 바인딩 완료");

        // 바인딩 직후 현재 상태 표시
        Refresh();
    }

    /// <summary>
    /// UI 바인딩 해제
    /// </summary>
    public void UnbindUI()
    {
        _bombFillImage = null;
        _harvestFillImage = null;
        _bombUsableIcon = null;
        _harvestUsableIcon = null;

        GameLogger.Info("ComponentGaugeUIController", "UI 바인딩 해제");
    }
    #endregion

    #region Public API
    /// <summary>
    /// Bomb 게이지 업데이트 (ActionBus에서 호출됨)
    /// </summary>
    public void UpdateBombGauge(float ratio, bool usable)
    {
        float previousRatio = _bombRatio;
        bool previousUsable = _bombUsable;

        _bombRatio = Mathf.Clamp01(ratio);
        _bombUsable = usable;

        // UI 업데이트
        RefreshUI();

        // 이벤트 발생
        if (!Mathf.Approximately(previousRatio, _bombRatio) || previousUsable != _bombUsable)
        {
            OnBombGaugeChanged?.Invoke(_bombRatio, _bombUsable);
        }
    }

    /// <summary>
    /// Harvest 게이지 업데이트 (ActionBus에서 호출됨)
    /// </summary>
    public void UpdateHarvestGauge(float ratio, bool usable)
    {
        float previousRatio = _harvestRatio;
        bool previousUsable = _harvestUsable;

        _harvestRatio = Mathf.Clamp01(ratio);
        _harvestUsable = usable;

        // UI 업데이트
        RefreshUI();

        // 이벤트 발생
        if (!Mathf.Approximately(previousRatio, _harvestRatio) || previousUsable != _harvestUsable)
        {
            OnHarvestGaugeChanged?.Invoke(_harvestRatio, _harvestUsable);
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
        _bombRatio = 0f;
        _harvestRatio = 0f;
        _bombUsable = false;
        _harvestUsable = false;
        RefreshUI();

        GameLogger.Info("ComponentGaugeUIController", "게이지 리셋됨");
    }

    /// <summary>
    /// 색상 설정 변경
    /// </summary>
    public void SetColors(Color bombColor, Color harvestColor, Color usableColor)
    {
        _bombColor = bombColor;
        _harvestColor = harvestColor;
        _usableColor = usableColor;
        RefreshUI();
    }
    #endregion

    #region Private Methods
    private void RefreshUI()
    {
        // Bomb 게이지 업데이트
        if (_bombFillImage != null)
        {
            _bombFillImage.fillAmount = _bombRatio;
            _bombFillImage.color = _bombUsable ? _usableColor : _bombColor;
        }

        // Harvest 게이지 업데이트
        if (_harvestFillImage != null)
        {
            _harvestFillImage.fillAmount = _harvestRatio;
            _harvestFillImage.color = _harvestUsable ? _usableColor : _harvestColor;
        }

        // 사용 가능 아이콘 표시
        if (_bombUsableIcon != null)
        {
            _bombUsableIcon.SetActive(_bombUsable);
        }

        if (_harvestUsableIcon != null)
        {
            _harvestUsableIcon.SetActive(_harvestUsable);
        }
    }
    #endregion
}

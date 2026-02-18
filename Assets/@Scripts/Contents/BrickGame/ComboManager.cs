using System;
using UnityEngine;

/// <summary>
/// 벽돌깨기 게임의 콤보 시스템 매니저
/// - 연속 벽돌 파괴 시 콤보 카운트 추적
/// - 2초 타임아웃으로 콤보 리셋
/// - 콤보 배수 계산 (2연속=1.5x, 5연속=2x, 10연속=3x, 20연속=5x)
/// - OnComboChanged 이벤트 발행 (UI 연동용)
/// </summary>
public class ComboManager
{
    #region 콤보 상태
    private int _comboCount = 0; // 현재 콤보 카운트
    private float _lastDestroyTime = -999f; // 마지막 벽돌 파괴 시간
    private float _comboTimer = 0f; // 콤보 타임아웃 남은 시간

    private const float COMBO_TIMEOUT = 2f; // 콤보 유지 시간 (초)

    /// <summary>
    /// 현재 콤보 카운트
    /// </summary>
    public int ComboCount => _comboCount;

    /// <summary>
    /// 현재 콤보 배수
    /// </summary>
    public float Multiplier => GetMultiplier();
    #endregion

    #region 이벤트
    /// <summary>
    /// 콤보 상태가 변경될 때 발생 (comboCount, multiplier)
    /// </summary>
    public event Action<int, float> OnComboChanged;
    #endregion

    #region 초기화
    public ComboManager()
    {
        GameLogger.Progress("ComboManager", "ComboManager 생성됨");
    }

    /// <summary>
    /// 매니저 초기화 (게임 시작 시)
    /// </summary>
    public void Initialize()
    {
        Reset();
        GameLogger.Success("ComboManager", "ComboManager 초기화 완료");
    }
    #endregion

    #region 콤보 추적
    /// <summary>
    /// 벽돌 파괴 알림 - 콤보 카운트 증가 또는 리셋
    /// </summary>
    public void NotifyBrickDestroyed()
    {
        float currentTime = Time.time;

        if (currentTime - _lastDestroyTime < COMBO_TIMEOUT && _comboCount > 0)
        {
            // 타임아웃 이내 연속 파괴 → 콤보 증가
            _comboCount++;
        }
        else
        {
            // 타임아웃 초과 또는 첫 파괴 → 콤보 시작
            _comboCount = 1;
        }

        _lastDestroyTime = currentTime;
        _comboTimer = COMBO_TIMEOUT;

        float multiplier = GetMultiplier();
        OnComboChanged?.Invoke(_comboCount, multiplier);

        if (_comboCount >= 2)
        {
            GameLogger.Info("ComboManager", $"콤보 {_comboCount}연속! 배수: {multiplier:F1}x");
        }
    }

    /// <summary>
    /// 매 프레임 호출 - 콤보 타임아웃 확인
    /// </summary>
    public void Update(float deltaTime)
    {
        if (_comboCount > 0 && _comboTimer > 0)
        {
            _comboTimer -= deltaTime;

            if (_comboTimer <= 0)
            {
                _comboTimer = 0;
                int previousCombo = _comboCount;
                _comboCount = 0;

                OnComboChanged?.Invoke(0, 1f);
                GameLogger.Info("ComboManager", $"콤보 타임아웃 (이전 콤보: {previousCombo}연속)");
            }
        }
    }
    #endregion

    #region 콤보 배수 계산
    /// <summary>
    /// 현재 콤보 카운트에 따른 배수 반환
    /// 2연속=1.5x, 5연속=2.0x, 10연속=3.0x, 20연속=5.0x
    /// </summary>
    public float GetMultiplier()
    {
        if (_comboCount < 2) return 1.0f;
        if (_comboCount < 5) return 1.5f;
        if (_comboCount < 10) return 2.0f;
        if (_comboCount < 20) return 3.0f;
        return 5.0f;
    }
    #endregion

    #region 리셋
    /// <summary>
    /// 콤보 상태 리셋 (게임 시작/종료 시)
    /// </summary>
    public void Reset()
    {
        _comboCount = 0;
        _lastDestroyTime = -999f;
        _comboTimer = 0f;

        OnComboChanged?.Invoke(0, 1f);
        GameLogger.Info("ComboManager", "콤보 상태 리셋");
    }
    #endregion

    #region 유틸리티
    /// <summary>
    /// 디버그 정보
    /// </summary>
    public override string ToString()
    {
        return $"[ComboManager] Combo: {_comboCount}, Multiplier: {GetMultiplier():F1}x, Timer: {_comboTimer:F1}s";
    }
    #endregion
}

using System;
using System.Collections.Generic;
using UnityEngine;
using MB.Infrastructure.Messages;

/// <summary>
/// 컴포넌트 게이지 충전 매니저 (Non-MonoBehaviour POCO)
/// 벽돌 파괴 점수의 10%로 Bomb/Harvest 게이지 충전
/// Managers.Game.BrickGame.Charge 로 접근
/// </summary>
public class ComponentChargeManager
{
    #region 상수
    /// <summary>
    /// Bomb 컴포넌트 최대 게이지
    /// </summary>
    public const float BOMB_MAX_CHARGE = 100f;

    /// <summary>
    /// Harvest 컴포넌트 최대 게이지
    /// </summary>
    public const float HARVEST_MAX_CHARGE = 150f;

    /// <summary>
    /// 벽돌 파괴 점수 대비 충전 비율 (10%)
    /// </summary>
    public const float CHARGE_RATE = 0.10f;

    /// <summary>
    /// Bomb 컴포넌트 ID 문자열
    /// </summary>
    public const string BOMB_ID = "bomb";

    /// <summary>
    /// Harvest 컴포넌트 ID 문자열
    /// </summary>
    public const string HARVEST_ID = "harvest";
    #endregion

    #region 내부 상태 클래스
    /// <summary>
    /// 플레이어별 컴포넌트 게이지 상태
    /// </summary>
    private class ChargeState
    {
        public float BombCharge { get; set; }
        public float HarvestCharge { get; set; }

        public void Reset()
        {
            BombCharge = 0f;
            HarvestCharge = 0f;
        }
    }
    #endregion

    #region 상태
    /// <summary>
    /// 플레이어별 게이지 상태 (key: playerID)
    /// </summary>
    private readonly Dictionary<int, ChargeState> _playerCharges = new Dictionary<int, ChargeState>();
    #endregion

    #region 생성자
    public ComponentChargeManager()
    {
        GameLogger.SystemStart("ComponentChargeManager", "컴포넌트 게이지 매니저 생성됨");
    }
    #endregion

    #region Public Methods - 게이지 충전
    /// <summary>
    /// 벽돌 파괴 시 게이지 충전
    /// 점수의 10%를 Bomb/Harvest 양쪽 게이지에 충전
    /// </summary>
    /// <param name="scoreValue">파괴된 벽돌의 점수</param>
    /// <param name="playerID">충전 대상 플레이어 ID (싱글플레이어: 0)</param>
    public void OnBrickDestroyed(int scoreValue, int playerID)
    {
        if (scoreValue <= 0) return;

        var state = GetOrCreateState(playerID);
        float chargeAmount = scoreValue * CHARGE_RATE;

        // Bomb 게이지 충전
        float prevBomb = state.BombCharge;
        state.BombCharge = Mathf.Min(state.BombCharge + chargeAmount, BOMB_MAX_CHARGE);

        if (!Mathf.Approximately(prevBomb, state.BombCharge))
        {
            PublishChargeChanged(playerID, BOMB_ID, state.BombCharge, BOMB_MAX_CHARGE);
        }

        // Harvest 게이지 충전
        float prevHarvest = state.HarvestCharge;
        state.HarvestCharge = Mathf.Min(state.HarvestCharge + chargeAmount, HARVEST_MAX_CHARGE);

        if (!Mathf.Approximately(prevHarvest, state.HarvestCharge))
        {
            PublishChargeChanged(playerID, HARVEST_ID, state.HarvestCharge, HARVEST_MAX_CHARGE);
        }

        GameLogger.Info("ComponentChargeManager",
            $"게이지 충전: Player={playerID}, +{chargeAmount:F1} (점수 {scoreValue}의 {CHARGE_RATE * 100}%), " +
            $"Bomb={state.BombCharge:F1}/{BOMB_MAX_CHARGE}, Harvest={state.HarvestCharge:F1}/{HARVEST_MAX_CHARGE}");
    }
    #endregion

    #region Public Methods - 게이지 조회
    /// <summary>
    /// 컴포넌트 사용 가능 여부 (게이지 풀 충전 확인)
    /// </summary>
    /// <param name="playerID">플레이어 ID</param>
    /// <param name="componentID">"bomb" 또는 "harvest"</param>
    /// <returns>게이지가 최대치 이상이면 true</returns>
    public bool CanUseComponent(int playerID, string componentID)
    {
        if (!_playerCharges.TryGetValue(playerID, out var state))
            return false;

        switch (componentID)
        {
            case BOMB_ID:
                return state.BombCharge >= BOMB_MAX_CHARGE;
            case HARVEST_ID:
                return state.HarvestCharge >= HARVEST_MAX_CHARGE;
            default:
                GameLogger.Warning("ComponentChargeManager", $"알 수 없는 컴포넌트 ID: {componentID}");
                return false;
        }
    }

    /// <summary>
    /// 게이지 소비 (컴포넌트 사용 후 리셋)
    /// </summary>
    /// <param name="playerID">플레이어 ID</param>
    /// <param name="componentID">"bomb" 또는 "harvest"</param>
    public void ConsumeCharge(int playerID, string componentID)
    {
        if (!_playerCharges.TryGetValue(playerID, out var state))
        {
            GameLogger.Warning("ComponentChargeManager", $"게이지 소비 실패: Player={playerID} 상태 없음");
            return;
        }

        switch (componentID)
        {
            case BOMB_ID:
                state.BombCharge = 0f;
                PublishChargeChanged(playerID, BOMB_ID, 0f, BOMB_MAX_CHARGE);
                GameLogger.Info("ComponentChargeManager", $"Bomb 게이지 소비: Player={playerID}");
                break;
            case HARVEST_ID:
                state.HarvestCharge = 0f;
                PublishChargeChanged(playerID, HARVEST_ID, 0f, HARVEST_MAX_CHARGE);
                GameLogger.Info("ComponentChargeManager", $"Harvest 게이지 소비: Player={playerID}");
                break;
            default:
                GameLogger.Warning("ComponentChargeManager", $"알 수 없는 컴포넌트 ID: {componentID}");
                break;
        }
    }

    /// <summary>
    /// 게이지 비율 반환 (0.0 ~ 1.0)
    /// </summary>
    /// <param name="playerID">플레이어 ID</param>
    /// <param name="componentID">"bomb" 또는 "harvest"</param>
    /// <returns>현재 충전량 / 최대 충전량 (0.0 ~ 1.0)</returns>
    public float GetChargeRatio(int playerID, string componentID)
    {
        if (!_playerCharges.TryGetValue(playerID, out var state))
            return 0f;

        switch (componentID)
        {
            case BOMB_ID:
                return Mathf.Clamp01(state.BombCharge / BOMB_MAX_CHARGE);
            case HARVEST_ID:
                return Mathf.Clamp01(state.HarvestCharge / HARVEST_MAX_CHARGE);
            default:
                return 0f;
        }
    }

    /// <summary>
    /// 현재 게이지 충전량 반환 (원시 값)
    /// </summary>
    /// <param name="playerID">플레이어 ID</param>
    /// <param name="componentID">"bomb" 또는 "harvest"</param>
    /// <returns>현재 충전량</returns>
    public float GetCurrentCharge(int playerID, string componentID)
    {
        if (!_playerCharges.TryGetValue(playerID, out var state))
            return 0f;

        switch (componentID)
        {
            case BOMB_ID:
                return state.BombCharge;
            case HARVEST_ID:
                return state.HarvestCharge;
            default:
                return 0f;
        }
    }
    #endregion

    #region Public Methods - 상태 초기화
    /// <summary>
    /// 모든 플레이어의 게이지 상태 초기화
    /// </summary>
    public void Reset()
    {
        _playerCharges.Clear();
        GameLogger.Info("ComponentChargeManager", "모든 게이지 상태 초기화됨");
    }
    #endregion

    #region Private Methods
    /// <summary>
    /// 플레이어 상태 조회 또는 생성
    /// </summary>
    private ChargeState GetOrCreateState(int playerID)
    {
        if (!_playerCharges.TryGetValue(playerID, out var state))
        {
            state = new ChargeState();
            _playerCharges[playerID] = state;
            GameLogger.Info("ComponentChargeManager", $"새 플레이어 게이지 상태 생성: Player={playerID}");
        }
        return state;
    }

    /// <summary>
    /// ActionBus를 통해 게이지 변경 이벤트 발행
    /// </summary>
    private void PublishChargeChanged(int playerID, string componentID, float currentCharge, float maxCharge)
    {
        float ratio = maxCharge > 0f ? Mathf.Clamp01(currentCharge / maxCharge) : 0f;

        Managers.PublishAction(
            ActionId.BrickGame_ComponentChargeChanged,
            new ComponentChargePayload(playerID, componentID, currentCharge, maxCharge, ratio));
    }
    #endregion

    #region 디버그 정보
    /// <summary>
    /// 현재 상태를 문자열로 반환 (디버깅용)
    /// </summary>
    public override string ToString()
    {
        if (_playerCharges.Count == 0)
            return "[ComponentChargeManager] 플레이어 없음";

        var sb = new System.Text.StringBuilder("[ComponentChargeManager] ");
        foreach (var kvp in _playerCharges)
        {
            sb.Append($"Player{kvp.Key}(Bomb={kvp.Value.BombCharge:F1}/{BOMB_MAX_CHARGE}, " +
                      $"Harvest={kvp.Value.HarvestCharge:F1}/{HARVEST_MAX_CHARGE}) ");
        }
        return sb.ToString();
    }
    #endregion
}

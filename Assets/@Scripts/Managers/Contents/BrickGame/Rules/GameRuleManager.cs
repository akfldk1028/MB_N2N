using System;
using System.Collections.Generic;
using MB.Infrastructure.Messages;

/// <summary>
/// 게임 규칙 관리자
/// Managers.Game.Rules 로 접근
///
/// 역할:
/// - 활성 규칙 관리
/// - 규칙 등록/변경
/// - 입력 이벤트 → 활성 규칙 전달
/// </summary>
public class GameRuleManager
{
    #region Registered Rules
    private Dictionary<string, Func<IGameRule>> _ruleFactories = new Dictionary<string, Func<IGameRule>>();
    #endregion

    #region State
    private IGameRule _activeRule;
    private bool _initialized = false;
    private IDisposable _inputSubscription;

    public IGameRule ActiveRule => _activeRule;
    public bool IsInitialized => _initialized;
    public string ActiveRuleId => _activeRule?.RuleId ?? "none";
    #endregion

    #region Events
    /// <summary>
    /// 규칙 변경 시 발생
    /// </summary>
    public event Action<IGameRule> OnRuleChanged;

    /// <summary>
    /// 자원(총알) 개수 변경 시 발생
    /// </summary>
    public event Action<int> OnResourceCountChanged;

    /// <summary>
    /// 발사 시 발생
    /// </summary>
    public event Action<int> OnFired;
    #endregion

    #region 생성자
    public GameRuleManager()
    {
        // 기본 규칙 등록
        RegisterRule<CannonBulletRule>();

        GameLogger.SystemStart("GameRuleManager", "생성됨 (CannonBulletRule 등록됨)");
    }
    #endregion

    #region 초기화
    /// <summary>
    /// 초기화 (GameScene 진입 시)
    /// </summary>
    public void Initialize(string defaultRuleId = "cannon_bullet")
    {
        if (_initialized)
        {
            GameLogger.Warning("GameRuleManager", "이미 초기화됨");
            return;
        }

        // 입력 이벤트 구독
        SubscribeToInputEvents();

        // 기본 규칙 활성화
        SetActiveRule(defaultRuleId);

        _initialized = true;
        GameLogger.Success("GameRuleManager", $"초기화 완료 (활성 규칙: {ActiveRuleId})");
    }

    /// <summary>
    /// 정리 (GameScene 종료 시)
    /// </summary>
    public void Cleanup()
    {
        UnsubscribeFromInputEvents();

        _activeRule?.Cleanup();
        _activeRule = null;

        _initialized = false;
        GameLogger.Info("GameRuleManager", "정리 완료");
    }
    #endregion

    #region 규칙 등록
    /// <summary>
    /// 규칙 팩토리 등록
    /// </summary>
    public void RegisterRule<T>() where T : IGameRule, new()
    {
        var sample = new T();
        string ruleId = sample.RuleId;

        if (_ruleFactories.ContainsKey(ruleId))
        {
            GameLogger.Warning("GameRuleManager", $"규칙 이미 등록됨: {ruleId}");
            return;
        }

        _ruleFactories[ruleId] = () => new T();
        GameLogger.Info("GameRuleManager", $"규칙 등록: {ruleId} ({sample.DisplayName})");
    }

    /// <summary>
    /// 등록된 규칙 ID 목록
    /// </summary>
    public IEnumerable<string> GetRegisteredRuleIds()
    {
        return _ruleFactories.Keys;
    }
    #endregion

    #region 규칙 변경
    /// <summary>
    /// 활성 규칙 설정
    /// </summary>
    public bool SetActiveRule(string ruleId)
    {
        if (!_ruleFactories.TryGetValue(ruleId, out var factory))
        {
            GameLogger.Error("GameRuleManager", $"등록되지 않은 규칙: {ruleId}");
            return false;
        }

        // 기존 규칙 정리
        if (_activeRule != null)
        {
            UnsubscribeFromRuleEvents(_activeRule);
            _activeRule.Cleanup();
        }

        // 새 규칙 생성 및 초기화
        _activeRule = factory();
        SubscribeToRuleEvents(_activeRule);
        _activeRule.Initialize();

        OnRuleChanged?.Invoke(_activeRule);

        GameLogger.Success("GameRuleManager", $"규칙 변경: {ruleId} ({_activeRule.DisplayName})");
        return true;
    }
    #endregion

    #region 이벤트 구독
    private void SubscribeToInputEvents()
    {
        _inputSubscription = Managers.Subscribe(ActionId.Input_Fire, OnFireInput);
    }

    private void UnsubscribeFromInputEvents()
    {
        _inputSubscription?.Dispose();
        _inputSubscription = null;
    }

    private void SubscribeToRuleEvents(IGameRule rule)
    {
        rule.OnResourceCountChanged += HandleResourceCountChanged;
        rule.OnFired += HandleFired;
    }

    private void UnsubscribeFromRuleEvents(IGameRule rule)
    {
        rule.OnResourceCountChanged -= HandleResourceCountChanged;
        rule.OnFired -= HandleFired;
    }
    #endregion

    #region 이벤트 핸들러
    private void OnFireInput(ActionMessage message)
    {
        _activeRule?.OnInput(GameInputAction.Fire);
    }

    private void HandleResourceCountChanged(int count)
    {
        OnResourceCountChanged?.Invoke(count);
    }

    private void HandleFired(int count)
    {
        OnFired?.Invoke(count);
    }
    #endregion

    #region Public API
    /// <summary>
    /// 현재 자원(총알) 개수
    /// </summary>
    public int GetResourceCount()
    {
        return _activeRule?.GetResourceCount() ?? 0;
    }

    /// <summary>
    /// 발사 가능 여부
    /// </summary>
    public bool CanFire()
    {
        return _activeRule?.CanFire() ?? false;
    }

    /// <summary>
    /// 규칙 리셋
    /// </summary>
    public void Reset()
    {
        _activeRule?.Reset();
    }
    #endregion
}

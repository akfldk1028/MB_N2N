using System;

/// <summary>
/// 게임 규칙 기본 추상 클래스
/// 공통 로직과 기본 구현 제공
/// </summary>
public abstract class BaseGameRule : IGameRule
{
    #region 식별자
    public abstract string RuleId { get; }
    public abstract string DisplayName { get; }
    #endregion

    #region State
    protected int _resourceCount = 0;
    protected bool _initialized = false;

    public int ResourceCount => _resourceCount;
    #endregion

    #region Events
    public event Action<int> OnResourceCountChanged;
    public event Action<int> OnFired;
    #endregion

    #region 라이프사이클
    public virtual void Initialize()
    {
        if (_initialized)
        {
            GameLogger.Warning(GetType().Name, "이미 초기화됨");
            return;
        }

        _resourceCount = 0;
        _initialized = true;

        GameLogger.Success(GetType().Name, $"규칙 초기화 완료: {DisplayName}");
    }

    public virtual void Cleanup()
    {
        OnResourceCountChanged = null;
        OnFired = null;
        _initialized = false;

        GameLogger.Info(GetType().Name, "정리 완료");
    }

    public virtual void Reset()
    {
        _resourceCount = 0;
        NotifyResourceChanged();

        GameLogger.Info(GetType().Name, "리셋됨");
    }
    #endregion

    #region 점수/자원 관리
    public virtual int GetResourceCount() => _resourceCount;

    public virtual bool CanFire() => _resourceCount > 0;

    public abstract void OnScoreChanged(int oldScore, int newScore);
    #endregion

    #region 입력 처리
    public abstract void OnInput(GameInputAction action);
    #endregion

    #region Protected Helpers
    /// <summary>
    /// 자원 추가
    /// </summary>
    protected void AddResource(int amount)
    {
        if (amount <= 0) return;

        _resourceCount += amount;
        NotifyResourceChanged();

        GameLogger.DevLog(GetType().Name, $"자원 추가: +{amount}, 현재: {_resourceCount}");
    }

    /// <summary>
    /// 자원 소비
    /// </summary>
    protected bool ConsumeResource(int amount)
    {
        if (amount <= 0 || _resourceCount < amount) return false;

        _resourceCount -= amount;
        NotifyResourceChanged();

        GameLogger.DevLog(GetType().Name, $"자원 소비: -{amount}, 남은: {_resourceCount}");
        return true;
    }

    /// <summary>
    /// 발사 이벤트 발생
    /// </summary>
    protected void NotifyFired(int count)
    {
        OnFired?.Invoke(count);
    }

    /// <summary>
    /// 자원 변경 이벤트 발생
    /// </summary>
    protected void NotifyResourceChanged()
    {
        OnResourceCountChanged?.Invoke(_resourceCount);
    }
    #endregion
}

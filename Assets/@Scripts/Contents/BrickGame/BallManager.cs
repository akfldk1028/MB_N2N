using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Assets.Scripts.Objects;

/// <summary>
/// 벽돌깨기 게임의 공(Ball) 관리 매니저
/// - 공 생명주기 관리 (등록/해제)
/// - 공격력 시스템 관리
/// - 활성 공 목록 관리
/// - FindObjectsOfType 대체
/// </summary>
public class BallManager
{
    #region 공 목록 관리
    private readonly List<PhysicsBall> _activeBalls = new List<PhysicsBall>();
    private readonly HashSet<PhysicsBall> _ballSet = new HashSet<PhysicsBall>(); // 중복 방지용
    
    /// <summary>
    /// 현재 활성화된 공의 개수
    /// </summary>
    public int ActiveBallCount => _activeBalls.Count;
    
    /// <summary>
    /// 활성 공 읽기 전용 리스트
    /// </summary>
    public IReadOnlyList<PhysicsBall> ActiveBalls => _activeBalls.AsReadOnly();
    #endregion
    
    #region 공격력 시스템
    private int _currentPower = 1; // 기본 공격력
    private float _powerTimer = 0f; // 파워업 남은 시간
    
    /// <summary>
    /// 현재 공격력 (모든 공 공유)
    /// </summary>
    public int CurrentPower => _currentPower;
    
    /// <summary>
    /// 파워업 남은 시간
    /// </summary>
    public float PowerTimer => _powerTimer;
    #endregion
    
    #region 이벤트
    /// <summary>
    /// 공이 등록될 때 발생
    /// </summary>
    public event Action<PhysicsBall> OnBallRegistered;
    
    /// <summary>
    /// 공이 해제될 때 발생
    /// </summary>
    public event Action<PhysicsBall> OnBallUnregistered;
    
    /// <summary>
    /// 공격력이 변경될 때 발생
    /// </summary>
    public event Action<int> OnPowerChanged;
    
    /// <summary>
    /// 모든 공이 바닥에 떨어졌을 때 발생
    /// </summary>
    public event Action OnAllBallsReturned;
    #endregion
    
    #region 초기화
    public BallManager()
    {
    }
    
    /// <summary>
    /// 매니저 초기화 (게임 시작 시)
    /// </summary>
    public void Initialize()
    {
        ClearAllBalls();
        ResetPower();
    }
    #endregion
    
    #region 공 등록/해제
    /// <summary>
    /// 공 등록 (PhysicsBall이 OnEnable에서 호출)
    /// </summary>
    public void RegisterBall(PhysicsBall ball)
    {
        if (ball == null)
        {
            GameLogger.Warning("BallManager", "null 공 등록 시도");
            return;
        }
        
        if (_ballSet.Contains(ball))
        {
            GameLogger.DevLog("BallManager", $"공 '{ball.name}' 이미 등록됨 (무시)");
            return;
        }
        
        _activeBalls.Add(ball);
        _ballSet.Add(ball);
        
        OnBallRegistered?.Invoke(ball);
        GameLogger.Info("BallManager", $"공 등록: {ball.name} (총 {ActiveBallCount}개)");
    }
    
    /// <summary>
    /// 공 해제 (PhysicsBall이 OnDisable에서 호출)
    /// </summary>
    public void UnregisterBall(PhysicsBall ball)
    {
        if (ball == null) return;
        
        if (!_ballSet.Contains(ball))
        {
            return; // 이미 해제됨
        }
        
        _activeBalls.Remove(ball);
        _ballSet.Remove(ball);
        
        OnBallUnregistered?.Invoke(ball);
        GameLogger.Info("BallManager", $"공 해제: {ball.name} (남은 공: {ActiveBallCount}개)");
        
        // 모든 공이 사라지면 이벤트 발생
        if (ActiveBallCount == 0)
        {
            OnAllBallsReturned?.Invoke();
            GameLogger.Progress("BallManager", "모든 공이 바닥에 떨어짐");
        }
    }
    
    /// <summary>
    /// 모든 공 목록 초기화
    /// </summary>
    public void ClearAllBalls()
    {
        _activeBalls.Clear();
        _ballSet.Clear();
        GameLogger.Info("BallManager", "모든 공 목록 초기화");
    }
    #endregion
    
    #region 공격력 관리
    /// <summary>
    /// 공격력 증가 (Star 아이템 획득 시)
    /// </summary>
    public void IncreasePower(int amount, float duration)
    {
        if (amount <= 0)
        {
            GameLogger.Warning("BallManager", $"잘못된 공격력 증가량: {amount}");
            return;
        }
        
        _currentPower += amount;
        _powerTimer = Mathf.Max(_powerTimer, duration); // 더 긴 지속시간 적용
        
        OnPowerChanged?.Invoke(_currentPower);
        GameLogger.Success("BallManager", $"공격력 증가: {_currentPower} (지속시간: {_powerTimer:F1}초)");
    }
    
    /// <summary>
    /// 공격력 리셋 (기본값으로)
    /// </summary>
    public void ResetPower()
    {
        _currentPower = 1;
        _powerTimer = 0f;
        
        OnPowerChanged?.Invoke(_currentPower);
        GameLogger.Info("BallManager", "공격력 초기화됨");
    }
    
    /// <summary>
    /// 매 프레임 호출 - 파워업 타이머 업데이트
    /// </summary>
    public void UpdatePowerTimer(float deltaTime)
    {
        if (_powerTimer > 0)
        {
            _powerTimer -= deltaTime;
            
            if (_powerTimer <= 0)
            {
                _powerTimer = 0;
                _currentPower = 1; // 기본값으로 리셋
                
                OnPowerChanged?.Invoke(_currentPower);
                GameLogger.Info("BallManager", "파워업 효과 종료");
            }
        }
    }
    #endregion
    
    #region 유틸리티
    /// <summary>
    /// Moving 또는 Launching 상태의 공 개수 조회
    /// </summary>
    public int GetMovingBallCount()
    {
        int count = 0;
        foreach (var ball in _activeBalls)
        {
            if (ball.CurrentState == EBallState.Moving || ball.CurrentState == EBallState.Launching)
            {
                count++;
            }
        }
        return count;
    }
    
    /// <summary>
    /// 특정 공이 마지막 활성 공인지 확인
    /// </summary>
    public bool IsLastMovingBall(PhysicsBall ball)
    {
        return GetMovingBallCount() <= 1;
    }
    
    /// <summary>
    /// 디버그 정보
    /// </summary>
    public override string ToString()
    {
        return $"[BallManager] Active: {ActiveBallCount}, Power: {_currentPower}, Timer: {_powerTimer:F1}s";
    }
    #endregion
}


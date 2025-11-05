using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Assets.Scripts.Objects;

/// <summary>
/// 벽돌깨기 게임의 벽돌(Brick) 관리 매니저
/// - 벽돌 생명주기 관리 (등록/해제)
/// - 활성 벽돌 목록 관리
/// - 벽돌 파괴 통계 추적
/// - FindObjectsOfType 대체
/// </summary>
public class BrickManager
{
    #region 벽돌 목록 관리
    private readonly List<Brick> _activeBricks = new List<Brick>();
    private readonly HashSet<Brick> _brickSet = new HashSet<Brick>(); // 중복 방지용
    
    /// <summary>
    /// 현재 활성화된 벽돌의 개수
    /// </summary>
    public int ActiveBrickCount => _activeBricks.Count;
    
    /// <summary>
    /// 활성 벽돌 읽기 전용 리스트
    /// </summary>
    public IReadOnlyList<Brick> ActiveBricks => _activeBricks.AsReadOnly();
    #endregion
    
    #region 벽돌 통계
    private int _totalBricksDestroyed = 0; // 현재 게임 세션에서 파괴된 벽돌 수
    
    /// <summary>
    /// 현재 게임 세션에서 파괴된 총 벽돌 수
    /// </summary>
    public int TotalBricksDestroyed => _totalBricksDestroyed;
    #endregion
    
    #region 이벤트
    /// <summary>
    /// 벽돌이 등록될 때 발생
    /// </summary>
    public event Action<Brick> OnBrickRegistered;
    
    /// <summary>
    /// 벽돌이 파괴될 때 발생 (점수 정보 포함)
    /// </summary>
    public event Action<Brick, int> OnBrickDestroyed;
    
    /// <summary>
    /// 벽돌이 해제될 때 발생
    /// </summary>
    public event Action<Brick> OnBrickUnregistered;
    
    /// <summary>
    /// 모든 벽돌이 파괴되었을 때 발생 (스테이지 클리어)
    /// </summary>
    public event Action OnAllBricksDestroyed;
    #endregion
    
    #region 초기화
    public BrickManager()
    {
        GameLogger.Progress("BrickManager", "BrickManager 생성됨");
    }
    
    /// <summary>
    /// 매니저 초기화 (게임 시작 시)
    /// </summary>
    public void Initialize()
    {
        ClearAllBricks();
        _totalBricksDestroyed = 0;
        GameLogger.Success("BrickManager", "BrickManager 초기화 완료");
    }
    #endregion
    
    #region 벽돌 등록/해제
    /// <summary>
    /// 벽돌 등록 (Brick이 Start에서 호출)
    /// </summary>
    public void RegisterBrick(Brick brick)
    {
        if (brick == null)
        {
            GameLogger.Warning("BrickManager", "null 벽돌 등록 시도");
            return;
        }
        
        if (_brickSet.Contains(brick))
        {
            GameLogger.DevLog("BrickManager", $"벽돌 '{brick.name}' 이미 등록됨 (무시)");
            return;
        }
        
        _activeBricks.Add(brick);
        _brickSet.Add(brick);
        
        OnBrickRegistered?.Invoke(brick);
        GameLogger.Info("BrickManager", $"벽돌 등록: {brick.name} (총 {ActiveBrickCount}개)");
    }
    
    /// <summary>
    /// 벽돌 파괴 처리 (Brick이 파괴될 때 호출)
    /// </summary>
    public void NotifyBrickDestroyed(Brick brick, int scoreValue)
    {
        if (brick == null) return;
        
        if (!_brickSet.Contains(brick))
        {
            return; // 이미 해제됨
        }
        
        _activeBricks.Remove(brick);
        _brickSet.Remove(brick);
        _totalBricksDestroyed++;
        
        OnBrickDestroyed?.Invoke(brick, scoreValue);
        GameLogger.Info("BrickManager", $"벽돌 파괴: {brick.name}, 점수: {scoreValue} (남은 벽돌: {ActiveBrickCount}개)");
        
        // 모든 벽돌이 파괴되면 이벤트 발생
        if (ActiveBrickCount == 0)
        {
            OnAllBricksDestroyed?.Invoke();
            GameLogger.Success("BrickManager", "모든 벽돌 파괴! 스테이지 클리어!");
        }
    }
    
    /// <summary>
    /// 벽돌 해제 (Brick이 OnDestroy에서 호출)
    /// </summary>
    public void UnregisterBrick(Brick brick)
    {
        if (brick == null) return;
        
        if (!_brickSet.Contains(brick))
        {
            return; // 이미 해제됨
        }
        
        _activeBricks.Remove(brick);
        _brickSet.Remove(brick);
        
        OnBrickUnregistered?.Invoke(brick);
        GameLogger.DevLog("BrickManager", $"벽돌 해제: {brick.name} (남은 벽돌: {ActiveBrickCount}개)");
    }
    
    /// <summary>
    /// 모든 벽돌 목록 초기화
    /// </summary>
    public void ClearAllBricks()
    {
        _activeBricks.Clear();
        _brickSet.Clear();
        GameLogger.Info("BrickManager", "모든 벽돌 목록 초기화");
    }
    #endregion
    
    #region 유틸리티
    /// <summary>
    /// 특정 행(Row)의 벽돌 개수 조회 (추후 확장)
    /// </summary>
    public int GetBrickCountInRow(int row)
    {
        // TODO: 벽돌에 row 정보 추가 시 구현
        return 0;
    }
    
    /// <summary>
    /// 가장 낮은 위치의 벽돌 Y 좌표 조회
    /// </summary>
    public float GetLowestBrickY()
    {
        if (ActiveBrickCount == 0)
        {
            return float.MaxValue;
        }
        
        float lowestY = float.MaxValue;
        foreach (var brick in _activeBricks)
        {
            if (brick != null && brick.transform.position.y < lowestY)
            {
                lowestY = brick.transform.position.y;
            }
        }
        return lowestY;
    }
    
    /// <summary>
    /// 디버그 정보
    /// </summary>
    public override string ToString()
    {
        return $"[BrickManager] Active: {ActiveBrickCount}, Destroyed: {_totalBricksDestroyed}";
    }
    #endregion
}


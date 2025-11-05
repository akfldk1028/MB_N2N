using System;
using UnityEngine;

/// <summary>
/// 벽돌깨기 게임 상태 관리 클래스
/// 게임 진행 중 변하는 모든 상태를 추적
/// </summary>
public class BrickGameState
{
    #region 게임 진행 상태
    /// <summary>
    /// 현재 게임 단계 (Idle, Playing, Paused, GameOver, StageClear)
    /// </summary>
    public GamePhase CurrentPhase { get; set; } = GamePhase.Idle;
    
    /// <summary>
    /// 게임 활성화 여부 (일시정지/게임오버 시 false)
    /// </summary>
    public bool IsGameActive => CurrentPhase == GamePhase.Playing;
    
    /// <summary>
    /// 현재 레벨 (CommonVars.level 대체)
    /// </summary>
    public int CurrentLevel { get; set; } = 1;
    
    /// <summary>
    /// 현재 점수
    /// </summary>
    public int CurrentScore { get; private set; }
    
    /// <summary>
    /// 생성된 행 수
    /// </summary>
    public int RowsSpawned { get; private set; }
    
    /// <summary>
    /// 다음 스폰 예정 시간
    /// </summary>
    public float NextSpawnTime { get; set; }
    
    /// <summary>
    /// 현재 스폰 간격
    /// </summary>
    public float CurrentSpawnInterval { get; set; }
    
    /// <summary>
    /// 새 벽돌 웨이브 플래그 (CommonVars.newWaveOfBricks 대체)
    /// </summary>
    public bool NewWaveOfBricks { get; set; }
    #endregion
    
    #region 점수 관련 메서드
    /// <summary>
    /// 점수 추가
    /// </summary>
    public void AddScore(int amount)
    {
        if (amount <= 0) return;
        
        CurrentScore += amount;
        GameLogger.Info("BrickGameState", $"점수 추가: +{amount}, 현재 점수: {CurrentScore}");
    }
    
    /// <summary>
    /// 점수 초기화
    /// </summary>
    public void ResetScore()
    {
        CurrentScore = 0;
    }
    #endregion
    
    #region 행 생성 관련 메서드
    /// <summary>
    /// 행 생성 카운터 증가
    /// </summary>
    public void IncrementRowsSpawned()
    {
        RowsSpawned++;
    }
    
    /// <summary>
    /// 행 카운터 초기화
    /// </summary>
    public void ResetRowsSpawned()
    {
        RowsSpawned = 0;
    }
    #endregion
    
    #region 상태 초기화
    /// <summary>
    /// 모든 상태를 초기값으로 리셋
    /// </summary>
    public void Reset()
    {
        CurrentPhase = GamePhase.Idle;
        CurrentLevel = 1;
        CurrentScore = 0;
        RowsSpawned = 0;
        NextSpawnTime = 0f;
        CurrentSpawnInterval = 0f;
        NewWaveOfBricks = false;
        
        GameLogger.Info("BrickGameState", "게임 상태 초기화됨");
    }
    #endregion
    
    #region 디버그 정보
    /// <summary>
    /// 현재 상태를 문자열로 반환 (디버깅용)
    /// </summary>
    public override string ToString()
    {
        return $"[BrickGameState] Active: {IsGameActive}, Score: {CurrentScore}, Rows: {RowsSpawned}, NextSpawn: {NextSpawnTime:F2}s";
    }
    #endregion
}


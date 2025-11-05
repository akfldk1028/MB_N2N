using System;
using UnityEngine;

/// <summary>
/// 벽돌깨기 게임 설정값
/// Inspector에서 설정 가능하도록 Serializable
/// </summary>
[System.Serializable]
public class BrickGameSettings
{
    [Header("게임 설정")]
    [Tooltip("게임 시작 후 첫 행이 생성되기까지의 초기 딜레이")]
    public float initialSpawnDelay = 2f;
    
    [Tooltip("새 행이 생성되는 기본 간격 (초)")]
    public float spawnInterval = 5f;
    
    [Tooltip("스폰 간격 감소율 (매 행마다 곱해짐)")]
    [Range(0.5f, 2f)]
    public float spawnIntervalDecreaseRate = 0.95f;
    
    [Tooltip("스폰 간격의 최소값 (더 이상 짧아지지 않음)")]
    public float minSpawnInterval = 1.5f;
    
    [Header("레벨 설정")]
    [Tooltip("게임 최대 레벨")]
    public int maxLevel = 50;
    
    [Tooltip("게임 시작 시 초기 레벨")]
    public int initialLevel = 1;
    
    [Header("초기 생성 설정")]
    [Tooltip("게임 시작 시 생성할 초기 행 수")]
    public int initialRowCount = 3;
    
    /// <summary>
    /// 기본 설정값 생성
    /// </summary>
    public static BrickGameSettings CreateDefault()
    {
        return new BrickGameSettings
        {
            initialSpawnDelay = 2f,
            spawnInterval = 5f,
            spawnIntervalDecreaseRate = 0.95f,
            minSpawnInterval = 1.5f,
            maxLevel = 50,
            initialLevel = 1,
            initialRowCount = 3
        };
    }
}


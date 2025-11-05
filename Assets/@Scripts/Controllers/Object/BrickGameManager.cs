using UnityEngine;
using System;
using TMPro;

public class BrickGameManager : MonoBehaviour
{
    [Header("게임 설정")]
    [SerializeField] private float initialSpawnDelay = 2f;
    [SerializeField] private float spawnInterval = 5f;
    [SerializeField] [Range(0.5f, 2f)] private float spawnIntervalDecreaseRate = 0.95f;
    [SerializeField] private float minSpawnInterval = 1.5f;
    
    [Header("레벨 설정")]
    [SerializeField] private int maxLevel = 50; // 최대 레벨
    [SerializeField] private int initialLevel = 1; // 초기 레벨
    
    [Header("점수 설정")]
    [SerializeField] public TextMeshPro scoreText; // 점수 표시 텍스트
    
    [Header("참조")]
    [SerializeField] private ObjectPlacement objectPlacer;
    
    // 게임 상태 변수
    private float nextSpawnTime = 0f;
    private float currentSpawnInterval;
    private bool isGameActive = false;
    private int rowsSpawned = 0; // 생성된 행 수 추적
    
    // 점수 관련 변수
    private int currentScore = 0;
    
    // 이벤트 정의
    public event Action OnGameStart;
    public event Action OnGamePause;
    public event Action OnGameResume;
    public event Action OnGameOver;
    public event Action OnRowSpawn;
    public event Action<int> OnLevelUp; // 레벨업 이벤트 추가
    public event Action<int> OnScoreChanged; // 점수 변경 이벤트 추가
    
    private void Awake()
    {
        // ObjectPlacer 자동 참조
        if (objectPlacer == null)
        {
            objectPlacer = FindObjectOfType<ObjectPlacement>();
        }
        
        if (objectPlacer == null)
        {
            Debug.LogError("ObjectPlacement 컴포넌트를 찾을 수 없습니다!");
        }
        
        // Score 텍스트 자동 참조 (없으면)
        // if (scoreText == null)
        // {
        //     scoreText = GameObject.Find("ScoreText")?.GetComponent<TextMeshProUGUI>();
        //     if (scoreText == null)
        //     {
        //         Debug.LogWarning("ScoreText를 찾을 수 없습니다. 점수 표시가 업데이트되지 않을 수 있습니다.");
        //     }
        // }
    }
    
    private void Start()
    {
        // 게임 자동 시작 (필요에 따라 제거 가능)
        StartGame();
    }
    
    private void Update()
    {
        if (!isGameActive) return;
        
        // 시간 체크하여 새 행 생성 여부 결정
        if (Time.time >= nextSpawnTime)
        {
            SpawnNewRow();
            
            // 다음 스폰 시간 설정 및 난이도 조정
            AdjustDifficulty();
        }
    }
    
    private void SpawnNewRow()
    {
        if (objectPlacer != null)
        {
            // numberOfRowsToSpawn을 1로 설정하여 한 줄씩만 소환
            objectPlacer.PlaceMultipleRows(1);
            
            // 행 생성 카운터 증가
            rowsSpawned++;
            
            // 레벨업 처리 - 한 줄 생성할 때마다 레벨 증가
            IncreaseLevel();
            
            // 이벤트 발생
            OnRowSpawn?.Invoke();
            
            // 새 블록 웨이브 생성 표시
            CommonVars.newWaveOfBricks = true;
        }
    }
    
    private void IncreaseLevel()
    {
        // 최대 레벨 체크
        if (CommonVars.level < maxLevel)
        {
            // 레벨 증가
            CommonVars.level++;
            
            // 레벨에 따른 난이도 조정
            AdjustDifficultyByLevel();
            
            // 이벤트 발생
            OnLevelUp?.Invoke(CommonVars.level);
            
            Debug.Log($"레벨 업! 현재 레벨: {CommonVars.level}, 생성된 행 수: {rowsSpawned}");
        }
    }
    
    private void AdjustDifficultyByLevel()
    {
        // 레벨에 따른 난이도 증가 로직
        // 예: 스폰 간격 추가 감소, 블록 이동 속도 증가 등
        
        // 레벨이 올라갈수록 스폰 간격 추가 감소
        float levelFactor = 1f - (0.05f * (CommonVars.level - 1)); // 레벨당 5% 추가 감소
        currentSpawnInterval *= levelFactor;
        currentSpawnInterval = Mathf.Max(currentSpawnInterval, minSpawnInterval);
        
        // 다음 스폰 시간 재설정 (즉시 반영)
        nextSpawnTime = Time.time + currentSpawnInterval;
    }
    
    private void AdjustDifficulty()
    {
        // 난이도 증가 (간격 감소)
        currentSpawnInterval *= spawnIntervalDecreaseRate;
        currentSpawnInterval = Mathf.Max(currentSpawnInterval, minSpawnInterval);
        
        // 다음 스폰 시간 설정
        nextSpawnTime = Time.time + currentSpawnInterval;
    }
    
    // 게임 시작 메서드
    public void StartGame()
    {
        if (objectPlacer == null) return;
        
        isGameActive = true;
        currentSpawnInterval = spawnInterval;
        nextSpawnTime = Time.time + initialSpawnDelay;
        
        // 행 카운터 초기화
        rowsSpawned = 0;
        
        // 점수 초기화
        currentScore = 0;
        UpdateScoreText();
        
        // CommonVars 변수 초기화
        CommonVars.RestartAllVariables();
        
        // 레벨 초기화 - 초기 레벨 설정
        CommonVars.level = initialLevel;
        
        // 초기 행 생성 - 명시적으로 3줄 생성
        objectPlacer.PlaceMultipleRows(3);
        
        // 이벤트 발생
        OnGameStart?.Invoke();
    }
    
    public void PauseGame()
    {
        isGameActive = false;
        OnGamePause?.Invoke();
    }
    
    public void ResumeGame()
    {
        isGameActive = true;
        OnGameResume?.Invoke();
    }
    
    public void GameOver()
    {
        isGameActive = false;
        OnGameOver?.Invoke();
    }
    
    // 벽돌이 파괴될 때 호출되는 메서드
    public void AddScore(int waveValue)
    {
        // wave 값만큼 점수 추가
        currentScore += waveValue;
        
        // 점수 텍스트 업데이트
        UpdateScoreText();
        
        // 이벤트 발생
        OnScoreChanged?.Invoke(currentScore);
        
        Debug.Log($"점수 추가: +{waveValue}, 현재 점수: {currentScore}");
    }
    
    // 점수 텍스트 업데이트
    private void UpdateScoreText()
    {
        if (scoreText != null)
        {
            scoreText.text = currentScore.ToString();
        }
    }
    
    // 현재 게임 상태 반환 (필요한 경우)
    public bool IsGameActive()
    {
        return isGameActive;
    }
    
    // 현재 난이도 정보 반환 (UI 표시 등에 사용 가능)
    public float GetCurrentSpawnInterval()
    {
        return currentSpawnInterval;
    }
    
    // 현재 레벨 반환
    public int GetCurrentLevel()
    {
        return CommonVars.level;
    }
    
    // 현재 점수 반환
    public int GetCurrentScore()
    {
        return currentScore;
    }
    
    // 생성된 총 행 수 반환
    public int GetRowsSpawned()
    {
        return rowsSpawned;
    }
}
using UnityEngine;
using System;
using System.Collections;
using TMPro;
using System.Collections.Generic;

public class ReleaseGameManager : MonoBehaviour
{
    // --- 총알 발사 관련 변수 추가 ---
    [Header("발사 설정")]
    [SerializeField] private GameObject bulletPrefab; // 총알 프리팹
    [SerializeField] private float bulletSpeed = 20f; // 총알 속도
    [SerializeField] private float bulletFireInterval = 0.1f; // 총알 간 발사 간격
    [SerializeField] private float bulletSpreadAngle = 5f; // 총알 퍼짐 각도
    // -------------------------------

    private bool isGameActive = false;
    private BrickGameManager _brickGameManager; // BrickGameManager 참조
    private int _latestScore = 0; // 최신 점수 저장
    
    // --- Cannon 참조 변경 ---
    private Cannon[] _cannons; // 캐논 배열 (IsometricGridGenerator에서 가져옴)
    // ----------------------
    
    // 이벤트 정의
    public event Action OnGameStart;
    public event Action OnGamePause;
    public event Action OnGameResume;
    public event Action OnGameOver;
    public event Action OnRowSpawn;
    public event Action OnSummonButtonClicked;
    
    private void Awake()
    {
       // 초기화 로직
    }
    
    private void Start()
    {
        isGameActive = true;
        OnGameStart?.Invoke();

        // --- BrickGameManager 찾기 ---
        // _brickGameManager = FindObjectOfType<BrickGameManager>();
        // if (_brickGameManager != null)
        // {
        //     _latestScore = _brickGameManager.GetCurrentScore();
        //     _brickGameManager.OnScoreChanged += HandleScoreChange;
        //     Debug.Log($"<color=green>[ReleaseGameManager] Initial Score: {_latestScore}</color> ");
        // }
        // else
        // {
        //      Debug.LogError($"<color=red>[ReleaseGameManager] BrickGameManager를 찾을 수 없습니다!</color> ");
        // }
        
        // --- IsometricGridGenerator에서 캐논 배열 가져오기 ---
        if (IsometricGridGenerator.Instance != null)
        {
            _cannons = IsometricGridGenerator.Instance.GetAllCannons();
            Debug.Log($"<color=cyan>[ReleaseGameManager] IsometricGridGenerator에서 {(_cannons != null ? _cannons.Length : 0)}개의 캐논을 가져왔습니다.</color>");
        }
        else
        {
            // 그리드 생성기를 찾을 수 없는 경우 대체 방법으로 FindObjectsOfType 사용
            _cannons = FindObjectsOfType<Cannon>();
            Debug.LogWarning($"<color=yellow>[ReleaseGameManager] IsometricGridGenerator를 찾을 수 없어 직접 캐논을 검색: {(_cannons != null ? _cannons.Length : 0)}개 발견</color>");
        }
        
        // --- UI 버튼 이벤트 구독 ---
        // UI_BasicGameScene.OnSummonButtonUIClicked += HandleSummonButtonClickFromUI;
        Debug.Log("<color=cyan>[ReleaseGameManager] 소환 버튼 클릭 이벤트 구독 완료</color>");
        // -------------------------
    }
    
    private void OnDestroy()
    {
        // --- 이벤트 구독 해지 ---
        if (_brickGameManager != null)
        {
            _brickGameManager.OnScoreChanged -= HandleScoreChange;
            Debug.Log("[ReleaseGameManager] Unsubscribed from OnScoreChanged event.");
        }
        
        // UI_BasicGameScene.OnSummonButtonUIClicked -= HandleSummonButtonClickFromUI;
        // -----------------------
    }
    
    private void Update()
    {
        if (!isGameActive) return;
    }
    
    public void StartGame()
    {
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
    
    // --- 버튼 클릭 이벤트 처리 (직접 총알 발사 로직) ---
    private void HandleSummonButtonClickFromUI()
    {
        Debug.Log($"<color=cyan>[ReleaseGameManager] 소환 버튼 클릭됨! 현재 점수: {_latestScore}</color>");
        
        // 총알 발사 시작
        StartFireBullets();
    }
    
    // 총알 발사 시작
    private void StartFireBullets()
    {
        // 최소 20발은 발사하도록 설정
        int bulletCount =  _latestScore;
        
        // 각 캐논마다 남은 총알 수 설정
        Dictionary<Cannon, int> cannonAmmo = new Dictionary<Cannon, int>();
        
        if (_cannons == null || _cannons.Length == 0)
        {
            Debug.LogWarning("<color=yellow>[ReleaseGameManager] 발사할 캐논이 없습니다!</color>");
            
            // 캐논이 없는 경우 다시 한번 IsometricGridGenerator에서 가져오기 시도
            if (IsometricGridGenerator.Instance != null)
            {
                _cannons = IsometricGridGenerator.Instance.GetAllCannons();
                Debug.Log($"<color=cyan>[ReleaseGameManager] 캐논 재검색: {(_cannons != null ? _cannons.Length : 0)}개</color>");
            }
            
            if (_cannons == null || _cannons.Length == 0)
                return;
        }
        
        // 각 캐논에 동일한 수의 총알 할당
        int bulletsPerCannon = bulletCount;
        int remainingBullets = bulletCount % _cannons.Length;
        
        for (int i = 0; i < _cannons.Length; i++)
        {
            // 각 캐논에 총알 수 할당 (남은 것은 앞쪽 캐논에 추가)
            cannonAmmo[_cannons[i]] = bulletsPerCannon + (i < remainingBullets ? 1 : 0);
        }
        
        // 캐논별 총알 발사 코루틴 시작
        foreach (var cannon in _cannons)
        {
            if (cannonAmmo[cannon] > 0)
            {
                StartCoroutine(FireBulletsFromCannonCoroutine(cannon, cannonAmmo[cannon]));
            }
        }
        
        Debug.Log($"<color=cyan>[ReleaseGameManager] 총 {bulletCount}발의 총알 발사 시작!</color>");
    }
    
    // 캐논별 총알 발사 코루틴
    private IEnumerator FireBulletsFromCannonCoroutine(Cannon cannon, int ammo)
    {
        if (bulletPrefab == null)
        {
            Debug.LogError("<color=red>[ReleaseGameManager] 총알 프리팹이 할당되지 않았습니다!</color>");
            yield break;
        }
        
        if (cannon == null || cannon.turretBarrel == null)
        {
            Debug.LogWarning("<color=yellow>[ReleaseGameManager] 캐논이 없거나 포신이 없습니다!</color>");
            yield break;
        }
        
        Debug.Log($"<color=yellow>[ReleaseGameManager] 캐논 '{cannon.name}'에서 총알 {ammo}발 발사 시작</color>");
        
        // 캐논별로 총알 발사 간격 (더 빠르게)
        float firingInterval = bulletFireInterval * 0.5f; // 숫자를 줄여서 더 빠르게 발사
        int firedCount = 0;
        
        // 캐논 정보 로깅
        Debug.Log($"<color=cyan>[ReleaseGameManager] 발사 캐논 정보: ID={cannon.playerID}, 색상={cannon.playerColor}, 발사 간격: {firingInterval}</color>");
        
        while (ammo > 0)
        {
            // 발사 위치 결정
            Transform firePoint = cannon.firePoint != null ? cannon.firePoint : cannon.turretBarrel;
            
     
            
            // 총알 생성
            GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);
            
            // 총알 스크립트 설정
            CannonBullet bulletScript = bullet.GetComponent<CannonBullet>();
            if (bulletScript != null)
            {
                // 1. 소유자와 플레이어 ID 설정
                bulletScript.ownerCannon = cannon;
                bulletScript.ownerPlayerID = cannon.playerID;
                bulletScript.ownerColor = cannon.playerColor;
                
                // 색상 정보 로깅
                Debug.Log($"<color=magenta>[ReleaseGameManager] 총알 소유자 설정: 플레이어 {cannon.playerID}, 색상 R:{cannon.playerColor.r}, G:{cannon.playerColor.g}, B:{cannon.playerColor.b}</color>");
                
                // 2. 소유자 정보 설정
                bulletScript.SetOwner(cannon, cannon.playerColor, cannon.playerID);
                
                // 3. 발사 명령
                bulletScript.Fire(cannon.turretBarrel.forward, bulletSpeed);
            }
            else
            {
                // CannonBullet 스크립트가 없으면 Rigidbody 사용
                Rigidbody rb = bullet.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.useGravity = false;
                    rb.AddForce(cannon.turretBarrel.forward * bulletSpeed, ForceMode.VelocityChange);
                }
            }
            
            ammo--;
            firedCount++;
            
            // 다음 총알 발사 전 짧은 대기 시간
            yield return new WaitForSeconds(firingInterval);
        }
        
        Debug.Log($"<color=magenta>[ReleaseGameManager] 캐논 '{cannon.name}'에서 총 {firedCount}발 발사 완료</color>");
    }
    
    // --- 점수 변경 처리 메서드 ---
    private void HandleScoreChange(int newScore)
    {
        _latestScore = newScore;
        Debug.Log($"[ReleaseGameManager] Score Updated: {_latestScore}");
    }
    // ---------------------------

    // 현재 게임 상태 반환 (필요한 경우)
    public bool IsGameActive()
    {
        return isGameActive;
    }
    

}
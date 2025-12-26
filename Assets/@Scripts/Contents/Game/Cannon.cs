using UnityEngine;
using System;
using System.Collections;
using Unity.Assets.Scripts.Objects; // IEnumerator 사용 시 필요할 수 있음

public class Cannon : BaseObject
{
    [Header("회전 설정")]
    public Transform turretBarrel;
    // public float rotationSpeed = 360f; // 이전: 초당 회전 각도 -> 삭제 또는 주석 처리
    public float sweepSpeed = 55.3f; // 얼마나 느리게 좌우로 움직일지 (값이 작을수록 느림)
    public float sweepAngle = 180f;  // 최대 좌우 회전 각도 (중심 기준 +/- sweepAngle/2)
    public float barrelTurnSpeed = 15.0f; // 포신이 목표 각도로 회전하는 부드러움 정도 (Slerp 속도)

    [Header("발사 설정")]
    public Transform firePoint; // 총알이 발사될 위치
    public GameObject bulletPrefab; // 총알 프리팹
    public float bulletSpeed = 30f; // ✅ 빠른 총알 속도!
    public float fireInterval = 0.015f; // ✅ 초당 ~66발 연사! (다다다다다닥)

    [Header("플레이어 정보")]
    public int playerID = -1; // 캐논 소유자 플레이어 ID (-1은 중립)
    public Color playerColor = Color.white; // 플레이어 색상

    [Header("체력 설정")]
    public int maxHealth = 100;
    private int _currentHealth;
    private bool _isDestroyed = false;

    /// <summary>
    /// 현재 체력
    /// </summary>
    public int CurrentHealth => _currentHealth;

    /// <summary>
    /// 대포 파괴 시 이벤트 (playerId)
    /// </summary>
    public static event Action<int> OnCannonDestroyed;

    private bool isInitialized = false;
    private Quaternion centerRotation = Quaternion.identity; // 그리드 중심을 향한 초기 회전
    private bool _isFiring = false; // 발사 중 여부

    void Awake()
    {
        if (turretBarrel == null)
        {
            Debug.LogError("Turret Barrel이 할당되지 않았습니다!", this);
            enabled = false;
        }
        
        // --- 발사 위치 설정 ---
        if (firePoint == null)
        {
            // firePoint가 설정되지 않았으면 포신의 끝부분 또는 자신의 위치 사용
            firePoint = turretBarrel != null ? turretBarrel : transform;
            Debug.LogWarning("FirePoint가 할당되지 않아 " + firePoint.name + "의 위치를 사용합니다.", this);
        }
        // ----------------------
    }

    void Start()
    {
        if (turretBarrel == null)
        {
            Debug.LogError($"[Cannon] {name} - turretBarrel이 없습니다!", this);
            enabled = false;
            return;
        }

        // 체력 초기화
        _currentHealth = maxHealth;
        _isDestroyed = false;

        // 초기화 시도 (IsometricGridGenerator가 없어도 작동하도록)
        TryInitialize();
    }

    void TryInitialize()
    {
        if (isInitialized) return;

        Vector3 gridCenter = Vector3.zero;

        // IsometricGridGenerator가 있으면 그 위치를 중심으로
        if (IsometricGridGenerator.Instance != null)
        {
            gridCenter = IsometricGridGenerator.Instance.transform.position;
        }
        else
        {
            // 없으면 월드 중심(0,0,0) 또는 현재 바라보는 방향 사용
            Debug.LogWarning($"[Cannon] {name} - IsometricGridGenerator 없음, 현재 방향 사용");
            centerRotation = turretBarrel.rotation;
            isInitialized = true;
            Debug.Log($"<color=cyan>[Cannon] {name} 초기화 완료 (기본 방향)</color>");
            return;
        }

        // 그리드 중심을 향한 초기 방향 계산
        Vector3 directionToCenter = gridCenter - turretBarrel.position;
        directionToCenter.y = 0; // 수평 회전만

        if (directionToCenter.sqrMagnitude > 0.001f)
        {
            centerRotation = Quaternion.LookRotation(directionToCenter);
        }
        else
        {
            centerRotation = turretBarrel.rotation;
        }

        isInitialized = true;
        Debug.Log($"<color=cyan>[Cannon] {name} 초기화 완료 (그리드 중심 방향)</color>");
    }

    void Update()
    {
        // 아직 초기화 안됐으면 다시 시도
        if (!isInitialized)
        {
            TryInitialize();
            if (!isInitialized) return;
        }

        // 시간에 따라 -1 ~ 1 사이를 천천히 반복하는 값 생성
        float sweepFactor = Mathf.Sin(Time.time * sweepSpeed);

        // 목표 각도 계산 (중심 회전 기준 좌우 sweepAngle/2 만큼)
        float currentAngleOffset = sweepFactor * (sweepAngle / 2f);
        Quaternion targetRotation = centerRotation * Quaternion.Euler(0, currentAngleOffset, 0);

        // Slerp를 사용하여 부드럽게 목표 각도로 회전
        turretBarrel.rotation = Quaternion.Slerp(
            turretBarrel.rotation,
            targetRotation,
            barrelTurnSpeed * Time.deltaTime // 값이 작을수록 더 부드럽고 느리게 회전
        );

        // 디버그: 3초마다 한번씩 로그 (스팸 방지)
        if (Time.frameCount % 180 == 0)
        {
            Debug.Log($"<color=green>[Cannon] {name} 회전 중: sweepFactor={sweepFactor:F2}, angle={currentAngleOffset:F1}°</color>");
        }
    }

    #region Damage & Health
    /// <summary>
    /// 데미지 받음
    /// </summary>
    /// <param name="damage">데미지량</param>
    /// <returns>사망 여부</returns>
    public bool TakeDamage(int damage)
    {
        if (_isDestroyed) return true;

        _currentHealth -= damage;
        Debug.Log($"<color=red>[Cannon] Player {playerID} 대포 피격! 데미지: {damage}, 남은 체력: {_currentHealth}/{maxHealth}</color>");

        if (_currentHealth <= 0)
        {
            _currentHealth = 0;
            Die();
            return true;
        }

        return false;
    }

    /// <summary>
    /// 대포 파괴 처리
    /// </summary>
    private void Die()
    {
        if (_isDestroyed) return;

        _isDestroyed = true;
        Debug.Log($"<color=red>[Cannon] ★★★ Player {playerID} 대포 파괴! 게임 오버! ★★★</color>");

        // 이벤트 발행
        OnCannonDestroyed?.Invoke(playerID);

        // TODO: 파괴 이펙트, 사운드 등 추가
    }

    /// <summary>
    /// 대포가 파괴되었는지 확인
    /// </summary>
    public bool IsDestroyed => _isDestroyed;
    #endregion

    #region Fire (총알 발사)
    /// <summary>
    /// 총알 발사 (bulletCount 개수만큼)
    /// </summary>
    public void Fire(int bulletCount)
    {
        if (_isDestroyed || _isFiring || bulletCount <= 0) return;

        StartCoroutine(FireCoroutine(bulletCount));
    }

    /// <summary>
    /// 단일 총알 발사
    /// </summary>
    public void FireSingle()
    {
        if (_isDestroyed) return;

        SpawnBullet();
    }

    /// <summary>
    /// 발사 코루틴 (연속 발사)
    /// </summary>
    private IEnumerator FireCoroutine(int bulletCount)
    {
        _isFiring = true;

        for (int i = 0; i < bulletCount; i++)
        {
            if (_isDestroyed) break;

            SpawnBullet();

            yield return new WaitForSeconds(fireInterval);
        }

        _isFiring = false;
        Debug.Log($"<color=cyan>[Cannon] Player {playerID} 발사 완료 - {bulletCount}발</color>");
    }

    /// <summary>
    /// 총알 생성
    /// </summary>
    private void SpawnBullet()
    {
        if (bulletPrefab == null)
        {
            Debug.LogWarning($"[Cannon] Player {playerID} 총알 프리팹이 없습니다!");
            return;
        }

        // 발사 위치 및 방향
        Transform spawnPoint = firePoint != null ? firePoint : turretBarrel;
        Vector3 spawnPos = spawnPoint.position;
        Vector3 direction = turretBarrel.forward;

        // 총알 생성
        GameObject bulletObj = Instantiate(bulletPrefab, spawnPos, Quaternion.identity);

        // ✅ 프리팹이 비활성화 상태일 수 있으므로 활성화
        bulletObj.SetActive(true);

        // 총알 설정
        CannonBullet bullet = bulletObj.GetComponent<CannonBullet>();
        if (bullet != null)
        {
            bullet.SetOwner(this, playerColor, playerID);
            bullet.Fire(direction, bulletSpeed);
            Debug.Log($"<color=yellow>[Cannon] Player {playerID} 총알 발사! 방향: {direction}</color>");
        }
        else
        {
            Debug.LogError($"<color=red>[Cannon] Player {playerID} CannonBullet 컴포넌트 없음!</color>");
        }
    }

    /// <summary>
    /// 발사 중인지 확인
    /// </summary>
    public bool IsFiring => _isFiring;
    #endregion

    // 기즈모: 그리드 중심으로 선 표시 (선택 사항)
    void OnDrawGizmosSelected()
    {
         if (IsometricGridGenerator.Instance != null)
         {
             Gizmos.color = Color.cyan;
             Vector3 gridCenter = IsometricGridGenerator.Instance.transform.position;
              if (turretBarrel != null)
                  Gizmos.DrawLine(turretBarrel.position, gridCenter);
              else
                  Gizmos.DrawLine(transform.position, gridCenter);

              // 스윕 범위 시각화 (선택 사항)
              if (isInitialized)
              {
                  Quaternion leftRot = centerRotation * Quaternion.Euler(0, -sweepAngle / 2f, 0);
                  Quaternion rightRot = centerRotation * Quaternion.Euler(0, sweepAngle / 2f, 0);
                  Vector3 leftDir = leftRot * Vector3.forward;
                  Vector3 rightDir = rightRot * Vector3.forward;
                  Gizmos.color = Color.yellow;
                  Gizmos.DrawRay(turretBarrel.position, leftDir * 5f); // 길이는 임의로 설정
                  Gizmos.DrawRay(turretBarrel.position, rightDir * 5f);
              }
         }
    }
}
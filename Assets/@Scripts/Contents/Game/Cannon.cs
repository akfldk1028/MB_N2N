using UnityEngine;
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

    // --- 발사 위치만 유지 ---
    [Header("발사 위치")]
    public Transform firePoint; // 총알이 발사될 위치
    // ------------------------

    [Header("플레이어 정보")]
    public int playerID = -1; // 캐논 소유자 플레이어 ID (-1은 중립)
    public Color playerColor = Color.white; // 플레이어 색상

    private bool isInitialized = false;
    private Quaternion centerRotation = Quaternion.identity; // 그리드 중심을 향한 초기 회전

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
        if (turretBarrel == null) { enabled = false; return; }

        if (IsometricGridGenerator.Instance == null)
        {
            Debug.LogError("IsometricGridGenerator Instance를 찾을 수 없습니다!", this);
            enabled = false;
            return;
        }

        InitializeCannon();
    }

    void InitializeCannon()
    {
        if (isInitialized) return;

        IsometricGridGenerator gridGen = IsometricGridGenerator.Instance;

        // 그리드 중심 계산 (대략적인)
        Vector3 gridCenter = gridGen.transform.position;

        // 그리드 중심을 향한 초기 방향 계산
        Vector3 directionToCenter = gridCenter - turretBarrel.position;
        directionToCenter.y = 0; // 수평 회전만
        if (directionToCenter.sqrMagnitude > 0.001f)
        {
            centerRotation = Quaternion.LookRotation(directionToCenter);
        }
        else
        {
            centerRotation = turretBarrel.rotation; // 방향 계산 불가 시 현재 회전값 사용
        }

        // 초기 회전 적용 (선택 사항: 바로 적용하거나 Slerp로 천천히)
        // turretBarrel.rotation = centerRotation;

        isInitialized = true;
        Debug.Log($"[{name}] Cannon Initialized. Center Rotation Set.");
    }

    void Update()
    {
        if (!isInitialized) return;

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
    }

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
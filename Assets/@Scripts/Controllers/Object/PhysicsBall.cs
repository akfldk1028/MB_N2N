using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using static Define;

namespace Unity.Assets.Scripts.Objects
{
    // 상태 Enum 정의
    public enum EBallState
    {
        None,      // 초기 상태 또는 정의되지 않은 상태
        Ready,     // 플랭크 위에서 발사 대기 중
        Launching, // 여러 개의 공을 순차적으로 발사 중
        Moving     // 발사 완료 후 자유롭게 이동 중
    }

    public class PhysicsBall : PhysicsObject
    {
        #region Fields & Properties
        [Header("Ball Properties")]
        [SerializeField] private int ballCount = 1;
        [SerializeField] private float launchForce = 1f;
        [SerializeField] private float bounceImpactThreshold = 2f; // 튕김 판정 임계값
        
        [Header("Ball Visuals")]
        [SerializeField] private GameObject ballModel;
        [SerializeField] private Material[] ballMaterials;
        [SerializeField] private GameObject ballNumberText;
        private TextMesh numberOfBalls;
        
        [Header("Prediction Line")]
        [SerializeField] private LineRenderer lineRenderer;
        [SerializeField] private LayerMask predictionLayerMask;
        [SerializeField] private float maxRayDistance = 15f;
        [SerializeField] private float reflectionLineLength = 2f;
        private bool showPredictionLine = false;
        
        [Header("Plank Interaction")]
        [SerializeField] private float maxBounceAngle = 75f;
        [SerializeField] private float plankMoveThreshold = 0.1f;
        [SerializeField] private Vector2 launchDirection = Vector2.up;
        [SerializeField] private PhysicsPlank plank; // Assign the GameObject with PhysicsPlank script in Inspector
        private float previousPlankX;
        private Collider2D _plankCollider;
        private const float SPAWN_OFFSET_Y = 0.05f;
        
        // Ball Behavior
        private bool multipleBalls = false;
        private int ballsShooted = 0;
        private float shootTimer = 0;
        private bool waveEnd = false;
        private float ballStuckYPos = 0;
        private bool _needsPositionReset = false; // LateUpdate에서 위치 재설정 필요 여부 플래그
        


        [Header("Power Up Effects")]
        private static int currentPower = 1; // 모든 공이 공유하는 공격력
        private static float powerTimer = 0f; // 모든 공이 공유하는 타이머
        [SerializeField] private Material normalMaterial; // 기본 재질
        [SerializeField] private Material poweredMaterial; // 파워업 상태 재질
        private Renderer ballRenderer;

        // 정적 변수 접근자
        public static int SharedPower => currentPower;
        public static float SharedPowerTimer => powerTimer;

        // 공격력 접근자
        public int AttackPower => currentPower;

        // 네트워크 변수
        private NetworkVariable<int> _syncedBallCount = new NetworkVariable<int>(1);
        private NetworkVariable<EBallState> _syncedState = new NetworkVariable<EBallState>(EBallState.None); // 상태 동기화 (여기서 정의)
        
        // 시스템 변수
        private Camera mainCamera;
        
        // 상태 머신 변수
        private EBallState _currentState = EBallState.None;
        public EBallState CurrentState
        {
            get => _currentState;
            protected set
            {
                if (_currentState != value)
                {
                     #if UNITY_EDITOR || DEVELOPMENT_BUILD
                     Debug.Log($"[{gameObject.name}] State Change: {_currentState} -> {value}");
                     #endif
                    _currentState = value;
                    // 서버이고 스폰된 상태면 클라이언트에 상태 동기화
                    if (IsServer && IsSpawned)
                    {
                        _syncedState.Value = _currentState;
                    }
                    // 상태 진입 시 초기화 로직 호출
                    OnEnterState(value);
                }
            }
        }
        #endregion

        #region Unity Lifecycle Methods
        public override bool Init()
        {
            if (!base.Init()) // base.Init() 호출 및 결과 확인
                return false;

            ObjectType = EObjectType.None; // Define.cs 에 Ball 추가 필요

            lineRenderer = GetComponent<LineRenderer>();
            _plankCollider = plank.GetComponent<BoxCollider2D>();

            if (lineRenderer != null)
            {
                lineRenderer.enabled = false;
            }

            mainCamera = Camera.main;
            Debug.Log($"<color=green>[Ball] Init: Ball 컴포넌트 초기화됨, 볼 개수: {ballCount}</color>");
            return true;
        }
        
        protected virtual void Start() // virtual로 선언하여 혹시 모를 자식 클래스 오버라이드 허용
        {
            // 서버에서만 초기 상태 설정 및 위치 조정
            if (IsServer || !IsSpawned)
            {
                 ResetBallToReadyState(); // 초기 상태 및 위치 설정

            }
                // 렌더러 컴포넌트 참조 초기화
            if (ballRenderer == null)
            {
                ballRenderer = GetComponent<Renderer>() ?? ballModel?.GetComponent<Renderer>();
            }
        }

        public void Update()
        {
            // 서버에서만 상태 머신 로직 실행
            if (IsServer || !IsSpawned) // 로컬 테스트용으로 !IsSpawned 추가
            {
                UpdateStateMachine();
            }
                UpdatePowerStatus();


        }
        
        protected override void FixedUpdate()
        {
            base.FixedUpdate(); // 부모 FixedUpdate 호출 (이전 상태 기록 등)
            
            // 서버에서만 물리 관련 업데이트 수행
             if (IsServer || !IsSpawned) // 로컬 테스트용으로 !IsSpawned 추가
             {
                // Moving 상태에서만 물리 업데이트 수행
                 if (CurrentState == EBallState.Moving && rb != null && !rb.isKinematic)
                 {
                     UpdateMovingPhysics();
                 }
             }
        }
        
        // 다른 Unity 생명주기 메서드는 필요시 여기에 추가 (OnEnable, OnDisable, LateUpdate 등)
        #endregion
        
        #region Network Lifecycle Methods
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsServer)
            {
                _syncedBallCount.Value = ballCount;
                _syncedState.Value = CurrentState; // 초기 상태 동기화
            }

            if (IsClient)
            {
                _syncedBallCount.OnValueChanged += OnBallCountChanged;
                _syncedState.OnValueChanged += OnStateChanged; // 상태 변경 콜백 등록

                 // 클라이언트는 서버로부터 받은 상태를 즉시 적용
                CurrentState = _syncedState.Value;
            }
        }
        
        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            
            if (IsClient)
            {
                _syncedBallCount.OnValueChanged -= OnBallCountChanged;
                _syncedState.OnValueChanged -= OnStateChanged; // 상태 변경 콜백 해제
            }
        }
        
        // 네트워크 콜백 메서드들
        private void OnBallCountChanged(int previousValue, int newValue){ ballCount = newValue;}
        
        private void OnStateChanged(EBallState previousValue, EBallState newValue)
        {
            // 클라이언트는 서버로부터 받은 상태를 적용
            if (!IsServer)
            {
                 CurrentState = newValue;
            }
        }
        #endregion
        
        #region Overridden Methods
        protected override void OnStuck()
        {
            base.OnStuck(); // 부모 클래스의 기본 구현 호출 (필요시)
            
            // 현재 Moving 상태일 때만 임의의 방향으로 약한 힘을 가해 공이 움직이도록 함
            if (CurrentState == EBallState.Moving)
            {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning($"[{gameObject.name}] Ball is stuck. Applying small random force.");
                #endif
                
                // 즉시 작은 힘을 가해 공을 움직이게 함
                if (rb != null && !rb.isKinematic)
                {
                    // 랜덤한 방향으로 작은 힘 적용
                    ApplyForce(UnityEngine.Random.insideUnitCircle.normalized * 0.15f, ForceMode2D.Impulse);
                }
            }
        }
        
        // 충돌 처리 (HandleCollision 오버라이드)
        protected override void HandleCollision(Collision2D collision)
        {
            // 서버에서만 충돌 처리
            if (!IsServer && IsSpawned) return; // 클라이언트는 충돌 로직 직접 처리 안함

            // 벽돌 또는 벽과의 충돌 처리
            if (collision.gameObject.CompareTag("Brick") || collision.gameObject.CompareTag("Wall"))
            {
                HandleBrickOrWallCollision(collision);
            }
            // 플랭크와의 충돌 처리 (Moving 상태일 때만)
            else if (CurrentState == EBallState.Moving && collision.gameObject.CompareTag("Plank"))
            {
                 // 플랭크 충돌 로직 (튕기는 각도 조절 등)
                 HandlePlankCollision(collision);
            }
            // 다른 물리 객체와의 충돌 (필요시 추가)
            // else if (collision.gameObject.GetComponent<PhysicsObject>() != null) { ... }
        }

        // 트리거 처리 (HandleTrigger 오버라이드)
        // PhysicsBall 클래스의 HandleTrigger 메서드 수정
        protected override void HandleTrigger(Collider2D other)
        {
            // 서버에서만 트리거 처리
            if (!IsServer && IsSpawned) return; // 클라이언트는 트리거 로직 직접 처리 안함

            // 바닥 경계선 트리거 (공 회수)
            if (other.CompareTag("BottomBoundary"))
            {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log($"[{gameObject.name}] Ball hit BottomBoundary. Current ball count: {ballCount}");
                #endif
                
                // 이 공이 마지막 남은 공인지 확인
                if (IsLastBall())
                {
                    // 마지막 공이면 Ready 상태로 재설정
                    ResetBallToReadyState();
                }
                else
                {
                    // 마지막 공이 아니면 이 공만 파괴
                    Destroy(gameObject);
                }
            }
        }

        // 이 공이 마지막 남은 공인지 확인하는 메서드
        private bool IsLastBall()
        {
            // 1. 현재 씬의 모든 PhysicsBall 찾기
            PhysicsBall[] allBalls = FindObjectsOfType<PhysicsBall>();
            
            // 2. Moving 또는 Launching 상태인 공의 개수 확인
            int movingBalls = 0;
            foreach (PhysicsBall ball in allBalls)
            {
                if (ball.CurrentState == EBallState.Moving || ball.CurrentState == EBallState.Launching)
                {
                    movingBalls++;
                }
            }
            
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[{gameObject.name}] Moving balls count: {movingBalls}");
            #endif
            
            // 3. 이 공을 포함해서 Moving 또는 Launching 상태인 공이 1개(자기 자신)뿐이라면 마지막 공
            return movingBalls <= 1;
        }
        
        // 기즈모 렌더링
        protected override void OnDrawGizmos()
        {
             base.OnDrawGizmos(); // 부모 기즈모 호출

             // 상태별 추가 정보 표시 (예시)
             #if UNITY_EDITOR
             UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f, $"State: {CurrentState}");
             if (CurrentState == EBallState.Ready)
             {
                 Gizmos.color = Color.green;
                 Gizmos.DrawLine(transform.position, transform.position + (Vector3)launchDirection * 2f);
             }
             #endif
        }
        #endregion

        #region State Machine Methods
        // 상태 머신 메인 로직
        private void UpdateStateMachine()
        {
            switch (CurrentState)
            {
                case EBallState.Ready:
                    UpdateReadyState();
                    break;
                case EBallState.Launching:
                    UpdateLaunchingState();
                    break;
                case EBallState.Moving:
                    UpdateMovingState();
                    break;
                case EBallState.None:
                    // 초기화 안된 상태 처리 (예: Ready 상태로 강제 전환)
                     if(IsServer) CurrentState = EBallState.Ready;
                    break;
            }
        }

        // 상태 진입 시 초기화 로직
        private void OnEnterState(EBallState newState)
        {
            switch (newState)
            {
                case EBallState.Ready:
                     SetBallPositionAbovePlank(); // 위치/속도 설정
                     previousPlankX = (plank != null) ? plank.transform.position.x : transform.position.x;
                     // Kinematic 설정은 여기서!
                     if (rb != null)
                     {
                         rb.isKinematic = true;
                     }
                    break;
                case EBallState.Launching:
                     // 발사할 공 개수 설정, 타이머 초기화
                     ballsShooted = 1;
                     shootTimer = 0f;
                    break;
                case EBallState.Moving:
                    // 이동 상태 시작 시 필요한 초기화
                    // Stuck 감지는 PhysicsObject에서 자동으로 처리됨
                    break;
            }
        }

        private void UpdateReadyState()
        {
             // // 게임 진행 가능할 때만 발사 로직 처리
             // if (CommonVars.canContinue && plank != null)
             // {
             //     float currentPlankX = plank.transform.position.x;
             //
             //     // 플랭크 위에 공 위치 유지 (약간의 오차 허용)
             //     if (Mathf.Abs(currentPlankX - transform.position.x) > 0.01f || Mathf.Abs(transform.position.y - GetPlankRelativeSpawnY()) > 0.01f)
             //     {
             //         SetBallPositionAbovePlank();
             //     }
             //
             //     // 플랭크 이동 감지 (첫 프레임 스킵 로직 제거 - 상태 진입 시 위치 고정으로 대체)
             //     if (Mathf.Abs(currentPlankX - previousPlankX) >= plankMoveThreshold)
             //     {
             //         LaunchBall(launchDirection); // 설정된 기본 방향으로 발사
             //     }
             //     else
             //     {
             //          previousPlankX = currentPlankX; // 이전 위치 업데이트
             //     }
             // }
        }

        private void UpdateLaunchingState()
        {
             if (ballCount > ballsShooted + 1)
             {
                  // 다음 공 발사 (첫 공과 같은 방향, 같은 위치에서)
                  LaunchBall(launchDirection); // PhysicsObject의 Launch 재사용

                  ballsShooted++;
                  #if UNITY_EDITOR || DEVELOPMENT_BUILD
                  Debug.Log($"[PhysicsBall] Launching ball {ballsShooted}/{ballCount}");
                  #endif
             }
        }

        private void UpdateMovingState(){}

        // UpdateMovingPhysics 메서드 개선 - 속도 상한 추가
        private void UpdateMovingPhysics()
        {
            if (rb != null)
            {
                float currentSpeed = rb.linearVelocity.magnitude;
                
                // 속도가 너무 느리면 최소 속도 유지
                if (currentSpeed > 0 && currentSpeed < 5f)
                {
                    rb.linearVelocity = rb.linearVelocity.normalized * 10f;
                }
                // 속도가 너무 빠르면 최대 속도로 제한
                else if (currentSpeed > 20f)
                {
                    rb.linearVelocity = rb.linearVelocity.normalized * 20f;
                    
                    #if UNITY_EDITOR || DEVELOPMENT_BUILD
                    Debug.LogWarning($"[{gameObject.name}] Ball speed capped at 20 (was {currentSpeed:F1})");
                    #endif
                }
            }
        }
        #endregion
        
        #region Ball Specific Methods
        // 볼 발사 메서드
        public void LaunchBall(Vector2 direction)
        {
            if (rb != null)
            {
                // 발사 직전에 Kinematic 해제!
                rb.isKinematic = false;

                Launch(direction.normalized, launchForce, ForceMode2D.Impulse); // PhysicsObject의 Launch 메서드 활용

                // 상태 전환
                if (ballCount > 1)
                {
                    CurrentState = EBallState.Launching;
                }
                else
                {
                    CurrentState = EBallState.Moving;
                }
            }
        }
        
        // 볼 개수 설정 메서드
        public void SetBallCount(int count)
        {
             // 서버 또는 스폰되지 않은 경우에만 개수 설정 가능
            if (IsServer || !IsSpawned)
            {
                ballCount = Mathf.Max(1, count);
                 // 스폰된 서버 객체일 때만 NetworkVariable 동기화
                if(IsServer && IsSpawned)
                {
                     _syncedBallCount.Value = ballCount;
                }
            }
        }
        
        // 공을 Ready 상태로 리셋하는 메서드
        private void ResetBallToReadyState()
        {
             // 상태 변경 전에 명시적으로 속도 초기화 (안전 장치)
             if (rb != null)
             {
                 rb.linearVelocity = Vector2.zero;
                 rb.angularVelocity = 0f;
             }

             // 상태 변경 (OnEnterState에서 위치 설정 및 Kinematic 설정)
             CurrentState = EBallState.Ready;

             ResetWave(); // CommonVars 변수들 초기화
        }
        
        // 웨이브 리셋 (게임 로직 관련 변수 초기화)
        private void ResetWave()
        {
            // 이 변수들이 Define.cs 또는 다른 곳에 정의되어 있어야 합니다.
            // 만약 CommonVars가 없다면, Define.cs 또는 관련 스크립트 확인 필요
            // CommonVars.ballsReachedDistance = 0;
            // CommonVars.canContinue = true;
            // CommonVars.startMovingTowardsMainBall = false;
            // CommonVars.firstBallHitBottomCollider = false;
            // CommonVars.newWaveOfBricks = true;
        }
        #endregion
        
        #region Collision Helper Methods
        // 벽돌/벽 충돌 처리
    private void HandleBrickOrWallCollision(Collision2D collision)
    {
        if (rb == null) return;
        
        // 충돌한 표면의 법선 벡터 가져오기
        Vector2 normal = collision.contacts[0].normal;
        
        // 현재 속도에 기반한 반사 방향 계산
        Vector2 reflectDir = Vector2.Reflect(rb.linearVelocity.normalized, normal);
        
        // 현재 속력 유지
        float currentSpeed = rb.linearVelocity.magnitude;
        
        // 속력이 너무 느리면 기본 속도로 설정
        if (currentSpeed < 5f) currentSpeed = 10f;
        
        // 속력이 너무 빠르면 최대 속도로 제한
        float maxSpeed = 12f;
        if (currentSpeed > maxSpeed) currentSpeed = maxSpeed;
        
        // 벽과의 충돌 경우 특별 처리
        if (collision.gameObject.CompareTag("Wall"))
        {
            // 벽과 거의 평행한 경우 각도 조정 (중요: 벽 타기 방지)
            float dotProduct = Vector2.Dot(reflectDir, normal);
            if (Mathf.Abs(dotProduct) < 0.1f) // 거의 평행할 때
            {
                // 법선 방향으로 살짝 밀어서 각도 조정
                reflectDir = reflectDir + normal * 0.2f;
                reflectDir.Normalize();
            }
            
            // 벽 충돌 시에는 랜덤 성분 없이 정확한 반사 적용
        }
        else if (collision.gameObject.CompareTag("Brick"))
        {
            // 벽돌일 경우 약간의 랜덤 방향 추가 (랜덤 각도 범위 축소)
            float randomAngle = UnityEngine.Random.Range(-2f, 2f) * Mathf.Deg2Rad; // 더 작은 랜덤 범위
            reflectDir = new Vector2(
                reflectDir.x * Mathf.Cos(randomAngle) - reflectDir.y * Mathf.Sin(randomAngle),
                reflectDir.x * Mathf.Sin(randomAngle) + reflectDir.y * Mathf.Cos(randomAngle)
            );
            
            // 벽돌에 데미지 적용 (필요시)
            Brick brick = collision.gameObject.GetComponent<Brick>();
            if (brick != null)
            {
                // brick.OnHit();
            }
        }
        
        // 새 속도 적용 - 정규화된 방향에 제한된 속도 적용
        rb.linearVelocity = reflectDir.normalized * currentSpeed;
        
        // 충돌 후 살짝 이동시켜 연속 충돌 방지
        transform.position += (Vector3)(reflectDir.normalized * 0.01f);
    }
        
            // 플랭크 충돌 처리 로직
    private void HandlePlankCollision(Collision2D collision)
    {
        if (rb == null || plank == null) return;

        // 충돌 지점과 플랭크 중심 간의 거리 계산
        Vector2 contactPoint = collision.GetContact(0).point;
        Vector2 plankCenter = plank.transform.position;
        float difference = contactPoint.x - plankCenter.x;

        // 플랭크 너비 대비 상대적 위치 (-1 ~ 1)
        float plankWidth = plank.GetComponent<BoxCollider2D>().size.x * plank.transform.localScale.x;
        float normalizedDifference = Mathf.Clamp(difference / (plankWidth / 2f), -1f, 1f);

        // 상대적 위치에 따라 반사 각도 계산 (최대 각도 제한)
        float angle = normalizedDifference * maxBounceAngle;
        Quaternion rotation = Quaternion.Euler(0f, 0f, -angle); // Z축 회전
        Vector2 bounceDirection = rotation * Vector2.up;

        // 현재 속력 유지 또는 약간 증가 (최대 속도 제한 추가)
        float currentSpeed = Mathf.Max(rb.linearVelocity.magnitude, 5f); // 최소 속도 보장
        float maxSpeed = 20f; // 최대 속도 제한
        
        // 속도 증가 계수 줄임 (1.05 → 1.02)
        float bounceSpeed = currentSpeed * 1.02f; 
        
        // 최대 속도 넘지 않도록 제한
        if (bounceSpeed > maxSpeed) bounceSpeed = maxSpeed;

        // 새 속도 적용
        rb.linearVelocity = bounceDirection.normalized * bounceSpeed;

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[PhysicsBall] Plank collision. Difference: {difference:F2}, Angle: {angle:F1}, Speed: {bounceSpeed:F1}");
        #endif
    }
        #endregion
        
        #region Utility Methods
        // 플랭크 위로 볼 위치 설정
    private void SetBallPositionAbovePlank()
    {
        if (plank != null)
        {
            // BallPositionUtility 사용하여 위치 계산
            // Vector3 newPosition = BallPositionUtility.GetLaunchPosition(plank, objectCollider, transform);
            // transform.position = newPosition;
            
            // 속도 초기화
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }
        }
        else
        {
            Debug.LogWarning($"[{gameObject.name} SetBallPositionAbovePlank] Plank reference is null");
        }
    }
        
        // 플랭크 기준 스폰 Y 좌표 계산
   private float GetPlankRelativeSpawnY()
    {
        if (plank == null) return transform.position.y; // 안전 장치
        
        // BallPositionUtility를 사용하여 전체 위치를 계산하고 Y 좌표만 반환
        // Vector3 calculatedPosition = BallPositionUtility.GetLaunchPosition(plank, objectCollider, transform);
        // return calculatedPosition.y;
        return  plank.transform.position.y;
    }

    public static void SharedPowerUp(int amount, float duration)
    {
        currentPower += amount;
        powerTimer = Mathf.Max(powerTimer, duration);
        
        Debug.Log($"<color=green>[PhysicsBall] 모든 공 공격력 증가: {currentPower}, 남은 시간: {powerTimer}초</color>");
    }

    public void PowerUp(int amount, float duration)
    {
        // 공유 메서드 호출 - 모든 공에 영향
        SharedPowerUp(amount, duration);
    }



 // PhysicsBall의 UpdatePowerStatus 메서드 수정
    private void UpdatePowerStatus()
    {
        // 색상 업데이트는 모든 공에서 실행
        ColorBallByPower();
        
  
        if (gameObject.name.Contains("ball") || FindObjectsOfType<PhysicsBall>().Length == 1)
        {
            if (powerTimer > 0)
            {
                powerTimer -= Time.deltaTime;
                
                if (powerTimer <= 0)
                {
                    powerTimer = 0;
                    currentPower = 1; // 기본값으로 리셋
                    
                    Debug.Log("<color=yellow>[PhysicsBall] 모든 공 공격력 효과 종료</color>");
                }
            }
        }
    }
    public void ColorBallByPower()
    {
        if (ballRenderer == null) return;
        
        // 목표 색상 계산 - 정적 공격력 사용
        Color targetColor;
        if (currentPower <= 1)
        {
            // 기본 상태 - 흰색
            targetColor = Color.white;
        }
        else if (currentPower <= 3)
        {
            // 공격력 2~3 - 노란색에서 빨간색으로 전환
            float intensity = (currentPower - 1) / 2f; // 0~1 사이 값
            targetColor = new Color(1, 1 - intensity, 0);
        }
        else if (currentPower <= 5)
        {
            // 공격력 4~5 - 빨간색에서 보라색으로 전환
            float intensity = (currentPower - 3) / 2f; // 0~1 사이 값
            targetColor = new Color(1, 0, intensity);
        }
        else
        {
            // 공격력 6 이상 - 보라색에서 파란색으로 전환
            float redValue = 1 - Mathf.Min((currentPower - 5) / 3f, 1f);
            targetColor = new Color(Mathf.Max(redValue, 0), 0, 1);
        }
        
        // 현재 색상에서 목표 색상으로 점진적 전환
        if (ballRenderer.material.HasProperty("_Color"))
        {
            Color currentColor = ballRenderer.material.color;
            ballRenderer.material.color = Color.Lerp(currentColor, targetColor, Time.deltaTime * 3f); // 속도 조절 가능
        }
        
        // 목표 크기 계산 - 정적 공격력 사용
        // float targetScale = 1.0f + (_sharedPower - 1) * 0.05f;
        // targetScale = Mathf.Min(targetScale, 1.3f); // 최대 130%까지만 커지도록 제한
        
        // // 현재 크기에서 목표 크기로 점진적 전환
        // transform.localScale = Vector3.Lerp(transform.localScale, 
        //     new Vector3(targetScale, targetScale, 1f), Time.deltaTime * 5f); // 속도 조절 가능
    }
        #endregion
    }
}
using System.Collections;
using System.Collections.Generic;
using Unity.Assets.Scripts.Objects;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 블록깨기 패들(Plank) 컨트롤러
///
/// 멀티플레이어 동기화:
/// - NetworkVariable + ServerRpc로 위치 동기화
/// - Owner(Client)가 입력 처리 → ServerRpc로 Server에 전송 → Server가 NetworkVariable 업데이트
/// - 경계(leftEnd, rightEnd)도 NetworkVariable로 동기화
/// </summary>
public class PhysicsPlank : PhysicsObject
{
    // ✅ 네트워크 위치 동기화 (Server가 Write, Everyone Read)
    private NetworkVariable<Vector3> _syncedPosition = new NetworkVariable<Vector3>(
        Vector3.zero,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    // ✅ 위치 보간용
    private float _interpolationSpeed = 15f;

    // ✅ 플레이어별 경계 동기화 (Server가 설정, Client가 읽음)
    private NetworkVariable<float> _syncedLeftBoundX = new NetworkVariable<float>(
        -8f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server
    );
    private NetworkVariable<float> _syncedRightBoundX = new NetworkVariable<float>(
        8f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server
    );

    public Transform leftEnd = null;  // 왼쪽 이동 한계점 Transform
    public Transform rightEnd = null; // 오른쪽 이동 한계점 Transform

    [Tooltip("플랭크가 마우스를 따라가는 속도. 값이 클수록 빠르게 반응합니다.")]
    [Range(1f, 20f)] // Inspector에서 슬라이더로 조절 가능
    public float smoothSpeed = 20f; // 따라가는 속도 조절 변수

    public Camera mainCamera = null; // 카메라 (자동 설정됨)
    private Plane plankPlane; // Raycast를 위한 평면

    // ✅ 컴포넌트 참조
    private Rigidbody2D _rb;
    private float _keyboardMoveSpeed = 15f; // PlankManager와 동일한 속도

    // ✅ 멀티플레이어 입력 처리 (Owner만) - 직접 Input 사용
    // ActionBus 제거: ParrelSync 환경에서 양쪽 에디터가 동시에 입력받는 문제 해결

    void Start()
    {
        // ✅ 네트워크 모드에서는 OnNetworkSpawn()에서 초기화하므로 스킵
        // (멀티플레이어에서 경계가 잘못 설정되는 것 방지)
        var netObj = GetComponent<Unity.Netcode.NetworkObject>();
        bool isNetworkMode = netObj != null && (netObj.IsSpawned || Unity.Netcode.NetworkManager.Singleton?.IsListening == true);

        if (isNetworkMode)
        {
            // 네트워크 모드: 기본 컴포넌트만 초기화, 경계는 OnNetworkSpawn()에서
            plankPlane = new Plane(Vector3.forward, transform.position);
            _rb = GetComponent<Rigidbody2D>();

            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            GameLogger.Info("PhysicsPlank", $"{gameObject.name} 네트워크 모드 - OnNetworkSpawn() 대기 중");
            return;
        }

        // ✅ 싱글플레이어 모드: 기존 로직
        AutoInitializeReferences();

        // 필수 참조 체크
        if (mainCamera == null)
        {
            GameLogger.Error("PhysicsPlank", "Camera를 찾을 수 없습니다!");
            enabled = false;
            return;
        }
        if (leftEnd == null || rightEnd == null)
        {
            GameLogger.Error("PhysicsPlank", "leftEnd 또는 rightEnd를 찾을 수 없습니다!");
            enabled = false;
            return;
        }
        if (leftEnd.position.x >= rightEnd.position.x)
        {
            GameLogger.Warning("PhysicsPlank", $"leftEnd({leftEnd.position.x})가 rightEnd({rightEnd.position.x})보다 큽니다!");
        }

        plankPlane = new Plane(Vector3.forward, transform.position);
        _rb = GetComponent<Rigidbody2D>();

        GameLogger.Success("PhysicsPlank", $"{gameObject.name} 싱글플레이어 초기화 완료");
    }

    /// <summary>
    /// 네트워크 모드 여부 확인 (NetworkBehaviour 캐스팅 캡슐화)
    /// </summary>
    public bool IsNetworkMode()
    {
        return IsSpawned;
    }

    #region Public 이동 메서드 (PlankManager에서 호출)

    /// <summary>
    /// 키보드 입력으로 패들 이동 (PlankManager에서 호출)
    /// </summary>
    /// <param name="horizontal">좌우 입력값 (-1, 0, 1)</param>
    /// <param name="deltaTime">프레임 시간</param>
    public void MoveByKeyboard(float horizontal, float deltaTime)
    {
        // ✅ 네트워크 모드: Owner만 이동 가능
        if (IsSpawned && !IsOwner)
        {
            return;
        }

        if (Mathf.Abs(horizontal) < 0.01f) return;

        Vector3 currentPosition = transform.position;
        float targetX = currentPosition.x + (horizontal * _keyboardMoveSpeed * deltaTime);

        // 경계 제한
        if (leftEnd != null && rightEnd != null)
        {
            targetX = Mathf.Clamp(targetX, leftEnd.position.x, rightEnd.position.x);
        }

        Vector3 newPosition = new Vector3(targetX, currentPosition.y, currentPosition.z);

        // Rigidbody2D로 이동
        ApplyMovement(newPosition);
    }

    /// <summary>
    /// 마우스/터치 위치로 패들 이동 (PlankManager에서 호출)
    /// </summary>
    /// <param name="pointerPosition">스크린 좌표</param>
    /// <param name="camera">카메라 참조</param>
    public void MoveByPointer(Vector3 pointerPosition, Camera camera)
    {
        // ✅ 네트워크 모드: Owner만 이동 가능
        if (IsSpawned && !IsOwner)
        {
            return;
        }

        if (camera == null || plankPlane.normal == Vector3.zero) return;

        // 1. 마우스 위치로 Ray 생성
        Ray ray = camera.ScreenPointToRay(pointerPosition);

        // 2. Ray와 플랭크 평면의 교차점 계산
        float enterDistance;
        if (plankPlane.Raycast(ray, out enterDistance))
        {
            // 교차점 월드 좌표 얻기
            Vector3 worldPosition = ray.GetPoint(enterDistance);
            float targetX = worldPosition.x;

            // 3. 경계 제한
            if (leftEnd != null && rightEnd != null)
            {
                targetX = Mathf.Clamp(targetX, leftEnd.position.x, rightEnd.position.x);
            }

            // 4. 부드럽게 이동
            Vector3 currentPosition = transform.position;
            Vector3 targetPosition = new Vector3(targetX, currentPosition.y, currentPosition.z);

            Vector3 smoothedPosition = Vector3.MoveTowards(currentPosition, targetPosition, smoothSpeed * Time.deltaTime);

            // Rigidbody2D로 이동
            ApplyMovement(smoothedPosition);
        }
    }

    /// <summary>
    /// 실제 위치 적용 (Rigidbody2D 또는 Transform)
    /// </summary>
    private void ApplyMovement(Vector3 newPosition)
    {
        if (_rb != null && _rb.isKinematic)
        {
            _rb.MovePosition(newPosition);
        }
        else
        {
            transform.position = newPosition;
        }
    }

    #endregion

    /// <summary>
    /// Inspector 없이 모든 참조 자동 설정
    /// </summary>
    private void AutoInitializeReferences()
    {
        // 1. leftEnd/rightEnd 자동 탐색
        if (leftEnd == null)
        {
            GameObject leftObj = GameObject.Find("LeftEnd");
            if (leftObj != null)
            {
                leftEnd = leftObj.transform;
                GameLogger.Info("PhysicsPlank", "LeftEnd 자동 탐색 완료");
            }
        }

        if (rightEnd == null)
        {
            GameObject rightObj = GameObject.Find("RightEnd");
            if (rightObj != null)
            {
                rightEnd = rightObj.transform;
                GameLogger.Info("PhysicsPlank", "RightEnd 자동 탐색 완료");
            }
        }

        // 2. Camera 자동 설정
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera != null)
            {
                GameLogger.Info("PhysicsPlank", "Camera.main 자동 설정 완료");
            }
        }

        // ✅ InputManager는 PlankManager가 관리 (PhysicsPlank는 이동만 담당)
    }

    // ✅ 네트워크 생명주기
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // ✅ 디버그: IsOwner 상태 확인
        GameLogger.Warning("PhysicsPlank", $"[DEBUG] OnNetworkSpawn: name={gameObject.name}, OwnerClientId={OwnerClientId}, LocalClientId={NetworkManager.LocalClientId}, IsOwner={IsOwner}, IsServer={IsServer}");

        // ✅ 경계 동기화: Client에서 NetworkVariable 값으로 경계 설정
        SetupBoundariesFromNetwork();
        _syncedLeftBoundX.OnValueChanged += OnBoundaryChanged;
        _syncedRightBoundX.OnValueChanged += OnBoundaryChanged;

        // ✅ 위치 동기화: NetworkVariable + ServerRpc 수동 동기화
        _syncedPosition.OnValueChanged += OnSyncedPositionChanged;

        // Server: 초기 위치 설정
        if (IsServer)
        {
            _syncedPosition.Value = transform.position;
            GameLogger.Info("PhysicsPlank", $"[Server] 초기 위치 동기화: {transform.position}");
        }

        if (IsOwner)
        {
            // ✅ Owner: 직접 Input.GetAxisRaw() 사용 (ActionBus 대신)
            // ParrelSync 환경에서 포커스된 에디터만 입력 처리하기 위함
            GameLogger.Success("PhysicsPlank", $"[Player {OwnerClientId}] Plank Owner 초기화 완료 (수동 NetworkVariable 동기화)");
        }
        else
        {
            // Non-Owner: NetworkVariable로 위치 보간
            GameLogger.Info("PhysicsPlank", $"[Non-Owner] Plank 위치는 NetworkVariable로 동기화: {gameObject.name}");
        }
    }

    /// <summary>
    /// NetworkVariable 위치 변경 콜백 (Non-Owner용)
    /// </summary>
    private void OnSyncedPositionChanged(Vector3 previousValue, Vector3 newValue)
    {
        // Non-Owner만 처리 (Owner는 직접 위치 설정)
        if (!IsOwner)
        {
            GameLogger.DevLog("PhysicsPlank", $"[Non-Owner] 위치 동기화 수신: {previousValue} → {newValue}");
        }
    }

    /// <summary>
    /// NetworkVariable에서 경계 값을 읽어서 경계 오브젝트 설정
    /// </summary>
    private void SetupBoundariesFromNetwork()
    {
        // 플레이어별 경계 오브젝트 이름
        string leftName = $"LeftEnd_Player{OwnerClientId}";
        string rightName = $"RightEnd_Player{OwnerClientId}";

        // 기존 경계 오브젝트 찾기 또는 생성
        GameObject leftObj = GameObject.Find(leftName);
        GameObject rightObj = GameObject.Find(rightName);

        if (leftObj == null)
        {
            leftObj = new GameObject(leftName);
            leftObj.transform.position = new Vector3(_syncedLeftBoundX.Value, transform.position.y, 0);
            GameLogger.Info("PhysicsPlank", $"[Client] 경계 생성: {leftName} at x={_syncedLeftBoundX.Value}");
        }

        if (rightObj == null)
        {
            rightObj = new GameObject(rightName);
            rightObj.transform.position = new Vector3(_syncedRightBoundX.Value, transform.position.y, 0);
            GameLogger.Info("PhysicsPlank", $"[Client] 경계 생성: {rightName} at x={_syncedRightBoundX.Value}");
        }

        leftEnd = leftObj.transform;
        rightEnd = rightObj.transform;

        GameLogger.Success("PhysicsPlank", $"[Player {OwnerClientId}] 경계 설정 완료: Left={_syncedLeftBoundX.Value}, Right={_syncedRightBoundX.Value}");
    }

    private void OnBoundaryChanged(float previousValue, float newValue)
    {
        // 경계 값 변경 시 재설정
        SetupBoundariesFromNetwork();
    }

    /// <summary>
    /// Server에서 경계 값 설정 (BrickGameMultiplayerSpawner에서 호출)
    /// </summary>
    public void SetBoundaries(float leftX, float rightX)
    {
        if (!IsServer)
        {
            GameLogger.Warning("PhysicsPlank", "SetBoundaries는 Server에서만 호출 가능!");
            return;
        }

        _syncedLeftBoundX.Value = leftX;
        _syncedRightBoundX.Value = rightX;

        GameLogger.Success("PhysicsPlank", $"[Server] 경계 설정: Left={leftX}, Right={rightX}");
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        // 경계 콜백 해제
        _syncedLeftBoundX.OnValueChanged -= OnBoundaryChanged;
        _syncedRightBoundX.OnValueChanged -= OnBoundaryChanged;

        // 위치 동기화 콜백 해제
        _syncedPosition.OnValueChanged -= OnSyncedPositionChanged;
    }

    private void Update()
    {
        // ✅ 멀티플레이어 모드
        if (IsSpawned)
        {
            if (IsOwner)
            {
                // ✅ 포커스 체크: ParrelSync 환경에서 다른 에디터 입력 무시
                if (Application.isFocused)
                {
                    // ✅ Owner: 직접 Input 처리
                    float horizontal = Input.GetAxisRaw("Horizontal");
                    if (Mathf.Abs(horizontal) > 0.01f)
                    {
                        MoveByKeyboard(horizontal, Time.deltaTime);
                    }
                }

                // ✅ Owner: 위치를 ServerRpc로 Server에 전송
                SyncPositionToServer();
            }
            else
            {
                // ✅ Non-Owner: NetworkVariable 값으로 보간
                InterpolateToSyncedPosition();
            }
        }
        // ✅ 싱글플레이어 모드: PlankManager.UpdateMovement()가 입력 처리
    }

    #region 위치 동기화 (수동 NetworkVariable + ServerRpc)

    /// <summary>
    /// Owner가 자신의 위치를 Server에 동기화
    /// </summary>
    private void SyncPositionToServer()
    {
        // 위치 변경이 있을 때만 전송 (대역폭 절약)
        if (Vector3.Distance(transform.position, _syncedPosition.Value) > 0.01f)
        {
            UpdatePositionServerRpc(transform.position);
        }
    }

    /// <summary>
    /// ServerRpc: Client → Server로 위치 전송
    /// </summary>
    [ServerRpc]
    private void UpdatePositionServerRpc(Vector3 newPosition)
    {
        // Server에서 NetworkVariable 업데이트 → 모든 Client에 자동 전파
        _syncedPosition.Value = newPosition;

        // ✅ Non-Owner Plank만 위치 업데이트 (Client가 Owner인 Plank)
        // Host가 Owner인 Plank는 이미 직접 이동하고 있으므로 덮어쓰면 안 됨
        if (!IsOwner)
        {
            transform.position = newPosition;
        }

        // ✅ 디버그: 30프레임마다 로그
        if (Time.frameCount % 30 == 0)
        {
            GameLogger.DevLog("PhysicsPlank", $"[ServerRpc] Plank={gameObject.name}, 위치={newPosition}, Owner={OwnerClientId}, IsOwner={IsOwner}");
        }
    }

    /// <summary>
    /// Non-Owner가 NetworkVariable 값으로 위치 보간
    /// </summary>
    private void InterpolateToSyncedPosition()
    {
        Vector3 targetPosition = _syncedPosition.Value;

        // 초기값(0,0,0)이면 무시
        if (targetPosition == Vector3.zero) return;

        // 부드러운 보간
        transform.position = Vector3.Lerp(transform.position, targetPosition, _interpolationSpeed * Time.deltaTime);
    }

    #endregion

    /// <summary>
    /// 플랭크와 공의 충돌 시 튕겨나갈 속도를 계산하여 반환합니다.
    /// </summary>
    /// <param name="ballRb">충돌한 공의 Rigidbody2D</param>
    /// <param name="collision">충돌 정보</param>
    /// <returns>계산된 반사 속도 벡터</returns>
    public Vector2 CalculateBounceVelocity(Rigidbody2D ballRb, Collision2D collision)
    {
            if (ballRb == null) return Vector2.zero; // 공 Rigidbody 없으면 처리 불가

            Vector2 hitPoint = collision.contacts[0].point;
            Transform plankTransform = collision.transform; // 플랭크 자신의 Transform
            Collider2D plankCollider = collision.collider; // 플랭크 자신의 Collider

            float xOffset = hitPoint.x - plankTransform.position.x;
            float normalizedOffset = xOffset / (plankCollider.bounds.size.x / 2f);
            normalizedOffset = Mathf.Clamp(normalizedOffset, -1f, 1f);

            float bounceAngle = normalizedOffset * 75f; // 최대 반사각 (75도)
            float bounceAngleRad = bounceAngle * Mathf.Deg2Rad;
            Vector2 bounceDirection = new Vector2(Mathf.Sin(bounceAngleRad), Mathf.Cos(bounceAngleRad)).normalized;

            // 공의 현재 속력 사용
            float currentSpeed = ballRb.linearVelocity.magnitude;
            float targetSpeed = currentSpeed;

            if (targetSpeed < 5f) targetSpeed = 10f; // 최소 속도 보정

            Vector2 bounceVelocity = bounceDirection * targetSpeed;
            // Debug.Log($"[PhysicsPlank] Calculated Bounce: Offset={normalizedOffset:F2}, Angle={bounceAngle:F1}, Dir={bounceDirection}, Speed={targetSpeed:F2}");

            return bounceVelocity;
    }

    // ✅ 입력 처리 로직 제거됨
    // - PlankManager.UpdateMovement()에서 통합 처리
    // - 코드 중복 제거, 단일 책임 원칙 준수

    // 기존 PlankBallCollision 메서드는 CalculateBounceVelocity로 대체되었으므로 제거 또는 주석 처리
    // public void PlankBallCollision(Collision2D collision)
    // {
    //     // ... 기존 코드 ...
    // }
}

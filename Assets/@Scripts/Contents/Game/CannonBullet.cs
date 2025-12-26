using UnityEngine;
using Unity.Netcode;

/// <summary>
/// 네트워크 동기화 총알 (땅따먹기 모드)
///
/// ✅ NetworkBehaviour 기반:
/// - SERVER에서 Spawn() 후 모든 CLIENT에 동기화
/// - direction, speed, ownerPlayerID를 NetworkVariable로 동기화
/// - 충돌 처리는 SERVER에서만 수행
/// </summary>
public class CannonBullet : NetworkBehaviour
{
    [Header("총알 설정")]
    public float lifetime = 5f;
    public int damage = 10;
    public GameObject hitEffect;

    // ✅ NetworkVariable로 동기화되는 값들
    private NetworkVariable<Vector3> _networkDirection = new NetworkVariable<Vector3>(
        Vector3.zero,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private NetworkVariable<float> _networkSpeed = new NetworkVariable<float>(
        20f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private NetworkVariable<int> _networkOwnerPlayerID = new NetworkVariable<int>(
        -1,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private NetworkVariable<Color> _networkOwnerColor = new NetworkVariable<Color>(
        Color.white,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private NetworkVariable<bool> _networkIsActive = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    // ✅ 스폰 위치 동기화 (SynchronizeTransform 대신 사용)
    private NetworkVariable<Vector3> _networkSpawnPosition = new NetworkVariable<Vector3>(
        Vector3.zero,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    // 로컬 캐시 (성능 최적화)
    [HideInInspector] public Cannon ownerCannon;
    [HideInInspector] public Color ownerColor;
    [HideInInspector] public int ownerPlayerID = -1;

    private Vector3 direction;
    private float speed = 20f;
    private bool isActive = false;
    private bool isDestroying = false;
    private float spawnTime;

    // ✅ Rigidbody 캐시 (MovePosition 사용을 위해)
    private Rigidbody _rigidbody;
    private int _moveLogCount = 0;  // 로그 카운터
    private float _fixedY;  // ✅ Y값 고정 (바닥에 묻히지 않도록)

    private void Awake()
    {
        // 리지드바디 설정 및 캐시
        _rigidbody = GetComponent<Rigidbody>();
        if (_rigidbody != null)
        {
            _rigidbody.useGravity = false;
            // ✅ 비운동학적(Dynamic)으로 설정해야 충돌 감지가 제대로 됨!
            _rigidbody.isKinematic = false;
            // ✅ 충돌 감지 모드: ContinuousDynamic으로 설정하여 고속 이동 시에도 감지
            _rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            // ✅ interpolation으로 부드러운 이동
            _rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            // ✅ 물리 충돌 시 밀리지 않도록 constraints 설정
            _rigidbody.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
            // ✅ drag 설정으로 안정적인 이동
            _rigidbody.linearDamping = 0f;
            _rigidbody.angularDamping = 0f;
        }

        // 콜라이더 설정
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        spawnTime = Time.time;
        isDestroying = false;

        Debug.Log($"<color=cyan>[CannonBullet] OnNetworkSpawn 시작 - IsServer={IsServer}, IsClient={IsClient}, IsSpawned={IsSpawned}</color>");
        Debug.Log($"<color=cyan>[CannonBullet] NetworkVariable 현재값 - isActive={_networkIsActive.Value}, dir={_networkDirection.Value}, speed={_networkSpeed.Value}</color>");

        // ✅ NetworkVariable 변경 콜백 등록
        _networkIsActive.OnValueChanged += OnActiveChanged;
        _networkDirection.OnValueChanged += OnDirectionChanged;
        _networkSpeed.OnValueChanged += OnSpeedChanged;
        _networkOwnerPlayerID.OnValueChanged += OnOwnerChanged;
        _networkOwnerColor.OnValueChanged += OnColorChanged;
        _networkSpawnPosition.OnValueChanged += OnSpawnPositionChanged;

        // ✅ CLIENT: 현재 값으로 초기화 (SERVER가 이미 설정했을 수 있음)
        SyncFromNetworkVariables();

        // ✅ CLIENT: 스폰 위치 즉시 적용 (SynchronizeTransform 대신)
        if (!IsServer && _networkSpawnPosition.Value != Vector3.zero)
        {
            transform.position = _networkSpawnPosition.Value;
            // ✅ CLIENT에서도 Y값 고정 설정
            _fixedY = _networkSpawnPosition.Value.y;
            Debug.Log($"<color=green>[CannonBullet] CLIENT 스폰 위치 적용: {_networkSpawnPosition.Value}, fixedY={_fixedY}</color>");
        }

        Debug.Log($"<color=cyan>[CannonBullet] OnNetworkSpawn 완료 - 로컬 isActive={isActive}, dir={direction}</color>");
    }

    public override void OnNetworkDespawn()
    {
        // 콜백 해제
        _networkIsActive.OnValueChanged -= OnActiveChanged;
        _networkDirection.OnValueChanged -= OnDirectionChanged;
        _networkSpeed.OnValueChanged -= OnSpeedChanged;
        _networkOwnerPlayerID.OnValueChanged -= OnOwnerChanged;
        _networkOwnerColor.OnValueChanged -= OnColorChanged;
        _networkSpawnPosition.OnValueChanged -= OnSpawnPositionChanged;

        base.OnNetworkDespawn();
    }

    /// <summary>
    /// NetworkVariable 값을 로컬 변수로 동기화
    /// </summary>
    private void SyncFromNetworkVariables()
    {
        direction = _networkDirection.Value;
        speed = _networkSpeed.Value;
        ownerPlayerID = _networkOwnerPlayerID.Value;
        ownerColor = _networkOwnerColor.Value;
        isActive = _networkIsActive.Value;

        // 색상 적용
        ApplyColor(ownerColor);
    }

    #region NetworkVariable Callbacks
    private void OnActiveChanged(bool oldValue, bool newValue)
    {
        isActive = newValue;
        Debug.Log($"<color=yellow>[CannonBullet] isActive 변경: {oldValue} → {newValue}</color>");
    }

    private void OnDirectionChanged(Vector3 oldValue, Vector3 newValue)
    {
        direction = newValue;
    }

    private void OnSpeedChanged(float oldValue, float newValue)
    {
        speed = newValue;
    }

    private void OnOwnerChanged(int oldValue, int newValue)
    {
        ownerPlayerID = newValue;
    }

    private void OnColorChanged(Color oldValue, Color newValue)
    {
        ownerColor = newValue;
        ApplyColor(newValue);
    }

    private void OnSpawnPositionChanged(Vector3 oldValue, Vector3 newValue)
    {
        // CLIENT에서만 위치 적용 (SERVER는 이미 올바른 위치에 있음)
        if (!IsServer && newValue != Vector3.zero)
        {
            transform.position = newValue;
            // ✅ CLIENT에서도 Y값 고정 설정
            _fixedY = newValue.y;
            Debug.Log($"<color=green>[CannonBullet] 위치 동기화: {newValue}, fixedY={_fixedY}</color>");
        }
    }
    #endregion

    /// <summary>
    /// 총알 발사 (SERVER에서 호출)
    /// </summary>
    /// <param name="dir">발사 방향</param>
    /// <param name="spd">속도 (0이면 기본값 사용)</param>
    /// <param name="fixedYValue">Y값 고정 (-1이면 현재 위치 사용)</param>
    public void Fire(Vector3 dir, float spd = 0, float fixedYValue = -1f)
    {
        Debug.Log($"<color=magenta>[CannonBullet] Fire() 시작 - IsServer={IsServer}, pos={transform.position}, dir={dir}, spd={spd}, fixedY={fixedYValue}</color>");

        // ✅ Y값 고정 저장 (명시적으로 전달받거나 현재 위치 사용)
        if (fixedYValue >= 0)
        {
            _fixedY = fixedYValue;
        }
        else
        {
            _fixedY = transform.position.y;
        }

        // ✅ Y=0은 블록과 동일한 높이로 정상값 (보정 제거)

        // ✅ XZ 평면에서만 이동하도록 방향 벡터 조정
        Vector3 normalizedDir = new Vector3(dir.x, 0, dir.z).normalized;

        // ✅ NetworkVariable 설정 (Spawn 전에도 설정 가능 - Spawn 시 Client로 동기화됨)
        // 주의: IsServer는 Spawn 전에 false일 수 있으므로, NetworkManager 직접 확인
        bool isServerContext = NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer;

        if (isServerContext)
        {
            _networkDirection.Value = normalizedDir;
            if (spd > 0) _networkSpeed.Value = spd;
            _networkIsActive.Value = true;
            // ✅ 스폰 위치 동기화 (CLIENT가 올바른 위치에서 시작하도록)
            Vector3 spawnPos = transform.position;
            spawnPos.y = _fixedY;  // Y값 보정
            _networkSpawnPosition.Value = spawnPos;
            Debug.Log($"<color=green>[CannonBullet] NetworkVariable 설정 완료 - dir={_networkDirection.Value}, speed={_networkSpeed.Value}, pos={_networkSpawnPosition.Value}, fixedY={_fixedY}</color>");
        }
        else
        {
            Debug.LogWarning($"<color=red>[CannonBullet] Fire()가 Server가 아닌 곳에서 호출됨!</color>");
        }

        // ✅ 로컬 값도 즉시 설정 (SERVER에서 바로 이동 시작)
        direction = normalizedDir;
        if (spd > 0) speed = spd;
        isActive = true;
    }

    /// <summary>
    /// 소유자 설정 (SERVER에서 호출)
    /// </summary>
    public void SetOwner(Cannon cannon, Color color, int playerID = -1)
    {
        ownerCannon = cannon;

        // ✅ NetworkVariable 설정 (Spawn 전에도 설정 가능)
        bool isServerContext = NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer;
        if (isServerContext)
        {
            _networkOwnerPlayerID.Value = playerID;
            _networkOwnerColor.Value = color;
        }

        // 로컬 값도 설정
        ownerColor = color;
        ownerPlayerID = playerID;

        Debug.Log($"<color=yellow>[CannonBullet] SetOwner - ID={playerID}, isServerContext={isServerContext}</color>");

        ApplyColor(color);
    }

    /// <summary>
    /// 색상 적용
    /// </summary>
    private void ApplyColor(Color color)
    {
        Renderer renderer = GetComponent<Renderer>();
        if (renderer == null)
        {
            renderer = GetComponentInChildren<Renderer>();
        }

        if (renderer != null)
        {
            renderer.material.color = color;
        }
    }

    /// <summary>
    /// ✅ Update()에서 velocity 설정 → Rigidbody가 이동하여 충돌 감지 정확!
    /// </summary>
    private void Update()
    {
        // ✅ lifetime 체크 (SERVER에서만 Despawn)
        if (IsServer && !isDestroying && Time.time - spawnTime > lifetime)
        {
            DestroyBullet();
            return;
        }

        // ✅ NetworkVariable 값 또는 로컬 값 사용 (둘 중 하나라도 활성화면 이동)
        bool netActive = _networkIsActive.Value;
        Vector3 netDir = _networkDirection.Value;
        float netSpeed = _networkSpeed.Value;

        // ✅ 로컬 값도 체크 (NetworkVariable 동기화 전에도 이동하도록)
        bool useLocal = isActive && direction.sqrMagnitude > 0.01f;
        bool useNetwork = netActive && netDir.sqrMagnitude > 0.01f;

        if (isDestroying) return;

        // ✅ 둘 다 비활성이면 정지
        if (!useLocal && !useNetwork)
        {
            if (_rigidbody != null) _rigidbody.linearVelocity = Vector3.zero;
            return;
        }

        // ✅ 사용할 방향과 속도 결정 (로컬 우선, 없으면 네트워크)
        Vector3 moveDir = useLocal ? direction : netDir;
        float moveSpeed = useLocal ? speed : netSpeed;

        // ✅ direction이 zero면 정지
        if (moveDir.sqrMagnitude < 0.01f)
        {
            if (_rigidbody != null) _rigidbody.linearVelocity = Vector3.zero;
            return;
        }

        // ✅ Rigidbody velocity 설정 → 물리 엔진이 이동 + 충돌 감지!
        if (_rigidbody != null)
        {
            // XZ 평면에서만 이동 (Y는 FreezePositionY로 고정됨)
            Vector3 velocity = new Vector3(moveDir.x * moveSpeed, 0, moveDir.z * moveSpeed);
            _rigidbody.linearVelocity = velocity;
        }

        // 처음 3프레임만 로그
        if (_moveLogCount < 3)
        {
            Debug.Log($"<color=cyan>[CannonBullet] Update 이동: pos={transform.position}, dir={moveDir}, speed={moveSpeed}, velocity={_rigidbody?.linearVelocity}</color>");
            _moveLogCount++;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // ✅ 모든 충돌 로그 (SERVER/CLIENT 상관없이)
        Debug.Log($"<color=magenta>[CannonBullet] OnTriggerEnter 호출됨! other={other.name}, IsServer={IsServer}, isDestroying={isDestroying}</color>");

        if (isDestroying) return;

        // ✅ SERVER에서만 충돌 처리 (블록 소유권 변경은 서버 권한)
        if (!IsServer)
        {
            Debug.Log($"<color=gray>[CannonBullet] CLIENT에서 충돌 감지 - 처리는 SERVER에서</color>");
            return;
        }

        HandleHit(other.gameObject);
    }

    private void HandleHit(GameObject hitObject)
    {
        if (isDestroying || hitObject == null) return;

        // ✅ 충돌 디버그 로그 (무엇과 충돌했는지 확인)
        Debug.Log($"<color=yellow>[CannonBullet] 충돌 감지! hitObject={hitObject.name}, tag={hitObject.tag}, " +
                  $"hasParent={hitObject.transform.parent != null}, " +
                  $"parentName={(hitObject.transform.parent != null ? hitObject.transform.parent.name : "none")}</color>");

        // 다른 총알과 충돌 무시 (파괴하지 않음 - 통과!)
        if (hitObject.GetComponent<CannonBullet>() != null)
        {
            return; // ✅ 로그도 제거 (스팸 방지)
        }

        // 벽과 충돌 시 파괴
        if (hitObject.CompareTag("Wall"))
        {
            DestroyBullet();
            return;
        }

        // ★★★ 대포 충돌 처리 (게임 오버 조건) ★★★
        Cannon hitCannon = hitObject.GetComponent<Cannon>();
        if (hitCannon == null)
        {
            hitCannon = hitObject.GetComponentInParent<Cannon>();
        }

        if (hitCannon != null)
        {
            // 자신의 대포면 통과
            if (hitCannon.playerID == ownerPlayerID)
            {
                return;
            }

            // 상대방 대포 명중!
            Debug.Log($"<color=red>[CannonBullet] ★★★ 상대방 대포 명중! Player {hitCannon.playerID} ★★★</color>");
            hitCannon.TakeDamage(damage);
            DestroyBullet();
            return;
        }

        // 그리드 블록과 충돌 처리
        bool isGridBlock = hitObject.CompareTag("GridBlock");
        bool hasGridGenerator = hitObject.transform.parent != null &&
                                hitObject.transform.parent.GetComponent<IsometricGridGenerator>() != null;

        Debug.Log($"<color=orange>[CannonBullet] GridBlock 체크: tag={hitObject.tag}, isGridBlock={isGridBlock}, hasGridGenerator={hasGridGenerator}</color>");

        if (isGridBlock || hasGridGenerator)
        {
            Debug.Log($"<color=green>[CannonBullet] ✅ GridBlock 조건 충족! 소유권 변경 처리 시작</color>");

            if (IsometricGridGenerator.Instance == null)
            {
                Debug.LogError("[CannonBullet] ❌ IsometricGridGenerator.Instance가 null!");
                DestroyBullet();
                return;
            }

            int blockOwnerID = IsometricGridGenerator.Instance.GetBlockOwner(hitObject);

            // ✅ 상세 디버그 로그
            Debug.Log($"<color=yellow>[CannonBullet] 블록 충돌 분석:</color>\n" +
                      $"  - hitObject.name: {hitObject.name}\n" +
                      $"  - blockOwnerID: {blockOwnerID}\n" +
                      $"  - 총알 ownerPlayerID: {ownerPlayerID}\n" +
                      $"  - 총알 ownerColor: {ownerColor}\n" +
                      $"  - 총알 위치: {transform.position}\n" +
                      $"  - 블록 위치: {hitObject.transform.position}");

            // 자신의 블록이면 통과
            if (blockOwnerID == ownerPlayerID && ownerPlayerID >= 0)
            {
                Debug.Log($"<color=gray>[CannonBullet] 자신의 블록 (소유자={blockOwnerID}) - 통과</color>");
                return;
            }

            // ✅ 중립(-1) 또는 상대방 블록인 경우 소유권 변경
            Debug.Log($"<color=lime>[CannonBullet] ★ 상대방/중립 블록 점령! {hitObject.name}: 소유자 {blockOwnerID} → {ownerPlayerID}</color>");

            // ✅ ClientRpc로 모든 클라이언트에 동기화
            var spawner = FindObjectOfType<BrickGameMultiplayerSpawner>();
            if (spawner != null)
            {
                spawner.ChangeBlockOwnerClientRpc(
                    hitObject.name,
                    ownerPlayerID,
                    ownerColor.r,
                    ownerColor.g,
                    ownerColor.b
                );
            }
            else
            {
                // Fallback: 로컬에서만 변경
                Renderer blockRenderer = hitObject.GetComponent<Renderer>();
                if (blockRenderer != null)
                {
                    blockRenderer.material.color = ownerColor;
                }
                IsometricGridGenerator.Instance.SetBlockOwner(hitObject, ownerPlayerID, ownerColor);
            }

            // 충돌 효과 생성
            if (hitEffect != null)
            {
                Instantiate(hitEffect, transform.position, Quaternion.identity);
            }

            DestroyBullet();
        }
        else
        {
            // ✅ GridBlock이 아닌 다른 물체와 충돌 - 무시 (통과)
            Debug.Log($"<color=gray>[CannonBullet] GridBlock 아님 - 통과 (tag={hitObject.tag})</color>");
        }
    }

    private void DestroyBullet()
    {
        if (isDestroying) return;

        // ✅ SERVER에서만 Despawn 가능
        if (!IsServer) return;

        isDestroying = true;
        _networkIsActive.Value = false;

        // 이펙트 생성
        if (hitEffect != null)
        {
            Instantiate(hitEffect, transform.position, Quaternion.identity);
        }

        // ✅ 풀링 사용: Despawn(false) + 풀로 반환
        if (IsSpawned)
        {
            if (NetworkBulletPool.Instance != null)
            {
                // ✅ 풀로 반환 (Despawn + ReturnToPool)
                NetworkBulletPool.Instance.DespawnAndReturn(NetworkObject);
            }
            else
            {
                // ✅ 폴백: 풀이 없으면 파괴
                NetworkObject.Despawn(true);
            }
        }
    }

    /// <summary>
    /// 풀에서 재사용 시 상태 리셋
    /// </summary>
    public void ResetForReuse()
    {
        isDestroying = false;
        isActive = false;
        direction = Vector3.zero;
        ownerPlayerID = -1;
        ownerCannon = null;
        ownerColor = Color.white;
        spawnTime = Time.time;
        _moveLogCount = 0;
        _fixedY = 0f;  // Y값 고정 리셋

        // ✅ Rigidbody 속도 리셋
        if (_rigidbody != null)
        {
            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
        }

        // ✅ SERVER 컨텍스트에서만 NetworkVariable 리셋
        bool isServerContext = NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer;
        if (isServerContext)
        {
            _networkIsActive.Value = false;
            _networkDirection.Value = Vector3.zero;
            _networkSpeed.Value = 20f;
            _networkOwnerPlayerID.Value = -1;
            _networkOwnerColor.Value = Color.white;
            _networkSpawnPosition.Value = Vector3.zero;
        }

        Debug.Log($"<color=green>[CannonBullet] 상태 리셋 완료 (풀 재사용 준비)</color>");
    }
}

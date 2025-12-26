using UnityEngine;
using System.Collections.Generic; // List를 사용하기 위해 추가
using static Define; // MATCHMAKING_PLAYER_COUNT 사용

public class IsometricGridGenerator : MonoBehaviour, IMap
{
    [Header("Grid Settings")]
    public GameObject cubePrefab;
    public GameObject wallPrefab; // New wall prefab
    public int gridSizeX = 20;
    public int gridSizeY = 20;
    public float aspectRatio = 1.5f; // Adjust this value to make the grid appear square

    public float cubeSize = 1.0f;
    public float spacing = 0.05f;
    public float gridHeight = 0.2f;
    public float wallHeight = 1.0f; // Height for walls
    
    [Header("Player Settings")] // 플레이어 수 설정 추가
    public int playerCount = Define.MATCHMAKING_PLAYER_COUNT; // Define에서 가져옴 (현재 2명)

    [Header("Material Settings")] // 머티리얼 설정을 위한 헤더 추가
    public Material borderMaterial; // BorderMaterial 참조 변수 추가

    [Header("Config (Optional)")]
    [Tooltip("ScriptableObject로 색상 관리. 비워두면 기존 하드코딩 색상 사용")]
    [SerializeField] private PlayerColorConfig colorConfig;

    [Tooltip("ScriptableObject로 그리드 설정 관리. 비워두면 Inspector 값 사용")]
    [SerializeField] private GridConfig gridConfig;

    [Header("Turret Settings")] // 터렛 설정 추가
    public GameObject standardTurretPrefab; // Standard Turret 프리팹 참조
    public float turretHeightOffset = 0f; // 터렛 배치 높이 오프셋 (0 = 바닥)

    [Header("Bullet Settings")] // 총알 설정 추가
    public GameObject bulletPrefab; // 총알 프리팹 (Cannon에서 사용)
    public float bulletSpeed = 30f; // ✅ 빠른 총알 속도!
    public float bulletFireInterval = 0.015f; // ✅ 초당 ~66발 연사! (다다다다다닥)

    // 생성된 캐논 리스트
    private List<Cannon> _cannons = new List<Cannon>();
    
    // 생성된 캐논 리스트에 접근할 수 있는 프로퍼티
    public List<Cannon> Cannons => _cannons;

    // 블록 소유권 관리를 위한 변수 추가
    private Dictionary<GameObject, int> _blockOwners = new Dictionary<GameObject, int>();
    private Dictionary<int, Color> _playerColors = new Dictionary<int, Color>();

    public static IsometricGridGenerator Instance { get; private set; }

    void Awake()
    {
        // 싱글톤 인스턴스 설정
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("IsometricGridGenerator 인스턴스가 이미 존재합니다. 새로 생성된 인스턴스를 파괴합니다.");
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }

        if (borderMaterial == null)
        {
            Debug.LogError("Border Material이 Inspector에서 할당되지 않았습니다!");
            enabled = false;
            return;
        }

        // ✅ bulletPrefab이 없으면 런타임에 생성
        if (bulletPrefab == null)
        {
            CreateBulletPrefab();
        }
    }

    /// <summary>
    /// 총알 프리팹 런타임 생성 (프리팹 미설정 시)
    /// </summary>
    private void CreateBulletPrefab()
    {
        Debug.Log("<color=yellow>[IsometricGridGenerator] bulletPrefab 런타임 생성 중...</color>");

        // ✅ 프리팹용 오브젝트 생성 (활성화 상태로 - NetworkObject Spawn 시 필요)
        bulletPrefab = new GameObject("CannonBulletPrefab");

        // ✅ 씬에서 숨기기 - Hierarchy에 안 보이고 저장 안 됨
        bulletPrefab.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSave;

        // ✅ 화면 밖으로 이동 (프리팹 템플릿이므로 보일 필요 없음)
        bulletPrefab.transform.position = new Vector3(-9999, -9999, -9999);

        // CannonBullet 스크립트 추가
        bulletPrefab.AddComponent<CannonBullet>();

        // ✅ Rigidbody (물리 이동 + 충돌 감지용) - Dynamic으로 설정!
        var rb = bulletPrefab.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = false;  // ✅ Dynamic Rigidbody (충돌 감지 정확!)
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;  // 고속 충돌 감지
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
        rb.linearDamping = 0f;
        rb.angularDamping = 0f;

        // Collider (충돌 감지)
        var collider = bulletPrefab.AddComponent<SphereCollider>();
        collider.radius = 0.3f;
        collider.isTrigger = true;

        // 시각적 표현 (Sphere)
        var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.transform.SetParent(bulletPrefab.transform);
        sphere.transform.localPosition = Vector3.zero;
        sphere.transform.localScale = Vector3.one * 0.5f;

        // Sphere의 콜라이더 제거 (중복 방지)
        var sphereCollider = sphere.GetComponent<Collider>();
        if (sphereCollider != null) Destroy(sphereCollider);

        // 노란색 머티리얼
        var renderer = sphere.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material = new Material(Shader.Find("Standard"));
            renderer.material.color = Color.yellow;
        }

        Debug.Log("<color=green>[IsometricGridGenerator] bulletPrefab 런타임 생성 완료!</color>");
    }

    void Start()
    {
        // ✅ Managers.Map에 현재 맵 등록
        Managers.Map?.SetCurrentMap(this);

        // borderMaterial이 할당되었는지 확인
        if (borderMaterial == null)
        {
            Debug.LogError("Border Material이 Inspector에서 할당되지 않았습니다!");
            enabled = false; // 오류 발생 시 스크립트 비활성화
            return;
        }

        // ✅ GridConfig가 있으면 값 적용 (Optional)
        ApplyGridConfig();

        // ✅ 플레이어 수를 Define에서 강제로 가져오기 (Inspector 값 무시)
        // 참고: 그리드는 시각적 요소이므로 HOST/CLIENT 모두 로컬에서 생성
        //       블록 소유권 변경만 네트워크로 동기화됨
        playerCount = Define.MATCHMAKING_PLAYER_COUNT;
        Debug.Log($"<color=cyan>[IsometricGridGenerator] playerCount = {playerCount} (Define.MATCHMAKING_PLAYER_COUNT에서 설정)</color>");

        CreateGrid();

        // ✅ 맵 컴포넌트 등록 (BOMB, HARVEST 등)
        RegisterMapComponents();

        // ✅ GameRuleManager.OnFired 이벤트 구독 (CannonBulletRule과 연결)
        SubscribeToGameRuleEvents();

        // ✅ CLIENT: SERVER가 Spawn한 캐논들을 찾기 위해 지연 호출
        var netManager = Unity.Netcode.NetworkManager.Singleton;
        if (netManager != null && netManager.IsClient && !netManager.IsServer)
        {
            StartCoroutine(DelayedRefreshCannons());
        }
    }

    /// <summary>
    /// GameRuleManager 이벤트 구독 (CannonBulletRule.OnFired 연결)
    /// </summary>
    private void SubscribeToGameRuleEvents()
    {
        if (Managers.Game?.Rules != null)
        {
            Managers.Game.Rules.OnFired += HandleRuleFired;
            Debug.Log("<color=green>[IsometricGridGenerator] GameRuleManager.OnFired 구독 완료!</color>");
        }
        else
        {
            Debug.LogWarning("[IsometricGridGenerator] GameRuleManager 없음 - OnFired 구독 실패");
        }
    }

    /// <summary>
    /// CannonBulletRule에서 발사 이벤트 수신 → 실제 대포 발사
    /// </summary>
    private void HandleRuleFired(int bulletCount)
    {
        if (bulletCount <= 0) return;

        // 로컬 플레이어 ID
        int localPlayerId = 0;
        if (Unity.Netcode.NetworkManager.Singleton != null && Unity.Netcode.NetworkManager.Singleton.IsClient)
        {
            localPlayerId = (int)Unity.Netcode.NetworkManager.Singleton.LocalClientId;
        }

        // 해당 플레이어의 대포 찾기
        Cannon myCannon = null;
        foreach (var cannon in _cannons)
        {
            if (cannon != null && cannon.playerID == localPlayerId)
            {
                myCannon = cannon;
                break;
            }
        }

        if (myCannon == null)
        {
            Debug.LogWarning($"[IsometricGridGenerator] HandleRuleFired - Player {localPlayerId}의 대포 없음");
            return;
        }

        Debug.Log($"<color=yellow>[IsometricGridGenerator] CannonBulletRule → 대포 발사! {bulletCount}발</color>");
        myCannon.Fire(bulletCount);
    }

    void OnDestroy()
    {
        // 이벤트 구독 해제
        if (Managers.Game?.Rules != null)
        {
            Managers.Game.Rules.OnFired -= HandleRuleFired;
        }
    }

    /// <summary>
    /// GridConfig ScriptableObject 값 적용 (Optional)
    /// </summary>
    private void ApplyGridConfig()
    {
        if (gridConfig == null)
        {
            Debug.Log("<color=yellow>[IsometricGridGenerator] gridConfig 미설정 - Inspector 값 사용</color>");
            return;
        }

        gridSizeX = gridConfig.gridSizeX;
        gridSizeY = gridConfig.gridSizeY;
        cubeSize = gridConfig.cubeSize;
        spacing = gridConfig.spacing;
        gridHeight = gridConfig.gridHeight;
        aspectRatio = gridConfig.aspectRatio;
        turretHeightOffset = gridConfig.turretHeightOffset;
        wallHeight = gridConfig.wallHeight;

        Debug.Log($"<color=cyan>[IsometricGridGenerator] gridConfig 적용 - Size({gridSizeX}x{gridSizeY})</color>");
    }

    /// <summary>
    /// 맵 컴포넌트 등록 (BOMB, HARVEST 등)
    /// 각 플레이어에게 BombComponent와 HarvestComponent를 생성하여 등록
    /// </summary>
    private void RegisterMapComponents()
    {
        if (Managers.Map?.Components == null)
        {
            Debug.LogWarning("[IsometricGridGenerator] MapComponentManager 없음 - 컴포넌트 등록 스킵");
            return;
        }

        // 컴포넌트 홀더 GameObject 생성 (정리용)
        var componentHolder = new GameObject("MapComponents");
        componentHolder.transform.SetParent(transform);

        // 각 플레이어에게 컴포넌트 등록
        for (int playerID = 0; playerID < playerCount; playerID++)
        {
            // BombComponent 생성 및 등록
            var bombGO = new GameObject($"BombComponent_P{playerID}");
            bombGO.transform.SetParent(componentHolder.transform);
            var bombComponent = bombGO.AddComponent<BombComponent>();
            Managers.Map.Components.Register(bombComponent, playerID);

            // HarvestComponent 생성 및 등록
            var harvestGO = new GameObject($"HarvestComponent_P{playerID}");
            harvestGO.transform.SetParent(componentHolder.transform);
            var harvestComponent = harvestGO.AddComponent<HarvestComponent>();
            Managers.Map.Components.Register(harvestComponent, playerID);
        }

        Debug.Log($"<color=magenta>[IsometricGridGenerator] 맵 컴포넌트 등록 완료 - {playerCount}명 × 2개 = {Managers.Map.Components.Count}개</color>");
    }

    /// <summary>
    /// CLIENT용: 지연 후 씬에서 캐논 찾기
    /// </summary>
    private System.Collections.IEnumerator DelayedRefreshCannons()
    {
        // SERVER가 캐논을 Spawn할 시간을 주기 위해 대기
        yield return new WaitForSeconds(0.5f);
        RefreshCannonsFromScene();

        // 아직 없으면 추가 대기
        if (_cannons.Count == 0)
        {
            yield return new WaitForSeconds(1.0f);
            RefreshCannonsFromScene();
        }
    }

    void Update()
    {
        // ✅ MapComponentManager Tick 호출 (쿨다운, 충전 등)
        Managers.Map?.Components?.Tick(Time.deltaTime);

        // ✅ 대포 발사는 GameRuleManager.OnFired 이벤트로 처리됨
        //    (Space키 → GameScene → ActionBus → GameRuleManager → CannonBulletRule → OnFired → HandleRuleFired)
        //    Enter키 레거시 처리 제거됨
    }

    public void CreateGrid()
    {
        // Clear existing objects and reset cannons list
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
        _cannons.Clear(); // 캐논 리스트 초기화
        _blockOwners.Clear(); // 블록 소유권 정보 초기화
        
        // *** 플레이어 색상 정의를 먼저 호출 ***
        DefinePlayerColors();
        
        float step = cubeSize + spacing;
        float startX = -(gridSizeX * step) / 2f;
        
        // Adjust the Z dimension by the aspect ratio to make it appear square
        int adjustedGridSizeY = Mathf.RoundToInt(gridSizeY * aspectRatio);
        float startZ = -(adjustedGridSizeY * step) / 2f;
        
        // Create grid tiles
        for (int x = 0; x < gridSizeX; x++)
        {
            for (int y = 0; y < adjustedGridSizeY; y++)
            {
                float posX = startX + x * step + cubeSize/2;
                float posZ = startZ + y * step + cubeSize/2;
                
                Vector3 position = new Vector3(posX, 0, posZ);
                
                GameObject cube;
                if (cubePrefab != null)
                {
                    cube = Instantiate(cubePrefab, position, Quaternion.identity);
                }
                else
                {
                    cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cube.transform.position = position;
                }
                
                // 태그 설정
                cube.tag = "GridBlock";

                // 고유 이름 설정 (ClientRpc에서 블록 찾기 위해 필수!)
                cube.name = $"GridBlock_{x}_{y}";

                cube.transform.localScale = new Vector3(cubeSize, gridHeight, cubeSize);
                cube.transform.parent = this.transform;
                
                // 콜라이더 설정 - 블록 크기에 맞춤
                BoxCollider collider = cube.GetComponent<BoxCollider>();
                if (collider != null)
                {
                    // 콜라이더를 블록 중심에 배치 (시각적으로 자연스러운 충돌)
                    collider.center = Vector3.zero;
                    collider.size = Vector3.one;

                    // ✅ isTrigger = FALSE! (총알이 isTrigger=true이므로 블록은 false여야 OnTriggerEnter 호출됨)
                    // Unity 규칙: 두 Collider가 모두 isTrigger=true면 OnTriggerEnter가 호출되지 않음!
                    collider.isTrigger = false;
                }
                
                Renderer renderer = cube.GetComponent<Renderer>();
                
                // 초기 블록 소유권 설정
                int ownerID = GetInitialOwnerID(x, y, gridSizeX, adjustedGridSizeY);
                
                // 블록 소유권 딕셔너리에 추가
                _blockOwners[cube] = ownerID;
                
                // 디버그: 초기 소유권 할당 확인
                Debug.Log($"<color=#ADD8E6>큐브 생성: ({x},{y}), 계산된 소유자 ID = {ownerID}</color>");
                
                // 렌더러가 있는 경우 소유자 색상 적용
                if (renderer != null)
                {
                    // 소유자 색상 가져오기
                    if (_playerColors.TryGetValue(ownerID, out Color ownerColor))
                    {
                        // 소유자 색상 적용
                        renderer.material.color = ownerColor;
                        Debug.Log($"<color=cyan>큐브 초기화 완료: ({x},{y}), 소유자={ownerID}, 색상={ownerColor}</color>");
                    }
                    else
                    {
                        // 오류: 플레이어 색상이 정의되지 않음 (기본 흰색으로 처리)
                        renderer.material.color = Color.white;
                        Debug.LogError($"<color=red>오류: 플레이어 {ownerID}의 색상이 정의되지 않았습니다!</color>");
                    }
                }
            }
        }

        // --- 터렛 배치 로직 추가 ---
        if (standardTurretPrefab == null)
        {
            Debug.LogError("Standard Turret Prefab이 Inspector에서 할당되지 않았습니다!");
        }
        else
        {
            // 모서리 좌표 계산 (타일 중심 기준)
            float halfCubeSize = cubeSize / 2f;
            float turretYPos = turretHeightOffset; // Y = 0 (기본), Inspector에서 조정 가능

            // 각 모서리 중심 좌표
            Vector3 bottomLeftPos = new Vector3(startX + halfCubeSize, turretYPos, startZ + halfCubeSize);
            Vector3 topRightPos = new Vector3(startX + (gridSizeX - 1) * step + halfCubeSize, turretYPos, startZ + (adjustedGridSizeY - 1) * step + halfCubeSize);
            Vector3 topLeftPos = new Vector3(startX + halfCubeSize, turretYPos, startZ + (adjustedGridSizeY - 1) * step + halfCubeSize);
            Vector3 bottomRightPos = new Vector3(startX + (gridSizeX - 1) * step + halfCubeSize, turretYPos, startZ + halfCubeSize);

            // 플레이어 색상 정의 (각 플레이어의 고유 색상)
            DefinePlayerColors();

            if (playerCount == 2)
            {
                // *** 플레이어 0과 1의 터렛 위치 교체 ***
                // 이전: 좌상단(0), 우하단(1)
                // 변경: 우하단(0), 좌상단(1)
                InstantiateTurret(standardTurretPrefab, bottomRightPos, transform, 0); // 우하단 - 플레이어 0번
                InstantiateTurret(standardTurretPrefab, topLeftPos, transform, 1); // 좌상단 - 플레이어 1번
            }
            else // 4인 또는 그 외
            {
                // 네 모서리에 모두 배치 (4인용은 그대로 유지)
                InstantiateTurret(standardTurretPrefab, bottomLeftPos, transform, 0); // 좌하단 - 플레이어 0번
                InstantiateTurret(standardTurretPrefab, topRightPos, transform, 1); // 우상단 - 플레이어 1번
                InstantiateTurret(standardTurretPrefab, topLeftPos, transform, 2); // 좌상단 - 플레이어 2번
                InstantiateTurret(standardTurretPrefab, bottomRightPos, transform, 3); // 우하단 - 플레이어 3번
            }
        }
        
        // Add walls around the grid - update this to use adjustedGridSizeY
        CreateWalls(startX, startZ, step, adjustedGridSizeY);
    }
        
    void CreateWalls(float startX, float startZ, float step, int adjustedGridSizeY)
    {
        // Calculate the full grid dimensions
        float gridWidth = gridSizeX * step;
        float gridDepth = adjustedGridSizeY * step;
        
        // Create the four walls
        // Top wall (Z+)
        CreateWallRow(startX, startZ + gridDepth, gridWidth, true);
        
        // Bottom wall (Z-)
        CreateWallRow(startX, startZ - step, gridWidth, true);
        
        // Left wall (X-)
        CreateWallRow(startX - step, startZ, gridDepth, false);
        
        // Right wall (X+)
        CreateWallRow(startX + gridWidth, startZ, gridDepth, false);
        
        // Add corner pieces
        CreateCornerPiece(startX - step, startZ - step);
        CreateCornerPiece(startX + gridWidth, startZ - step);
        CreateCornerPiece(startX - step, startZ + gridDepth);
        CreateCornerPiece(startX + gridWidth, startZ + gridDepth);
    }
    void CreateCornerPiece(float x, float z)
    {
        Vector3 position = new Vector3(x + cubeSize/2, wallHeight/2, z + cubeSize/2);
        
        GameObject corner;
        if (wallPrefab != null)
        {
            corner = Instantiate(wallPrefab, position, Quaternion.identity);
        }
        else
        {
            corner = GameObject.CreatePrimitive(PrimitiveType.Cube);
            corner.transform.position = position;
        }
        
        // Wall 태그 설정
        corner.tag = "Wall";
        
        corner.transform.localScale = new Vector3(cubeSize, wallHeight, cubeSize);
        corner.transform.parent = this.transform;
        
        // Set wall material
        Renderer renderer = corner.GetComponent<Renderer>();
        if (renderer != null && borderMaterial != null) // borderMaterial이 null이 아닌지 확인
        {
            // renderer.material.color = wallColor; // 이전 색상 설정 제거
            renderer.material = borderMaterial; // 머티리얼 할당
        }
    }
    void CreateWallRow(float startX, float startZ, float length, bool isHorizontal)
    {
        float step = cubeSize + spacing;
        int segments = Mathf.CeilToInt(length / step);
        
        for (int i = 0; i < segments; i++)
        {
            float posX = isHorizontal ? startX + i * step + cubeSize/2 : startX + cubeSize/2;
            float posZ = isHorizontal ? startZ + cubeSize/2 : startZ + i * step + cubeSize/2;
            
            Vector3 position = new Vector3(posX, wallHeight/2, posZ);
            
            GameObject wall;
            if (wallPrefab != null)
            {
                wall = Instantiate(wallPrefab, position, Quaternion.identity);
            }
            else
            {
                wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wall.transform.position = position;
            }
            
            // Wall 태그 설정
            wall.tag = "Wall";
            
            // Set wall dimensions
            if (isHorizontal)
            {
                wall.transform.localScale = new Vector3(cubeSize, wallHeight, cubeSize);
            }
            else
            {
                wall.transform.localScale = new Vector3(cubeSize, wallHeight, cubeSize);
            }
            
            wall.transform.parent = this.transform;
            
            // Set wall material
            Renderer renderer = wall.GetComponent<Renderer>();
            if (renderer != null && borderMaterial != null) // borderMaterial이 null이 아닌지 확인
            {
                // renderer.material.color = wallColor; // 이전 색상 설정 제거
                renderer.material = borderMaterial; // 머티리얼 할당
            }
        }
    }

    // Helper method to get color based on Brick.cs logic
    private Color GetColorFromBrickLogic(int number)
    {
        if (number <= 30)
        {
            return new Color(1, 1 - (number / 30f), 0); // Yellow to Red
        }
        else if (number <= 60)
        {
            return new Color(1, 0, (number - 30) / 30f); // Red to Purple
        }
        else
        {
            float redColorValue = 1 - ((number - 60) / 30f);
            return new Color(Mathf.Max(redColorValue, 0), 0, 1); // Purple to Blue
        }
    }

    Color GetCubeColor(int x, int y, int adjustedGridSizeY)
    {
        // 플레이어 수에 따라 로직 분기
        if (playerCount == 2)
        {
            // 2인용: 좌하단 vs 우상단 구분 (대각선 기준)
            // 기울기 계산: y / adjustedGridSizeY > x / gridSizeX  =>  y * gridSizeX > x * adjustedGridSizeY
            if (y * gridSizeX > x * adjustedGridSizeY) // 근사적으로 대각선 위 (우상단 영역)
            {
                 return GetColorFromBrickLogic(2); // 거의 노란색 (우상단 색상)
            }
            else // 근사적으로 대각선 아래 (좌하단 영역)
            {
                 return new Color(0.5f, 0.8f, 0.5f); // 차분한 초록색 (좌하단 색상)
            }
        }
        else // 기본 4인용 또는 그 외
        {
            // 기존 4분할 로직
            bool isRightHalf = x >= gridSizeX / 2;
            bool isTopHalf = y >= adjustedGridSizeY / 2;

            if (!isRightHalf && isTopHalf) // 좌상단
            {
                // return new Color(1f, 0.7f, 0.7f); // 이전: 채도 낮은 빨간색
                return GetColorFromBrickLogic(28); // 거의 빨간색
            }
            else if (isRightHalf && isTopHalf) // 우상단
            {
                // return new Color(1f, 1f, 0.7f); // 이전: 채도 낮은 노란색
                return GetColorFromBrickLogic(2); // 거의 노란색
            }
            else if (!isRightHalf && !isTopHalf) // 좌하단
            {
                // return new Color(0.7f, 1f, 0.7f); // 이전: 채도 낮은 초록색
                return new Color(0.5f, 0.8f, 0.5f); // 차분한 초록색 (로직과 별개)
            }
            else // 우하단
            {
                // return new Color(1f, 0.8f, 0.6f); // 이전: 채도 낮은 주황색
                return GetColorFromBrickLogic(15); // 주황색
            }
        }
    }

    // 각 플레이어의 색상 정의
    private void DefinePlayerColors()
    {
        _playerColors.Clear();

        // ✅ ScriptableObject 설정이 있으면 사용
        if (colorConfig != null && colorConfig.playerColors != null && colorConfig.playerColors.Length > 0)
        {
            foreach (var pc in colorConfig.playerColors)
            {
                _playerColors[pc.playerID] = pc.color;
            }
            Debug.Log($"<color=cyan>[IsometricGridGenerator] colorConfig에서 {colorConfig.playerColors.Length}개 색상 로드</color>");
            return;
        }

        // ✅ Fallback: 기존 하드코딩 색상
        // 플레이어 수에 따라 색상 정의
        if (playerCount == 2)
        {
            // *** 플레이어 0과 1의 색상 교체 ***
            // 이전: 0(초록), 1(노랑)
            // 변경: 0(노랑), 1(초록)
             _playerColors[0] = GetColorFromBrickLogic(2); // 노란색 - 플레이어 0
             _playerColors[1] = new Color(0.5f, 0.8f, 0.5f); // 초록색 - 플레이어 1
        }
        else // 4인용 또는 그 외
        {
            // 4인용은 그대로 유지
            _playerColors[0] = new Color(1.0f, 0.0f, 0.0f); // 빨강 (좌하단)
            _playerColors[1] = new Color(1.0f, 0.8f, 0.0f); // 노랑 (우상단)
            _playerColors[2] = new Color(0.0f, 0.6f, 0.0f); // 초록 (좌상단)
            _playerColors[3] = new Color(0.5f, 0.3f, 1.0f); // 보라 (우하단)
        }
        
        // 디버그 로그로 플레이어 색상 출력
        for (int i = 0; i < (playerCount == 2 ? 2 : 4); i++)
        {
            if (_playerColors.TryGetValue(i, out Color color))
            {
                Debug.Log($"<color=cyan>[IsometricGridGenerator] 플레이어 {i}의 색상: R:{color.r}, G:{color.g}, B:{color.b}</color>");
            }
        }
    }
    
    // 초기 블록 소유자 ID 결정 
    private int GetInitialOwnerID(int x, int y, int width, int height)
    {
        // 플레이어 수에 따라 초기 소유권 다르게 설정
        if (playerCount == 2)
        {
            // *** 플레이어 0과 1의 영역 교체 ***
            // 기울기 계산: y / height > x / width  =>  y * width > x * height
            if (y * width > x * height) // 근사적으로 대각선 위 (원래 플레이어 1 영역)
            {
                return 1; // 이제 플레이어 1 영역
            }
            else // 근사적으로 대각선 아래 (원래 플레이어 0 영역)
            {
                return 0; // 이제 플레이어 0 영역
            }
        }
        else // 4인용
        {
           // 4인용은 그대로 유지
            bool isRightHalf = x >= width / 2;
            bool isTopHalf = y >= height / 2;

            if (!isRightHalf && isTopHalf) // 좌상단
            {
                return 2;
            }
            else if (isRightHalf && isTopHalf) // 우상단
            {
                return 1;
            }
            else if (!isRightHalf && !isTopHalf) // 좌하단
            {
                return 0;
            }
            else // 우하단
            {
                return 3;
            }
        }
    }

    // 터렛 인스턴스화 헬퍼 함수 (플레이어 ID 추가)
    void InstantiateTurret(GameObject prefab, Vector3 position, Transform parent, int playerID = -1)
    {
        // ✅ 멀티플레이어 CLIENT는 터렛 생성 스킵 (SERVER가 Spawn하면 자동으로 받음)
        var netManager = Unity.Netcode.NetworkManager.Singleton;
        if (netManager != null && netManager.IsClient && !netManager.IsServer)
        {
            Debug.Log($"<color=yellow>[IsometricGridGenerator] CLIENT 모드 - 터렛 생성 스킵 (SERVER에서 Spawn됨)</color>");
            return;
        }

        // 터렛 생성 (SERVER 또는 싱글플레이어만)
        GameObject turretGO = Instantiate(prefab, position, Quaternion.identity, parent);

        // ✅ 멀티플레이어: NetworkObject가 있으면 Spawn (서버만)
        var networkObject = turretGO.GetComponent<Unity.Netcode.NetworkObject>();
        if (networkObject != null && netManager != null && netManager.IsServer)
        {
            networkObject.Spawn();
            Debug.Log($"<color=magenta>[IsometricGridGenerator] 터렛 NetworkObject.Spawn() 완료 (playerID={playerID})</color>");
        }
        
        // Cannon 컴포넌트 확인 및 리스트에 추가
        Cannon cannon = turretGO.GetComponent<Cannon>();
        if (cannon != null)
        {
            _cannons.Add(cannon);
            
            // 플레이어 ID 설정
            cannon.playerID = playerID;

            // ✅ 총알 프리팹 설정 (핵심!)
            if (bulletPrefab != null)
            {
                cannon.bulletPrefab = bulletPrefab;
                cannon.bulletSpeed = bulletSpeed;
                cannon.fireInterval = bulletFireInterval;
                Debug.Log($"<color=green>[IsometricGridGenerator] 캐논 {playerID}에 bulletPrefab 설정 완료</color>");
            }
            else
            {
                Debug.LogWarning($"<color=red>[IsometricGridGenerator] bulletPrefab이 없습니다! 총알 발사 불가!</color>");
            }

            // 플레이어 색상 설정 (시각적 구분용)
            if (_playerColors.TryGetValue(playerID, out Color color))
            {
                Renderer renderer = cannon.GetComponentInChildren<Renderer>();
                if (renderer != null)
                {
                    renderer.material.color = color;
                }

                // 플레이어 색상 저장
                cannon.playerColor = color;
            }

            Debug.Log($"<color=cyan>[IsometricGridGenerator] 플레이어 {playerID}의 캐논 생성: {turretGO.name}</color>");
        }
        else
        {
            Debug.LogWarning($"<color=yellow>[IsometricGridGenerator] 생성된 터렛에 Cannon 컴포넌트가 없습니다: {turretGO.name}</color>");
        }
    }
    
    // ReleaseGameManager가 호출할 수 있는 모든 캐논 가져오기 메서드
    public Cannon[] GetAllCannons()
    {
        // CLIENT인 경우 캐논 리스트가 비어있으면 씬에서 찾기
        if (_cannons.Count == 0)
        {
            RefreshCannonsFromScene();
        }
        return _cannons.ToArray();
    }

    /// <summary>
    /// CLIENT용: 씬에서 스폰된 캐논들을 찾아서 _cannons 리스트에 추가
    /// SERVER가 Spawn한 캐논들을 CLIENT에서 찾을 때 사용
    /// </summary>
    public void RefreshCannonsFromScene()
    {
        var allCannons = FindObjectsOfType<Cannon>();
        _cannons.Clear();

        foreach (var cannon in allCannons)
        {
            if (cannon != null && !_cannons.Contains(cannon))
            {
                _cannons.Add(cannon);

                // 플레이어 색상 설정 (CLIENT에서도 색상 적용)
                if (_playerColors.TryGetValue(cannon.playerID, out Color color))
                {
                    cannon.playerColor = color;
                    Renderer renderer = cannon.GetComponentInChildren<Renderer>();
                    if (renderer != null)
                    {
                        renderer.material.color = color;
                    }
                }

                // 총알 프리팹 설정 (CLIENT에서도 필요)
                if (bulletPrefab != null && cannon.bulletPrefab == null)
                {
                    cannon.bulletPrefab = bulletPrefab;
                    cannon.bulletSpeed = bulletSpeed;
                    cannon.fireInterval = bulletFireInterval;
                }

                Debug.Log($"<color=cyan>[IsometricGridGenerator] CLIENT - 캐논 발견: Player {cannon.playerID}</color>");
            }
        }

        Debug.Log($"<color=green>[IsometricGridGenerator] RefreshCannonsFromScene 완료 - {_cannons.Count}개 캐논 등록</color>");
    }

    // 블록 소유권 설정 메서드
    public bool SetBlockOwner(GameObject block, int playerID, Color playerColor)
    {
        if (block == null) return false;

        // 이전 소유자 저장 (이벤트 전파용)
        int oldOwnerID = -1;
        if (_blockOwners.TryGetValue(block, out int existingOwner))
        {
            oldOwnerID = existingOwner;
        }

        // 블록 소유권 업데이트
        _blockOwners[block] = playerID;

        // 블록 색상 변경 (이미 Renderer에서 설정했지만 백업으로 유지)
        Renderer renderer = block.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = playerColor;
        }

        // ✅ 컴포넌트에 블록 점령 이벤트 전파 (HarvestComponent 충전 등)
        Managers.Map?.Components?.NotifyBlockCaptured(block, oldOwnerID, playerID);

        Debug.Log($"<color=magenta>[IsometricGridGenerator] 블록 {block.name}의 소유권이 플레이어 {playerID}로 변경됨</color>");
        return true;
    }
    
    // 특정 플레이어가 소유한 블록 수 반환
    public int GetBlockCountByPlayer(int playerID)
    {
        int count = 0;
        foreach (var pair in _blockOwners)
        {
            if (pair.Value == playerID)
                count++;
        }
        return count;
    }
    
    // 모든 블록 수 반환
    public int GetTotalBlockCount()
    {
        return gridSizeX * Mathf.RoundToInt(gridSizeY * aspectRatio);
    }
    
    // 플레이어 색상 가져오기
    public Color GetPlayerColor(int playerID)
    {
        if (_playerColors.TryGetValue(playerID, out Color color))
            return color;
            
        return Color.white; // 기본 색상
    }
    
    // 블록 소유자 ID 가져오기
    public int GetBlockOwner(GameObject block)
    {
        if (block == null) return -1;

        if (_blockOwners.TryGetValue(block, out int ownerID))
            return ownerID;

        return -1; // 소유자 없음 (중립)
    }

    /// <summary>
    /// 블록 이름으로 찾기 (ClientRpc에서 사용)
    /// </summary>
    public GameObject FindBlockByName(string blockName)
    {
        if (string.IsNullOrEmpty(blockName)) return null;

        foreach (var pair in _blockOwners)
        {
            if (pair.Key != null && pair.Key.name == blockName)
            {
                return pair.Key;
            }
        }

        // _blockOwners에 없으면 자식에서 직접 찾기
        foreach (Transform child in transform)
        {
            if (child.name == blockName && child.CompareTag("GridBlock"))
            {
                return child.gameObject;
            }
        }

        return null;
    }

    /// <summary>
    /// 로컬에서만 블록 소유권 변경 (ClientRpc에서 사용)
    /// 네트워크 동기화 없이 로컬 상태만 업데이트
    /// </summary>
    public void SetBlockOwnerLocal(GameObject block, int playerID, Color playerColor)
    {
        if (block == null) return;

        // 블록 소유권 업데이트 (로컬만)
        _blockOwners[block] = playerID;

        // 블록 색상 변경
        Renderer renderer = block.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = playerColor;
        }

        Debug.Log($"<color=yellow>[IsometricGridGenerator] LOCAL 블록 {block.name} 소유권 → 플레이어 {playerID}</color>");
    }

    /// <summary>
    /// 플레이어의 블록을 제거 (중립으로 변경)
    /// 총알 발사 시 호출하여 점수 차감
    /// </summary>
    /// <param name="playerID">플레이어 ID</param>
    /// <param name="count">제거할 블록 수</param>
    /// <returns>실제로 제거된 블록 수</returns>
    public int RemovePlayerBlocks(int playerID, int count)
    {
        if (count <= 0) return 0;

        // 플레이어 소유 블록들 찾기
        List<GameObject> playerBlocks = new List<GameObject>();
        foreach (var pair in _blockOwners)
        {
            if (pair.Value == playerID && pair.Key != null)
            {
                playerBlocks.Add(pair.Key);
            }
        }

        // 제거할 수 있는 만큼만 제거
        int removeCount = Mathf.Min(count, playerBlocks.Count);

        // 중립 색상 (config에서 가져오거나 기본 회색)
        Color neutralColor = colorConfig != null ? colorConfig.neutralColor : new Color(0.5f, 0.5f, 0.5f, 1f);

        for (int i = 0; i < removeCount; i++)
        {
            GameObject block = playerBlocks[i];

            // 소유권 중립으로 변경
            _blockOwners[block] = -1;

            // 색상 중립으로 변경
            Renderer renderer = block.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = neutralColor;
            }
        }

        Debug.Log($"<color=orange>[IsometricGridGenerator] Player {playerID}의 블록 {removeCount}개 제거됨 (남은 블록: {playerBlocks.Count - removeCount})</color>");
        return removeCount;
    }
}
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Unity.Assets.Scripts.Objects;

/// <summary>
/// 블록깨기 멀티플레이 스폰 매니저
/// 플레이어가 접속하면 자동으로 Ball과 Plank를 생성
/// Inspector 없이 코드로만 동작
/// </summary>
public class BrickGameMultiplayerSpawner : NetworkBehaviour
{
    #region 설정 (코드로 자동 설정)
    private GameObject _ballPrefab;
    private GameObject _plankPrefab;
    private Camera _mainCamera;

    // 플레이어별 스폰 위치 오프셋
    private float _plankYPosition = -4f; // 패들 Y 위치
    private float _plankSpacing = 3f;     // 플레이어간 간격

    // 경계 Transform (코드로 찾기)
    private Transform _leftBoundary;
    private Transform _rightBoundary;
    #endregion

    #region 플레이어 추적
    private Dictionary<ulong, PlayerObjects> _playerObjects = new Dictionary<ulong, PlayerObjects>();

    private class PlayerObjects
    {
        public GameObject Ball;
        public GameObject Plank;
        public int PlayerIndex;
    }
    #endregion

    void Start()
    {
        // 프리팹 자동 로드 (Resources 폴더에서)
        LoadPrefabs();

        // 카메라 찾기
        _mainCamera = Camera.main;
        if (_mainCamera == null)
        {
            GameLogger.Error("BrickGameMultiplayerSpawner", "Main Camera를 찾을 수 없습니다!");
        }

        // 경계 찾기 (Scene에서)
        FindBoundaries();
    }

    /// <summary>
    /// Prefab 자동 로드 (Resources 또는 Addressables)
    /// </summary>
    private void LoadPrefabs()
    {
        // Resources/GameScene/Model/ball.prefab
        _ballPrefab = Resources.Load<GameObject>("GameScene/Model/ball");
        if (_ballPrefab == null)
        {
            GameLogger.Error("BrickGameMultiplayerSpawner", "ball 프리팹을 찾을 수 없습니다! Resources/GameScene/Model/ball.prefab 확인");
        }
        else
        {
            GameLogger.Success("BrickGameMultiplayerSpawner", $"Ball 프리팹 로드 완료: {_ballPrefab.name}");
        }

        // Plank는 씬에서 찾거나 프리팹으로 생성
        // 방법 1: 씬의 기존 Plank를 복제
        PhysicsPlank existingPlank = FindObjectOfType<PhysicsPlank>();
        if (existingPlank != null)
        {
            // 기존 Plank를 프리팹화 (런타임에 복제용)
            _plankPrefab = existingPlank.gameObject;
            GameLogger.Success("BrickGameMultiplayerSpawner", $"Plank 원본 발견: {_plankPrefab.name}");
        }
        else
        {
            GameLogger.Warning("BrickGameMultiplayerSpawner", "씬에서 PhysicsPlank를 찾을 수 없습니다. Resources에서 로드 시도...");
            _plankPrefab = Resources.Load<GameObject>("GameScene/Plank");
            if (_plankPrefab == null)
            {
                GameLogger.Error("BrickGameMultiplayerSpawner", "Plank 프리팹을 찾을 수 없습니다!");
            }
        }
    }

    /// <summary>
    /// 씬에서 경계 Transform 찾기
    /// </summary>
    private void FindBoundaries()
    {
        // "LeftEnd", "RightEnd" GameObject 찾기 (BrickGame 씬 구조)
        _leftBoundary = GameObject.Find("LeftEnd")?.transform;
        _rightBoundary = GameObject.Find("RightEnd")?.transform;

        if (_leftBoundary == null || _rightBoundary == null)
        {
            GameLogger.Warning("BrickGameMultiplayerSpawner", "경계(LeftEnd/RightEnd)를 찾을 수 없습니다. 기본값 사용");
            // 기본값 생성
            var left = new GameObject("LeftEnd_Auto");
            left.transform.position = new Vector3(-8f, _plankYPosition, 0);
            _leftBoundary = left.transform;

            var right = new GameObject("RightEnd_Auto");
            right.transform.position = new Vector3(8f, _plankYPosition, 0);
            _rightBoundary = right.transform;
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            // 서버: 클라이언트 연결 이벤트 구독
            NetworkManager.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.OnClientDisconnectCallback += OnClientDisconnected;

            GameLogger.Success("BrickGameMultiplayerSpawner", "서버 모드 - 클라이언트 연결 대기 중");
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            NetworkManager.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.OnClientDisconnectCallback -= OnClientDisconnected;
        }

        base.OnNetworkDespawn();
    }

    /// <summary>
    /// 클라이언트 연결 시 Ball과 Plank 생성 (서버에서 실행)
    /// </summary>
    private void OnClientConnected(ulong clientId)
    {
        if (!IsServer) return;

        GameLogger.Info("BrickGameMultiplayerSpawner", $"🎮 플레이어 {clientId} 연결됨 - Ball & Plank 생성 중...");

        int playerIndex = _playerObjects.Count;

        // 1. Plank 생성
        GameObject plankObject = SpawnPlankForPlayer(clientId, playerIndex);

        // 2. Ball 생성
        GameObject ballObject = SpawnBallForPlayer(clientId, playerIndex, plankObject);

        // 3. 플레이어 오브젝트 추적
        _playerObjects[clientId] = new PlayerObjects
        {
            Ball = ballObject,
            Plank = plankObject,
            PlayerIndex = playerIndex
        };

        GameLogger.Success("BrickGameMultiplayerSpawner", $"✅ 플레이어 {clientId} 스폰 완료 (Index: {playerIndex})");
    }

    /// <summary>
    /// 플레이어별 Plank 생성
    /// </summary>
    private GameObject SpawnPlankForPlayer(ulong clientId, int playerIndex)
    {
        if (_plankPrefab == null)
        {
            GameLogger.Error("BrickGameMultiplayerSpawner", "Plank 프리팹이 없습니다!");
            return null;
        }

        // 플레이어별 위치 계산
        float xPosition = CalculatePlayerXPosition(playerIndex);
        Vector3 spawnPosition = new Vector3(xPosition, _plankYPosition, 0);

        // Plank 생성 (기존 씬 Plank 복제 또는 프리팹 인스턴스화)
        GameObject plankObject;

        if (_plankPrefab.scene.IsValid())
        {
            // 씬 오브젝트인 경우 복제
            plankObject = Instantiate(_plankPrefab);
        }
        else
        {
            // 프리팹인 경우 그대로 인스턴스화
            plankObject = Instantiate(_plankPrefab);
        }

        plankObject.name = $"Plank_Player{clientId}";
        plankObject.transform.position = spawnPosition;

        // NetworkObject 설정
        NetworkObject networkObject = plankObject.GetComponent<NetworkObject>();
        if (networkObject == null)
        {
            networkObject = plankObject.AddComponent<NetworkObject>();
        }

        // 스폰 (Owner 지정)
        networkObject.SpawnWithOwnership(clientId);

        // PhysicsPlank 설정
        PhysicsPlank plank = plankObject.GetComponent<PhysicsPlank>();
        if (plank != null)
        {
            // 경계 설정
            plank.leftEnd = _leftBoundary;
            plank.rightEnd = _rightBoundary;
            plank.mainCamera = _mainCamera;
        }

        GameLogger.Info("BrickGameMultiplayerSpawner", $"  📍 Plank 스폰: {spawnPosition}");

        return plankObject;
    }

    /// <summary>
    /// 플레이어별 Ball 생성
    /// </summary>
    private GameObject SpawnBallForPlayer(ulong clientId, int playerIndex, GameObject plankObject)
    {
        if (_ballPrefab == null)
        {
            GameLogger.Error("BrickGameMultiplayerSpawner", "Ball 프리팹이 없습니다!");
            return null;
        }

        // Ball 위치: Plank 위에
        Vector3 spawnPosition = plankObject.transform.position + Vector3.up * 1f;

        // Ball 생성
        GameObject ballObject = Instantiate(_ballPrefab, spawnPosition, Quaternion.identity);
        ballObject.name = $"Ball_Player{clientId}";

        // NetworkObject 설정
        NetworkObject networkObject = ballObject.GetComponent<NetworkObject>();
        if (networkObject == null)
        {
            networkObject = ballObject.AddComponent<NetworkObject>();
        }

        // 스폰 (Owner 지정)
        networkObject.SpawnWithOwnership(clientId);

        // PhysicsBall 설정
        PhysicsBall ball = ballObject.GetComponent<PhysicsBall>();
        if (ball != null)
        {
            // Plank 참조 설정
            PhysicsPlank plank = plankObject.GetComponent<PhysicsPlank>();
            if (plank != null)
            {
                // Reflection 또는 public field로 plank 설정
                var field = typeof(PhysicsBall).GetField("plank",
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.Instance);

                if (field != null)
                {
                    field.SetValue(ball, plank);
                }
            }
        }

        GameLogger.Info("BrickGameMultiplayerSpawner", $"  ⚽ Ball 스폰: {spawnPosition}");

        return ballObject;
    }

    /// <summary>
    /// 플레이어 X 위치 계산 (플레이어 인덱스 기반)
    /// </summary>
    private float CalculatePlayerXPosition(int playerIndex)
    {
        // 2인 플레이: 왼쪽(-2), 오른쪽(+2)
        // 3인 플레이: 왼쪽(-3), 중앙(0), 오른쪽(+3)
        // 4인 플레이: -4.5, -1.5, +1.5, +4.5

        int totalPlayers = _playerObjects.Count + 1;

        if (totalPlayers == 1)
        {
            return 0; // 1인: 중앙
        }
        else if (totalPlayers == 2)
        {
            return playerIndex == 0 ? -_plankSpacing : _plankSpacing;
        }
        else
        {
            // 3인 이상: 균등 배치
            float totalWidth = _plankSpacing * (totalPlayers - 1);
            float startX = -totalWidth / 2f;
            return startX + (playerIndex * _plankSpacing);
        }
    }

    /// <summary>
    /// 클라이언트 연결 해제 시 오브젝트 제거
    /// </summary>
    private void OnClientDisconnected(ulong clientId)
    {
        if (!IsServer) return;

        if (_playerObjects.TryGetValue(clientId, out PlayerObjects objects))
        {
            // Ball & Plank 제거
            if (objects.Ball != null)
            {
                NetworkObject ballNetObj = objects.Ball.GetComponent<NetworkObject>();
                if (ballNetObj != null && ballNetObj.IsSpawned)
                {
                    ballNetObj.Despawn();
                }
                Destroy(objects.Ball);
            }

            if (objects.Plank != null)
            {
                NetworkObject plankNetObj = objects.Plank.GetComponent<NetworkObject>();
                if (plankNetObj != null && plankNetObj.IsSpawned)
                {
                    plankNetObj.Despawn();
                }
                Destroy(objects.Plank);
            }

            _playerObjects.Remove(clientId);

            GameLogger.Warning("BrickGameMultiplayerSpawner", $"🔌 플레이어 {clientId} 연결 해제 - 오브젝트 제거됨");
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Define;

public class GameScene : BaseScene
{
	private bool _brickGameInitialized = false;

	public override bool Init()
	{
		if (base.Init() == false)
			return false;

		SceneType = EScene.GameScene;

		// ✅ CameraManager 초기화를 여기서 먼저 수행 (BrickGameMultiplayerSpawner.OnNetworkSpawn보다 먼저)
		Managers.Camera.Initialize();
		GameLogger.Success("GameScene", "CameraManager 초기화 완료");

		// ✅ BrickGame UI 표시 (prefab 없으면 스킵 - 멀티플레이어 Client에서 발생 가능)
		try
		{
			var sceneUI = Managers.UI.ShowSceneUI<UI_BrickGameScene>();
			if (sceneUI != null)
			{
				GameLogger.Success("GameScene", "UI_BrickGameScene 표시 완료");
			}
			else
			{
				GameLogger.Warning("GameScene", "UI_BrickGameScene prefab을 찾을 수 없음 (Addressable 미로드 또는 prefab 없음)");
			}
		}
		catch (System.Exception e)
		{
			GameLogger.Warning("GameScene", $"UI_BrickGameScene 로드 실패 (무시): {e.Message}");
		}

		GameLogger.Success("GameScene", "GameScene Init 완료 (BrickGame은 Start에서 초기화)");
		return true;
	}

	// BrickGame 초기화를 Start()로 지연 (Awake에서 블로킹 방지)
	private void Start()
	{
		if (_brickGameInitialized)
			return;

		GameLogger.Progress("GameScene", "Start()에서 BrickGame 초기화 시작...");
		InitializeBrickGame();
		_brickGameInitialized = true;
	}

	private void InitializeBrickGame()
	{
		// BrickGameInitializer가 모든 초기화 담당
		var initializer = new BrickGameInitializer();

		if (initializer.Initialize())
		{
			// ✅ GameRuleManager 초기화 (점수→총알 규칙 활성화)
			Managers.Game.Rules.Initialize();
			GameLogger.Success("GameScene", "GameRuleManager 초기화 완료");

			// ✅ BulletSpawner 초기화 (멀티플레이어에서만)
			if (MultiplayerUtil.IsMultiplayer())
			{
				InitializeBulletSpawner();
			}

			// ✅ MultiplayerUtil 사용
			if (MultiplayerUtil.IsSinglePlayer())
			{
				// 싱글플레이어: 네트워크 동기화 연결 및 게임 시작
				ConnectNetworkSync();
				Managers.Game.BrickGame?.StartGame();
				GameLogger.Success("GameScene", "BrickGame 초기화 및 시작 완료!");
			}
			else
			{
				// 멀티플레이어: BrickGameMultiplayerSpawner가 플레이어별 게임 시작
				GameLogger.Success("GameScene", "[Multiplayer] 플레이어별 게임은 BrickGameMultiplayerSpawner가 관리");
			}
		}
		else
		{
			GameLogger.Error("GameScene", "BrickGame 초기화 실패!");
		}
	}

	/// <summary>
	/// BulletSpawner 초기화 (멀티플레이어용)
	/// </summary>
	private void InitializeBulletSpawner()
	{
		// BrickGameMultiplayerSpawner에 BulletSpawner 컴포넌트 추가
		var multiplayerSpawner = FindObjectOfType<BrickGameMultiplayerSpawner>();
		if (multiplayerSpawner != null)
		{
			var bulletSpawner = multiplayerSpawner.GetComponent<BrickGameBulletSpawner>();
			if (bulletSpawner == null)
			{
				bulletSpawner = multiplayerSpawner.gameObject.AddComponent<BrickGameBulletSpawner>();
				GameLogger.Success("GameScene", "BrickGameBulletSpawner 컴포넌트 추가됨");
			}
		}
		else
		{
			GameLogger.Warning("GameScene", "BrickGameMultiplayerSpawner를 찾을 수 없어 BulletSpawner 초기화 스킵");
		}

		// ✅ 중앙 맵(땅따먹기) 총알 컨트롤러 초기화
		InitializeCentralMapBulletController();
	}

	/// <summary>
	/// 중앙 맵 총알 컨트롤러 초기화 (땅따먹기용)
	/// </summary>
	private void InitializeCentralMapBulletController()
	{
		// ✅ HOST/CLIENT 모두 CentralMapBulletController 필요
		//    - HOST: 총알 Spawn
		//    - CLIENT: ActionBus 구독 + ServerRpc 호출
		var netManager = Unity.Netcode.NetworkManager.Singleton;

		// IsometricGridGenerator가 있는지 확인 (중앙 맵 모드)
		var gridGenerator = FindObjectOfType<IsometricGridGenerator>();
		if (gridGenerator == null)
		{
			GameLogger.Warning("GameScene", "IsometricGridGenerator 없음 - 동적 생성 시도...");
			gridGenerator = CreateIsometricGridGenerator();

			if (gridGenerator == null)
			{
				GameLogger.Error("GameScene", "IsometricGridGenerator 동적 생성 실패!");
				return;
			}
		}

		// ✅ 플레이어 수 설정 (기존 것이든 새로 만든 것이든)
		int actualPlayerCount = GetActualPlayerCount();
		if (gridGenerator.playerCount != actualPlayerCount)
		{
			GameLogger.Info("GameScene", $"IsometricGridGenerator playerCount 변경: {gridGenerator.playerCount} → {actualPlayerCount}");
			gridGenerator.playerCount = actualPlayerCount;
		}

		// 이미 있는지 확인
		var bulletController = FindObjectOfType<CentralMapBulletController>();
		if (bulletController != null)
		{
			GameLogger.Info("GameScene", "CentralMapBulletController 이미 존재함");
			return;
		}

		// NetworkObject가 있는 GameObject에 추가 (BrickGameMultiplayerSpawner 또는 새로 생성)
		var multiplayerSpawner = FindObjectOfType<BrickGameMultiplayerSpawner>();
		if (multiplayerSpawner != null)
		{
			bulletController = multiplayerSpawner.gameObject.AddComponent<CentralMapBulletController>();
			// ✅ 이미 Spawn된 NetworkObject에 AddComponent한 경우 OnNetworkSpawn()이 자동 호출되지 않음!
			// 반드시 ManualInitialize() 호출하여 ActionBus 구독 등 초기화 수행
			bulletController.ManualInitialize();
			GameLogger.Success("GameScene", "CentralMapBulletController 컴포넌트 추가 및 ManualInitialize 완료");
		}
		else
		{
			// Fallback: 새 NetworkObject 생성 (Host만)
			if (netManager != null && netManager.IsServer)
			{
				var controllerObj = new GameObject("CentralMapBulletController");
				var networkObj = controllerObj.AddComponent<Unity.Netcode.NetworkObject>();
				bulletController = controllerObj.AddComponent<CentralMapBulletController>();
				networkObj.Spawn();
				GameLogger.Success("GameScene", "CentralMapBulletController 새로 생성 및 스폰됨");
			}
		}
	}

	/// <summary>
	/// IsometricGridGenerator 동적 생성 (땅따먹기 맵 생성기)
	/// </summary>
	private IsometricGridGenerator CreateIsometricGridGenerator()
	{
		GameLogger.Progress("GameScene", "IsometricGridGenerator 동적 생성 중...");

		// 1. GameObject 생성
		var gridObj = new GameObject("IsometricGridGenerator");
		var gridGenerator = gridObj.AddComponent<IsometricGridGenerator>();

		// 2. StandardTurret 프리팹 로드 (Cannon 포함)
		var turretPrefab = Managers.Resource.Load<GameObject>("GameScene/Rocket/StandardTurret");
		if (turretPrefab == null)
		{
			// Fallback: 다른 경로 시도
			turretPrefab = Resources.Load<GameObject>("GameScene/Rocket/StandardTurret");
		}
		if (turretPrefab == null)
		{
			turretPrefab = Resources.Load<GameObject>("GameScene/Model/Rocket/StandardTurret");
		}

		if (turretPrefab != null)
		{
			gridGenerator.standardTurretPrefab = turretPrefab;
			GameLogger.Success("GameScene", $"StandardTurret 프리팹 로드 완료: {turretPrefab.name}");

			// ✅ NetworkPrefabs에 런타임 등록 (Spawn 가능하도록)
			var netManager = Unity.Netcode.NetworkManager.Singleton;
			if (netManager != null)
			{
				try
				{
					netManager.AddNetworkPrefab(turretPrefab);
					GameLogger.Success("GameScene", "StandardTurret을 NetworkPrefabs에 등록 완료");
				}
				catch (System.Exception e)
				{
					// 이미 등록되어 있으면 무시
					GameLogger.Warning("GameScene", $"NetworkPrefab 등록 (이미 등록됨?): {e.Message}");
				}
			}
		}
		else
		{
			GameLogger.Error("GameScene", "StandardTurret 프리팹을 찾을 수 없습니다!");
		}

		// 3. BorderMaterial 로드
		var borderMat = Managers.Resource.Load<Material>("GameScene/Materials/BorderMaterial");
		if (borderMat == null)
		{
			borderMat = Resources.Load<Material>("GameScene/Materials/BorderMaterial");
		}
		if (borderMat == null)
		{
			// Fallback: 기본 머티리얼 생성
			borderMat = new Material(Shader.Find("Standard"));
			borderMat.color = Color.gray;
			GameLogger.Warning("GameScene", "BorderMaterial 없음 - 기본 머티리얼 사용");
		}
		else
		{
			GameLogger.Success("GameScene", "BorderMaterial 로드 완료");
		}
		gridGenerator.borderMaterial = borderMat;

		// 4. 그리드 설정 (2인 플레이어)
		gridGenerator.playerCount = 2;
		gridGenerator.gridSizeX = 15;  // 적당한 크기
		gridGenerator.gridSizeY = 10;
		gridGenerator.cubeSize = 1.0f;
		gridGenerator.spacing = 0.05f;

		// 5. 위치 설정 (맵 중앙)
		gridObj.transform.position = new Vector3(0, 0, 0);

		GameLogger.Success("GameScene", $"IsometricGridGenerator 동적 생성 완료 (playerCount={gridGenerator.playerCount})");
		return gridGenerator;
	}

	/// <summary>
	/// BrickGameNetworkSync 찾아서 BrickGameManager에 연결
	/// </summary>
	private void ConnectNetworkSync()
	{
		var networkSync = FindObjectOfType<BrickGameNetworkSync>();

		if (networkSync != null)
		{
			Managers.Game.BrickGame.ConnectNetworkSync(networkSync);
			GameLogger.Success("GameScene", "BrickGameNetworkSync 연결 완료");
		}
		else
		{
			GameLogger.Warning("GameScene", "BrickGameNetworkSync를 찾을 수 없습니다 (싱글플레이어 모드)");
		}
	}

	// ✅ 매 프레임 입력 처리 (방향키 + 스페이스바를 ActionBus에 발행)
	private void Update()
	{
		// ✅ 포커스가 없으면 입력 무시 (ParrelSync 등 멀티 에디터 테스트 시 필수)
		if (!Application.isFocused) return;

		// 방향키 입력 받기
		float horizontal = Input.GetAxisRaw("Horizontal"); // A/D 또는 Left/Right Arrow

		// ActionBus에 Input_ArrowKey 발행 (0 포함 - 키를 뗐을 때도 발행)
		var payload = new MB.Infrastructure.Messages.ArrowKeyPayload(horizontal);
		Managers.PublishAction(MB.Infrastructure.Messages.ActionId.Input_ArrowKey, payload);

		// ✅ 스페이스바 = 대포 발사 (BrickGame - 벽돌깨기)
		if (Input.GetKeyDown(KeyCode.Space))
		{
			Managers.PublishAction(MB.Infrastructure.Messages.ActionId.Input_Fire);
		}

		// ✅ Enter키 = 대포 발사 (CentralMap - 땅따먹기)
		if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
		{
			Managers.PublishAction(MB.Infrastructure.Messages.ActionId.Input_CentralMapFire);
		}
	}

	/// <summary>
	/// 실제 플레이어 수 가져오기
	/// </summary>
	private int GetActualPlayerCount()
	{
		// 1순위: NetworkManager 연결된 클라이언트 수
		var netManager = Unity.Netcode.NetworkManager.Singleton;
		if (netManager != null && netManager.IsServer)
		{
			int connectedCount = netManager.ConnectedClientsIds.Count;
			if (connectedCount > 0)
			{
				GameLogger.Info("GameScene", $"플레이어 수: NetworkManager에서 {connectedCount}명 감지");
				return connectedCount;
			}
		}

		// 2순위: Define 상수 사용
		int defineCount = Define.MATCHMAKING_PLAYER_COUNT;
		GameLogger.Info("GameScene", $"플레이어 수: Define.MATCHMAKING_PLAYER_COUNT = {defineCount}");
		return defineCount;
	}

	public override void Clear()
	{
		// ✅ GameRuleManager 정리
		Managers.Game?.Rules?.Cleanup();

		// ✅ MultiplayerUtil 사용
		if (MultiplayerUtil.IsMultiplayer())
		{
			// 멀티플레이어: 모든 플레이어 게임 정리
			Managers.Game?.CleanupAllPlayerGames();
			Managers.Camera?.ResetViewports();
			GameLogger.Info("GameScene", "GameScene 정리 완료 (멀티플레이어 - 모든 플레이어 게임 정리됨)");
		}
		else
		{
			// 싱글플레이어: 단일 BrickGame 정리
			Managers.Game?.CleanupBrickGame();
			Managers.Camera?.ResetViewports();
			GameLogger.Info("GameScene", "GameScene 정리 완료 (싱글플레이어)");
		}
	}
}

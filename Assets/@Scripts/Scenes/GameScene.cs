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
			// 멀티플레이어 모드 확인
			var networkManager = Unity.Netcode.NetworkManager.Singleton;
			bool isMultiplayer = networkManager != null && networkManager.IsListening;

			if (!isMultiplayer)
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

	// ✅ 매 프레임 입력 처리 (방향키를 ActionBus에 발행)
	private void Update()
	{
		// ✅ 포커스가 없으면 입력 무시 (ParrelSync 등 멀티 에디터 테스트 시 필수)
		if (!Application.isFocused) return;

		// 방향키 입력 받기
		float horizontal = Input.GetAxisRaw("Horizontal"); // A/D 또는 Left/Right Arrow

		// ActionBus에 Input_ArrowKey 발행 (0 포함 - 키를 뗐을 때도 발행)
		var payload = new MB.Infrastructure.Messages.ArrowKeyPayload(horizontal);
		Managers.PublishAction(MB.Infrastructure.Messages.ActionId.Input_ArrowKey, payload);
	}

	public override void Clear()
	{
		// 멀티플레이어 모드 확인
		var networkManager = Unity.Netcode.NetworkManager.Singleton;
		bool isMultiplayer = networkManager != null && networkManager.IsListening;

		if (isMultiplayer)
		{
			// 멀티플레이어: 모든 플레이어 게임 정리
			Managers.Game?.CleanupAllPlayerGames();
			Managers.Camera?.ResetViewports();  // Viewport만 초기화
			GameLogger.Info("GameScene", "GameScene 정리 완료 (멀티플레이어 - 모든 플레이어 게임 정리됨)");
		}
		else
		{
			// 싱글플레이어: 단일 BrickGame 정리
			Managers.Game?.CleanupBrickGame();
			Managers.Camera?.ResetViewports();  // Viewport만 초기화
			GameLogger.Info("GameScene", "GameScene 정리 완료 (싱글플레이어)");
		}
	}
}

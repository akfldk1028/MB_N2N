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
			// 게임 시작 (공 준비 상태로 전환)
			Managers.Game.BrickGame?.StartGame();
			GameLogger.Success("GameScene", "BrickGame 초기화 및 시작 완료!");
		}
		else
		{
			GameLogger.Error("GameScene", "BrickGame 초기화 실패!");
		}
	}

	public override void Clear()
	{
		// BrickGame 정리 (ActionBus 구독 해제)
		Managers.Game?.CleanupBrickGame();
		GameLogger.Info("GameScene", "GameScene 정리 완료");
	}
}

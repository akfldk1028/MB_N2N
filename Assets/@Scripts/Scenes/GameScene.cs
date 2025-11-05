using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Define;

public class GameScene : BaseScene
{
	public override bool Init()
	{
		if (base.Init() == false)
			return false;

		SceneType = EScene.GameScene;

		// 벽돌깨기 게임 초기화 (씬 오브젝트 자동 탐색)
		InitializeBrickGame();

		return true;
	}

	private void InitializeBrickGame()
	{
		// BrickGameInitializer가 모든 초기화 담당
		var initializer = new BrickGameInitializer();
		
		if (initializer.Initialize())
		{
			// 게임 시작 (공 준비 상태로 전환)
			Managers.Game.BrickGame?.StartGame();
		}
		else
		{
			GameLogger.Error("GameScene", "BrickGame 초기화 실패!");
		}
	}

	public override void Clear()
	{

	}
}

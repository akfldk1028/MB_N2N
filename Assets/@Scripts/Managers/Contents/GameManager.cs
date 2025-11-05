/*
 * 게임 매니저 (GameManager)
 * 
 * 역할:
 * 1. 게임의 핵심 데이터와 게임 진행 상태 관리
 * 2. 게임 세이브 데이터(리소스, 영웅 등) 저장 및 로드
 * 3. 플레이어 진행 상황 추적 및 저장
 * 4. 영웅 소유 상태 및 레벨, 경험치 등의 데이터 관리
 * 5. JSON 형식으로 게임 데이터를 저장하고 로드하는 기능 제공
 * 6. 게임 초기화 및 데이터 설정 제어
 * 7. Managers 클래스를 통해 전역적으로 접근 가능한 게임 데이터 제공
 */

using Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;


public class GameManager
{
	// BrickGameManager 참조 (Non-MonoBehaviour)
	private BrickGameManager _brickGame = new BrickGameManager();
	private System.IDisposable _brickGameUpdateSubscription;

	public BrickGameManager BrickGame => _brickGame;

	public GameManager()
	{
		Debug.Log("<color=yellow>[GameManager]</color> 생성됨");
	}

	/// <summary>
	/// BrickGame 초기화 (BrickGameInitializer에서 호출)
	/// Client는 brickPlacer를 null로 전달 가능 (Server만 벽돌 생성)
	/// </summary>
	public void InitializeBrickGame(
		IBrickPlacer brickPlacer,
		PhysicsPlank plank,
		Camera mainCamera,
		BrickGameSettings settings)
	{
		GameLogger.Progress("GameManager", "BrickGame 초기화 시작...");

		// BrickGameManager 초기화
		_brickGame.Initialize(brickPlacer, plank, mainCamera, settings);

		// ActionBus에 Update 구독
		_brickGameUpdateSubscription = Managers.Subscribe(
			MB.Infrastructure.Messages.ActionId.System_Update,
			_brickGame.OnUpdate
		);

		GameLogger.Success("GameManager", "BrickGame 초기화 완료 및 Update 구독됨!");
	}

	/// <summary>
	/// BrickGame 정리 (씬 전환 시 호출)
	/// </summary>
	public void CleanupBrickGame()
	{
		_brickGameUpdateSubscription?.Dispose();
		_brickGameUpdateSubscription = null;

		GameLogger.Info("GameManager", "BrickGame 정리 완료 (Update 구독 해제)");
	}





	#region Save & Load	
	public string Path { get { return Application.persistentDataPath + "/SaveData.json"; } }


	#endregion

	#region Action
	#endregion
}

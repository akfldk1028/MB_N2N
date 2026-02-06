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
	// ✅ 단일 BrickGameManager (하위 호환 - Server용 또는 싱글플레이어용)
	private BrickGameManager _brickGame = new BrickGameManager();
	private System.IDisposable _brickGameUpdateSubscription;

	public BrickGameManager BrickGame => _brickGame;

	// ✅ 플레이어별 BrickGameManager (멀티플레이어 경쟁 모드)
	private Dictionary<ulong, BrickGameManager> _playerGames = new Dictionary<ulong, BrickGameManager>();
	private Dictionary<ulong, System.IDisposable> _playerGameSubscriptions = new Dictionary<ulong, System.IDisposable>();

	// ✅ 게임 규칙 매니저 (점수→총알 등)
	private GameRuleManager _rules = new GameRuleManager();
	public GameRuleManager Rules => _rules;

	// ✅ 승리 조건 매니저 (게임 전역 - 대포 파괴 시 승패 결정)
	private WinConditionManager _winCondition = new WinConditionManager();
	public WinConditionManager WinCondition => _winCondition;

	public GameManager()
	{
		Debug.Log("<color=yellow>[GameManager]</color> 생성됨 (GameRuleManager, WinConditionManager 포함)");
	}

	/// <summary>
	/// 승리 조건 매니저 초기화 (게임 시작 시 한번만 호출)
	/// </summary>
	public void InitializeWinCondition()
	{
		_winCondition.Initialize();
		GameLogger.Success("GameManager", "WinConditionManager 초기화 완료!");
	}

	/// <summary>
	/// 승리 조건 매니저 정리 (씬 전환 시)
	/// </summary>
	public void CleanupWinCondition()
	{
		_winCondition.Cleanup();
		GameLogger.Info("GameManager", "WinConditionManager 정리 완료!");
	}

	/// <summary>
	/// 승리 조건 리셋 (게임 재시작 시)
	/// </summary>
	public void ResetWinCondition()
	{
		_winCondition.Reset();
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

	#region 플레이어별 BrickGame 관리 (멀티플레이어 경쟁 모드)

	/// <summary>
	/// 플레이어별 BrickGame 초기화
	/// </summary>
	public void InitializePlayerGame(
		ulong clientId,
		IBrickPlacer brickPlacer,
		PhysicsPlank plank,
		Camera mainCamera,
		BrickGameSettings settings)
	{
		GameLogger.Progress("GameManager", $"[Player {clientId}] BrickGame 초기화 시작...");

		// 이미 존재하면 정리
		if (_playerGames.ContainsKey(clientId))
		{
			CleanupPlayerGame(clientId);
		}

		// 새 BrickGameManager 생성
		var playerGame = new BrickGameManager();
		playerGame.Initialize(brickPlacer, plank, mainCamera, settings);

		// ActionBus에 Update 구독
		var subscription = Managers.Subscribe(
			MB.Infrastructure.Messages.ActionId.System_Update,
			playerGame.OnUpdate
		);

		// Dictionary에 저장
		_playerGames[clientId] = playerGame;
		_playerGameSubscriptions[clientId] = subscription;

		GameLogger.Success("GameManager", $"[Player {clientId}] BrickGame 초기화 완료!");
	}

	/// <summary>
	/// 플레이어별 BrickGame 정리
	/// </summary>
	public void CleanupPlayerGame(ulong clientId)
	{
		if (_playerGameSubscriptions.TryGetValue(clientId, out var subscription))
		{
			subscription?.Dispose();
			_playerGameSubscriptions.Remove(clientId);
		}

		if (_playerGames.Remove(clientId))
		{
			GameLogger.Info("GameManager", $"[Player {clientId}] BrickGame 정리 완료");
		}
	}

	/// <summary>
	/// 플레이어별 BrickGame 가져오기
	/// </summary>
	public BrickGameManager GetPlayerGame(ulong clientId)
	{
		if (_playerGames.TryGetValue(clientId, out var game))
		{
			return game;
		}

		GameLogger.Warning("GameManager", $"[Player {clientId}] BrickGame이 존재하지 않습니다!");
		return null;
	}

	/// <summary>
	/// 모든 플레이어 게임 정리
	/// </summary>
	public void CleanupAllPlayerGames()
	{
		foreach (var clientId in _playerGames.Keys.ToList())
		{
			CleanupPlayerGame(clientId);
		}

		GameLogger.Info("GameManager", "모든 플레이어 BrickGame 정리 완료");
	}

	#endregion





	#region Save & Load	
	public string Path { get { return Application.persistentDataPath + "/SaveData.json"; } }


	#endregion

	#region Action
	#endregion
}

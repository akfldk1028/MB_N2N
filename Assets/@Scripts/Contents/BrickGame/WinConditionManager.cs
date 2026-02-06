using System;
using UnityEngine;
using MB.Infrastructure.Messages;

/// <summary>
/// 승리 조건 관리 매니저 (Non-MonoBehaviour, Sub-Manager 패턴)
///
/// 역할:
/// 1. Cannon.OnCannonDestroyed 이벤트 구독
/// 2. 승리/패배 조건 판정
/// 3. OnGameEnded 이벤트 발행 (네트워크 동기화용)
///
/// 접근: Managers.Game.BrickGame.WinCondition
/// </summary>
public class WinConditionManager
{
    #region State
    private bool _gameEnded = false;
    private int _winnerPlayerID = -1;
    private int _loserPlayerID = -1;
    #endregion

    #region Events
    /// <summary>
    /// 게임 종료 시 발생 (winnerID, loserID)
    /// BrickGameMultiplayerSpawner에서 구독하여 ClientRpc로 동기화
    /// </summary>
    public event Action<int, int> OnGameEnded;
    #endregion

    #region Public Properties
    public bool IsGameEnded => _gameEnded;
    public int WinnerPlayerID => _winnerPlayerID;
    public int LoserPlayerID => _loserPlayerID;
    #endregion

    #region Initialization
    /// <summary>
    /// 초기화 (BrickGameManager.Initialize()에서 호출)
    /// </summary>
    public void Initialize()
    {
        _gameEnded = false;
        _winnerPlayerID = -1;
        _loserPlayerID = -1;

        // Cannon 파괴 이벤트 구독
        Cannon.OnCannonDestroyed += HandleCannonDestroyed;

        GameLogger.Success("WinConditionManager", "초기화 완료 - Cannon.OnCannonDestroyed 구독");
    }

    /// <summary>
    /// 정리 (씬 전환 시 호출)
    /// </summary>
    public void Cleanup()
    {
        // 이벤트 구독 해제
        Cannon.OnCannonDestroyed -= HandleCannonDestroyed;

        GameLogger.Info("WinConditionManager", "정리 완료 - 이벤트 구독 해제");
    }
    #endregion

    #region Win Condition Logic
    /// <summary>
    /// Cannon 파괴 시 호출됨
    /// </summary>
    private void HandleCannonDestroyed(int destroyedPlayerID)
    {
        // 이미 게임 종료되었으면 무시
        if (_gameEnded)
        {
            GameLogger.Warning("WinConditionManager", "이미 게임 종료됨 - 무시");
            return;
        }

        GameLogger.Warning("WinConditionManager", $"★★★ Cannon 파괴 감지! Player {destroyedPlayerID} ★★★");

        // SERVER에서만 승리 조건 처리 (멀티플레이어)
        if (!MultiplayerUtil.HasServerAuthority())
        {
            GameLogger.Info("WinConditionManager", "Client에서는 승리 조건 처리 안 함 - Server 대기");
            return;
        }

        // 승자/패자 결정
        _loserPlayerID = destroyedPlayerID;
        _winnerPlayerID = GetOtherPlayerID(destroyedPlayerID);
        _gameEnded = true;

        GameLogger.Success("WinConditionManager",
            $"★★★ 게임 종료! 승자: Player {_winnerPlayerID}, 패자: Player {_loserPlayerID} ★★★");

        // 이벤트 발행 (BrickGameMultiplayerSpawner가 구독 → ClientRpc 동기화)
        OnGameEnded?.Invoke(_winnerPlayerID, _loserPlayerID);

        // ActionBus로 UI에 알림 (Server에서 직접)
        ProcessGameEndedLocal(_winnerPlayerID, _loserPlayerID);
    }

    /// <summary>
    /// 상대 플레이어 ID 반환 (2인 플레이어 기준)
    /// </summary>
    private int GetOtherPlayerID(int playerID)
    {
        return playerID == 0 ? 1 : 0;
    }

    /// <summary>
    /// 게임 종료 처리 (로컬)
    /// </summary>
    private void ProcessGameEndedLocal(int winnerID, int loserID)
    {
        // ActionBus로 UI에 알림
        Managers.PublishAction(ActionId.BrickGame_GameEnded,
            new GameEndedPayload(winnerID, loserID));

        // 패자의 BrickGameManager에 GameOver 호출
        var loserGame = Managers.Game?.GetPlayerGame((ulong)loserID);
        if (loserGame != null)
        {
            loserGame.GameOver();
        }

        // 승자의 BrickGameManager도 GameOver (게임 종료)
        var winnerGame = Managers.Game?.GetPlayerGame((ulong)winnerID);
        if (winnerGame != null)
        {
            winnerGame.GameOver();
        }
    }

    /// <summary>
    /// 클라이언트에서 게임 종료 처리 (ClientRpc에서 호출)
    /// </summary>
    public void ProcessGameEndedFromServer(int winnerID, int loserID)
    {
        if (_gameEnded)
        {
            GameLogger.Info("WinConditionManager", "이미 게임 종료 처리됨 - 스킵");
            return;
        }

        _gameEnded = true;
        _winnerPlayerID = winnerID;
        _loserPlayerID = loserID;

        GameLogger.Success("WinConditionManager",
            $"[Client] 게임 종료 수신! 승자: Player {winnerID}, 패자: Player {loserID}");

        ProcessGameEndedLocal(winnerID, loserID);
    }
    #endregion

    #region Public API
    /// <summary>
    /// 게임 리셋 (재시작 시)
    /// </summary>
    public void Reset()
    {
        _gameEnded = false;
        _winnerPlayerID = -1;
        _loserPlayerID = -1;
        GameLogger.Info("WinConditionManager", "게임 상태 리셋");
    }

    /// <summary>
    /// 로컬 플레이어가 승자인지 확인
    /// </summary>
    public bool IsLocalPlayerWinner()
    {
        if (!_gameEnded) return false;

        var networkManager = Unity.Netcode.NetworkManager.Singleton;
        if (networkManager == null) return true; // 싱글플레이어

        ulong localClientId = networkManager.LocalClientId;
        return (int)localClientId == _winnerPlayerID;
    }
    #endregion
}

/// <summary>
/// 게임 종료 Payload (ActionBus용)
/// </summary>
public class GameEndedPayload : IActionPayload
{
    public int WinnerPlayerID { get; }
    public int LoserPlayerID { get; }

    public GameEndedPayload(int winnerID, int loserID)
    {
        WinnerPlayerID = winnerID;
        LoserPlayerID = loserID;
    }
}

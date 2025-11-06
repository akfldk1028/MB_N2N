using System;
using Unity.Netcode;
using UnityEngine;
using MB.Infrastructure.Messages;

/// <summary>
/// BrickGame 멀티플레이어 동기화 관리
/// Server의 BrickGameManager 이벤트를 NetworkedMessageChannel로 전달하여 모든 Client 동기화
/// Managers.Game.BrickGame.Network로 접근
/// </summary>
public class BrickGameNetworkSync : NetworkBehaviour
{
    #region NetworkedMessageChannels
    private NetworkedMessageChannel<BrickGameScoreMessage> _scoreChannel;
    private NetworkedMessageChannel<BrickGameStateMessage> _stateChannel;
    private NetworkedMessageChannel<BrickGameRowSpawnedMessage> _rowSpawnedChannel;
    #endregion

    #region Synced State (Client에서 읽기용)
    private int _currentScore;
    private int _currentLevel;
    private GamePhase _currentPhase;

    public int Score => _currentScore;
    public int Level => _currentLevel;
    public GamePhase Phase => _currentPhase;
    #endregion

    #region References
    private BrickGameManager _gameManager;
    private bool _isInitialized;
    #endregion

    #region Initialization
    /// <summary>
    /// BrickGameManager 연결 및 초기화
    /// </summary>
    public void Initialize(BrickGameManager gameManager)
    {
        if (_isInitialized)
        {
            GameLogger.Warning("BrickGameNetworkSync", "이미 초기화됨");
            return;
        }

        _gameManager = gameManager;
        _isInitialized = true;

        GameLogger.Success("BrickGameNetworkSync", "BrickGameManager 연결 완료");
    }
    #endregion

    #region NetworkBehaviour Lifecycle
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // NetworkedMessageChannel 초기화
        InitializeChannels();

        if (IsServer)
        {
            // Server: BrickGameManager 이벤트 구독
            SubscribeToGameManagerEvents();
            GameLogger.Success("BrickGameNetworkSync", "[Server] 이벤트 구독 완료");
        }
        else
        {
            // Client: NetworkedMessageChannel 메시지 구독
            SubscribeToNetworkMessages();
            GameLogger.Success("BrickGameNetworkSync", "[Client] 네트워크 메시지 구독 완료");
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            UnsubscribeFromGameManagerEvents();
        }

        DisposeChannels();
        base.OnNetworkDespawn();
    }
    #endregion

    #region Channel Initialization
    private void InitializeChannels()
    {
        _scoreChannel = new NetworkedMessageChannel<BrickGameScoreMessage>();
        _scoreChannel.Initialize(NetworkManager);

        _stateChannel = new NetworkedMessageChannel<BrickGameStateMessage>();
        _stateChannel.Initialize(NetworkManager);

        _rowSpawnedChannel = new NetworkedMessageChannel<BrickGameRowSpawnedMessage>();
        _rowSpawnedChannel.Initialize(NetworkManager);

        GameLogger.Success("BrickGameNetworkSync", "NetworkedMessageChannel 초기화 완료");
    }

    private void DisposeChannels()
    {
        _scoreChannel?.Dispose();
        _stateChannel?.Dispose();
        _rowSpawnedChannel?.Dispose();
    }
    #endregion

    #region Server: GameManager Event Subscription
    private void SubscribeToGameManagerEvents()
    {
        if (_gameManager == null)
        {
            GameLogger.Error("BrickGameNetworkSync", "BrickGameManager가 null입니다!");
            return;
        }

        _gameManager.OnScoreChanged += HandleScoreChanged;
        _gameManager.OnLevelUp += HandleLevelUp;
        _gameManager.OnGameStart += HandleGameStart;
        _gameManager.OnGamePause += HandleGamePause;
        _gameManager.OnGameResume += HandleGameResume;
        _gameManager.OnGameOver += HandleGameOver;
        _gameManager.OnRowSpawn += HandleRowSpawn;
    }

    private void UnsubscribeFromGameManagerEvents()
    {
        if (_gameManager == null) return;

        _gameManager.OnScoreChanged -= HandleScoreChanged;
        _gameManager.OnLevelUp -= HandleLevelUp;
        _gameManager.OnGameStart -= HandleGameStart;
        _gameManager.OnGamePause -= HandleGamePause;
        _gameManager.OnGameResume -= HandleGameResume;
        _gameManager.OnGameOver -= HandleGameOver;
        _gameManager.OnRowSpawn -= HandleRowSpawn;
    }
    #endregion

    #region Server: Event Handlers (NetworkedMessageChannel로 Publish)
    private void HandleScoreChanged(int score)
    {
        if (!IsServer) return;

        _currentScore = score;
        _currentLevel = _gameManager.GetCurrentLevel();

        var message = new BrickGameScoreMessage(score, _currentLevel, _gameManager.GetRowsSpawned());
        _scoreChannel.Publish(message);

        // 로컬 ActionBus에도 발행
        Managers.PublishAction(ActionId.BrickGame_ScoreChanged,
            new BrickGameScorePayload(score, _currentLevel));

        GameLogger.DevLog("BrickGameNetworkSync", $"[Server] Score 동기화: {score}");
    }

    private void HandleLevelUp(int level)
    {
        if (!IsServer) return;

        _currentLevel = level;

        // 점수 메시지에 레벨 포함되므로 별도 전송 불필요
        // 로컬 ActionBus에만 발행
        Managers.PublishAction(ActionId.BrickGame_LevelUp, new BrickGameLevelPayload(level));

        GameLogger.Info("BrickGameNetworkSync", $"[Server] Level Up: {level}");
    }

    private void HandleGameStart()
    {
        if (!IsServer) return;
        PublishStateChange(GamePhase.Playing);
    }

    private void HandleGamePause()
    {
        if (!IsServer) return;
        PublishStateChange(GamePhase.Paused);
    }

    private void HandleGameResume()
    {
        if (!IsServer) return;
        PublishStateChange(GamePhase.Playing);
    }

    private void HandleGameOver()
    {
        if (!IsServer) return;
        PublishStateChange(GamePhase.GameOver);
    }

    private void HandleRowSpawn()
    {
        if (!IsServer) return;

        var message = new BrickGameRowSpawnedMessage(_gameManager.GetRowsSpawned());
        _rowSpawnedChannel.Publish(message);

        Managers.PublishAction(ActionId.BrickGame_RowSpawned);

        GameLogger.DevLog("BrickGameNetworkSync", $"[Server] Row Spawned: {_gameManager.GetRowsSpawned()}");
    }

    private void PublishStateChange(GamePhase phase)
    {
        _currentPhase = phase;

        var message = new BrickGameStateMessage(phase);
        _stateChannel.Publish(message);

        Managers.PublishAction(ActionId.BrickGame_GameStateChanged, new BrickGameStatePayload(phase));

        GameLogger.Info("BrickGameNetworkSync", $"[Server] GameState 변경: {phase}");
    }
    #endregion

    #region Client: Network Message Subscription
    private void SubscribeToNetworkMessages()
    {
        _scoreChannel.Subscribe(OnScoreMessageReceived);
        _stateChannel.Subscribe(OnStateMessageReceived);
        _rowSpawnedChannel.Subscribe(OnRowSpawnedMessageReceived);
    }

    private void OnScoreMessageReceived(BrickGameScoreMessage message)
    {
        if (IsServer) return; // Server는 이미 로컬에서 처리

        _currentScore = message.Score;
        _currentLevel = message.Level;

        // 로컬 ActionBus에 발행 (UI 업데이트용)
        Managers.PublishAction(ActionId.BrickGame_ScoreChanged,
            new BrickGameScorePayload(message.Score, message.Level));

        GameLogger.DevLog("BrickGameNetworkSync", $"[Client] Score 수신: {message.Score}");
    }

    private void OnStateMessageReceived(BrickGameStateMessage message)
    {
        if (IsServer) return;

        _currentPhase = message.Phase;

        Managers.PublishAction(ActionId.BrickGame_GameStateChanged,
            new BrickGameStatePayload(message.Phase));

        GameLogger.Info("BrickGameNetworkSync", $"[Client] GameState 수신: {message.Phase}");
    }

    private void OnRowSpawnedMessageReceived(BrickGameRowSpawnedMessage message)
    {
        if (IsServer) return;

        Managers.PublishAction(ActionId.BrickGame_RowSpawned);

        GameLogger.DevLog("BrickGameNetworkSync", $"[Client] Row Spawned 수신: {message.RowIndex}");
    }
    #endregion

    #region Public API (Managers.Game.BrickGame.Network.Score 접근용)
    /// <summary>
    /// 현재 동기화된 점수 반환
    /// </summary>
    public int GetScore() => _currentScore;

    /// <summary>
    /// 현재 동기화된 레벨 반환
    /// </summary>
    public int GetLevel() => _currentLevel;

    /// <summary>
    /// 현재 동기화된 게임 상태 반환
    /// </summary>
    public GamePhase GetPhase() => _currentPhase;
    #endregion
}

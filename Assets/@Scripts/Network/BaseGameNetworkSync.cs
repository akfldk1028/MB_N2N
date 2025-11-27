using System;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 게임 네트워크 동기화 베이스 클래스
/// Server의 GameManager 이벤트를 NetworkedMessageChannel로 모든 Client에 동기화
/// </summary>
/// <typeparam name="TManager">동기화할 게임 매니저 타입 (BrickGameManager, ReleaseGameManager 등)</typeparam>
public abstract class BaseGameNetworkSync<TManager> : NetworkBehaviour where TManager : class
{
    #region References
    protected TManager _gameManager;
    protected bool _isInitialized;
    #endregion

    #region Initialization
    /// <summary>
    /// GameManager 연결 및 초기화
    /// </summary>
    public virtual void Initialize(TManager gameManager)
    {
        if (_isInitialized)
        {
            GameLogger.Warning(GetType().Name, "이미 초기화됨");
            return;
        }

        _gameManager = gameManager;
        _isInitialized = true;

        GameLogger.Success(GetType().Name, "GameManager 연결 완료");
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
            // Server: GameManager 이벤트 구독
            SubscribeToGameManagerEvents();
            GameLogger.Success(GetType().Name, "[Server] 이벤트 구독 완료");
        }
        else
        {
            // Client: NetworkedMessageChannel 메시지 구독
            SubscribeToNetworkMessages();
            GameLogger.Success(GetType().Name, "[Client] 네트워크 메시지 구독 완료");
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

    #region Abstract Methods (하위 클래스에서 구현)
    /// <summary>
    /// NetworkedMessageChannel 초기화
    /// </summary>
    protected abstract void InitializeChannels();

    /// <summary>
    /// NetworkedMessageChannel 정리
    /// </summary>
    protected abstract void DisposeChannels();

    /// <summary>
    /// [Server] GameManager 이벤트 구독
    /// </summary>
    protected abstract void SubscribeToGameManagerEvents();

    /// <summary>
    /// [Server] GameManager 이벤트 구독 해제
    /// </summary>
    protected abstract void UnsubscribeFromGameManagerEvents();

    /// <summary>
    /// [Client] Network 메시지 구독
    /// </summary>
    protected abstract void SubscribeToNetworkMessages();
    #endregion
}

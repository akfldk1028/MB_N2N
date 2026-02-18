using System;
using System.Collections.Generic;
using UnityEngine;
using MB.Infrastructure.Messages;

/// <summary>
/// BrickGame 이벤트 ↔ VFX 매핑 바인더.
/// ActionBus 이벤트를 구독하여 적절한 파티클 이펙트를 자동 재생.
/// VFXManager 자체는 게임 비종속이므로 이 클래스가 게임 특화 매핑을 담당.
/// </summary>
public class BrickGameVFXBinder : IDisposable
{
    private readonly List<IDisposable> _subscriptions = new();

    public void Initialize()
    {
        _subscriptions.Add(Managers.Subscribe(ActionId.BrickGame_BrickDestroyed, OnBrickDestroyed));
        _subscriptions.Add(Managers.Subscribe(ActionId.BrickGame_BallBounce, OnBallBounce));
        _subscriptions.Add(Managers.Subscribe(ActionId.BrickGame_GameStateChanged, OnGameStateChanged));
    }

    private void OnBrickDestroyed(ActionMessage msg)
    {
        if (msg.TryGetPayload<BrickDestroyedVFXPayload>(out var payload))
        {
            Managers.VFX?.SpawnVFX(VFXManager.VFX_BRICK_BREAK, payload.Position, payload.BrickColor);
        }
    }

    private void OnBallBounce(ActionMessage msg)
    {
        if (msg.TryGetPayload<BallBounceVFXPayload>(out var payload))
        {
            Managers.VFX?.SpawnVFX(VFXManager.VFX_BALL_BOUNCE, payload.Position);
        }
    }

    private void OnGameStateChanged(ActionMessage msg)
    {
        if (msg.TryGetPayload<BrickGameStatePayload>(out var payload))
        {
            switch (payload.Phase)
            {
                case GamePhase.StageClear:
                    Managers.VFX?.SpawnVFX(VFXManager.VFX_STAGE_CLEAR, Vector3.zero);
                    break;
                case GamePhase.Victory:
                    Managers.VFX?.SpawnVFX(VFXManager.VFX_VICTORY, Vector3.zero);
                    break;
            }
        }
    }

    public void Dispose()
    {
        foreach (var sub in _subscriptions)
            sub?.Dispose();
        _subscriptions.Clear();
    }
}

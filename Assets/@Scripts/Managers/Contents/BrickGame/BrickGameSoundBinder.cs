using System;
using System.Collections.Generic;
using MB.Infrastructure.Messages;

/// <summary>
/// BrickGame 이벤트 ↔ 사운드 매핑 바인더.
/// ActionBus 이벤트를 구독하여 적절한 SFX/BGM을 자동 재생.
/// SoundManager 자체는 게임 비종속이므로 이 클래스가 게임 특화 매핑을 담당.
/// </summary>
public class BrickGameSoundBinder : IDisposable
{
    private readonly List<IDisposable> _subscriptions = new();

    public void Initialize()
    {
        _subscriptions.Add(Managers.Subscribe(ActionId.BrickGame_BrickDestroyed, OnBrickDestroyed));
        _subscriptions.Add(Managers.Subscribe(ActionId.BrickGame_ScoreChanged, OnScoreChanged));
        _subscriptions.Add(Managers.Subscribe(ActionId.BrickGame_GameStateChanged, OnGameStateChanged));
        _subscriptions.Add(Managers.Subscribe(ActionId.BrickGame_GameOver, OnGameOver));
    }

    private void OnBrickDestroyed(ActionMessage msg)
    {
        Managers.Sound?.PlaySFX("sfx_brick_break");
    }

    private void OnScoreChanged(ActionMessage msg)
    {
        Managers.Sound?.PlaySFX("sfx_score");
    }

    private void OnGameStateChanged(ActionMessage msg)
    {
        if (msg.TryGetPayload<BrickGameStatePayload>(out var payload))
        {
            switch (payload.Phase)
            {
                case GamePhase.StageClear:
                    Managers.Sound?.PlaySFX("sfx_stage_clear");
                    break;
                case GamePhase.Victory:
                    Managers.Sound?.PlaySFX("sfx_victory");
                    Managers.Sound?.StopBGM();
                    break;
                case GamePhase.Playing:
                    Managers.Sound?.PlayBGM("bgm_main");
                    break;
            }
        }
    }

    private void OnGameOver(ActionMessage msg)
    {
        Managers.Sound?.PlaySFX("sfx_game_over");
        Managers.Sound?.StopBGM();
    }

    public void Dispose()
    {
        foreach (var sub in _subscriptions)
            sub?.Dispose();
        _subscriptions.Clear();
    }
}

using System;
using MB.Infrastructure.Messages;

/// <summary>
/// 대포 총알 규칙
/// - 점수 = 총알 개수
/// - 스페이스바 = 모든 총알 발사
/// - 발사된 총알은 상대방 게임에 영향
/// </summary>
public class CannonBulletRule : BaseGameRule
{
    #region 식별자
    public override string RuleId => "cannon_bullet";
    public override string DisplayName => "대포 총알 모드";
    #endregion

    #region Settings
    /// <summary>
    /// 한 번에 발사하는 총알 수 (0 = 전부)
    /// </summary>
    public int BulletsPerFire { get; set; } = 0;

    /// <summary>
    /// 발사 쿨다운 (초)
    /// </summary>
    public float FireCooldown { get; set; } = 0.5f;
    #endregion

    #region State
    private float _lastFireTime = 0f;
    private IDisposable _scoreSubscription;
    #endregion

    #region 라이프사이클
    public override void Initialize()
    {
        base.Initialize();

        // ActionBus 구독: 점수 변경 이벤트
        _scoreSubscription = Managers.Subscribe(
            ActionId.BrickGame_ScoreChanged,
            OnScoreChangedMessage
        );

        GameLogger.Success("CannonBulletRule", "초기화 완료 - 점수→총알 규칙 활성화");
    }

    public override void Cleanup()
    {
        _scoreSubscription?.Dispose();
        _scoreSubscription = null;

        base.Cleanup();
    }
    #endregion

    #region 점수 변경 처리
    /// <summary>
    /// ActionBus에서 점수 변경 메시지 수신
    /// </summary>
    private void OnScoreChangedMessage(ActionMessage message)
    {
        // 멀티플레이어 페이로드 처리
        if (message.TryGetPayload<MultiplayerScorePayload>(out var multiPayload))
        {
            // 내 점수 변경만 처리
            ulong localClientId = MultiplayerUtil.GetLocalClientId();

            if (multiPayload.ChangedClientId == localClientId)
            {
                int myNewScore = localClientId == 0
                    ? multiPayload.Player0Score
                    : multiPayload.Player1Score;

                // 현재 자원 개수와 비교하여 증가분만 추가
                int gained = myNewScore - _resourceCount;
                if (gained > 0)
                {
                    AddResource(gained);
                }
            }
        }
    }

    public override void OnScoreChanged(int oldScore, int newScore)
    {
        // 직접 호출 시 (싱글플레이어 등)
        int gained = newScore - oldScore;
        if (gained > 0)
        {
            AddResource(gained);
        }
    }
    #endregion

    #region 입력 처리
    public override void OnInput(GameInputAction action)
    {
        switch (action)
        {
            case GameInputAction.Fire:
                TryFire();
                break;

            case GameInputAction.FireAll:
                TryFireAll();
                break;
        }
    }

    /// <summary>
    /// 발사 시도
    /// </summary>
    private void TryFire()
    {
        if (!CanFire())
        {
            GameLogger.DevLog("CannonBulletRule", "발사 불가: 총알 없음");
            return;
        }

        // 쿨다운 체크
        float currentTime = UnityEngine.Time.time;
        if (currentTime - _lastFireTime < FireCooldown)
        {
            GameLogger.DevLog("CannonBulletRule", "발사 불가: 쿨다운 중");
            return;
        }

        // 발사할 총알 수 결정
        int toFire = BulletsPerFire > 0
            ? UnityEngine.Mathf.Min(BulletsPerFire, _resourceCount)
            : _resourceCount;

        // 총알 소비 및 발사
        if (ConsumeResource(toFire))
        {
            _lastFireTime = currentTime;
            NotifyFired(toFire);

            // ActionBus에 발사 이벤트 발행
            Managers.PublishAction(
                ActionId.BrickGame_BulletFired,
                new BulletFiredPayload(MultiplayerUtil.GetLocalClientId(), toFire)
            );

            GameLogger.Info("CannonBulletRule", $"총알 발사: {toFire}발, 남은: {_resourceCount}");
        }
    }

    /// <summary>
    /// 모든 총알 발사
    /// </summary>
    private void TryFireAll()
    {
        int original = BulletsPerFire;
        BulletsPerFire = 0; // 전부 발사
        TryFire();
        BulletsPerFire = original;
    }
    #endregion
}

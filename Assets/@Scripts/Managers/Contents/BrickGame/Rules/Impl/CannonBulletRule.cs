using System;
using MB.Infrastructure.Messages;

/// <summary>
/// 대포 총알 규칙
/// - 점수 = 총알 개수 (직접 동일!)
/// - 스페이스바 = 1발 발사, 점수 -1
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
    /// 한 번에 발사하는 총알 수
    /// </summary>
    public int BulletsPerFire { get; set; } = 1;

    /// <summary>
    /// 발사 쿨다운 (초)
    /// </summary>
    public float FireCooldown { get; set; } = 0.1f;
    #endregion

    #region State
    private float _lastFireTime = 0f;
    #endregion

    #region 라이프사이클
    public override void Initialize()
    {
        if (_initialized)
        {
            GameLogger.Warning("CannonBulletRule", "이미 초기화됨 - 스킵");
            return;
        }

        base.Initialize();
        _lastFireTime = 0f;

        GameLogger.Success("CannonBulletRule", "초기화 완료 - 점수=총알 규칙 활성화");
    }

    public override void Cleanup()
    {
        base.Cleanup();
    }

    public override void Reset()
    {
        base.Reset();
        _lastFireTime = 0f;
    }
    #endregion

    #region 점수 = 총알 (직접 사용)
    /// <summary>
    /// 현재 총알 개수 = 현재 점수
    /// </summary>
    public override int GetResourceCount()
    {
        return GetCurrentScore();
    }

    /// <summary>
    /// 발사 가능 여부 = 점수 > 0
    /// </summary>
    public override bool CanFire()
    {
        return GetCurrentScore() > 0;
    }

    /// <summary>
    /// 현재 점수 가져오기 (싱글/멀티 분기)
    /// </summary>
    private int GetCurrentScore()
    {
        if (Managers.Game?.BrickGame == null) return 0;
        return Managers.Game.BrickGame.Score;
    }

    /// <summary>
    /// 점수 차감 (발사 시)
    /// </summary>
    private bool SubtractScore(int amount)
    {
        if (amount <= 0) return false;

        int currentScore = GetCurrentScore();
        if (currentScore < amount) return false;

        // BrickGameManager에서 점수 차감
        Managers.Game?.BrickGame?.SubtractScore(amount);

        GameLogger.Info("CannonBulletRule", $"점수 차감: -{amount}, 남은 점수: {GetCurrentScore()}");
        return true;
    }

    public override void OnScoreChanged(int oldScore, int newScore)
    {
        // 점수 = 총알이므로 별도 처리 불필요
        // UI 업데이트용 이벤트만 발생
        NotifyResourceChanged();
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
        int currentScore = GetCurrentScore();

        if (currentScore <= 0)
        {
            GameLogger.DevLog("CannonBulletRule", "발사 불가: 점수(총알) 없음");
            return;
        }

        // 쿨다운 체크
        float currentTime = UnityEngine.Time.time;
        if (currentTime - _lastFireTime < FireCooldown)
        {
            return; // 쿨다운 중 - 로그 스팸 방지
        }

        // 발사할 총알 수 결정
        int toFire = BulletsPerFire > 0
            ? UnityEngine.Mathf.Min(BulletsPerFire, currentScore)
            : currentScore;

        // ✅ 점수 차감 (총알 소비)
        if (SubtractScore(toFire))
        {
            _lastFireTime = currentTime;
            NotifyFired(toFire);
            NotifyResourceChanged();

            // ActionBus에 발사 이벤트 발행
            Managers.PublishAction(
                ActionId.BrickGame_BulletFired,
                new BulletFiredPayload(MultiplayerUtil.GetLocalClientId(), toFire)
            );

            GameLogger.Info("CannonBulletRule", $"🔫 발사: {toFire}발, 남은 점수: {GetCurrentScore()}");
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

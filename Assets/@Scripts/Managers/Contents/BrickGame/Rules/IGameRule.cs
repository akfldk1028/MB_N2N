using System;

/// <summary>
/// 게임 규칙 인터페이스
/// 다양한 게임 모드를 지원하기 위한 Strategy Pattern
///
/// 구현 예시:
/// - CannonBulletRule: 점수 = 총알 개수, 스페이스바 = 발사
/// - ClassicBrickRule: 일반 블록깨기 규칙
/// </summary>
public interface IGameRule
{
    #region 식별자
    /// <summary>
    /// 규칙 고유 ID
    /// </summary>
    string RuleId { get; }

    /// <summary>
    /// 표시 이름
    /// </summary>
    string DisplayName { get; }
    #endregion

    #region 라이프사이클
    /// <summary>
    /// 초기화 (게임 시작 시)
    /// </summary>
    void Initialize();

    /// <summary>
    /// 정리 (게임 종료 시)
    /// </summary>
    void Cleanup();

    /// <summary>
    /// 리셋 (게임 재시작 시)
    /// </summary>
    void Reset();
    #endregion

    #region 점수/자원 관리
    /// <summary>
    /// 현재 사용 가능한 총알(자원) 개수
    /// </summary>
    int GetResourceCount();

    /// <summary>
    /// 발사 가능 여부
    /// </summary>
    bool CanFire();

    /// <summary>
    /// 점수 변경 시 호출
    /// </summary>
    void OnScoreChanged(int oldScore, int newScore);
    #endregion

    #region 입력 처리
    /// <summary>
    /// 입력 액션 처리
    /// </summary>
    void OnInput(GameInputAction action);
    #endregion

    #region 이벤트
    /// <summary>
    /// 자원(총알) 개수 변경 시 발생
    /// </summary>
    event Action<int> OnResourceCountChanged;

    /// <summary>
    /// 발사 시 발생 (발사 개수 전달)
    /// </summary>
    event Action<int> OnFired;
    #endregion
}

/// <summary>
/// 게임 입력 액션 열거형
/// </summary>
public enum GameInputAction
{
    None,
    Fire,           // 스페이스바 - 발사
    FireAll,        // 모든 총알 발사
    Pause,          // 일시정지
    Resume          // 재개
}

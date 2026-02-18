/// <summary>
/// 파워업 아이템의 종류를 정의하는 열거형
/// 벽돌 파괴 시 확률적으로 드롭되는 아이템 타입
/// </summary>
public enum PowerUpType
{
    /// <summary>
    /// 파워업 없음
    /// </summary>
    None = 0,

    /// <summary>
    /// 스타 - 공 공격력 부스트
    /// </summary>
    Star = 1,

    /// <summary>
    /// 보너스볼 - 추가 공 생성
    /// </summary>
    BonusBall = 2
}

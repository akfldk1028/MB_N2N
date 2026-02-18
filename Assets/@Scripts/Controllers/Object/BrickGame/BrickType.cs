namespace Unity.Assets.Scripts.Objects
{
    /// <summary>
    /// 벽돌 타입 정의 - 특수 벽돌 시스템
    /// Normal: 기본 HP 벽돌
    /// Explosion: 파괴 시 주변 3x3 범위 연쇄 파괴
    /// Steel: 공으로 파괴 불가, 총알로만 파괴
    /// Item: 파괴 시 반드시 파워업 드롭
    /// Gold: HP=1, 점수 5배
    /// </summary>
    public enum BrickType
    {
        Normal = 0,
        Explosion = 1,
        Steel = 2,
        Item = 3,
        Gold = 4
    }
}

using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// 맵 컴포넌트 타입
    /// </summary>
    public enum MapComponentType
    {
        Block,      // 블록 (색 변경 가능)
        Cannon,     // 대포 (발사/피격)
        Obstacle,   // 장애물 (총알 막음)
        Machine,    // 기계 (특수 효과)
        Trap,       // 함정 (데미지)
        Portal,     // 포탈 (총알 이동)
        Wall        // 벽 (경계)
    }

    /// <summary>
    /// 맵 컴포넌트 기본 인터페이스
    /// 모든 맵 요소(블록, 대포, 장애물 등)가 구현
    /// </summary>
    public interface IMapComponent
    {
        #region 식별자
        /// <summary>
        /// 컴포넌트 고유 ID
        /// </summary>
        string ComponentId { get; }

        /// <summary>
        /// 컴포넌트 타입
        /// </summary>
        MapComponentType ComponentType { get; }
        #endregion

        #region 소유권
        /// <summary>
        /// 소유자 플레이어 ID (-1 = 중립)
        /// </summary>
        int OwnerPlayerId { get; }

        /// <summary>
        /// 소유자 색상
        /// </summary>
        Color OwnerColor { get; }

        /// <summary>
        /// 소유권 변경 가능 여부
        /// </summary>
        bool CanChangeOwner { get; }

        /// <summary>
        /// 소유권 변경
        /// </summary>
        bool SetOwner(int playerId, Color color);
        #endregion

        #region 위치
        /// <summary>
        /// 그리드 좌표 (x, y)
        /// </summary>
        Vector2Int GridPosition { get; }

        /// <summary>
        /// 월드 좌표
        /// </summary>
        Vector3 WorldPosition { get; }
        #endregion

        #region 상호작용
        /// <summary>
        /// 총알 충돌 시 호출
        /// </summary>
        /// <param name="bullet">충돌한 총알</param>
        /// <returns>총알 파괴 여부</returns>
        bool OnBulletHit(IBullet bullet);

        /// <summary>
        /// 컴포넌트 초기화
        /// </summary>
        void Initialize(Vector2Int gridPos, int ownerId, Color color);

        /// <summary>
        /// 컴포넌트 정리
        /// </summary>
        void Cleanup();
        #endregion
    }

    /// <summary>
    /// 총알 인터페이스
    /// </summary>
    public interface IBullet
    {
        int OwnerPlayerId { get; }
        Color OwnerColor { get; }
        int Damage { get; }
        void Destroy();
    }

    /// <summary>
    /// 대포 전용 인터페이스
    /// </summary>
    public interface ICannon : IMapComponent
    {
        /// <summary>
        /// 체력 (0이 되면 게임 오버)
        /// </summary>
        int Health { get; }

        /// <summary>
        /// 최대 체력
        /// </summary>
        int MaxHealth { get; }

        /// <summary>
        /// 발사 위치
        /// </summary>
        Transform FirePoint { get; }

        /// <summary>
        /// 총알 발사
        /// </summary>
        void Fire(Vector3 direction, float speed);

        /// <summary>
        /// 데미지 받음
        /// </summary>
        /// <returns>사망 여부</returns>
        bool TakeDamage(int damage);

        /// <summary>
        /// 대포 파괴 시 이벤트
        /// </summary>
        event System.Action<ICannon> OnCannonDestroyed;
    }

    /// <summary>
    /// 블록 전용 인터페이스
    /// </summary>
    public interface IBlock : IMapComponent
    {
        /// <summary>
        /// 블록 내구도 (0이면 파괴)
        /// </summary>
        int Durability { get; }

        /// <summary>
        /// 블록 파괴 가능 여부
        /// </summary>
        bool IsDestructible { get; }
    }

    /// <summary>
    /// 장애물 전용 인터페이스
    /// </summary>
    public interface IObstacle : IMapComponent
    {
        /// <summary>
        /// 총알 통과 가능 여부
        /// </summary>
        bool IsPassable { get; }

        /// <summary>
        /// 총알 반사 여부
        /// </summary>
        bool IsReflective { get; }
    }

    /// <summary>
    /// 기계 전용 인터페이스
    /// </summary>
    public interface IMachine : IMapComponent
    {
        /// <summary>
        /// 기계 활성화 상태
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        /// 기계 활성화/비활성화
        /// </summary>
        void SetActive(bool active);

        /// <summary>
        /// 기계 효과 발동
        /// </summary>
        void Activate();
    }
}

using UnityEngine;
using System.Collections.Generic;

namespace Game.Core
{
    /// <summary>
    /// 맵 레이아웃 인터페이스
    /// 다양한 맵 형태를 지원하기 위한 Strategy Pattern
    ///
    /// 구현 예시:
    /// - DiagonalLayout: 2인 대각선 분할
    /// - QuadrantLayout: 4인 4분할
    /// - HexagonLayout: 6인 육각형
    /// </summary>
    public interface IMapLayout
    {
        #region 식별자
        /// <summary>
        /// 레이아웃 ID
        /// </summary>
        string LayoutId { get; }

        /// <summary>
        /// 표시 이름
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        /// 지원 플레이어 수
        /// </summary>
        int PlayerCount { get; }
        #endregion

        #region 그리드 설정
        /// <summary>
        /// 그리드 X 크기
        /// </summary>
        int GridSizeX { get; }

        /// <summary>
        /// 그리드 Y 크기
        /// </summary>
        int GridSizeY { get; }
        #endregion

        #region 컴포넌트 배치
        /// <summary>
        /// 블록 배치 정보 반환
        /// </summary>
        IEnumerable<BlockPlacement> GetBlockPlacements();

        /// <summary>
        /// 대포 배치 정보 반환
        /// </summary>
        IEnumerable<CannonPlacement> GetCannonPlacements();

        /// <summary>
        /// 장애물 배치 정보 반환
        /// </summary>
        IEnumerable<ObstaclePlacement> GetObstaclePlacements();

        /// <summary>
        /// 특수 컴포넌트 배치 정보 반환
        /// </summary>
        IEnumerable<ComponentPlacement> GetSpecialPlacements();
        #endregion

        #region 소유권 계산
        /// <summary>
        /// 좌표 기반 초기 소유자 ID 계산
        /// </summary>
        int GetInitialOwnerId(int x, int y);

        /// <summary>
        /// 플레이어별 색상 반환
        /// </summary>
        Color GetPlayerColor(int playerId);
        #endregion
    }

    #region 배치 데이터 구조체
    /// <summary>
    /// 컴포넌트 배치 기본 정보
    /// </summary>
    [System.Serializable]
    public struct ComponentPlacement
    {
        public Vector2Int gridPosition;
        public MapComponentType componentType;
        public int initialOwnerId;
        public string prefabKey; // Addressable 또는 Resources 키

        public ComponentPlacement(Vector2Int pos, MapComponentType type, int ownerId = -1, string prefab = null)
        {
            gridPosition = pos;
            componentType = type;
            initialOwnerId = ownerId;
            prefabKey = prefab;
        }
    }

    /// <summary>
    /// 블록 배치 정보
    /// </summary>
    [System.Serializable]
    public struct BlockPlacement
    {
        public Vector2Int gridPosition;
        public int initialOwnerId;
        public int durability;
        public bool isDestructible;

        public BlockPlacement(Vector2Int pos, int ownerId, int dur = 1, bool destructible = false)
        {
            gridPosition = pos;
            initialOwnerId = ownerId;
            durability = dur;
            isDestructible = destructible;
        }
    }

    /// <summary>
    /// 대포 배치 정보
    /// </summary>
    [System.Serializable]
    public struct CannonPlacement
    {
        public Vector2Int gridPosition;
        public int playerId;
        public int maxHealth;
        public float rotationAngle; // 초기 회전 각도

        public CannonPlacement(Vector2Int pos, int player, int health = 100, float rotation = 0f)
        {
            gridPosition = pos;
            playerId = player;
            maxHealth = health;
            rotationAngle = rotation;
        }
    }

    /// <summary>
    /// 장애물 배치 정보
    /// </summary>
    [System.Serializable]
    public struct ObstaclePlacement
    {
        public Vector2Int gridPosition;
        public string obstacleType; // "wall", "rock", "machine" 등
        public bool isPassable;
        public bool isReflective;
        public bool isDestructible;

        public ObstaclePlacement(Vector2Int pos, string type, bool passable = false, bool reflective = false, bool destructible = false)
        {
            gridPosition = pos;
            obstacleType = type;
            isPassable = passable;
            isReflective = reflective;
            isDestructible = destructible;
        }
    }
    #endregion
}

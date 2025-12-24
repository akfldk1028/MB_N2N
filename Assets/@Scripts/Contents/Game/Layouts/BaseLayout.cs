using UnityEngine;
using System.Collections.Generic;
using Game.Core;
using Game.Data;

namespace Game.Layouts
{
    /// <summary>
    /// 맵 레이아웃 기본 클래스
    /// 공통 기능 제공 (색상, 그리드 설정 등)
    /// </summary>
    public abstract class BaseLayout : IMapLayout
    {
        #region Fields
        protected readonly MapData _mapData;
        protected readonly Color[] _playerColors;
        #endregion

        #region Properties
        public abstract string LayoutId { get; }
        public abstract string DisplayName { get; }
        public virtual int PlayerCount => _mapData.playerCount;
        public virtual int GridSizeX => _mapData.gridSizeX;
        public virtual int GridSizeY => _mapData.gridSizeY;
        #endregion

        #region Constructor
        protected BaseLayout(MapData mapData)
        {
            _mapData = mapData;
            _playerColors = mapData.playerColors;
        }
        #endregion

        #region Abstract Methods
        public abstract int GetInitialOwnerId(int x, int y);
        #endregion

        #region Virtual Methods (Override 가능)
        public virtual IEnumerable<BlockPlacement> GetBlockPlacements()
        {
            for (int x = 0; x < GridSizeX; x++)
            {
                for (int y = 0; y < GridSizeY; y++)
                {
                    int ownerId = GetInitialOwnerId(x, y);
                    yield return new BlockPlacement(
                        new Vector2Int(x, y),
                        ownerId,
                        dur: 1,
                        destructible: false
                    );
                }
            }
        }

        public abstract IEnumerable<CannonPlacement> GetCannonPlacements();

        public virtual IEnumerable<ObstaclePlacement> GetObstaclePlacements()
        {
            // 기본: 장애물 없음
            // MapData에 정의된 장애물이 있으면 반환
            if (_mapData.obstacles != null)
            {
                foreach (var obs in _mapData.obstacles)
                {
                    yield return new ObstaclePlacement(
                        obs.gridPosition,
                        obs.obstacleType,
                        obs.isPassable,
                        obs.isReflective,
                        obs.isDestructible
                    );
                }
            }
        }

        public virtual IEnumerable<ComponentPlacement> GetSpecialPlacements()
        {
            // 기본: 특수 컴포넌트 없음
            if (_mapData.specialComponents != null)
            {
                foreach (var comp in _mapData.specialComponents)
                {
                    yield return new ComponentPlacement(
                        comp.gridPosition,
                        comp.componentType,
                        comp.initialOwnerId,
                        comp.prefab?.name
                    );
                }
            }
        }

        public virtual Color GetPlayerColor(int playerId)
        {
            if (playerId < 0 || playerId >= _playerColors.Length)
                return Color.gray;
            return _playerColors[playerId];
        }
        #endregion
    }
}

using UnityEngine;
using System.Collections.Generic;
using Game.Core;
using Game.Data;

namespace Game.Layouts
{
    /// <summary>
    /// 4분할 레이아웃 (4인용)
    ///
    /// 맵 구조:
    /// ┌────┬────┐
    /// │ P2 │ P3 │
    /// ├────┼────┤
    /// │ P0 │ P1 │
    /// └────┴────┘
    ///
    /// Player 0: 왼쪽 하단 (노랑)
    /// Player 1: 오른쪽 하단 (초록)
    /// Player 2: 왼쪽 상단 (빨강)
    /// Player 3: 오른쪽 상단 (보라)
    /// 대포: 각 코너
    /// </summary>
    public class QuadrantLayout : BaseLayout
    {
        public override string LayoutId => "quadrant";
        public override string DisplayName => "4분할";

        public QuadrantLayout(MapData mapData) : base(mapData) { }

        /// <summary>
        /// 4분할 소유권 결정
        /// </summary>
        public override int GetInitialOwnerId(int x, int y)
        {
            int halfX = GridSizeX / 2;
            int halfY = GridSizeY / 2;

            if (x < halfX && y < halfY) return 0; // 왼쪽 하단
            if (x >= halfX && y < halfY) return 1; // 오른쪽 하단
            if (x < halfX && y >= halfY) return 2; // 왼쪽 상단
            return 3; // 오른쪽 상단
        }

        public override IEnumerable<CannonPlacement> GetCannonPlacements()
        {
            // Player 0: 왼쪽 하단 모서리
            yield return new CannonPlacement(
                new Vector2Int(0, 0),
                player: 0,
                health: _mapData.cannonMaxHealth,
                rotation: 45f // 오른쪽 위를 향함
            );

            // Player 1: 오른쪽 하단 모서리
            yield return new CannonPlacement(
                new Vector2Int(GridSizeX - 1, 0),
                player: 1,
                health: _mapData.cannonMaxHealth,
                rotation: 135f // 왼쪽 위를 향함
            );

            // Player 2: 왼쪽 상단 모서리
            yield return new CannonPlacement(
                new Vector2Int(0, GridSizeY - 1),
                player: 2,
                health: _mapData.cannonMaxHealth,
                rotation: -45f // 오른쪽 아래를 향함
            );

            // Player 3: 오른쪽 상단 모서리
            yield return new CannonPlacement(
                new Vector2Int(GridSizeX - 1, GridSizeY - 1),
                player: 3,
                health: _mapData.cannonMaxHealth,
                rotation: -135f // 왼쪽 아래를 향함
            );
        }
    }
}

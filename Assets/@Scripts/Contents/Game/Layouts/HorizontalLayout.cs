using UnityEngine;
using System.Collections.Generic;
using Game.Core;
using Game.Data;

namespace Game.Layouts
{
    /// <summary>
    /// 수평 2분할 레이아웃 (2인용)
    ///
    /// 맵 구조:
    /// ┌─────────┐
    /// │   P1    │
    /// ├─────────┤
    /// │   P0    │
    /// └─────────┘
    ///
    /// Player 0: 하단 (노랑)
    /// Player 1: 상단 (초록)
    /// 대포: 상하 중앙
    /// </summary>
    public class HorizontalLayout : BaseLayout
    {
        public override string LayoutId => "horizontal";
        public override string DisplayName => "수평 2분할";

        public HorizontalLayout(MapData mapData) : base(mapData) { }

        public override int GetInitialOwnerId(int x, int y)
        {
            int halfY = GridSizeY / 2;
            return (y < halfY) ? 0 : 1;
        }

        public override IEnumerable<CannonPlacement> GetCannonPlacements()
        {
            int centerX = GridSizeX / 2;

            // Player 0: 하단 중앙
            yield return new CannonPlacement(
                new Vector2Int(centerX, 0),
                player: 0,
                health: _mapData.cannonMaxHealth,
                rotation: 90f // 위를 향함
            );

            // Player 1: 상단 중앙
            yield return new CannonPlacement(
                new Vector2Int(centerX, GridSizeY - 1),
                player: 1,
                health: _mapData.cannonMaxHealth,
                rotation: -90f // 아래를 향함
            );
        }
    }
}

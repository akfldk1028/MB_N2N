using UnityEngine;
using System.Collections.Generic;
using Game.Core;
using Game.Data;

namespace Game.Layouts
{
    /// <summary>
    /// 수직 2분할 레이아웃 (2인용)
    ///
    /// 맵 구조:
    /// ┌────┬────┐
    /// │    │    │
    /// │ P0 │ P1 │
    /// │    │    │
    /// └────┴────┘
    ///
    /// Player 0: 좌측 (노랑)
    /// Player 1: 우측 (초록)
    /// 대포: 좌우 중앙
    /// </summary>
    public class VerticalLayout : BaseLayout
    {
        public override string LayoutId => "vertical";
        public override string DisplayName => "수직 2분할";

        public VerticalLayout(MapData mapData) : base(mapData) { }

        public override int GetInitialOwnerId(int x, int y)
        {
            int halfX = GridSizeX / 2;
            return (x < halfX) ? 0 : 1;
        }

        public override IEnumerable<CannonPlacement> GetCannonPlacements()
        {
            int centerY = GridSizeY / 2;

            // Player 0: 좌측 중앙
            yield return new CannonPlacement(
                new Vector2Int(0, centerY),
                player: 0,
                health: _mapData.cannonMaxHealth,
                rotation: 0f // 오른쪽을 향함
            );

            // Player 1: 우측 중앙
            yield return new CannonPlacement(
                new Vector2Int(GridSizeX - 1, centerY),
                player: 1,
                health: _mapData.cannonMaxHealth,
                rotation: 180f // 왼쪽을 향함
            );
        }
    }
}

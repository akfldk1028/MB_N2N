using UnityEngine;
using System.Collections.Generic;
using Game.Core;
using Game.Data;

namespace Game.Layouts
{
    /// <summary>
    /// 대각선 2분할 레이아웃 (2인용)
    ///
    /// 맵 구조:
    /// ┌─────────┐
    /// │ P1      │
    /// │   ╲     │
    /// │     ╲   │
    /// │       P0│
    /// └─────────┘
    ///
    /// Player 0: 오른쪽 하단 (노랑)
    /// Player 1: 왼쪽 상단 (초록)
    /// 대포: 양 대각선 끝
    /// </summary>
    public class DiagonalLayout : BaseLayout
    {
        public override string LayoutId => "diagonal";
        public override string DisplayName => "대각선 2분할";

        public DiagonalLayout(MapData mapData) : base(mapData) { }

        /// <summary>
        /// 대각선 기준 소유권 결정
        /// x + y > (gridSizeX + gridSizeY) / 2 → Player 0
        /// 그 외 → Player 1
        /// </summary>
        public override int GetInitialOwnerId(int x, int y)
        {
            float diagonalLine = (GridSizeX + GridSizeY - 2) / 2f;
            return (x + y > diagonalLine) ? 0 : 1;
        }

        public override IEnumerable<CannonPlacement> GetCannonPlacements()
        {
            // Player 0: 오른쪽 하단 모서리
            yield return new CannonPlacement(
                new Vector2Int(GridSizeX - 1, 0),
                player: 0,
                health: _mapData.cannonMaxHealth,
                rotation: 135f // 왼쪽 위를 향함
            );

            // Player 1: 왼쪽 상단 모서리
            yield return new CannonPlacement(
                new Vector2Int(0, GridSizeY - 1),
                player: 1,
                health: _mapData.cannonMaxHealth,
                rotation: -45f // 오른쪽 아래를 향함
            );
        }
    }
}

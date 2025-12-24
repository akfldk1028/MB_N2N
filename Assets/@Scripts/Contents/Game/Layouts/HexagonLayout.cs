using UnityEngine;
using System.Collections.Generic;
using Game.Core;
using Game.Data;

namespace Game.Layouts
{
    /// <summary>
    /// 육각형 6분할 레이아웃 (6인용)
    ///
    /// 맵 구조 (정육각형):
    ///       ╱─────╲
    ///      ╱ P5  P4 ╲
    ///     │    ╱╲    │
    ///     │ P0  ╳ P3 │
    ///     │    ╲╱    │
    ///      ╲ P1  P2 ╱
    ///       ╲─────╱
    ///
    /// 6개 파이 조각으로 분할
    /// 대포: 각 섹터의 외곽 중앙
    /// </summary>
    public class HexagonLayout : BaseLayout
    {
        public override string LayoutId => "hexagon";
        public override string DisplayName => "육각형 6분할";

        public HexagonLayout(MapData mapData) : base(mapData) { }

        /// <summary>
        /// 육각형 파이 조각 소유권 결정
        /// 중심에서의 각도로 6개 섹터 구분
        /// </summary>
        public override int GetInitialOwnerId(int x, int y)
        {
            float centerX = (GridSizeX - 1) / 2f;
            float centerY = (GridSizeY - 1) / 2f;

            float dx = x - centerX;
            float dy = y - centerY;

            // 중심일 경우 중립
            if (Mathf.Approximately(dx, 0) && Mathf.Approximately(dy, 0))
                return -1;

            // 각도 계산 (0 ~ 360)
            float angle = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
            if (angle < 0) angle += 360f;

            // 6개 섹터로 분할 (각 60도)
            int sector = Mathf.FloorToInt(angle / 60f);
            return sector % 6;
        }

        public override IEnumerable<CannonPlacement> GetCannonPlacements()
        {
            float centerX = (GridSizeX - 1) / 2f;
            float centerY = (GridSizeY - 1) / 2f;
            float radius = Mathf.Min(GridSizeX, GridSizeY) / 2f - 1;

            for (int i = 0; i < 6; i++)
            {
                // 60도 간격으로 배치
                float angle = (60f * i + 30f) * Mathf.Deg2Rad; // 30도 오프셋 (중앙)
                int x = Mathf.RoundToInt(centerX + radius * Mathf.Cos(angle));
                int y = Mathf.RoundToInt(centerY + radius * Mathf.Sin(angle));

                // 그리드 범위 체크
                x = Mathf.Clamp(x, 0, GridSizeX - 1);
                y = Mathf.Clamp(y, 0, GridSizeY - 1);

                yield return new CannonPlacement(
                    new Vector2Int(x, y),
                    player: i,
                    health: _mapData.cannonMaxHealth,
                    rotation: 60f * i + 210f // 중심을 향함
                );
            }
        }
    }
}

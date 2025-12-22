using UnityEngine;

namespace MB.Network.Factories
{
    /// <summary>
    /// BrickGame 멀티플레이어 Borders(벽) 생성 전담 Factory
    /// - 플레이어별 물리 벽 생성 (BoxCollider2D + SpriteRenderer)
    /// - Server/Client 동기화 지원
    /// </summary>
    public class BrickGameBordersFactory
    {
        #region Cached Resources
        private Sprite _cachedWhiteSprite;
        #endregion

        #region Configuration
        private readonly Color _borderColor = new Color(0.227f, 0.227f, 0.227f, 1f);
        private const float BORDER_THICKNESS = 1f;
        private const float TOP_OFFSET = 12f;
        private const float BOTTOM_OFFSET = 1f;
        #endregion

        #region Public API
        /// <summary>
        /// 플레이어별 물리 벽(borders) 생성
        /// </summary>
        /// <param name="clientId">플레이어 ID</param>
        /// <param name="leftX">왼쪽 경계 X 좌표</param>
        /// <param name="rightX">오른쪽 경계 X 좌표</param>
        /// <param name="plankY">패들 Y 좌표</param>
        /// <returns>Borders Container GameObject</returns>
        public GameObject CreateBorders(ulong clientId, float leftX, float rightX, float plankY)
        {
            GameObject container = new GameObject($"Borders_Player{clientId}");

            // 영역 계산
            var area = CalculateArea(leftX, rightX, plankY);

            // 4개 벽 생성
            CreateLeftWall(container, clientId, area);
            CreateRightWall(container, clientId, area);
            CreateTopWall(container, clientId, area);
            CreateBottomWall(container, clientId, area);

            GameLogger.Success("BrickGameBordersFactory",
                $"[Player {clientId}] Borders 생성 완료 - L:{leftX:F1}, R:{rightX:F1}, T:{area.topY:F1}, B:{area.bottomY:F1}");

            return container;
        }

        /// <summary>
        /// Client용 Borders 생성 (ClientRpc에서 호출)
        /// </summary>
        public GameObject CreateBordersForClient(ulong clientId, float leftX, float rightX, float plankY)
        {
            GameObject container = new GameObject($"Borders_Player{clientId}_Client");

            var area = CalculateArea(leftX, rightX, plankY);

            CreateLeftWall(container, clientId, area, "_Client");
            CreateRightWall(container, clientId, area, "_Client");
            CreateTopWall(container, clientId, area, "_Client");
            CreateBottomWall(container, clientId, area, "_Client");

            GameLogger.Success("BrickGameBordersFactory",
                $"[Client] Player {clientId} Borders 생성 완료!");

            return container;
        }
        #endregion

        #region Area Calculation
        private struct BorderArea
        {
            public float leftX;
            public float rightX;
            public float centerX;
            public float bottomY;
            public float topY;
            public float centerY;
            public float width;
            public float height;
        }

        private BorderArea CalculateArea(float leftX, float rightX, float plankY)
        {
            float bottomY = plankY - BOTTOM_OFFSET;
            float topY = plankY + TOP_OFFSET;

            return new BorderArea
            {
                leftX = leftX,
                rightX = rightX,
                centerX = (leftX + rightX) / 2f,
                bottomY = bottomY,
                topY = topY,
                centerY = (topY + bottomY) / 2f,
                width = Mathf.Abs(rightX - leftX),
                height = topY - bottomY
            };
        }
        #endregion

        #region Wall Creation
        private void CreateLeftWall(GameObject parent, ulong clientId, BorderArea area, string suffix = "")
        {
            var wall = CreateBorderWall(
                $"borderLeft_Player{clientId}{suffix}",
                new Vector3(area.leftX - 0.5f, area.centerY, 0),
                new Vector3(BORDER_THICKNESS, area.height, 1f),
                "Wall",
                false
            );
            wall.transform.SetParent(parent.transform);
        }

        private void CreateRightWall(GameObject parent, ulong clientId, BorderArea area, string suffix = "")
        {
            var wall = CreateBorderWall(
                $"borderRight_Player{clientId}{suffix}",
                new Vector3(area.rightX + 0.5f, area.centerY, 0),
                new Vector3(BORDER_THICKNESS, area.height, 1f),
                "Wall",
                false
            );
            wall.transform.SetParent(parent.transform);
        }

        private void CreateTopWall(GameObject parent, ulong clientId, BorderArea area, string suffix = "")
        {
            var wall = CreateBorderWall(
                $"borderTop_Player{clientId}{suffix}",
                new Vector3(area.centerX, area.topY + 0.5f, 0),
                new Vector3(area.width + 2f, BORDER_THICKNESS, 1f),
                "Wall",
                false
            );
            wall.transform.SetParent(parent.transform);
        }

        private void CreateBottomWall(GameObject parent, ulong clientId, BorderArea area, string suffix = "")
        {
            var wall = CreateBorderWall(
                $"borderBottom_Player{clientId}{suffix}",
                new Vector3(area.centerX, area.bottomY - 0.5f, 0),
                new Vector3(area.width + 2f, BORDER_THICKNESS, 1f),
                "BottomBoundary",
                true  // IsTrigger for game over detection
            );
            wall.transform.SetParent(parent.transform);
        }
        #endregion

        #region Helper Methods
        /// <summary>
        /// 벽 GameObject 생성 (BoxCollider2D + SpriteRenderer)
        /// </summary>
        private GameObject CreateBorderWall(string name, Vector3 position, Vector3 scale, string tag, bool isTrigger)
        {
            GameObject wall = new GameObject(name);
            wall.transform.position = position;
            wall.transform.localScale = scale;
            wall.tag = tag;

            // BoxCollider2D
            BoxCollider2D collider = wall.AddComponent<BoxCollider2D>();
            collider.size = Vector2.one;
            collider.isTrigger = isTrigger;

            // SpriteRenderer (시각적 표시)
            SpriteRenderer renderer = wall.AddComponent<SpriteRenderer>();
            renderer.sprite = GetWhiteSquareSprite();
            renderer.color = _borderColor;
            renderer.sortingOrder = 1;

            return wall;
        }

        /// <summary>
        /// 1x1 흰색 스프라이트 생성 (캐싱)
        /// </summary>
        private Sprite GetWhiteSquareSprite()
        {
            if (_cachedWhiteSprite != null) return _cachedWhiteSprite;

            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();

            _cachedWhiteSprite = Sprite.Create(
                texture,
                new Rect(0, 0, 1, 1),
                new Vector2(0.5f, 0.5f),
                1f
            );

            return _cachedWhiteSprite;
        }
        #endregion
    }
}

using UnityEngine;
using System.Collections.Generic;
using Game.Core;

namespace Game.Data
{
    /// <summary>
    /// 맵 데이터 ScriptableObject
    /// Unity Inspector에서 맵 설정 가능
    ///
    /// 사용법:
    /// 1. Project 창에서 우클릭 → Create → Game → Map Data
    /// 2. Inspector에서 맵 설정
    /// 3. BattleFieldManager.LoadMap(mapData) 호출
    /// </summary>
    [CreateAssetMenu(fileName = "NewMapData", menuName = "Game/Map Data", order = 1)]
    public class MapData : ScriptableObject
    {
        #region 기본 정보
        [Header("기본 정보")]
        [Tooltip("맵 고유 ID")]
        public string mapId;

        [Tooltip("맵 표시 이름")]
        public string displayName;

        [Tooltip("맵 설명")]
        [TextArea(2, 4)]
        public string description;

        [Tooltip("맵 썸네일")]
        public Sprite thumbnail;
        #endregion

        #region 플레이어 설정
        [Header("플레이어 설정")]
        [Tooltip("지원 플레이어 수")]
        [Range(2, 8)]
        public int playerCount = 2;

        [Tooltip("플레이어별 색상")]
        public Color[] playerColors = new Color[]
        {
            new Color(1f, 0.8f, 0f),     // Player 0: 노랑
            new Color(0.5f, 0.8f, 0.5f), // Player 1: 초록
            new Color(1f, 0f, 0f),       // Player 2: 빨강
            new Color(0.5f, 0.3f, 1f)    // Player 3: 보라
        };
        #endregion

        #region 그리드 설정
        [Header("그리드 설정")]
        [Tooltip("그리드 X 크기")]
        [Range(5, 50)]
        public int gridSizeX = 20;

        [Tooltip("그리드 Y 크기")]
        [Range(5, 50)]
        public int gridSizeY = 20;

        [Tooltip("큐브 크기")]
        public float cubeSize = 1.0f;

        [Tooltip("큐브 간격")]
        public float spacing = 0.05f;

        [Tooltip("그리드 높이")]
        public float gridHeight = 0.2f;
        #endregion

        #region 레이아웃 설정
        [Header("레이아웃 설정")]
        [Tooltip("레이아웃 타입")]
        public MapLayoutType layoutType = MapLayoutType.Diagonal;

        [Tooltip("커스텀 레이아웃 (선택사항)")]
        public TextAsset customLayoutJson;
        #endregion

        #region 대포 설정
        [Header("대포 설정")]
        [Tooltip("대포 최대 체력")]
        public int cannonMaxHealth = 100;

        [Tooltip("대포 배치 (자동 계산 안 할 경우)")]
        public CannonPlacementData[] customCannonPlacements;
        #endregion

        #region 장애물 설정
        [Header("장애물 설정")]
        [Tooltip("장애물 배치")]
        public ObstaclePlacementData[] obstacles;
        #endregion

        #region 특수 컴포넌트
        [Header("특수 컴포넌트")]
        [Tooltip("기계, 함정, 포탈 등")]
        public SpecialComponentData[] specialComponents;
        #endregion

        #region Prefab 참조
        [Header("Prefab 참조")]
        public GameObject blockPrefab;
        public GameObject cannonPrefab;
        public GameObject wallPrefab;
        public GameObject bulletPrefab;
        #endregion

        #region 유효성 검사
        private void OnValidate()
        {
            // 플레이어 색상 배열 크기 조정
            if (playerColors == null || playerColors.Length < playerCount)
            {
                var newColors = new Color[playerCount];
                for (int i = 0; i < playerCount; i++)
                {
                    newColors[i] = (playerColors != null && i < playerColors.Length)
                        ? playerColors[i]
                        : GetDefaultColor(i);
                }
                playerColors = newColors;
            }
        }

        private Color GetDefaultColor(int index)
        {
            Color[] defaults = new Color[]
            {
                new Color(1f, 0.8f, 0f),     // 노랑
                new Color(0.5f, 0.8f, 0.5f), // 초록
                new Color(1f, 0f, 0f),       // 빨강
                new Color(0.5f, 0.3f, 1f),   // 보라
                new Color(0f, 0.8f, 1f),     // 하늘
                new Color(1f, 0.5f, 0f),     // 주황
                new Color(1f, 0.5f, 1f),     // 분홍
                new Color(0.5f, 0.5f, 0.5f)  // 회색
            };
            return defaults[index % defaults.Length];
        }
        #endregion
    }

    #region 레이아웃 타입
    public enum MapLayoutType
    {
        Diagonal,   // 대각선 2분할
        Quadrant,   // 4분할
        Horizontal, // 수평 2분할
        Vertical,   // 수직 2분할
        Hexagon,    // 육각형 (6인)
        Custom      // 커스텀 JSON
    }
    #endregion

    #region 직렬화용 데이터 클래스
    [System.Serializable]
    public class CannonPlacementData
    {
        public int playerId;
        public Vector2Int gridPosition;
        public float rotationAngle;
    }

    [System.Serializable]
    public class ObstaclePlacementData
    {
        public string obstacleType;
        public Vector2Int gridPosition;
        public bool isPassable;
        public bool isReflective;
        public bool isDestructible;
        public GameObject prefabOverride;
    }

    [System.Serializable]
    public class SpecialComponentData
    {
        public string componentId;
        public MapComponentType componentType;
        public Vector2Int gridPosition;
        public int initialOwnerId;
        public GameObject prefab;
        [TextArea(1, 3)]
        public string customData; // JSON 형태의 추가 데이터
    }
    #endregion
}

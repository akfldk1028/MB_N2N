using UnityEngine;
using System;
using System.Collections.Generic;
using Game.Data;
using Game.Layouts;

namespace Game.Core
{
    /// <summary>
    /// 전장 맵 관리 싱글톤
    ///
    /// 역할:
    /// 1. MapData 로드 및 맵 인스턴스화
    /// 2. 모든 맵 컴포넌트 라이프사이클 관리
    /// 3. 블록 소유권 관리
    /// 4. 대포 파괴 감지 → 게임 종료
    ///
    /// 사용법:
    /// BattleFieldManager.Instance.LoadMap(mapData);
    /// BattleFieldManager.Instance.OnCannonDestroyed += HandleGameOver;
    /// </summary>
    public class BattleFieldManager : MonoBehaviour
    {
        #region Singleton
        private static BattleFieldManager _instance;
        public static BattleFieldManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<BattleFieldManager>();
                    if (_instance == null)
                    {
                        var go = new GameObject("[BattleFieldManager]");
                        _instance = go.AddComponent<BattleFieldManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }
        #endregion

        #region Events
        /// <summary>
        /// 대포 파괴 시 이벤트 (playerId, cannon)
        /// </summary>
        public event Action<int, ICannon> OnCannonDestroyed;

        /// <summary>
        /// 게임 종료 시 이벤트 (winnerPlayerId)
        /// </summary>
        public event Action<int> OnGameOver;

        /// <summary>
        /// 블록 소유권 변경 시 이벤트 (block, newOwnerId)
        /// </summary>
        public event Action<IBlock, int> OnBlockOwnerChanged;

        /// <summary>
        /// 맵 로드 완료 시 이벤트
        /// </summary>
        public event Action OnMapLoaded;
        #endregion

        #region State
        private MapData _currentMapData;
        private IMapLayout _currentLayout;
        private bool _isMapLoaded = false;
        private bool _isGameOver = false;
        #endregion

        #region Collections
        /// <summary>
        /// 그리드 좌표 → 블록 매핑
        /// </summary>
        private Dictionary<Vector2Int, IBlock> _blocks = new Dictionary<Vector2Int, IBlock>();

        /// <summary>
        /// 플레이어ID → 대포 매핑
        /// </summary>
        private Dictionary<int, ICannon> _cannons = new Dictionary<int, ICannon>();

        /// <summary>
        /// 그리드 좌표 → 장애물 매핑
        /// </summary>
        private Dictionary<Vector2Int, IObstacle> _obstacles = new Dictionary<Vector2Int, IObstacle>();

        /// <summary>
        /// 그리드 좌표 → 기계 매핑
        /// </summary>
        private Dictionary<Vector2Int, IMachine> _machines = new Dictionary<Vector2Int, IMachine>();

        /// <summary>
        /// 플레이어ID → 색상 매핑
        /// </summary>
        private Dictionary<int, Color> _playerColors = new Dictionary<int, Color>();
        #endregion

        #region Properties
        public MapData CurrentMapData => _currentMapData;
        public IMapLayout CurrentLayout => _currentLayout;
        public bool IsMapLoaded => _isMapLoaded;
        public bool IsGameOver => _isGameOver;
        public int PlayerCount => _currentMapData?.playerCount ?? 0;
        public int GridSizeX => _currentLayout?.GridSizeX ?? 0;
        public int GridSizeY => _currentLayout?.GridSizeY ?? 0;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
        #endregion

        #region Map Loading
        /// <summary>
        /// 맵 데이터 로드 및 초기화
        /// </summary>
        public bool LoadMap(MapData mapData)
        {
            if (mapData == null)
            {
                Debug.LogError("[BattleFieldManager] MapData is null!");
                return false;
            }

            // 기존 맵 정리
            UnloadMap();

            _currentMapData = mapData;
            _isGameOver = false;

            // 레이아웃 생성
            _currentLayout = CreateLayout(mapData);
            if (_currentLayout == null)
            {
                Debug.LogError("[BattleFieldManager] Failed to create layout!");
                return false;
            }

            // 플레이어 색상 설정
            SetupPlayerColors();

            // 컴포넌트 생성
            SpawnBlocks();
            SpawnCannons();
            SpawnObstacles();
            SpawnSpecialComponents();

            _isMapLoaded = true;
            OnMapLoaded?.Invoke();

            Debug.Log($"[BattleFieldManager] Map '{mapData.displayName}' loaded successfully!");
            return true;
        }

        /// <summary>
        /// 맵 언로드 및 정리
        /// </summary>
        public void UnloadMap()
        {
            // 블록 정리
            foreach (var block in _blocks.Values)
            {
                block?.Cleanup();
            }
            _blocks.Clear();

            // 대포 정리
            foreach (var cannon in _cannons.Values)
            {
                if (cannon != null)
                {
                    cannon.OnCannonDestroyed -= HandleCannonDestroyed;
                    cannon.Cleanup();
                }
            }
            _cannons.Clear();

            // 장애물 정리
            foreach (var obstacle in _obstacles.Values)
            {
                obstacle?.Cleanup();
            }
            _obstacles.Clear();

            // 기계 정리
            foreach (var machine in _machines.Values)
            {
                machine?.Cleanup();
            }
            _machines.Clear();

            _playerColors.Clear();
            _currentMapData = null;
            _currentLayout = null;
            _isMapLoaded = false;

            Debug.Log("[BattleFieldManager] Map unloaded.");
        }
        #endregion

        #region Layout Factory
        /// <summary>
        /// MapData에서 레이아웃 생성
        /// </summary>
        private IMapLayout CreateLayout(MapData mapData)
        {
            switch (mapData.layoutType)
            {
                case MapLayoutType.Diagonal:
                    return new DiagonalLayout(mapData);
                case MapLayoutType.Quadrant:
                    return new QuadrantLayout(mapData);
                case MapLayoutType.Horizontal:
                    return new HorizontalLayout(mapData);
                case MapLayoutType.Vertical:
                    return new VerticalLayout(mapData);
                case MapLayoutType.Hexagon:
                    return new HexagonLayout(mapData);
                case MapLayoutType.Custom:
                    return CreateCustomLayout(mapData);
                default:
                    Debug.LogWarning($"[BattleFieldManager] Unknown layout type: {mapData.layoutType}, using Diagonal");
                    return new DiagonalLayout(mapData);
            }
        }

        private IMapLayout CreateCustomLayout(MapData mapData)
        {
            if (mapData.customLayoutJson == null)
            {
                Debug.LogWarning("[BattleFieldManager] Custom layout requires JSON, falling back to Diagonal");
                return new DiagonalLayout(mapData);
            }

            // TODO: JSON 파싱 구현
            return new DiagonalLayout(mapData);
        }
        #endregion

        #region Spawning
        private void SetupPlayerColors()
        {
            _playerColors.Clear();
            for (int i = 0; i < _currentMapData.playerCount; i++)
            {
                _playerColors[i] = _currentMapData.playerColors[i];
            }
        }

        private void SpawnBlocks()
        {
            foreach (var placement in _currentLayout.GetBlockPlacements())
            {
                var block = SpawnBlock(placement);
                if (block != null)
                {
                    _blocks[placement.gridPosition] = block;
                }
            }
            Debug.Log($"[BattleFieldManager] Spawned {_blocks.Count} blocks");
        }

        private void SpawnCannons()
        {
            foreach (var placement in _currentLayout.GetCannonPlacements())
            {
                var cannon = SpawnCannon(placement);
                if (cannon != null)
                {
                    _cannons[placement.playerId] = cannon;
                    cannon.OnCannonDestroyed += HandleCannonDestroyed;
                }
            }
            Debug.Log($"[BattleFieldManager] Spawned {_cannons.Count} cannons");
        }

        private void SpawnObstacles()
        {
            foreach (var placement in _currentLayout.GetObstaclePlacements())
            {
                var obstacle = SpawnObstacle(placement);
                if (obstacle != null)
                {
                    _obstacles[placement.gridPosition] = obstacle;
                }
            }
            Debug.Log($"[BattleFieldManager] Spawned {_obstacles.Count} obstacles");
        }

        private void SpawnSpecialComponents()
        {
            foreach (var placement in _currentLayout.GetSpecialPlacements())
            {
                SpawnSpecialComponent(placement);
            }
        }
        #endregion

        #region Component Factory (Override 가능)
        protected virtual IBlock SpawnBlock(BlockPlacement placement)
        {
            if (_currentMapData.blockPrefab == null) return null;

            var worldPos = GridToWorld(placement.gridPosition);
            var go = Instantiate(_currentMapData.blockPrefab, worldPos, Quaternion.identity);
            go.name = $"Block_{placement.gridPosition.x}_{placement.gridPosition.y}";

            var block = go.GetComponent<IBlock>();
            if (block != null)
            {
                var color = GetPlayerColor(placement.initialOwnerId);
                block.Initialize(placement.gridPosition, placement.initialOwnerId, color);
            }

            return block;
        }

        protected virtual ICannon SpawnCannon(CannonPlacement placement)
        {
            if (_currentMapData.cannonPrefab == null) return null;

            var worldPos = GridToWorld(placement.gridPosition);
            var rotation = Quaternion.Euler(0, 0, placement.rotationAngle);
            var go = Instantiate(_currentMapData.cannonPrefab, worldPos, rotation);
            go.name = $"Cannon_Player{placement.playerId}";

            var cannon = go.GetComponent<ICannon>();
            if (cannon != null)
            {
                var color = GetPlayerColor(placement.playerId);
                cannon.Initialize(placement.gridPosition, placement.playerId, color);
            }

            return cannon;
        }

        protected virtual IObstacle SpawnObstacle(ObstaclePlacement placement)
        {
            var prefab = _currentMapData.wallPrefab; // 기본 장애물 프리팹

            if (prefab == null) return null;

            var worldPos = GridToWorld(placement.gridPosition);
            var go = Instantiate(prefab, worldPos, Quaternion.identity);
            go.name = $"Obstacle_{placement.obstacleType}_{placement.gridPosition.x}_{placement.gridPosition.y}";

            var obstacle = go.GetComponent<IObstacle>();
            if (obstacle != null)
            {
                obstacle.Initialize(placement.gridPosition, -1, Color.gray);
            }

            return obstacle;
        }

        protected virtual void SpawnSpecialComponent(ComponentPlacement placement)
        {
            // 특수 컴포넌트 생성 (기계, 함정, 포탈 등)
            // 서브클래스에서 오버라이드
        }
        #endregion

        #region Block Management
        /// <summary>
        /// 블록 소유권 변경
        /// </summary>
        public bool SetBlockOwner(Vector2Int gridPos, int newOwnerId)
        {
            if (!_blocks.TryGetValue(gridPos, out var block))
                return false;

            if (!block.CanChangeOwner)
                return false;

            var color = GetPlayerColor(newOwnerId);
            if (block.SetOwner(newOwnerId, color))
            {
                OnBlockOwnerChanged?.Invoke(block, newOwnerId);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 특정 좌표의 블록 가져오기
        /// </summary>
        public IBlock GetBlock(Vector2Int gridPos)
        {
            _blocks.TryGetValue(gridPos, out var block);
            return block;
        }

        /// <summary>
        /// 특정 플레이어가 소유한 블록 수
        /// </summary>
        public int GetBlockCount(int playerId)
        {
            int count = 0;
            foreach (var block in _blocks.Values)
            {
                if (block.OwnerPlayerId == playerId)
                    count++;
            }
            return count;
        }
        #endregion

        #region Cannon Management
        /// <summary>
        /// 플레이어의 대포 가져오기
        /// </summary>
        public ICannon GetCannon(int playerId)
        {
            _cannons.TryGetValue(playerId, out var cannon);
            return cannon;
        }

        /// <summary>
        /// 대포 파괴 처리
        /// </summary>
        private void HandleCannonDestroyed(ICannon cannon)
        {
            int destroyedPlayerId = cannon.OwnerPlayerId;
            Debug.Log($"[BattleFieldManager] Cannon destroyed! Player {destroyedPlayerId} eliminated.");

            OnCannonDestroyed?.Invoke(destroyedPlayerId, cannon);

            // 남은 대포 확인
            CheckGameOver(destroyedPlayerId);
        }

        /// <summary>
        /// 게임 종료 체크
        /// </summary>
        private void CheckGameOver(int eliminatedPlayerId)
        {
            if (_isGameOver) return;

            // 살아있는 플레이어 찾기
            List<int> alivePlayers = new List<int>();
            foreach (var kvp in _cannons)
            {
                if (kvp.Value != null && kvp.Value.Health > 0)
                {
                    alivePlayers.Add(kvp.Key);
                }
            }

            // 한 명만 남으면 게임 종료
            if (alivePlayers.Count <= 1)
            {
                _isGameOver = true;
                int winnerId = alivePlayers.Count == 1 ? alivePlayers[0] : -1;
                Debug.Log($"[BattleFieldManager] Game Over! Winner: Player {winnerId}");
                OnGameOver?.Invoke(winnerId);
            }
        }
        #endregion

        #region Coordinate Conversion
        /// <summary>
        /// 그리드 좌표 → 월드 좌표 변환
        /// </summary>
        public Vector3 GridToWorld(Vector2Int gridPos)
        {
            float cellSize = _currentMapData?.cubeSize ?? 1f;
            float spacing = _currentMapData?.spacing ?? 0.05f;
            float totalSize = cellSize + spacing;

            // 중앙 기준 오프셋
            float offsetX = (GridSizeX - 1) * totalSize * 0.5f;
            float offsetY = (GridSizeY - 1) * totalSize * 0.5f;

            return new Vector3(
                gridPos.x * totalSize - offsetX,
                gridPos.y * totalSize - offsetY,
                0
            );
        }

        /// <summary>
        /// 월드 좌표 → 그리드 좌표 변환
        /// </summary>
        public Vector2Int WorldToGrid(Vector3 worldPos)
        {
            float cellSize = _currentMapData?.cubeSize ?? 1f;
            float spacing = _currentMapData?.spacing ?? 0.05f;
            float totalSize = cellSize + spacing;

            float offsetX = (GridSizeX - 1) * totalSize * 0.5f;
            float offsetY = (GridSizeY - 1) * totalSize * 0.5f;

            int x = Mathf.RoundToInt((worldPos.x + offsetX) / totalSize);
            int y = Mathf.RoundToInt((worldPos.y + offsetY) / totalSize);

            return new Vector2Int(x, y);
        }
        #endregion

        #region Utility
        /// <summary>
        /// 플레이어 색상 가져오기
        /// </summary>
        public Color GetPlayerColor(int playerId)
        {
            if (_playerColors.TryGetValue(playerId, out var color))
                return color;
            return Color.gray; // 중립
        }

        /// <summary>
        /// 그리드 범위 체크
        /// </summary>
        public bool IsValidGridPosition(Vector2Int gridPos)
        {
            return gridPos.x >= 0 && gridPos.x < GridSizeX &&
                   gridPos.y >= 0 && gridPos.y < GridSizeY;
        }

        /// <summary>
        /// 게임 리셋
        /// </summary>
        public void ResetGame()
        {
            if (_currentMapData != null)
            {
                LoadMap(_currentMapData);
            }
        }
        #endregion
    }
}

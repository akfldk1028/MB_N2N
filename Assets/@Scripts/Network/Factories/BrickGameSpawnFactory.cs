using UnityEngine;
using Unity.Assets.Scripts.Objects;

namespace MB.Network.Factories
{
    /// <summary>
    /// BrickGame 멀티플레이어 Ball/Plank 생성 전담 Factory
    /// - 컨텍스트 주입 방식으로 의존성 관리
    /// - 순수 GameObject 생성 담당 (NetworkObject 스폰은 Spawner가 처리)
    /// </summary>
    public class BrickGameSpawnFactory
    {
        #region Context (주입된 의존성)
        private readonly SpawnContext _context;
        #endregion

        #region Context Class
        /// <summary>
        /// Factory에 필요한 컨텍스트 (Spawner에서 주입)
        /// </summary>
        public class SpawnContext
        {
            public GameObject BallPrefab { get; set; }
            public GameObject PlankPrefab { get; set; }
            public Camera MainCamera { get; set; }
            public Transform LeftBoundary { get; set; }
            public Transform RightBoundary { get; set; }
            public float PlankYPosition { get; set; } = -4f;
            public float PlankSpacing { get; set; } = 15f;
        }
        #endregion

        #region Result Classes
        /// <summary>
        /// Plank 생성 결과 (Plank + 경계 오브젝트)
        /// </summary>
        public class PlankSpawnResult
        {
            public GameObject PlankObject { get; set; }
            public GameObject LeftBoundary { get; set; }
            public GameObject RightBoundary { get; set; }
            public PhysicsPlank PlankComponent { get; set; }
        }

        /// <summary>
        /// Ball 생성 결과
        /// </summary>
        public class BallSpawnResult
        {
            public GameObject BallObject { get; set; }
            public PhysicsBall BallComponent { get; set; }
        }
        #endregion

        #region Constructor
        public BrickGameSpawnFactory(SpawnContext context)
        {
            _context = context;
        }

        /// <summary>
        /// 컨텍스트 업데이트 (프리팹 로드 후 호출)
        /// </summary>
        public void UpdateContext(SpawnContext context)
        {
            _context.BallPrefab = context.BallPrefab ?? _context.BallPrefab;
            _context.PlankPrefab = context.PlankPrefab ?? _context.PlankPrefab;
            _context.MainCamera = context.MainCamera ?? _context.MainCamera;
            _context.LeftBoundary = context.LeftBoundary ?? _context.LeftBoundary;
            _context.RightBoundary = context.RightBoundary ?? _context.RightBoundary;
        }
        #endregion

        #region Public API
        /// <summary>
        /// 플레이어별 Plank 생성 (NetworkObject 스폰 전 단계)
        /// </summary>
        public PlankSpawnResult CreatePlank(ulong clientId)
        {
            if (_context.PlankPrefab == null)
            {
                GameLogger.Error("BrickGameSpawnFactory", "Plank 프리팹이 없습니다!");
                return null;
            }

            // 1. xOffset 계산
            float xOffset = CalculatePlayerXOffset(clientId);

            // 2. 스폰 위치 계산
            float centerX = (_context.LeftBoundary.position.x + _context.RightBoundary.position.x) / 2f;
            Vector3 spawnPosition = new Vector3(centerX + xOffset, _context.PlankYPosition, 0);

            // 3. 플레이어별 경계 생성
            GameObject leftBound = new GameObject($"LeftEnd_Player{clientId}");
            GameObject rightBound = new GameObject($"RightEnd_Player{clientId}");

            leftBound.transform.position = new Vector3(
                _context.LeftBoundary.position.x + xOffset,
                _context.LeftBoundary.position.y,
                _context.LeftBoundary.position.z
            );
            rightBound.transform.position = new Vector3(
                _context.RightBoundary.position.x + xOffset,
                _context.RightBoundary.position.y,
                _context.RightBoundary.position.z
            );

            // 4. Plank 인스턴스 생성 (비활성 상태로)
            GameObject plankObject = Object.Instantiate(_context.PlankPrefab);
            plankObject.name = $"Plank_Player{clientId}";
            plankObject.transform.position = spawnPosition;
            plankObject.SetActive(false); // Start() 방지

            // 5. PhysicsPlank 컴포넌트 설정
            PhysicsPlank plank = plankObject.GetComponent<PhysicsPlank>();
            if (plank != null)
            {
                plank.leftEnd = leftBound.transform;
                plank.rightEnd = rightBound.transform;
                plank.mainCamera = _context.MainCamera;

                GameLogger.Success("BrickGameSpawnFactory",
                    $"[Player {clientId}] Plank 경계 설정: L={leftBound.transform.position.x:F1}, R={rightBound.transform.position.x:F1}");
            }

            GameLogger.Info("BrickGameSpawnFactory",
                $"[Player {clientId}] Plank 생성 완료: pos={spawnPosition}, xOffset={xOffset}");

            return new PlankSpawnResult
            {
                PlankObject = plankObject,
                LeftBoundary = leftBound,
                RightBoundary = rightBound,
                PlankComponent = plank
            };
        }

        /// <summary>
        /// 플레이어별 Ball 생성 (NetworkObject 스폰 전 단계)
        /// </summary>
        public BallSpawnResult CreateBall(ulong clientId, PlankSpawnResult plankResult)
        {
            if (_context.BallPrefab == null)
            {
                GameLogger.Error("BrickGameSpawnFactory", "Ball 프리팹이 없습니다!");
                return null;
            }

            if (plankResult?.PlankObject == null)
            {
                GameLogger.Error("BrickGameSpawnFactory", "Plank가 없어서 Ball을 생성할 수 없습니다!");
                return null;
            }

            // 1. Ball 위치: Plank 위
            Vector3 spawnPosition = plankResult.PlankObject.transform.position + Vector3.up * 1f;

            // 2. Ball 인스턴스 생성 (비활성 상태로)
            GameObject ballObject = Object.Instantiate(_context.BallPrefab, spawnPosition, Quaternion.identity);
            ballObject.name = $"Ball_Player{clientId}";
            ballObject.SetActive(false); // Start() 방지

            // 3. PhysicsBall → PhysicsPlank 참조 연결
            PhysicsBall ball = ballObject.GetComponent<PhysicsBall>();
            if (ball != null && plankResult.PlankComponent != null)
            {
                ball.SetPlankReference(plankResult.PlankComponent);
                GameLogger.Success("BrickGameSpawnFactory",
                    $"[Player {clientId}] Ball → Plank 참조 연결 완료");
            }
            else
            {
                if (ball == null)
                    GameLogger.Error("BrickGameSpawnFactory", $"[Player {clientId}] PhysicsBall 컴포넌트 없음!");
                if (plankResult.PlankComponent == null)
                    GameLogger.Error("BrickGameSpawnFactory", $"[Player {clientId}] PhysicsPlank 컴포넌트 없음!");
            }

            GameLogger.Info("BrickGameSpawnFactory",
                $"[Player {clientId}] Ball 생성 완료: pos={spawnPosition}");

            return new BallSpawnResult
            {
                BallObject = ballObject,
                BallComponent = ball
            };
        }

        /// <summary>
        /// Plank 활성화 (NetworkObject 스폰 전 호출)
        /// </summary>
        public void ActivatePlank(PlankSpawnResult result)
        {
            if (result?.PlankObject != null)
            {
                result.PlankObject.SetActive(true);
            }
        }

        /// <summary>
        /// Ball 활성화 (NetworkObject 스폰 전 호출)
        /// </summary>
        public void ActivateBall(BallSpawnResult result)
        {
            if (result?.BallObject != null)
            {
                result.BallObject.SetActive(true);
            }
        }
        #endregion

        #region Helper Methods
        /// <summary>
        /// 플레이어별 X 오프셋 계산
        /// - clientId 0 (Host) → 왼쪽 (-spacing)
        /// - clientId 1 (Client) → 오른쪽 (+spacing)
        /// </summary>
        private float CalculatePlayerXOffset(ulong clientId)
        {
            return (clientId == 0) ? -_context.PlankSpacing : _context.PlankSpacing;
        }
        #endregion
    }
}

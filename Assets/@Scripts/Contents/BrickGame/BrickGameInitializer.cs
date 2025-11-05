using UnityEngine;
using Unity.Assets.Scripts.Objects;

/// <summary>
/// BrickGame 초기화 전담 클래스
/// - 씬 오브젝트 수집
/// - 의존성 검증
/// - Manager 초기화
/// </summary>
public class BrickGameInitializer
{
    #region Scene Objects Container
    private class SceneObjects
    {
        public ObjectPlacement ObjectPlacement;
        public PhysicsPlank Plank;
        public Camera MainCamera;
        public PhysicsBall[] Balls;
        public Brick[] Bricks;
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// BrickGame 초기화 메인 진입점
    /// </summary>
    public bool Initialize()
    {

        var sceneObjects = CollectSceneObjects();

        if (!ValidateRequirements(sceneObjects))
            return false;

        InjectDependencies(sceneObjects);
        SetupGameObjects(sceneObjects);

        return true;
    }
    #endregion

    #region Collection
    private SceneObjects CollectSceneObjects()
    {
        GameLogger.Progress("BrickGameInitializer", "씬 오브젝트 수집 시작...");

        var objects = new SceneObjects
        {
            ObjectPlacement = Object.FindFirstObjectByType<ObjectPlacement>(),
            Plank = Object.FindFirstObjectByType<PhysicsPlank>(),
            MainCamera = Camera.main,
            Balls = Object.FindObjectsByType<PhysicsBall>(FindObjectsSortMode.None),
            Bricks = Object.FindObjectsByType<Brick>(FindObjectsSortMode.None)
        };

        // 수집 결과 로그
        GameLogger.Info("BrickGameInitializer", $"ObjectPlacement: {(objects.ObjectPlacement != null ? "✅" : "❌")}");
        GameLogger.Info("BrickGameInitializer", $"PhysicsPlank: {(objects.Plank != null ? "✅" : "❌")}");
        GameLogger.Info("BrickGameInitializer", $"MainCamera: {(objects.MainCamera != null ? "✅" : "❌")}");
        GameLogger.Info("BrickGameInitializer", $"Balls: {objects.Balls?.Length ?? 0}개");
        GameLogger.Info("BrickGameInitializer", $"Bricks: {objects.Bricks?.Length ?? 0}개");

        return objects;
    }
    #endregion

    #region Validation
    private bool ValidateRequirements(SceneObjects objects)
    {
        GameLogger.Progress("BrickGameInitializer", "필수 오브젝트 검증 중...");

        bool isValid = true;

        // 선택: ObjectPlacement (멀티플레이어에서는 불필요)
        if (objects.ObjectPlacement == null)
        {
            GameLogger.Warning("BrickGameInitializer", "ObjectPlacement 없음 (Client 모드는 정상)");
        }
        else
        {
            GameLogger.Success("BrickGameInitializer", "ObjectPlacement 발견!");
        }

        // 필수: PhysicsPlank
        if (objects.Plank == null)
        {
            GameLogger.Error("BrickGameInitializer", "❌ PhysicsPlank를 찾을 수 없습니다! (필수)");
            isValid = false;
        }
        else
        {
            GameLogger.Success("BrickGameInitializer", "PhysicsPlank 발견!");
        }

        // 필수: Camera
        if (objects.MainCamera == null)
        {
            GameLogger.Error("BrickGameInitializer", "❌ MainCamera를 찾을 수 없습니다! (필수)");
            isValid = false;
        }
        else
        {
            GameLogger.Success("BrickGameInitializer", "MainCamera 발견!");
        }

        // 선택: PhysicsBall (동적 생성 가능)
        if (objects.Balls == null || objects.Balls.Length == 0)
        {
            GameLogger.Info("BrickGameInitializer", "PhysicsBall 없음 (동적 생성 예정)");
        }

        if (isValid)
        {
            GameLogger.Success("BrickGameInitializer", "✅ 모든 필수 오브젝트 검증 통과!");
        }
        else
        {
            GameLogger.Error("BrickGameInitializer", "❌ 필수 오브젝트 검증 실패!");
        }

        return isValid;
    }
    #endregion

    #region Dependency Injection
    private void InjectDependencies(SceneObjects objects)
    {
        // GameManager 초기화 호출
        // Client도 동일하게 초기화하되, BrickGameManager.OnUpdate()에서 IsServer 체크
        Managers.Game.InitializeBrickGame(
            objects.ObjectPlacement,  // Client도 받지만 사용 안 함 (OnUpdate에서 체크)
            objects.Plank,
            objects.MainCamera,
            null  // 기본 설정 사용
        );

        GameLogger.Success("BrickGameInitializer", "BrickGame 의존성 주입 완료");
    }
    #endregion

    #region Setup
    private void SetupGameObjects(SceneObjects objects)
    {

        SetupBalls(objects);
        SetupBricks(objects);

    }

    private void SetupBalls(SceneObjects objects)
    {
        if (objects.Balls == null || objects.Balls.Length == 0)
        {
            return;
        }

        var ballManager = Managers.Game?.BrickGame?.Ball;
        if (ballManager == null)
        {
            return;
        }

        foreach (var ball in objects.Balls)
        {
            if (ball == null) continue;

            // 리플렉션으로 Plank 할당 (Inspector 설정 불필요)
            var plankField = typeof(PhysicsBall).GetField("plank",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (plankField != null && objects.Plank != null)
            {
                plankField.SetValue(ball, objects.Plank);
            }
            else
            {
            }
        }

    }

    private void SetupBricks(SceneObjects objects)
    {
        if (objects.Bricks == null || objects.Bricks.Length == 0)
        {
            return;
        }

        var brickManager = Managers.Game?.BrickGame?.Brick;
        if (brickManager == null)
        {
            return;
        }

        // 벽돌은 Start()에서 자동 등록
    }
    #endregion
}

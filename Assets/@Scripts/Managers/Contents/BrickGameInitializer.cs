using UnityEngine;

/// <summary>
/// 벽돌깨기 게임 초기화 클래스
/// 게임 시작 시 필요한 모든 컴포넌트를 찾고 초기화합니다.
/// </summary>
public class BrickGameInitializer
{
    public bool Initialize()
    {
        GameLogger.Progress("BrickGameInitializer", "벽돌깨기 게임 초기화 시작");

        // BrickGameManager 찾기
        var brickGameManager = Object.FindObjectOfType<BrickGameManager>();
        if (brickGameManager == null)
        {
            GameLogger.Warning("BrickGameInitializer", "BrickGameManager를 찾을 수 없습니다. 게임이 제대로 동작하지 않을 수 있습니다.");
            return false;
        }

        GameLogger.Success("BrickGameInitializer", "BrickGameManager 초기화 완료");
        return true;
    }
}

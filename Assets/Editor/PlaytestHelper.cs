using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Unity.Netcode;
using System.IO;

/// <summary>
/// CLI(MCP)에서 런타임 UI 버튼을 트리거하기 위한 에디터 헬퍼.
/// Play 모드에서만 동작.
/// </summary>
public static class PlaytestHelper
{
    // ======== 버튼 클릭 ========

    [MenuItem("Tools/Playtest/Click Start Button")]
    public static void ClickStartButton()
    {
        if (!RequirePlayMode()) return;
        ClickButtonByName("StartButton");
    }

    [MenuItem("Tools/Playtest/Click Restart Button")]
    public static void ClickRestartButton()
    {
        if (!RequirePlayMode()) return;
        ClickButtonByName("RestartButton");
    }

    [MenuItem("Tools/Playtest/Click Lobby Button")]
    public static void ClickLobbyButton()
    {
        if (!RequirePlayMode()) return;
        ClickButtonByName("LobbyButton");
    }

    // ======== 게임 상태 조회 ========

    [MenuItem("Tools/Playtest/Get Game State")]
    public static void GetGameState()
    {
        if (!RequirePlayMode()) return;

        string phase = "Unknown";
        string score = "N/A";
        string players = "N/A";
        string role = "N/A";
        string scene = SceneManager.GetActiveScene().name;

        // BrickGameManager 상태
        var gameManager = Managers.Game;
        if (gameManager != null)
        {
            var brickGame = gameManager.BrickGame;
            if (brickGame != null)
            {
                phase = brickGame.Phase.ToString();
                score = brickGame.Score.ToString();
            }
        }

        // NetworkManager 상태
        var netManager = NetworkManager.Singleton;
        if (netManager != null && netManager.IsListening)
        {
            int connected = netManager.ConnectedClients?.Count ?? 0;
            players = $"{connected}/2";
            role = netManager.IsHost ? "Host" : netManager.IsClient ? "Client" : "None";
        }
        else
        {
            players = "0/2";
            role = "Disconnected";
        }

        Debug.Log($"[PlaytestStatus] Phase={phase} Score={score} Players={players} Role={role} Scene={scene}");
    }

    // ======== 캡쳐 ========

    [MenuItem("Tools/Playtest/Trigger Capture")]
    public static void TriggerCapture()
    {
        if (!RequirePlayMode()) return;
        MultiplayerCapture.TriggerCapture();
    }

    [MenuItem("Tools/Playtest/Collect Clone Screenshots")]
    public static void CollectCloneScreenshots()
    {
        string vpDir = Path.Combine(Application.dataPath, "..", "Library", "VP");
        string destDir = Path.Combine(Application.dataPath, "..", "Screenshots");
        Directory.CreateDirectory(destDir);

        if (!Directory.Exists(vpDir))
        {
            Debug.LogWarning("[PlaytestHelper] Library/VP 폴더 없음 - MPPM이 활성화되지 않았을 수 있음");
            return;
        }

        int copied = 0;
        // Library/VP/mppm{hash}/Screenshots/ 패턴 검색
        foreach (var mppmDir in Directory.GetDirectories(vpDir))
        {
            string cloneScreenshotDir = Path.Combine(mppmDir, "Screenshots");
            if (!Directory.Exists(cloneScreenshotDir)) continue;

            foreach (var file in Directory.GetFiles(cloneScreenshotDir, "*.png"))
            {
                string destFile = Path.Combine(destDir, $"clone_{Path.GetFileName(file)}");
                File.Copy(file, destFile, true);
                copied++;
                Debug.Log($"[PlaytestHelper] Copied clone screenshot: {destFile}");
            }
        }

        if (copied == 0)
            Debug.LogWarning("[PlaytestHelper] 클론 스크린샷 없음");
        else
            Debug.Log($"[PlaytestHelper] 클론 스크린샷 {copied}개 수집 완료");
    }

    // ======== Play 모드 제어 ========

    [MenuItem("Tools/Playtest/Stop Play Mode")]
    public static void StopPlayMode()
    {
        if (EditorApplication.isPlaying)
        {
            EditorApplication.isPlaying = false;
            Debug.Log("[PlaytestHelper] Play 모드 종료");
        }
        else
        {
            Debug.LogWarning("[PlaytestHelper] 이미 Play 모드가 아님");
        }
    }

    // ======== 내부 유틸리티 ========

    private static bool RequirePlayMode()
    {
        if (!EditorApplication.isPlaying)
        {
            Debug.LogWarning("[PlaytestHelper] Play 모드에서만 사용 가능");
            return false;
        }
        return true;
    }

    private static void ClickButtonByName(string buttonName)
    {
        var buttons = Object.FindObjectsByType<Button>(FindObjectsSortMode.None);
        foreach (var btn in buttons)
        {
            if (btn.gameObject.name == buttonName)
            {
                if (!btn.gameObject.activeInHierarchy)
                {
                    Debug.LogWarning($"[PlaytestHelper] Button '{buttonName}' found but inactive");
                    return;
                }
                Debug.Log($"[PlaytestHelper] Clicking '{buttonName}'");
                var pointer = new PointerEventData(EventSystem.current);
                ExecuteEvents.Execute(btn.gameObject, pointer, ExecuteEvents.pointerClickHandler);
                return;
            }
        }
        Debug.LogWarning($"[PlaytestHelper] Button '{buttonName}' not found");
    }
}

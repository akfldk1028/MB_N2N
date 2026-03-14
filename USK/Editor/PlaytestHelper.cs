// [USK] CLI(MCP) 플레이테스트 헬퍼
// ★ 새 프로젝트 적용 시: GetGameState() 내 게임 매니저 접근 코드 수정

using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Unity.Netcode;
using System.IO;

public static class USK_PlaytestHelper
{
    // ======== 버튼 클릭 ========

    [MenuItem("Tools/USK/Click Start Button")]
    public static void ClickStartButton() => ClickButton("StartButton");

    [MenuItem("Tools/USK/Click Restart Button")]
    public static void ClickRestartButton() => ClickButton("RestartButton");

    [MenuItem("Tools/USK/Click Lobby Button")]
    public static void ClickLobbyButton() => ClickButton("LobbyButton");

    // ======== 게임 상태 조회 ========

    [MenuItem("Tools/USK/Get Game State")]
    public static void GetGameState()
    {
        if (!RequirePlayMode()) return;

        string phase = "Unknown";
        string score = "N/A";
        string players = "N/A";
        string role = "N/A";
        string scene = SceneManager.GetActiveScene().name;

        // ★★★ 프로젝트별 수정 필요 ★★★
        // 여기서 게임 매니저에 접근하여 Phase, Score 등을 가져옴
        // 예: var gameManager = YourGameManager.Instance;
        //     phase = gameManager.CurrentPhase.ToString();
        //     score = gameManager.Score.ToString();

        // NetworkManager 상태 (범용)
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

        Debug.Log($"[USK_Status] Phase={phase} Score={score} Players={players} Role={role} Scene={scene}");
    }

    // ======== 캡쳐 ========

    [MenuItem("Tools/USK/Trigger Capture")]
    public static void TriggerCapture()
    {
        if (!RequirePlayMode()) return;
        USK_MultiplayerCapture.TriggerCapture();
    }

    [MenuItem("Tools/USK/Collect Clone Screenshots")]
    public static void CollectCloneScreenshots()
    {
        string vpDir = Path.Combine(Application.dataPath, "..", "Library", "VP");
        string destDir = Path.Combine(Application.dataPath, "..", "Screenshots");
        Directory.CreateDirectory(destDir);

        if (!Directory.Exists(vpDir))
        {
            Debug.LogWarning("[USK_Helper] Library/VP 폴더 없음");
            return;
        }

        int copied = 0;
        foreach (var mppmDir in Directory.GetDirectories(vpDir))
        {
            string cloneScreenshotDir = Path.Combine(mppmDir, "Screenshots");
            if (!Directory.Exists(cloneScreenshotDir)) continue;

            foreach (var file in Directory.GetFiles(cloneScreenshotDir, "*.png"))
            {
                string destFile = Path.Combine(destDir, $"clone_{Path.GetFileName(file)}");
                File.Copy(file, destFile, true);
                copied++;
            }
        }

        Debug.Log(copied > 0
            ? $"[USK_Helper] 클론 스크린샷 {copied}개 수집 완료"
            : "[USK_Helper] 클론 스크린샷 없음");
    }

    // ======== Play 모드 제어 ========

    [MenuItem("Tools/USK/Stop Play Mode")]
    public static void StopPlayMode()
    {
        if (EditorApplication.isPlaying)
        {
            EditorApplication.isPlaying = false;
            Debug.Log("[USK_Helper] Play 모드 종료");
        }
    }

    // ======== 내부 유틸 ========

    private static bool RequirePlayMode()
    {
        if (!EditorApplication.isPlaying)
        {
            Debug.LogWarning("[USK_Helper] Play 모드에서만 사용 가능");
            return false;
        }
        return true;
    }

    private static void ClickButton(string name)
    {
        if (!RequirePlayMode()) return;
        var buttons = Object.FindObjectsByType<Button>(FindObjectsSortMode.None);
        foreach (var btn in buttons)
        {
            if (btn.gameObject.name == name && btn.gameObject.activeInHierarchy)
            {
                Debug.Log($"[USK_Helper] Clicking '{name}'");
                ExecuteEvents.Execute(btn.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerClickHandler);
                return;
            }
        }
        Debug.LogWarning($"[USK_Helper] Button '{name}' not found");
    }
}

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
    // ======== 공 발사 ========

    [MenuItem("Tools/Playtest/Launch Ball")]
    public static void LaunchBall()
    {
        if (!RequirePlayMode()) return;

        // PhysicsBall은 런타임 어셈블리 → Reflection으로 접근
        var ballType = System.Type.GetType("PhysicsBall, Assembly-CSharp");
        if (ballType == null)
        {
            Debug.LogError("[PlaytestHelper] PhysicsBall 타입을 찾을 수 없음");
            return;
        }

        var balls = Object.FindObjectsByType(ballType, FindObjectsSortMode.None);
        var currentStateProp = ballType.GetProperty("CurrentState");
        var changeStateMethod = ballType.GetMethod("ChangeState",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var requestLaunchMethod = ballType.GetMethod("RequestLaunchServerRpc",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

        // EBallState.Ready = 0, EBallState.Launching = 1
        var ballStateType = System.Type.GetType("EBallState, Assembly-CSharp");
        object readyState = System.Enum.Parse(ballStateType, "Ready");
        object launchingState = System.Enum.Parse(ballStateType, "Launching");

        int launched = 0;
        foreach (var ball in balls)
        {
            var nb = ball as Unity.Netcode.NetworkBehaviour;
            object state = currentStateProp?.GetValue(ball);
            if (state == null || !state.Equals(readyState)) continue;

            if (nb != null && (nb.IsServer || !nb.IsSpawned))
            {
                changeStateMethod?.Invoke(ball, new object[] { launchingState });
                launched++;
                Debug.Log($"[PlaytestHelper] Ball launched (Server): {((Component)ball).gameObject.name}");
            }
            else if (nb != null && nb.IsOwner && requestLaunchMethod != null)
            {
                requestLaunchMethod.Invoke(ball, null);
                launched++;
                Debug.Log($"[PlaytestHelper] Ball launch requested (ServerRpc): {((Component)ball).gameObject.name}");
            }
        }

        if (launched == 0)
            Debug.LogWarning($"[PlaytestHelper] Ready 상태인 공 없음 (총 {balls.Length}개 공 발견)");
        else
            Debug.Log($"[PlaytestHelper] {launched}개 공 발사됨");
    }

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

    // ======== 클론 로그 수집 ========

    [MenuItem("Tools/Playtest/Get Clone Logs")]
    public static void GetCloneLogs()
    {
        string vpDir = Path.Combine(Application.dataPath, "..", "Library", "VP");

        if (!Directory.Exists(vpDir))
        {
            Debug.LogWarning("[PlaytestHelper] Library/VP 폴더 없음 - MPPM이 활성화되지 않았을 수 있음");
            return;
        }

        bool foundAny = false;

        foreach (var mppmDir in Directory.GetDirectories(vpDir, "mppm*"))
        {
            string dirName = Path.GetFileName(mppmDir);

            // 후보 로그 경로 목록 (우선순위 순)
            string[] candidatePaths = new string[]
            {
                Path.Combine(mppmDir, "Logs", "Player.log"),
                Path.Combine(mppmDir, "Logs", "Player-prev.log"),
                Path.Combine(mppmDir, "Player.log"),
            };

            foreach (var logPath in candidatePaths)
            {
                if (!File.Exists(logPath)) continue;

                foundAny = true;
                try
                {
                    var lines = File.ReadAllLines(logPath);
                    int total = lines.Length;
                    int start = Mathf.Max(0, total - 50);
                    var lastLines = new System.Text.StringBuilder();
                    lastLines.AppendLine($"[PlaytestHelper] Clone log ({dirName}) - last {total - start}/{total} lines: {logPath}");
                    lastLines.AppendLine("---");
                    for (int i = start; i < total; i++)
                        lastLines.AppendLine(lines[i]);

                    Debug.Log(lastLines.ToString());
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[PlaytestHelper] 로그 읽기 실패 ({logPath}): {ex.Message}");
                }

                break; // 첫 번째 유효한 로그만 읽음
            }
        }

        if (!foundAny)
            Debug.LogWarning("[PlaytestHelper] MPPM 클론 로그 파일을 찾을 수 없음 (Library/VP/mppm*/Logs/Player.log)");
    }

    // ======== 플랭크 조작 ========

    private static bool _autoMoving = false;
    private static float _autoMoveDir = 1f;

    [MenuItem("Tools/Playtest/Move Plank Left")]
    public static void MovePlankLeft()
    {
        MovePlank(-1f);
    }

    [MenuItem("Tools/Playtest/Move Plank Right")]
    public static void MovePlankRight()
    {
        MovePlank(1f);
    }

    [MenuItem("Tools/Playtest/Move Plank Center")]
    public static void MovePlankCenter()
    {
        if (!RequirePlayMode()) return;
        var plank = FindLocalPlank();
        if (plank == null) return;

        if (plank.leftEnd != null && plank.rightEnd != null)
        {
            float centerX = (plank.leftEnd.position.x + plank.rightEnd.position.x) / 2f;
            var pos = plank.transform.position;
            plank.transform.position = new Vector3(centerX, pos.y, pos.z);
            Debug.Log($"[PlaytestHelper] Plank moved to center: x={centerX:F1}");
        }
    }

    [MenuItem("Tools/Playtest/Toggle Auto Move Plank")]
    public static void ToggleAutoMovePlank()
    {
        if (!RequirePlayMode()) return;

        _autoMoving = !_autoMoving;
        if (_autoMoving)
        {
            EditorApplication.update += AutoMovePlankUpdate;
            Debug.Log("[PlaytestHelper] Auto-move plank ON (좌우 반복)");
        }
        else
        {
            EditorApplication.update -= AutoMovePlankUpdate;
            Debug.Log("[PlaytestHelper] Auto-move plank OFF");
        }
    }

    private static void AutoMovePlankUpdate()
    {
        if (!EditorApplication.isPlaying)
        {
            _autoMoving = false;
            EditorApplication.update -= AutoMovePlankUpdate;
            return;
        }

        var plank = FindLocalPlank();
        if (plank == null) return;

        float speed = 8f * Time.deltaTime;
        var pos = plank.transform.position;
        float newX = pos.x + _autoMoveDir * speed;

        if (plank.leftEnd != null && plank.rightEnd != null)
        {
            if (newX >= plank.rightEnd.position.x) _autoMoveDir = -1f;
            if (newX <= plank.leftEnd.position.x) _autoMoveDir = 1f;
            newX = Mathf.Clamp(newX, plank.leftEnd.position.x, plank.rightEnd.position.x);
        }

        plank.transform.position = new Vector3(newX, pos.y, pos.z);
    }

    private static void MovePlank(float direction)
    {
        if (!RequirePlayMode()) return;
        var plank = FindLocalPlank();
        if (plank == null) return;

        float step = 2f;
        var pos = plank.transform.position;
        float newX = pos.x + direction * step;

        if (plank.leftEnd != null && plank.rightEnd != null)
        {
            newX = Mathf.Clamp(newX, plank.leftEnd.position.x, plank.rightEnd.position.x);
        }

        plank.transform.position = new Vector3(newX, pos.y, pos.z);
        Debug.Log($"[PlaytestHelper] Plank moved {(direction > 0 ? "right" : "left")}: x={newX:F1}");
    }

    private static PhysicsPlank FindLocalPlank()
    {
        var planks = Object.FindObjectsByType<PhysicsPlank>(FindObjectsSortMode.None);
        foreach (var p in planks)
        {
            // Host의 로컬 플랭크 = Player0
            if (p.gameObject.name.Contains("Player0") || p.IsOwner)
            {
                return p;
            }
        }
        if (planks.Length > 0)
        {
            Debug.Log($"[PlaytestHelper] Using first plank: {planks[0].gameObject.name}");
            return planks[0];
        }
        Debug.LogWarning("[PlaytestHelper] PhysicsPlank not found");
        return null;
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

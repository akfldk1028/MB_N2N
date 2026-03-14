using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using System.IO;

/// <summary>
/// 전체 플레이테스트 파이프라인 오케스트레이션.
/// Play 진입 → 연결 대기 → 게임 씬 대기 → 캡쳐 → 결과 출력.
/// EditorApplication.update 기반 상태머신으로 비동기 처리.
/// </summary>
public static class PlaytestPipeline
{
    private enum PipelineState
    {
        Idle,
        WaitingForPlay,
        WaitingForConnection,
        WaitingForGameScene,
        Capturing,
        Complete
    }

    private static PipelineState _state = PipelineState.Idle;
    private static float _stateStartTime;
    private static float _pipelineStartTime;

    private const float ConnectionTimeout = 30f;
    private const float GameSceneTimeout = 15f;
    private const float CaptureDelay = 3f;

    [MenuItem("Tools/Playtest/Run Full Pipeline")]
    public static void RunFullPipeline()
    {
        if (_state != PipelineState.Idle)
        {
            Debug.LogWarning("[PlaytestPipeline] 이미 실행 중입니다. 현재 상태: " + _state);
            return;
        }

        Debug.Log("[PlaytestPipeline] ===== 파이프라인 시작 =====");
        _pipelineStartTime = (float)EditorApplication.timeSinceStartup;

        if (EditorApplication.isPlaying)
        {
            Debug.Log("[PlaytestPipeline] 이미 Play 모드 - 연결 대기로 이동");
            TransitionTo(PipelineState.WaitingForConnection);
        }
        else
        {
            Debug.Log("[PlaytestPipeline] Play 모드 진입...");
            EditorApplication.isPlaying = true;
            TransitionTo(PipelineState.WaitingForPlay);
        }

        EditorApplication.update -= OnUpdate;
        EditorApplication.update += OnUpdate;
    }

    [MenuItem("Tools/Playtest/Cancel Pipeline")]
    public static void CancelPipeline()
    {
        if (_state == PipelineState.Idle)
        {
            Debug.Log("[PlaytestPipeline] 실행 중인 파이프라인 없음");
            return;
        }
        Debug.Log("[PlaytestPipeline] 파이프라인 취소됨");
        Cleanup();
    }

    [MenuItem("Tools/Playtest/Pipeline Status")]
    public static void PipelineStatus()
    {
        Debug.Log($"[PlaytestPipeline] State={_state}");
    }

    private static void OnUpdate()
    {
        float elapsed = (float)EditorApplication.timeSinceStartup - _stateStartTime;

        switch (_state)
        {
            case PipelineState.WaitingForPlay:
                if (EditorApplication.isPlaying)
                {
                    Debug.Log("[PlaytestPipeline] Play 모드 진입 완료");
                    TransitionTo(PipelineState.WaitingForConnection);
                }
                else if (elapsed > 10f)
                {
                    Fail("Play 모드 진입 타임아웃 (10초)");
                }
                break;

            case PipelineState.WaitingForConnection:
                if (!EditorApplication.isPlaying)
                {
                    Fail("Play 모드가 예기치 않게 종료됨");
                    break;
                }
                var netManager = NetworkManager.Singleton;
                if (netManager != null && netManager.IsListening)
                {
                    int connected = netManager.ConnectedClients?.Count ?? 0;
                    if (connected >= 2)
                    {
                        Debug.Log($"[PlaytestPipeline] 2명 연결 완료 (Players={connected})");
                        TransitionTo(PipelineState.WaitingForGameScene);
                    }
                    else if (elapsed > ConnectionTimeout)
                    {
                        Debug.LogWarning($"[PlaytestPipeline] 연결 타임아웃 - 현재 {connected}/2명. 캡쳐로 이동...");
                        TransitionTo(PipelineState.WaitingForGameScene);
                    }
                }
                else if (elapsed > ConnectionTimeout)
                {
                    Debug.LogWarning("[PlaytestPipeline] NetworkManager 없거나 미시작 - 캡쳐로 이동...");
                    TransitionTo(PipelineState.WaitingForGameScene);
                }
                break;

            case PipelineState.WaitingForGameScene:
                if (!EditorApplication.isPlaying)
                {
                    Fail("Play 모드가 예기치 않게 종료됨");
                    break;
                }
                string sceneName = SceneManager.GetActiveScene().name;
                bool isGameScene = sceneName.Contains("Game") || sceneName.Contains("BrickGame");
                if (isGameScene || elapsed > GameSceneTimeout)
                {
                    if (!isGameScene)
                        Debug.LogWarning($"[PlaytestPipeline] 게임 씬 타임아웃 - 현재 씬: {sceneName}");
                    else
                        Debug.Log($"[PlaytestPipeline] 게임 씬 로드 확인: {sceneName}");
                    TransitionTo(PipelineState.Capturing);
                }
                break;

            case PipelineState.Capturing:
                if (!EditorApplication.isPlaying)
                {
                    Fail("Play 모드가 예기치 않게 종료됨");
                    break;
                }
                if (elapsed > CaptureDelay)
                {
                    DoCaptureAndCollect();
                    TransitionTo(PipelineState.Complete);
                }
                break;

            case PipelineState.Complete:
                PrintSummary();
                Cleanup();
                break;

            case PipelineState.Idle:
                Cleanup();
                break;
        }
    }

    private static void TransitionTo(PipelineState newState)
    {
        _state = newState;
        _stateStartTime = (float)EditorApplication.timeSinceStartup;
        Debug.Log($"[PlaytestPipeline] → {newState}");
    }

    private static void DoCaptureAndCollect()
    {
        Debug.Log("[PlaytestPipeline] 메인 에디터 캡쳐...");
        MultiplayerCapture.TriggerCapture();

        // 클론 스크린샷 수집
        Debug.Log("[PlaytestPipeline] 클론 스크린샷 수집...");
        PlaytestHelper.CollectCloneScreenshots();
    }

    private static void PrintSummary()
    {
        float totalTime = (float)EditorApplication.timeSinceStartup - _pipelineStartTime;
        Debug.Log("[PlaytestPipeline] ===== 파이프라인 완료 =====");
        Debug.Log($"[PlaytestPipeline] 총 소요 시간: {totalTime:F1}초");

        // 게임 상태 출력
        PlaytestHelper.GetGameState();

        // 스크린샷 목록
        string screenshotDir = Path.Combine(Application.dataPath, "..", "Screenshots");
        if (Directory.Exists(screenshotDir))
        {
            var files = Directory.GetFiles(screenshotDir, "*.png");
            Debug.Log($"[PlaytestPipeline] Screenshots/ 폴더: {files.Length}개 파일");
            // 최근 5개만 표시
            var sorted = files;
            System.Array.Sort(sorted, (a, b) => File.GetLastWriteTime(b).CompareTo(File.GetLastWriteTime(a)));
            for (int i = 0; i < System.Math.Min(5, sorted.Length); i++)
            {
                Debug.Log($"[PlaytestPipeline]   {Path.GetFileName(sorted[i])}");
            }
        }

        Debug.Log("[PlaytestPipeline] → get_console_logs(logType: 'error') 로 에러 확인하세요");
        Debug.Log("[PlaytestPipeline] → Read로 Screenshots/ PNG 파일을 확인하세요");
    }

    private static void Fail(string reason)
    {
        Debug.LogError($"[PlaytestPipeline] 실패: {reason}");
        Cleanup();
    }

    private static void Cleanup()
    {
        _state = PipelineState.Idle;
        EditorApplication.update -= OnUpdate;
    }
}

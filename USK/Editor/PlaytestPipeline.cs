// [USK] 전체 플레이테스트 파이프라인 자동화
// ★ 범용 - 수정 불필요 (PlaytestHelper.GetGameState 수정으로 충분)

using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using System.IO;

public static class USK_PlaytestPipeline
{
    private enum State { Idle, WaitingForPlay, WaitingForConnection, WaitingForGameScene, Capturing, Complete, Failed }

    private static State _state = State.Idle;
    private static float _stateStartTime;
    private static float _pipelineStartTime;

    private const float ConnectionTimeout = 30f;
    private const float GameSceneTimeout = 15f;
    private const float CaptureDelay = 3f;

    [MenuItem("Tools/USK/Run Full Pipeline")]
    public static void RunFullPipeline()
    {
        if (_state != State.Idle)
        {
            Debug.LogWarning("[USK_Pipeline] 이미 실행 중: " + _state);
            return;
        }

        Debug.Log("[USK_Pipeline] ===== 시작 =====");
        _pipelineStartTime = (float)EditorApplication.timeSinceStartup;

        if (EditorApplication.isPlaying)
        {
            TransitionTo(State.WaitingForConnection);
        }
        else
        {
            EditorApplication.isPlaying = true;
            TransitionTo(State.WaitingForPlay);
        }

        EditorApplication.update -= OnUpdate;
        EditorApplication.update += OnUpdate;
    }

    [MenuItem("Tools/USK/Cancel Pipeline")]
    public static void Cancel()
    {
        if (_state != State.Idle)
        {
            Debug.Log("[USK_Pipeline] 취소됨");
            Cleanup();
        }
    }

    private static void OnUpdate()
    {
        float elapsed = (float)EditorApplication.timeSinceStartup - _stateStartTime;

        switch (_state)
        {
            case State.WaitingForPlay:
                if (EditorApplication.isPlaying)
                    TransitionTo(State.WaitingForConnection);
                else if (elapsed > 10f)
                    Fail("Play 모드 진입 타임아웃");
                break;

            case State.WaitingForConnection:
                if (!EditorApplication.isPlaying) { Fail("Play 종료됨"); break; }
                var net = NetworkManager.Singleton;
                if (net != null && net.IsListening && (net.ConnectedClients?.Count ?? 0) >= 2)
                    TransitionTo(State.WaitingForGameScene);
                else if (elapsed > ConnectionTimeout)
                    TransitionTo(State.WaitingForGameScene); // 타임아웃 시에도 진행
                break;

            case State.WaitingForGameScene:
                if (!EditorApplication.isPlaying) { Fail("Play 종료됨"); break; }
                if (elapsed > GameSceneTimeout || SceneManager.GetActiveScene().name.Contains("Game"))
                    TransitionTo(State.Capturing);
                break;

            case State.Capturing:
                if (!EditorApplication.isPlaying) { Fail("Play 종료됨"); break; }
                if (elapsed > CaptureDelay)
                {
                    USK_MultiplayerCapture.TriggerCapture();
                    USK_PlaytestHelper.CollectCloneScreenshots();
                    TransitionTo(State.Complete);
                }
                break;

            case State.Complete:
                float total = (float)EditorApplication.timeSinceStartup - _pipelineStartTime;
                Debug.Log($"[USK_Pipeline] ===== 완료 ({total:F1}초) =====");
                USK_PlaytestHelper.GetGameState();
                Cleanup();
                break;

            default:
                Cleanup();
                break;
        }
    }

    private static void TransitionTo(State s)
    {
        _state = s;
        _stateStartTime = (float)EditorApplication.timeSinceStartup;
        Debug.Log($"[USK_Pipeline] → {s}");
    }

    private static void Fail(string reason)
    {
        Debug.LogError($"[USK_Pipeline] 실패: {reason}");
        Cleanup();
    }

    private static void Cleanup()
    {
        _state = State.Idle;
        EditorApplication.update -= OnUpdate;
    }
}

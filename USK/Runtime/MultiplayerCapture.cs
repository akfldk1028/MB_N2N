using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
using System;

/// <summary>
/// [USK] 모든 플레이어 인스턴스(메인+클론)에서 스크린샷을 캡쳐.
/// F12: 즉시 캡쳐 / 자동 캡쳐도 지원.
/// 파일명: mp_player{N}_{timestamp}.png
/// 저장 경로: 프로젝트루트/Screenshots/
///
/// ★ 새 프로젝트 적용 시 수정 불필요 (범용)
/// </summary>
public class USK_MultiplayerCapture : MonoBehaviour
{
    private static USK_MultiplayerCapture _instance;

    [Header("Settings")]
    [SerializeField] private KeyCode captureKey = KeyCode.F12;
    [SerializeField] private bool autoCaptureOnStart = true;
    [SerializeField] private float autoCaptureDelay = 2f;

    private string _screenshotDir;
    private int _playerIndex;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (_instance != null) return;
        var go = new GameObject("[USK_MultiplayerCapture]");
        _instance = go.AddComponent<USK_MultiplayerCapture>();
        DontDestroyOnLoad(go);
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;

        _screenshotDir = Path.Combine(Application.dataPath, "..", "Screenshots");
        Directory.CreateDirectory(_screenshotDir);

        _playerIndex = GetPlayerIndex();
        Debug.Log($"[USK_Capture] Player {_playerIndex} ready. Press {captureKey} to capture.");
    }

    private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (autoCaptureOnStart)
        {
            CancelInvoke(nameof(Capture));
            Invoke(nameof(Capture), autoCaptureDelay);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(captureKey))
            Capture();
    }

    private void Start()
    {
        if (autoCaptureOnStart)
        {
            Invoke(nameof(Capture), autoCaptureDelay);
        }
    }

    public void Capture()
    {
        string filename = $"mp_player{_playerIndex}_{DateTime.Now:yyyyMMdd_HHmmss}.png";
        string path = Path.Combine(_screenshotDir, filename);
        ScreenCapture.CaptureScreenshot(path);
        Debug.Log($"[USK_Capture] Player {_playerIndex} captured: {filename}");
    }

    public static void TriggerCapture()
    {
        if (_instance != null)
            _instance.Capture();
        else
            Debug.LogWarning("[USK_Capture] Instance not found");
    }

    private static int GetPlayerIndex()
    {
#if UNITY_EDITOR
        try
        {
            if (Unity.Multiplayer.Playmode.CurrentPlayer.IsMainEditor)
                return 1;
            var tags = Unity.Multiplayer.Playmode.CurrentPlayer.ReadOnlyTags();
            if (tags != null && tags.Length > 0)
                foreach (var tag in tags)
                    if (int.TryParse(tag, out int idx))
                        return idx + 1;
            return 2;
        }
        catch { }
#endif
        var args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i].Contains("clone") || args[i].Contains("Clone"))
            {
                if (i + 1 < args.Length && int.TryParse(args[i + 1], out int idx))
                    return idx + 1;
                return 2;
            }
            if (args[i] == "-instanceId" && i + 1 < args.Length && int.TryParse(args[i + 1], out int id))
                return id;
        }
        if (Application.dataPath.Contains("clone")) return 2;
        return 1;
    }
}

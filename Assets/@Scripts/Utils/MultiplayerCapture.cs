using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
using System;

/// <summary>
/// 모든 플레이어 인스턴스(메인+클론)에서 스크린샷을 캡쳐.
/// F12: 즉시 캡쳐 / 자동 캡쳐도 지원.
/// 파일명: mp_player{N}_{timestamp}.png
/// 저장 경로: 프로젝트루트/Screenshots/
/// </summary>
public class MultiplayerCapture : MonoBehaviour
{
    private static MultiplayerCapture _instance;

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
        var go = new GameObject("[MultiplayerCapture]");
        _instance = go.AddComponent<MultiplayerCapture>();
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
        Debug.Log($"[MultiplayerCapture] Player {_playerIndex} ready. Press {captureKey} to capture.");
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[MultiplayerCapture] Scene loaded: {scene.name}");
        if (autoCaptureOnStart)
        {
            CancelInvoke(nameof(Capture));
            Invoke(nameof(Capture), autoCaptureDelay);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(captureKey))
        {
            Capture();
        }
    }

    private void Start()
    {
        // AutoCreate(AfterSceneLoad) 시점에 첫 씬은 이미 로드 완료 → OnSceneLoaded 미호출
        // 따라서 Start에서 첫 캡쳐 예약
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
        Debug.Log($"[MultiplayerCapture] Player {_playerIndex} captured: {filename}");
    }

    /// <summary>
    /// 에디터/외부에서 호출 가능한 즉시 캡쳐 트리거
    /// </summary>
    public static void TriggerCapture()
    {
        if (_instance != null)
        {
            _instance.Capture();
        }
        else
        {
            Debug.LogWarning("[MultiplayerCapture] Instance not found - cannot capture");
        }
    }

    /// <summary>
    /// 플레이어 인덱스 판별. MPPM API 사용 우선, 폴백으로 커맨드라인/경로 파싱.
    /// </summary>
    private static int GetPlayerIndex()
    {
#if UNITY_EDITOR
        try
        {
            if (Unity.Multiplayer.Playmode.CurrentPlayer.IsMainEditor)
                return 1;
            // 클론인 경우
            var tags = Unity.Multiplayer.Playmode.CurrentPlayer.ReadOnlyTags();
            if (tags != null && tags.Length > 0)
            {
                // 태그에서 숫자 추출 시도
                foreach (var tag in tags)
                {
                    if (int.TryParse(tag, out int idx))
                        return idx + 1;
                }
            }
            return 2; // 기본 클론 = player 2
        }
        catch
        {
            // MPPM API 사용 불가 시 폴백
        }
#endif

        // 폴백: 커맨드라인 args
        var args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i].Contains("clone") || args[i].Contains("Clone"))
                return ParseCloneIndex(args, i);

            if (args[i] == "-intanceId" || args[i] == "-instanceId")
            {
                if (i + 1 < args.Length && int.TryParse(args[i + 1], out int id))
                    return id;
            }
        }

        // 폴백: Application.dataPath
        if (Application.dataPath.Contains("clone"))
        {
            var path = Application.dataPath;
            var idx = path.IndexOf("clone", StringComparison.OrdinalIgnoreCase);
            if (idx >= 0)
            {
                var after = path.Substring(idx + 5).TrimStart('_', ' ');
                if (after.Length > 0 && char.IsDigit(after[0]))
                    return int.Parse(after[0].ToString()) + 1;
                return 2;
            }
        }

        return 1; // main editor = player 1
    }

    private static int ParseCloneIndex(string[] args, int i)
    {
        if (i + 1 < args.Length && int.TryParse(args[i + 1], out int idx))
            return idx + 1;
        return 2;
    }
}

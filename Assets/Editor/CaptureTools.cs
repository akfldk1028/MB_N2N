// Assets/Editor/CaptureTools.cs
// Claude Code의 /unity-playtest, /unity-capture Skill이 이 스크립트를 사용합니다.

using UnityEditor;
using UnityEngine;
using System.IO;
using System;

public static class CaptureTools
{
    private static string ScreenshotDir =>
        Path.Combine(Application.dataPath, "..", "Screenshots");

    [MenuItem("Tools/Capture Game View")]
    public static void CaptureGameView()
    {
        if (!EditorApplication.isPlaying)
        {
            Debug.LogWarning("[CaptureTools] Game View capture requires Play mode");
            return;
        }

        // Game View must be focused for ScreenCapture to work
        var gameViewType = typeof(Editor).Assembly.GetType("UnityEditor.GameView");
        if (gameViewType != null)
            EditorWindow.GetWindow(gameViewType).Focus();

        Directory.CreateDirectory(ScreenshotDir);
        string path = Path.Combine(ScreenshotDir,
            $"game_{DateTime.Now:yyyyMMdd_HHmmss}.png");
        ScreenCapture.CaptureScreenshot(path);
        Debug.Log($"[CaptureTools] Game View saved: {path}");
    }

    [MenuItem("Tools/Capture Scene View")]
    public static void CaptureSceneView()
    {
        var sv = SceneView.lastActiveSceneView;
        if (sv == null)
        {
            Debug.LogError("[CaptureTools] No active Scene View");
            return;
        }

        int w = 1920, h = 1080;
        var rt = new RenderTexture(w, h, 24);
        sv.camera.targetTexture = rt;
        sv.camera.Render();

        RenderTexture.active = rt;
        var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        tex.Apply();

        sv.camera.targetTexture = null;
        RenderTexture.active = null;

        Directory.CreateDirectory(ScreenshotDir);
        string path = Path.Combine(ScreenshotDir,
            $"scene_{DateTime.Now:yyyyMMdd_HHmmss}.png");
        File.WriteAllBytes(path, tex.EncodeToPNG());

        UnityEngine.Object.DestroyImmediate(rt);
        UnityEngine.Object.DestroyImmediate(tex);

        Debug.Log($"[CaptureTools] Scene View saved: {path}");
    }

    private const string AutoCaptureKey = "CaptureTools_AutoCapture";

    [MenuItem("Tools/Capture on Play")]
    public static void ToggleAutoCapture()
    {
        bool current = EditorPrefs.GetBool(AutoCaptureKey, false);
        EditorPrefs.SetBool(AutoCaptureKey, !current);
        Menu.SetChecked("Tools/Capture on Play", !current);
        Debug.Log($"[CaptureTools] Auto-capture on Play: {!current}");
    }

    [MenuItem("Tools/Capture on Play", true)]
    private static bool ToggleAutoCaptureValidate()
    {
        Menu.SetChecked("Tools/Capture on Play", EditorPrefs.GetBool(AutoCaptureKey, false));
        return true;
    }

    [InitializeOnLoadMethod]
    private static void Init()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode && EditorPrefs.GetBool(AutoCaptureKey, false))
        {
            EditorApplication.delayCall += () =>
            {
                EditorApplication.delayCall += CaptureGameView;
            };
        }
    }
}

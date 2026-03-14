// [USK] Game/Scene View 캡쳐 도구
// ★ 범용 - 수정 불필요

using UnityEditor;
using UnityEngine;
using System.IO;
using System;

public static class USK_CaptureTools
{
    private static string ScreenshotDir =>
        Path.Combine(Application.dataPath, "..", "Screenshots");

    [MenuItem("Tools/USK/Capture Game View")]
    public static void CaptureGameView()
    {
        if (!EditorApplication.isPlaying)
        {
            Debug.LogWarning("[USK_Capture] Game View capture requires Play mode");
            return;
        }

        var gameViewType = typeof(Editor).Assembly.GetType("UnityEditor.GameView");
        if (gameViewType != null)
            EditorWindow.GetWindow(gameViewType).Focus();

        Directory.CreateDirectory(ScreenshotDir);
        string path = Path.Combine(ScreenshotDir, $"game_{DateTime.Now:yyyyMMdd_HHmmss}.png");
        ScreenCapture.CaptureScreenshot(path);
        Debug.Log($"[USK_Capture] Game View saved: {path}");
    }

    [MenuItem("Tools/USK/Capture Scene View")]
    public static void CaptureSceneView()
    {
        var sv = SceneView.lastActiveSceneView;
        if (sv == null)
        {
            Debug.LogError("[USK_Capture] No active Scene View");
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
        string path = Path.Combine(ScreenshotDir, $"scene_{DateTime.Now:yyyyMMdd_HHmmss}.png");
        File.WriteAllBytes(path, tex.EncodeToPNG());

        UnityEngine.Object.DestroyImmediate(rt);
        UnityEngine.Object.DestroyImmediate(tex);

        Debug.Log($"[USK_Capture] Scene View saved: {path}");
    }
}

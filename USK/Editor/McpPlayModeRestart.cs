using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>
/// MCP Unity 서버가 Play 모드에서 끊기는 문제 패치 v2.
///
/// 근본 원인: McpUnityServer.OnPlayModeStateChanged()가 ExitingEditMode에서
///            StopServer(4001) 호출 → WebSocket 끊김 → Node.js 크래시 →
///            Claude Code MCP 도구 완전 소멸
///
/// 해결: 패키지의 playModeStateChanged 핸들러를 reflection으로 제거하여
///       Play 모드에서도 WebSocket 서버를 유지. 서버가 죽으면 즉시 재시작.
/// </summary>
[InitializeOnLoad]
public static class McpPlayModeRestart
{
    static McpPlayModeRestart()
    {
        // ★ MPPM 클론 오인 캐시 리셋 (메인 에디터에서 서버 시작 가능하게)
        ResetMPPMCloneDetectionCache();

        // 패키지의 StopServer 호출을 막기 위해 핸들러 제거
        RemovePackagePlayModeHandler();
        RemovePackageAssemblyReloadHandler();

        // 우리 핸들러 등록 (서버 유지 + 필요시 재시작)
        EditorApplication.playModeStateChanged += OnPlayModeChanged;

        AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;

        // 서버가 안 떠있으면 강제 시작
        if (!IsServerListening())
        {
            TryStartServer();
        }

        Debug.Log($"[MCP Patch v2] 초기화 완료, IsListening={IsServerListening()}");
    }

    /// <summary>
    /// 패키지(McpUnityServer)가 등록한 playModeStateChanged 핸들러를 제거.
    /// 이 핸들러가 ExitingEditMode에서 StopServer(4001)을 호출하는 원흉.
    /// </summary>
    private static void RemovePackagePlayModeHandler()
    {
        try
        {
            var serverType = GetServerType();
            if (serverType == null)
            {
                Debug.LogWarning("[MCP Patch v2] McpUnityServer 타입을 찾을 수 없음");
                return;
            }

            // 패키지의 static 핸들러 메서드 찾기
            var handlerMethod = serverType.GetMethod("OnPlayModeStateChanged",
                BindingFlags.NonPublic | BindingFlags.Static);

            if (handlerMethod == null)
            {
                // public일 수도 있음
                handlerMethod = serverType.GetMethod("OnPlayModeStateChanged",
                    BindingFlags.Public | BindingFlags.Static);
            }

            if (handlerMethod != null)
            {
                var handler = (Action<PlayModeStateChange>)Delegate.CreateDelegate(
                    typeof(Action<PlayModeStateChange>), handlerMethod);
                EditorApplication.playModeStateChanged -= handler;
                Debug.Log("[MCP Patch v2] 패키지 PlayMode 핸들러 제거 성공 (StopServer 차단)");
            }
            else
            {
                Debug.LogWarning("[MCP Patch v2] OnPlayModeStateChanged 메서드를 찾을 수 없음 - 대체 방식 시도");
                // 대체: beforeAssemblyReload 핸들러도 제거 시도
                RemovePackageAssemblyReloadHandler(serverType);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[MCP Patch v2] 핸들러 제거 실패: {e.Message}");
        }
    }

    /// <summary>
    /// 패키지의 beforeAssemblyReload 핸들러도 제거 (StopServer 호출 차단)
    /// </summary>
    private static void RemovePackageAssemblyReloadHandler(Type serverType)
    {
        try
        {
            var handlerMethod = serverType.GetMethod("OnBeforeAssemblyReload",
                BindingFlags.NonPublic | BindingFlags.Static);
            if (handlerMethod == null)
                handlerMethod = serverType.GetMethod("OnBeforeAssemblyReload",
                    BindingFlags.Public | BindingFlags.Static);

            if (handlerMethod != null)
            {
                var handler = (AssemblyReloadEvents.AssemblyReloadCallback)
                    Delegate.CreateDelegate(typeof(AssemblyReloadEvents.AssemblyReloadCallback), handlerMethod);
                AssemblyReloadEvents.beforeAssemblyReload -= handler;
                Debug.Log("[MCP Patch v2] 패키지 AssemblyReload 핸들러도 제거");
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[MCP Patch v2] AssemblyReload 핸들러 제거 실패: {e.Message}");
        }
    }

    private static void OnAfterAssemblyReload()
    {
        // Assembly reload 후 패키지 핸들러가 다시 등록될 수 있으므로 재제거
        RemovePackagePlayModeHandler();
        RemovePackageAssemblyReloadHandler();

        bool listening = IsServerListening();
        Debug.Log($"[MCP Patch v2] Assembly reload 완료, IsListening={listening}");

        if (!listening)
        {
            Debug.Log("[MCP Patch v2] Assembly reload 후 서버 재시작");
            TryStartServer();
        }
    }

    /// <summary>
    /// 패키지의 OnBeforeAssemblyReload 핸들러를 직접 제거 (StopServer 차단)
    /// </summary>
    private static void RemovePackageAssemblyReloadHandler()
    {
        var serverType = GetServerType();
        if (serverType == null) return;
        RemovePackageAssemblyReloadHandler(serverType);
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        bool listening = IsServerListening();
        Debug.Log($"[MCP Patch v2] PlayModeState={state}, IsListening={listening}");

        switch (state)
        {
            case PlayModeStateChange.ExitingEditMode:
                // ★ 핵심: 서버를 멈추지 않음! 패키지 핸들러는 이미 제거됨
                if (!listening)
                {
                    Debug.Log("[MCP Patch v2] ExitingEditMode인데 서버가 꺼져있음 - 시작");
                    TryStartServer();
                }
                else
                {
                    Debug.Log("[MCP Patch v2] ExitingEditMode - 서버 유지 (StopServer 차단됨)");
                }
                break;

            case PlayModeStateChange.EnteredPlayMode:
                // 패키지 핸들러 재제거 (도메인 리로드 후 다시 등록될 수 있음)
                RemovePackagePlayModeHandler();
                RemovePackageAssemblyReloadHandler();

                // 서버가 죽었으면 재시작 (항상 시도)
                if (!listening)
                {
                    Debug.Log("[MCP Patch v2] EnteredPlayMode - 서버 재시작");
                    TryStartServer();
                }
                else
                {
                    Debug.Log("[MCP Patch v2] EnteredPlayMode - 서버 정상 유지중");
                }
                // 안전망: delayCall로 한번 더 확인
                EditorApplication.delayCall += RetryStartIfNeeded;
                break;

            case PlayModeStateChange.ExitingPlayMode:
                // 서버 유지
                Debug.Log("[MCP Patch v2] ExitingPlayMode - 서버 유지");
                break;

            case PlayModeStateChange.EnteredEditMode:
                // 서버가 죽었으면 재시작
                if (!listening)
                {
                    Debug.Log("[MCP Patch v2] EnteredEditMode - 서버 재시작");
                    TryStartServer();
                    EditorApplication.delayCall += RetryStartIfNeeded;
                }
                break;
        }
    }

    private static void RetryStartIfNeeded()
    {
        if (!IsServerListening())
        {
            Debug.Log("[MCP Patch v2] 재시도 StartServer...");
            TryStartServer();

            // 2차 재시도
            EditorApplication.delayCall += () =>
            {
                if (!IsServerListening())
                {
                    Debug.LogWarning("[MCP Patch v2] 2차 재시도...");
                    TryStartServer();
                }
            };
        }
    }

    /// <summary>
    /// McpUtils._isMultiplayerPlayModeClone 캐시를 false로 리셋.
    /// MPPM이 메인 에디터를 클론으로 오인하는 문제 해결.
    /// </summary>
    private static void ResetMPPMCloneDetectionCache()
    {
#if UNITY_EDITOR
        try
        {
            // 메인 에디터에서만 리셋
            if (!Unity.Multiplayer.Playmode.CurrentPlayer.IsMainEditor)
                return;

            var utilsType = Type.GetType("McpUnity.Utils.McpUtils, McpUnity.Editor");
            if (utilsType == null) return;

            var cacheField = utilsType.GetField("_isMultiplayerPlayModeClone",
                BindingFlags.NonPublic | BindingFlags.Static);
            if (cacheField != null)
            {
                // Nullable<bool>을 false로 설정
                cacheField.SetValue(null, (bool?)false);
                Debug.Log("[MCP Patch v2] MPPM 클론 감지 캐시 리셋 → false (메인 에디터)");
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[MCP Patch v2] MPPM 캐시 리셋 실패: {e.Message}");
        }
#endif
    }

    private static Type GetServerType()
    {
        return Type.GetType("McpUnity.Unity.McpUnityServer, McpUnity.Editor");
    }

    private static void TryStartServer()
    {
        // ★ MPPM 클론 감지 캐시를 리셋하여 메인 에디터에서도 서버 시작 가능하게
        ResetMPPMCloneDetectionCache();

        var serverType = GetServerType();
        if (serverType == null)
        {
            Debug.LogWarning("[MCP Patch v2] McpUnityServer 타입 없음");
            return;
        }

        var instanceProp = serverType.GetProperty("Instance",
            BindingFlags.Public | BindingFlags.Static);
        var startMethod = serverType.GetMethod("StartServer",
            BindingFlags.Public | BindingFlags.Instance,
            null, Type.EmptyTypes, null);

        if (instanceProp == null || startMethod == null)
        {
            Debug.LogWarning($"[MCP Patch v2] Reflection 실패 - Instance={instanceProp != null}, StartServer={startMethod != null}");
            return;
        }

        try
        {
            var instance = instanceProp.GetValue(null);
            if (instance == null)
            {
                Debug.LogWarning("[MCP Patch v2] Instance가 null");
                return;
            }
            startMethod.Invoke(instance, null);
            Debug.Log($"[MCP Patch v2] StartServer 호출 완료, IsListening={IsServerListening()}");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[MCP Patch v2] StartServer 실패: {e.Message}");
        }
    }

    private static bool IsServerListening()
    {
        var serverType = GetServerType();
        if (serverType == null) return false;

        var instanceProp = serverType.GetProperty("Instance",
            BindingFlags.Public | BindingFlags.Static);
        var isListeningProp = serverType.GetProperty("IsListening",
            BindingFlags.Public | BindingFlags.Instance);
        if (instanceProp == null || isListeningProp == null) return false;

        try
        {
            var instance = instanceProp.GetValue(null);
            if (instance == null) return false;
            return (bool)isListeningProp.GetValue(instance);
        }
        catch { return false; }
    }
}

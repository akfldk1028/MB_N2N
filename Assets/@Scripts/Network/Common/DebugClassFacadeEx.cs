using UnityEngine;

/// <summary>
/// 디버그 로깅을 위한 파사드 클래스 - VContainer 의존성을 제거하고 Managers 패턴으로 리팩토링됨
/// 다양한 로그 레벨과 색상을 지원하는 통합 디버깅 시스템
/// </summary>
public class DebugClassFacadeEx
{
    private bool m_EnableDebugLogs = true;
    private bool m_EnableInfoLogs = true;
    private bool m_EnableWarningLogs = true;
    private bool m_EnableErrorLogs = true;

    public DebugClassFacadeEx(bool enableDebug = true, bool enableInfo = true, bool enableWarning = true, bool enableError = true)
    {
        m_EnableDebugLogs = enableDebug;
        m_EnableInfoLogs = enableInfo;
        m_EnableWarningLogs = enableWarning;
        m_EnableErrorLogs = enableError;
    }

    /// <summary>
    /// 디버그 레벨 로그 출력
    /// </summary>
    public void LogDebug(string className, string message)
    {
        if (m_EnableDebugLogs)
        {
            Debug.Log($"<color=grey>[DEBUG][{className}] {message}</color>");
        }
    }

    /// <summary>
    /// 정보 레벨 로그 출력
    /// </summary>
    public void LogInfo(string className, string message)
    {
        if (m_EnableInfoLogs)
        {
            Debug.Log($"<color=white>[INFO][{className}] {message}</color>");
        }
    }

    /// <summary>
    /// 경고 레벨 로그 출력
    /// </summary>
    public void LogWarning(string className, string message)
    {
        if (m_EnableWarningLogs)
        {
            Debug.LogWarning($"<color=yellow>[WARNING][{className}] {message}</color>");
        }
    }

    /// <summary>
    /// 오류 레벨 로그 출력
    /// </summary>
    public void LogError(string className, string message)
    {
        if (m_EnableErrorLogs)
        {
            Debug.LogError($"<color=red>[ERROR][{className}] {message}</color>");
        }
    }

    /// <summary>
    /// 커스텀 색상으로 로그 출력
    /// </summary>
    public void LogWithColor(string className, string message, string color)
    {
        Debug.Log($"<color={color}>[{className}] {message}</color>");
    }

    /// <summary>
    /// 로그 레벨 설정
    /// </summary>
    public void SetLogLevels(bool debug, bool info, bool warning, bool error)
    {
        m_EnableDebugLogs = debug;
        m_EnableInfoLogs = info;
        m_EnableWarningLogs = warning;
        m_EnableErrorLogs = error;
    }
}
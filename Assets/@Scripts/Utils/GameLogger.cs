using UnityEngine;

/// <summary>
/// 게임 전체에서 사용하는 중앙집중식 로그 시스템
/// 색상, 이모지, 로그 레벨을 통합 관리
/// </summary>
public static class GameLogger
{
    public enum LogLevel
    {
        Debug,      // 개발 디버그용
        Info,       // 일반 정보
        Warning,    // 경고
        Error,      // 오류
        Success     // 성공
    }

    public enum LogColor
    {
        White,
        Green,
        Red, 
        Yellow,
        Cyan,
        Magenta,
        Orange,
        Lime,
        Blue,
        Grey
    }

    // 개발 빌드에서만 로그 출력 여부
    public static bool EnableLogging = true;

    /// <summary>
    /// 기본 로그 출력 (색상과 레벨 지정 가능)
    /// </summary>
    public static void Log(string className, string message, LogColor color = LogColor.White, LogLevel level = LogLevel.Info)
    {
        if (!EnableLogging) return;

        string colorName = GetColorName(color);
        string emoji = GetEmojiForLevel(level);
        string formattedMessage = $"<color={colorName}>[{className}] {emoji} {message}</color>";

        switch (level)
        {
            case LogLevel.Warning:
                Debug.LogWarning(formattedMessage);
                break;
            case LogLevel.Error:
                Debug.LogError(formattedMessage);
                break;
            default:
                Debug.Log(formattedMessage);
                break;
        }
    }

    /// <summary>
    /// 성공 로그 (자동으로 녹색 + ✅)
    /// </summary>
    public static void Success(string className, string message)
    {
        Log(className, message, LogColor.Green, LogLevel.Success);
    }

    /// <summary>
    /// 경고 로그 (자동으로 노란색 + ⚠️)
    /// </summary>
    public static void Warning(string className, string message)
    {
        Log(className, message, LogColor.Yellow, LogLevel.Warning);
    }

    /// <summary>
    /// 오류 로그 (자동으로 빨간색 + ❌)
    /// </summary>
    public static void Error(string className, string message)
    {
        Log(className, message, LogColor.Red, LogLevel.Error);
    }

    /// <summary>
    /// 정보 로그 (자동으로 시안색 + 🔧)
    /// </summary>
    public static void Info(string className, string message)
    {
        Log(className, message, LogColor.Cyan, LogLevel.Info);
    }

    /// <summary>
    /// 진행 상황 로그 (자동으로 마젠타 + 🔄)
    /// </summary>
    public static void Progress(string className, string message)
    {
        Log(className, message, LogColor.Magenta, LogLevel.Info);
    }

    /// <summary>
    /// 네트워크 관련 로그 (자동으로 라임 + 📡)
    /// </summary>
    public static void Network(string className, string message)
    {
        Log(className, message, LogColor.Lime, LogLevel.Info);
    }

    /// <summary>
    /// 시스템 시작 로그 (자동으로 시안 + 🚀)
    /// </summary>
    public static void SystemStart(string className, string message)
    {
        Log(className, message, LogColor.Cyan, LogLevel.Info);
    }

    // === 헬퍼 메서드들 ===

    private static string GetColorName(LogColor color)
    {
        return color switch
        {
            LogColor.White => "white",
            LogColor.Green => "green", 
            LogColor.Red => "red",
            LogColor.Yellow => "yellow",
            LogColor.Cyan => "cyan",
            LogColor.Magenta => "magenta",
            LogColor.Orange => "orange",
            LogColor.Lime => "lime",
            LogColor.Blue => "blue",
            LogColor.Grey => "grey",
            _ => "white"
        };
    }

    private static string GetEmojiForLevel(LogLevel level)
    {
        return level switch
        {
            LogLevel.Debug => "🔍",
            LogLevel.Info => "🔧", 
            LogLevel.Warning => "⚠️",
            LogLevel.Error => "❌",
            LogLevel.Success => "✅",
            _ => "📝"
        };
    }

    /// <summary>
    /// 개발 빌드에서만 로그 활성화
    /// </summary>
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public static void DevLog(string className, string message, LogColor color = LogColor.Grey)
    {
        Log(className, $"[DEV] {message}", color, LogLevel.Debug);
    }

    /// <summary>
    /// 성능 측정을 위한 로그
    /// </summary>
    public static void Performance(string className, string message, float duration = -1)
    {
        string perfMessage = duration > 0 
            ? $"⏱️ {message} (소요시간: {duration:F2}ms)"
            : $"⏱️ {message}";
        Log(className, perfMessage, LogColor.Orange, LogLevel.Info);
    }
}







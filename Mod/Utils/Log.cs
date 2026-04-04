using MelonLoader;
using UnityEngine;

namespace Mod.Utils;

internal enum LogSource
{
    LEHud,
    Hooks,
    Game,
    Settings,
    AntiIdle
}

internal enum LogLevel
{
    Info,
    Warning,
    Error
}

internal static class Log
{
    private sealed class ThrottleState
    {
        public DateTime LastLogUtc { get; set; }
        public int SuppressedCount { get; set; }
    }

    private static readonly object s_lock = new();
    private static readonly Dictionary<string, ThrottleState> s_throttleStates = new(StringComparer.Ordinal);

    public static void Info(LogSource source, string message) => Write(source, LogLevel.Info, message);

    public static void Warning(LogSource source, string message) => Write(source, LogLevel.Warning, message);

    public static void Error(LogSource source, string message) => Write(source, LogLevel.Error, message);

    public static void InfoThrottled(LogSource source, string key, string message, TimeSpan interval)
        => WriteThrottled(source, LogLevel.Info, key, message, interval);

    public static void WarningThrottled(LogSource source, string key, string message, TimeSpan interval)
        => WriteThrottled(source, LogLevel.Warning, key, message, interval);

    public static void ErrorThrottled(LogSource source, string key, string message, TimeSpan interval)
        => WriteThrottled(source, LogLevel.Error, key, message, interval);

    public static void GameLog(LogType logType, string? contextName, string? format)
    {
        var level = logType switch
        {
            LogType.Warning => LogLevel.Warning,
            LogType.Assert => LogLevel.Error,
            LogType.Error => LogLevel.Error,
            LogType.Exception => LogLevel.Error,
            _ => LogLevel.Info
        };

        var cleanedContext = string.IsNullOrWhiteSpace(contextName) ? "null" : contextName;
        var cleanedMessage = string.IsNullOrWhiteSpace(format) ? "<empty>" : format.Trim();
        var payload = $"{logType} | ctx={cleanedContext} | {cleanedMessage}";
        var key = $"{logType}|{cleanedContext}|{cleanedMessage}";

        WriteThrottled(LogSource.Game, level, key, payload, TimeSpan.FromSeconds(2));
    }

    public static void GameException(string? contextName, string? exceptionText)
    {
        var cleanedContext = string.IsNullOrWhiteSpace(contextName) ? "null" : contextName;
        var cleanedException = string.IsNullOrWhiteSpace(exceptionText) ? "<empty>" : exceptionText.Trim();
        var payload = $"Exception | ctx={cleanedContext} | {cleanedException}";
        var key = $"exception|{cleanedContext}|{cleanedException}";

        WriteThrottled(LogSource.Game, LogLevel.Error, key, payload, TimeSpan.FromSeconds(2));
    }

    private static void WriteThrottled(LogSource source, LogLevel level, string key, string message, TimeSpan interval)
    {
        if (interval <= TimeSpan.Zero)
        {
            Write(source, level, message);
            return;
        }

        var now = DateTime.UtcNow;
        int suppressedCount = 0;

        lock (s_lock)
        {
            if (!s_throttleStates.TryGetValue(key, out var state))
            {
                s_throttleStates[key] = new ThrottleState { LastLogUtc = now };
            }
            else if (now - state.LastLogUtc < interval)
            {
                state.SuppressedCount++;
                return;
            }
            else
            {
                suppressedCount = state.SuppressedCount;
                state.SuppressedCount = 0;
                state.LastLogUtc = now;
            }
        }

        if (suppressedCount > 0)
        {
            message = $"{message} (suppressed {suppressedCount} repeats)";
        }

        Write(source, level, message);
    }

    private static void Write(LogSource source, LogLevel level, string message)
    {
        var prefixed = $"[{source}] {message}";
        switch (level)
        {
            case LogLevel.Warning:
                MelonLogger.Warning(prefixed);
                break;
            case LogLevel.Error:
                MelonLogger.Error(prefixed);
                break;
            default:
                MelonLogger.Msg(prefixed);
                break;
        }
    }
}

using MelonLoader;
using UnityEngine;
using System.Globalization;

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
    private sealed class GameEvent
    {
        public DateTime TimestampUtc { get; init; }
        public LogLevel Level { get; init; }
        public string Phase { get; init; } = "Unknown";
        public string Summary { get; init; } = string.Empty;
    }

    private sealed class ThrottleState
    {
        public DateTime LastLogUtc { get; set; }
        public int SuppressedCount { get; set; }
    }

    private const int MaxBufferedGameEvents = 240;

    private static readonly object s_lock = new();
    private static readonly Dictionary<string, ThrottleState> s_throttleStates = new(StringComparer.Ordinal);
    private static readonly Queue<GameEvent> s_gameEvents = new();
    private static readonly Dictionary<string, DateTime> s_dumpGates = new(StringComparer.Ordinal);
    private static string s_currentGamePhase = "Boot";

    public static void Info(LogSource source, string message) => Write(source, LogLevel.Info, message);

    public static void Warning(LogSource source, string message) => Write(source, LogLevel.Warning, message);

    public static void Error(LogSource source, string message) => Write(source, LogLevel.Error, message);

    public static void InfoThrottled(LogSource source, string key, string message, TimeSpan interval)
        => WriteThrottled(source, LogLevel.Info, key, message, interval);

    public static void WarningThrottled(LogSource source, string key, string message, TimeSpan interval)
        => WriteThrottled(source, LogLevel.Warning, key, message, interval);

    public static void ErrorThrottled(LogSource source, string key, string message, TimeSpan interval)
        => WriteThrottled(source, LogLevel.Error, key, message, interval);

    public static void MarkGamePhase(string phase)
    {
        if (string.IsNullOrWhiteSpace(phase))
            return;

        lock (s_lock)
        {
            s_currentGamePhase = phase.Trim();
        }
    }

    public static bool IsLikelyLoginFailureException(string? exceptionText)
    {
        if (string.IsNullOrWhiteSpace(exceptionText))
            return false;

        return exceptionText.Contains("LoadingScreen", StringComparison.OrdinalIgnoreCase)
            || exceptionText.Contains("DisableAsync", StringComparison.OrdinalIgnoreCase)
            || exceptionText.Contains("NullReferenceException", StringComparison.OrdinalIgnoreCase)
            || exceptionText.Contains("returning to menu", StringComparison.OrdinalIgnoreCase);
    }

    public static void DumpRecentGameEventsThrottled(string key, string reason, TimeSpan minInterval, int maxLines = 80)
    {
        if (string.IsNullOrWhiteSpace(key))
            key = "default";
        if (maxLines <= 0)
            maxLines = 1;

        DateTime now = DateTime.UtcNow;
        GameEvent[] snapshot;
        string phaseAtDump;

        lock (s_lock)
        {
            if (s_dumpGates.TryGetValue(key, out var lastDumpUtc) && now - lastDumpUtc < minInterval)
            {
                return;
            }

            s_dumpGates[key] = now;
            snapshot = s_gameEvents.ToArray();
            phaseAtDump = s_currentGamePhase;
        }

        Warning(LogSource.Game, $"Diagnostic dump triggered: {reason} (phase={phaseAtDump})");
        if (snapshot.Length == 0)
        {
            Info(LogSource.Game, "Diagnostic dump: no buffered game events.");
            return;
        }

        int start = Math.Max(0, snapshot.Length - maxLines);
        for (int i = start; i < snapshot.Length; i++)
        {
            var e = snapshot[i];
            Write(LogSource.Game, e.Level, $"{e.TimestampUtc:HH:mm:ss.fff} [{e.Phase}] {e.Summary}");
        }
    }

    public static void GameLog(LogType logType, string? contextName, string? format, IReadOnlyList<string>? args)
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
        var cleanedMessage = FormatGameMessage(format, args);
        var payload = $"{logType} | ctx={cleanedContext} | {cleanedMessage}";
        var key = $"{logType}|{cleanedContext}|{cleanedMessage}";

        RecordGameEvent(level, payload);

        // Preserve all Error/Exception details to help root-cause login/load failures.
        if (level == LogLevel.Error)
        {
            Write(LogSource.Game, level, payload);
            return;
        }

        WriteThrottled(LogSource.Game, level, key, payload, TimeSpan.FromSeconds(2));
    }

    public static void GameException(string? contextName, string? exceptionText)
    {
        var cleanedContext = string.IsNullOrWhiteSpace(contextName) ? "null" : contextName;
        var cleanedException = string.IsNullOrWhiteSpace(exceptionText) ? "<empty>" : exceptionText.Trim();
        var payload = $"Exception | ctx={cleanedContext} | {cleanedException}";
        RecordGameEvent(LogLevel.Error, payload);
        Write(LogSource.Game, LogLevel.Error, payload);
    }

    private static string FormatGameMessage(string? format, IReadOnlyList<string>? args)
    {
        var template = string.IsNullOrWhiteSpace(format) ? "<empty>" : format.Trim();
        if (args == null || args.Count == 0)
            return template;

        var values = new object?[args.Count];
        for (int i = 0; i < args.Count; i++)
        {
            values[i] = args[i] ?? "null";
        }

        try
        {
            return string.Format(CultureInfo.InvariantCulture, template, values);
        }
        catch (FormatException)
        {
            return $"{template} | args=[{string.Join(", ", values)}]";
        }
    }

    private static void RecordGameEvent(LogLevel level, string message)
    {
        string summary = CreateSummary(message);

        lock (s_lock)
        {
            s_gameEvents.Enqueue(new GameEvent
            {
                TimestampUtc = DateTime.UtcNow,
                Level = level,
                Phase = s_currentGamePhase,
                Summary = summary
            });

            while (s_gameEvents.Count > MaxBufferedGameEvents)
            {
                _ = s_gameEvents.Dequeue();
            }
        }
    }

    private static string CreateSummary(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return "<empty>";

        string firstLine = message;
        int newlineIndex = message.IndexOfAny(new[] { '\r', '\n' });
        if (newlineIndex >= 0)
        {
            firstLine = message[..newlineIndex];
        }

        firstLine = firstLine.Trim();
        const int maxLength = 220;
        if (firstLine.Length <= maxLength)
            return firstLine;

        return firstLine[..maxLength] + "...";
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

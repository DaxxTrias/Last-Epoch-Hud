using MelonLoader;
using UnityEngine;
using System.Globalization;
using System.Diagnostics;
using System.Text;

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

    private sealed class NetworkBreadcrumb
    {
        public DateTime TimestampUtc { get; init; }
        public string Phase { get; init; } = "Unknown";
        public string Stage { get; init; } = string.Empty;
        public string Details { get; init; } = string.Empty;
    }

    private sealed class ThrottleState
    {
        public DateTime LastLogUtc { get; set; }
        public int SuppressedCount { get; set; }
    }

    private const int MaxBufferedGameEvents = 240;
    private const int MaxBufferedNetworkBreadcrumbs = 400;

    private static readonly object s_lock = new();
    private static readonly Dictionary<string, ThrottleState> s_throttleStates = new(StringComparer.Ordinal);
    private static readonly Queue<GameEvent> s_gameEvents = new();
    private static readonly Queue<NetworkBreadcrumb> s_networkBreadcrumbs = new();
    private static readonly Dictionary<string, DateTime> s_networkBreadcrumbGates = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, DateTime> s_dumpGates = new(StringComparer.Ordinal);
    private static string s_currentGamePhase = "Boot";
    private static DateTime s_shaDiagnosticsWindowUntilUtc = DateTime.MinValue;
    private static int s_shaDiagnosticsCapturesInWindow = 0;
    private static bool IsNetworkDiagnosticsEnabled => global::Mod.Settings.enableNetworkDiagnostics;

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

    public static void ArmShaDiagnosticsWindow(string reason, TimeSpan duration)
    {
        if (!IsNetworkDiagnosticsEnabled)
            return;

        if (duration <= TimeSpan.Zero)
            duration = TimeSpan.FromSeconds(30);

        DateTime now = DateTime.UtcNow;
        lock (s_lock)
        {
            var until = now + duration;
            if (until > s_shaDiagnosticsWindowUntilUtc)
            {
                s_shaDiagnosticsWindowUntilUtc = until;
                s_shaDiagnosticsCapturesInWindow = 0;
            }
        }

        InfoThrottled(
            LogSource.Game,
            $"sha-window-arm:{reason}",
            $"SHA diagnostics armed for {duration.TotalSeconds:F0}s ({reason})",
            TimeSpan.FromSeconds(15));
    }

    public static bool IsShaDiagnosticsWindowActive()
    {
        if (!IsNetworkDiagnosticsEnabled)
            return false;

        lock (s_lock)
        {
            return DateTime.UtcNow <= s_shaDiagnosticsWindowUntilUtc;
        }
    }

    public static void CaptureNetworkBreadcrumb(string stage, object? payload, TimeSpan minInterval)
    {
        if (!IsNetworkDiagnosticsEnabled)
            return;

        if (string.IsNullOrWhiteSpace(stage))
            return;

        DateTime now = DateTime.UtcNow;
        string typeName = payload?.GetType().FullName ?? "<null>";
        string gateKey = $"{stage}|{typeName}";
        string phase;

        lock (s_lock)
        {
            if (s_networkBreadcrumbGates.TryGetValue(gateKey, out var lastUtc) && now - lastUtc < minInterval)
                return;

            s_networkBreadcrumbGates[gateKey] = now;
            phase = s_currentGamePhase;
        }

        EnqueueNetworkBreadcrumb(now, phase, stage, DescribeNetworkPayload(payload));
    }

    public static void CaptureNetworkBreadcrumb(string stage, string details, TimeSpan minInterval)
    {
        if (!IsNetworkDiagnosticsEnabled)
            return;

        if (string.IsNullOrWhiteSpace(stage))
            return;

        DateTime now = DateTime.UtcNow;
        string gateKey = $"{stage}|{details}";
        string phase;

        lock (s_lock)
        {
            if (s_networkBreadcrumbGates.TryGetValue(gateKey, out var lastUtc) && now - lastUtc < minInterval)
                return;

            s_networkBreadcrumbGates[gateKey] = now;
            phase = s_currentGamePhase;
        }

        EnqueueNetworkBreadcrumb(now, phase, stage, string.IsNullOrWhiteSpace(details) ? "<empty>" : details.Trim());
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

    public static void DumpRecentNetworkBreadcrumbsThrottled(string key, string reason, TimeSpan minInterval, int maxLines = 100)
    {
        if (!IsNetworkDiagnosticsEnabled)
            return;

        if (string.IsNullOrWhiteSpace(key))
            key = "net-default";
        if (maxLines <= 0)
            maxLines = 1;

        DateTime now = DateTime.UtcNow;
        NetworkBreadcrumb[] snapshot;
        string phaseAtDump;

        lock (s_lock)
        {
            if (s_dumpGates.TryGetValue(key, out var lastDumpUtc) && now - lastDumpUtc < minInterval)
                return;

            s_dumpGates[key] = now;
            snapshot = s_networkBreadcrumbs.ToArray();
            phaseAtDump = s_currentGamePhase;
        }

        Warning(LogSource.Game, $"Network breadcrumb dump triggered: {reason} (phase={phaseAtDump})");
        if (snapshot.Length == 0)
        {
            Info(LogSource.Game, "Network breadcrumb dump: no buffered breadcrumbs.");
            return;
        }

        int start = Math.Max(0, snapshot.Length - maxLines);
        for (int i = start; i < snapshot.Length; i++)
        {
            var b = snapshot[i];
            Warning(LogSource.Game, $"NET {b.TimestampUtc:HH:mm:ss.fff} [{b.Phase}] {b.Stage} | {b.Details}");
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

        if (IsShaMismatchMessage(cleanedMessage))
        {
            TryCaptureShaMismatchDiagnostics(cleanedContext, cleanedMessage);
        }

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

    private static bool IsShaMismatchMessage(string message)
    {
        return message.Contains("SHA MISMATCH", StringComparison.OrdinalIgnoreCase);
    }

    private static void TryCaptureShaMismatchDiagnostics(string contextName, string message)
    {
        if (!IsNetworkDiagnosticsEnabled)
            return;

        DateTime now = DateTime.UtcNow;
        lock (s_lock)
        {
            if (now > s_shaDiagnosticsWindowUntilUtc)
            {
                return;
            }

            if (s_shaDiagnosticsCapturesInWindow >= 1)
            {
                return;
            }

            s_shaDiagnosticsCapturesInWindow++;
        }

        Warning(LogSource.Game, $"SHA probe captured in active window | ctx={contextName} | {message}");
        DumpRecentGameEventsThrottled(
            key: "sha-mismatch-recent-events",
            reason: "SHA mismatch observed in connect/load window",
            minInterval: TimeSpan.FromSeconds(5),
            maxLines: 120);
        DumpRecentNetworkBreadcrumbsThrottled(
            key: "sha-mismatch-network-breadcrumbs",
            reason: "SHA mismatch observed in connect/load window",
            minInterval: TimeSpan.FromSeconds(5),
            maxLines: 120);

        try
        {
            var stack = new StackTrace(skipFrames: 2, fNeedFileInfo: false);
            var frames = stack.GetFrames();
            if (frames == null || frames.Length == 0)
            {
                Warning(LogSource.Game, "SHA probe stack trace unavailable.");
                return;
            }

            int maxFrames = Math.Min(frames.Length, 24);
            string? likelyOrigin = null;
            for (int i = 0; i < maxFrames; i++)
            {
                var method = frames[i].GetMethod();
                if (method == null)
                    continue;

                string typeName = method.DeclaringType?.FullName ?? "<global>";
                string methodName = method.Name;
                Warning(LogSource.Game, $"SHA probe stack[{i}] {typeName}.{methodName}");

                if (likelyOrigin == null && IsLikelyOriginFrame(typeName, methodName))
                {
                    likelyOrigin = typeName == "<global>"
                        ? methodName
                        : $"{typeName}.{methodName}";
                }
            }

            if (!string.IsNullOrWhiteSpace(likelyOrigin))
            {
                Warning(LogSource.Game, $"SHA probe likely origin: {likelyOrigin}");
            }
            else
            {
                Warning(LogSource.Game, "SHA probe likely origin: unavailable (all frames looked like mod/runtime wrappers)");
            }
        }
        catch (Exception ex)
        {
            Warning(LogSource.Game, $"SHA probe stack trace failed: {ex.GetType().Name} {ex.Message}");
        }
    }

    public static string DescribePayloadForDiagnostics(object? payload) => DescribeNetworkPayload(payload);

    private static bool IsLikelyOriginFrame(string typeName, string methodName)
    {
        if (typeName.StartsWith("Mod.", StringComparison.Ordinal)
            || typeName.StartsWith("HarmonyLib.", StringComparison.Ordinal)
            || typeName.StartsWith("System.", StringComparison.Ordinal)
            || typeName.StartsWith("Il2CppInterop.Runtime.", StringComparison.Ordinal))
        {
            return false;
        }

        if (typeName == "<global>")
        {
            if (methodName.Contains("DMD<", StringComparison.Ordinal)
                || methodName.Contains("il2cpp_runtime_invoke", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // IL2CPP global wrappers often keep the native target as Namespace.Type::Method.
            return methodName.Contains("::", StringComparison.Ordinal);
        }

        return true;
    }

    private static void EnqueueNetworkBreadcrumb(DateTime timestampUtc, string phase, string stage, string details)
    {
        lock (s_lock)
        {
            s_networkBreadcrumbs.Enqueue(new NetworkBreadcrumb
            {
                TimestampUtc = timestampUtc,
                Phase = phase,
                Stage = stage,
                Details = details
            });

            while (s_networkBreadcrumbs.Count > MaxBufferedNetworkBreadcrumbs)
            {
                _ = s_networkBreadcrumbs.Dequeue();
            }
        }
    }

    private static string DescribeNetworkPayload(object? payload)
    {
        if (payload == null)
            return "payload=<null>";

        try
        {
            var t = payload.GetType();
            var sb = new StringBuilder(200);
            sb.Append("type=").Append(t.FullName ?? t.Name);

            AppendKnownMember(sb, payload, t, "MessageKey");
            AppendKnownMember(sb, payload, t, "messageKey");
            AppendKnownMember(sb, payload, t, "MessageType");
            AppendKnownMember(sb, payload, t, "m_messageType");
            AppendKnownMember(sb, payload, t, "m_receivedMessageType");
            AppendKnownMember(sb, payload, t, "messageType");
            AppendKnownMember(sb, payload, t, "OpCode");
            AppendKnownMember(sb, payload, t, "Opcode");
            AppendKnownMember(sb, payload, t, "opCode");
            AppendKnownMember(sb, payload, t, "Type");
            AppendKnownMember(sb, payload, t, "type");
            AppendKnownMember(sb, payload, t, "Id");
            AppendKnownMember(sb, payload, t, "id");
            AppendKnownMember(sb, payload, t, "LengthBytes");
            AppendKnownMember(sb, payload, t, "LengthBits");
            AppendKnownMember(sb, payload, t, "m_bitLength");
            AppendKnownMember(sb, payload, t, "m_sequenceChannel");
            AppendKnownMember(sb, payload, t, "SequenceChannel");
            AppendKnownMember(sb, payload, t, "SenderConnection");
            AppendKnownMember(sb, payload, t, "SenderEndPoint");
            AppendKnownMember(sb, payload, t, "SenderEndpoint");
            AppendKnownMember(sb, payload, t, "ReceiveTime");
            AppendKnownMember(sb, payload, t, "PositionInBytes");
            AppendKnownMember(sb, payload, t, "PositionInBits");
            TryAppendDataFingerprint(sb, payload, t);

            string text = payload.ToString() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(text))
            {
                text = text.Trim();
                if (!string.Equals(text, t.FullName, StringComparison.Ordinal)
                    && !string.Equals(text, t.Name, StringComparison.Ordinal))
                {
                    sb.Append(" | text=").Append(TrimForLog(text, 140));
                }
            }

            return sb.ToString();
        }
        catch (Exception ex)
        {
            return $"payload-describe-failed: {ex.GetType().Name} {ex.Message}";
        }
    }

    private static void AppendKnownMember(StringBuilder sb, object payload, Type type, string memberName)
    {
        try
        {
            var prop = type.GetProperty(memberName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            if (prop != null && prop.GetIndexParameters().Length == 0)
            {
                var value = prop.GetValue(payload);
                if (value != null)
                {
                    sb.Append(" | ").Append(memberName).Append('=').Append(TrimForLog(value.ToString() ?? "null", 60));
                    return;
                }
            }

            var field = type.GetField(memberName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            if (field != null)
            {
                var value = field.GetValue(payload);
                if (value != null)
                {
                    sb.Append(" | ").Append(memberName).Append('=').Append(TrimForLog(value.ToString() ?? "null", 60));
                }
            }
        }
        catch
        {
            // best-effort diagnostics only
        }
    }

    private static void TryAppendDataFingerprint(StringBuilder sb, object payload, Type type)
    {
        object? data = TryGetMemberValue(payload, type, "m_data")
            ?? TryGetMemberValue(payload, type, "data")
            ?? TryGetMemberValue(payload, type, "Data");
        if (data == null)
            return;

        if (!TryExtractBytes(data, out var bytes))
            return;

        int len = bytes.Length;
        uint hash = ComputeFNV1a(bytes.AsSpan(0, Math.Min(len, 128)));
        sb.Append(" | dataLen=").Append(len).Append(" | dataFp=").Append(hash.ToString("X8", CultureInfo.InvariantCulture));
    }

    private static object? TryGetMemberValue(object payload, Type type, string memberName)
    {
        try
        {
            var prop = type.GetProperty(memberName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            if (prop != null && prop.GetIndexParameters().Length == 0)
                return prop.GetValue(payload);

            var field = type.GetField(memberName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            if (field != null)
                return field.GetValue(payload);
        }
        catch
        {
            // diagnostics best-effort
        }

        return null;
    }

    private static bool TryExtractBytes(object data, out byte[] bytes)
    {
        bytes = Array.Empty<byte>();

        if (data is byte[] direct)
        {
            bytes = direct;
            return true;
        }

        if (data is Array arr && arr.Length > 0)
        {
            try
            {
                bytes = new byte[arr.Length];
                for (int i = 0; i < arr.Length; i++)
                {
                    bytes[i] = Convert.ToByte(arr.GetValue(i), CultureInfo.InvariantCulture);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        return false;
    }

    private static uint ComputeFNV1a(ReadOnlySpan<byte> data)
    {
        const uint offset = 2166136261;
        const uint prime = 16777619;
        uint hash = offset;
        for (int i = 0; i < data.Length; i++)
        {
            hash ^= data[i];
            hash *= prime;
        }

        return hash;
    }

    private static string TrimForLog(string value, int maxLen)
    {
        if (string.IsNullOrWhiteSpace(value))
            return value;

        if (value.Length <= maxLen)
            return value;

        return value[..maxLen] + "...";
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

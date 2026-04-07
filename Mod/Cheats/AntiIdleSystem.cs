using MelonLoader;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using HarmonyLib;
using Il2Cpp;
using Mod.Utils;

namespace Mod.Cheats
{
    /// <summary>
    /// Handles anti-idle timeout detection and prevention for Last Epoch
    /// </summary>
    internal static class AntiIdleSystem
    {
        private const bool VerboseHeartbeatLogs = false; // Toggle to re-enable detailed heartbeat write logs
        private const bool VerboseStatusLogs = false; // Toggle to re-enable status/info logs
        private const bool VerboseKeepaliveLogs = false; // Toggle to log successful synthetic keepalives
        private const bool VerboseActionLogs = false; // Toggle to log legacy action start/complete messages
        private const bool VerboseSuppressionLogs = false; // Toggle to log activity suppression and suppressed notifies
        #region Fields and Properties
        
        // Networking references (will be set by patches)
        private static object? _netMultiClient;
        private static object? _serverConnection;
        private static object? _netPeer;
        private static object? _clientNetworkService;
        // State tracking
        private static bool _isInitialized = false;
        private static float _lastStatusCheck = 0f;
        private static float _lastHeartbeat = 0f;
        private static float _lastAntiIdleAction = 0f;
        // private static float _lastSyntheticKeepAlive = 0f;
        private static float _nextInputNotifyAt = 0f;
        private static bool _loggedNoSendMethod;
        
        // Cache last observed delivery + channel from real traffic
        private static object? _lastDeliveryMethodObj;
        private static int _lastSequenceChannel = 0;
        private static float _lastDeliveryCaptureTime = 0f;
#pragma warning disable CS0414 // Naming Styles
        private static bool _isSendingSyntheticKeepalive = false; // TODO: Remove this
#pragma warning restore CS0414 // Naming Styles

        // Wrapper traffic observation
        private static float _lastWrapperSendTime = 0f;
        private static float _lastWrapperReceiveTime = 0f;
        private static readonly Dictionary<string, int> _wrapperKeyCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        private static float _lastWrapperKeySummaryTime = 0f;
        
        // Configuration
        private const float STATUS_CHECK_INTERVAL = 5f;    // Check connection status every 5 seconds
        private const float HEARTBEAT_INTERVAL = 30f;      // Send heartbeat every 30 seconds
        // Status tracking
        private static string? _lastConnectionStatus;
        private static int _consecutiveIdleDetections = 0;
        private static readonly List<string> _recentDisconnectReasons = new List<string>();
        private static double _lastHeartbeatValue = double.NaN;
        private static bool _isConnected = false;

        // Activity suppression
        private static float _suppressSyntheticUntil = 0f;
        private static float _lastInputCheck = 0f;
        private const float INPUT_CHECK_INTERVAL = 0.25f; // 4x per second
        private static Vector3 _lastMousePos;
        private static bool _lastMousePosInitialized = false;
        private static string _lastSuppressionReason = "unknown"; // TODO: Remove this
        
        #endregion
        
        #region Public Interface
        
        /// <summary>
        /// Returns true when only the Simple Anti-Idle (server input notify) mode is active.
        /// </summary>
        private static bool IsSimpleOnly => Settings.useSimpleAntiIdle && !Settings.useAntiIdle;
        
        /// <summary>
        /// Called from the mod's OnUpdate to perform anti-idle checks and actions
        /// </summary>
        public static void OnUpdate()
        {
            // Check if anti-idle is enabled
            if (!Settings.useAntiIdle && !Settings.useSimpleAntiIdle)
                return;
                
            if (!_isInitialized)
            {
                Initialize();
                return;
            }
            
            try
            {
                // Lightweight input/activity detection (suppresses synthetic keepalive only)
                DetectUserActivity();

                // Simple Anti-Idle UI pulse/notify
                if (Settings.useSimpleAntiIdle)
                {
                    if (_nextInputNotifyAt <= 0f)
                        ScheduleNextInputNotify();

                    if (Time.time >= _nextInputNotifyAt)
                    {
                        if (IsInputNotifyAllowed())
                        {
                            TryServerInputNotify();
                            ScheduleNextInputNotify();
                        }
                        else
                        {
                            // If suppressed, push next attempt beyond suppression window; otherwise short backoff
                            float remaining = GetSuppressionSecondsRemaining();
                            if (remaining > 0f)
                            {
                                if (VerboseSuppressionLogs)
#pragma warning disable CS0162 // Unreachable code detected
                                    MelonLogger.Msg($"[AntiIdle] Input notify suppressed ({_lastSuppressionReason}), remaining {remaining:F0}s");
#pragma warning restore CS0162 // Unreachable code detected
                                _nextInputNotifyAt = Time.time + Mathf.Max(5f, remaining + UnityEngine.Random.Range(1f, 3f));
                            }
                            else
                            {
                                // Not allowed for another reason (feature disabled mid-flight); back off a bit
                                _nextInputNotifyAt = Time.time + 15f;
                            }
                        }
                    }
                }

                // Skip legacy heartbeat/status/action when in Simple-only mode
                if (!IsSimpleOnly)
                {
                    // Check connection status periodically
                    if (Time.time - _lastStatusCheck >= STATUS_CHECK_INTERVAL)
                    {
                        CheckConnectionStatus();
                        _lastStatusCheck = Time.time;
                    }
                    
                    // Quiet heartbeat probe (local timer reset only)
                    if (Time.time - _lastHeartbeat >= HEARTBEAT_INTERVAL)
                    {
                        SendHeartbeat();
                        _lastHeartbeat = Time.time;
                    }
                    
                    // Perform periodic anti-idle action (legacy heartbeat reset path)
                    if (Settings.useAntiIdle)
                    {
                        var interval = Mathf.Max(Settings.antiIdleInterval, 10f); // Minimum 10 seconds
                        if (Time.time - _lastAntiIdleAction >= interval)
                        {
                            PerformAntiIdleAction();
                            _lastAntiIdleAction = Time.time;
                        }
                    }
                }

                // Legacy synthetic keepalive SENDER disabled.
                // The suppression window and read-only observation hooks remain for future research.
                // if (Settings.useSyntheticKeepAlive && !Settings.useSimpleAntiIdle) { /* disabled */ }

                // Periodic wrapper key usage summary (every ~60s)
                if (Time.time - _lastWrapperKeySummaryTime >= 60f && _wrapperKeyCounts.Count > 0)
                {
                    _lastWrapperKeySummaryTime = Time.time;
                    try
                    {
                        string? topKey = null;
                        int topCount = 0;
                        foreach (var kv in _wrapperKeyCounts)
                        {
                            if (kv.Value > topCount)
                            {
                                topKey = kv.Key;
                                topCount = kv.Value;
                            }
                        }

                        if (VerboseKeepaliveLogs && topKey != null)
#pragma warning disable CS0162 // Unreachable code detected
                            MelonLogger.Msg($"[AntiIdle] Wrapper traffic: top={topKey} count={topCount} (window ~60s)");
#pragma warning restore CS0162 // Unreachable code detected

                        _wrapperKeyCounts.Clear();
                    }
                    catch { }
                }
            }
            catch (Exception e)
            {
                MelonLogger.Error($"AntiIdleSystem.OnUpdate error: {e.Message}");
            }
        }
        
        /// <summary>
        /// Called by the mod on scene change to suppress keepalives for a short period
        /// </summary>
        public static void OnSceneChanged()
        {
            try
            {
                RegisterActivity(Settings.sceneChangeSuppressionSeconds, "scene-change");
            }
            catch (Exception e)
            {
                MelonLogger.Error($"AntiIdleSystem.OnSceneChanged error: {e.Message}");
            }
        }
        
        /// <summary>
        /// Called by wrapper when a MessageBuffer is sent
        /// </summary>
        public static void OnWrapperBufferSent(object messageKey, object method, int channel)
        {
            try
            {
                string keyName = messageKey?.ToString() ?? "<null>";
                string methodName = method?.ToString() ?? "<null>";
                string composite = $"key={keyName}|method={methodName}|ch={channel}";
                if (_wrapperKeyCounts.TryGetValue(composite, out var count))
                    _wrapperKeyCounts[composite] = count + 1;
                else
                    _wrapperKeyCounts[composite] = 1;
                _lastWrapperSendTime = Time.time;
            }
            catch (Exception e)
            {
                MelonLogger.Error($"AntiIdleSystem.OnWrapperBufferSent error: {e.Message}");
            }
        }
        
        /// <summary>
        /// Called by patches when connection status changes
        /// </summary>
        public static void OnConnectionStatusChanged(object? status)
        {
            try
            {
                if (IsSimpleOnly)
                {
                    // In Simple-only mode we do not engage legacy status-based actions
                    var statusStringSimple = status?.ToString() ?? "Unknown";
                    _lastConnectionStatus = statusStringSimple;
                    _isConnected = string.Equals(statusStringSimple, "Connected", StringComparison.OrdinalIgnoreCase);
                    return;
                }

                var statusString = status?.ToString() ?? "Unknown";
                
                // Only log if status actually changed
                if (_lastConnectionStatus != statusString)
                {
                    if (VerboseStatusLogs)
#pragma warning disable CS0162 // Unreachable code detected
                        MelonLogger.Msg($"[AntiIdle] Connection status changed from '{_lastConnectionStatus ?? "None"}' to '{statusString}'");
#pragma warning restore CS0162 // Unreachable code detected
                    _lastConnectionStatus = statusString;
                    _isConnected = string.Equals(statusString, "Connected", StringComparison.OrdinalIgnoreCase);
                    
                    // Also emit a one-time snapshot of key objects on status change only
                    if (VerboseStatusLogs)
                    {
#pragma warning disable CS0162 // Unreachable code detected
                        if (_netMultiClient != null)
                        {
                            var clientType = _netMultiClient.GetType();
                            MelonLogger.Msg($"[AntiIdle] NetMultiClient: {clientType.Name}");
                        }
                        else
                        {
                            MelonLogger.Msg("[AntiIdle] NetMultiClient: null");
                        }
#pragma warning restore CS0162 // Unreachable code detected
                    }

                    if (VerboseStatusLogs)
                    {
#pragma warning disable CS0162 // Unreachable code detected
                        if (_serverConnection != null)
                        {
                            var connType = _serverConnection.GetType();
                            MelonLogger.Msg($"[AntiIdle] ServerConnection: {connType.Name}");
                        }
                        else
                        {
                            MelonLogger.Msg("[AntiIdle] ServerConnection: null");
                        }
#pragma warning restore CS0162 // Unreachable code detected
                    }
                    
                    // Treat status transitions as activity (short suppression)
                    // if (_isConnected)
                    //     RegisterActivity(Mathf.Min(Settings.networkActivitySuppressionSeconds, 30f), "status-change");
                    
                    // Check if this is a concerning status
                    if (IsConcerningStatus(statusString))
                    {
                        _consecutiveIdleDetections++;
                        MelonLogger.Warning($"[AntiIdle] Concerning connection status detected: {statusString} (Count: {_consecutiveIdleDetections})");
                        
                        // Trigger immediate anti-idle action
                        PerformAntiIdleAction();
                    }
                    else
                    {
                        // Reset counter if status is good
                        _consecutiveIdleDetections = 0;
                    }
                }
            }
            catch (Exception e)
            {
                MelonLogger.Error($"AntiIdleSystem.OnConnectionStatusChanged error: {e.Message}");
            }
        }
        
        /// <summary>
        /// Called by patches when a disconnect is attempted
        /// </summary>
        public static void OnDisconnectAttempted(string? reason)
        {
            try
            {
                if (IsSimpleOnly)
                    return;

                MelonLogger.Warning($"[AntiIdle] Disconnect attempted - Reason: {reason ?? "No reason provided"}");
                
                // Track recent disconnect reasons for analysis
                _recentDisconnectReasons.Add($"{DateTime.Now:HH:mm:ss} - {reason ?? "Unknown"}");
                
                // Keep only last 10 reasons
                if (_recentDisconnectReasons.Count > 10)
                {
                    _recentDisconnectReasons.RemoveAt(0);
                }
                
                // If this looks like an idle timeout, trigger immediate action
                if (IsIdleTimeoutReason(reason))
                {
                    MelonLogger.Warning("[AntiIdle] Idle timeout detected! Triggering immediate anti-idle action.");
                    PerformAntiIdleAction();
                }
            }
            catch (Exception e)
            {
                MelonLogger.Error($"AntiIdleSystem.OnDisconnectAttempted error: {e.Message}");
            }
        }
        
        /// <summary>
        /// Called by patches when a message is sent
        /// </summary>
        public static void OnMessageSent(object message, object deliveryMethod, int sequenceChannel)
        {
            try
            {
                // Reset idle detection counter when we send messages
                if (_consecutiveIdleDetections > 0)
                {
                    MelonLogger.Msg($"[AntiIdle] Message sent - resetting idle detection counter");
                    _consecutiveIdleDetections = 0;
                }

                // Network traffic implies activity; suppress synthetic keepalive briefly
                // if (!_isSendingSyntheticKeepalive)
                //     RegisterActivity(Settings.networkActivitySuppressionSeconds, "network-send");

                // Capture delivery + channel to reuse for synthetic keepalive
                _lastDeliveryMethodObj = deliveryMethod;
                _lastSequenceChannel = sequenceChannel;
                _lastDeliveryCaptureTime = Time.time;

                // Timestamp wrapper send
                _lastWrapperSendTime = Time.time;
            }
            catch (Exception e)
            {
                MelonLogger.Error($"AntiIdleSystem.OnMessageSent error: {e.Message}");
            }
        }
        
        /// <summary>
        /// Called by wrapper when a network message is received.
        /// </summary>
        public static void OnWrapperReceive(object incomingMessage)
        {
            try
            {
                _lastWrapperReceiveTime = Time.time;
            }
            catch (Exception e)
            {
                MelonLogger.Error($"AntiIdleSystem.OnWrapperReceive error: {e.Message}");
            }
        }
        
        /// <summary>
        /// Called by patches when NetConnection status changes
        /// </summary>
        public static void OnNetConnectionStatusChanged(object connection, object status)
        {
            try
            {
                var statusString = status?.ToString() ?? "Unknown";
                if (VerboseStatusLogs)
#pragma warning disable CS0162 // Unreachable code detected
                    MelonLogger.Msg($"[AntiIdle] NetConnection status: {statusString}");
#pragma warning restore CS0162 // Unreachable code detected
                
                // Handle the same way as main connection status
                OnConnectionStatusChanged(status);
            }
            catch (Exception e)
            {
                MelonLogger.Error($"AntiIdleSystem.OnNetConnectionStatusChanged error: {e.Message}");
            }
        }
        
        /// <summary>
        /// Called by patches when Steam connection status changes
        /// </summary>
        public static void OnSteamConnectionStatusChanged(object data)
        {
            try
            {
                MelonLogger.Msg("[AntiIdle] Steam connection status changed");
                // TODO: Parse Steam networking data if needed
            }
            catch (Exception e)
            {
                MelonLogger.Error($"AntiIdleSystem.OnSteamConnectionStatusChanged error: {e.Message}");
            }
        }
        
        /// <summary>
        /// Called when the in-game settings menu is opened to register user presence.
        /// </summary>
        public static void OnMenuOpened()
        {
            try
            {
                if (!Settings.suppressKeepAliveOnActivity)
                    return;
                RegisterActivity(Settings.activitySuppressionSeconds, "menu");
            }
            catch (Exception e)
            {
                MelonLogger.Error($"AntiIdleSystem.OnMenuOpened error: {e.Message}");
            }
        }
        
        /// <summary>
        /// Called when the in-game settings menu is closed to register user presence.
        /// </summary>
        public static void OnMenuClosed()
        {
            try
            {
                if (!Settings.suppressKeepAliveOnActivity)
                    return;
                RegisterActivity(Settings.activitySuppressionSeconds, "menu");
            }
            catch (Exception e)
            {
                MelonLogger.Error($"AntiIdleSystem.OnMenuClosed error: {e.Message}");
            }
        }
        
        #endregion
        
        #region Patch Integration Methods
        
        /// <summary>
        /// Set the NetMultiClient instance (called by patches)
        /// </summary>
        public static void SetNetMultiClient(object netMultiClient)
        {
            try
            {
                if (_netMultiClient != netMultiClient)
                {
                    _netMultiClient = netMultiClient;
                    MelonLogger.Msg("[AntiIdle] NetMultiClient instance set");
                }
            }
            catch (Exception e)
            {
                MelonLogger.Error($"AntiIdleSystem.SetNetMultiClient error: {e.Message}");
            }
        }
        
        /// <summary>
        /// Set the server connection (called by patches)
        /// </summary>
        public static void SetServerConnection(object connection)
        {
            try
            {
                if (_serverConnection != connection)
                {
                    _serverConnection = connection;
                    MelonLogger.Msg("[AntiIdle] Server connection set");
                }
            }
            catch (Exception e)
            {
                MelonLogger.Error($"AntiIdleSystem.SetServerConnection error: {e.Message}");
            }
        }
        
        /// <summary>
        /// Set the ClientNetworkService wrapper (called by patches)
        /// </summary>
        public static void SetClientNetworkService(object service)
        {
            try
            {
                if (_clientNetworkService != service)
                {
                    _clientNetworkService = service;
                    MelonLogger.Msg("[AntiIdle] ClientNetworkService set");
                }
            }
            catch (Exception e)
            {
                MelonLogger.Error($"AntiIdleSystem.SetClientNetworkService error: {e.Message}");
            }
        }

        /// <summary>
        /// Clear stored connections (called by patches)
        /// </summary>
        public static void ClearConnections()
        {
            try
            {
                _netMultiClient = null;
                _serverConnection = null;
                _lastConnectionStatus = null;
                _consecutiveIdleDetections = 0;
                MelonLogger.Msg("[AntiIdle] Connections cleared");
            }
            catch (Exception e)
            {
                MelonLogger.Error($"AntiIdleSystem.ClearConnections error: {e.Message}");
            }
        }
        
        #endregion
        
        #region Private Implementation
        
        private static void Initialize()
        {
            try
            {
                MelonLogger.Msg("[AntiIdle] Initializing Anti-Idle System");
                _isInitialized = true;
                _lastStatusCheck = Time.time;
                _lastHeartbeat = Time.time;
                _lastAntiIdleAction = Time.time;
                _lastMousePosInitialized = false;
                if (Settings.useSimpleAntiIdle)
                {
                    _nextInputNotifyAt = Time.time + UnityEngine.Random.Range(25f, 45f);
                    MelonLogger.Msg("[AntiIdle] Server input notify scheduled shortly (~25-45s)");
                }
            }
            catch (Exception e)
            {
                MelonLogger.Error($"AntiIdleSystem.Initialize error: {e.Message}");
            }
        }
        
        // Helper: try to get Lidgren NetTime.Now; fallback to Unity time
        private static double GetNetworkNowSeconds()
        {
            try
            {
                var asm = _netMultiClient?.GetType().Assembly;
                if (asm == null)
                {
                    foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        try 
                        { 
                            var asmName = a.GetName()?.Name; 
                            if (!string.IsNullOrEmpty(asmName) && asmName.Contains("Lidgren", StringComparison.OrdinalIgnoreCase)) 
                            { 
                                asm = a; 
                                break; 
                            } 
                        }
                        catch { }
                    }
                }
                var netTimeType = asm?.GetType("Il2CppLidgren.Network.NetTime") ?? asm?.GetType("Lidgren.Network.NetTime");
                if (netTimeType != null)
                {
                    var nowProp = netTimeType.GetProperty("Now", BindingFlags.Public | BindingFlags.Static);
                    if (nowProp != null)
                    {
                        var val = nowProp.GetValue(null);
                        return Convert.ToDouble(val);
                    }
                    var getNow = netTimeType.GetMethod("get_Now", BindingFlags.Public | BindingFlags.Static);
                    if (getNow != null)
                    {
                        var val = getNow.Invoke(null, null);
                        return Convert.ToDouble(val);
                    }
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Msg($"[AntiIdle] NetTime.Now unavailable ({ex.Message}) - using Unity time");
            }
            try { return (double)Time.realtimeSinceStartup; } catch { }
            return DateTime.UtcNow.TimeOfDay.TotalSeconds;
        }
        
        private static void CheckConnectionStatus()
        {
            try
            {
                if (IsSimpleOnly)
                    return;
                // Lightweight periodic check
                
                // Parse networking state during status check
                ParseNetworkingState();
                
                // If we have too many consecutive idle detections, force an action
                if (_consecutiveIdleDetections >= 3)
                {
                    MelonLogger.Warning($"[AntiIdle] Multiple idle detections ({_consecutiveIdleDetections}) - forcing anti-idle action");
                    PerformAntiIdleAction();
                    _consecutiveIdleDetections = 0; // Reset after action
                }
            }
            catch (Exception e)
            {
                MelonLogger.Error($"AntiIdleSystem.CheckConnectionStatus error: {e.Message}");
            }
        }
        
        private static void SendHeartbeat()
        {
            try
            {
                if (IsSimpleOnly)
                    return;
                // Quiet heartbeat call
                
                // Try to reset the heartbeat timer
                ResetHeartbeatTimer();
            }
            catch (Exception e)
            {
                MelonLogger.Error($"AntiIdleSystem.SendHeartbeat error: {e.Message}");
            }
        }
        
        private static void PerformAntiIdleAction()
        {
            try
            {
                if (!Settings.useAntiIdle)
                    return;

                if (VerboseActionLogs)
#pragma warning disable CS0162 // Unreachable code detected
                    MelonLogger.Msg("[AntiIdle] Action: heartbeat reset + status snapshot");
#pragma warning restore CS0162 // Unreachable code detected

                // Try to reset the heartbeat timer by calling the game's internal heartbeat method
                ResetHeartbeatTimer();
                
                // Brief networking snapshot
                ParseNetworkingState();
                if (VerboseActionLogs)
#pragma warning disable CS0162 // Unreachable code detected
                    MelonLogger.Msg("[AntiIdle] Action complete");
#pragma warning restore CS0162 // Unreachable code detected
            }
            catch (Exception e)
            {
                MelonLogger.Error($"AntiIdleSystem.PerformAntiIdleAction error: {e.Message}");
            }
        }
        
        private static bool IsConcerningStatus(string status)
        {
            if (string.IsNullOrEmpty(status)) return false;
            
            var statusLower = status.ToLowerInvariant();
            
            // Check for concerning status keywords
            return statusLower.Contains("idle") ||
                   statusLower.Contains("disconnect") ||
                   statusLower.Contains("timeout") ||
                   statusLower.Contains("inactive") ||
                   statusLower.Contains("dropped");
        }
        
        private static bool IsIdleTimeoutReason(string? reason)
        {
            if (string.IsNullOrEmpty(reason)) return false;
            
            var reasonLower = reason.ToLowerInvariant();
            
            // Check for idle timeout keywords in disconnect reasons
            return reasonLower.Contains("idle") ||
                   reasonLower.Contains("timeout") ||
                   reasonLower.Contains("inactive") ||
                   reasonLower.Contains("no activity") ||
                   reasonLower.Contains("afk");
        }
        
        #endregion
        
        #region Debug and Logging
        
        /// <summary>
        /// Get debug information about the anti-idle system
        /// </summary>
        public static string GetDebugInfo()
        {
            return $"[AntiIdle] Status: Initialized={_isInitialized}, " +
                   $"NetMultiClient={_netMultiClient != null}, " +
                   $"ServerConnection={_serverConnection != null}, " +
                   $"ClientNetworkService={_clientNetworkService != null}, " +
                   $"LastStatus={_lastConnectionStatus ?? "None"}, " +
                   $"IdleDetections={_consecutiveIdleDetections}, " +
                   $"RecentDisconnects={_recentDisconnectReasons.Count}, " +
                   $"Suppressed={(Settings.suppressKeepAliveOnActivity && Time.time < _suppressSyntheticUntil)}, " +
                   $"LastWrapperSend={(Time.time - _lastWrapperSendTime):F1}s ago, LastWrapperRecv={(Time.time - _lastWrapperReceiveTime):F1}s ago";
        }
        
        #endregion
        
        #region Simple Input Notify

        private static void ScheduleNextInputNotify()
        {
            // Tie to the same interval but allow independent jitter; minimum 30s
            var baseInterval = Mathf.Max(Settings.simpleAntiIdleInterval, 60f);
            var jitter = UnityEngine.Random.Range(-8f, 8f);
            _nextInputNotifyAt = Time.time + Mathf.Max(30f, baseInterval * 0.75f + jitter);
            MelonLogger.Msg($"[AntiIdle] Next server input notify in {(_nextInputNotifyAt - Time.time):F0}s");
        }

        private static bool IsInputNotifyAllowed()
        {
            if (!Settings.useSimpleAntiIdle) return false;
            if (Settings.suppressKeepAliveOnActivity && Time.time < _suppressSyntheticUntil) return false;
            return true;
        }

        private static float GetSuppressionSecondsRemaining()
        {
            if (!Settings.suppressKeepAliveOnActivity) return 0f;
            return Mathf.Max(0f, _suppressSyntheticUntil - Time.time);
        }

        private static void TryServerInputNotify()
        {
            try
            {
                if (EpochInputManagerBridge.TrySendInputActionPerformed())
                {
                    MelonLogger.Msg("[AntiIdle] Server input notify invoked via EpochInputManager");
                    return;
                }

                if (!_loggedNoSendMethod)
                {
                    _loggedNoSendMethod = true;
                    MelonLogger.Warning("[AntiIdle] Input notify method not found (EpochInputManager)");
                }
            }
            catch (Exception e)
            {
                MelonLogger.Error($"[AntiIdle] ServerInputNotify error: {e.Message}");
            }
        }

        #endregion

        #region Heartbeat Management

        // Cached reflection bindings for heartbeat reset (resolved once per client type)
        private static bool _hbCacheResolved;
        private static Type? _hbCacheForType;
        private static FieldInfo? _hbField;           // m_lastHeartbeat on client
        private static PropertyInfo? _hbProp;          // m_lastHeartbeat property on client
        private static FieldInfo? _hbAltField;         // any double field containing "heartbeat"
        private static FieldInfo? _hbNetPeerField;     // field on client whose type contains "NetPeer"
        private static FieldInfo? _hbNpField;          // m_lastHeartbeat on NetPeer
        private static PropertyInfo? _hbNpProp;        // m_lastHeartbeat property on NetPeer
        private static MethodInfo[]? _hbMethods;       // heartbeat/keepalive/ping methods
        private static MethodInfo[]? _hbSetters;       // setter methods with heartbeat in name

        private static double ReadAsDouble(object? value)
        {
            if (value == null) return double.NaN;
            try { return Convert.ToDouble(value); } catch { return double.NaN; }
        }

        private static FieldInfo? FindFieldRecursive(Type type, string name)
        {
            for (var t = type; t != null; t = t.BaseType!)
            {
                var fi = t.GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                if (fi != null) return fi;
            }
            return null;
        }

        private static List<FieldInfo> GetAllInstanceFields(Type type)
        {
            var result = new List<FieldInfo>();
            for (var t = type; t != null; t = t.BaseType!)
            {
                foreach (var f in t.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                    result.Add(f);
            }
            return result;
        }

        private static void ResolveHeartbeatBindings(Type clientType)
        {
            if (_hbCacheResolved && _hbCacheForType == clientType) return;
            _hbCacheResolved = true;
            _hbCacheForType = clientType;

            _hbField = FindFieldRecursive(clientType, "m_lastHeartbeat");
            _hbProp = clientType.GetProperty("m_lastHeartbeat", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);

            var allFields = GetAllInstanceFields(clientType);

            _hbAltField = null;
            _hbNetPeerField = null;
            for (int i = 0; i < allFields.Count; i++)
            {
                var f = allFields[i];
                if (_hbAltField == null && f.FieldType == typeof(double) && f.Name.IndexOf("heartbeat", StringComparison.OrdinalIgnoreCase) >= 0)
                    _hbAltField = f;
                if (_hbNetPeerField == null && f.FieldType.Name.Contains("NetPeer"))
                    _hbNetPeerField = f;
            }

            // Resolve NetPeer-level heartbeat bindings if we found the peer field
            _hbNpField = null;
            _hbNpProp = null;
            if (_hbNetPeerField != null)
            {
                var peerType = _hbNetPeerField.FieldType;
                _hbNpField = FindFieldRecursive(peerType, "m_lastHeartbeat");
                _hbNpProp = peerType.GetProperty("m_lastHeartbeat", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
            }

            // Resolve heartbeat/keepalive methods
            var methods = clientType.GetMethods();
            var hbMethodList = new List<MethodInfo>();
            var setterList = new List<MethodInfo>();
            for (int i = 0; i < methods.Length; i++)
            {
                var m = methods[i];
                var nameLower = m.Name.ToLowerInvariant();
                if ((nameLower.Contains("heartbeat") || nameLower.Contains("keepalive") || nameLower.Contains("ping"))
                    && !nameLower.StartsWith("get_"))
                {
                    hbMethodList.Add(m);
                }
                if (nameLower.Contains("set") && nameLower.Contains("heartbeat"))
                {
                    setterList.Add(m);
                }
            }
            _hbMethods = hbMethodList.Count > 0 ? hbMethodList.ToArray() : null;
            _hbSetters = setterList.Count > 0 ? setterList.ToArray() : null;
        }

        private static void ResetHeartbeatTimer()
        {
            try
            {
                if (_netMultiClient == null) return;

                var clientType = _netMultiClient.GetType();
                ResolveHeartbeatBindings(clientType);

                bool resetSucceeded = false;
                var nowSeconds = GetNetworkNowSeconds();

                // Try direct field
                if (_hbField != null)
                {
                    try
                    {
                        _hbField.SetValue(_netMultiClient, nowSeconds);
                        var after = ReadAsDouble(_hbField.GetValue(_netMultiClient));
                        resetSucceeded = !double.IsNaN(after) && Math.Abs(after - nowSeconds) <= 1.5;
                    }
                    catch { }
                }

                // Try direct property
                if (!resetSucceeded && _hbProp != null && _hbProp.CanWrite)
                {
                    try
                    {
                        _hbProp.SetValue(_netMultiClient, nowSeconds);
                        var after = ReadAsDouble(_hbProp.GetValue(_netMultiClient));
                        resetSucceeded = !double.IsNaN(after) && Math.Abs(after - nowSeconds) <= 1.5;
                    }
                    catch { }
                }

                // Try alternative heartbeat field
                if (!resetSucceeded && _hbAltField != null)
                {
                    try
                    {
                        _hbAltField.SetValue(_netMultiClient, nowSeconds);
                        var after = ReadAsDouble(_hbAltField.GetValue(_netMultiClient));
                        resetSucceeded = !double.IsNaN(after) && Math.Abs(after - nowSeconds) <= 1.5;
                    }
                    catch { }
                }

                // Try NetPeer heartbeat
                if (!resetSucceeded && _hbNetPeerField != null)
                {
                    try
                    {
                        var netPeerInstance = _hbNetPeerField.GetValue(_netMultiClient);
                        if (netPeerInstance != null)
                        {
                            SetNetPeer(netPeerInstance);
                            if (_hbNpField != null)
                            {
                                _hbNpField.SetValue(netPeerInstance, nowSeconds);
                                var after = ReadAsDouble(_hbNpField.GetValue(netPeerInstance));
                                resetSucceeded = !double.IsNaN(after) && Math.Abs(after - nowSeconds) <= 1.5;
                            }
                            else if (_hbNpProp != null && _hbNpProp.CanWrite)
                            {
                                _hbNpProp.SetValue(netPeerInstance, nowSeconds);
                                var after = ReadAsDouble(_hbNpProp.GetValue(netPeerInstance));
                                resetSucceeded = !double.IsNaN(after) && Math.Abs(after - nowSeconds) <= 1.5;
                            }
                        }
                    }
                    catch { }
                }

                // Try heartbeat/keepalive/ping methods
                if (!resetSucceeded && _hbMethods != null)
                {
                    for (int i = 0; i < _hbMethods.Length; i++)
                    {
                        try
                        {
                            _hbMethods[i].Invoke(_netMultiClient, null);
                            resetSucceeded = true;
                            break;
                        }
                        catch { }
                    }
                }

                // Try setter methods with heartbeat in name
                if (!resetSucceeded && _hbSetters != null)
                {
                    for (int i = 0; i < _hbSetters.Length; i++)
                    {
                        try
                        {
                            var ps = _hbSetters[i].GetParameters();
                            if (ps.Length == 1 && ps[0].ParameterType == typeof(double))
                            {
                                _hbSetters[i].Invoke(_netMultiClient, new object[] { nowSeconds });
                                resetSucceeded = true;
                                break;
                            }
                            else if (ps.Length == 1 && ps[0].ParameterType == typeof(float))
                            {
                                _hbSetters[i].Invoke(_netMultiClient, new object[] { (float)nowSeconds });
                                resetSucceeded = true;
                                break;
                            }
                            else if (ps.Length == 0)
                            {
                                _hbSetters[i].Invoke(_netMultiClient, null);
                                resetSucceeded = true;
                                break;
                            }
                        }
                        catch { }
                    }
                }

                TryResetTimeoutOnConnection(nowSeconds);
            }
            catch (Exception e)
            {
                MelonLogger.Error($"AntiIdleSystem.ResetHeartbeatTimer error: {e.Message}");
            }
        }

        private static void TryResetTimeoutOnConnection(double nowSeconds)
        {
            try
            {
                if (_serverConnection == null) return;
                var connType = _serverConnection.GetType();

                // Extend any timeout deadline fields we can find
                foreach (var f in connType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
                {
                    var name = f.Name.ToLowerInvariant();
                    if (f.FieldType == typeof(double) || f.FieldType == typeof(float))
                    {
                        // timeout-like deadlines: push out by 300s
                        if (name.Contains("timeout") || name.Contains("deadline") || name.Contains("idle"))
                        {
                            try
                            {
                                var before = Convert.ToDouble(f.GetValue(_serverConnection));
                                var target = nowSeconds + 300.0;
                                if (f.FieldType == typeof(double)) f.SetValue(_serverConnection, target);
                                else f.SetValue(_serverConnection, (float)target);
                                var after = Convert.ToDouble(f.GetValue(_serverConnection));
                                // Quiet log to avoid spam; only log significant changes
                                if (Math.Abs(after - before) > 1.0)
                                    MelonLogger.Msg($"[AntiIdle] Timeout field '{f.Name}' adjusted: {before:F1} -> {after:F1}");
                            }
                            catch { }
                        }

                        // last-heard/last-activity markers: set to now
                        if (name.Contains("last") && (name.Contains("heard") || name.Contains("receive") || name.Contains("activity") || name.Contains("seen")))
                        {
                            try
                            {
                                if (f.FieldType == typeof(double)) f.SetValue(_serverConnection, nowSeconds);
                                else f.SetValue(_serverConnection, (float)nowSeconds);
                            }
                            catch { }
                        }
                    }
                }

                // Try common properties too
                foreach (var p in connType.GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
                {
                    var name = p.Name.ToLowerInvariant();
                    if (!p.CanWrite) continue;
                    var pt = p.PropertyType;
                    if (pt == typeof(double) || pt == typeof(float))
                    {
                        try
                        {
                            if (name.Contains("timeout") || name.Contains("deadline") || name.Contains("idle"))
                            {
                                var target = nowSeconds + 300.0;
                                if (pt == typeof(double)) p.SetValue(_serverConnection, target);
                                else p.SetValue(_serverConnection, (float)target);
                            }
                            if (name.Contains("last") && (name.Contains("heard") || name.Contains("receive") || name.Contains("activity") || name.Contains("seen")))
                            {
                                if (pt == typeof(double)) p.SetValue(_serverConnection, nowSeconds);
                                else p.SetValue(_serverConnection, (float)nowSeconds);
                            }
                        }
                        catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"AntiIdleSystem.TryResetTimeoutOnConnection error: {ex.Message}");
            }
        }

        private static void SendSyntheticKeepAlive()
        {
            try
            {
                if (_netMultiClient == null || !_isConnected) return;
                var clientType = _netMultiClient.GetType();

                // Create an empty user message via CreateMessage if available
                object? msg = null;
                var createMsgNoArg = clientType.GetMethod("CreateMessage", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
                var createMsgInt = clientType.GetMethod("CreateMessage", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(int) }, null);
                if (createMsgNoArg != null)
                    msg = createMsgNoArg.Invoke(_netMultiClient, null);
                else if (createMsgInt != null)
                    msg = createMsgInt.Invoke(_netMultiClient, new object[] { 1 }); // reserve 1 byte

                // Try via NetPeer if client couldn't create message
                if (msg == null)
                {
                    var peerField = clientType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                        .FirstOrDefault(f => f.FieldType.Name.Contains("NetPeer"));
                    var peer = peerField?.GetValue(_netMultiClient);
                    if (peer != null)
                    {
                        var peerType = peer.GetType();
                        var pCreateMsgNoArg = peerType.GetMethod("CreateMessage", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
                        var pCreateMsgInt = peerType.GetMethod("CreateMessage", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(int) }, null);
                        if (pCreateMsgNoArg != null) msg = pCreateMsgNoArg.Invoke(peer, null);
                        else if (pCreateMsgInt != null) msg = pCreateMsgInt.Invoke(peer, new object[] { 1 });
                    }
                }

                if (msg == null) return;

                // Write a tiny payload to avoid empty-message drops (best-effort)
                try
                {
                    var writeByte = msg.GetType().GetMethod("Write", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(byte) }, null);
                    writeByte?.Invoke(msg, new object[] { (byte)0 });
                }
                catch { }

                bool sent = false;
                string? lastError = null;

                // Preferred delivery: use last observed if compatible; else ReliableUnordered; fallback to Unreliable
                object? deliveryPreferred = _lastDeliveryMethodObj;
                object? deliveryReliable = null;
                object? deliveryUnreliable = null;
                try
                {
                    var deliveryEnum = clientType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                        .FirstOrDefault(m => m.Name == "SendMessage" && m.GetParameters().Length == 3)?.GetParameters()[1].ParameterType;
                    if (deliveryEnum != null && deliveryEnum.IsEnum)
                    {
                        foreach (var name in Enum.GetNames(deliveryEnum))
                        {
                            if (name.Equals("ReliableUnordered", StringComparison.OrdinalIgnoreCase)) deliveryReliable = Enum.Parse(deliveryEnum, name);
                            if (name.Equals("Unreliable", StringComparison.OrdinalIgnoreCase)) deliveryUnreliable = Enum.Parse(deliveryEnum, name);
                        }
                        if (deliveryPreferred != null && deliveryPreferred.GetType() != deliveryEnum)
                        {
                            // Incompatible cached enum: ignore
                            deliveryPreferred = null;
                        }
                    }
                }
                catch { }

                // Try direct NetConnection.SendMessage first, if available
                try
                {
                    _isSendingSyntheticKeepalive = true;
                    if (_serverConnection != null)
                    {
                        var connType = _serverConnection.GetType();
                        var connSend = connType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                            .FirstOrDefault(m => m.Name == "SendMessage" && m.GetParameters().Length == 3);
                        if (connSend != null)
                        {
                            var ps = connSend.GetParameters();
                            var deliveryType = ps[1].ParameterType;
                            object delivery = (deliveryPreferred != null && deliveryPreferred.GetType() == deliveryType)
                                ? deliveryPreferred
                                : (deliveryReliable ?? (deliveryType.IsEnum ? Enum.Parse(deliveryType, "ReliableUnordered") : (object)0));
                            var channel = (_lastDeliveryCaptureTime > 0f) ? _lastSequenceChannel : 0;
                            var result = connSend.Invoke(_serverConnection, new object[] { msg, delivery, channel });
                            var ok = result?.ToString()?.Contains("Sent", StringComparison.OrdinalIgnoreCase) == true || result?.ToString()?.Contains("Queued", StringComparison.OrdinalIgnoreCase) == true;
                            sent = ok;
                            if (!ok)
                                lastError = $"ConnSend result={result?.ToString() ?? "null"}";
                        }
                    }
                }
                catch (Exception ex)
                {
                    lastError = $"ConnSend error: {ex.Message}";
                }
                finally { _isSendingSyntheticKeepalive = false; }

                // Try 4-arg overload first: SendMessage(msg, connection, method, channel)
                try
                {
                    _isSendingSyntheticKeepalive = true;
                    var send4 = clientType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                        .FirstOrDefault(m => m.Name == "SendMessage" && m.GetParameters().Length == 4 && m.GetParameters()[1].ParameterType.Name.Contains("NetConnection"));
                    if (!sent && send4 != null && _serverConnection != null)
                    {
                        var ps = send4.GetParameters();
                        var channel = (_lastDeliveryCaptureTime > 0f) ? _lastSequenceChannel : 0;
                        var paramDeliveryType = ps[2].ParameterType;
                        var delivery = (deliveryPreferred != null && deliveryPreferred.GetType() == paramDeliveryType)
                            ? deliveryPreferred
                            : (deliveryReliable ?? deliveryUnreliable ?? paramDeliveryType.GetEnumValues().GetValue(0));
                        var result = send4.Invoke(_netMultiClient, new object[] { msg, _serverConnection, delivery!, channel });
                        var ok = result?.ToString()?.Contains("Sent", StringComparison.OrdinalIgnoreCase) == true || result?.ToString()?.Contains("Queued", StringComparison.OrdinalIgnoreCase) == true;
                        sent = ok;
                        if (!ok)
                            lastError = $"Send4 result={result?.ToString() ?? "null"}";
                    }
                }
                catch (Exception ex)
                {
                    lastError = $"Send4 error: {ex.Message}";
                }
                finally { _isSendingSyntheticKeepalive = false; }

                // Try 3-arg overload: SendMessage(msg, method, channel)
                if (!sent)
                {
                    try
                    {
                        _isSendingSyntheticKeepalive = true;
                        var send3 = clientType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                            .FirstOrDefault(m => m.Name == "SendMessage" && m.GetParameters().Length == 3);
                        if (send3 != null)
                        {
                            var ps = send3.GetParameters();
                            var channel = (_lastDeliveryCaptureTime > 0f) ? _lastSequenceChannel : 0;
                            var paramDeliveryType = ps[1].ParameterType;
                            var delivery = (deliveryPreferred != null && deliveryPreferred.GetType() == paramDeliveryType)
                                ? deliveryPreferred
                                : (deliveryReliable ?? deliveryUnreliable ?? paramDeliveryType.GetEnumValues().GetValue(0));
                            var result = send3.Invoke(_netMultiClient, new object[] { msg, delivery!, channel });
                            var ok = result?.ToString()?.Contains("Sent", StringComparison.OrdinalIgnoreCase) == true || result?.ToString()?.Contains("Queued", StringComparison.OrdinalIgnoreCase) == true;
                            sent = ok;
                            if (!ok)
                                lastError = $"Send3 result={result?.ToString() ?? "null"}";
                        }
                    }
                    catch (Exception ex)
                    {
                        lastError = $"Send3 error: {ex.Message}";
                    }
                    finally { _isSendingSyntheticKeepalive = false; }
                }

                // Peer-level send as last resort
                if (!sent)
                {
                    try
                    {
                        _isSendingSyntheticKeepalive = true;
                        var peerField = clientType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                            .FirstOrDefault(f => f.FieldType.Name.Contains("NetPeer"));
                        var peer = peerField?.GetValue(_netMultiClient);
                        if (peer != null)
                        {
                            var peerType = peer.GetType();
                            var sendPeer = peerType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                                .FirstOrDefault(m => m.Name == "SendMessage" && m.GetParameters().Length >= 3);
                            if (sendPeer != null)
                            {
                                var ps = sendPeer.GetParameters();
                                object delivery;
                                if (_lastDeliveryCaptureTime > 0f && _lastDeliveryMethodObj != null && ps[1].ParameterType.IsEnum && _lastDeliveryMethodObj.GetType() == ps[1].ParameterType)
                                    delivery = _lastDeliveryMethodObj;
                                else
                                    delivery = ps[1].ParameterType.IsEnum ? Enum.Parse(ps[1].ParameterType, "ReliableUnordered") : (object)0;
                                int channel = (_lastDeliveryCaptureTime > 0f) ? _lastSequenceChannel : 0;
                                object? result;
                                if (ps.Length == 3)
                                    result = sendPeer.Invoke(peer, new object[] { msg, delivery, channel });
                                else if (ps.Length == 4 && ps[1].ParameterType.Name.Contains("NetConnection") && _serverConnection != null)
                                    result = sendPeer.Invoke(peer, new object[] { msg, _serverConnection, delivery, channel });
                                else
                                    result = sendPeer.Invoke(peer, new object[] { msg, delivery, channel });

                                var ok = result?.ToString()?.Contains("Sent", StringComparison.OrdinalIgnoreCase) == true || result?.ToString()?.Contains("Queued", StringComparison.OrdinalIgnoreCase) == true;
                                sent = ok;
                                if (!ok)
                                    lastError = $"SendPeer result={result?.ToString() ?? "null"}";
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        lastError = $"SendPeer error: {ex.Message}";
                    }
                    finally { _isSendingSyntheticKeepalive = false; }
                }

                // As a final fallback, try invoking Ping/KeepAlive methods on connection or peer
                if (!sent)
                {
                    try
                    {
                        bool invoked = false;
                        if (_serverConnection != null)
                        {
                            var ct = _serverConnection.GetType();
                            var ping = ct.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                                .FirstOrDefault(m => m.Name.IndexOf("ping", StringComparison.OrdinalIgnoreCase) >= 0 || m.Name.IndexOf("keepalive", StringComparison.OrdinalIgnoreCase) >= 0);
                            if (ping != null)
                            {
                                ping.Invoke(_serverConnection, null);
                                invoked = true;
                            }
                        }
                        if (!invoked && _netPeer != null)
                        {
                            var pt = _netPeer.GetType();
                            var ping = pt.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                                .FirstOrDefault(m => m.Name.IndexOf("ping", StringComparison.OrdinalIgnoreCase) >= 0 || m.Name.IndexOf("keepalive", StringComparison.OrdinalIgnoreCase) >= 0);
                            ping?.Invoke(_netPeer, null);
                        }
                    }
                    catch { }
                }

                if (!sent && lastError != null)
                {
                    MelonLogger.Warning($"[AntiIdle] Keepalive send failed: {lastError}");
                }
                else if (sent && VerboseKeepaliveLogs)
                {
                    MelonLogger.Msg("[AntiIdle] Keepalive sent successfully");
                }

                // Also reset heartbeat right after
                ResetHeartbeatTimer();
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"AntiIdleSystem.SendSyntheticKeepAlive error: {ex.Message}");
            }
        }
        
        #endregion
        
        #region State Parsing Methods
        
        private static void ParseNetworkingState()
        {
            try
            {
                // Minimal networking snapshot (quiet): update internal heartbeat tracking only
                if (_netMultiClient != null)
                {
                    var clientType = _netMultiClient.GetType();
                    var lastHeartbeatField = clientType.GetField("m_lastHeartbeat");
                    if (lastHeartbeatField != null)
                    {
                        var lastHeartbeatValue = lastHeartbeatField.GetValue(_netMultiClient);
                        if (lastHeartbeatValue != null)
                        {
                            _lastHeartbeatValue = Convert.ToDouble(lastHeartbeatValue);
                        }
                    }
                }
                // Do not log NetMultiClient/ServerConnection here; only on status change
            }
            catch (Exception e)
            {
                MelonLogger.Error($"AntiIdleSystem.ParseNetworkingState error: {e.Message}");
            }
        }
        
        // UI/Player state logging removed to reduce noise
        
        #region NetPeer Integration
        
        /// <summary>
        /// Called by NetPeer patches to set the NetPeer instance
        /// </summary>
        public static void SetNetPeer(object netPeer)
        {
            try
            {
                _netPeer = netPeer;
            }
            catch (Exception e)
            {
                MelonLogger.Error($"AntiIdleSystem.SetNetPeer error: {e.Message}");
            }
        }
        
        /// <summary>
        /// Called by NetPeer getter patch when heartbeat is read
        /// </summary>
        public static void OnHeartbeatRead(double value)
        {
            try
            {
                // Log significant heartbeat changes
                if (!double.IsNaN(_lastHeartbeatValue) && Math.Abs(value - _lastHeartbeatValue) > 10.0)
                {
                    MelonLogger.Msg($"[AntiIdle] Heartbeat read: {value:F2} (Change: {value - _lastHeartbeatValue:F2})");
                }
                _lastHeartbeatValue = value;
            }
            catch (Exception e)
            {
                MelonLogger.Error($"AntiIdleSystem.OnHeartbeatRead error: {e.Message}");
            }
        }
        
        /// <summary>
        /// Called by NetPeer setter patch when heartbeat is written
        /// </summary>
        public static void OnHeartbeatWrite(double value)
        {
            try
            {
                // Log significant heartbeat writes
                if (!double.IsNaN(_lastHeartbeatValue) && Math.Abs(value - _lastHeartbeatValue) > 10.0)
                {
                    MelonLogger.Msg($"[AntiIdle] Heartbeat written: {value:F2} (Change: {value - _lastHeartbeatValue:F2})");
                }
                _lastHeartbeatValue = value;
            }
            catch (Exception e)
            {
                MelonLogger.Error($"AntiIdleSystem.OnHeartbeatWrite error: {e.Message}");
            }
        }
        
        #endregion

        // Detects user activity cheaply and sets suppression window for synthetic keepalive
        private static void DetectUserActivity()
        {
            try
            {
                if (!Settings.suppressKeepAliveOnActivity) return;
                if (!Application.isFocused) return;

                if (!_lastMousePosInitialized)
                {
                    _lastMousePos = Input.mousePosition;
                    _lastMousePosInitialized = true;
                }

                if (Time.time - _lastInputCheck < INPUT_CHECK_INTERVAL)
                    return;
                _lastInputCheck = Time.time;

                bool activityDetected = false;

                // Any key or mouse button this frame
                if (Input.anyKeyDown)
                    activityDetected = true;
                else if (Input.GetMouseButton(0) || Input.GetMouseButton(1) || Input.GetMouseButton(2))
                    activityDetected = true;
                else
                {
                    // Mouse moved significantly
                    var pos = Input.mousePosition;
                    if ((_lastMousePos - pos).sqrMagnitude > 4f)
                        activityDetected = true;
                    _lastMousePos = pos;
                }

                if (activityDetected)
                {
                    RegisterActivity(Settings.activitySuppressionSeconds, "input");
                }
            }
            catch (Exception e)
            {
                MelonLogger.Error($"AntiIdleSystem.DetectUserActivity error: {e.Message}");
            }
        }

        // Extends suppression window for synthetic keepalive
        private static void RegisterActivity(float seconds, string reason = "activity")
        {
            if (seconds <= 0f) return;
            var until = Time.time + seconds;
            if (until > _suppressSyntheticUntil)
            {
                _suppressSyntheticUntil = until;
                _lastSuppressionReason = reason;
                if (VerboseSuppressionLogs)
                {
#pragma warning disable CS0162 // Unreachable code detected
                    var remaining = _suppressSyntheticUntil - Time.time;
                    MelonLogger.Msg($"[AntiIdle] Suppression registered: reason={reason}, duration={seconds:F0}s, remaining={remaining:F0}s");
#pragma warning restore CS0162 // Unreachable code detected
                }
            }
        }
        
        #endregion
    }
} 
using MelonLoader;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

namespace Mod.Cheats
{
    /// <summary>
    /// Handles anti-idle timeout detection and prevention for Last Epoch
    /// </summary>
    internal static class AntiIdleSystem
    {
        private const bool VerboseHeartbeatLogs = false; // Toggle to re-enable detailed heartbeat write logs
        private const bool VerboseStatusLogs = false; // Toggle to re-enable status/info logs
        #region Fields and Properties
        
        // Networking references (will be set by patches)
        private static object? _netMultiClient;
        private static object? _serverConnection;
        private static object? _netPeer;
        
        // State tracking
        private static bool _isInitialized = false;
        private static float _lastStatusCheck = 0f;
        private static float _lastHeartbeat = 0f;
        private static float _lastAntiIdleAction = 0f;
        private static float _lastSyntheticKeepAlive = 0f;
        
        // Configuration
        private const float STATUS_CHECK_INTERVAL = 5f;    // Check connection status every 5 seconds
        private const float HEARTBEAT_INTERVAL = 30f;      // Send heartbeat every 30 seconds
        private const float ANTI_IDLE_ACTION_INTERVAL = 60f; // Perform anti-idle action every 60 seconds
        private const float SYNTHETIC_KEEPALIVE_MIN_INTERVAL = 15f; // Safety floor
        
        // Status tracking
        private static string? _lastConnectionStatus;
        private static int _consecutiveIdleDetections = 0;
        private static readonly List<string> _recentDisconnectReasons = new List<string>();
        private static double _lastHeartbeatValue = double.NaN;
        private static bool _isConnected = false;

        // Activity suppression
        private static float _suppressSyntheticUntil = 0f;
        private static bool _wasSuppressedLastTick = false;
        private static float _lastInputCheck = 0f;
        private const float INPUT_CHECK_INTERVAL = 0.25f; // 4x per second
        private static Vector3 _lastMousePos;
        private static bool _lastMousePosInitialized = false;
        
        #endregion
        
        #region Public Interface
        
        /// <summary>
        /// Called from the mod's OnUpdate to perform anti-idle checks and actions
        /// </summary>
        public static void OnUpdate()
        {
            // Check if anti-idle is enabled
            if (!Settings.useAntiIdle)
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

                // Check connection status periodically
                if (Time.time - _lastStatusCheck >= STATUS_CHECK_INTERVAL)
                {
                    CheckConnectionStatus();
                    _lastStatusCheck = Time.time;
                }
                
                // Send periodic heartbeat to prevent idle
                if (Time.time - _lastHeartbeat >= HEARTBEAT_INTERVAL)
                {
                    SendHeartbeat();
                    _lastHeartbeat = Time.time;
                }
                
                // Perform periodic anti-idle action (window open/close)
                var interval = Mathf.Max(Settings.antiIdleInterval, 10f); // Minimum 10 seconds
                if (Time.time - _lastAntiIdleAction >= interval)
                {
                    PerformAntiIdleAction();
                    _lastAntiIdleAction = Time.time;
                }

                // Optionally send a synthetic keepalive user message to server
                if (Settings.useSyntheticKeepAlive)
                {
                    if (_isConnected)
                    {
                        // Suppress synthetic keepalive when user is active
                        bool isSuppressed = Settings.suppressKeepAliveOnActivity && Time.time < _suppressSyntheticUntil;
                        if (isSuppressed)
                        {
                            if (!_wasSuppressedLastTick)
                                MelonLogger.Msg($"[AntiIdle] Suppressing synthetic keepalive for {(Mathf.Max(0f, _suppressSyntheticUntil - Time.time)):F0}s");
                            _wasSuppressedLastTick = true;
                        }
                        else
                        {
                            if (_wasSuppressedLastTick)
                                MelonLogger.Msg("[AntiIdle] Resuming synthetic keepalive");
                            _wasSuppressedLastTick = false;

                            var jitterSeconds = UnityEngine.Random.Range(-2f, 2f);
                            var effectiveInterval = Mathf.Max(Settings.keepAliveInterval + jitterSeconds, SYNTHETIC_KEEPALIVE_MIN_INTERVAL);
                            if (Time.time - _lastSyntheticKeepAlive >= effectiveInterval)
                            {
                                SendSyntheticKeepAlive();
                                _lastSyntheticKeepAlive = Time.time;
                            }
                        }
                    }
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
                RegisterActivity(Settings.sceneChangeSuppressionSeconds);
            }
            catch (Exception e)
            {
                MelonLogger.Error($"AntiIdleSystem.OnSceneChanged error: {e.Message}");
            }
        }
        
        /// <summary>
        /// Called by patches when connection status changes
        /// </summary>
        public static void OnConnectionStatusChanged(object? status)
        {
            try
            {
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
                    if (_isConnected)
                        RegisterActivity(Mathf.Min(Settings.networkActivitySuppressionSeconds, 30f));
                    
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
                RegisterActivity(Settings.networkActivitySuppressionSeconds);
            }
            catch (Exception e)
            {
                MelonLogger.Error($"AntiIdleSystem.OnMessageSent error: {e.Message}");
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
                MelonLogger.Msg("[AntiIdle] Action: heartbeat reset + status snapshot");
                
                // Try to reset the heartbeat timer by calling the game's internal heartbeat method
                ResetHeartbeatTimer();
                
                // Brief networking snapshot
                ParseNetworkingState();
                MelonLogger.Msg("[AntiIdle] Action complete");
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
                   $"LastStatus={_lastConnectionStatus ?? "None"}, " +
                   $"IdleDetections={_consecutiveIdleDetections}, " +
                   $"RecentDisconnects={_recentDisconnectReasons.Count}, " +
                   $"Suppressed={(Settings.suppressKeepAliveOnActivity && Time.time < _suppressSyntheticUntil)}";
        }
        
        #endregion
        
        #region Heartbeat Management
        
        private static void ResetHeartbeatTimer()
        {
            try
            {
                if (_netMultiClient == null) return;
                
                var clientType = _netMultiClient.GetType();
                
                // Method 1: Try direct field/property reset FIRST (most reliable)
                
                bool resetSucceeded = false;
                var nowSeconds = GetNetworkNowSeconds();
                
                // Helper: recursive base-class field lookup
                static FieldInfo? FindFieldRecursive(Type type, string name)
                {
                    for (var t = type; t != null; t = t.BaseType!)
                    {
                        var fi = t.GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                        if (fi != null) return fi;
                    }
                    return null;
                }
                
                // Helper: enumerate all instance fields across type hierarchy
                static IEnumerable<FieldInfo> GetAllInstanceFields(Type type)
                {
                    for (var t = type; t != null; t = t.BaseType!)
                    {
                        foreach (var f in t.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                            yield return f;
                    }
                }
                
                // Try field by exact name (search hierarchy)
                var lastHeartbeatField = FindFieldRecursive(clientType, "m_lastHeartbeat");
                
                // Try property by exact name on current type (fallback)
                var lastHeartbeatProperty = clientType.GetProperty("m_lastHeartbeat",
                    BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);

                static double ReadAsDouble(object? value)
                {
                    if (value == null) return double.NaN;
                    try { return Convert.ToDouble(value); } catch { return double.NaN; }
                }
                
                if (lastHeartbeatField != null)
                {
                    try
                    {
                        var before = ReadAsDouble(lastHeartbeatField.GetValue(_netMultiClient));
                        lastHeartbeatField.SetValue(_netMultiClient, nowSeconds);
                        var after = ReadAsDouble(lastHeartbeatField.GetValue(_netMultiClient));
                        if (VerboseHeartbeatLogs)
#pragma warning disable CS0162 // Unreachable code detected
                            MelonLogger.Msg($"[AntiIdle] Heartbeat FIELD write: {before:F3} -> {after:F3} (target {nowSeconds:F3})");
#pragma warning restore CS0162 // Unreachable code detected
                        resetSucceeded = !double.IsNaN(after) && Math.Abs(after - nowSeconds) <= 1.5;
                    }
                    catch (Exception ex)
                    {
                        if (VerboseHeartbeatLogs)
#pragma warning disable CS0162 // Unreachable code detected
                            MelonLogger.Msg($"[AntiIdle] Heartbeat FIELD write failed: {ex.Message}");
#pragma warning restore CS0162 // Unreachable code detected
                    }
                }
                else if (lastHeartbeatProperty != null && lastHeartbeatProperty.CanWrite)
                {
                    try
                    {
                        var before = ReadAsDouble(lastHeartbeatProperty.GetValue(_netMultiClient));
                        lastHeartbeatProperty.SetValue(_netMultiClient, nowSeconds); // Set to current network time
                        var after = ReadAsDouble(lastHeartbeatProperty.GetValue(_netMultiClient));
                        if (VerboseHeartbeatLogs)
#pragma warning disable CS0162 // Unreachable code detected
                            MelonLogger.Msg($"[AntiIdle] Heartbeat PROPERTY write: {before:F3} -> {after:F3} (target {nowSeconds:F3})");
#pragma warning restore CS0162 // Unreachable code detected
                        resetSucceeded = !double.IsNaN(after) && Math.Abs(after - nowSeconds) <= 1.5;
                    }
                    catch (Exception ex)
                    {
                        if (VerboseHeartbeatLogs)
#pragma warning disable CS0162 // Unreachable code detected
                            MelonLogger.Msg($"[AntiIdle] Heartbeat PROPERTY write failed: {ex.Message}");
#pragma warning restore CS0162 // Unreachable code detected
                    }
                }
                
                // If still not successful, try alternative fields on NetMultiClient (search hierarchy)
                if (!resetSucceeded)
                {
                    var allFields = GetAllInstanceFields(clientType);
                    var altField = allFields.FirstOrDefault(f =>
                        f.Name.ToLowerInvariant().Contains("heartbeat") &&
                        f.FieldType == typeof(double));
                    if (altField != null)
                    {
                        try
                        {
                            var before = ReadAsDouble(altField.GetValue(_netMultiClient));
                            altField.SetValue(_netMultiClient, nowSeconds);
                            var after = ReadAsDouble(altField.GetValue(_netMultiClient));
                            if (VerboseHeartbeatLogs)
#pragma warning disable CS0162 // Unreachable code detected
                                MelonLogger.Msg($"[AntiIdle] Heartbeat ALT FIELD write: {before:F3} -> {after:F3} (target {nowSeconds:F3})");
#pragma warning restore CS0162 // Unreachable code detected
                            resetSucceeded = !double.IsNaN(after) && Math.Abs(after - nowSeconds) <= 1.5;
                        }
                        catch (Exception ex)
                        {
                            if (VerboseHeartbeatLogs)
#pragma warning disable CS0162 // Unreachable code detected
                                MelonLogger.Msg($"[AntiIdle] Heartbeat ALT FIELD write failed: {ex.Message}");
#pragma warning restore CS0162 // Unreachable code detected
                        }
                    }
                }
                
                // If still not successful, try to find NetPeer and reset its m_lastHeartbeat
                if (!resetSucceeded)
                {
                    var netPeerField = GetAllInstanceFields(clientType)
                        .FirstOrDefault(f => f.FieldType.Name.Contains("NetPeer"));
                    if (netPeerField != null)
                    {
                        var netPeerInstance = netPeerField.GetValue(_netMultiClient);
                        if (netPeerInstance != null)
                        {
                            SetNetPeer(netPeerInstance);
                            var netPeerType = netPeerInstance.GetType();
                            var npHeartbeatField = FindFieldRecursive(netPeerType, "m_lastHeartbeat");
                            var npHeartbeatProperty = netPeerType.GetProperty("m_lastHeartbeat", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
                            if (npHeartbeatField != null || (npHeartbeatProperty != null && npHeartbeatProperty.CanWrite))
                            {
                                try
                                {
                                    double before = double.NaN;
                                    double after = double.NaN;
                                    if (npHeartbeatField != null)
                                    {
                                        before = ReadAsDouble(npHeartbeatField.GetValue(netPeerInstance));
                                        npHeartbeatField.SetValue(netPeerInstance, nowSeconds);
                                        after = ReadAsDouble(npHeartbeatField.GetValue(netPeerInstance));
                                        if (VerboseHeartbeatLogs)
#pragma warning disable CS0162 // Unreachable code detected
                                            MelonLogger.Msg($"[AntiIdle] NetPeer FIELD write: {before:F3} -> {after:F3} (target {nowSeconds:F3})");
#pragma warning restore CS0162 // Unreachable code detected
                                    }
                                    else if (npHeartbeatProperty != null)
                                    {
                                        before = ReadAsDouble(npHeartbeatProperty.GetValue(netPeerInstance));
                                        npHeartbeatProperty.SetValue(netPeerInstance, nowSeconds);
                                        after = ReadAsDouble(npHeartbeatProperty.GetValue(netPeerInstance));
                                        if (VerboseHeartbeatLogs)
#pragma warning disable CS0162 // Unreachable code detected
                                            MelonLogger.Msg($"[AntiIdle] NetPeer PROPERTY write: {before:F3} -> {after:F3} (target {nowSeconds:F3})");
#pragma warning restore CS0162 // Unreachable code detected
                                    }
                                    resetSucceeded = !double.IsNaN(after) && Math.Abs(after - nowSeconds) <= 1.5;
                                }
                                catch (Exception ex)
                                {
                                    if (VerboseHeartbeatLogs)
#pragma warning disable CS0162 // Unreachable code detected
                                        MelonLogger.Msg($"[AntiIdle] NetPeer heartbeat write failed: {ex.Message}");
#pragma warning restore CS0162 // Unreachable code detected
                                }
                            }
                        }
                    }
                }
                
                // If still not successful, try to find and call methods as last resort
                if (!resetSucceeded)
                {
                    // Method 2: Try to find and call a heartbeat method (quiet)
                    var heartbeatMethods = clientType.GetMethods()
                        .Where(m => (m.Name.ToLowerInvariant().Contains("heartbeat") ||
                                   m.Name.ToLowerInvariant().Contains("keepalive") ||
                                   m.Name.ToLowerInvariant().Contains("ping")) &&
                               !m.Name.ToLowerInvariant().StartsWith("get_"))
                        .ToArray();
                    
                    foreach (var method in heartbeatMethods)
                    {
                        try
                        {
                            method.Invoke(_netMultiClient, null);
                            resetSucceeded = true;
                            break;
                        }
                        catch (Exception ex)
                        {
                            MelonLogger.Msg($"[AntiIdle] Heartbeat method call failed: {ex.Message}");
                        }
                    }
                }
                
                // Method 3 (informational): Log setter methods that look related
                if (!resetSucceeded)
                {
                    var setterMethods = clientType.GetMethods()
                        .Where(m => m.Name.ToLowerInvariant().Contains("set") &&
                               m.Name.ToLowerInvariant().Contains("heartbeat"))
                        .ToArray();
                    
                    foreach (var method in setterMethods)
                    {
                        try
                        {
                            var parameters = method.GetParameters();
                            if (parameters.Length == 1 && parameters[0].ParameterType == typeof(double))
                            {
                                method.Invoke(_netMultiClient, new object[] { nowSeconds });
                                resetSucceeded = true;
                                break;
                            }
                            else if (parameters.Length == 1 && parameters[0].ParameterType == typeof(float))
                            {
                                method.Invoke(_netMultiClient, new object[] { (float)nowSeconds });
                                resetSucceeded = true;
                                break;
                            }
                            else if (parameters.Length == 0)
                            {
                                method.Invoke(_netMultiClient, null);
                                resetSucceeded = true;
                                break;
                            }
                        }
                        catch (Exception ex)
                        {
                            MelonLogger.Msg($"[AntiIdle] Setter call failed: {ex.Message}");
                        }
                    }
                }
                
                // Method 4 (informational): Find SendMessage for potential synthetic keepalive in future
                if (!resetSucceeded)
                {
                    var sendMessageMethod = clientType.GetMethod("SendMessage");
                    if (sendMessageMethod != null)
                    {
                        // Found SendMessage; keep in mind for future keepalive
                    }
                }
                
                if (!resetSucceeded)
                {
                    // No heartbeat reset method succeeded
                }

                // Best-effort: also reset timeout-related state on the primary server connection
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

                // Preferred delivery: ReliableUnordered; fallback to Unreliable
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
                    }
                }
                catch { }

                // Try direct NetConnection.SendMessage first, if available
                try
                {
                    if (_serverConnection != null)
                    {
                        var connType = _serverConnection.GetType();
                        var connSend = connType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                            .FirstOrDefault(m => m.Name == "SendMessage" && m.GetParameters().Length == 3);
                        if (connSend != null)
                        {
                            var ps = connSend.GetParameters();
                            var deliveryType = ps[1].ParameterType;
                            object delivery = deliveryReliable ?? (deliveryType.IsEnum ? Enum.Parse(deliveryType, "ReliableUnordered") : (object)0);
                            var channel = 0;
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

                // Try 4-arg overload first: SendMessage(msg, connection, method, channel)
                try
                {
                    var send4 = clientType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                        .FirstOrDefault(m => m.Name == "SendMessage" && m.GetParameters().Length == 4 && m.GetParameters()[1].ParameterType.Name.Contains("NetConnection"));
                    if (!sent && send4 != null && _serverConnection != null)
                    {
                        var ps = send4.GetParameters();
                        var channel = 0;
                        var delivery = deliveryReliable ?? deliveryUnreliable ?? ps[2].ParameterType.GetEnumValues().GetValue(0);
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

                // Try 3-arg overload: SendMessage(msg, method, channel)
                if (!sent)
                {
                    try
                    {
                        var send3 = clientType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                            .FirstOrDefault(m => m.Name == "SendMessage" && m.GetParameters().Length == 3);
                        if (send3 != null)
                        {
                            var ps = send3.GetParameters();
                            var channel = 0;
                            var delivery = deliveryReliable ?? deliveryUnreliable ?? ps[1].ParameterType.GetEnumValues().GetValue(0);
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
                }

                // Peer-level send as last resort
                if (!sent)
                {
                    try
                    {
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
                                object delivery = ps[1].ParameterType.IsEnum ? Enum.Parse(ps[1].ParameterType, "ReliableUnordered") : (object)0;
                                int channel = 0;
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
                }

                if (!sent && lastError != null)
                {
                    MelonLogger.Warning($"[AntiIdle] Keepalive send failed: {lastError}");
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
                if (Input.anyKey)
                    activityDetected = true;
                else if (Input.GetMouseButton(0) || Input.GetMouseButton(1) || Input.GetMouseButton(2))
                    activityDetected = true;
                else
                {
                    // Mouse moved significantly
                    var pos = Input.mousePosition;
                    if ((_lastMousePos - pos).sqrMagnitude > 0.5f)
                        activityDetected = true;
                    _lastMousePos = pos;
                }

                if (activityDetected)
                {
                    RegisterActivity(Settings.activitySuppressionSeconds);
                }
            }
            catch (Exception e)
            {
                MelonLogger.Error($"AntiIdleSystem.DetectUserActivity error: {e.Message}");
            }
        }

        // Extends suppression window for synthetic keepalive
        private static void RegisterActivity(float seconds)
        {
            if (seconds <= 0f) return;
            var until = Time.time + seconds;
            if (until > _suppressSyntheticUntil)
                _suppressSyntheticUntil = until;
        }
        
        #endregion
    }
} 
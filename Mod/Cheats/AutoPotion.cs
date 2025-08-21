using Il2Cpp;
using Mod.Game;
using MelonLoader;
using UnityEngine;

namespace Mod.Cheats
{
    internal class AutoPotion
    {
        // Cached component references for performance
        private static LocalPlayer? _cachedLocalPlayer;
        private static PlayerHealth? _cachedPlayerHealth;
        private static ChangeHealthMaterialDuringLifetime? _cachedReaperCheck;
        private static GameObject? _cachedPlayerObject;
        
        // Timing and state management
        private static float _lastUseTime = 0f;
        private static bool _componentsInitialized = false;
        
        // Configuration constants
        private const float DEFAULT_COOLDOWN_SECONDS = 1.0f;
        private const float HEALTH_THRESHOLD_MULTIPLIER = 0.01f; // Convert percentage to decimal
        
        /// <summary>
        /// Gets the current cooldown time in seconds (configurable via Settings)
        /// </summary>
        private static float CooldownSeconds 
        {
            get
            {
                var cooldown = Settings.autoPotionCooldown;
                // Ensure minimum cooldown to prevent spam
                return Mathf.Max(cooldown, 0.1f);
            }
        }
        
        /// <summary>
        /// Gets the health threshold as a decimal (0.0-1.0) for efficient comparison
        /// </summary>
        private static float HealthThresholdDecimal 
        {
            get
            {
                var threshold = Settings.autoHealthPotion * HEALTH_THRESHOLD_MULTIPLIER;
                // Clamp to valid range
                return Mathf.Clamp01(threshold);
            }
        }

        /// <summary>
        /// Initializes or refreshes cached component references
        /// </summary>
        private static bool InitializeComponents()
        {
            try
            {
                var localPlayer = ObjectManager.GetLocalPlayer();
                if (localPlayer == null)
                {
                    _componentsInitialized = false;
                    return false;
                }

                // Cache components only if they've changed or aren't initialized
                if (!_componentsInitialized || _cachedPlayerObject != localPlayer)
                {
                    _cachedPlayerObject = localPlayer;
                    _cachedLocalPlayer = localPlayer.GetComponent<LocalPlayer>();
                    _cachedPlayerHealth = localPlayer.GetComponent<PlayerHealth>();
                    _cachedReaperCheck = localPlayer.GetComponentInChildren<ChangeHealthMaterialDuringLifetime>();
                    _componentsInitialized = true;
                    
                    MelonLogger.Msg("AutoPotion: Components cached successfully");
                }

                // In offline mode, LocalPlayer component does not exist; only require PlayerHealth
                if (ObjectManager.IsOfflineMode())
                {
                    return _cachedPlayerHealth != null;
                }

                // In online mode, require both LocalPlayer (for input) and PlayerHealth
                return _cachedLocalPlayer != null && _cachedPlayerHealth != null;
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error($"AutoPotion: Failed to initialize components - {ex.Message}");
                _componentsInitialized = false;
                return false;
            }
        }

        /// <summary>
        /// Checks if enough time has passed since the last potion use
        /// </summary>
        private static bool IsCooldownExpired()
        {
            return Time.time - _lastUseTime >= CooldownSeconds;
        }

        /// <summary>
        /// Checks if the player is in a valid state to use potions
        /// </summary>
        private static bool IsPlayerStateValid()
        {
            try
            {
                // Check if player is in Reaper Form (prevents potion usage)
                if (_cachedReaperCheck?.materialToChangeTo == UIGlobeHealth.AlternateMaterial.ReaperForm)
                {
                    return false;
                }

                // Additional state checks could be added here
                // e.g., if player is dead, in menu, etc.
                
                return true;
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error($"AutoPotion: Failed to validate player state - {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Attempts to use a health potion if conditions are met
        /// </summary>
        public static void UseHealthPotion()
        {
            try
            {
                // Early exit if auto-potion is disabled
                if (!Settings.useAutoPot) return;
                
                // Skip entirely in offline mode because LocalPlayer input component is not present
                if (ObjectManager.IsOfflineMode()) return;
                
                // Check cooldown
                if (!IsCooldownExpired()) return;
                
                // Ensure components are initialized
                if (!InitializeComponents()) return;
                
                // Validate player state
                if (!IsPlayerStateValid()) return;

                // Use the potion (requires LocalPlayer component -> online mode only)
                _cachedLocalPlayer?.PotionKeyPressed();
                _lastUseTime = Time.time;
                
                MelonLogger.Msg($"AutoPotion: Health potion used at {Time.time:F2}");
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error($"AutoPotion: Failed to use health potion - {ex.Message}");
            }
        }

        /// <summary>
        /// Main update method called every frame to monitor health and trigger potion usage
        /// </summary>
        public static void OnUpdate()
        {
            try
            {
                // Early exit if auto-potion is disabled or no player exists
                if (!Settings.useAutoPot || !ObjectManager.HasPlayer()) return;

                // If offline, disable behavior entirely
                if (ObjectManager.IsOfflineMode()) return;

                // Initialize components if needed
                if (!InitializeComponents()) return;

                // Check if health is below threshold and trigger potion usage
                var currentHealthPercent = _cachedPlayerHealth?.getHealthPercent() ?? 1.0f;
                
                // Validate health percentage is in valid range
                if (float.IsNaN(currentHealthPercent) || float.IsInfinity(currentHealthPercent))
                {
                    MelonLogger.Warning("AutoPotion: Invalid health percentage detected, skipping potion check");
                    return;
                }
                
                if (currentHealthPercent <= HealthThresholdDecimal)
                {
                    UseHealthPotion();
                }
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error($"AutoPotion: Update failed - {ex.Message}");
            }
        }

        /// <summary>
        /// Clears cached components when scene changes or player is lost
        /// </summary>
        public static void ClearCache()
        {
            _cachedLocalPlayer = null;
            _cachedPlayerHealth = null;
            _cachedReaperCheck = null;
            _cachedPlayerObject = null;
            _componentsInitialized = false;
            MelonLogger.Msg("AutoPotion: Component cache cleared");
        }

        /// <summary>
        /// Debug method to log current auto-potion system state
        /// </summary>
        public static void LogDebugInfo()
        {
            try
            {
                MelonLogger.Msg($"AutoPotion Debug Info:");
                MelonLogger.Msg($"  Enabled: {Settings.useAutoPot}");
                MelonLogger.Msg($"  Health Threshold: {Settings.autoHealthPotion:F1}% ({HealthThresholdDecimal:F3})");
                MelonLogger.Msg($"  Cooldown: {CooldownSeconds:F1}s");
                MelonLogger.Msg($"  Components Initialized: {_componentsInitialized}");
                MelonLogger.Msg($"  Last Use Time: {_lastUseTime:F2}");
                MelonLogger.Msg($"  Current Time: {Time.time:F2}");
                MelonLogger.Msg($"  Cooldown Expired: {IsCooldownExpired()}");
                MelonLogger.Msg($"  Offline Mode: {ObjectManager.IsOfflineMode()}");
                
                if (_cachedPlayerHealth != null)
                {
                    var healthPercent = _cachedPlayerHealth.getHealthPercent();
                    MelonLogger.Msg($"  Current Health: {healthPercent * 100:F1}%");
                    MelonLogger.Msg($"  Should Use Potion: {healthPercent <= HealthThresholdDecimal}");
                }
                else
                {
                    MelonLogger.Msg($"  Current Health: Unknown (PlayerHealth component not found)");
                }
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error($"AutoPotion: Failed to log debug info - {ex.Message}");
            }
        }
    }
}

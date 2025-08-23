using Il2Cpp;
using Mod.Game;
using MelonLoader;
using UnityEngine;

namespace Mod.Cheats
{
    internal static class AutoDisconnect
    {
        // Cached components (reusing the AutoPotion pattern)
        private static GameObject? _cachedPlayerObject;
        private static PlayerHealth? _cachedPlayerHealth;
        private static ChangeHealthMaterialDuringLifetime? _cachedReaperCheck;
        private static bool _componentsInitialized = false;

        // Timing / debounce
        private static float _lastAttemptTime = 0f;

        private static float ThresholdDecimal
        {
            get
            {
                return Mathf.Clamp01(Settings.autoDisconnectHealthPercent * 0.01f);
            }
        }

        private static float CooldownSeconds
        {
            get
            {
                return Mathf.Max(Settings.autoDisconnectCooldownSeconds, 1f);
            }
        }

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

                if (!_componentsInitialized || _cachedPlayerObject != localPlayer)
                {
                    _cachedPlayerObject = localPlayer;
                    _cachedPlayerHealth = localPlayer.GetComponent<PlayerHealth>();
                    _cachedReaperCheck = localPlayer.GetComponentInChildren<ChangeHealthMaterialDuringLifetime>();
                    _componentsInitialized = true;
                }

                return _cachedPlayerHealth != null;
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"AutoDisconnect: component init failed - {ex.Message}");
                _componentsInitialized = false;
                return false;
            }
        }

        private static bool IsSuppressed()
        {
            // Suppress around scene changes or activity using AntiIdle suppression window if present (best-effort)
            // For now we simply reuse Settings.sceneChangeSuppressionSeconds via Mod hook; expand later if needed.
            return false;
        }

        private static bool IsStateValid()
        {
            // offline gate?
            // if (ObjectManager.IsOfflineMode()) return false;

            // Skip known blocked states (e.g., Reaper form blocks potions; may also skip DC if desired)
            if (_cachedReaperCheck?.materialToChangeTo == UIGlobeHealth.AlternateMaterial.ReaperForm)
                return false;

            return true;
        }

        public static void OnUpdate()
        {
            try
            {
                if (!Settings.useAutoDisconnect) return;
                if (!ObjectManager.HasPlayer()) return;
                if (!InitializeComponents()) return;
                if (!IsStateValid()) return;
                if (IsSuppressed()) return;

                // Debounce attempts
                if (Time.time - _lastAttemptTime < CooldownSeconds) return;

                float hp = _cachedPlayerHealth?.getHealthPercent() ?? 1f;
                if (float.IsNaN(hp) || float.IsInfinity(hp)) return;

                if (hp <= ThresholdDecimal)
                {
                    _lastAttemptTime = Time.time;

                    // Stub: Log only unless confirmation disabled
                    if (Settings.autoDisconnectConfirm)
                    {
                        MelonLogger.Msg($"[AutoDisconnect] Would disconnect now (HP={hp * 100f:F1}% <= {Settings.autoDisconnectHealthPercent:F1}% threshold)");
                        return;
                    }

                    // TODO: Implement safe quit-to-menu flow (native call preferred). Placeholder:
                    MelonLogger.Warning("[AutoDisconnect] Triggered (stub) - implement quit-to-menu invocation here.");
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"AutoDisconnect.OnUpdate error: {ex.Message}");
            }
        }

        public static void ClearCache()
        {
            _cachedPlayerObject = null;
            _cachedPlayerHealth = null;
            _cachedReaperCheck = null;
            _componentsInitialized = false;
        }
    }
} 
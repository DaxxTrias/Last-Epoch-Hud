using Il2Cpp;
using Mod.Game;
using MelonLoader;
using UnityEngine;
using Il2CppLE.UI;

namespace Mod.Cheats
{
    internal static class AutoDisconnect
    {
        // Cached components (reusing the AutoPotion pattern)
        private static GameObject? _cachedPlayerObject;
        private static PlayerHealth? _cachedPlayerHealth;
        private static ChangeHealthMaterialDuringLifetime? _cachedReaperCheck;
        private static bool _componentsInitialized = false;
        private static UIBase? _cachedUIBase;

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

        public static void SetUIBase(UIBase? uiBase)
        {
            if (uiBase != null)
                _cachedUIBase = uiBase;
        }

        private static bool TryEnsureUIBase()
        {
            if (_cachedUIBase != null)
                return true;
            try
            {
                // Fast path: active object in scene
                var foundActive = UnityEngine.Object.FindObjectOfType<UIBase>();
                if (foundActive != null)
                {
                    _cachedUIBase = foundActive;
                    return true;
                }

                // Fallback: include inactive/hidden objects
                var all = Resources.FindObjectsOfTypeAll<UIBase>();
                if (all != null && all.Length > 0)
                {
                    // Prefer enabled/active if any
                    foreach (var ui in all)
                    {
                        if (ui != null && ui.isActiveAndEnabled)
                        {
                            _cachedUIBase = ui;
                            return true;
                        }
                    }
                    // Otherwise, take the first available instance
                    _cachedUIBase = all[0];
                    return _cachedUIBase != null;
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[AutoDisconnect] Find UIBase failed: {ex.Message}");
            }
            return false;
        }

        private static bool TryExitToLogin()
        {
            try
            {
                if (!TryEnsureUIBase())
                    return false;

                var ui = _cachedUIBase;
                if (ui == null)
                    return false;

                ui.ExitToLogin();
                return true;
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[AutoDisconnect] ExitToLogin failed: {ex.Message}");
                return false;
            }
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
                    // Optional: require no potions remaining
                    if (Settings.autoDisconnectOnlyWhenNoPotions)
                    {
                        var remaining = AutoPotion.TryGetRemainingPotions();
                        if (!remaining.HasValue)
                        {
                            // Unknown count: be conservative; do not disconnect
                            return;
                        }
                        if (remaining.Value > 0)
                        {
                            // Potions available: let AutoPotion handle it instead
                            return;
                        }
                    }

                    _lastAttemptTime = Time.time;

                    if (TryExitToLogin())
                    {
                        MelonLogger.Msg("[AutoDisconnect] ExitToLogin invoked");
                    }
                    else
                    {
                        MelonLogger.Warning("[AutoDisconnect] UIBase not available; unable to ExitToLogin");
                    }
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
            _cachedUIBase = null;
        }
    }
} 
using Il2CppLE.UI.Minimap;
using MelonLoader;
using UnityEngine;

namespace Mod.Cheats
{
    internal class MapHack
    {
        private const float DefaultRevealRadius = 40f;
        private const float BoostRevealRadius = 5000f;
        private const float BoostDurationSeconds = 60f;
        private const float LookupRetrySeconds = 1f;
        private const float ErrorLogCooldownSeconds = 5f;
        private const string MinimapCanvasName = "DMMap Canvas";

        private static Minimap? s_minimap;
        private static GameObject? s_minimapCanvas;

        private static int s_sceneVersion;
        private static int s_boostedSceneVersion = -1;
        private static bool s_boostApplied;
        private static float s_restoreAt;
        private static float s_nextLookupAt;
        private static float s_nextErrorLogAt;
        private static float s_originalRevealRadius = DefaultRevealRadius;
        private static bool s_hasOriginalRevealRadius;

        public static void OnSceneWasInitialized()
        {
            unchecked
            {
                s_sceneVersion++;
            }

            s_minimap = null;
            s_minimapCanvas = null;
            s_boostApplied = false;
            s_restoreAt = 0f;
            s_nextLookupAt = 0f;
            s_nextErrorLogAt = 0f;
            s_originalRevealRadius = DefaultRevealRadius;
            s_hasOriginalRevealRadius = false;
        }

        public static void OnUpdate(bool hasPlayer)
        {
            // If toggled off mid-session, restore immediately if we previously boosted.
            if (!Settings.mapHack)
            {
                RestoreIfNeeded(force: true);
                return;
            }

            // Keep restore path independent from player availability.
            RestoreIfNeeded(force: false);
            if (s_boostApplied)
                return;

            if (!hasPlayer)
                return;

            // One reveal pulse per scene.
            if (s_boostedSceneVersion == s_sceneVersion)
                return;

            if (!TryGetMinimapInstance(out Minimap? minimap) || minimap == null)
                return;

            if (TryGetRevealRadius(minimap, out float originalRevealRadius))
            {
                s_originalRevealRadius = originalRevealRadius;
                s_hasOriginalRevealRadius = true;
            }

            if (!TrySetRevealRadius(minimap, BoostRevealRadius))
                return;

            s_boostApplied = true;
            s_boostedSceneVersion = s_sceneVersion;
            s_restoreAt = Time.unscaledTime + BoostDurationSeconds;
            MelonLogger.Msg($"[MapHack] Temporarily set Minimap.RevealRadius to {BoostRevealRadius:F0} for {BoostDurationSeconds:F0}s.");
        }

        private static void RestoreIfNeeded(bool force)
        {
            if (!s_boostApplied)
                return;

            if (!force && Time.unscaledTime < s_restoreAt)
                return;

            if (!TryGetMinimapInstance(out Minimap? minimap) || minimap == null)
                return;

            float restoreValue = s_hasOriginalRevealRadius ? s_originalRevealRadius : DefaultRevealRadius;
            if (!TrySetRevealRadius(minimap, restoreValue))
                return;

            s_boostApplied = false;
            s_hasOriginalRevealRadius = false;
            MelonLogger.Msg($"[MapHack] Restored Minimap.RevealRadius to {restoreValue:F0}.");
        }

        private static bool TryGetMinimapInstance(out Minimap? minimap)
        {
            minimap = null;
            if (s_minimap != null)
            {
                minimap = s_minimap;
                return true;
            }

            if (Time.unscaledTime < s_nextLookupAt)
                return false;

            try
            {
                if (s_minimapCanvas == null)
                {
                    s_minimapCanvas = GameObject.Find(MinimapCanvasName);
                    if (s_minimapCanvas == null)
                    {
                        s_nextLookupAt = Time.unscaledTime + LookupRetrySeconds;
                        return false;
                    }
                }

                Minimap? resolved = s_minimapCanvas.GetComponent<Minimap>();
                if (resolved == null)
                    resolved = s_minimapCanvas.GetComponentInChildren<Minimap>(true);
                if (resolved == null)
                {
                    s_nextLookupAt = Time.unscaledTime + LookupRetrySeconds;
                    return false;
                }

                s_minimap = resolved;
                minimap = resolved;
                return true;
            }
            catch (Exception e)
            {
                s_nextLookupAt = Time.unscaledTime + LookupRetrySeconds;
                LogErrorThrottled($"[MapHack] Minimap lookup failed: {e.Message}");
                return false;
            }
        }

        private static bool TryGetRevealRadius(Minimap minimap, out float value)
        {
            value = DefaultRevealRadius;
            try
            {
                value = minimap.RevealRadius;
                return true;
            }
            catch (Exception e)
            {
                LogErrorThrottled($"[MapHack] Reading RevealRadius failed: {e.Message}");
                return false;
            }
        }

        private static bool TrySetRevealRadius(Minimap minimap, float value)
        {
            try
            {
                minimap.RevealRadius = value;
                return true;
            }
            catch (Exception e)
            {
                LogErrorThrottled($"[MapHack] Failed setting RevealRadius: {e.Message}");
                return false;
            }
        }

        private static void LogErrorThrottled(string message)
        {
            if (Time.unscaledTime < s_nextErrorLogAt)
                return;

            s_nextErrorLogAt = Time.unscaledTime + ErrorLogCooldownSeconds;
            MelonLogger.Error(message);
        }
    }
}

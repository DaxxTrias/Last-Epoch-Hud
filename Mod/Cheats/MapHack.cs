using HarmonyLib;
using MelonLoader;
using Mod.Utils;
using System.Reflection;
using UnityEngine;

namespace Mod.Cheats
{
    internal class MapHack
    {
        private const float DefaultRevealRadius = 40f;
        private const float BoostRevealRadius = 5000f;
        private const float BoostDurationSeconds = 60f;
        private const float LookupRetrySeconds = 1f;
        private const string MinimapCanvasName = "DMMap Canvas";

        private static Type? s_minimapType;
        private static Il2CppSystem.Type? s_minimapIl2CppType;
        private static MethodInfo? s_getRevealRadiusMethod;
        private static MethodInfo? s_setRevealRadiusMethod;
        private static PropertyInfo? s_revealRadiusProperty;
        private static FieldInfo? s_revealRadiusField;
        private static UnityEngine.Object? s_minimapInstance;
        private static GameObject? s_minimapCanvas;

        private static int s_sceneVersion;
        private static int s_boostedSceneVersion = -1;
        private static bool s_boostApplied;
        private static float s_restoreAt;
        private static float s_nextLookupAt;
        private static float s_originalRevealRadius = DefaultRevealRadius;
        private static bool s_hasOriginalRevealRadius;
        private static bool s_loggedMissingType;

        public static void OnSceneWasInitialized()
        {
            unchecked
            {
                s_sceneVersion++;
            }

            s_minimapInstance = null;
            s_minimapCanvas = null;
            s_boostApplied = false;
            s_restoreAt = 0f;
            s_nextLookupAt = 0f;
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

            if (!TryGetMinimapInstance(out UnityEngine.Object? minimap) || minimap == null)
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

            if (!TryGetMinimapInstance(out UnityEngine.Object? minimap) || minimap == null)
                return;

            float restoreValue = s_hasOriginalRevealRadius ? s_originalRevealRadius : DefaultRevealRadius;
            if (!TrySetRevealRadius(minimap, restoreValue))
                return;

            s_boostApplied = false;
            s_hasOriginalRevealRadius = false;
            MelonLogger.Msg($"[MapHack] Restored Minimap.RevealRadius to {restoreValue:F0}.");
        }

        private static bool TryGetMinimapInstance(out UnityEngine.Object? minimap)
        {
            minimap = null;
            if (s_minimapInstance != null)
            {
                minimap = s_minimapInstance;
                return true;
            }

            if (Time.unscaledTime < s_nextLookupAt)
                return false;

            if (!TryResolveMinimapMembers())
            {
                s_nextLookupAt = Time.unscaledTime + LookupRetrySeconds;
                return false;
            }

            if (TryGetMinimapFromCanvas(out minimap))
                return true;

            UnityEngine.Object[] instances = Resources.FindObjectsOfTypeAll(s_minimapIl2CppType!);
            for (int i = 0; i < instances.Length; i++)
            {
                if (instances[i] == null)
                    continue;

                s_minimapInstance = instances[i];
                minimap = s_minimapInstance;
                return true;
            }

            s_nextLookupAt = Time.unscaledTime + LookupRetrySeconds;
            return false;
        }

        private static bool TryGetMinimapFromCanvas(out UnityEngine.Object? minimap)
        {
            minimap = null;

            // Cached canvas can become invalid on scene transitions.
            if (s_minimapCanvas == null)
            {
                s_minimapCanvas = GameObject.Find(MinimapCanvasName);
                if (s_minimapCanvas == null)
                    return false;
            }

            Component? component = s_minimapCanvas.GetComponent(s_minimapIl2CppType!);
            if (component == null)
                component = s_minimapCanvas.GetComponentInChildren(s_minimapIl2CppType!, true);

            if (component == null)
                return false;

            s_minimapInstance = component;
            minimap = component;
            return true;
        }

        private static bool TryResolveMinimapMembers()
        {
            if (s_minimapType == null)
            {
                s_minimapType = TypeLookup.FindType(
                    "Il2CppLE.UI.Minimap.Minimap",
                    "LE.UI.Minimap.Minimap");

                if (s_minimapType == null)
                {
                    if (!s_loggedMissingType)
                    {
                        s_loggedMissingType = true;
                        MelonLogger.Error("[MapHack] Could not resolve Minimap type. Tried Il2CppLE.UI.Minimap.Minimap.");
                    }
                    return false;
                }
            }

            if (s_minimapIl2CppType == null)
            {
                string? aqn = s_minimapType.AssemblyQualifiedName;
                if (!string.IsNullOrWhiteSpace(aqn))
                    s_minimapIl2CppType = Il2CppSystem.Type.GetType(aqn);
                if (s_minimapIl2CppType == null)
                    s_minimapIl2CppType = Il2CppSystem.Type.GetType("Il2CppLE.UI.Minimap.Minimap, Il2CppLE");
                if (s_minimapIl2CppType == null)
                {
                    MelonLogger.Error("[MapHack] Failed converting Minimap type to Il2CppSystem.Type.");
                    return false;
                }
            }

            if (s_setRevealRadiusMethod == null && s_revealRadiusProperty == null && s_revealRadiusField == null)
            {
                s_getRevealRadiusMethod = AccessTools.PropertyGetter(s_minimapType, "RevealRadius")
                    ?? AccessTools.Method(s_minimapType, "get_RevealRadius");
                s_setRevealRadiusMethod = AccessTools.PropertySetter(s_minimapType, "RevealRadius")
                    ?? AccessTools.Method(s_minimapType, "set_RevealRadius", new[] { typeof(float) });
                s_revealRadiusProperty = AccessTools.Property(s_minimapType, "RevealRadius");
                s_revealRadiusField = AccessTools.Field(s_minimapType, "RevealRadius")
                    ?? AccessTools.Field(s_minimapType, "revealRadius");

                if (s_setRevealRadiusMethod == null && s_revealRadiusProperty == null && s_revealRadiusField == null)
                {
                    MelonLogger.Error("[MapHack] Minimap.RevealRadius setter/field not found.");
                    return false;
                }
            }

            return true;
        }

        private static bool TryGetRevealRadius(UnityEngine.Object minimap, out float value)
        {
            value = DefaultRevealRadius;

            try
            {
                if (!TryResolveMinimapMembers())
                    return false;

                object target = minimap;
                if (s_getRevealRadiusMethod != null)
                {
                    object? result = s_getRevealRadiusMethod.Invoke(target, null);
                    if (result is float radius)
                    {
                        value = radius;
                        return true;
                    }
                }

                if (s_revealRadiusProperty != null && s_revealRadiusProperty.CanRead)
                {
                    object? result = s_revealRadiusProperty.GetValue(target);
                    if (result is float radius)
                    {
                        value = radius;
                        return true;
                    }
                }

                if (s_revealRadiusField != null)
                {
                    object? result = s_revealRadiusField.GetValue(target);
                    if (result is float radius)
                    {
                        value = radius;
                        return true;
                    }
                }
            }
            catch
            {
                // Non-fatal: fallback restore value is the known default.
            }

            return false;
        }

        private static bool TrySetRevealRadius(UnityEngine.Object minimap, float value)
        {
            try
            {
                if (!TryResolveMinimapMembers())
                    return false;

                object target = minimap;
                if (s_setRevealRadiusMethod != null)
                {
                    s_setRevealRadiusMethod.Invoke(target, new object[] { value });
                    return true;
                }

                if (s_revealRadiusProperty != null && s_revealRadiusProperty.CanWrite)
                {
                    s_revealRadiusProperty.SetValue(target, value);
                    return true;
                }

                if (s_revealRadiusField != null)
                {
                    s_revealRadiusField.SetValue(target, value);
                    return true;
                }
            }
            catch (Exception e)
            {
                MelonLogger.Error($"[MapHack] Failed setting RevealRadius: {e.Message}");
            }

            return false;
        }
    }
}

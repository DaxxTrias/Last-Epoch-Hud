using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace Mod.Utils
{
    /// <summary>
    /// Shared reflection bridge for EpochInputManager singleton access.
    /// Centralizes input notify + menu input blocking bindings.
    /// </summary>
    internal static class EpochInputManagerBridge
    {
        private static Type? s_epochInputManagerType;
        private static MethodInfo? s_miSendInputActionPerformed;
        private static PropertyInfo? s_piButtonPressBlocked;
        private static PropertyInfo? s_piInstance;
        private static FieldInfo? s_fiInstance;
        private static object? s_cachedInstance;
        private static bool s_bindingsResolved;
        private static float s_nextResolveAttemptAt;

        private static bool s_loggedNoType;
        private static bool s_loggedNoSendMethod;
        private static bool s_loggedNoButtonBlockedProperty;
        private static bool s_loggedNoInstanceAccessor;

        private static bool s_hasAppliedButtonBlockState;
        private static bool s_lastAppliedButtonBlockState;
        private static object? s_lastAppliedButtonBlockInstance;

        public static bool TrySendInputActionPerformed()
        {
            try
            {
                EnsureBindings();
                if (s_epochInputManagerType == null)
                {
                    return false;
                }

                if (s_miSendInputActionPerformed == null)
                {
                    if (!s_loggedNoSendMethod)
                    {
                        s_loggedNoSendMethod = true;
                        MelonLoader.MelonLogger.Warning("[LEHud] EpochInputManager.SendInputActionPerformed not found");
                    }
                    return false;
                }

                object? target = null;
                if (!s_miSendInputActionPerformed.IsStatic)
                {
                    target = GetEpochInputManagerInstance();
                    if (target == null)
                    {
                        return false;
                    }
                }

                s_miSendInputActionPerformed.Invoke(target, null);
                return true;
            }
            catch (Exception e)
            {
                MelonLoader.MelonLogger.Error($"[LEHud] EpochInputManager SendInputActionPerformed invoke failed: {e.Message}");
                return false;
            }
        }

        public static bool TrySetButtonPressBlocked(bool blocked)
        {
            try
            {
                EnsureBindings();
                if (s_epochInputManagerType == null)
                {
                    return false;
                }

                if (s_piButtonPressBlocked == null || !s_piButtonPressBlocked.CanWrite)
                {
                    if (!s_loggedNoButtonBlockedProperty)
                    {
                        s_loggedNoButtonBlockedProperty = true;
                        MelonLoader.MelonLogger.Warning("[LEHud] EpochInputManager.ButtonPressBlocked setter not found");
                    }
                    return false;
                }

                object? instance = GetEpochInputManagerInstance();
                if (instance == null)
                {
                    return false;
                }

                // Hot-path guard: avoid redundant property writes every frame.
                if (s_hasAppliedButtonBlockState
                    && ReferenceEquals(s_lastAppliedButtonBlockInstance, instance)
                    && s_lastAppliedButtonBlockState == blocked)
                {
                    return true;
                }

                s_piButtonPressBlocked.SetValue(instance, blocked, null);
                s_lastAppliedButtonBlockInstance = instance;
                s_lastAppliedButtonBlockState = blocked;
                s_hasAppliedButtonBlockState = true;
                return true;
            }
            catch (Exception e)
            {
                MelonLoader.MelonLogger.Error($"[LEHud] EpochInputManager.ButtonPressBlocked set failed: {e.Message}");
                return false;
            }
        }

        private static void EnsureBindings()
        {
            if (s_bindingsResolved)
            {
                return;
            }

            if (Time.unscaledTime < s_nextResolveAttemptAt)
            {
                return;
            }

            s_nextResolveAttemptAt = Time.unscaledTime + 2f;
            s_epochInputManagerType = TypeLookup.FindType(
                "Il2Cpp.EpochInputManager",
                "EpochInputManager",
                "Il2CppLE.EpochInputManager",
                "Il2CppLE.Input.EpochInputManager");

            if (s_epochInputManagerType == null)
            {
                if (!s_loggedNoType)
                {
                    s_loggedNoType = true;
                    MelonLoader.MelonLogger.Warning("[LEHud] EpochInputManager type not found");
                }
                return;
            }

            s_miSendInputActionPerformed = AccessTools.Method(s_epochInputManagerType, "SendInputActionPerformed", Type.EmptyTypes);
            if (s_miSendInputActionPerformed == null)
            {
                var methods = AccessTools.GetDeclaredMethods(s_epochInputManagerType);
                for (int i = 0; i < methods.Count; i++)
                {
                    MethodInfo method = methods[i];
                    if (method.Name == "SendInputActionPerformed" && method.GetParameters().Length == 0)
                    {
                        s_miSendInputActionPerformed = method;
                        break;
                    }
                }
            }

            s_piButtonPressBlocked = s_epochInputManagerType.GetProperty(
                "ButtonPressBlocked",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            s_piInstance = s_epochInputManagerType.GetProperty(
                "Instance",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

            FieldInfo[] staticFields = s_epochInputManagerType.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            for (int i = 0; i < staticFields.Length; i++)
            {
                FieldInfo field = staticFields[i];
                if (!field.FieldType.IsAssignableFrom(s_epochInputManagerType))
                {
                    continue;
                }

                if (string.Equals(field.Name, "Instance", StringComparison.Ordinal)
                    || string.Equals(field.Name, "instance", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(field.Name, "s_instance", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(field.Name, "_instance", StringComparison.OrdinalIgnoreCase))
                {
                    s_fiInstance = field;
                    break;
                }
            }

            s_bindingsResolved = true;
        }

        private static object? GetEpochInputManagerInstance()
        {
            if (s_epochInputManagerType == null)
            {
                return null;
            }

            if (s_cachedInstance is UnityEngine.Object cachedUnityObject && cachedUnityObject != null)
            {
                return s_cachedInstance;
            }

            if (s_cachedInstance != null && s_cachedInstance is not UnityEngine.Object)
            {
                return s_cachedInstance;
            }

            try
            {
                object? instance = null;
                if (s_piInstance != null)
                {
                    instance = s_piInstance.GetValue(null, null);
                }

                if (instance == null && s_fiInstance != null)
                {
                    instance = s_fiInstance.GetValue(null);
                }

                if (instance == null)
                {
                    if (!s_loggedNoInstanceAccessor && s_piInstance == null && s_fiInstance == null)
                    {
                        s_loggedNoInstanceAccessor = true;
                        MelonLoader.MelonLogger.Warning("[LEHud] EpochInputManager singleton accessor not found");
                    }
                    return null;
                }

                s_cachedInstance = instance;
                return instance;
            }
            catch
            {
                return null;
            }
        }
    }
}

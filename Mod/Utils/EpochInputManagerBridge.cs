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
        private static bool s_loggedNoIdleNotificationField;
        private static bool s_loggedNoIdleWarnAfterField;

        private static bool s_hasAppliedButtonBlockState;
        private static bool s_lastAppliedButtonBlockState;
        private static object? s_lastAppliedButtonBlockInstance;

        private static Type? s_idleBindingsForType;
        private static FieldInfo? s_fiLastInputActionNotificationTime;
        private static PropertyInfo? s_piLastInputActionNotificationTime;
        private static FieldInfo? s_fiWarnAfterSecondsSinceLastInputNotification;
        private static PropertyInfo? s_piWarnAfterSecondsSinceLastInputNotification;
        private static float s_lastCheckIdleAssistAttemptAt;
        private static float s_lastCheckIdleSnapshotLogAt;

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

        public static void RegisterKnownInstance(object? instance)
        {
            if (instance == null)
                return;

            s_cachedInstance = instance;
            var runtimeType = instance.GetType();
            if (!s_bindingsResolved || s_epochInputManagerType != runtimeType)
            {
                ResolveBindingsForType(runtimeType);
            }
        }

        public static void ClearKnownInstance(object? instance)
        {
            if (s_cachedInstance == null)
                return;

            if (instance == null || ReferenceEquals(s_cachedInstance, instance))
            {
                s_cachedInstance = null;
                s_lastAppliedButtonBlockInstance = null;
                s_hasAppliedButtonBlockState = false;
            }
        }

        public static void OnCheckIdleInputObserved(object? instance)
        {
            try
            {
                if (instance == null)
                    return;

                RegisterKnownInstance(instance);
                if (!global::Mod.Settings.useSimpleAntiIdle)
                    return;

                var runtimeType = instance.GetType();
                EnsureIdleBindings(runtimeType);

                float lastNotify = ReadFloatMember(instance, s_fiLastInputActionNotificationTime, s_piLastInputActionNotificationTime);
                float warnAfter = ReadFloatMember(instance, s_fiWarnAfterSecondsSinceLastInputNotification, s_piWarnAfterSecondsSinceLastInputNotification);
                if (float.IsNaN(lastNotify))
                {
                    if (!s_loggedNoIdleNotificationField)
                    {
                        s_loggedNoIdleNotificationField = true;
                        MelonLoader.MelonLogger.Warning("[LEHud] EpochInputManager last notification timestamp member not found");
                    }
                    return;
                }

                if (float.IsNaN(warnAfter) || warnAfter <= 0f)
                {
                    warnAfter = 600f;
                    if (!s_loggedNoIdleWarnAfterField)
                    {
                        s_loggedNoIdleWarnAfterField = true;
                        MelonLoader.MelonLogger.Warning("[LEHud] EpochInputManager warn-after member not found, assuming 600s");
                    }
                }

                float now = Time.realtimeSinceStartup;
                if (now <= 0f)
                    now = Time.unscaledTime;
                if (now <= 0f)
                    return;

                float elapsed = now - lastNotify;
                if (elapsed < 0f)
                    return;

                if (global::Mod.Settings.enableNetworkDiagnostics && Time.unscaledTime - s_lastCheckIdleSnapshotLogAt >= 30f)
                {
                    s_lastCheckIdleSnapshotLogAt = Time.unscaledTime;
                    Log.InfoThrottled(
                        LogSource.AntiIdle,
                        "epoch-checkidle-snapshot",
                        $"EpochInputManager.CheckIdleInput observed: elapsed={elapsed:F1}s warnAfter={warnAfter:F1}s",
                        TimeSpan.FromSeconds(20));
                }

                float assistThreshold = Mathf.Max(30f, warnAfter - 45f);
                if (elapsed < assistThreshold)
                    return;

                if (Time.unscaledTime - s_lastCheckIdleAssistAttemptAt < 20f)
                    return;
                s_lastCheckIdleAssistAttemptAt = Time.unscaledTime;

                bool sent = TrySendInputActionPerformed();
                if (sent)
                {
                    Log.WarningThrottled(
                        LogSource.AntiIdle,
                        "epoch-checkidle-assist-sent",
                        $"EpochInputManager idle assist sent input notify at elapsed={elapsed:F1}s (warnAfter={warnAfter:F1}s)",
                        TimeSpan.FromSeconds(10));
                }
                else
                {
                    Log.WarningThrottled(
                        LogSource.AntiIdle,
                        "epoch-checkidle-assist-failed",
                        $"EpochInputManager idle assist failed at elapsed={elapsed:F1}s (warnAfter={warnAfter:F1}s)",
                        TimeSpan.FromSeconds(15));
                }
            }
            catch (Exception e)
            {
                MelonLoader.MelonLogger.Error($"[LEHud] EpochInputManager CheckIdleInput observer failed: {e.Message}");
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
            Type? type = TypeLookup.FindType(
                "Il2Cpp.EpochInputManager",
                "EpochInputManager",
                "Il2CppLE.EpochInputManager",
                "Il2CppLE.Input.EpochInputManager");

            if (type == null)
            {
                if (!s_loggedNoType)
                {
                    s_loggedNoType = true;
                    MelonLoader.MelonLogger.Warning("[LEHud] EpochInputManager type not found");
                }
                return;
            }

            ResolveBindingsForType(type);
        }

        private static void ResolveBindingsForType(Type epochInputManagerType)
        {
            s_epochInputManagerType = epochInputManagerType;
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

        private static void EnsureIdleBindings(Type runtimeType)
        {
            if (s_idleBindingsForType == runtimeType)
                return;

            s_idleBindingsForType = runtimeType;
            s_fiLastInputActionNotificationTime = FindFloatField(runtimeType,
                "_lastInputActionNotificationTime",
                "lastInputActionNotificationTime",
                "LastInputActionNotificationTime",
                "<LastInputActionNotificationTime>k__BackingField");
            s_piLastInputActionNotificationTime = FindFloatProperty(runtimeType,
                "LastInputActionNotificationTime",
                "lastInputActionNotificationTime");

            s_fiWarnAfterSecondsSinceLastInputNotification = FindFloatField(runtimeType,
                "WarnAfterSecondsSinceLastInputNotification",
                "warnAfterSecondsSinceLastInputNotification",
                "<WarnAfterSecondsSinceLastInputNotification>k__BackingField");
            s_piWarnAfterSecondsSinceLastInputNotification = FindFloatProperty(runtimeType,
                "WarnAfterSecondsSinceLastInputNotification",
                "warnAfterSecondsSinceLastInputNotification");
        }

        private static FieldInfo? FindFloatField(Type type, params string[] names)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            for (int i = 0; i < names.Length; i++)
            {
                var fi = type.GetField(names[i], flags);
                if (fi != null && fi.FieldType == typeof(float))
                    return fi;
            }

            var allFields = type.GetFields(flags);
            for (int i = 0; i < allFields.Length; i++)
            {
                var f = allFields[i];
                if (f.FieldType != typeof(float))
                    continue;

                for (int j = 0; j < names.Length; j++)
                {
                    if (NameMatchesCandidate(f.Name, names[j]))
                        return f;
                }
            }

            return null;
        }

        private static PropertyInfo? FindFloatProperty(Type type, params string[] names)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            for (int i = 0; i < names.Length; i++)
            {
                var pi = type.GetProperty(names[i], flags);
                if (pi != null && pi.PropertyType == typeof(float))
                    return pi;
            }

            var allProps = type.GetProperties(flags);
            for (int i = 0; i < allProps.Length; i++)
            {
                var p = allProps[i];
                if (p.PropertyType != typeof(float))
                    continue;

                for (int j = 0; j < names.Length; j++)
                {
                    if (NameMatchesCandidate(p.Name, names[j]))
                        return p;
                }
            }

            return null;
        }

        private static bool NameMatchesCandidate(string memberName, string candidate)
        {
            if (string.Equals(memberName, candidate, StringComparison.OrdinalIgnoreCase))
                return true;

            string normalizedMember = NormalizeIdentifier(memberName);
            string normalizedCandidate = NormalizeIdentifier(candidate);
            if (normalizedMember.Length == 0 || normalizedCandidate.Length == 0)
                return false;

            return normalizedMember.IndexOf(normalizedCandidate, StringComparison.OrdinalIgnoreCase) >= 0
                || normalizedCandidate.IndexOf(normalizedMember, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string NormalizeIdentifier(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            var chars = new char[value.Length];
            int idx = 0;
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (char.IsLetterOrDigit(c))
                {
                    chars[idx++] = c;
                }
            }

            return idx == 0 ? string.Empty : new string(chars, 0, idx);
        }

        private static float ReadFloatMember(object instance, FieldInfo? field, PropertyInfo? prop)
        {
            try
            {
                if (field != null)
                {
                    object? value = field.GetValue(instance);
                    if (value != null)
                        return Convert.ToSingle(value);
                }
            }
            catch { }

            try
            {
                if (prop != null)
                {
                    object? value = prop.GetValue(instance, null);
                    if (value != null)
                        return Convert.ToSingle(value);
                }
            }
            catch { }

            return float.NaN;
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

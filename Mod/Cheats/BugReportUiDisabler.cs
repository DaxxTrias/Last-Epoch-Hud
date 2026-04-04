using MelonLoader;
using UnityEngine;

namespace Mod.Cheats.Patches
{
    internal static class BugReportUiDisabler
    {
        private static readonly HashSet<string> s_logOnce = new(StringComparer.Ordinal);
        private static readonly Dictionary<string, Func<object, object?>?> s_memberGetterCache = new(StringComparer.Ordinal);
        private static readonly Dictionary<Type, Func<object, bool>?> s_closeMethodCache = new();

        public static void LogOnce(string key, string message)
        {
            lock (s_logOnce)
            {
                if (!s_logOnce.Add(key))
                    return;
            }

            MelonLogger.Msg(message);
        }

        public static void TryDisableBugReportUi(object? owner, string source)
        {
            if (owner == null)
                return;

            try
            {
                TryHideMemberObject(owner, "bugReportButton", source);
                TryHideMemberObject(owner, "submitBugReportButton", source);
                TryCloseAndHideMemberObject(owner, "bugReportPanel", source);

                string typeName = owner.GetType().FullName ?? owner.GetType().Name;
                if (typeName.IndexOf("BugReportPanel", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    TryCloseAndHide(
                        panelLikeObject: owner,
                        source: source,
                        objectName: owner.GetType().Name);
                }
            }
            catch (Exception e)
            {
                MelonLogger.Error($"[LeHud.Hooks]  {source} bug-report disable error: {e.Message}");
            }
        }

        private static object? TryGetMemberValue(object owner, string memberName)
        {
            try
            {
                var ownerType = owner.GetType();
                var key = $"{ownerType.FullName ?? ownerType.Name}|{memberName}";
                Func<object, object?>? getter;
                lock (s_memberGetterCache)
                {
                    if (!s_memberGetterCache.TryGetValue(key, out getter))
                    {
                        getter = BuildMemberGetter(ownerType, memberName);
                        s_memberGetterCache[key] = getter;
                    }
                }

                if (getter != null)
                    return getter(owner);
            }
            catch
            {
                // Best-effort reflection lookup for version-variant IL2CPP wrappers.
            }

            return null;
        }

        private static Func<object, object?>? BuildMemberGetter(Type ownerType, string memberName)
        {
            const System.Reflection.BindingFlags Flags =
                System.Reflection.BindingFlags.Instance
                | System.Reflection.BindingFlags.Public
                | System.Reflection.BindingFlags.NonPublic
                | System.Reflection.BindingFlags.DeclaredOnly;

            for (var t = ownerType; t != null; t = t.BaseType)
            {
                var field = t.GetField(memberName, Flags);
                if (field != null)
                    return owner => field.GetValue(owner);

                var property = t.GetProperty(memberName, Flags);
                if (property == null || property.GetIndexParameters().Length != 0)
                    continue;

                var getter = property.GetGetMethod(nonPublic: true);
                if (getter != null)
                    return owner => getter.Invoke(owner, null);
            }

            return null;
        }

        private static bool TryInvokeClose(object target)
        {
            var targetType = target.GetType();
            Func<object, bool>? closeInvoker;

            lock (s_closeMethodCache)
            {
                if (!s_closeMethodCache.TryGetValue(targetType, out closeInvoker))
                {
                    closeInvoker = BuildCloseInvoker(targetType);
                    s_closeMethodCache[targetType] = closeInvoker;
                }
            }

            if (closeInvoker == null)
                return false;

            try
            {
                return closeInvoker(target);
            }
            catch
            {
                return false;
            }
        }

        private static Func<object, bool>? BuildCloseInvoker(Type targetType)
        {
            const System.Reflection.BindingFlags Flags =
                System.Reflection.BindingFlags.Instance
                | System.Reflection.BindingFlags.Public
                | System.Reflection.BindingFlags.NonPublic
                | System.Reflection.BindingFlags.DeclaredOnly;

            for (var t = targetType; t != null; t = t.BaseType)
            {
                var method = t.GetMethod("Close", Flags, binder: null, types: Type.EmptyTypes, modifiers: null);
                if (method == null)
                    continue;

                return target =>
                {
                    method.Invoke(target, null);
                    return true;
                };
            }

            return null;
        }

        private static bool TrySetInactive(object? target)
        {
            if (target == null)
                return false;

            try
            {
                if (target is GameObject gameObject)
                {
                    gameObject.SetActive(false);
                    return true;
                }

                if (target is Component component && component.gameObject != null)
                {
                    component.gameObject.SetActive(false);
                    return true;
                }

                if (TryGetMemberValue(target, "gameObject") is GameObject nestedGameObject)
                {
                    nestedGameObject.SetActive(false);
                    return true;
                }
            }
            catch
            {
                // Suppress any UI mutation fault to keep hooks non-fatal.
            }

            return false;
        }

        private static void TryCloseAndHide(object? panelLikeObject, string source, string objectName)
        {
            if (panelLikeObject == null)
                return;

            bool changed = false;
            try
            {
                changed = TryInvokeClose(panelLikeObject);
            }
            catch
            {
                // Ignore missing/unstable panel Close wrappers; hiding still handles disablement.
            }

            if (TrySetInactive(panelLikeObject))
                changed = true;

            if (changed)
            {
                LogOnce(
                    key: $"{source}:{objectName}:closed",
                    message: $"[LeHud.Hooks]  {source} closed/hidden {objectName}.");
            }
        }

        private static void TryHideMemberObject(object owner, string memberName, string source)
        {
            var memberObject = TryGetMemberValue(owner, memberName);
            if (memberObject == null)
                return;

            if (TrySetInactive(memberObject))
            {
                LogOnce(
                    key: $"{source}:{owner.GetType().Name}:{memberName}:hidden",
                    message: $"[LeHud.Hooks]  {source} hid {owner.GetType().Name}.{memberName}.");
            }
        }

        private static void TryCloseAndHideMemberObject(object owner, string memberName, string source)
        {
            var memberObject = TryGetMemberValue(owner, memberName);
            if (memberObject == null)
                return;

            TryCloseAndHide(
                panelLikeObject: memberObject,
                source: source,
                objectName: $"{owner.GetType().Name}.{memberName}");
        }
    }
}

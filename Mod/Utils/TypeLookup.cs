using System.Reflection;

namespace Mod.Utils
{
    internal static class TypeLookup
    {
        private static readonly Dictionary<string, Type?> s_cache = new(StringComparer.Ordinal);
        private static readonly object s_cacheLock = new();

        public static Type? FindType(params string[] candidates)
        {
            if (candidates == null || candidates.Length == 0)
                return null;

            foreach (string? candidate in candidates)
            {
                if (string.IsNullOrWhiteSpace(candidate))
                    continue;

                Type? type = FindType(candidate);
                if (type != null)
                    return type;
            }

            return null;
        }

        public static Type? FindType(string typeName)
        {
            if (string.IsNullOrWhiteSpace(typeName))
                return null;

            lock (s_cacheLock)
            {
                if (s_cache.TryGetValue(typeName, out Type? cached))
                    return cached;
            }

            Type? resolved = Resolve(typeName);
            lock (s_cacheLock)
            {
                s_cache[typeName] = resolved;
            }

            return resolved;
        }

        private static Type? Resolve(string typeName)
        {
            try
            {
                Type? direct = Type.GetType(typeName, throwOnError: false);
                if (direct != null)
                    return direct;
            }
            catch
            {
                // Ignore and continue with loaded-assembly lookup.
            }

            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                try
                {
                    Type? type = assemblies[i].GetType(typeName, throwOnError: false, ignoreCase: false);
                    if (type != null)
                        return type;
                }
                catch
                {
                    // Some dynamic/interop assemblies can throw during metadata access.
                }
            }

            return null;
        }
    }
}

using Il2Cpp;
using UnityEngine;

namespace Mod.Game
{
	internal static class PlayerHealthReader
	{
		private static GameObject? s_cachedPlayerObject;
		private static PlayerHealth? s_cachedPlayerHealth;

		public static bool TryGetLocalHealthPercent(out float healthPercent)
		{
			healthPercent = 1f;
			var localPlayer = ObjectManager.GetLocalPlayer();
			if (localPlayer == null)
				return false;

			if (!ReferenceEquals(s_cachedPlayerObject, localPlayer))
			{
				s_cachedPlayerObject = localPlayer;
				s_cachedPlayerHealth = localPlayer.GetComponent<PlayerHealth>();
			}

			if (s_cachedPlayerHealth == null)
				return false;

			try
			{
				healthPercent = s_cachedPlayerHealth.getHealthPercent();
				return !float.IsNaN(healthPercent) && !float.IsInfinity(healthPercent);
			}
			catch
			{
				return false;
			}
		}

		public static void ClearCache()
		{
			s_cachedPlayerObject = null;
			s_cachedPlayerHealth = null;
		}
	}
}

using UnityEngine;
using Mod.Cheats;

namespace Mod.Game
{
	internal class ObjectManager
	{
		private static GameObject? localPlayer;
		private static bool? isOfflineMode;

		public static void AttemptToFindPlayer()
		{
			// Reset detection state before searching
			isOfflineMode = null;

			localPlayer = GameObject.Find("MainPlayer(Clone)"); // offline mode
			if (localPlayer != null)
			{
				isOfflineMode = true;
			}
			else
			{
				localPlayer = GameObject.Find("Local Player(Clone)"); // online mode
				if (localPlayer != null)
				{
					isOfflineMode = false;
				}
			}
		}

		public static void OnSceneLoaded()
		{
			// Clear any cached references when scene changes
			ClearPlayerCache();
			AttemptToFindPlayer();
		}

		public static bool HasPlayer()
		{
			if (localPlayer == null)
			{
				AttemptToFindPlayer();
			}

			return localPlayer != null;
		}

		public static GameObject? GetLocalPlayer()
		{
			if (localPlayer == null)
			{
				AttemptToFindPlayer();
			}

			return localPlayer;
		}

		public static bool IsOfflineMode()
		{
			if (localPlayer == null)
			{
				AttemptToFindPlayer();
			}
			return isOfflineMode.HasValue && isOfflineMode.Value;
		}

		public static bool IsOnlineMode()
		{
			if (localPlayer == null)
			{
				AttemptToFindPlayer();
			}
			return isOfflineMode.HasValue && !isOfflineMode.Value;
		}

		/// <summary>
		/// Clears cached player reference and related component caches
		/// </summary>
		public static void ClearPlayerCache()
		{
			localPlayer = null;
			isOfflineMode = null;
			AutoPotion.ClearCache();
		}
	}
}

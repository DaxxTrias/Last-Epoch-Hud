using UnityEngine;
using Mod.Cheats;

namespace Mod.Game
{
	internal class ObjectManager
	{
		private static GameObject? localPlayer;
		private static bool? isOfflineMode;
		private static int s_lastLookupFrame = -1;
		private static float s_nextLookupAt;
		private static float s_lookupRetryDelay = InitialLookupRetrySeconds;

		private const float InitialLookupRetrySeconds = 0.2f;
		private const float MaxLookupRetrySeconds = 2.0f;

		public static void AttemptToFindPlayer(bool force = false)
		{
			if (!force && !ShouldAttemptLookup())
			{
				return;
			}

			PerformPlayerLookup();
		}

		private static bool ShouldAttemptLookup()
		{
			if (localPlayer != null)
			{
				return false;
			}

			int frame = Time.frameCount;
			if (s_lastLookupFrame == frame)
			{
				return false;
			}

			if (Time.unscaledTime < s_nextLookupAt)
			{
				return false;
			}

			s_lastLookupFrame = frame;
			return true;
		}

		private static void PerformPlayerLookup()
		{
			// Reset detection state before searching.
			isOfflineMode = null;

			localPlayer = GameObject.Find("MainPlayer(Clone)"); // offline mode
			if (localPlayer != null)
			{
				isOfflineMode = true;
				s_lookupRetryDelay = InitialLookupRetrySeconds;
				s_nextLookupAt = 0f;
			}
			else
			{
				localPlayer = GameObject.Find("Local Player(Clone)"); // online mode
				if (localPlayer != null)
				{
					isOfflineMode = false;
					s_lookupRetryDelay = InitialLookupRetrySeconds;
					s_nextLookupAt = 0f;
				}
				else
				{
					s_nextLookupAt = Time.unscaledTime + s_lookupRetryDelay;
					s_lookupRetryDelay = Mathf.Min(MaxLookupRetrySeconds, s_lookupRetryDelay * 2f);
				}
			}
		}

		public static void OnSceneLoaded()
		{
			// Clear any cached references when scene changes
			ClearPlayerCache();
			AttemptToFindPlayer(force: true);
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
			s_lastLookupFrame = -1;
			s_nextLookupAt = 0f;
			s_lookupRetryDelay = InitialLookupRetrySeconds;
			AutoPotion.ClearCache();
		}
	}
}

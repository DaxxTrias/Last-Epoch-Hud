using UnityEngine;
using Mod.Cheats;

namespace Mod.Game
{
    internal class ObjectManager
    {
        private static GameObject? localPlayer;

        public static void AttemptToFindPlayer()
        {
            localPlayer = GameObject.Find("MainPlayer(Clone)"); // offline mode
            if (localPlayer == null)
            {
                localPlayer = GameObject.Find("Local Player(Clone)"); // online mode
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

        /// <summary>
        /// Clears cached player reference and related component caches
        /// </summary>
        public static void ClearPlayerCache()
        {
            localPlayer = null;
            AutoPotion.ClearCache();
        }
    }
}

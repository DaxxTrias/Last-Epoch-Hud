﻿using MelonLoader;
using Mod.Cheats;
using Mod.Cheats.ESP;
using Mod.Game;

[assembly: MelonInfo(typeof(Mod.Mod), "Mod", "0.1.0", "Daxx")]
[assembly: MelonGame("Eleventh Hour Games", "Last Epoch")]

namespace Mod
{
    public static class BuildInfo
    {
        public const string Name = "Mod"; // Name of the Mod.  (MUST BE SET)
        public const string Description = "Mod for Testing"; // Description for the Mod.  (Set as null if none)
        public const string Author = "Daxx"; // Author of the Mod.  (MUST BE SET)
        public const string Company = null; // Company that made the Mod.  (Set as null if none)
        public const string Version = "0.1.0"; // Version of the Mod.  (MUST BE SET)
        public const string DownloadLink = null; // Download Link for the Mod.  (Set as null if none)
    }

    public class Mod : MelonMod
    {
        public override void OnInitializeMelon()
        {
            //MelonLogger.Msg("OnApplicationStart");
        }

        public override void OnLateInitializeMelon() // Runs after OnApplicationStart.
        {
            //MelonLogger.Msg("OnApplicationLateStart");
        }

        public override void OnSceneWasLoaded(int buildindex, string sceneName) // Runs when a Scene has Loaded and is passed the Scene's Build Index and Name.
        {
            //MelonLogger.Msg("OnSceneWasLoaded: " + buildindex.ToString() + " | " + sceneName);
        }

        public override void OnSceneWasInitialized(int buildindex, string sceneName) // Runs when a Scene has Initialized and is passed the Scene's Build Index and Name.
        {
            //MelonLogger.Msg("OnSceneWasInitialized: " + buildindex.ToString() + " | " + sceneName);
            try
            {
                ObjectManager.OnSceneLoaded();
                MapHack.OnSceneWasLoaded();
            }
            catch (System.Exception e)
            {
                MelonLogger.Error(e.ToString());
            }
        }

        public override void OnSceneWasUnloaded(int buildIndex, string sceneName) // Runs when a Scene has Unloaded and is passed the Scene's Build Index and Name.
        {
            //MelonLogger.Msg("OnSceneWasUnloaded: " + buildIndex.ToString() + " | " + sceneName);
        }

        public override void OnUpdate() // Runs once per frame.
        {
            try
            {
                ESP.OnUpdate();
                AutoPotion.OnUpdate();
                Menu.OnUpdate();
            }
            catch (Exception e)
            {
                MelonLogger.Error(e.ToString());
            }

            //MelonLogger.Msg("OnUpdate");
        }

        public override void OnFixedUpdate() // Can run multiple times per frame. Mostly used for Physics.
        {
            //MelonLogger.Msg("OnFixedUpdate");
        }

        public override void OnLateUpdate() // Runs once per frame after OnUpdate and OnFixedUpdate have finished.
        {
            //MelonLogger.Msg("OnLateUpdate");
        }


        public override void OnGUI() // Can run multiple times per frame. Mostly used for Unity's IMGUI.
        {
            try
            {
                Menu.OnGUI();
                ESP.OnGUI();
            }
            catch (Exception e)
            {
                MelonLogger.Error(e.ToString());
            }
        }

        public override void OnApplicationQuit() // Runs when the Game is told to Close.
        {
            //MelonLogger.Msg("OnApplicationQuit");
        }

        public override void OnPreferencesSaved() // Runs when Melon Preferences get saved.
        {
            //MelonLogger.Msg("OnPreferencesSaved");
        }

        public override void OnPreferencesLoaded() // Runs when Melon Preferences get loaded.
        {
            //MelonLogger.Msg("OnPreferencesLoaded");
        }
    }
}

using MelonLoader;
using Mod.Cheats;
using Mod.Cheats.ESP;
using Mod.Game;
using System.Reflection;

[assembly: MelonInfo(typeof(Mod.Mod), "Mod", "0.3.1", "Daxx")]
[assembly: MelonGame("Eleventh Hour Games", "Last Epoch")]

namespace Mod
{
    public static class BuildInfo
    {
        public const string Name = "Mod"; // Name of the Mod.  (MUST BE SET)
        public const string Description = "Mod for Last Epoch"; // Description for the Mod.  (Set as null if none)
        public const string Author = "Daxx"; // Author of the Mod.  (MUST BE SET)
        public const string Company = null; // Company that made the Mod.  (Set as null if none)
        public const string Version = "0.3.1"; // Version of the Mod.  (MUST BE SET)
        public const string DownloadLink = null; // Download Link for the Mod.  (Set as null if none)
    }

    public class Mod : MelonMod
    {
        private static bool isOnGUI = false;

        public override void OnInitializeMelon()
        {
            //MelonLogger.Msg("OnApplicationStart");
        }

        public override void OnLateInitializeMelon() // Runs after OnApplicationStart.
        {
            //MelonLogger.Msg("OnApplicationLateStart");
            Drawing.Initialize();
        }

        public override void OnSceneWasLoaded(int buildindex, string sceneName) // Runs when a Scene has Loaded and is passed the Scene's Build Index and Name.
        {
            //MelonLogger.Msg("OnSceneWasLoaded: " + buildindex.ToString() + " | " + sceneName); // occurs before scene init
            GameMods.FogRemover();
        }

        public override void OnSceneWasInitialized(int buildindex, string sceneName) // Runs when a Scene has Initialized and is passed the Scene's Build Index and Name.
        {
            //MelonLogger.Msg("OnSceneWasInitialized: " + buildindex.ToString() + " | " + sceneName);

            //foreach (MethodInfo mi in typeof(UnityEngine.Physics)
            //    .GetMethods(BindingFlags.Public | BindingFlags.Static))
            //{
            //    MelonLogger.Msg($"Method: {mi.Name} ({string.Join(", ", mi.GetParameters().Select(p => p.ParameterType.Name))})");
            //}

            try
            {
                ObjectManager.OnSceneLoaded();
                MapHack.OnSceneWasLoaded();
                GameMods.FogRemover();
                GameMods.playerLantern();
            }
            catch (Exception e)
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
                if (Settings.timeScale != 1.0f)
                    UnityEngine.Time.timeScale = Settings.timeScale;
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
            if (isOnGUI) return;
            isOnGUI = true;

            try
            {
                Drawing.SetupGuiStyle();
                Menu.OnGUI();
                ESP.OnGUI();
            }
            catch (Exception e)
            {
                MelonLogger.Error(e.ToString());
            }

            isOnGUI = false;
        }

        public override void OnApplicationQuit() // Runs when the Game is told to Close.
        {
            //MelonLogger.Msg("OnApplicationQuit");
            SpriteManager.Cleanup();
            Drawing.Cleanup();
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

using UnityEngine;
using ObjectManager = Mod.Game.ObjectManager;
using Il2Cpp;
using MelonLoader;
using UnityEngine.SceneManagement;

namespace Mod.Cheats
{
    internal static class GameMods
    {
        public static void FogRemover()
        {
            //todo: getting kicked due to idle can cause this to stop triggering. staleness?
            //todo: alternatively we may need to do more checks then just sceneInit (x3)
            if (!Settings.removeFog)
                return;

            if (ObjectManager.GetLocalPlayer() == null)
                return;

            MelonLogger.Msg($"Patching fog");
            // Iterate through all loaded scenes
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded || scene.isSubScene)
                {
                    // Get all root objects in the scene
                    var rootObjects = scene.GetRootGameObjects();
                    foreach (var rootObject in rootObjects)
                    {
                        //TODO: this method doesn't seem to work for the 'snow fog' in arctic biomes
                        var lights = rootObject.GetComponentsInChildren<HxVolumetricLight>(true);
                        foreach (var light in lights)
                        {
                            if (light.dirty)
                                {
                                    light.dirty = false;
                                }
                        }
                    }
                }
            }
        }

        public static void playerLantern()
        {
            if (Settings.playerLantern)
            {
                var player = ObjectManager.GetLocalPlayer();
                if (player == null) return;
                var light = player.GetComponent<Light>();
                if (light != null && !light.enabled)
                {
                    light.intensity = 2.5f;
                    light.range = 40f;
                    light.enabled = true;
                    MelonLogger.Msg($"Found & patched player lantern");
                    return;
                }
                else if (light == null)
                {
                    light = player.AddComponent<Light>();
                    light.intensity = 3f;
                    light.range = 35f;
                    light.enabled = true;
                    MelonLogger.Msg($"Injected player lantern");
                    return;
                }
            }
            else if (!Settings.playerLantern)
            {
                var player = ObjectManager.GetLocalPlayer();
                if (player == null) return;
                var light = player.GetComponent<Light>();
                if (light != null)
                {
                    light.enabled = false;
                    MelonLogger.Msg($"Disabled player lantern");
                }
                else
                {
                    MelonLogger.Msg($"Player lantern not found");
                }
            }
        }
    }
}

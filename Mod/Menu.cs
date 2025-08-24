using Mod.Cheats;
using Mod.Game;
using UnityEngine;
using static UnityEngine.GUI;
using MelonLoader;
using System.Linq;

namespace Mod
{
    internal class Menu
    {
        private static bool guiVisible = false;
        private const float resizeGripSize = 20.0f;
        private static bool isResizing = false;

        public static bool npcDrawingsDropdown = false;
        public static bool npcClassificationsDropdown = false;
        public static bool itemDrawingsDropdown = false;
        public static bool gamePatchesDropdown = false;
        public static bool riskyOptionsDropdown = false;

        public static void DrawModWindow(int windowID)
        {
            GUILayout.BeginVertical();

            npcDrawingsDropdown = GUILayout.Toggle(npcDrawingsDropdown, "NPC Drawings:", "button");
            if (npcDrawingsDropdown)
            {
                foreach (KeyValuePair<string, bool> entry in Settings.npcDrawings)
                {
                    bool result = GUILayout.Toggle(entry.Value, entry.Key);
                    if (result != entry.Value)
                    {
                        Settings.npcDrawings[entry.Key] = result;
                    }
                }
            }

            npcClassificationsDropdown = GUILayout.Toggle(npcClassificationsDropdown, "NPC Classifications:", "button");
            if (npcClassificationsDropdown)
            {
                foreach (KeyValuePair<string, bool> entry in Settings.npcClassifications)
                {
                    bool result = GUILayout.Toggle(entry.Value, entry.Key);
                    if (result != entry.Value)
                    {
                        Settings.npcClassifications[entry.Key] = result;
                    }
                }
            }

            itemDrawingsDropdown = GUILayout.Toggle(itemDrawingsDropdown, "Item Drawings:", "button");
            if (itemDrawingsDropdown)
            {
                bool lootFilterEnabled = Settings.useLootFilter;
                Settings.useLootFilter = GUILayout.Toggle(Settings.useLootFilter, "Use Loot Filter");

                if (lootFilterEnabled)
                {
                    GUI.enabled = false;
                }

                foreach (KeyValuePair<string, bool> entry in Settings.itemDrawings)
                {
                    if (!lootFilterEnabled)
                    {
                        bool result = GUILayout.Toggle(entry.Value, entry.Key);
                        if (result != entry.Value)
                        {
                            Settings.itemDrawings[entry.Key] = result;
                        }
                    }
                }

                GUI.enabled = true;
            }

            GUI.enabled = true;

            gamePatchesDropdown = GUILayout.Toggle(gamePatchesDropdown, "Game Patches:", "button");
            if (gamePatchesDropdown)
            {
                bool previousRemoveFog = Settings.removeFog;
                Settings.removeFog = GUILayout.Toggle(Settings.removeFog, "Remove Fog");
                if (Settings.removeFog != previousRemoveFog)
                    GameMods.FogRemover();

                Settings.cameraZoomUnlock = GUILayout.Toggle(Settings.cameraZoomUnlock, "Camera Zoom Unlock");
                Settings.minimapZoomUnlock = GUILayout.Toggle(Settings.minimapZoomUnlock, "Minimap Zoom Unlock");
                Settings.mapHack = GUILayout.Toggle(Settings.mapHack, "Map Hack");

                bool previousPlayerLantern = Settings.playerLantern;
                Settings.playerLantern = GUILayout.Toggle(Settings.playerLantern, "Player Lantern");
                if (Settings.playerLantern != previousPlayerLantern)
                    GameMods.playerLantern();

                #region spacing
                GUILayout.Space(10);
                #endregion

                #region spacing
                GUILayout.Space(10);
                #endregion

                // Minimap Enemy Circles Settings (moved below Lantern)
                GUI.color = Color.green;
                GUILayout.Label("Monster Type Filters:");
                GUI.color = Color.white;
                Settings.showMagicMonsters = GUILayout.Toggle(Settings.showMagicMonsters, "Show Magic Monsters (Blue)");
                Settings.showRareMonsters = GUILayout.Toggle(Settings.showRareMonsters, "Show Rare Monsters (Yellow)");
                Settings.showWhiteMonsters = GUILayout.Toggle(Settings.showWhiteMonsters, "Show White Monsters");
            }

            riskyOptionsDropdown = GUILayout.Toggle(riskyOptionsDropdown, "Risky Options:", "button");
            if (riskyOptionsDropdown)
            {
                GUILayout.Label("These options are provided at your own risk.");
                #region spacing
                GUILayout.Space(10);
                #endregion

                GUILayout.Label("TimeScale: " + Settings.timeScale.ToString("F1"));
                Settings.timeScale = GUILayout.HorizontalSlider(Settings.timeScale, 0.1f, 6.0f);
                #region spacing
                GUILayout.Space(10);
                #endregion

                if (!ObjectManager.IsOfflineMode())
                {
                    GUILayout.Label("Allow Any Waypoint: unavailable in online mode");
                }
                else
                {
                    Settings.useAnyWaypoint = GUILayout.Toggle(Settings.useAnyWaypoint, "Allow Any Waypoint");
                }
                //Settings.pickupCrafting = GUILayout.Toggle(Settings.pickupCrafting, "Pickup Crafting Items");

                // Settings.debugESPNames = GUILayout.Toggle(Settings.debugESPNames, "Debug ESP Names");

                #region spacing
                GUILayout.Space(10);
                #endregion

                // Anti-Idle (Synthetic Keepalive)
                Settings.useAntiIdle = GUILayout.Toggle(Settings.useAntiIdle, "Anti-Idle (Synthetic Keepalive)");
                if (Settings.useAntiIdle)
                {
                    GUILayout.Label("Keepalive Interval (s): " + Settings.keepAliveInterval.ToString("F0"));
                    Settings.keepAliveInterval = GUILayout.HorizontalSlider(Settings.keepAliveInterval, 10f, 120f);

                    GUILayout.Label("Anti-Idle Action Interval (s): " + Settings.antiIdleInterval.ToString("F0"));
                    Settings.antiIdleInterval = GUILayout.HorizontalSlider(Settings.antiIdleInterval, 30f, 300f);

                    // Suppression controls
                    Settings.suppressKeepAliveOnActivity = GUILayout.Toggle(Settings.suppressKeepAliveOnActivity, "Pause Keepalive On Activity");
                    if (Settings.suppressKeepAliveOnActivity)
                    {
                        GUILayout.Label("Activity Suppression (s): " + Settings.activitySuppressionSeconds.ToString("F0"));
                        Settings.activitySuppressionSeconds = GUILayout.HorizontalSlider(Settings.activitySuppressionSeconds, 30f, 300f);

                        GUILayout.Label("Scene Change Suppression (s): " + Settings.sceneChangeSuppressionSeconds.ToString("F0"));
                        Settings.sceneChangeSuppressionSeconds = GUILayout.HorizontalSlider(Settings.sceneChangeSuppressionSeconds, 30f, 300f);

                        GUILayout.Label("Network Activity Suppression (s): " + Settings.networkActivitySuppressionSeconds.ToString("F0"));
                        Settings.networkActivitySuppressionSeconds = GUILayout.HorizontalSlider(Settings.networkActivitySuppressionSeconds, 5f, 120f);
                    }
                }
            }

            #region spacing
            GUILayout.Space(10);
            #endregion

            GUILayout.Label("Draw Distance: " + Settings.drawDistance.ToString("F1"));
            Settings.drawDistance = GUILayout.HorizontalSlider(Settings.drawDistance, 0.0f, 300.0f);

            // Hide AutoPotion when offline
            if (ObjectManager.IsOfflineMode())
            {
                GUILayout.Label("Auto HP Pot: unavailable in offline mode");
            }
            else
            {
                Settings.useAutoPot = GUILayout.Toggle(Settings.useAutoPot, "Auto HP Pot");
                if (Settings.useAutoPot)
                {
                    GUILayout.Label("Auto HP Pot Threshold %: " + Settings.autoHealthPotion.ToString("F1"));
                    Settings.autoHealthPotion = GUILayout.HorizontalSlider(Settings.autoHealthPotion, 0.0f, 100.0f);
                    
                    GUILayout.Label("Auto HP Pot Cooldown: " + Settings.autoPotionCooldown.ToString("F1") + "s");
                    Settings.autoPotionCooldown = GUILayout.HorizontalSlider(Settings.autoPotionCooldown, 0.1f, 5.0f);
                }
            }

            // Auto-Disconnect (placed near AutoPotion)
            Settings.useAutoDisconnect = GUILayout.Toggle(Settings.useAutoDisconnect, "Auto Disconnect on Low HP");
            if (Settings.useAutoDisconnect)
            {
                GUILayout.Label("Auto Disconnect Threshold %: " + Settings.autoDisconnectHealthPercent.ToString("F1"));
                Settings.autoDisconnectHealthPercent = GUILayout.HorizontalSlider(Settings.autoDisconnectHealthPercent, 0.0f, 100.0f);

                GUILayout.Label("Auto Disconnect Cooldown: " + Settings.autoDisconnectCooldownSeconds.ToString("F0") + "s");
                Settings.autoDisconnectCooldownSeconds = GUILayout.HorizontalSlider(Settings.autoDisconnectCooldownSeconds, 1f, 60f);

                Settings.autoDisconnectOnlyWhenNoPotions = GUILayout.Toggle(Settings.autoDisconnectOnlyWhenNoPotions, "Only Disconnect When Out of Potions");
            }

            #region spacing
            GUILayout.Space(10);
            #endregion

            // Manual settings import/export (JSON)
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Import Settings (JSON)"))
            {
                if (SettingsConfig.ImportFromStandalone())
                {
                    MelonLogger.Msg("[Settings] Imported settings from JSON");
                    // Apply immediate effects for certain patches
                    GameMods.FogRemover();
                    GameMods.playerLantern();
                }
                else
                {
                    MelonLogger.Msg("[Settings] No JSON config found to import");
                }
            }
            if (GUILayout.Button("Export Settings (JSON)"))
            {
                if (SettingsConfig.ExportToStandalone())
                {
                    MelonLogger.Msg("[Settings] Exported settings to JSON");
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();

            Rect resizeGripRect = new Rect(
                windowRect.width - resizeGripSize, windowRect.height - resizeGripSize, resizeGripSize, resizeGripSize);
            GUI.Box(resizeGripRect, "");

            GUI.DragWindow(new Rect(0, 0, 10000, 20));

            ProcessResizing(resizeGripRect, windowID);
        }

        private static void ProcessResizing(Rect resizeGripRect, int windowID)
        {
            Event currentEvent = Event.current;
            switch (currentEvent.type)
            {
                case EventType.MouseDown:
                    // Check if the mouse is within the resize grip area
                    if (resizeGripRect.Contains(currentEvent.mousePosition))
                    {
                        currentEvent.Use(); // Mark the event as used
                        isResizing = true; // Set a flag indicating that we're resizing
                    }
                    break;

                case EventType.MouseUp:
                    isResizing = false; // Clear the resizing flag on mouse up
                    break;

                case EventType.MouseDrag:
                    if (isResizing)
                    {
                        // Directly adjust windowRect for resizing
                        windowRect.width += currentEvent.delta.x;
                        windowRect.height += currentEvent.delta.y;
                        // Enforce minimum size constraints
                        windowRect.width = Mathf.Max(windowRect.width, 250);
                        windowRect.height = Mathf.Max(windowRect.height, 200);
                        currentEvent.Use();
                    }
                    break;
            }
        }

        public static Rect windowRect = new Rect(20, 20, 250, 700);

        public static void OnGUI()
        {
            if (guiVisible)
            {
                windowRect = GUI.Window(0, windowRect, (WindowFunction)DrawModWindow, "LaSt EpOP");
            }
        }

        public static void OnUpdate()
        {
            if (Input.GetKeyDown(KeyCode.Insert))
            {
                bool wasVisible = guiVisible;
                guiVisible = !guiVisible;
                // If closing the menu, immediately persist current settings to MelonPreferences
                if (wasVisible && !guiVisible)
                {
                    SettingsConfig.ApplyToPreferencesFromSettings();
                    SettingsConfig.Save();
                    MelonLogger.Msg("[LEHud] Preferences Saved!");
                }
            }

            // Debug key for auto-potion system (F12)
            if (Input.GetKeyDown(KeyCode.F12))
            {
                AutoPotion.LogDebugInfo();
            }
        }
    }
}

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

                #region spacing
                GUILayout.Space(10);
                #endregion

                // Minimap Enemy Circles Settings
                GUI.color = Color.green;
                GUILayout.Label("Monster Type Filters:");
                GUI.color = Color.white;
                Settings.showMagicMonsters = GUILayout.Toggle(Settings.showMagicMonsters, "Show Magic Monsters (Blue)");
                Settings.showRareMonsters = GUILayout.Toggle(Settings.showRareMonsters, "Show Rare Monsters (Yellow)");
                Settings.showWhiteMonsters = GUILayout.Toggle(Settings.showWhiteMonsters, "Show White Monsters");

                #region spacing
                GUILayout.Space(10);
                #endregion

                GUILayout.Label("Minimap Scale: " + Settings.minimapScale.ToString("F1"));
                Settings.minimapScale = GUILayout.HorizontalSlider(Settings.minimapScale, 1.0f, 15.0f);

                bool previousPlayerLantern = Settings.playerLantern;
                Settings.playerLantern = GUILayout.Toggle(Settings.playerLantern, "Player Lantern");
                if (Settings.playerLantern != previousPlayerLantern)
                    GameMods.playerLantern();
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
                guiVisible = !guiVisible;
            }

            // Debug key for auto-potion system (F12)
            if (Input.GetKeyDown(KeyCode.F12))
            {
                AutoPotion.LogDebugInfo();
            }
        }
    }
}

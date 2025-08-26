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
		// new dropdowns
		public static bool automationDropdown = false;
		public static bool antiIdleSubDropdown = false;
		public static bool espDropdown = false;

		public static void DrawModWindow(int windowID)
		{
			GUILayout.BeginVertical();

			// ESP grouping for drawing-related options
			espDropdown = GUILayout.Toggle(espDropdown, "ESP:", "button");
			if (espDropdown)
			{
				npcDrawingsDropdown = GUILayout.Toggle(npcDrawingsDropdown, "NPC Alignment:", "button");
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

				npcClassificationsDropdown = GUILayout.Toggle(npcClassificationsDropdown, "NPC Rarity:", "button");
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

				itemDrawingsDropdown = GUILayout.Toggle(itemDrawingsDropdown, "Item Filters:", "button");
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

				// Draw Distance for drawing categories
				GUILayout.Label("Draw Distance: " + Settings.drawDistance.ToString("F1"));
				Settings.drawDistance = GUILayout.HorizontalSlider(Settings.drawDistance, 0.0f, 300.0f);
			}

			GUI.enabled = true;

			// Automation Section
			automationDropdown = GUILayout.Toggle(automationDropdown, "Automation:", "button");
			if (automationDropdown)
			{
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

				Settings.useAutoDisconnect = GUILayout.Toggle(Settings.useAutoDisconnect, "Auto Disconnect on Low HP");
				if (Settings.useAutoDisconnect)
				{
					GUILayout.Label("Auto Disconnect Threshold %: " + Settings.autoDisconnectHealthPercent.ToString("F1"));
					Settings.autoDisconnectHealthPercent = GUILayout.HorizontalSlider(Settings.autoDisconnectHealthPercent, 0.0f, 100.0f);

					GUILayout.Label("Auto Disconnect Cooldown: " + Settings.autoDisconnectCooldownSeconds.ToString("F0") + "s");
					Settings.autoDisconnectCooldownSeconds = GUILayout.HorizontalSlider(Settings.autoDisconnectCooldownSeconds, 1f, 60f);

					Settings.autoDisconnectOnlyWhenNoPotions = GUILayout.Toggle(Settings.autoDisconnectOnlyWhenNoPotions, "Only Disconnect When Out of Potions");
				}
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

				// Enemy Radar (Minimap for now)
				GUI.color = Color.green;
				GUILayout.Label("Radar Monster Type Filters:");
				GUI.color = Color.white;
				Settings.showMagicMonsters = GUILayout.Toggle(Settings.showMagicMonsters, "Show Magic Monsters");
				Settings.showRareMonsters = GUILayout.Toggle(Settings.showRareMonsters, "Show Rare Monsters");
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

				// Anti-Idle (nested)
				antiIdleSubDropdown = GUILayout.Toggle(antiIdleSubDropdown, "Anti-Idle", "button");
				if (antiIdleSubDropdown)
				{
					Settings.useSimpleAntiIdle = GUILayout.Toggle(Settings.useSimpleAntiIdle, "Enable Anti-Idle");
					if (Settings.useSimpleAntiIdle)
					{
						GUILayout.Label("Pulse Interval (s): " + Settings.simpleAntiIdleInterval.ToString("F0"));
						Settings.simpleAntiIdleInterval = GUILayout.HorizontalSlider(Settings.simpleAntiIdleInterval, 60f, 900f);

						// Suppression controls (shared)
						Settings.suppressKeepAliveOnActivity = GUILayout.Toggle(Settings.suppressKeepAliveOnActivity, "Suppress When Actively Playing");
						if (Settings.suppressKeepAliveOnActivity)
						{
							GUILayout.Label("Activity Suppression (s): " + Settings.activitySuppressionSeconds.ToString("F0"));
							Settings.activitySuppressionSeconds = GUILayout.HorizontalSlider(Settings.activitySuppressionSeconds, 5f, 300f);

							GUILayout.Label("Scene Change Suppression (s): " + (Settings.sceneChangeSuppressionSeconds <= 0f ? "Disabled" : Settings.sceneChangeSuppressionSeconds.ToString("F0")));
							Settings.sceneChangeSuppressionSeconds = GUILayout.HorizontalSlider(Settings.sceneChangeSuppressionSeconds, 0f, 300f);

							// GUILayout.Label("Network Activity Suppression (s): " + (Settings.networkActivitySuppressionSeconds <= 0f ? "Disabled" : Settings.networkActivitySuppressionSeconds.ToString("F0")));
							// Settings.networkActivitySuppressionSeconds = GUILayout.HorizontalSlider(Settings.networkActivitySuppressionSeconds, 0f, 120f);
						}
					}
				}
				// Hide legacy synthetic keepalive controls
			}

			#region spacing
			GUILayout.Space(10);
			#endregion

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
					// Notify Anti-Idle suppression of user presence
					AntiIdleSystem.OnMenuClosed();
				}
				else if (!wasVisible && guiVisible)
				{
					// Notify Anti-Idle suppression of user presence
					AntiIdleSystem.OnMenuOpened();
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

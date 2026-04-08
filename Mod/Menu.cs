using System.Linq;
using UnityEngine;
using static UnityEngine.GUI;
using MelonLoader;
using Mod.Cheats;
using Mod.Cheats.ESP;
using Mod.Game;
using Mod.Utils;

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
		public static bool dpsMeterSubDropdown = false;
		public static bool espDropdown = false;
		public static bool specialsSubDropdown = false; // Placeholder for future per-special options
#if DEBUG
		public static bool debugToolsDropdown = false;
#endif

		public static void DrawModWindow(int windowID)
		{
			GUILayout.BeginVertical();

			// ESP grouping for drawing-related options
			espDropdown = GUILayout.Toggle(espDropdown, "ESP:", "button");
			if (espDropdown)
			{
				// Specials sub-selection
				specialsSubDropdown = GUILayout.Toggle(specialsSubDropdown, "Specials", "button");
				if (specialsSubDropdown)
				{
					// Per-special toggles
					Settings.espShowLootLizards = GUILayout.Toggle(Settings.espShowLootLizards, "Show Loot Lizards");
					Settings.espShowChampions = GUILayout.Toggle(Settings.espShowChampions, "Show Champions");
					Settings.espShowChests = GUILayout.Toggle(Settings.espShowChests, "Show Chests");
					Settings.espShowShrines = GUILayout.Toggle(Settings.espShowShrines, "Show Shrines");
					Settings.espShowRunePrisons = GUILayout.Toggle(Settings.espShowRunePrisons, "Show Rune Prisons");
					
					GUILayout.Space(6);
					Settings.showESPLines = GUILayout.Toggle(Settings.showESPLines, "Show ESP Lines");
					Settings.showESPLabels = GUILayout.Toggle(Settings.showESPLabels, "Show ESP Labels");
					
					GUILayout.Label("Chest ESP Vertical Cull (m): " + Settings.espVerticalCullMeters.ToString("F0"));
					Settings.espVerticalCullMeters = GUILayout.HorizontalSlider(Settings.espVerticalCullMeters, 0f, 200f);
				}

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

#if DEBUG
			debugToolsDropdown = GUILayout.Toggle(debugToolsDropdown, "DEBUG Tools:", "button");
			if (debugToolsDropdown)
			{
				Settings.enableNetworkDiagnostics = GUILayout.Toggle(Settings.enableNetworkDiagnostics, "Enable Network Diagnostics (Verbose)");
                if (Settings.enableNetworkDiagnostics)
                {
                    GUILayout.Label("Captures deep ClientNetworkService breadcrumbs during connect/load troubleshooting.");
                }
				Settings.debugEnableDiagnostics = GUILayout.Toggle(Settings.debugEnableDiagnostics, "Enable Diagnostics");
				if (Settings.debugEnableDiagnostics)
				{
					Settings.debugShowLocalPlayerPanel = GUILayout.Toggle(Settings.debugShowLocalPlayerPanel, "Show Local Player Panel");
					Settings.debugShowLocalPlayerWorldLabel = GUILayout.Toggle(Settings.debugShowLocalPlayerWorldLabel, "Show Local Player World Label");
					Settings.debugDrawAllManagerActors = GUILayout.Toggle(Settings.debugDrawAllManagerActors, "Draw All ActorManager Actors (No Sorting)");
					Settings.debugDrawAllGroundItems = GUILayout.Toggle(Settings.debugDrawAllGroundItems, "Draw All GroundItemVisuals");
					Settings.debugDrawAllGroundGold = GUILayout.Toggle(Settings.debugDrawAllGroundGold, "Draw All GroundGoldVisuals");
					Settings.debugDrawManagerLines = GUILayout.Toggle(Settings.debugDrawManagerLines, "Draw Debug Lines To Targets");
					Settings.debugIgnoreDistanceCulling = GUILayout.Toggle(Settings.debugIgnoreDistanceCulling, "Ignore Draw Distance Culling");

					GUILayout.Label("Debug Max Entries/System: " + Settings.debugMaxEntriesPerSystem.ToString());
					var debugMax = GUILayout.HorizontalSlider(Settings.debugMaxEntriesPerSystem, 10f, 500f);
					Settings.debugMaxEntriesPerSystem = Mathf.RoundToInt(debugMax);
				}
			}
#endif

			// Automation Section
			automationDropdown = GUILayout.Toggle(automationDropdown, "Automation:", "button");
			if (automationDropdown)
			{
				Settings.useAutoPot = GUILayout.Toggle(Settings.useAutoPot, "Auto HP Pot");
				if (Settings.useAutoPot)
				{
					GUILayout.Label("Auto HP Pot Threshold %: " + Settings.autoHealthPotion.ToString("F1"));
					Settings.autoHealthPotion = GUILayout.HorizontalSlider(Settings.autoHealthPotion, 0.0f, 100.0f);

					GUILayout.Label("Auto HP Pot Cooldown: " + Settings.autoPotionCooldown.ToString("F1") + "s");
					Settings.autoPotionCooldown = GUILayout.HorizontalSlider(Settings.autoPotionCooldown, 0.1f, 5.0f);
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

				dpsMeterSubDropdown = GUILayout.Toggle(dpsMeterSubDropdown, "DPS Meter", "button");
				if (dpsMeterSubDropdown)
				{
					bool wasEnabled = Settings.enableDpsMeter;
					Settings.enableDpsMeter = GUILayout.Toggle(Settings.enableDpsMeter, "Enable DPS Meter Overlay");
					Settings.enableDpsMeterOnlineRaw = GUILayout.Toggle(
						Settings.enableDpsMeterOnlineRaw,
						"Allow Online Raw Source");
					Settings.dpsMeterPanelLocked = GUILayout.Toggle(
						Settings.dpsMeterPanelLocked,
						"Lock DPS Panel Position/Size");
					Settings.enableDamageNumberDiagnostics = GUILayout.Toggle(
						Settings.enableDamageNumberDiagnostics,
						"Enable DamageNumber Diagnostics (Verbose Logs)");
					if (wasEnabled && !Settings.enableDpsMeter)
					{
						DpsMeter.Reset();
					}

					if (Settings.enableDpsMeter)
					{
						GUILayout.Label("DPS Window (s): " + Settings.dpsMeterWindowSeconds.ToString("F1"));
						Settings.dpsMeterWindowSeconds = GUILayout.HorizontalSlider(Settings.dpsMeterWindowSeconds, 1f, 20f);

						Settings.dpsMeterAutoReset = GUILayout.Toggle(Settings.dpsMeterAutoReset, "Auto Reset After Inactivity");
						if (Settings.dpsMeterAutoReset)
						{
							GUILayout.Label("Inactivity Reset (s): " + Settings.dpsMeterInactivityResetSeconds.ToString("F1"));
							Settings.dpsMeterInactivityResetSeconds = GUILayout.HorizontalSlider(Settings.dpsMeterInactivityResetSeconds, 2f, 60f);
						}

						if (GUILayout.Button("Reset DPS Stats"))
						{
							DpsMeter.Reset();
						}
						if (GUILayout.Button("Reset DPS Panel Layout"))
						{
							DpsMeter.ResetPanelLayout();
						}

						if (!ObjectManager.IsOfflineMode() && Settings.enableDpsMeterOnlineRaw)
						{
							GUILayout.Space(4f);
							GUILayout.Label("Online Ownership Filter");
							if (GUILayout.Button("Filter Mode: " + DescribeDpsFilterMode(Settings.dpsMeterOnlineFilterMode)))
							{
								Settings.dpsMeterOnlineFilterMode = (Settings.dpsMeterOnlineFilterMode + 1) % 3;
							}

							GUILayout.Label("Near Radius (incoming bias): " + Settings.dpsMeterNearPlayerMeters.ToString("F1") + "m");
							Settings.dpsMeterNearPlayerMeters = GUILayout.HorizontalSlider(Settings.dpsMeterNearPlayerMeters, 0.5f, 6f);

							float minFar = Mathf.Max(Settings.dpsMeterNearPlayerMeters + 0.2f, 0.7f);
							GUILayout.Label("Far Radius (outgoing bias): " + Settings.dpsMeterFarPlayerMeters.ToString("F1") + "m");
							Settings.dpsMeterFarPlayerMeters = GUILayout.HorizontalSlider(Settings.dpsMeterFarPlayerMeters, minFar, 12f);

							GUILayout.Label("HP Drop Correlation Window: " + Settings.dpsMeterHpDropCorrelationMs.ToString("F0") + "ms");
							Settings.dpsMeterHpDropCorrelationMs = GUILayout.HorizontalSlider(Settings.dpsMeterHpDropCorrelationMs, 50f, 1000f);
						}
					}

					if (!Settings.dpsMeterPanelLocked)
					{
						GUILayout.Label("DPS panel unlocked: drag title to move, bottom-right grip to resize.");
					}
					if (!ObjectManager.IsOfflineMode() && !Settings.enableDpsMeterOnlineRaw)
					{
						GUILayout.Label("Online meter disabled. Enable 'Online Raw Source' to collect from damage-number text.");
					}
					if (!ObjectManager.IsOfflineMode() && Settings.enableDpsMeterOnlineRaw)
					{
						GUILayout.Label("Online Raw can be filtered by proximity + local HP-drop correlation.");
					}
					if (Settings.enableDamageNumberDiagnostics)
					{
						GUILayout.Label("DamageNumber diagnostics are active. Check Melon logs for renderer summaries.");
					}
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
				Settings.mapHack = GUILayout.Toggle(Settings.mapHack, "Map Hack (Boost RevealRadius 14,000%)");

				bool previousPlayerLantern = Settings.playerLantern;
				Settings.playerLantern = GUILayout.Toggle(Settings.playerLantern, "Player Lantern");
				if (Settings.playerLantern != previousPlayerLantern)
					GameMods.playerLantern();

				Settings.blockMenuInputWhenOpen = GUILayout.Toggle(
					Settings.blockMenuInputWhenOpen,
					"Block Game Input While Menu Open (Keyboard + Mouse)");

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
				Settings.showWhiteMonsters = GUILayout.Toggle(Settings.showWhiteMonsters, "Show White Monsters");
				Settings.showMagicMonsters = GUILayout.Toggle(Settings.showMagicMonsters, "Show Magic Monsters");
				Settings.showRareMonsters = GUILayout.Toggle(Settings.showRareMonsters, "Show Rare Monsters");
				Settings.showBossMonsters = GUILayout.Toggle(Settings.showBossMonsters, "Show Boss Monsters");
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
						Settings.forceIsIdleFalseFallback = GUILayout.Toggle(
							Settings.forceIsIdleFalseFallback,
							"Force IsIdle FALSE Fallback (high risk)");

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
			Box(resizeGripRect, "");

			DragWindow(new Rect(0, 0, 10000, 20));

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

		private static string DescribeDpsFilterMode(int mode)
		{
			return mode switch
			{
				1 => "Likely Outgoing",
				2 => "Likely Incoming",
				_ => "All Visible"
			};
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

			// Optional input-blocking: blocks gameplay keyboard + mouse while menu is visible.
			bool shouldBlockGameInput = Settings.blockMenuInputWhenOpen && guiVisible;
			EpochInputManagerBridge.TrySetMenuInputBlocked(shouldBlockGameInput);

			// Debug key for auto-potion system (F12)
			if (Input.GetKeyDown(KeyCode.F12))
			{
				AutoPotion.LogDebugInfo();
			}

#if DEBUG
			// Debug key for actor/local-player correlation diagnostics (F11)
			if (Input.GetKeyDown(KeyCode.F11))
			{
				DebugDiagnostics.LogCorrelationSnapshot();
			}
#endif
		}
	}
}

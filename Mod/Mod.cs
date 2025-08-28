using MelonLoader;
using Mod.Cheats;
using Mod.Cheats.ESP;
using Mod.Game;
using System.Reflection;
using HarmonyLib;
using Il2CppLidgren.Network;
using Il2CppSystem.Net;

[assembly: MelonInfo(typeof(Mod.Mod), "LEHud", "0.4.0", "Daxx")]
[assembly: MelonGame("Eleventh Hour Games", "Last Epoch")]

namespace Mod
{
	public static class BuildInfo
	{
		public const string Name = "LEHud"; // Name of the Mod.  (MUST BE SET)
		public const string Description = "Hud mod for Last Epoch"; // Description for the Mod.  (Set as null if none)
		public const string Author = "Daxx"; // Author of the Mod.  (MUST BE SET)
		public const string Company = null; // Company that made the Mod.  (Set as null if none)
		public const string Version = "0.4.0"; // Version of the Mod.  (MUST BE SET)
		public const string DownloadLink = null; // Download Link for the Mod.  (Set as null if none)
	}

	public class Mod : MelonMod
	{
		private static bool isOnGUI = false;
		private const string HarmonyId = "LEHud.Patches";
		private static HarmonyLib.Harmony? s_harmony;

		public override void OnInitializeMelon()
		{
			try
			{
				// Initialize preferences and load into Settings before applying patches
				SettingsConfig.Init();
				SettingsConfig.LoadIntoSettings();

				s_harmony = new HarmonyLib.Harmony(HarmonyId);
				s_harmony.PatchAll(typeof(Mod).Assembly);
				MelonLogger.Msg("[LEHud] Harmony patches applied");
				VerifyNetworkingTargets();
			}
			catch (System.Exception e)
			{
				MelonLogger.Error($"[LEHud] Harmony init failed: {e.Message}");
			}
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
			//	.GetMethods(BindingFlags.Public | BindingFlags.Static))
			//{
			//	MelonLogger.Msg($"Method: {mi.Name} ({string.Join(", ", mi.GetParameters().Select(p => p.ParameterType.Name))})");
			//}

			try
			{
				ObjectManager.OnSceneLoaded();
				MapHack.OnSceneWasLoaded();
				GameMods.FogRemover();
				GameMods.playerLantern();

				// Inform AntiIdleSystem to suppress synthetic keepalive briefly after scene load
				AntiIdleSystem.OnSceneChanged();
				AutoDisconnect.ClearCache();
				Shrines.OnSceneChanged();
				RunePrisons.OnSceneChanged();
				Chests.OnSceneChanged();
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
                MinimapEnemyCircles.Update();
				AntiIdleSystem.OnUpdate(); // Add anti-idle system
				AutoDisconnect.OnUpdate();
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
			catch (System.Exception e)
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
			try
			{
				// Persist current runtime settings to preferences on quit
				SettingsConfig.ApplyToPreferencesFromSettings();
				SettingsConfig.Save();

				s_harmony?.UnpatchSelf();
				MelonLogger.Msg("[LEHud] Harmony patches unpatched on quit");
			}
			catch (Exception e)
			{
				MelonLogger.Error($"[LEHud] Harmony unpatch failed: {e.Message}");
			}
		}

		public override void OnPreferencesSaved() // Runs when Melon Preferences get saved.
		{
			//MelonLogger.Msg("OnPreferencesSaved");
		}

		public override void OnPreferencesLoaded() // Runs when Melon Preferences get loaded.
		{
			try
			{
				if (!SettingsConfig.IsInitialized)
					return;
				SettingsConfig.LoadIntoSettings();
				MelonLogger.Msg("[LEHud] Preferences loaded into Settings");
			}
			catch (Exception e)
			{
				MelonLogger.Error($"[LEHud] Preferences load error: {e.Message}");
			}
		}

		private static void VerifyNetworkingTargets()
		{
			try
			{
				var nmcType = typeof(NetMultiClient);
				var npType = typeof(NetPeer);
				MelonLogger.Msg($"[LEHud] Verify: NetMultiClient type = {nmcType.Name}, NetPeer type = {npType.Name}");

				var connect = AccessTools.Method(nmcType, "Connect", new[] { typeof(IPEndPoint), typeof(NetOutgoingMessage) });
				MelonLogger.Msg($"[LEHud] Verify: NetMultiClient.Connect found = {connect != null}");

				var disconnect = AccessTools.Method(nmcType, "Disconnect", new[] { typeof(string) });
				MelonLogger.Msg($"[LEHud] Verify: NetMultiClient.Disconnect found = {disconnect != null}");

				var sendMessage = AccessTools.Method(nmcType, "SendMessage", new[] { typeof(NetOutgoingMessage), typeof(NetDeliveryMethod), typeof(int) });
				MelonLogger.Msg($"[LEHud] Verify: NetMultiClient.SendMessage found = {sendMessage != null}");

				var getConnStatus = AccessTools.Method(nmcType, "get_ConnectionStatus");
				MelonLogger.Msg($"[LEHud] Verify: NetMultiClient.get_ConnectionStatus found = {getConnStatus != null}");

				var heartbeatField = AccessTools.Field(npType, "m_lastHeartbeat");
				MelonLogger.Msg($"[LEHud] Verify: NetPeer.m_lastHeartbeat field found = {heartbeatField != null}");

				// NetTime verification
				try
				{
					var netTimeType = AccessTools.TypeByName("Il2CppLidgren.Network.NetTime") ?? AccessTools.TypeByName("Lidgren.Network.NetTime");
					if (netTimeType != null)
					{
						var nowProp = netTimeType.GetProperty("Now", BindingFlags.Public | BindingFlags.Static) ?? netTimeType.GetProperty("get_Now", BindingFlags.Public | BindingFlags.Static);
						var nowMethod = nowProp == null ? netTimeType.GetMethod("get_Now", BindingFlags.Public | BindingFlags.Static) : null;
						bool found = nowProp != null || nowMethod != null;
						MelonLogger.Msg($"[LEHud] Verify: NetTime.Now found = {found} ({netTimeType.FullName})");
					}
					else
					{
						MelonLogger.Msg("[LEHud] Verify: NetTime type not found");
					}
				}
				catch (System.Exception ex)
				{
					MelonLogger.Error($"[LEHud] Verify: NetTime check error: {ex.Message}");
				}
			}
			catch (System.Exception e)
			{
				MelonLogger.Error($"[LEHud] VerifyNetworkingTargets error: {e.Message}");
			}
		}
	}
}

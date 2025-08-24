using MelonLoader;
using System.Collections.Generic;

namespace Mod
{
	internal static class SettingsConfig
	{
		private const string CatGeneral = "LEHud.General";
		private const string CatPatches = "LEHud.Patches";
		private const string CatAutoPotion = "LEHud.AutoPotion";
		private const string CatAntiIdle = "LEHud.AntiIdle";
		private const string CatAutoDisconnect = "LEHud.AutoDisconnect";
		private const string CatMinimap = "LEHud.Minimap";
		private const string CatNPC = "LEHud.NPC";
		private const string CatItems = "LEHud.Items";

		// Categories
		private static MelonPreferences_Category? _general;
		private static MelonPreferences_Category? _patches;
		private static MelonPreferences_Category? _autoPotion;
		private static MelonPreferences_Category? _antiIdle;
		private static MelonPreferences_Category? _autoDisconnect;
		private static MelonPreferences_Category? _minimap;
		private static MelonPreferences_Category? _npc;
		private static MelonPreferences_Category? _items;

		// Entries - General
		private static MelonPreferences_Entry<bool>? _mapHack;
		private static MelonPreferences_Entry<float>? _drawDistance;
		private static MelonPreferences_Entry<float>? _timeScale;
		private static MelonPreferences_Entry<bool>? _useLootFilter;

		// Entries - Patches
		private static MelonPreferences_Entry<bool>? _removeFog;
		private static MelonPreferences_Entry<bool>? _cameraZoomUnlock;
		private static MelonPreferences_Entry<bool>? _minimapZoomUnlock;
		private static MelonPreferences_Entry<bool>? _playerLantern;
		private static MelonPreferences_Entry<bool>? _useAnyWaypoint;

		// Entries - AutoPotion
		private static MelonPreferences_Entry<bool>? _useAutoPot;
		private static MelonPreferences_Entry<float>? _autoHealthPotion;
		private static MelonPreferences_Entry<float>? _autoPotionCooldown;

		// Entries - AntiIdle
		private static MelonPreferences_Entry<bool>? _useAntiIdle;
		private static MelonPreferences_Entry<float>? _antiIdleInterval;
		private static MelonPreferences_Entry<bool>? _useSyntheticKeepAlive;
		private static MelonPreferences_Entry<float>? _keepAliveInterval;
		private static MelonPreferences_Entry<bool>? _suppressKeepAliveOnActivity;
		private static MelonPreferences_Entry<float>? _activitySuppressionSeconds;
		private static MelonPreferences_Entry<float>? _sceneChangeSuppressionSeconds;
		private static MelonPreferences_Entry<float>? _networkActivitySuppressionSeconds;

		// Entries - AutoDisconnect
		private static MelonPreferences_Entry<bool>? _useAutoDisconnect;
		private static MelonPreferences_Entry<float>? _autoDisconnectHealthPercent;
		private static MelonPreferences_Entry<float>? _autoDisconnectCooldownSeconds;
		private static MelonPreferences_Entry<bool>? _autoDisconnectOnlyWhenNoPotions;

		// Entries - Minimap
		private static MelonPreferences_Entry<bool>? _showMinimapEnemyCircles;
		private static MelonPreferences_Entry<float>? _minimapCircleSize;
		private static MelonPreferences_Entry<float>? _minimapScale;
		private static MelonPreferences_Entry<bool>? _autoScaleMinimap;
		private static MelonPreferences_Entry<float>? _minimapScaleFactor;
		private static MelonPreferences_Entry<float>? _minimapWorldRadiusMeters;
		private static MelonPreferences_Entry<bool>? _minimapFlipX;
		private static MelonPreferences_Entry<bool>? _minimapFlipY;
		private static MelonPreferences_Entry<float>? _minimapBasisRotationDegrees;
		private static MelonPreferences_Entry<bool>? _showMagicMonsters;
		private static MelonPreferences_Entry<bool>? _showRareMonsters;
		private static MelonPreferences_Entry<bool>? _showWhiteMonsters;
		private static MelonPreferences_Entry<float>? _minimapOffsetX;
		private static MelonPreferences_Entry<float>? _minimapOffsetY;

		// Dynamic dictionaries (NPC/Items)
		private static readonly Dictionary<string, MelonPreferences_Entry<bool>> _npcClassificationEntries = new();
		private static readonly Dictionary<string, MelonPreferences_Entry<bool>> _npcDrawingEntries = new();
		private static readonly Dictionary<string, MelonPreferences_Entry<bool>> _itemDrawingEntries = new();

		public static void Init()
		{
			_general = MelonPreferences.CreateCategory(CatGeneral, "LEHud - General");
			_patches = MelonPreferences.CreateCategory(CatPatches, "LEHud - Patches");
			_autoPotion = MelonPreferences.CreateCategory(CatAutoPotion, "LEHud - AutoPotion");
			_antiIdle = MelonPreferences.CreateCategory(CatAntiIdle, "LEHud - AntiIdle");
			_autoDisconnect = MelonPreferences.CreateCategory(CatAutoDisconnect, "LEHud - AutoDisconnect");
			_minimap = MelonPreferences.CreateCategory(CatMinimap, "LEHud - Minimap");
			_npc = MelonPreferences.CreateCategory(CatNPC, "LEHud - NPC");
			_items = MelonPreferences.CreateCategory(CatItems, "LEHud - Items");

			_mapHack = _general.CreateEntry("MapHack", Settings.mapHack);
			_drawDistance = _general.CreateEntry("DrawDistance", Settings.drawDistance);
			_timeScale = _general.CreateEntry("TimeScale", Settings.timeScale);
			_useLootFilter = _general.CreateEntry("UseLootFilter", Settings.useLootFilter);

			_removeFog = _patches.CreateEntry("RemoveFog", Settings.removeFog);
			_cameraZoomUnlock = _patches.CreateEntry("CameraZoomUnlock", Settings.cameraZoomUnlock);
			_minimapZoomUnlock = _patches.CreateEntry("MinimapZoomUnlock", Settings.minimapZoomUnlock);
			_playerLantern = _patches.CreateEntry("PlayerLantern", Settings.playerLantern);
			_useAnyWaypoint = _patches.CreateEntry("UseAnyWaypoint", Settings.useAnyWaypoint);

			_useAutoPot = _autoPotion.CreateEntry("UseAutoPot", Settings.useAutoPot);
			_autoHealthPotion = _autoPotion.CreateEntry("AutoHealthPotionPercent", Settings.autoHealthPotion);
			_autoPotionCooldown = _autoPotion.CreateEntry("AutoPotionCooldownSeconds", Settings.autoPotionCooldown);

			_useAntiIdle = _antiIdle.CreateEntry("UseAntiIdle", Settings.useAntiIdle);
			_antiIdleInterval = _antiIdle.CreateEntry("AntiIdleIntervalSeconds", Settings.antiIdleInterval);
			_useSyntheticKeepAlive = _antiIdle.CreateEntry("UseSyntheticKeepAlive", Settings.useSyntheticKeepAlive);
			_keepAliveInterval = _antiIdle.CreateEntry("KeepAliveIntervalSeconds", Settings.keepAliveInterval);
			_suppressKeepAliveOnActivity = _antiIdle.CreateEntry("SuppressKeepAliveOnActivity", Settings.suppressKeepAliveOnActivity);
			_activitySuppressionSeconds = _antiIdle.CreateEntry("ActivitySuppressionSeconds", Settings.activitySuppressionSeconds);
			_sceneChangeSuppressionSeconds = _antiIdle.CreateEntry("SceneChangeSuppressionSeconds", Settings.sceneChangeSuppressionSeconds);
			_networkActivitySuppressionSeconds = _antiIdle.CreateEntry("NetworkActivitySuppressionSeconds", Settings.networkActivitySuppressionSeconds);

			_useAutoDisconnect = _autoDisconnect.CreateEntry("UseAutoDisconnect", Settings.useAutoDisconnect);
			_autoDisconnectHealthPercent = _autoDisconnect.CreateEntry("AutoDisconnectHealthPercent", Settings.autoDisconnectHealthPercent);
			_autoDisconnectCooldownSeconds = _autoDisconnect.CreateEntry("AutoDisconnectCooldownSeconds", Settings.autoDisconnectCooldownSeconds);
			_autoDisconnectOnlyWhenNoPotions = _autoDisconnect.CreateEntry("OnlyWhenNoPotions", Settings.autoDisconnectOnlyWhenNoPotions);

			_showMinimapEnemyCircles = _minimap.CreateEntry("ShowEnemyCircles", Settings.showMinimapEnemyCircles);
			_minimapCircleSize = _minimap.CreateEntry("CircleSize", Settings.minimapCircleSize);
			_minimapScale = _minimap.CreateEntry("Scale", Settings.minimapScale);
			_autoScaleMinimap = _minimap.CreateEntry("AutoScaleMinimap", Settings.autoScaleMinimap);
			_minimapScaleFactor = _minimap.CreateEntry("ScaleFactor", Settings.minimapScaleFactor);
			_minimapWorldRadiusMeters = _minimap.CreateEntry("WorldRadiusMeters", Settings.minimapWorldRadiusMeters);
			_minimapFlipX = _minimap.CreateEntry("FlipX", Settings.minimapFlipX);
			_minimapFlipY = _minimap.CreateEntry("FlipY", Settings.minimapFlipY);
			_minimapBasisRotationDegrees = _minimap.CreateEntry("BasisRotationDegrees", Settings.minimapBasisRotationDegrees);
			_showMagicMonsters = _minimap.CreateEntry("ShowMagicMonsters", Settings.showMagicMonsters);
			_showRareMonsters = _minimap.CreateEntry("ShowRareMonsters", Settings.showRareMonsters);
			_showWhiteMonsters = _minimap.CreateEntry("ShowWhiteMonsters", Settings.showWhiteMonsters);
			_minimapOffsetX = _minimap.CreateEntry("OffsetX", Settings.minimapOffsetX);
			_minimapOffsetY = _minimap.CreateEntry("OffsetY", Settings.minimapOffsetY);

			// Dynamic dictionary-backed entries
			EnsureDictionaryEntries(Settings.npcClassifications, _npcClassificationEntries, _npc, prefix: "Class_");
			EnsureDictionaryEntries(Settings.npcDrawings, _npcDrawingEntries, _npc, prefix: "Draw_");
			EnsureDictionaryEntries(Settings.itemDrawings, _itemDrawingEntries, _items, prefix: "Draw_");

			// Materialize entries on disk so users see them immediately on first run
			MelonPreferences.Save();
		}

		public static void LoadIntoSettings()
		{
			if (_general == null)
				return;

			Settings.mapHack = _mapHack!.Value;
			Settings.drawDistance = Clamp(_drawDistance!.Value, 0f, 1000f);
			Settings.timeScale = Clamp(_timeScale!.Value, 0.1f, 10f);
			Settings.useLootFilter = _useLootFilter!.Value;

			Settings.removeFog = _removeFog!.Value;
			Settings.cameraZoomUnlock = _cameraZoomUnlock!.Value;
			Settings.minimapZoomUnlock = _minimapZoomUnlock!.Value;
			Settings.playerLantern = _playerLantern!.Value;
			Settings.useAnyWaypoint = _useAnyWaypoint!.Value;

			Settings.useAutoPot = _useAutoPot!.Value;
			Settings.autoHealthPotion = Clamp(_autoHealthPotion!.Value, 0f, 100f);
			Settings.autoPotionCooldown = Clamp(_autoPotionCooldown!.Value, 0.1f, 30f);

			Settings.useAntiIdle = _useAntiIdle!.Value;
			Settings.antiIdleInterval = Clamp(_antiIdleInterval!.Value, 10f, 600f);
			Settings.useSyntheticKeepAlive = _useSyntheticKeepAlive!.Value;
			Settings.keepAliveInterval = Clamp(_keepAliveInterval!.Value, 5f, 300f);
			Settings.suppressKeepAliveOnActivity = _suppressKeepAliveOnActivity!.Value;
			Settings.activitySuppressionSeconds = Clamp(_activitySuppressionSeconds!.Value, 0f, 600f);
			Settings.sceneChangeSuppressionSeconds = Clamp(_sceneChangeSuppressionSeconds!.Value, 0f, 600f);
			Settings.networkActivitySuppressionSeconds = Clamp(_networkActivitySuppressionSeconds!.Value, 0f, 600f);

			Settings.useAutoDisconnect = _useAutoDisconnect!.Value;
			Settings.autoDisconnectHealthPercent = Clamp(_autoDisconnectHealthPercent!.Value, 0f, 100f);
			Settings.autoDisconnectCooldownSeconds = Clamp(_autoDisconnectCooldownSeconds!.Value, 1f, 300f);
			Settings.autoDisconnectOnlyWhenNoPotions = _autoDisconnectOnlyWhenNoPotions!.Value;

			Settings.showMinimapEnemyCircles = _showMinimapEnemyCircles!.Value;
			Settings.minimapCircleSize = Clamp(_minimapCircleSize!.Value, 1f, 64f);
			Settings.minimapScale = Clamp(_minimapScale!.Value, 0.1f, 100f);
			Settings.autoScaleMinimap = _autoScaleMinimap!.Value;
			Settings.minimapScaleFactor = Clamp(_minimapScaleFactor!.Value, 0.1f, 20f);
			Settings.minimapWorldRadiusMeters = Clamp(_minimapWorldRadiusMeters!.Value, 10f, 10000f);
			Settings.minimapFlipX = _minimapFlipX!.Value;
			Settings.minimapFlipY = _minimapFlipY!.Value;
			Settings.minimapBasisRotationDegrees = Clamp(_minimapBasisRotationDegrees!.Value, -360f, 360f);
			Settings.showMagicMonsters = _showMagicMonsters!.Value;
			Settings.showRareMonsters = _showRareMonsters!.Value;
			Settings.showWhiteMonsters = _showWhiteMonsters!.Value;
			Settings.minimapOffsetX = Clamp(_minimapOffsetX!.Value, -1000f, 1000f);
			Settings.minimapOffsetY = Clamp(_minimapOffsetY!.Value, -1000f, 1000f);

			// Apply dictionary entries back into current dictionaries
			ApplyEntriesToDictionary(Settings.npcClassifications, _npcClassificationEntries);
			ApplyEntriesToDictionary(Settings.npcDrawings, _npcDrawingEntries);
			ApplyEntriesToDictionary(Settings.itemDrawings, _itemDrawingEntries);
		}

		public static void ApplyToPreferencesFromSettings()
		{
			if (_general == null)
				return;

			_mapHack!.Value = Settings.mapHack;
			_drawDistance!.Value = Settings.drawDistance;
			_timeScale!.Value = Settings.timeScale;
			_useLootFilter!.Value = Settings.useLootFilter;

			_removeFog!.Value = Settings.removeFog;
			_cameraZoomUnlock!.Value = Settings.cameraZoomUnlock;
			_minimapZoomUnlock!.Value = Settings.minimapZoomUnlock;
			_playerLantern!.Value = Settings.playerLantern;
			_useAnyWaypoint!.Value = Settings.useAnyWaypoint;

			_useAutoPot!.Value = Settings.useAutoPot;
			_autoHealthPotion!.Value = Settings.autoHealthPotion;
			_autoPotionCooldown!.Value = Settings.autoPotionCooldown;

			_useAntiIdle!.Value = Settings.useAntiIdle;
			_antiIdleInterval!.Value = Settings.antiIdleInterval;
			_useSyntheticKeepAlive!.Value = Settings.useSyntheticKeepAlive;
			_keepAliveInterval!.Value = Settings.keepAliveInterval;
			_suppressKeepAliveOnActivity!.Value = Settings.suppressKeepAliveOnActivity;
			_activitySuppressionSeconds!.Value = Settings.activitySuppressionSeconds;
			_sceneChangeSuppressionSeconds!.Value = Settings.sceneChangeSuppressionSeconds;
			_networkActivitySuppressionSeconds!.Value = Settings.networkActivitySuppressionSeconds;

			_useAutoDisconnect!.Value = Settings.useAutoDisconnect;
			_autoDisconnectHealthPercent!.Value = Settings.autoDisconnectHealthPercent;
			_autoDisconnectCooldownSeconds!.Value = Settings.autoDisconnectCooldownSeconds;
			_autoDisconnectOnlyWhenNoPotions!.Value = Settings.autoDisconnectOnlyWhenNoPotions;

			_showMinimapEnemyCircles!.Value = Settings.showMinimapEnemyCircles;
			_minimapCircleSize!.Value = Settings.minimapCircleSize;
			_minimapScale!.Value = Settings.minimapScale;
			_autoScaleMinimap!.Value = Settings.autoScaleMinimap;
			_minimapScaleFactor!.Value = Settings.minimapScaleFactor;
			_minimapWorldRadiusMeters!.Value = Settings.minimapWorldRadiusMeters;
			_minimapFlipX!.Value = Settings.minimapFlipX;
			_minimapFlipY!.Value = Settings.minimapFlipY;
			_minimapBasisRotationDegrees!.Value = Settings.minimapBasisRotationDegrees;
			_showMagicMonsters!.Value = Settings.showMagicMonsters;
			_showRareMonsters!.Value = Settings.showRareMonsters;
			_showWhiteMonsters!.Value = Settings.showWhiteMonsters;
			_minimapOffsetX!.Value = Settings.minimapOffsetX;
			_minimapOffsetY!.Value = Settings.minimapOffsetY;

			// Sync dictionary-backed prefs
			SyncDictionaryToEntries(Settings.npcClassifications, _npcClassificationEntries, _npc!, prefix: "Class_");
			SyncDictionaryToEntries(Settings.npcDrawings, _npcDrawingEntries, _npc!, prefix: "Draw_");
			SyncDictionaryToEntries(Settings.itemDrawings, _itemDrawingEntries, _items!, prefix: "Draw_");
		}

		public static void Save()
		{
			MelonPreferences.Save();
		}

		private static float Clamp(float value, float min, float max)
		{
			if (value < min) return min;
			if (value > max) return max;
			return value;
		}

		private static void EnsureDictionaryEntries(
			Dictionary<string, bool> source,
			Dictionary<string, MelonPreferences_Entry<bool>> target,
			MelonPreferences_Category category,
			string prefix)
		{
			foreach (var kv in source)
			{
				var key = prefix + kv.Key;
				if (!target.ContainsKey(kv.Key))
				{
					target[kv.Key] = category.CreateEntry(key, kv.Value);
				}
			}
		}

		private static void ApplyEntriesToDictionary(
			Dictionary<string, bool> target,
			Dictionary<string, MelonPreferences_Entry<bool>> entries)
		{
			var keys = new List<string>(target.Keys);
			foreach (var k in keys)
			{
				if (entries.TryGetValue(k, out var entry))
				{
					target[k] = entry.Value;
				}
			}
		}

		private static void SyncDictionaryToEntries(
			Dictionary<string, bool> source,
			Dictionary<string, MelonPreferences_Entry<bool>> entries,
			MelonPreferences_Category category,
			string prefix)
		{
			// Ensure any new keys have entries
			EnsureDictionaryEntries(source, entries, category, prefix);
			// Push values
			foreach (var kv in source)
			{
				if (entries.TryGetValue(kv.Key, out var entry))
				{
					entry.Value = kv.Value;
				}
			}
		}
	}
} 
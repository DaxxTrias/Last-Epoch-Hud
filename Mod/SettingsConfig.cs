using MelonLoader;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System;

namespace Mod
{
	internal static class SettingsConfig
	{
		private const string CatGeneral = "LEHud_General";
		private const string CatPatches = "LEHud_Patches";
		private const string CatAutoPotion = "LEHud_AutoPotion";
		private const string CatAntiIdle = "LEHud_AntiIdle";
		private const string CatAutoDisconnect = "LEHud_AutoDisconnect";
		private const string CatMinimap = "LEHud_Minimap";
		private const string CatNPC = "LEHud_NPC";
		private const string CatItems = "LEHud_Items";
		private const string CatESP = "LEHud_ESP";

		// Categories
		private static MelonPreferences_Category? _general;
		private static MelonPreferences_Category? _patches;
		private static MelonPreferences_Category? _autoPotion;
		private static MelonPreferences_Category? _antiIdle;
		private static MelonPreferences_Category? _autoDisconnect;
		private static MelonPreferences_Category? _minimap;
		private static MelonPreferences_Category? _npc;
		private static MelonPreferences_Category? _items;
		private static MelonPreferences_Category? _esp;

		// Entries - General
		private static MelonPreferences_Entry<bool>? _mapHack;
		private static MelonPreferences_Entry<float>? _drawDistance;
		private static MelonPreferences_Entry<float>? _timeScale;
		private static MelonPreferences_Entry<bool>? _useLootFilter;
		private static MelonPreferences_Entry<bool>? _enableNetworkDiagnostics;
		private static MelonPreferences_Entry<bool>? _enableDpsMeter;
		private static MelonPreferences_Entry<float>? _dpsMeterWindowSeconds;
		private static MelonPreferences_Entry<float>? _dpsMeterInactivityResetSeconds;
		private static MelonPreferences_Entry<bool>? _dpsMeterAutoReset;
		private static MelonPreferences_Entry<bool>? _enableDpsMeterOnlineRaw;
		private static MelonPreferences_Entry<int>? _dpsMeterOnlineFilterMode;
		private static MelonPreferences_Entry<float>? _dpsMeterNearPlayerMeters;
		private static MelonPreferences_Entry<float>? _dpsMeterFarPlayerMeters;
		private static MelonPreferences_Entry<float>? _dpsMeterHpDropCorrelationMs;
		private static MelonPreferences_Entry<bool>? _dpsMeterPanelLocked;
		private static MelonPreferences_Entry<float>? _dpsMeterPanelX;
		private static MelonPreferences_Entry<float>? _dpsMeterPanelY;
		private static MelonPreferences_Entry<float>? _dpsMeterPanelWidth;
		private static MelonPreferences_Entry<float>? _dpsMeterPanelHeight;
		private static MelonPreferences_Entry<bool>? _enableDamageNumberDiagnostics;

		// Entries - ESP
		private static MelonPreferences_Entry<bool>? _showESPLines;
		private static MelonPreferences_Entry<bool>? _showESPLabels;
		private static MelonPreferences_Entry<float>? _espVerticalCullMeters;
		private static MelonPreferences_Entry<bool>? _espShowChests;
		private static MelonPreferences_Entry<bool>? _espShowShrines;
		private static MelonPreferences_Entry<bool>? _espShowRunePrisons;
		private static MelonPreferences_Entry<bool>? _espShowChampions;
		private static MelonPreferences_Entry<bool>? _espShowLootLizards;
#if DEBUG
		private static MelonPreferences_Entry<bool>? _debugEnableDiagnostics;
		private static MelonPreferences_Entry<bool>? _debugShowLocalPlayerPanel;
		private static MelonPreferences_Entry<bool>? _debugShowLocalPlayerWorldLabel;
		private static MelonPreferences_Entry<bool>? _debugDrawAllManagerActors;
		private static MelonPreferences_Entry<bool>? _debugDrawAllGroundItems;
		private static MelonPreferences_Entry<bool>? _debugDrawAllGroundGold;
		private static MelonPreferences_Entry<bool>? _debugDrawManagerLines;
		private static MelonPreferences_Entry<bool>? _debugIgnoreDistanceCulling;
		private static MelonPreferences_Entry<int>? _debugMaxEntriesPerSystem;
#endif

		// Entries - Patches
		private static MelonPreferences_Entry<bool>? _removeFog;
		private static MelonPreferences_Entry<bool>? _cameraZoomUnlock;
		private static MelonPreferences_Entry<bool>? _minimapZoomUnlock;
		private static MelonPreferences_Entry<bool>? _playerLantern;
		private static MelonPreferences_Entry<bool>? _useAnyWaypoint;
		private static MelonPreferences_Entry<bool>? _blockMenuInputWhenOpen;

		// Entries - AutoPotion
		private static MelonPreferences_Entry<bool>? _useAutoPot;
		private static MelonPreferences_Entry<float>? _autoHealthPotion;
		private static MelonPreferences_Entry<float>? _autoPotionCooldown;

		// Entries - AntiIdle
		private static MelonPreferences_Entry<bool>? _useAntiIdle;
		private static MelonPreferences_Entry<float>? _antiIdleInterval;
		private static MelonPreferences_Entry<bool>? _suppressKeepAliveOnActivity;
		private static MelonPreferences_Entry<float>? _activitySuppressionSeconds;
		private static MelonPreferences_Entry<float>? _sceneChangeSuppressionSeconds;
		// private static MelonPreferences_Entry<float>? _networkActivitySuppressionSeconds;
		private static MelonPreferences_Entry<bool>? _useSimpleAntiIdle;
		private static MelonPreferences_Entry<float>? _simpleAntiIdleInterval;
		private static MelonPreferences_Entry<bool>? _forceIsIdleFalseFallback;

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
		private static MelonPreferences_Entry<bool>? _showBossMonsters;
		private static MelonPreferences_Entry<float>? _minimapOffsetX;
		private static MelonPreferences_Entry<float>? _minimapOffsetY;

		// Dynamic dictionaries (NPC/Items)
		private static readonly Dictionary<string, MelonPreferences_Entry<bool>> _npcClassificationEntries = new();
		private static readonly Dictionary<string, MelonPreferences_Entry<bool>> _npcDrawingEntries = new();
		private static readonly Dictionary<string, MelonPreferences_Entry<bool>> _itemDrawingEntries = new();

		// Initialization guard against early OnPreferencesLoaded callbacks
		private static bool _isInitialized;
		public static bool IsInitialized => _isInitialized;

		public static void Init()
		{
			_isInitialized = false;
			_general = MelonPreferences.CreateCategory(CatGeneral, "LEHud - General");
			_patches = MelonPreferences.CreateCategory(CatPatches, "LEHud - Patches");
			_autoPotion = MelonPreferences.CreateCategory(CatAutoPotion, "LEHud - AutoPotion");
			_antiIdle = MelonPreferences.CreateCategory(CatAntiIdle, "LEHud - AntiIdle");
			_autoDisconnect = MelonPreferences.CreateCategory(CatAutoDisconnect, "LEHud - AutoDisconnect");
			_minimap = MelonPreferences.CreateCategory(CatMinimap, "LEHud - Minimap");
			_npc = MelonPreferences.CreateCategory(CatNPC, "LEHud - NPC");
			_items = MelonPreferences.CreateCategory(CatItems, "LEHud - Items");
			_esp = MelonPreferences.CreateCategory(CatESP, "LEHud - ESP");

			// Bind categories to a single prefs file and autoload existing values before creating entries
			var prefsDir = System.IO.Path.GetDirectoryName(GetStandaloneConfigPath())!;
			var prefsPath = System.IO.Path.Combine(prefsDir, "LEHud.melon.cfg");
			_general.SetFilePath(prefsPath, autoload: true);
			_patches.SetFilePath(prefsPath, autoload: true);
			_autoPotion.SetFilePath(prefsPath, autoload: true);
			_antiIdle.SetFilePath(prefsPath, autoload: true);
			_autoDisconnect.SetFilePath(prefsPath, autoload: true);
			_minimap.SetFilePath(prefsPath, autoload: true);
			_npc.SetFilePath(prefsPath, autoload: true);
			_items.SetFilePath(prefsPath, autoload: true);
			_esp.SetFilePath(prefsPath, autoload: true);

			_mapHack = _general.CreateEntry("MapHack", Settings.mapHack);
			_drawDistance = _general.CreateEntry("DrawDistance", Settings.drawDistance);
			_timeScale = _general.CreateEntry("TimeScale", Settings.timeScale);
			_useLootFilter = _general.CreateEntry("UseLootFilter", Settings.useLootFilter);
			_enableNetworkDiagnostics = _general.CreateEntry("EnableNetworkDiagnostics", Settings.enableNetworkDiagnostics);
			_enableDpsMeter = _general.CreateEntry("EnableDpsMeter", Settings.enableDpsMeter);
			_dpsMeterWindowSeconds = _general.CreateEntry("DpsMeterWindowSeconds", Settings.dpsMeterWindowSeconds);
			_dpsMeterInactivityResetSeconds = _general.CreateEntry("DpsMeterInactivityResetSeconds", Settings.dpsMeterInactivityResetSeconds);
			_dpsMeterAutoReset = _general.CreateEntry("DpsMeterAutoReset", Settings.dpsMeterAutoReset);
			_enableDpsMeterOnlineRaw = _general.CreateEntry("EnableDpsMeterOnlineRaw", Settings.enableDpsMeterOnlineRaw);
			_dpsMeterOnlineFilterMode = _general.CreateEntry("DpsMeterOnlineFilterMode", Settings.dpsMeterOnlineFilterMode);
			_dpsMeterNearPlayerMeters = _general.CreateEntry("DpsMeterNearPlayerMeters", Settings.dpsMeterNearPlayerMeters);
			_dpsMeterFarPlayerMeters = _general.CreateEntry("DpsMeterFarPlayerMeters", Settings.dpsMeterFarPlayerMeters);
			_dpsMeterHpDropCorrelationMs = _general.CreateEntry("DpsMeterHpDropCorrelationMs", Settings.dpsMeterHpDropCorrelationMs);
			_dpsMeterPanelLocked = _general.CreateEntry("DpsMeterPanelLocked", Settings.dpsMeterPanelLocked);
			_dpsMeterPanelX = _general.CreateEntry("DpsMeterPanelX", Settings.dpsMeterPanelX);
			_dpsMeterPanelY = _general.CreateEntry("DpsMeterPanelY", Settings.dpsMeterPanelY);
			_dpsMeterPanelWidth = _general.CreateEntry("DpsMeterPanelWidth", Settings.dpsMeterPanelWidth);
			_dpsMeterPanelHeight = _general.CreateEntry("DpsMeterPanelHeight", Settings.dpsMeterPanelHeight);
			_enableDamageNumberDiagnostics = _general.CreateEntry("EnableDamageNumberDiagnostics", Settings.enableDamageNumberDiagnostics);

			_showESPLines = _esp.CreateEntry("ShowESPLines", Settings.showESPLines);
			_showESPLabels = _esp.CreateEntry("ShowESPLabels", Settings.showESPLabels);
			_espVerticalCullMeters = _esp.CreateEntry("EspVerticalCullMeters", Settings.espVerticalCullMeters);
			_espShowChests = _esp.CreateEntry("ShowChests", Settings.espShowChests);
			_espShowShrines = _esp.CreateEntry("ShowShrines", Settings.espShowShrines);
			_espShowRunePrisons = _esp.CreateEntry("ShowRunePrisons", Settings.espShowRunePrisons);
			_espShowChampions = _esp.CreateEntry("ShowChampions", Settings.espShowChampions);
			_espShowLootLizards = _esp.CreateEntry("ShowLootLizards", Settings.espShowLootLizards);
#if DEBUG
			_debugEnableDiagnostics = _esp.CreateEntry("DebugEnableDiagnostics", Settings.debugEnableDiagnostics);
			_debugShowLocalPlayerPanel = _esp.CreateEntry("DebugShowLocalPlayerPanel", Settings.debugShowLocalPlayerPanel);
			_debugShowLocalPlayerWorldLabel = _esp.CreateEntry("DebugShowLocalPlayerWorldLabel", Settings.debugShowLocalPlayerWorldLabel);
			_debugDrawAllManagerActors = _esp.CreateEntry("DebugDrawAllManagerActors", Settings.debugDrawAllManagerActors);
			_debugDrawAllGroundItems = _esp.CreateEntry("DebugDrawAllGroundItems", Settings.debugDrawAllGroundItems);
			_debugDrawAllGroundGold = _esp.CreateEntry("DebugDrawAllGroundGold", Settings.debugDrawAllGroundGold);
			_debugDrawManagerLines = _esp.CreateEntry("DebugDrawManagerLines", Settings.debugDrawManagerLines);
			_debugIgnoreDistanceCulling = _esp.CreateEntry("DebugIgnoreDistanceCulling", Settings.debugIgnoreDistanceCulling);
			_debugMaxEntriesPerSystem = _esp.CreateEntry("DebugMaxEntriesPerSystem", Settings.debugMaxEntriesPerSystem);
#endif

			_removeFog = _patches.CreateEntry("RemoveFog", Settings.removeFog);
			_cameraZoomUnlock = _patches.CreateEntry("CameraZoomUnlock", Settings.cameraZoomUnlock);
			_minimapZoomUnlock = _patches.CreateEntry("MinimapZoomUnlock", Settings.minimapZoomUnlock);
			_playerLantern = _patches.CreateEntry("PlayerLantern", Settings.playerLantern);
			_useAnyWaypoint = _patches.CreateEntry("UseAnyWaypoint", Settings.useAnyWaypoint);
			_blockMenuInputWhenOpen = _patches.CreateEntry("BlockMenuInputWhenOpen", Settings.blockMenuInputWhenOpen);

			_useAutoPot = _autoPotion.CreateEntry("UseAutoPot", Settings.useAutoPot);
			_autoHealthPotion = _autoPotion.CreateEntry("AutoHealthPotionPercent", Settings.autoHealthPotion);
			_autoPotionCooldown = _autoPotion.CreateEntry("AutoPotionCooldownSeconds", Settings.autoPotionCooldown);

			_useAntiIdle = _antiIdle.CreateEntry("UseAntiIdle", Settings.useAntiIdle);
			_antiIdleInterval = _antiIdle.CreateEntry("AntiIdleIntervalSeconds", Settings.antiIdleInterval);
			_useSimpleAntiIdle = _antiIdle.CreateEntry("UseSimpleAntiIdle", Settings.useSimpleAntiIdle);
			_simpleAntiIdleInterval = _antiIdle.CreateEntry("SimpleAntiIdleIntervalSeconds", Settings.simpleAntiIdleInterval);
			_forceIsIdleFalseFallback = _antiIdle.CreateEntry("ForceIsIdleFalseFallback", Settings.forceIsIdleFalseFallback);
			_suppressKeepAliveOnActivity = _antiIdle.CreateEntry("SuppressKeepAliveOnActivity", Settings.suppressKeepAliveOnActivity);
			_activitySuppressionSeconds = _antiIdle.CreateEntry("ActivitySuppressionSeconds", Settings.activitySuppressionSeconds);
			_sceneChangeSuppressionSeconds = _antiIdle.CreateEntry("SceneChangeSuppressionSeconds", Settings.sceneChangeSuppressionSeconds);
			// _networkActivitySuppressionSeconds = _antiIdle.CreateEntry("NetworkActivitySuppressionSeconds", Settings.networkActivitySuppressionSeconds);

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
			_showBossMonsters = _minimap.CreateEntry("ShowBossMonsters", Settings.showBossMonsters);
			_minimapOffsetX = _minimap.CreateEntry("OffsetX", Settings.minimapOffsetX);
			_minimapOffsetY = _minimap.CreateEntry("OffsetY", Settings.minimapOffsetY);

			// Dynamic dictionary-backed entries
			EnsureDictionaryEntries(Settings.npcClassifications, _npcClassificationEntries, _npc, prefix: "Class_");
			EnsureDictionaryEntries(Settings.npcDrawings, _npcDrawingEntries, _npc, prefix: "Draw_");
			EnsureDictionaryEntries(Settings.itemDrawings, _itemDrawingEntries, _items, prefix: "Draw_");

			// Removed premature save; saving occurs after loads or on user changes
			_isInitialized = true;
		}

		public static void LoadIntoSettings()
		{
			if (_general == null || !_isInitialized)
				return;

			Settings.mapHack = _mapHack!.Value;
			Settings.drawDistance = Clamp(_drawDistance!.Value, 0f, 1000f);
			Settings.timeScale = Clamp(_timeScale!.Value, 0.1f, 10f);
			Settings.useLootFilter = _useLootFilter!.Value;
			Settings.enableNetworkDiagnostics = _enableNetworkDiagnostics!.Value;
			Settings.enableDpsMeter = _enableDpsMeter!.Value;
			Settings.dpsMeterWindowSeconds = Clamp(_dpsMeterWindowSeconds!.Value, 0.5f, 30f);
			Settings.dpsMeterInactivityResetSeconds = Clamp(_dpsMeterInactivityResetSeconds!.Value, 2f, 300f);
			Settings.dpsMeterAutoReset = _dpsMeterAutoReset!.Value;
			Settings.enableDpsMeterOnlineRaw = _enableDpsMeterOnlineRaw!.Value;
			Settings.dpsMeterOnlineFilterMode = Math.Clamp(_dpsMeterOnlineFilterMode!.Value, 0, 2);
			Settings.dpsMeterNearPlayerMeters = Clamp(_dpsMeterNearPlayerMeters!.Value, 0.5f, 10f);
			Settings.dpsMeterFarPlayerMeters = Clamp(_dpsMeterFarPlayerMeters!.Value, 0.6f, 20f);
			Settings.dpsMeterHpDropCorrelationMs = Clamp(_dpsMeterHpDropCorrelationMs!.Value, 50f, 1000f);
			if (Settings.dpsMeterFarPlayerMeters <= Settings.dpsMeterNearPlayerMeters)
				Settings.dpsMeterFarPlayerMeters = Settings.dpsMeterNearPlayerMeters + 0.2f;
			Settings.dpsMeterPanelLocked = _dpsMeterPanelLocked!.Value;
			Settings.dpsMeterPanelX = Clamp(_dpsMeterPanelX!.Value, -1f, 10000f);
			Settings.dpsMeterPanelY = Clamp(_dpsMeterPanelY!.Value, -1f, 10000f);
			Settings.dpsMeterPanelWidth = Clamp(_dpsMeterPanelWidth!.Value, 280f, 1400f);
			Settings.dpsMeterPanelHeight = Clamp(_dpsMeterPanelHeight!.Value, 220f, 1400f);
			Settings.enableDamageNumberDiagnostics = _enableDamageNumberDiagnostics!.Value;

			Settings.showESPLines = _showESPLines!.Value;
			Settings.showESPLabels = _showESPLabels!.Value;
			Settings.espVerticalCullMeters = Clamp(_espVerticalCullMeters!.Value, 0f, 200f);
			Settings.espShowChests = _espShowChests!.Value;
			Settings.espShowShrines = _espShowShrines!.Value;
			Settings.espShowRunePrisons = _espShowRunePrisons!.Value;
			Settings.espShowChampions = _espShowChampions!.Value;
			Settings.espShowLootLizards = _espShowLootLizards!.Value;
#if DEBUG
			Settings.debugEnableDiagnostics = _debugEnableDiagnostics!.Value;
			Settings.debugShowLocalPlayerPanel = _debugShowLocalPlayerPanel!.Value;
			Settings.debugShowLocalPlayerWorldLabel = _debugShowLocalPlayerWorldLabel!.Value;
			Settings.debugDrawAllManagerActors = _debugDrawAllManagerActors!.Value;
			Settings.debugDrawAllGroundItems = _debugDrawAllGroundItems!.Value;
			Settings.debugDrawAllGroundGold = _debugDrawAllGroundGold!.Value;
			Settings.debugDrawManagerLines = _debugDrawManagerLines!.Value;
			Settings.debugIgnoreDistanceCulling = _debugIgnoreDistanceCulling!.Value;
			Settings.debugMaxEntriesPerSystem = Math.Clamp(_debugMaxEntriesPerSystem!.Value, 10, 500);
#endif

			Settings.removeFog = _removeFog!.Value;
			Settings.cameraZoomUnlock = _cameraZoomUnlock!.Value;
			Settings.minimapZoomUnlock = _minimapZoomUnlock!.Value;
			Settings.playerLantern = _playerLantern!.Value;
			Settings.useAnyWaypoint = _useAnyWaypoint!.Value;
			Settings.blockMenuInputWhenOpen = _blockMenuInputWhenOpen!.Value;

			Settings.useAutoPot = _useAutoPot!.Value;
			Settings.autoHealthPotion = Clamp(_autoHealthPotion!.Value, 0f, 100f);
			Settings.autoPotionCooldown = Clamp(_autoPotionCooldown!.Value, 0.1f, 30f);

			Settings.useAntiIdle = _useAntiIdle!.Value;
			Settings.antiIdleInterval = Clamp(_antiIdleInterval!.Value, 10f, 600f);
			Settings.useSimpleAntiIdle = _useSimpleAntiIdle!.Value;
			Settings.simpleAntiIdleInterval = Clamp(_simpleAntiIdleInterval!.Value, 60f, 1800f);
			Settings.forceIsIdleFalseFallback = _forceIsIdleFalseFallback!.Value;
			Settings.suppressKeepAliveOnActivity = _suppressKeepAliveOnActivity!.Value;
			Settings.activitySuppressionSeconds = Clamp(_activitySuppressionSeconds!.Value, 0f, 600f);
			Settings.sceneChangeSuppressionSeconds = Clamp(_sceneChangeSuppressionSeconds!.Value, 0f, 600f);
			// Settings.networkActivitySuppressionSeconds = Clamp(_networkActivitySuppressionSeconds!.Value, 0f, 600f);

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
			Settings.showBossMonsters = _showBossMonsters!.Value;
			Settings.minimapOffsetX = Clamp(_minimapOffsetX!.Value, -1000f, 1000f);
			Settings.minimapOffsetY = Clamp(_minimapOffsetY!.Value, -1000f, 1000f);

			// Apply dictionary entries back into current dictionaries
			ApplyEntriesToDictionary(Settings.npcClassifications, _npcClassificationEntries);
			ApplyEntriesToDictionary(Settings.npcDrawings, _npcDrawingEntries);
			ApplyEntriesToDictionary(Settings.itemDrawings, _itemDrawingEntries);
		}

		public static void ApplyToPreferencesFromSettings()
		{
			if (_general == null || !_isInitialized)
				return;

			_mapHack!.Value = Settings.mapHack;
			_drawDistance!.Value = Settings.drawDistance;
			_timeScale!.Value = Settings.timeScale;
			_useLootFilter!.Value = Settings.useLootFilter;
			_enableNetworkDiagnostics!.Value = Settings.enableNetworkDiagnostics;
			_enableDpsMeter!.Value = Settings.enableDpsMeter;
			_dpsMeterWindowSeconds!.Value = Settings.dpsMeterWindowSeconds;
			_dpsMeterInactivityResetSeconds!.Value = Settings.dpsMeterInactivityResetSeconds;
			_dpsMeterAutoReset!.Value = Settings.dpsMeterAutoReset;
			_enableDpsMeterOnlineRaw!.Value = Settings.enableDpsMeterOnlineRaw;
			_dpsMeterOnlineFilterMode!.Value = Settings.dpsMeterOnlineFilterMode;
			_dpsMeterNearPlayerMeters!.Value = Settings.dpsMeterNearPlayerMeters;
			_dpsMeterFarPlayerMeters!.Value = Settings.dpsMeterFarPlayerMeters;
			_dpsMeterHpDropCorrelationMs!.Value = Settings.dpsMeterHpDropCorrelationMs;
			_dpsMeterPanelLocked!.Value = Settings.dpsMeterPanelLocked;
			_dpsMeterPanelX!.Value = Settings.dpsMeterPanelX;
			_dpsMeterPanelY!.Value = Settings.dpsMeterPanelY;
			_dpsMeterPanelWidth!.Value = Settings.dpsMeterPanelWidth;
			_dpsMeterPanelHeight!.Value = Settings.dpsMeterPanelHeight;
			_enableDamageNumberDiagnostics!.Value = Settings.enableDamageNumberDiagnostics;

			_showESPLines!.Value = Settings.showESPLines;
			_showESPLabels!.Value = Settings.showESPLabels;
			_espVerticalCullMeters!.Value = Settings.espVerticalCullMeters;
			_espShowChests!.Value = Settings.espShowChests;
			_espShowShrines!.Value = Settings.espShowShrines;
			_espShowRunePrisons!.Value = Settings.espShowRunePrisons;
			_espShowChampions!.Value = Settings.espShowChampions;
			_espShowLootLizards!.Value = Settings.espShowLootLizards;
#if DEBUG
			_debugEnableDiagnostics!.Value = Settings.debugEnableDiagnostics;
			_debugShowLocalPlayerPanel!.Value = Settings.debugShowLocalPlayerPanel;
			_debugShowLocalPlayerWorldLabel!.Value = Settings.debugShowLocalPlayerWorldLabel;
			_debugDrawAllManagerActors!.Value = Settings.debugDrawAllManagerActors;
			_debugDrawAllGroundItems!.Value = Settings.debugDrawAllGroundItems;
			_debugDrawAllGroundGold!.Value = Settings.debugDrawAllGroundGold;
			_debugDrawManagerLines!.Value = Settings.debugDrawManagerLines;
			_debugIgnoreDistanceCulling!.Value = Settings.debugIgnoreDistanceCulling;
			_debugMaxEntriesPerSystem!.Value = Settings.debugMaxEntriesPerSystem;
#endif

			_removeFog!.Value = Settings.removeFog;
			_cameraZoomUnlock!.Value = Settings.cameraZoomUnlock;
			_minimapZoomUnlock!.Value = Settings.minimapZoomUnlock;
			_playerLantern!.Value = Settings.playerLantern;
			_useAnyWaypoint!.Value = Settings.useAnyWaypoint;
			_blockMenuInputWhenOpen!.Value = Settings.blockMenuInputWhenOpen;

			_useAutoPot!.Value = Settings.useAutoPot;
			_autoHealthPotion!.Value = Settings.autoHealthPotion;
			_autoPotionCooldown!.Value = Settings.autoPotionCooldown;

			_useAntiIdle!.Value = Settings.useAntiIdle;
			_antiIdleInterval!.Value = Settings.antiIdleInterval;
			_useSimpleAntiIdle!.Value = Settings.useSimpleAntiIdle;
			_simpleAntiIdleInterval!.Value = Settings.simpleAntiIdleInterval;
			_forceIsIdleFalseFallback!.Value = Settings.forceIsIdleFalseFallback;
			_suppressKeepAliveOnActivity!.Value = Settings.suppressKeepAliveOnActivity;
			_activitySuppressionSeconds!.Value = Settings.activitySuppressionSeconds;
			_sceneChangeSuppressionSeconds!.Value = Settings.sceneChangeSuppressionSeconds;
			// _networkActivitySuppressionSeconds!.Value = Settings.networkActivitySuppressionSeconds;

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
			_showBossMonsters!.Value = Settings.showBossMonsters;
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
			try
			{
				_general?.SaveToFile();
				_patches?.SaveToFile();
				_autoPotion?.SaveToFile();
				_antiIdle?.SaveToFile();
				_autoDisconnect?.SaveToFile();
				_minimap?.SaveToFile();
				_npc?.SaveToFile();
				_items?.SaveToFile();
				_esp?.SaveToFile();
			}
			catch (Exception e)
			{
				MelonLogger.Error($"[Settings] Explicit category save error: {e.Message}");
			}
		}

		// Standalone file (JSON) import/export under UserData/LEHud.cfg
		public static bool LoadStandaloneIfExists()
		{
			try
			{
				var path = GetStandaloneConfigPath();
				if (!File.Exists(path))
					return false;

				var json = File.ReadAllText(path);
				var snapshot = JsonSerializer.Deserialize<SettingsSnapshot>(json);
				if (snapshot == null)
					return false;

				ApplySnapshot(snapshot);
				MelonLogger.Msg($"[Settings] Loaded standalone config: {path}");
				return true;
			}
			catch (Exception e)
			{
				MelonLogger.Error($"[Settings] LoadStandaloneIfExists error: {e.Message}");
				return false;
			}
		}

		public static void SaveStandalone()
		{
			try
			{
				var path = GetStandaloneConfigPath();
				Directory.CreateDirectory(Path.GetDirectoryName(path)!);
				var snapshot = CreateSnapshot();
				var json = JsonSerializer.Serialize(snapshot, new JsonSerializerOptions { WriteIndented = true });
				File.WriteAllText(path, json);
				MelonLogger.Msg($"[Settings] Saved standalone config: {path}");
			}
			catch (Exception e)
			{
				MelonLogger.Error($"[Settings] SaveStandalone error: {e.Message}");
			}
		}

		public static string GetStandaloneConfigPath()
		{
			// Resolve UserData directory from MelonEnvironment (new) or MelonUtils (old) via reflection to avoid obsolete warnings
			try
			{
				var envType = Type.GetType("MelonLoader.MelonEnvironment, MelonLoader");
				if (envType != null)
				{
					var prop = envType.GetProperty("UserDataDirectory", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
					if (prop != null)
					{
						var dir = prop.GetValue(null) as string;
						if (!string.IsNullOrEmpty(dir))
							return Path.Combine(dir!, "LEHud.cfg");
					}
				}
				var utilsType = Type.GetType("MelonLoader.MelonUtils, MelonLoader");
				if (utilsType != null)
				{
					var prop = utilsType.GetProperty("UserDataDirectory", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
					if (prop != null)
					{
						var dir = prop.GetValue(null) as string;
						if (!string.IsNullOrEmpty(dir))
							return Path.Combine(dir!, "LEHud.cfg");
					}
				}
			}
			catch { }
			var fallbackDir = Path.Combine(AppContext.BaseDirectory, "UserData");
			return Path.Combine(fallbackDir, "LEHud.cfg");
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

		// Snapshot DTO for JSON IO
		private sealed class SettingsSnapshot
		{
			public bool mapHack { get; set; }
			public float drawDistance { get; set; }
			public float autoHealthPotion { get; set; }
			public float autoPotionCooldown { get; set; }
			public float timeScale { get; set; }
			public bool useAutoPot { get; set; }
			public bool useLootFilter { get; set; }
			public bool enableNetworkDiagnostics { get; set; }
			public bool enableDpsMeter { get; set; }
			public float dpsMeterWindowSeconds { get; set; }
			public float dpsMeterInactivityResetSeconds { get; set; }
			public bool dpsMeterAutoReset { get; set; }
			public bool enableDpsMeterOnlineRaw { get; set; }
			public int dpsMeterOnlineFilterMode { get; set; }
			public float dpsMeterNearPlayerMeters { get; set; }
			public float dpsMeterFarPlayerMeters { get; set; }
			public float dpsMeterHpDropCorrelationMs { get; set; }
			public bool dpsMeterPanelLocked { get; set; }
			public float dpsMeterPanelX { get; set; }
			public float dpsMeterPanelY { get; set; }
			public float dpsMeterPanelWidth { get; set; }
			public float dpsMeterPanelHeight { get; set; }
			public bool enableDamageNumberDiagnostics { get; set; }
			public bool removeFog { get; set; }
			public bool cameraZoomUnlock { get; set; }
			public bool minimapZoomUnlock { get; set; }
			public bool playerLantern { get; set; }
			public bool useAnyWaypoint { get; set; }
			public bool blockMenuInputWhenOpen { get; set; }
			public bool useAntiIdle { get; set; }
			public float antiIdleInterval { get; set; }
			public bool suppressKeepAliveOnActivity { get; set; }
			public float activitySuppressionSeconds { get; set; }
			public float sceneChangeSuppressionSeconds { get; set; }
			public float networkActivitySuppressionSeconds { get; set; }
			public bool forceIsIdleFalseFallback { get; set; }
			public bool useAutoDisconnect { get; set; }
			public float autoDisconnectHealthPercent { get; set; }
			public float autoDisconnectCooldownSeconds { get; set; }
			public bool autoDisconnectOnlyWhenNoPotions { get; set; }
			public bool showMinimapEnemyCircles { get; set; }
			public float minimapCircleSize { get; set; }
			public float minimapScale { get; set; }
			public bool autoScaleMinimap { get; set; }
			public float minimapScaleFactor { get; set; }
			public float minimapWorldRadiusMeters { get; set; }
			public bool minimapFlipX { get; set; }
			public bool minimapFlipY { get; set; }
			public float minimapBasisRotationDegrees { get; set; }
			public bool showMagicMonsters { get; set; }
			public bool showRareMonsters { get; set; }
			public bool showWhiteMonsters { get; set; }
			public bool showBossMonsters { get; set; }
			public float minimapOffsetX { get; set; }
			public float minimapOffsetY { get; set; }
			public Dictionary<string, bool> npcClassifications { get; set; } = new();
			public Dictionary<string, bool> npcDrawings { get; set; } = new();
			public Dictionary<string, bool> itemDrawings { get; set; } = new();
			public bool useSimpleAntiIdle { get; set; }
			public float simpleAntiIdleInterval { get; set; }
			public bool showESPLines { get; set; }
			public bool showESPLabels { get; set; }
			public float espVerticalCullMeters { get; set; }
			public bool espShowChests { get; set; }
			public bool espShowShrines { get; set; }
			public bool espShowRunePrisons { get; set; }
			public bool espShowChampions { get; set; }
			public bool espShowLootLizards { get; set; }
#if DEBUG
			public bool debugEnableDiagnostics { get; set; }
			public bool debugShowLocalPlayerPanel { get; set; }
			public bool debugShowLocalPlayerWorldLabel { get; set; }
			public bool debugDrawAllManagerActors { get; set; }
			public bool debugDrawAllGroundItems { get; set; }
			public bool debugDrawAllGroundGold { get; set; }
			public bool debugDrawManagerLines { get; set; }
			public bool debugIgnoreDistanceCulling { get; set; }
			public int debugMaxEntriesPerSystem { get; set; }
#endif
		}

		private static SettingsSnapshot CreateSnapshot()
		{
			return new SettingsSnapshot
			{
				mapHack = Settings.mapHack,
				drawDistance = Settings.drawDistance,
				autoHealthPotion = Settings.autoHealthPotion,
				autoPotionCooldown = Settings.autoPotionCooldown,
				timeScale = Settings.timeScale,
				useAutoPot = Settings.useAutoPot,
				useLootFilter = Settings.useLootFilter,
				enableNetworkDiagnostics = Settings.enableNetworkDiagnostics,
				enableDpsMeter = Settings.enableDpsMeter,
				dpsMeterWindowSeconds = Settings.dpsMeterWindowSeconds,
				dpsMeterInactivityResetSeconds = Settings.dpsMeterInactivityResetSeconds,
				dpsMeterAutoReset = Settings.dpsMeterAutoReset,
				enableDpsMeterOnlineRaw = Settings.enableDpsMeterOnlineRaw,
				dpsMeterOnlineFilterMode = Settings.dpsMeterOnlineFilterMode,
				dpsMeterNearPlayerMeters = Settings.dpsMeterNearPlayerMeters,
				dpsMeterFarPlayerMeters = Settings.dpsMeterFarPlayerMeters,
				dpsMeterHpDropCorrelationMs = Settings.dpsMeterHpDropCorrelationMs,
				dpsMeterPanelLocked = Settings.dpsMeterPanelLocked,
				dpsMeterPanelX = Settings.dpsMeterPanelX,
				dpsMeterPanelY = Settings.dpsMeterPanelY,
				dpsMeterPanelWidth = Settings.dpsMeterPanelWidth,
				dpsMeterPanelHeight = Settings.dpsMeterPanelHeight,
				enableDamageNumberDiagnostics = Settings.enableDamageNumberDiagnostics,
				removeFog = Settings.removeFog,
				cameraZoomUnlock = Settings.cameraZoomUnlock,
				minimapZoomUnlock = Settings.minimapZoomUnlock,
				playerLantern = Settings.playerLantern,
				useAnyWaypoint = Settings.useAnyWaypoint,
				blockMenuInputWhenOpen = Settings.blockMenuInputWhenOpen,
				useAntiIdle = Settings.useAntiIdle,
				antiIdleInterval = Settings.antiIdleInterval,
				forceIsIdleFalseFallback = Settings.forceIsIdleFalseFallback,
				suppressKeepAliveOnActivity = Settings.suppressKeepAliveOnActivity,
				activitySuppressionSeconds = Settings.activitySuppressionSeconds,
				sceneChangeSuppressionSeconds = Settings.sceneChangeSuppressionSeconds,
				// networkActivitySuppressionSeconds = Settings.networkActivitySuppressionSeconds,
				useAutoDisconnect = Settings.useAutoDisconnect,
				autoDisconnectHealthPercent = Settings.autoDisconnectHealthPercent,
				autoDisconnectCooldownSeconds = Settings.autoDisconnectCooldownSeconds,
				autoDisconnectOnlyWhenNoPotions = Settings.autoDisconnectOnlyWhenNoPotions,
				showMinimapEnemyCircles = Settings.showMinimapEnemyCircles,
				minimapCircleSize = Settings.minimapCircleSize,
				minimapScale = Settings.minimapScale,
				autoScaleMinimap = Settings.autoScaleMinimap,
				minimapScaleFactor = Settings.minimapScaleFactor,
				minimapWorldRadiusMeters = Settings.minimapWorldRadiusMeters,
				minimapFlipX = Settings.minimapFlipX,
				minimapFlipY = Settings.minimapFlipY,
				minimapBasisRotationDegrees = Settings.minimapBasisRotationDegrees,
				showMagicMonsters = Settings.showMagicMonsters,
				showRareMonsters = Settings.showRareMonsters,
				showWhiteMonsters = Settings.showWhiteMonsters,
				showBossMonsters = Settings.showBossMonsters,
				minimapOffsetX = Settings.minimapOffsetX,
				minimapOffsetY = Settings.minimapOffsetY,
				npcClassifications = new Dictionary<string, bool>(Settings.npcClassifications),
				npcDrawings = new Dictionary<string, bool>(Settings.npcDrawings),
				itemDrawings = new Dictionary<string, bool>(Settings.itemDrawings),
				useSimpleAntiIdle = Settings.useSimpleAntiIdle,
				simpleAntiIdleInterval = Settings.simpleAntiIdleInterval,
				showESPLines = Settings.showESPLines,
				showESPLabels = Settings.showESPLabels,
				espVerticalCullMeters = Settings.espVerticalCullMeters,
				espShowChests = Settings.espShowChests,
				espShowShrines = Settings.espShowShrines,
				espShowRunePrisons = Settings.espShowRunePrisons,
				espShowChampions = Settings.espShowChampions,
				espShowLootLizards = Settings.espShowLootLizards,
#if DEBUG
				debugEnableDiagnostics = Settings.debugEnableDiagnostics,
				debugShowLocalPlayerPanel = Settings.debugShowLocalPlayerPanel,
				debugShowLocalPlayerWorldLabel = Settings.debugShowLocalPlayerWorldLabel,
				debugDrawAllManagerActors = Settings.debugDrawAllManagerActors,
				debugDrawAllGroundItems = Settings.debugDrawAllGroundItems,
				debugDrawAllGroundGold = Settings.debugDrawAllGroundGold,
				debugDrawManagerLines = Settings.debugDrawManagerLines,
				debugIgnoreDistanceCulling = Settings.debugIgnoreDistanceCulling,
				debugMaxEntriesPerSystem = Settings.debugMaxEntriesPerSystem
#endif
			};
		}

		private static void ApplySnapshot(SettingsSnapshot s)
		{
			Settings.mapHack = s.mapHack;
			Settings.drawDistance = Clamp(s.drawDistance, 0f, 1000f);
			Settings.autoHealthPotion = Clamp(s.autoHealthPotion, 0f, 100f);
			Settings.autoPotionCooldown = Clamp(s.autoPotionCooldown, 0.1f, 30f);
			Settings.timeScale = Clamp(s.timeScale, 0.1f, 10f);
			Settings.useAutoPot = s.useAutoPot;
			Settings.useLootFilter = s.useLootFilter;
			Settings.enableNetworkDiagnostics = s.enableNetworkDiagnostics;
			Settings.enableDpsMeter = s.enableDpsMeter;
			Settings.dpsMeterWindowSeconds = Clamp(s.dpsMeterWindowSeconds, 0.5f, 30f);
			Settings.dpsMeterInactivityResetSeconds = Clamp(s.dpsMeterInactivityResetSeconds, 2f, 300f);
			Settings.dpsMeterAutoReset = s.dpsMeterAutoReset;
			Settings.enableDpsMeterOnlineRaw = s.enableDpsMeterOnlineRaw;
			Settings.dpsMeterOnlineFilterMode = Math.Clamp(s.dpsMeterOnlineFilterMode, 0, 2);
			Settings.dpsMeterNearPlayerMeters = Clamp(s.dpsMeterNearPlayerMeters, 0.5f, 10f);
			Settings.dpsMeterFarPlayerMeters = Clamp(s.dpsMeterFarPlayerMeters, 0.6f, 20f);
			Settings.dpsMeterHpDropCorrelationMs = Clamp(s.dpsMeterHpDropCorrelationMs, 50f, 1000f);
			if (Settings.dpsMeterFarPlayerMeters <= Settings.dpsMeterNearPlayerMeters)
				Settings.dpsMeterFarPlayerMeters = Settings.dpsMeterNearPlayerMeters + 0.2f;
			Settings.dpsMeterPanelLocked = s.dpsMeterPanelLocked;
			Settings.dpsMeterPanelX = Clamp(s.dpsMeterPanelX, -1f, 10000f);
			Settings.dpsMeterPanelY = Clamp(s.dpsMeterPanelY, -1f, 10000f);
			Settings.dpsMeterPanelWidth = Clamp(s.dpsMeterPanelWidth, 280f, 1400f);
			Settings.dpsMeterPanelHeight = Clamp(s.dpsMeterPanelHeight, 220f, 1400f);
			Settings.enableDamageNumberDiagnostics = s.enableDamageNumberDiagnostics;
			Settings.removeFog = s.removeFog;
			Settings.cameraZoomUnlock = s.cameraZoomUnlock;
			Settings.minimapZoomUnlock = s.minimapZoomUnlock;
			Settings.playerLantern = s.playerLantern;
			Settings.useAnyWaypoint = s.useAnyWaypoint;
			Settings.blockMenuInputWhenOpen = s.blockMenuInputWhenOpen;
			Settings.useAntiIdle = s.useAntiIdle;
			Settings.antiIdleInterval = Clamp(s.antiIdleInterval, 10f, 600f);
			Settings.forceIsIdleFalseFallback = s.forceIsIdleFalseFallback;
			Settings.suppressKeepAliveOnActivity = s.suppressKeepAliveOnActivity;
			Settings.activitySuppressionSeconds = Clamp(s.activitySuppressionSeconds, 0f, 600f);
			Settings.sceneChangeSuppressionSeconds = Clamp(s.sceneChangeSuppressionSeconds, 0f, 600f);
			// Settings.networkActivitySuppressionSeconds = Clamp(s.networkActivitySuppressionSeconds, 0f, 600f);
			Settings.useAutoDisconnect = s.useAutoDisconnect;
			Settings.autoDisconnectHealthPercent = Clamp(s.autoDisconnectHealthPercent, 0f, 100f);
			Settings.autoDisconnectCooldownSeconds = Clamp(s.autoDisconnectCooldownSeconds, 1f, 300f);
			Settings.autoDisconnectOnlyWhenNoPotions = s.autoDisconnectOnlyWhenNoPotions;
			Settings.showMinimapEnemyCircles = s.showMinimapEnemyCircles;
			Settings.minimapCircleSize = Clamp(s.minimapCircleSize, 1f, 64f);
			Settings.minimapScale = Clamp(s.minimapScale, 0.1f, 100f);
			Settings.autoScaleMinimap = s.autoScaleMinimap;
			Settings.minimapScaleFactor = Clamp(s.minimapScaleFactor, 0.1f, 20f);
			Settings.minimapWorldRadiusMeters = Clamp(s.minimapWorldRadiusMeters, 10f, 10000f);
			Settings.minimapFlipX = s.minimapFlipX;
			Settings.minimapFlipY = s.minimapFlipY;
			Settings.minimapBasisRotationDegrees = Clamp(s.minimapBasisRotationDegrees, -360f, 360f);
			Settings.showMagicMonsters = s.showMagicMonsters;
			Settings.showRareMonsters = s.showRareMonsters;
			Settings.showWhiteMonsters = s.showWhiteMonsters;
			Settings.showBossMonsters = s.showBossMonsters;
			Settings.minimapOffsetX = Clamp(s.minimapOffsetX, -1000f, 1000f);
			Settings.minimapOffsetY = Clamp(s.minimapOffsetY, -1000f, 1000f);
			Settings.useSimpleAntiIdle = s.useSimpleAntiIdle;
			Settings.simpleAntiIdleInterval = Clamp(s.simpleAntiIdleInterval, 60f, 1800f);
			Settings.showESPLines = s.showESPLines;
			Settings.showESPLabels = s.showESPLabels;
			Settings.espVerticalCullMeters = Clamp(s.espVerticalCullMeters, 0f, 200f);
			Settings.espShowChests = s.espShowChests;
			Settings.espShowShrines = s.espShowShrines;
			Settings.espShowRunePrisons = s.espShowRunePrisons;
			Settings.espShowChampions = s.espShowChampions;
			Settings.espShowLootLizards = s.espShowLootLizards;
#if DEBUG
			Settings.debugEnableDiagnostics = s.debugEnableDiagnostics;
			Settings.debugShowLocalPlayerPanel = s.debugShowLocalPlayerPanel;
			Settings.debugShowLocalPlayerWorldLabel = s.debugShowLocalPlayerWorldLabel;
			Settings.debugDrawAllManagerActors = s.debugDrawAllManagerActors;
			Settings.debugDrawAllGroundItems = s.debugDrawAllGroundItems;
			Settings.debugDrawAllGroundGold = s.debugDrawAllGroundGold;
			Settings.debugDrawManagerLines = s.debugDrawManagerLines;
			Settings.debugIgnoreDistanceCulling = s.debugIgnoreDistanceCulling;
			Settings.debugMaxEntriesPerSystem = Math.Clamp(s.debugMaxEntriesPerSystem, 10, 500);
#endif

			ApplyDictionarySafely(Settings.npcClassifications, s.npcClassifications);
			ApplyDictionarySafely(Settings.npcDrawings, s.npcDrawings);
			ApplyDictionarySafely(Settings.itemDrawings, s.itemDrawings);
		}

		private static void ApplyDictionarySafely(Dictionary<string, bool> target, Dictionary<string, bool> source)
		{
			foreach (var kv in source)
			{
				target[kv.Key] = kv.Value;
			}
		}

		// Added manual import/export helpers for JSON
		/*
		public static bool ImportFromStandalone()
		{
			var path = GetStandaloneConfigPath();
			if (!File.Exists(path))
			{
				MelonLogger.Msg($"[Settings] Standalone config not found: {path}");
				return false;
			}
			var loaded = LoadStandaloneIfExists();
			if (loaded)
			{
				ApplyToPreferencesFromSettings();
				Save();
			}
			return loaded;
		}

		public static bool ExportToStandalone()
		{
			var path = GetStandaloneConfigPath();
			SaveStandalone();
			return File.Exists(path);
		}
		*/
	}
} 
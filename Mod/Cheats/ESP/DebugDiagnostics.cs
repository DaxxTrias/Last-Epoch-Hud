#if DEBUG
using System.Text;
using Il2Cpp;
using Mod.Game;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Mod.Cheats.ESP
{
	internal static class DebugDiagnostics
	{
		private static readonly Color DebugActorColor = new Color(1f, 0.75f, 0.2f, 1f);
		private static readonly Color DebugItemColor = new Color(0.2f, 1f, 0.95f, 1f);
		private static readonly Color DebugGoldColor = new Color(1f, 0.93f, 0.35f, 1f);
		private static readonly Color LocalPlayerColor = new Color(0.35f, 1f, 0.35f, 1f);
		private static readonly StringBuilder SnapshotBuilder = new StringBuilder(1024);
		private static readonly Rect PanelRect = new Rect(14f, 14f, 760f, 190f);

		private static string _localPlayerSnapshot = "No local player snapshot yet.";
		private static int _lastActorScanned;
		private static int _lastActorDrawn;
		private static int _lastItemsDrawn;
		private static int _lastGoldDrawn;
		private static float _nextSnapshotAt;

		public static void OnUpdate()
		{
			if (!Settings.debugEnableDiagnostics)
				return;

			var localPlayer = ObjectManager.GetLocalPlayer();
			if (localPlayer == null)
				return;

			if (Time.unscaledTime >= _nextSnapshotAt)
			{
				RefreshLocalPlayerSnapshot(localPlayer);
				_nextSnapshotAt = Time.unscaledTime + 0.25f;
			}

			if (Settings.debugShowLocalPlayerWorldLabel)
			{
				DrawLocalPlayerLabel(localPlayer);
			}

			DrawAllActorsFromActorManager(localPlayer);
			DrawAllGroundItems(localPlayer);
			DrawAllGroundGold(localPlayer);
		}

		public static void OnGUI()
		{
			if (!Settings.debugEnableDiagnostics || !Settings.debugShowLocalPlayerPanel)
				return;

			GUI.Box(PanelRect, "LEHud Debug Diagnostics (DEBUG build only)");
			var labelRect = new Rect(PanelRect.x + 8f, PanelRect.y + 24f, PanelRect.width - 14f, PanelRect.height - 28f);
			GUI.Label(labelRect, _localPlayerSnapshot);
		}

		private static void DrawLocalPlayerLabel(GameObject localPlayer)
		{
			var playerPos = localPlayer.transform.position;
			var labelPos = playerPos + Vector3.up * 2.0f;
			var sceneName = GetSceneName(localPlayer);
			var label = $"[LOCAL] {localPlayer.name} P:{FormatVec3(playerPos)} S:{sceneName}";
			ESP.AddString(label, labelPos, LocalPlayerColor);
		}

		private static void DrawAllActorsFromActorManager(GameObject localPlayer)
		{
			_lastActorScanned = 0;
			_lastActorDrawn = 0;

			if (!Settings.debugDrawAllManagerActors || ActorManager.instance == null)
				return;

			int maxEntries = Mathf.Clamp(Settings.debugMaxEntriesPerSystem, 10, 500);
			var localPos = localPlayer.transform.position;
			float maxDistance = Settings.drawDistance;

			foreach (var actorBucket in ActorManager.instance.visuals)
			{
				string alignmentName = actorBucket.alignment?.name ?? "Unknown";
				if (actorBucket.visuals == null || actorBucket.visuals._list == null)
					continue;

				foreach (var actor in actorBucket.visuals._list)
				{
					if (actor == null || actor.gameObject == null)
						continue;
					if (!actor.gameObject.activeInHierarchy)
						continue;

					_lastActorScanned++;

					var actorPos = actor.transform.position;
					if (!Settings.debugIgnoreDistanceCulling && Vector3.Distance(localPos, actorPos) > maxDistance)
						continue;

					if (_lastActorDrawn >= maxEntries)
						continue;

					string actorClass = "Unknown";
					var displayInfo = actor.GetComponent<ActorDisplayInformation>();
					if (displayInfo != null)
					{
						actorClass = displayInfo.actorClass.ToString();
					}

					var sceneName = GetSceneName(actor.gameObject);
					var labelPos = actor.GetHealthBarPosition();
					labelPos.y += 0.8f;
					var label = $"[ACT] {GetActorName(actor)} | A:{alignmentName} C:{actorClass} Dead:{actor.dead} | P:{FormatVec3(actorPos)} | S:{sceneName}";
					ESP.AddString(label, labelPos, DebugActorColor);

					if (Settings.debugDrawManagerLines)
					{
						ESP.AddLine(localPos, actorPos, DebugActorColor);
					}

					_lastActorDrawn++;
				}
			}
		}

		private static void DrawAllGroundItems(GameObject localPlayer)
		{
			_lastItemsDrawn = 0;

			if (!Settings.debugDrawAllGroundItems || GroundItemVisuals.all == null || GroundItemVisuals.all._list == null)
				return;

			int maxEntries = Mathf.Clamp(Settings.debugMaxEntriesPerSystem, 10, 500);
			var localPos = localPlayer.transform.position;
			float maxDistance = Settings.drawDistance;

			foreach (var item in GroundItemVisuals.all._list)
			{
				if (item == null || item.gameObject == null || !item.gameObject.activeInHierarchy)
					continue;

				var itemPos = item.transform.position;
				if (!Settings.debugIgnoreDistanceCulling && Vector3.Distance(localPos, itemPos) > maxDistance)
					continue;

				if (_lastItemsDrawn >= maxEntries)
					continue;

				string itemName = item.itemData?.FullName ?? item.name;
				string rarity = item.groundItemRarityVisuals?.name ?? "Unknown";
				string sceneName = GetSceneName(item.gameObject);
				string label = $"[ITEM] {itemName} | R:{rarity} | P:{FormatVec3(itemPos)} | S:{sceneName}";

				ESP.AddString(label, itemPos, DebugItemColor);
				if (Settings.debugDrawManagerLines)
				{
					ESP.AddLine(localPos, itemPos, DebugItemColor);
				}

				_lastItemsDrawn++;
			}
		}

		private static void DrawAllGroundGold(GameObject localPlayer)
		{
			_lastGoldDrawn = 0;

			if (!Settings.debugDrawAllGroundGold || GroundGoldVisuals.all == null || GroundGoldVisuals.all._list == null)
				return;

			int maxEntries = Mathf.Clamp(Settings.debugMaxEntriesPerSystem, 10, 500);
			var localPos = localPlayer.transform.position;
			float maxDistance = Settings.drawDistance;

			foreach (var gold in GroundGoldVisuals.all._list)
			{
				if (gold == null || gold.gameObject == null || !gold.gameObject.activeInHierarchy)
					continue;

				var goldPos = gold.transform.position;
				if (!Settings.debugIgnoreDistanceCulling && Vector3.Distance(localPos, goldPos) > maxDistance)
					continue;

				if (_lastGoldDrawn >= maxEntries)
					continue;

				string sceneName = GetSceneName(gold.gameObject);
				string label = $"[GOLD] {gold.goldValue} | P:{FormatVec3(goldPos)} | S:{sceneName}";

				ESP.AddString(label, goldPos, DebugGoldColor);
				if (Settings.debugDrawManagerLines)
				{
					ESP.AddLine(localPos, goldPos, DebugGoldColor);
				}

				_lastGoldDrawn++;
			}
		}

		private static void RefreshLocalPlayerSnapshot(GameObject localPlayer)
		{
			var t = localPlayer.transform;
			var activeScene = SceneManager.GetActiveScene();
			var localScene = localPlayer.scene;
			int componentCount = localPlayer.GetComponents<Component>().Length;
			int childCount = t.childCount;

			string alignment = "Unknown";
			string actorClass = "Unknown";
			FindActorManagerMetadata(localPlayer, ref alignment, ref actorClass);

			SnapshotBuilder.Clear();
			SnapshotBuilder.AppendLine($"Scene: {activeScene.name} | Player Scene: {localScene.name} | Offline: {ObjectManager.IsOfflineMode()}");
			SnapshotBuilder.AppendLine($"Player: {localPlayer.name} | ID: {localPlayer.GetInstanceID()} | Active: {localPlayer.activeInHierarchy}");
			SnapshotBuilder.AppendLine($"Pos: {FormatVec3(t.position)} | Rot: {FormatVec3(t.eulerAngles)} | Scale: {FormatVec3(t.localScale)}");
			SnapshotBuilder.AppendLine($"Children: {childCount} | Components: {componentCount}");
			SnapshotBuilder.AppendLine($"Alignment: {alignment} | ActorClass: {actorClass}");
			SnapshotBuilder.AppendLine($"Drawn This Frame => Actors: {_lastActorDrawn}/{_lastActorScanned}, Items: {_lastItemsDrawn}, Gold: {_lastGoldDrawn}");
			SnapshotBuilder.AppendLine("Note: Debug world labels/lines use the base ESP label/line visibility toggles.");
			_localPlayerSnapshot = SnapshotBuilder.ToString();
		}

		private static void FindActorManagerMetadata(GameObject localPlayer, ref string alignment, ref string actorClass)
		{
			if (ActorManager.instance == null)
				return;

			int localPlayerId = localPlayer.GetInstanceID();
			foreach (var actorBucket in ActorManager.instance.visuals)
			{
				if (actorBucket.visuals == null || actorBucket.visuals._list == null)
					continue;

				foreach (var actor in actorBucket.visuals._list)
				{
					if (actor == null || actor.gameObject == null)
						continue;
					if (actor.gameObject.GetInstanceID() != localPlayerId)
						continue;

					alignment = actorBucket.alignment?.name ?? "Unknown";
					var info = actor.GetComponent<ActorDisplayInformation>();
					if (info != null)
					{
						actorClass = info.actorClass.ToString();
					}
					return;
				}
			}
		}

		private static string GetSceneName(GameObject go)
		{
			var scene = go.scene;
			return scene.IsValid() ? scene.name : "UnknownScene";
		}

		private static string GetActorName(ActorVisuals actor)
		{
			if (actor.isPlayer && actor.UserIdentity != null && !string.IsNullOrWhiteSpace(actor.UserIdentity.Username))
			{
				return actor.UserIdentity.Username;
			}

			var displayInfo = actor.gameObject.GetComponent<ActorDisplayInformation>();
			if (displayInfo != null)
			{
				string? localized = null;
				try
				{
					localized = displayInfo.GetLocalizedName();
				}
				catch
				{
					// Ignore IL2CPP binding exceptions in debug diagnostics.
				}

				if (!string.IsNullOrWhiteSpace(localized))
				{
					return localized;
				}

				if (!string.IsNullOrWhiteSpace(displayInfo.displayName))
				{
					return displayInfo.displayName;
				}
			}

			return actor.name ?? "UnknownActor";
		}

		private static string FormatVec3(Vector3 v)
		{
			return $"{v.x:F1},{v.y:F1},{v.z:F1}";
		}
	}
}
#endif

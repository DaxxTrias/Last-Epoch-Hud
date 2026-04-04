#if DEBUG
using System;
using System.Collections.Generic;
using System.Text;
using Il2Cpp;
using MelonLoader;
using Mod.Game;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Mod.Cheats.ESP
{
	internal static class DebugDiagnostics
	{
		private sealed class CandidateInfo
		{
			public ActorVisuals Actor = null!;
			public int Score;
			public float Distance;
			public bool IsPlayer;
			public string Username = string.Empty;
			public string Alignment = "Unknown";
			public string ActorClass = "Unknown";
			public int ActorId;
			public int RootId;
			public string GameObjectName = string.Empty;
			public string SceneName = "UnknownScene";
			public string Reason = string.Empty;
		}

		private static readonly Color DebugActorColor = new Color(1f, 0.75f, 0.2f, 1f);
		private static readonly Color DebugItemColor = new Color(0.2f, 1f, 0.95f, 1f);
		private static readonly Color DebugGoldColor = new Color(1f, 0.93f, 0.35f, 1f);
		private static readonly Color LocalPlayerColor = new Color(0.35f, 1f, 0.35f, 1f);
		private static readonly StringBuilder SnapshotBuilder = new StringBuilder(1024);
		private static readonly List<CandidateInfo> CandidateBuffer = new List<CandidateInfo>(256);
		private static readonly Rect PanelRect = new Rect(14f, 14f, 980f, 300f);

		private static string _localPlayerSnapshot = "No local player snapshot yet.";
		private static int _lastActorScanned;
		private static int _lastActorDrawn;
		private static int _lastItemsDrawn;
		private static int _lastGoldDrawn;
		private static int _lastActorBuckets;
		private static int _lastPlayerActorsSeen;
		private static string _actorCorrelationSummary = "Actor correlation not computed yet.";
		private static string _candidateSummary = "No actor candidates captured.";
		private static float _nextSnapshotAt;
		private static ActorVisuals? _resolvedLocalActor;
		private static int _resolvedLocalActorId;
		private static int _resolvedLocalActorRootId;
		private static float _resolvedLocalActorDistance;
		private static string _resolvedLocalReason = "No local actor resolved.";

		public static void OnUpdate()
		{
			if (!Settings.debugEnableDiagnostics)
				return;

			var localPlayer = ObjectManager.GetLocalPlayer();
			if (localPlayer == null)
				return;

			ResolveLocalActor(localPlayer);

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

		public static void LogCorrelationSnapshot()
		{
			var localPlayer = ObjectManager.GetLocalPlayer();
			if (localPlayer == null)
			{
				MelonLogger.Msg("[LEHud.Debug] No local player available for correlation dump.");
				return;
			}

			ResolveLocalActor(localPlayer);
			RefreshLocalPlayerSnapshot(localPlayer);
			MelonLogger.Msg("[LEHud.Debug] ===== Local Correlation Snapshot =====");
			var lines = _localPlayerSnapshot.Split('\n');
			for (int i = 0; i < lines.Length; i++)
			{
				var line = lines[i].TrimEnd('\r');
				if (!string.IsNullOrWhiteSpace(line))
				{
					MelonLogger.Msg("[LEHud.Debug] " + line);
				}
			}
		}

		private static void DrawLocalPlayerLabel(GameObject localPlayer)
		{
			var localGoPos = localPlayer.transform.position;
			var actorPos = localGoPos;
			var actorName = localPlayer.name;
			var sceneName = GetSceneName(localPlayer);
			if (_resolvedLocalActor != null && _resolvedLocalActor.gameObject != null)
			{
				actorPos = _resolvedLocalActor.transform.position;
				actorName = GetActorName(_resolvedLocalActor);
				sceneName = GetSceneName(_resolvedLocalActor.gameObject);
			}

			float delta = Vector3.Distance(localGoPos, actorPos);
			var labelPos = actorPos + Vector3.up * 2.0f;
			var label = $"[LOCAL] {actorName} GO:{FormatVec3(localGoPos)} ACT:{FormatVec3(actorPos)} d:{delta:F2} S:{sceneName}";
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

					string actorClass = GetActorClass(actor);
					string username = GetUsername(actor);
					bool isResolvedLocal = IsResolvedLocal(actor);
					string labelPrefix = isResolvedLocal ? "[ACT-LOCAL]" : "[ACT]";
					Color color = isResolvedLocal ? LocalPlayerColor : DebugActorColor;

					var sceneName = GetSceneName(actor.gameObject);
					var labelPos = actorPos + Vector3.up * 1.8f;
					float dist = Vector3.Distance(localPos, actorPos);
					var label = $"{labelPrefix} N:{GetActorName(actor)} U:{NullToDash(username)} GO:{actor.gameObject.name} | A:{alignmentName} C:{actorClass} Player:{actor.isPlayer} Dead:{actor.dead} D:{dist:F1} | P:{FormatVec3(actorPos)} | S:{sceneName}";
					ESP.AddString(label, labelPos, color);

					if (Settings.debugDrawManagerLines)
					{
						ESP.AddLine(localPos, actorPos, color);
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

			var localGoPos = t.position;
			string resolvedActorName = _resolvedLocalActor != null ? GetActorName(_resolvedLocalActor) : "Unresolved";
			string resolvedActorClass = _resolvedLocalActor != null ? GetActorClass(_resolvedLocalActor) : "Unknown";
			string resolvedActorAlignment = ResolveActorAlignment(_resolvedLocalActor);
			string resolvedActorScene = _resolvedLocalActor != null && _resolvedLocalActor.gameObject != null
				? GetSceneName(_resolvedLocalActor.gameObject)
				: "UnknownScene";
			Vector3 resolvedActorPos = _resolvedLocalActor != null ? _resolvedLocalActor.transform.position : localGoPos;

			SnapshotBuilder.Clear();
			SnapshotBuilder.AppendLine($"Scene: {activeScene.name} | Player Scene: {localScene.name} | Offline: {ObjectManager.IsOfflineMode()}");
			SnapshotBuilder.AppendLine($"ObjectManager LocalGO: {localPlayer.name} ID:{localPlayer.GetInstanceID()} RootID:{GetRootId(localPlayer.transform)} Active:{localPlayer.activeInHierarchy}");
			SnapshotBuilder.AppendLine($"LocalGO Pos:{FormatVec3(localGoPos)} | Rot:{FormatVec3(t.eulerAngles)} | Scale:{FormatVec3(t.localScale)}");
			SnapshotBuilder.AppendLine($"Children: {childCount} | Components: {componentCount}");
			SnapshotBuilder.AppendLine($"Resolved Actor: {resolvedActorName} ID:{_resolvedLocalActorId} RootID:{_resolvedLocalActorRootId} Dist:{_resolvedLocalActorDistance:F2}");
			SnapshotBuilder.AppendLine($"Resolved Actor Meta: Alignment:{resolvedActorAlignment} Class:{resolvedActorClass} Scene:{resolvedActorScene} Pos:{FormatVec3(resolvedActorPos)}");
			SnapshotBuilder.AppendLine(_actorCorrelationSummary);
			SnapshotBuilder.AppendLine(_candidateSummary);
			SnapshotBuilder.AppendLine($"Drawn This Frame => Actors: {_lastActorDrawn}/{_lastActorScanned}, Items: {_lastItemsDrawn}, Gold: {_lastGoldDrawn}");
			SnapshotBuilder.AppendLine($"ActorManager buckets:{_lastActorBuckets}, playerActorsSeen:{_lastPlayerActorsSeen}");
			SnapshotBuilder.AppendLine($"Resolver reason: {_resolvedLocalReason}");
			SnapshotBuilder.AppendLine("Hint: press F11 to log a full resolver snapshot.");
			_localPlayerSnapshot = SnapshotBuilder.ToString();
		}

		private static void ResolveLocalActor(GameObject localPlayer)
		{
			_resolvedLocalActor = null;
			_resolvedLocalActorId = 0;
			_resolvedLocalActorRootId = 0;
			_resolvedLocalActorDistance = -1f;
			_resolvedLocalReason = "No candidate selected.";
			_actorCorrelationSummary = "Actor correlation unavailable.";
			_candidateSummary = "No candidates captured.";
			_lastActorBuckets = 0;
			_lastPlayerActorsSeen = 0;
			CandidateBuffer.Clear();

			if (ActorManager.instance == null)
			{
				_actorCorrelationSummary = "ActorManager.instance is null.";
				return;
			}

			int localPlayerId = localPlayer.GetInstanceID();
			int localRootId = GetRootId(localPlayer.transform);
			var localPos = localPlayer.transform.position;
			var localSceneName = GetSceneName(localPlayer);

			foreach (var bucket in ActorManager.instance.visuals)
			{
				_lastActorBuckets++;
				string alignmentName = bucket.alignment?.name ?? "Unknown";
				if (bucket.visuals == null || bucket.visuals._list == null)
					continue;

				foreach (var actor in bucket.visuals._list)
				{
					if (actor == null || actor.gameObject == null)
						continue;
					if (!actor.gameObject.activeInHierarchy)
						continue;

					bool isPlayer = actor.isPlayer;
					if (isPlayer)
					{
						_lastPlayerActorsSeen++;
					}

					string username = GetUsername(actor);
					var actorPos = actor.transform.position;
					float distance = Vector3.Distance(localPos, actorPos);
					int actorId = actor.gameObject.GetInstanceID();
					int actorRootId = GetRootId(actor.transform);
					bool sameScene = IsSameScene(localSceneName, GetSceneName(actor.gameObject));
					bool hasUsername = !string.IsNullOrWhiteSpace(username);
					bool idMatch = actorId == localPlayerId;
					bool rootMatch = actorRootId == localRootId;
					string actorClass = GetActorClass(actor);

					int score = 0;
					if (idMatch) score += 10000;
					if (rootMatch) score += 6000;
					if (isPlayer) score += 1200;
					if (hasUsername) score += 800;
					if (sameScene) score += 250;
					if (alignmentName == "Good") score += 100;
					if (!actor.dead) score += 50;
					score += Mathf.RoundToInt(Mathf.Clamp(300f - (distance * 30f), -400f, 300f));
					if (!isPlayer && alignmentName != "Good") score -= 150;
					if (actor.dead) score -= 100;

					bool keepCandidate = isPlayer || hasUsername || idMatch || rootMatch || distance <= 25f;
					if (!keepCandidate)
						continue;

					string reason = BuildReason(idMatch, rootMatch, isPlayer, hasUsername, sameScene, distance, actor.dead);
					CandidateBuffer.Add(new CandidateInfo
					{
						Actor = actor,
						Score = score,
						Distance = distance,
						IsPlayer = isPlayer,
						Username = username,
						Alignment = alignmentName,
						ActorClass = actorClass,
						ActorId = actorId,
						RootId = actorRootId,
						GameObjectName = actor.gameObject.name ?? "Unnamed",
						SceneName = GetSceneName(actor.gameObject),
						Reason = reason
					});
				}
			}

			if (CandidateBuffer.Count == 0)
			{
				_actorCorrelationSummary = $"No candidates found. LocalGO ID:{localPlayerId} RootID:{localRootId}.";
				return;
			}

			CandidateBuffer.Sort((a, b) => b.Score.CompareTo(a.Score));
			var best = CandidateBuffer[0];
			bool confident = best.ActorId == localPlayerId
				|| best.RootId == localRootId
				|| (best.IsPlayer && best.Distance <= 8f)
				|| best.Score >= 1800;

			if (confident)
			{
				_resolvedLocalActor = best.Actor;
				_resolvedLocalActorId = best.ActorId;
				_resolvedLocalActorRootId = best.RootId;
				_resolvedLocalActorDistance = best.Distance;
				_resolvedLocalReason = best.Reason;
			}
			else
			{
				_resolvedLocalReason = "Top candidate not confident enough.";
			}

			_actorCorrelationSummary =
				$"Correlation: LocalGO(ID:{localPlayerId}/Root:{localRootId}) -> BestActor(ID:{best.ActorId}/Root:{best.RootId}) Score:{best.Score} Dist:{best.Distance:F2}";

			int lines = Mathf.Min(4, CandidateBuffer.Count);
			var sb = new StringBuilder(512);
			sb.Append("Top candidates: ");
			for (int i = 0; i < lines; i++)
			{
				var c = CandidateBuffer[i];
				if (i > 0) sb.Append(" || ");
				sb.Append($"#{i + 1} sc:{c.Score} d:{c.Distance:F1} p:{c.IsPlayer} user:{NullToDash(c.Username)} id:{c.ActorId} root:{c.RootId} go:{c.GameObjectName} a:{c.Alignment} c:{c.ActorClass} r:{c.Reason}");
			}
			_candidateSummary = sb.ToString();
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

		private static string GetActorClass(ActorVisuals actor)
		{
			var info = actor.GetComponent<ActorDisplayInformation>();
			return info != null ? info.actorClass.ToString() : "Unknown";
		}

		private static string GetUsername(ActorVisuals actor)
		{
			if (actor.UserIdentity == null)
				return string.Empty;
			return actor.UserIdentity.Username ?? string.Empty;
		}

		private static string ResolveActorAlignment(ActorVisuals? actor)
		{
			if (actor == null || actor.gameObject == null || ActorManager.instance == null)
				return "Unknown";

			int actorId = actor.gameObject.GetInstanceID();
			foreach (var bucket in ActorManager.instance.visuals)
			{
				if (bucket.visuals == null || bucket.visuals._list == null)
					continue;
				foreach (var entry in bucket.visuals._list)
				{
					if (entry == null || entry.gameObject == null)
						continue;
					if (entry.gameObject.GetInstanceID() == actorId)
						return bucket.alignment?.name ?? "Unknown";
				}
			}

			return "Unknown";
		}

		private static bool IsResolvedLocal(ActorVisuals actor)
		{
			return _resolvedLocalActor != null
				&& actor.gameObject != null
				&& actor.gameObject.GetInstanceID() == _resolvedLocalActorId;
		}

		private static int GetRootId(Transform tr)
		{
			var root = tr.root;
			return root != null && root.gameObject != null ? root.gameObject.GetInstanceID() : tr.gameObject.GetInstanceID();
		}

		private static bool IsSameScene(string a, string b)
		{
			return string.Equals(a, b, StringComparison.Ordinal);
		}

		private static string BuildReason(
			bool idMatch,
			bool rootMatch,
			bool isPlayer,
			bool hasUsername,
			bool sameScene,
			float distance,
			bool dead)
		{
			var sb = new StringBuilder(96);
			if (idMatch) sb.Append("id ");
			if (rootMatch) sb.Append("root ");
			if (isPlayer) sb.Append("player ");
			if (hasUsername) sb.Append("user ");
			if (sameScene) sb.Append("scene ");
			if (distance <= 5f) sb.Append("near ");
			if (dead) sb.Append("dead ");
			var result = sb.ToString().Trim();
			return string.IsNullOrEmpty(result) ? "none" : result;
		}

		private static string NullToDash(string value)
		{
			return string.IsNullOrWhiteSpace(value) ? "-" : value;
		}
	}
}
#endif

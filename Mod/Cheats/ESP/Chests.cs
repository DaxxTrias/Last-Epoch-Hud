using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Il2Cpp;
using MelonLoader;
using Mod.Game;

namespace Mod.Cheats.ESP
{
	internal static class Chests
	{
		private static GameObject? s_chestManager;
		private static readonly List<Transform> s_chestTransforms = new List<Transform>(16);
		private static readonly Color ChestColor = Drawing.BloodOrange;
		private const int ManagerSearchIntervalFrames = 180;
		private const int CacheRebuildIntervalFramesWhenEmpty = 20;
		private const int CacheRebuildIntervalFramesWhenPopulated = 120;
		private const int RebuildBatchSize = 64;
		private const int DiagnosticMinReportIntervalFrames = 300;
		private const int DiagnosticMaxCandidatesPerReport = 48;
		private const int DiagnosticMaxCachedChestPaths = 24;
		private static int s_nextManagerSearchFrame;
		private static int s_nextCacheRebuildFrame;
		private static bool s_rebuildInProgress;
		private static Transform? s_rebuildRoot;
		private static int s_rebuildChildIndex;
		private static int s_lastNoManagerLogFrame = -10000;
		private static int s_lastDiagnosticReportFrame = -10000;
		private static int s_lastDiagnosticChestCount = -1;
		private static string s_lastDiagnosticManagerPath = string.Empty;

		public static void OnSceneChanged()
		{
			s_chestManager = null;
			s_chestTransforms.Clear();
			s_nextManagerSearchFrame = 0;
			s_nextCacheRebuildFrame = 0;
			s_rebuildInProgress = false;
			s_rebuildRoot = null;
			s_rebuildChildIndex = 0;
			s_lastNoManagerLogFrame = -10000;
			s_lastDiagnosticReportFrame = -10000;
			s_lastDiagnosticChestCount = -1;
			s_lastDiagnosticManagerPath = string.Empty;
		}

		private static bool IsDiagnosticsEnabled()
		{
			return false;
		}

		private static string BuildTransformPath(Transform? tr)
		{
			if (tr == null) return "<null>";

			var segments = new List<string>(12);
			var current = tr;
			int guard = 0;
			while (current != null && guard < 64)
			{
				segments.Add(string.IsNullOrWhiteSpace(current.name) ? "<unnamed>" : current.name);
				current = current.parent;
				guard++;
			}

			segments.Reverse();
			return string.Join("/", segments);
		}

		private static void LogManagerSelection(string source, GameObject manager)
		{
			if (!IsDiagnosticsEnabled()) return;
			if (manager == null || manager.transform == null) return;
			var path = BuildTransformPath(manager.transform);
			MelonLogger.Msg($"[LEHud.ESP.Chests] Manager selected via {source}. path={path}, children={manager.transform.childCount}");
		}

		private static void TryFindManager()
		{
			if (s_chestManager != null) return;
			if (Time.frameCount < s_nextManagerSearchFrame) return;
			s_nextManagerSearchFrame = Time.frameCount + ManagerSearchIntervalFrames;

			// Primary: exact-name lookup across known variants
			s_chestManager = GameObject.Find("ChestPlacementManager");
			if (s_chestManager != null)
			{
				LogManagerSelection("exact:ChestPlacementManager", s_chestManager);
				return;
			}
			s_chestManager = GameObject.Find("Chest Placement Manager");
			if (s_chestManager != null)
			{
				LogManagerSelection("exact:Chest Placement Manager", s_chestManager);
				return;
			}

			if (s_chestManager == null && IsDiagnosticsEnabled() && Time.frameCount - s_lastNoManagerLogFrame >= ManagerSearchIntervalFrames)
			{
				s_lastNoManagerLogFrame = Time.frameCount;
				MelonLogger.Msg("[LEHud.ESP.Chests] Manager search pass found no valid chest manager.");
			}
		}

		private static bool LooksLikeChest(GameObject go)
		{
			if (go == null) return false;
			var name = go.name ?? string.Empty;
			// Exclude placeholder/empty nodes used only for placement
			if (name.StartsWith("Chest Spot", StringComparison.OrdinalIgnoreCase)) return false;

			// Names often: "Chest(Clone)", variants with prefixes; any with "Chest" should qualify
			if (name.IndexOf("Chest", StringComparison.OrdinalIgnoreCase) >= 0) return true;

			// Component-based hint seen in inspector for actual chests
			if (go.GetComponent<ChestVisualsCreator>() != null) return true;

			return false;
		}

		private static bool IsComponentEnabled(Component comp)
		{
			try
			{
				var behaviour = comp as Behaviour;
				if (behaviour != null) return behaviour.enabled;
			}
			catch (Exception) { }
			return true;
		}

		private static bool IsKnownConsumedChestState(GameObject go)
		{
			var root = go.transform;
			if (root == null) return false;

			bool foundEnableOnClick = false;
			bool foundDisableOnClick = false;
			bool enableOnClickActive = false;
			bool disableOnClickActive = false;

			for (int i = 0; i < root.childCount; i++)
			{
				var child = root.GetChild(i);
				if (child == null || child.gameObject == null) continue;

				var childGo = child.gameObject;
				var childName = childGo.name ?? string.Empty;

				if (childName.Equals("Enable On Click", StringComparison.OrdinalIgnoreCase))
				{
					foundEnableOnClick = true;
					enableOnClickActive = childGo.activeSelf;
				}
				else if (childName.Equals("Disable On Click", StringComparison.OrdinalIgnoreCase))
				{
					foundDisableOnClick = true;
					disableOnClickActive = childGo.activeSelf;
				}

				if (foundEnableOnClick && foundDisableOnClick) break;
			}

			// Only classify consumed when both known state markers are present.
			if (!foundEnableOnClick || !foundDisableOnClick) return false;
			return enableOnClickActive && !disableOnClickActive;
		}

		private static bool IsChestInteractable(GameObject go)
		{
			if (!go.activeInHierarchy) return false;
			if (IsKnownConsumedChestState(go)) return false;

			var outline = go.GetComponent<OutlineOnMouseOver>();
			if (outline != null && IsComponentEnabled(outline)) return true;

			var clickListener = go.GetComponent<WorldObjectClickListener>();
			if (clickListener != null && IsComponentEnabled(clickListener)) return true;

			var root = go.transform;
			if (root == null) return false;

			// Some chest variants place interaction components on an immediate child mesh root.
			for (int i = 0; i < root.childCount; i++)
			{
				var child = root.GetChild(i);
				if (child == null || child.gameObject == null || !child.gameObject.activeInHierarchy) continue;

				var childOutline = child.gameObject.GetComponent<OutlineOnMouseOver>();
				if (childOutline != null && IsComponentEnabled(childOutline)) return true;

				var childClickListener = child.gameObject.GetComponent<WorldObjectClickListener>();
				if (childClickListener != null && IsComponentEnabled(childClickListener)) return true;
			}

			return false;
		}

		private static bool ContainsTransformReference(List<Transform> list, Transform target)
		{
			for (int i = 0; i < list.Count; i++)
			{
				if (ReferenceEquals(list[i], target)) return true;
			}
			return false;
		}

		private static bool IsEligibleChest(Transform tr)
		{
			if (tr == null) return false;
			var go = tr.gameObject;
			if (go == null || !go.activeInHierarchy) return false;
			if (!LooksLikeChest(go)) return false;
			if (!IsChestInteractable(go)) return false;
			return true;
		}

		private static bool TryResolveChestTransform(Transform candidate, out Transform chestTransform)
		{
			chestTransform = null!;
			if (candidate == null) return false;

			// Most scenes: direct child is the chest root.
			if (IsEligibleChest(candidate))
			{
				chestTransform = candidate;
				return true;
			}

			// Some scenes: manager child is a wrapper/slot and chest is nested one level down.
			for (int i = 0; i < candidate.childCount; i++)
			{
				var child = candidate.GetChild(i);
				if (!IsEligibleChest(child)) continue;
				chestTransform = child;
				return true;
			}

			return false;
		}

		private static bool TryResolveChestTransformDiagnostic(Transform candidate, out Transform chestTransform, out string resolution)
		{
			chestTransform = null!;
			resolution = string.Empty;

			if (candidate == null)
			{
				resolution = "candidate null";
				return false;
			}

			var candidateGo = candidate.gameObject;
			if (candidateGo == null)
			{
				resolution = "candidate gameObject null";
				return false;
			}

			if (!candidateGo.activeInHierarchy)
			{
				resolution = "candidate inactive";
				return false;
			}

			bool candidateLooksLikeChest = LooksLikeChest(candidateGo);
			bool candidateInteractable = candidateLooksLikeChest && IsChestInteractable(candidateGo);
			if (candidateLooksLikeChest && candidateInteractable)
			{
				chestTransform = candidate;
				resolution = "direct";
				return true;
			}

			bool anyNestedChestLike = false;
			for (int i = 0; i < candidate.childCount; i++)
			{
				var child = candidate.GetChild(i);
				if (child == null || child.gameObject == null || !child.gameObject.activeInHierarchy) continue;
				if (!LooksLikeChest(child.gameObject)) continue;

				anyNestedChestLike = true;
				if (!IsChestInteractable(child.gameObject)) continue;

				chestTransform = child;
				resolution = "nested";
				return true;
			}

			if (!candidateLooksLikeChest && !anyNestedChestLike)
			{
				resolution = "no chest-like candidate";
				return false;
			}

			if (candidateLooksLikeChest && !candidateInteractable && !anyNestedChestLike)
			{
				resolution = "direct chest not interactable";
				return false;
			}

			if (anyNestedChestLike)
			{
				resolution = "nested chest-like nodes not interactable";
				return false;
			}

			resolution = "candidate rejected";
			return false;
		}

		private static void MaybeEmitScanDiagnostics(Transform? root, string trigger)
		{
			if (!IsDiagnosticsEnabled()) return;
			if (root == null) return;

			var managerPath = BuildTransformPath(root);
			int frame = Time.frameCount;
			int chestCount = s_chestTransforms.Count;
			bool changed = chestCount != s_lastDiagnosticChestCount
				|| !string.Equals(managerPath, s_lastDiagnosticManagerPath, StringComparison.Ordinal);
			if (!changed && frame - s_lastDiagnosticReportFrame < DiagnosticMinReportIntervalFrames) return;

			s_lastDiagnosticReportFrame = frame;
			s_lastDiagnosticChestCount = chestCount;
			s_lastDiagnosticManagerPath = managerPath;

			int childCount = root.childCount;
			int inspectCount = childCount < DiagnosticMaxCandidatesPerReport ? childCount : DiagnosticMaxCandidatesPerReport;
			int resolvedDirect = 0;
			int resolvedNested = 0;
			int rejected = 0;

			var sb = new StringBuilder(4096);
			sb.AppendLine($"[LEHud.ESP.Chests] Scan report ({trigger})");
			sb.AppendLine($"  managerPath={managerPath}");
			sb.AppendLine($"  managerChildren={childCount}, inspected={inspectCount}, cachedChests={chestCount}, frame={frame}");

			for (int i = 0; i < inspectCount; i++)
			{
				var candidate = root.GetChild(i);
				if (candidate == null)
				{
					rejected++;
					sb.AppendLine($"  - [{i}] <null child>");
					continue;
				}

				if (TryResolveChestTransformDiagnostic(candidate, out var resolved, out var resolution))
				{
					if (string.Equals(resolution, "direct", StringComparison.Ordinal))
					{
						resolvedDirect++;
					}
					else
					{
						resolvedNested++;
					}

					sb.AppendLine($"  + [{i}] {BuildTransformPath(candidate)} -> {BuildTransformPath(resolved)} ({resolution})");
				}
				else
				{
					rejected++;
					sb.AppendLine($"  - [{i}] {BuildTransformPath(candidate)} ({resolution})");
				}
			}

			if (childCount > inspectCount)
			{
				sb.AppendLine($"  ... truncated manager candidates: {childCount - inspectCount}");
			}

			sb.AppendLine($"  summary: direct={resolvedDirect}, nested={resolvedNested}, rejected={rejected}");

			int cachedToPrint = s_chestTransforms.Count < DiagnosticMaxCachedChestPaths ? s_chestTransforms.Count : DiagnosticMaxCachedChestPaths;
			if (cachedToPrint > 0)
			{
				sb.AppendLine("  cached chest paths:");
				for (int i = 0; i < cachedToPrint; i++)
				{
					var chest = s_chestTransforms[i];
					if (chest == null) continue;
					sb.AppendLine($"    * {BuildTransformPath(chest)}");
				}
				if (s_chestTransforms.Count > cachedToPrint)
				{
					sb.AppendLine($"    ... truncated cached chest paths: {s_chestTransforms.Count - cachedToPrint}");
				}
			}

			MelonLogger.Msg(sb.ToString());
		}

		private static void BeginRebuildIfNeeded()
		{
			TryFindManager();
			if (s_chestManager == null) return;
			if (s_rebuildInProgress) return;
			if (Time.frameCount < s_nextCacheRebuildFrame) return;

			s_rebuildRoot = s_chestManager.transform;
			if (s_rebuildRoot == null) return;

			s_rebuildInProgress = true;
			s_rebuildChildIndex = 0;
			s_chestTransforms.Clear();
		}

		private static void ProcessRebuildBatch()
		{
			if (!s_rebuildInProgress) return;
			if (s_rebuildRoot == null || s_rebuildRoot.gameObject == null)
			{
				s_rebuildInProgress = false;
				s_rebuildRoot = null;
				s_rebuildChildIndex = 0;
				s_nextCacheRebuildFrame = Time.frameCount + CacheRebuildIntervalFramesWhenEmpty;
				return;
			}

			int processed = 0;
			int childCount = s_rebuildRoot.childCount;

			while (processed < RebuildBatchSize && s_rebuildChildIndex < childCount)
			{
				var child = s_rebuildRoot.GetChild(s_rebuildChildIndex);
				s_rebuildChildIndex++;
				processed++;

				if (child == null) continue;
				if (!TryResolveChestTransform(child, out var chestTransform)) continue;
				if (ContainsTransformReference(s_chestTransforms, chestTransform)) continue;

				s_chestTransforms.Add(chestTransform);
			}

			if (s_rebuildChildIndex >= childCount)
			{
				var completedRoot = s_rebuildRoot;
				s_rebuildInProgress = false;
				s_rebuildRoot = null;
				s_rebuildChildIndex = 0;
				s_nextCacheRebuildFrame = Time.frameCount + (s_chestTransforms.Count == 0
					? CacheRebuildIntervalFramesWhenEmpty
					: CacheRebuildIntervalFramesWhenPopulated);
				MaybeEmitScanDiagnostics(completedRoot, "rebuild-complete");
			}
		}

		public static void OnUpdate()
		{
			if (!ObjectManager.HasPlayer()) return;
			if (!Settings.espShowChests) return;

			BeginRebuildIfNeeded();
			ProcessRebuildBatch();
			if (s_chestTransforms.Count == 0) return;

			var player = ObjectManager.GetLocalPlayer();
			if (player == null) return;
			var playerPos = player.transform.position;
			float maxDist = Settings.drawDistance;
			float maxDistSq = maxDist * maxDist;
			float maxVertical = Settings.espVerticalCullMeters;

			for (int i = 0; i < s_chestTransforms.Count; i++)
			{
				var tr = s_chestTransforms[i];
				if (tr == null) continue;
				var go = tr.gameObject;
				if (go == null || !go.activeInHierarchy) continue;
				if (IsKnownConsumedChestState(go)) continue;
				// Interactable checks are done during periodic cache rebuild to keep this hot path light.

				// Additional sanity: require an enabled collider and at least one enabled renderer when possible
				// var collider = go.GetComponent<Collider>();
				// if (collider == null || !collider.enabled) continue;
				// var renderer = go.GetComponentInChildren<Renderer>();
				// if (renderer == null || !renderer.enabled) continue;

				var pos = tr.position;
				if (Mathf.Abs(pos.y - playerPos.y) > maxVertical) continue;
				var delta = pos - playerPos;
				if (delta.sqrMagnitude > maxDistSq) continue;

				var labelPos = pos; labelPos.y += 0.5f;
				if (Settings.showESPLines) ESP.AddLine(playerPos, pos, ChestColor);
				if (Settings.showESPLabels) ESP.AddString("Chest", labelPos, ChestColor);
			}
		}

		// Legacy no-ops to remain compatible with existing patches; manager scan is authoritative
		public static void Register(GameObject go) { }
		public static void Unregister(GameObject go) { }
	}
} 
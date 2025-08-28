using System;
using System.Collections.Generic;
using UnityEngine;
using Il2Cpp;
using Mod.Game;

namespace Mod.Cheats.ESP
{
	internal static class Chests
	{
		private static GameObject? s_chestManager;
		private static readonly List<Transform> s_chestTransforms = new List<Transform>(16);
		private static readonly List<string> s_chestNames = new List<string>(16);
		private static readonly Color ChestColor = Drawing.BloodOrange;

		public static void OnSceneChanged()
		{
			s_chestManager = null;
			s_chestTransforms.Clear();
			s_chestNames.Clear();
		}

		private static string SanitizeLabel(string? value)
		{
			if (string.IsNullOrEmpty(value)) return string.Empty;
			var sanitized = value.Replace("\r", " ").Replace("\n", " ").Replace("\t", " ");
			return sanitized.Trim();
		}

		private static string GetDisplayName(GameObject go)
		{
			try
			{
				var info = go.GetComponent<DisplayInformation>();
				if (info != null)
				{
					string? localized = null;
					try { localized = info.GetLocalizedName(); } catch (Exception) { }
					if (!string.IsNullOrWhiteSpace(localized)) return SanitizeLabel(localized);
					if (!string.IsNullOrWhiteSpace(info.displayName)) return SanitizeLabel(info.displayName);
				}
			}
			catch (Exception) { }
			return SanitizeLabel(go.name);
		}

		private static void TryFindManager()
		{
			if (s_chestManager != null) return;

			// Primary: exact-name lookup
			s_chestManager = GameObject.Find("Chest Placement Manager");
			if (s_chestManager != null) return;

			// Fallback: broader scan for an object that looks like a chest container
			var allTransforms = Resources.FindObjectsOfTypeAll<Transform>();
			for (int i = 0; i < allTransforms.Length; i++)
			{
				var tr = allTransforms[i];
				if (tr == null || tr.gameObject == null) continue;
				var go = tr.gameObject;
				var name = go.name ?? string.Empty;
				if (name.IndexOf("Chest", StringComparison.OrdinalIgnoreCase) >= 0 && tr.childCount > 0)
				{
					s_chestManager = go;
					break;
				}
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

		private static bool IsChestInteractable(GameObject go)
		{
			if (!go.activeInHierarchy) return false;

			var outline = go.GetComponent<OutlineOnMouseOver>();
			if (outline == null) return false;
			return IsComponentEnabled(outline);
		}

		private static void RebuildChestCacheIfNeeded()
		{
			TryFindManager();
			if (s_chestManager == null) return;

			var t = s_chestManager.transform;
			if (t == null) return;

			int currentActiveChests = 0;
			for (int i = 0; i < t.childCount; i++)
			{
				var child = t.GetChild(i);
				if (child != null && child.gameObject != null && child.gameObject.activeInHierarchy && LooksLikeChest(child.gameObject) && IsChestInteractable(child.gameObject))
				{
					currentActiveChests++;
				}
			}

			if (currentActiveChests == s_chestTransforms.Count && currentActiveChests > 0)
			{
				return;
			}

			s_chestTransforms.Clear();
			s_chestNames.Clear();

			for (int i = 0; i < t.childCount; i++)
			{
				var child = t.GetChild(i);
				if (child == null) continue;
				var go = child.gameObject;
				if (go == null || !go.activeInHierarchy) continue;
				if (!LooksLikeChest(go)) continue;
				if (!IsChestInteractable(go)) continue;

				s_chestTransforms.Add(child);
				s_chestNames.Add(GetDisplayName(go));
			}
		}

		public static void OnUpdate()
		{
			if (!ObjectManager.HasPlayer()) return;
			if (!Settings.espShowChests) return;

			RebuildChestCacheIfNeeded();
			if (s_chestTransforms.Count == 0) return;

			var player = ObjectManager.GetLocalPlayer();
			if (player == null) return;
			var playerPos = player.transform.position;
			float maxDist = Settings.drawDistance;
			float maxVertical = Settings.espVerticalCullMeters;

			for (int i = 0; i < s_chestTransforms.Count; i++)
			{
				var tr = s_chestTransforms[i];
				if (tr == null) continue;
				var go = tr.gameObject;
				if (go == null || !go.activeInHierarchy) continue;
				if (!IsChestInteractable(go)) continue; // consumed/disabled since cache built

				// Additional sanity: require an enabled collider and at least one enabled renderer when possible
				// var collider = go.GetComponent<Collider>();
				// if (collider == null || !collider.enabled) continue;
				// var renderer = go.GetComponentInChildren<Renderer>();
				// if (renderer == null || !renderer.enabled) continue;

				var pos = tr.position;
				if (Mathf.Abs(pos.y - playerPos.y) > maxVertical) continue;
				if (Vector3.Distance(playerPos, pos) > maxDist) continue;

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
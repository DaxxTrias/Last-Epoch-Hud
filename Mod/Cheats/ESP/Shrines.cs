using Il2Cpp;
using Mod.Game;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mod.Cheats.ESP
{
	internal static class Shrines
	{
		private static GameObject? s_shrineManager;
		private static readonly List<Transform> s_shrineTransforms = new List<Transform>(16);
		private static readonly List<string> s_shrineNames = new List<string>(16);
		private static readonly Color ShrineColor = Drawing.BloodOrange;

		private static string SanitizeLabel(string? value)
		{
			if (string.IsNullOrEmpty(value)) return string.Empty;
			var sanitized = value.Replace("\r", " ").Replace("\n", " ").Replace("\t", " ");
			return sanitized.Trim();
		}

		public static void OnSceneChanged()
		{
			// Clear cached references on scene transitions
			s_shrineManager = null;
			s_shrineTransforms.Clear();
			s_shrineNames.Clear();
		}

		private static void TryFindManager()
		{
			if (s_shrineManager != null) return;

			// Primary: exact-name lookup (observed in inspector)
			s_shrineManager = GameObject.Find("Shrine Placement Manager");
			if (s_shrineManager != null) return;

			// Fallback: broader scan for any object that looks like a shrine container
			// We avoid allocations by iterating transforms
			var allTransforms = Resources.FindObjectsOfTypeAll<Transform>();
			for (int i = 0; i < allTransforms.Length; i++)
			{
				var tr = allTransforms[i];
				if (tr == null || tr.gameObject == null) continue;
				var go = tr.gameObject;
				// Heuristic: manager with multiple children named "Shrine Spot" or containing active shrine clones
				if (go.name != null && go.name.IndexOf("Shrine", StringComparison.OrdinalIgnoreCase) >= 0 && tr.childCount > 0)
				{
					s_shrineManager = go;
					break;
				}
			}
		}

		private static string GetDisplayName(GameObject shrineGo)
		{
			try
			{
				var info = shrineGo.GetComponent<DisplayInformation>();
				if (info != null)
				{
					string? localized = null;
					try { localized = info.GetLocalizedName(); } catch (Exception) { }
					if (!string.IsNullOrWhiteSpace(localized)) return SanitizeLabel(localized);
					if (!string.IsNullOrWhiteSpace(info.displayName)) return SanitizeLabel(info.displayName);
				}
			}
			catch (Exception) { /* Some builds may lack DisplayInformation; fall back to name */ }

			return SanitizeLabel(shrineGo.name);
		}

		private static bool LooksLikeShrine(GameObject go)
		{
			if (go == null) return false;
			var name = go.name ?? string.Empty;
			// Exclude placeholder/empty nodes used only for placement
			// if (name.Equals("Shrine Spot", StringComparison.OrdinalIgnoreCase)) return false;
			
			// Also exclude common variants like "Shrine Spot (Clone)" or numbered spots
			if (name.StartsWith("Shrine Spot", StringComparison.OrdinalIgnoreCase)) return false;
			// Names often: "Maelstrom Shrine(Clone)", "Loot Lizard Shrine - Low Level(Clone)"
			if (name.IndexOf("Shrine", StringComparison.OrdinalIgnoreCase) >= 0) return true;

			// Component-based hints seen in inspector
			if (go.GetComponent<UseAbilityInteraction>() != null) return true;
			if (go.GetComponent<ActivateGameObjectInteraction>() != null) return true;
			if (go.GetComponent<WorldObjectClickListener>() != null) return true;

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
			return true; // assume enabled if unknown
		}

		private static bool IsShrineInteractable(GameObject go)
		{
			// If object is inactive, it is not interactable
			if (!go.activeInHierarchy) return false;

			bool anyFound = false;
			bool anyEnabled = false;

			// var uai = go.GetComponent<UseAbilityInteraction>();
			// if (uai != null) { anyFound = true; anyEnabled |= IsComponentEnabled(uai); }
			// var agi = go.GetComponent<ActivateGameObjectInteraction>();
			// if (agi != null) { anyFound = true; anyEnabled |= IsComponentEnabled(agi); }
			// var wocl = go.GetComponent<WorldObjectClickListener>();
			// if (wocl != null) { anyFound = true; anyEnabled |= IsComponentEnabled(wocl); }
			// var collider = go.GetComponent<BoxCollider>();
			// if (collider != null) { anyFound = true; anyEnabled |= collider.enabled; }
			var outline = go.GetComponent<OutlineOnMouseOver>();
			if (outline != null) { anyFound = true; anyEnabled |= IsComponentEnabled(outline); }

			// If no known components are present, treat as not interactable to avoid placeholders
			return anyFound && anyEnabled;
		}

		private static void RebuildShrineCacheIfNeeded()
		{
			TryFindManager();
			if (s_shrineManager == null) return;

			// If we already cached children and counts match, keep cache
			var t = s_shrineManager.transform;
			if (t == null) return;

			// Simple validation: if cached count != current active shrine children, rebuild
			int currentActiveShrines = 0;
			for (int i = 0; i < t.childCount; i++)
			{
				var child = t.GetChild(i);
				if (child != null && child.gameObject != null && child.gameObject.activeInHierarchy && LooksLikeShrine(child.gameObject) && IsShrineInteractable(child.gameObject))
				{
					currentActiveShrines++;
				}
			}

			if (currentActiveShrines == s_shrineTransforms.Count && currentActiveShrines > 0)
			{
				return; // Cache still valid enough
			}

			// Rebuild cache
			s_shrineTransforms.Clear();
			s_shrineNames.Clear();

			for (int i = 0; i < t.childCount; i++)
			{
				var child = t.GetChild(i);
				if (child == null) continue;
				var go = child.gameObject;
				if (go == null || !go.activeInHierarchy) continue;
				if (!LooksLikeShrine(go)) continue;
				if (!IsShrineInteractable(go)) continue; // skip consumed/disabled shrines

				s_shrineTransforms.Add(child);
				s_shrineNames.Add(GetDisplayName(go));
			}
		}

		public static void OnUpdate()
		{
			if (!ObjectManager.HasPlayer()) return;
			if (!Settings.espShowShrines) return;

			RebuildShrineCacheIfNeeded();
			if (s_shrineTransforms.Count == 0) return;

			var localPlayer = ObjectManager.GetLocalPlayer();
			if (localPlayer == null) return;
			var playerPos = localPlayer.transform.position;

			for (int i = 0; i < s_shrineTransforms.Count; i++)
			{
				var tr = s_shrineTransforms[i];
				if (tr == null) continue;
				var go = tr.gameObject;
				if (go == null || !go.activeInHierarchy) continue;
				if (!IsShrineInteractable(go)) continue; // was used since cache built

				var pos = tr.position;
				if (Vector3.Distance(playerPos, pos) > Settings.drawDistance) continue;

				var labelPos = pos; labelPos.y += 0.5f;
				var name = (i < s_shrineNames.Count) ? s_shrineNames[i] : SanitizeLabel(go.name);

				// Respect global specials toggle and per-special whitelist
				// If user provided a whitelist, honor it; otherwise draw all shrines
				bool draw = true;
				try { draw = Settings.ShouldDrawShrine(name) || true; } catch (Exception) { draw = true; }
				if (!draw) continue;

				if (Settings.showESPLines) ESP.AddLine(playerPos, pos, ShrineColor);
				if (Settings.showESPLabels) ESP.AddString(name, labelPos, ShrineColor);
			}
		}
	}
} 
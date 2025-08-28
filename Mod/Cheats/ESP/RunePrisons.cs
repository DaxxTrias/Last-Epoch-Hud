using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Il2Cpp;
using Mod.Game;

namespace Mod.Cheats.ESP
{
	internal static class RunePrisons
	{
		private static readonly List<Transform> s_runePrisonTransforms = new List<Transform>(8);
		private static GameObject? s_zoneManager;
		private static bool s_needsScan;
		private static float s_nextScanTime;
		private const float FirstScanDelay = 0.25f;
		private const float RetryScanInterval = 5.0f;
		private static readonly Color SpecialColor = new Color(0.90f, 0.30f, 0.00f, 1f);
		private static PropertyInfo? s_visualsTriggeredProperty;

		public static void OnSceneChanged()
		{
			s_runePrisonTransforms.Clear();
			s_zoneManager = null;
			s_needsScan = true;
			s_nextScanTime = Time.unscaledTime + FirstScanDelay;
			// Cache reflection for optional triggered check if available
			s_visualsTriggeredProperty = typeof(RunePrisonVisuals).GetProperty("triggered", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		}

		private static void TryFindZoneManager()
		{
			if (s_zoneManager != null) return;
			s_zoneManager = GameObject.Find("ZoneManager");
		}

		private static void RebuildOnce()
		{
			TryFindZoneManager();
			if (s_zoneManager == null) { s_nextScanTime = Time.unscaledTime + RetryScanInterval; return; }
			var root = s_zoneManager.transform;
			if (root == null) { s_nextScanTime = Time.unscaledTime + RetryScanInterval; return; }

			s_runePrisonTransforms.Clear();
			for (int i = 0; i < root.childCount; i++)
			{
				var child = root.GetChild(i);
				if (child == null) continue;
				var go = child.gameObject;
				if (go == null) continue;
				// Select direct children with RunePrisonVisuals component
				var visuals = go.GetComponent<RunePrisonVisuals>();
				if (visuals != null && go.activeInHierarchy)
				{
					// Skip if already triggered/consumed when the API is available
					if (s_visualsTriggeredProperty != null)
					{
						object? val = null;
						try { val = s_visualsTriggeredProperty.GetValue(visuals); }
						catch { /* ignore reflection issues */ }
						if (val is bool b && b)
							continue;
					}

					s_runePrisonTransforms.Add(child);
				}
			}

			// Schedule periodic rescans to catch late activations or dynamic spawns
			s_nextScanTime = Time.unscaledTime + RetryScanInterval;
			s_needsScan = true;
		}

		public static void OnUpdate()
		{
			if (!ObjectManager.HasPlayer()) return;
			if (!Settings.espShowRunePrisons) return;

			if (s_needsScan && Time.unscaledTime >= s_nextScanTime)
			{
				RebuildOnce();
			}
			if (s_runePrisonTransforms.Count == 0) return;

			// Prune consumed or inactive rune prisons
			for (int i = s_runePrisonTransforms.Count - 1; i >= 0; i--)
			{
				var tr = s_runePrisonTransforms[i];
				if (tr == null) { s_runePrisonTransforms.RemoveAt(i); continue; }
				var go = tr.gameObject;
				if (go == null || !go.activeInHierarchy) { s_runePrisonTransforms.RemoveAt(i); continue; }

				// If visuals report triggered, remove
				if (s_visualsTriggeredProperty != null)
				{
					var visuals = go.GetComponent<RunePrisonVisuals>();
					if (visuals != null)
					{
						object? val = null;
						try { val = s_visualsTriggeredProperty.GetValue(visuals); }
						catch { /* ignore reflection issues */ }
						if (val is bool b && b)
						{
							s_runePrisonTransforms.RemoveAt(i);
							continue;
						}
					}
				}
			}

			var player = ObjectManager.GetLocalPlayer();
			if (player == null) return;
			var p = player.transform.position;
			float maxDistSq = Settings.drawDistance * Settings.drawDistance;

			for (int i = 0; i < s_runePrisonTransforms.Count; i++)
			{
				var tr = s_runePrisonTransforms[i];
				if (tr == null) continue;
				var go = tr.gameObject;
				if (go == null || !go.activeInHierarchy) continue;

				var pos = tr.position;
				var diff = pos - p;
				if (diff.sqrMagnitude > maxDistSq) continue;
				var labelPos = pos; labelPos.y += 0.5f;

				if (Settings.showESPLines) ESP.AddLine(p, pos, SpecialColor);
				if (Settings.showESPLabels) ESP.AddString("Rune Prison", labelPos, SpecialColor);
			}
		}
	}
} 
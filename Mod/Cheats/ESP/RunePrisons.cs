using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using Il2Cpp;

namespace Mod.Cheats.ESP
{
	internal static class RunePrisons
	{
		private static readonly List<Transform> s_runePrisonTransforms = new List<Transform>(8);
		private static readonly List<Transform> s_sceneTraversalStack = new List<Transform>(128);
		private static bool s_needsScan;
		private static float s_nextScanTime;
		private const float FirstScanDelay = 0.25f;
		private const float RetryScanInterval = 5.0f;
		private static readonly Color SpecialColor = new Color(0.90f, 0.30f, 0.00f, 1f);
		private const string RunePrisonVisualsObjectName = "Rune Prison Visuals(Clone)";
		private static PropertyInfo? s_visualsTriggeredProperty;

		public static void OnSceneChanged()
		{
			s_runePrisonTransforms.Clear();
			s_sceneTraversalStack.Clear();
			s_needsScan = true;
			s_nextScanTime = Time.unscaledTime + FirstScanDelay;
			// Cache reflection for optional triggered check if available
			s_visualsTriggeredProperty = typeof(RunePrisonVisuals).GetProperty("triggered", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		}

		private static bool IsTriggered(RunePrisonVisuals visuals)
		{
			if (s_visualsTriggeredProperty == null) return false;
			object? val = null;
			try { val = s_visualsTriggeredProperty.GetValue(visuals); }
			catch { return false; }
			return val is bool b && b;
		}

		private static void TryAddRunePrison(Transform candidate)
		{
			if (candidate == null) return;
			var go = candidate.gameObject;
			if (go == null || !go.activeInHierarchy) return;
			if (!string.Equals(go.name, RunePrisonVisualsObjectName, StringComparison.Ordinal)) return;

			var visuals = go.GetComponent<RunePrisonVisuals>();
			if (visuals == null) return;
			if (IsTriggered(visuals)) return;

			s_runePrisonTransforms.Add(candidate);
		}

		private static void RebuildOnce(GameObject player)
		{
			var scene = player.scene;
			if (!scene.IsValid() || !scene.isLoaded)
			{
				s_nextScanTime = Time.unscaledTime + RetryScanInterval;
				return;
			}

			s_runePrisonTransforms.Clear();
			s_sceneTraversalStack.Clear();

			var roots = scene.GetRootGameObjects();
			for (int i = 0; i < roots.Length; i++)
			{
				var rootGo = roots[i];
				if (rootGo == null) continue;
				var rootTransform = rootGo.transform;
				if (rootTransform == null) continue;
				s_sceneTraversalStack.Add(rootTransform);
			}

			while (s_sceneTraversalStack.Count > 0)
			{
				int lastIndex = s_sceneTraversalStack.Count - 1;
				var current = s_sceneTraversalStack[lastIndex];
				s_sceneTraversalStack.RemoveAt(lastIndex);
				if (current == null) continue;

				TryAddRunePrison(current);

				for (int i = 0; i < current.childCount; i++)
				{
					var child = current.GetChild(i);
					if (child == null) continue;
					s_sceneTraversalStack.Add(child);
				}
			}

			// Schedule periodic rescans to catch late activations or dynamic spawns
			s_nextScanTime = Time.unscaledTime + RetryScanInterval;
			s_needsScan = true;
		}

		public static void OnUpdate(GameObject player)
		{
			if (!Settings.espShowRunePrisons) return;

			if (s_needsScan && Time.unscaledTime >= s_nextScanTime)
			{
				RebuildOnce(player);
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
						if (IsTriggered(visuals))
						{
							s_runePrisonTransforms.RemoveAt(i);
							continue;
						}
					}
				}
			}

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
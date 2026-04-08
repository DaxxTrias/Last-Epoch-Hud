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
		private static readonly List<Transform> s_scanResults = new List<Transform>(8);
		private static readonly List<Transform> s_sceneTraversalStack = new List<Transform>(128);
		private static bool s_needsScan;
		private static bool s_scanInProgress;
		private static float s_nextScanTime;
		private const float FirstScanDelay = 0.25f;
		private const float RetryScanIntervalWhenEmpty = 20.0f;
		private const float RetryScanIntervalWhenPopulated = 8.0f;
		private const int ScanTraversalBatchSize = 96;
		private static readonly Color SpecialColor = new Color(0.90f, 0.30f, 0.00f, 1f);
		private const string RunePrisonVisualsObjectName = "Rune Prison Visuals(Clone)";
		private static PropertyInfo? s_visualsTriggeredProperty;

		public static void OnSceneChanged()
		{
			s_runePrisonTransforms.Clear();
			s_scanResults.Clear();
			s_sceneTraversalStack.Clear();
			s_needsScan = true;
			s_scanInProgress = false;
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

		private static void TryAddRunePrison(Transform candidate, List<Transform> target)
		{
			if (candidate == null) return;
			var go = candidate.gameObject;
			if (go == null || !go.activeInHierarchy) return;
			if (!string.Equals(go.name, RunePrisonVisualsObjectName, StringComparison.Ordinal)) return;

			var visuals = go.GetComponent<RunePrisonVisuals>();
			if (visuals == null) return;
			if (IsTriggered(visuals)) return;

			target.Add(candidate);
		}

		private static void BeginRebuild(GameObject player)
		{
			var scene = player.scene;
			if (!scene.IsValid() || !scene.isLoaded)
			{
				s_nextScanTime = Time.unscaledTime + RetryScanIntervalWhenEmpty;
				s_needsScan = true;
				return;
			}

			s_scanResults.Clear();
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
			s_scanInProgress = true;
			s_needsScan = false;
		}

		private static void ProcessRebuildBatch(float now)
		{
			if (!s_scanInProgress)
				return;

			int processed = 0;
			while (processed < ScanTraversalBatchSize && s_sceneTraversalStack.Count > 0)
			{
				int lastIndex = s_sceneTraversalStack.Count - 1;
				var current = s_sceneTraversalStack[lastIndex];
				s_sceneTraversalStack.RemoveAt(lastIndex);
				if (current == null)
					continue;

				TryAddRunePrison(current, s_scanResults);

				for (int i = 0; i < current.childCount; i++)
				{
					var child = current.GetChild(i);
					if (child == null)
						continue;
					s_sceneTraversalStack.Add(child);
				}

				processed++;
			}

			if (s_sceneTraversalStack.Count > 0)
				return;

			s_scanInProgress = false;
			s_runePrisonTransforms.Clear();
			for (int i = 0; i < s_scanResults.Count; i++)
			{
				s_runePrisonTransforms.Add(s_scanResults[i]);
			}
			s_scanResults.Clear();

			// Schedule periodic rescans to catch late activations or dynamic spawns.
			float retryInterval = s_runePrisonTransforms.Count == 0
				? RetryScanIntervalWhenEmpty
				: RetryScanIntervalWhenPopulated;
			s_nextScanTime = now + retryInterval;
			s_needsScan = true;
		}

		public static void OnUpdate(GameObject player)
		{
			if (!Settings.espShowRunePrisons) return;

			float now = Time.unscaledTime;
			if (s_needsScan && now >= s_nextScanTime)
			{
				BeginRebuild(player);
			}
			ProcessRebuildBatch(now);

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
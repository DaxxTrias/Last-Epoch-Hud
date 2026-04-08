using System.Globalization;
using System.Text;
using Mod.Game;
using Mod.Utils;
using UnityEngine;

namespace Mod.Cheats
{
	internal static class DpsMeter
	{
		private enum DpsSourceMode
		{
			None = 0,
			OfflineRelay = 1,
			OnlineRaw = 2
		}

		private const int HitEventHit = 1;
		private const int HitEventCrit = 2;
		private const int HitEventKill = 4;
		private const int HitEventFreeze = 8;
		private const int HitEventStun = 16;
		private const int HitEventBlock = 32;
		private const int HitEventMeleeHit = 64;
		private const int HitEventParry = 128;
		private const int HitEventSuperCrit = 256;
		private const float OnlineSameTextGapSeconds = 0.25f;
		private const float OnlineStateTtlSeconds = 12f;
		private const int PanelWindowId = 90873;
		private const float DefaultPanelWidth = 360f;
		private const float DefaultPanelHeight = 320f;
		private const float MinPanelWidth = 320f;
		private const float MinPanelHeight = 250f;
		private const float ResizeGripSize = 18f;

		private readonly struct HitSample
		{
			public readonly float Time;
			public readonly float Damage;

			public HitSample(float time, float damage)
			{
				Time = time;
				Damage = damage;
			}
		}

		private struct OnlineSampleState
		{
			public string LastText;
			public float LastSeenAt;
		}

		private static readonly Queue<HitSample> s_recentHits = new Queue<HitSample>(256);
		private static readonly Dictionary<int, OnlineSampleState> s_onlineStates = new Dictionary<int, OnlineSampleState>(128);
		private static readonly List<int> s_onlinePruneBuffer = new List<int>(64);
		private static readonly Dictionary<uint, int> s_onlineColorHistogram = new Dictionary<uint, int>(24);
		private static readonly StringBuilder s_textBuilder = new StringBuilder(256);
		private static Rect s_panelRect = new Rect(0f, 88f, DefaultPanelWidth, DefaultPanelHeight);

		private static DpsSourceMode s_sourceMode;
		private static bool s_panelInitialized;
		private static bool s_panelResizing;
		private static int s_lastScreenWidth;
		private static int s_lastScreenHeight;
		private static string s_panelText = string.Empty;
		private static GUIStyle? s_panelLabelStyle;
		private static float s_recentDamage;
		private static float s_totalDamage;
		private static int s_totalEvents;
		private static int s_totalHits;
		private static int s_inferredMisses;
		private static int s_critEvents;
		private static int s_superCritEvents;
		private static int s_killEvents;
		private static int s_blockEvents;
		private static int s_parryEvents;
		private static int s_freezeEvents;
		private static int s_stunEvents;
		private static int s_meleeHitEvents;
		private static float s_peakHit;
		private static float s_minHit = float.MaxValue;
		private static float s_currentDps;
		private static float s_peakDps;
		private static float s_minDps = float.MaxValue;
		private static float s_firstHitAt = -1f;
		private static float s_lastHitAt = -1f;
		private static int s_onlineColorSamples;
		private static int s_onlineColorCritEvents;
		private static Color s_onlineLastColor;
		private static bool s_onlineHasLastColor;
		private static Color s_onlineLearnedNormalColor;
		private static bool s_onlineHasLearnedNormalColor;
		private static int s_onlineLearnedNormalCount;
		private static Color s_onlineLearnedCritColor;
		private static bool s_onlineHasLearnedCritColor;
		private static int s_onlineLearnedCritCount;

		public static void OnDamageEvent(object source, float damage, int hitEvents)
		{
			_ = source;
			if (GetCurrentMode() != DpsSourceMode.OfflineRelay || float.IsNaN(damage) || float.IsInfinity(damage))
				return;

			s_totalEvents++;

			bool isSuperCrit = HasFlag(hitEvents, HitEventSuperCrit);
			bool isCrit = HasFlag(hitEvents, HitEventCrit);
			bool hasHitFlag = HasFlag(hitEvents, HitEventHit) || isCrit || isSuperCrit;
			bool hasDamage = damage > 0f;

			if (isSuperCrit)
			{
				s_superCritEvents++;
				s_critEvents++;
			}
			else if (isCrit)
			{
				s_critEvents++;
			}

			if (HasFlag(hitEvents, HitEventKill))
				s_killEvents++;
			if (HasFlag(hitEvents, HitEventBlock))
				s_blockEvents++;
			if (HasFlag(hitEvents, HitEventParry))
				s_parryEvents++;
			if (HasFlag(hitEvents, HitEventFreeze))
				s_freezeEvents++;
			if (HasFlag(hitEvents, HitEventStun))
				s_stunEvents++;
			if (HasFlag(hitEvents, HitEventMeleeHit))
				s_meleeHitEvents++;

			// Approximation: treat no-hit-flag + no-damage as a miss-like event.
			if (!hasHitFlag && !hasDamage)
			{
				s_inferredMisses++;
			}

			if (!hasDamage)
				return;

			float now = Time.unscaledTime;
			if (s_totalHits == 0)
				s_firstHitAt = now;

			s_lastHitAt = now;
			s_totalHits++;
			s_totalDamage += damage;
			s_peakHit = Mathf.Max(s_peakHit, damage);
			s_minHit = Mathf.Min(s_minHit, damage);

			s_recentHits.Enqueue(new HitSample(now, damage));
			s_recentDamage += damage;

			PruneWindow(now);
			RefreshDps();
		}

		public static void OnOnlineDamageTextSample(object source, string? text, Color? textColor)
		{
			if (GetCurrentMode() != DpsSourceMode.OnlineRaw)
				return;

			if (string.IsNullOrWhiteSpace(text) || !TryParseDamageText(text, out float damage) || damage <= 0f)
				return;

			if (!TryGetSourceInstanceId(source, out int instanceId))
				return;

			float now = Time.unscaledTime;
			if (!ShouldAcceptOnlineSample(instanceId, text!, now))
				return;

			s_totalEvents++;
			RegisterHit(damage, now);
			if (textColor.HasValue)
			{
				s_onlineColorSamples++;
				s_onlineLastColor = textColor.Value;
				s_onlineHasLastColor = true;
				RecordOnlineColorSample(textColor.Value);
				UpdateOnlineColorCalibration();
				if (IsLikelyCritColorAdaptive(textColor.Value))
				{
					s_onlineColorCritEvents++;
				}
			}
		}

		public static void OnUpdate()
		{
			if (!Settings.enableDpsMeter)
			{
				MaybeLogOnlineColorSummary("disabled");
				ResetInternal();
				s_sourceMode = DpsSourceMode.None;
				return;
			}

			// Treat player-loss as a session boundary: clear all stats, including peaks.
			if (!ObjectManager.HasPlayer())
			{
				MaybeLogOnlineColorSummary("player-lost");
				ResetInternal();
				s_sourceMode = DpsSourceMode.None;
				return;
			}

			var currentMode = GetCurrentMode();
			if (currentMode != s_sourceMode)
			{
				MaybeLogOnlineColorSummary("mode-changed");
				ResetInternal();
				s_sourceMode = currentMode;
			}
			if (s_sourceMode == DpsSourceMode.None)
				return;

			float now = Time.unscaledTime;
			PruneWindow(now);
			RefreshDps();
			if (s_sourceMode == DpsSourceMode.OnlineRaw)
			{
				PruneOnlineStates(now);
			}

			if (!Settings.dpsMeterAutoReset || s_lastHitAt < 0f)
				return;

			float resetAfter = Mathf.Max(2f, Settings.dpsMeterInactivityResetSeconds);
			if (now - s_lastHitAt >= resetAfter)
			{
				ResetForInactivity();
			}
		}

		public static void OnGUI()
		{
			if (GetCurrentMode() == DpsSourceMode.None)
				return;

			EnsurePanelInitialized();
			EnsurePanelStyle();
			s_panelText = BuildOverlayText(Time.unscaledTime);
			AutoGrowPanelForContent();
			s_panelRect = GUI.Window(PanelWindowId, s_panelRect, (GUI.WindowFunction)DrawPanelWindow, "LEHud DPS Meter");
			ClampPanelToScreen();
			SyncPanelToSettings();
		}

		public static void OnSceneChanged()
		{
			MaybeLogOnlineColorSummary("scene-changed");
			ResetInternal();
			s_sourceMode = DpsSourceMode.None;
		}

		public static void Reset()
		{
			MaybeLogOnlineColorSummary("manual");
			ResetInternal();
		}

		public static void ResetPanelLayout()
		{
			Settings.dpsMeterPanelX = -1f;
			Settings.dpsMeterPanelY = 88f;
			Settings.dpsMeterPanelWidth = DefaultPanelWidth;
			Settings.dpsMeterPanelHeight = DefaultPanelHeight;
			s_panelInitialized = false;
		}

		private static void ResetInternal()
		{
			s_recentHits.Clear();
			s_onlineStates.Clear();
			s_onlinePruneBuffer.Clear();
			s_onlineColorHistogram.Clear();
			s_recentDamage = 0f;
			s_totalDamage = 0f;
			s_totalEvents = 0;
			s_totalHits = 0;
			s_inferredMisses = 0;
			s_critEvents = 0;
			s_superCritEvents = 0;
			s_killEvents = 0;
			s_blockEvents = 0;
			s_parryEvents = 0;
			s_freezeEvents = 0;
			s_stunEvents = 0;
			s_meleeHitEvents = 0;
			s_peakHit = 0f;
			s_minHit = float.MaxValue;
			s_currentDps = 0f;
			s_peakDps = 0f;
			s_minDps = float.MaxValue;
			s_firstHitAt = -1f;
			s_lastHitAt = -1f;
			s_onlineColorSamples = 0;
			s_onlineColorCritEvents = 0;
			s_onlineLastColor = default;
			s_onlineHasLastColor = false;
			s_onlineLearnedNormalColor = default;
			s_onlineHasLearnedNormalColor = false;
			s_onlineLearnedNormalCount = 0;
			s_onlineLearnedCritColor = default;
			s_onlineHasLearnedCritColor = false;
			s_onlineLearnedCritCount = 0;
		}

		private static void ResetForInactivity()
		{
			// Keep session peaks until scene change or player-loss.
			float preservedPeakHit = s_peakHit;
			float preservedPeakDps = s_peakDps;
			MaybeLogOnlineColorSummary("inactivity");
			ResetInternal();
			s_peakHit = preservedPeakHit;
			s_peakDps = preservedPeakDps;
		}

		private static void EnsurePanelInitialized()
		{
			if (!s_panelInitialized)
			{
				float width = Mathf.Max(MinPanelWidth, Settings.dpsMeterPanelWidth);
				float height = Mathf.Max(MinPanelHeight, Settings.dpsMeterPanelHeight);
				float x = Settings.dpsMeterPanelX;
				float y = Settings.dpsMeterPanelY;

				if (x < 0f)
					x = Mathf.Max(10f, Screen.width - width - 14f);
				if (y < 0f)
					y = 88f;

				s_panelRect = new Rect(x, y, width, height);
				s_panelInitialized = true;
			}

			if (Screen.width != s_lastScreenWidth || Screen.height != s_lastScreenHeight)
			{
				ClampPanelToScreen();
				s_lastScreenWidth = Screen.width;
				s_lastScreenHeight = Screen.height;
			}
		}

		private static void EnsurePanelStyle()
		{
			if (s_panelLabelStyle != null)
				return;

			s_panelLabelStyle = new GUIStyle(GUI.skin.label)
			{
				wordWrap = true,
				clipping = TextClipping.Clip
			};
		}

		private static void AutoGrowPanelForContent()
		{
			if (!Settings.dpsMeterPanelLocked || s_panelLabelStyle == null)
				return;

			float availableWidth = Mathf.Max(120f, s_panelRect.width - 16f);
			float neededHeight = s_panelLabelStyle.CalcHeight(new GUIContent(s_panelText), availableWidth) + 34f;
			if (neededHeight > s_panelRect.height)
			{
				s_panelRect.height = neededHeight;
			}
		}

		private static void DrawPanelWindow(int windowId)
		{
			var style = s_panelLabelStyle ?? GUI.skin.label;
			Rect contentRect = new Rect(8f, 24f, s_panelRect.width - 16f, s_panelRect.height - 32f);
			GUI.Label(contentRect, s_panelText, style);

			if (!Settings.dpsMeterPanelLocked)
			{
				Rect resizeGripRect = new Rect(
					s_panelRect.width - ResizeGripSize - 2f,
					s_panelRect.height - ResizeGripSize - 2f,
					ResizeGripSize,
					ResizeGripSize);
				GUI.Box(resizeGripRect, "");
				ProcessPanelResizing(resizeGripRect);
				GUI.DragWindow(new Rect(0, 0, s_panelRect.width - ResizeGripSize - 6f, 20f));
			}

			_ = windowId;
		}

		private static void ProcessPanelResizing(Rect resizeGripRect)
		{
			Event currentEvent = Event.current;
			switch (currentEvent.type)
			{
				case EventType.MouseDown:
					if (resizeGripRect.Contains(currentEvent.mousePosition))
					{
						s_panelResizing = true;
						currentEvent.Use();
					}
					break;
				case EventType.MouseUp:
					s_panelResizing = false;
					break;
				case EventType.MouseDrag:
					if (!s_panelResizing)
						break;

					s_panelRect.width = Mathf.Max(MinPanelWidth, s_panelRect.width + currentEvent.delta.x);
					s_panelRect.height = Mathf.Max(MinPanelHeight, s_panelRect.height + currentEvent.delta.y);
					ClampPanelToScreen();
					currentEvent.Use();
					break;
			}
		}

		private static void ClampPanelToScreen()
		{
			s_panelRect.width = Mathf.Max(MinPanelWidth, s_panelRect.width);
			s_panelRect.height = Mathf.Max(MinPanelHeight, s_panelRect.height);

			float maxX = Mathf.Max(0f, Screen.width - s_panelRect.width);
			float maxY = Mathf.Max(0f, Screen.height - s_panelRect.height);
			s_panelRect.x = Mathf.Clamp(s_panelRect.x, 0f, maxX);
			s_panelRect.y = Mathf.Clamp(s_panelRect.y, 0f, maxY);
		}

		private static void SyncPanelToSettings()
		{
			Settings.dpsMeterPanelX = s_panelRect.x;
			Settings.dpsMeterPanelY = s_panelRect.y;
			Settings.dpsMeterPanelWidth = s_panelRect.width;
			Settings.dpsMeterPanelHeight = s_panelRect.height;
		}

		private static void PruneWindow(float now)
		{
			float windowSeconds = Mathf.Max(0.5f, Settings.dpsMeterWindowSeconds);
			while (s_recentHits.Count > 0)
			{
				HitSample oldest = s_recentHits.Peek();
				if (now - oldest.Time <= windowSeconds)
					break;

				s_recentDamage -= oldest.Damage;
				s_recentHits.Dequeue();
			}

			if (s_recentDamage < 0f)
				s_recentDamage = 0f;
		}

		private static void RefreshDps()
		{
			float windowSeconds = Mathf.Max(0.5f, Settings.dpsMeterWindowSeconds);
			s_currentDps = s_recentDamage / windowSeconds;
			if (s_currentDps > s_peakDps)
				s_peakDps = s_currentDps;
			if (s_currentDps > 0.01f)
				s_minDps = Mathf.Min(s_minDps, s_currentDps);
		}

		private static string BuildOverlayText(float now)
		{
			s_textBuilder.Clear();

			float avgDps = 0f;
			float combatSeconds = 0f;
			if (s_totalHits > 0 && s_firstHitAt >= 0f)
			{
				combatSeconds = Mathf.Max(0.001f, now - s_firstHitAt);
				avgDps = s_totalDamage / combatSeconds;
			}

			s_textBuilder.Append("Source: ").Append(DescribeCurrentMode()).Append('\n');
			s_textBuilder.Append("Hits: ").Append(s_totalHits).Append('\n');
			if (s_sourceMode == DpsSourceMode.OfflineRelay)
			{
				s_textBuilder.Append("Events: ").Append(s_totalEvents).Append(" | Misses~: ").Append(s_inferredMisses).Append('\n');
				s_textBuilder.Append("Crits: ").Append(s_critEvents).Append(" (Super: ").Append(s_superCritEvents).Append(')').Append('\n');
				s_textBuilder.Append("Kills: ").Append(s_killEvents).Append(" | Block: ").Append(s_blockEvents).Append(" | Parry: ").Append(s_parryEvents).Append('\n');
				s_textBuilder.Append("Freeze: ").Append(s_freezeEvents).Append(" | Stun: ").Append(s_stunEvents).Append(" | MeleeHit: ").Append(s_meleeHitEvents).Append('\n');
			}
			else if (s_sourceMode == DpsSourceMode.OnlineRaw)
			{
				float critPercent = s_totalHits > 0
					? (100f * s_onlineColorCritEvents / s_totalHits)
					: 0f;
				s_textBuilder.Append("Crits~(color): ").Append(s_onlineColorCritEvents)
					.Append(" | Crit %: ").Append(critPercent.ToString("F1", CultureInfo.InvariantCulture)).Append('%').Append('\n');
				if (Settings.enableDamageNumberDiagnostics && s_onlineHasLastColor)
				{
					s_textBuilder.Append("Last Color: ").Append(FormatColor(s_onlineLastColor)).Append('\n');
				}
				if (Settings.enableDamageNumberDiagnostics && s_onlineHasLearnedNormalColor)
				{
					s_textBuilder.Append("Calib Normal: ").Append(FormatColor(s_onlineLearnedNormalColor))
						.Append(" x").Append(s_onlineLearnedNormalCount).Append('\n');
				}
				if (Settings.enableDamageNumberDiagnostics && s_onlineHasLearnedCritColor)
				{
					s_textBuilder.Append("Calib Crit: ").Append(FormatColor(s_onlineLearnedCritColor))
						.Append(" x").Append(s_onlineLearnedCritCount).Append('\n');
				}
				if (Settings.enableDamageNumberDiagnostics)
				{
					AppendOnlineCalibrationText();
				}
				s_textBuilder.Append("Note: includes all visible damage numbers.\n");
			}
			s_textBuilder.Append("Total Damage: ").Append(FormatNumber(s_totalDamage)).Append('\n');
			s_textBuilder.Append("Current DPS: ").Append(FormatNumber(s_currentDps)).Append('\n');
			s_textBuilder.Append("Average DPS: ").Append(FormatNumber(avgDps)).Append('\n');
			s_textBuilder.Append("Peak DPS: ").Append(FormatNumber(s_peakDps)).Append('\n');
			s_textBuilder.Append("Min DPS: ").Append(FormatMaybe(s_minDps)).Append('\n');
			s_textBuilder.Append("Peak Hit: ").Append(FormatNumber(s_peakHit)).Append('\n');
			s_textBuilder.Append("Min Hit: ").Append(FormatMaybe(s_minHit)).Append('\n');
			s_textBuilder.Append("Window: ").Append(Settings.dpsMeterWindowSeconds.ToString("F1")).Append("s");
			if (s_totalHits > 0)
			{
				s_textBuilder.Append(" | Combat: ").Append(combatSeconds.ToString("F1")).Append("s");
			}

			return s_textBuilder.ToString();
		}

		private static string FormatMaybe(float value)
		{
			if (value == float.MaxValue)
				return "-";
			return FormatNumber(value);
		}

		private static string FormatNumber(float value)
		{
			if (value >= 1_000_000f)
				return (value / 1_000_000f).ToString("F2") + "m";
			if (value >= 1_000f)
				return (value / 1_000f).ToString("F2") + "k";
			return value.ToString("F1");
		}

		private static bool HasFlag(int value, int flag)
		{
			return (value & flag) != 0;
		}

		private static DpsSourceMode GetCurrentMode()
		{
			if (!ObjectManager.HasPlayer())
				return DpsSourceMode.None;

			if (ObjectManager.IsOfflineMode())
				return DpsSourceMode.OfflineRelay;

			if (Settings.enableDpsMeterOnlineRaw)
				return DpsSourceMode.OnlineRaw;

			return DpsSourceMode.None;
		}

		private static string DescribeCurrentMode()
		{
			return s_sourceMode switch
			{
				DpsSourceMode.OfflineRelay => "Offline Relay",
				DpsSourceMode.OnlineRaw => "Online Raw",
				_ => "Disabled"
			};
		}

		private static void RegisterHit(float damage, float now)
		{
			if (s_totalHits == 0)
				s_firstHitAt = now;

			s_lastHitAt = now;
			s_totalHits++;
			s_totalDamage += damage;
			s_peakHit = Mathf.Max(s_peakHit, damage);
			s_minHit = Mathf.Min(s_minHit, damage);
			s_recentHits.Enqueue(new HitSample(now, damage));
			s_recentDamage += damage;
			PruneWindow(now);
			RefreshDps();
		}

		private static bool TryGetSourceInstanceId(object source, out int instanceId)
		{
			instanceId = 0;
			if (source is UnityEngine.Object unityObject)
			{
				instanceId = unityObject.GetInstanceID();
				return instanceId != 0;
			}

			return false;
		}

		private static bool ShouldAcceptOnlineSample(int instanceId, string text, float now)
		{
			if (!s_onlineStates.TryGetValue(instanceId, out var state))
			{
				s_onlineStates[instanceId] = new OnlineSampleState
				{
					LastText = text,
					LastSeenAt = now
				};
				return true;
			}

			bool accepted = !string.Equals(state.LastText, text, StringComparison.Ordinal)
				|| (now - state.LastSeenAt) >= OnlineSameTextGapSeconds;

			state.LastText = text;
			state.LastSeenAt = now;
			s_onlineStates[instanceId] = state;
			return accepted;
		}

		private static void PruneOnlineStates(float now)
		{
			if (s_onlineStates.Count == 0)
				return;

			s_onlinePruneBuffer.Clear();
			foreach (var kv in s_onlineStates)
			{
				if (now - kv.Value.LastSeenAt > OnlineStateTtlSeconds)
					s_onlinePruneBuffer.Add(kv.Key);
			}

			for (int i = 0; i < s_onlinePruneBuffer.Count; i++)
			{
				s_onlineStates.Remove(s_onlinePruneBuffer[i]);
			}
		}

		private static bool TryParseDamageText(string text, out float damage)
		{
			damage = 0f;
			string trimmed = text.Trim();
			if (trimmed.Length == 0)
				return false;

			int start = -1;
			for (int i = 0; i < trimmed.Length; i++)
			{
				if (char.IsDigit(trimmed[i]))
				{
					start = i;
					break;
				}
			}
			if (start < 0)
				return false;

			int end = start;
			bool seenDecimal = false;
			while (end < trimmed.Length)
			{
				char c = trimmed[end];
				if (char.IsDigit(c))
				{
					end++;
					continue;
				}
				if (c == ',' )
				{
					end++;
					continue;
				}
				if (c == '.' && !seenDecimal)
				{
					seenDecimal = true;
					end++;
					continue;
				}
				break;
			}

			if (end <= start)
				return false;

			string token = trimmed[start..end].Replace(",", string.Empty);
			if (!float.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
				return false;

			float multiplier = 1f;
			if (end < trimmed.Length)
			{
				char suffix = char.ToLowerInvariant(trimmed[end]);
				if (suffix == 'k')
					multiplier = 1_000f;
				else if (suffix == 'm')
					multiplier = 1_000_000f;
				else if (suffix == 'b')
					multiplier = 1_000_000_000f;
			}

			damage = value * multiplier;
			return !float.IsNaN(damage) && !float.IsInfinity(damage);
		}

		private static bool IsLikelyCritColorAdaptive(Color color)
		{
			if (s_onlineHasLearnedCritColor && ColorDistanceRgb(color, s_onlineLearnedCritColor) <= 0.12f)
				return true;
			if (s_onlineHasLearnedNormalColor && ColorDistanceRgb(color, s_onlineLearnedNormalColor) <= 0.08f)
				return false;
			if (s_onlineHasLearnedNormalColor)
				return IsLikelyCritAgainstNormal(color, s_onlineLearnedNormalColor);
			return IsLikelyCritColorFallback(color);
		}

		private static bool IsLikelyCritColorFallback(Color color)
		{
			Color.RGBToHSV(color, out float h, out float s, out float v);
			float hueDegrees = h * 360f;
			bool yellowBand = hueDegrees >= 35f && hueDegrees <= 75f;
			return yellowBand && s >= 0.35f && v >= 0.5f;
		}

		private static bool IsLikelyCritAgainstNormal(Color sample, Color normal)
		{
			Color.RGBToHSV(sample, out float sampleHue, out float sampleSat, out float sampleVal);
			Color.RGBToHSV(normal, out float normalHue, out float normalSat, out float normalVal);

			float sampleHueDegrees = sampleHue * 360f;
			float hueDelta = HueDeltaDegrees(sampleHue, normalHue);
			bool warmBand = sampleHueDegrees >= 30f && sampleHueDegrees <= 80f;
			bool saturationLift = sampleSat >= Mathf.Max(0.30f, normalSat + 0.15f);
			bool valueOk = sampleVal >= Mathf.Max(0.35f, normalVal - 0.12f);
			bool awayFromNormal = hueDelta >= 10f || ColorDistanceRgb(sample, normal) >= 0.10f;

			return warmBand && saturationLift && valueOk && awayFromNormal;
		}

		private static string FormatColor(Color color)
		{
			return $"{color.r:F2},{color.g:F2},{color.b:F2},{color.a:F2}";
		}

		private static void RecordOnlineColorSample(Color color)
		{
			uint key = ColorToKey(color);
			if (s_onlineColorHistogram.TryGetValue(key, out int count))
			{
				s_onlineColorHistogram[key] = count + 1;
				return;
			}

			if (s_onlineColorHistogram.Count >= 24)
				return;

			s_onlineColorHistogram[key] = 1;
		}

		private static void UpdateOnlineColorCalibration()
		{
			if (!TryGetTopOnlineColor(out uint topKey, out int topCount) || topCount < 5)
			{
				s_onlineHasLearnedNormalColor = false;
				s_onlineHasLearnedCritColor = false;
				return;
			}

			Color normal = KeyToColor(topKey);
			s_onlineLearnedNormalColor = normal;
			s_onlineLearnedNormalCount = topCount;
			s_onlineHasLearnedNormalColor = true;

			s_onlineHasLearnedCritColor = false;
			uint bestCritKey = 0;
			int bestCritCount = 0;

			foreach (var kv in s_onlineColorHistogram)
			{
				if (kv.Key == topKey || kv.Value < 3)
					continue;

				Color candidate = KeyToColor(kv.Key);
				if (!IsLikelyCritAgainstNormal(candidate, normal))
					continue;

				if (kv.Value <= bestCritCount)
					continue;

				bestCritCount = kv.Value;
				bestCritKey = kv.Key;
			}

			if (bestCritCount > 0)
			{
				s_onlineLearnedCritColor = KeyToColor(bestCritKey);
				s_onlineLearnedCritCount = bestCritCount;
				s_onlineHasLearnedCritColor = true;
			}
		}

		private static uint ColorToKey(Color color)
		{
			var c32 = (Color32)color;
			return ((uint)c32.r << 24)
				| ((uint)c32.g << 16)
				| ((uint)c32.b << 8)
				| c32.a;
		}

		private static Color KeyToColor(uint key)
		{
			byte r = (byte)((key >> 24) & 0xFF);
			byte g = (byte)((key >> 16) & 0xFF);
			byte b = (byte)((key >> 8) & 0xFF);
			byte a = (byte)(key & 0xFF);
			return new Color32(r, g, b, a);
		}

		private static float ColorDistanceRgb(Color a, Color b)
		{
			float dr = a.r - b.r;
			float dg = a.g - b.g;
			float db = a.b - b.b;
			return Mathf.Sqrt((dr * dr) + (dg * dg) + (db * db));
		}

		private static float HueDeltaDegrees(float hueA, float hueB)
		{
			float delta = Mathf.Abs(hueA - hueB);
			delta = Mathf.Min(delta, 1f - delta);
			return delta * 360f;
		}

		private static void AppendOnlineCalibrationText()
		{
			if (!TryGetTopOnlineColor(out uint topKey, out int topCount))
				return;

			var topColor = KeyToColor(topKey);
			s_textBuilder.Append("Top Color #1: ").Append(FormatColor(topColor)).Append(" x").Append(topCount).Append('\n');

			if (TryGetSecondOnlineColor(topKey, out uint secondKey, out int secondCount))
			{
				var secondColor = KeyToColor(secondKey);
				s_textBuilder.Append("Top Color #2: ").Append(FormatColor(secondColor)).Append(" x").Append(secondCount).Append('\n');
			}
		}

		private static bool TryGetTopOnlineColor(out uint colorKey, out int count)
		{
			colorKey = 0;
			count = 0;
			foreach (var kv in s_onlineColorHistogram)
			{
				if (kv.Value <= count)
					continue;
				colorKey = kv.Key;
				count = kv.Value;
			}
			return count > 0;
		}

		private static bool TryGetSecondOnlineColor(uint firstKey, out uint colorKey, out int count)
		{
			colorKey = 0;
			count = 0;
			foreach (var kv in s_onlineColorHistogram)
			{
				if (kv.Key == firstKey || kv.Value <= count)
					continue;
				colorKey = kv.Key;
				count = kv.Value;
			}
			return count > 0;
		}

		private static void MaybeLogOnlineColorSummary(string reason)
		{
			if (s_sourceMode != DpsSourceMode.OnlineRaw || s_onlineColorSamples <= 0)
				return;

			float critPercent = s_totalHits > 0
				? (100f * s_onlineColorCritEvents / s_totalHits)
				: 0f;
			string summary = $"[DpsMeter] Online color summary ({reason}) hits={s_totalHits} crits~={s_onlineColorCritEvents} crit%={critPercent:F1}";

			if (TryGetTopOnlineColor(out uint topKey, out int topCount))
			{
				summary += $" | top1={FormatColor(KeyToColor(topKey))}x{topCount}";
				if (TryGetSecondOnlineColor(topKey, out uint secondKey, out int secondCount))
				{
					summary += $" | top2={FormatColor(KeyToColor(secondKey))}x{secondCount}";
				}
			}
			if (s_onlineHasLearnedNormalColor)
			{
				summary += $" | normal={FormatColor(s_onlineLearnedNormalColor)}x{s_onlineLearnedNormalCount}";
			}
			if (s_onlineHasLearnedCritColor)
			{
				summary += $" | crit={FormatColor(s_onlineLearnedCritColor)}x{s_onlineLearnedCritCount}";
			}

			Log.InfoThrottled(LogSource.Hooks, $"dps-online-color-summary:{reason}", summary, TimeSpan.FromSeconds(5));
		}
	}
}

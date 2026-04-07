using System.Text;
using UnityEngine;

namespace Mod.Cheats
{
	internal static class DpsMeter
	{
		private const int HitEventHit = 1;
		private const int HitEventCrit = 2;
		private const int HitEventKill = 4;
		private const int HitEventFreeze = 8;
		private const int HitEventStun = 16;
		private const int HitEventBlock = 32;
		private const int HitEventMeleeHit = 64;
		private const int HitEventParry = 128;
		private const int HitEventSuperCrit = 256;

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

		private static readonly Queue<HitSample> s_recentHits = new Queue<HitSample>(256);
		private static readonly StringBuilder s_textBuilder = new StringBuilder(256);
		private static Rect s_panelRect = new Rect(0f, 88f, 320f, 252f);

		private static bool s_panelAnchored;
		private static int s_lastScreenWidth;
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

		public static void OnDamageEvent(object source, float damage, int hitEvents)
		{
			_ = source;
			if (!Settings.enableDpsMeter || float.IsNaN(damage) || float.IsInfinity(damage))
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

		public static void OnUpdate()
		{
			if (!Settings.enableDpsMeter)
				return;

			float now = Time.unscaledTime;
			PruneWindow(now);
			RefreshDps();

			if (!Settings.dpsMeterAutoReset || s_lastHitAt < 0f)
				return;

			float resetAfter = Mathf.Max(2f, Settings.dpsMeterInactivityResetSeconds);
			if (now - s_lastHitAt >= resetAfter)
			{
				ResetInternal();
			}
		}

		public static void OnGUI()
		{
			if (!Settings.enableDpsMeter)
				return;

			EnsurePanelAnchored();

			GUI.Box(s_panelRect, "LEHud DPS Meter");
			Rect contentRect = new Rect(
				s_panelRect.x + 8f,
				s_panelRect.y + 24f,
				s_panelRect.width - 16f,
				s_panelRect.height - 30f);
			GUI.Label(contentRect, BuildOverlayText(Time.unscaledTime));
		}

		public static void OnSceneChanged()
		{
			ResetInternal();
		}

		public static void Reset()
		{
			ResetInternal();
		}

		private static void ResetInternal()
		{
			s_recentHits.Clear();
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
		}

		private static void EnsurePanelAnchored()
		{
			int screenWidth = Screen.width;
			if (s_panelAnchored && screenWidth > 0 && screenWidth == s_lastScreenWidth)
				return;

			s_panelRect.x = Mathf.Max(10f, screenWidth - s_panelRect.width - 14f);
			s_panelAnchored = true;
			s_lastScreenWidth = screenWidth;
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

			s_textBuilder.Append("Hits: ").Append(s_totalHits).Append('\n');
			s_textBuilder.Append("Events: ").Append(s_totalEvents).Append(" | Misses~: ").Append(s_inferredMisses).Append('\n');
			s_textBuilder.Append("Crits: ").Append(s_critEvents).Append(" (Super: ").Append(s_superCritEvents).Append(')').Append('\n');
			s_textBuilder.Append("Kills: ").Append(s_killEvents).Append(" | Block: ").Append(s_blockEvents).Append(" | Parry: ").Append(s_parryEvents).Append('\n');
			s_textBuilder.Append("Freeze: ").Append(s_freezeEvents).Append(" | Stun: ").Append(s_stunEvents).Append(" | MeleeHit: ").Append(s_meleeHitEvents).Append('\n');
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
	}
}

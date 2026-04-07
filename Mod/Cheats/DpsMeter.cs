using System.Text;
using UnityEngine;

namespace Mod.Cheats
{
	internal static class DpsMeter
	{
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
		private static Rect s_panelRect = new Rect(0f, 88f, 300f, 198f);

		private static bool s_panelAnchored;
		private static int s_lastScreenWidth;
		private static float s_recentDamage;
		private static float s_totalDamage;
		private static int s_totalHits;
		private static float s_peakHit;
		private static float s_minHit = float.MaxValue;
		private static float s_currentDps;
		private static float s_peakDps;
		private static float s_minDps = float.MaxValue;
		private static float s_firstHitAt = -1f;
		private static float s_lastHitAt = -1f;

		public static void OnDamageSample(object source, float damage)
		{
			_ = source;
			if (!Settings.enableDpsMeter || damage <= 0f || float.IsNaN(damage) || float.IsInfinity(damage))
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
			s_totalHits = 0;
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
	}
}

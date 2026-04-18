using System.Text;
using UnityEngine;

namespace Mod.Cheats
{
	internal sealed class OnlineCritColorCalibrator
	{
		private const int MaxHistogramEntries = 24;
		private readonly Dictionary<uint, int> _colorHistogram = new Dictionary<uint, int>(MaxHistogramEntries);

		public int TotalSamples { get; private set; }
		public int CritSamples { get; private set; }
		public Color LastColor { get; private set; }
		public bool HasLastColor { get; private set; }
		public Color LearnedNormalColor { get; private set; }
		public bool HasLearnedNormalColor { get; private set; }
		public int LearnedNormalCount { get; private set; }
		public Color LearnedCritColor { get; private set; }
		public bool HasLearnedCritColor { get; private set; }
		public int LearnedCritCount { get; private set; }

		public void Reset()
		{
			_colorHistogram.Clear();
			TotalSamples = 0;
			CritSamples = 0;
			LastColor = default;
			HasLastColor = false;
			LearnedNormalColor = default;
			HasLearnedNormalColor = false;
			LearnedNormalCount = 0;
			LearnedCritColor = default;
			HasLearnedCritColor = false;
			LearnedCritCount = 0;
		}

		public void RegisterSample(Color color)
		{
			TotalSamples++;
			LastColor = color;
			HasLastColor = true;
			RecordColorSample(color);
			UpdateCalibration();
			if (IsLikelyCritColorAdaptive(color))
			{
				CritSamples++;
			}
		}

		public void AppendTopColors(StringBuilder builder)
		{
			if (!TryGetTopColor(out uint topKey, out Color topColor, out int topCount))
				return;

			builder.Append("Top Color #1: ").Append(FormatColor(topColor)).Append(" x").Append(topCount).Append('\n');
			if (TryGetSecondColor(topKey, out Color secondColor, out int secondCount))
			{
				builder.Append("Top Color #2: ").Append(FormatColor(secondColor)).Append(" x").Append(secondCount).Append('\n');
			}
		}

		public bool TryGetTopColor(out uint colorKey, out Color color, out int count)
		{
			colorKey = 0;
			color = default;
			count = 0;
			foreach (var kv in _colorHistogram)
			{
				if (kv.Value <= count)
					continue;
				colorKey = kv.Key;
				count = kv.Value;
			}

			if (count <= 0)
				return false;

			color = KeyToColor(colorKey);
			return true;
		}

		public bool TryGetSecondColor(uint firstKey, out Color color, out int count)
		{
			color = default;
			count = 0;
			uint colorKey = 0;
			foreach (var kv in _colorHistogram)
			{
				if (kv.Key == firstKey || kv.Value <= count)
					continue;
				colorKey = kv.Key;
				count = kv.Value;
			}

			if (count <= 0)
				return false;

			color = KeyToColor(colorKey);
			return true;
		}

		public static string FormatColor(Color color)
		{
			return $"{color.r:F2},{color.g:F2},{color.b:F2},{color.a:F2}";
		}

		private void RecordColorSample(Color color)
		{
			uint key = ColorToKey(color);
			if (_colorHistogram.TryGetValue(key, out int count))
			{
				_colorHistogram[key] = count + 1;
				return;
			}

			if (_colorHistogram.Count >= MaxHistogramEntries)
				return;

			_colorHistogram[key] = 1;
		}

		private void UpdateCalibration()
		{
			if (!TryGetTopColor(out uint topKey, out Color normal, out int topCount) || topCount < 5)
			{
				HasLearnedNormalColor = false;
				HasLearnedCritColor = false;
				return;
			}

			LearnedNormalColor = normal;
			LearnedNormalCount = topCount;
			HasLearnedNormalColor = true;

			HasLearnedCritColor = false;
			uint bestCritKey = 0;
			int bestCritCount = 0;

			foreach (var kv in _colorHistogram)
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
				LearnedCritColor = KeyToColor(bestCritKey);
				LearnedCritCount = bestCritCount;
				HasLearnedCritColor = true;
			}
		}

		private bool IsLikelyCritColorAdaptive(Color color)
		{
			if (HasLearnedCritColor && ColorDistanceRgb(color, LearnedCritColor) <= 0.12f)
				return true;
			if (HasLearnedNormalColor && ColorDistanceRgb(color, LearnedNormalColor) <= 0.08f)
				return false;
			if (HasLearnedNormalColor)
				return IsLikelyCritAgainstNormal(color, LearnedNormalColor);
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
	}
}

using System.Globalization;
using UnityEngine;

namespace Mod.Cheats
{
	internal sealed class OnlineDamageTextSampler
	{
		private const float SameTextGapSeconds = 0.25f;
		private const float StateTtlSeconds = 12f;

		private struct SampleState
		{
			public string LastText;
			public float LastSeenAt;
		}

		private readonly Dictionary<int, SampleState> _states = new Dictionary<int, SampleState>(128);
		private readonly List<int> _pruneBuffer = new List<int>(64);

		public void Reset()
		{
			_states.Clear();
			_pruneBuffer.Clear();
		}

		public void PruneExpired(float now)
		{
			if (_states.Count == 0)
				return;

			_pruneBuffer.Clear();
			foreach (var kv in _states)
			{
				if (now - kv.Value.LastSeenAt > StateTtlSeconds)
					_pruneBuffer.Add(kv.Key);
			}

			for (int i = 0; i < _pruneBuffer.Count; i++)
			{
				_states.Remove(_pruneBuffer[i]);
			}
		}

		public bool TryAcceptSample(object source, string? text, float now, out float damage)
		{
			damage = 0f;
			if (string.IsNullOrWhiteSpace(text) || !TryParseDamageText(text, out damage) || damage <= 0f)
				return false;

			if (!TryGetSourceInstanceId(source, out int instanceId))
				return false;

			return ShouldAcceptSample(instanceId, text, now);
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

		private bool ShouldAcceptSample(int instanceId, string text, float now)
		{
			if (!_states.TryGetValue(instanceId, out var state))
			{
				_states[instanceId] = new SampleState
				{
					LastText = text,
					LastSeenAt = now
				};
				return true;
			}

			bool accepted = !string.Equals(state.LastText, text, StringComparison.Ordinal)
				|| (now - state.LastSeenAt) >= SameTextGapSeconds;

			state.LastText = text;
			state.LastSeenAt = now;
			_states[instanceId] = state;
			return accepted;
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
				if (c == ',')
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
	}
}

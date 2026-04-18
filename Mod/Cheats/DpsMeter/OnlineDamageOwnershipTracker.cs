using Mod.Game;
using UnityEngine;

namespace Mod.Cheats
{
	internal sealed class OnlineDamageOwnershipTracker
	{
		private float _lastKnownLocalHealthPercent = -1f;
		private float _lastLocalHealthDropAt = -1f;

		public OnlineDamageFilterMode GetMode()
		{
			return Settings.dpsMeterOnlineFilterMode switch
			{
				1 => OnlineDamageFilterMode.LikelyOutgoing,
				2 => OnlineDamageFilterMode.LikelyIncoming,
				_ => OnlineDamageFilterMode.AllVisible
			};
		}

		public void Reset()
		{
			_lastKnownLocalHealthPercent = -1f;
			_lastLocalHealthDropAt = -1f;
		}

		public void OnUpdate(float now)
		{
			if (PlayerHealthReader.TryGetLocalHealthPercent(out float healthPercent))
			{
				if (_lastKnownLocalHealthPercent >= 0f && healthPercent < _lastKnownLocalHealthPercent - 0.0001f)
				{
					_lastLocalHealthDropAt = now;
				}
				_lastKnownLocalHealthPercent = healthPercent;
			}
			else
			{
				_lastKnownLocalHealthPercent = -1f;
			}
		}

		public bool ShouldInclude(Vector3? sampleWorldPosition, float now)
		{
			OnlineDamageFilterMode mode = GetMode();
			if (mode == OnlineDamageFilterMode.AllVisible)
				return true;

			if (!TryGetLocalPlayerPosition(out Vector3 playerPosition))
				return true;

			bool hasWorldPosition = sampleWorldPosition.HasValue;
			Vector3 worldPosition = sampleWorldPosition.GetValueOrDefault();
			bool recentHealthDrop = HasRecentLocalHealthDrop(now);
			float nearMeters = Mathf.Clamp(Settings.dpsMeterNearPlayerMeters, 0.5f, 10f);
			float farMeters = Mathf.Max(nearMeters + 0.2f, Settings.dpsMeterFarPlayerMeters);

			return OnlineDamageOwnershipFilter.ShouldInclude(
				mode,
				hasWorldPosition,
				worldPosition,
				playerPosition,
				nearMeters,
				farMeters,
				recentHealthDrop);
		}

		private bool HasRecentLocalHealthDrop(float now)
		{
			if (_lastLocalHealthDropAt < 0f)
				return false;

			float windowSeconds = Mathf.Clamp(Settings.dpsMeterHpDropCorrelationMs, 50f, 1000f) / 1000f;
			return (now - _lastLocalHealthDropAt) <= windowSeconds;
		}

		private static bool TryGetLocalPlayerPosition(out Vector3 position)
		{
			position = default;
			var localPlayer = ObjectManager.GetLocalPlayer();
			if (localPlayer == null)
				return false;

			var transform = localPlayer.transform;
			if (transform == null)
				return false;

			position = transform.position;
			return true;
		}
	}
}

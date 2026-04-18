using UnityEngine;

namespace Mod.Cheats
{
	internal enum OnlineDamageFilterMode
	{
		AllVisible = 0,
		LikelyOutgoing = 1,
		LikelyIncoming = 2
	}

	internal static class OnlineDamageOwnershipFilter
	{
		public static bool ShouldInclude(
			OnlineDamageFilterMode mode,
			bool hasWorldPosition,
			Vector3 worldPosition,
			Vector3 playerPosition,
			float nearMeters,
			float farMeters,
			bool recentLocalHealthDrop)
		{
			if (mode == OnlineDamageFilterMode.AllVisible)
				return true;

			if (!hasWorldPosition)
			{
				// If we cannot classify by position, keep outgoing permissive and incoming conservative.
				return mode == OnlineDamageFilterMode.LikelyOutgoing
					|| (mode == OnlineDamageFilterMode.LikelyIncoming && recentLocalHealthDrop);
			}

			float distance = Vector3.Distance(worldPosition, playerPosition);
			bool near = distance <= nearMeters;
			bool far = distance >= farMeters;

			if (mode == OnlineDamageFilterMode.LikelyOutgoing)
			{
				if (far)
					return true;
				if (near)
					return !recentLocalHealthDrop;
				return !recentLocalHealthDrop;
			}

			// LikelyIncoming
			if (near)
				return true;
			if (far)
				return false;
			return recentLocalHealthDrop;
		}

		public static string Describe(OnlineDamageFilterMode mode)
		{
			return mode switch
			{
				OnlineDamageFilterMode.LikelyOutgoing => "Likely Outgoing",
				OnlineDamageFilterMode.LikelyIncoming => "Likely Incoming",
				_ => "All Visible"
			};
		}
	}
}

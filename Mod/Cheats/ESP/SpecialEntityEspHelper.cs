using System;
using Il2Cpp;
using UnityEngine;

namespace Mod.Cheats.ESP
{
	internal enum SpecialEntityType
	{
		None = 0,
		LootLizard = 1,
		Champion = 2
	}

	internal static class SpecialEntityEspHelper
	{
		private static readonly Color SpecialEntityColor = Drawing.BloodOrange;
		private const string ChampionPrefix = "Champion ";
		private const string LootLizardFallbackName = "Loot Lizard";
		private const string LootLizardFlairPrefix = "<<< ";
		private const string LootLizardFlairSuffix = " >>>";

		internal static bool IsSpecial(SpecialEntityType specialType) => specialType != SpecialEntityType.None;

		internal static SpecialEntityType DetectType(ActorVisuals actor)
		{
			if (IsLootLizard(actor))
			{
				return SpecialEntityType.LootLizard;
			}

			if (IsChampion(actor))
			{
				return SpecialEntityType.Champion;
			}

			return SpecialEntityType.None;
		}

		internal static bool ShouldRender(SpecialEntityType specialType)
		{
			return specialType switch
			{
				SpecialEntityType.Champion => Settings.espShowChampions,
				SpecialEntityType.LootLizard => Settings.espShowLootLizards,
				_ => true
			};
		}

		internal static string BuildLabel(SpecialEntityType specialType, string baseLabel)
		{
			return specialType switch
			{
				SpecialEntityType.Champion => ChampionPrefix + baseLabel,
				SpecialEntityType.LootLizard => BuildLootLizardLabel(baseLabel),
				_ => baseLabel
			};
		}

		internal static Color ResolveColor(SpecialEntityType specialType, Color defaultColor)
		{
			return IsSpecial(specialType) ? SpecialEntityColor : defaultColor;
		}

		internal static EspStringStyle ResolveTextStyle(SpecialEntityType specialType)
		{
			return specialType == SpecialEntityType.LootLizard
				? EspStringStyle.Emphasized
				: EspStringStyle.Default;
		}

		private static string BuildLootLizardLabel(string baseLabel)
		{
			var normalized = baseLabel.Trim();
			if (normalized.Length == 0 || normalized.IndexOf("LootLizard", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				normalized = LootLizardFallbackName;
			}

			return LootLizardFlairPrefix + normalized + LootLizardFlairSuffix;
		}

		private static bool IsLootLizard(ActorVisuals actor)
		{
			if (actor.gameObject.GetComponent<LootLizardFleeing>() != null
				|| actor.gameObject.GetComponentInParent<LootLizardFleeing>() != null
				|| actor.gameObject.GetComponentInChildren<LootLizardFleeing>() != null)
			{
				return true;
			}

			var go = actor.gameObject;
			if (!string.IsNullOrEmpty(go.name) && go.name.IndexOf("LootLizard", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return true;
			}

			var parent = go.transform != null ? go.transform.parent : null;
			if (parent != null && !string.IsNullOrEmpty(parent.name) && parent.name.IndexOf("LootLizard", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return true;
			}

			return false;
		}

		private static bool IsChampion(ActorVisuals actor)
		{
			var info = actor.GetComponent<ActorDisplayInformation>();
			if (info == null)
			{
				return false;
			}

			try
			{
				return info.IsChampion();
			}
			catch (Exception)
			{
				return false;
			}
		}
	}
}

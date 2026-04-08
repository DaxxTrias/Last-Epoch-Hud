using System;
using System.Collections.Generic;
using System.Reflection;
using Il2Cpp;
using UnityEngine;

namespace Mod.Cheats.ESP
{
	internal enum SpecialEntityType
	{
		None = 0,
		LootLizard = 1,
		Champion = 2,
		Omen = 3
	}

	internal static class SpecialEntityEspHelper
	{
		private static readonly Color SpecialEntityColor = Drawing.BloodOrange;
		private const string ChampionPrefix = "Champion ";
		private const string OmenPrefix = "Omen ";
		private const string LootLizardFallbackName = "Loot Lizard";
		private const string LootLizardFlairPrefix = "<<< ";
		private const string LootLizardFlairSuffix = " >>>";
		private const int OmenCacheSoftLimit = 4096;
		private static readonly Dictionary<int, bool> s_omenStateByRootId = new(capacity: 128);
		private static readonly PropertyInfo? s_actorSyncActorDataProperty = typeof(ActorSync).GetProperty("actorData", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		private static readonly FieldInfo? s_actorSyncActorDataField = typeof(ActorSync).GetField("actorData", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		private static readonly PropertyInfo? s_actorDataIsOmenProperty = typeof(ActorData).GetProperty("isOmen", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		private static readonly FieldInfo? s_actorDataIsOmenField = typeof(ActorData).GetField("isOmen", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

		internal static bool IsSpecial(SpecialEntityType specialType) => specialType != SpecialEntityType.None;

		internal static SpecialEntityType DetectType(ActorVisuals actor)
		{
			if (IsLootLizard(actor))
			{
				return SpecialEntityType.LootLizard;
			}

			if (IsOmen(actor))
			{
				return SpecialEntityType.Omen;
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
				SpecialEntityType.Omen => Settings.espShowOmens,
				_ => true
			};
		}

		internal static string BuildLabel(SpecialEntityType specialType, string baseLabel)
		{
			return specialType switch
			{
				SpecialEntityType.Champion => ChampionPrefix + baseLabel,
				SpecialEntityType.LootLizard => BuildLootLizardLabel(baseLabel),
				SpecialEntityType.Omen => OmenPrefix + baseLabel,
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

		private static bool IsOmen(ActorVisuals actor)
		{
			var rootTransform = actor.transform != null ? actor.transform.root : null;
			var rootGameObject = rootTransform != null ? rootTransform.gameObject : actor.gameObject;
			int cacheKey = rootGameObject.GetInstanceID();
			if (s_omenStateByRootId.TryGetValue(cacheKey, out var cachedResult))
			{
				return cachedResult;
			}

			var isOmen = ComputeIsOmen(actor, rootGameObject);
			if (s_omenStateByRootId.Count >= OmenCacheSoftLimit)
			{
				s_omenStateByRootId.Clear();
			}

			s_omenStateByRootId[cacheKey] = isOmen;
			return isOmen;
		}

		private static bool ComputeIsOmen(ActorVisuals actor, GameObject rootGameObject)
		{
			if (TryReadOmenFlagFromActorSync(rootGameObject.GetComponent<ActorSync>(), out var isOmenFromRoot))
			{
				return isOmenFromRoot;
			}

			if (TryReadOmenFlagFromActorSync(actor.gameObject.GetComponent<ActorSync>(), out var isOmenFromActor))
			{
				return isOmenFromActor;
			}

			if (TryReadOmenFlagFromActorSync(actor.gameObject.GetComponentInParent<ActorSync>(), out var isOmenFromParent))
			{
				return isOmenFromParent;
			}

			if (TryReadOmenFlagFromActorData(rootGameObject.GetComponent<ActorData>(), out var isOmenFromRootData))
			{
				return isOmenFromRootData;
			}

			if (TryReadOmenFlagFromActorData(actor.gameObject.GetComponent<ActorData>(), out var isOmenFromActorData))
			{
				return isOmenFromActorData;
			}

			if (TryReadOmenFlagFromActorData(actor.gameObject.GetComponentInParent<ActorData>(), out var isOmenFromParentData))
			{
				return isOmenFromParentData;
			}

			// Last-ditch fallback for unknown builds where reflection bindings fail.
			return ContainsOmenToken(rootGameObject.name) || ContainsOmenToken(actor.gameObject.name);
		}

		private static bool TryReadOmenFlagFromActorSync(ActorSync? actorSync, out bool isOmen)
		{
			isOmen = false;
			if (actorSync == null)
			{
				return false;
			}

			object? actorData;
			try
			{
				actorData = s_actorSyncActorDataProperty?.GetValue(actorSync)
					?? s_actorSyncActorDataField?.GetValue(actorSync);
			}
			catch (Exception)
			{
				return false;
			}

			return TryReadOmenFlagFromActorDataObject(actorData, out isOmen);
		}

		private static bool TryReadOmenFlagFromActorData(ActorData? actorData, out bool isOmen)
		{
			return TryReadOmenFlagFromActorDataObject(actorData, out isOmen);
		}

		private static bool TryReadOmenFlagFromActorDataObject(object? actorDataObject, out bool isOmen)
		{
			isOmen = false;
			if (actorDataObject == null)
			{
				return false;
			}

			object? value;
			try
			{
				value = s_actorDataIsOmenProperty?.GetValue(actorDataObject)
					?? s_actorDataIsOmenField?.GetValue(actorDataObject);
			}
			catch (Exception)
			{
				return false;
			}

			if (value is bool flag)
			{
				isOmen = flag;
				return true;
			}

			if (value != null && bool.TryParse(value.ToString(), out var parsed))
			{
				isOmen = parsed;
				return true;
			}

			return false;
		}

		private static bool ContainsOmenToken(string? name)
		{
			return !string.IsNullOrWhiteSpace(name)
				&& name.IndexOf("omen", StringComparison.OrdinalIgnoreCase) >= 0;
		}
	}
}

using Il2Cpp;
using Mod.Game;
using UnityEngine;
using System;

namespace Mod.Cheats.ESP
{
	internal class Actors
	{
		//        Alignments: 
		//     
		//        Good: Seems to be players and player pets
		//        Evil: Seems to be enemies
		//        Barrel: Containers that are destructible
		//        HostileNeutral: Seems to be neutral enemies
		//        FriendlyNeutral: Seems to be neutral NPCs
		//        SummonedCorpse: Necromancer summons

		private static readonly Color MagicLightBlue = new Color(0.55f, 0.8f, 1f, 1f);
		private static readonly Color BloodOrange = new Color(0.90f, 0.30f, 0.00f, 1f);

		private static string SanitizeLabel(string? value)
		{
			if (string.IsNullOrEmpty(value)) return string.Empty;
			// Replace control characters that can cause IMGUI clipping or wrapping
			var sanitized = value.Replace("\r", " ").Replace("\n", " ").Replace("\t", " ");
			return sanitized.Trim();
		}

		private static string GetActorName(ActorVisuals actor)
		{
			if (actor.isPlayer && actor.UserIdentity != null)
			{
				return SanitizeLabel(actor.UserIdentity.Username);
			}
			else
			{
				var displayInformation = actor.gameObject.GetComponent<ActorDisplayInformation>();

				if (displayInformation != null)
				{
					// Prefer the localized name when available; fall back to displayName
					string? localizedName = null;
					try
					{
						localizedName = displayInformation.GetLocalizedName();
					}
					catch (Exception)
					{
						// Some IL2CPP builds may throw; ignore and use fallbacks
					}

					if (!string.IsNullOrWhiteSpace(localizedName))
					{
						return SanitizeLabel(localizedName);
					}

					if (!string.IsNullOrWhiteSpace(displayInformation.displayName))
					{
						return SanitizeLabel(displayInformation.displayName);
					}
				}
			}

			return SanitizeLabel(actor.name);
		}

		private static Color GetRarityColor(ActorVisuals actor, string alignmentName)
		{
			var info = actor.GetComponent<ActorDisplayInformation>();
			if (info != null)
			{
				if (info.actorClass == DisplayActorClass.Boss) return Color.red;
				if (info.actorClass == DisplayActorClass.Rare) return Color.yellow;
				if (info.actorClass == DisplayActorClass.Magic) return MagicLightBlue;
				// Normal/other defaults to white
				return Color.white;
			}
			// Fallback to alignment color if no display info
			return Drawing.AlignmentToColor(alignmentName);
		}

		// Unified special entity detection for ESP logic and future features
		internal enum SpecialEntityType
		{
			None = 0,
			LootLizard = 1,
			Champion = 2,
			Chest = 3
		}
		
		private static bool IsLootLizard(ActorVisuals actor)
		{
			// Component-based detection first (if available in this build)
			if (actor.gameObject.GetComponent<LootLizardFleeing>() != null
				|| actor.gameObject.GetComponentInParent<LootLizardFleeing>() != null
				|| actor.gameObject.GetComponentInChildren<LootLizardFleeing>() != null)
			{
				return true;
			}
			
			// Name-based fallbacks (seen in screenshots: "v_LootLizard Blue (Clone)" and parent "sync v_LootLizard ...")
			const StringComparison ic = StringComparison.OrdinalIgnoreCase;
			var go = actor.gameObject;
			if (!string.IsNullOrEmpty(go.name) && go.name.IndexOf("LootLizard", ic) >= 0)
			{
				return true;
			}
			
			var parent = go.transform != null ? go.transform.parent : null;
			if (parent != null && !string.IsNullOrEmpty(parent.name) && parent.name.IndexOf("LootLizard", ic) >= 0)
			{
				return true;
			}
			
			// Intentionally avoid display/localized name checks to prevent false positives
			return false;
		}

		private static bool IsChest(ActorVisuals actor)
		{
			var go = actor.gameObject;
			if (go.GetComponent<ChestVisualsCreator>() != null
				|| go.GetComponentInParent<ChestVisualsCreator>() != null
				|| go.GetComponentInChildren<ChestVisualsCreator>() != null)
			{
				return true;
			}
			return false;
		}
 
 		private static bool IsChampion(ActorVisuals actor)
 		{
 			var info = actor.GetComponent<ActorDisplayInformation>();
 			if (info == null) return false;
 			try
 			{
 				// Certain IL2CPP variants may throw on missing bindings; guard accordingly
 				return info.IsChampion();
 			}
 			catch (Exception)
 			{
 				return false;
 			}
 		}
 
 		internal static SpecialEntityType DetectSpecialEntity(ActorVisuals actor)
 		{
 			// Loot Lizards are unique entities; detect them first
 			if (IsLootLizard(actor))
 			{
 				return SpecialEntityType.LootLizard;
 			}

			// Chests via visuals creator component
			if (IsChest(actor))
			{
				return SpecialEntityType.Chest;
			}
 
 			// Champion NPCs (super rares)
 			if (IsChampion(actor))
 			{
 				return SpecialEntityType.Champion;
 			}
 
 			return SpecialEntityType.None;
 		}

		// Convenience exposure for later use by other systems
		internal static bool IsChampionActor(ActorVisuals actor) => DetectSpecialEntity(actor) == SpecialEntityType.Champion;
		internal static bool IsChestActor(ActorVisuals actor) => DetectSpecialEntity(actor) == SpecialEntityType.Chest;

		public static void GatherActors()
		{
			if (ActorManager.instance == null) return;

			var localPlayer = ObjectManager.GetLocalPlayer();
			if (localPlayer == null) return;

			foreach (var visual in ActorManager.instance.visuals)
			{
				foreach (var actor in visual.visuals._list)
				{
					if (!actor.gameObject.activeInHierarchy) continue;

					// Detect special entities (loot lizards, champions, etc.)
					var special = DetectSpecialEntity(actor);
					bool isLizard = special == SpecialEntityType.LootLizard;

					// Non-lizard actors respect alignment/classification filters
					if (!isLizard)
					{
						if (!Settings.ShouldDrawNPCAlignment(visual.alignment.name)) continue;

						var actorDisplayInfo = actor.GetComponent<ActorDisplayInformation>();
						if (actorDisplayInfo != null)
						{
							// in 1.2 in offline mode, we could find the lizards reliably by the LootLizardFleeing component
							// TODO: verify if this is still the case in 1.3
							if (!Settings.ShouldDrawNPCClassification(actorDisplayInfo.actorClass))
							{
								continue;
							}
						}
					}

					float distance = Vector3.Distance(
						actor.transform.position, localPlayer.transform.position);

					if (distance >= Settings.drawDistance || actor.dead) continue;

					var name = GetActorName(actor);
					var position = actor.GetHealthBarPosition();
					position.y += 0.5f;

					var color = (isLizard || special == SpecialEntityType.Champion || special == SpecialEntityType.Chest) ? BloodOrange : GetRarityColor(actor, visual.alignment.name);
					ESP.AddLine(localPlayer.transform.position, actor.transform.position, color);
					//ESP.AddString(name + " (" + distance.ToString("F1") + ")  ", position, color);
					ESP.AddString(name, position, color);
				}
			}
		}

		public static void OnUpdate()
		{
			if (ObjectManager.HasPlayer())
			{
				GatherActors();
			}
		}
	}
}

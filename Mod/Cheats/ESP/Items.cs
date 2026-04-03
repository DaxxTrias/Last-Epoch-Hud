using Il2Cpp;
using Il2CppItemFiltering;
using Mod.Game;
using System;
using UnityEngine;

namespace Mod.Cheats.ESP
{
    internal class Items
    {
        private static readonly string[] s_supportedRarities =
        {
            "Magic",
            "Common",
            "Unique",
            "Legendary",
            "Rare",
            "Set",
            "Exalted"
        };

        public static void GatherItems()
        {
            if (GroundItemVisuals.all == null) return;

            var localPlayer = ObjectManager.GetLocalPlayer();
            if (localPlayer == null || localPlayer.transform == null) return;

            foreach (var item in GroundItemVisuals.all._list)
            {
                // Ensure the item is active in the scene.
                if (item?.gameObject == null || !item.gameObject.activeInHierarchy) continue;

                if (Vector3.Distance(localPlayer.transform.position, item.transform.position) > Settings.drawDistance) continue;

                Rule.RuleOutcome filter = ItemFiltering.Match(item.itemData, null, null, 0);

                if (Settings.useLootFilter && filter == Rule.RuleOutcome.HIDE) continue;

                var rarity = ResolveItemRarity(item);

                // Ensure rarity is not null before calling ShouldDrawItemRarity.
                if (string.IsNullOrEmpty(rarity) || !Settings.ShouldDrawItemRarity(rarity))
                {
                    continue;
                }

                var color = Drawing.ItemRarityToColor(rarity);

                ESP.AddLine(localPlayer.transform.position, item.transform.position, color);
                ESP.AddString(item.itemData.FullName, item.transform.position, color);
            }
        }

        private static string? ResolveItemRarity(GroundItemVisuals item)
        {
            if (item == null) return null;

            // Legacy fallback in case upstream fields are still populated in some game versions.
            string? legacyName = NormalizeRarity(item.groundItemRarityVisuals?.name);
            if (!string.IsNullOrEmpty(legacyName))
            {
                return legacyName;
            }

            Transform? rarityRoot = item.groundItemRarityVisuals?.transform;
            if (rarityRoot == null)
            {
                return null;
            }

            int childCount = rarityRoot.childCount;
            for (int i = 0; i < childCount; i++)
            {
                Transform child = rarityRoot.GetChild(i);
                if (child == null || child.gameObject == null || !child.gameObject.activeSelf)
                {
                    continue;
                }

                string? childRarity = NormalizeRarity(child.name);
                if (!string.IsNullOrEmpty(childRarity))
                {
                    return childRarity;
                }
            }

            return null;
        }

        private static string? NormalizeRarity(string? rawName)
        {
            if (string.IsNullOrWhiteSpace(rawName))
            {
                return null;
            }

            for (int i = 0; i < s_supportedRarities.Length; i++)
            {
                string rarity = s_supportedRarities[i];
                if (rawName.IndexOf(rarity, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return rarity;
                }
            }

            return null;
        }

        public static void OnUpdate()
        {
            if (ObjectManager.HasPlayer())
            {
                GatherItems();
            }
        }
    }
}

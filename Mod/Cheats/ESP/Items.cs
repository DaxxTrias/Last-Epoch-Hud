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

                if (Settings.useLootFilter)
                {
                    Rule.RuleOutcome filter = ItemFiltering.Match(item.itemData, null, null, 0);
                    if (filter == Rule.RuleOutcome.HIDE) continue;
                }

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

            string? itemDataRarity = ResolveItemDataRarity(item.itemData);
            if (!string.IsNullOrEmpty(itemDataRarity))
            {
                return itemDataRarity;
            }

            // New LE versions populate the V2 rarity visuals; keep legacy as a fallback.
            string? v2Name = NormalizeRarity(item.groundItemRarityVisualsV2?.name);
            if (!string.IsNullOrEmpty(v2Name))
            {
                return v2Name;
            }

            string? legacyName = NormalizeRarity(item.groundItemRarityVisuals?.name);
            if (!string.IsNullOrEmpty(legacyName))
            {
                return legacyName;
            }

            Transform? rarityRoot = item.groundItemRarityVisualsV2?.transform ?? item.groundItemRarityVisuals?.transform;
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

        private static string? ResolveItemDataRarity(ItemDataUnpacked? itemData)
        {
            if (itemData == null)
            {
                return null;
            }

            // Preferred source in current LE builds.
            try
            {
                string? methodRarity = NormalizeRarity(itemData.GetDefaultRarityVisualRarity().ToString());
                if (!string.IsNullOrEmpty(methodRarity))
                {
                    return methodRarity;
                }
            }
            catch (Exception)
            {
                // IL2CPP binding can throw in some edge cases; continue with byte fallback.
            }

            return ResolveItemDataRarityFromByte(itemData.rarity);
        }

        private static string? ResolveItemDataRarityFromByte(byte rarityByte)
        {
            // Known values observed in-game after the LE update.
            return rarityByte switch
            {
                3 => "Rare",
                7 => "Unique",
                _ => null
            };
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

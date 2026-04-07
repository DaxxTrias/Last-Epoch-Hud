using Il2Cpp;
using Mod.Game;
using UnityEngine;
using Color = UnityEngine.Color;

namespace Mod.Cheats.ESP
{
    internal class GoldPiles
    {
        private const string GoldSuffix = " Gold";

        public static void GatherGoldPiles(GameObject player)
        {
            if (!Settings.DrawGoldPiles() || Settings.useLootFilter) return;
            if (GroundGoldVisuals.all == null) return;

            var playerPos = player.transform.position;
            float maxDistSq = Settings.drawDistance * Settings.drawDistance;

            foreach (var item in GroundGoldVisuals.all._list)
            {
                if (item?.gameObject == null || !item.gameObject.activeInHierarchy) continue;

                var itemPos = item.transform.position;
                var delta = itemPos - playerPos;
                if (delta.sqrMagnitude > maxDistSq) continue;

                ESP.AddLine(playerPos, itemPos, Color.white);
                ESP.AddString(string.Concat(item.goldValue.ToString(), GoldSuffix), itemPos, Color.white);
            }
        }

        public static void OnUpdate(GameObject player)
        {
            GatherGoldPiles(player);
        }
    }
}

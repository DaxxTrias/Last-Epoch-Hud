using Il2Cpp;
using Mod.Game;
using UnityEngine;
using Color = UnityEngine.Color;

namespace Mod.Cheats.ESP
{
    internal class GoldPiles
    {
        private const string GoldSuffix = " Gold";

        public static void GatherGoldPiles()
        {
            if (!Settings.DrawGoldPiles() || Settings.useLootFilter) return;
            if (GroundGoldVisuals.all == null) return;

            var localPlayer = ObjectManager.GetLocalPlayer();
            if (localPlayer == null || localPlayer.transform == null) return;

            var playerPos = localPlayer.transform.position;
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

        public static void OnUpdate()
        {
            if (ObjectManager.HasPlayer())
            {
                GatherGoldPiles();
            }
        }
    }
}

using Il2Cpp;

namespace Mod
{
    internal class Settings
    {
        public static bool mapHack = true;
        public static float drawDistance = 100.0f;
        public static float autoHealthPotion = 50.0f;
        public static float timeScale = 1.0f;
        public static bool useAutoPot = true;
        public static bool useLootFilter = true;
        public static bool removeFog = true;
        public static bool cameraZoomUnlock = true;
        public static bool minimapZoomUnlock = true;
        public static bool playerLantern = true;
        public static bool useAnyWaypoint = false;
        //public static bool pickupCrafting = false;

        public static Dictionary<string, bool> npcClassifications = new Dictionary<string, bool>
        {
            { "Normal", false },
            { "Magic", true },
            { "Rare", true },
            { "Boss", true }
        };

        public static Dictionary<string, bool> npcDrawings = new Dictionary<string, bool>
        {
            { "Good", false },
            { "Evil", true },
            { "Barrel", false },
            { "HostileNeutral", true },
            { "FriendlyNeutral", true },
            { "SummonedCorpse", true }
        };

        public static Dictionary<string, bool> itemDrawings = new Dictionary<string, bool>
        {
            { "Magic", true },
            { "Common", false },
            { "Unique", true },
            { "Legendary", true },
            { "Rare", true },
            { "Set", true },
            { "Exalted", true },
            { "Gold Piles", false }
        };

        public static bool DrawGoldPiles()
        {
            return itemDrawings.TryGetValue("Gold Piles", out bool draw) ? draw : false;
        }

        public static bool ShouldDrawItemRarity(string rarity)
        {
            foreach (KeyValuePair<string, bool> entry in itemDrawings)
            {
                if (rarity.Contains(entry.Key))
                {
                    return entry.Value;
                }
            }

            return false;
        }

        public static bool ShouldDrawNPCAlignment(string alignment)
        {
            return npcDrawings.TryGetValue(alignment, out bool draw) ? draw : false;
        }

        public static bool ShouldDrawNPCClassification(DisplayActorClass actorClass)
        {
            string classificationKey = "Normal";

            switch (actorClass)
            {
                case DisplayActorClass.Magic:
                    classificationKey = "Magic";
                    break;
                case DisplayActorClass.Rare:
                    classificationKey = "Rare";
                    break;
                case DisplayActorClass.Boss:
                    classificationKey = "Boss";
                    break;
            }

            return npcClassifications.TryGetValue(classificationKey, out bool draw) ? draw : false;
        }

        public static bool ShouldDrawShrine(string shrineType)
        {
            return shrineType == "Shrine of Scales" || shrineType == "Shrine of Shards";
        }
    }
}

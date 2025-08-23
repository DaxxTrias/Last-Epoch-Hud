using Il2Cpp;

namespace Mod
{
    internal class Settings
    {
        public static bool mapHack = true;
        public static float drawDistance = 100.0f;
        public static float autoHealthPotion = 50.0f;
        public static float autoPotionCooldown = 1.0f; // Configurable cooldown in seconds
        public static float timeScale = 1.0f;
        public static bool useAutoPot = true;
        public static bool useLootFilter = true;
        public static bool removeFog = true;
        public static bool cameraZoomUnlock = true;
        public static bool minimapZoomUnlock = true;
        public static bool playerLantern = true;
        public static bool useAnyWaypoint = false;
        public static bool useAntiIdle = false;
        public static float antiIdleInterval = 60f; // Anti-idle action interval in seconds
        public static bool useSyntheticKeepAlive = true; // Send small user message periodically
        public static float keepAliveInterval = 20f; // Seconds between synthetic keepalive sends

        // Anti-Idle suppression controls
        public static bool suppressKeepAliveOnActivity = true; // Pause synthetic keepalive when user activity is detected
        public static float activitySuppressionSeconds = 120f; // How long to suppress after input/activity
        public static float sceneChangeSuppressionSeconds = 150f; // Suppress on scene change
        public static float networkActivitySuppressionSeconds = 45f; // Suppress after any outbound message

        //public static bool pickupCrafting = false;

        // Minimap Enemy Circles Settings
        public static bool showMinimapEnemyCircles = true;
        public static float minimapCircleSize = 6f;
        public static float minimapScale = 8.3f; // Fallback pixels-per-meter if autoScaleMinimap is disabled
        public static bool autoScaleMinimap = true; // Derive pixels-per-meter from Icons rect and world radius
        public static float minimapScaleFactor = 1.3f; // Additional multiplier on computed pixels-per-meter
        public static float minimapWorldRadiusMeters = 67f; // Real-world radius represented by the minimap visible radius
        public static bool minimapFlipX = false; // Flip horizontal axis to match DMap handedness
        public static bool minimapFlipY = false; // Flip vertical axis if needed
        public static float minimapBasisRotationDegrees = 90f; // Additional rotation to align axes (applied before map rotation)
        public static bool showMagicMonsters = true;
        public static bool showRareMonsters = true;
        public static bool showWhiteMonsters = false;
        public static float minimapOffsetX = 0f;
        public static float minimapOffsetY = 0f;

        // public static bool debugESPNames = false;

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

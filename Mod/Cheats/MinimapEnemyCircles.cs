using System.Collections.Generic;
using UnityEngine;
using Il2Cpp;
using MelonLoader;
using System.Linq;
using Mod.Game;

namespace Mod.Cheats
{
    public static class MinimapEnemyCircles
    {
        // Canvas reference for UI
        private static Canvas? minimapCanvas;
        private static bool isInitialized = false;
        private static bool isMinimapOpen = false;
        private static bool wasTabPressed = false;
        
        // Debug info for GUI display
        public static string lastDebugInfo = "";
        public static int lastEnemyCount = 0;
        public static int lastCircleCount = 0;
        public static string lastCreateAttempt = "";
        
        // Store our UI circles for cleanup
        private static readonly List<GameObject> enemyCircles = new List<GameObject>();
        private static int debugUpdateCounter = 0; // Add counter to reduce debug frequency
        
        // Target containers discovered from the scene hierarchy (screenshot)
        private static RectTransform? iconsContainer; // ".../DMMap Canvas/Icons"
        private static RectTransform? mapContainer;   // ".../DMMap Canvas/Map" (optional for later)
        private static GameObject? smallMinimapBg;    // "GUI/Canvas (animated)/Minimap Holder/Minimap(Clone)/SquareMinimap/minimapBG"
        
        // Prebuilt sprites cache to avoid per-frame texture allocations
        private static Sprite? spriteWhite;
        private static Sprite? spriteYellow;
        private static Sprite? spriteBlue;
        private static Sprite? spriteRed;

        private static readonly Color MagicLightBlue = new Color(0.55f, 0.8f, 1f, 1f);
        
        public static void Update()
        {
            // Only update debug info every 30 frames (about twice per second at 60fps)
            debugUpdateCounter++;
            bool shouldUpdateDebug = debugUpdateCounter >= 30;
            if (shouldUpdateDebug) 
            {
                debugUpdateCounter = 0;
            }
            
            // Always update debug info, even if feature is disabled
            if (!Settings.showMinimapEnemyCircles) 
            {
                if (shouldUpdateDebug) lastDebugInfo = "Feature DISABLED in settings";
                return;
            }
            
            if (!Initialize(shouldUpdateDebug)) return;
            
            // Skip rendering if fullscreen map is open (detected via small minimap BG inactive)
            if (IsFullscreenMapOpen())
            {
                ClearCircles();
                if (shouldUpdateDebug) lastDebugInfo = "Fullscreen map OPEN - circles suppressed";
                return;
            }
            
            CheckMinimapToggle(shouldUpdateDebug);
            
            if (!isMinimapOpen) 
            {
                ClearCircles();
                if (shouldUpdateDebug) lastDebugInfo = "Minimap CLOSED - circles cleared";
                return;
            }
            
            UpdateEnemyCircles(shouldUpdateDebug);
        }
        
        private static void CheckMinimapToggle(bool updateDebug = true)
        {
            // Drive open state from the Icons container visibility if available
            if (iconsContainer != null)
            {
                isMinimapOpen = iconsContainer.gameObject.activeInHierarchy;
                return;
            }
            
            // Fallback: previous tab toggle (kept as a last resort)
            bool isTabPressed = Input.GetKey(KeyCode.Tab);
            if (isTabPressed && !wasTabPressed)
            {
                isMinimapOpen = !isMinimapOpen;
            }
            wasTabPressed = isTabPressed;
        }
        
        private static bool IsFullscreenMapOpen()
        {
            // Explicit sentinel: when small minimap BG is disabled, fullscreen map is likely open
            if (smallMinimapBg != null)
            {
                return !smallMinimapBg.activeInHierarchy;
            }
            // If not bound yet, try to find once by explicit path
            smallMinimapBg = GameObject.Find("GUI/Canvas (animated)/Minimap Holder/Minimap(Clone)/SquareMinimap/minimapBG");
            return smallMinimapBg != null && !smallMinimapBg.activeInHierarchy;
        }
        
        public static bool Initialize(bool updateDebug = true)
        {
            if (isInitialized && (iconsContainer != null)) return true;
            
            try
            {
                // Try exact paths first (from screenshot)
                GameObject iconsGO = GameObject.Find("BWF/GameManager/GeneralGameManager/Minimap Folder/DMMap/DMMap Canvas/Icons");
                GameObject mapGO = GameObject.Find("BWF/GameManager/GeneralGameManager/Minimap Folder/DMMap/DMMap Canvas/Map");
                
                if (iconsGO == null)
                {
                    // Fallback: find any GameObject named "Icons" under a parent named "DMMap Canvas"
                    foreach (var rt in UnityEngine.Object.FindObjectsOfType<RectTransform>())
                    {
                        if (rt == null) continue;
                        if (rt.name != "Icons") continue;
                        Transform parent = rt.transform.parent;
                        while (parent != null)
                        {
                            if (parent.name == "DMMap Canvas")
                            {
                                iconsGO = rt.gameObject;
                                break;
                            }
                            parent = parent.parent;
                        }
                        if (iconsGO != null) break;
                    }
                }
                
                if (iconsGO != null)
                {
                    iconsContainer = iconsGO.GetComponent<RectTransform>();
                    minimapCanvas = iconsGO.GetComponentInParent<Canvas>();
                }
                
                if (mapGO != null)
                {
                    mapContainer = mapGO.GetComponent<RectTransform>();
                }
                
                // Bind small minimap BG sentinel
                smallMinimapBg = GameObject.Find("GUI/Canvas (animated)/Minimap Holder/Minimap(Clone)/SquareMinimap/minimapBG");
                
                // Build sprite cache once
                EnsureSpriteCache();
                
                if (iconsContainer != null)
                {
                    isInitialized = true;
                    if (updateDebug) lastDebugInfo = $"Bound Icons container: {iconsContainer.name}";
                    return true;
                }
                
                if (updateDebug) lastDebugInfo = "Icons container not found";
                return false;
            }
            catch (System.Exception e)
            {
                if (updateDebug) lastDebugInfo = $"Initialize error: {e.Message}";
                return false;
            }
        }
        
        private static void EnsureSpriteCache()
        {
            // Choose a fixed reasonable texture size for cached sprites; scale via RectTransform
            const int baseSize = 16;
            if (spriteWhite == null) spriteWhite = BuildCircleSprite(baseSize, Color.white);
            if (spriteYellow == null) spriteYellow = BuildCircleSprite(baseSize, Color.yellow);
            if (spriteBlue == null) spriteBlue = BuildCircleSprite(baseSize, MagicLightBlue);
            if (spriteRed == null) spriteRed = BuildCircleSprite(baseSize, Color.red);
        }
        
        private static Sprite BuildCircleSprite(int size, Color color)
        {
            var tex = CreateCircleTexture(size, color);
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }
        
        public static void UpdateEnemyCircles(bool updateDebug = true)
        {
            try
            {
                if (ActorManager.instance == null) 
                {
                    if (updateDebug) lastDebugInfo = "ActorManager.instance is null";
                    return;
                }
                
                // Try to get player using ObjectManager
                var playerActor = ObjectManager.GetLocalPlayer();
                if (playerActor == null)
                {
                    if (updateDebug) lastDebugInfo = "No local player found";
                    return;
                }
                
                // Clear old circles each frame to get fresh positions
                ClearCircles();
                
                List<ActorVisuals> enemies = new List<ActorVisuals>();
                int totalVisuals = 0;
                int alignmentFiltered = 0;
                
                foreach (var visual in ActorManager.instance.visuals)
                {
                    totalVisuals++;
                    
                    // Skip if no alignment or not hostile
                    if (visual.alignment == null) continue;
                    if (visual.alignment.name != "Evil" && visual.alignment.name != "HostileNeutral") 
                    {
                        alignmentFiltered++;
                        continue;
                    }
                    
                    // Check each visual actor
                    if (visual.visuals != null && visual.visuals._list != null)
                    {
                        foreach (var actor in visual.visuals._list)
                        {
                            if (actor == null || actor.gameObject == null) continue;
                            if (!actor.gameObject.activeInHierarchy) continue;
                            
                            // Skip dead enemies
                            if (actor.dead) continue;
                            
                            // Check distance to player
                            float distance = Vector3.Distance(
                                actor.transform.position, playerActor.transform.position);
                            
                            if (distance < Settings.drawDistance)
                            {
                                enemies.Add(actor);
                            }
                        }
                    }
                }
                
                lastEnemyCount = enemies.Count;
                int successfulCircles = 0;
                
                // Precompute mapping parameters
                float pixelsPerMeter = GetPixelsPerMeter();
                float mapRotationRad = GetMapRotationRadians();
                Vector2 maxBounds = GetIconRectHalfSize();
                
                foreach (var enemy in enemies.Take(100)) // Limit to 100 enemies
                {
                    var enemyPos = enemy.transform.position;
                    var minimapPos = WorldToMinimapPosition(enemyPos, playerActor.transform.position, pixelsPerMeter, mapRotationRad);
                    
                    // Check if position is within Icons rect bounds
                    if (Mathf.Abs(minimapPos.x) > maxBounds.x || Mathf.Abs(minimapPos.y) > maxBounds.y)
                        continue;
                    
                    // Check if this enemy type should be shown based on settings
                    if (!ShouldShowEnemyType(enemy))
                        continue;
                    
                    var color = GetEnemyColor(enemy);
                    CreateMinimapCircle(enemy, color, minimapPos);
                    successfulCircles++;
                }
                
                lastCircleCount = enemyCircles.Count;
                
                // Update creation attempt status
                lastCreateAttempt = $"Successfully created {successfulCircles} circles for {lastEnemyCount} enemies";
                
                // Set final debug info with complete status
                if (updateDebug) lastDebugInfo = $"ACTIVE | Summary: Visuals: {totalVisuals}, Filtered: {alignmentFiltered}, Enemies: {lastEnemyCount}, Circles: {lastCircleCount}";
            }
            catch (System.Exception e)
            {
                if (updateDebug) lastDebugInfo = $"Error updating circles: {e.Message}";
            }
        }
        
        private static float GetPixelsPerMeter()
        {
            if (iconsContainer == null)
            {
                return Settings.minimapScale * Settings.minimapScaleFactor;
            }
            if (!Settings.autoScaleMinimap)
            {
                return Settings.minimapScale * Settings.minimapScaleFactor;
            }
            var rect = iconsContainer.rect;
            float radiusPixels = Mathf.Min(rect.width, rect.height) * 0.5f;
            float radiusMeters = Mathf.Max(1f, Settings.minimapWorldRadiusMeters);
            return (radiusPixels / radiusMeters) * Settings.minimapScaleFactor;
        }
        
        private static float GetMapRotationRadians()
        {
            if (mapContainer == null)
                return 0f;
            // Map often rotates around Z; rotate our relative vector by -map rotation to stay aligned
            float zDeg = mapContainer.localEulerAngles.z;
            return -zDeg * Mathf.Deg2Rad;
        }
        
        private static Vector2 GetIconRectHalfSize()
        {
            if (iconsContainer == null)
                return new Vector2(300f, 300f);
            var rect = iconsContainer.rect;
            return new Vector2(rect.width * 0.5f, rect.height * 0.5f);
        }
        
        private static float GetBasisRotationRadians()
        {
            return Settings.minimapBasisRotationDegrees * Mathf.Deg2Rad;
        }
        
        private static Vector2 WorldToMinimapPosition(Vector3 worldPosition, Vector3 playerPosition, float pixelsPerMeter, float rotationRadians)
        {
            // Calculate relative position to player in world XZ plane
            Vector2 rel = new Vector2(worldPosition.x - playerPosition.x, worldPosition.z - playerPosition.z);
            
            // Apply basis rotation to align world axes to DMap axes
            float basisRad = GetBasisRotationRadians();
            if (basisRad != 0f)
            {
                float cosB = Mathf.Cos(basisRad);
                float sinB = Mathf.Sin(basisRad);
                float bx = rel.x * cosB - rel.y * sinB;
                float by = rel.x * sinB + rel.y * cosB;
                rel = new Vector2(bx, by);
            }
            
            // Rotate to match map rotation
            if (rotationRadians != 0f)
            {
                float cos = Mathf.Cos(rotationRadians);
                float sin = Mathf.Sin(rotationRadians);
                float rx = rel.x * cos - rel.y * sin;
                float ry = rel.x * sin + rel.y * cos;
                rel = new Vector2(rx, ry);
            }
            
            // Optional axis flips to match DMap handedness
            if (Settings.minimapFlipX) rel.x = -rel.x;
            if (Settings.minimapFlipY) rel.y = -rel.y;
            
            // Convert to minimap space (Unity UI is typically +Y up). Map convention: x->right, z->forward
            float minimapX = rel.x * pixelsPerMeter + Settings.minimapOffsetX;
            float minimapY = rel.y * pixelsPerMeter + Settings.minimapOffsetY;
            
            return new Vector2(minimapX, minimapY);
        }
        
        private static Color GetEnemyColor(ActorVisuals enemy)
        {
            var displayInfo = enemy.GetComponent<ActorDisplayInformation>();
            if (displayInfo != null)
            {
                if (displayInfo.actorClass == DisplayActorClass.Boss) return Color.red;
                if (displayInfo.actorClass == DisplayActorClass.Rare) return Color.yellow;
                if (displayInfo.actorClass == DisplayActorClass.Magic) return MagicLightBlue;
            }
            return Color.white;
        }
        
        private static Sprite? GetSpriteForColor(Color color)
        {
            // Map arbitrary input color to the closest cached sprite
            if (color.Equals(Color.red)) return spriteRed;
            if (color.Equals(Color.yellow)) return spriteYellow;
            if (Approximately(color, MagicLightBlue) || color.Equals(Color.blue)) return spriteBlue;
            return spriteWhite;
        }
        
        private static bool Approximately(Color a, Color b)
        {
            return Mathf.Abs(a.r - b.r) < 0.02f &&
                   Mathf.Abs(a.g - b.g) < 0.02f &&
                   Mathf.Abs(a.b - b.b) < 0.02f &&
                   Mathf.Abs(a.a - b.a) < 0.02f;
        }
        
        private static bool ShouldShowEnemyType(ActorVisuals enemy)
        {
            var displayInfo = enemy.GetComponent<ActorDisplayInformation>();
            if (displayInfo != null)
            {
                // Check each monster type against settings
                if (displayInfo.actorClass == DisplayActorClass.Magic && !Settings.showMagicMonsters)
                    return false;
                if (displayInfo.actorClass == DisplayActorClass.Rare && !Settings.showRareMonsters)
                    return false;
                if (displayInfo.actorClass == DisplayActorClass.Boss) // Always show bosses (they're red)
                    return true;
                
                // For normal/white monsters
                if (displayInfo.actorClass == DisplayActorClass.Normal && !Settings.showWhiteMonsters)
                    return false;
            }
            else
            {
                // If no display info, treat as white monster
                if (!Settings.showWhiteMonsters)
                    return false;
            }
            
            return true; // Show by default if not filtered out
        }
        
        private static void CreateMinimapCircle(ActorVisuals enemy, Color color, Vector2 position)
        {
            try
            {
                if (iconsContainer == null) 
                {
                    lastCreateAttempt = "Icons container is null - cannot create circle";
                    return;
                }
                
                // Clamp and unify size to avoid zero/invalid textures
                int size = Mathf.Max(2, Mathf.RoundToInt(Settings.minimapCircleSize));
                Vector2 sizeDelta = new Vector2(size, size);
                
                // Resolve sprite from cache; build on-demand fallback if needed
                Sprite? sprite = GetSpriteForColor(color);
                if (sprite == null)
                {
                    sprite = BuildCircleSprite(Mathf.Max(8, size), color);
                }
                
                // Create object only after we have a valid sprite
                var circleObj = new GameObject($"EnemyCircle_{enemy.name}");
                circleObj.transform.SetParent(iconsContainer.transform, false);
                
                var rectTransform = circleObj.AddComponent<RectTransform>();
                rectTransform.anchoredPosition = position;
                rectTransform.sizeDelta = sizeDelta;
                
                var image = circleObj.AddComponent<UnityEngine.UI.Image>();
                image.raycastTarget = false;
                image.sprite = sprite;
                
                enemyCircles.Add(circleObj);
                lastCreateAttempt = $"Created circle at {position} for {enemy.name}";
            }
            catch (System.Exception e)
            {
                lastCreateAttempt = $"Failed to create circle: {e.Message}";
            }
        }
        
        private static Texture2D CreateCircleTexture(int size, Color color)
        {
            size = Mathf.Max(2, size);
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;
            var center = new Vector2(size / 2f, size / 2f);
            var radius = size / 2f;
            
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    
                    if (distance <= radius)
                    {
                        float alpha = distance <= radius - 1 ? 1.0f : 1.0f - (distance - (radius - 1));
                        texture.SetPixel(x, y, new Color(color.r, color.g, color.b, alpha));
                    }
                    else
                    {
                        texture.SetPixel(x, y, Color.clear);
                    }
                }
            }
            
            texture.Apply(false, false);
            return texture;
        }
        
        public static void ClearCircles()
        {
            foreach (var circle in enemyCircles)
            {
                if (circle != null)
                {
                    UnityEngine.Object.Destroy(circle);
                }
            }
            enemyCircles.Clear();
        }
    }
}

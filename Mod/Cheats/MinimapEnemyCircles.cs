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
        private static List<GameObject> enemyCircles = new List<GameObject>();
        private static int debugUpdateCounter = 0; // Add counter to reduce debug frequency
        
        // Target containers discovered from the scene hierarchy (screenshot)
        private static RectTransform? iconsContainer; // ".../DMMap Canvas/Icons"
        private static RectTransform? mapContainer;   // ".../DMMap Canvas/Map" (optional for later)
        
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
                
                // If nothing was created, draw a center debug circle to validate visibility
                if (successfulCircles == 0 && iconsContainer != null)
                {
                    var color = new Color(1f, 0f, 1f, 0.9f); // magenta for visibility
                    CreateDebugCircleAtCenter(color);
                }
                
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
                return Settings.minimapScale;
            }
            if (!Settings.autoScaleMinimap)
            {
                return Settings.minimapScale;
            }
            var rect = iconsContainer.rect;
            float radiusPixels = Mathf.Min(rect.width, rect.height) * 0.5f;
            float radiusMeters = Mathf.Max(1f, Settings.drawDistance);
            return radiusPixels / radiusMeters;
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
        
        private static Vector2 WorldToMinimapPosition(Vector3 worldPosition, Vector3 playerPosition, float pixelsPerMeter, float rotationRadians)
        {
            // Calculate relative position to player in world XZ plane
            Vector2 rel = new Vector2(worldPosition.x - playerPosition.x, worldPosition.z - playerPosition.z);
            
            // Rotate to match map rotation
            if (rotationRadians != 0f)
            {
                float cos = Mathf.Cos(rotationRadians);
                float sin = Mathf.Sin(rotationRadians);
                float rx = rel.x * cos - rel.y * sin;
                float ry = rel.x * sin + rel.y * cos;
                rel = new Vector2(rx, ry);
            }
            
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
                if (displayInfo.actorClass == DisplayActorClass.Magic) return Color.blue;
            }
            return Color.white;
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
                
                var circleObj = new GameObject($"EnemyCircle_{enemy.name}");
                circleObj.transform.SetParent(iconsContainer.transform, false);
                
                var rectTransform = circleObj.AddComponent<RectTransform>();
                rectTransform.anchoredPosition = position;
                rectTransform.sizeDelta = new Vector2(Settings.minimapCircleSize, Settings.minimapCircleSize);
                
                var image = circleObj.AddComponent<UnityEngine.UI.Image>();
                var texture = CreateCircleTexture((int)Settings.minimapCircleSize, color);
                var sprite = Sprite.Create(texture, new Rect(0, 0, Settings.minimapCircleSize, Settings.minimapCircleSize), new Vector2(0.5f, 0.5f));
                image.sprite = sprite;
                
                enemyCircles.Add(circleObj);
                lastCreateAttempt = $"Created circle at {position} for {enemy.name}";
            }
            catch (System.Exception e)
            {
                lastCreateAttempt = $"Failed to create circle: {e.Message}";
            }
        }
        
        private static void CreateDebugCircleAtCenter(Color color)
        {
            if (iconsContainer == null) return;
            var go = new GameObject("EnemyCircle_DebugCenter");
            go.transform.SetParent(iconsContainer.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(Settings.minimapCircleSize * 1.5f, Settings.minimapCircleSize * 1.5f);
            var img = go.AddComponent<UnityEngine.UI.Image>();
            var tex = CreateCircleTexture((int)(Settings.minimapCircleSize * 1.5f), color);
            var spr = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            img.sprite = spr;
            enemyCircles.Add(go);
        }
        
        private static Texture2D CreateCircleTexture(int size, Color color)
        {
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
                    UnityEngine.Object.DestroyImmediate(circle);
                }
            }
            enemyCircles.Clear();
        }
    }
}

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
            
            CheckMinimapToggle(shouldUpdateDebug);
            
            if (!isMinimapOpen) 
            {
                ClearCircles();
                if (shouldUpdateDebug) lastDebugInfo = "Minimap CLOSED - circles cleared";
                return;
            }
            
            if (!Initialize(shouldUpdateDebug)) return;
            
            UpdateEnemyCircles(shouldUpdateDebug);
        }
        
        private static void CheckMinimapToggle(bool updateDebug = true)
        {
            bool isTabPressed = Input.GetKey(KeyCode.Tab);
            
            // Toggle on Tab press (not hold)
            if (isTabPressed && !wasTabPressed)
            {
                isMinimapOpen = !isMinimapOpen;
            }
            
            wasTabPressed = isTabPressed;
            
            // Also check if the actual minimap UI is visible when we think it should be open
            if (isMinimapOpen && minimapCanvas != null)
            {
                bool canvasActive = minimapCanvas.gameObject.activeInHierarchy && minimapCanvas.enabled;
                if (!canvasActive)
                {
                    isMinimapOpen = false;
                }
            }
        }
        
        public static bool Initialize(bool updateDebug = true)
        {
            if (isInitialized && minimapCanvas != null) return true;
            
            var allCanvases = UnityEngine.Object.FindObjectsOfType<Canvas>();
            
            if (allCanvases != null)
            {
                // Try multiple canvas name patterns
                string[] minimapNames = { "minimap", "Minimap", "MINIMAP", "map", "Map" };
                
                foreach (var canvas in allCanvases)
                {
                    if (canvas?.name != null)
                    {
                        foreach (var pattern in minimapNames)
                        {
                            if (canvas.name.ToLower().Contains(pattern.ToLower()))
                            {
                                minimapCanvas = canvas;
                                isInitialized = true;
                                if (updateDebug) lastDebugInfo = $"Found minimap canvas: {canvas.name}";
                                return true;
                            }
                        }
                    }
                }
                
                // If no minimap canvas found, try to use any canvas as fallback
                if (allCanvases.Length > 0)
                {
                    minimapCanvas = allCanvases[0];
                    isInitialized = true;
                    if (updateDebug) lastDebugInfo = $"Using fallback canvas: {allCanvases[0].name}";
                    return true;
                }
            }
            
            if (updateDebug) lastDebugInfo = "No suitable canvas found";
            return false;
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
                            
                            if (distance < Settings.minimapDrawDistance)
                            {
                                enemies.Add(actor);
                            }
                        }
                    }
                }
                
                lastEnemyCount = enemies.Count;
                int successfulCircles = 0;
                
                foreach (var enemy in enemies.Take(100)) // Limit to 100 enemies
                {
                    var enemyPos = enemy.transform.position;
                    var minimapPos = WorldToMinimapPosition(enemyPos, playerActor.transform.position);
                    
                    // Check if position is within reasonable bounds
                    if (Mathf.Abs(minimapPos.x) > 300.0f || Mathf.Abs(minimapPos.y) > 300.0f)
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
        
        private static Vector2 WorldToMinimapPosition(Vector3 worldPosition, Vector3 playerPosition)
        {
            // Calculate relative position to player
            Vector3 relativePosition = worldPosition - playerPosition;
            
            // Rotate 90 degrees left (counter-clockwise): (x,y) -> (-y,x)
            // Original: x = relativePosition.x, y = relativePosition.z
            // After 90┬░ left rotation: x = -relativePosition.z, y = relativePosition.x
            float minimapX = -relativePosition.z * Settings.minimapScale + Settings.minimapOffsetX;
            float minimapY = relativePosition.x * Settings.minimapScale + Settings.minimapOffsetY;
            
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
                if (minimapCanvas == null) 
                {
                    lastCreateAttempt = "Canvas is null - cannot create circle";
                    return;
                }
                
                var circleObj = new GameObject($"EnemyCircle_{enemy.name}");
                circleObj.transform.SetParent(minimapCanvas.transform, false);
                
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
        
        private static Texture2D CreateCircleTexture(int size, Color color)
        {
            var texture = new Texture2D(size, size);
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
            
            texture.Apply();
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

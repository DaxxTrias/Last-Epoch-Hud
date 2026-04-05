using System.Collections.Generic;
using Il2Cpp;
using Mod.Game;
using UnityEngine;
using UnityEngine.UI;

namespace Mod.Cheats
{
    public static class MinimapEnemyCircles
    {
        // Canvas reference for UI
        private static Canvas? minimapCanvas;
        private static bool isInitialized = false;
        private static bool isMinimapOpen = false;
        private static bool wasTabPressed = false;
        private static float nextInitializeAttemptAt = 0f;
        private static float initializeRetryDelaySeconds = InitialInitializeRetrySeconds;
        private const float InitialInitializeRetrySeconds = 0.25f;
        private const float MaxInitializeRetrySeconds = 3.0f;
        private const int MaxRenderedEnemies = 100;
        private const float RotationEpsilon = 0.0001f;
        private const string HostileAlignmentName = "Evil";
        private const string HostileNeutralAlignmentName = "HostileNeutral";
        
        // Debug info for GUI display
        public static string lastDebugInfo = "";
        public static int lastEnemyCount = 0;
        public static int lastCircleCount = 0;
        public static string lastCreateAttempt = "";
        
        private sealed class CircleVisual
        {
            public CircleVisual(GameObject gameObject, RectTransform rectTransform, Image image)
            {
                GameObject = gameObject;
                RectTransform = rectTransform;
                Image = image;
            }

            public GameObject GameObject { get; }
            public RectTransform RectTransform { get; }
            public Image Image { get; }
        }

        // Store and reuse our UI circles to avoid per-frame Instantiate/Destroy churn
        private static readonly List<CircleVisual> enemyCircles = new List<CircleVisual>(MaxRenderedEnemies);
        private static int activeCircleCount = 0;
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

            var playerActor = ObjectManager.GetLocalPlayer();
            if (playerActor == null)
            {
                ClearCircles();
                if (shouldUpdateDebug) lastDebugInfo = "No local player found";
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
            
            UpdateEnemyCircles(playerActor, shouldUpdateDebug);
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
            if (isInitialized && (iconsContainer != null))
            {
                EnsureSpriteCache();
                return true;
            }
            if (Time.unscaledTime < nextInitializeAttemptAt) return false;
            
            try
            {
                int previousIconsContainerId = iconsContainer != null ? iconsContainer.GetInstanceID() : 0;

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
                    if (previousIconsContainerId != iconsContainer.GetInstanceID())
                    {
                        DestroyAllCircles();
                    }

                    isInitialized = true;
                    initializeRetryDelaySeconds = InitialInitializeRetrySeconds;
                    nextInitializeAttemptAt = 0f;
                    if (updateDebug) lastDebugInfo = $"Bound Icons container: {iconsContainer.name}";
                    return true;
                }
                
                nextInitializeAttemptAt = Time.unscaledTime + initializeRetryDelaySeconds;
                initializeRetryDelaySeconds = Mathf.Min(MaxInitializeRetrySeconds, initializeRetryDelaySeconds * 2f);
                if (updateDebug) lastDebugInfo = "Icons container not found";
                return false;
            }
            catch (System.Exception e)
            {
                nextInitializeAttemptAt = Time.unscaledTime + initializeRetryDelaySeconds;
                initializeRetryDelaySeconds = Mathf.Min(MaxInitializeRetrySeconds, initializeRetryDelaySeconds * 2f);
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
        
        public static void UpdateEnemyCircles(GameObject playerActor, bool updateDebug = true)
        {
            try
            {
                if (ActorManager.instance == null) 
                {
                    if (updateDebug) lastDebugInfo = "ActorManager.instance is null";
                    return;
                }

                if (iconsContainer == null)
                {
                    if (updateDebug) lastDebugInfo = "Icons container not initialized";
                    return;
                }

                CompactCirclePool();

                Transform playerTransform = playerActor.transform;
                Vector3 playerPosition = playerTransform.position;
                float drawDistance = Settings.drawDistance;
                float drawDistanceSqr = drawDistance * drawDistance;
                int circleSize = Mathf.Max(2, Mathf.RoundToInt(Settings.minimapCircleSize));
                Vector2 circleSizeDelta = new Vector2(circleSize, circleSize);

                int totalVisuals = 0;
                int alignmentFiltered = 0;
                int candidateEnemyCount = 0;
                int successfulCircles = 0;

                // Precompute mapping parameters once per update
                float pixelsPerMeter = GetPixelsPerMeter();
                Vector2 maxBounds = GetIconRectHalfSize();
                float mapRotationRad = GetMapRotationRadians();
                float basisRad = GetBasisRotationRadians();

                bool applyBasisRotation = Mathf.Abs(basisRad) > RotationEpsilon;
                bool applyMapRotation = Mathf.Abs(mapRotationRad) > RotationEpsilon;
                float cosBasis = 1f;
                float sinBasis = 0f;
                float cosMap = 1f;
                float sinMap = 0f;

                if (applyBasisRotation)
                {
                    cosBasis = Mathf.Cos(basisRad);
                    sinBasis = Mathf.Sin(basisRad);
                }

                if (applyMapRotation)
                {
                    cosMap = Mathf.Cos(mapRotationRad);
                    sinMap = Mathf.Sin(mapRotationRad);
                }

                bool flipX = Settings.minimapFlipX;
                bool flipY = Settings.minimapFlipY;
                float offsetX = Settings.minimapOffsetX;
                float offsetY = Settings.minimapOffsetY;

                foreach (var visual in ActorManager.instance.visuals)
                {
                    totalVisuals++;
                    
                    // Skip if no alignment or not hostile
                    if (visual.alignment == null) continue;
                    string alignmentName = visual.alignment.name;
                    if (alignmentName != HostileAlignmentName && alignmentName != HostileNeutralAlignmentName) 
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

                            Vector3 actorPosition = actor.transform.position;
                            Vector3 delta = actorPosition - playerPosition;
                            if (delta.sqrMagnitude >= drawDistanceSqr) continue;

                            candidateEnemyCount++;
                            if (successfulCircles >= MaxRenderedEnemies) continue;

                            if (!TryGetEnemySprite(actor, out Sprite? sprite) || sprite == null)
                                continue;

                            Vector2 minimapPos = WorldDeltaToMinimapPosition(
                                delta.x,
                                delta.z,
                                pixelsPerMeter,
                                offsetX,
                                offsetY,
                                flipX,
                                flipY,
                                applyBasisRotation,
                                cosBasis,
                                sinBasis,
                                applyMapRotation,
                                cosMap,
                                sinMap);

                            if (Mathf.Abs(minimapPos.x) > maxBounds.x || Mathf.Abs(minimapPos.y) > maxBounds.y)
                                continue;

                            UpsertMinimapCircle(successfulCircles, sprite, minimapPos, circleSizeDelta);
                            successfulCircles++;
                        }
                    }
                }

                lastEnemyCount = candidateEnemyCount;
                SetActiveCircleCount(successfulCircles);
                lastCircleCount = successfulCircles;
                lastCreateAttempt = $"Updated {successfulCircles} circles for {candidateEnemyCount} enemies";
                
                // Set final debug info with complete status
                if (updateDebug)
                {
                    lastDebugInfo = $"ACTIVE | Visuals: {totalVisuals}, Filtered: {alignmentFiltered}, InRange: {lastEnemyCount}, Rendered: {lastCircleCount}, Pool: {enemyCircles.Count}";
                }
            }
            catch (System.Exception e)
            {
                if (updateDebug) lastDebugInfo = $"Error updating circles: {e.Message}";
            }
        }

        private static bool TryGetEnemySprite(ActorVisuals enemy, out Sprite? sprite)
        {
            var displayInfo = enemy.GetComponent<ActorDisplayInformation>();
            if (displayInfo == null)
            {
                if (!Settings.showWhiteMonsters)
                {
                    sprite = null;
                    return false;
                }

                sprite = spriteWhite;
                return sprite != null;
            }

            switch (displayInfo.actorClass)
            {
                case DisplayActorClass.Boss:
                    if (!Settings.showBossMonsters)
                    {
                        sprite = null;
                        return false;
                    }

                    sprite = spriteRed;
                    return sprite != null;
                case DisplayActorClass.Rare:
                    if (!Settings.showRareMonsters)
                    {
                        sprite = null;
                        return false;
                    }

                    sprite = spriteYellow;
                    return sprite != null;
                case DisplayActorClass.Magic:
                    if (!Settings.showMagicMonsters)
                    {
                        sprite = null;
                        return false;
                    }

                    sprite = spriteBlue;
                    return sprite != null;
                case DisplayActorClass.Normal:
                    if (!Settings.showWhiteMonsters)
                    {
                        sprite = null;
                        return false;
                    }

                    sprite = spriteWhite;
                    return sprite != null;
                default:
                    // Match previous behavior: unknown classes with display info were shown by default.
                    sprite = spriteWhite;
                    return sprite != null;
            }
        }

        private static void UpsertMinimapCircle(int index, Sprite sprite, Vector2 position, Vector2 sizeDelta)
        {
            CircleVisual circle = GetOrCreateCircle(index);
            if (iconsContainer != null && circle.RectTransform.parent != iconsContainer.transform)
            {
                circle.RectTransform.SetParent(iconsContainer.transform, false);
            }

            circle.RectTransform.anchoredPosition = position;
            if (circle.RectTransform.sizeDelta != sizeDelta)
            {
                circle.RectTransform.sizeDelta = sizeDelta;
            }

            if (circle.Image.sprite != sprite)
            {
                circle.Image.sprite = sprite;
            }

            if (!circle.GameObject.activeSelf)
            {
                circle.GameObject.SetActive(true);
            }
        }

        private static CircleVisual GetOrCreateCircle(int index)
        {
            if (index < enemyCircles.Count)
            {
                CircleVisual existing = enemyCircles[index];
                if (existing.GameObject != null && existing.RectTransform != null && existing.Image != null)
                {
                    return existing;
                }

                CircleVisual replacement = CreateCircleVisual();
                enemyCircles[index] = replacement;
                return replacement;
            }

            CircleVisual created = CreateCircleVisual();
            enemyCircles.Add(created);
            return created;
        }

        private static CircleVisual CreateCircleVisual()
        {
            var circleObj = new GameObject("EnemyCircle");
            if (iconsContainer != null)
            {
                circleObj.transform.SetParent(iconsContainer.transform, false);
            }

            var rectTransform = circleObj.AddComponent<RectTransform>();
            var image = circleObj.AddComponent<Image>();
            image.raycastTarget = false;
            circleObj.SetActive(false);
            return new CircleVisual(circleObj, rectTransform, image);
        }

        private static void CompactCirclePool()
        {
            for (int i = enemyCircles.Count - 1; i >= 0; i--)
            {
                CircleVisual circle = enemyCircles[i];
                if (circle.GameObject == null || circle.RectTransform == null || circle.Image == null)
                {
                    enemyCircles.RemoveAt(i);
                }
            }

            if (activeCircleCount > enemyCircles.Count)
            {
                activeCircleCount = enemyCircles.Count;
            }
        }

        private static void SetActiveCircleCount(int count)
        {
            activeCircleCount = Mathf.Clamp(count, 0, enemyCircles.Count);

            for (int i = activeCircleCount; i < enemyCircles.Count; i++)
            {
                GameObject circleObject = enemyCircles[i].GameObject;
                if (circleObject != null && circleObject.activeSelf)
                {
                    circleObject.SetActive(false);
                }
            }
        }

        private static void DestroyAllCircles()
        {
            foreach (CircleVisual circle in enemyCircles)
            {
                if (circle.GameObject != null)
                {
                    UnityEngine.Object.Destroy(circle.GameObject);
                }
            }

            enemyCircles.Clear();
            activeCircleCount = 0;
            lastCircleCount = 0;
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
        
        private static Vector2 WorldDeltaToMinimapPosition(
            float worldDeltaX,
            float worldDeltaZ,
            float pixelsPerMeter,
            float offsetX,
            float offsetY,
            bool flipX,
            bool flipY,
            bool applyBasisRotation,
            float cosBasis,
            float sinBasis,
            bool applyMapRotation,
            float cosMap,
            float sinMap)
        {
            float relX = worldDeltaX;
            float relY = worldDeltaZ;
            
            // Apply basis rotation to align world axes to DMap axes
            if (applyBasisRotation)
            {
                float bx = relX * cosBasis - relY * sinBasis;
                float by = relX * sinBasis + relY * cosBasis;
                relX = bx;
                relY = by;
            }
            
            // Rotate to match map rotation
            if (applyMapRotation)
            {
                float rx = relX * cosMap - relY * sinMap;
                float ry = relX * sinMap + relY * cosMap;
                relX = rx;
                relY = ry;
            }
            
            // Optional axis flips to match DMap handedness
            if (flipX) relX = -relX;
            if (flipY) relY = -relY;
            
            // Convert to minimap space (Unity UI is typically +Y up). Map convention: x->right, z->forward
            float minimapX = relX * pixelsPerMeter + offsetX;
            float minimapY = relY * pixelsPerMeter + offsetY;
            
            return new Vector2(minimapX, minimapY);
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
            SetActiveCircleCount(0);
            lastCircleCount = 0;
        }
    }
}

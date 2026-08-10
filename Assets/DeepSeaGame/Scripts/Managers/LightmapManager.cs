using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DeepSeaGame
{
    public class LightmapManager : MonoBehaviour
    {
        public static LightmapManager Instance { get; private set; }
        
        [SerializeField] private RawImage _lightmapOverlay;
        
        [Header("Light Attenuation Settings")]
        [Tooltip("The number of tiles light can traverse from its source before attenuation/dimming begins.")]
        [SerializeField] private int _tileAmountBeforeAttenuationBegins = 2;
        [SerializeField] private float _fullBrightnessInterpretation = 15f;
        [SerializeField] private float _solidForegroundAttenuation = 1.0f;
        [SerializeField] private float _backgroundOnlyAttenuation = 0.5f;

        [Header("Flashlight Settings")]
        [Tooltip("Controls how sharply the flashlight cone fades toward its edges. 1 = linear, 2+ = brighter core with sharper falloff.")]
        [SerializeField] private float _coneEdgeFalloffPower = 2f;

        [Header("Texture Settings")]
        [Tooltip("How many light pixels per game tile. 1 = 1x1, 2 = 2x2, 4 = 4x4. Higher means less pixelated but more cost.")]
        [SerializeField, Min(1)] private int _lightTilesPerGameTile = 1;

        [Tooltip("The filter mode for the lightmap overlay texture (Point for pixelated tiles, Bilinear for smooth).")]
        [SerializeField] private FilterMode _lightmapFilterMode = FilterMode.Point;

        [Tooltip("Applies a CPU box blur to the lightmap to smooth out pixelation.")]
        [SerializeField] private bool _enableBlur = true;

        [Tooltip("Number of times to run the box blur. More passes = smoother light but higher CPU cost.")]
        [SerializeField, Min(1)] private int _blurPasses = 1;

        [Header("Padding Settings"), Tooltip("Extra padding (in tiles) around the camera frustum for light calculations. Prevents lighting pop-in on screen edges.")]
        [SerializeField] private int _extraLightmapPadding = 8;
        [SerializeField] private Shader _multiplyShader;

        // Cached runtime variables to completely eliminate GC garbage collection overhead
        private WorldDataStore _worldDataStore;
        private RectInt _currentVisibleTileBounds;
        private RectInt _currentInflatedBounds;
        private float[,] _lightGrid;
        private float[,] _blurGrid;
        private int[,] _distGrid;
        private int _gridWidth;
        private int _gridHeight;
        
        private Texture2D _lightmapTexture;
        private Color32[] _colorBuffer;
        private readonly Queue<Vector2Int> _bfsQueue = new();

        private void Awake()
        {
            Instance = this;
        }
        
        private void Start() 
        {
            WorldManager.Instance.OnWorldReady += SubscribeToTileChanges;
        }
        
        private void OnDestroy() 
        {
            PlayerCamera.OnVisibleTileBoundsChanged -= UpdateLightmap;
            WorldManager.Instance.OnWorldReady -= SubscribeToTileChanges;
            
            if(Player.Instance != null)
            {
                Player.Instance.FlashlightController.OnFlashlightStateChanged -= OnFlashlightStateChanged;
            }

            if (_worldDataStore != null)
            {
                _worldDataStore.TileChanged -= HandleTileChanged;
            }

            // Clean up resources to prevent memory leaks
            if (_lightmapTexture != null)
            {
                Destroy(_lightmapTexture);
            }
        }

        private void SubscribeToTileChanges()
        {
            PlayerCamera.OnVisibleTileBoundsChanged += UpdateLightmap;
            Player.Instance.FlashlightController.OnFlashlightStateChanged += OnFlashlightStateChanged;

            if (_worldDataStore == null && WorldManager.Instance != null)
            {
                _worldDataStore = WorldManager.Instance.WorldDataStore;
            }

            if (_worldDataStore != null)
            {
                _worldDataStore.TileChanged -= HandleTileChanged;
                _worldDataStore.TileChanged += HandleTileChanged;
            }
        }

        private void OnFlashlightStateChanged()
        {
            UpdateLightmap(_currentVisibleTileBounds);
        }

        private void HandleTileChanged(Vector2Int tilePosition, ushort previousTileId, ushort newTileId, WorldTm targetMap)
        {
            // Only trigger recalculation if the modified tile falls inside the active inflated calculations boundary
            if (_gridWidth > 0 && _gridHeight > 0 && _currentInflatedBounds.Contains(tilePosition))
            {
                UpdateLightmap(_currentVisibleTileBounds);
            }
        }

        private void UpdateLightmap(RectInt currentVisibleTileBounds)
        {
            if (WorldManager.Instance == null || !WorldManager.Instance.IsWorldReady) return;

            if (_worldDataStore == null)
            {
                _worldDataStore = WorldManager.Instance.WorldDataStore;
                if (_worldDataStore == null) return;
            }

            _currentVisibleTileBounds = currentVisibleTileBounds;

            if (!TryInflateBounds(currentVisibleTileBounds, out RectInt inflatedBounds)) return;

            PrepareLightmap(inflatedBounds.width * _lightTilesPerGameTile, inflatedBounds.height * _lightTilesPerGameTile);
            SeedLightSources(inflatedBounds);
            RunLightSourceBFSPropagation(inflatedBounds);
            RunFlashlightBFSPropagation(inflatedBounds);
            ApplyLightmapToOverlay(inflatedBounds);
        }

        private bool TryInflateBounds(RectInt visibleBounds, out RectInt inflatedBounds)
        {
            int minX = visibleBounds.xMin - _extraLightmapPadding;
            int minY = visibleBounds.yMin - _extraLightmapPadding;
            int maxX = visibleBounds.xMax + _extraLightmapPadding;
            int maxY = visibleBounds.yMax + _extraLightmapPadding;

            inflatedBounds = new RectInt(minX, minY, maxX - minX, maxY - minY);
            _currentInflatedBounds = inflatedBounds;

            return inflatedBounds.width > 0 && inflatedBounds.height > 0;
        }

        private void PrepareLightmap(int width, int height)
        {
            if (_lightGrid == null || _gridWidth != width || _gridHeight != height)
            {
                _lightGrid = new float[width, height];
                _blurGrid = new float[width, height];
                _distGrid = new int[width, height];
                _gridWidth = width;
                _gridHeight = height;

                if (_lightmapTexture != null) Destroy(_lightmapTexture);

                _lightmapTexture = new Texture2D(width, height, TextureFormat.RGB24, false)
                {
                    filterMode = _lightmapFilterMode,
                    wrapMode = TextureWrapMode.Clamp
                };

                _colorBuffer = new Color32[width * height];
            }
            else
            {
                // Reuse existing buffers — just zero out the light grid
                Array.Clear(_lightGrid, 0, _lightGrid.Length);
            }

            // Initialize all distances to int.MaxValue
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    _distGrid[x, y] = int.MaxValue;
                }
            }

            _bfsQueue.Clear();
        }

        private void SeedLightSources(RectInt inflatedBounds)
        {
            int width  = inflatedBounds.width * _lightTilesPerGameTile;
            int height = inflatedBounds.height * _lightTilesPerGameTile;

            for (int localX = 0; localX < width; localX++)
            {
                for (int localY = 0; localY < height; localY++)
                {
                    int worldX = inflatedBounds.x + localX / _lightTilesPerGameTile;
                    int worldY = inflatedBounds.y + localY / _lightTilesPerGameTile;

                    // Skip tiles outside world bounds — they must not act as light sources
                    if (!_worldDataStore.IsInBounds(worldX, worldY)) continue;

                    ushort fgId = _worldDataStore.GetTileId(worldX, worldY, WorldTm.ForegroundTilemap);
                    ushort bgId = _worldDataStore.GetTileId(worldX, worldY, WorldTm.BackgroundTilemap);
                    TileSO fgTile = GameDataRegistry.Instance.GetTileSOFromTileId(fgId);

                    bool isOpenSky = fgId == GameDataRegistry.INVALID_ID && bgId == GameDataRegistry.INVALID_ID && worldY > WorldManager.Instance.WorldGenerator.WorldGenerationData.UndergroundMaxYLevel;
                                    
                    if (isOpenSky)
                    {
                        _lightGrid[localX, localY] = _fullBrightnessInterpretation;
                        _distGrid[localX, localY] = 0;
                        _bfsQueue.Enqueue(new Vector2Int(localX, localY));
                        continue;
                    }
                    else if(fgId == GameDataRegistry.INVALID_ID)
                    {
                        continue;
                    }

                    if (fgTile.LightValue > 0)
                    {
                        _lightGrid[localX, localY] = fgTile.LightValue;
                        _distGrid[localX, localY] = 0;
                        _bfsQueue.Enqueue(new Vector2Int(localX, localY));
                    }
                }
            }
        }

        private void RunLightSourceBFSPropagation(RectInt inflatedBounds)
        {
            int width  = inflatedBounds.width * _lightTilesPerGameTile;
            int height = inflatedBounds.height * _lightTilesPerGameTile;

            // Static cardinal directions: Up, Down, Left, Right
            Vector2Int[] directions = { new(0, 1), new(0, -1), new(-1, 0), new(1, 0) };

            while (_bfsQueue.Count > 0)
            {
                Vector2Int curr = _bfsQueue.Dequeue();
                float currLight = _lightGrid[curr.x, curr.y];
                int currDist = _distGrid[curr.x, curr.y];

                foreach (Vector2Int dir in directions)
                {
                    int nextX = curr.x + dir.x;
                    int nextY = curr.y + dir.y;

                    if (nextX < 0 || nextX >= width || nextY < 0 || nextY >= height) continue;

                    int nextDist = currDist + 1;

                    int worldX = inflatedBounds.x + nextX / _lightTilesPerGameTile;
                    int worldY = inflatedBounds.y + nextY / _lightTilesPerGameTile;

                    // Stop BFS propagation at the world boundary — don't let light leak through out-of-bounds space
                    if (!_worldDataStore.IsInBounds(worldX, worldY)) continue;

                    ushort fgId = _worldDataStore.GetTileId(worldX, worldY, WorldTm.ForegroundTilemap);
                    ushort bgId = _worldDataStore.GetTileId(worldX, worldY, WorldTm.BackgroundTilemap);

                    float attenuation = 0f;
                    if (nextDist > _tileAmountBeforeAttenuationBegins * _lightTilesPerGameTile)
                    {
                        attenuation = GetTileAttenuation(fgId, bgId) / _lightTilesPerGameTile;
                    }

                    float newLight = currLight - attenuation;

                    if (newLight > 0f)
                    {
                        bool shouldUpdate = false;
                        if (newLight > _lightGrid[nextX, nextY])
                        {
                            shouldUpdate = true;
                        }
                        else if (newLight == _lightGrid[nextX, nextY] && nextDist < _distGrid[nextX, nextY])
                        {
                            shouldUpdate = true;
                        }

                        if (shouldUpdate)
                        {
                            _lightGrid[nextX, nextY] = newLight;
                            _distGrid[nextX, nextY] = nextDist;
                            _bfsQueue.Enqueue(new Vector2Int(nextX, nextY));
                        }
                    }
                }
            }
        }
        
        private void RunFlashlightBFSPropagation(RectInt inflatedBounds)
        {
            // Early out: no player or flashlight is off
            if (Player.Instance == null || Player.Instance.FlashlightController == null || !Player.Instance.FlashlightController.IsFlashlightOn)
                return;

            FlashlightController fc = Player.Instance.FlashlightController;

            // Convert player world position to local grid coords within the inflated bounds
            Vector2Int playerTile = fc.PlayerCenterTilePosition;
            int originLocalX = (playerTile.x - inflatedBounds.x) * _lightTilesPerGameTile;
            int originLocalY = (playerTile.y - inflatedBounds.y) * _lightTilesPerGameTile;

            int width = inflatedBounds.width * _lightTilesPerGameTile;
            int height = inflatedBounds.height * _lightTilesPerGameTile;

            // If the player is outside the inflated bounds, nothing to do
            if (originLocalX < 0 || originLocalX >= width || originLocalY < 0 || originLocalY >= height)
                return;

            int maxRange = fc.FlashlightRange;
            float flashlightIntensity = fc.FlashlightIntensity;
            Vector2 coneDir = fc.ConeDirection;
            Vector2 playerWorldPos = fc.CenterOfPlayerPosition;

            // Pre-compute world position of player tile center for ray origin
            Vector2 rayOrigin = new Vector2(playerWorldPos.x, playerWorldPos.y);

            // Iterate over every tile in the inflated bounds
            for (int localX = 0; localX < width; localX++)
            {
                for (int localY = 0; localY < height; localY++)
                {
                    // Sub-tile center in world space
                    float worldX = inflatedBounds.x + (localX + 0.5f) / _lightTilesPerGameTile;
                    float worldY = inflatedBounds.y + (localY + 0.5f) / _lightTilesPerGameTile;

                    Vector2 tileCenter = new Vector2(worldX, worldY);
                    float distToTile = Vector2.Distance(rayOrigin, tileCenter);

                    // Skip if beyond max range
                    if (distToTile > maxRange) continue;

                    Vector2 toTile = tileCenter - rayOrigin;

                    // Angle from cone center axis to this tile
                    float angleToTile = Vector2.Angle(coneDir, toTile);

                    // Skip tiles outside the cone
                    if (angleToTile > fc.ConeHalfAngle) continue;

                    // March a ray from the player to this tile's center, stepping tile-by-tile
                    float totalAttenuation = MarchRay(rayOrigin, tileCenter, inflatedBounds, out int tileCount);

                    if (totalAttenuation < 0f)
                    {
                        // Ray went out of bounds, skip this tile
                        continue;
                    }

                    // The first tile (player tile) gets no attenuation.
                    // Attenuation begins after _tileAmountBeforeAttenuationBegins tiles.
                    // MarchRay already computed the per-tile attenuation total.
                    float lightValue = flashlightIntensity - totalAttenuation;

                    if (lightValue > 0f)
                    {
                        // Apply angle-based edge falloff (1.0 at center, 0.0 at edge)
                        float angleFraction = Mathf.Clamp01(1f - (angleToTile / fc.ConeHalfAngle));
                        float edgeFalloff = Mathf.Pow(angleFraction, _coneEdgeFalloffPower);

                        float finalFlashlightValue = lightValue * edgeFalloff;

                        // Blend with ambient: take the brighter of the two
                        _lightGrid[localX, localY] = Mathf.Max(_lightGrid[localX, localY], finalFlashlightValue);
                    }
                }
            }
        }
        
        private float MarchRay(Vector2 origin, Vector2 target, RectInt inflatedBounds, out int tileCount)
        {
            tileCount = 0;
            float totalAttenuation = 0f;

            Vector2 dir = target - origin;
            float totalDist = dir.magnitude;
            if (totalDist < 0.001f) return 0f;

            dir /= totalDist;

            // DDA-style tile marching: step through each tile boundary
            // Start at the origin tile (player tile)
            int currentTileX = Mathf.FloorToInt(origin.x);
            int currentTileY = Mathf.FloorToInt(origin.y);

            // Determine step direction
            int stepX = dir.x >= 0 ? 1 : -1;
            int stepY = dir.y >= 0 ? 1 : -1;

            // tMax: distance to next tile boundary along each axis
            // tDelta: distance to traverse one full tile along each axis
            float tMaxX, tMaxY, tDeltaX, tDeltaY;

            // Calculate fractional position within the current tile
            float fracX = dir.x >= 0 ? (currentTileX + 1 - origin.x) : (origin.x - currentTileX);
            float fracY = dir.y >= 0 ? (currentTileY + 1 - origin.y) : (origin.y - currentTileY);

            // If direction component is near zero, set a very large tMax so it never triggers
            if (Mathf.Abs(dir.x) > 0.0001f)
            {
                tDeltaX = 1f / Mathf.Abs(dir.x);
                tMaxX = fracX * tDeltaX;
            }
            else
            {
                tDeltaX = float.MaxValue;
                tMaxX = float.MaxValue;
            }

            if (Mathf.Abs(dir.y) > 0.0001f)
            {
                tDeltaY = 1f / Mathf.Abs(dir.y);
                tMaxY = fracY * tDeltaY;
            }
            else
            {
                tDeltaY = float.MaxValue;
                tMaxY = float.MaxValue;
            }

            // Track distance along the ray for the initial no-attenuation zone
            float distTraveled = 0f;

            // March until we reach or pass the target
            while (true)
            {
                // Check if we've reached the target tile
                if (currentTileX == Mathf.FloorToInt(target.x) && currentTileY == Mathf.FloorToInt(target.y))
                {
                    // We've arrived at the destination tile. Break after processing.
                    break;
                }

                // Advance to the next tile boundary
                if (tMaxX < tMaxY)
                {
                    distTraveled = tMaxX;
                    currentTileX += stepX;
                    tMaxX += tDeltaX;
                }
                else
                {
                    distTraveled = tMaxY;
                    currentTileY += stepY;
                    tMaxY += tDeltaY;
                }

                // If we've gone past the target along the ray, we're done
                if (distTraveled >= totalDist) break;

                // Increment tile count (we've stepped into a new tile)
                tileCount++;

                // Convert current tile to world coords to look up tile data
                int worldX = currentTileX;
                int worldY = currentTileY;

                // Check bounds
                if (worldX < inflatedBounds.x || worldX >= inflatedBounds.xMax ||
                    worldY < inflatedBounds.y || worldY >= inflatedBounds.yMax)
                {
                    // Ray left the calculated bounds
                    tileCount = 0;
                    return -1f;
                }

                // Check if we're past the initial no-attenuation zone
                // tileCount starts at 0 for the first tile after origin, so
                // attenuation starts when tileCount > _tileAmountBeforeAttenuationBegins
                // (matching the original BFS condition: nextDist > _tileAmountBeforeAttenuationBegins)
                if (tileCount > _tileAmountBeforeAttenuationBegins)
                {
                    ushort fgId = _worldDataStore.GetTileId(worldX, worldY, WorldTm.ForegroundTilemap);
                    ushort bgId = _worldDataStore.GetTileId(worldX, worldY, WorldTm.BackgroundTilemap);

                    float attenuation = GetTileAttenuation(fgId, bgId);
                    totalAttenuation += attenuation;
                }
            }

            return totalAttenuation;
        }

        private float GetTileAttenuation(ushort fgId, ushort bgId)
        {
            if (fgId != GameDataRegistry.INVALID_ID) return _solidForegroundAttenuation;
            if (bgId != GameDataRegistry.INVALID_ID) return _backgroundOnlyAttenuation;
            return _backgroundOnlyAttenuation;
        }

        private void ApplyLightmapToOverlay(RectInt inflatedBounds)
        {
            if (_enableBlur)
            {
                BlurLightGrid();
            }

            int width = inflatedBounds.width * _lightTilesPerGameTile;
            int height = inflatedBounds.height * _lightTilesPerGameTile;

            // Map each light value [0, 15] to a grayscale byte [0, 255]
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float normalized = Mathf.Clamp01(_lightGrid[x, y] / _fullBrightnessInterpretation);
                    byte grayscale = (byte)Mathf.RoundToInt(normalized * 255f);
                    _colorBuffer[y * width + x] = new Color32(grayscale, grayscale, grayscale, 255);
                }
            }

            // Sync filter mode if tweaked in the Inspector at runtime
            if (_lightmapTexture.filterMode != _lightmapFilterMode)
                _lightmapTexture.filterMode = _lightmapFilterMode;

            _lightmapTexture.SetPixels32(_colorBuffer);
            _lightmapTexture.Apply();

            _lightmapOverlay.texture = _lightmapTexture;

            // Assign multiply material or fall back to raw grayscale
            _lightmapOverlay.material = new Material(_multiplyShader);

            UpdateOverlayRectTf(inflatedBounds);
        }

        private void BlurLightGrid()
        {
            for (int pass = 0; pass < _blurPasses; pass++)
            {
                // Horizontal pass: _lightGrid -> _blurGrid
                for (int y = 0; y < _gridHeight; y++)
                {
                    for (int x = 0; x < _gridWidth; x++)
                    {
                        float sum = _lightGrid[x, y];
                        int count = 1;
                        if (x > 0) { sum += _lightGrid[x - 1, y]; count++; }
                        if (x < _gridWidth - 1) { sum += _lightGrid[x + 1, y]; count++; }
                        _blurGrid[x, y] = sum / count;
                    }
                }

                // Vertical pass: _blurGrid -> _lightGrid
                for (int x = 0; x < _gridWidth; x++)
                {
                    for (int y = 0; y < _gridHeight; y++)
                    {
                        float sum = _blurGrid[x, y];
                        int count = 1;
                        if (y > 0) { sum += _blurGrid[x, y - 1]; count++; }
                        if (y < _gridHeight - 1) { sum += _blurGrid[x, y + 1]; count++; }
                        _lightGrid[x, y] = sum / count;
                    }
                }
            }
        }

        private void UpdateOverlayRectTf(RectInt bounds)
        {
            Vector2 center = new Vector2(bounds.xMin + bounds.xMax, bounds.yMin + bounds.yMax) * 0.5f;
            Vector2 size   = new Vector2(bounds.width, bounds.height);

            _lightmapOverlay.rectTransform.position   = center;
            _lightmapOverlay.rectTransform.sizeDelta  = size;
            _lightmapOverlay.rectTransform.localScale = Vector3.one;
        }
    }
}

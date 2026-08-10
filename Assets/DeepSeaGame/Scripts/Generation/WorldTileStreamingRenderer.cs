using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace DeepSeaGame
{
    public class WorldTileStreamingRenderer : MonoBehaviour
    {
        private WorldDataStore _worldDataStore;
        private Tilemap _foregroundTilemap;
        private Tilemap _backgroundTilemap;
        private Tilemap _airTilemap;
        private TilemapRenderer _airTilemapRenderer;
        private Transform _multiTileRenderingTf;
        private RectInt _renderedBounds;
        private bool _isInitialized;
        private bool _hasRenderedBounds;
        private Dictionary<Vector2Int, GameObject> _spawnedMultiTileObjects = new();

        [Header("Air Mask Rendering")]
        [SerializeField] private Material _airMaskStencilMaterial;
        [SerializeField] private Shader _airShader;
        [SerializeField] private int _airMaskSortingOrder = 98;

        private void Start() 
        {
            PlayerCamera.OnVisibleTileBoundsChanged += HandleVisibleTileBoundsChanged;
        }

        private void OnDestroy()
        {
            PlayerCamera.OnVisibleTileBoundsChanged -= HandleVisibleTileBoundsChanged;

            if (_worldDataStore != null)
            {
                _worldDataStore.TileChanged -= HandleTileChanged;
                _worldDataStore.MultiTileChanged -= HandleMultiTileChanged;
            }
        }

        public void Initialize(WorldDataStore worldDataStore, Tilemap foregroundTilemap, Tilemap backgroundTilemap, Tilemap airTilemap, Transform multiTileRenderingTf)
        {
            if (_worldDataStore != null)
            {
                _worldDataStore.TileChanged -= HandleTileChanged;
                _worldDataStore.MultiTileChanged -= HandleMultiTileChanged;
            }

            _worldDataStore = worldDataStore;
            _foregroundTilemap = foregroundTilemap;
            _backgroundTilemap = backgroundTilemap;
            _airTilemap = airTilemap;
            _airTilemapRenderer = _airTilemap != null ? _airTilemap.GetComponent<TilemapRenderer>() : null;
            _multiTileRenderingTf = multiTileRenderingTf;

            _isInitialized = _worldDataStore != null && _foregroundTilemap != null && _backgroundTilemap != null && _airTilemap != null && _airTilemapRenderer != null;
            _hasRenderedBounds = false;
            _renderedBounds = default;

            if (!_isInitialized)
            {
                Debug.LogWarning("WorldTileStreamingRenderer could not initialize because required references are missing.");
                return;
            }

            ConfigureAirMaskRenderer();

            _worldDataStore.TileChanged += HandleTileChanged;
            _worldDataStore.MultiTileChanged += HandleMultiTileChanged;
            
            _foregroundTilemap.ClearAllTiles();
            _backgroundTilemap.ClearAllTiles();
            _airTilemap.ClearAllTiles();
            
            // Clean up any existing multi-tile GameObjects
            foreach (var go in _spawnedMultiTileObjects.Values)
            {
                if (go != null) Destroy(go);
            }
            _spawnedMultiTileObjects.Clear();

            if (PlayerCamera.Instance != null)
            {
                HandleVisibleTileBoundsChanged(PlayerCamera.Instance.CurrentVisibleTileBounds);
            }
        }

        private void ConfigureAirMaskRenderer()
        {
            if (_airMaskStencilMaterial == null)
            {
                if (_airShader == null)
                {
                    Debug.LogWarning("Could not find UntitledDeepSeaGame/AirMaskStencilWrite. AirTilemap will render visibly instead of writing the ocean cutout stencil.");
                    return;
                }

                _airMaskStencilMaterial = new Material(_airShader)
                {
                    name = "Runtime Air Mask Stencil Write",
                    hideFlags = HideFlags.DontSave
                };
            }

            _airTilemapRenderer.sharedMaterial = _airMaskStencilMaterial;
            _airTilemapRenderer.sortingOrder = _airMaskSortingOrder;
        }

        private void HandleMultiTileChanged(Vector2Int anchorPosition, TileSO multiTile, bool isPlacingMultiTile, bool flipX)
        {
            if (isPlacingMultiTile)
            {
                // Only spawn if it's within the currently rendered view
                if (_renderedBounds.Contains(anchorPosition))
                {
                    SpawnMultiTile(anchorPosition, multiTile, flipX);
                }
            }
            else
            {
                DespawnMultiTile(anchorPosition);
            }
        }

        private void SpawnMultiTile(Vector2Int anchorPosition, TileSO multiTile, bool flipX = false)
        {
            if (_spawnedMultiTileObjects.ContainsKey(anchorPosition) || multiTile.Prefab == null)
            {
                return;
            }

            GameObject go = Instantiate(multiTile.Prefab, new Vector3(anchorPosition.x, anchorPosition.y, 0), Quaternion.identity, _multiTileRenderingTf);
            
            if (flipX)
            {
                if(go.TryGetComponent(out IInteractable interactable))
                {
                    interactable.OnFlipX();
                }
            }
            
            _spawnedMultiTileObjects.Add(anchorPosition, go);
        }

        private void DespawnMultiTile(Vector2Int anchorPosition)
        {
            if (_spawnedMultiTileObjects.TryGetValue(anchorPosition, out GameObject go))
            {
                Destroy(go);
                _spawnedMultiTileObjects.Remove(anchorPosition);
            }
        }

        private void HandleTileChanged(Vector2Int tilePosition, ushort previousTileId, ushort newTileId, WorldTm targetMap)
        {
            if (!_isInitialized || !_hasRenderedBounds || !_renderedBounds.Contains(tilePosition))
            {
                return;
            }

            ApplyTile(tilePosition.x, tilePosition.y, targetMap);
        }

        private void HandleVisibleTileBoundsChanged(RectInt visibleBounds)
        {
            if (!_isInitialized)
            {
                return;
            }

            RectInt clampedBounds = ClampToWorldBounds(visibleBounds);

            if (!_hasRenderedBounds)
            {
                RenderRect(clampedBounds);
                _renderedBounds = clampedBounds;
                _hasRenderedBounds = true;
                return;
            }

            if (_renderedBounds == clampedBounds)
            {
                return;
            }

            if (!_renderedBounds.Overlaps(clampedBounds))
            {
                ClearRect(_renderedBounds);
                RenderRect(clampedBounds);
                _renderedBounds = clampedBounds;
                return;
            }

            UpdateRenderedBounds(_renderedBounds, clampedBounds);
            _renderedBounds = clampedBounds;
        }

        private void UpdateRenderedBounds(RectInt previousBounds, RectInt currentBounds)
        {
            ClearColumnsOutside(previousBounds, currentBounds);
            ClearRowsOutside(previousBounds, currentBounds);
            RenderColumnsOutside(previousBounds, currentBounds);
            RenderRowsOutside(previousBounds, currentBounds);
        }

        private void ApplyTile(int x, int y, WorldTm targetMap)
        {
            if (targetMap == WorldTm.AirTilemap)
            {
                bool hasUnderwaterAir = _worldDataStore.IsUnderwaterAirAt(x, y);
                _airTilemap.SetTile(new Vector3Int(x, y, 0), hasUnderwaterAir ? GameDataRegistry.Instance.AirTile : null);

                return;
            }

            ushort tileId = _worldDataStore.GetTileId(x, y, targetMap);
            TileBase tile = tileId == GameDataRegistry.INVALID_ID ? null : GameDataRegistry.Instance.GetTileSOFromTileId(tileId);

            if (targetMap == WorldTm.ForegroundTilemap)
            {
                _foregroundTilemap.SetTile(new Vector3Int(x, y, 0), tile);
            }
            else
            {
                _backgroundTilemap.SetTile(new Vector3Int(x, y, 0), tile);
            }
        }

        private void ClearColumnsOutside(RectInt previousBounds, RectInt currentBounds)
        {
            if (currentBounds.xMin > previousBounds.xMin)
            {
                ClearRect(CreateRectFromMinMax(previousBounds.xMin, previousBounds.yMin, currentBounds.xMin, previousBounds.yMax));
            }

            if (currentBounds.xMax < previousBounds.xMax)
            {
                ClearRect(CreateRectFromMinMax(currentBounds.xMax, previousBounds.yMin, previousBounds.xMax, previousBounds.yMax));
            }
        }

        private void ClearRowsOutside(RectInt previousBounds, RectInt currentBounds)
        {
            int overlapMinX = Mathf.Max(previousBounds.xMin, currentBounds.xMin);
            int overlapMaxX = Mathf.Min(previousBounds.xMax, currentBounds.xMax);

            if (overlapMaxX <= overlapMinX)
            {
                return;
            }

            if (currentBounds.yMin > previousBounds.yMin)
            {
                ClearRect(CreateRectFromMinMax(overlapMinX, previousBounds.yMin, overlapMaxX, currentBounds.yMin));
            }

            if (currentBounds.yMax < previousBounds.yMax)
            {
                ClearRect(CreateRectFromMinMax(overlapMinX, currentBounds.yMax, overlapMaxX, previousBounds.yMax));
            }
        }

        private void RenderColumnsOutside(RectInt previousBounds, RectInt currentBounds)
        {
            if (currentBounds.xMin < previousBounds.xMin)
            {
                RenderRect(CreateRectFromMinMax(currentBounds.xMin, currentBounds.yMin, previousBounds.xMin, currentBounds.yMax));
            }

            if (currentBounds.xMax > previousBounds.xMax)
            {
                RenderRect(CreateRectFromMinMax(previousBounds.xMax, currentBounds.yMin, currentBounds.xMax, currentBounds.yMax));
            }
        }

        private void RenderRowsOutside(RectInt previousBounds, RectInt currentBounds)
        {
            int overlapMinX = Mathf.Max(previousBounds.xMin, currentBounds.xMin);
            int overlapMaxX = Mathf.Min(previousBounds.xMax, currentBounds.xMax);

            if (overlapMaxX <= overlapMinX)
            {
                return;
            }

            if (currentBounds.yMin < previousBounds.yMin)
            {
                RenderRect(CreateRectFromMinMax(overlapMinX, currentBounds.yMin, overlapMaxX, previousBounds.yMin));
            }

            if (currentBounds.yMax > previousBounds.yMax)
            {
                RenderRect(CreateRectFromMinMax(overlapMinX, previousBounds.yMax, overlapMaxX, currentBounds.yMax));
            }
        }

        private void RenderRect(RectInt bounds)
        {
            if (bounds.width <= 0 || bounds.height <= 0)
            {
                return;
            }

            for (int x = bounds.xMin; x < bounds.xMax; x++)
            {
                for (int y = bounds.yMin; y < bounds.yMax; y++)
                {
                    ApplyTile(x, y, WorldTm.ForegroundTilemap);
                    ApplyTile(x, y, WorldTm.BackgroundTilemap);
                    ApplyTile(x, y, WorldTm.AirTilemap);
                }
            }

            // Render Multi-Tiles found within these bounds
            foreach (var kvp in _worldDataStore.ActiveMultiTiles)
            {
                Vector2Int anchor = kvp.Key;
                if (bounds.Contains(anchor))
                {
                    bool flipX = kvp.Value.FlipX;
                    SpawnMultiTile(anchor, kvp.Value.TileSO, flipX);
                }
            }
        }

        private void ClearRect(RectInt bounds)
        {
            if (!_isInitialized || bounds.width <= 0 || bounds.height <= 0)
            {
                return;
            }

            for (int x = bounds.xMin; x < bounds.xMax; x++)
            {
                for (int y = bounds.yMin; y < bounds.yMax; y++)
                {
                    _foregroundTilemap.SetTile(new Vector3Int(x, y, 0), null);
                    _backgroundTilemap.SetTile(new Vector3Int(x, y, 0), null);
                    _airTilemap.SetTile(new Vector3Int(x, y, 0), null);
                }
            }

            // Clean up Multi-Tiles that are leaving the rendered bounds
            List<Vector2Int> toRemove = new();
            foreach (var anchor in _spawnedMultiTileObjects.Keys)
            {
                if (bounds.Contains(anchor))
                {
                    toRemove.Add(anchor);
                }
            }

            foreach (var anchor in toRemove)
            {
                DespawnMultiTile(anchor);
            }
        }

        private RectInt CreateRectFromMinMax(int minX, int minY, int maxX, int maxY)
        {
            return new RectInt(minX, minY, Mathf.Max(0, maxX - minX), Mathf.Max(0, maxY - minY));
        }

        private RectInt ClampToWorldBounds(RectInt bounds)
        {
            if (_worldDataStore.Width == 0 || _worldDataStore.Height == 0)
            {
                return new RectInt(0, 0, 0, 0);
            }

            int minX = Mathf.Clamp(bounds.xMin, 0, _worldDataStore.Width);
            int minY = Mathf.Clamp(bounds.yMin, 0, _worldDataStore.Height);
            int maxX = Mathf.Clamp(bounds.xMax, 0, _worldDataStore.Width);
            int maxY = Mathf.Clamp(bounds.yMax, 0, _worldDataStore.Height);

            if (maxX < minX)
            {
                maxX = minX;
            }

            if (maxY < minY)
            {
                maxY = minY;
            }

            return CreateRectFromMinMax(minX, minY, maxX, maxY);
        }
    }
}

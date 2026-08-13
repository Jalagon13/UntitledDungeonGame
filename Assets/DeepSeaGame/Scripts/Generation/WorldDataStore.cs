using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeepSeaGame
{
    public struct MultiTileData
    {
        public TileSO TileSO;
        public bool FlipX;

        public MultiTileData(TileSO tileSO, bool flipX)
        {
            TileSO = tileSO;
            FlipX = flipX;
        }
    }

    // The source of truth for the entire world
    public class WorldDataStore
    {
        public event Action<Vector2Int, ushort, ushort, WorldTm> TileChanged;
        public event Action<Vector2Int, TileSO, bool, bool> MultiTileChanged;

        public ushort[,] FgTileData { get; private set; }
        public ushort[,] BgTileData { get; private set; }
        
        private readonly HashSet<int> _underwaterAirTiles = new();
        public Dictionary<Vector2Int, MultiTileData> ActiveMultiTiles { get; private set; }

        public int Width => FgTileData.GetLength(0);
        public int Height => FgTileData.GetLength(1);
        
        
        private readonly WorldGenerationData _data;
        
        public WorldDataStore(WorldGenerationData data)
        {
            _data = data;

            FgTileData = new ushort[_data.WorldWidth, _data.WorldHeight];
            BgTileData = new ushort[_data.WorldWidth, _data.WorldHeight];

            _underwaterAirTiles.Clear();
            ActiveMultiTiles = new();

            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    FgTileData[x, y] = GameDataRegistry.INVALID_ID;
                    BgTileData[x, y] = GameDataRegistry.INVALID_ID;
                }
            }
        }

        public BiomeType GetBiomeAt(int x, int y)
        {
            if(y > _data.UndergroundMaxYLevel)
            {       
                return BiomeType.Surface;
            }
            
            if(y <= _data.UndergroundMaxYLevel)
            {
                return BiomeType.Underground;
            }
        
            return BiomeType.None;
        }

        public void SetForegroundTileId(int x, int y, ushort tileId)
        {
            if (!IsInBounds(x, y))
            {
                Debug.LogWarning($"Failed to set tile at ({x}, {y}) on {WorldTm.ForegroundTilemap} because it is out of bounds.");
                return;
            }

            // If trying to set it to the same tile, do nothing
            ushort previousTileId = FgTileData[x, y];
            if (previousTileId == tileId)
            {
                return;
            }

            FgTileData[x, y] = tileId;

            TileChanged?.Invoke(new Vector2Int(x, y), previousTileId, tileId, WorldTm.ForegroundTilemap);

            if (tileId != GameDataRegistry.INVALID_ID)
            {
                // Clear underwater air
                bool removed = _underwaterAirTiles.Remove(GetTileIndex(x, y));
                if (removed)
                {
                    TileChanged?.Invoke(new Vector2Int(x, y), GameDataRegistry.INVALID_ID, GameDataRegistry.INVALID_ID, WorldTm.WaterTilemap);
                }
            }
            else
            {
                TileSO tileJustDestroyed = GameDataRegistry.Instance.GetTileSOFromTileId(previousTileId);
                if(tileJustDestroyed.BreakMode == TileBreakMode.FromHitTileUp)
                {
                    TileSO tileAbove = GameDataRegistry.Instance.GetTileSOFromTileId(GetTileId(x, y + 1, WorldTm.ForegroundTilemap));
                    if(tileAbove != null && tileAbove.StringID == tileJustDestroyed.StringID)
                    {
                        SetForegroundTileId(x, y + 1, GameDataRegistry.INVALID_ID);
                    }
                }
            }
        }

        public void SetBackgroundTileId(int x, int y, ushort tileId)
        {
            if (!IsInBounds(x, y))
            {
                Debug.LogWarning($"Failed to set tile at ({x}, {y}) on {WorldTm.BackgroundTilemap} because it is out of bounds.");
                return;
            }

            // If trying to set it to the same tile, do nothing
            ushort previousTileId = BgTileData[x, y];
            if (previousTileId == tileId)
            {
                return;
            }

            BgTileData[x, y] = tileId;

            TileChanged?.Invoke(new Vector2Int(x, y), previousTileId, tileId, WorldTm.BackgroundTilemap);
        }

        public void SetMultiTile(int x, int y, TileSO tile, bool flipX = false)
        {
            if (!IsInBounds(x, y))
            {
                return;
            }

            // Register the anchor so the renderer knows where to spawn the multi-tile entity
            Vector2Int anchor = new Vector2Int(x, y);
            ActiveMultiTiles[anchor] = new MultiTileData(tile, flipX);

            // Fill the entire footprint in the tile data array with the actual Tile ID.
            ushort tileId = GameDataRegistry.Instance.GetTileIdFromTileSO(tile);
            for (int i = 0; i < tile.Size.x; i++)
            {
                for (int j = 0; j < tile.Size.y; j++)
                {
                    // Calling SetTileId triggers the TileChanged event for every coordinate in the footprint
                    SetForegroundTileId(x + i, y + j, tileId);
                }
            }

            // Notify the renderer to spawn the GameObject at the anchor
            MultiTileChanged?.Invoke(anchor, tile, true, flipX);
        }

        public void DestroyMultiTile(int x, int y)
        {
            if (!IsInBounds(x, y)) return;

            Vector2Int anchor = Vector2Int.zero;
            TileSO multiTileSO = null;
            bool found = false;

            // Search the registry to find which multi-tile footprint contains these coordinates
            foreach (var kvp in ActiveMultiTiles)
            {
                Vector2Int pos = kvp.Key;
                TileSO so = kvp.Value.TileSO;
                if (x >= pos.x && x < pos.x + so.Size.x && y >= pos.y && y < pos.y + so.Size.y)
                {
                    anchor = pos;
                    multiTileSO = so;
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                Debug.LogWarning($"Could not break a multitile here at ({x}, {y}). This should not be possible, It should exist");
                return;
            }

            // Clear every tile in the multi-tile's footprint
            for (int i = 0; i < multiTileSO.Size.x; i++)
            {
                for (int j = 0; j < multiTileSO.Size.y; j++)
                {
                    // SetTileId will trigger the TileChanged event and clean up the anchor registry automatically
                    SetForegroundTileId(anchor.x + i, anchor.y + j, GameDataRegistry.INVALID_ID);
                }
            }

            // Notify the renderer to remove the GameObject and clean up the registry
            MultiTileChanged?.Invoke(anchor, multiTileSO, false, false);
            ActiveMultiTiles.Remove(anchor);
        }

        public ushort GetTileId(int x, int y, WorldTm targetMap = WorldTm.ForegroundTilemap)
        {
            if (!IsInBounds(x, y))
            {
                return GameDataRegistry.INVALID_ID;
            }

            return targetMap == WorldTm.ForegroundTilemap ? FgTileData[x, y] : BgTileData[x, y];
        }

        public bool IsInBounds(int x, int y)
        {
            return x >= 0 && x < Width && y >= 0 && y < Height;
        }
        
        public bool IsUnderwaterAirAt(int x, int y)
        {
            if (!IsInBounds(x, y))
            {
                return false;
            }
            
            return _underwaterAirTiles.Contains(GetTileIndex(x, y));
        }

        public void AddUnderwaterAirTile(int x, int y)
        {
            if (!IsInBounds(x, y))
            {
                return;
            }

            if (_underwaterAirTiles.Add(GetTileIndex(x, y)))
            {
                TileChanged?.Invoke(new Vector2Int(x, y), GameDataRegistry.INVALID_ID, GameDataRegistry.INVALID_ID, WorldTm.WaterTilemap);
            }
        }

        public void RemoveUnderwaterAirTile(int x, int y)
        {
            if (!IsInBounds(x, y))
            {
                return;
            }

            if (_underwaterAirTiles.Remove(GetTileIndex(x, y)))
            {
                TileChanged?.Invoke(new Vector2Int(x, y), GameDataRegistry.INVALID_ID, GameDataRegistry.INVALID_ID, WorldTm.WaterTilemap);
            }
        }
        
        private int GetTileIndex(int x, int y)
        {
            return (y * Width) + x;
        }
    }
}

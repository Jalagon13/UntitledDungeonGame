using System.Collections;
using UnityEngine;

namespace DeepSeaGame
{
    public class PlaceKelpStep : GenerationStep
    {
        [SerializeField] private TileSO _kelpTile;
        [SerializeField] private TileSO _sandTile;
        [SerializeField] private int _kelpMinHeight = 2;
        [SerializeField] private int _kelpMaxHeight = 10;
        [SerializeField, Range(0f, 1f), Tooltip("Controls the sparseness of the kelp. Higher value = more kelp.")] 
        private float _kelpSpawnChance = 0.5f;
    
        public override WorldGenerationState State => WorldGenerationState.PlacingKelp;

        public override IEnumerator Execute(WorldGenerationContext context)
        {
            WorldDataStore dataStore = context.DataStore;
            System.Random random = context.Random;
            int width = dataStore.Width;
            int height = dataStore.Height;
            
            ushort kelpId = GameDataRegistry.Instance.GetTileIdFromTileSO(_kelpTile);
            ushort sandId = GameDataRegistry.Instance.GetTileIdFromTileSO(_sandTile);
            
            int lastKelpX = -2;

            for (int x = 0; x < width; x++)
            {
                // Yield occasionally so we don't block the main thread too long
                if (x % context.Config.ColumnsPerFrame == 0)
                {
                    context.SetStepProgress((float)x / width);
                    yield return null;
                }

                // Chance to place kelp based on spawn chance
                if (random.NextDouble() > _kelpSpawnChance) 
                {
                    continue;
                }

                // Must be separated by at least 1 tile
                if (x - lastKelpX <= 1)
                {
                    continue;
                }

                // Travel downward from sea level to find a solid foreground tile
                int hitY = -1;
                for (int y = height; y >= 0; y--)
                {
                    ushort tileId = dataStore.GetTileId(x, y, WorldTm.ForegroundTilemap);
                    if (tileId != GameDataRegistry.INVALID_ID)
                    {
                        hitY = y;
                        break;
                    }
                }

                // If we hit something and it's sand
                if (hitY >= 0)
                {
                    ushort hitTileId = dataStore.GetTileId(x, hitY, WorldTm.ForegroundTilemap);
                    if (hitTileId == sandId)
                    {
                        // Place kelp
                        int kelpHeight = random.Next(_kelpMinHeight, _kelpMaxHeight + 1);
                        
                        // Traverse upward planting kelp
                        for (int i = 1; i <= kelpHeight; i++)
                        {
                            int placeY = hitY + i;
                            
                            // Stop if we hit sea level (kelp shouldn't grow above sea level)
                            if (placeY >= height)
                            {
                                break;
                            }

                            // Only plant if the tile is empty
                            ushort currentTile = dataStore.GetTileId(x, placeY, WorldTm.ForegroundTilemap);
                            if (currentTile == GameDataRegistry.INVALID_ID)
                            {
                                dataStore.SetForegroundTileId(x, placeY, kelpId);
                            }
                            else
                            {
                                // Stop growing if it hits an obstacle
                                break;
                            }
                        }
                        
                        lastKelpX = x;
                    }
                }
            }
            
            context.SetStepProgress(1f);
        }
    }
}

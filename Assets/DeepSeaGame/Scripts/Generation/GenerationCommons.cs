using System;
using UnityEngine;

namespace DeepSeaGame
{
    public enum WorldGenerationState
    {
        NotStarted,
        Initializing,
        GeneratingSurface,
        FillingTerrain,
        CarvingCaves,
        CarvingCaveEntrances,
        PlacingIronOre,
        PlacingKelp,
        FinalizingSpawn,
        Completed
    }
    
    public enum WorldTm
    {
        ForegroundTilemap,
        BackgroundTilemap,
        WaterTilemap
    }

    public enum TileBreakMode
    {
        SingleTileHit,
        FromHitTileUp,
        FromHitTileDown,
    }
}
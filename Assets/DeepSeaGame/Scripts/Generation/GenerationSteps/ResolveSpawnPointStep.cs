using System.Collections;
using UnityEngine;

namespace DeepSeaGame
{
    public class ResolveSpawnPointStep : GenerationStep
    {
        public override WorldGenerationState State => WorldGenerationState.FinalizingSpawn;

        public override IEnumerator Execute(WorldGenerationContext context)
        {
            int centerX = context.Config.WorldWidth / 2;
            int seaLevelY = context.Config.SeaLevelY;

            context.SpawnTile = new Vector3Int(centerX, seaLevelY - 14, 0);
            context.SetStepProgress(1f);
            yield break;
        }
    }
}

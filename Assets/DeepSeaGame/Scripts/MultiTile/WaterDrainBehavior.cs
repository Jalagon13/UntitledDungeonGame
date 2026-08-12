using System.Collections.Generic;
using UnityEngine;

namespace DeepSeaGame
{
    [CreateAssetMenu(fileName = "Water Drain Behavior", menuName = "MultiTile/Lifecycle/WaterDrain")]
    public class WaterDrainBehavior : MultiTileBehavior
    {
        [SerializeField] private int _maxTileDetection = 40;
        [SerializeField] private float _drainInterval = 5f;
        [SerializeField] private int _minYHeightToWork = 250;

        public override void OnPlaced(MultiTileInstance instance, WorldDataStore dataStore)
        {
            if (IsSpaceClosedOff(instance.Anchor, dataStore, out HashSet<Vector2Int> visited))
            {
                DrainWater(instance.Anchor, dataStore, visited);
            }
        }

        public override void OnRemoved(MultiTileInstance instance, WorldDataStore dataStore)
        {
            if (dataStore.IsUnderwaterAirAt(instance.Anchor.x, instance.Anchor.y))
            {
                Queue<Vector2Int> queue = new Queue<Vector2Int>();
                HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
                
                queue.Enqueue(instance.Anchor);
                visited.Add(instance.Anchor);
                
                Vector2Int[] directions = new Vector2Int[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
                
                while (queue.Count > 0)
                {
                    Vector2Int current = queue.Dequeue();
                    
                    foreach (Vector2Int dir in directions)
                    {
                        Vector2Int neighbor = current + dir;
                        
                        if (dataStore.IsInBounds(neighbor.x, neighbor.y) && !visited.Contains(neighbor) && dataStore.IsUnderwaterAirAt(neighbor.x, neighbor.y))
                        {
                            visited.Add(neighbor);
                            queue.Enqueue(neighbor);
                        }
                    }
                }
                
                FillWater(instance.Anchor, dataStore, visited);
            }
        }

        public override void OnUpdate(MultiTileInstance instance, WorldDataStore dataStore, float deltaTime)
        {
            if(instance.Anchor.y < _minYHeightToWork)
            {
                return;
            }
            
            instance.Timer += deltaTime;
            if (instance.Timer >= _drainInterval)
            {
                instance.Timer -= _drainInterval;
                OnTimerComplete(instance, dataStore);
            }
        }

        private void OnTimerComplete(MultiTileInstance instance, WorldDataStore dataStore)
        {
            if (IsSpaceClosedOff(instance.Anchor, dataStore, out HashSet<Vector2Int> visited))
            {
                DrainWater(instance.Anchor, dataStore, visited);
            }
            else if(dataStore.IsUnderwaterAirAt(instance.Anchor.x, instance.Anchor.y))
            {
                FillWater(instance.Anchor, dataStore, visited);
            }
        }

        // Flood fills to check if the space is closed off by foreground tiles and its size is less than or equal to _maxTileDetection
        private bool IsSpaceClosedOff(Vector2Int startPos, WorldDataStore dataStore, out HashSet<Vector2Int> visited)
        {
            Queue<Vector2Int> queue = new();
            visited = new();
            
            queue.Enqueue(startPos);
            visited.Add(startPos);

            Vector2Int[] directions = new Vector2Int[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

            while (queue.Count > 0)
            {
                // If the pocket is larger than our max detection size, it is not considered closed off (or too big)
                if (visited.Count > _maxTileDetection)
                {
                    return false;
                }

                Vector2Int current = queue.Dequeue();

                foreach (Vector2Int dir in directions)
                {
                    Vector2Int neighbor = current + dir;

                    // If we reach out of bounds, the space is open to the edge of the world
                    if (!dataStore.IsInBounds(neighbor.x, neighbor.y))
                    {
                        return false;
                    }

                    // If there is a valid solid foreground tile (or one marked as an enclosure), it acts as a boundary wall
                    ushort tileId = dataStore.GetTileId(neighbor.x, neighbor.y);
                    if (tileId != GameDataRegistry.INVALID_ID)
                    {
                        var tileSO = GameDataRegistry.Instance.GetTileSOFromTileId(tileId);
                        if (tileSO.IsSolid || tileSO.ActsAsEnclosure)
                        {
                            continue;
                        }
                    }

                    // If it is empty space and we haven't visited it yet, keep filling
                    if (!visited.Contains(neighbor))
                    {
                        visited.Add(neighbor);
                        queue.Enqueue(neighbor);
                    }
                }
            }

            // If we finish checking and never exceeded max tiles or hit bounds, it is completely enclosed
            return true;
        }

        private void DrainWater(Vector2Int anchor, WorldDataStore dataStore, HashSet<Vector2Int> visited)
        {
            foreach (Vector2Int pos in visited)
            {
                dataStore.AddUnderwaterAirTile(pos.x, pos.y);
            }
            // Debug.Log($"ShelterCore valid space detected at {anchor}. Drained {visited.Count} tiles.");
        }

        private void FillWater(Vector2Int anchor, WorldDataStore dataStore, HashSet<Vector2Int> visited)
        {
            foreach (Vector2Int pos in visited)
            {
                dataStore.RemoveUnderwaterAirTile(pos.x, pos.y);
            }
            // Debug.Log($"ShelterCore exposed at {anchor}. Filled {visited.Count} tiles with water.");
        }
    }
}

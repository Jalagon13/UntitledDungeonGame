using System.Collections.Generic;
using UnityEngine;

namespace UntitledDungeonGame
{
    [CreateAssetMenu(fileName = "New Resource Data", menuName = "Data/ResourceData")]
    public class ResourceSO : ScriptableObject
    {
        [Header("General Settings")]
        [Tooltip("Name of the resource world object")]
        public string ResourceName;
        [Tooltip("If true, the player can pass through this resource")]
        public bool PassThrough = false;
        [Tooltip("If true, this resource can be destroyed")]
        public bool CanBeDestroyed = true;
        [Tooltip("Prefab for this Resource")]
        public ResourceObject ResourcePrefab;
        [Tooltip("Hardness value determining mining speed")]
        public float Hardness = 1f;
        [Tooltip("Which tool is needed to harvest")]
        public ToolType HarvestType;

        [Tooltip("Loot table for items dropped by this resource")]
        public List<Loot> Table = new();
    }
}

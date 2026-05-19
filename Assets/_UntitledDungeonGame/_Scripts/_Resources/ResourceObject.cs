using System;
using UnityEngine;

namespace UntitledDungeonGame
{
    public class ResourceObject : MonoBehaviour
    {
        [SerializeField]
        private ResourceSO _resourceData;
        public ResourceSO Data => _resourceData;
        
        [SerializeField]
        private Transform _dropPoint;

        public void Destroy()
        {
            Debug.Log($"Destroying {name}");
            SpawnItems();
            Destroy(gameObject);
        }

        public void SpawnItems()
        {
            Debug.Log($"Item spawn logic local to resource object");
            LootTable.SpawnLoot(_resourceData.Table, _dropPoint.transform.position);
        }
        
        public virtual void OnGhostSpawn()
        {
            
        }
    }
}
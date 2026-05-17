using UnityEngine;

namespace UntitledDungeonGame
{
    public class ResourceObject : MonoBehaviour
    {
        [SerializeField]
        private ResourceSO _resourceData;
        public ResourceSO Data => _resourceData;
    }
}
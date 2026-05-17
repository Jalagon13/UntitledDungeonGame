using UnityEngine;

namespace UntitledDungeonGame
{
    public class PlacementManager : MonoBehaviour
    {
        public static PlacementManager Instance { get; private set; }
        
        private void Awake()
        {
            Instance = this;
        }
        
        
    }
}

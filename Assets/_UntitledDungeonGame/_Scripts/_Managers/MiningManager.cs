using UnityEngine;

namespace UntitledDungeonGame
{
    public class MiningManager : MonoBehaviour
    {
        public static MiningManager Instance { get; private set; }
        
        [SerializeField]
        private float _miningRange = 3f;
        public float MiningRange => _miningRange;
        
        private void Awake()
        {
            Instance = this;
        }
        
        
    }
}
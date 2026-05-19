using UnityEngine;

namespace UntitledDungeonGame
{
    public class TorchObject : ResourceObject
    {
        [SerializeField]
        private LightSource _lightSource;
    
        public override void OnGhostSpawn()
        {
            base.OnGhostSpawn();

            _lightSource.enabled = false;
        }
    }
}

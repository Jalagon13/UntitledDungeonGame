using UnityEngine;
using Unity.Netcode;

namespace UntitledDungeonGame
{
    public class NetworkLifeState : NetworkBehaviour
    {
        [SerializeField]
        private NetworkVariable<LifeState> _lifeState = new NetworkVariable<LifeState>();

        public NetworkVariable<LifeState> LifeState => _lifeState;
    }
}


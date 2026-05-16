using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace UntitledDungeonGame
{
    /// <summary>
    /// <see cref="ClientCharacter"/> is responsible for displaying a character on the client's screen based on state information sent by the server.
    /// </summary>
    public class ClientCharacter : NetworkBehaviour
    {
        [SerializeField]
        private ServerCharacter _serverCharacter;

        [SerializeField]
        private GameObject _visuals;
        public GameObject Visuals => _visuals;

        [SerializeField]
        private GameObject _colliderHolder;
        public GameObject ColliderHolder => _colliderHolder;

        private BaseState _currentSuperState;
        private BaseState _currentSubState;
        private AIStateData _currentSuperStateData;
        private AIStateData _currentSubStateData;

        public override void OnNetworkSpawn()
        {
            if (!IsClient) return;

            if (!_serverCharacter.CharacterData.IsNpc)
            {
                gameObject.name = $"Player_{OwnerClientId}";

                if (_serverCharacter.IsOwner && _serverCharacter.TryGetComponent(out Player player))
                {
                    player.OnNetworkSpawnLocalClientInitializations();
                }
            }
        }

        protected override void OnNetworkPostSpawn()
        {
            if (!IsClient) return;

            _serverCharacter.SuperAIState.OnValueChanged += OnSuperAIStateChanged;
            _serverCharacter.SubAIState.OnValueChanged += OnSubAIStateChanged;
        }

        public override void OnNetworkDespawn()
        {
            if (!IsClient) return;

            _serverCharacter.SuperAIState.OnValueChanged -= OnSuperAIStateChanged;
            _serverCharacter.SubAIState.OnValueChanged -= OnSubAIStateChanged;
        }

        private void Update()
        {
            if (!IsClient) return;

            _currentSuperState?.ClientUpdateState(_currentSuperStateData);
            _currentSubState?.ClientUpdateState(_currentSubStateData);
        }

        private void OnSuperAIStateChanged(AIStateData previousValue, AIStateData newValue)
        {
            // Take the previousValue and run the exit function, take the new value, and run the enter function somehow
            BaseState previousSuperState = _serverCharacter.StateMachine.GetState(previousValue.CurrentState);
            previousSuperState?.ClientExitState(previousValue);

            _currentSuperState = _serverCharacter.StateMachine.GetState(newValue.CurrentState);
            _currentSuperState?.ClientEnterState(newValue);
            _currentSuperStateData = newValue;
        }

        private void OnSubAIStateChanged(AIStateData previousValue, AIStateData newValue)
        {
            BaseState previousSubState = _serverCharacter.StateMachine.GetState(previousValue.CurrentState);
            previousSubState?.ClientExitState(previousValue);

            _currentSubState = _serverCharacter.StateMachine.GetState(newValue.CurrentState);
            _currentSubState?.ClientEnterState(newValue);
            _currentSubStateData = newValue;
        }
    }
}

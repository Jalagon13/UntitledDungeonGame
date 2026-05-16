using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace UntitledDungeonGame
{
    [RequireComponent(typeof(ServerCharacter))]
    public class Player : NetworkBehaviour
    {
        public static event EventHandler<PlayerIdEventArgs> OnAnyPlayerSpawned;
        public class PlayerIdEventArgs : EventArgs
        {
            public ulong PlayerId;
        }
        
        public static Player Instance { get; private set; }

        private ServerCharacter _character;
        public ServerCharacter Character => _character;
        
        private void Awake()
        {
            _character = GetComponent<ServerCharacter>();
        }

        public override void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            GameInput.Instance.OnMove -= GameInput_OnMove;
            InventoryManager.Instance.OnInventoryOpenChanged -= OnInventoryOpenChanged;
        }

        public void OnNetworkSpawnLocalClientInitializations()
        {
            Instance = this;

            OnAnyPlayerSpawned?.Invoke(this, new PlayerIdEventArgs
            {
                PlayerId = OwnerClientId
            });

            // local player start up code here, maybe input
            GameInput.Instance.OnMove += GameInput_OnMove;
            InventoryManager.Instance.OnInventoryOpenChanged += OnInventoryOpenChanged;
        }

        private void OnInventoryOpenChanged(bool isOpen)
        {
            _character.Movement?.ReceiveMoveInput(Vector2.zero);
            
            if(!isOpen)
            {
                _character.Movement?.ReceiveMoveInput(GameInput.Instance.MoveInput);
            }
        }

        private void GameInput_OnMove(object sender, InputAction.CallbackContext context)
        {
            if (_character == null || !_character.IsOwner)
            {
                return;
            }

            _character.Movement?.ReceiveMoveInput(GameInput.Instance.MoveInput);
        }
    }
}

using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace UntitledDungeonGame
{
    [RequireComponent(typeof(ServerCharacter))]
    public class Player : MonoBehaviour
    {
        public static Player Instance { get; private set; }

        private ServerCharacter _character;
        public ServerCharacter Character => _character;
        
        private void Awake()
        {
            Instance = this;

            _character = GetComponent<ServerCharacter>();
        }

        private void Start()
        {
            GameInput.Instance.OnMove += GameInput_OnMove;
            InventoryManager.Instance.OnInventoryOpenChanged += OnInventoryOpenChanged;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            GameInput.Instance.OnMove -= GameInput_OnMove;
            InventoryManager.Instance.OnInventoryOpenChanged -= OnInventoryOpenChanged;
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

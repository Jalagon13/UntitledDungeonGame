using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace UntitledDungeonGame
{
    public class GameInput : MonoBehaviour
    {
        public static GameInput Instance { get; private set; }

        public event EventHandler<InputAction.CallbackContext> OnPrimaryActionStarted;

        public event EventHandler<InputAction.CallbackContext> OnMove;

        public event EventHandler<InputAction.CallbackContext> OnToggleInventory;
        public event EventHandler<InputAction.CallbackContext> OnScrollWheel;
        public event EventHandler<InputAction.CallbackContext> OnSelectSlot;

        private bool _isGameplayInputBlocked, _primaryHeldDown;
        public bool PrimaryActionHeldDown => _primaryHeldDown;

        public bool IsGameplayInputBlocked 
        {
            get { return _isGameplayInputBlocked; }
            set 
            { 
                // Debug.Log($"GameplayInputBlockedChanged to {value}");
                _isGameplayInputBlocked = value; 
            }
        }

        private PlayerInput _playerInput;

        public Vector2 MoveInput { get; private set; }

        private void Awake()
        {
            Instance = this;

            _playerInput = new();
            _playerInput.Enable();
            
            _playerInput.Player.PrimaryAction.started += PlayerInput_OnPrimaryAction;
            _playerInput.Player.PrimaryAction.performed += PlayerInput_OnPrimaryAction;
            _playerInput.Player.PrimaryAction.canceled += PlayerInput_OnPrimaryAction;

            _playerInput.Player.Move.started += PlayerInput_OnMove;
            _playerInput.Player.Move.performed += PlayerInput_OnMove;
            _playerInput.Player.Move.canceled += PlayerInput_OnMove;

            _playerInput.UI.ScrollWheel.performed += PlayerInput_OnScrollWheel;
            _playerInput.UI.SelectSlot.started += PlayerInput_OnSelectSlot;
            _playerInput.UI.ToggleInventory.started += GameInput_OnToggleInventory;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            _playerInput.Player.PrimaryAction.started -= PlayerInput_OnPrimaryAction;
            _playerInput.Player.PrimaryAction.performed -= PlayerInput_OnPrimaryAction;
            _playerInput.Player.PrimaryAction.canceled -= PlayerInput_OnPrimaryAction;

            _playerInput.Player.Move.started -= PlayerInput_OnMove;
            _playerInput.Player.Move.performed -= PlayerInput_OnMove;
            _playerInput.Player.Move.canceled -= PlayerInput_OnMove;

            _playerInput.UI.ScrollWheel.performed -= PlayerInput_OnScrollWheel;
            _playerInput.UI.SelectSlot.started -= PlayerInput_OnSelectSlot;
            _playerInput.UI.ToggleInventory.started -= GameInput_OnToggleInventory;

            _playerInput.Disable();
            _playerInput.Dispose();
        }

        private void PlayerInput_OnPrimaryAction(InputAction.CallbackContext context)
        {
            _primaryHeldDown = context.performed || context.started;
            
            if(_isGameplayInputBlocked) return;

            OnPrimaryActionStarted?.Invoke(this, context);
        }

        private void PlayerInput_OnScrollWheel(InputAction.CallbackContext context)
        {
            if (_isGameplayInputBlocked) return;

            OnScrollWheel?.Invoke(this, context);
        }

        private void PlayerInput_OnSelectSlot(InputAction.CallbackContext context)
        {
            if(_isGameplayInputBlocked) return;
        
            OnSelectSlot?.Invoke(this, context);
        }

        private void GameInput_OnToggleInventory(InputAction.CallbackContext context)
        {
            OnToggleInventory?.Invoke(this, context);
        }

        private void PlayerInput_OnMove(InputAction.CallbackContext context)
        {
            MoveInput = context.ReadValue<Vector2>();
            if (_isGameplayInputBlocked) return;

            OnMove?.Invoke(this, context);
        }
    }
}

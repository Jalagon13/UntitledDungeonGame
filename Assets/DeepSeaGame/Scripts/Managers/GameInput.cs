using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DeepSeaGame
{
    public class GameInput : MonoBehaviour
    {
        public static GameInput Instance { get; private set; }

        public event EventHandler<InputAction.CallbackContext> OnPrimaryActionStarted;
        public event EventHandler<InputAction.CallbackContext> OnSecondaryActionStarted;
        public event EventHandler<InputAction.CallbackContext> OnToggleFlashlight;
        
        public event EventHandler<InputAction.CallbackContext> OnMove;
        public event EventHandler<InputAction.CallbackContext> OnJump;

        public event EventHandler<InputAction.CallbackContext> OnToggleInventory;
        public event EventHandler<InputAction.CallbackContext> OnScrollWheel;
        public event EventHandler<InputAction.CallbackContext> OnSelectSlot;
        public event EventHandler<InputAction.CallbackContext> OnInteract;
        public event EventHandler<InputAction.CallbackContext> OnPlaceLightSource;
        public event EventHandler<InputAction.CallbackContext> OnTogglePauseMenu;

        private bool _isGameplayInputBlocked, _primaryHeldDown, _secondaryHeldDown, _jumpHeldDown;

        public bool JumpHeldDown => !_isGameplayInputBlocked && _jumpHeldDown;
        public bool PrimaryActionHeldDown => !_isGameplayInputBlocked && _primaryHeldDown;
        public bool SecondaryActionHeldDown => !_isGameplayInputBlocked && _secondaryHeldDown;

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
            
            _playerInput.Player.SecondaryAction.started += PlayerInput_OnSecondaryAction;
            _playerInput.Player.SecondaryAction.performed += PlayerInput_OnSecondaryAction;
            _playerInput.Player.SecondaryAction.canceled += PlayerInput_OnSecondaryAction;

            _playerInput.Player.Move.started += PlayerInput_OnMove;
            _playerInput.Player.Move.performed += PlayerInput_OnMove;
            _playerInput.Player.Move.canceled += PlayerInput_OnMove;
            
            _playerInput.Player.Jump.started += PlayerInput_OnJump;
            _playerInput.Player.Jump.canceled += PlayerInput_OnJump;
            
            _playerInput.Player.Interact.started += PlayerInput_OnInteract;
            _playerInput.Player.ToggleFlashlight.started += PlayerInput_OnToggleFlashlight;
            _playerInput.Player.PlaceLightSource.started += PlayerInput_OnPlaceLightSource;

            _playerInput.UI.ScrollWheel.performed += PlayerInput_OnScrollWheel;
            _playerInput.UI.SelectSlot.started += PlayerInput_OnSelectSlot;
            _playerInput.UI.ToggleInventory.started += GameInput_OnToggleCraftingMenu;
            _playerInput.UI.TogglePauseMenu.started += PlayerInput_OnTogglePauseMenu;
        }

        private void OnDestroy()
        {
            _playerInput.Player.PrimaryAction.started -= PlayerInput_OnPrimaryAction;
            _playerInput.Player.PrimaryAction.performed -= PlayerInput_OnPrimaryAction;
            _playerInput.Player.PrimaryAction.canceled -= PlayerInput_OnPrimaryAction;

            _playerInput.Player.SecondaryAction.started -= PlayerInput_OnSecondaryAction;
            _playerInput.Player.SecondaryAction.performed -= PlayerInput_OnSecondaryAction;
            _playerInput.Player.SecondaryAction.canceled -= PlayerInput_OnSecondaryAction;

            _playerInput.Player.Move.started -= PlayerInput_OnMove;
            _playerInput.Player.Move.performed -= PlayerInput_OnMove;
            _playerInput.Player.Move.canceled -= PlayerInput_OnMove;

            _playerInput.Player.Jump.started -= PlayerInput_OnJump;
            _playerInput.Player.Jump.canceled -= PlayerInput_OnJump;

            _playerInput.Player.Interact.started -= PlayerInput_OnInteract;
            _playerInput.Player.ToggleFlashlight.started -= PlayerInput_OnToggleFlashlight;
            _playerInput.Player.PlaceLightSource.started -= PlayerInput_OnPlaceLightSource;

            _playerInput.UI.ScrollWheel.performed -= PlayerInput_OnScrollWheel;
            _playerInput.UI.SelectSlot.started -= PlayerInput_OnSelectSlot;
            _playerInput.UI.ToggleInventory.started -= GameInput_OnToggleCraftingMenu;
            _playerInput.UI.TogglePauseMenu.started -= PlayerInput_OnTogglePauseMenu;

            _playerInput.Disable();
            _playerInput.Dispose();
        }

        private void PlayerInput_OnTogglePauseMenu(InputAction.CallbackContext context)
        {
            OnTogglePauseMenu?.Invoke(this, context);
        }

        private void PlayerInput_OnPlaceLightSource(InputAction.CallbackContext context)
        {
            if (_isGameplayInputBlocked) return;

            if(context.started)
            {
                OnPlaceLightSource?.Invoke(this, context);
            }
        }

        private void PlayerInput_OnToggleFlashlight(InputAction.CallbackContext context)
        {
            if (_isGameplayInputBlocked) return;

            if (context.started)
            {
                OnToggleFlashlight?.Invoke(this, context);
            }
        }

        private void PlayerInput_OnSecondaryAction(InputAction.CallbackContext context)
        {
            if (context.canceled || _isGameplayInputBlocked)
            {
                _secondaryHeldDown = false;
            }
            else
            {
                _secondaryHeldDown = context.performed || context.started;
            }

            if (_isGameplayInputBlocked) return;

            OnSecondaryActionStarted?.Invoke(this, context);
        }

        private void PlayerInput_OnInteract(InputAction.CallbackContext context)
        {
            if (_isGameplayInputBlocked) return;

            OnInteract?.Invoke(this, context);
        }

        private void PlayerInput_OnJump(InputAction.CallbackContext context)
        {
            if(context.started)
            {
                _jumpHeldDown = true;
            }
            else if(context.canceled)
            {
                _jumpHeldDown = false;
            }

            if (_isGameplayInputBlocked || !_jumpHeldDown) return;

            OnJump?.Invoke(this, context);
        }

        private void PlayerInput_OnPrimaryAction(InputAction.CallbackContext context)
        {
            if (context.canceled || _isGameplayInputBlocked)
            {
                _primaryHeldDown = false;
            }
            else
            {
                _primaryHeldDown = context.performed || context.started;
            }
            
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

        private void GameInput_OnToggleCraftingMenu(InputAction.CallbackContext context)
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

using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace UntitledDungeonGame
{
    public class GameInput : MonoBehaviour
    {
        public static GameInput Instance { get; private set; }

        public event EventHandler<InputAction.CallbackContext> OnMove;

        private PlayerInput _playerInput;

        public Vector2 MoveInput { get; private set; }

        private void Awake()
        {
            Instance = this;

            _playerInput = new();
            _playerInput.Enable();

            _playerInput.Player.Move.started += PlayerInput_OnMove;
            _playerInput.Player.Move.performed += PlayerInput_OnMove;
            _playerInput.Player.Move.canceled += PlayerInput_OnMove;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            _playerInput.Player.Move.started -= PlayerInput_OnMove;
            _playerInput.Player.Move.performed -= PlayerInput_OnMove;
            _playerInput.Player.Move.canceled -= PlayerInput_OnMove;

            _playerInput.Disable();
            _playerInput.Dispose();
        }

        private void PlayerInput_OnMove(InputAction.CallbackContext context)
        {
            MoveInput = context.ReadValue<Vector2>();
            OnMove?.Invoke(this, context);
        }
    }
}

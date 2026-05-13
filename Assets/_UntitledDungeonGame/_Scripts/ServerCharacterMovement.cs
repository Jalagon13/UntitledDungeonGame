using UnityEngine;

namespace UntitledDungeonGame
{
    public class ServerCharacterMovement : MonoBehaviour
    {
        [SerializeField]
        private ServerCharacter _serverCharacter;

        [SerializeField]
        private Rigidbody2D _rigidbody2D;
        public Rigidbody2D RigidBody2D => _rigidbody2D;

        private Vector2 _moveInput;

        public void ReceiveMoveInput(Vector2 moveInput)
        {
            _moveInput = moveInput;

            if (_moveInput.sqrMagnitude > 0.0001f)
            {
                StartMovement();
            }
            else
            {
                StartIdle();
            }
        }

        public void StartMovement()
        {
            if (_serverCharacter == null)
            {
                return;
            }

            if (_serverCharacter.MovementState.Value != MovementState.Moving)
            {
                _serverCharacter.MovementState.Value = MovementState.Moving;
            }
        }

        public void StartIdle()
        {
            if (_serverCharacter == null)
            {
                return;
            }

            if (_serverCharacter.MovementState.Value != MovementState.Idle)
            {
                _serverCharacter.MovementState.Value = MovementState.Idle;
            }
        }

        public void FixedUpdateMovement()
        {
            if (_rigidbody2D == null)
            {
                return;
            }

            Vector2 targetVelocity = _moveInput.normalized * _serverCharacter.CharacterData.BaseSpeed;
            _rigidbody2D.linearVelocity = targetVelocity;

            if (_moveInput.sqrMagnitude <= 0.01f)
            {
                _rigidbody2D.linearVelocity = Vector2.zero;
            }
        }
    }
}

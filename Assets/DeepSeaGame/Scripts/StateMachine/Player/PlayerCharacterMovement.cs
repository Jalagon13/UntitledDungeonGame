using System;
using UnityEngine;

namespace DeepSeaGame
{
    public class PlayerCharacterMovement : CharacterMovement
    {
        [SerializeField] private Transform _playerVisuals;
        [SerializeField] private float _visualRotationSpeed = 8f;
        
        [Header("Collider Size Settings")]
        [SerializeField] private Vector2 _airBodySize;
        [SerializeField] private Vector2 _waterBodySize;

        [Header("Player Character Move Settings")]
        [SerializeField] private float _minJumpPower = 8f;
        [SerializeField] private float _maxJumpPower = 24f;
        [SerializeField] private float _maxJumpHoldTime = 0.225f;

        private bool _isGrounded;
        private bool _jumpRequested;
        private bool _jumpCharging;
        private float _jumpChargeStartTime;
        private PlayerArmController _playerArmController;
        private Quaternion _currentVisualRotation = Quaternion.identity;

        private void Awake()
        {
            _playerArmController = GetComponent<PlayerArmController>();
        }
        
        private void Start() 
        {
            if(Player.Instance != null)
            {
                Player.Instance.Character.CurrentStatus.OnValueChanged += StatusChanged;    
            }
        }

        public override void OnDestroy()
        {
            if (Player.Instance != null)
            {
                Player.Instance.Character.CurrentStatus.OnValueChanged -= StatusChanged;
            }
        }

        private void StatusChanged(Status previousValue, Status newValue)
        {
            _gridCollider.SetBodyColliderSize(newValue == Status.InAir ? _airBodySize : _waterBodySize);
        }

        protected override void WaterMovement()
        {
            if (_serverCharacter.CharacterData.CanMove)
            {
                float currentSpeed = _serverCharacter.CharacterData.BaseSpeed;

                _velocity = Vector2.Lerp(_velocity, DesiredDirection * currentSpeed, _serverCharacter.CharacterData.TurnSharpness * Time.fixedDeltaTime);
            }

            // Only allow changing facing direction if we are not currently swinging
            if (_playerArmController == null || !_playerArmController.IsAttacking)
            {
                if (Mathf.Abs(DesiredDirection.x) > 0.01f)
                {
                    _serverCharacter.CurrentDirection.Value = DesiredDirection.x > 0 ? Direction.Right : Direction.Left;
                }
            }

            // Rotate player visuals so their y-axis points toward the velocity direction, lerping smoothly
            Quaternion targetVisualRotation = default;
            
            if (Player.Instance.Character.StateMachine.CurrentState.StateKey == AIState.Locomotion && _velocity.sqrMagnitude > 20f)
            {
                Vector3 velocityDirection = new Vector3(_velocity.x, _velocity.y, 0f).normalized;
                targetVisualRotation = Quaternion.FromToRotation(Vector3.up, velocityDirection);
            }
            else if(Player.Instance.Character.StateMachine.CurrentState.StateKey != AIState.Locomotion)
            {
                targetVisualRotation = Quaternion.identity;
            }

            _currentVisualRotation = Quaternion.Slerp(_currentVisualRotation, targetVisualRotation, _visualRotationSpeed * Time.fixedDeltaTime);
            _playerVisuals.rotation = _currentVisualRotation;
            
        }

        protected override void AirMovement()
        {
            // 1. Grounded Check via our Grid Data
            _isGrounded = _gridCollider.IsGrounded();

            if (_serverCharacter.CharacterData.CanMove)
            {
                float currentSpeed = _serverCharacter.CharacterData.BaseSpeed;

                // 2. Horizontal Movement (Lerp for that snappy control)
                float targetX = Mathf.Lerp(_velocity.x, DesiredDirection.x * currentSpeed, _serverCharacter.CharacterData.TurnSharpness * Time.fixedDeltaTime);

                // 3. Vertical Movement (Constant Gravity)
                float targetY = _velocity.y;
                if (!_isGrounded || targetY > 0)
                {
                    targetY += (_gravity * Time.fixedDeltaTime);
                    targetY = Mathf.Max(targetY, _gravity);
                }
                else
                {
                    targetY = 0f;
                }

                // 4. Jump Logic
                if (_jumpRequested)
                {
                    if (_isGrounded)
                    {
                        targetY = _minJumpPower;
                        _jumpCharging = true;
                        _jumpChargeStartTime = Time.unscaledTime;
                    }

                    _jumpRequested = false; // Consume request regardless of success
                }

                if (_jumpCharging)
                {
                    if (GameInput.Instance != null && GameInput.Instance.JumpHeldDown)
                    {
                        float elapsedHoldTime = Time.unscaledTime - _jumpChargeStartTime;
                        if (elapsedHoldTime < _maxJumpHoldTime)
                        {
                            float holdRatio = Mathf.Clamp01(elapsedHoldTime / _maxJumpHoldTime);
                            float chargedJumpPower = Mathf.Lerp(_minJumpPower, _maxJumpPower, holdRatio);
                            targetY = Mathf.Max(targetY, chargedJumpPower);
                        }
                        else
                        {
                            _jumpCharging = false;
                        }
                    }
                    else
                    {
                        _jumpCharging = false;
                    }
                }

                _velocity = new Vector2(targetX, targetY);

                // 5. Update Direction (Horizontal only in air, only if not swinging)
                if (_playerArmController == null || !_playerArmController.IsAttacking)
                {
                    if (Mathf.Abs(DesiredDirection.x) > 0.01f)
                    {
                        _serverCharacter.CurrentDirection.Value = DesiredDirection.x > 0 ? Direction.Right : Direction.Left;
                    }
                }
            }

            // Lerp visual rotation back to upright while in air
            _currentVisualRotation = Quaternion.Slerp(_currentVisualRotation, Quaternion.identity, _visualRotationSpeed * Time.fixedDeltaTime);
            _playerVisuals.rotation = _currentVisualRotation;
        }

        public void ReceiveJumpInput()
        {
            _jumpRequested = true;
        }

    }
}

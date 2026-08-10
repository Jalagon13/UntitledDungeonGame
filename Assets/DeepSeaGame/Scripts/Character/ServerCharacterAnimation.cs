using System;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace DeepSeaGame
{
    public class ServerCharacterAnimation : NetworkBehaviour
    {
        [SerializeField]
        private ServerCharacter _serverCharacter;

        [SerializeField]
        private NetworkHealthState _networkHealthState;
        [SerializeField]
        private List<ServerSpriteAnimHandler> _spriteAnimHandlers = new List<ServerSpriteAnimHandler>();

        private Direction _actionDirection = Direction.None; // Used for casting direction and swing direction

        public override void OnNetworkSpawn()
        {
            if (IsOwner)
            {
                _networkHealthState.LifeState.OnValueChanged += OnLifeStateChanged;
                _serverCharacter.MovementState.OnValueChanged += PlayCurrentMoveState;
                _serverCharacter.CurrentDirection.OnValueChanged += OnDirectionChanged;
                _serverCharacter.CurrentStatus.OnValueChanged += OnStatusChanged;

                if (_serverCharacter.TryGetComponent(out Player player))
                {
                    player.PlayerArmController.AimDirection.OnValueChanged += OnActionDirectionChanged;
                    player.PlayerArmController.AimingStateChanged += OnAimingStateChanged;
                }
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsOwner && _networkHealthState != null)
            {
                _networkHealthState.LifeState.OnValueChanged -= OnLifeStateChanged;
                _serverCharacter.MovementState.OnValueChanged -= PlayCurrentMoveState;
                _serverCharacter.CurrentDirection.OnValueChanged -= OnDirectionChanged;
                _serverCharacter.CurrentStatus.OnValueChanged -= OnStatusChanged;
                
                if (_serverCharacter.TryGetComponent(out Player player))
                {
                    player.PlayerArmController.AimDirection.OnValueChanged -= OnActionDirectionChanged;
                    player.PlayerArmController.AimingStateChanged -= OnAimingStateChanged;
                }
            }
        }

        private void OnStatusChanged(Status previousValue, Status newValue)
        {
            foreach (ServerSpriteAnimHandler handler in _spriteAnimHandlers)
            {
                handler.PlayAnimation(_serverCharacter.MovementState.Value, GetAnimationDirection(_serverCharacter.CurrentDirection.Value));
            }
        }

        private void OnActionDirectionChanged(Direction previousValue, Direction newValue)
        {
            _actionDirection = newValue;

            foreach (ServerSpriteAnimHandler handler in _spriteAnimHandlers)
            {
                handler.PlayAnimation(_serverCharacter.MovementState.Value, GetAnimationDirection(_serverCharacter.CurrentDirection.Value));
            }
        }

        private void OnDirectionChanged(Direction previousValue, Direction newValue)
        {
            foreach (ServerSpriteAnimHandler handler in _spriteAnimHandlers)
            {
                handler.PlayAnimation(_serverCharacter.MovementState.Value, GetAnimationDirection(newValue));
            }
        }

        private void PlayCurrentMoveState(MovementState previousMovementState, MovementState newMovementState)
        {
            Direction direction = _serverCharacter.CurrentDirection.Value;

            foreach (ServerSpriteAnimHandler handler in _spriteAnimHandlers)
            {
                handler.PlayAnimation(newMovementState, GetAnimationDirection(direction));
            }
        }

        private Direction GetAnimationDirection(Direction fallbackDirection)
        {
            if (_serverCharacter.TryGetComponent(out Player player) && player.PlayerArmController != null && player.PlayerArmController.IsAiming)
            {
                return _actionDirection == Direction.None ? fallbackDirection : _actionDirection;
            }

            return fallbackDirection;
        }

        private void OnAimingStateChanged(bool isAiming)
        {
            foreach (ServerSpriteAnimHandler handler in _spriteAnimHandlers)
            {
                handler.PlayAnimation(_serverCharacter.MovementState.Value, GetAnimationDirection(_serverCharacter.CurrentDirection.Value));
            }
        }

        private void OnLifeStateChanged(LifeState previousValue, LifeState newValue)
        {
            // TODO: Later
            switch (newValue)
            {
                case LifeState.Alive:

                    break;
                case LifeState.IFrame:

                    break;
                case LifeState.Dead:

                    break;
            }
        }
    }
}

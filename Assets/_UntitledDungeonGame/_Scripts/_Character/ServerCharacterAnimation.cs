using System;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace UntitledDungeonGame
{
    public class ServerCharacterAnimation : NetworkBehaviour
    {
        [SerializeField]
        private ServerCharacter _serverCharacter;

        [SerializeField]
        private NetworkLifeState _networkLifeState;
        [SerializeField]
        private List<ServerSpriteAnimHandler> _spriteAnimHandlers = new List<ServerSpriteAnimHandler>();

        private CardinalDirection _actionDirection = CardinalDirection.None; // Used for casting direction and swing direction

        public override void OnNetworkSpawn()
        {
            if (IsOwner)
            {
                _networkLifeState.LifeState.OnValueChanged += OnLifeStateChanged;
                _serverCharacter.MovementState.OnValueChanged += PlayCurrentMoveState;
                _serverCharacter.CardinalDirection.OnValueChanged += OnCardinalDirectionChanged;
                // if (_serverCharacter.TryGetComponent(out Player player))
                // {
                //     player.PlayerHand.SwingDirection.OnValueChanged += OnActionDirectionChanged;
                //     player.PlayerHand.CastingDirection.OnValueChanged += OnActionDirectionChanged;
                // }
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsOwner && _networkLifeState != null)
            {
                _networkLifeState.LifeState.OnValueChanged -= OnLifeStateChanged;
                _serverCharacter.MovementState.OnValueChanged -= PlayCurrentMoveState;
                _serverCharacter.CardinalDirection.OnValueChanged -= OnCardinalDirectionChanged;
                // if (_serverCharacter.TryGetComponent(out Player player))
                // {
                //     player.PlayerHand.SwingDirection.OnValueChanged -= OnActionDirectionChanged;
                //     player.PlayerHand.CastingDirection.OnValueChanged -= OnActionDirectionChanged;
                // }
            }
        }

        private void OnActionDirectionChanged(CardinalDirection previousValue, CardinalDirection newValue)
        {
            _actionDirection = newValue;

            foreach (ServerSpriteAnimHandler handler in _spriteAnimHandlers)
            {
                handler.PlayAnimation(_serverCharacter.MovementState.Value, _actionDirection == CardinalDirection.None ? _serverCharacter.CardinalDirection.Value : _actionDirection);
            }
        }

        private void OnCardinalDirectionChanged(CardinalDirection previousValue, CardinalDirection newValue)
        {
            foreach (ServerSpriteAnimHandler handler in _spriteAnimHandlers)
            {
                handler.PlayAnimation(_serverCharacter.MovementState.Value, _actionDirection == CardinalDirection.None ? newValue : _actionDirection);
            }
        }

        private void PlayCurrentMoveState(MovementState previousMovementState, MovementState newMovementState)
        {
            CardinalDirection direction = _serverCharacter.CardinalDirection.Value;

            foreach (ServerSpriteAnimHandler handler in _spriteAnimHandlers)
            {
                handler.PlayAnimation(newMovementState, _actionDirection == CardinalDirection.None ? direction : _actionDirection);
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

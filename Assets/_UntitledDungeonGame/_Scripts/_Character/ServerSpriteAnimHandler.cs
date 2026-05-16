using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace UntitledDungeonGame
{
    public class ServerSpriteAnimHandler : NetworkBehaviour
    {
        [SerializeField]
        private AnimationConfigSO _animConfig;

        private ServerCharacter _serverCharacter;
        private Animator _animator;

        private void Awake()
        {
            _serverCharacter = transform.root.GetComponent<ServerCharacter>();
            _animator = GetComponent<Animator>();
        }

        public void PlayAnimation(MovementState movementState, CardinalDirection cardinalDirection)
        {
            UpdateSpriteOrientationClientRpc(cardinalDirection);
            AnimationClip clip = null;

            if (movementState == MovementState.Idle)
            {
                clip = cardinalDirection switch
                {
                    CardinalDirection.North => _animConfig.BackIdleClip,
                    CardinalDirection.South => _animConfig.FrontIdleClip,
                    _ => _animConfig.SideIdleClip,
                };
            }
            else if (movementState == MovementState.Pursuing || movementState == MovementState.Knockback || movementState == MovementState.Moving || movementState == MovementState.Fleeing)
            {
                clip = cardinalDirection switch
                {
                    CardinalDirection.North => _animConfig.BackMoveClip,
                    CardinalDirection.South => _animConfig.FrontMoveClip,
                    _ => _animConfig.SideMoveClip,
                };
            }

            if (clip != null)
            {
                AnimStateManager.ChangeAnimationState(_animator, clip);
            }
            else
            {
                Debug.Log($"Clip is null");
            }
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void UpdateSpriteOrientationClientRpc(CardinalDirection direction)
        {
            bool isPlayer = _serverCharacter.TryGetComponent(out Player player);

            // Default scale facing East
            if (isPlayer)
            {
                transform.localScale = Vector3.one;
            }
            else
            {
                transform.parent.localScale = Vector3.one;
            }

            // Flip sprite for West direction
            if (direction == CardinalDirection.West)
            {
                if (isPlayer)
                {
                    transform.localScale = new Vector3(-1, 1, 1);
                }
                else
                {
                    transform.parent.localScale = new Vector3(-1, 1, 1);
                }
            }
        }
    }
}
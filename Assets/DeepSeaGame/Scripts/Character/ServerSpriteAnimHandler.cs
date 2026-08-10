using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace DeepSeaGame
{
    public class ServerSpriteAnimHandler : NetworkBehaviour
    {
        [SerializeField] private AnimationClip _swimMoveClip;
        [SerializeField] private AnimationClip _swimIdleClip;
        [SerializeField] private AnimationClip _groundedIdleClip;
        [SerializeField] private AnimationClip _groundedMoveClip;

        private ServerCharacter _serverCharacter;
        private Animator _animator;

        private void Awake()
        {
            _serverCharacter = transform.root.GetComponent<ServerCharacter>();
            _animator = GetComponent<Animator>();
        }

        public void PlayAnimation(MovementState movementState, Direction direction)
        {
            UpdateSpriteOrientationClientRpc(direction);
            AnimationClip clip = null;

            if(_serverCharacter.StateMachineType == StateMachineType.Player)
            {
                if (movementState == MovementState.Idle)
                {
                    clip = _serverCharacter.CurrentStatus.Value == Status.InWater ? _swimIdleClip : _groundedIdleClip;
                }
                else if (movementState == MovementState.Moving)
                {
                    clip = _serverCharacter.CurrentStatus.Value == Status.InWater ? _swimMoveClip : _groundedMoveClip;
                }
            }
            else
            {
                if (movementState == MovementState.Idle)
                {
                    clip = _swimIdleClip;
                }
                else if (movementState == MovementState.Moving || movementState == MovementState.Pursuing || movementState == MovementState.Knockback || movementState == MovementState.Fleeing)
                {
                    clip = _swimMoveClip;
                }
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
        private void UpdateSpriteOrientationClientRpc(Direction direction)
        {
            bool isPlayer = _serverCharacter.TryGetComponent(out Player player);

            // Flip sprite for West direction
            if (direction == Direction.Left)
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
            else if(direction == Direction.Right)
            {
                if (isPlayer)
                {
                    transform.localScale = new Vector3(1, 1, 1);
                }
                else
                {
                    transform.parent.localScale = new Vector3(1, 1, 1);
                }
            }
        }
    }
}
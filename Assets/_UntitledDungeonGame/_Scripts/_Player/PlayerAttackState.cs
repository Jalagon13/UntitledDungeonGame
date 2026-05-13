using System;
using System.Collections;
using UnityEngine;

namespace UntitledDungeonGame
{
    public class PlayerAttackState : BaseState
    {
        private PlayerStateMachine _ctx;
        private float _swingCd;
        // private ToolItemSO _toolItemSO;
        // private CardinalDirection _swingDirection;

        public PlayerAttackState(AIState key, StateMachine context) : base(key, context)
        {
            IsSuperState = true;
            _ctx = Context as PlayerStateMachine;
        }

        protected override void EnterState(AIStateData stateData)
        {
            // Debug.Log("Player entering swing");
            // _toolItemSO = _ctx.HeldItem as ToolItemSO;
            // _swingCd = _toolItemSO.SwingCooldown;
            // _swingDirection = _ctx.PlayerRef.PlayerHand.AimDirection.Value;

            // float duration = _toolItemSO.SwingDuration;

            // switch (_swingDirection)
            // {
            //     case CardinalDirection.North:
            //         Swing(160, 20, duration, true, CardinalDirection.North);
            //         break;
            //     case CardinalDirection.South:
            //         Swing(340, 200, duration, false, CardinalDirection.South);
            //         break;
            //     case CardinalDirection.West:
            //         Swing(110, 250, duration, false, CardinalDirection.West);
            //         break;
            //     case CardinalDirection.East:
            //         Swing(70, 290, duration, true, CardinalDirection.East);
            //         break;
            // }
        }

        // private void Swing(int startAngle, int endAngle, float duration, bool clockwise, CardinalDirection swingDirection, int swingSpellId = -1)
        // {
        //     // TODO: Melee Collider Data set up here
        //     if (clockwise && endAngle > startAngle) startAngle += 360;
        //     else if (!clockwise && startAngle > endAngle) endAngle += 360;

        //     Quaternion startRotation = Quaternion.Euler(0, 0, startAngle);
        //     Quaternion endRotation = Quaternion.Euler(0, 0, endAngle);

        //     MeleeCollider.SwingData swingData = new()
        //     {
        //         Damage = _toolItemSO.Damage,
        //         Knockback = _toolItemSO.Knockback,
        //         DetectionBetweenHitsDuration = _toolItemSO.DetectionBetweenHitsDuration,
        //         HitSound = _toolItemSO.HitSound,
        //         ColliderLength = _toolItemSO.ColliderLength
        //     };
        //     _ctx.PlayerRef.PlayerHand.SwingDirection.Value = swingDirection;
        //     _ctx.PlayerRef.PlayerHand.PerformSwingClientRpc(startRotation, endRotation, duration, swingDirection);
        //     _ctx.PlayerRef.PlayerHand.MeleeCollider.StartSwing(swingData);
        // }

        public override void UpdateState()
        {

        }

        public override void CheckSwitchStates()
        {
            // if (!_ctx.PlayerRef.PlayerHand.IsSwinging)
            // {
            //     SwitchState(new AIStateData(AIState.Grounded, 0));
            // }
        }

        public override void ExitState()
        {
            // _ctx.SwingCooldownTimer.AddTime(_swingCd);
            // if (_ctx.ServerCharacter.MovementState.Value == MovementState.Idle)
            // {
            //     _ctx.ServerCharacter.CardinalDirection.Value = _swingDirection;
            // }
        }
    }
}

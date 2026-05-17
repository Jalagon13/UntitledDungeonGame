using System;
using System.Collections;
using UnityEngine;

namespace UntitledDungeonGame
{
    public class PlayerAttackState : BaseState
    {
        private PlayerStateMachine _ctx;
        private ToolItemSO _toolItemSO;
        private CardinalDirection _swingDirection;

        public PlayerAttackState(AIState key, StateMachine context) : base(key, context)
        {
            IsSuperState = true;
            _ctx = Context as PlayerStateMachine;
        }

        protected override void EnterState(AIStateData stateData)
        {
            Debug.Log("Player entering swing");
            _toolItemSO = _ctx.HeldItem as ToolItemSO;
            _swingDirection = _ctx.PlayerRef.PlayerArmController.AimDirection.Value;

            ushort toolItemId = GameDataRegistry.Instance.GetItemIdFromItemSO(_toolItemSO);
            float duration = _toolItemSO.SwingDuration;

            switch (_swingDirection)
            {
                case CardinalDirection.North:
                    Swing(160, 20, duration, true, CardinalDirection.North, toolItemId);
                    break;
                case CardinalDirection.South:
                    Swing(340, 200, duration, false, CardinalDirection.South, toolItemId);
                    break;
                case CardinalDirection.West:
                    Swing(110, 250, duration, false, CardinalDirection.West, toolItemId);
                    break;
                case CardinalDirection.East:
                    Swing(70, 290, duration, true, CardinalDirection.East, toolItemId);
                    break;
            }
        }

        private void Swing(int startAngle, int endAngle, float duration, bool clockwise, CardinalDirection swingDirection, ushort toolItemId)
        {
            // TODO: Melee Collider Data set up here
            if (clockwise && endAngle > startAngle) startAngle += 360;
            else if (!clockwise && startAngle > endAngle) endAngle += 360;

            Quaternion startRotation = Quaternion.Euler(0, 0, startAngle);
            Quaternion endRotation = Quaternion.Euler(0, 0, endAngle);

            _ctx.PlayerRef.PlayerArmController.PerformSwing(startRotation, endRotation, duration, swingDirection, toolItemId);
        }

        public override void UpdateState()
        {

        }

        public override void CheckSwitchStates()
        {
            if (!_ctx.PlayerRef.PlayerArmController.IsSwinging)
            {
                SwitchState(new AIStateData(AIState.Grounded, 0));
            }
        }

        public override void ExitState()
        {
            if (_ctx.ServerCharacter.MovementState.Value == MovementState.Idle)
            {
                _ctx.ServerCharacter.CardinalDirection.Value = _swingDirection;
            }
        }
    }
}

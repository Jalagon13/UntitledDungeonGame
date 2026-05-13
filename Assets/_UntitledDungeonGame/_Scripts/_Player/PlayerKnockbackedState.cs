using UnityEngine;

namespace UntitledDungeonGame
{
    public class PlayerKnockbackedState : BaseState
    {
        private PlayerStateMachine _ctx;

        public PlayerKnockbackedState(AIState key, StateMachine context) : base(key, context)
        {
            _ctx = Context as PlayerStateMachine;
        }

        protected override void EnterState(AIStateData stateData)
        {
            // Debug.Log("Player entering knockbacked");
        }

        public override void UpdateState()
        {

        }

        public override void CheckSwitchStates()
        {
            if (_ctx.ServerCharacter.MovementState.Value == MovementState.Idle)
            {
                SwitchState(new AIStateData(AIState.Idle));
            }
            else if (_ctx.ServerCharacter.MovementState.Value == MovementState.Moving)
            {
                SwitchState(new AIStateData(AIState.Moving));
            }
        }

        public override void ExitState()
        {

        }

        public override void ClientEnterState(AIStateData stateData)
        {
            // NTFS: Maybe add client side wind particles here
        }
    }
}

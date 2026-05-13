using UnityEngine;

namespace UntitledDungeonGame
{
    public class PlayerDeadState : BaseState
    {
        private PlayerStateMachine _ctx;

        public PlayerDeadState(AIState key, StateMachine context) : base(key, context)
        {
            IsSuperState = true;
            _ctx = Context as PlayerStateMachine;
        }

        protected override void EnterState(AIStateData stateData)
        {
            Debug.Log("Player entering dead");
            if (_ctx.ServerCharacter.TryGetComponent(out Collider2D collider2D))
            {
                collider2D.enabled = false;
            }

            // _ctx.Character.ClientCharacter.ColliderHolder.gameObject.SetActive(false);
        }

        public override void UpdateState()
        {

        }

        public override void CheckSwitchStates()
        {
            // if (_ctx.Character.LifeState == LifeState.IFrame)
            // {
            //     SwitchState(new AIStateData(AIState.Grounded));
            // }
        }

        public override void ExitState()
        {
            if (_ctx.ServerCharacter.TryGetComponent(out Collider2D collider2D))
            {
                collider2D.enabled = true;
            }

            // _ctx.ServerCharacter.ClientCharacter.ColliderHolder.gameObject.SetActive(true);
        }

        public override void ClientEnterState(AIStateData stateData)
        {
            // NTFS: Player death animations here, just turn off visuals for now
            // _ctx.ServerCharacter.ClientFeedbacks.PlayDeathFeedbacksRpc(stateData.Payload);
        }

        public override void ClientExitState(AIStateData stateData)
        {
            // _ctx.ServerCharacter.ClientCharacter.Visuals.SetActive(true);
        }
    }
}
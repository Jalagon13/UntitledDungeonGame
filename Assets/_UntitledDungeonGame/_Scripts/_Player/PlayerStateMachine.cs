using UnityEngine;

namespace UntitledDungeonGame
{
    public class PlayerStateMachine : StateMachine
    {
        public PlayerStateMachine(ServerCharacter character)
        {
            // This constructor gets played on all client machines
            _serverCharacter = character;

            // Sub States
            _states[AIState.Idle] = new PlayerIdleState(AIState.Idle, this);
            _states[AIState.Moving] = new PlayerMoveState(AIState.Moving, this);
            _states[AIState.Knockbacked] = new PlayerKnockbackedState(AIState.Knockbacked, this);

            // Super States
            _states[AIState.Grounded] = new PlayerGroundedState(AIState.Grounded, this);
            _states[AIState.Attacking] = new PlayerAttackState(AIState.Attacking, this);
            _states[AIState.Dead] = new PlayerDeadState(AIState.Dead, this);

            _currentState = _states[AIState.Grounded];
        }

        public override void ReceiveHP(ServerCharacter inflicter, int amount)
        {
            if (inflicter != null)
            {
                if (amount < 0)
                {
                    // Damaged
                }
                else
                {
                    // Healed
                }
            }
        }
    }
}

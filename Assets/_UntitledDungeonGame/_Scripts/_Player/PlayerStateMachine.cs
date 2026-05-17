using UnityEngine;

namespace UntitledDungeonGame
{
    public class PlayerStateMachine : StateMachine
    {
        private readonly Player _playerRef;
        public Player PlayerRef => _playerRef;

        private ItemSO _heldItem;
        public ItemSO HeldItem => _heldItem;

        public PlayerStateMachine(ServerCharacter character, Player player)
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

            _playerRef = player;
            _playerRef.SelectedItemID.OnValueChanged += OnSelectedItemIDChanged;
        }

        public override void OwnerInitialization()
        {

        }

        public override void Dispose()
        {
            _playerRef.SelectedItemID.OnValueChanged -= OnSelectedItemIDChanged;
        }

        private void OnSelectedItemIDChanged(ushort previousValue, ushort newValue)
        {
            // Played on all client machines for this player instance
            _heldItem = GameDataRegistry.Instance.GetItemSOFromItemId(newValue);
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

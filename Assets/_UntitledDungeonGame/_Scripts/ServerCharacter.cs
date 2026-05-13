using UnityEngine;
using Unity.Netcode;

namespace UntitledDungeonGame
{
    public enum CharacterStateMachine
    {
        Player,
        BasicNpc
    }

    public enum MovementState
    {
        Idle,
        Moving,
        Knockback,
        Pursuing,
        Fleeing
    }

    public class ServerCharacter : NetworkBehaviour
    {
        [SerializeField]
        private CharacterStateMachine _aiType;

        [SerializeField]
        private CharacterDataSO _characterData;
        public CharacterDataSO CharacterData => _characterData;

        [SerializeField]
        private ServerCharacterMovement _serverCharacterMovement;
        public ServerCharacterMovement Movement => _serverCharacterMovement;

        private StateMachine _stateMachine;
        public StateMachine StateMachine => _stateMachine;

        public NetworkVariable<MovementState> MovementState { get; set; } = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        public NetworkVariable<AIStateData> SuperAIState { get; set; } = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        public NetworkVariable<AIStateData> SubAIState { get; set; } = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        private void Awake()
        {
            switch (_aiType)
            {
                case CharacterStateMachine.BasicNpc:
                    // _stateMachine = new BasicNpcStateMachine(this);
                    break;
                case CharacterStateMachine.Player:
                    _stateMachine = new PlayerStateMachine(this);
                    break;
            }
        }

        public override void OnDestroy()
        {
            _stateMachine?.Dispose();
        }

        public override void OnNetworkSpawn()
        {
            if (IsOwner)
            {
                _stateMachine?.OwnerInitialization();
                _stateMachine?.StartStateMachine();
            }
        }

        private void Update()
        {
            if (IsOwner /* || (_characterData.IsNpc && IsServer) */)
            {
                if (_stateMachine != null)
                {
                    _stateMachine.UpdateAI();
                }
            }
        }

        private void FixedUpdate()
        {
            if (IsOwner /* || (_characterData.IsNpc && IsServer) */)
            {
                _serverCharacterMovement.FixedUpdateMovement();
            }
        }
    }
}

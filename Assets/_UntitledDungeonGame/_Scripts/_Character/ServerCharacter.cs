using UnityEngine;
using Unity.Netcode;

namespace UntitledDungeonGame
{
    [RequireComponent(typeof(NetworkHealthState), typeof(NetworkLifeState))]
    public class ServerCharacter : NetworkBehaviour
    {
        [SerializeField]
        private CharacterStateMachine _aiType;

        [SerializeField]
        private CharacterSO _characterData;
        public CharacterSO CharacterData => _characterData;

        [SerializeField]
        private ServerCharacterMovement _serverCharacterMovement;
        public ServerCharacterMovement Movement => _serverCharacterMovement;

        [SerializeField]
        private ClientCharacter _clientCharacter;
        public ClientCharacter ClientCharacter => _clientCharacter;

        public NetworkHealthState NetHealthState { get; private set; }
        public int HitPoints
        {
            get => NetHealthState.HitPoints.Value;
            private set => NetHealthState.HitPoints.Value = value;
        }

        public NetworkLifeState NetLifeState { get; private set; }
        public LifeState LifeState
        {
            get => NetLifeState.LifeState.Value;
            private set => NetLifeState.LifeState.Value = value;
        }


        private StateMachine _stateMachine;
        public StateMachine StateMachine => _stateMachine;

        public NetworkVariable<MovementState> MovementState { get; set; } = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        public NetworkVariable<CardinalDirection> CardinalDirection { get; set; } = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        public NetworkVariable<AIStateData> SuperAIState { get; set; } = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        public NetworkVariable<AIStateData> SubAIState { get; set; } = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        private void Awake()
        {
            NetHealthState = GetComponent<NetworkHealthState>();
            NetLifeState = GetComponent<NetworkLifeState>();

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

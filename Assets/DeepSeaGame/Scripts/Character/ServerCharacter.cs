using UnityEngine;
using Unity.Netcode;
using System;
using System.Collections;

namespace DeepSeaGame
{
    [RequireComponent(typeof(NetworkHealthState), typeof(DamageReceiver), typeof(GridCollider))]
    public class ServerCharacter : NetworkBehaviour
    {
        [SerializeField] private StateMachineType _stateMachineType;
        public StateMachineType StateMachineType => _stateMachineType;
    
        [SerializeField] private CharacterSO _characterData;
        public CharacterSO CharacterData => _characterData;

        [SerializeField] private CharacterMovement _characterMovement;
        public CharacterMovement Movement => _characterMovement;

        [SerializeField] private ClientCharacter _clientCharacter;
        public ClientCharacter ClientCharacter => _clientCharacter;

        [SerializeField] private ClientCharacterFeedbacks _clientFeedbacks;
        public ClientCharacterFeedbacks ClientFeedbacks => _clientFeedbacks;

        public NetworkHealthState NetHealthState { get; private set; }
        public int HitPoints
        {
            get => NetHealthState.HitPoints.Value;
            private set => NetHealthState.HitPoints.Value = value;
        }

        public LifeState LifeState
        {
            get => NetHealthState.LifeState.Value;
            private set => NetHealthState.LifeState.Value = value;
        }
        
        // public BuffHandler BuffHandler { get; private set; }

        private CharacterStats _characterStats;
        public CharacterStats Stats => _characterStats;

        protected StateMachine _stateMachine;
        public StateMachine StateMachine => _stateMachine;

        private DamageReceiver _damageReceiver;
        public DamageReceiver DamageReceiver => _damageReceiver;

        private ServerCharacter _inflicter;
        public ServerCharacter Inflicter => _inflicter;
        
        private Vector2 _inflicterToTargetDirection;
        public Vector2 InflicterToTargetDirection => _inflicterToTargetDirection;
        
        private GridCollider _gridCollider;
        public GridCollider GridCollider => _gridCollider;

        private float _knockbackForceFromInflicter;
        public float KnockbackForceFromInflicter => _knockbackForceFromInflicter;

        public NetworkVariable<MovementState> MovementState { get; set; } = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        public NetworkVariable<Direction> CurrentDirection { get; set; } = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        public NetworkVariable<AIStateData> SuperAIState { get; set; } = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        public NetworkVariable<AIStateData> SubAIState { get; set; } = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        public NetworkVariable<Status> CurrentStatus { get; set; } = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        protected virtual void Awake()
        {
            _damageReceiver = GetComponent<DamageReceiver>();
            _gridCollider = GetComponent<GridCollider>();
            _characterStats = new(_characterData);

            NetHealthState = GetComponent<NetworkHealthState>();
            
            _stateMachine = StateMachineFactory.CreateStateMachine(this, _stateMachineType);
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                HitPoints = _characterStats.MaxHealth.GetValue();

                _damageReceiver.HpReceived += ReceiveHP;
            }
        }

        protected override void OnNetworkPostSpawn()
        {
            _stateMachine = StateMachineFactory.CreateStateMachine(this, _stateMachineType);
            _stateMachine?.OwnerInitialization();
            _stateMachine?.StartStateMachine();
        }

        public override void OnDestroy()
        {
            _stateMachine?.Dispose();
        }

        public override void OnNetworkDespawn()
        {
            if(IsServer)
            {
                _damageReceiver.HpReceived -= ReceiveHP;
            }
        }

        private void Update()
        {
            if (IsOwner /* || (_characterData.IsNpc && IsServer) */)
            {
                if (_stateMachine != null)
                {
                    _characterStats.TickBuffs(Time.deltaTime);
                    _stateMachine.UpdateAI();
                }
            }
        }

        private void FixedUpdate()
        {
            if (IsOwner /* || (_characterData.IsNpc && IsServer) */)
            {
                Vector2Int gridPos = new Vector2Int(Mathf.FloorToInt(transform.position.x), Mathf.FloorToInt(transform.position.y + 1));
                WorldDataStore worldData = WorldManager.Instance.WorldDataStore;
                bool isInAir = worldData.IsUnderwaterAirAt(gridPos.x, gridPos.y);
                CurrentStatus.Value = isInAir ? Status.InAir : Status.InWater;

                _characterMovement.FixedUpdateMovement();
            }
        }

        private void ReceiveHP(object sender, DamageReceiver.HpReceivedEventArgs e)
        {
            Debug.Log($"{gameObject.name}: Receiving HP: {e.HpReceived}");
            if (LifeState == LifeState.Dead) return;
            
            _inflicter = e.Inflicter;
            int hpReceived = e.HpReceived;

            if (_inflicter != null)
            {
                _inflicterToTargetDirection = (Vector2)(transform.position - _inflicter.transform.position).normalized;
                _knockbackForceFromInflicter = e.KnockbackForce;
            }
            else
            {
                _inflicterToTargetDirection = Vector2.zero;
                _knockbackForceFromInflicter = 0f;
            }

            if (hpReceived > 0)
            {
                // HP healing mod functionality here
                float healingMod = 1f;
                hpReceived = (int)(hpReceived * healingMod);
            }
            else
            {
                if (LifeState == LifeState.IFrame)
                    return;
                
                // Damage reduction mod functionality here
                if (hpReceived + _characterStats.Defense.GetValue() > -1)
                {
                    hpReceived = -1;
                }
                else
                {
                    float difficultyMult = 0.5f; // Placeholder for difficulty multiplier, 0.5 for normal, 0.75 for hard, 1 for insane TENT mults
                    hpReceived += Mathf.RoundToInt((int)(_characterStats.Defense.GetValue() * difficultyMult));
                }

                // Play damage numbers on client
                if(_clientFeedbacks != null)
                {
                    _clientFeedbacks.PlayDamageNumbersRpc(hpReceived);

                    // If not dead after taking damage, play character damaged feedbacks
                    if (HitPoints + hpReceived > 0 || !_characterData.CanDie)
                    {
                        _clientFeedbacks.PlayDamageFeedbacksRpc(_inflicterToTargetDirection);
                    }
                }

                if (_characterData.CanBeKnockedBack && e.PlayKnockback && _inflicter != null && _characterMovement != null)
                {
                    _characterMovement.StartKnockback(_inflicter.transform.position, e.KnockbackForce);
                }

                if (HitPoints + hpReceived > 0)
                {
                    StartCoroutine(StartIFrameTimer());
                }
            }
            
            HitPoints = Mathf.Clamp(HitPoints + hpReceived, 0, _characterData.BaseMaxHealth);
            _stateMachine?.ReceiveHP(_inflicter, hpReceived);
            Debug.Log($"[{gameObject.name}] CurrentHP: {HitPoints}");

            if (HitPoints <= 0 && _characterData.CanDie)
            {
                Debug.Log($"[{gameObject.name}] DEAD");
                LifeState = LifeState.Dead;

                if (_characterData.IsNpc)
                {
                    LootTable.SpawnLoot(_characterData.LootDrops, transform.position);
                }
            }
        }

        public IEnumerator StartIFrameTimer()
        {
            LifeState = LifeState.IFrame;
            yield return new WaitForSeconds(_characterData.IFrameDuration);
            LifeState = LifeState.Alive;
        }
    }
}

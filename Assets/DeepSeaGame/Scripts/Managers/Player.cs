using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DeepSeaGame
{
    [RequireComponent(typeof(ServerCharacter))]
    public class Player : NetworkBehaviour
    {
        public static Player Instance { get; private set; }
        
        public static event EventHandler<PlayerIdEventArgs> OnAnyPlayerSpawned;
        public class PlayerIdEventArgs : EventArgs
        {
            public ulong PlayerId;
        }

        [SerializeField] private BoxCollider2D _playerCollider;
        public BoxCollider2D PlayerCollider => _playerCollider;

        private ServerCharacter _character;
        public ServerCharacter Character => _character;
        
        private PlayerArmController _playerArmController;
        public PlayerArmController PlayerArmController => _playerArmController;
        
        private FlashlightController _flashlightController;
        public FlashlightController FlashlightController => _flashlightController;

        public NetworkVariable<ushort> SelectedItemID { get; private set; } = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        public Vector3 PlayerCenter => transform.position + new Vector3(0f, _playerCollider.offset.y, 0f);

        [HideInInspector]
        public Vector2 SpawnPoint;
        
        [Header("Buffs")]
        [SerializeField] private BuffSO _inAirHealthBuffSO;
        [SerializeField] private BuffSO _inWaterHealthBuffSO;

        private Buff _inAirHeathBuff;
        private Buff _inWaterHealthBuff;

        private void Awake()
        {
            _character = GetComponent<ServerCharacter>();
            _playerArmController = GetComponent<PlayerArmController>();
            _flashlightController = GetComponent<FlashlightController>();

            // Create runtime instances from SOs if provided, otherwise fall back to defaults.
            _inAirHeathBuff = _inAirHealthBuffSO != null ? _inAirHealthBuffSO.CreateBuffInstance() : new("InAirHealthBuff", StatType.MaxHealth, percentAmount: 0.5f);
            _inWaterHealthBuff = _inWaterHealthBuffSO != null ? _inWaterHealthBuffSO.CreateBuffInstance() : new("InWaterHealthBuff", StatType.MaxHealth, percentAmount: 0.5f, duration: 5f);
        }

        public override void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            if (GameInput.Instance != null)
            {
                GameInput.Instance.OnMove -= GameInput_OnMove;
                GameInput.Instance.OnJump -= GameInput_OnJump;
            }
            
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.OnInventoryOpenChanged -= OnInventoryOpenChanged;
                InventoryManager.Instance.OnSelectedHotbarSlotChanged -= OnSelectedHotbarSlotChanged;
            }

            if (Character != null)
            {
                Character.CurrentStatus.OnValueChanged -= OnCurrentStatusChanged;
            }
        }

        public void OnNetworkSpawnLocalClientInitializations()
        {
            Instance = this;

            OnAnyPlayerSpawned?.Invoke(this, new PlayerIdEventArgs
            {
                PlayerId = OwnerClientId
            });

            // local player start up code here, maybe input
            GameInput.Instance.OnMove += GameInput_OnMove;
            GameInput.Instance.OnJump += GameInput_OnJump;
            InventoryManager.Instance.OnInventoryOpenChanged += OnInventoryOpenChanged;
            InventoryManager.Instance.OnSelectedHotbarSlotChanged += OnSelectedHotbarSlotChanged;
            Character.CurrentStatus.OnValueChanged += OnCurrentStatusChanged;
            SyncSelectedItemToCurrentHotbarSelection();
        }

        private void OnCurrentStatusChanged(Status previousValue, Status newValue)
        {
            if(previousValue == Status.InWater && newValue == Status.InAir)
            {
                Character.Stats.StopBuff(_inWaterHealthBuff);
                Character.Stats.StartBuff(_inAirHeathBuff);
                
                AudioManager.Instance.PlayOneShot(FMODEvents.Instance.OxygenReplenishSFX, transform.position);
            }
            else if (previousValue == Status.InAir && newValue == Status.InWater)
            {
                Character.Stats.StopBuff(_inAirHeathBuff);
                Character.Stats.StartBuff(_inWaterHealthBuff, false);
            }
        }

        private void GameInput_OnJump(object sender, InputAction.CallbackContext e)
        {
            if (_character == null || !_character.IsOwner)
            {
                return;
            }
            
            PlayerCharacterMovement playerCharMove = _character.Movement as PlayerCharacterMovement;
            playerCharMove?.ReceiveJumpInput();
        }

        private void GameInput_OnMove(object sender, InputAction.CallbackContext context)
        {
            if (_character == null || !_character.IsOwner)
            {
                return;
            }

            _character.Movement?.ReceiveMoveInput(GameInput.Instance.MoveInput);
        }

        private void OnSelectedHotbarSlotChanged(int slotIndex, InventoryStack selectedHotbarSlotStack)
        {
            if (IsOwner)
            {
                ushort hotBatSlotStackItemID = GameDataRegistry.Instance.GetItemIdFromItemSO(selectedHotbarSlotStack.Item);
                if (SelectedItemID.Value == hotBatSlotStackItemID || hotBatSlotStackItemID == GameDataRegistry.INVALID_ID)
                {
                    return;
                }
                // Debug.Log($"Changed Selected Item ID: {hotBatSlotStackItemID}");
                SelectedItemID.Value = hotBatSlotStackItemID;
            }
        }

        private void SyncSelectedItemToCurrentHotbarSelection()
        {
            if (!IsOwner || InventoryManager.Instance == null)
            {
                return;
            }

            ushort hotbarItemId = GameDataRegistry.Instance.GetItemIdFromItemSO(InventoryManager.Instance.SelectedHotbarStack.Item);
            if (SelectedItemID.Value != hotbarItemId)
            {
                SelectedItemID.Value = hotbarItemId;
            }
        }

        private void OnInventoryOpenChanged(bool isOpen)
        {
            _character.Movement?.ReceiveMoveInput(Vector2.zero);
            
            if(!isOpen)
            {
                _character.Movement?.ReceiveMoveInput(GameInput.Instance.MoveInput);
            }
        }

        public void Respawn()
        {
            transform.SetPositionAndRotation(SpawnPoint, Quaternion.identity);
            StartCoroutine(_character.StartIFrameTimer());
            _character.DamageReceiver.ReceiveHP(_character, _character.Stats.MaxHealth.GetValue(), false);
        }
    }
}

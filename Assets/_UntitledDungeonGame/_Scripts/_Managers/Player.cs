using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace UntitledDungeonGame
{
    [RequireComponent(typeof(ServerCharacter), typeof(PlayerArmController))]
    public class Player : NetworkBehaviour
    {
        public static event EventHandler<PlayerIdEventArgs> OnAnyPlayerSpawned;
        public class PlayerIdEventArgs : EventArgs
        {
            public ulong PlayerId;
        }
        
        public static Player Instance { get; private set; }

        private ServerCharacter _character;
        public ServerCharacter Character => _character;
        
        private PlayerArmController _playerArmController;
        public PlayerArmController PlayerArmController => _playerArmController;

        public NetworkVariable<ushort> SelectedItemID { get; private set; } = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        
        
        private CapsuleCollider2D _playerCollider;
        public CapsuleCollider2D PlayerCollider => _playerCollider;

        private void Awake()
        {
            _character = GetComponent<ServerCharacter>();
            _playerArmController = GetComponent<PlayerArmController>();
            _playerCollider = GetComponent<CapsuleCollider2D>();
        }

        public override void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            GameInput.Instance.OnMove -= GameInput_OnMove;
            InventoryManager.Instance.OnInventoryOpenChanged -= OnInventoryOpenChanged;
            InventoryManager.Instance.OnSelectedHotbarSlotChanged -= OnSelectedHotbarSlotChanged;
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
            InventoryManager.Instance.OnInventoryOpenChanged += OnInventoryOpenChanged;
            InventoryManager.Instance.OnSelectedHotbarSlotChanged += OnSelectedHotbarSlotChanged;
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
                Debug.Log($"Changed Selected Item ID: {hotBatSlotStackItemID}");
                SelectedItemID.Value = hotBatSlotStackItemID;
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

        private void GameInput_OnMove(object sender, InputAction.CallbackContext context)
        {
            if (_character == null || !_character.IsOwner)
            {
                return;
            }

            _character.Movement?.ReceiveMoveInput(GameInput.Instance.MoveInput);
        }
    }
}

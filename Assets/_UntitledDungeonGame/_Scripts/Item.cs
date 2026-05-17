using System.Collections;
using Unity.Netcode;
using UnityEngine;

namespace UntitledDungeonGame
{
    public class Item : NetworkBehaviour
    {
        [SerializeField] private float _attractRange = 2.75f;
        [SerializeField] private float _attractSpeed = 5f;
        [SerializeField] private float _turnSharpness = 5f;
        [SerializeField] private float _initialCollectDelay = 0.5f;
        
        private SpriteRenderer _sr;
        private Rigidbody2D _rb;
        private bool _canCollect, _itemCollected;
        private Vector2 _direction;
        private Vector2 _velocity;
        private Collider2D _itemCollider;

        private NetworkVariable<SyncItemData> _syncItemDataNetworkVariable = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        private void Awake()
        {
            _sr = transform.GetChild(0).GetComponent<SpriteRenderer>();
            _rb = GetComponent<Rigidbody2D>();
        }

        private IEnumerator Start()
        {
            yield return new WaitForSeconds(_initialCollectDelay);
            _canCollect = true;
        }
        
        private void FixedUpdate()
        {
            if (_itemCollected) return;
            
            Player closestPlayer = null;
            float closestDist = Mathf.Infinity;

            foreach (var clientId in NetworkManager.ConnectedClientsIds)
            {
                Player player = NetworkManager.ConnectedClients[clientId].PlayerObject.GetComponent<Player>();
                float dist = Vector2.Distance(transform.position, player.transform.position);

                if (dist < closestDist && dist < _attractRange)
                {
                    closestPlayer = player;
                    closestDist = dist;
                }
            }

            if (closestPlayer != null && _canCollect)
            {
                Vector2 currentPosition = _rb.position;
                Vector2 targetPosition = closestPlayer.transform.position;
                _direction = (targetPosition - currentPosition).normalized;
                _velocity = Vector2.Lerp(_velocity, _direction * _attractSpeed, _turnSharpness * Time.fixedDeltaTime);
                _rb.linearVelocity = _velocity;

                // Check if the item is within the bounds of any CollectTag collider
                if (Vector2.Distance(currentPosition, targetPosition) < 0.25f)
                {
                    if (/* closestValidCollectCollider.transform.root.GetComponent<Player>().OwnerClientId == NetworkManager.LocalClientId && */ _canCollect && !_itemCollected /* && !InventoryFull() */)
                    {
                        _itemCollected = true;

                        AddItemClientRpc(_syncItemDataNetworkVariable.Value, RpcTarget.Single(closestPlayer.GetComponent<Player>().OwnerClientId, RpcTargetUse.Persistent));
                        return;
                    }
                }
            }


        }

        public void Initialize(SyncItemData syncItemData)
        {
            _syncItemDataNetworkVariable.Value = syncItemData;
            _itemCollider = GetComponent<Collider2D>();

            UpdateItemDataAndVisuals();
        }

        private void UpdateItemDataAndVisuals()
        {
            ItemSO itemSO = GameDataRegistry.Instance.GetItemSOFromItemId(_syncItemDataNetworkVariable.Value.ItemId);

            _sr.sprite = itemSO.InventoryIcon;
            gameObject.name = $"Item_{itemSO.ItemName}";
        }

        [Rpc(SendTo.SpecifiedInParams)]
        private void AddItemClientRpc(SyncItemData syncItemData, RpcParams rpcParams = default)
        {
            ItemSO itemSO = GameDataRegistry.Instance.GetItemSOFromItemId(syncItemData.ItemId);

            InventoryStack stack = new(itemSO, syncItemData.Quantity);

            InventoryManager.Instance.AddItem(stack.Item, stack.Amount);
            GameManager.Instance.DestroyItemServerRpc(NetworkObject);
        }

    }
}

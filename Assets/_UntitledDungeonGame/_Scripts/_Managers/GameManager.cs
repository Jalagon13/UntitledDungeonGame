using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace UntitledDungeonGame
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        public static Vector2 MouseWorldPosition { get; private set; }

        [SerializeField] private GameObject _itemBasePrefab;

        private void Awake()
        {
            Instance = this;
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
        
        private void Update()
        {
            MouseWorldPosition = (Vector2)Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        }

        private void OnSceneLoaded(Scene arg0, LoadSceneMode arg1)
        {
            if (Loader.IsHost)
            {
                Debug.Log($"Starting game as host");
                NetworkManager.Singleton.StartHost();
            }
            else
            {
                Debug.Log($"Starting game as client");
                NetworkManager.Singleton.StartClient();
            }
        }

        public void SpawnItem(InventoryStack stack, Vector2 spawnPos)
        {
            if (stack == null)
            {
                Debug.LogWarning($"Warning, item can't be spawned because it is null");
                return;
            }

            SyncItemData syncItemData = new SyncItemData
            {
                ItemId = GameDataRegistry.Instance.GetItemIdFromItemSO(stack.Item),
                Quantity = (ushort)stack.Amount,
            };

            SpawnItemServerRpc(syncItemData, spawnPos);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void SpawnItemServerRpc(SyncItemData syncItemData, Vector2 spawnPos)
        {
            GameObject itemGameObject = Instantiate(_itemBasePrefab, spawnPos, Quaternion.identity);

            NetworkObject itemNetworkObject = itemGameObject.GetComponent<NetworkObject>();
            itemNetworkObject.SpawnWithObservers = false;
            itemNetworkObject.Spawn(true);

            Item item = itemGameObject.GetComponent<Item>();
            item.Initialize(syncItemData);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        public void DestroyItemServerRpc(NetworkObjectReference itemNetworkObjectReference)
        {
            itemNetworkObjectReference.TryGet(out NetworkObject itemNetworkObject);
            Item item = itemNetworkObject.GetComponent<Item>();

            Destroy(item.gameObject);
        }
    }
}

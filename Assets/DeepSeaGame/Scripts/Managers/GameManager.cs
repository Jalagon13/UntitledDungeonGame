using System;
using MoreMountains.Feedbacks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace DeepSeaGame
{
    public class GameManager : MonoBehaviour
    {
        public Action OnPrototypeEnd;
    
        public static GameManager Instance { get; private set; }
        public static Vector2 MouseWorldPosition { get; private set; }
        public static Vector2Int MouseTilePosition { get; private set; }

        [SerializeField] private GameObject _itemBasePrefab;
        [SerializeField] private MMF_Player _damageNumbersFeedback;

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
            MouseTilePosition = Vector2Int.FloorToInt(MouseWorldPosition);
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
        
        public void EndPrototype()
        {
            OnPrototypeEnd?.Invoke();
        }

        public void SpawnWorldText(string text, Vector2 position, Color color, float fontSize = 1f, float duration = 0.5f)
        {
            _damageNumbersFeedback.enabled = true;
            MMF_FloatingText floatingText = _damageNumbersFeedback.GetFeedbackOfType<MMF_FloatingText>();
            
            Gradient gradient;
            GradientColorKey[] colorKey;
            GradientAlphaKey[] alphaKey;

            floatingText.Value = text;
            // use Intensity to control scale/font size (spawner must be configured to use intensity for scale)
            floatingText.Intensity = fontSize;
            // force a custom lifetime for this floating text
            floatingText.ForceLifetime = true;
            floatingText.Lifetime = duration;

            // we setup some fancy colors
            gradient = new Gradient();
            
            // Populate the color keys at the relative time 0 and 1 (0 and 100%). Can be used for custom gradients later
            colorKey = new GradientColorKey[2];
            colorKey[0].color = color;
            colorKey[0].time = 0.0f;
            colorKey[1].color = color;
            colorKey[1].time = 1.0f;
            
            // Populate the alpha  keys at relative time 0 and 1  (0 and 100%)
            alphaKey = new GradientAlphaKey[2];
            alphaKey[0].alpha = 0.0f;
            alphaKey[0].time = 0.0f;
            alphaKey[1].alpha = 1.0f;
            alphaKey[1].time = 1.0f;
            
            gradient.SetKeys(colorKey, alphaKey);

            floatingText.ForceColor = true;
            floatingText.AnimateColorGradient = gradient;

            _damageNumbersFeedback.transform.position = position;
            _damageNumbersFeedback.PlayFeedbacks(position);

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

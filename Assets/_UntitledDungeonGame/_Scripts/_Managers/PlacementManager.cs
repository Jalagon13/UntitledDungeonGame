using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace UntitledDungeonGame
{
    public class PlacementManager : MonoBehaviour
    {
        public static PlacementManager Instance { get; private set; }

        [SerializeField]
        private float _placementRange = 3f;
        public float PlacementRange => _placementRange;

        [SerializeField]
        private LayerMask _resourceLayer;

        [Header("Ghost Settings")]
        [SerializeField] private Color _ghostColorValid = Color.green;
        [SerializeField] private Color _ghostColorInvalid = Color.red;
        

        private PlacingState _placingState;
        public PlacingState PlacingState => _placingState;

        private PlaceableItemSO _currentPlaceable;
        public PlaceableItemSO CurrentPlaceable => _currentPlaceable;

        private PlaceableItemSO _previousStructureItemSO = null;

        private GameObject _ghostPlaceableGameObject;
        private bool _isGhostInValidPosition = false;

        private void Awake()
        {
            Instance = this;
        }
        
        private void Start()
        {
            GameInput.Instance.OnPrimaryActionStarted += OnPrimaryActionStarted;
            InventoryManager.Instance.OnSelectedHotbarSlotChanged += OnSelectedHotbarSlotChanged;
        }
        
        private void OnDestroy()
        {
            GameInput.Instance.OnPrimaryActionStarted -= OnPrimaryActionStarted;
            InventoryManager.Instance.OnSelectedHotbarSlotChanged -= OnSelectedHotbarSlotChanged;
        }
        
        private void Update()
        {
            if(_currentPlaceable != null)
            {
                GhostPlaceableHandle();
                
                if(_placingState == PlacingState.Placing)
                {
                    TryToPlacePlaceable();
                }
            }
            else if (_ghostPlaceableGameObject != null)
            {
                Destroy(_ghostPlaceableGameObject);
                _ghostPlaceableGameObject = null;
            }
        }

        private void TryToPlacePlaceable()
        {
            if (_ghostPlaceableGameObject != null && _isGhostInValidPosition)
            {
                Instantiate(_currentPlaceable.PlaceablePrefab, _ghostPlaceableGameObject.transform.position, _ghostPlaceableGameObject.transform.rotation);

                Destroy(_ghostPlaceableGameObject);
                _ghostPlaceableGameObject = null;
                
                InventoryManager.Instance.SubtractOneFromHotbarSelectedSlot();
            }
        }

        private void GhostPlaceableHandle()
        {
            CreateGhostPlaceablePrefab();
            MoveGhostPrefabToMousePosition();
            CheckBuildValidity();
        }

        private void CreateGhostPlaceablePrefab()
        {
            if (_ghostPlaceableGameObject == null || _currentPlaceable != _previousStructureItemSO)
            {
                if (_ghostPlaceableGameObject != null)
                {
                    Destroy(_ghostPlaceableGameObject);
                }

                _ghostPlaceableGameObject = Instantiate(_currentPlaceable.PlaceablePrefab.gameObject);
                _ghostPlaceableGameObject.name = $"Ghost_{_ghostPlaceableGameObject.name}";
                
                // Set placeable ghost material here and disable any colliders
                GhostifyPlaceable(_ghostColorInvalid);

                _previousStructureItemSO = _currentPlaceable;
            }
        }

        private void MoveGhostPrefabToMousePosition()
        {
            if (_ghostPlaceableGameObject == null) return;
            
            if(PlayerWithinPlacingRangeOfMouse())
            {
                _ghostPlaceableGameObject.transform.position = GameManager.MouseTilePosition;
            }
            else
            {
                Vector2 playerPosition = Player.Instance.transform.position;
                Vector2 direction = (GameManager.MouseWorldPosition - playerPosition).normalized;
                Vector2 endPoint = playerPosition + Player.Instance.PlayerCollider.offset + direction * _placementRange;
                _ghostPlaceableGameObject.transform.position = Vector3Int.FloorToInt(endPoint);
            }
            
        }

        private void CheckBuildValidity()
        {
            if (_ghostPlaceableGameObject == null) return;

            BoxCollider2D boxCollider2D = _ghostPlaceableGameObject.transform.GetComponent<BoxCollider2D>();

            if (boxCollider2D != null)
            {
                Vector3 worldCenter = boxCollider2D.transform.TransformPoint(boxCollider2D.offset);
                float tinyOffset = 0.05f; // Used to make the box slightly smaller because if not, if you try to place it next to a resource it will still detect it and consider it invalid position
                Vector2 size = new(boxCollider2D.size.x - tinyOffset, boxCollider2D.size.y - tinyOffset);
                
                Collider2D[] colliders = Physics2D.OverlapBoxAll(worldCenter, size, 0);
                foreach (Collider2D collider in colliders)
                {
                    if (collider.gameObject == _ghostPlaceableGameObject) continue;
                    
                    // The the collider is hovering over a resource, set it as not a valid position
                    if(((1 << collider.gameObject.layer) & _resourceLayer) != 0)
                    {
                        GhostifyPlaceable(_ghostColorInvalid);
                        _isGhostInValidPosition = false;
                        return;
                    }
                    
                    // Create the same thing for wall colliders
                }
            }
            
            GhostifyPlaceable(_ghostColorValid);
            _isGhostInValidPosition = true;
        }

        private void GhostifyPlaceable(Color color)
        {
            foreach (SpriteRenderer spriteRenderer in _ghostPlaceableGameObject.GetComponentsInChildren<SpriteRenderer>())
            {
                spriteRenderer.color = color;
            }

            foreach (Collider2D modelCollider in _ghostPlaceableGameObject.GetComponentsInChildren<Collider2D>())
            {
                modelCollider.enabled = false;
            }
        }

        private void OnSelectedHotbarSlotChanged(int arg1, InventoryStack stack)
        {
            if (!stack.IsEmpty && stack.Item is PlaceableItemSO placeableItemSO)
            {
                _currentPlaceable = placeableItemSO;
            }
            else
            {
                _currentPlaceable = null;
            }
        }

        private void OnPrimaryActionStarted(object sender, InputAction.CallbackContext e)
        {
            if(_currentPlaceable == null)
            {
                _placingState = PlacingState.Idle;
                return;
            }
        
            PlacingState newState = (e.started || e.performed) ? PlacingState.Placing : PlacingState.Idle;

            if (_placingState == newState) return;

            _placingState = newState;

            Debug.Log($"Placing State: {_placingState}");
        }

        private bool PlayerWithinPlacingRangeOfMouse()
        {
            return Vector2.Distance(Player.Instance.transform.position, GameManager.MouseWorldPosition) <= _placementRange;
        }
    }
}

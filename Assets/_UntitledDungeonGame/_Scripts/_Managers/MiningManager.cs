using System;
using System.Collections;
using SingularityGroup.HotReload;
using UnityEngine;
using UnityEngine.InputSystem;

namespace UntitledDungeonGame
{
    public class MiningManager : MonoBehaviour
    {
        public static MiningManager Instance { get; private set; }
        
        [SerializeField]
        private float _miningRange = 3f;
        public float MiningRange => _miningRange;

        [SerializeField]
        private float _timeBetweenMiningSounds = 0.225f;

        [SerializeField]
        private LayerMask _resourceLayer;

        private readonly Collider2D[] _hoverResults = new Collider2D[8];
        private ContactFilter2D _resourceHoverFilter;

        public ResourceObject HoveredResource { get; private set; }
        public bool IsHoveringResource => HoveredResource != null;
        
        private MiningState _miningState;
        public MiningState MiningState => _miningState;

        private Coroutine _currentMiningCoroutine;
        
        private ToolItemSO _currentTool;
        public ToolItemSO CurrentTool => _currentTool;


        private void Awake()
        {
            Instance = this;

            _resourceHoverFilter = new ContactFilter2D
            {
                useLayerMask = true,
                layerMask = _resourceLayer,
                useTriggers = true
            };
        }
        
        private void Start()
        {
            GameInput.Instance.OnPrimaryActionStarted += OnPrimaryActionStarted;
            InventoryManager.Instance.OnSelectedHotbarSlotChanged += OnSelectedHotbarSlotChanged;
        }
        
        private void OnDestroy()
        {
            if(GameInput.Instance != null)
            {
                GameInput.Instance.OnPrimaryActionStarted -= OnPrimaryActionStarted;
                InventoryManager.Instance.OnSelectedHotbarSlotChanged -= OnSelectedHotbarSlotChanged;
            }
        }

        private void Update()
        {
            UpdateHoveredResource();
            TryToMineResource();
        }

        private void OnSelectedHotbarSlotChanged(int arg1, InventoryStack stack)
        {
            if(!stack.IsEmpty && stack.Item is ToolItemSO toolItemSO)
            {
                _currentTool = toolItemSO;
            }
            else
            {
                _currentTool = null;
            }
        }

        private void OnPrimaryActionStarted(object sender, InputAction.CallbackContext e)
        {
            MiningState newState = (e.started || e.performed) ? MiningState.Detecting : MiningState.Idle;

            if (_miningState == newState) return;
            
            _miningState = newState;
            
            if(_miningState == MiningState.Idle && _currentMiningCoroutine != null)
            {
                StopMiningRoutine();
            }
            
            Debug.Log($"Mining State: {_miningState}");
        }

        private void UpdateHoveredResource()
        {
            HoveredResource = null;

            int hitCount = Physics2D.OverlapPoint(GameManager.MouseWorldPosition, _resourceHoverFilter, _hoverResults);

            for (int i = 0; i < hitCount; i++)
            {
                Collider2D hitCollider = _hoverResults[i];
                if (hitCollider == null)
                {
                    continue;
                }

                if (hitCollider.TryGetComponent(out ResourceObject resourceObject))
                {
                    HoveredResource = resourceObject;
                    // Debug.Log($"Hovering {HoveredResource.name}");
                    return;
                }

                HoveredResource = hitCollider.GetComponentInParent<ResourceObject>();
                if (HoveredResource != null)
                {
                    // Debug.Log($"Hovering {HoveredResource.name}");
                    return;
                }
            }
        }

        private void TryToMineResource()
        {
            if(_miningState != MiningState.Detecting || HoveredResource == null || _currentMiningCoroutine != null || _currentTool == null || _currentTool.HarvestType != HoveredResource.Data.HarvestType || !PlayerWithinMiningRangeOfMouse()) return;

            _currentMiningCoroutine = StartCoroutine(MiningRoutine());
        }

        private IEnumerator MiningRoutine()
        {
            float totalTicks = HoveredResource.Data.Hardness * 30f / Mathf.Max(_currentTool.MiningPower, 0.1f);
            float totalMiningTime = totalTicks * 0.05f;
            float elapsedTime = 0f;
            float nextSoundTime = _timeBetweenMiningSounds;

            PlayMiningSound();

            while (elapsedTime < totalMiningTime)
            {
                if(_currentTool == null)
                {
                    StopMiningRoutine();
                    yield break;
                }

                elapsedTime += Time.deltaTime;

                // Play mining sounds at intervals
                if (elapsedTime >= nextSoundTime)
                {
                    PlayMiningSound();
                    nextSoundTime += _timeBetweenMiningSounds;
                }

                yield return null;
            }
            
            HandleDestruction();
        }

        private void HandleDestruction()
        {
            HoveredResource.Destroy();
            StopMiningRoutine();
        }

        private void PlayMiningSound()
        {
            
        }
        
        private void StopMiningRoutine()
        {
            StopCoroutine(_currentMiningCoroutine);
            _currentMiningCoroutine = null;
        }

        private bool PlayerWithinMiningRangeOfMouse()
        {
            return Vector2.Distance(Player.Instance.transform.position, GameManager.MouseWorldPosition) <= _miningRange;
        }
    }
}

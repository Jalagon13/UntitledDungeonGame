using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace UntitledDungeonGame
{
    public class InventoryManager : MonoBehaviour
    {
        public static InventoryManager Instance { get; private set; }

        public event Action<int, InventoryStack> OnSelectedHotbarSlotChanged;
        public event Action OnInventoryChanged;
        public event Action<bool> OnInventoryOpenChanged;
        public event Action<InventoryStack> OnCursorStackChanged;
        public event Action<ItemSO, int> OnItemPickup;

        private const int _minimumHotbarSlotCount = 1;
        private const int _minimumTotalSlotCount = 8;

        [SerializeField] private CraftingMenuUI _craftingMenuUI;
        public CraftingMenuUI CraftingMenuUI => _craftingMenuUI;


        [Header("Inventory Layout")]
        [SerializeField, Min(_minimumTotalSlotCount), Tooltip("Total number of inventory slots available to the player, including the hotbar.")]
        private int _slotCount = 24;

        [SerializeField, Min(_minimumHotbarSlotCount), Tooltip("Number of slots reserved for the hotbar at the bottom of the screen.")]
        private int _hotbarSlotCount = 8;

        [SerializeField, Min(0), Tooltip("Hotbar slot selected when the game starts. Clamped to the available hotbar range.")]
        private int _startingSelectedHotbarSlotIndex = 0;

        [Header("Stacking")]
        [SerializeField, Min(1), Tooltip("Maximum number of items allowed in a single stack for stackable items.")]
        private int _inventoryStackMax = 9999;

        public int HotbarSlotCount => Mathf.Min(_hotbarSlotCount, _slots.Count);
        public int SelectedHotbarSlotIndex { get; private set; } = -1;


        [SerializeField] 
        private ItemCollectWorldUI _itemCollectPlatePrefab;
        
        [SerializeField]
        private float _timeBetweenCollections = 0.1f;

        [Header("Starting Items")]
        [SerializeField] private float _initialDelay;
        [SerializeField] private float _delayBetweenItemsGiven;
        [SerializeField] private List<InventoryStack> _startingItems = new();

        private readonly List<InventoryStack> _slots = new();
        public List<InventoryStack> Slots => _slots;
        public int SlotCount => _slots.Count;

        public InventoryStack CursorStack { get; private set; } = new();
        public InventoryStack SelectedHotbarStack { get; private set; } = new();

        public bool IsInventoryOpen { get; private set; }
        public bool IsFull => !_slots.Exists(HasRoomForAnyItem);
        
        private bool _isCollecting;
        private Queue<InventoryStack> _itemQueue = new();
        private Dictionary<string, ItemCollectWorldUI> _itemPlates = new(); // Maybe replace string with an item id if I decide to make that later

        private void Awake()
        {
            Instance = this;
            ClampStartingSelectedHotbarSlotIndex();
            InitializeSlots();
        }

        private IEnumerator Start()
        {
            SelectHotbarSlot(_startingSelectedHotbarSlotIndex);

            GameInput.Instance.OnSelectSlot += GameInput_OnSelectSlot;
            GameInput.Instance.OnScrollWheel += GameInput_OnScrollWheel;
            GameInput.Instance.OnToggleInventory += GameInput_OnToggleInventory;
            // HealthManager.Instance.OnDeath += HandleDeath;

            yield return null;

            CloseInventory(force: true);

            yield return new WaitForSeconds(_initialDelay);

            foreach (InventoryStack slotItem in _startingItems)
            {
                AddItem(slotItem.Item, slotItem.Amount);
                yield return new WaitForSeconds(_delayBetweenItemsGiven);
            }
        }

        private void OnDestroy()
        {
            if(GameInput.Instance != null)
            {
                GameInput.Instance.OnSelectSlot -= GameInput_OnSelectSlot;
                GameInput.Instance.OnScrollWheel -= GameInput_OnScrollWheel;
                GameInput.Instance.OnToggleInventory -= GameInput_OnToggleInventory;
            }
            
            // HealthManager.Instance.OnDeath -= HandleDeath;

        }

        private void OnValidate()
        {
            ClampStartingSelectedHotbarSlotIndex();
        }

        #region Input

        private void GameInput_OnToggleInventory(object sender, InputAction.CallbackContext e)
        {
            ToggleInventory();
        }

        public void ToggleInventory()
        {
            if (IsInventoryOpen)
            {
                CloseInventory();
                return;
            }

            OpenInventory();
        }

        private void OpenInventory()
        {
            if (IsInventoryOpen || !CanOpenInventory())
            {
                return;
            }

            IsInventoryOpen = true;
            OnInventoryOpenChanged?.Invoke(true);
            OnInventoryChanged?.Invoke();
            OnCursorStackChanged?.Invoke(CursorStack.Clone());

            GameInput.Instance.IsGameplayInputBlocked = true;
        }

        private void CloseInventory(bool force = false)
        {
            if ((!IsInventoryOpen && !force) || !CursorStack.IsEmpty)
            {
                return;
            }

            IsInventoryOpen = false;
            OnInventoryOpenChanged?.Invoke(false);
            OnInventoryChanged?.Invoke();

            GameInput.Instance.IsGameplayInputBlocked = false;
        }

        private void HandleDeath()
        {
            CloseInventory(force: true);
        }

        private bool CanOpenInventory()
        {
            return Player.Instance.Character.StateMachine.CurrentState.StateKey != AIState.Dead;
        }

        private void GameInput_OnSelectSlot(object sender, InputAction.CallbackContext context)
        {
            if (!context.started || Player.Instance.Character.StateMachine.CurrentState.StateKey == AIState.Dead)
            {
                return;
            }

            var control = context.control;

            if (control is KeyControl key)
            {
                int slotIndex = key.keyCode - Key.Digit1;
                if (slotIndex >= 0 && slotIndex < HotbarSlotCount)
                {
                    SelectHotbarSlot(slotIndex);
                }
            }
        }

        private void GameInput_OnScrollWheel(object sender, InputAction.CallbackContext context)
        {
            if (!context.performed || Player.Instance.Character.StateMachine.CurrentState.StateKey == AIState.Dead)
            {
                return;
            }

            Vector2 scrollDelta = context.ReadValue<Vector2>();
            int itemCount = HotbarSlotCount;
            if (itemCount == 0)
            {
                return;
            }

            int selectedSlotIndex = SelectedHotbarSlotIndex;

            if (scrollDelta.y > 0f)
            {
                int upcomingIndex = selectedSlotIndex - 1;
                selectedSlotIndex = upcomingIndex < 0 ? itemCount - 1 : selectedSlotIndex - 1;
                SelectHotbarSlot(selectedSlotIndex);
            }
            else if (scrollDelta.y < 0f)
            {
                int upcomingIndex = selectedSlotIndex + 1;
                selectedSlotIndex = upcomingIndex >= itemCount ? 0 : selectedSlotIndex + 1;
                SelectHotbarSlot(selectedSlotIndex);
            }
        }

        #endregion

        #region Inventory Item Functions

        public void SubtractOneFromHotbarSelectedSlot()
        {
            if (!IsValidSlotIndex(SelectedHotbarSlotIndex))
            {
                return;
            }

            InventoryStack selectedSlot = _slots[SelectedHotbarSlotIndex];
            if (selectedSlot.IsEmpty)
            {
                return;
            }

            selectedSlot.RemoveAmount(1);
            RefreshAfterInventoryChange();
        }

        public void ClearSelectedSlotItem()
        {
            _slots[SelectedHotbarSlotIndex].Clear();
            RefreshAfterInventoryChange();
        }

        public int GetItemAmount(ItemSO item)
        {
            if (item == null) return 0;

            int itemCounter = 0;
            foreach (InventoryStack inventorySlotItem in _slots)
            {
                if (inventorySlotItem.IsEmpty) continue;

                if (inventorySlotItem.Item.ItemName == item.ItemName)
                {
                    itemCounter += inventorySlotItem.Amount;
                }
            }

            return itemCounter;
        }

        public bool HasItemAmount(ItemSO item, int amount)
        {
            int itemCounter = 0;

            foreach (InventoryStack inventorySlotItem in _slots)
            {
                if (inventorySlotItem.IsEmpty) continue;

                if (inventorySlotItem.Item.ItemName == item.ItemName)
                {
                    itemCounter += inventorySlotItem.Amount;
                }
            }

            return itemCounter >= amount;
        }

        public void AddItem(ItemSO item, int amount = 1, bool playCollectSound = true)
        {
            if (item == null || amount <= 0 || IsFull)
            {
                return;
            }

            _itemQueue.Enqueue(new InventoryStack(item, amount));

            if (!_isCollecting)
            {
                StartCoroutine(StaggeredItemCollection(playCollectSound));
            }
        }

        private IEnumerator StaggeredItemCollection(bool playCollectSound)
        {
            _isCollecting = true;

            while (_itemQueue.Count > 0)
            {
                InventoryStack itemToCollect = _itemQueue.Dequeue();
                
                Add(itemToCollect);
                if (playCollectSound)
                {
                    // SoundManager.Instance.PlayOneShot(FMODEvents.Instance.ItemPickup, Player.Instance.transform.position);
                }

                string itemName = itemToCollect.Item.ItemName;
                InventoryStack invItemToDisplay = new(itemToCollect.Item, itemToCollect.Amount);

                // If there exists an item collect plate as the item being collected, delete it and spawn a new one
                if (_itemPlates.ContainsKey(itemName))
                {
                    // Create refreshed item with updated quantities
                    int currentQuantity = _itemPlates[itemName].DisplayAmount;
                    int additionalQuantity = itemToCollect.Amount;

                    invItemToDisplay.SetAmount(currentQuantity + additionalQuantity);

                    // Delete the currently spawned item,
                    Destroy(_itemPlates[itemName].gameObject);

                    // Remove it from the dictionary
                    _itemPlates.Remove(itemName);
                }

                SpawnItemCollectPlate(invItemToDisplay);

                yield return new WaitForSeconds(_timeBetweenCollections);
            }

            _isCollecting = false;
        }

        private void SpawnItemCollectPlate(InventoryStack itemToCollect)
        {
            string itemName = itemToCollect.Item.ItemName;
            ItemCollectWorldUI itemPlate = Instantiate(_itemCollectPlatePrefab, Player.Instance.transform.position, Quaternion.identity);
            itemPlate.DisplayedItem = itemToCollect;
            itemPlate.OnAnimationComplete += () =>
            {
                Destroy(itemPlate.gameObject);
                _itemPlates.Remove(itemName);
            };
            _itemPlates.Add(itemName, itemPlate);
        }

        private void Add(InventoryStack stack)
        {
            int remainingAmount = stack.Amount;
            FillExistingStacks(stack.Item, ref remainingAmount);
            FillEmptySlots(stack.Item, ref remainingAmount);

            int amountAdded = stack.Amount - remainingAmount;
            if (amountAdded > 0)
            {
                OnItemPickup?.Invoke(stack.Item, amountAdded);
            }

            RefreshAfterInventoryChange();
        }

        public int RemoveItem(ItemSO item, int amount = 1)
        {
            if (item == null || amount <= 0)
            {
                return amount;
            }

            int remainingAmount = amount;
            for (int index = _slots.Count - 1; index >= 0 && remainingAmount > 0; index--)
            {
                InventoryStack slot = _slots[index];
                if (slot.IsEmpty || slot.Item != item)
                {
                    continue;
                }

                remainingAmount -= slot.RemoveAmount(remainingAmount);
            }

            if (remainingAmount < amount)
            {
                RefreshAfterInventoryChange();
            }

            return remainingAmount;
        }

        private void FillExistingStacks(ItemSO item, ref int remainingAmount)
        {
            if (item == null || remainingAmount <= 0)
            {
                return;
            }

            int maxStackSize = GetMaxStackSize(item);
            if (maxStackSize <= 1)
            {
                return;
            }

            foreach (InventoryStack slot in _slots)
            {
                if (remainingAmount <= 0)
                {
                    return;
                }

                if (slot.IsEmpty || slot.Item != item || slot.Amount >= maxStackSize)
                {
                    continue;
                }

                int amountToAdd = Mathf.Min(maxStackSize - slot.Amount, remainingAmount);
                slot.AddAmount(amountToAdd);
                remainingAmount -= amountToAdd;
            }
        }

        private void FillEmptySlots(ItemSO item, ref int remainingAmount)
        {
            foreach (InventoryStack slot in _slots)
            {
                if (remainingAmount <= 0)
                {
                    return;
                }

                if (!slot.IsEmpty)
                {
                    continue;
                }

                int amountToAdd = Mathf.Min(GetMaxStackSize(item), remainingAmount);
                slot.Set(item, amountToAdd);
                remainingAmount -= amountToAdd;
            }
        }

        public void RefreshAfterInventoryChange()
        {
            UpdateSelectedHotbarStack();
            OnInventoryChanged?.Invoke();
        }

        #endregion

        #region Slot Click Functions

        public void HandleSlotLeftClick(int slotIndex, bool isShiftHeld)
        {
            if (!IsValidSlotIndex(slotIndex))
            {
                return;
            }

            if (isShiftHeld && CursorStack.IsEmpty)
            {
                QuickMoveSlot(slotIndex);
                return;
            }

            InventoryStack slotItem = _slots[slotIndex];
            if (CursorStack.IsEmpty)
            {
                if (slotItem.IsEmpty)
                {
                    return;
                }

                CursorStack = slotItem.Clone();
                slotItem.Clear();
                RefreshAfterInventoryChange();
                NotifyCursorStackChanged();
                return;
            }

            if (slotItem.IsEmpty)
            {
                _slots[slotIndex] = CursorStack.Clone();
                CursorStack.Clear();
                RefreshAfterInventoryChange();
                NotifyCursorStackChanged();
                return;
            }

            if (CanStacksMerge(slotItem, CursorStack))
            {
                int movedAmount = MoveAmount(CursorStack, slotItem, GetMaxStackSize(slotItem.Item));
                if (movedAmount > 0)
                {
                    RefreshAfterInventoryChange();
                    NotifyCursorStackChanged();
                }

                return;
            }

            InventoryStack swappedItem = slotItem.Clone();
            _slots[slotIndex] = CursorStack.Clone();
            CursorStack = swappedItem;
            RefreshAfterInventoryChange();
            NotifyCursorStackChanged();
        }

        public void HandleSlotRightClick(int slotIndex)
        {
            if (!IsValidSlotIndex(slotIndex))
            {
                return;
            }

            InventoryStack slotItem = _slots[slotIndex];

            if (CursorStack.IsEmpty)
            {
                if (slotItem.IsEmpty)
                {
                    return;
                }

                int cursorAmount = Mathf.CeilToInt(slotItem.Amount * 0.5f);
                CursorStack = new InventoryStack(slotItem.Item, cursorAmount);
                slotItem.RemoveAmount(cursorAmount);
                RefreshAfterInventoryChange();
                NotifyCursorStackChanged();
                return;
            }

            if (slotItem.IsEmpty)
            {
                _slots[slotIndex].Set(CursorStack.Item, 1);
                CursorStack.RemoveAmount(1);
                RefreshAfterInventoryChange();
                NotifyCursorStackChanged();
                return;
            }

            if (!CanStacksMerge(slotItem, CursorStack))
            {
                return;
            }

            int movedAmount = MoveAmount(CursorStack, slotItem, GetMaxStackSize(slotItem.Item), 1);
            if (movedAmount <= 0)
            {
                return;
            }

            RefreshAfterInventoryChange();
            NotifyCursorStackChanged();
        }

        public void ClearCursorStack()
        {
            if (CursorStack.IsEmpty)
            {
                return;
            }

            CursorStack.Clear();
            NotifyCursorStackChanged();
        }

        private void NotifyCursorStackChanged()
        {
            if (CursorStack.IsEmpty)
            {
                CursorStack.Clear();
            }

            OnCursorStackChanged?.Invoke(CursorStack.Clone());
        }

        private void QuickMoveSlot(int slotIndex)
        {
            if (!IsValidSlotIndex(slotIndex))
            {
                return;
            }

            InventoryStack sourceItem = _slots[slotIndex];
            if (sourceItem.IsEmpty)
            {
                return;
            }

            bool isHotbarSlot = slotIndex < HotbarSlotCount;
            int targetStart = isHotbarSlot ? HotbarSlotCount : 0;
            int targetEnd = isHotbarSlot ? _slots.Count : HotbarSlotCount;

            MoveSlotIntoRange(slotIndex, targetStart, targetEnd);
            RefreshAfterInventoryChange();
        }

        private void MoveSlotIntoRange(int sourceIndex, int targetStart, int targetEnd)
        {
            InventoryStack sourceItem = _slots[sourceIndex];
            if (sourceItem.IsEmpty)
            {
                return;
            }

            int maxStackSize = GetMaxStackSize(sourceItem.Item);

            for (int targetIndex = targetStart; targetIndex < targetEnd; targetIndex++)
            {
                if (targetIndex == sourceIndex)
                {
                    continue;
                }

                InventoryStack targetItem = _slots[targetIndex];
                if (!CanStacksMerge(targetItem, sourceItem))
                {
                    continue;
                }

                MoveAmount(sourceItem, targetItem, maxStackSize);
                if (sourceItem.IsEmpty)
                {
                    return;
                }
            }

            for (int targetIndex = targetStart; targetIndex < targetEnd; targetIndex++)
            {
                if (targetIndex == sourceIndex)
                {
                    continue;
                }

                InventoryStack targetItem = _slots[targetIndex];
                if (!targetItem.IsEmpty)
                {
                    continue;
                }

                int amountToMove = Mathf.Min(maxStackSize, sourceItem.Amount);
                targetItem.Set(sourceItem.Item, amountToMove);
                sourceItem.RemoveAmount(amountToMove);

                if (sourceItem.IsEmpty)
                {
                    return;
                }
            }
        }

        private bool HasRoomForAnyItem(InventoryStack slot)
        {
            return slot.IsEmpty || (slot.Item != null && slot.Amount < GetMaxStackSize(slot.Item));
        }

        private bool CanStacksMerge(InventoryStack target, InventoryStack source)
        {
            return target != null &&
                source != null &&
                !target.IsEmpty &&
                !source.IsEmpty &&
                target.Item == source.Item &&
                target.Amount < GetMaxStackSize(target.Item);
        }

        private int MoveAmount(InventoryStack source, InventoryStack target, int maxTargetAmount, int requestedAmount = int.MaxValue)
        {
            if (source == null || target == null || source.IsEmpty || target.IsEmpty)
            {
                return 0;
            }

            int amountToMove = Mathf.Min(requestedAmount, source.Amount);
            amountToMove = Mathf.Min(amountToMove, maxTargetAmount - target.Amount);
            if (amountToMove <= 0)
            {
                return 0;
            }

            target.AddAmount(amountToMove);
            source.RemoveAmount(amountToMove);
            return amountToMove;
        }

        private int GetMaxStackSize(ItemSO item)
        {
            if (item == null)
            {
                return 1;
            }

            return item.IsStackable ? _inventoryStackMax : 1;
        }

        #endregion

        private void SelectHotbarSlot(int hotbarSlotIndex)
        {
            if (HotbarSlotCount == 0)
            {
                SelectedHotbarSlotIndex = 0;
                UpdateSelectedHotbarStack();
                return;
            }

            int newIndex = Mathf.Clamp(hotbarSlotIndex, 0, HotbarSlotCount - 1);
            if (newIndex == SelectedHotbarSlotIndex)
            {
                return;
            }

            SelectedHotbarSlotIndex = newIndex;
            UpdateSelectedHotbarStack();
        }

        private void UpdateSelectedHotbarStack()
        {
            if (!IsValidSlotIndex(SelectedHotbarSlotIndex))
            {
                SelectedHotbarStack = new InventoryStack();
                OnSelectedHotbarSlotChanged?.Invoke(SelectedHotbarSlotIndex, SelectedHotbarStack.Clone());
                Debug.Log($"Invalid hotbar slot index: {SelectedHotbarSlotIndex}");
                return;
            }

            SelectedHotbarStack = _slots[SelectedHotbarSlotIndex];
            OnSelectedHotbarSlotChanged?.Invoke(SelectedHotbarSlotIndex, SelectedHotbarStack);
        }

        public InventoryStack GetSlot(int slotIndex)
        {
            return IsValidSlotIndex(slotIndex) ? _slots[slotIndex] : new InventoryStack();
        }

        private bool IsValidSlotIndex(int slotIndex)
        {
            return slotIndex >= 0 && slotIndex < _slots.Count;
        }

        private void InitializeSlots()
        {
            _slots.Clear();

            for (int i = 0; i < _slotCount; i++)
            {
                _slots.Add(new InventoryStack());
            }
        }

        private void ClampStartingSelectedHotbarSlotIndex()
        {
            int maxHotbarIndex = Mathf.Max(0, _hotbarSlotCount - 1);
            _startingSelectedHotbarSlotIndex = Mathf.Clamp(_startingSelectedHotbarSlotIndex, 0, maxHotbarIndex);
        }
    }
}

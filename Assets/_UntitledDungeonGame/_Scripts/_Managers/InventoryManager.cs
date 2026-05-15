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
        public int HotbarSlotCount => Mathf.Min(_hotbarSlotCount, _slots.Count);
        public int SelectedHotbarSlotIndex { get; private set; } = -1;

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
        public bool IsFull => !_slots.Exists(slot => slot.IsEmpty);

        private void Awake()
        {
            Instance = this;
            InitializeSlots();
        }

        private IEnumerator Start()
        {
            SelectHotbarSlot(0);

            GameInput.Instance.OnSelectSlot += GameInput_OnSelectSlot;
            GameInput.Instance.OnScrollWheel += GameInput_OnScrollWheel;
            GameInput.Instance.OnToggleInventory += GameInput_OnToggleInventory;
            // HealthManager.Instance.OnDeath += HandleDeath;

            yield return null;

            CloseInventory(force: true);

            yield return new WaitForSeconds(_initialDelay);

            foreach (InventoryStack slotItem in _startingItems)
            {
                AddItem(slotItem.Item);
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
            if (!context.started || Player.Instance.Character.StateMachine.CurrentState.StateKey != AIState.Dead)
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
            if (!context.performed || Player.Instance.Character.StateMachine.CurrentState.StateKey != AIState.Dead)
            {
                return;
            }

            Vector2 scrollDelta = context.ReadValue<Vector2>();
            int itemCount = _hotbarSlotCount;
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
                    itemCounter++;
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
                    itemCounter++;
                }
            }

            return itemCounter >= amount;
        }

        public int AddItem(ItemSO item, int amount = 1)
        {
            if (item == null || amount <= 0)
            {
                return 0;
            }

            if (IsFull)
            {
                return amount;
            }

            int remainingAmount = amount;
            FillEmptySlots(item, ref remainingAmount);

            int amountAdded = amount - remainingAmount;
            if (amountAdded > 0)
            {
                OnItemPickup?.Invoke(item, amountAdded);
            }

            if (remainingAmount < amount)
            {
                RefreshAfterInventoryChange();
            }

            return remainingAmount;
        }

        public List<InventoryStack> AddItems(IEnumerable<InventoryStack> itemsToAdd)
        {
            List<InventoryStack> leftOvers = new();

            if (itemsToAdd == null)
            {
                return leftOvers;
            }

            foreach (InventoryStack slotItem in itemsToAdd)
            {
                if (slotItem == null || slotItem.IsEmpty)
                {
                    continue;
                }

                int remainingAmount = AddItem(slotItem.Item);
                if (remainingAmount > 0)
                {
                    leftOvers.Add(new InventoryStack(slotItem.Item));
                }
            }

            return leftOvers;
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

                slot.Clear();
                remainingAmount--;
            }

            RefreshAfterInventoryChange();
            return remainingAmount;
        }

        public List<InventoryStack> RemoveItems(IEnumerable<InventoryStack> itemsToRemove)
        {
            List<InventoryStack> leftOvers = new();

            if (itemsToRemove == null)
            {
                return leftOvers;
            }

            foreach (InventoryStack slotItem in itemsToRemove)
            {
                if (slotItem == null || slotItem.IsEmpty)
                {
                    continue;
                }

                int remainingAmount = RemoveItem(slotItem.Item);
                if (remainingAmount > 0)
                {
                    leftOvers.Add(new InventoryStack(slotItem.Item));
                }
            }

            return leftOvers;
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

                slot.Set(item);
                remainingAmount--;
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

            InventoryStack swappedItem = slotItem.Clone();
            _slots[slotIndex] = CursorStack.Clone();
            CursorStack = swappedItem;
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

                targetItem.Set(sourceItem.Item);
                sourceItem.Clear();
                return;
            }
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

            SelectedHotbarStack = _slots[SelectedHotbarSlotIndex].Clone();
            OnSelectedHotbarSlotChanged?.Invoke(SelectedHotbarSlotIndex, SelectedHotbarStack.Clone());
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
    }
}

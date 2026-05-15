using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace UntitledDungeonGame
{
    public class InventoryUI : NetworkBehaviour
    {
        [SerializeField] private InventorySlotUI _slotPrefab;
        [SerializeField] private RectTransform _hotbarPanel;
        [SerializeField] private RectTransform _inventoryPanel;
        [SerializeField] private RectTransform _menu;


        [Header("Cursor Inventory UI")]
        [SerializeField] private RectTransform _dragItemRoot;
        [SerializeField] private Image _dragItemIcon;
        [SerializeField] private TextMeshProUGUI _dragItemCountText;

        private readonly List<InventorySlotUI> _slotUis = new();

        private void Awake()
        {
            if (NetworkManager != null)
            {
                NetworkManager.OnClientConnectedCallback += InitializeInventoryUI;
            }
        }

        public override void OnDestroy()
        {
            InventoryManager.Instance.OnInventoryChanged -= RefreshAll;
            InventoryManager.Instance.OnSelectedHotbarSlotChanged -= HandleSelectedHotbarChanged;
            InventoryManager.Instance.OnInventoryOpenChanged -= SetInventoryVisible;
            InventoryManager.Instance.OnCursorStackChanged -= RefreshDragItem;

            if (NetworkManager != null)
            {
                NetworkManager.OnClientConnectedCallback -= InitializeInventoryUI;
            }
        }

        private void Update()
        {
            UpdateDragItemPosition();
        }

        private void InitializeInventoryUI(ulong clientId)
        {
            if (NetworkManager.LocalClientId != clientId) return;

            BuildSlots();
            RefreshAll();

            InventoryManager.Instance.OnInventoryChanged += RefreshAll;
            InventoryManager.Instance.OnSelectedHotbarSlotChanged += HandleSelectedHotbarChanged;
            InventoryManager.Instance.OnInventoryOpenChanged += SetInventoryVisible;
            InventoryManager.Instance.OnCursorStackChanged += RefreshDragItem;
        }

        private void RefreshDragItem(InventoryStack cursorStack)
        {
            bool shouldShow = InventoryManager.Instance.IsInventoryOpen &&
                cursorStack != null && !cursorStack.IsEmpty;


            _dragItemRoot.gameObject.SetActive(shouldShow);

            if (!shouldShow) return;

            _dragItemIcon.sprite = cursorStack.Item.InventoryIcon;
            _dragItemIcon.enabled = cursorStack.Item.InventoryIcon != null;
            _dragItemCountText.text = string.Empty;
        }

        private void HandleSelectedHotbarChanged(int arg1, InventoryStack stack)
        {
            RefreshAll();
        }

        private void BuildSlots()
        {
            CreateSlotRange(0, InventoryManager.Instance.HotbarSlotCount, _hotbarPanel);
            CreateSlotRange(InventoryManager.Instance.HotbarSlotCount, InventoryManager.Instance.Slots.Count, _inventoryPanel);
        }

        private void CreateSlotRange(int startIndex, int endIndex, RectTransform parent)
        {
            for (int slotIndex = startIndex; slotIndex < endIndex; slotIndex++)
            {
                InventorySlotUI slotUi = CreateSlotInstance(parent);
                slotUi.Initialize(this, slotIndex);
                _slotUis.Add(slotUi);
            }
        }

        private InventorySlotUI CreateSlotInstance(RectTransform parent)
        {
            InventorySlotUI slot = Instantiate(_slotPrefab, parent);
            slot.transform.localScale = Vector3.one;
            return slot;
        }

        private void RefreshAll()
        {
            foreach (InventorySlotUI slotUi in _slotUis)
            {
                InventoryStack stack = InventoryManager.Instance.GetSlot(slotUi.SlotIndex);
                slotUi.Refresh(stack);
            }

            RefreshDragItem(InventoryManager.Instance.CursorStack);
        }

        public void SetInventoryVisible(bool isVisible)
        {
            _menu.gameObject.SetActive(isVisible);

            if (!isVisible)
            {
                _dragItemRoot.gameObject.SetActive(false);
            }
            else
            {
                RefreshDragItem(InventoryManager.Instance.CursorStack);
            }
        }

        private void UpdateDragItemPosition()
        {
            if (_dragItemRoot == null || !_dragItemRoot.gameObject.activeSelf || Mouse.current == null)
            {
                return;
            }

            Vector2 mouseScreenPosition = Mouse.current.position.ReadValue();
            _dragItemRoot.position = mouseScreenPosition;
        }

        public void HandleSlotClick(int slotIndex, PointerEventData.InputButton button)
        {
            bool isShiftHeld = Keyboard.current != null && ((Keyboard.current.leftShiftKey?.isPressed ?? false) || (Keyboard.current.rightShiftKey?.isPressed ?? false));

            if (button == PointerEventData.InputButton.Left)
            {
                InventoryManager.Instance.HandleSlotLeftClick(slotIndex, isShiftHeld);
            }
        }
    }
}

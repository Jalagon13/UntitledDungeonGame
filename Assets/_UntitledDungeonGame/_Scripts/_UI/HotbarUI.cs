using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace UntitledDungeonGame
{
    public class HotbarUI : NetworkBehaviour
    {
        [SerializeField] private HotbarSlotUI _hotbarSlotUIPrefab;
        [SerializeField] private RectTransform _hotbarDisplay;
        [SerializeField] private RectTransform _hotbarSlotHolder;

        private readonly List<HotbarSlotUI> _hotbarSlotUis = new();
        
        private void Awake()
        {
            if (NetworkManager != null)
            {
                NetworkManager.OnClientConnectedCallback += InitializeHotbar;
            }
        }

        public override void OnDestroy()
        {
            InventoryManager.Instance.OnInventoryChanged -= RefreshHotbar;
            InventoryManager.Instance.OnInventoryOpenChanged -= UpdateHotbarDisplay;
            InventoryManager.Instance.OnSelectedHotbarSlotChanged -= HandleSelectedHotbarChanged;

            if (NetworkManager != null)
            {
                NetworkManager.OnClientConnectedCallback -= InitializeHotbar;
            }
        }

        private void InitializeHotbar(ulong clientId)
        {
            if (NetworkManager.LocalClientId != clientId) return;

            BuildHotbar();
            RefreshHotbar();
            RefreshSelection(InventoryManager.Instance.SelectedHotbarSlotIndex);
            
            InventoryManager.Instance.OnInventoryChanged += RefreshHotbar;
            InventoryManager.Instance.OnInventoryOpenChanged += UpdateHotbarDisplay;
            InventoryManager.Instance.OnSelectedHotbarSlotChanged += HandleSelectedHotbarChanged;
        }

        private void RefreshHotbar()
        {
            foreach (HotbarSlotUI slotUi in _hotbarSlotUis)
            {
                InventoryStack stack = InventoryManager.Instance.GetSlot(slotUi.SlotIndex);
                slotUi.Refresh(stack);
            }
        }

        private void BuildHotbar()
        {
            for (int slotIndex = 0; slotIndex < InventoryManager.Instance.HotbarSlotCount; slotIndex++)
            {
                HotbarSlotUI hotbarSlotUI = CreateSlotInstance(_hotbarSlotHolder);
                hotbarSlotUI.Initialize(this, slotIndex);
                _hotbarSlotUis.Add(hotbarSlotUI);
            }
        }

        private void HandleSelectedHotbarChanged(int selectedSlotIndex, InventoryStack stack)
        {
            RefreshSelection(selectedSlotIndex);
        }

        private void RefreshSelection(int selectedSlotIndex)
        {
            foreach (HotbarSlotUI slotUi in _hotbarSlotUis)
            {
                slotUi.SetSelected(slotUi.SlotIndex == selectedSlotIndex);
            }
        }

        private HotbarSlotUI CreateSlotInstance(RectTransform parent)
        {
            HotbarSlotUI hotbarSlot = Instantiate(_hotbarSlotUIPrefab, parent);
            hotbarSlot.transform.localScale = Vector3.one;
            return hotbarSlot;
        }

        private void UpdateHotbarDisplay(bool inventoryOpen)
        {
            if(inventoryOpen)
            {
                HideHotbar();
            }
            else
            {
                ShowHotbar();
            }
        }

        private void ShowHotbar()
        {
            _hotbarDisplay.gameObject.SetActive(true); 
        }
        
        private void HideHotbar()
        {
            _hotbarDisplay.gameObject.SetActive(false);
        }
    }
}

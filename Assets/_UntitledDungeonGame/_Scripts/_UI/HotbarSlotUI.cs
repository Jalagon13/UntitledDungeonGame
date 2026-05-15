using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UntitledDungeonGame
{
    public class HotbarSlotUI : MonoBehaviour
    {
        [SerializeField] private Image _iconImage;
        [SerializeField] private TextMeshProUGUI _countText;

        private HotbarUI _hotbarUI;

        public int SlotIndex { get; private set; }

        public void Initialize(HotbarUI inventoryUI, int slotIndex)
        {
            SlotIndex = slotIndex;
            _hotbarUI = inventoryUI;
            name = $"Inventory Slot {slotIndex + 1}";
        }

        public void Refresh(InventoryStack stack)
        {
            bool showItem = stack != null && !stack.IsEmpty && stack.Item != null;
            _iconImage.enabled = showItem && stack.Item.InventoryIcon != null;
            _iconImage.sprite = showItem ? stack.Item.InventoryIcon : null;
            _countText.text = string.Empty;
        }
    }
}

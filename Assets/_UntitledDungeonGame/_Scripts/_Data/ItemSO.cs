using UnityEngine;

namespace UntitledDungeonGame
{
    [CreateAssetMenu(fileName = "New Item Data", menuName = "Data/ItemData")]
    public class ItemSO : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField, Tooltip("Display name shown anywhere this item is referenced in UI.")]
        private string _itemName;

        [SerializeField, TextArea, Tooltip("Longer description for the item.")]
        private string _itemDescription;

        [Header("Visuals")]
        [SerializeField, Tooltip("Sprite used for the inventory icon. This is the fallback approach instead of rendering the prefab automatically.")]
        private Sprite _inventoryIcon;

        [Header("Inventory")]
        [SerializeField, Tooltip("Whether this item can stack in the inventory.")]
        private bool _isStackable = true;

        public string ItemName => _itemName;
        public string ItemDescription => _itemDescription;
        public Sprite InventoryIcon => _inventoryIcon;
        public bool IsStackable => _isStackable;
    }
}

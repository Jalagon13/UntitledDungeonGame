using System;
using UnityEngine;

namespace UntitledDungeonGame
{
    [Serializable]
    public class InventoryStack
    {
        [SerializeField] private ItemSO _item;

        public InventoryStack()
        {
            _item = null;
        }

        public InventoryStack(ItemSO item)
        {
            Set(item);
        }

        public ItemSO Item => _item;
        public bool IsEmpty => _item == null;

        public void Set(ItemSO item)
        {
            _item = item;
        }

        public void Clear()
        {
            _item = null;
        }

        public InventoryStack Clone()
        {
            return IsEmpty ? new InventoryStack() : new InventoryStack(_item);
        }
    }
}

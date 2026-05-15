using System;
using UnityEngine;

namespace UntitledDungeonGame
{
    [Serializable]
    public class InventoryStack
    {
        [SerializeField] private ItemSO _item;
        [SerializeField] private int _amount;

        public InventoryStack()
        {
            Clear();
        }

        public InventoryStack(ItemSO item)
        {
            Set(item, item == null ? 0 : 1);
        }

        public InventoryStack(ItemSO item, int amount)
        {
            Set(item, amount);
        }

        public ItemSO Item => _item;
        public int Amount => IsEmpty ? 0 : Mathf.Max(1, _amount);
        public bool IsEmpty => _item == null;

        public void Set(ItemSO item)
        {
            Set(item, item == null ? 0 : 1);
        }

        public void Set(ItemSO item, int amount)
        {
            if (item == null || amount <= 0)
            {
                Clear();
                return;
            }

            _item = item;
            _amount = amount;
        }

        public void SetAmount(int amount)
        {
            if (IsEmpty || amount <= 0)
            {
                Clear();
                return;
            }

            _amount = amount;
        }

        public void AddAmount(int amount)
        {
            if (amount <= 0 || IsEmpty)
            {
                return;
            }

            _amount = Amount + amount;
        }

        public int RemoveAmount(int amount)
        {
            if (amount <= 0 || IsEmpty)
            {
                return 0;
            }

            int removedAmount = Mathf.Min(Amount, amount);
            _amount -= removedAmount;

            if (_amount <= 0)
            {
                Clear();
            }

            return removedAmount;
        }

        public void Clear()
        {
            _item = null;
            _amount = 0;
        }

        public InventoryStack Clone()
        {
            return IsEmpty ? new InventoryStack() : new InventoryStack(_item, Amount);
        }
    }
}

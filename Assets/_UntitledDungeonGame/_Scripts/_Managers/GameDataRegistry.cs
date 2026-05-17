using System.Collections.Generic;
using UnityEngine;

namespace UntitledDungeonGame
{
    public class GameDataRegistry : MonoBehaviour
    {
        public static GameDataRegistry Instance { get; private set; }
        public const ushort INVALID_ID = ushort.MaxValue;
        

        [SerializeField]
        private List<ItemSO> _itemData;
        

        private void Awake()
        {
            Instance = this;
        }

        #region Item Data Functions

        public ushort GetItemIdFromItemSO(ItemSO itemData)
        {
            if (itemData == null)
            {
                return INVALID_ID;
            }

            for (int i = 0; i < _itemData.Count; i++)
            {
                if (_itemData[i].ItemName == itemData.ItemName)
                {
                    return (ushort)i;
                }
            }

            Debug.LogError($"ItemDataSO '{itemData}' not found!");
            return ushort.MaxValue;
        }

        public ItemSO GetItemSOFromItemId(ushort itemId)
        {
            if (itemId >= _itemData.Count || itemId < 0)
            {
                // Debug.LogError($"Invalid Item ID: {itemId}");
                return null;
            }

            return _itemData[itemId];
        }

        #endregion


    }
}
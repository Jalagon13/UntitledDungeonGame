using UnityEngine;

namespace UntitledDungeonGame
{
    [CreateAssetMenu(fileName = "New Placeable Data", menuName = "Data/PlaceableItemData")]
    public class PlaceableItemSO : ItemSO
    {
        [field: SerializeField] public ResourceObject PlaceablePrefab { get; private set; }
    }
}

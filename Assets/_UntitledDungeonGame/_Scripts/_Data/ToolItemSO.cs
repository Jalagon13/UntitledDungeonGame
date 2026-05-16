using UnityEngine;

namespace UntitledDungeonGame
{
    [CreateAssetMenu(fileName = "New Tool Data", menuName = "Data/ToolItemData")]
    public class ToolItemSO : ItemSO
    {
        [field: SerializeField] public GameObject SwingGameObject { get; private set; } // NTFS: Turn this into its own class not a gameobject
        [field: SerializeField] public int Damage { get; private set; } = 4;
        [field: SerializeField] public int Knockback { get; private set; } = 6;
        [field: SerializeField] public float SwingDuration { get; private set; } = 0.35f;
        [field: SerializeField] public float SwingCooldown { get; private set; } = 0.25f;
    }
}

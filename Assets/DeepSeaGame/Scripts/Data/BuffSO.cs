using UnityEngine;

namespace DeepSeaGame
{
    [CreateAssetMenu(fileName = "NewBuff", menuName = "Data/Buff", order = 0)]
    public class BuffSO : ScriptableObject
    {
        public string BuffName = "New Buff";
        public StatType TargetStat = StatType.MaxHealth;
        public int FlatAmount = 0;
        [Tooltip("Use multiplier for percent: 1.5 = 150%, 0.5 = 50%")]
        public float PercentAmount = 0f;
        [Tooltip("Duration in seconds. Negative for indefinite.")]
        public float Duration = -1f;

        public Buff CreateBuffInstance()
        {
            string nameToUse = string.IsNullOrWhiteSpace(BuffName) ? name : BuffName;
            return new Buff(nameToUse, TargetStat, FlatAmount, PercentAmount, Duration);
        }
    }
}

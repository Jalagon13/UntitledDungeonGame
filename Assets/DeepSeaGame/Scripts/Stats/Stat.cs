using System.Collections.Generic;
using UnityEngine;

namespace DeepSeaGame
{
    public class Stat
    {
        private readonly int _baseAmount;
        private readonly List<StatModifier> _modifiers = new();

        private bool _dirty = true;
        private int _finalValue;

        public Stat(int baseAmount)
        {
            _baseAmount = baseAmount;
        }
        
        public void AddModifier(StatModifier modifier)
        {
            _modifiers.Add(modifier);
            _dirty = true;
        }
        
        public void RemoveModifier(StatModifier modifier)
        {
            if (_modifiers.Remove(modifier))
            {
                _dirty = true;
            }
        }

        public int GetValue()
        {
            if (_dirty) Recalculate();
            return _finalValue;
        }

        private void Recalculate()
        {
            int flat = 0;
            // Percent modifiers are treated as multipliers. Example: 1.5 = 150%, 0.5 = 50%.
            float multiplier = 1f;

            foreach (var modifier in _modifiers)
            {
                if (modifier.Type == StatModifierType.Flat)
                {
                    flat += Mathf.RoundToInt(modifier.Value);
                }
                else if (modifier.Type == StatModifierType.Percent)
                {
                    multiplier *= modifier.Value;
                }
            }

            _finalValue = Mathf.RoundToInt((_baseAmount + flat) * multiplier);
            _dirty = false;
        }
    }
}
using System;
using UnityEngine;

namespace DeepSeaGame
{
    public enum StatType
    {
        MoveSpeed,
        MaxHealth,
        Defense
    }

    public enum StatModifierType
    {
        Flat,
        Percent
    }

    public readonly struct StatModifier : IEquatable<StatModifier>
    {
        public StatModifierType Type { get; }
        public float Value { get; }

        public StatModifier(StatModifierType type, float value)
        {
            Type = type;
            Value = value;
        }

        public bool Equals(StatModifier other) => Type == other.Type && Value.Equals(other.Value);
        public override bool Equals(object obj) => obj is StatModifier other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Type, Value);
    }

    public class Buff
    {
        public string Name { get; }
        public StatType TargetStat { get; }
        public int FlatAmount { get; }
        public float PercentAmount { get; }
        public float RemainingDuration { get; private set; }
        private readonly float _defaultDuration;
        public bool IsIndefinite => RemainingDuration < 0f;
        public bool IsActive { get; private set; }

        public Buff(string name, StatType targetStat, int flatAmount = 0, float percentAmount = 0f, float duration = -1f)
        {
            Name = string.IsNullOrWhiteSpace(name) ? "Unnamed Buff" : name;
            TargetStat = targetStat;
            FlatAmount = flatAmount;
            PercentAmount = percentAmount;
            RemainingDuration = duration;
            _defaultDuration = duration;
            IsActive = true;
        }

        internal void ApplyTo(CharacterStats owner)
        {
            if (owner == null) return;

            // Reactivate and reset duration to default when applying again
            IsActive = true;
            RemainingDuration = _defaultDuration;

            var stat = owner.GetStat(TargetStat);
            if (FlatAmount != 0)
            {
                stat.AddModifier(new StatModifier(StatModifierType.Flat, FlatAmount));
            }

            if (!Mathf.Approximately(PercentAmount, 0f))
            {
                stat.AddModifier(new StatModifier(StatModifierType.Percent, PercentAmount));
            }
        }

        internal void RemoveFrom(CharacterStats owner)
        {
            var stat = owner.GetStat(TargetStat);
            if (FlatAmount != 0)
            {
                stat.RemoveModifier(new StatModifier(StatModifierType.Flat, FlatAmount));
            }

            if (!Mathf.Approximately(PercentAmount, 0f))
            {
                stat.RemoveModifier(new StatModifier(StatModifierType.Percent, PercentAmount));
            }
        }

        public void Tick(float deltaTime, CharacterStats owner)
        {
            if (!IsActive || IsIndefinite) return;

            RemainingDuration -= deltaTime;
            if (RemainingDuration <= 0f)
            {
                Stop(owner);
            }
        }

        public void Stop(CharacterStats owner)
        {
            if (!IsActive) return;

            RemoveFrom(owner);
            IsActive = false;
        }
    }
}

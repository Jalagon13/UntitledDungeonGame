using System.Collections.Generic;
using UnityEngine;

namespace DeepSeaGame
{
    public class CharacterStats
    {
        public Stat MoveSpeed { get; }
        public Stat MaxHealth { get; }
        public Stat Defense { get; }

        private readonly List<Buff> _activeBuffs = new();
        public IReadOnlyList<Buff> ActiveBuffs => _activeBuffs;

        public CharacterStats(CharacterSO data)
        {
            MoveSpeed = new(data.BaseSpeed);
            MaxHealth = new(data.BaseMaxHealth);
            Defense = new(data.BaseDefense);
        }

        public void TickBuffs(float deltaTime)
        {
            for (int i = _activeBuffs.Count - 1; i >= 0; i--)
            {
                _activeBuffs[i].Tick(deltaTime, this);
                if (!_activeBuffs[i].IsActive)
                {
                    _activeBuffs.RemoveAt(i);
                }
            }
        }

        public void StartBuff(Buff buff)
        {
            if (buff == null) return;
            if (_activeBuffs.Contains(buff)) return;

            _activeBuffs.Add(buff);
            buff.ApplyTo(this);
            Debug.Log($"Added buff {buff.Name}");
        }

        public void StopBuff(Buff buff)
        {
            if (buff == null) return;
            if (!_activeBuffs.Remove(buff)) return;

            buff.Stop(this);
            Debug.Log($"Stop buff {buff.Name}");
        }

        public Stat GetStat(StatType type)
        {
            return type switch
            {
                StatType.MoveSpeed => MoveSpeed,
                StatType.MaxHealth => MaxHealth,
                StatType.Defense => Defense,
                _ => throw new System.ArgumentOutOfRangeException(nameof(type), type, null)
            };
        }
    }
}

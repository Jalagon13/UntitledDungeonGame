using System.Collections.Generic;
using UnityEngine;

namespace DeepSeaGame
{
    public class CharacterStats
    {
        public System.Action<Buff> OnBuffStarted;
        public System.Action<Buff> OnBuffStopped;
        
        public Stat MoveSpeed { get; }
        public Stat MaxHealth { get; }
        public Stat Defense { get; }

        private readonly List<Buff> _activeBuffs = new();
        public IReadOnlyList<Buff> ActiveBuffs => _activeBuffs;
        
        private ServerCharacter _character;


        public CharacterStats(CharacterSO data, ServerCharacter character)
        {
            _character = character;

            MoveSpeed = new(data.BaseSpeed);
            MaxHealth = new(data.BaseMaxHealth);
            Defense = new(data.BaseDefense);
        }

        public void TickBuffs(float deltaTime)
        {
            for (int i = _activeBuffs.Count - 1; i >= 0; i--)
            {
                var buff = _activeBuffs[i];
                buff.Tick(deltaTime, this);
                if (!buff.IsActive)
                {
                    _activeBuffs.RemoveAt(i);
                    OnBuffStopped?.Invoke(buff);
                }
            }
        }

        public void StartBuff(Buff buff, bool showWorldText = true)
        {
            if (buff == null) return;
            if (_activeBuffs.Contains(buff)) return;

            _activeBuffs.Add(buff);
            buff.ApplyTo(this);
            OnBuffStarted?.Invoke(buff);
            
            if(showWorldText)
            {
                GameManager.Instance.SpawnWorldText($"+{buff.Name} Buff", _character.transform.position + new Vector3(0, 2f, 0), Color.cyan, 0.5f, 1.5f);
            }
        }

        public void StopBuff(Buff buff)
        {
            if (buff == null) return;
            if (!_activeBuffs.Remove(buff)) return;

            buff.Stop(this);
            OnBuffStopped?.Invoke(buff);
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

using System;
using Unity.Netcode;
using UnityEngine;

namespace DeepSeaGame
{
    public class NetworkHealthState : NetworkBehaviour
    {
        [HideInInspector]
        public NetworkVariable<int> HitPoints = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        public event EventHandler<PointsChangedEventArgs> OnHitPointsChanged;
        public class PointsChangedEventArgs : EventArgs// NTFS: Class under construction
        {
            public int MaxPoints { get; }
            public int CurrentPoints { get; }

            public PointsChangedEventArgs(int currentPoints, int maxPoints)
            {
                MaxPoints = maxPoints;
                CurrentPoints = currentPoints;
            }
        }

        private ServerCharacter _serverCharacter;
        private NetworkVariable<LifeState> _lifeState = new NetworkVariable<LifeState>();
        public NetworkVariable<LifeState> LifeState => _lifeState;

        private void Awake()
        {
            _serverCharacter = GetComponent<ServerCharacter>();
        }

        private void OnEnable()
        {
            HitPoints.OnValueChanged += HitPointsChanged;
        }

        private void OnDisable()
        {
            HitPoints.OnValueChanged -= HitPointsChanged;
        }

        private void HitPointsChanged(int previousValue, int newValue)
        {
            OnHitPointsChanged?.Invoke(this, new PointsChangedEventArgs(HitPoints.Value, _serverCharacter.Stats.MaxHealth.GetValue()));
        }

        public bool IsFullHp()
        {
            return HitPoints.Value >= _serverCharacter.Stats.MaxHealth.GetValue();
        }

        public void AddHp(int amount)
        {
            // Double check with GPT if this logic is correct
            HitPoints.Value += Mathf.Clamp(amount, 0, _serverCharacter.Stats.MaxHealth.GetValue());
        }
    }
}

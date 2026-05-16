using System;
using Unity.Netcode;
using UnityEngine;

namespace UntitledDungeonGame
{
    // NTFS: Class under construction
    public class PointsChangedEventArgs : EventArgs
    {
        public int MaxPoints { get; }
        public int CurrentPoints { get; }

        public PointsChangedEventArgs(int currentPoints, int maxPoints)
        {
            MaxPoints = maxPoints;
            CurrentPoints = currentPoints;
        }
    }

    public class NetworkHealthState : NetworkBehaviour
    {
        [HideInInspector]
        public NetworkVariable<int> HitPoints = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        public event EventHandler<PointsChangedEventArgs> OnHitPointsChanged;

        private ServerCharacter _serverCharacter;

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
            // OnHitPointsChanged?.Invoke(this, new PointsChangedEventArgs(HitPoints.Value, _serverCharacter.Stats.MaxHealth.AsIntValue));
        }

        public bool IsFullHp()
        {
            // return HitPoints.Value >= _serverCharacter.Stats.MaxHealth.AsIntValue;
            return true;
        }

        public void AddHp(int amount)
        {
            // Double check with GPT if this logic is correct
            // HitPoints.Value += Mathf.Clamp(amount, 0, _serverCharacter.Stats.MaxHealth.AsIntValue);
        }
    }
}

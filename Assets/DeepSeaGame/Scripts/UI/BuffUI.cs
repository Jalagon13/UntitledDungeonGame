using UnityEngine;
using System.Collections.Generic;

namespace DeepSeaGame
{
    public class BuffUI : MonoBehaviour
    {
        [SerializeField] private BuffDisplayUI _buffDisplayPrefab;
        [SerializeField] private RectTransform _buffPanel;
        
        private readonly Dictionary<Buff, BuffDisplayUI> _activeDisplays = new();
        private readonly List<Buff> _indefiniteOrder = new();
        private readonly List<Buff> _durationOrder = new();

        private CharacterStats _subscribedStats;

        private void Awake()
        {
            Player.OnAnyPlayerSpawned += Player_OnAnyPlayerSpawned;
        }

        private void Start()
        {
            if (Player.Instance != null)
            {
                TrySubscribeToPlayer(Player.Instance);
            }
        }

        private void OnDestroy()
        {
            Player.OnAnyPlayerSpawned -= Player_OnAnyPlayerSpawned;
            
            UnsubscribeFromStats();
            ClearAllDisplays();
        }

        private void Player_OnAnyPlayerSpawned(object sender, Player.PlayerIdEventArgs e)
        {
            if (Player.Instance != null)
            {
                TrySubscribeToPlayer(Player.Instance);
            }
        }

        private void TrySubscribeToPlayer(Player player)
        {
            if (player == null || player.Character == null) return;
            var stats = player.Character.Stats;
            if (stats == null) return;

            if (_subscribedStats == stats) return;

            UnsubscribeFromStats();
            SubscribeToStats(stats);
        }

        private void SubscribeToStats(CharacterStats stats)
        {
            _subscribedStats = stats;
            stats.OnBuffStarted += HandleBuffStarted;
            stats.OnBuffStopped += HandleBuffStopped;

            // Initialize any existing buffs
            foreach (var buff in stats.ActiveBuffs)
            {
                HandleBuffStarted(buff);
            }
        }

        private void UnsubscribeFromStats()
        {
            if (_subscribedStats != null)
            {
                _subscribedStats.OnBuffStarted -= HandleBuffStarted;
                _subscribedStats.OnBuffStopped -= HandleBuffStopped;
                _subscribedStats = null;
            }
        }

        private void ClearAllDisplays()
        {
            foreach (var kv in _activeDisplays)
            {
                if (kv.Value != null)
                {
                    Destroy(kv.Value.gameObject);
                }
            }

            _activeDisplays.Clear();
            _indefiniteOrder.Clear();
            _durationOrder.Clear();
        }

        private void HandleBuffStarted(Buff buff)
        {
            if (buff == null || _activeDisplays.ContainsKey(buff)) return;

            var go = Instantiate(_buffDisplayPrefab, _buffPanel);
            go.Initialize(buff);
            _activeDisplays.Add(buff, go);

            if (buff.IsIndefinite)
            {
                _indefiniteOrder.Add(buff);
            }
            else
            {
                _durationOrder.Add(buff);
            }

            RebuildOrder();
        }

        private void HandleBuffStopped(Buff buff)
        {
            if (buff == null) return;
            if (!_activeDisplays.TryGetValue(buff, out var display)) return;

            if (display != null)
            {
                Destroy(display.gameObject);
            }

            _activeDisplays.Remove(buff);
            _indefiniteOrder.Remove(buff);
            _durationOrder.Remove(buff);
        }

        private void RebuildOrder()
        {
            int idx = 0;
            foreach (var b in _indefiniteOrder)
            {
                if (_activeDisplays.TryGetValue(b, out var d) && d != null)
                {
                    d.transform.SetSiblingIndex(idx++);
                }
            }

            foreach (var b in _durationOrder)
            {
                if (_activeDisplays.TryGetValue(b, out var d) && d != null)
                {
                    d.transform.SetSiblingIndex(idx++);
                }
            }
        }

        private void Update()
        {
            // Refresh timers for duration buffs
            foreach (var kv in _activeDisplays)
            {
                if (kv.Value == null) continue;
                kv.Value.RefreshDuration();
            }
        }
    }
}

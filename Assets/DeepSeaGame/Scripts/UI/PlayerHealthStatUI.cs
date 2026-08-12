using System;
using System.Collections;
using System.Collections.Generic;
using MoreMountains.Tools;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace DeepSeaGame
{
    public class PlayerHealthStatUI : MonoBehaviour
    {
        [SerializeField] private RectTransform _background;
        [SerializeField] private Image _healthBarFg;
        [SerializeField] private TextMeshProUGUI _amountText;

        private float _originalBackgroundWidth;
        private int _baseMaxHealth;
        private int _lastKnownMaxHealth;

        private void Awake()
        {
            Player.OnAnyPlayerSpawned += Player_OnAnyPlayerSpawned;
            if (_background != null)
            {
                _originalBackgroundWidth = _background.sizeDelta.x;
            }
        }

        private void OnDestroy()
        {
            Player.OnAnyPlayerSpawned -= Player_OnAnyPlayerSpawned;
            if (Player.Instance != null)
            {
                Player.Instance.Character.NetHealthState.OnHitPointsChanged -= Player_OnPlayerHealthUpdated;
            }
        }

        private void Update()
        {
            if (Player.Instance == null || Player.Instance.Character == null)
            {
                return;
            }

            int runtimeMaxHealth = Player.Instance.Character.Stats.MaxHealth.GetValue();
            if (runtimeMaxHealth != _lastKnownMaxHealth)
            {
                _lastKnownMaxHealth = runtimeMaxHealth;
                RefreshView();
            }
        }

        private void Player_OnAnyPlayerSpawned(object sender, Player.PlayerIdEventArgs e)
        {
            if (Player.Instance == null)
            {
                return;
            }

            Player.Instance.Character.NetHealthState.OnHitPointsChanged += Player_OnPlayerHealthUpdated;
            _baseMaxHealth = Player.Instance.Character.CharacterData.BaseMaxHealth;
            _lastKnownMaxHealth = Player.Instance.Character.Stats.MaxHealth.GetValue();
            _originalBackgroundWidth = _background != null ? _background.sizeDelta.x : 0f;
            UpdateView(Player.Instance.Character.NetHealthState.HitPoints.Value, _lastKnownMaxHealth);
        }

        private void Player_OnPlayerHealthUpdated(object sender, NetworkHealthState.PointsChangedEventArgs e)
        {
            UpdateView(e.CurrentPoints, Player.Instance.Character.Stats.MaxHealth.GetValue());
        }

        private void RefreshView()
        {
            if (Player.Instance == null)
            {
                return;
            }

            UpdateView(Player.Instance.Character.NetHealthState.HitPoints.Value, Player.Instance.Character.Stats.MaxHealth.GetValue());
        }

        private void UpdateView(int currentAmount, int maxAmount)
        {
            float fill = maxAmount > 0 ? Mathf.Clamp01((float)currentAmount / maxAmount) : 0f;
            _healthBarFg.fillAmount = fill;
            _amountText.text = $"HP: {currentAmount}/{maxAmount}";

            if (_background != null && _originalBackgroundWidth > 0f && _baseMaxHealth > 0)
            {
                float widthRatio = maxAmount / (float)_baseMaxHealth;
                float newWidth = _originalBackgroundWidth * widthRatio;
                _background.sizeDelta = new Vector2(newWidth, _background.sizeDelta.y);
            }
        }
    }
}

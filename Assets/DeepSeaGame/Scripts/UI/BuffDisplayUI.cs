using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeepSeaGame
{
    public class BuffDisplayUI : MonoBehaviour
    {
        [SerializeField] private Image _buffIcon;
        [SerializeField] private TextMeshProUGUI _durationText;

        private Buff _boundBuff;

        public Buff BoundBuff => _boundBuff;

        public void Initialize(Buff buff)
        {
            _boundBuff = buff;
            if (_buffIcon != null)
            {
                _buffIcon.sprite = buff.Icon;
            }

            if (_durationText != null)
            {
                _durationText.text = $"{00}:{00:00}";
            }

            RefreshDuration();
        }

        public void RefreshDuration()
        {
            if (_durationText == null || _boundBuff == null) return;

            if (_boundBuff.IsIndefinite)
            {
                _durationText.gameObject.SetActive(false);
            }
            else
            {
                _durationText.gameObject.SetActive(true);
                float remaining = _boundBuff.RemainingDuration;
                int totalSeconds = Mathf.Max(0, Mathf.CeilToInt(remaining));
                int minutes = totalSeconds / 60;
                int seconds = totalSeconds % 60;
                _durationText.text = $"{minutes}:{seconds:00}";
            }
        }
        
        
    }
}

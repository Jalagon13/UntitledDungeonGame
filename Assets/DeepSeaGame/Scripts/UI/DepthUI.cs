using TMPro;
using UnityEngine;

namespace DeepSeaGame
{
    public class DepthUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _depthText;
        [SerializeField] private bool _useMetric = true;

        private float _metersPerUnit = 0.67f; // 1 tile ≈ 0.67m

        private void Update()
        {
            if (Player.Instance == null || _depthText == null)
                return;

            float relativeUnits = WorldManager.Instance.WorldDataStore.Height - Player.Instance.transform.position.y;

            if (_useMetric)
            {
                float depthMeters = relativeUnits * _metersPerUnit;
                _depthText.text = FormatDepth(depthMeters, "m");
            }
            else
            {
                float depthFeet = relativeUnits * _metersPerUnit * 3.28084f;
                _depthText.text = FormatDepth(depthFeet, "ft");
            }
        }

        private string FormatDepth(float value, string unit)
        {
            string sign = value > 0 ? "-" : "+";
            return $"{sign}{Mathf.Abs(value):0} {unit}";
        }
    }
}
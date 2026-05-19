using UnityEngine;

namespace UntitledDungeonGame
{
    public class LightSource : MonoBehaviour
    {
        [field: SerializeField, Range(0, 1)] public float LightIntensity { get; private set; } = 1f;
        [field: SerializeField, Range(0, 10)] public float LightRadius { get; private set; } = 5f;

        private Vector3 _lastWorldPosition;

        private void Start()
        {
            _lastWorldPosition = transform.position;

            if (LightmapManager.Instance != null)
            {
                LightmapManager.Instance.RegisterLightSource(this);
            }
        }

        private void OnEnable()
        {
            if (LightmapManager.Instance != null)
            {
                LightmapManager.Instance.RegisterLightSource(this);
            }
        }

        private void OnDisable()
        {
            if (LightmapManager.Instance != null)
            {
                LightmapManager.Instance.DeregisterLightSource(this);
            }
        }

        private void Update()
        {
            if (LightmapManager.Instance == null)
            {
                return;
            }

            float updateThreshold = 1f / LightmapManager.Instance.LightmapScale;

            if (Vector3.Distance(transform.position, _lastWorldPosition) >= updateThreshold)
            {
                LightmapManager.Instance.UpdateLightmap();
                _lastWorldPosition = transform.position;
            }
        }
    }
}

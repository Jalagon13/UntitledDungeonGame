using System;
using System.Collections;
using UnityEngine;

namespace DeepSeaGame
{
    public class UndergroundBackground : MonoBehaviour
    {
        [SerializeField] private GameObject _camera;
        [SerializeField] private float _parallaxEffect;

        private float _startPos, _yCenterLevel;
        private bool _initialized;

        private void FixedUpdate()
        {
            if (!_initialized) return;

            float distance = _camera.transform.position.x * _parallaxEffect;

            transform.position = new Vector3(_startPos + distance, _yCenterLevel, transform.position.z);
        }

        public void Initialize(WorldGenerationData worldGenerationData)
        {
            StartCoroutine(DelayInitialization(worldGenerationData));
        }

        private IEnumerator DelayInitialization(WorldGenerationData data)
        {
            yield return new WaitForSeconds(1f);

            _startPos = transform.position.x + (_camera.transform.position.x * _parallaxEffect);
            
            float undergroundHeight = data.UndergroundMaxYLevel - data.UndergroundMinYLevel;
            _yCenterLevel = (undergroundHeight * 0.5f) + data.UndergroundMinYLevel;
            GetComponent<SpriteRenderer>().size = new(data.WorldWidth, undergroundHeight);

            _initialized = true;
        }
    }
}

using System;
using System.Collections;
using UnityEngine;

namespace DeepSeaGame
{
    public class ParallaxBackground : MonoBehaviour
    {
        [SerializeField] private GameObject _camera;
        [SerializeField] private float _parallaxEffect;
        
        private float _length, _startPos;
        private bool _initialized;
        
        private void FixedUpdate() 
        {
            if(!_initialized) return;
        
            float temp = _camera.transform.position.x * (1 - _parallaxEffect);
            float distance = _camera.transform.position.x * _parallaxEffect;
            
            transform.position = new Vector3(_startPos + distance, transform.position.y, transform.position.z);
            
            if(temp > _startPos + _length)
            {
                _startPos += _length;
            } 
            else if (temp < _startPos - _length)
            {
                _startPos -= _length;
            }
        }

        public void Initialize(Vector3 spawnPosition)
        {
            StartCoroutine(DelayInitialization());
        }
        
        private IEnumerator DelayInitialization()
        {
            yield return new WaitForSeconds(1f);

            // Compute start position relative to the camera so we don't double-count
            // the camera's world position when applying the parallax distance.
            _startPos = transform.position.x - _camera.transform.position.x * _parallaxEffect;
            _length = GetComponent<SpriteRenderer>().bounds.size.x;
            
            _initialized = true;
        }
    }
}

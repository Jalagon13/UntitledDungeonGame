using System.Collections.Generic;
using UnityEngine;

namespace DeepSeaGame
{
    public class ParallaxBackground : MonoBehaviour
    {
        public ParallaxCamera ParallaxCamera;
        
        private List<ParallaxLayer> _parallaxLayers = new();

        private void Start()
        {
            if (ParallaxCamera == null)
            {
                if (Camera.main != null)
                {
                    ParallaxCamera = Camera.main.GetComponent<ParallaxCamera>();
                }

                if (ParallaxCamera == null)
                {
                    ParallaxCamera = UnityEngine.Object.FindFirstObjectByType<ParallaxCamera>();
                }
            }

            if (ParallaxCamera != null)
            {
                ParallaxCamera.OnCameraTranslate += Move;
            }

            _parallaxLayers.Clear();

            ParallaxLayer[] layers = GetComponentsInChildren<ParallaxLayer>();
            foreach (var layer in layers)
            {
                if (layer != null)
                {
                    _parallaxLayers.Add(layer);
                }
            }
        }

        private void OnDestroy()
        {
            if (ParallaxCamera != null)
            {
                ParallaxCamera.OnCameraTranslate -= Move;
            }
        }

        private void Move(float delta)
        {
            foreach (ParallaxLayer layer in _parallaxLayers)
            {
                layer.Move(delta);
            }
        }
    }
}
using UnityEngine;

namespace DeepSeaGame
{
    public class ParallaxCamera : MonoBehaviour
    {
        public ParallaxCameraDelegate OnCameraTranslate;
        public delegate void ParallaxCameraDelegate(float deltaMovement);

        private float _oldPosition;

        private void Start()
        {
            _oldPosition = transform.position.x;
        }

        private void Update()
        {
            if (transform.position.x != _oldPosition)
            {
                if (OnCameraTranslate != null)
                {
                    float delta = _oldPosition - transform.position.x;
                    OnCameraTranslate(delta);
                }

                _oldPosition = transform.position.x;
            }
        }
    }
}
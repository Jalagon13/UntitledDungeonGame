using UnityEngine;

namespace DeepSeaGame
{
    public class SurfaceParallax : ParallaxLayer
    {
        private SpriteRenderer[] _tiles = new SpriteRenderer[3];
        private float _tileWidth;
        private Transform _playerTransform;

        protected override void Awake()
        {
            base.Awake();

            if (_spriteRenderer == null)
            {
                Debug.LogError("SurfaceParallax requires a SpriteRenderer on the same GameObject.");
                enabled = false;
                return;
            }

            for (int i = 0; i < 3; i++)
            {
                var go = new GameObject("SurfaceTile_" + i);
                go.transform.parent = transform;
                go.transform.localRotation = Quaternion.identity;
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = _spriteRenderer.sprite;
                sr.sharedMaterial = _spriteRenderer.sharedMaterial;
                sr.sortingLayerID = _spriteRenderer.sortingLayerID;
                sr.sortingOrder = _spriteRenderer.sortingOrder;
                _tiles[i] = sr;
            }

            _spriteRenderer.enabled = false;
        }

        public override void Initialize(WorldGenerationData worldGenerationData)
        {
            base.Initialize(worldGenerationData);

            if (_spriteRenderer == null || _spriteRenderer.sprite == null) return;

            _tileWidth = _spriteRenderer.sprite.bounds.size.x * Mathf.Abs(transform.lossyScale.x);

            for (int i = 0; i < 3; i++)
            {
                _tiles[i].transform.localPosition = new Vector3((i - 1) * _tileWidth, 0f, 0f);
            }
        }

        public void SetPlayerTransform(Transform player)
        {
            _playerTransform = player;
        }

        public void ResetToPlayerPosition(Vector3 playerPosition)
        {
            if (!_hasBeenInitalized) return;

            for (int i = 0; i < 3; i++)
            {
                _tiles[i].transform.position = new Vector3(playerPosition.x + (i - 1) * _tileWidth, transform.position.y, transform.position.z);
            }
        }

        public override void Move(float delta)
        {
            if (!_hasBeenInitalized) return;

            float move = -delta * ParallaxFactor;
            for (int i = 0; i < 3; i++)
            {
                _tiles[i].transform.localPosition += new Vector3(move, 0f, 0f);
            }

            float centerX = _playerTransform != null ? _playerTransform.position.x : (Camera.main != null ? Camera.main.transform.position.x : transform.position.x);
            RepositionIfNeeded(centerX);
        }

        private void RepositionIfNeeded(float centerX)
        {
            for (int i = 0; i < 3; i++)
            {
                var t = _tiles[i].transform;
                float dx = t.position.x - centerX;
                if (dx < -_tileWidth)
                {
                    t.position += new Vector3(_tileWidth * 3f, 0f, 0f);
                }
                else if (dx > _tileWidth)
                {
                    t.position -= new Vector3(_tileWidth * 3f, 0f, 0f);
                }
            }
        }
    }
}

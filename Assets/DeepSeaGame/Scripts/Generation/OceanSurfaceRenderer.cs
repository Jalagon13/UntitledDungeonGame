using UnityEngine;

namespace DeepSeaGame
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class OceanSurfaceRenderer : MonoBehaviour
    {
        [SerializeField] private WorldGenerationData _worldGenerationData;
        [SerializeField, Min(0.01f)] private float _surfaceHeight = 0.35f;
        [SerializeField] private Material _oceanStencilMaterial;
        [SerializeField] private Shader _shader;
        [SerializeField] private Color _surfaceColor = new(0.55f, 0.9f, 1f, 0.7f);
        [SerializeField] private int _sortingOrder = 100;
        
        private SpriteRenderer _spriteRenderer;

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            Refresh();
        }

        private void Start()
        {
            Refresh();
        }

        public void Initialize(WorldGenerationData worldGenerationData)
        {
            _worldGenerationData = worldGenerationData;
            Refresh();
        }

        public void Refresh()
        {
            if (_worldGenerationData == null)
            {
                return;
            }

            if (_spriteRenderer == null)
            {
                _spriteRenderer = GetComponent<SpriteRenderer>();
            }
            
            float width = _worldGenerationData.WorldWidth;
            float seaLevelY = _worldGenerationData.SeaLevelY;

            _spriteRenderer.sprite = OceanRenderSpriteUtility.UnitSprite;
            _spriteRenderer.color = Color.white;
            _spriteRenderer.sortingOrder = _sortingOrder;
            ConfigureStencilMaterial();

            transform.position = new Vector3(width * 0.5f, seaLevelY - (_surfaceHeight * 0.5f));
            transform.localScale = new Vector3(width, _surfaceHeight, 1f);
        }

        private void ConfigureStencilMaterial()
        {
            if (_oceanStencilMaterial == null)
            {
                if (_shader == null)
                {
                    Debug.LogWarning("Could not find DeepSeaGame/OceanStencilRead. Ocean surface will render without AirTilemap cutouts.");
                    return;
                }

                _oceanStencilMaterial = new Material(_shader)
                {
                    name = "Runtime Ocean Surface Stencil Read",
                    hideFlags = HideFlags.DontSave
                };
            }

            _oceanStencilMaterial.SetColor("_Color", _surfaceColor);
            _spriteRenderer.sharedMaterial = _oceanStencilMaterial;
        }
    }
}

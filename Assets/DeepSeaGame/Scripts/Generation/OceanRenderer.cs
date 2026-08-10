using UnityEngine;

namespace DeepSeaGame
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class OceanRenderer : MonoBehaviour
    {
        [SerializeField] private WorldGenerationData _worldGenerationData;
        [SerializeField] private Material _oceanStencilMaterial;
        [SerializeField] private Shader _shader;
        [SerializeField] private Color _waterColor = new(0.05f, 0.42f, 0.72f, 0.5f);
        [SerializeField] private int _sortingOrder = 99;

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
            if(_worldGenerationData == null)
            {
                return; 
            }
        
            if (_spriteRenderer == null)
            {
                _spriteRenderer = GetComponent<SpriteRenderer>();
            }

            float width = _worldGenerationData.WorldWidth;
            float height = _worldGenerationData.SeaLevelY;

            _spriteRenderer.sprite = OceanRenderSpriteUtility.UnitSprite;
            _spriteRenderer.color = Color.white;
            _spriteRenderer.sortingOrder = _sortingOrder;
            ConfigureStencilMaterial();

            transform.position = new Vector3(width * 0.5f, height * 0.5f);
            transform.localScale = new Vector3(width, height, 1f);
        }

        private void ConfigureStencilMaterial()
        {
            if (_oceanStencilMaterial == null)
            {
                if (_shader == null)
                {
                    Debug.LogWarning("Could not find DeepSeaGame/OceanStencilRead. Ocean will render without AirTilemap cutouts.");
                    return;
                }

                _oceanStencilMaterial = new Material(_shader)
                {
                    name = "Runtime Ocean Stencil Read",
                    hideFlags = HideFlags.DontSave
                };
            }

            _oceanStencilMaterial.SetColor("_Color", _waterColor);
            _spriteRenderer.sharedMaterial = _oceanStencilMaterial;
        }
    }
}

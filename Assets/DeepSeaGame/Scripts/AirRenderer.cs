using UnityEngine;
using UnityEngine.Tilemaps;

namespace DeepSeaGame
{
    public class AirRenderer : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _camBlueRenderer;
        
        private TilemapRenderer _airTm;
        
        private void Awake() 
        {
            _airTm = GetComponent<TilemapRenderer>();    
        }
        
        
    }
}

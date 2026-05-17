using UnityEngine;

namespace UntitledDungeonGame
{
    public class SwingObject : MonoBehaviour
    {
        public void OnStartSwing()
        {
            Debug.Log($"Start Swinging {gameObject.name}");
        }
        
        public void OnEndSwing()
        {
            Debug.Log($"End Swinging {gameObject.name}");
        }
    }
}

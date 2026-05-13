using UnityEngine;

namespace UntitledDungeonGame
{
    [CreateAssetMenu(fileName = "New Character Data", menuName = "Data/CharacterData")]
    public class CharacterDataSO : ScriptableObject
    {
        [Tooltip("Base Speed for character")]
        public float BaseSpeed;
    }
}

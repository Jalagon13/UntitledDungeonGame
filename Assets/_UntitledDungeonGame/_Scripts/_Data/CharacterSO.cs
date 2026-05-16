using UnityEngine;

namespace UntitledDungeonGame
{
    [CreateAssetMenu(fileName = "New Character Data", menuName = "Data/CharacterData")]
    public class CharacterSO : ScriptableObject
    {
        [Header("Core Stats")]
        [Tooltip("Base Speed for character")]
        public float BaseSpeed;


        [Space]
        [Header("Movement & Physics")]
        [Tooltip("If false, the NPC will remain idle and not move")]
        public bool CanMove = true;
        [Tooltip("Smaller values = slower transition to desired direction")]
        public float TurnSharpness = 5f;

        [Space]
        [Header("AI Parameters")]
        [Tooltip("Indicates whether the character is an NPC")]
        public bool IsNpc;
    }
}

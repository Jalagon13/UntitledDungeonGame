using System;
using UnityEngine;

namespace UntitledDungeonGame
{
    [Serializable]
    public class AnimationConfigSO
    {
        public AnimationClip SideMoveClip;
        public AnimationClip SideIdleClip;
        public AnimationClip FrontMoveClip;
        public AnimationClip FrontIdleClip;
        public AnimationClip BackMoveClip;
        public AnimationClip BackIdleClip;
    }

    public enum CardinalDirection
    {
        None,
        North,
        South,
        West,
        East
    }

    public enum CharacterStateMachine
    {
        Player,
        BasicNpc
    }

    public enum MovementState
    {
        Idle,
        Moving,
        Knockback,
        Pursuing,
        Fleeing
    }

    public enum LifeState
    {
        Alive,
        IFrame,
        Dead
    }

    public enum ToolType
    {
        Pickaxe,
        Axe,
        Sword
    }
    
    public enum MiningState
    {
        Idle,
        Detecting
    }
    
    public enum PlacingState
    {
        Idle,
        Placing
    }

    public struct TileVisibility
    {
        public int Visibility; // 0 = transparent, 1 = opaque

        public TileVisibility(int visibility)
        {
            Visibility = visibility;
        }
    }
}
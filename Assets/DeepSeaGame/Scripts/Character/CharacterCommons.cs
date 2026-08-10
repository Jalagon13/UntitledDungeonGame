using System;
using UnityEngine;

namespace DeepSeaGame
{
    public enum Direction
    {
        None,
        Up,
        Down,
        Left,
        Right
    }
    
    public enum Status
    {
        InWater,
        InAir
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
        Drill,
        Spear
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
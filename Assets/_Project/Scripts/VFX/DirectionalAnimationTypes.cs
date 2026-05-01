using System;
using UnityEngine;

public enum DirectionalFacing
{
    Down = 0,
    DownRight = 1,
    Right = 2,
    UpRight = 3,
    Up = 4,
    UpLeft = 5,
    Left = 6,
    DownLeft = 7
}

public enum DirectionalAnimationState
{
    Idle = 0,
    Move = 1,
    Attack = 2,
    Dash = 3,
    Parry = 4,
    Hit = 5,
    Death = 6,
    Cast = 7,
    Charge = 8,
    Broken = 9,
    Stagger = 10,
    Finisher = 11,
    Special = 12
}

public enum DirectionalFacingMode
{
    FourDirection = 0,
    EightDirection = 1
}

public enum DirectionalFacingPriority
{
    PreferAimDirection = 0,
    PreferMovementDirection = 1
}

public enum IsoFacingSource
{
    LastMovement = 0,
    Movement = 1,
    AimAction = 2,
    AimFallback = 3,
    VelocityFallback = 4
}

[Serializable]
public struct DirectionalAnimationClipSlot
{
    public DirectionalAnimationState state;
    public DirectionalFacing direction;
    public AnimationClip clip;
}

[Serializable]
public struct DirectionalAnimationStateFallback
{
    public DirectionalAnimationState state;
    public DirectionalAnimationState fallbackState;
}

[Serializable]
public struct DirectionalAnimationDirectionFallback
{
    public DirectionalFacing direction;
    public DirectionalFacing fallbackDirection;
}

public struct DirectionalClipResolution
{
    public AnimationClip clip;
    public DirectionalAnimationState requestedState;
    public DirectionalFacing requestedDirection;
    public DirectionalAnimationState resolvedState;
    public DirectionalFacing resolvedDirection;
    public bool usedFallback;
    public bool shouldFlipX;

    public string StateName => DirectionalAnimationUtility.GetStateName(resolvedState, resolvedDirection);
    public string ClipName => clip != null ? clip.name : string.Empty;
}

public static class DirectionalAnimationUtility
{
    public static readonly DirectionalFacing[] AllDirections =
    {
        DirectionalFacing.Down,
        DirectionalFacing.DownRight,
        DirectionalFacing.Right,
        DirectionalFacing.UpRight,
        DirectionalFacing.Up,
        DirectionalFacing.UpLeft,
        DirectionalFacing.Left,
        DirectionalFacing.DownLeft
    };

    public static readonly DirectionalAnimationState[] AllStates =
    {
        DirectionalAnimationState.Idle,
        DirectionalAnimationState.Move,
        DirectionalAnimationState.Attack,
        DirectionalAnimationState.Dash,
        DirectionalAnimationState.Parry,
        DirectionalAnimationState.Hit,
        DirectionalAnimationState.Death,
        DirectionalAnimationState.Cast,
        DirectionalAnimationState.Charge,
        DirectionalAnimationState.Broken,
        DirectionalAnimationState.Stagger,
        DirectionalAnimationState.Finisher,
        DirectionalAnimationState.Special
    };

    public static string GetStateName(DirectionalAnimationState state, DirectionalFacing direction)
    {
        return $"{state}_{direction}";
    }

    public static DirectionalFacing VectorToEightWay(Vector2 direction, DirectionalFacing fallback = DirectionalFacing.Down)
    {
        if (direction.sqrMagnitude <= 0.0001f)
            return fallback;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        if (angle < 0f)
            angle += 360f;

        int index = Mathf.RoundToInt(angle / 45f) % 8;
        switch (index)
        {
            case 0: return DirectionalFacing.Right;
            case 1: return DirectionalFacing.UpRight;
            case 2: return DirectionalFacing.Up;
            case 3: return DirectionalFacing.UpLeft;
            case 4: return DirectionalFacing.Left;
            case 5: return DirectionalFacing.DownLeft;
            case 6: return DirectionalFacing.Down;
            case 7: return DirectionalFacing.DownRight;
            default: return fallback;
        }
    }

    public static DirectionalFacing VectorToFourWay(Vector2 direction, DirectionalFacing fallback = DirectionalFacing.Down)
    {
        if (direction.sqrMagnitude <= 0.0001f)
            return fallback;

        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
            return direction.x >= 0f ? DirectionalFacing.Right : DirectionalFacing.Left;

        return direction.y >= 0f ? DirectionalFacing.Up : DirectionalFacing.Down;
    }

    public static bool IsDiagonal(DirectionalFacing direction)
    {
        return direction == DirectionalFacing.DownRight ||
               direction == DirectionalFacing.UpRight ||
               direction == DirectionalFacing.UpLeft ||
               direction == DirectionalFacing.DownLeft;
    }

    public static bool IsLeftFacing(DirectionalFacing direction)
    {
        return direction == DirectionalFacing.Left ||
               direction == DirectionalFacing.UpLeft ||
               direction == DirectionalFacing.DownLeft;
    }

    public static DirectionalFacing MirrorLeftToRight(DirectionalFacing direction)
    {
        switch (direction)
        {
            case DirectionalFacing.Left:
                return DirectionalFacing.Right;
            case DirectionalFacing.UpLeft:
                return DirectionalFacing.UpRight;
            case DirectionalFacing.DownLeft:
                return DirectionalFacing.DownRight;
            default:
                return direction;
        }
    }
}

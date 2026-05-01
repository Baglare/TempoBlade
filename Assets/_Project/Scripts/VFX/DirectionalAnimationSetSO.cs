using UnityEngine;

[CreateAssetMenu(fileName = "DirectionalAnimationSet", menuName = "TempoBlade/Animation/Directional Animation Set")]
public class DirectionalAnimationSetSO : ScriptableObject
{
    [Header("Identity")]
    public string characterId = "Character";
    public string displayName = "Character";

    [Header("Direction")]
    public bool supportsEightDirections = false;
    public DirectionalFacingMode defaultFacingMode = DirectionalFacingMode.FourDirection;
    public DirectionalFacing defaultFacing = DirectionalFacing.Down;
    public DirectionalFacing downRightFallback = DirectionalFacing.Right;
    public DirectionalFacing upRightFallback = DirectionalFacing.Right;
    public DirectionalFacing upLeftFallback = DirectionalFacing.Left;
    public DirectionalFacing downLeftFallback = DirectionalFacing.Left;

    [Header("Fallback")]
    public AnimationClip defaultClip;
    public DirectionalAnimationStateFallback[] stateFallbacks =
    {
        new DirectionalAnimationStateFallback { state = DirectionalAnimationState.Dash, fallbackState = DirectionalAnimationState.Move },
        new DirectionalAnimationStateFallback { state = DirectionalAnimationState.Parry, fallbackState = DirectionalAnimationState.Attack },
        new DirectionalAnimationStateFallback { state = DirectionalAnimationState.Attack, fallbackState = DirectionalAnimationState.Idle },
        new DirectionalAnimationStateFallback { state = DirectionalAnimationState.Hit, fallbackState = DirectionalAnimationState.Idle },
        new DirectionalAnimationStateFallback { state = DirectionalAnimationState.Cast, fallbackState = DirectionalAnimationState.Attack },
        new DirectionalAnimationStateFallback { state = DirectionalAnimationState.Charge, fallbackState = DirectionalAnimationState.Move },
        new DirectionalAnimationStateFallback { state = DirectionalAnimationState.Broken, fallbackState = DirectionalAnimationState.Stagger },
        new DirectionalAnimationStateFallback { state = DirectionalAnimationState.Stagger, fallbackState = DirectionalAnimationState.Hit },
        new DirectionalAnimationStateFallback { state = DirectionalAnimationState.Finisher, fallbackState = DirectionalAnimationState.Attack },
        new DirectionalAnimationStateFallback { state = DirectionalAnimationState.Special, fallbackState = DirectionalAnimationState.Idle }
    };
    public DirectionalAnimationDirectionFallback[] directionFallbacks;

    [Header("Flip")]
    public bool useSpriteFlip = true;
    public bool leftRightClipsAreSeparate = false;
    public bool flipWhenFacingLeft = true;

    [Header("Clips")]
    public DirectionalAnimationClipSlot[] clips;

    public DirectionalClipResolution ResolveClip(DirectionalAnimationState requestedState, DirectionalFacing requestedDirection)
    {
        DirectionalClipResolution resolution = new DirectionalClipResolution
        {
            requestedState = requestedState,
            requestedDirection = requestedDirection,
            resolvedState = requestedState,
            resolvedDirection = requestedDirection
        };

        DirectionalAnimationState state = requestedState;
        for (int stateStep = 0; stateStep < 8; stateStep++)
        {
            DirectionalFacing direction = ResolveDirectionFallback(requestedDirection);
            for (int directionStep = 0; directionStep < 8; directionStep++)
            {
                DirectionalFacing lookupDirection = ResolveMirroredLookupDirection(direction);
                AnimationClip clip = FindClip(state, lookupDirection);
                if (clip != null)
                {
                    resolution.clip = clip;
                    resolution.resolvedState = state;
                    resolution.resolvedDirection = direction;
                    resolution.usedFallback = state != requestedState || direction != requestedDirection || lookupDirection != direction;
                    resolution.shouldFlipX = ShouldFlip(requestedDirection, lookupDirection);
                    return resolution;
                }

                DirectionalFacing nextDirection = ResolveDirectionFallback(direction);
                if (nextDirection == direction)
                    break;

                direction = nextDirection;
            }

            DirectionalAnimationState nextState = ResolveStateFallback(state);
            if (nextState == state)
                break;

            state = nextState;
        }

        resolution.clip = defaultClip;
        resolution.resolvedState = requestedState;
        resolution.resolvedDirection = ResolveDirectionFallback(requestedDirection);
        resolution.usedFallback = defaultClip != null;
        resolution.shouldFlipX = ShouldFlip(requestedDirection, resolution.resolvedDirection);
        return resolution;
    }

    public DirectionalFacing ResolveDirectionFallback(DirectionalFacing direction)
    {
        if (!supportsEightDirections && DirectionalAnimationUtility.IsDiagonal(direction))
            return ResolveDiagonalFallback(direction);

        if (directionFallbacks != null)
        {
            for (int i = 0; i < directionFallbacks.Length; i++)
            {
                if (directionFallbacks[i].direction == direction)
                    return directionFallbacks[i].fallbackDirection;
            }
        }

        return direction;
    }

    public DirectionalAnimationState ResolveStateFallback(DirectionalAnimationState state)
    {
        if (stateFallbacks != null)
        {
            for (int i = 0; i < stateFallbacks.Length; i++)
            {
                if (stateFallbacks[i].state == state)
                    return stateFallbacks[i].fallbackState;
            }
        }

        return state == DirectionalAnimationState.Idle ? DirectionalAnimationState.Idle : DirectionalAnimationState.Idle;
    }

    private DirectionalFacing ResolveDiagonalFallback(DirectionalFacing direction)
    {
        switch (direction)
        {
            case DirectionalFacing.DownRight:
                return downRightFallback;
            case DirectionalFacing.UpRight:
                return upRightFallback;
            case DirectionalFacing.UpLeft:
                return upLeftFallback;
            case DirectionalFacing.DownLeft:
                return downLeftFallback;
            default:
                return direction;
        }
    }

    private DirectionalFacing ResolveMirroredLookupDirection(DirectionalFacing direction)
    {
        if (useSpriteFlip && !leftRightClipsAreSeparate)
            return DirectionalAnimationUtility.MirrorLeftToRight(direction);

        return direction;
    }

    private bool ShouldFlip(DirectionalFacing requestedDirection, DirectionalFacing resolvedDirection)
    {
        if (!useSpriteFlip || !flipWhenFacingLeft)
            return false;

        if (leftRightClipsAreSeparate)
            return false;

        return DirectionalAnimationUtility.IsLeftFacing(requestedDirection) ||
               DirectionalAnimationUtility.IsLeftFacing(resolvedDirection);
    }

    private AnimationClip FindClip(DirectionalAnimationState state, DirectionalFacing direction)
    {
        if (clips == null)
            return null;

        for (int i = 0; i < clips.Length; i++)
        {
            if (clips[i].state == state && clips[i].direction == direction && clips[i].clip != null)
                return clips[i].clip;
        }

        return null;
    }
}

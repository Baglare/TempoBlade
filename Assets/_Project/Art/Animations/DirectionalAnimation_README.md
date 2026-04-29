# Directional Animation V1

1. Select sliced sprites in frame order, then open `TempoBlade/Animation/Directional Clip Builder`.
2. Set `Character`, `State`, `Direction`, `Frame Rate`, and `Output Folder`.
3. Create clips named like `Knight_Idle_Down.anim`.
4. Run `TempoBlade/Animation/Create Base Directional Animator Controller` once to create the standard controller.
5. Create a `DirectionalAnimationSetSO`, assign clips per state/direction, then assign the SO and base controller to `CharacterDirectionalAnimator`.

State names used by the runtime follow `State_Direction`, for example `Idle_Down`, `Move_Right`, `Attack_UpLeft`.

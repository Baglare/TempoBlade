using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class CharacterDirectionalAnimator : MonoBehaviour
{
    [Header("Data")]
    public DirectionalAnimationSetSO animationSet;
    public RuntimeAnimatorController baseController;
    public bool useRuntimeOverrideController = true;

    [Header("References")]
    public IsoVisualRoot visualRoot;
    public IsoFacingController facingController;
    public Animator animator;
    public SpriteRenderer bodyRenderer;
    public Rigidbody2D body;

    [Header("State")]
    public DirectionalAnimationState defaultState = DirectionalAnimationState.Idle;
    public float movingThreshold = 0.05f;
    public bool playAutomatically = true;

    [Header("Warnings")]
    public bool warnWhenMissingSetup = true;
    public bool debugLog = false;

    [Header("Debug")]
    [SerializeField] private DirectionalAnimationState currentState;
    [SerializeField] private DirectionalFacing currentDirection = DirectionalFacing.Down;
    [SerializeField] private string selectedClipName;
    [SerializeField] private bool fallbackUsed;
    [SerializeField] private bool supportsEightDirections;
    [SerializeField] private Vector2 currentFacingVector = Vector2.down;

    private readonly List<KeyValuePair<AnimationClip, AnimationClip>> overridePairs = new List<KeyValuePair<AnimationClip, AnimationClip>>();
    private AnimatorOverrideController runtimeOverrideController;
    private DirectionalAnimationState requestedBaseState;
    private DirectionalAnimationState pulseState;
    private float pulseEndTime;
    private int pulsePriority;
    private Vector3 lastPosition;
    private string lastPlayedStateName;
    private bool setupWarningIssued;
    private bool clipWarningIssued;

    public DirectionalAnimationState CurrentState => currentState;
    public DirectionalFacing CurrentDirection => currentDirection;
    public string SelectedClipName => selectedClipName;
    public bool FallbackUsed => fallbackUsed;
    public Vector2 CurrentFacingVector => currentFacingVector;

    private void Awake()
    {
        requestedBaseState = defaultState;
        lastPosition = transform.position;
        ResolveReferences();
        RebuildOverrideController();
    }

    private void OnEnable()
    {
        lastPosition = transform.position;
        ResolveReferences();
        RebuildOverrideController();
    }

    private void OnValidate()
    {
        movingThreshold = Mathf.Max(0.001f, movingThreshold);
        ResolveReferences();
    }

    private void LateUpdate()
    {
        if (!playAutomatically)
            return;

        PlayResolvedState(ResolveRequestedState(), ResolveFacing());
        lastPosition = transform.position;
    }

    [ContextMenu("Resolve Directional Animator References")]
    public void ResolveReferences()
    {
        visualRoot ??= GetComponent<IsoVisualRoot>();
        if (visualRoot != null)
            visualRoot.Resolve();

        facingController ??= GetComponent<IsoFacingController>();
        body ??= GetComponent<Rigidbody2D>();

        if (bodyRenderer == null && visualRoot != null)
            bodyRenderer = visualRoot.bodyRenderer;
        bodyRenderer ??= GetComponentInChildren<SpriteRenderer>(true);

        if (animator == null && bodyRenderer != null)
            animator = bodyRenderer.GetComponentInParent<Animator>();
        animator ??= GetComponentInChildren<Animator>(true);
    }

    public void SetBaseState(DirectionalAnimationState state)
    {
        requestedBaseState = state;
    }

    public void RequestState(DirectionalAnimationState state, float duration, int priority = 10)
    {
        if (duration <= 0f)
            return;

        if (Time.time < pulseEndTime && priority < pulsePriority)
            return;

        pulseState = state;
        pulsePriority = priority;
        pulseEndTime = Time.time + duration;
    }

    public void ClearPulseState(DirectionalAnimationState state)
    {
        if (Time.time < pulseEndTime && pulseState == state)
            pulseEndTime = 0f;
    }

    [ContextMenu("Rebuild Runtime Override Controller")]
    public void RebuildOverrideController()
    {
        if (animationSet == null || animator == null)
            return;

        RuntimeAnimatorController source = baseController != null ? baseController : animator.runtimeAnimatorController;
        if (source == null)
            return;

        if (!useRuntimeOverrideController)
        {
            animator.runtimeAnimatorController = source;
            return;
        }

        runtimeOverrideController = new AnimatorOverrideController(source);
        overridePairs.Clear();
        runtimeOverrideController.GetOverrides(overridePairs);

        for (int i = 0; i < overridePairs.Count; i++)
        {
            AnimationClip original = overridePairs[i].Key;
            if (original == null)
                continue;

            if (!TryParseStateName(original.name, out DirectionalAnimationState state, out DirectionalFacing direction))
                continue;

            DirectionalClipResolution resolution = animationSet.ResolveClip(state, direction);
            if (resolution.clip != null)
                overridePairs[i] = new KeyValuePair<AnimationClip, AnimationClip>(original, resolution.clip);
        }

        runtimeOverrideController.ApplyOverrides(overridePairs);
        animator.runtimeAnimatorController = runtimeOverrideController;
    }

    private DirectionalAnimationState ResolveRequestedState()
    {
        if (Time.time < pulseEndTime)
            return pulseState;

        return requestedBaseState;
    }

    private DirectionalFacing ResolveFacing()
    {
        Vector2 direction = Vector2.zero;
        if (facingController != null)
            direction = facingController.CurrentDirection;

        if (direction.sqrMagnitude <= 0.0001f && body != null)
            direction = body.linearVelocity;

        if (direction.sqrMagnitude <= 0.0001f)
        {
            Vector2 delta = (Vector2)(transform.position - lastPosition);
            if (delta.sqrMagnitude > movingThreshold * movingThreshold * Time.deltaTime * Time.deltaTime)
                direction = delta;
        }

        if (direction.sqrMagnitude > 0.0001f)
            currentFacingVector = direction.normalized;

        DirectionalFacing fallback = currentDirection;
        currentDirection = DirectionalAnimationUtility.VectorToEightWay(currentFacingVector, fallback);
        return currentDirection;
    }

    private void PlayResolvedState(DirectionalAnimationState state, DirectionalFacing direction)
    {
        currentState = state;
        supportsEightDirections = animationSet != null && animationSet.supportsEightDirections;

        if (animationSet == null || animator == null)
        {
            WarnMissingSetup();
            return;
        }

        DirectionalClipResolution resolution = animationSet.ResolveClip(state, direction);
        selectedClipName = resolution.ClipName;
        fallbackUsed = resolution.usedFallback;

        if (bodyRenderer != null && animationSet.useSpriteFlip)
            bodyRenderer.flipX = resolution.shouldFlipX;

        if (resolution.clip == null)
        {
            WarnMissingClip(state, direction);
            return;
        }

        string stateName = resolution.StateName;
        int stateHash = Animator.StringToHash(stateName);
        if (!animator.HasState(0, stateHash))
        {
            WarnMissingSetup();
            return;
        }

        if (stateName != lastPlayedStateName)
        {
            animator.Play(stateHash, 0, 0f);
            lastPlayedStateName = stateName;
            if (debugLog)
                Debug.Log($"[DirectionalAnimator] {name}: {stateName} -> {selectedClipName} fallback={fallbackUsed}", this);
        }
    }

    private void WarnMissingSetup()
    {
        if (!warnWhenMissingSetup || setupWarningIssued)
            return;

        setupWarningIssued = true;
        Debug.LogWarning($"[DirectionalAnimator] {name}: AnimationSet, Animator veya controller eksik. Directional animation pasif kalacak.", this);
    }

    private void WarnMissingClip(DirectionalAnimationState state, DirectionalFacing direction)
    {
        if (!warnWhenMissingSetup || clipWarningIssued)
            return;

        clipWarningIssued = true;
        Debug.LogWarning($"[DirectionalAnimator] {name}: {state}_{direction} icin clip bulunamadi. Mevcut gorsel davranis korunuyor.", this);
    }

    private static bool TryParseStateName(string stateName, out DirectionalAnimationState state, out DirectionalFacing direction)
    {
        state = DirectionalAnimationState.Idle;
        direction = DirectionalFacing.Down;

        if (string.IsNullOrEmpty(stateName))
            return false;

        int separator = stateName.LastIndexOf('_');
        if (separator <= 0 || separator >= stateName.Length - 1)
            return false;

        string statePart = stateName.Substring(0, separator);
        string directionPart = stateName.Substring(separator + 1);
        return System.Enum.TryParse(statePart, out state) &&
               System.Enum.TryParse(directionPart, out direction);
    }
}

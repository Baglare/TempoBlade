using UnityEngine;

[DisallowMultipleComponent]
public class IsoFacingController : MonoBehaviour
{
    [Header("Input")]
    public bool preferAimDirection = true;
    public DirectionalFacingPriority facingPriority = DirectionalFacingPriority.PreferAimDirection;
    public bool useEightWayFacing = true;
    public float movingThreshold = 0.05f;
    public float movementInputDeadzone = 0.01f;

    [Header("References")]
    public Rigidbody2D body;
    public PlayerController playerController;
    public PlayerCombat playerCombat;
    public Animator animator;
    public SpriteRenderer spriteFlipTarget;

    [Header("Fallback")]
    public bool applySpriteFlip = false;

    [Header("Debug")]
    [SerializeField] private Vector2 currentDirection = Vector2.right;
    [SerializeField] private Vector2 movementFacingDirection = Vector2.right;
    [SerializeField] private Vector2 aimDirection = Vector2.right;
    [SerializeField] private Vector2 actionFacingDirection = Vector2.right;
    [SerializeField] private Vector2 storedIdleFacingDirection = Vector2.right;
    [SerializeField] private Vector2 currentVisualFacingDirection = Vector2.right;
    [SerializeField] private bool actionFacingOverrideActive;
    [SerializeField] private bool hasStoredActionFacing;
    [SerializeField] private IsoFacingSource chosenFacingSource = IsoFacingSource.LastMovement;
    [SerializeField] private Vector2 rawMoveInput;
    [SerializeField] private bool hasMoveInput;
    [SerializeField] private int facingIndex;
    [SerializeField] private bool isMoving;

    private static readonly int FacingXHash = Animator.StringToHash("FacingX");
    private static readonly int FacingYHash = Animator.StringToHash("FacingY");
    private static readonly int FacingIndexHash = Animator.StringToHash("FacingIndex");
    private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
    private Vector3 lastWorldPosition;

    public Vector2 CurrentDirection => currentDirection;
    public Vector2 MovementFacingDirection => movementFacingDirection;
    public Vector2 AimDirection => aimDirection;
    public Vector2 ActionFacingDirection => actionFacingDirection;
    public Vector2 CurrentVisualFacingDirection => currentVisualFacingDirection;
    public bool ActionFacingOverrideActive => actionFacingOverrideActive;
    public IsoFacingSource ChosenFacingSource => chosenFacingSource;
    public Vector2 RawMoveInput => rawMoveInput;
    public bool HasMoveInput => hasMoveInput;
    public int FacingIndex => facingIndex;
    public bool IsMoving => isMoving;

    private float actionFacingOverrideUntil;

    private void Awake()
    {
        ResolveReferences();
        lastWorldPosition = transform.position;
    }

    private void OnEnable()
    {
        lastWorldPosition = transform.position;
    }

    private void OnValidate()
    {
        movingThreshold = Mathf.Max(0.001f, movingThreshold);
        movementInputDeadzone = Mathf.Max(0.001f, movementInputDeadzone);
        ResolveReferences();
    }

    private void LateUpdate()
    {
        UpdateFacing();
    }

    [ContextMenu("Resolve Facing References")]
    public void ResolveReferences()
    {
        body ??= GetComponent<Rigidbody2D>();
        playerController ??= GetComponent<PlayerController>();
        playerCombat ??= GetComponent<PlayerCombat>();
        animator ??= GetComponentInChildren<Animator>(true);
        spriteFlipTarget ??= GetComponentInChildren<SpriteRenderer>(true);
    }

    public void SetActionFacingOverride(Vector2 direction, float duration)
    {
        if (direction.sqrMagnitude <= 0.001f || duration <= 0f)
            return;

        actionFacingDirection = direction.normalized;
        storedIdleFacingDirection = actionFacingDirection;
        hasStoredActionFacing = true;
        actionFacingOverrideActive = true;
        actionFacingOverrideUntil = Mathf.Max(actionFacingOverrideUntil, Time.time + duration);
    }

    public void ClearActionFacingOverride()
    {
        actionFacingOverrideActive = false;
        actionFacingOverrideUntil = 0f;
    }

    public void UpdateFacing()
    {
        Vector2 direction = ResolveDirection();
        if (direction.sqrMagnitude > 0.001f)
            currentDirection = direction.normalized;

        currentVisualFacingDirection = currentDirection;
        isMoving = hasMoveInput || (body != null && body.linearVelocity.sqrMagnitude >= movingThreshold * movingThreshold);
        facingIndex = useEightWayFacing ? DirectionToEightWayIndex(currentDirection) : DirectionToFourWayIndex(currentDirection);

        if (applySpriteFlip && spriteFlipTarget != null && Mathf.Abs(currentDirection.x) > 0.05f)
            spriteFlipTarget.flipX = currentDirection.x < 0f;

        if (animator != null)
        {
            if (HasAnimatorParameter(animator, FacingXHash, AnimatorControllerParameterType.Float))
                animator.SetFloat(FacingXHash, currentDirection.x);

            if (HasAnimatorParameter(animator, FacingYHash, AnimatorControllerParameterType.Float))
                animator.SetFloat(FacingYHash, currentDirection.y);

            if (HasAnimatorParameter(animator, FacingIndexHash, AnimatorControllerParameterType.Int))
                animator.SetInteger(FacingIndexHash, facingIndex);

            if (HasAnimatorParameter(animator, IsMovingHash, AnimatorControllerParameterType.Bool))
                animator.SetBool(IsMovingHash, isMoving);
        }

        lastWorldPosition = transform.position;
    }

    private Vector2 ResolveDirection()
    {
        if (actionFacingOverrideActive && Time.time >= actionFacingOverrideUntil)
            ClearActionFacingOverride();

        if (playerCombat != null && playerCombat.CurrentAimDirection.sqrMagnitude > 0.001f)
            aimDirection = playerCombat.CurrentAimDirection.normalized;

        if (actionFacingOverrideActive)
        {
            chosenFacingSource = IsoFacingSource.AimAction;
            return actionFacingDirection;
        }

        if (playerController != null)
        {
            rawMoveInput = playerController.CurrentMoveInput;
            hasMoveInput = rawMoveInput.sqrMagnitude > movementInputDeadzone * movementInputDeadzone;
            if (hasMoveInput)
            {
                movementFacingDirection = rawMoveInput.normalized;
                storedIdleFacingDirection = movementFacingDirection;
                hasStoredActionFacing = false;
                chosenFacingSource = IsoFacingSource.Movement;
                return movementFacingDirection;
            }

            if (hasStoredActionFacing && storedIdleFacingDirection.sqrMagnitude > 0.001f)
            {
                chosenFacingSource = IsoFacingSource.LastAction;
                return storedIdleFacingDirection.normalized;
            }

            if (playerController.LastMovementFacing.sqrMagnitude > 0.001f)
            {
                movementFacingDirection = playerController.LastMovementFacing.normalized;
                storedIdleFacingDirection = movementFacingDirection;
                chosenFacingSource = IsoFacingSource.LastMovement;
                return movementFacingDirection;
            }
        }
        else
        {
            rawMoveInput = Vector2.zero;
            hasMoveInput = false;
        }

        if (body != null && body.linearVelocity.sqrMagnitude > movingThreshold * movingThreshold)
        {
            movementFacingDirection = body.linearVelocity.normalized;
            chosenFacingSource = IsoFacingSource.VelocityFallback;
            return movementFacingDirection;
        }

        if (playerController == null)
        {
            Vector2 worldDelta = (Vector2)(transform.position - lastWorldPosition);
            if (worldDelta.sqrMagnitude > movingThreshold * movingThreshold * Time.deltaTime * Time.deltaTime)
            {
                movementFacingDirection = worldDelta.normalized;
                chosenFacingSource = IsoFacingSource.VelocityFallback;
                return movementFacingDirection;
            }

            // Non-player users, such as enemies, keep the previous configurable behavior.
            if (facingPriority == DirectionalFacingPriority.PreferAimDirection)
            {
                if (preferAimDirection && playerCombat != null && playerCombat.CurrentAimDirection.sqrMagnitude > 0.001f)
                {
                    chosenFacingSource = IsoFacingSource.AimFallback;
                    return playerCombat.CurrentAimDirection;
                }
            }
            else
            {
                if (preferAimDirection && playerCombat != null && playerCombat.CurrentAimDirection.sqrMagnitude > 0.001f)
                {
                    chosenFacingSource = IsoFacingSource.AimFallback;
                    return playerCombat.CurrentAimDirection;
                }
            }

            chosenFacingSource = IsoFacingSource.LastMovement;
            return currentDirection;
        }

        chosenFacingSource = IsoFacingSource.LastMovement;
        return movementFacingDirection.sqrMagnitude > 0.001f ? movementFacingDirection : currentDirection;
    }

    private static int DirectionToFourWayIndex(Vector2 direction)
    {
        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
            return direction.x >= 0f ? 1 : 3;

        return direction.y >= 0f ? 0 : 2;
    }

    private static int DirectionToEightWayIndex(Vector2 direction)
    {
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        if (angle < 0f)
            angle += 360f;

        return Mathf.RoundToInt(angle / 45f) % 8;
    }

    private static bool HasAnimatorParameter(Animator targetAnimator, int hash, AnimatorControllerParameterType type)
    {
        if (targetAnimator == null)
            return false;

        AnimatorControllerParameter[] parameters = targetAnimator.parameters;
        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].nameHash == hash && parameters[i].type == type)
                return true;
        }

        return false;
    }
}

using UnityEngine;

[DisallowMultipleComponent]
public class IsoFacingController : MonoBehaviour
{
    [Header("Input")]
    public bool preferAimDirection = true;
    public bool useEightWayFacing = true;
    public float movingThreshold = 0.05f;

    [Header("References")]
    public Rigidbody2D body;
    public PlayerCombat playerCombat;
    public Animator animator;
    public SpriteRenderer spriteFlipTarget;

    [Header("Fallback")]
    public bool applySpriteFlip = false;

    [Header("Debug")]
    [SerializeField] private Vector2 currentDirection = Vector2.right;
    [SerializeField] private int facingIndex;
    [SerializeField] private bool isMoving;

    private static readonly int FacingXHash = Animator.StringToHash("FacingX");
    private static readonly int FacingYHash = Animator.StringToHash("FacingY");
    private static readonly int FacingIndexHash = Animator.StringToHash("FacingIndex");
    private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");

    public Vector2 CurrentDirection => currentDirection;
    public int FacingIndex => facingIndex;
    public bool IsMoving => isMoving;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnValidate()
    {
        movingThreshold = Mathf.Max(0.001f, movingThreshold);
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
        playerCombat ??= GetComponent<PlayerCombat>();
        animator ??= GetComponentInChildren<Animator>(true);
        spriteFlipTarget ??= GetComponentInChildren<SpriteRenderer>(true);
    }

    public void UpdateFacing()
    {
        Vector2 direction = ResolveDirection();
        if (direction.sqrMagnitude > 0.001f)
            currentDirection = direction.normalized;

        isMoving = body != null && body.linearVelocity.sqrMagnitude >= movingThreshold * movingThreshold;
        facingIndex = useEightWayFacing ? DirectionToEightWayIndex(currentDirection) : DirectionToFourWayIndex(currentDirection);

        if (applySpriteFlip && spriteFlipTarget != null && Mathf.Abs(currentDirection.x) > 0.05f)
            spriteFlipTarget.flipX = currentDirection.x < 0f;

        if (animator != null)
        {
            animator.SetFloat(FacingXHash, currentDirection.x);
            animator.SetFloat(FacingYHash, currentDirection.y);
            animator.SetInteger(FacingIndexHash, facingIndex);
            animator.SetBool(IsMovingHash, isMoving);
        }
    }

    private Vector2 ResolveDirection()
    {
        if (preferAimDirection && playerCombat != null && playerCombat.CurrentAimDirection.sqrMagnitude > 0.001f)
            return playerCombat.CurrentAimDirection;

        if (body != null && body.linearVelocity.sqrMagnitude > movingThreshold * movingThreshold)
            return body.linearVelocity;

        return currentDirection;
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
}

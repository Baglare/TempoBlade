using System.Collections;
using UnityEngine;

public class EnemyWardenLinker : EnemyBase
{
    [Header("Guardian Link")]
    public float guardianSearchRadius = 12f;
    public float guardianLinkCooldown = 6f;
    public float guardianLinkWindup = 0.45f;
    public float guardianLinkDuration = 4.5f;
    public float guardianDamageReductionMultiplier = 0.72f;
    public float guardianStaggerDurationMultiplier = 0.6f;
    public bool guardianIgnoreNextHeavyStagger = true;
    public Color guardianLinkColor = new Color(0.25f, 1f, 0.55f, 0.95f);

    [Header("Warden Call")]
    public GameObject summonedWardenPrefab;
    public float wardenCallCooldown = 12f;
    public float wardenCallWindup = 0.55f;
    public float summonedWardenDuration = 7.5f;
    public float summonedWardenAlpha = 0.72f;
    public float summonOffset = 1.2f;
    public Color summonCueColor = new Color(0.45f, 1f, 1f, 0.95f);

    [Header("Shield Step")]
    public float interceptOffset = 0.45f;
    public float shieldStepMoveSpeed = 4.1f;
    public float shieldStepRefreshInterval = 0.15f;
    public float shieldStepTolerance = 0.22f;
    public float retreatDistance = 2.5f;
    public float maxDistanceFromGuardTarget = 1.75f;

    [Header("Animation")]
    [SerializeField] private float deathAnimDuration = 0.6f;

    private Transform playerTransform;
    private Animator animator;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private SupportLinkVisual linkVisual;

    private EnemyBase linkedTarget;
    private EnemyBase positioningTarget;
    private float linkEndTime;
    private float nextLinkTime;
    private float nextWardenCallTime;
    private float nextShieldStepRefreshTime;
    private Vector2 currentInterceptPoint;
    private bool isDead;
    private bool isCasting;

    protected override void Start()
    {
        base.Start();
        deathDelay = deathAnimDuration;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            playerTransform = player.transform;

        animator = GetComponentInChildren<Animator>();
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        linkVisual = GetComponent<SupportLinkVisual>();
        if (linkVisual == null)
            linkVisual = gameObject.AddComponent<SupportLinkVisual>();
    }

    private void Update()
    {
        if (isDead || isStunned || playerTransform == null)
            return;

        if (linkedTarget != null && (!linkedTarget.gameObject.activeInHierarchy || linkedTarget.CurrentHealth <= 0f))
            ClearLink();

        if (linkedTarget != null && Time.time >= linkEndTime)
            ClearLink();

        if (positioningTarget != null && (!positioningTarget.gameObject.activeInHierarchy || positioningTarget.CurrentHealth <= 0f))
            positioningTarget = null;

        if (!isCasting && Time.time >= nextLinkTime)
        {
            StartCoroutine(GuardianLinkRoutine());
            return;
        }

        if (!isCasting && Time.time >= nextWardenCallTime && summonedWardenPrefab != null)
        {
            StartCoroutine(WardenCallRoutine());
            return;
        }

        UpdatePositioning();
    }

    public override void TakeDamage(float amount)
    {
        if (isDead)
            return;

        if (animator != null)
            animator.SetTrigger("TakeHit");

        base.TakeDamage(amount);
    }

    public override void Stun(float duration)
    {
        if (isDead)
            return;

        StopAllCoroutines();
        isCasting = false;

        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        if (spriteRenderer != null)
            spriteRenderer.color = Color.white;

        base.Stun(duration);
    }

    protected override void OnDeathAnimationStart()
    {
        isDead = true;
        StopAllCoroutines();
        ClearLink();

        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
            col.enabled = false;

        if (animator != null)
            animator.SetTrigger("Die");
    }

    private IEnumerator GuardianLinkRoutine()
    {
        isCasting = true;
        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        EnemyBase target = EnemySupportUtility.SelectGuardianLinkTarget(transform, guardianSearchRadius, this);
        if (target == null)
        {
            nextLinkTime = Time.time + guardianLinkCooldown;
            isCasting = false;
            yield break;
        }

        if (spriteRenderer != null)
            spriteRenderer.color = guardianLinkColor;

        yield return new WaitForSeconds(guardianLinkWindup);

        linkedTarget = target;
        linkEndTime = Time.time + guardianLinkDuration;
        linkedTarget.GetSupportBuffReceiver()?.ApplyGuardianLink(
            guardianLinkDuration,
            guardianDamageReductionMultiplier,
            guardianStaggerDurationMultiplier,
            guardianIgnoreNextHeavyStagger);
        linkVisual.SetTarget(linkedTarget.transform);

        SupportPulseVisualUtility.SpawnPulse(transform.position, 0.2f, 1.5f, 0.22f, guardianLinkColor);
        if (DamagePopupManager.Instance != null)
            DamagePopupManager.Instance.CreateText(linkedTarget.transform.position + Vector3.up * 1.6f, "LINKED", guardianLinkColor, 5.5f);

        if (spriteRenderer != null)
            spriteRenderer.color = Color.white;

        nextLinkTime = Time.time + guardianLinkCooldown;
        isCasting = false;
    }

    private IEnumerator WardenCallRoutine()
    {
        isCasting = true;
        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        if (spriteRenderer != null)
            spriteRenderer.color = summonCueColor;

        yield return new WaitForSeconds(wardenCallWindup);

        Vector3 summonPosition = transform.position;
        if (linkedTarget != null)
        {
            Vector2 awayFromTarget = ((Vector2)transform.position - (Vector2)linkedTarget.transform.position).normalized;
            if (awayFromTarget.sqrMagnitude <= 0.001f)
                awayFromTarget = Vector2.right;
            summonPosition = linkedTarget.transform.position + (Vector3)(awayFromTarget * summonOffset);
        }

        EnemySummonHelper.SummonTemporaryEnemy(summonedWardenPrefab, summonPosition, summonedWardenDuration, summonedWardenAlpha);
        SupportPulseVisualUtility.SpawnPulse(summonPosition, 0.2f, 1.1f, 0.25f, summonCueColor);
        if (DamagePopupManager.Instance != null)
            DamagePopupManager.Instance.CreateText(summonPosition + Vector3.up, "WARDEN!", summonCueColor, 6f);

        if (spriteRenderer != null)
            spriteRenderer.color = Color.white;

        nextWardenCallTime = Time.time + wardenCallCooldown;
        isCasting = false;
    }

    private void UpdatePositioning()
    {
        if (Time.time >= nextShieldStepRefreshTime)
        {
            currentInterceptPoint = ComputeInterceptPoint();
            nextShieldStepRefreshTime = Time.time + shieldStepRefreshInterval;
        }

        Vector2 toPoint = currentInterceptPoint - (Vector2)transform.position;
        bool isMoving = toPoint.magnitude > shieldStepTolerance;

        if (rb != null)
        {
            rb.linearVelocity = isMoving
                ? toPoint.normalized * shieldStepMoveSpeed * GetSupportMoveSpeedMultiplier()
                : Vector2.zero;
        }

        if (spriteRenderer != null)
            spriteRenderer.flipX = playerTransform.position.x < transform.position.x;

        if (animator != null)
            animator.SetBool("IsMoving", isMoving);
    }

    private Vector2 ComputeInterceptPoint()
    {
        if (linkedTarget != null && linkedTarget.CurrentHealth > 0f)
            positioningTarget = linkedTarget;
        else if (positioningTarget == null || positioningTarget.CurrentHealth <= 0f)
            positioningTarget = EnemySupportUtility.SelectGuardianLinkTarget(transform, guardianSearchRadius, this);

        EnemyBase target = linkedTarget != null && linkedTarget.CurrentHealth > 0f
            ? linkedTarget
            : positioningTarget;

        if (target != null && target.CurrentHealth > 0f)
        {
            Vector2 targetPos = target.transform.position;
            Vector2 toPlayer = (Vector2)playerTransform.position - targetPos;
            if (toPlayer.sqrMagnitude > 0.001f)
            {
                Vector2 interceptPoint = targetPos + toPlayer.normalized * interceptOffset;
                float distFromTarget = Vector2.Distance(interceptPoint, targetPos);
                if (distFromTarget > maxDistanceFromGuardTarget)
                    interceptPoint = targetPos + (interceptPoint - targetPos).normalized * maxDistanceFromGuardTarget;
                return interceptPoint;
            }

            return targetPos;
        }

        float distToPlayer = Vector2.Distance(transform.position, playerTransform.position);
        if (distToPlayer <= retreatDistance)
        {
            Vector2 away = ((Vector2)transform.position - (Vector2)playerTransform.position).normalized;
            return (Vector2)transform.position + away * interceptOffset;
        }

        return transform.position;
    }

    private void ClearLink()
    {
        linkedTarget = null;
        linkEndTime = 0f;
        if (linkVisual != null)
            linkVisual.SetTarget(null);
    }
}

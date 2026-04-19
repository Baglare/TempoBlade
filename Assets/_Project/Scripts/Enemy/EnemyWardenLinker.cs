using System.Collections;
using UnityEngine;

public class EnemyWardenLinker : EnemyBase
{
    [System.Serializable]
    public class LinkerTempoConfig
    {
        public TempoTierFloatValue guardianLinkWindupMultiplier = new TempoTierFloatValue { t0 = 1f, t1 = 0.88f, t2 = 0.88f, t3 = 0.8f };
        public TempoTierFloatValue summonCooldownMultiplier = new TempoTierFloatValue { t0 = 1f, t1 = 1f, t2 = 0.9f, t3 = 0.85f };
        public TempoTierFloatValue shieldStepMoveMultiplier = new TempoTierFloatValue { t0 = 1f, t1 = 1f, t2 = 1.12f, t3 = 1.18f };
        public float t2GuardianDamageMultiplier = 0.92f;
        public float t2GuardianStaggerMultiplier = 0.9f;
        public float t3GuardianDamageMultiplier = 0.82f;
        public float t3GuardianStaggerMultiplier = 0.82f;
        public float t3HeavyHitHealthFraction = 0.15f;
        public float t3EchoDamageFraction = 0.35f;
        public float t3HeavyStunThreshold = 0.65f;
        public float t3EchoStaggerDuration = 0.32f;
    }

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

    [Header("Tempo")]
    public LinkerTempoConfig tempoConfig = new LinkerTempoConfig();

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
    private bool suppressEchoCallbacks;
    private EnemyBase subscribedLinkedTarget;

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

        float windup = guardianLinkWindup * tempoConfig.guardianLinkWindupMultiplier.Evaluate(CurrentTempoTier);
        yield return new WaitForSeconds(Mathf.Max(0.05f, windup));

        UnsubscribeFromLinkedTarget();
        linkedTarget = target;
        SubscribeToLinkedTarget(linkedTarget);
        linkEndTime = Time.time + guardianLinkDuration;

        float damageMultiplier = guardianDamageReductionMultiplier;
        float staggerMultiplier = guardianStaggerDurationMultiplier;
        if (CurrentTempoTier >= TempoManager.TempoTier.T2)
        {
            damageMultiplier *= tempoConfig.t2GuardianDamageMultiplier;
            staggerMultiplier *= tempoConfig.t2GuardianStaggerMultiplier;
        }
        if (CurrentTempoTier == TempoManager.TempoTier.T3)
        {
            damageMultiplier *= tempoConfig.t3GuardianDamageMultiplier;
            staggerMultiplier *= tempoConfig.t3GuardianStaggerMultiplier;
        }

        linkedTarget.GetSupportBuffReceiver()?.ApplyGuardianLink(
            guardianLinkDuration,
            damageMultiplier,
            staggerMultiplier,
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

        float summonCooldown = wardenCallCooldown * tempoConfig.summonCooldownMultiplier.Evaluate(CurrentTempoTier);
        nextWardenCallTime = Time.time + Mathf.Max(1f, summonCooldown);
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
            float stepSpeed = shieldStepMoveSpeed * tempoConfig.shieldStepMoveMultiplier.Evaluate(CurrentTempoTier);
            rb.linearVelocity = isMoving
                ? toPoint.normalized * stepSpeed * GetSupportMoveSpeedMultiplier()
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
        UnsubscribeFromLinkedTarget();
        linkedTarget = null;
        linkEndTime = 0f;
        if (linkVisual != null)
            linkVisual.SetTarget(null);
    }

    private void SubscribeToLinkedTarget(EnemyBase target)
    {
        if (target == null)
            return;

        subscribedLinkedTarget = target;
        subscribedLinkedTarget.OnDamageTaken += HandleLinkedTargetDamageTaken;
        subscribedLinkedTarget.OnStunned += HandleLinkedTargetStunned;
    }

    private void UnsubscribeFromLinkedTarget()
    {
        if (subscribedLinkedTarget == null)
            return;

        subscribedLinkedTarget.OnDamageTaken -= HandleLinkedTargetDamageTaken;
        subscribedLinkedTarget.OnStunned -= HandleLinkedTargetStunned;
        subscribedLinkedTarget = null;
    }

    private void HandleLinkedTargetDamageTaken(float amount)
    {
        if (CurrentTempoTier != TempoManager.TempoTier.T3 || linkedTarget == null || suppressEchoCallbacks)
            return;

        float heavyThreshold = linkedTarget.MaxHealth * tempoConfig.t3HeavyHitHealthFraction;
        if (amount < heavyThreshold)
            return;

        ApplyEcho(amount * tempoConfig.t3EchoDamageFraction, true);
    }

    private void HandleLinkedTargetStunned(float duration)
    {
        if (CurrentTempoTier != TempoManager.TempoTier.T3 || suppressEchoCallbacks)
            return;

        if (duration < tempoConfig.t3HeavyStunThreshold)
            return;

        ApplyEcho(0f, true);
    }

    private void ApplyEcho(float damageAmount, bool applyStagger)
    {
        suppressEchoCallbacks = true;
        try
        {
            if (damageAmount > 0f)
                base.TakeDamage(damageAmount);

            if (applyStagger)
                base.Stun(tempoConfig.t3EchoStaggerDuration);
        }
        finally
        {
            suppressEchoCallbacks = false;
        }
    }
}

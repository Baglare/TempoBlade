using System.Collections;
using UnityEngine;

public class EnemyDuelist : EnemyBase, IParryReactive
{
    [System.Serializable]
    public class DuelistTempoConfig
    {
        public TempoTierFloatValue moveSpeedMultiplier = new TempoTierFloatValue { t0 = 1f, t1 = 1.06f, t2 = 1.08f, t3 = 1.12f };
        public TempoTierFloatValue attackCooldownMultiplier = new TempoTierFloatValue { t0 = 1f, t1 = 0.92f, t2 = 0.82f, t3 = 0.76f };
        public TempoTierFloatValue attackWindupMultiplier = new TempoTierFloatValue { t0 = 1f, t1 = 0.95f, t2 = 0.82f, t3 = 0.7f };
        public TempoTierFloatValue blockAngleBonus = new TempoTierFloatValue { t0 = 0f, t1 = 8f, t2 = 12f, t3 = 16f };
        public float t2ReadWindowDuration = 1f;
        public float t2ReadAttackWindow = 1.2f;
        public int t2ReadAttackCount = 3;
        public float t2ReadAttackCooldownMultiplier = 0.82f;
        public float t2ReadAttackWindupMultiplier = 0.75f;
        public float t3DuelStanceDuration = 2.6f;
        public float t3DuelStanceCooldown = 5.5f;
        public float t3DuelStanceMoveMultiplier = 1.12f;
        public float t3PerfectParryVulnerabilityMultiplier = 1.5f;
    }

    private static readonly int AnimIsMoving = Animator.StringToHash("IsMoving");
    private static readonly int AnimIsGuarding = Animator.StringToHash("IsGuarding");
    private static readonly int AnimAttack = Animator.StringToHash("Attack");
    private static readonly int AnimHurt = Animator.StringToHash("Hurt");
    private static readonly int AnimDie = Animator.StringToHash("Die");

    [Header("Duelist Settings")]
    public float moveSpeed = 2f;
    public float attackRange = 1.5f;
    public float attackCooldown = 3f;

    [Header("Block Settings")]
    [Tooltip("Blocking angle (degrees). 180 means full frontal block.")]
    public float blockAngle = 140f;
    [SerializeField] private bool isGuarding = false;

    [Header("Combat")]
    public float damage = 15f;
    public float attackWindup = 0.6f;
    public Transform attackPoint;
    public float attackRadius = 1f;

    [Header("Arc Visual")]
    [Tooltip("WeaponArcVisual component'i. Enemy altindaki child'a eklenir.")]
    public WeaponArcVisual weaponArcVisual;

    [Header("Tempo")]
    public DuelistTempoConfig tempoConfig = new DuelistTempoConfig();

    [Header("Animation")]
    [SerializeField] private float deathAnimDuration = 0.5f;

    [Header("Attack Alignment")]
    [SerializeField] private bool autoAlignAttackPoint = true;
    [SerializeField] private float attackPointFrontPadding = 0.08f;
    [SerializeField] private float attackPointHeightOffset = 0f;

    private Transform playerTransform;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private float nextAttackTime;
    private bool isAttacking;
    private bool isDead;
    private bool guardBroken;
    private Coroutine guardBreakRoutine;
    private float baseBlockAngle;
    private float readWindowEndTime;
    private float duelStanceEndTime;
    private float nextDuelStanceTime;

    public bool AllowParryExecute => true;

    protected override void Start()
    {
        base.Start();
        deathDelay = deathAnimDuration;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            playerTransform = player.transform;

        animator = GetComponentInChildren<Animator>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        isGuarding = true;
        guardBroken = false;

        if (weaponArcVisual != null)
            weaponArcVisual.range = attackRadius;

        ApplyTempoTuning(CurrentTempoTier);
    }

    protected override void OnTempoTierChanged(TempoManager.TempoTier tier)
    {
        base.OnTempoTierChanged(tier);
        ApplyTempoTuning(tier);
    }

    private void Update()
    {
        if (isDead)
            return;

        if (weaponArcVisual != null && playerTransform != null)
        {
            Vector2 dirToPlayer = ((Vector2)playerTransform.position - (Vector2)transform.position).normalized;
            weaponArcVisual.UpdateVisuals(transform.position, dirToPlayer, isAttacking, false);
        }

        bool isMoving = false;
        if (isStunned || playerTransform == null)
        {
            UpdateAnimatorState(false);
            return;
        }

        FacePlayer();
        UpdateVisuals();
        SyncAttackPointToSprite();
        UpdateReadAndStanceState();

        if (isAttacking)
            return;

        float dist = Vector2.Distance(transform.position, playerTransform.position);
        if (dist <= attackRange)
        {
            if (Time.time >= nextAttackTime && !guardBroken)
                StartCoroutine(AttackRoutine());
        }
        else
        {
            MoveTowardsPlayer();
            isMoving = true;
        }

        UpdateAnimatorState(isMoving);
    }

    private void FacePlayer()
    {
        if (isAttacking || playerTransform == null)
            return;

        if (playerTransform.position.x > transform.position.x)
            transform.localScale = new Vector3(1f, 1f, 1f);
        else
            transform.localScale = new Vector3(-1f, 1f, 1f);
    }

    private void MoveTowardsPlayer()
    {
        float moveMultiplier = tempoConfig.moveSpeedMultiplier.Evaluate(CurrentTempoTier);
        if (IsDuelStanceActive)
            moveMultiplier *= tempoConfig.t3DuelStanceMoveMultiplier;

        transform.position = Vector2.MoveTowards(
            transform.position,
            playerTransform.position,
            GetEffectiveMoveSpeed(moveSpeed * moveMultiplier) * Time.deltaTime);
    }

    private IEnumerator AttackRoutine()
    {
        isAttacking = true;
        isGuarding = false;
        UpdateAnimatorState(false);
        if (animator != null)
            animator.SetTrigger(AnimAttack);

        SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
        Color originalColor = sr != null ? sr.color : Color.white;
        if (sr != null)
            sr.color = Color.red;

        float attackSpeedMultiplier = Mathf.Max(0.01f, GetSupportAttackSpeedMultiplier());
        float currentWindup = attackWindup *
                              tempoConfig.attackWindupMultiplier.Evaluate(CurrentTempoTier) /
                              attackSpeedMultiplier;
        if (IsReadWindowActive)
            currentWindup *= tempoConfig.t2ReadAttackWindupMultiplier;
        if (IsDuelStanceActive)
            currentWindup *= 0.9f;

        yield return new WaitForSeconds(currentWindup);

        if (!isDead && !isStunned)
            ResolveAttackHit();

        if (sr != null)
            sr.color = originalColor;

        float recoveryTime = 0.8f;
        if (IsReadWindowActive)
            recoveryTime = 0.5f;
        if (IsDuelStanceActive)
            recoveryTime = 0.45f;

        yield return new WaitForSeconds(recoveryTime);

        isGuarding = !guardBroken;
        isAttacking = false;

        float currentCooldown = attackCooldown *
                                tempoConfig.attackCooldownMultiplier.Evaluate(CurrentTempoTier) /
                                attackSpeedMultiplier;
        if (IsReadWindowActive)
            currentCooldown *= tempoConfig.t2ReadAttackCooldownMultiplier;
        if (IsDuelStanceActive)
            currentCooldown *= 0.85f;

        nextAttackTime = Time.time + GetEffectiveCooldownDuration(currentCooldown);
        UpdateAnimatorState(false);
    }

    public override void TakeDamage(float amount)
    {
        if (isDead)
            return;

        if (isGuarding && !guardBroken && playerTransform != null)
        {
            Vector2 facing = transform.localScale.x >= 0f ? Vector2.right : Vector2.left;
            Vector2 toPlayer = ((Vector2)playerTransform.position - (Vector2)transform.position).normalized;
            if (Vector2.Angle(facing, toPlayer) <= blockAngle * 0.5f)
            {
                if (DamagePopupManager.Instance != null)
                    DamagePopupManager.Instance.CreateText(transform.position + Vector3.up * 1.5f, "BLOCK!", Color.cyan, 5f);

                if (CurrentTempoTier >= TempoManager.TempoTier.T1)
                    readWindowEndTime = Mathf.Max(readWindowEndTime, Time.time + 0.5f);

                return;
            }
        }

        if (animator != null)
            animator.SetTrigger(AnimHurt);

        base.TakeDamage(amount);
    }

    public override void Stun(float duration)
    {
        float finalDuration = duration;
        if (CurrentTempoTier == TempoManager.TempoTier.T3 && duration >= 0.45f)
            finalDuration *= tempoConfig.t3PerfectParryVulnerabilityMultiplier;

        base.Stun(finalDuration);
        isAttacking = false;
        isGuarding = false;
    }

    public void OnParryReaction(ParryReactionContext context)
    {
        if (isDead)
            return;

        StopAllCoroutines();
        isAttacking = false;
        isGuarding = false;

        if (animator != null)
            animator.SetTrigger(AnimHurt);

        float reactionDuration = Mathf.Max(0.05f, context.duration);
        if (CurrentTempoTier == TempoManager.TempoTier.T3)
            reactionDuration *= tempoConfig.t3PerfectParryVulnerabilityMultiplier;

        if (context.breakGuard)
        {
            if (guardBreakRoutine != null)
                StopCoroutine(guardBreakRoutine);

            guardBreakRoutine = StartCoroutine(GuardBreakRoutine(reactionDuration));
            return;
        }

        base.Stun(reactionDuration);
        StartCoroutine(RestoreGuardAfterDelay(reactionDuration));
    }

    private void ResolveAttackHit()
    {
        if (attackPoint == null)
            return;

        Collider2D[] hits = Physics2D.OverlapCircleAll(attackPoint.position, attackRadius);
        foreach (Collider2D hit in hits)
        {
            if (!hit.CompareTag("Player"))
                continue;

            ParrySystem parry = hit.GetComponent<ParrySystem>();
            Vector2 strikeOrigin = attackPoint != null ? (Vector2)attackPoint.position : (Vector2)transform.position;
            if (parry != null && parry.TryBlockMelee(strikeOrigin, gameObject))
                continue;

            IDamageable damageable = hit.GetComponent<IDamageable>();
            PlayerController controller = hit.GetComponent<PlayerController>();
            if (controller != null && controller.IsInvulnerable)
            {
                hit.GetComponent<DashPerkController>()?.NotifyMeleeDodged(this);
                continue;
            }

            damageable?.TakeDamage(GetEffectiveDamage(damage));
        }
    }

    private void UpdateReadAndStanceState()
    {
        if (CurrentTempoTier >= TempoManager.TempoTier.T2 &&
            EnemySupportUtility.IsPlayerRepeatingBasicAttacks(tempoConfig.t2ReadAttackWindow, tempoConfig.t2ReadAttackCount))
        {
            readWindowEndTime = Mathf.Max(readWindowEndTime, Time.time + tempoConfig.t2ReadWindowDuration);
        }

        if (CurrentTempoTier == TempoManager.TempoTier.T3 &&
            !IsDuelStanceActive &&
            Time.time >= nextDuelStanceTime &&
            playerTransform != null &&
            Vector2.Distance(transform.position, playerTransform.position) <= attackRange + 0.6f)
        {
            duelStanceEndTime = Time.time + tempoConfig.t3DuelStanceDuration;
            nextDuelStanceTime = Time.time + tempoConfig.t3DuelStanceCooldown;
        }
    }

    private void ApplyTempoTuning(TempoManager.TempoTier tier)
    {
        if (baseBlockAngle <= 0f)
            baseBlockAngle = blockAngle;

        blockAngle = baseBlockAngle + tempoConfig.blockAngleBonus.Evaluate(tier);
    }

    private bool IsReadWindowActive => Time.time < readWindowEndTime;
    private bool IsDuelStanceActive => Time.time < duelStanceEndTime;

    private void UpdateVisuals()
    {
        SpriteRenderer sr = spriteRenderer != null ? spriteRenderer : GetComponentInChildren<SpriteRenderer>();
        if (sr == null)
            return;

        if (animator != null)
        {
            if (!isAttacking)
                sr.color = Color.white;
            return;
        }

        if (isAttacking)
            return;

        if (isGuarding)
            sr.color = Color.blue;
    }

    private void SyncAttackPointToSprite()
    {
        if (!autoAlignAttackPoint || attackPoint == null || spriteRenderer == null || spriteRenderer.sprite == null)
            return;

        Bounds spriteBounds = spriteRenderer.sprite.bounds;
        Vector3 local = attackPoint.localPosition;
        local.x = spriteBounds.extents.x + attackPointFrontPadding;
        local.y = spriteBounds.center.y + attackPointHeightOffset;
        attackPoint.localPosition = local;
    }

    protected override void OnDeathAnimationStart()
    {
        isDead = true;
        StopAllCoroutines();

        if (animator != null)
            animator.SetTrigger(AnimDie);

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
            col.enabled = false;
    }

    private void UpdateAnimatorState(bool isMoving)
    {
        if (animator == null)
            return;

        animator.SetBool(AnimIsMoving, isMoving);
        animator.SetBool(AnimIsGuarding, isGuarding && !guardBroken && !isAttacking && !isDead && !isStunned);
    }

    private IEnumerator GuardBreakRoutine(float duration)
    {
        guardBroken = true;
        base.Stun(duration);
        yield return new WaitForSeconds(duration);

        guardBroken = false;
        if (!isDead)
            isGuarding = true;

        guardBreakRoutine = null;
    }

    private IEnumerator RestoreGuardAfterDelay(float duration)
    {
        yield return new WaitForSeconds(duration);
        if (!isDead && !guardBroken)
            isGuarding = true;
    }

    private void OnDrawGizmosSelected()
    {
        if (attackPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackPoint.position, attackRadius);
        }
    }
}

using UnityEngine;
using System.Collections;

public class EnemyCaster : EnemyBase, IParryReactive
{
    [System.Serializable]
    public class CasterTempoConfig
    {
        public TempoTierFloatValue castWindupMultiplier = new TempoTierFloatValue { t0 = 1f, t1 = 0.88f, t2 = 0.88f, t3 = 0.88f };
        public TempoTierFloatValue fireCooldownMultiplier = new TempoTierFloatValue { t0 = 1f, t1 = 0.95f, t2 = 0.95f, t3 = 0.95f };
        public float predictiveLeadTime = 0.25f;
        public float overchargeChance = 0.35f;
        public float overchargeProjectileScale = 1.35f;
        public float overchargeDamageMultiplier = 1.35f;
        public float overchargeInterruptStunMultiplier = 1.5f;
        public float overchargeInterruptLockDuration = 1.2f;
    }

    [Header("Caster Settings")]
    public GameObject projectilePrefab;
    public Transform firePoint;
    public float attackRange = 8f;
    public float retreatRange = 5f;
    public float firePermissionRange = 9.5f;
    public float fireRate = 2f;
    public float castWindup = 0.35f;
    public float tacticalSearchRadius = 3f;
    public int tacticalSampleCount = 10;

    [Header("Tempo")]
    public CasterTempoConfig tempoConfig = new CasterTempoConfig();

    [Header("Animation")]
    [SerializeField] private float deathAnimDuration = 0.8f;

    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Transform playerTransform;
    private Rigidbody2D playerRb;
    private EnemyCastCircleTelegraph castCircleTelegraph;
    private float nextFireTime;
    private bool isCasting;
    private bool isDead;
    private bool activeOverchargeCast;
    private bool activeEliteBurstCast;
    private Coroutine attackRoutine;

    public bool AllowParryExecute => true;

    protected override void Start()
    {
        base.Start();

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
            playerRb = player.GetComponent<Rigidbody2D>();
        }

        animator = GetComponentInChildren<Animator>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        castCircleTelegraph = GetComponent<EnemyCastCircleTelegraph>();
        if (castCircleTelegraph == null)
            castCircleTelegraph = gameObject.AddComponent<EnemyCastCircleTelegraph>();
        castCircleTelegraph.Configure(new Color(0.95f, 0.2f, 1f, 0.9f), 0.42f, 0.05f, 1.4f, 42);
        deathDelay = deathAnimDuration;
    }

    private void Update()
    {
        if (isDead || isStunned || playerTransform == null)
            return;

        Vector2 origin = firePoint != null ? (Vector2)firePoint.position : (Vector2)transform.position;
        float dist = Vector2.Distance(transform.position, playerTransform.position);
        FacePlayer();

        bool moving = false;
        if (!isCasting)
        {
            float maintainRange = Mathf.Max(retreatRange + 0.75f, (retreatRange + attackRange) * 0.5f);
            RangedTacticalDecision decision = RangedTacticalMovementUtility.EvaluatePosition(
                transform,
                playerTransform,
                origin,
                maintainRange,
                firePermissionRange,
                tacticalSearchRadius,
                tacticalSampleCount,
                transform);

            if (decision.canFireFromCurrent && Time.time >= nextFireTime)
            {
                StopMovement();
                attackRoutine = StartCoroutine(AttackRoutine());
            }
            else
            {
                Vector2 moveTarget = decision.foundBetterPosition ? decision.moveTarget : (Vector2)transform.position;
                if (dist < retreatRange && !decision.foundBetterPosition)
                {
                    Vector2 away = ((Vector2)transform.position - (Vector2)playerTransform.position).normalized;
                    moveTarget = (Vector2)transform.position + away * tacticalSearchRadius;
                }

                if ((moveTarget - (Vector2)transform.position).sqrMagnitude > 0.04f)
                {
                    MoveTowards(moveTarget);
                    moving = true;
                }
                else
                {
                    StopMovement();
                }
            }
        }

        if (animator != null)
            animator.SetBool("IsMoving", moving);
    }

    private void FacePlayer()
    {
        if (spriteRenderer == null || playerTransform == null)
            return;

        UpdateSpriteFacing(spriteRenderer, playerTransform.position.x);
    }

    private void MoveTowards(Vector2 target)
    {
        float speed = GetEffectiveMoveSpeedFromData(3f);
        transform.position = Vector2.MoveTowards(transform.position, target, speed * Time.deltaTime);
    }

    private void StopMovement()
    {
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.linearVelocity = Vector2.zero;
    }

    private IEnumerator AttackRoutine()
    {
        isCasting = true;
        EmitCombatAction(EnemyCombatActionType.Cast);
        activeOverchargeCast = CurrentTempoTier == TempoManager.TempoTier.T3 && Random.value < tempoConfig.overchargeChance;
        activeEliteBurstCast = ShouldUseEliteBurstOrb();

        if (spriteRenderer != null)
        {
            if (activeEliteBurstCast && ActiveEliteProfile != null)
                spriteRenderer.color = ActiveEliteProfile.casterBurstOrb.burstCueColor;
            else
                spriteRenderer.color = activeOverchargeCast ? new Color(1f, 0.45f, 0.15f, 1f) : Color.magenta;
        }

        float attackSpeedMultiplier = Mathf.Max(0.01f, GetSupportAttackSpeedMultiplier());
        float windup = GetEffectiveCooldownDuration(castWindup * tempoConfig.castWindupMultiplier.Evaluate(CurrentTempoTier)) / attackSpeedMultiplier;
        float timer = 0f;
        while (timer < windup)
        {
            timer += Time.deltaTime;
            if (castCircleTelegraph != null)
                castCircleTelegraph.SetProgress(Mathf.Clamp01(timer / Mathf.Max(0.01f, windup)));
            yield return null;
        }

        if (castCircleTelegraph != null)
            castCircleTelegraph.FlashComplete();

        if (spriteRenderer != null)
            spriteRenderer.color = Color.white;

        if (animator != null)
            animator.SetTrigger("Attack");

        if (projectilePrefab != null && firePoint != null)
        {
            Vector2 aimDirection = GetAimDirection();
            GameObject projObj = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
            Projectile proj = projObj.GetComponent<Projectile>();
            if (proj != null)
            {
                proj.owner = gameObject;
                proj.damage = GetEffectiveDamageFromData(proj.damage);
                if (activeOverchargeCast)
                {
                    proj.damage *= tempoConfig.overchargeDamageMultiplier;
                    projObj.transform.localScale *= tempoConfig.overchargeProjectileScale;
                }

                TryApplyEliteBurstOrb(projObj, activeEliteBurstCast);

                proj.Launch(aimDirection);
            }
        }

        float cooldown = GetEffectiveCooldownDuration(fireRate * tempoConfig.fireCooldownMultiplier.Evaluate(CurrentTempoTier)) / attackSpeedMultiplier;
        nextFireTime = Time.time + cooldown;
        activeOverchargeCast = false;
        activeEliteBurstCast = false;
        isCasting = false;
        if (castCircleTelegraph != null)
            castCircleTelegraph.Hide();
        attackRoutine = null;
    }

    private Vector2 GetAimDirection()
    {
        if (playerTransform == null || firePoint == null)
            return Vector2.right;

        Vector2 targetPosition = playerTransform.position;
        if (CurrentTempoTier >= TempoManager.TempoTier.T2 && playerRb != null)
            targetPosition += playerRb.linearVelocity * tempoConfig.predictiveLeadTime;

        Vector2 direction = (targetPosition - (Vector2)firePoint.position).normalized;
        return direction.sqrMagnitude > 0.001f ? direction : Vector2.right;
    }

    public override void TakeDamage(float amount)
    {
        if (isDead)
            return;

        base.TakeDamage(amount);
        if (!isDead && animator != null)
            animator.SetTrigger("TakeHit");
    }

    public override void Stun(float duration)
    {
        if (isDead)
            return;

        InterruptCast(duration, false);
    }

    protected override void OnDeathAnimationStart()
    {
        isDead = true;
        isCasting = false;
        activeOverchargeCast = false;
        activeEliteBurstCast = false;
        StopAllCoroutines();

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
            col.enabled = false;

        if (animator != null)
            animator.SetTrigger("Die");
    }

    public void OnParryReaction(ParryReactionContext context)
    {
        if (isDead)
            return;

        InterruptCast(Mathf.Max(0.05f, context.duration), true);
    }

    private void InterruptCast(float duration, bool triggerHurt)
    {
        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
            attackRoutine = null;
        }

        isCasting = false;

        if (triggerHurt && animator != null)
            animator.SetTrigger("TakeHit");

        if (spriteRenderer != null)
            spriteRenderer.color = Color.white;
        if (castCircleTelegraph != null)
            castCircleTelegraph.Hide();

        float finalDuration = duration;
        if (activeOverchargeCast)
        {
            finalDuration *= tempoConfig.overchargeInterruptStunMultiplier;
            nextFireTime = Mathf.Max(nextFireTime, Time.time + tempoConfig.overchargeInterruptLockDuration);
        }

        activeOverchargeCast = false;
        activeEliteBurstCast = false;

        base.Stun(finalDuration);
        nextFireTime = Mathf.Max(nextFireTime, Time.time + finalDuration);
    }

    private bool ShouldUseEliteBurstOrb()
    {
        if (ActiveEliteProfile == null)
            return false;

        CasterBurstOrbSettings burstSettings = ActiveEliteProfile.casterBurstOrb;
        bool mechanicEnabled =
            HasEliteMechanic(EliteMechanicType.CasterBurstOrb) ||
            (burstSettings != null && burstSettings.burstOrbChance > 0f);

        if (!mechanicEnabled)
            return false;

        return burstSettings != null && Random.value <= burstSettings.burstOrbChance;
    }

    private void TryApplyEliteBurstOrb(GameObject projectileObject, bool useBurstOrb)
    {
        if (projectileObject == null || !useBurstOrb || ActiveEliteProfile == null)
            return;

        CasterBurstOrbSettings burstSettings = ActiveEliteProfile.casterBurstOrb;
        if (burstSettings == null)
            return;

        ProjectileBurstOnImpact burst = projectileObject.GetComponent<ProjectileBurstOnImpact>();
        if (burst == null)
            burst = projectileObject.AddComponent<ProjectileBurstOnImpact>();

        burst.ConfigurePrimary(burstSettings, ActiveEliteProfile.eliteAudioEvent, ActiveEliteProfile.eliteCueColor);
        PlayEliteCue(projectileObject.transform.position, true, false);
    }

}

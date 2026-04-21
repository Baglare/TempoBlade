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
    public float fireRate = 2f;
    public float castWindup = 0.35f;

    [Header("Tempo")]
    public CasterTempoConfig tempoConfig = new CasterTempoConfig();

    [Header("Animation")]
    [SerializeField] private float deathAnimDuration = 0.8f;

    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Transform playerTransform;
    private Rigidbody2D playerRb;
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
        deathDelay = deathAnimDuration;
    }

    private void Update()
    {
        if (isDead || isStunned || playerTransform == null)
            return;

        float dist = Vector2.Distance(transform.position, playerTransform.position);
        FacePlayer();

        bool moving = false;
        if (!isCasting)
        {
            if (dist > attackRange)
            {
                MoveTowards(playerTransform.position);
                moving = true;
            }
            else if (dist < retreatRange)
            {
                Vector2 dir = ((Vector2)transform.position - (Vector2)playerTransform.position).normalized;
                MoveTowards((Vector2)transform.position + dir);
                moving = true;
            }
            else
            {
                StopMovement();
                if (Time.time >= nextFireTime)
                    attackRoutine = StartCoroutine(AttackRoutine());
            }
        }

        if (animator != null)
            animator.SetBool("IsMoving", moving);
    }

    private void FacePlayer()
    {
        if (spriteRenderer == null || playerTransform == null)
            return;

        spriteRenderer.flipX = playerTransform.position.x < transform.position.x;
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
        yield return new WaitForSeconds(windup);

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

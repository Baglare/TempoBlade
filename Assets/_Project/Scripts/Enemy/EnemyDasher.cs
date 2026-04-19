using UnityEngine;
using System.Collections;

public class EnemyDasher : EnemyBase
{
    [System.Serializable]
    public class DasherTempoConfig
    {
        public TempoTierFloatValue reactiveDodgeChanceMultiplier = new TempoTierFloatValue { t0 = 1f, t1 = 0.9f, t2 = 0.7f, t3 = 0.5f };
        public TempoTierFloatValue proactiveDashCooldownMultiplier = new TempoTierFloatValue { t0 = 1f, t1 = 0.85f, t2 = 0.72f, t3 = 0.6f };
        public TempoTierFloatValue aggressionRangeMultiplier = new TempoTierFloatValue { t0 = 1f, t1 = 1f, t2 = 0.88f, t3 = 0.82f };
        public float t2FeintChance = 0.4f;
        public float t3DoubleDashChance = 0.35f;
        public float t3PunishWindowDuration = 0.9f;
        public float proactiveDashBaseCooldown = 2.6f;
        public float feintTowardBias = 0.35f;
    }

    [Header("Dasher Settings")]
    public GameObject projectilePrefab;
    public Transform firePoint;
    public float preferredRange = 7f;
    public float minRange = 4f;
    public float fireRate = 1.2f;

    [Header("Perfect Dash (i-frame kaÃ§Ä±nma)")]
    [Range(0f, 1f)] public float dodgeChance = 0.45f;
    public float dodgeSpeed = 18f;
    public float dodgeDuration = 0.28f;

    [Header("Tempo")]
    public DasherTempoConfig tempoConfig = new DasherTempoConfig();

    [Header("Animation")]
    [SerializeField] private float deathAnimDuration = 0.8f;

    private enum State { Kiting, DashEvading, PunishWindow }
    private State state = State.Kiting;

    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Transform playerTransform;
    private Rigidbody2D rb;

    private float nextFireTime;
    private float nextProactiveDashTime;
    private float punishWindowEndTime;
    private bool isDead;
    private bool isEvading;

    protected override void Start()
    {
        base.Start();
        deathDelay = deathAnimDuration;

        rb = GetComponent<Rigidbody2D>();

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            playerTransform = player.transform;

        animator = GetComponentInChildren<Animator>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    private void Update()
    {
        if (isDead || isStunned || playerTransform == null)
            return;
        if (state == State.DashEvading)
            return;

        if (state == State.PunishWindow && Time.time >= punishWindowEndTime)
            state = State.Kiting;

        float dist = Vector2.Distance(transform.position, playerTransform.position);
        float speed = (enemyData != null ? enemyData.moveSpeed : 7f) * GetSupportMoveSpeedMultiplier();
        if (state == State.PunishWindow)
            speed *= 0.55f;

        FacePlayer();
        float aggressiveMinRange = minRange * tempoConfig.aggressionRangeMultiplier.Evaluate(CurrentTempoTier);
        Vector2 awayDir = ((Vector2)transform.position - (Vector2)playerTransform.position).normalized;

        if (CanStartProactiveDash(dist))
        {
            StartCoroutine(CombatDashRoutine(dist));
            return;
        }

        if (dist < aggressiveMinRange)
        {
            MoveBy(awayDir * speed);
        }
        else if (dist > preferredRange * 1.3f)
        {
            MoveBy(-awayDir * speed * 0.5f);
        }
        else
        {
            Vector2 strafe = new Vector2(-awayDir.y, awayDir.x);
            MoveBy(strafe * speed * 0.75f);
        }

        if (animator != null)
            animator.SetBool("IsMoving", true);

        if (Time.time >= nextFireTime)
            StartCoroutine(FireRoutine());
    }

    private IEnumerator FireRoutine()
    {
        nextFireTime = Time.time + fireRate / Mathf.Max(0.01f, GetSupportAttackSpeedMultiplier());

        if (projectilePrefab == null || firePoint == null || playerTransform == null)
            yield break;

        if (animator != null)
            animator.SetTrigger("Attack");

        Vector2 aimDir = ((Vector2)playerTransform.position - (Vector2)firePoint.position).normalized;
        GameObject projObj = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
        Projectile proj = projObj.GetComponent<Projectile>();
        if (proj != null)
        {
            proj.owner = gameObject;
            proj.damage = enemyData != null ? enemyData.damage : proj.damage;
            proj.Launch(aimDir);
        }
    }

    private IEnumerator CombatDashRoutine(float distanceToPlayer)
    {
        isEvading = true;
        state = State.DashEvading;
        float cooldown = tempoConfig.proactiveDashBaseCooldown * tempoConfig.proactiveDashCooldownMultiplier.Evaluate(CurrentTempoTier);
        nextProactiveDashTime = Time.time + cooldown;

        Vector2 toPlayer = ((Vector2)playerTransform.position - (Vector2)transform.position).normalized;
        Vector2 lateral = new Vector2(-toPlayer.y, toPlayer.x) * (Random.value < 0.5f ? -1f : 1f);
        Vector2 dashDir = lateral;

        if (CurrentTempoTier >= TempoManager.TempoTier.T2)
        {
            bool feint = Random.value < tempoConfig.t2FeintChance;
            dashDir = (lateral + (feint ? toPlayer * tempoConfig.feintTowardBias : toPlayer * 0.18f)).normalized;
        }

        if (spriteRenderer != null)
            spriteRenderer.color = new Color(0.4f, 0.8f, 1f);

        yield return DashInDirection(dashDir);

        bool shouldPunish = distanceToPlayer > preferredRange * 1.15f;
        if (CurrentTempoTier == TempoManager.TempoTier.T3 && Random.value < tempoConfig.t3DoubleDashChance)
        {
            Vector2 secondDir = (new Vector2(-dashDir.y, dashDir.x) + toPlayer * 0.22f).normalized;
            yield return DashInDirection(secondDir);
            shouldPunish = true;
        }

        if (rb != null)
            rb.linearVelocity = Vector2.zero;
        if (spriteRenderer != null)
            spriteRenderer.color = Color.white;

        isEvading = false;
        if (CurrentTempoTier == TempoManager.TempoTier.T3 && shouldPunish)
            EnterPunishWindow();
        else
            state = State.Kiting;
    }

    private IEnumerator DashEvade()
    {
        isEvading = true;
        state = State.DashEvading;

        Vector2 dodgeDir = playerTransform != null
            ? ((Vector2)transform.position - (Vector2)playerTransform.position).normalized
            : Random.insideUnitCircle.normalized;

        if (spriteRenderer != null)
            spriteRenderer.color = new Color(0.4f, 0.8f, 1f);

        yield return DashInDirection(dodgeDir);

        if (rb != null)
            rb.linearVelocity = Vector2.zero;
        if (spriteRenderer != null)
            spriteRenderer.color = Color.white;

        isEvading = false;
        state = State.Kiting;
    }

    public override void TakeDamage(float amount)
    {
        if (isDead || isEvading)
            return;

        float reactiveChance = dodgeChance * tempoConfig.reactiveDodgeChanceMultiplier.Evaluate(CurrentTempoTier);
        if (state != State.DashEvading && Random.value < reactiveChance)
        {
            StartCoroutine(DashEvade());
            if (DamagePopupManager.Instance != null)
            {
                DamagePopupManager.Instance.CreateText(
                    transform.position + Vector3.up * 1.2f,
                    "EVADE!",
                    new Color(0.4f, 0.9f, 1f),
                    5f);
            }
            return;
        }

        base.TakeDamage(amount);
    }

    public override void Stun(float duration)
    {
        if (isEvading)
        {
            StopAllCoroutines();
            isEvading = false;
            state = State.Kiting;
            if (rb != null)
                rb.linearVelocity = Vector2.zero;
            if (spriteRenderer != null)
                spriteRenderer.color = Color.white;
        }

        base.Stun(duration);
    }

    protected override void OnDeathAnimationStart()
    {
        isDead = true;
        isEvading = false;
        StopAllCoroutines();

        if (animator != null)
            animator.SetTrigger("Die");
        if (spriteRenderer != null)
            spriteRenderer.color = Color.white;

        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
            col.enabled = false;
    }

    private bool CanStartProactiveDash(float distanceToPlayer)
    {
        if (CurrentTempoTier == TempoManager.TempoTier.T0)
            return false;
        if (state == State.PunishWindow)
            return false;
        if (Time.time < nextProactiveDashTime)
            return false;

        return distanceToPlayer <= preferredRange * 1.15f && distanceToPlayer >= minRange * 0.75f;
    }

    private IEnumerator DashInDirection(Vector2 direction)
    {
        float timer = 0f;
        while (timer < dodgeDuration)
        {
            if (rb != null)
                rb.linearVelocity = direction * dodgeSpeed;
            timer += Time.deltaTime;
            yield return null;
        }
    }

    private void EnterPunishWindow()
    {
        punishWindowEndTime = Time.time + tempoConfig.t3PunishWindowDuration;
        state = State.PunishWindow;
    }

    private void MoveBy(Vector2 velocity)
    {
        if (rb != null)
            rb.linearVelocity = velocity;
        else
            transform.position += (Vector3)(velocity * Time.deltaTime);
    }

    private void FacePlayer()
    {
        if (spriteRenderer == null || playerTransform == null)
            return;
        spriteRenderer.flipX = playerTransform.position.x < transform.position.x;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, preferredRange);
        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(transform.position, minRange);
    }
}

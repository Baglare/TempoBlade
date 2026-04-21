using UnityEngine;
using System.Collections;

public class EnemyAssassin : EnemyBase
{
    [System.Serializable]
    public class AssassinTempoConfig
    {
        public TempoTierFloatValue preAttackVisibleMultiplier = new TempoTierFloatValue { t0 = 1f, t1 = 0.9f, t2 = 0.78f, t3 = 0.78f };
        public TempoTierFloatValue attackCooldownMultiplier = new TempoTierFloatValue { t0 = 1f, t1 = 1f, t2 = 0.9f, t3 = 0.9f };
        public TempoTierFloatValue orbitDistance = new TempoTierFloatValue { t0 = 0f, t1 = 0.8f, t2 = 1.25f, t3 = 1.45f };
        public TempoTierFloatValue orbitRefreshInterval = new TempoTierFloatValue { t0 = 99f, t1 = 0.7f, t2 = 0.4f, t3 = 0.3f };
        public float t3DoubleRepositionChance = 0.35f;
        public float t3PunishWindowDuration = 1.1f;
        public float t3PunishSpeedMultiplier = 0.55f;
        public float shortRepositionDistance = 1f;
        public float shortRepositionDuration = 0.08f;
    }

    [Header("Assassin Settings")]
    public float detectionRange = 8f;
    public float attackRange = 1.4f;
    public float attackDamage = 20f;
    public float attackCooldown = 2.5f;
    public float retreatDuration = 1.6f;

    [Header("Visibility")]
    public float invisibleAlpha = 0f;
    public float semiVisibleAlpha = 0.3f;
    public float preAttackVisibleTime = 0.15f;

    [Header("Tempo")]
    public AssassinTempoConfig tempoConfig = new AssassinTempoConfig();

    [Header("Animation")]
    [SerializeField] private float deathAnimDuration = 0.5f;

    private enum State { RoamingInvisible, TrackingInvisible, PreAttack, Attacking, Retreating }
    private State state = State.RoamingInvisible;

    private Transform playerTransform;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private Vector2 wanderTarget;
    private float wanderTimer;
    private float nextAttackTime;
    private bool isDead;
    private float punishWindowEndTime;
    private int orbitSide = 1;
    private float nextOrbitRefreshTime;

    protected override void Start()
    {
        base.Start();
        deathDelay = deathAnimDuration;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            playerTransform = player.transform;

        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        animator = GetComponentInChildren<Animator>();

        SetAlpha(invisibleAlpha);
        SetNewWanderTarget();
    }

    private void Update()
    {
        if (isDead || isStunned || playerTransform == null)
            return;

        float dist = Vector2.Distance(transform.position, playerTransform.position);
        float speed = GetEffectiveMoveSpeedFromData(3f);
        if (IsPunishWindowActive())
            speed *= tempoConfig.t3PunishSpeedMultiplier;

        RefreshVisibility();

        if (animator != null)
        {
            bool moving = state == State.RoamingInvisible || state == State.TrackingInvisible || state == State.Retreating;
            animator.SetBool("IsMoving", moving);
        }

        switch (state)
        {
            case State.RoamingInvisible:
                DoWander(speed);
                if (dist <= detectionRange)
                    state = State.TrackingInvisible;
                break;

            case State.TrackingInvisible:
                FaceTarget(playerTransform.position);
                if (dist > detectionRange * 1.2f)
                {
                    state = State.RoamingInvisible;
                    break;
                }

                if (dist <= attackRange * 2.5f && Time.time >= nextAttackTime)
                {
                    state = State.PreAttack;
                    StartCoroutine(AttackSequence());
                }
                else
                {
                    Vector2 trackingTarget = GetTrackingTarget();
                    transform.position = Vector2.MoveTowards(transform.position, trackingTarget, speed * Time.deltaTime);
                }
                break;

            case State.Retreating:
                Vector2 away = ((Vector2)transform.position - (Vector2)playerTransform.position).normalized;
                transform.position = Vector2.MoveTowards(
                    transform.position,
                    (Vector2)transform.position + away,
                    speed * 1.2f * Time.deltaTime);
                break;
        }
    }

    private IEnumerator AttackSequence()
    {
        float attackSpeedMultiplier = Mathf.Max(0.01f, GetSupportAttackSpeedMultiplier());
        float visibleTime = preAttackVisibleTime * tempoConfig.preAttackVisibleMultiplier.Evaluate(CurrentTempoTier);

        SetAlpha(1f);
        yield return new WaitForSeconds(visibleTime / attackSpeedMultiplier);

        if (CurrentTempoTier == TempoManager.TempoTier.T3 && Random.value < tempoConfig.t3DoubleRepositionChance)
        {
            yield return ShortReposition(orbitSide);
            yield return ShortReposition(-orbitSide);
        }

        state = State.Attacking;
        if (playerTransform != null)
        {
            float lungeTime = 0.12f;
            float elapsed = 0f;
            while (elapsed < lungeTime)
            {
                transform.position = Vector2.MoveTowards(transform.position, playerTransform.position, 22f * Time.deltaTime);
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        bool hitPlayer = false;
        bool parried = false;
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, attackRange);
        foreach (var hit in hits)
        {
            if (!hit.CompareTag("Player"))
                continue;

            ParrySystem parry = hit.GetComponent<ParrySystem>();
            if (parry != null && parry.TryBlockMelee(transform.position, gameObject))
            {
                parried = true;
                continue;
            }

            PlayerController playerController = hit.GetComponent<PlayerController>();
            if (playerController != null && playerController.IsInvulnerable)
            {
                hit.GetComponent<DashPerkController>()?.NotifyMeleeDodged(this);
                continue;
            }

            hit.GetComponent<IDamageable>()?.TakeDamage(GetEffectiveDamage(attackDamage));
            hitPlayer = true;
        }

        SetAlpha(invisibleAlpha);
        state = State.Retreating;
        nextAttackTime = Time.time + GetEffectiveCooldownDuration(attackCooldown * tempoConfig.attackCooldownMultiplier.Evaluate(CurrentTempoTier)) / attackSpeedMultiplier;

        if (CurrentTempoTier == TempoManager.TempoTier.T3 && (parried || !hitPlayer))
            EnterPunishWindow();

        yield return new WaitForSeconds(retreatDuration);
        if (!isDead)
            state = State.TrackingInvisible;
    }

    private IEnumerator ShortReposition(int sideSign)
    {
        if (playerTransform == null)
            yield break;

        Vector2 toPlayer = ((Vector2)playerTransform.position - (Vector2)transform.position).normalized;
        Vector2 lateral = new Vector2(-toPlayer.y, toPlayer.x) * Mathf.Sign(sideSign);
        Vector2 start = transform.position;
        Vector2 end = start + lateral * tempoConfig.shortRepositionDistance;
        float elapsed = 0f;
        while (elapsed < tempoConfig.shortRepositionDuration)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector2.Lerp(start, end, elapsed / tempoConfig.shortRepositionDuration);
            yield return null;
        }
    }

    private void RefreshVisibility()
    {
        if (state == State.PreAttack || state == State.Attacking)
            return;

        if (IsPunishWindowActive())
        {
            SetAlpha(1f);
            return;
        }

        float targetAlpha = ShouldBeRevealed() ? semiVisibleAlpha : invisibleAlpha;
        SetAlpha(targetAlpha);
    }

    private bool ShouldBeRevealed()
    {
        if (RoomManager.Instance == null)
            return false;

        var enemies = RoomManager.Instance.activeEnemies;
        if (enemies.Count == 0)
            return false;
        if (enemies.Count <= 3)
            return true;

        foreach (var enemy in enemies)
        {
            if (enemy != null && enemy.GetComponent<EnemyAssassin>() == null)
                return false;
        }

        return true;
    }

    private void SetAlpha(float alpha)
    {
        if (spriteRenderer == null)
            return;

        Color c = spriteRenderer.color;
        c.a = alpha;
        spriteRenderer.color = c;
    }

    private void DoWander(float speed)
    {
        wanderTimer -= Time.deltaTime;
        if (wanderTimer <= 0f || Vector2.Distance(transform.position, wanderTarget) < 0.5f)
            SetNewWanderTarget();

        transform.position = Vector2.MoveTowards(transform.position, wanderTarget, speed * Time.deltaTime);
    }

    private void SetNewWanderTarget()
    {
        wanderTarget = (Vector2)transform.position + Random.insideUnitCircle.normalized * 4f;
        wanderTimer = Random.Range(2f, 4f);
    }

    private void FaceTarget(Vector2 target)
    {
        if (spriteRenderer == null)
            return;

        spriteRenderer.flipX = target.x < transform.position.x;
    }

    private Vector2 GetTrackingTarget()
    {
        if (CurrentTempoTier == TempoManager.TempoTier.T0 || playerTransform == null)
            return playerTransform.position;

        if (Time.time >= nextOrbitRefreshTime)
        {
            nextOrbitRefreshTime = Time.time + tempoConfig.orbitRefreshInterval.Evaluate(CurrentTempoTier);
            orbitSide = Random.value < 0.5f ? -1 : 1;
        }

        Vector2 toPlayer = ((Vector2)playerTransform.position - (Vector2)transform.position).normalized;
        Vector2 lateral = new Vector2(-toPlayer.y, toPlayer.x) * orbitSide;
        return (Vector2)playerTransform.position + lateral * tempoConfig.orbitDistance.Evaluate(CurrentTempoTier);
    }

    private void EnterPunishWindow()
    {
        punishWindowEndTime = Time.time + tempoConfig.t3PunishWindowDuration;
    }

    private bool IsPunishWindowActive()
    {
        return Time.time < punishWindowEndTime;
    }

    public override void TakeDamage(float amount)
    {
        if (isDead)
            return;

        if (spriteRenderer != null)
        {
            Color c = spriteRenderer.color;
            c.a = Mathf.Max(c.a, 0.6f);
            spriteRenderer.color = c;
        }

        base.TakeDamage(amount);
    }

    public override void Stun(float duration)
    {
        base.Stun(duration);
        if (state == State.PreAttack || state == State.Attacking)
        {
            StopAllCoroutines();
            state = State.TrackingInvisible;
            if (CurrentTempoTier == TempoManager.TempoTier.T3)
                EnterPunishWindow();
        }
    }

    protected override void OnDeathAnimationStart()
    {
        isDead = true;
        StopAllCoroutines();
        SetAlpha(1f);

        if (animator != null)
            animator.SetTrigger("Die");

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
            col.enabled = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.5f, 0f, 0.5f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}

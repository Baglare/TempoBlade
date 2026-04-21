using System.Collections;
using UnityEngine;

public class EnemyDeadeye : EnemyBase
{
    [System.Serializable]
    public class DeadeyeTempoConfig
    {
        public TempoTierFloatValue lockAimDurationMultiplier = new TempoTierFloatValue { t0 = 1f, t1 = 0.88f, t2 = 0.88f, t3 = 0.72f };
        public TempoTierFloatValue movementRefreshMultiplier = new TempoTierFloatValue { t0 = 1f, t1 = 0.82f, t2 = 0.72f, t3 = 0.68f };
        public TempoTierFloatValue repositionSampleMultiplier = new TempoTierFloatValue { t0 = 1f, t1 = 1f, t2 = 1.5f, t3 = 1.8f };
        public float t3ShotDamageMultiplier = 1.25f;
        public float t3ProjectileSpeedMultiplier = 1.2f;
        public float t3RecoveryDuration = 0.55f;
        public Color t3TelegraphColor = new Color(1f, 0.3f, 0.12f, 1f);
        public float t3TelegraphWidthMultiplier = 1.4f;
    }

    [Header("Shot")]
    public GameObject projectilePrefab;
    public Transform firePoint;
    public float preferredRange = 10f;
    public float minimumRange = 5.5f;
    public float maximumRange = 14f;
    public float firePermissionRange = 12.5f;
    public float shotCooldown = 3.1f;
    public float lockAimDuration = 0.95f;
    public Color lockAimColor = new Color(1f, 0.18f, 0.18f, 0.95f);
    public float telegraphStartWidth = 0.04f;
    public float telegraphEndWidth = 0.12f;

    [Header("Reposition Dash")]
    public float repositionTriggerRange = 3.2f;
    public float repositionDuration = 0.24f;
    public float repositionSpeed = 11f;
    public float repositionCooldown = 3.75f;
    public GameObject controlShotPrefab;
    public float controlShotLifeTimeOverride = 0.9f;

    [Header("Movement")]
    public float movementRefreshInterval = 0.3f;
    public float movementTolerance = 0.2f;
    public int repositionSampleCount = 10;
    public float tacticalSearchRadius = 4f;
    public LayerMask lineOfSightMask = Physics2D.DefaultRaycastLayers;

    [Header("Tempo")]
    public DeadeyeTempoConfig tempoConfig = new DeadeyeTempoConfig();

    [Header("Animation")]
    [SerializeField] private float deathAnimDuration = 0.7f;

    private Transform playerTransform;
    private Animator animator;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private EnemyAimTelegraph aimTelegraph;
    private DeadeyeEchoLine activeEchoLine;

    private float nextShotTime;
    private float nextRepositionTime;
    private float nextMoveRefreshTime;
    private Vector2 desiredMoveTarget;
    private bool isDead;
    private bool isActing;
    private bool suppressEchoLineForNextShot;

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
        aimTelegraph = GetComponent<EnemyAimTelegraph>();
        if (aimTelegraph == null)
            aimTelegraph = gameObject.AddComponent<EnemyAimTelegraph>();

        desiredMoveTarget = transform.position;
        RefreshTelegraphStyle();
    }

    protected override void OnTempoTierChanged(TempoManager.TempoTier tier)
    {
        base.OnTempoTierChanged(tier);
        RefreshTelegraphStyle();
    }

    private void Update()
    {
        if (isDead || isStunned || playerTransform == null)
            return;

        FacePlayer();

        Vector2 origin = firePoint != null ? (Vector2)firePoint.position : (Vector2)transform.position;
        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);
        bool hasLineOfSight = EnemyLineOfSightUtility.HasLineOfSight(origin, playerTransform, lineOfSightMask, transform);

        if (!isActing && distanceToPlayer <= repositionTriggerRange && Time.time >= nextRepositionTime)
        {
            StartCoroutine(RepositionRoutine());
            return;
        }

        if (!isActing && hasLineOfSight && distanceToPlayer >= minimumRange && distanceToPlayer <= maximumRange && Time.time >= nextShotTime)
        {
            StartCoroutine(LockAimRoutine());
            return;
        }

        UpdateMovement(hasLineOfSight, distanceToPlayer, origin);
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
        isActing = false;
        if (aimTelegraph != null)
            aimTelegraph.Hide();

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
        isActing = false;
        if (aimTelegraph != null)
            aimTelegraph.Hide();

        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
            col.enabled = false;

        if (animator != null)
            animator.SetTrigger("Die");
    }

    private void UpdateMovement(bool hasLineOfSight, float distanceToPlayer, Vector2 firingOrigin)
    {
        float refreshInterval = movementRefreshInterval * tempoConfig.movementRefreshMultiplier.Evaluate(CurrentTempoTier);
        if (Time.time >= nextMoveRefreshTime)
        {
            RangedTacticalDecision decision = RangedTacticalMovementUtility.EvaluatePosition(
                transform,
                playerTransform,
                firingOrigin,
                preferredRange,
                firePermissionRange,
                tacticalSearchRadius,
                Mathf.RoundToInt(repositionSampleCount * tempoConfig.repositionSampleMultiplier.Evaluate(CurrentTempoTier)),
                transform);
            desiredMoveTarget = decision.foundBetterPosition ? decision.moveTarget : ChooseMovementTarget(hasLineOfSight, distanceToPlayer);
            nextMoveRefreshTime = Time.time + Mathf.Max(0.08f, refreshInterval);
        }

        Vector2 toTarget = desiredMoveTarget - (Vector2)transform.position;
        bool isMoving = toTarget.magnitude > movementTolerance;

        if (rb != null)
        {
            float speed = GetEffectiveMoveSpeedFromData(3f);
            if (HasEliteMechanic(EliteMechanicType.DeadeyeEchoLine) && activeEchoLine != null && activeEchoLine.IsActive && ActiveEliteProfile != null)
                speed *= ActiveEliteProfile.deadeyeEchoLine.mobilityMultiplierWhileActive;
            rb.linearVelocity = isMoving ? toTarget.normalized * speed : Vector2.zero;
        }

        if (animator != null)
            animator.SetBool("IsMoving", isMoving);
    }

    private Vector2 ChooseMovementTarget(bool hasLineOfSight, float distanceToPlayer)
    {
        Vector2 self = transform.position;
        Vector2 player = playerTransform.position;

        if (distanceToPlayer < minimumRange)
        {
            Vector2 away = (self - player).normalized;
            if (away.sqrMagnitude <= 0.001f)
                away = Vector2.right;
            return self + away * (preferredRange - distanceToPlayer + 1.5f);
        }

        if (distanceToPlayer > maximumRange)
        {
            Vector2 toward = (player - self).normalized;
            return player - toward * preferredRange;
        }

        if (hasLineOfSight)
            return self;

        Vector2 best = self;
        float bestScore = float.MinValue;
        int samples = Mathf.Max(4, Mathf.RoundToInt(repositionSampleCount * tempoConfig.repositionSampleMultiplier.Evaluate(CurrentTempoTier)));
        for (int i = 0; i < samples; i++)
        {
            float angle = (360f / samples) * i;
            Vector2 dir = Quaternion.Euler(0f, 0f, angle) * Vector2.right;
            Vector2 candidate = player + dir * preferredRange;
            bool candidateHasLine = EnemyLineOfSightUtility.HasLineOfSight(candidate, playerTransform, lineOfSightMask, transform);
            float score = 0f;
            if (candidateHasLine)
                score += 100f;
            score -= Vector2.Distance(self, candidate) * 0.35f;
            score -= Mathf.Abs(Vector2.Distance(candidate, player) - preferredRange) * 2f;

            if (CurrentTempoTier >= TempoManager.TempoTier.T2)
                score += Mathf.Max(0f, Vector2.Dot((candidate - player).normalized, (self - player).normalized)) * 4f;

            if (score > bestScore)
            {
                bestScore = score;
                best = candidate;
            }
        }

        return best;
    }

    private IEnumerator LockAimRoutine()
    {
        isActing = true;
        EmitCombatAction(EnemyCombatActionType.Attack);
        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        if (spriteRenderer != null)
            spriteRenderer.color = lockAimColor;

        float timer = 0f;
        float duration = GetEffectiveCooldownDuration(lockAimDuration * tempoConfig.lockAimDurationMultiplier.Evaluate(CurrentTempoTier));
        Vector2 finalDirection = Vector2.right;
        while (timer < duration)
        {
            if (playerTransform != null)
            {
                Vector2 start = firePoint != null ? firePoint.position : transform.position;
                finalDirection = ((Vector2)playerTransform.position - start).normalized;
                if (finalDirection.sqrMagnitude <= 0.001f)
                    finalDirection = Vector2.right;

                if (aimTelegraph != null)
                    aimTelegraph.Show(start, start + finalDirection * maximumRange);
            }

            timer += Time.deltaTime;
            yield return null;
        }

        if (aimTelegraph != null)
            aimTelegraph.Hide();
        if (spriteRenderer != null)
            spriteRenderer.color = Color.white;

        if (animator != null)
            animator.SetTrigger("Attack");

        FireProjectile(projectilePrefab, finalDirection, false);

        if (CurrentTempoTier == TempoManager.TempoTier.T3)
            yield return new WaitForSeconds(tempoConfig.t3RecoveryDuration);

        nextShotTime = Time.time + GetEffectiveCooldownDuration(shotCooldown) / Mathf.Max(0.01f, GetSupportAttackSpeedMultiplier());
        isActing = false;
    }

    private IEnumerator RepositionRoutine()
    {
        isActing = true;
        EmitCombatAction(EnemyCombatActionType.Dash);
        if (aimTelegraph != null)
            aimTelegraph.Hide();

        Vector2 escapePoint;
        if (!RangedTacticalMovementUtility.TryFindEscapePoint(transform, playerTransform.position, repositionTriggerRange + 1.8f, 140f, 8, transform, out escapePoint))
            escapePoint = (Vector2)transform.position + (((Vector2)transform.position - (Vector2)playerTransform.position).normalized * (repositionTriggerRange + 1.5f));

        Vector2 away = (escapePoint - (Vector2)transform.position).normalized;
        if (away.sqrMagnitude <= 0.001f)
            away = Vector2.right;

        if (animator != null)
            animator.SetTrigger("Attack");

        FireProjectile(controlShotPrefab, -away, true);

        float timer = 0f;
        while (timer < repositionDuration)
        {
            if (rb != null)
                rb.linearVelocity = away * repositionSpeed;
            timer += Time.deltaTime;
            yield return null;
        }

        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        nextRepositionTime = Time.time + GetEffectiveCooldownDuration(repositionCooldown);
        isActing = false;
    }

    private void FireProjectile(GameObject prefab, Vector2 direction, bool isControlShot)
    {
        if (prefab == null || firePoint == null)
            return;

        GameObject projObj = Instantiate(prefab, firePoint.position, Quaternion.identity);
        Projectile projectile = projObj.GetComponent<Projectile>();
        if (projectile == null)
            return;

        projectile.owner = gameObject;
        projectile.damage = GetEffectiveDamageFromData(projectile.damage);

        if (isControlShot)
        {
            projectile.lifeTime = Mathf.Min(projectile.lifeTime, controlShotLifeTimeOverride);
        }
        else if (CurrentTempoTier == TempoManager.TempoTier.T3)
        {
            projectile.damage *= tempoConfig.t3ShotDamageMultiplier;
            projectile.speed *= tempoConfig.t3ProjectileSpeedMultiplier;
            projObj.transform.localScale *= 1.1f;
        }

        projectile.Launch(direction);

        if (!isControlShot && HasEliteMechanic(EliteMechanicType.DeadeyeEchoLine) && ActiveEliteProfile != null && !suppressEchoLineForNextShot)
            SpawnEchoLine(direction);

        suppressEchoLineForNextShot = false;
    }

    private void FacePlayer()
    {
        if (spriteRenderer == null || playerTransform == null)
            return;

        spriteRenderer.flipX = playerTransform.position.x < transform.position.x;
    }

    private void RefreshTelegraphStyle()
    {
        if (aimTelegraph == null)
            return;

        if (CurrentTempoTier == TempoManager.TempoTier.T3)
        {
            aimTelegraph.Configure(
                tempoConfig.t3TelegraphColor,
                telegraphStartWidth * tempoConfig.t3TelegraphWidthMultiplier,
                telegraphEndWidth * tempoConfig.t3TelegraphWidthMultiplier,
                120);
            return;
        }

        aimTelegraph.Configure(lockAimColor, telegraphStartWidth, telegraphEndWidth, 120);
    }

    private void SpawnEchoLine(Vector2 direction)
    {
        EliteDeadeyeEchoLineSettings settings = ActiveEliteProfile != null ? ActiveEliteProfile.deadeyeEchoLine : null;
        if (settings == null || activeEchoLine != null && activeEchoLine.IsActive || firePoint == null)
            return;

        GameObject lineObject = new GameObject("DeadeyeEchoLine");
        activeEchoLine = lineObject.AddComponent<DeadeyeEchoLine>();
        Vector2 start = firePoint.position;
        Vector2 end = start + direction.normalized * maximumRange;
        activeEchoLine.Configure(start, end, settings.lineDuration, settings.lineThickness, settings.echoLineColor, OnEchoLineTriggered);
    }

    private void OnEchoLineTriggered()
    {
        if (isDead || isStunned || playerTransform == null || !HasEliteMechanic(EliteMechanicType.DeadeyeEchoLine) || ActiveEliteProfile == null)
            return;

        if (!isActing)
            StartCoroutine(EchoRefireRoutine());
    }

    private IEnumerator EchoRefireRoutine()
    {
        isActing = true;
        suppressEchoLineForNextShot = true;
        Vector2 aimDirection = ((Vector2)playerTransform.position - (Vector2)(firePoint != null ? firePoint.position : transform.position)).normalized;
        float duration = ActiveEliteProfile.deadeyeEchoLine.refireLockAimDuration;
        float timer = 0f;
        while (timer < duration)
        {
            if (playerTransform != null && firePoint != null)
            {
                aimDirection = ((Vector2)playerTransform.position - (Vector2)firePoint.position).normalized;
                if (aimTelegraph != null)
                    aimTelegraph.Show(firePoint.position, (Vector2)firePoint.position + aimDirection * maximumRange);
            }
            timer += Time.deltaTime;
            yield return null;
        }

        if (aimTelegraph != null)
            aimTelegraph.Hide();
        FireProjectile(projectilePrefab, aimDirection, false);
        nextShotTime = Time.time + GetEffectiveCooldownDuration(shotCooldown) / Mathf.Max(0.01f, GetSupportAttackSpeedMultiplier());
        isActing = false;
    }
}

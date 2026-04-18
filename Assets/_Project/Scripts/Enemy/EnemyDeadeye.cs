using System.Collections;
using UnityEngine;

public class EnemyDeadeye : EnemyBase
{
    [Header("Shot")]
    public GameObject projectilePrefab;
    public Transform firePoint;
    public float preferredRange = 10f;
    public float minimumRange = 5.5f;
    public float maximumRange = 14f;
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
    public LayerMask lineOfSightMask = Physics2D.DefaultRaycastLayers;

    [Header("Animation")]
    [SerializeField] private float deathAnimDuration = 0.7f;

    private Transform playerTransform;
    private Animator animator;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private EnemyAimTelegraph aimTelegraph;

    private float nextShotTime;
    private float nextRepositionTime;
    private float nextMoveRefreshTime;
    private Vector2 desiredMoveTarget;
    private bool isDead;
    private bool isActing;

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
        aimTelegraph.Configure(lockAimColor, telegraphStartWidth, telegraphEndWidth);

        desiredMoveTarget = transform.position;
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

        UpdateMovement(hasLineOfSight, distanceToPlayer);
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

    private void UpdateMovement(bool hasLineOfSight, float distanceToPlayer)
    {
        if (Time.time >= nextMoveRefreshTime)
        {
            desiredMoveTarget = ChooseMovementTarget(hasLineOfSight, distanceToPlayer);
            nextMoveRefreshTime = Time.time + movementRefreshInterval;
        }

        Vector2 toTarget = desiredMoveTarget - (Vector2)transform.position;
        bool isMoving = toTarget.magnitude > movementTolerance;

        if (rb != null)
        {
            float speed = (enemyData != null ? enemyData.moveSpeed : 3f) * GetSupportMoveSpeedMultiplier();
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
        int samples = Mathf.Max(4, repositionSampleCount);
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
        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        if (spriteRenderer != null)
            spriteRenderer.color = lockAimColor;

        float timer = 0f;
        Vector2 finalDirection = Vector2.right;
        while (timer < lockAimDuration)
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

        nextShotTime = Time.time + shotCooldown / Mathf.Max(0.01f, GetSupportAttackSpeedMultiplier());
        isActing = false;
    }

    private IEnumerator RepositionRoutine()
    {
        isActing = true;
        if (aimTelegraph != null)
            aimTelegraph.Hide();

        Vector2 away = ((Vector2)transform.position - (Vector2)playerTransform.position).normalized;
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

        nextRepositionTime = Time.time + repositionCooldown;
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
        projectile.damage = enemyData != null ? enemyData.damage : projectile.damage;

        if (isControlShot)
            projectile.lifeTime = Mathf.Min(projectile.lifeTime, controlShotLifeTimeOverride);

        projectile.Launch(direction);
    }

    private void FacePlayer()
    {
        if (spriteRenderer == null || playerTransform == null)
            return;

        spriteRenderer.flipX = playerTransform.position.x < transform.position.x;
    }
}

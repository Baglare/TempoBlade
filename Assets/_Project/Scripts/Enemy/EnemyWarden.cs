using System.Collections;
using UnityEngine;

public class EnemyWarden : EnemyBase, IParryReactive
{
    private enum WardenState
    {
        SeekingProtectTarget,
        Guarding,
        RepositionDash,
        ClosePunish,
        Berserk,
        Dead
    }

    private enum HitAngleZone
    {
        Front,
        Side,
        Back
    }

    private static readonly int AnimIsMoving = Animator.StringToHash("IsMoving");
    private static readonly int AnimIsGuarding = Animator.StringToHash("IsGuarding");
    private static readonly int AnimRepositionDash = Animator.StringToHash("RepositionDash");
    private static readonly int AnimClosePunish = Animator.StringToHash("ClosePunish");
    private static readonly int AnimBerserk = Animator.StringToHash("Berserk");
    private static readonly int AnimHurt = Animator.StringToHash("Hurt");
    private static readonly int AnimDie = Animator.StringToHash("Die");

    private const float ShieldPunishCooldown = 0.2f;
    private const float BerserkChargeDuration = 0.35f;
    private const float ProtectTargetRefreshInterval = 0.4f;

    [Header("Protection")]
    public float protectSearchRadius = 10f;
    public float interceptOffset = 1.2f;
    public float positionTolerance = 0.2f;
    public float repathInterval = 0.12f;
    public float guardMoveSpeed = 2f;

    [Header("Shield")]
    public float shieldFrontHalfAngle = 60f;
    [Range(0f, 1f)] public float reducedSideDamageMultiplier = 0.45f;
    public float shieldHitPlayerStaggerDuration = 0.2f;
    public float shieldHitTempoSteal = 8f;
    public float shieldHitKnockback = 7f;

    [Header("Reposition Dash")]
    public float dashTriggerDistance = 1.15f;
    public float repositionDashSpeed = 12f;
    public float repositionDashDuration = 0.2f;
    public float repositionDashCooldown = 1.25f;

    [Header("Close Punish")]
    public float closePunishRange = 1.35f;
    public float closePunishWindup = 0.45f;
    public float closePunishDamage = 18f;
    public float closePunishStunDuration = 0.8f;
    public float closePunishCooldown = 2.4f;
    public float closePunishRecovery = 0.8f;
    public Transform attackPoint;
    public float closePunishRadius = 1f;

    [Header("Berserk")]
    public float berserkMoveSpeed = 4.5f;
    public float berserkChargeSpeed = 13f;
    public float berserkChargeCooldown = 1.4f;
    public float berserkDuration = 5f;
    public float collisionDamage = 14f;
    public float collisionDamageInterval = 0.45f;

    [Header("Animation")]
    [SerializeField] private float deathAnimDuration = 0.7f;

    private Transform playerTransform;
    private PlayerController playerController;
    private PlayerCombat playerCombat;
    private Animator animator;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;

    private EnemyBase protectTarget;
    private WardenState currentState = WardenState.SeekingProtectTarget;
    private Coroutine activeActionRoutine;
    private float nextRepathTime;
    private Vector2 cachedGuardPoint;
    private float lastRepositionDashTime = -999f;
    private float lastClosePunishTime = -999f;
    private float lastShieldPunishTime = -999f;
    private float lastCollisionDamageTime = -999f;
    private float lastProtectTargetRefreshTime = -999f;
    private float lastBerserkChargeTime = -999f;
    private float berserkEndTime = -1f;
    private bool permanentBerserk;
    private bool isDead;
    private bool isExecutingAction;
    private bool isBerserkCharging;
    private float guardBrokenUntilTime;

    public bool AllowParryExecute => true;

    protected override void Start()
    {
        base.Start();
        deathDelay = deathAnimDuration;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
            playerController = player.GetComponent<PlayerController>();
            playerCombat = player.GetComponent<PlayerCombat>();
        }

        animator = GetComponentInChildren<Animator>();
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        TryAcquireProtectTarget();
        if (protectTarget == null && CountOtherLivingEnemies() <= 0)
            EnterBerserk(false);
        else if (protectTarget != null)
            currentState = WardenState.Guarding;
    }

    private void Update()
    {
        if (isDead || currentState == WardenState.Dead)
            return;

        if (playerTransform == null)
            return;

        if (isStunned)
        {
            if (rb != null)
                rb.linearVelocity = Vector2.zero;

            UpdateAnimator(false);
            return;
        }

        if (currentState == WardenState.Berserk)
        {
            UpdateBerserk();
            return;
        }

        if (protectTarget != null && !HasValidProtectTarget())
        {
            EnterBerserk(CountOtherLivingEnemies() > 0);
            return;
        }

        if (protectTarget == null && !TryAcquireProtectTarget())
        {
            EnterBerserk(CountOtherLivingEnemies() > 0);
            return;
        }

        UpdateGuarding();
    }

    public override void TakeDamage(float amount)
    {
        if (isDead)
            return;

        if (CanUseShieldDefense())
        {
            HitAngleZone hitZone = EvaluateIncomingHitZone();
            if (hitZone == HitAngleZone.Front)
            {
                ApplyShieldFrontPunish();
                return;
            }

            if (hitZone == HitAngleZone.Side)
                amount *= Mathf.Clamp01(reducedSideDamageMultiplier);
        }

        if (animator != null)
            animator.SetTrigger(AnimHurt);

        base.TakeDamage(amount);
    }

    public override void Stun(float duration)
    {
        if (isDead)
            return;

        CancelActiveAction();
        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        base.Stun(duration);
    }

    public void OnParryReaction(ParryReactionContext context)
    {
        if (isDead)
            return;

        CancelActiveAction();

        if (context.breakGuard)
            guardBrokenUntilTime = Time.time + Mathf.Max(0.05f, context.duration);

        if (animator != null)
            animator.SetTrigger(AnimHurt);

        base.Stun(Mathf.Max(0.05f, context.duration));
    }

    protected override void OnDeathAnimationStart()
    {
        isDead = true;
        currentState = WardenState.Dead;
        CancelActiveAction();

        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        if (animator != null)
            animator.SetTrigger(AnimDie);

        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
            col.enabled = false;
    }

    private void UpdateGuarding()
    {
        currentState = WardenState.Guarding;
        FaceTowardsPlayer();
        float moveSpeedMultiplier = GetSupportMoveSpeedMultiplier();
        float attackSpeedMultiplier = Mathf.Max(0.01f, GetSupportAttackSpeedMultiplier());

        if (Time.time >= nextRepathTime)
        {
            cachedGuardPoint = GetGuardPoint();
            nextRepathTime = Time.time + repathInterval;
        }

        if (!isExecutingAction && CanStartClosePunish())
        {
            StartAction(ClosePunishRoutine());
            return;
        }

        if (!isExecutingAction && CanStartRepositionDash())
        {
            StartAction(RepositionDashRoutine());
            return;
        }

        float distance = Vector2.Distance(transform.position, cachedGuardPoint);
        bool isMoving = distance > positionTolerance;

        if (rb != null)
        {
            if (isMoving)
            {
                Vector2 dir = (cachedGuardPoint - (Vector2)transform.position).normalized;
                rb.linearVelocity = dir * guardMoveSpeed * moveSpeedMultiplier;
            }
            else
            {
                rb.linearVelocity = Vector2.zero;
            }
        }

        UpdateAnimator(isMoving);
    }

    private void UpdateBerserk()
    {
        FaceTowardsPlayer();
        float moveSpeedMultiplier = GetSupportMoveSpeedMultiplier();

        if (!permanentBerserk && Time.time >= berserkEndTime)
        {
            if (TryAcquireProtectTarget())
            {
                currentState = WardenState.Guarding;
                permanentBerserk = false;
                if (rb != null)
                    rb.linearVelocity = Vector2.zero;
                return;
            }

            permanentBerserk = true;
        }

        if (!isExecutingAction && Time.time >= lastBerserkChargeTime + (berserkChargeCooldown / Mathf.Max(0.01f, GetSupportAttackSpeedMultiplier())))
        {
            StartAction(BerserkChargeRoutine());
            return;
        }

        if (rb != null)
        {
            Vector2 dir = ((Vector2)playerTransform.position - (Vector2)transform.position).normalized;
            rb.linearVelocity = dir * berserkMoveSpeed * moveSpeedMultiplier;
        }

        UpdateAnimator(true);
    }

    private IEnumerator RepositionDashRoutine()
    {
        isExecutingAction = true;
        currentState = WardenState.RepositionDash;
        lastRepositionDashTime = Time.time;
        float moveSpeedMultiplier = GetSupportMoveSpeedMultiplier();

        if (animator != null)
            animator.SetTrigger(AnimRepositionDash);

        float elapsed = 0f;
        while (elapsed < repositionDashDuration)
        {
            if (protectTarget == null)
                break;

            Vector2 desired = GetGuardPoint();
            Vector2 toPoint = desired - (Vector2)transform.position;
            if (toPoint.magnitude <= positionTolerance)
                break;

            if (rb != null)
                rb.linearVelocity = toPoint.normalized * repositionDashSpeed * moveSpeedMultiplier;

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        isExecutingAction = false;
        currentState = WardenState.Guarding;
    }

    private IEnumerator ClosePunishRoutine()
    {
        isExecutingAction = true;
        currentState = WardenState.ClosePunish;
        lastClosePunishTime = Time.time;
        float attackSpeedMultiplier = Mathf.Max(0.01f, GetSupportAttackSpeedMultiplier());

        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        if (animator != null)
            animator.SetTrigger(AnimClosePunish);

        yield return new WaitForSeconds(closePunishWindup / attackSpeedMultiplier);

        if (!isDead && !isStunned && playerTransform != null)
        {
            Vector2 strikeOrigin = attackPoint != null ? (Vector2)attackPoint.position : (Vector2)transform.position;
            float strikeRadius = Mathf.Max(0.05f, closePunishRadius);
            bool playerInRange = Vector2.Distance(strikeOrigin, playerTransform.position) <= strikeRadius;

            if (playerInRange)
            {
                ParrySystem parry = playerTransform.GetComponent<ParrySystem>();
                if (parry != null && parry.TryBlockMelee(strikeOrigin, gameObject))
                {
                    yield return new WaitForSeconds(closePunishRecovery / attackSpeedMultiplier);
                    isExecutingAction = false;
                    currentState = WardenState.Guarding;
                    yield break;
                }

                if (playerController != null && playerController.IsInvulnerable)
                {
                    playerTransform.GetComponent<DashPerkController>()?.NotifyMeleeDodged(this);
                }
                else
                {
                    if (playerCombat != null)
                        playerCombat.TakeDamage(closePunishDamage);

                    playerController?.ApplyExternalStagger(closePunishStunDuration, Vector2.zero);
                }
            }
        }

        yield return new WaitForSeconds(closePunishRecovery / attackSpeedMultiplier);

        isExecutingAction = false;
        currentState = WardenState.Guarding;
    }

    private IEnumerator BerserkChargeRoutine()
    {
        isExecutingAction = true;
        isBerserkCharging = true;
        currentState = WardenState.Berserk;
        lastBerserkChargeTime = Time.time;

        float elapsed = 0f;
        while (elapsed < BerserkChargeDuration)
        {
            if (playerTransform == null)
                break;

            Vector2 dir = ((Vector2)playerTransform.position - (Vector2)transform.position).normalized;
            if (rb != null)
                rb.linearVelocity = dir * berserkChargeSpeed;

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        isBerserkCharging = false;
        isExecutingAction = false;
    }

    private void StartAction(IEnumerator routine)
    {
        CancelActiveAction();
        activeActionRoutine = StartCoroutine(routine);
    }

    private void CancelActiveAction()
    {
        if (activeActionRoutine != null)
            StopCoroutine(activeActionRoutine);

        activeActionRoutine = null;
        isExecutingAction = false;
        isBerserkCharging = false;
    }

    private void EnterBerserk(bool temporary)
    {
        protectTarget = null;
        currentState = WardenState.Berserk;
        permanentBerserk = !temporary;
        berserkEndTime = temporary ? Time.time + berserkDuration : float.PositiveInfinity;
        guardBrokenUntilTime = 0f;
        CancelActiveAction();

        if (animator != null)
            animator.SetBool(AnimBerserk, true);
    }

    private bool TryAcquireProtectTarget()
    {
        if (Time.time < lastProtectTargetRefreshTime + ProtectTargetRefreshInterval && HasValidProtectTarget())
            return true;

        lastProtectTargetRefreshTime = Time.time;

        EnemyBase[] allEnemies = FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);
        EnemyBase bestPriorityTarget = null;
        float bestPriorityDistance = float.MaxValue;
        EnemyBase bestFallbackTarget = null;
        float bestFallbackDistance = float.MaxValue;

        for (int i = 0; i < allEnemies.Length; i++)
        {
            EnemyBase enemy = allEnemies[i];
            if (enemy == null || enemy == this || enemy.CurrentHealth <= 0f || !enemy.gameObject.activeInHierarchy)
                continue;

            float distance = Vector2.Distance(transform.position, enemy.transform.position);
            if (distance > protectSearchRadius)
                continue;

            bool isPriority = enemy.enemyData != null && enemy.enemyData.wardenProtectPriority == WardenProtectPriority.RangedSupport;
            if (isPriority)
            {
                if (distance < bestPriorityDistance)
                {
                    bestPriorityDistance = distance;
                    bestPriorityTarget = enemy;
                }
            }
            else if (distance < bestFallbackDistance)
            {
                bestFallbackDistance = distance;
                bestFallbackTarget = enemy;
            }
        }

        protectTarget = bestPriorityTarget != null ? bestPriorityTarget : bestFallbackTarget;
        if (protectTarget != null)
        {
            currentState = WardenState.Guarding;
            permanentBerserk = false;
            if (animator != null)
                animator.SetBool(AnimBerserk, false);
        }

        return protectTarget != null;
    }

    private bool HasValidProtectTarget()
    {
        return protectTarget != null &&
               protectTarget.CurrentHealth > 0f &&
               protectTarget.gameObject.activeInHierarchy;
    }

    private int CountOtherLivingEnemies()
    {
        EnemyBase[] allEnemies = FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);
        int count = 0;
        for (int i = 0; i < allEnemies.Length; i++)
        {
            EnemyBase enemy = allEnemies[i];
            if (enemy == null || enemy == this || enemy.CurrentHealth <= 0f || !enemy.gameObject.activeInHierarchy)
                continue;

            count++;
        }

        return count;
    }

    private Vector2 GetGuardPoint()
    {
        if (protectTarget == null || playerTransform == null)
            return transform.position;

        Vector2 targetPos = protectTarget.transform.position;
        Vector2 toPlayer = (Vector2)playerTransform.position - targetPos;
        if (toPlayer.sqrMagnitude <= 0.001f)
            return targetPos;

        return targetPos + toPlayer.normalized * interceptOffset;
    }

    private bool CanStartRepositionDash()
    {
        if (protectTarget == null)
            return false;

        if (Time.time < lastRepositionDashTime + repositionDashCooldown)
            return false;

        return Vector2.Distance(transform.position, cachedGuardPoint) >= dashTriggerDistance;
    }

    private bool CanStartClosePunish()
    {
        if (Time.time < lastClosePunishTime + (closePunishCooldown / Mathf.Max(0.01f, GetSupportAttackSpeedMultiplier())))
            return false;

        return Vector2.Distance(transform.position, playerTransform.position) <= closePunishRange;
    }

    private bool CanUseShieldDefense()
    {
        return currentState != WardenState.Berserk &&
               Time.time >= guardBrokenUntilTime;
    }

    private HitAngleZone EvaluateIncomingHitZone()
    {
        if (playerTransform == null)
            return HitAngleZone.Front;

        Vector2 toPlayer = ((Vector2)playerTransform.position - (Vector2)transform.position).normalized;
        Vector2 forward = GetShieldForward();
        float angle = Vector2.Angle(forward, toPlayer);

        if (angle <= shieldFrontHalfAngle)
            return HitAngleZone.Front;

        if (angle >= 180f - shieldFrontHalfAngle)
            return HitAngleZone.Back;

        return HitAngleZone.Side;
    }

    private Vector2 GetShieldForward()
    {
        if (playerTransform != null)
        {
            Vector2 toPlayer = (Vector2)playerTransform.position - (Vector2)transform.position;
            if (toPlayer.sqrMagnitude > 0.001f)
                return toPlayer.normalized;
        }

        return transform.localScale.x >= 0f ? Vector2.right : Vector2.left;
    }

    private void ApplyShieldFrontPunish()
    {
        if (Time.time < lastShieldPunishTime + ShieldPunishCooldown)
            return;

        lastShieldPunishTime = Time.time;

        if (playerController != null)
        {
            Vector2 away = ((Vector2)playerTransform.position - (Vector2)transform.position).normalized;
            playerController.ApplyExternalStagger(
                shieldHitPlayerStaggerDuration,
                away * shieldHitKnockback);
        }

        if (TempoManager.Instance != null && shieldHitTempoSteal > 0f)
            TempoManager.Instance.AddTempo(-shieldHitTempoSteal);
    }

    private void FaceTowardsPlayer()
    {
        if (playerTransform == null)
            return;

        Vector3 scale = transform.localScale;
        scale.x = playerTransform.position.x < transform.position.x ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x);
        transform.localScale = scale;
    }

    private void UpdateAnimator(bool isMoving)
    {
        if (animator == null)
            return;

        animator.SetBool(AnimIsMoving, isMoving);
        animator.SetBool(AnimIsGuarding, CanUseShieldDefense() && currentState != WardenState.Berserk && !isExecutingAction);
        animator.SetBool(AnimBerserk, currentState == WardenState.Berserk);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryApplyBerserkCollisionDamage(collision.collider);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        TryApplyBerserkCollisionDamage(collision.collider);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryApplyBerserkCollisionDamage(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryApplyBerserkCollisionDamage(other);
    }

    private void TryApplyBerserkCollisionDamage(Collider2D other)
    {
        if (currentState != WardenState.Berserk || other == null || !other.CompareTag("Player"))
            return;

        if (Time.time < lastCollisionDamageTime + collisionDamageInterval)
            return;

        if (playerController != null && playerController.IsInvulnerable)
            return;

        lastCollisionDamageTime = Time.time;

        if (playerCombat != null)
            playerCombat.TakeDamage(collisionDamage);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, protectSearchRadius);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, closePunishRange);

        if (attackPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackPoint.position, closePunishRadius);
        }
    }
}

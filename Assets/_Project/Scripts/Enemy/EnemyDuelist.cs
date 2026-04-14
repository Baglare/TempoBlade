using UnityEngine;
using System.Collections;

public class EnemyDuelist : EnemyBase, IParryReactive
{
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
    public float attackWindup = 0.6f; // Telegraph duration
    public Transform attackPoint;
    public float attackRadius = 1f;

    [Header("Arc Visual")]
    [Tooltip("WeaponArcVisual component'i. Enemy altındaki child'a eklenir.")]
    public WeaponArcVisual weaponArcVisual;

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

    public bool AllowParryExecute => true;

    protected override void Start()
    {
        base.Start();
        deathDelay = deathAnimDuration;
        // Find player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) playerTransform = player.transform;
        animator = GetComponentInChildren<Animator>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        
        // Start with guard up
        isGuarding = true;
        guardBroken = false;

        if (weaponArcVisual != null)
            weaponArcVisual.range = attackRadius;
    }

    private void Update()
    {
        if (isDead) return;

        // Kılıç/yay görselini her frame güncelle
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

        // Face Player
        FacePlayer();
        
        // Renk degisimi ile durum goster (Gecici Visual Feedback)
        UpdateVisuals();
        SyncAttackPointToSprite();

        if (isAttacking) return;

        float dist = Vector2.Distance(transform.position, playerTransform.position);

        if (dist <= attackRange)
        {
            // Stop and Attack if ready
            if (Time.time >= nextAttackTime && !guardBroken)
            {
                StartCoroutine(AttackRoutine());
            }
        }
        else
        {
            // Approach slowly with guard
            MoveTowardsPlayer();
            isMoving = true;
        }

        UpdateAnimatorState(isMoving);
    }

    private void FacePlayer()
    {
        // Yon degisimini sadece saldirmiyorken yap (Dark Souls tarzi commitment)
        // AttackRoutine icinde isAttacking=true oldugu surece burasi calismaz.
        // Boylece saldiri basladigi an yon kilitlenir.
        if (isAttacking) return;

        if (playerTransform.position.x > transform.position.x)
            transform.localScale = new Vector3(1, 1, 1);
        else
            transform.localScale = new Vector3(-1, 1, 1);
    }

    private void MoveTowardsPlayer()
    {
        transform.position = Vector2.MoveTowards(transform.position, playerTransform.position, moveSpeed * Time.deltaTime);
    }

    private IEnumerator AttackRoutine()
    {
        isAttacking = true;
        isGuarding = false; // Drop guard to attack!
        UpdateAnimatorState(false);
        if (animator != null) animator.SetTrigger(AnimAttack);

        // 1. Telegraph (Hazirlik)
        SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
        Color originalColor = Color.white; 
        if (sr != null) originalColor = sr.color;
        
        // Danger visual (Kirmizi yanip sonme)
        if (sr != null) sr.color = Color.red; 

        // Tempo'ya gore agresiflesme (Windup kisalir)
        float currentWindup = attackWindup;
        if (TempoManager.Instance != null)
        {
            var tier = TempoManager.Instance.CurrentTier;
            if (tier == TempoManager.TempoTier.T2) currentWindup *= 0.75f; // %25 Daha hizli vurur
            if (tier == TempoManager.TempoTier.T3) currentWindup *= 0.6f;  // %40 Daha hizli vurur!
        }

        yield return new WaitForSeconds(currentWindup);

        // 2. Strike
        // Check hit
        if (attackPoint != null)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(attackPoint.position, attackRadius);
            foreach(var hit in hits)
            {
                if (!hit.CompareTag("Player")) continue;

                // Yonlu parry kontrolu (saldiri noktasindan geliyormus gibi degerlendir)
                ParrySystem parry = hit.GetComponent<ParrySystem>();
                Vector2 strikeOrigin = attackPoint != null ? (Vector2)attackPoint.position : (Vector2)transform.position;
                if (parry != null && parry.TryBlockMelee(strikeOrigin, gameObject))
                {
                    continue;
                }

                IDamageable damageable = hit.GetComponent<IDamageable>();
                var playerController = hit.GetComponent<PlayerController>();
                if (playerController != null && playerController.IsInvulnerable)
                {
                    hit.GetComponent<DashPerkController>()?.NotifyMeleeDodged(this);
                    continue;
                }

                if (damageable != null)
                {
                    damageable.TakeDamage(damage);
                }
            }
        }

        if (sr != null) sr.color = originalColor;

        // 3. Recovery (Vulnerable period)
        float recoveryTime = 0.8f;
        float currentCooldown = attackCooldown;
        if (TempoManager.Instance != null)
        {
            var tier = TempoManager.Instance.CurrentTier;
            if (tier == TempoManager.TempoTier.T2) 
            {
                recoveryTime = 0.6f;
                currentCooldown *= 0.8f; // Bekleme suresi kisalir
            }
            if (tier == TempoManager.TempoTier.T3) 
            {
                recoveryTime = 0.4f;
                currentCooldown *= 0.6f; // Durmak bilmez
            }
        }

        yield return new WaitForSeconds(recoveryTime);

        isGuarding = !guardBroken; // Raise guard again
        isAttacking = false;
        nextAttackTime = Time.time + currentCooldown;
        UpdateAnimatorState(false);
    }

    public override void TakeDamage(float amount)
    {
        if (isDead) return;

        // Blok Kontrolu
        if (isGuarding && !guardBroken && playerTransform != null)
        {
            // Basit x yonu kontrolu (Cunku sadece saga/sola donuyoruz)
            float facingDir = transform.localScale.x; // 1 (Right) or -1 (Left)
            
            // Oyuncunun yonu (bana gore nerede?)
            float dirToPlayerX = Mathf.Sign(playerTransform.position.x - transform.position.x);

            // Eger oyuncu onumdeyse BLOKLA
            if (Mathf.Approximately(facingDir, dirToPlayerX))
            {
                // Block Effect
                 if (DamagePopupManager.Instance != null)
                     DamagePopupManager.Instance.CreateText(transform.position + Vector3.up * 1.5f, "BLOCK!", Color.cyan, 5f);
                

                
                // Belki bir ses veya spark efekti?
                // Geri tepme (Knockback) eklenebilir
                return; // Hasari iptal et
            }
        }

        // Blok degilse veya arkadan vurduysa hasar ye
        if (animator != null) animator.SetTrigger(AnimHurt);
        base.TakeDamage(amount);
    }
    
    private void UpdateVisuals()
    {
        SpriteRenderer sr = spriteRenderer != null ? spriteRenderer : GetComponentInChildren<SpriteRenderer>();
        if (sr == null) return;

        if (animator != null)
        {
            if (!isAttacking)
                sr.color = Color.white;
            return;
        }

        if (isAttacking) 
        {
            // AttackRoutine handles color override (Red)
        }
        else if (isGuarding)
        {
            sr.color = Color.blue; // Mavi = Defans (Kalkan)
        }
        else
        {
            // sr.color = Color.white; // Normal
            // Not: HitFlash karisabilir, o yuzden surekli set etmemek lazim.
            // Simdilik sadece Guard durumunu mavi yapiyoruz.
        }
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

        var rb = GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        var col = GetComponent<Collider2D>();
        if (col != null)
            col.enabled = false;
    }

    private void UpdateAnimatorState(bool isMoving)
    {
        if (animator == null) return;

        animator.SetBool(AnimIsMoving, isMoving);
        animator.SetBool(AnimIsGuarding, isGuarding && !guardBroken && !isAttacking && !isDead && !isStunned);
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

        if (context.breakGuard)
        {
            if (guardBreakRoutine != null)
                StopCoroutine(guardBreakRoutine);

            guardBreakRoutine = StartCoroutine(GuardBreakRoutine(Mathf.Max(0.05f, context.duration)));
            return;
        }

        base.Stun(Mathf.Max(0.05f, context.duration));
        StartCoroutine(RestoreGuardAfterDelay(Mathf.Max(0.05f, context.duration)));
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

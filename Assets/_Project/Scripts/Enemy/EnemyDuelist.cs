using UnityEngine;
using System.Collections;

public class EnemyDuelist : EnemyBase
{
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

    private Transform playerTransform;
    private float nextAttackTime;
    private bool isAttacking;

    protected override void Start()
    {
        base.Start();
        // Find player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) playerTransform = player.transform;
        
        // Start with guard up
        isGuarding = true;

        if (weaponArcVisual != null)
            weaponArcVisual.range = attackRadius;
    }

    private void Update()
    {
        // Kılıç/yay görselini her frame güncelle
        if (weaponArcVisual != null && playerTransform != null)
        {
            Vector2 dirToPlayer = ((Vector2)playerTransform.position - (Vector2)transform.position).normalized;
            weaponArcVisual.UpdateVisuals(transform.position, dirToPlayer, isAttacking, false);
        }

        if (isStunned || playerTransform == null) return;

        // Face Player
        FacePlayer();
        
        // Renk degisimi ile durum goster (Gecici Visual Feedback)
        UpdateVisuals();

        if (isAttacking) return;

        float dist = Vector2.Distance(transform.position, playerTransform.position);

        if (dist <= attackRange)
        {
            // Stop and Attack if ready
            if (Time.time >= nextAttackTime)
            {
                StartCoroutine(AttackRoutine());
            }
        }
        else
        {
            // Approach slowly with guard
            MoveTowardsPlayer();
        }
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

                DashSkillRuntime dashRuntime = hit.GetComponent<DashSkillRuntime>();
                if (dashRuntime == null) dashRuntime = hit.GetComponentInParent<DashSkillRuntime>();
                if (dashRuntime != null && dashRuntime.TryDodgeMelee(transform.position))
                    continue;

                // Yonlu parry kontrolu (saldiri noktasindan geliyormus gibi degerlendir)
                ParrySystem parry = hit.GetComponent<ParrySystem>();
                if (parry == null) parry = hit.GetComponentInParent<ParrySystem>();
                Vector2 strikeOrigin = attackPoint != null ? (Vector2)attackPoint.position : (Vector2)transform.position;
                if (parry != null && parry.TryBlockMelee(strikeOrigin))
                {
                    Stun(1.0f); // Duelist parry edilirse sendeler
                    continue;
                }

                IDamageable damageable = hit.GetComponent<IDamageable>();
                if (damageable == null) damageable = hit.GetComponentInParent<IDamageable>();
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

        isGuarding = true; // Raise guard again
        isAttacking = false;
        nextAttackTime = Time.time + currentCooldown;
    }

    public override void TakeDamage(float amount)
    {
        // Blok Kontrolu
        if (isGuarding && playerTransform != null)
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
        base.TakeDamage(amount);
    }
    
    private void UpdateVisuals()
    {
        SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
        if (sr == null) return;

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

    private void OnDrawGizmosSelected()
    {
        if (attackPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackPoint.position, attackRadius);
        }
    }
}

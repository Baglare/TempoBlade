using UnityEngine;
using System.Collections;

/// <summary>
/// Uzak mesafeli büyücü düşman.
///
/// Animator Controller'da şu parametreler tanımlanmalı:
///   - isMoving   (Bool)    : hareket/idle geçişi
///   - Attack     (Trigger) : ateş etme anı
///   - TakeHit    (Trigger) : hasar alma
///   - Die        (Trigger) : ölüm
///
/// State geçişleri (önerilen):
///   Idle ←→ Walk            (isMoving bool)
///   Any State → TakeHit     (TakeHit trigger, Has Exit Time: false)
///   Any State → Die         (Die trigger, Has Exit Time: false)
///   Idle/Walk → Attack      (Attack trigger)
///   Attack/TakeHit → Idle   (Has Exit Time: true, otomatik dönüş)
///   Die → [Exit]            (Has Exit Time: true, Loop Time: false)
/// </summary>
public class EnemyCaster : EnemyBase
{
    [Header("Caster Settings")]
    public GameObject projectilePrefab;
    public Transform firePoint;
    public float attackRange = 8f;   // Ateş etme mesafesi
    public float retreatRange = 5f;  // Çok yaklaşırsa kaçma mesafesi
    public float fireRate = 2f;

    [Header("Telegraph")]
    public float telegraphTime = 0.5f;
    public LineRenderer telegraphLine;

    [Header("Animasyon")]
    [Tooltip("Ölüm animasyonu süresi (saniye). Animator'daki Death clip süresiyle eşleştir.")]
    [SerializeField] private float deathAnimDuration = 0.8f;

    // Önbellek
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    // Durum
    private float nextFireTime = 0f;
    private Transform playerTransform;
    private bool isAttacking = false;
    private bool isDead = false;

    protected override void Start()
    {
        base.Start();

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) playerTransform = player.transform;

        animator = GetComponentInChildren<Animator>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (telegraphLine != null) telegraphLine.enabled = false;

        // EnemyBase'e ölüm gecikmesini bildir (animasyon bitene kadar bekle)
        deathDelay = deathAnimDuration;
    }

    private void Update()
    {
        if (isDead || isStunned || playerTransform == null) return;

        float dist = Vector2.Distance(transform.position, playerTransform.position);

        FacePlayer();

        bool moving = false;

        if (!isAttacking)
        {
            if (dist > attackRange)
            {
                MoveTowards(playerTransform.position);
                moving = true;
            }
            else if (dist < retreatRange)
            {
                // Kiting: oyuncudan uzaklaş
                Vector2 dir = ((Vector2)transform.position - (Vector2)playerTransform.position).normalized;
                MoveTowards((Vector2)transform.position + dir);
                moving = true;
            }
            else
            {
                StopMovement();
                if (Time.time >= nextFireTime)
                    StartCoroutine(AttackRoutine());
            }
        }

        if (animator != null)
            animator.SetBool("IsMoving", moving);
    }

    // ------------------------------------------------------------------ //

    private void FacePlayer()
    {
        if (spriteRenderer == null || playerTransform == null) return;
        spriteRenderer.flipX = playerTransform.position.x < transform.position.x;
    }

    private void MoveTowards(Vector2 target)
    {
        float speed = enemyData != null ? enemyData.moveSpeed : 3f;
        transform.position = Vector2.MoveTowards(transform.position, target, speed * Time.deltaTime);
    }

    private void StopMovement()
    {
        var rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;
    }

    // ------------------------------------------------------------------ //

    private IEnumerator AttackRoutine()
    {
        isAttacking = true;

        // 1. Telegraph: hedefi kilitle ve kırmızı uyarı ver
        if (telegraphLine != null) telegraphLine.enabled = true;
        Color originalColor = spriteRenderer != null ? spriteRenderer.color : Color.white;
        if (spriteRenderer != null) spriteRenderer.color = Color.magenta;

        float timer = 0f;
        Vector2 aimDirection = Vector2.right;

        while (timer < telegraphTime)
        {
            if (playerTransform != null)
            {
                if (telegraphLine != null && firePoint != null)
                {
                    telegraphLine.SetPosition(0, firePoint.position);
                    telegraphLine.SetPosition(1, playerTransform.position);
                }
                if (firePoint != null)
                    aimDirection = ((Vector2)playerTransform.position - (Vector2)firePoint.position).normalized;
            }
            timer += Time.deltaTime;
            yield return null;
        }

        // 2. Ateş: kilitlenen yöne mermi at
        if (telegraphLine != null) telegraphLine.enabled = false;
        if (spriteRenderer != null) spriteRenderer.color = originalColor;

        if (animator != null) animator.SetTrigger("Attack");

        if (projectilePrefab != null && firePoint != null)
        {
            GameObject projObj = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
            Projectile proj = projObj.GetComponent<Projectile>();
            if (proj != null)
            {
                proj.owner = gameObject;
                proj.damage = enemyData != null ? enemyData.damage : proj.damage;
                proj.Launch(aimDirection);
            }
        }

        nextFireTime = Time.time + fireRate;
        isAttacking = false;
    }

    // ------------------------------------------------------------------ //
    // HASAR & ÖLÜM
    // ------------------------------------------------------------------ //

    public override void TakeDamage(float amount)
    {
        if (isDead) return;
        base.TakeDamage(amount); // canı düşür, micro-stun, Die() tetikleyebilir

        // Die() çağrıldıysa isDead zaten true; TakeHit tetiklenmesin
        if (!isDead && animator != null)
            animator.SetTrigger("TakeHit");
    }

    /// <summary>
    /// EnemyBase.Die() tarafından Destroy öncesinde çağrılır.
    /// Ölüm animasyonunu başlatır ve gameplay bileşenlerini devre dışı bırakır.
    /// </summary>
    protected override void OnDeathAnimationStart()
    {
        isDead = true;
        isAttacking = false;
        StopAllCoroutines();

        if (telegraphLine != null) telegraphLine.enabled = false;

        var rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;

        // Çarpışmayı kapat: ölü düşman player'ı bloklamasın
        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        if (animator != null) animator.SetTrigger("Die");
    }
}

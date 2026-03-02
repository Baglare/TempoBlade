using UnityEngine;
using System.Collections;

/// <summary>
/// Hızlı uzak mesafeli düşman.
///
/// – EnemyCaster'dan farklı olarak hareket ederken de ateş edebilir.
/// – Yüksek hareket hızı, sürekli kiting (strafe + geri çekilme).
/// – Hasar alınca belirli bir olasılıkla "perfect dash" yapar:
///   dashDuration boyunca hasar almaz (i-frame) ve geriye fırlar.
///
/// Animator parametreleri: IsMoving (bool), Attack (trigger), Die (trigger).
/// </summary>
public class EnemyDasher : EnemyBase
{
    [Header("Dasher Settings")]
    public GameObject projectilePrefab;
    public Transform  firePoint;
    [Tooltip("İdeal kiting mesafesi")]
    public float preferredRange = 7f;
    [Tooltip("Bu mesafeden yakınsa geri kaç")]
    public float minRange       = 4f;
    public float fireRate       = 1.2f;

    [Header("Perfect Dash (i-frame kaçınma)")]
    [Range(0f, 1f)]
    [Tooltip("Hasar alınca perfect dash tetiklenme olasılığı")]
    public float dodgeChance    = 0.45f;
    public float dodgeSpeed     = 18f;   // Daha yüksek hız
    public float dodgeDuration  = 0.28f; // Biraz daha uzun i-frame ve fırlama süresi

    [Header("Animasyon")]
    [SerializeField] private float deathAnimDuration = 0.8f;

    private enum State { Kiting, DashEvading }
    private State state = State.Kiting;

    private Animator       animator;
    private SpriteRenderer spriteRenderer;
    private Transform      playerTransform;
    private Rigidbody2D    rb;

    private float nextFireTime;
    private bool  isDead;
    private bool  isEvading; // i-frame aktif mi

    protected override void Start()
    {
        base.Start();
        deathDelay = deathAnimDuration;

        rb = GetComponent<Rigidbody2D>();

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) playerTransform = player.transform;

        animator       = GetComponentInChildren<Animator>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    private void Update()
    {
        if (isDead || isStunned || playerTransform == null) return;
        if (state == State.DashEvading) return; // coroutine yönetir

        float dist  = Vector2.Distance(transform.position, playerTransform.position);
        float speed = enemyData != null ? enemyData.moveSpeed : 7f;

        FacePlayer();

        Vector2 awayDir = ((Vector2)transform.position - (Vector2)playerTransform.position).normalized;

        if (dist < minRange)
        {
            // Çok yakın → direkt kaç
            MoveBy(awayDir * speed);
        }
        else if (dist > preferredRange * 1.3f)
        {
            // Çok uzak → biraz yaklaş
            MoveBy(-awayDir * speed * 0.5f);
        }
        else
        {
            // İdeal mesafe → strafe (yana hareket)
            Vector2 strafe = new Vector2(-awayDir.y, awayDir.x);
            MoveBy(strafe * speed * 0.75f);
        }

        if (animator != null) animator.SetBool("IsMoving", true);

        // Hareket ederken ateş et
        if (Time.time >= nextFireTime)
            StartCoroutine(FireRoutine());
    }

    // ─────────────────────────────── ATEŞ ───────────────────────────────────

    private IEnumerator FireRoutine()
    {
        nextFireTime = Time.time + fireRate;

        if (projectilePrefab == null || firePoint == null || playerTransform == null)
            yield break;

        if (animator != null) animator.SetTrigger("Attack");

        Vector2 aimDir = ((Vector2)playerTransform.position -
                          (Vector2)firePoint.position).normalized;

        GameObject projObj = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
        Projectile proj    = projObj.GetComponent<Projectile>();
        if (proj != null)
        {
            proj.owner  = gameObject;
            proj.damage = enemyData != null ? enemyData.damage : proj.damage;
            proj.Launch(aimDir);
        }
    }

    // ──────────────────────────── PERFECT DASH ──────────────────────────────

    private IEnumerator DashEvade()
    {
        isEvading = true;
        state     = State.DashEvading;

        // Oyuncudan uzak tarafa fırla
        Vector2 dodgeDir = playerTransform != null
            ? ((Vector2)transform.position - (Vector2)playerTransform.position).normalized
            : Random.insideUnitCircle.normalized;

        // Renk tonu ile "perfect dodge" görsel ipucu
        if (spriteRenderer != null) spriteRenderer.color = new Color(0.4f, 0.8f, 1f);

        float timer = 0f;
        while (timer < dodgeDuration)
        {
            if (rb != null) rb.linearVelocity = dodgeDir * dodgeSpeed;
            timer += Time.deltaTime;
            yield return null;
        }

        if (rb != null) rb.linearVelocity = Vector2.zero;
        if (spriteRenderer != null) spriteRenderer.color = Color.white;

        isEvading = false;
        state     = State.Kiting;
    }

    // ─────────────────────────── HASAR & ÖLÜM ───────────────────────────────

    public override void TakeDamage(float amount)
    {
        if (isDead)     return;
        if (isEvading)  return; // i-frame: hasar atlatıldı

        // Perfect Dash olasılığı
        if (state != State.DashEvading && Random.value < dodgeChance)
        {
            StartCoroutine(DashEvade());

            // "EVADE!" popup
            if (DamagePopupManager.Instance != null)
                DamagePopupManager.Instance.CreateText(
                    transform.position + Vector3.up * 1.2f,
                    "EVADE!", new Color(0.4f, 0.9f, 1f), 5f);
            return;
        }

        base.TakeDamage(amount);
    }

    public override void Stun(float duration)
    {
        // Dash i-frame'i iptal et (parry / parry shockwave etkisi)
        if (isEvading)
        {
            StopAllCoroutines();
            isEvading = false;
            state     = State.Kiting;
            if (rb != null) rb.linearVelocity = Vector2.zero;
            if (spriteRenderer != null) spriteRenderer.color = Color.white;
        }
        base.Stun(duration);
    }

    protected override void OnDeathAnimationStart()
    {
        isDead    = true;
        isEvading = false;
        StopAllCoroutines();

        if (animator != null) animator.SetTrigger("Die");
        if (spriteRenderer != null) spriteRenderer.color = Color.white;

        if (rb  != null) rb.linearVelocity = Vector2.zero;

        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;
    }

    // ─────────────────────────────── YARDIMCI ───────────────────────────────

    private void MoveBy(Vector2 velocity)
    {
        if (rb != null)
            rb.linearVelocity = velocity;
        else
            transform.position += (Vector3)(velocity * Time.deltaTime);
    }

    private void FacePlayer()
    {
        if (spriteRenderer == null || playerTransform == null) return;
        spriteRenderer.flipX = playerTransform.position.x < transform.position.x;
    }

    // ─────────────────────────────── GIZMOS ─────────────────────────────────

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, preferredRange);
        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(transform.position, minRange);
    }
}

using UnityEngine;
using System.Collections;

/// <summary>
/// Görünmez suikastçı düşman.
///
/// Davranış:
///   – Oyuncu menzilde değil → görünmez rastgele gezinir.
///   – Oyuncu tespit menziline girdi → görünmez takip eder.
///   – Saldırıdan 0.15 s önce görünür olur, saldırır, yeniden görünmez olur, çekilir.
///
/// Özel kural: Arenada sadece suikastçılar kaldıysa VEYA toplam
///   düşman sayısı ≤ 3 ise, tüm suikastçılar yarı saydam (0.3 alpha) görünür.
///
/// Animator parametreleri: IsMoving (bool), Die (trigger).
/// </summary>
public class EnemyAssassin : EnemyBase
{
    [Header("Assassin Settings")]
    public float detectionRange  = 8f;
    public float attackRange     = 1.4f;
    public float attackDamage    = 20f;
    public float attackCooldown  = 2.5f;
    public float retreatDuration = 1.6f;

    [Header("Görünürlük")]
    [Tooltip("Normal gezinme/takip sırasında alpha değeri (0 = tam görünmez)")]
    public float invisibleAlpha   = 0f;
    [Tooltip("Yarı görünür durumlarda alpha (sadece suikastçılar/son 3 mob)")]
    public float semiVisibleAlpha = 0.3f;
    [Tooltip("Saldırı öncesinde görünür kalınan süre (saniye)")]
    public float preAttackVisibleTime = 0.15f;

    [Header("Animasyon")]
    [SerializeField] private float deathAnimDuration = 0.5f;

    private enum State { RoamingInvisible, TrackingInvisible, PreAttack, Attacking, Retreating }
    private State state = State.RoamingInvisible;

    private Transform playerTransform;
    private SpriteRenderer spriteRenderer;
    private Animator animator;

    private Vector2 wanderTarget;
    private float   wanderTimer;
    private float   nextAttackTime;
    private bool    isDead;

    protected override void Start()
    {
        base.Start();
        deathDelay = deathAnimDuration;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) playerTransform = player.transform;

        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        animator       = GetComponentInChildren<Animator>();

        SetAlpha(invisibleAlpha);
        SetNewWanderTarget();
    }

    private void Update()
    {
        if (isDead || isStunned || playerTransform == null) return;

        float dist  = Vector2.Distance(transform.position, playerTransform.position);
        float speed = enemyData != null ? enemyData.moveSpeed : 3f;

        RefreshVisibility();

        if (animator != null)
            animator.SetBool("IsMoving", state == State.RoamingInvisible ||
                                          state == State.TrackingInvisible ||
                                          state == State.Retreating);

        switch (state)
        {
            case State.RoamingInvisible:
                DoWander(speed);
                if (dist <= detectionRange) state = State.TrackingInvisible;
                break;

            case State.TrackingInvisible:
                FaceTarget(playerTransform.position);

                // Oyuncu kaçtıysa tekrar gez
                if (dist > detectionRange * 1.2f)
                {
                    state = State.RoamingInvisible;
                    break;
                }

                // Uygun mesafeyse saldırıya hazırlan
                if (dist <= attackRange * 2.5f && Time.time >= nextAttackTime)
                {
                    state = State.PreAttack;
                    StartCoroutine(AttackSequence());
                }
                else
                {
                    // Takip et
                    transform.position = Vector2.MoveTowards(
                        transform.position, playerTransform.position, speed * Time.deltaTime);
                }
                break;

            case State.Retreating:
                // Oyuncudan uzaklaş
                Vector2 away = ((Vector2)transform.position -
                                (Vector2)playerTransform.position).normalized;
                transform.position = Vector2.MoveTowards(
                    transform.position,
                    (Vector2)transform.position + away,
                    speed * 1.2f * Time.deltaTime);
                break;

            // PreAttack / Attacking → coroutine yönetir
        }
    }

    // ─────────────────────────── SALDIRI DIZISI ─────────────────────────────

    private IEnumerator AttackSequence()
    {
        // 1. Kısa süre görünür ol (uyarı penceresi)
        SetAlpha(1f);
        yield return new WaitForSeconds(preAttackVisibleTime);

        state = State.Attacking;

        // 2. Hızlı lunge (oyuncuya fırlat)
        if (playerTransform != null)
        {
            float lungeTime = 0.12f;
            float elapsed   = 0f;
            while (elapsed < lungeTime)
            {
                transform.position = Vector2.MoveTowards(
                    transform.position, playerTransform.position, 22f * Time.deltaTime);
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        // 3. Hasar ver (yonlu parry kontrolu)
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, attackRange);
        foreach (var hit in hits)
        {
            if (!hit.CompareTag("Player")) continue;

            ParrySystem parry = hit.GetComponent<ParrySystem>();
            if (parry != null && parry.TryBlockMelee(transform.position, gameObject))
                continue; // Parry basarili — suikastci geri cekiliyor

            var playerController = hit.GetComponent<PlayerController>();
            if (playerController != null && playerController.IsInvulnerable)
            {
                hit.GetComponent<DashPerkController>()?.NotifyMeleeDodged(this);
                continue;
            }

            hit.GetComponent<IDamageable>()?.TakeDamage(attackDamage);
        }

        // 4. Görünmez ol + çekil
        SetAlpha(invisibleAlpha);
        state = State.Retreating;
        nextAttackTime = Time.time + attackCooldown;

        yield return new WaitForSeconds(retreatDuration);

        // Hâlâ hayattaysak takip moduna dön
        if (!isDead) state = State.TrackingInvisible;
    }

    // ──────────────────────────── GÖRÜNÜRLÜK ────────────────────────────────

    /// <summary>
    /// Her frame görünürlüğü günceller.
    /// Saldırı sekansı sırasında (coroutine kontrolünde) müdahale etmez.
    /// </summary>
    private void RefreshVisibility()
    {
        if (state == State.PreAttack || state == State.Attacking) return;

        float targetAlpha = ShouldBeRevealed() ? semiVisibleAlpha : invisibleAlpha;
        SetAlpha(targetAlpha);
    }

    /// <summary>
    /// Sahnede yalnızca suikastçılar kaldıysa VEYA toplam düşman sayısı ≤ 3 ise true döner.
    /// </summary>
    private bool ShouldBeRevealed()
    {
        if (RoomManager.Instance == null) return false;

        var enemies = RoomManager.Instance.activeEnemies;
        if (enemies.Count == 0)  return false;
        if (enemies.Count <= 3)  return true;

        foreach (var e in enemies)
        {
            if (e != null && e.GetComponent<EnemyAssassin>() == null)
                return false;
        }
        return true; // Hepsi suikastçı
    }

    private void SetAlpha(float alpha)
    {
        if (spriteRenderer == null) return;
        Color c = spriteRenderer.color;
        c.a = alpha;
        spriteRenderer.color = c;
    }

    // ─────────────────────────────── HAREKET ────────────────────────────────

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
        wanderTimer  = Random.Range(2f, 4f);
    }

    private void FaceTarget(Vector2 target)
    {
        if (spriteRenderer == null) return;
        spriteRenderer.flipX = target.x < transform.position.x;
    }

    // ─────────────────────────── HASAR & ÖLÜM ───────────────────────────────

    public override void TakeDamage(float amount)
    {
        if (isDead) return;

        // Vurulunca ani görünür olma (küçük bilgi sızıntısı)
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
        // Stun olunca saldırı korumasını iptal et
        if (state == State.PreAttack || state == State.Attacking)
        {
            StopAllCoroutines();
            state = State.TrackingInvisible;
        }
    }

    protected override void OnDeathAnimationStart()
    {
        isDead = true;
        StopAllCoroutines();

        // Ölünce tam görünür ol
        SetAlpha(1f);

        if (animator != null) animator.SetTrigger("Die");

        var rb  = GetComponent<Rigidbody2D>();
        if (rb  != null) rb.linearVelocity = Vector2.zero;

        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;
    }

    // ─────────────────────────────── GIZMOS ─────────────────────────────────

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.5f, 0f, 0.5f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}

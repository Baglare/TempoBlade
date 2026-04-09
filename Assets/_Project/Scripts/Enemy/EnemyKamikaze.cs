using UnityEngine;
using System.Collections;

/// <summary>
/// İntihar bombacısı düşman.
/// Oyuncuyu fark edince koşar; patlama menziline girince görsel telegraph başlar (0.8 s).
/// Oyuncu bu sürede:
///   – Perfect Parry (parry penceresi aktifken) → patlama iptal, kamikaze ölür.
///   – Dodge i-frame (IsInvulnerable) → hasar atlatılır.
/// Animator parametreleri: IsMoving (bool), Die (trigger).
/// </summary>
public class EnemyKamikaze : EnemyBase
{
    [Header("Kamikaze Settings")]
    public float detectionRange   = 10f;  // Oyuncuyu görme menzili
    public float explosionRange   = 1.4f; // Bu mesafede telegraph başlar (daha yakından patlar)
    public float explosionRadius  = 2.5f; // Patlama AoE yarıçapı
    public float explosionDamage  = 30f;
    public float rushSpeed        = 7.5f; // Biraz daha hızlı koşsun
    [Tooltip("Telegraph süresi (saniye). Oyuncunun parry/dodge yapabileceği pencere.")]
    public float telegraphDuration = 0.45f;

    [Header("Animasyon")]
    [SerializeField] private float deathAnimDuration = 0.5f;

    private enum State { Wandering, Chasing, Telegraphing }
    private State state = State.Wandering;

    private Transform playerTransform;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private Vector3 originalScale;

    // Gezinme
    private Vector2 wanderTarget;
    private float wanderTimer;

    private bool isDead;
    private GameObject indicatorObj;

    protected override void Start()
    {
        base.Start();
        deathDelay = deathAnimDuration;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) playerTransform = player.transform;

        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        animator       = GetComponentInChildren<Animator>();
        originalScale  = transform.localScale;

        SetNewWanderTarget();
    }

    private void Update()
    {
        if (isDead || isStunned || playerTransform == null) return;

        float dist = Vector2.Distance(transform.position, playerTransform.position);

        switch (state)
        {
            case State.Wandering:
                DoWander();
                if (dist <= detectionRange)
                    state = State.Chasing;
                break;

            case State.Chasing:
                // Oyuncu çok uzaklaştıysa gezer moda dön
                if (dist > detectionRange * 1.4f)
                {
                    state = State.Wandering;
                    break;
                }
                MoveTowards(playerTransform.position, rushSpeed);
                FaceTarget(playerTransform.position);
                if (animator != null) animator.SetBool("IsMoving", true);

                if (dist <= explosionRange)
                {
                    state = State.Telegraphing;
                    StartCoroutine(TelegraphAndExplode());
                }
                break;

            // Telegraphing coroutine tarafından yönetilir
        }
    }

    // ─────────────────────────────── HAREKET ───────────────────────────────

    private void DoWander()
    {
        wanderTimer -= Time.deltaTime;
        if (wanderTimer <= 0f || Vector2.Distance(transform.position, wanderTarget) < 0.5f)
            SetNewWanderTarget();

        float speed = enemyData != null ? enemyData.moveSpeed : 2f;
        MoveTowards(wanderTarget, speed);

        if (animator != null) animator.SetBool("IsMoving", true);
    }

    private void SetNewWanderTarget()
    {
        wanderTarget = (Vector2)transform.position + Random.insideUnitCircle.normalized * 4f;
        wanderTimer  = Random.Range(2f, 4f);
    }

    private void MoveTowards(Vector2 target, float speed)
    {
        transform.position = Vector2.MoveTowards(transform.position, target, speed * Time.deltaTime);
    }

    private void FaceTarget(Vector2 target)
    {
        if (spriteRenderer == null) return;
        spriteRenderer.flipX = target.x < transform.position.x;
    }

    // ─────────────────────────── TELEGRAPH & PATLAMA ────────────────────────

    private IEnumerator TelegraphAndExplode()
    {
        // Dur
        var rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;
        if (animator != null) animator.SetBool("IsMoving", false);

        // Görsel İndikatör (Tehlike Alanı) oluştur
        if (indicatorObj != null) Destroy(indicatorObj);
        indicatorObj = new GameObject("Kamikaze_AoE_Indicator");
        indicatorObj.transform.position = transform.position;
        LineRenderer aoeIndicator = indicatorObj.AddComponent<LineRenderer>();
        aoeIndicator.startWidth = 0.08f;
        aoeIndicator.endWidth = 0.08f;
        aoeIndicator.positionCount = 41;
        aoeIndicator.useWorldSpace = false;
        
        // Standart material ataması
        aoeIndicator.material = new Material(Shader.Find("Sprites/Default"));
        aoeIndicator.startColor = new Color(1f, 0f, 0f, 0f);
        aoeIndicator.endColor = new Color(1f, 0f, 0f, 0f);
        
        float angle = 0f;
        for (int i = 0; i <= 40; i++)
        {
            float x = Mathf.Sin(Mathf.Deg2Rad * angle) * explosionRadius;
            float y = Mathf.Cos(Mathf.Deg2Rad * angle) * explosionRadius;
            aoeIndicator.SetPosition(i, new Vector3(x, y, 0));
            angle += (360f / 40);
        }

        // Renk + büyüme animasyonu (sarı→kırmızı, %150 büyüme)
        float timer = 0f;
        while (timer < telegraphDuration)
        {
            float t = timer / telegraphDuration;
            if (spriteRenderer != null)
                spriteRenderer.color = Color.Lerp(Color.yellow, Color.red,
                                           Mathf.PingPong(timer * 6f, 1f));
            transform.localScale = Vector3.Lerp(originalScale, originalScale * 1.5f, t);
            
            // Indicator'ı yavasca belirginlestir
            if (aoeIndicator != null)
            {
                aoeIndicator.startColor = new Color(1f, 0.1f, 0.1f, Mathf.Lerp(0.1f, 0.6f, t));
                aoeIndicator.endColor = new Color(1f, 0.1f, 0.1f, Mathf.Lerp(0.1f, 0.6f, t));
            }

            timer += Time.deltaTime;
            yield return null;
        }

        if (indicatorObj != null) Destroy(indicatorObj);
        Explode();
    }

    private void Explode()
    {
        if (isDead) return;

        // 1. Parry kontrolü
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        ParrySystem parry = playerObj != null ? playerObj.GetComponent<ParrySystem>() : null;
        if (parry != null && parry.TryParry())
        {
            // Perfect Parry: patlama iptal, kamikaze ölür
            Die();
            return;
        }

        // 2. Dodge i-frame kontrolü
        PlayerController pc = playerObj != null ? playerObj.GetComponent<PlayerController>() : null;
        bool playerInvulnerable = pc != null && pc.IsInvulnerable;

        // 3. AoE hasar
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius);
        foreach (var hit in hits)
        {
            if (!hit.CompareTag("Player")) continue;

            if (playerInvulnerable)
            {
                hit.GetComponent<DashPerkController>()?.NotifyMeleeDodged(this);
                continue;
            }

            hit.GetComponent<IDamageable>()?.TakeDamage(explosionDamage);
        }

        Die();
    }

    // ─────────────────────────── HASAR & ÖLÜM ───────────────────────────────

    public override void TakeDamage(float amount)
    {
        if (isDead) return;
        base.TakeDamage(amount);
    }

    protected override void OnDeathAnimationStart()
    {
        isDead = true;
        StopAllCoroutines();

        if (indicatorObj != null) Destroy(indicatorObj);

        transform.localScale = originalScale; // Boyutu sıfırla

        if (animator != null) animator.SetTrigger("Die");

        var rb  = GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;

        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;
    }

    // ─────────────────────────────── GIZMOS ─────────────────────────────────

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRange);
        Gizmos.color = new Color(1f, 0.4f, 0f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}

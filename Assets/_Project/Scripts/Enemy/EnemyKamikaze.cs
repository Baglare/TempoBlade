using System.Collections;
using UnityEngine;

/// <summary>
/// Intihar bombacisi dusman.
/// Oyuncuyu fark edince kosar; patlama menziline girince telegraph baslar.
/// Perfect parry patlamayi iptal eder. Dodge i-frame ile hasar atlatilabilir.
/// </summary>
public class EnemyKamikaze : EnemyBase
{
    [System.Serializable]
    public class KamikazeTempoConfig
    {
        public TempoTierFloatValue rushSpeedMultiplier = new TempoTierFloatValue { t0 = 1f, t1 = 1.12f, t2 = 1.12f, t3 = 1.25f };
        public TempoTierFloatValue telegraphDurationMultiplier = new TempoTierFloatValue { t0 = 1f, t1 = 1f, t2 = 0.85f, t3 = 0.75f };
        public float t2SqueezeOffsetDistance = 1.1f;
        public float t3IncomingDamageMultiplier = 1.25f;
        public float t3ChainReactionRadius = 3.25f;
    }

    [Header("Kamikaze Settings")]
    public float detectionRange = 10f;
    public float explosionRange = 1.4f;
    public float explosionRadius = 2.5f;
    public float explosionDamage = 30f;
    public float rushSpeed = 7.5f;
    [Tooltip("Telegraph suresi (saniye). Oyuncunun parry/dodge yapabilecegi pencere.")]
    public float telegraphDuration = 0.45f;

    [Header("Tempo")]
    public KamikazeTempoConfig tempoConfig = new KamikazeTempoConfig();

    [Header("Animasyon")]
    [SerializeField] private float deathAnimDuration = 0.5f;

    private enum State { Wandering, Chasing, Telegraphing }
    private State state = State.Wandering;

    private Transform playerTransform;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private Vector3 originalScale;

    private Vector2 wanderTarget;
    private float wanderTimer;
    private bool isDead;
    private bool chainPrimed;
    private GameObject indicatorObj;
    private bool spawnedUnstableCore;

    protected override void Start()
    {
        base.Start();
        deathDelay = ResolveDeathAnimationDelay(deathAnimDuration);

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            playerTransform = player.transform;

        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        animator = GetComponentInChildren<Animator>();
        originalScale = transform.localScale;

        SetNewWanderTarget();
    }

    private void Update()
    {
        if (isDead || isStunned || playerTransform == null)
            return;

        float dist = Vector2.Distance(transform.position, playerTransform.position);
        switch (state)
        {
            case State.Wandering:
                DoWander();
                if (dist <= detectionRange)
                    state = State.Chasing;
                break;

            case State.Chasing:
                if (dist > detectionRange * 1.4f)
                {
                    state = State.Wandering;
                    break;
                }

                Vector2 chaseTarget = GetChaseTargetPosition();
                MoveTowards(chaseTarget, GetEffectiveMoveSpeed(rushSpeed * tempoConfig.rushSpeedMultiplier.Evaluate(CurrentTempoTier)));
                FaceTarget(chaseTarget);
                if (animator != null)
                    animator.SetBool("IsMoving", true);

                if (dist <= explosionRange)
                {
                    EmitCombatAction(EnemyCombatActionType.Skill);
                    state = State.Telegraphing;
                    StartCoroutine(TelegraphAndExplode());
                }
                break;
        }
    }

    public override void TakeDamage(float amount)
    {
        if (isDead)
            return;

        if (CurrentTempoTier == TempoManager.TempoTier.T3)
            amount *= tempoConfig.t3IncomingDamageMultiplier;

        base.TakeDamage(amount);
    }

    protected override void OnDeathAnimationStart()
    {
        isDead = true;
        StopAllCoroutines();

        if (indicatorObj != null)
            Destroy(indicatorObj);

        transform.localScale = originalScale;

        if (animator != null)
            animator.SetTrigger("Die");

        TrySpawnUnstableCore();

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
            col.enabled = false;
    }

    private void DoWander()
    {
        wanderTimer -= Time.deltaTime;
        if (wanderTimer <= 0f || Vector2.Distance(transform.position, wanderTarget) < 0.5f)
            SetNewWanderTarget();

        float speed = GetEffectiveMoveSpeedFromData(2f);
        MoveTowards(wanderTarget, speed);

        if (animator != null)
            animator.SetBool("IsMoving", true);
    }

    private void SetNewWanderTarget()
    {
        wanderTarget = (Vector2)transform.position + Random.insideUnitCircle.normalized * 4f;
        wanderTimer = Random.Range(2f, 4f);
    }

    private void MoveTowards(Vector2 target, float speed)
    {
        transform.position = Vector2.MoveTowards(transform.position, target, speed * Time.deltaTime);
    }

    private void FaceTarget(Vector2 target)
    {
        if (spriteRenderer == null)
            return;

        UpdateSpriteFacing(spriteRenderer, target.x);
    }

    private Vector2 GetChaseTargetPosition()
    {
        Vector2 playerPosition = playerTransform.position;
        if (CurrentTempoTier < TempoManager.TempoTier.T2)
            return playerPosition;

        EnemyBase[] allEnemies = FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);
        for (int i = 0; i < allEnemies.Length; i++)
        {
            EnemyBase enemy = allEnemies[i];
            if (enemy == null || enemy == this || enemy.CurrentHealth <= 0f || !enemy.gameObject.activeInHierarchy)
                continue;

            if (Vector2.Distance(transform.position, enemy.transform.position) > 2.5f)
                continue;

            Vector2 toPlayer = (playerPosition - (Vector2)transform.position).normalized;
            if (toPlayer.sqrMagnitude <= 0.001f)
                return playerPosition;

            Vector2 perpendicular = new Vector2(-toPlayer.y, toPlayer.x);
            float side = Mathf.Sign(Vector2.Dot(perpendicular, (Vector2)enemy.transform.position - playerPosition));
            if (Mathf.Approximately(side, 0f))
                side = Random.value < 0.5f ? -1f : 1f;

            return playerPosition + perpendicular * side * tempoConfig.t2SqueezeOffsetDistance;
        }

        return playerPosition;
    }

    private IEnumerator TelegraphAndExplode()
    {
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.linearVelocity = Vector2.zero;
        if (animator != null)
            animator.SetBool("IsMoving", false);

        if (indicatorObj != null)
            Destroy(indicatorObj);

        indicatorObj = new GameObject("Kamikaze_AoE_Indicator");
        indicatorObj.transform.position = transform.position;
        LineRenderer aoeIndicator = indicatorObj.AddComponent<LineRenderer>();
        aoeIndicator.startWidth = 0.08f;
        aoeIndicator.endWidth = 0.08f;
        aoeIndicator.positionCount = 41;
        aoeIndicator.useWorldSpace = false;
        aoeIndicator.material = new Material(Shader.Find("Sprites/Default"));
        aoeIndicator.startColor = new Color(1f, 0f, 0f, 0f);
        aoeIndicator.endColor = new Color(1f, 0f, 0f, 0f);

        float angle = 0f;
        for (int i = 0; i <= 40; i++)
        {
            float x = Mathf.Sin(Mathf.Deg2Rad * angle) * explosionRadius;
            float y = Mathf.Cos(Mathf.Deg2Rad * angle) * explosionRadius;
            aoeIndicator.SetPosition(i, new Vector3(x, y, 0f));
            angle += 360f / 40f;
        }

        float actualTelegraphDuration = telegraphDuration * tempoConfig.telegraphDurationMultiplier.Evaluate(CurrentTempoTier);
        actualTelegraphDuration = Mathf.Max(0.08f, actualTelegraphDuration);

        float timer = 0f;
        while (timer < actualTelegraphDuration)
        {
            float t = timer / actualTelegraphDuration;
            if (spriteRenderer != null)
                spriteRenderer.color = Color.Lerp(Color.yellow, Color.red, Mathf.PingPong(timer * 6f, 1f));
            transform.localScale = Vector3.Lerp(originalScale, originalScale * 1.5f, t);

            if (aoeIndicator != null)
            {
                aoeIndicator.startColor = new Color(1f, 0.1f, 0.1f, Mathf.Lerp(0.1f, 0.6f, t));
                aoeIndicator.endColor = new Color(1f, 0.1f, 0.1f, Mathf.Lerp(0.1f, 0.6f, t));
            }

            timer += Time.deltaTime;
            yield return null;
        }

        if (indicatorObj != null)
            Destroy(indicatorObj);

        Explode();
    }

    private void Explode()
    {
        if (isDead)
            return;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        ParrySystem parry = playerObj != null ? playerObj.GetComponent<ParrySystem>() : null;
        if (parry != null && parry.TryParry(gameObject))
        {
            TriggerNearbyUnstableCores();
            Die();
            return;
        }

        PlayerController pc = playerObj != null ? playerObj.GetComponent<PlayerController>() : null;
        bool playerInvulnerable = pc != null && pc.IsInvulnerable;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius);
        foreach (Collider2D hit in hits)
        {
            if (!hit.CompareTag("Player"))
                continue;

            if (playerInvulnerable)
            {
                hit.GetComponent<DashPerkController>()?.NotifyMeleeDodged(this);
                continue;
            }

            hit.GetComponent<IDamageable>()?.TakeDamage(GetEffectiveDamage(explosionDamage));
        }

        TriggerNearbyUnstableCores();
        Die();
    }

    private void TriggerNearbyUnstableCores()
    {
        if (CurrentTempoTier != TempoManager.TempoTier.T3)
            return;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, tempoConfig.t3ChainReactionRadius);
        foreach (Collider2D hit in hits)
        {
            EnemyKamikaze other = hit.GetComponent<EnemyKamikaze>();
            if (other == null || other == this)
                continue;

            other.PrimeFromChain();
        }
    }

    private void PrimeFromChain()
    {
        if (isDead || state == State.Telegraphing || chainPrimed)
            return;

        chainPrimed = true;
        state = State.Telegraphing;
        StartCoroutine(TelegraphAndExplode());
    }

    private void TrySpawnUnstableCore()
    {
        if (spawnedUnstableCore || !HasEliteMechanic(EliteMechanicType.KamikazeUnstableCore) || ActiveEliteProfile == null)
            return;

        spawnedUnstableCore = true;
        GameObject coreObject = new GameObject("UnstableCore");
        coreObject.transform.position = transform.position;
        UnstableCoreObject core = coreObject.AddComponent<UnstableCoreObject>();
        core.Configure(ActiveEliteProfile.kamikazeUnstableCore, GetEffectiveDamage(explosionDamage));
    }

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

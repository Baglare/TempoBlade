using UnityEngine;
using System.Collections;

public class EnemyAssassin : EnemyBase
{
    [System.Serializable]
    public class AssassinTempoConfig
    {
        public TempoTierFloatValue preAttackVisibleMultiplier = new TempoTierFloatValue { t0 = 1f, t1 = 0.9f, t2 = 0.78f, t3 = 0.78f };
        public TempoTierFloatValue attackCooldownMultiplier = new TempoTierFloatValue { t0 = 1f, t1 = 1f, t2 = 0.9f, t3 = 0.9f };
        public TempoTierFloatValue orbitDistance = new TempoTierFloatValue { t0 = 0f, t1 = 0.8f, t2 = 1.25f, t3 = 1.45f };
        public TempoTierFloatValue orbitRefreshInterval = new TempoTierFloatValue { t0 = 99f, t1 = 0.7f, t2 = 0.4f, t3 = 0.3f };
        public float t3DoubleRepositionChance = 0.35f;
        public float t3PunishWindowDuration = 1.1f;
        public float t3PunishSpeedMultiplier = 0.55f;
        public float shortRepositionDistance = 1f;
        public float shortRepositionDuration = 0.08f;
    }

    [Header("Assassin Settings")]
    public float detectionRange = 8f;
    public float attackRange = 1.4f;
    public float attackDamage = 20f;
    public float attackCooldown = 2.5f;
    public float retreatDuration = 1.6f;

    [Header("Visibility")]
    public float invisibleAlpha = 0f;
    public float semiVisibleAlpha = 0.3f;
    public float preAttackVisibleTime = 0.15f;

    [Header("Tempo")]
    public AssassinTempoConfig tempoConfig = new AssassinTempoConfig();

    [Header("Animation")]
    [SerializeField] private float deathAnimDuration = 0.5f;

    private enum State { RoamingInvisible, TrackingInvisible, PreAttack, Attacking, Retreating }
    private State state = State.RoamingInvisible;

    private Transform playerTransform;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private Vector2 wanderTarget;
    private float wanderTimer;
    private float nextAttackTime;
    private bool isDead;
    private float punishWindowEndTime;
    private int orbitSide = 1;
    private float nextOrbitRefreshTime;
    private float nextEliteShadowEchoTime;

    protected override void Start()
    {
        base.Start();
        deathDelay = deathAnimDuration;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            playerTransform = player.transform;

        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        animator = GetComponentInChildren<Animator>();

        SetAlpha(invisibleAlpha);
        SetNewWanderTarget();
    }

    private void Update()
    {
        if (isDead || isStunned || playerTransform == null)
            return;

        float dist = Vector2.Distance(transform.position, playerTransform.position);
        float speed = GetEffectiveMoveSpeedFromData(3f);
        if (IsPunishWindowActive())
            speed *= tempoConfig.t3PunishSpeedMultiplier;

        RefreshVisibility();

        if (animator != null)
        {
            bool moving = state == State.RoamingInvisible || state == State.TrackingInvisible || state == State.Retreating;
            animator.SetBool("IsMoving", moving);
        }

        switch (state)
        {
            case State.RoamingInvisible:
                DoWander(speed);
                if (dist <= detectionRange)
                    state = State.TrackingInvisible;
                break;

            case State.TrackingInvisible:
                FaceTarget(playerTransform.position);
                if (dist > detectionRange * 1.2f)
                {
                    state = State.RoamingInvisible;
                    break;
                }

                if (dist <= attackRange * 2.5f && Time.time >= nextAttackTime)
                {
                    state = State.PreAttack;
                    StartCoroutine(AttackSequence());
                }
                else
                {
                    Vector2 trackingTarget = GetTrackingTarget();
                    transform.position = Vector2.MoveTowards(transform.position, trackingTarget, speed * Time.deltaTime);
                }
                break;

            case State.Retreating:
                Vector2 away = ((Vector2)transform.position - (Vector2)playerTransform.position).normalized;
                transform.position = Vector2.MoveTowards(
                    transform.position,
                    (Vector2)transform.position + away,
                    speed * 1.2f * Time.deltaTime);
                break;
        }
    }

    private IEnumerator AttackSequence()
    {
        if (HasEliteMechanic(EliteMechanicType.AssassinShadowEcho) &&
            ActiveEliteProfile != null &&
            Time.time >= nextEliteShadowEchoTime &&
            Random.value < 0.55f)
        {
            yield return EliteShadowEchoRoutine();
            yield break;
        }

        float attackSpeedMultiplier = Mathf.Max(0.01f, GetSupportAttackSpeedMultiplier());
        float visibleTime = preAttackVisibleTime * tempoConfig.preAttackVisibleMultiplier.Evaluate(CurrentTempoTier);
        EmitCombatAction(EnemyCombatActionType.Attack);

        SetAlpha(1f);
        yield return new WaitForSeconds(visibleTime / attackSpeedMultiplier);

        if (CurrentTempoTier == TempoManager.TempoTier.T3 && Random.value < tempoConfig.t3DoubleRepositionChance)
        {
            yield return ShortReposition(orbitSide);
            yield return ShortReposition(-orbitSide);
        }

        state = State.Attacking;
        if (playerTransform != null)
        {
            float lungeTime = 0.12f;
            float elapsed = 0f;
            while (elapsed < lungeTime)
            {
                transform.position = Vector2.MoveTowards(transform.position, playerTransform.position, 22f * Time.deltaTime);
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        bool hitPlayer = false;
        bool parried = false;
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, attackRange);
        foreach (var hit in hits)
        {
            if (!hit.CompareTag("Player"))
                continue;

            ParrySystem parry = hit.GetComponent<ParrySystem>();
            if (parry != null && parry.TryBlockMelee(transform.position, gameObject))
            {
                parried = true;
                continue;
            }

            PlayerController playerController = hit.GetComponent<PlayerController>();
            if (playerController != null && playerController.IsInvulnerable)
            {
                hit.GetComponent<DashPerkController>()?.NotifyMeleeDodged(this);
                continue;
            }

            hit.GetComponent<IDamageable>()?.TakeDamage(GetEffectiveDamage(attackDamage));
            hitPlayer = true;
        }

        SetAlpha(invisibleAlpha);
        state = State.Retreating;
        nextAttackTime = Time.time + GetEffectiveCooldownDuration(attackCooldown * tempoConfig.attackCooldownMultiplier.Evaluate(CurrentTempoTier)) / attackSpeedMultiplier;

        if (CurrentTempoTier == TempoManager.TempoTier.T3 && (parried || !hitPlayer))
            EnterPunishWindow();

        yield return new WaitForSeconds(retreatDuration);
        if (!isDead)
            state = State.TrackingInvisible;
    }

    private IEnumerator EliteShadowEchoRoutine()
    {
        EliteAssassinShadowEchoSettings settings = ActiveEliteProfile != null ? ActiveEliteProfile.assassinShadowEcho : null;
        if (settings == null || playerTransform == null)
            yield break;

        EmitCombatAction(EnemyCombatActionType.Dash);
        SetAlpha(0f);
        state = State.PreAttack;

        Vector2 playerPos = playerTransform.position;
        Vector2[] offsets =
        {
            new Vector2(-1f, -1f),
            new Vector2(-1f, 1f),
            new Vector2(1f, -1f),
            new Vector2(1f, 1f)
        };

        int firstIndex = Random.Range(0, offsets.Length);
        int secondIndex = (firstIndex + Random.Range(1, offsets.Length)) % offsets.Length;
        bool realFirst = Random.value < 0.5f;
        Vector2 realPos = playerPos + offsets[firstIndex].normalized * settings.offsetDistance;
        Vector2 echoPos = playerPos + offsets[secondIndex].normalized * settings.offsetDistance;
        if (!realFirst)
        {
            Vector2 temp = realPos;
            realPos = echoPos;
            echoPos = temp;
        }

        yield return EntryDashTo(playerPos, settings);
        yield return PerformEliteShadowStrike(realFirst ? realPos : echoPos, realFirst, settings);
        yield return new WaitForSeconds(settings.halfBeatDelay);
        yield return PerformEliteShadowStrike(realFirst ? echoPos : realPos, !realFirst, settings);

        SetAlpha(invisibleAlpha);
        state = State.Retreating;
        nextAttackTime = Time.time + GetEffectiveCooldownDuration(attackCooldown * tempoConfig.attackCooldownMultiplier.Evaluate(CurrentTempoTier));
        nextEliteShadowEchoTime = Time.time + GetEffectiveCooldownDuration(attackCooldown * 1.65f);
        yield return new WaitForSeconds(retreatDuration);
        if (!isDead)
            state = State.TrackingInvisible;
    }

    private IEnumerator PerformEliteShadowStrike(Vector2 strikePosition, bool realStrike, EliteAssassinShadowEchoSettings settings)
    {
        EmitCombatAction(EnemyCombatActionType.Attack);
        Vector2 strikeTarget = playerTransform != null ? (Vector2)playerTransform.position : strikePosition;
        if (realStrike)
        {
            transform.position = strikePosition;
            SetAlpha(0.85f);
            if (spriteRenderer != null)
                spriteRenderer.color = settings.realBodyCueColor;
            yield return LungeTo(strikeTarget, settings.entryDashDuration);
        }
        else
        {
            SpawnEchoBody(strikePosition, settings);
            SupportPulseVisualUtility.SpawnPulse(strikeTarget, 0.08f, settings.strikeRadius, 0.1f, settings.echoColor);
            yield return new WaitForSeconds(settings.entryDashDuration * 0.8f);
        }

        yield return new WaitForSeconds(0.05f);

        Collider2D[] hits = Physics2D.OverlapCircleAll(strikeTarget, settings.strikeRadius);
        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];
            if (!hit.CompareTag("Player"))
                continue;

            ParrySystem parry = hit.GetComponent<ParrySystem>();
            if (parry != null && parry.TryBlockMelee(strikeTarget, gameObject))
            {
                if (realStrike)
                    Stun(settings.heavyParryStun);
                continue;
            }

            PlayerController controller = hit.GetComponent<PlayerController>();
            if (controller != null && controller.IsInvulnerable)
            {
                hit.GetComponent<DashPerkController>()?.NotifyMeleeDodged(this);
                continue;
            }

            hit.GetComponent<IDamageable>()?.TakeDamage(GetEffectiveDamage(attackDamage) * settings.strikeDamageMultiplier);
        }

        if (realStrike && spriteRenderer != null)
            spriteRenderer.color = Color.white;
    }

    private IEnumerator EntryDashTo(Vector2 playerPos, EliteAssassinShadowEchoSettings settings)
    {
        Vector2 toPlayer = (playerPos - (Vector2)transform.position).normalized;
        if (toPlayer.sqrMagnitude <= 0.001f)
            yield break;

        Vector2 start = transform.position;
        Vector2 end = start + toPlayer * settings.entryDashDistance;
        float elapsed = 0f;
        while (elapsed < settings.entryDashDuration)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector2.Lerp(start, end, elapsed / settings.entryDashDuration);
            yield return null;
        }
    }

    private IEnumerator LungeTo(Vector2 target, float duration)
    {
        Vector2 start = transform.position;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector2.Lerp(start, target, elapsed / Mathf.Max(0.01f, duration));
            yield return null;
        }
    }

    private void SpawnEchoBody(Vector2 position, EliteAssassinShadowEchoSettings settings)
    {
        if (spriteRenderer == null || spriteRenderer.sprite == null)
        {
            SupportPulseVisualUtility.SpawnPulse(position, 0.12f, 0.6f, 0.15f, settings.echoColor);
            return;
        }

        GameObject echoObject = new GameObject("AssassinShadowEcho");
        echoObject.transform.position = position;
        SpriteRenderer ghost = echoObject.AddComponent<SpriteRenderer>();
        ghost.sprite = spriteRenderer.sprite;
        ghost.flipX = spriteRenderer.flipX;
        ghost.sortingLayerID = spriteRenderer.sortingLayerID;
        ghost.sortingOrder = spriteRenderer.sortingOrder - 1;
        ghost.color = new Color(settings.echoColor.r, settings.echoColor.g, settings.echoColor.b, Mathf.Max(0.45f, settings.echoColor.a));
        echoObject.transform.localScale = transform.localScale;
        Destroy(echoObject, Mathf.Max(0.18f, settings.halfBeatDelay + 0.12f));
    }

    private IEnumerator ShortReposition(int sideSign)
    {
        if (playerTransform == null)
            yield break;

        Vector2 toPlayer = ((Vector2)playerTransform.position - (Vector2)transform.position).normalized;
        Vector2 lateral = new Vector2(-toPlayer.y, toPlayer.x) * Mathf.Sign(sideSign);
        Vector2 start = transform.position;
        Vector2 end = start + lateral * tempoConfig.shortRepositionDistance;
        float elapsed = 0f;
        while (elapsed < tempoConfig.shortRepositionDuration)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector2.Lerp(start, end, elapsed / tempoConfig.shortRepositionDuration);
            yield return null;
        }
    }

    private void RefreshVisibility()
    {
        if (state == State.PreAttack || state == State.Attacking)
            return;

        if (IsPunishWindowActive())
        {
            SetAlpha(1f);
            return;
        }

        float targetAlpha = ShouldBeRevealed() ? semiVisibleAlpha : invisibleAlpha;
        SetAlpha(targetAlpha);
    }

    private bool ShouldBeRevealed()
    {
        if (RoomManager.Instance == null)
            return false;

        var enemies = RoomManager.Instance.activeEnemies;
        if (enemies.Count == 0)
            return false;
        if (enemies.Count <= 3)
            return true;

        foreach (var enemy in enemies)
        {
            if (enemy != null && enemy.GetComponent<EnemyAssassin>() == null)
                return false;
        }

        return true;
    }

    private void SetAlpha(float alpha)
    {
        if (spriteRenderer == null)
            return;

        Color c = spriteRenderer.color;
        c.a = alpha;
        spriteRenderer.color = c;
    }

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
        wanderTimer = Random.Range(2f, 4f);
    }

    private void FaceTarget(Vector2 target)
    {
        if (spriteRenderer == null)
            return;

        UpdateSpriteFacing(spriteRenderer, target.x);
    }

    private Vector2 GetTrackingTarget()
    {
        if (CurrentTempoTier == TempoManager.TempoTier.T0 || playerTransform == null)
            return playerTransform.position;

        if (Time.time >= nextOrbitRefreshTime)
        {
            nextOrbitRefreshTime = Time.time + tempoConfig.orbitRefreshInterval.Evaluate(CurrentTempoTier);
            orbitSide = Random.value < 0.5f ? -1 : 1;
        }

        Vector2 toPlayer = ((Vector2)playerTransform.position - (Vector2)transform.position).normalized;
        Vector2 lateral = new Vector2(-toPlayer.y, toPlayer.x) * orbitSide;
        return (Vector2)playerTransform.position + lateral * tempoConfig.orbitDistance.Evaluate(CurrentTempoTier);
    }

    private void EnterPunishWindow()
    {
        punishWindowEndTime = Time.time + tempoConfig.t3PunishWindowDuration;
    }

    private bool IsPunishWindowActive()
    {
        return Time.time < punishWindowEndTime;
    }

    public override void TakeDamage(float amount)
    {
        if (isDead)
            return;

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
        if (state == State.PreAttack || state == State.Attacking)
        {
            StopAllCoroutines();
            state = State.TrackingInvisible;
            if (CurrentTempoTier == TempoManager.TempoTier.T3)
                EnterPunishWindow();
        }
    }

    protected override void OnDeathAnimationStart()
    {
        isDead = true;
        StopAllCoroutines();
        SetAlpha(1f);

        if (animator != null)
            animator.SetTrigger("Die");

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
            col.enabled = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.5f, 0f, 0.5f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}

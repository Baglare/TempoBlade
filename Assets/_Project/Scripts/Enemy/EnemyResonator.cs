using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyResonator : EnemyBase
{
    [System.Serializable]
    public class ResonatorTempoConfig
    {
        public TempoTierFloatValue rallyCooldownMultiplier = new TempoTierFloatValue { t0 = 1f, t1 = 0.9f, t2 = 0.9f, t3 = 0.8f };
        public TempoTierFloatValue anchorRefreshMultiplier = new TempoTierFloatValue { t0 = 1f, t1 = 1f, t2 = 0.82f, t3 = 0.72f };
        public float t2TempoStaticLeadTime = 0.4f;
        public float t2RallyRadiusMultiplier = 1.1f;
        public float t2RallyDurationMultiplier = 1.1f;
        public float t3RallyMoveMultiplier = 1.12f;
        public float t3RallyAttackMultiplier = 1.08f;
        public float t3TempoStaticRadiusMultiplier = 1.12f;
        public float t3TempoStaticGainMultiplier = 0.88f;
        public float t3TempoStaticDecayMultiplier = 1.12f;
        public float t3IncomingStunMultiplier = 1.35f;
        public float t3InterruptCooldownPenalty = 0.8f;
    }

    [Header("Positioning")]
    public float supportSearchRadius = 10f;
    public float desiredSupportDistance = 2.5f;
    public float retreatDistance = 3f;
    public float repositionMoveSpeed = 2.8f;
    public float anchorRefreshInterval = 0.35f;

    [Header("Rally Pulse")]
    public float rallyPulseRadius = 4.5f;
    public float rallyPulseCooldown = 6f;
    public float rallyPulseWindup = 0.45f;
    public float rallyBuffDuration = 4f;
    public float rallyMoveSpeedMultiplier = 1.22f;
    public float rallyAttackSpeedMultiplier = 1.18f;
    public bool rallyIgnoreNextLightStagger = true;
    public Color rallyPulseColor = new Color(0.2f, 1f, 1f, 0.95f);

    [Header("Tempo Static")]
    public float tempoStaticCooldown = 7.5f;
    public float tempoStaticWindup = 0.35f;
    public float tempoStaticDuration = 4f;
    public float tempoStaticRadius = 2.6f;
    public float tempoStaticGainMultiplier = 0.72f;
    public float tempoStaticDecayMultiplier = 1.35f;
    public Color tempoStaticColor = new Color(0.3f, 0.95f, 1f, 0.85f);

    [Header("Sound Burst")]
    public float soundBurstRange = 1.8f;
    public float soundBurstCooldown = 3f;
    public float soundBurstDamage = 6f;
    public float soundBurstKnockback = 9f;
    public float soundBurstStagger = 0.22f;
    public Color soundBurstColor = new Color(1f, 0.55f, 0.2f, 0.9f);

    [Header("Tempo")]
    public ResonatorTempoConfig tempoConfig = new ResonatorTempoConfig();

    [Header("Animation")]
    [SerializeField] private float deathAnimDuration = 0.6f;

    private readonly List<EnemyBase> nearbyAllies = new List<EnemyBase>();

    private Transform playerTransform;
    private PlayerController playerController;
    private PlayerCombat playerCombat;
    private Rigidbody2D playerRb;
    private Animator animator;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private EnemyOverheadMeter overheadMeter;
    private float crescendoMeter;
    private bool isCrescendoChanneling;
    private bool crescendoPresentationActive;
    private bool crescendoReadyAnnounced;

    private float nextAnchorRefreshTime;
    private float nextRallyPulseTime;
    private float nextTempoStaticTime;
    private float nextSoundBurstTime;
    private Vector2 currentAnchor;
    private bool isDead;
    private bool isCasting;

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
            playerRb = player.GetComponent<Rigidbody2D>();
        }

        animator = GetComponentInChildren<Animator>();
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        overheadMeter = GetComponent<EnemyOverheadMeter>();
        if (overheadMeter == null)
            overheadMeter = gameObject.AddComponent<EnemyOverheadMeter>();
        RefreshEliteMeterVisibility();

        currentAnchor = transform.position;
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        EnemyBase.OnEnemyCombatAction += HandleEnemyCombatAction;
    }

    protected override void OnDisable()
    {
        EnemyBase.OnEnemyCombatAction -= HandleEnemyCombatAction;
        base.OnDisable();
    }

    private void Update()
    {
        if (isDead || isStunned || playerTransform == null)
            return;

        RefreshCrescendoPresentationIfNeeded();

        if (HasEliteMechanic(EliteMechanicType.ResonatorCrescendo) && !isCasting && !isCrescendoChanneling && crescendoMeter >= 1f)
        {
            StartCoroutine(CrescendoRoutine());
            return;
        }

        float anchorRefresh = anchorRefreshInterval * tempoConfig.anchorRefreshMultiplier.Evaluate(CurrentTempoTier);
        if (Time.time >= nextAnchorRefreshTime)
        {
            currentAnchor = EnemySupportUtility.FindBestRallyAnchor(transform, supportSearchRadius, GetCurrentRallyRadius(), this, out _);
            nextAnchorRefreshTime = Time.time + Mathf.Max(0.08f, anchorRefresh);
        }

        if (!isCasting && Time.time >= nextSoundBurstTime && Vector2.Distance(transform.position, playerTransform.position) <= soundBurstRange)
        {
            StartCoroutine(SoundBurstRoutine());
            return;
        }

        if (!isCasting && Time.time >= nextRallyPulseTime)
        {
            StartCoroutine(RallyPulseRoutine());
            return;
        }

        if (!isCasting && Time.time >= nextTempoStaticTime)
        {
            StartCoroutine(TempoStaticRoutine());
            return;
        }

        UpdateMovement();
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
        isCasting = false;

        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        if (spriteRenderer != null)
            spriteRenderer.color = Color.white;

        float finalDuration = duration;
        if (CurrentTempoTier == TempoManager.TempoTier.T3)
        {
            finalDuration *= tempoConfig.t3IncomingStunMultiplier;
            nextRallyPulseTime = Mathf.Max(nextRallyPulseTime, Time.time + tempoConfig.t3InterruptCooldownPenalty);
            nextTempoStaticTime = Mathf.Max(nextTempoStaticTime, Time.time + tempoConfig.t3InterruptCooldownPenalty);
        }

        if (HasEliteMechanic(EliteMechanicType.ResonatorCrescendo) && isCrescendoChanneling)
        {
            crescendoMeter = Mathf.Clamp01(crescendoMeter - ActiveEliteProfile.resonatorCrescendo.interruptLoss);
            UpdateOverheadMeter();
            isCrescendoChanneling = false;
            StopAllCoroutines();
            isCasting = false;
        }

        base.Stun(finalDuration);
    }

    protected override void OnDeathAnimationStart()
    {
        isDead = true;
        StopAllCoroutines();

        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
            col.enabled = false;

        if (animator != null)
            animator.SetTrigger("Die");
    }

    private void UpdateMovement()
    {
        Vector2 targetPosition = currentAnchor;
        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);
        if (distanceToPlayer <= retreatDistance)
        {
            Vector2 away = ((Vector2)transform.position - (Vector2)playerTransform.position).normalized;
            targetPosition = (Vector2)transform.position + away * desiredSupportDistance;
        }

        if (!EnemyLineOfSightUtility.IsPointNavigable(targetPosition, 0.3f, transform))
        {
            RangedTacticalDecision decision = RangedTacticalMovementUtility.EvaluatePosition(
                transform,
                playerTransform,
                transform.position,
                desiredSupportDistance + 1.2f,
                supportSearchRadius,
                2.5f,
                8,
                transform);
            if (decision.foundBetterPosition)
                targetPosition = decision.moveTarget;
        }

        Vector2 toTarget = targetPosition - (Vector2)transform.position;
        bool isMoving = toTarget.magnitude > 0.2f;
        if (rb != null)
        {
            rb.linearVelocity = isMoving
                ? toTarget.normalized * GetEffectiveMoveSpeed(repositionMoveSpeed)
                : Vector2.zero;
        }

        if (spriteRenderer != null)
            spriteRenderer.flipX = playerTransform.position.x < transform.position.x;

        if (animator != null)
            animator.SetBool("IsMoving", isMoving);
    }

    private IEnumerator RallyPulseRoutine()
    {
        isCasting = true;
        if (rb != null)
            rb.linearVelocity = Vector2.zero;
        EmitCombatAction(EnemyCombatActionType.Skill);

        if (spriteRenderer != null)
            spriteRenderer.color = rallyPulseColor;

        yield return new WaitForSeconds(GetEffectiveCooldownDuration(rallyPulseWindup));

        float currentRadius = GetCurrentRallyRadius();
        float currentDuration = GetCurrentRallyDuration();
        float currentMoveBuff = rallyMoveSpeedMultiplier;
        float currentAttackBuff = rallyAttackSpeedMultiplier;
        if (CurrentTempoTier == TempoManager.TempoTier.T3)
        {
            currentMoveBuff *= tempoConfig.t3RallyMoveMultiplier;
            currentAttackBuff *= tempoConfig.t3RallyAttackMultiplier;
        }

        EnemySupportUtility.GatherNearbyAllies(transform, currentRadius, nearbyAllies, this);
        for (int i = 0; i < nearbyAllies.Count; i++)
        {
            EnemyBase ally = nearbyAllies[i];
            ally?.GetSupportBuffReceiver()?.ApplyRallyBuff(
                currentDuration,
                currentMoveBuff,
                currentAttackBuff,
                rallyIgnoreNextLightStagger);
        }

        SupportPulseVisualUtility.SpawnPulse(transform.position, 0.25f, currentRadius, 0.32f, rallyPulseColor);
        if (DamagePopupManager.Instance != null)
            DamagePopupManager.Instance.CreateText(transform.position + Vector3.up * 1.8f, "RALLY!", rallyPulseColor, 6f);

        if (spriteRenderer != null)
            spriteRenderer.color = Color.white;

        float cooldown = rallyPulseCooldown * tempoConfig.rallyCooldownMultiplier.Evaluate(CurrentTempoTier);
        nextRallyPulseTime = Time.time + Mathf.Max(0.2f, GetEffectiveCooldownDuration(cooldown));
        isCasting = false;
    }

    private IEnumerator TempoStaticRoutine()
    {
        isCasting = true;
        if (rb != null)
            rb.linearVelocity = Vector2.zero;
        EmitCombatAction(EnemyCombatActionType.Cast);

        if (spriteRenderer != null)
            spriteRenderer.color = tempoStaticColor;

        yield return new WaitForSeconds(GetEffectiveCooldownDuration(tempoStaticWindup));

        Vector3 zonePosition = playerTransform.position;
        if (CurrentTempoTier >= TempoManager.TempoTier.T2 && playerRb != null)
            zonePosition += (Vector3)(playerRb.linearVelocity * tempoConfig.t2TempoStaticLeadTime);

        GameObject zoneObject = new GameObject("TempoStaticZone");
        zoneObject.transform.position = zonePosition;
        TempoStaticZone zone = zoneObject.AddComponent<TempoStaticZone>();
        zone.Configure(
            tempoStaticDuration,
            GetCurrentTempoStaticRadius(),
            GetCurrentTempoStaticGainMultiplier(),
            GetCurrentTempoStaticDecayMultiplier());

        SupportPulseVisualUtility.SpawnPulse(zonePosition, 0.25f, GetCurrentTempoStaticRadius(), 0.25f, tempoStaticColor);
        if (DamagePopupManager.Instance != null)
            DamagePopupManager.Instance.CreateText(zonePosition + Vector3.up, "STATIC", tempoStaticColor, 5.5f);

        if (spriteRenderer != null)
            spriteRenderer.color = Color.white;

        nextTempoStaticTime = Time.time + GetEffectiveCooldownDuration(tempoStaticCooldown);
        isCasting = false;
    }

    private IEnumerator SoundBurstRoutine()
    {
        isCasting = true;
        if (rb != null)
            rb.linearVelocity = Vector2.zero;
        EmitCombatAction(EnemyCombatActionType.Skill);

        yield return new WaitForSeconds(0.12f);

        Vector2 toPlayer = playerTransform.position - transform.position;
        if (toPlayer.magnitude <= soundBurstRange)
        {
            if (playerCombat != null)
                playerCombat.TakeDamage(GetEffectiveDamage(soundBurstDamage));

            if (playerController != null)
                playerController.ApplyExternalStagger(soundBurstStagger, toPlayer.normalized * soundBurstKnockback);
        }

        SupportPulseVisualUtility.SpawnPulse(transform.position, 0.2f, soundBurstRange, 0.18f, soundBurstColor);
        if (DamagePopupManager.Instance != null)
            DamagePopupManager.Instance.CreateText(transform.position + Vector3.up * 1.4f, "BURST!", soundBurstColor, 5.5f);

        nextSoundBurstTime = Time.time + GetEffectiveCooldownDuration(soundBurstCooldown);
        isCasting = false;
    }

    private float GetCurrentRallyRadius()
    {
        if (CurrentTempoTier < TempoManager.TempoTier.T2)
            return rallyPulseRadius;

        return rallyPulseRadius * tempoConfig.t2RallyRadiusMultiplier;
    }

    private float GetCurrentRallyDuration()
    {
        if (CurrentTempoTier < TempoManager.TempoTier.T2)
            return rallyBuffDuration;

        return rallyBuffDuration * tempoConfig.t2RallyDurationMultiplier;
    }

    private float GetCurrentTempoStaticRadius()
    {
        if (CurrentTempoTier != TempoManager.TempoTier.T3)
            return tempoStaticRadius;

        return tempoStaticRadius * tempoConfig.t3TempoStaticRadiusMultiplier;
    }

    private float GetCurrentTempoStaticGainMultiplier()
    {
        if (CurrentTempoTier != TempoManager.TempoTier.T3)
            return tempoStaticGainMultiplier;

        return tempoStaticGainMultiplier * tempoConfig.t3TempoStaticGainMultiplier;
    }

    private float GetCurrentTempoStaticDecayMultiplier()
    {
        if (CurrentTempoTier != TempoManager.TempoTier.T3)
            return tempoStaticDecayMultiplier;

        return tempoStaticDecayMultiplier * tempoConfig.t3TempoStaticDecayMultiplier;
    }

    private void HandleEnemyCombatAction(EnemyCombatActionEvent combatEvent)
    {
        if (!HasEliteMechanic(EliteMechanicType.ResonatorCrescendo) || combatEvent.source == null || combatEvent.source == this)
            return;

        EliteResonatorCrescendoSettings settings = ActiveEliteProfile != null ? ActiveEliteProfile.resonatorCrescendo : null;
        if (settings == null)
            return;

        float distance = Vector2.Distance(transform.position, combatEvent.worldPosition);
        if (distance > settings.actionRadius)
            return;

        float meterGain = 0f;
        switch (combatEvent.actionType)
        {
            case EnemyCombatActionType.Attack: meterGain = settings.meterPerAttack; break;
            case EnemyCombatActionType.Dash: meterGain = settings.meterPerDash; break;
            case EnemyCombatActionType.Cast: meterGain = settings.meterPerCast; break;
            case EnemyCombatActionType.Skill: meterGain = settings.meterPerSkill; break;
            case EnemyCombatActionType.Summon: meterGain = settings.meterPerSummon; break;
        }

        if (meterGain <= 0f)
            return;

        float previousMeter = crescendoMeter;
        crescendoMeter = Mathf.Clamp01(crescendoMeter + meterGain * Mathf.Max(0.2f, combatEvent.weight));
        UpdateOverheadMeter();
        if (!crescendoReadyAnnounced && previousMeter < 1f && crescendoMeter >= 1f)
        {
            crescendoReadyAnnounced = true;
            SupportPulseVisualUtility.SpawnPulse(transform.position, 0.18f, 1.2f, 0.2f, settings.meterColor);
            if (DamagePopupManager.Instance != null)
                DamagePopupManager.Instance.CreateText(transform.position + Vector3.up * 1.9f, "CRESCENDO", settings.meterColor, 5.8f);
        }
    }

    private IEnumerator CrescendoRoutine()
    {
        EliteResonatorCrescendoSettings settings = ActiveEliteProfile != null ? ActiveEliteProfile.resonatorCrescendo : null;
        if (settings == null)
            yield break;

        isCasting = true;
        isCrescendoChanneling = true;
        if (rb != null)
            rb.linearVelocity = Vector2.zero;
        EmitCombatAction(EnemyCombatActionType.Skill, 1.5f);
        if (spriteRenderer != null)
            spriteRenderer.color = settings.meterColor;

        float timer = 0f;
        while (timer < settings.channelDuration)
        {
            timer += Time.deltaTime;
            overheadMeter?.SetProgress(timer / Mathf.Max(0.01f, settings.channelDuration));
            yield return null;
        }

        EnemySupportUtility.GatherNearbyAllies(transform, settings.pulseRadius, nearbyAllies, this);
        for (int i = 0; i < nearbyAllies.Count; i++)
        {
            nearbyAllies[i]?.GetSupportBuffReceiver()?.ApplyRallyBuff(
                settings.pulseDuration,
                settings.pulseMoveMultiplier,
                settings.pulseAttackMultiplier,
                true);
        }

        if (TempoManager.Instance != null)
            TempoManager.Instance.AddTempo(-settings.playerRhythmShock);

        SupportPulseVisualUtility.SpawnPulse(transform.position, 0.3f, settings.pulseRadius, 0.35f, settings.meterColor);
        if (spriteRenderer != null)
            spriteRenderer.color = Color.white;
        isCrescendoChanneling = false;
        isCasting = false;
        crescendoMeter = 0f;
        crescendoReadyAnnounced = false;
        UpdateOverheadMeter();
    }

    private void RefreshEliteMeterVisibility()
    {
        if (overheadMeter == null)
            return;

        if (HasEliteMechanic(EliteMechanicType.ResonatorCrescendo) && ActiveEliteProfile != null)
        {
            overheadMeter.Configure(ActiveEliteProfile.resonatorCrescendo.meterColor, 0.95f, 0.08f);
            overheadMeter.SetVisible(true);
            UpdateOverheadMeter();
            return;
        }

        overheadMeter.SetVisible(false);
    }

    private void UpdateOverheadMeter()
    {
        if (overheadMeter == null || !HasEliteMechanic(EliteMechanicType.ResonatorCrescendo))
            return;

        overheadMeter.SetVisible(true);
        overheadMeter.SetProgress(crescendoMeter);
    }

    private void RefreshCrescendoPresentationIfNeeded()
    {
        bool shouldShow = HasEliteMechanic(EliteMechanicType.ResonatorCrescendo) && ActiveEliteProfile != null;
        if (shouldShow == crescendoPresentationActive)
            return;

        crescendoPresentationActive = shouldShow;
        RefreshEliteMeterVisibility();
        if (!shouldShow)
            crescendoReadyAnnounced = false;
    }
}

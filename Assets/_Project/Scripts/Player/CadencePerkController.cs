using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class CadencePerkController : MonoBehaviour
{
    private enum CadenceActionType
    {
        None,
        Attack,
        Dash,
        Parry,
        Skill
    }

    [Header("T1: Sabit Nabiz")]
    public float steadyPulseDecayMultiplier = 0.78f;

    [Header("T1: Gecis Ritmi")]
    public float transitionWindow = 1.25f;
    public float transitionTempoBonus = 2.0f;
    public float transitionFocusMultiplier = 1.25f;

    [Header("T1: Yumusak Dusus")]
    public float softFallDuration = 2.0f;
    public float softFallDamageCarryBonus = 0.08f;
    public float softFallDecayMultiplier = 0.85f;

    [Header("T1: Olculu Guc")]
    public float measuredPowerHoldTime = 2.5f;
    public float measuredPowerDamageBonus = 0.08f;
    public float measuredPowerAttackCooldownMultiplier = 0.96f;
    public float measuredPowerStunDuration = 0.18f;

    [Header("T1: Ritim Kalkani")]
    public float rhythmShieldWhiffPenaltyMultiplier = 0.55f;
    public float rhythmShieldDamagePenaltyMultiplier = 0.70f;

    [Header("T2: Cadence Odagi")]
    public float focusCadenceEffectMultiplier = 1.30f;
    public float focusDecayMultiplier = 0.70f;
    public float focusDamagePenaltyMultiplier = 0.65f;
    [Tooltip("Cadence focus acikken Overdrive esik patlamasi gibi ani esik odullerini carpar.")]
    public float focusOverdriveThresholdMultiplier = 0.75f;

    [Header("T2 Measured: Olcu Cizgisi")]
    [Range(0f, 1f)] public float stableZoneCenter = 0.55f;
    [Range(0.02f, 1f)] public float stableZoneWidth = 0.28f;
    public float stableZoneDecayMultiplier = 0.82f;
    public float stableZoneDamageBonus = 0.05f;

    [Header("T2 Measured: Denge Noktasi")]
    public float balanceStackGainPerSecond = 0.65f;
    public float balanceStackDecayPerSecond = 1.0f;
    public int balanceMaxStacks = 5;
    public float balanceDamageBonusPerStack = 0.025f;
    public float balanceCounterBonusPerStack = 0.025f;
    public float balanceStunPerStack = 0.04f;

    [Header("T2 Measured: Zamanli Vurgu")]
    public float rhythmMinInterval = 0.45f;
    public float rhythmMaxInterval = 1.25f;
    public int timedAccentActionsRequired = 3;
    public float timedAccentDamageBonus = 0.22f;
    public float timedAccentTempoBonus = 1.5f;
    public float timedAccentStunDuration = 0.25f;

    [Header("T2 Measured: Geri Toparlanma")]
    public float recoveryReturnWhiffMultiplier = 0.45f;
    public float recoveryReturnGraceDuration = 1.2f;

    [Header("T2 Measured: Kusursuz Olcu")]
    public float perfectMeasureRequiredTime = 5.0f;
    public float perfectMeasureDamageBonus = 0.35f;
    public float perfectMeasureTempoBonus = 3.0f;
    public float perfectMeasureStunDuration = 0.45f;
    public float perfectMeasureDodgeRefund = 0.08f;
    public float perfectMeasureParryRefund = 0.06f;
    public float perfectMeasureCounterRefresh = 0.35f;

    [Header("T2 Flow: Akis Halkasi")]
    public int flowMaxStacks = 6;
    public float flowStackGrace = 1.6f;
    public float flowStackDecayPerSecond = 1.0f;
    public float flowDamageBonusPerStack = 0.018f;
    public float flowTempoBonusPerStack = 0.20f;

    [Header("T2 Flow: Kayar Devam")]
    public float slidingContinuationDecayMultiplier = 0.45f;
    public float movementVelocityThreshold = 0.15f;
    public float targetSwitchGrace = 1.0f;

    [Header("T2 Flow: Dalga Sekmesi")]
    public int waveBounceMinStacks = 4;
    public float waveBounceRange = 3.5f;
    public float waveBounceDamageRatio = 0.25f;
    public float waveBounceCooldown = 0.7f;

    [Header("T2 Flow: Esik Sorfu")]
    public float thresholdSurfDuration = 2.2f;
    [Range(0f, 1f)] public float thresholdSurfFlowCarryRatio = 0.50f;
    public float thresholdSurfDamageCarryBonus = 0.06f;
    public float thresholdSurfDecayMultiplier = 0.85f;

    [Header("T2 Flow: Taskin Uyum")]
    public int overflowHarmonyMinStacks = 5;
    public float overflowHarmonyInterval = 3.0f;
    public float overflowHarmonyTempoBonus = 2.5f;
    public float overflowHarmonyDodgeRefund = 0.10f;
    public float overflowHarmonyParryRefund = 0.08f;

    private bool hasSteadyPulse;
    private bool hasTransitionRhythm;
    private bool hasSoftFall;
    private bool hasMeasuredPower;
    private bool hasRhythmShield;
    private bool hasCommitment;
    private bool hasMeasureLine;
    private bool hasBalancePoint;
    private bool hasTimedAccent;
    private bool hasRecoveryReturn;
    private bool hasPerfectMeasure;
    private bool hasFlowRing;
    private bool hasSlidingContinuation;
    private bool hasWaveBounce;
    private bool hasThresholdSurf;
    private bool hasOverflowHarmony;

    private PlayerController playerController;
    private ParrySystem parrySystem;
    private Rigidbody2D rb;

    private TempoManager.TempoTier lastTier = TempoManager.TempoTier.T0;
    private float tierHoldTimer;
    private float softFallTimer;
    private float recoveryGraceTimer;
    private float thresholdSurfTimer;
    private float balanceStacks;
    private float lastActionTime = -999f;
    private CadenceActionType lastActionType = CadenceActionType.None;
    private int rhythmChain;
    private float perfectMeasureTimer;
    private bool timedAccentReady;
    private bool perfectMeasureReady;
    private bool accentAppliedThisAttack;
    private bool perfectMeasureAppliedThisAttack;
    private float pendingHitTempoBonus;
    private float flowStacks;
    private float flowGraceTimer;
    private float overflowHarmonyTimer;
    private float waveBounceCooldownTimer;
    private bool waveBounceUsedThisAttack;
    private EnemyBase lastHitEnemy;
    private float targetSwitchTimer;
    private Collider2D[] waveBounceHitBuffer = new Collider2D[32];

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
        parrySystem = GetComponent<ParrySystem>();
        rb = GetComponent<Rigidbody2D>();
    }

    private void OnEnable()
    {
        Subscribe();
    }

    private void Start()
    {
        StartCoroutine(DelayedSync());
    }

    private IEnumerator DelayedSync()
    {
        yield return null;
        Subscribe();
        if (TempoManager.Instance != null)
            lastTier = TempoManager.Instance.CurrentTier;
        if (AxisProgressionManager.Instance != null)
            HandleBuildChanged(AxisProgressionManager.Instance.CurrentBuild);
    }

    private void OnDisable()
    {
        Unsubscribe();
        if (TempoManager.Instance != null)
            TempoManager.Instance.SetCadenceTempoMultipliers(1f, 1f);
    }

    private void Update()
    {
        float dt = Time.deltaTime;

        if (softFallTimer > 0f) softFallTimer -= dt;
        if (recoveryGraceTimer > 0f) recoveryGraceTimer -= dt;
        if (thresholdSurfTimer > 0f) thresholdSurfTimer -= dt;
        if (flowGraceTimer > 0f) flowGraceTimer -= dt;
        if (targetSwitchTimer > 0f) targetSwitchTimer -= dt;
        if (waveBounceCooldownTimer > 0f) waveBounceCooldownTimer -= dt;

        TickTierHold(dt);
        TickBalance(dt);
        TickFlow(dt);
        ApplyTempoMultipliers();
    }

    private void Subscribe()
    {
        if (AxisProgressionManager.Instance != null)
        {
            AxisProgressionManager.Instance.OnBuildChanged -= HandleBuildChanged;
            AxisProgressionManager.Instance.OnBuildChanged += HandleBuildChanged;
        }

        if (TempoManager.Instance != null)
        {
            TempoManager.Instance.OnTierChanged -= HandleTierChanged;
            TempoManager.Instance.OnTierChanged += HandleTierChanged;
            lastTier = TempoManager.Instance.CurrentTier;
        }

        if (playerController != null)
        {
            playerController.OnDodgeStarted -= HandleDodgeStarted;
            playerController.OnDodgeStarted += HandleDodgeStarted;
        }

        if (parrySystem != null)
        {
            parrySystem.OnParryStarted -= HandleParryStarted;
            parrySystem.OnParryStarted += HandleParryStarted;
            parrySystem.OnParryFail -= HandleParryFail;
            parrySystem.OnParryFail += HandleParryFail;
        }
    }

    private void Unsubscribe()
    {
        if (AxisProgressionManager.Instance != null)
            AxisProgressionManager.Instance.OnBuildChanged -= HandleBuildChanged;
        if (TempoManager.Instance != null)
            TempoManager.Instance.OnTierChanged -= HandleTierChanged;
        if (playerController != null)
            playerController.OnDodgeStarted -= HandleDodgeStarted;
        if (parrySystem != null)
        {
            parrySystem.OnParryStarted -= HandleParryStarted;
            parrySystem.OnParryFail -= HandleParryFail;
        }
    }

    private void HandleBuildChanged(PlayerBuild build)
    {
        if (build == null) return;

        hasSteadyPulse = build.HasFlag(EffectKeyRegistry.CadenceSteadyPulse);
        hasTransitionRhythm = build.HasFlag(EffectKeyRegistry.CadenceTransitionRhythm);
        hasSoftFall = build.HasFlag(EffectKeyRegistry.CadenceSoftFall);
        hasMeasuredPower = build.HasFlag(EffectKeyRegistry.CadenceMeasuredPower);
        hasRhythmShield = build.HasFlag(EffectKeyRegistry.CadenceRhythmShield);
        hasCommitment = build.HasFlag(EffectKeyRegistry.CadenceT2Commitment);
        hasMeasureLine = build.HasFlag(EffectKeyRegistry.CadenceMeasureLine);
        hasBalancePoint = build.HasFlag(EffectKeyRegistry.CadenceBalancePoint);
        hasTimedAccent = build.HasFlag(EffectKeyRegistry.CadenceTimedAccent);
        hasRecoveryReturn = build.HasFlag(EffectKeyRegistry.CadenceRecoveryReturn);
        hasPerfectMeasure = build.HasFlag(EffectKeyRegistry.CadencePerfectMeasure);
        hasFlowRing = build.HasFlag(EffectKeyRegistry.CadenceFlowRing);
        hasSlidingContinuation = build.HasFlag(EffectKeyRegistry.CadenceSlidingContinuation);
        hasWaveBounce = build.HasFlag(EffectKeyRegistry.CadenceWaveBounce);
        hasThresholdSurf = build.HasFlag(EffectKeyRegistry.CadenceThresholdSurf);
        hasOverflowHarmony = build.HasFlag(EffectKeyRegistry.CadenceOverflowHarmony);
    }

    private void HandleTierChanged(TempoManager.TempoTier tier)
    {
        if ((int)tier < (int)lastTier)
        {
            if (hasSoftFall)
                softFallTimer = softFallDuration;

            if (hasThresholdSurf)
            {
                thresholdSurfTimer = thresholdSurfDuration;
                flowStacks = Mathf.Min(flowMaxStacks, flowStacks * thresholdSurfFlowCarryRatio);
                flowGraceTimer = Mathf.Max(flowGraceTimer, thresholdSurfDuration);
            }

            if (hasRecoveryReturn && IsInStableZone())
                recoveryGraceTimer = recoveryReturnGraceDuration;
        }

        lastTier = tier;
        tierHoldTimer = 0f;
    }

    public void NotifyAttackAction()
    {
        pendingHitTempoBonus = 0f;
        accentAppliedThisAttack = false;
        perfectMeasureAppliedThisAttack = false;
        waveBounceUsedThisAttack = false;
        RecordAction(CadenceActionType.Attack);
    }

    public void NotifyDashAction()
    {
        RecordAction(CadenceActionType.Dash);
    }

    public void NotifySkillAction()
    {
        RecordAction(CadenceActionType.Skill);
    }

    public float GetAttackCooldownMultiplier()
    {
        float multiplier = 1f;

        if (hasMeasuredPower && tierHoldTimer >= measuredPowerHoldTime)
            multiplier *= measuredPowerAttackCooldownMultiplier;

        if (hasPerfectMeasure && perfectMeasureReady)
            multiplier *= 0.92f;

        return Mathf.Max(0.1f, multiplier);
    }

    public float GetGlobalDamageBonus(float attackMultiplier, float totalCounter)
    {
        float bonus = 0f;

        if (hasMeasuredPower && tierHoldTimer >= measuredPowerHoldTime)
            bonus += measuredPowerDamageBonus * CadenceMultiplier();

        if (hasSoftFall && softFallTimer > 0f)
            bonus += softFallDamageCarryBonus * CadenceMultiplier();

        if (hasMeasureLine && IsInStableZone())
            bonus += stableZoneDamageBonus * CadenceMultiplier();

        if (hasBalancePoint && balanceStacks > 0f)
            bonus += Mathf.FloorToInt(balanceStacks) * balanceDamageBonusPerStack * CadenceMultiplier();

        if (hasTimedAccent && timedAccentReady)
        {
            bonus += timedAccentDamageBonus * CadenceMultiplier();
            pendingHitTempoBonus += timedAccentTempoBonus * CadenceMultiplier();
            accentAppliedThisAttack = true;
        }

        if (hasPerfectMeasure && perfectMeasureReady)
        {
            bonus += perfectMeasureDamageBonus * CadenceMultiplier();
            pendingHitTempoBonus += perfectMeasureTempoBonus * CadenceMultiplier();
            perfectMeasureAppliedThisAttack = true;
        }

        if (hasFlowRing && flowStacks > 0f)
            bonus += Mathf.FloorToInt(flowStacks) * flowDamageBonusPerStack * CadenceMultiplier();

        if (hasThresholdSurf && thresholdSurfTimer > 0f)
            bonus += thresholdSurfDamageCarryBonus * CadenceMultiplier();

        return bonus;
    }

    public float GetTargetDamageBonus(EnemyBase enemy, float attackMultiplier, float totalCounter)
    {
        if (enemy == null)
            return 0f;

        return hasBalancePoint && totalCounter > 0f
            ? Mathf.FloorToInt(balanceStacks) * balanceCounterBonusPerStack * CadenceMultiplier()
            : 0f;
    }

    public float GetTempoGainOnHit()
    {
        float result = pendingHitTempoBonus;
        pendingHitTempoBonus = 0f;

        if (hasFlowRing && flowStacks > 0f)
            result += Mathf.FloorToInt(flowStacks) * flowTempoBonusPerStack * CadenceMultiplier();

        return result;
    }

    public float ModifyWhiffTempoPenalty(float penalty)
    {
        float multiplier = 1f;

        if (hasRhythmShield)
            multiplier *= rhythmShieldWhiffPenaltyMultiplier;
        if (hasRecoveryReturn && IsInStableZone())
        {
            multiplier *= recoveryReturnWhiffMultiplier;
            recoveryGraceTimer = recoveryReturnGraceDuration;
        }

        BreakRhythm();
        return penalty * multiplier;
    }

    public float GetIncomingDamageMultiplier()
    {
        return 1f;
    }

    public float GetOverdriveThresholdMultiplier()
    {
        return hasCommitment ? Mathf.Max(0f, focusOverdriveThresholdMultiplier) : 1f;
    }

    public void NotifyEnemyHit(EnemyBase enemy, bool killed, float attackMultiplier, float totalCounter)
    {
        if (enemy == null)
            return;

        if (enemy != lastHitEnemy && lastHitEnemy != null)
            targetSwitchTimer = targetSwitchGrace;
        lastHitEnemy = enemy;

        if (hasMeasuredPower && tierHoldTimer >= measuredPowerHoldTime)
            enemy.Stun(measuredPowerStunDuration * CadenceMultiplier());

        if (accentAppliedThisAttack)
        {
            enemy.Stun(timedAccentStunDuration * CadenceMultiplier());
            timedAccentReady = false;
            accentAppliedThisAttack = false;
        }

        if (perfectMeasureAppliedThisAttack)
        {
            enemy.Stun(perfectMeasureStunDuration * CadenceMultiplier());
            playerController?.ReduceDodgeCooldown(perfectMeasureDodgeRefund);
            parrySystem?.ReduceRecoveryCooldown(perfectMeasureParryRefund);
            parrySystem?.RefreshOrExtendCounterWindow(perfectMeasureCounterRefresh);
            perfectMeasureReady = false;
            perfectMeasureAppliedThisAttack = false;
            perfectMeasureTimer = 0f;
        }

        if (hasBalancePoint && balanceStacks >= 1f)
            enemy.Stun(Mathf.FloorToInt(balanceStacks) * balanceStunPerStack * CadenceMultiplier());
    }

    public void TryWaveBounce(EnemyBase source, float sourceDamage)
    {
        if (!hasWaveBounce || source == null || waveBounceUsedThisAttack || waveBounceCooldownTimer > 0f)
            return;
        if (flowStacks < waveBounceMinStacks)
            return;

        int hitCount = CombatPhysicsQueryUtility.OverlapCircleAllLayers(source.transform.position, waveBounceRange, ref waveBounceHitBuffer, 32);
        EnemyBase best = null;
        float bestDist = float.MaxValue;

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit = waveBounceHitBuffer[i];
            if (hit == null)
                continue;

            var enemy = hit.GetComponent<EnemyBase>();
            if (enemy == null || enemy == source)
                continue;

            float dist = Vector2.Distance(source.transform.position, enemy.transform.position);
            if (dist < bestDist)
            {
                bestDist = dist;
                best = enemy;
            }
        }

        if (best == null)
            return;

        EnemyDamageUtility.ApplyDamage(
            best,
            sourceDamage * waveBounceDamageRatio,
            EnemyDamageSource.Skill,
            gameObject,
            EnemyDamageUtility.DirectionFromInstigator(best, gameObject),
            0.55f,
            isPerfectTiming: true);
        waveBounceUsedThisAttack = true;
        waveBounceCooldownTimer = waveBounceCooldown;

        if (DamagePopupManager.Instance != null)
            DamagePopupManager.Instance.CreateText(best.transform.position + Vector3.up, "FLOW ECHO", new Color(0.35f, 0.95f, 0.78f), 5f);
    }

    private void HandleDodgeStarted(Vector2 dir)
    {
        NotifyDashAction();
    }

    private void HandleParryStarted(Vector2 dir)
    {
        RecordAction(CadenceActionType.Parry);
    }

    private void HandleParryFail()
    {
        if (hasRecoveryReturn && IsInStableZone())
            recoveryGraceTimer = recoveryReturnGraceDuration;
        BreakRhythm();
    }

    private void RecordAction(CadenceActionType type)
    {
        float now = Time.time;
        float interval = now - lastActionTime;
        bool hasPrevious = lastActionType != CadenceActionType.None;
        bool changedType = hasPrevious && type != lastActionType;
        bool smoothTransition = changedType && interval <= transitionWindow;
        bool timedRhythm = hasPrevious && interval >= rhythmMinInterval && interval <= rhythmMaxInterval;
        bool spammed = hasPrevious && interval < rhythmMinInterval;

        if (hasTransitionRhythm && smoothTransition && TempoManager.Instance != null)
            TempoManager.Instance.AddTempo(transitionTempoBonus * TransitionMultiplier());

        if (hasFlowRing)
        {
            if (smoothTransition)
            {
                flowStacks = Mathf.Min(flowMaxStacks, flowStacks + 1f);
                flowGraceTimer = flowStackGrace;
            }
            else if (!hasSlidingContinuation && interval > transitionWindow)
            {
                flowStacks = Mathf.Max(0f, flowStacks - 1f);
            }
        }

        if (hasTimedAccent || hasPerfectMeasure)
        {
            if (timedRhythm && !spammed)
            {
                rhythmChain++;
                if (hasTimedAccent && rhythmChain >= timedAccentActionsRequired)
                {
                    timedAccentReady = true;
                    rhythmChain = 0;
                }
            }
            else if (spammed || interval > rhythmMaxInterval)
            {
                BreakRhythm();
            }
        }

        lastActionType = type;
        lastActionTime = now;
    }

    private void TickTierHold(float dt)
    {
        if (TempoManager.Instance == null)
            return;

        if (TempoManager.Instance.CurrentTier == lastTier)
            tierHoldTimer += dt;
        else
            lastTier = TempoManager.Instance.CurrentTier;
    }

    private void TickBalance(float dt)
    {
        bool stable = IsInStableZone();

        if (hasBalancePoint && stable)
            balanceStacks = Mathf.Min(balanceMaxStacks, balanceStacks + balanceStackGainPerSecond * dt * CadenceMultiplier());
        else
            balanceStacks = Mathf.Max(0f, balanceStacks - balanceStackDecayPerSecond * dt);

        bool rhythmMaintained = stable || flowStacks > 0f || recoveryGraceTimer > 0f;
        if (hasPerfectMeasure && rhythmMaintained)
        {
            perfectMeasureTimer += dt;
            if (perfectMeasureTimer >= perfectMeasureRequiredTime)
            {
                perfectMeasureReady = true;
                perfectMeasureTimer = 0f;
            }
        }
        else if (!rhythmMaintained)
        {
            perfectMeasureTimer = 0f;
        }
    }

    private void TickFlow(float dt)
    {
        if (!hasFlowRing)
            return;

        bool movementCarry = hasSlidingContinuation && IsMoving();
        bool targetCarry = hasSlidingContinuation && targetSwitchTimer > 0f;
        float decayMultiplier = movementCarry || targetCarry ? slidingContinuationDecayMultiplier : 1f;

        if (flowGraceTimer <= 0f)
            flowStacks = Mathf.Max(0f, flowStacks - flowStackDecayPerSecond * decayMultiplier * dt);

        if (!hasOverflowHarmony || flowStacks < overflowHarmonyMinStacks)
            return;

        overflowHarmonyTimer += dt;
        if (overflowHarmonyTimer < overflowHarmonyInterval)
            return;

        overflowHarmonyTimer = 0f;
        playerController?.ReduceDodgeCooldown(overflowHarmonyDodgeRefund);
        parrySystem?.ReduceRecoveryCooldown(overflowHarmonyParryRefund);
        if (TempoManager.Instance != null)
            TempoManager.Instance.AddTempo(overflowHarmonyTempoBonus * CadenceMultiplier());
    }

    private void ApplyTempoMultipliers()
    {
        if (TempoManager.Instance == null)
            return;

        float decay = 1f;
        float damagePenalty = 1f;

        if (hasSteadyPulse)
            decay *= steadyPulseDecayMultiplier;
        if (hasSoftFall && softFallTimer > 0f)
            decay *= softFallDecayMultiplier;
        if (hasCommitment)
        {
            decay *= focusDecayMultiplier;
            damagePenalty *= focusDamagePenaltyMultiplier;
        }
        if (hasMeasureLine && IsInStableZone())
            decay *= stableZoneDecayMultiplier;
        if (hasThresholdSurf && thresholdSurfTimer > 0f)
            decay *= thresholdSurfDecayMultiplier;
        if (hasRhythmShield)
            damagePenalty *= rhythmShieldDamagePenaltyMultiplier;

        TempoManager.Instance.SetCadenceTempoMultipliers(decay, damagePenalty);
    }

    private bool IsInStableZone()
    {
        if (!hasMeasureLine || TempoManager.Instance == null)
            return false;

        float lower;
        float upper;
        GetTierBounds(TempoManager.Instance.CurrentTier, out lower, out upper);

        float width = Mathf.Max(0.01f, upper - lower);
        float center = lower + width * stableZoneCenter;
        float half = width * stableZoneWidth * 0.5f;
        float value = TempoManager.Instance.tempo;
        return value >= center - half && value <= center + half;
    }

    private void GetTierBounds(TempoManager.TempoTier tier, out float lower, out float upper)
    {
        var tempo = TempoManager.Instance;
        if (tempo == null)
        {
            lower = 0f;
            upper = 100f;
            return;
        }

        switch (tier)
        {
            case TempoManager.TempoTier.T0:
                lower = 0f;
                upper = tempo.tier1Start;
                break;
            case TempoManager.TempoTier.T1:
                lower = tempo.tier1Start;
                upper = tempo.tier2Start;
                break;
            case TempoManager.TempoTier.T2:
                lower = tempo.tier2Start;
                upper = tempo.tier3Start;
                break;
            default:
                lower = tempo.tier3Start;
                upper = tempo.maxTempo;
                break;
        }
    }

    private void BreakRhythm()
    {
        rhythmChain = 0;
        timedAccentReady = false;
        perfectMeasureTimer = 0f;
        if (!hasThresholdSurf)
            thresholdSurfTimer = 0f;
    }

    private float CadenceMultiplier()
    {
        return hasCommitment ? focusCadenceEffectMultiplier : 1f;
    }

    private float TransitionMultiplier()
    {
        return hasCommitment ? transitionFocusMultiplier : 1f;
    }

    private bool IsMoving()
    {
        if (playerController != null &&
            (playerController.currentState == PlayerController.PlayerState.Moving ||
             playerController.currentState == PlayerController.PlayerState.Dodging ||
             playerController.currentState == PlayerController.PlayerState.DashStriking))
            return true;

        return rb != null && rb.linearVelocity.sqrMagnitude > movementVelocityThreshold * movementVelocityThreshold;
    }
}

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Parry skill tree perk kontrol sistemi.
/// Numeric tuning tamamen inspector üzerinden yapılır.
/// </summary>
[DisallowMultipleComponent]
public class ParryPerkController : MonoBehaviour
{
    private static readonly Collider2D[] executeReadyScanBuffer = new Collider2D[32];

    private PlayerController playerController;
    private ParrySystem parrySystem;
    private DashPerkController dashPerkController;

    [Header("=== T1: Yansıtma ===")]
    public float baseReflectSpeedMultiplier = 1.5f;
    public float baseReflectDamageMultiplier = 1f;
    public float reflectEdgeThickness = 0.35f;

    [Header("=== T1: Kusursuz Zamanlama ===")]
    public float perfectWindowDuration = 0.06f;

    [Header("=== T1: Karşılık Duruşu ===")]
    public float counterWindowDuration = 0.5f;
    public float counterBonusPerMelee = 0.15f;
    public float counterBonusPerRanged = 0.10f;

    [Header("=== T1: Kusursuz Kırılma ===")]
    public float perfectMeleeStunDuration = 1.5f;
    public float perfectBossInterruptDuration = 0.35f;

    [Header("=== T1: Ritim İadesi ===")]
    public float successRecoveryRefund = 0.03f;
    public float perfectRecoveryRefund = 0.07f;
    public float perfectTempoGain = 8f;

    [Header("=== T2: Parry Odağı ===")]
    public float commitmentParryTempoMultiplier = 1.30f;
    public float commitmentParryRecoveryMultiplier = 0.80f;
    public float commitmentParryWindowMultiplier = 1.20f;
    public float commitmentPerfectWindowMultiplier = 1.30f;
    public float commitmentCounterMultiplier = 1.25f;
    public float commitmentDashCooldownMultiplier = 1.20f;
    public float commitmentDashWindowMultiplier = 0.85f;
    public float commitmentDashTempoMultiplier = 0.75f;

    [Header("=== T2 Balistik: Ters Cephe ===")]
    public float reverseFrontHalfAngle = 55f;
    public float reverseRearHalfAngle = 55f;

    [Header("=== T2 Balistik: Aşırı Sekme ===")]
    public float overdeflectSpeedMultiplier = 1.5f;
    public float overdeflectDamageMultiplier = 1.5f;
    public int overdeflectPierceCount = 2;

    [Header("=== T2 Balistik: Bastırıcı İz ===")]
    public float suppressDuration = 0.75f;
    public float suppressBossInterruptDuration = 0.25f;

    [Header("=== T2 Balistik: Kırık Yörünge ===")]
    public int fracturedSplitCount = 2;
    public float fracturedSplitDamageMultiplier = 0.45f;
    public float fracturedSplitAngleSpread = 24f;
    public float fracturedSplitSpeedMultiplier = 1f;

    [Header("=== T2 Balistik: Geri Besleme ===")]
    public int feedbackMaxStacks = 5;
    public float feedbackStackDuration = 5f;
    public float feedbackRecoveryRefund = 0.02f;
    public float feedbackSpeedBonusPerStack = 0.08f;
    public float feedbackDamageBonusPerStack = 0.08f;

    [Header("=== T2 Mükemmeliyetçi: Yakın İnfaz ===")]
    public float closeExecuteRange = 2.2f;

    [Header("=== T2 Mükemmeliyetçi: İnce Kenar ===")]
    public float fineEdgePerfectWindowMultiplier = 1.5f;
    public float fineEdgeNormalWindowMultiplier = 0.35f;

    [Header("=== T2 Mükemmeliyetçi: Ağır Karşılık ===")]
    public float heavyRiposteStunDuration = 2.25f;
    public float heavyRiposteBossInterruptDuration = 0.5f;
    public float guardBreakDuration = 2f;

    [Header("=== T2 Mükemmeliyetçi: Dönen Koni ===")]
    public float rotatingConeDegreesPerSecond = 1080f;
    public float rotatingConeDuration = 0.18f;
    public float rotatingConeProjectileWindowExtensionMultiplier = 2f;
    public float rotatingConeProjectileMaxWindowBonus = 0.26f;

    [Header("=== T2 Mükemmeliyetçi: Kusursuz Döngü ===")]
    public float perfectCycleRecoveryRefund = 0.08f;
    public float perfectCycleCounterRefreshDuration = 0.35f;
    public float perfectCycleTempoGain = 6f;

    private bool _hasReflect;
    private bool _hasPerfectTiming;
    private bool _hasCounterStance;
    private bool _hasPerfectBreak;
    private bool _hasRhythmReturn;
    private bool _hasCommitment;
    private bool _hasReverseFront;
    private bool _hasOverdeflect;
    private bool _hasSuppressiveTrace;
    private bool _hasFracturedOrbit;
    private bool _hasFeedback;
    private bool _hasCloseExecute;
    private bool _hasFineEdge;
    private bool _hasHeavyRiposte;
    private bool _hasRotatingCone;
    private bool _hasPerfectCycle;

    private int _feedbackStacks;
    private float _feedbackTimer;
    private float _baseParryOnlyEndTime;
    private bool _lastBaseParryOnlyState;

    public bool CanDeflectProjectiles => _hasReflect;
    public event System.Action<EnemyControlFeedbackData> OnEnemyControlFeedback;

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
        parrySystem = GetComponent<ParrySystem>();
        dashPerkController = GetComponent<DashPerkController>();
    }

    private void OnEnable()
    {
        if (parrySystem != null)
        {
            parrySystem.OnParryResolved += HandleParryResolved;
        }

        SubscribeToAxisManager();
    }

    private void OnDisable()
    {
        if (parrySystem != null)
        {
            parrySystem.OnParryResolved -= HandleParryResolved;
        }

        if (AxisProgressionManager.Instance != null)
            AxisProgressionManager.Instance.OnBuildChanged -= HandleBuildChanged;
    }

    private void Start()
    {
        StartCoroutine(DelayedBuildSync());
    }

    private System.Collections.IEnumerator DelayedBuildSync()
    {
        yield return null;

        SubscribeToAxisManager();

        if (AxisProgressionManager.Instance != null)
            HandleBuildChanged(AxisProgressionManager.Instance.CurrentBuild);
    }

    private void Update()
    {
        if (_feedbackTimer > 0f)
        {
            _feedbackTimer -= Time.deltaTime;
            if (_feedbackTimer <= 0f)
            {
                _feedbackStacks = 0;
                _feedbackTimer = 0f;
            }
        }

        if (parrySystem != null)
            parrySystem.deflectEdgeThickness = reflectEdgeThickness;

        bool baseParryOnly = IsBaseParryOnlyActive;
        if (baseParryOnly != _lastBaseParryOnlyState)
        {
            _lastBaseParryOnlyState = baseParryOnly;
            ApplyToParrySystem();
        }
    }

    private void SubscribeToAxisManager()
    {
        if (AxisProgressionManager.Instance != null)
        {
            AxisProgressionManager.Instance.OnBuildChanged -= HandleBuildChanged;
            AxisProgressionManager.Instance.OnBuildChanged += HandleBuildChanged;
        }
    }

    private void HandleBuildChanged(PlayerBuild build)
    {
        if (build == null) return;

        _hasReflect = build.HasFlag(EffectKeyRegistry.ParryReflect);
        _hasPerfectTiming = build.HasFlag(EffectKeyRegistry.ParryPerfectTiming);
        _hasCounterStance = build.HasFlag(EffectKeyRegistry.ParryCounterStance);
        _hasPerfectBreak = build.HasFlag(EffectKeyRegistry.ParryPerfectBreak);
        _hasRhythmReturn = build.HasFlag(EffectKeyRegistry.ParryRhythmReturn);
        _hasCommitment = build.HasFlag(EffectKeyRegistry.ParryT2Commitment);
        _hasReverseFront = build.HasFlag(EffectKeyRegistry.ParryReverseFront);
        _hasOverdeflect = build.HasFlag(EffectKeyRegistry.ParryOverdeflect);
        _hasSuppressiveTrace = build.HasFlag(EffectKeyRegistry.ParrySuppressiveTrace);
        _hasFracturedOrbit = build.HasFlag(EffectKeyRegistry.ParryFracturedOrbit);
        _hasFeedback = build.HasFlag(EffectKeyRegistry.ParryFeedback);
        _hasCloseExecute = build.HasFlag(EffectKeyRegistry.ParryCloseExecute);
        _hasFineEdge = build.HasFlag(EffectKeyRegistry.ParryFineEdge);
        _hasHeavyRiposte = build.HasFlag(EffectKeyRegistry.ParryHeavyRiposte);
        _hasRotatingCone = build.HasFlag(EffectKeyRegistry.ParryRotatingCone);
        _hasPerfectCycle = build.HasFlag(EffectKeyRegistry.ParryPerfectCycle);

        ApplyToParrySystem();
        ApplyCommitmentToDash();

        if (!_hasFeedback)
        {
            _feedbackStacks = 0;
            _feedbackTimer = 0f;
        }
    }

    private void ApplyToParrySystem()
    {
        if (parrySystem == null) return;
        bool usePerks = !IsBaseParryOnlyActive;

        parrySystem.allowProjectileDeflect = usePerks && _hasReflect;
        parrySystem.enablePerfectParry = usePerks && _hasPerfectTiming;
        parrySystem.enableCounterWindow = usePerks && _hasCounterStance;
        parrySystem.counterWindowDuration = counterWindowDuration;
        parrySystem.deflectEdgeThickness = reflectEdgeThickness;

        float counterMult = usePerks && _hasCommitment ? commitmentCounterMultiplier : 1f;
        parrySystem.counterBonusPerMelee = usePerks ? counterBonusPerMelee * counterMult : 0f;
        parrySystem.counterBonusPerRanged = usePerks ? counterBonusPerRanged * counterMult : 0f;

        float normalWindowMult = 1f;
        float perfectWindow = usePerks ? perfectWindowDuration : 0f;

        if (usePerks && _hasFineEdge)
        {
            normalWindowMult *= fineEdgeNormalWindowMultiplier;
            perfectWindow *= fineEdgePerfectWindowMultiplier;
        }

        if (usePerks && _hasCommitment)
        {
            parrySystem.SetParryCommitmentMultipliers(
                commitmentParryTempoMultiplier,
                commitmentParryRecoveryMultiplier,
                commitmentParryWindowMultiplier);
            perfectWindow *= commitmentPerfectWindowMultiplier;
        }
        else
        {
            parrySystem.SetParryCommitmentMultipliers(1f, 1f, 1f);
        }

        parrySystem.normalWindowMultiplier = normalWindowMult;
        parrySystem.perfectWindowDuration = perfectWindow;

        if (usePerks && _hasReverseFront)
        {
            parrySystem.useDualArc = true;
            parrySystem.dualArcFrontHalfAngle = reverseFrontHalfAngle;
            parrySystem.dualArcRearHalfAngle = reverseRearHalfAngle;
        }
        else
        {
            parrySystem.useDualArc = false;
            parrySystem.dualArcFrontHalfAngle = parrySystem.parryArcHalfAngle;
            parrySystem.dualArcRearHalfAngle = 0f;
        }

        parrySystem.rotateArcWhileActive = false;
        parrySystem.rotatingArcDegreesPerSecond = rotatingConeDegreesPerSecond;
        parrySystem.rotatingArcDuration = rotatingConeDuration;
        parrySystem.projectileWindowExtensionMultiplier = usePerks && _hasRotatingCone
            ? rotatingConeProjectileWindowExtensionMultiplier
            : 1f;
        parrySystem.projectileMaxWindowBonus = usePerks && _hasRotatingCone
            ? rotatingConeProjectileMaxWindowBonus
            : 0f;
        parrySystem.omniProjectileDeflectWhileActive = usePerks && _hasRotatingCone;
    }

    private void ApplyCommitmentToDash()
    {
        if (playerController != null)
            playerController.SetParryCommitmentDodgeCooldownMultiplier(_hasCommitment ? commitmentDashCooldownMultiplier : 1f);

        if (dashPerkController != null)
        {
            dashPerkController.SetExternalDodgeWindowMultiplier(_hasCommitment ? commitmentDashWindowMultiplier : 1f);
            dashPerkController.SetExternalTempoGainMultiplier(_hasCommitment ? commitmentDashTempoMultiplier : 1f);
        }
    }

    public DeflectContext BuildDeflectContext()
    {
        DeflectContext context = DeflectContext.Default(gameObject);
        if (IsBaseParryOnlyActive)
            return context;

        context.speedMultiplier = baseReflectSpeedMultiplier;
        context.damageMultiplier = baseReflectDamageMultiplier;

        if (_hasOverdeflect)
        {
            context.speedMultiplier *= overdeflectSpeedMultiplier;
            context.damageMultiplier *= overdeflectDamageMultiplier;
            context.pierceCount += Mathf.Max(0, overdeflectPierceCount);
        }

        if (_hasSuppressiveTrace)
            context.suppressDuration = suppressDuration;

        if (_hasFracturedOrbit)
        {
            context.splitCount = fracturedSplitCount;
            context.splitDamageMultiplier = fracturedSplitDamageMultiplier;
            context.splitAngleSpread = fracturedSplitAngleSpread;
            context.splitSpeedMultiplier = fracturedSplitSpeedMultiplier;
        }

        if (_hasFeedback && _feedbackStacks > 0)
        {
            context.speedMultiplier *= 1f + (_feedbackStacks * feedbackSpeedBonusPerStack);
            context.damageMultiplier *= 1f + (_feedbackStacks * feedbackDamageBonusPerStack);
        }

        return context;
    }

    public bool TryGetCloseExecuteReadyTarget(out GameObject target)
    {
        target = null;

        if (IsBaseParryOnlyActive || !_hasCloseExecute || parrySystem == null || !parrySystem.IsParryActive || !parrySystem.allowProjectileDeflect)
            return false;

        float scanRadius = Mathf.Max(closeExecuteRange, parrySystem.CurrentDeflectRange);
        int hitCount = Physics2D.OverlapCircleNonAlloc(transform.position, scanRadius, executeReadyScanBuffer);
        float closestDistance = float.MaxValue;

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit = executeReadyScanBuffer[i];
            if (hit == null || !hit.TryGetComponent<IDeflectable>(out var deflectable))
                continue;

            if (deflectable.ObjectOwner == gameObject || deflectable.IsDeflected || !deflectable.CanBeDeflected)
                continue;

            if (!parrySystem.CanDeflectProjectileAt(hit.transform.position))
                continue;

            GameObject owner = deflectable.SourceOwner != null ? deflectable.SourceOwner : ResolveSourceOwner(hit.gameObject);
            if (!IsCloseExecuteEligible(owner))
                continue;

            float distance = Vector2.Distance(transform.position, owner.transform.position);
            if (distance >= closestDistance)
                continue;

            closestDistance = distance;
            target = owner;
        }

        return target != null;
    }

    private void HandleParryResolved(ParryEventData data)
    {
        if (IsBaseParryOnlyActive)
            return;

        if (_hasRhythmReturn)
        {
            parrySystem.ReduceRecoveryCooldown(data.isPerfect ? perfectRecoveryRefund : successRecoveryRefund);

            if (data.isPerfect && TempoManager.Instance != null)
                TempoManager.Instance.AddTempo(perfectTempoGain);
        }

        if (_hasFeedback && data.isRanged)
        {
            _feedbackStacks = Mathf.Min(feedbackMaxStacks, _feedbackStacks + 1);
            _feedbackTimer = feedbackStackDuration;
            parrySystem.ReduceRecoveryCooldown(feedbackRecoveryRefund);
        }

        if (data.isPerfect)
        {
            if (_hasPerfectBreak && !data.isRanged)
                ApplyEnemyReaction(data.source, perfectMeleeStunDuration, perfectBossInterruptDuration, false);

            if (_hasHeavyRiposte && !data.isRanged)
                ApplyEnemyReaction(data.source, heavyRiposteStunDuration, heavyRiposteBossInterruptDuration, true);

            if (_hasCloseExecute && data.isRanged)
                TryCloseExecute(data.source);

            if (_hasPerfectCycle)
            {
                parrySystem.ReduceRecoveryCooldown(perfectCycleRecoveryRefund);
                parrySystem.RefreshOrExtendCounterWindow(perfectCycleCounterRefreshDuration);

                if (TempoManager.Instance != null)
                    TempoManager.Instance.AddTempo(perfectCycleTempoGain);
            }
        }
    }

    public void HandleProjectileHitReaction(GameObject hitTarget)
    {
        if (IsBaseParryOnlyActive)
            return;

        if (!_hasSuppressiveTrace || hitTarget == null) return;

        ApplyReactionToTarget(hitTarget, suppressDuration, suppressBossInterruptDuration, false, true);
    }

    private void TryCloseExecute(GameObject source)
    {
        if (source == null) return;

        GameObject owner = ResolveProjectileOwner(source);
        if (!IsCloseExecuteEligible(owner))
            return;

        var damageable = owner.GetComponent<IDamageable>();
        if (damageable == null) return;

        var enemyBase = owner.GetComponent<EnemyBase>();
        if (enemyBase != null)
        {
            float lethal = Mathf.Max(enemyBase.CurrentHealth, enemyBase.MaxHealth) + 1f;
            enemyBase.TakeDamage(lethal);
        }
        else
        {
            damageable.TakeDamage(9999f);
        }

        EmitEnemyControlFeedback(owner, EnemyControlFeedbackType.ExecuteTriggered, 0.35f, false);
    }

    private void ApplyEnemyReaction(GameObject source, float stunDuration, float bossInterruptDuration, bool breakGuard)
    {
        if (source == null) return;

        GameObject target = ResolveSourceOwner(source);
        if (target == null) return;

        ApplyReactionToTarget(target, stunDuration, bossInterruptDuration, breakGuard, false);
    }

    private void ApplyReactionToTarget(GameObject target, float stunDuration, float bossInterruptDuration, bool breakGuard, bool isProjectile)
    {
        if (target == null) return;
        bool isBoss = target.GetComponent<EnemyBoss>() != null;

        var reactive = target.GetComponent<IParryReactive>();
        if (reactive != null)
        {
            reactive.OnParryReaction(new ParryReactionContext
            {
                isProjectile = isProjectile,
                isPerfect = true,
                breakGuard = breakGuard,
                interruptOnly = isBoss,
                duration = isBoss ? bossInterruptDuration : stunDuration,
                instigator = gameObject
            });
            EmitEnemyControlFeedback(
                target,
                breakGuard ? EnemyControlFeedbackType.GuardBreak : (isBoss ? EnemyControlFeedbackType.Stagger : EnemyControlFeedbackType.Stun),
                isBoss ? bossInterruptDuration : stunDuration,
                isBoss);
            return;
        }

        var enemyBase = target.GetComponent<EnemyBase>();
        if (enemyBase != null)
        {
            enemyBase.Stun(stunDuration);
            EmitEnemyControlFeedback(
                target,
                breakGuard ? EnemyControlFeedbackType.GuardBreak : EnemyControlFeedbackType.Stun,
                stunDuration,
                false);
        }
    }

    private GameObject ResolveProjectileOwner(GameObject source)
    {
        if (source == null) return null;

        var deflectable = source.GetComponent<IDeflectable>();
        return deflectable != null ? deflectable.SourceOwner : ResolveSourceOwner(source);
    }

    private GameObject ResolveSourceOwner(GameObject source)
    {
        if (source == null) return null;

        var hitbox = source.GetComponent<AttackHitbox>();
        if (hitbox != null && hitbox.owner != null)
            return hitbox.owner.gameObject;

        var deflectable = source.GetComponent<IDeflectable>();
        if (deflectable != null && deflectable.SourceOwner != null)
            return deflectable.SourceOwner;

        return source;
    }

    private bool IsCloseExecuteEligible(GameObject owner)
    {
        if (owner == null)
            return false;

        if (Vector2.Distance(transform.position, owner.transform.position) > closeExecuteRange)
            return false;

        if (owner.GetComponent<EnemyBoss>() != null)
            return false;

        var reactive = owner.GetComponent<IParryReactive>();
        if (reactive != null && !reactive.AllowParryExecute)
            return false;

        return owner.GetComponent<IDamageable>() != null;
    }

    private void EmitEnemyControlFeedback(GameObject target, EnemyControlFeedbackType type, float duration, bool isBoss)
    {
        if (target == null)
            return;

        OnEnemyControlFeedback?.Invoke(new EnemyControlFeedbackData
        {
            target = target,
            type = type,
            duration = duration,
            isBoss = isBoss,
            worldPosition = target.transform.position
        });
    }

    public bool IsBaseParryOnlyActive => Time.time < _baseParryOnlyEndTime;

    public void ForceBaseParryOnly(float duration)
    {
        if (duration <= 0f)
            return;

        _baseParryOnlyEndTime = Mathf.Max(_baseParryOnlyEndTime, Time.time + duration);
        parrySystem?.ForceResetForBaseParryOnly();
        ApplyToParrySystem();
    }
}

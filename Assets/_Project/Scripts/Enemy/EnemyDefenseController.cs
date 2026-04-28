using System;
using UnityEngine;

[DisallowMultipleComponent]
public class EnemyDefenseController : MonoBehaviour
{
    private static readonly EnemyDefenseSettings FallbackSettings = new EnemyDefenseSettings();

    private EnemyBase owner;
    private EnemyOverheadMeter debugMeter;
    private float currentStability;
    private float maxStability;
    private float brokenTimer;
    private float lastStabilityDamageTime = -999f;
    private bool initialized;
    private bool isGuarding;
    private bool armorActive;

    public float CurrentStability => currentStability;
    public float MaxStability => maxStability;
    public bool IsBroken { get; private set; }
    public bool IsGuarding => isGuarding || (owner != null && owner.IsDefenseGuardActive);
    public bool ArmorActive => armorActive;
    public float LastStabilityDamageTime => lastStabilityDamageTime;

    public event Action<EnemyDamageResult> OnDamageResolved;
    public event Action<EnemyDamageResult> OnBrokenStarted;
    public event Action<EnemyDamageResult> OnBrokenEnded;

    public void Initialize(EnemyBase target)
    {
        owner = target != null ? target : GetComponent<EnemyBase>();
        RefreshFromOwnerData(true);
        initialized = true;
    }

    private void Awake()
    {
        if (owner == null)
            owner = GetComponent<EnemyBase>();
    }

    private void Update()
    {
        if (!initialized && owner != null)
            Initialize(owner);

        EnemyDefenseSettings settings = GetSettings();
        if (IsBroken)
        {
            brokenTimer -= Time.deltaTime;
            if (brokenTimer <= 0f)
                EndBroken(settings);
        }
        else if (settings.stabilityRecoveryMode == EnemyStabilityRecoveryMode.DelayedRegeneration ||
                 settings.stabilityRecoveryMode == EnemyStabilityRecoveryMode.PartialAfterBrokenThenRegenerate)
        {
            if (Time.time - lastStabilityDamageTime >= Mathf.Max(0f, settings.stabilityRecoveryDelay))
            {
                currentStability = Mathf.MoveTowards(
                    currentStability,
                    maxStability,
                    Mathf.Max(0f, settings.stabilityRecoveryRate) * Time.deltaTime);
            }
        }

        RefreshDebugMeter(settings);
    }

    public void RefreshFromOwnerData(bool resetStability)
    {
        EnemyCombatClass combatClass = owner != null ? owner.CombatClass : EnemyCombatClass.Normal;
        EnemyDefenseSettings settings = GetSettings();
        float oldMax = maxStability;
        maxStability = ResolveMaxStability(settings, combatClass);
        armorActive = settings.hasArmor;

        if (resetStability || oldMax <= 0f)
            currentStability = maxStability;
        else
            currentStability = Mathf.Clamp(currentStability * (maxStability / oldMax), 0f, maxStability);
    }

    public void SetGuarding(bool active)
    {
        isGuarding = active;
    }

    public void SetArmorActive(bool active)
    {
        armorActive = active;
    }

    public EnemyDamageResult ResolveDamage(EnemyDamagePayload payload)
    {
        if (!initialized)
            Initialize(owner);

        EnemyDefenseSettings settings = GetSettings();
        EnemyCombatClass combatClass = owner != null ? owner.CombatClass : EnemyCombatClass.Normal;
        float healthDamage = Mathf.Max(0f, payload.healthDamage);
        float stabilityDamage = payload.hasExplicitStabilityDamage
            ? Mathf.Max(0f, payload.stabilityDamage)
            : healthDamage * 0.65f;

        EnemyDamageResult result = new EnemyDamageResult
        {
            payload = payload,
            targetClass = combatClass,
            specialDefenseType = settings.specialDefenseType,
            originalHealthDamage = healthDamage,
            originalStabilityDamage = stabilityDamage,
            brokenBeforeHit = IsBroken,
            currentStability = currentStability,
            maxStability = maxStability
        };

        if (healthDamage <= 0f && stabilityDamage <= 0f)
        {
            result.ignored = true;
            OnDamageResolved?.Invoke(result);
            return result;
        }

        if (IsBroken)
            healthDamage *= Mathf.Max(0f, settings.brokenDamageMultiplier);

        if (!IsBroken && settings.hasArmor && armorActive)
        {
            result.armorApplied = true;
            healthDamage *= 1f - Mathf.Clamp(settings.armorDamageReduction, 0f, 0.95f);
            stabilityDamage *= Mathf.Max(0f, settings.armorStabilityMultiplier);
        }

        if (!IsBroken && settings.hasGuard && IsGuarding && IsHitInsideGuardArc(settings, payload.hitDirection))
        {
            result.guardApplied = true;
            healthDamage *= 1f - Mathf.Clamp(settings.guardDamageReduction, 0f, 0.95f);
            stabilityDamage *= Mathf.Max(0f, settings.guardStabilityMultiplier);
        }

        stabilityDamage *= Mathf.Max(0f, settings.stabilityDamageTakenMultiplier);
        stabilityDamage *= ResolveClassStabilityDamageTakenMultiplier(combatClass);

        if (!IsBroken && stabilityDamage > 0f && maxStability > 0f)
        {
            currentStability = Mathf.Max(0f, currentStability - stabilityDamage);
            lastStabilityDamageTime = Time.time;
            result.appliedStabilityDamage = stabilityDamage;
            result.currentStability = currentStability;
            result.maxStability = maxStability;

            if (currentStability <= 0f)
            {
                result.didBreak = true;
                StartBroken(settings, combatClass, ref result);
            }
        }

        result.appliedHealthDamage = healthDamage;
        result.isBroken = IsBroken;
        result.currentStability = currentStability;
        result.maxStability = maxStability;
        result.shouldInterrupt = ShouldInterrupt(settings, result);
        result.interruptDuration = ResolveInterruptDuration(settings, result);

        OnDamageResolved?.Invoke(result);
        EmitFeedback(settings, result);
        return result;
    }

    private void StartBroken(EnemyDefenseSettings settings, EnemyCombatClass combatClass, ref EnemyDamageResult result)
    {
        IsBroken = true;
        brokenTimer = Mathf.Max(0.05f, settings.brokenDuration * ResolveClassBrokenDurationMultiplier(combatClass));
        result.interruptDuration = brokenTimer;
        result.shouldInterrupt = true;
        result.isBroken = true;
        OnBrokenStarted?.Invoke(result);
        owner?.HandleDefenseBrokenStarted(result);
    }

    private void EndBroken(EnemyDefenseSettings settings)
    {
        IsBroken = false;
        brokenTimer = 0f;

        switch (settings.stabilityRecoveryMode)
        {
            case EnemyStabilityRecoveryMode.None:
                currentStability = 0f;
                break;
            case EnemyStabilityRecoveryMode.PartialAfterBrokenThenRegenerate:
                currentStability = maxStability * Mathf.Clamp01(settings.brokenRecoveryStabilityPercent);
                break;
            default:
                currentStability = maxStability;
                break;
        }

        EnemyDamageResult result = new EnemyDamageResult
        {
            targetClass = owner != null ? owner.CombatClass : EnemyCombatClass.Normal,
            specialDefenseType = settings.specialDefenseType,
            currentStability = currentStability,
            maxStability = maxStability,
            isBroken = false
        };

        OnBrokenEnded?.Invoke(result);
        owner?.HandleDefenseBrokenEnded(result);
    }

    private EnemyDefenseSettings GetSettings()
    {
        return owner != null && owner.enemyData != null && owner.enemyData.defense != null
            ? owner.enemyData.defense
            : FallbackSettings;
    }

    private float ResolveMaxStability(EnemyDefenseSettings settings, EnemyCombatClass combatClass)
    {
        if (settings.maxStability > 0f)
            return settings.maxStability;

        float maxHealth = owner != null ? owner.MaxHealth : 100f;
        float classMultiplier = combatClass switch
        {
            EnemyCombatClass.Elite => 1.15f,
            EnemyCombatClass.MiniBoss => 1.65f,
            EnemyCombatClass.Boss => 2.25f,
            _ => 0.85f
        };

        return Mathf.Max(20f, maxHealth * classMultiplier);
    }

    private static float ResolveClassStabilityDamageTakenMultiplier(EnemyCombatClass combatClass)
    {
        return combatClass switch
        {
            EnemyCombatClass.Elite => 0.78f,
            EnemyCombatClass.MiniBoss => 0.45f,
            EnemyCombatClass.Boss => 0.25f,
            _ => 1f
        };
    }

    private static float ResolveClassBrokenDurationMultiplier(EnemyCombatClass combatClass)
    {
        return combatClass switch
        {
            EnemyCombatClass.Elite => 0.7f,
            EnemyCombatClass.MiniBoss => 0.38f,
            EnemyCombatClass.Boss => 0.22f,
            _ => 1f
        };
    }

    private bool ShouldInterrupt(EnemyDefenseSettings settings, EnemyDamageResult result)
    {
        if (result.didBreak)
            return true;

        if (settings.poiseLevel == EnemyPoiseLevel.SuperArmor)
            return false;

        if (result.payload.isFinisher || result.payload.isCritical || result.payload.isParryCounter)
            return true;

        return settings.poiseLevel == EnemyPoiseLevel.Low && settings.interruptResistance < 0.85f;
    }

    private float ResolveInterruptDuration(EnemyDefenseSettings settings, EnemyDamageResult result)
    {
        if (result.didBreak)
            return result.interruptDuration;

        float baseDuration = result.payload.isCritical ? 0.28f : 0.2f;
        float poiseMultiplier = settings.poiseLevel switch
        {
            EnemyPoiseLevel.Medium => 0.65f,
            EnemyPoiseLevel.High => 0.35f,
            EnemyPoiseLevel.SuperArmor => 0f,
            _ => 1f
        };

        return baseDuration * poiseMultiplier * (1f - Mathf.Clamp01(settings.interruptResistance));
    }

    private bool IsHitInsideGuardArc(EnemyDefenseSettings settings, Vector2 hitDirection)
    {
        if (settings.guardArcAngle >= 359f)
            return true;

        Vector2 directionFromTargetToAttacker = -hitDirection;
        if (directionFromTargetToAttacker.sqrMagnitude <= 0.001f && owner != null)
            directionFromTargetToAttacker = -owner.GetDefenseForward();

        Vector2 forward = owner != null ? owner.GetDefenseForward() : Vector2.right;
        return Vector2.Angle(forward, directionFromTargetToAttacker.normalized) <= settings.guardArcAngle * 0.5f;
    }

    private void EmitFeedback(EnemyDefenseSettings settings, EnemyDamageResult result)
    {
        if (owner == null || result.ignored)
            return;

        if (result.didBreak)
        {
            EnemyStateFeedback.EnsureFor(owner.gameObject)?.ShowState(EnemyStateFeedbackType.Broken, result.interruptDuration);
            if (DamagePopupManager.Instance != null)
                DamagePopupManager.Instance.CreateText(owner.transform.position + Vector3.up * 1.5f, "BROKEN!", new Color(1f, 0.35f, 0.1f), 7f);
        }
        else if (result.guardApplied)
        {
            EnemyStateFeedback.EnsureFor(owner.gameObject)?.ShowState(EnemyStateFeedbackType.Guard, 0.25f);
            if (DamagePopupManager.Instance != null)
                DamagePopupManager.Instance.CreateText(owner.transform.position + Vector3.up * 1.25f, "GUARD", Color.cyan, 5f);
        }
        else if (result.armorApplied)
        {
            EnemyStateFeedback.EnsureFor(owner.gameObject)?.ShowState(EnemyStateFeedbackType.Armor, 0.25f);
            if (DamagePopupManager.Instance != null)
                DamagePopupManager.Instance.CreateText(owner.transform.position + Vector3.up * 1.25f, "ARMOR", new Color(0.75f, 0.75f, 0.85f), 5f);
        }

        if (settings.logDebug)
        {
            Debug.Log(
                $"[EnemyDefense] {owner.name} class={result.targetClass} stability={result.currentStability:F1}/{result.maxStability:F1} broken={result.isBroken} armor={result.armorApplied} guard={result.guardApplied} hp={result.appliedHealthDamage:F1} stabilityDamage={result.appliedStabilityDamage:F1}");
        }
    }

    private void RefreshDebugMeter(EnemyDefenseSettings settings)
    {
        if (!settings.showDebugStabilityMeter)
        {
            if (debugMeter != null)
                debugMeter.SetVisible(false);
            return;
        }

        if (debugMeter == null)
            debugMeter = GetComponent<EnemyOverheadMeter>() ?? gameObject.AddComponent<EnemyOverheadMeter>();

        Color color = IsBroken ? new Color(1f, 0.28f, 0.12f) : new Color(0.45f, 0.85f, 1f);
        debugMeter.Configure(color, 1f, 0.06f);
        debugMeter.SetVisible(true);
        debugMeter.SetProgress(maxStability > 0f ? currentStability / maxStability : 0f);
    }
}

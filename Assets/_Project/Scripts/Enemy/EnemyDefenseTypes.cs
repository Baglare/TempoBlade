using System;
using UnityEngine;

public enum EnemyStabilityRecoveryMode
{
    None,
    DelayedRegeneration,
    FullAfterBroken,
    PartialAfterBrokenThenRegenerate
}

public enum EnemyPoiseLevel
{
    Low,
    Medium,
    High,
    SuperArmor
}

public enum EnemySpecialDefenseType
{
    None,
    BossSpecialHook,
    TempoShieldHook
}

public enum EnemyDamageSource
{
    Unknown,
    PlayerAttack,
    PlayerFinisher,
    ParryCounter,
    DashAttack,
    ProjectileDeflect,
    Skill,
    Environment
}

[Serializable]
public class EnemyDefenseSettings
{
    [Header("Stability")]
    public float maxStability = 0f;
    public EnemyStabilityRecoveryMode stabilityRecoveryMode = EnemyStabilityRecoveryMode.DelayedRegeneration;
    public float stabilityRecoveryDelay = 1.25f;
    public float stabilityRecoveryRate = 18f;
    public float brokenDuration = 0.9f;
    public float brokenDamageMultiplier = 1.2f;
    public float stabilityDamageTakenMultiplier = 1f;
    [Range(0f, 1f)] public float brokenRecoveryStabilityPercent = 1f;

    [Header("Poise / Interrupt")]
    public EnemyPoiseLevel poiseLevel = EnemyPoiseLevel.Low;
    [Range(0f, 1f)] public float interruptResistance = 0f;

    [Header("Armor")]
    public bool hasArmor = false;
    [Range(0f, 0.95f)] public float armorDamageReduction = 0.25f;
    public float armorStabilityMultiplier = 1f;

    [Header("Guard")]
    public bool hasGuard = false;
    [Range(0f, 0.95f)] public float guardDamageReduction = 0.65f;
    public float guardStabilityMultiplier = 1.25f;
    [Range(1f, 360f)] public float guardArcAngle = 150f;

    [Header("Counter / Special Hooks")]
    public bool counterEnabled = false;
    public bool hasCounterWindow = false;
    public EnemySpecialDefenseType specialDefenseType = EnemySpecialDefenseType.None;

    [Header("Debug")]
    public bool logDebug = false;
    public bool showDebugStabilityMeter = false;
}

[Serializable]
public struct EnemyDamagePayload
{
    public float healthDamage;
    public float stabilityDamage;
    public bool hasExplicitStabilityDamage;
    public EnemyDamageSource damageSource;
    public Vector2 hitDirection;
    public GameObject instigator;
    public bool isFinisher;
    public bool isParryCounter;
    public bool isDashAttack;
    public bool isCritical;
    public bool isPerfectTiming;

    public static EnemyDamagePayload FromHealthDamage(float healthDamage, GameObject instigator = null)
    {
        return new EnemyDamagePayload
        {
            healthDamage = healthDamage,
            stabilityDamage = 0f,
            hasExplicitStabilityDamage = false,
            damageSource = EnemyDamageSource.Unknown,
            hitDirection = Vector2.zero,
            instigator = instigator
        };
    }
}

[Serializable]
public struct EnemyDamageResult
{
    public EnemyDamagePayload payload;
    public EnemyCombatClass targetClass;
    public EnemySpecialDefenseType specialDefenseType;
    public float originalHealthDamage;
    public float originalStabilityDamage;
    public float appliedHealthDamage;
    public float appliedStabilityDamage;
    public float currentStability;
    public float maxStability;
    public bool armorApplied;
    public bool guardApplied;
    public bool brokenBeforeHit;
    public bool didBreak;
    public bool isBroken;
    public bool shouldInterrupt;
    public float interruptDuration;
    public bool ignored;

    public static EnemyDamageResult Ignored(EnemyDamagePayload payload, EnemyCombatClass targetClass)
    {
        return new EnemyDamageResult
        {
            payload = payload,
            targetClass = targetClass,
            originalHealthDamage = Mathf.Max(0f, payload.healthDamage),
            originalStabilityDamage = Mathf.Max(0f, payload.stabilityDamage),
            ignored = true
        };
    }
}

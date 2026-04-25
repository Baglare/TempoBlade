using System;
using UnityEngine;

[CreateAssetMenu(fileName = "New Finisher", menuName = "TempoBlade/Finisher")]
public class FinisherSO : ScriptableObject
{
    [Header("Identity")]
    public string finisherId = "finisher_default";
    public string displayName = "Default Finisher";

    [Header("Requirements")]
    public TempoManager.TempoTier requiredTempoTier = TempoManager.TempoTier.T3;
    public FinisherTempoCostMode tempoCostMode = FinisherTempoCostMode.ResetToZero;
    public bool disableTempoGainDuringFinisher = true;

    [Header("Runtime")]
    public FinisherTargetingMode targetingMode = FinisherTargetingMode.FrontArcArea;
    public FinisherExecutionMode executionMode = FinisherExecutionMode.ForwardCleave;
    public FinisherPlayerSafetyMode playerSafetyMode = FinisherPlayerSafetyMode.None;
    public FinisherMovementBehavior movementBehavior = FinisherMovementBehavior.None;
    public FinisherTimeScaleBehavior timeScaleBehavior = FinisherTimeScaleBehavior.None;
    public FinisherReturnBehavior returnBehavior = FinisherReturnBehavior.None;

    [Header("Damage / Rules")]
    public FinisherDamageProfile damageProfile = new FinisherDamageProfile();
    public FinisherEnemyClassRuleSet enemyClassRuleSet = new FinisherEnemyClassRuleSet();

    [Header("Camera / VFX")]
    public FinisherCameraVfxProfile cameraVfxProfile = new FinisherCameraVfxProfile();
}

public enum FinisherTempoCostMode
{
    ResetToZero
}

public enum FinisherTargetingMode
{
    FrontArcArea,
    ClosestInFront,
    SelfRadius
}

public enum FinisherExecutionMode
{
    ForwardCleave,
    DashThroughMultiHit,
    HeavySlamBurst
}

public enum FinisherPlayerSafetyMode
{
    None,
    InvulnerableDuringAction
}

public enum FinisherMovementBehavior
{
    None,
    StepForward,
    DashToPrimaryTarget
}

public enum FinisherTimeScaleBehavior
{
    None,
    ShortSlowMotion
}

public enum FinisherReturnBehavior
{
    None,
    ReturnToStart,
    SnapBehindPrimaryTarget
}

[Serializable]
public class FinisherDamageProfile
{
    public float damageMultiplier = 3.5f;
    public float flatBonusDamage = 0f;
    public int hitCount = 1;
    public float timeBetweenHits = 0.05f;
    public float rangeMultiplier = 1.8f;
    public float radiusBonus = 0f;
    public float attackOffsetBonus = 0.35f;
}

[Serializable]
public class FinisherEnemyClassRule
{
    public float damageMultiplier = 1f;
    [Tooltip("0 veya daha dusukse bu sinifa zarar verme ust limiti uygulanmaz.")]
    public float maxHealthPercentCap = 0f;
    public float pressureStaggerDuration = 0.35f;
    public bool allowKillingBlow = true;
}

[Serializable]
public class FinisherEnemyClassRuleSet
{
    public FinisherEnemyClassRule normal = new FinisherEnemyClassRule
    {
        damageMultiplier = 1f,
        maxHealthPercentCap = 0f,
        pressureStaggerDuration = 0.45f,
        allowKillingBlow = true
    };

    public FinisherEnemyClassRule elite = new FinisherEnemyClassRule
    {
        damageMultiplier = 0.82f,
        maxHealthPercentCap = 0.55f,
        pressureStaggerDuration = 0.55f,
        allowKillingBlow = true
    };

    public FinisherEnemyClassRule miniBoss = new FinisherEnemyClassRule
    {
        damageMultiplier = 0.52f,
        maxHealthPercentCap = 0.18f,
        pressureStaggerDuration = 0.7f,
        allowKillingBlow = false
    };

    public FinisherEnemyClassRule boss = new FinisherEnemyClassRule
    {
        damageMultiplier = 0.35f,
        maxHealthPercentCap = 0.1f,
        pressureStaggerDuration = 0.4f,
        allowKillingBlow = false
    };

    public FinisherEnemyClassRule GetRule(EnemyCombatClass combatClass)
    {
        return combatClass switch
        {
            EnemyCombatClass.Elite => elite,
            EnemyCombatClass.MiniBoss => miniBoss,
            EnemyCombatClass.Boss => boss,
            _ => normal
        };
    }
}

[Serializable]
public class FinisherCameraVfxProfile
{
    public string popupText = "FINISHER!";
    public Color popupColor = Color.magenta;
    public float cameraShakeIntensity = 8f;
    public float cameraShakeDuration = 0.25f;
    public bool useHeavyHitStop = true;
    public float slowMotionScale = 0.35f;
    public float slowMotionDuration = 0.06f;
}

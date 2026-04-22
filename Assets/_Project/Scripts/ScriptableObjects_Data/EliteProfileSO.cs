using System;
using UnityEngine;

public enum EliteMechanicType
{
    None = 0,
    CasterBurstOrb = 1,
    MeleeRendCombo = 2,
    DasherFalseExit = 3,
    AssassinShadowEcho = 4,
    TrapperTetherTrap = 5,
    DuelistGuardDebt = 6,
    DeadeyeEchoLine = 7,
    KamikazeUnstableCore = 8,
    WardenLivingDefenceWall = 9,
    ResonatorCrescendo = 10,
    WardenLinkerMultipleLink = 11
}

[Serializable]
public class CasterBurstOrbSettings
{
    public float burstOrbChance = 0.35f;
    public float impactRadius = 1.35f;
    public float impactDamageMultiplier = 0.65f;
    public int fragmentCount = 4;
    public float fragmentAngleSpread = 80f;
    public float fragmentSpeedMultiplier = 0.9f;
    public float fragmentLifetimeMultiplier = 0.5f;
    public float fragmentDamageMultiplier = 0.4f;
    public float fragmentExplosionRadius = 0.7f;
    public float fragmentExplosionDamageMultiplier = 0.35f;
    public Color burstCueColor = new Color(1f, 0.45f, 0.15f, 1f);
}

[Serializable]
public class EliteMeleeRendComboSettings
{
    public float firstHitDamageMultiplier = 0.85f;
    public float secondHitDamageMultiplier = 0.95f;
    public float thirdHitDamageMultiplier = 1.6f;
    public Vector2 firstWindupRange = new Vector2(0.22f, 0.32f);
    public Vector2 secondGapRange = new Vector2(0.22f, 0.38f);
    public Vector2 thirdGapRange = new Vector2(0.28f, 0.42f);
    public float activeFrames = 0.16f;
    public float recoveryDuration = 1.05f;
    public float hitStaggerDuration = 0.16f;
    public float hitKnockback = 6f;
    public float reactionGap = 0.1f;
    public float heavyCleaveArcMultiplier = 1.4f;
}

[Serializable]
public class EliteDasherFalseExitSettings
{
    public float firstDashDistance = 3.1f;
    public float firstDashDuration = 0.18f;
    public float falseExitArcDegrees = 120f;
    public float snapDashDuration = 0.14f;
    public float snapDashSpeed = 20f;
    public float snapHitDamageMultiplier = 1.2f;
    public float snapHitStagger = 0.24f;
    public float snapParryStun = 0.55f;
    public float exposedDuration = 1.1f;
    public float externalCounterBonus = 0.2f;
    public float thirdDashDistance = 3.5f;
    public float thirdDashDuration = 0.2f;
    public float thirdDashSectorDegrees = 110f;
}

[Serializable]
public class EliteAssassinShadowEchoSettings
{
    public float entryDashDistance = 2.8f;
    public float entryDashDuration = 0.14f;
    public float offsetDistance = 2.4f;
    public float halfBeatDelay = 0.22f;
    public float strikeRadius = 1.25f;
    public float strikeDamageMultiplier = 1.2f;
    public float heavyParryStun = 1.6f;
    public Color echoColor = new Color(0.75f, 0.4f, 1f, 0.65f);
    public Color realBodyCueColor = new Color(1f, 0.95f, 0.5f, 0.9f);
}

[Serializable]
public class EliteTrapperTetherSettings
{
    public float tetherSearchRadius = 6f;
    public float tetherLifetime = 6f;
    public float linkWindup = 0.45f;
    public float tetherCooldown = 2.6f;
    public float miniBurstDamage = 9f;
    public float miniBurstRadius = 1f;
    public float slowMultiplier = 0.7f;
    public float slowDuration = 1.3f;
    public float touchCooldown = 0.4f;
    public Color tetherColor = new Color(1f, 0.55f, 0.15f, 0.95f);
}

[Serializable]
public class EliteDuelistGuardDebtSettings
{
    public int requiredGuardHits = 3;
    public float debtDecayDelay = 10f;
    public float bashWindup = 0.28f;
    public float bashRange = 1.55f;
    public float bashDamageMultiplier = 0.55f;
    public float bashMovementLockDuration = 0.45f;
    public float bashBaseParryOnlyDuration = 0.55f;
    public float bashParryStunDuration = 1.25f;
    public float interPhaseDelay = 0.22f;
    public float cleaveWindup = 0.24f;
    public float cleaveRange = 1.95f;
    public float cleaveDamageMultiplier = 2.2f;
    public float debtCooldown = 4.4f;
    public Color debtColor = new Color(1f, 0.2f, 0.18f, 0.95f);
}

[Serializable]
public class EliteDeadeyeEchoLineSettings
{
    public float lineDuration = 2.4f;
    public float lineThickness = 0.28f;
    public float refireLockAimDuration = 0.2f;
    public float mobilityMultiplierWhileActive = 0.6f;
    public Color echoLineColor = new Color(1f, 0.45f, 0.12f, 0.75f);
}

[Serializable]
public class EliteKamikazeUnstableCoreSettings
{
    public float coreLifetime = 3.1f;
    public float earlyBreakDetonationDelay = 0.6f;
    public float fullExplosionRadius = 3.7f;
    public float fullExplosionDamageMultiplier = 1.35f;
    public float brokenExplosionRadius = 1.8f;
    public float brokenExplosionDamageMultiplier = 0.5f;
    public Color coreColor = new Color(1f, 0.35f, 0.15f, 0.95f);
}

[Serializable]
public class EliteWardenLivingWallSettings
{
    public float wallDuration = 4.5f;
    public float wallCooldown = 10f;
    public float maxProtectTargetDistance = 3.8f;
    public float wallWidth = 4.8f;
    public float arcRadius = 2.2f;
    public float arcDegrees = 120f;
    public float segmentThickness = 0.65f;
    public float segmentHeight = 1.9f;
    public float lineWidth = 0.2f;
    public float projectileGapWidth = 0.42f;
    public int gapCount = 2;
    public Color wallColor = new Color(0.35f, 0.95f, 1f, 0.85f);
}

[Serializable]
public class EliteResonatorCrescendoSettings
{
    public float actionRadius = 10f;
    public float meterPerAttack = 0.14f;
    public float meterPerDash = 0.18f;
    public float meterPerCast = 0.16f;
    public float meterPerSkill = 0.22f;
    public float meterPerSummon = 0.24f;
    public float channelDuration = 0.8f;
    public float interruptLoss = 0.45f;
    public float pulseRadius = 5.4f;
    public float pulseMoveMultiplier = 1.25f;
    public float pulseAttackMultiplier = 1.22f;
    public float pulseDuration = 3.2f;
    public float playerRhythmShock = 5f;
    public Color meterColor = new Color(1f, 0.35f, 0.8f, 0.95f);
}

[Serializable]
public class EliteWardenLinkerMultipleLinkSettings
{
    public float clusterRadius = 5.5f;
    public float heavyHitWindow = 4f;
    public float heavyHitHealthFraction = 0.16f;
    public float chainDamageFraction = 0.28f;
    public float deathStaggerDuration = 0.45f;
    public float extraLinkWindupMultiplierPerLink = 0.2f;
    public float extraLinkCooldownMultiplierPerLink = 0.3f;
    public float extraLinkDurationPenaltyPerLink = 0.1f;
    public float summonCooldownMultiplier = 1.9f;
    public Color multiLinkColor = new Color(0.35f, 1f, 0.8f, 0.95f);
}

[CreateAssetMenu(fileName = "EliteProfile", menuName = "TempoBlade/Enemy/Elite Profile")]
public class EliteProfileSO : ScriptableObject
{
    [Header("Stat Multipliers")]
    public float healthMultiplier = 1.25f;
    public float damageMultiplier = 1.12f;
    public float cooldownMultiplier = 0.9f;
    public float moveSpeedMultiplier = 1f;

    [Header("Elite Cue")]
    public Color eliteCueColor = new Color(1f, 0.55f, 0.15f, 1f);
    public GameObject eliteVfxPrefab;
    public AudioEventId eliteAudioEvent = AudioEventId.None;

    [Header("Mechanic")]
    public EliteMechanicType eliteMechanicType = EliteMechanicType.None;

    [Header("Mechanic Settings")]
    public CasterBurstOrbSettings casterBurstOrb = new CasterBurstOrbSettings();
    public EliteMeleeRendComboSettings meleeRendCombo = new EliteMeleeRendComboSettings();
    public EliteDasherFalseExitSettings dasherFalseExit = new EliteDasherFalseExitSettings();
    public EliteAssassinShadowEchoSettings assassinShadowEcho = new EliteAssassinShadowEchoSettings();
    public EliteTrapperTetherSettings trapperTetherTrap = new EliteTrapperTetherSettings();
    public EliteDuelistGuardDebtSettings duelistGuardDebt = new EliteDuelistGuardDebtSettings();
    public EliteDeadeyeEchoLineSettings deadeyeEchoLine = new EliteDeadeyeEchoLineSettings();
    public EliteKamikazeUnstableCoreSettings kamikazeUnstableCore = new EliteKamikazeUnstableCoreSettings();
    public EliteWardenLivingWallSettings wardenLivingDefenceWall = new EliteWardenLivingWallSettings();
    public EliteResonatorCrescendoSettings resonatorCrescendo = new EliteResonatorCrescendoSettings();
    public EliteWardenLinkerMultipleLinkSettings wardenLinkerMultipleLink = new EliteWardenLinkerMultipleLinkSettings();

    public bool HasMechanic(EliteMechanicType mechanicType)
    {
        return eliteMechanicType == mechanicType;
    }
}

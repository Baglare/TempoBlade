using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public enum WeaponType
{
    Unknown,
    Sword,
    DualBlades,
    Greatsword,
    Spear,
    Katana,
    Scythe,
    DemonHand
}

public enum WeaponMilestoneState
{
    Base,
    ReinforcementI,
    ReinforcementII,
    SpecializationPending,
    Specialized
}

[Serializable]
public class WeaponAttackRhythmProfile
{
    public bool overrideDefaultProfile = false;
    public string profileName = "Balanced";
    public float cooldownMultiplier = 1f;
    public float windupMultiplier = 1f;
    public float comboWindowMultiplier = 1f;
}

[Serializable]
public class WeaponRangeProfile
{
    public bool overrideDefaultProfile = false;
    public string profileName = "Standard";
    public float rangeMultiplier = 1f;
    public float attackOffsetBonus = 0f;
}

[Serializable]
public class WeaponStaggerProfile
{
    public bool overrideDefaultProfile = false;
    public string profileName = "Balanced";
    public float extraStaggerOnHit = 0f;
    public float extraStaggerOnHeavyHit = 0.08f;
    public float heavyHitThreshold = 1.5f;
    public float finisherPressureBonus = 0.1f;
}

[Serializable]
public class WeaponRecoveryProfile
{
    public bool overrideDefaultProfile = false;
    public string profileName = "Standard";
    public float recoveryMultiplier = 1f;
}

[Serializable]
public class WeaponTempoGainStyle
{
    public bool overrideDefaultProfile = false;
    public string profileName = "Balanced";
    public float tempoGainOnHit = 2f;
    public float tempoGainOnProjectileDeflect = 10f;
    public float whiffPenalty = -5f;
}

[Serializable]
public class WeaponRiskProfile
{
    public bool overrideDefaultProfile = false;
    public string profileName = "Measured";
    public float whiffPenaltyMultiplier = 1f;
    public float counterBonusMultiplier = 1f;
}

[Serializable]
public class WeaponUpgradeScalingData
{
    public bool useOverrideArrays = false;
    public float[] damagePerLevel = new float[0];
    public float[] speedPerLevel = new float[0];
    public float[] rangePerLevel = new float[0];
    public int[] upgradeCosts = new int[0];
    public float[] successRates = new float[0];
}

[Serializable]
public class WeaponMilestoneBonusData
{
    public string label = string.Empty;
    [TextArea] public string description = string.Empty;
    public float flatDamageBonus = 0f;
    public float attackRateReduction = 0f;
    public float flatRangeBonus = 0f;
    public float extraStaggerOnHit = 0f;
    public float tempoGainOnHitBonus = 0f;
    public float whiffPenaltyDelta = 0f;
    public float comboWindowMultiplierBonus = 0f;
    public float recoveryMultiplierBonus = 0f;
}

[Serializable]
public class WeaponSpecializationChoiceData
{
    public string choiceId = string.Empty;
    public string displayName = string.Empty;
    [TextArea] public string description = string.Empty;
    public float flatDamageBonus = 0f;
    public float attackRateReduction = 0f;
    public float flatRangeBonus = 0f;
    public float extraStaggerOnHit = 0f;
    public float tempoGainOnHitBonus = 0f;
    public float whiffPenaltyDelta = 0f;
    public float comboWindowMultiplierBonus = 0f;
    public float recoveryMultiplierBonus = 0f;
    public FinisherSO finisherOverride;
}

[Serializable]
public class WeaponMilestoneUpgradeData
{
    public bool useCustomMilestones = false;
    public WeaponMilestoneBonusData level3 = new WeaponMilestoneBonusData();
    public WeaponMilestoneBonusData level6 = new WeaponMilestoneBonusData();
    [FormerlySerializedAs("level9Choices")]
    public WeaponSpecializationChoiceData[] level10Choices = new WeaponSpecializationChoiceData[2];
}

public struct WeaponResolvedStats
{
    public WeaponType weaponType;
    public string weaponTypeLabel;
    public float damage;
    public float attackRate;
    public float range;
    public float attackOffset;
    public float comboWindowMultiplier;
    public float windupMultiplier;
    public float recoveryMultiplier;
    public float tempoGainOnHit;
    public float tempoGainOnProjectileDeflect;
    public float whiffPenalty;
    public float extraStaggerOnHit;
    public float extraStaggerOnHeavyHit;
    public float heavyHitThreshold;
    public float finisherPressureBonus;
    public float counterBonusMultiplier;
    public WeaponMilestoneState milestoneState;
    public string milestoneLabel;
    public string specializationId;
    public string specializationName;
}

public static class WeaponArchetypeDefaults
{
    private static readonly Dictionary<WeaponType, WeaponAttackRhythmProfile> AttackRhythmProfiles = new()
    {
        { WeaponType.Sword, new WeaponAttackRhythmProfile { profileName = "Dengeli Seri", cooldownMultiplier = 1f, windupMultiplier = 1f, comboWindowMultiplier = 1.05f } },
        { WeaponType.DualBlades, new WeaponAttackRhythmProfile { profileName = "Hizli Akis", cooldownMultiplier = 0.92f, windupMultiplier = 0.85f, comboWindowMultiplier = 1.15f } },
        { WeaponType.Greatsword, new WeaponAttackRhythmProfile { profileName = "Agir Vurgu", cooldownMultiplier = 1.08f, windupMultiplier = 1.18f, comboWindowMultiplier = 0.85f } },
        { WeaponType.Spear, new WeaponAttackRhythmProfile { profileName = "Uzun Ritm", cooldownMultiplier = 1f, windupMultiplier = 1.05f, comboWindowMultiplier = 1f } },
        { WeaponType.Katana, new WeaponAttackRhythmProfile { profileName = "Keskin Ritim", cooldownMultiplier = 0.96f, windupMultiplier = 0.92f, comboWindowMultiplier = 1.08f } },
        { WeaponType.Scythe, new WeaponAttackRhythmProfile { profileName = "Genis Savuru", cooldownMultiplier = 1.04f, windupMultiplier = 1.1f, comboWindowMultiplier = 0.95f } },
        { WeaponType.DemonHand, new WeaponAttackRhythmProfile { profileName = "Ham Baskı", cooldownMultiplier = 1.12f, windupMultiplier = 1.16f, comboWindowMultiplier = 0.82f } }
    };

    private static readonly Dictionary<WeaponType, WeaponRangeProfile> RangeProfiles = new()
    {
        { WeaponType.Sword, new WeaponRangeProfile { profileName = "Standart", rangeMultiplier = 1f, attackOffsetBonus = 0f } },
        { WeaponType.DualBlades, new WeaponRangeProfile { profileName = "Yakin", rangeMultiplier = 0.96f, attackOffsetBonus = -0.05f } },
        { WeaponType.Greatsword, new WeaponRangeProfile { profileName = "Baski", rangeMultiplier = 1.08f, attackOffsetBonus = 0.08f } },
        { WeaponType.Spear, new WeaponRangeProfile { profileName = "Uzun", rangeMultiplier = 1.18f, attackOffsetBonus = 0.14f } },
        { WeaponType.Katana, new WeaponRangeProfile { profileName = "Kesit", rangeMultiplier = 1.04f, attackOffsetBonus = 0.04f } },
        { WeaponType.Scythe, new WeaponRangeProfile { profileName = "Yaygin", rangeMultiplier = 1.1f, attackOffsetBonus = 0.1f } },
        { WeaponType.DemonHand, new WeaponRangeProfile { profileName = "Kisa Vahsi", rangeMultiplier = 0.92f, attackOffsetBonus = -0.04f } }
    };

    private static readonly Dictionary<WeaponType, WeaponStaggerProfile> StaggerProfiles = new()
    {
        { WeaponType.Sword, new WeaponStaggerProfile { profileName = "Tutarlı", extraStaggerOnHit = 0.03f, extraStaggerOnHeavyHit = 0.12f, heavyHitThreshold = 1.4f, finisherPressureBonus = 0.18f } },
        { WeaponType.DualBlades, new WeaponStaggerProfile { profileName = "Kesik Kesik", extraStaggerOnHit = 0f, extraStaggerOnHeavyHit = 0.06f, heavyHitThreshold = 1.6f, finisherPressureBonus = 0.12f } },
        { WeaponType.Greatsword, new WeaponStaggerProfile { profileName = "Kirici", extraStaggerOnHit = 0.08f, extraStaggerOnHeavyHit = 0.22f, heavyHitThreshold = 1.2f, finisherPressureBonus = 0.28f } },
        { WeaponType.Spear, new WeaponStaggerProfile { profileName = "Itici", extraStaggerOnHit = 0.04f, extraStaggerOnHeavyHit = 0.1f, heavyHitThreshold = 1.45f, finisherPressureBonus = 0.18f } },
        { WeaponType.Katana, new WeaponStaggerProfile { profileName = "Temiz Kesit", extraStaggerOnHit = 0.02f, extraStaggerOnHeavyHit = 0.08f, heavyHitThreshold = 1.45f, finisherPressureBonus = 0.16f } },
        { WeaponType.Scythe, new WeaponStaggerProfile { profileName = "Alan Kontrol", extraStaggerOnHit = 0.05f, extraStaggerOnHeavyHit = 0.14f, heavyHitThreshold = 1.35f, finisherPressureBonus = 0.22f } },
        { WeaponType.DemonHand, new WeaponStaggerProfile { profileName = "Ezici", extraStaggerOnHit = 0.06f, extraStaggerOnHeavyHit = 0.2f, heavyHitThreshold = 1.25f, finisherPressureBonus = 0.25f } }
    };

    private static readonly Dictionary<WeaponType, WeaponRecoveryProfile> RecoveryProfiles = new()
    {
        { WeaponType.Sword, new WeaponRecoveryProfile { profileName = "Dengeli", recoveryMultiplier = 1f } },
        { WeaponType.DualBlades, new WeaponRecoveryProfile { profileName = "Akici", recoveryMultiplier = 0.92f } },
        { WeaponType.Greatsword, new WeaponRecoveryProfile { profileName = "Commitli", recoveryMultiplier = 1.18f } },
        { WeaponType.Spear, new WeaponRecoveryProfile { profileName = "Olculu", recoveryMultiplier = 1.02f } },
        { WeaponType.Katana, new WeaponRecoveryProfile { profileName = "Keskin", recoveryMultiplier = 0.96f } },
        { WeaponType.Scythe, new WeaponRecoveryProfile { profileName = "Agir Savuru", recoveryMultiplier = 1.08f } },
        { WeaponType.DemonHand, new WeaponRecoveryProfile { profileName = "Vahsi", recoveryMultiplier = 1.16f } }
    };

    private static readonly Dictionary<WeaponType, WeaponTempoGainStyle> TempoStyles = new()
    {
        { WeaponType.Sword, new WeaponTempoGainStyle { profileName = "Tutarlı Kazanım", tempoGainOnHit = 2.1f, tempoGainOnProjectileDeflect = 10f, whiffPenalty = -5f } },
        { WeaponType.DualBlades, new WeaponTempoGainStyle { profileName = "Akışçı", tempoGainOnHit = 1.6f, tempoGainOnProjectileDeflect = 10f, whiffPenalty = -4f } },
        { WeaponType.Greatsword, new WeaponTempoGainStyle { profileName = "Ağır Tahsilat", tempoGainOnHit = 3f, tempoGainOnProjectileDeflect = 10f, whiffPenalty = -6f } },
        { WeaponType.Spear, new WeaponTempoGainStyle { profileName = "Dengeli Baskı", tempoGainOnHit = 2.2f, tempoGainOnProjectileDeflect = 10f, whiffPenalty = -5f } },
        { WeaponType.Katana, new WeaponTempoGainStyle { profileName = "Keskin Akış", tempoGainOnHit = 1.9f, tempoGainOnProjectileDeflect = 10f, whiffPenalty = -4.5f } },
        { WeaponType.Scythe, new WeaponTempoGainStyle { profileName = "Alan Tahsili", tempoGainOnHit = 2.4f, tempoGainOnProjectileDeflect = 10f, whiffPenalty = -5.5f } },
        { WeaponType.DemonHand, new WeaponTempoGainStyle { profileName = "Vahsi Tahsilat", tempoGainOnHit = 2.8f, tempoGainOnProjectileDeflect = 10f, whiffPenalty = -6.5f } }
    };

    private static readonly Dictionary<WeaponType, WeaponRiskProfile> RiskProfiles = new()
    {
        { WeaponType.Sword, new WeaponRiskProfile { profileName = "Orta Risk", whiffPenaltyMultiplier = 1f, counterBonusMultiplier = 1f } },
        { WeaponType.DualBlades, new WeaponRiskProfile { profileName = "Mobil Risk", whiffPenaltyMultiplier = 0.9f, counterBonusMultiplier = 0.95f } },
        { WeaponType.Greatsword, new WeaponRiskProfile { profileName = "Agir Commit", whiffPenaltyMultiplier = 1.2f, counterBonusMultiplier = 1.08f } },
        { WeaponType.Spear, new WeaponRiskProfile { profileName = "Mesafeli Risk", whiffPenaltyMultiplier = 1f, counterBonusMultiplier = 1f } },
        { WeaponType.Katana, new WeaponRiskProfile { profileName = "Keskin Commit", whiffPenaltyMultiplier = 1.05f, counterBonusMultiplier = 1.05f } },
        { WeaponType.Scythe, new WeaponRiskProfile { profileName = "Geniş Savuru", whiffPenaltyMultiplier = 1.1f, counterBonusMultiplier = 1f } },
        { WeaponType.DemonHand, new WeaponRiskProfile { profileName = "Ham Baskı", whiffPenaltyMultiplier = 1.2f, counterBonusMultiplier = 1.06f } }
    };

    private static readonly Dictionary<string, FinisherSO> FallbackFinisherCache = new();
    private static readonly Dictionary<WeaponType, WeaponMilestoneUpgradeData> MilestoneDefaults = BuildMilestoneDefaults();

    public static WeaponType ResolveWeaponType(WeaponSO weapon)
    {
        if (weapon == null)
            return WeaponType.Unknown;

        if (weapon.weaponType != WeaponType.Unknown)
            return weapon.weaponType;

        string name = weapon.weaponName?.ToLowerInvariant() ?? string.Empty;
        if (name.Contains("dagger"))
            return WeaponType.DualBlades;
        if (name.Contains("test"))
            return WeaponType.Greatsword;
        if (name.Contains("starter") || name.Contains("starting"))
            return WeaponType.Sword;

        return WeaponType.Unknown;
    }

    public static WeaponAttackRhythmProfile GetAttackRhythmProfile(WeaponSO weapon)
    {
        WeaponType type = ResolveWeaponType(weapon);
        if (weapon != null && weapon.attackRhythmProfile != null && weapon.attackRhythmProfile.overrideDefaultProfile)
            return weapon.attackRhythmProfile;

        return AttackRhythmProfiles.TryGetValue(type, out WeaponAttackRhythmProfile profile)
            ? profile
            : AttackRhythmProfiles[WeaponType.Sword];
    }

    public static WeaponRangeProfile GetRangeProfile(WeaponSO weapon)
    {
        WeaponType type = ResolveWeaponType(weapon);
        if (weapon != null && weapon.rangeProfile != null && weapon.rangeProfile.overrideDefaultProfile)
            return weapon.rangeProfile;

        return RangeProfiles.TryGetValue(type, out WeaponRangeProfile profile)
            ? profile
            : RangeProfiles[WeaponType.Sword];
    }

    public static WeaponStaggerProfile GetStaggerProfile(WeaponSO weapon)
    {
        WeaponType type = ResolveWeaponType(weapon);
        if (weapon != null && weapon.staggerProfile != null && weapon.staggerProfile.overrideDefaultProfile)
            return weapon.staggerProfile;

        return StaggerProfiles.TryGetValue(type, out WeaponStaggerProfile profile)
            ? profile
            : StaggerProfiles[WeaponType.Sword];
    }

    public static WeaponRecoveryProfile GetRecoveryProfile(WeaponSO weapon)
    {
        WeaponType type = ResolveWeaponType(weapon);
        if (weapon != null && weapon.recoveryProfile != null && weapon.recoveryProfile.overrideDefaultProfile)
            return weapon.recoveryProfile;

        return RecoveryProfiles.TryGetValue(type, out WeaponRecoveryProfile profile)
            ? profile
            : RecoveryProfiles[WeaponType.Sword];
    }

    public static WeaponTempoGainStyle GetTempoGainStyle(WeaponSO weapon)
    {
        WeaponType type = ResolveWeaponType(weapon);
        if (weapon != null && weapon.tempoGainStyle != null && weapon.tempoGainStyle.overrideDefaultProfile)
            return weapon.tempoGainStyle;

        return TempoStyles.TryGetValue(type, out WeaponTempoGainStyle style)
            ? style
            : TempoStyles[WeaponType.Sword];
    }

    public static WeaponRiskProfile GetRiskProfile(WeaponSO weapon)
    {
        WeaponType type = ResolveWeaponType(weapon);
        if (weapon != null && weapon.riskProfile != null && weapon.riskProfile.overrideDefaultProfile)
            return weapon.riskProfile;

        return RiskProfiles.TryGetValue(type, out WeaponRiskProfile profile)
            ? profile
            : RiskProfiles[WeaponType.Sword];
    }

    public static WeaponMilestoneUpgradeData GetMilestoneData(WeaponSO weapon)
    {
        WeaponType type = ResolveWeaponType(weapon);
        if (weapon != null && weapon.milestoneUpgradeData != null && weapon.milestoneUpgradeData.useCustomMilestones)
            return weapon.milestoneUpgradeData;

        return MilestoneDefaults.TryGetValue(type, out WeaponMilestoneUpgradeData data)
            ? data
            : MilestoneDefaults[WeaponType.Sword];
    }

    public static FinisherSO GetFallbackFinisher(WeaponSO weapon)
    {
        WeaponType type = ResolveWeaponType(weapon);
        string key = weapon != null && !string.IsNullOrWhiteSpace(weapon.weaponName)
            ? weapon.weaponName
            : type.ToString();

        if (FallbackFinisherCache.TryGetValue(key, out FinisherSO existing) && existing != null)
            return existing;

        FinisherSO finisher = ScriptableObject.CreateInstance<FinisherSO>();
        finisher.hideFlags = HideFlags.HideAndDontSave;
        finisher.displayName = type switch
        {
            WeaponType.DualBlades => "Threadcut Rush",
            WeaponType.Greatsword => "Ruin Slam",
            _ => "Breakline Cleave"
        };
        finisher.finisherId = "fallback_" + key.Replace(" ", "_").ToLowerInvariant();

        switch (type)
        {
            case WeaponType.DualBlades:
                finisher.targetingMode = FinisherTargetingMode.ClosestInFront;
                finisher.executionMode = FinisherExecutionMode.DashThroughMultiHit;
                finisher.playerSafetyMode = FinisherPlayerSafetyMode.InvulnerableDuringAction;
                finisher.movementBehavior = FinisherMovementBehavior.DashToPrimaryTarget;
                finisher.returnBehavior = FinisherReturnBehavior.SnapBehindPrimaryTarget;
                finisher.damageProfile.damageMultiplier = 1.05f;
                finisher.damageProfile.hitCount = 4;
                finisher.damageProfile.timeBetweenHits = 0.045f;
                finisher.damageProfile.rangeMultiplier = 2.2f;
                finisher.cameraVfxProfile.popupText = "SHRED!";
                finisher.cameraVfxProfile.popupColor = new Color(1f, 0.45f, 0.88f, 1f);
                finisher.cameraVfxProfile.cameraShakeIntensity = 6f;
                break;

            case WeaponType.Greatsword:
                finisher.targetingMode = FinisherTargetingMode.FrontArcArea;
                finisher.executionMode = FinisherExecutionMode.HeavySlamBurst;
                finisher.playerSafetyMode = FinisherPlayerSafetyMode.None;
                finisher.movementBehavior = FinisherMovementBehavior.StepForward;
                finisher.timeScaleBehavior = FinisherTimeScaleBehavior.ShortSlowMotion;
                finisher.damageProfile.damageMultiplier = 4.4f;
                finisher.damageProfile.hitCount = 1;
                finisher.damageProfile.rangeMultiplier = 1.85f;
                finisher.damageProfile.radiusBonus = 0.45f;
                finisher.damageProfile.attackOffsetBonus = 0.5f;
                finisher.cameraVfxProfile.popupText = "RUIN!";
                finisher.cameraVfxProfile.popupColor = new Color(1f, 0.55f, 0.2f, 1f);
                finisher.cameraVfxProfile.cameraShakeIntensity = 10f;
                finisher.cameraVfxProfile.cameraShakeDuration = 0.32f;
                finisher.cameraVfxProfile.slowMotionScale = 0.28f;
                finisher.cameraVfxProfile.slowMotionDuration = 0.08f;
                break;

            default:
                finisher.targetingMode = FinisherTargetingMode.FrontArcArea;
                finisher.executionMode = FinisherExecutionMode.ForwardCleave;
                finisher.playerSafetyMode = FinisherPlayerSafetyMode.InvulnerableDuringAction;
                finisher.movementBehavior = FinisherMovementBehavior.StepForward;
                finisher.returnBehavior = FinisherReturnBehavior.None;
                finisher.damageProfile.damageMultiplier = 3.2f;
                finisher.damageProfile.hitCount = 1;
                finisher.damageProfile.rangeMultiplier = 1.9f;
                finisher.cameraVfxProfile.popupText = "SEVER!";
                finisher.cameraVfxProfile.popupColor = new Color(0.95f, 0.25f, 0.55f, 1f);
                finisher.cameraVfxProfile.cameraShakeIntensity = 7f;
                break;
        }

        FallbackFinisherCache[key] = finisher;
        return finisher;
    }

    private static Dictionary<WeaponType, WeaponMilestoneUpgradeData> BuildMilestoneDefaults()
    {
        return new Dictionary<WeaponType, WeaponMilestoneUpgradeData>
        {
            [WeaponType.Sword] = new WeaponMilestoneUpgradeData
            {
                level3 = new WeaponMilestoneBonusData
                {
                    label = "Aci Aç",
                    description = "Kisa menzilli guvenli vurgu kazanir.",
                    flatRangeBonus = 0.08f,
                    tempoGainOnHitBonus = 0.2f
                },
                level6 = new WeaponMilestoneBonusData
                {
                    label = "Hat Baskisi",
                    description = "On hatta daha tutarli kontrol kurar.",
                    extraStaggerOnHit = 0.05f,
                    recoveryMultiplierBonus = -0.04f
                },
                level10Choices = new[]
                {
                    new WeaponSpecializationChoiceData
                    {
                        choiceId = "sword_vanguard",
                        displayName = "Vanguard Edge",
                        description = "Biraz daha uzun, daha sert ve daha önden baskili.",
                        flatRangeBonus = 0.14f,
                        extraStaggerOnHit = 0.05f
                    },
                    new WeaponSpecializationChoiceData
                    {
                        choiceId = "sword_rythmline",
                        displayName = "Rhythm Line",
                        description = "Akis ve recovery'yi koruyan daha tempo dostu hat.",
                        tempoGainOnHitBonus = 0.35f,
                        recoveryMultiplierBonus = -0.06f
                    }
                }
            },
            [WeaponType.DualBlades] = new WeaponMilestoneUpgradeData
            {
                level3 = new WeaponMilestoneBonusData
                {
                    label = "Akis Kesiti",
                    description = "Kombo penceresi ve hiz biraz daha rahatlar.",
                    comboWindowMultiplierBonus = 0.08f,
                    attackRateReduction = 0.015f
                },
                level6 = new WeaponMilestoneBonusData
                {
                    label = "Kanat Kesisim",
                    description = "Kisa menzilde daha verimli tempo toplar.",
                    tempoGainOnHitBonus = 0.35f,
                    flatRangeBonus = 0.04f
                },
                level10Choices = new[]
                {
                    new WeaponSpecializationChoiceData
                    {
                        choiceId = "dual_flurry",
                        displayName = "Flurry Thread",
                        description = "Daha hizli, daha akici, daha az cezali.",
                        attackRateReduction = 0.025f,
                        whiffPenaltyDelta = 0.8f
                    },
                    new WeaponSpecializationChoiceData
                    {
                        choiceId = "dual_ghoststep",
                        displayName = "Ghost Step",
                        description = "Daha uzun erisimi ve hedef ustunde kalma gucu verir.",
                        flatRangeBonus = 0.1f,
                        extraStaggerOnHit = 0.03f
                    }
                }
            },
            [WeaponType.Greatsword] = new WeaponMilestoneUpgradeData
            {
                level3 = new WeaponMilestoneBonusData
                {
                    label = "Agir Esik",
                    description = "Agir vuruslar daha net hissedilir.",
                    flatDamageBonus = 6f,
                    extraStaggerOnHit = 0.08f
                },
                level6 = new WeaponMilestoneBonusData
                {
                    label = "Yikim Hatti",
                    description = "Savurus alanini ve finisher baskisini buyutur.",
                    flatRangeBonus = 0.12f,
                    recoveryMultiplierBonus = 0.04f
                },
                level10Choices = new[]
                {
                    new WeaponSpecializationChoiceData
                    {
                        choiceId = "great_rupture",
                        displayName = "Rupture Weight",
                        description = "Daha fazla hasar ve kırıcı baski verir.",
                        flatDamageBonus = 12f,
                        extraStaggerOnHit = 0.12f
                    },
                    new WeaponSpecializationChoiceData
                    {
                        choiceId = "great_breakline",
                        displayName = "Breakline Reach",
                        description = "Menzil ve vurma çizgisini genişletir.",
                        flatRangeBonus = 0.18f,
                        comboWindowMultiplierBonus = 0.05f
                    }
                }
            }
        };
    }
}

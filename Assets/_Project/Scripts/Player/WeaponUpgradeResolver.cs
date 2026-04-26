using System.Collections.Generic;
using UnityEngine;

public static class WeaponUpgradeResolver
{
    private static readonly HashSet<string> MissingFinisherWarnings = new();

    public static WeaponResolvedStats Resolve(WeaponSO weapon, int level, string specializationId)
    {
        WeaponResolvedStats stats = new WeaponResolvedStats
        {
            weaponType = WeaponArchetypeDefaults.ResolveWeaponType(weapon),
            weaponTypeLabel = WeaponArchetypeDefaults.ResolveWeaponType(weapon).ToString()
        };

        if (weapon == null)
            return stats;

        WeaponAttackRhythmProfile rhythm = WeaponArchetypeDefaults.GetAttackRhythmProfile(weapon);
        WeaponRangeProfile rangeProfile = WeaponArchetypeDefaults.GetRangeProfile(weapon);
        WeaponStaggerProfile stagger = WeaponArchetypeDefaults.GetStaggerProfile(weapon);
        WeaponRecoveryProfile recovery = WeaponArchetypeDefaults.GetRecoveryProfile(weapon);
        WeaponTempoGainStyle tempo = WeaponArchetypeDefaults.GetTempoGainStyle(weapon);
        WeaponRiskProfile risk = WeaponArchetypeDefaults.GetRiskProfile(weapon);

        stats.damage = weapon.GetUpgradedDamage(level);
        stats.attackRate = weapon.GetUpgradedAttackRate(level);
        stats.range = weapon.GetUpgradedRange(level);
        stats.attackOffset = weapon.attackOffset;
        stats.comboWindowMultiplier = rhythm.comboWindowMultiplier;
        stats.windupMultiplier = rhythm.windupMultiplier;
        stats.recoveryMultiplier = Mathf.Max(0.15f, rhythm.cooldownMultiplier * recovery.recoveryMultiplier);
        stats.tempoGainOnHit = Mathf.Max(0f, tempo.tempoGainOnHit);
        stats.tempoGainOnProjectileDeflect = Mathf.Max(0f, tempo.tempoGainOnProjectileDeflect);
        stats.whiffPenalty = tempo.whiffPenalty * Mathf.Max(0.1f, risk.whiffPenaltyMultiplier);
        stats.extraStaggerOnHit = Mathf.Max(0f, stagger.extraStaggerOnHit);
        stats.extraStaggerOnHeavyHit = Mathf.Max(0f, stagger.extraStaggerOnHeavyHit);
        stats.heavyHitThreshold = Mathf.Max(1f, stagger.heavyHitThreshold);
        stats.finisherPressureBonus = Mathf.Max(0f, stagger.finisherPressureBonus);
        stats.counterBonusMultiplier = Mathf.Max(0.1f, risk.counterBonusMultiplier);

        stats.range *= Mathf.Max(0.2f, rangeProfile.rangeMultiplier);
        stats.attackOffset += rangeProfile.attackOffsetBonus;

        ApplyMilestoneBonuses(weapon, level, specializationId, ref stats);
        return stats;
    }

    public static WeaponMilestoneUpgradeData GetMilestones(WeaponSO weapon)
    {
        return WeaponArchetypeDefaults.GetMilestoneData(weapon);
    }

    public static WeaponSpecializationChoiceData GetSelectedSpecialization(WeaponSO weapon, string specializationId)
    {
        if (weapon == null || string.IsNullOrWhiteSpace(specializationId))
            return null;

        WeaponMilestoneUpgradeData milestones = GetMilestones(weapon);
        if (milestones?.level10Choices == null)
            return null;

        for (int i = 0; i < milestones.level10Choices.Length; i++)
        {
            WeaponSpecializationChoiceData choice = milestones.level10Choices[i];
            if (choice == null || string.IsNullOrWhiteSpace(choice.choiceId))
                continue;

            if (choice.choiceId == specializationId)
                return choice;
        }

        return null;
    }

    public static FinisherSO ResolveFinisher(WeaponSO weapon, string specializationId)
    {
        if (weapon == null)
            return null;

        WeaponSpecializationChoiceData selected = GetSelectedSpecialization(weapon, specializationId);
        if (selected != null && selected.finisherOverride != null)
            return selected.finisherOverride;

        if (weapon.finisher != null)
            return weapon.finisher;

        if (MissingFinisherWarnings.Add(weapon.weaponName))
            Debug.LogWarning($"[Finisher] {weapon.weaponName} icin finisher referansi eksik. Runtime fallback kullaniliyor.");

        return WeaponArchetypeDefaults.GetFallbackFinisher(weapon);
    }

    public static WeaponMilestoneState GetMilestoneState(int level, string specializationId)
    {
        if (level >= WeaponSO.MaxUpgradeLevel)
            return string.IsNullOrWhiteSpace(specializationId) ? WeaponMilestoneState.SpecializationPending : WeaponMilestoneState.Specialized;
        if (level >= 6)
            return WeaponMilestoneState.ReinforcementII;
        if (level >= 3)
            return WeaponMilestoneState.ReinforcementI;
        return WeaponMilestoneState.Base;
    }

    public static string GetMilestoneLabel(WeaponSO weapon, int level, string specializationId)
    {
        WeaponMilestoneUpgradeData milestones = GetMilestones(weapon);
        WeaponMilestoneState state = GetMilestoneState(level, specializationId);
        return state switch
        {
            WeaponMilestoneState.ReinforcementI => string.IsNullOrWhiteSpace(milestones?.level3?.label) ? "+3 Reinforcement" : milestones.level3.label,
            WeaponMilestoneState.ReinforcementII => string.IsNullOrWhiteSpace(milestones?.level6?.label) ? "+6 Reinforcement" : milestones.level6.label,
            WeaponMilestoneState.SpecializationPending => "+10 Specialization Bekliyor",
            WeaponMilestoneState.Specialized => GetSelectedSpecialization(weapon, specializationId)?.displayName ?? "Specialized",
            _ => "Base"
        };
    }

    private static void ApplyMilestoneBonuses(WeaponSO weapon, int level, string specializationId, ref WeaponResolvedStats stats)
    {
        WeaponMilestoneUpgradeData milestones = GetMilestones(weapon);
        if (milestones == null)
        {
            stats.milestoneState = GetMilestoneState(level, specializationId);
            stats.milestoneLabel = GetMilestoneLabel(weapon, level, specializationId);
            return;
        }

        if (level >= 3)
            ApplyBonus(milestones.level3, ref stats);

        if (level >= 6)
            ApplyBonus(milestones.level6, ref stats);

        WeaponSpecializationChoiceData selected = level >= WeaponSO.MaxUpgradeLevel ? GetSelectedSpecialization(weapon, specializationId) : null;
        if (selected != null)
            ApplySpecialization(selected, ref stats);

        stats.milestoneState = GetMilestoneState(level, specializationId);
        stats.milestoneLabel = GetMilestoneLabel(weapon, level, specializationId);
        stats.specializationId = selected != null ? selected.choiceId : string.Empty;
        stats.specializationName = selected != null ? selected.displayName : string.Empty;
    }

    private static void ApplyBonus(WeaponMilestoneBonusData bonus, ref WeaponResolvedStats stats)
    {
        if (bonus == null)
            return;

        stats.damage += bonus.flatDamageBonus;
        stats.attackRate = Mathf.Max(0.05f, stats.attackRate - bonus.attackRateReduction);
        stats.range += bonus.flatRangeBonus;
        stats.extraStaggerOnHit += bonus.extraStaggerOnHit;
        stats.tempoGainOnHit += bonus.tempoGainOnHitBonus;
        stats.whiffPenalty += bonus.whiffPenaltyDelta;
        stats.comboWindowMultiplier += bonus.comboWindowMultiplierBonus;
        stats.recoveryMultiplier = Mathf.Max(0.15f, stats.recoveryMultiplier + bonus.recoveryMultiplierBonus);
    }

    private static void ApplySpecialization(WeaponSpecializationChoiceData specialization, ref WeaponResolvedStats stats)
    {
        if (specialization == null)
            return;

        stats.damage += specialization.flatDamageBonus;
        stats.attackRate = Mathf.Max(0.05f, stats.attackRate - specialization.attackRateReduction);
        stats.range += specialization.flatRangeBonus;
        stats.extraStaggerOnHit += specialization.extraStaggerOnHit;
        stats.tempoGainOnHit += specialization.tempoGainOnHitBonus;
        stats.whiffPenalty += specialization.whiffPenaltyDelta;
        stats.comboWindowMultiplier += specialization.comboWindowMultiplierBonus;
        stats.recoveryMultiplier = Mathf.Max(0.15f, stats.recoveryMultiplier + specialization.recoveryMultiplierBonus);
    }
}

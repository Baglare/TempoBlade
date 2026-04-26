using System;
using System.Collections.Generic;
using UnityEngine;

public enum WeaponUpgradeFailureMode
{
    KeepLevel,
    DropOneLevel
}

[Serializable]
public class WeaponFailureOutcomePolicy
{
    public WeaponUpgradeFailureMode failureMode = WeaponUpgradeFailureMode.KeepLevel;
    public int downgradeStartLevel = 8;
    [Range(0f, 1f)] public float downgradeChance = 0f;
    [Range(0f, 1f)] public float resourceRefundRatio = 0f;
}

[Serializable]
public class WeaponMilestoneDefinition
{
    public int level = 0;
    public string label = string.Empty;
    [TextArea] public string description = string.Empty;
    public bool unlocksSpecialization = false;
    public bool riskTier = false;
}

[Serializable]
public class WeaponUpgradeProgressionData
{
    public bool useExtendedProgression = false;
    public ProgressionResourceCost[] additionalCosts = new ProgressionResourceCost[0];
    public bool allowSuccessBooster = false;
    public ProgressionResourceType successBoosterType = ProgressionResourceType.SuccessBooster;
    public int successBoosterAmount = 1;
    public float successBoosterFlatBonus = 0.1f;
    public WeaponFailureOutcomePolicy failurePolicy = new WeaponFailureOutcomePolicy();
    public WeaponMilestoneDefinition[] milestoneDefinitions = new WeaponMilestoneDefinition[0];
}

[Serializable]
public class WeaponProgressionEntry
{
    public string weaponName = string.Empty;
    public int highestUpgradeLevel = 0;
    public int highestMilestoneLevel = 0;
    public int failureCount = 0;
}

[Serializable]
public class WeaponProgressionState
{
    public List<WeaponProgressionEntry> entries = new List<WeaponProgressionEntry>();

    public WeaponProgressionEntry GetOrCreateEntry(string weaponName)
    {
        EnsureEntries();
        if (string.IsNullOrWhiteSpace(weaponName))
            weaponName = "Unknown Weapon";

        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].weaponName == weaponName)
                return entries[i];
        }

        WeaponProgressionEntry created = new WeaponProgressionEntry
        {
            weaponName = weaponName
        };
        entries.Add(created);
        return created;
    }

    private void EnsureEntries()
    {
        if (entries == null)
            entries = new List<WeaponProgressionEntry>();
    }
}

public class WeaponUpgradeAttempt
{
    public WeaponSO weapon;
    public int currentLevel;
    public int targetLevel;
    public int goldCost;
    public List<ProgressionResourceCost> additionalCosts = new List<ProgressionResourceCost>();
    public float successRate;
    public bool canUseSuccessBooster;
    public ProgressionResourceType successBoosterType;
    public int successBoosterAmount;
    public float successBoosterFlatBonus;
    public bool usingSuccessBooster;
    public bool canAffordBaseCosts;
    public bool canAffordWithBooster;
    public bool isMilestoneUpgrade;
    public WeaponFailureOutcomePolicy failurePolicy;
}

public class WeaponUpgradeResult
{
    public bool succeeded;
    public string message = string.Empty;
    public int previousLevel;
    public int resultingLevel;
    public float rolledValue;
    public float finalSuccessRate;
    public bool usedSuccessBooster;
    public bool levelDowngraded;
    public bool milestoneReached;
}

public static class WeaponProgressionService
{
    private static readonly WeaponMilestoneDefinition[] DefaultMilestones =
    {
        new WeaponMilestoneDefinition { level = 3, label = "+3 Reinforcement", description = "First identity reinforcement." },
        new WeaponMilestoneDefinition { level = 6, label = "+6 Reinforcement", description = "Second identity reinforcement." },
        new WeaponMilestoneDefinition { level = WeaponSO.MaxUpgradeLevel, label = "+10 Specialization", description = "Specialization threshold.", unlocksSpecialization = true, riskTier = true }
    };

    public static void SyncLegacyWeaponState(SaveData data, bool logFallback)
    {
        if (data == null)
            return;

        data.EnsureProgressionState();
        bool hadLegacyEntries = data.weaponUpgrades != null && data.weaponUpgrades.Count > 0;

        if (data.weaponUpgrades != null)
        {
            for (int i = 0; i < data.weaponUpgrades.Count; i++)
            {
                WeaponUpgradeEntry legacyEntry = data.weaponUpgrades[i];
                if (legacyEntry == null || string.IsNullOrWhiteSpace(legacyEntry.weaponName))
                    continue;

                WeaponProgressionEntry runtimeEntry = data.weaponProgressionState.GetOrCreateEntry(legacyEntry.weaponName);
                runtimeEntry.highestUpgradeLevel = Mathf.Max(runtimeEntry.highestUpgradeLevel, legacyEntry.upgradeLevel);

                IReadOnlyList<WeaponMilestoneDefinition> milestones = GetMilestones(null);
                for (int milestoneIndex = 0; milestoneIndex < milestones.Count; milestoneIndex++)
                {
                    if (legacyEntry.upgradeLevel >= milestones[milestoneIndex].level)
                        runtimeEntry.highestMilestoneLevel = Mathf.Max(runtimeEntry.highestMilestoneLevel, milestones[milestoneIndex].level);
                }
            }
        }

        if (logFallback && hadLegacyEntries)
            Debug.LogWarning("[WeaponProgression] Legacy weapon upgrade levels synchronized into extended weapon progression state.");
    }

    public static IReadOnlyList<WeaponMilestoneDefinition> GetMilestones(WeaponSO weapon)
    {
        if (weapon != null &&
            weapon.upgradeProgression != null &&
            weapon.upgradeProgression.useExtendedProgression &&
            weapon.upgradeProgression.milestoneDefinitions != null &&
            weapon.upgradeProgression.milestoneDefinitions.Length > 0)
        {
            return weapon.upgradeProgression.milestoneDefinitions;
        }

        return DefaultMilestones;
    }

    public static WeaponUpgradeAttempt BuildAttempt(WeaponSO weapon, bool useSuccessBooster = false)
    {
        WeaponUpgradeAttempt attempt = new WeaponUpgradeAttempt
        {
            weapon = weapon
        };

        if (weapon == null || SaveManager.Instance == null)
            return attempt;

        int currentLevel = SaveManager.Instance.data.GetWeaponLevel(weapon.weaponName);
        attempt.currentLevel = currentLevel;
        attempt.targetLevel = currentLevel + 1;
        attempt.goldCost = weapon.GetUpgradeCost(currentLevel);
        attempt.successRate = weapon.GetSuccessRate(currentLevel);
        attempt.failurePolicy = CloneFailurePolicy(weapon.upgradeProgression != null ? weapon.upgradeProgression.failurePolicy : null);
        attempt.isMilestoneUpgrade = IsMilestoneUpgrade(weapon, attempt.targetLevel);

        if (weapon.upgradeProgression != null && weapon.upgradeProgression.useExtendedProgression)
        {
            attempt.canUseSuccessBooster = weapon.upgradeProgression.allowSuccessBooster;
            attempt.successBoosterType = weapon.upgradeProgression.successBoosterType;
            attempt.successBoosterAmount = Mathf.Max(0, weapon.upgradeProgression.successBoosterAmount);
            attempt.successBoosterFlatBonus = Mathf.Max(0f, weapon.upgradeProgression.successBoosterFlatBonus);

            if (weapon.upgradeProgression.additionalCosts != null)
            {
                for (int i = 0; i < weapon.upgradeProgression.additionalCosts.Length; i++)
                {
                    ProgressionResourceCost source = weapon.upgradeProgression.additionalCosts[i];
                    if (source == null || source.amount <= 0)
                        continue;

                    attempt.additionalCosts.Add(new ProgressionResourceCost
                    {
                        resourceType = source.resourceType,
                        amount = source.amount
                    });
                }
            }
        }

        List<ProgressionResourceCost> baseCosts = BuildBaseCostList(attempt);
        attempt.canAffordBaseCosts = ProgressionResourceWalletService.CanAfford(baseCosts);
        attempt.usingSuccessBooster = useSuccessBooster && attempt.canUseSuccessBooster;
        attempt.canAffordWithBooster = ProgressionResourceWalletService.CanAfford(BuildCostsWithBooster(attempt));
        return attempt;
    }

    public static WeaponUpgradeResult TryUpgrade(WeaponSO weapon, bool useSuccessBooster)
    {
        WeaponUpgradeResult result = new WeaponUpgradeResult();
        WeaponUpgradeAttempt attempt = BuildAttempt(weapon, useSuccessBooster);
        result.previousLevel = attempt.currentLevel;
        result.resultingLevel = attempt.currentLevel;
        result.finalSuccessRate = attempt.successRate;
        result.usedSuccessBooster = attempt.usingSuccessBooster;

        if (weapon == null || SaveManager.Instance == null)
        {
            result.message = "Silah veya save verisi bulunamadi.";
            return result;
        }

        if (attempt.currentLevel >= WeaponSO.MaxUpgradeLevel)
        {
            result.message = "Silah zaten maksimum seviyede.";
            return result;
        }

        List<ProgressionResourceCost> costsToSpend = attempt.usingSuccessBooster
            ? BuildCostsWithBooster(attempt)
            : BuildBaseCostList(attempt);

        if (!ProgressionResourceWalletService.CanAfford(costsToSpend))
        {
            result.message = "Yeterli kaynak yok.";
            return result;
        }

        float finalSuccessRate = attempt.successRate;
        if (attempt.usingSuccessBooster)
            finalSuccessRate = Mathf.Clamp01(finalSuccessRate + attempt.successBoosterFlatBonus);

        ProgressionResourceWalletService.SpendCosts(costsToSpend, false);

        float roll = UnityEngine.Random.Range(0f, 1f);
        result.rolledValue = roll;
        result.finalSuccessRate = finalSuccessRate;

        SaveData data = SaveManager.Instance.data;
        if (roll <= finalSuccessRate)
        {
            int nextLevel = attempt.targetLevel;
            data.SetWeaponLevel(weapon.weaponName, nextLevel);
            result.succeeded = true;
            result.resultingLevel = nextLevel;
            result.milestoneReached = attempt.isMilestoneUpgrade;
            result.message = "Yukseltme basarili! +" + nextLevel;
            UpdateExtendedState(weapon.weaponName, nextLevel, true, false);
        }
        else
        {
            int levelAfterFailure = attempt.currentLevel;
            bool downgraded = ShouldDowngradeOnFailure(attempt, attempt.currentLevel);
            if (downgraded)
            {
                levelAfterFailure = Mathf.Max(0, attempt.currentLevel - 1);
                data.SetWeaponLevel(weapon.weaponName, levelAfterFailure);
            }

            ApplyFailureRefund(costsToSpend, attempt.failurePolicy);
            result.succeeded = false;
            result.resultingLevel = levelAfterFailure;
            result.levelDowngraded = downgraded;
            result.message = downgraded
                ? "Yukseltme basarisiz! Silah bir seviye dustu."
                : "Yukseltme basarisiz! Silah ayni kaldi.";
            UpdateExtendedState(weapon.weaponName, levelAfterFailure, false, downgraded);
        }

        SaveManager.Instance.Save();
        return result;
    }

    private static void UpdateExtendedState(string weaponName, int resultingLevel, bool success, bool downgraded)
    {
        if (SaveManager.Instance == null)
            return;

        SaveData data = SaveManager.Instance.data;
        data.EnsureProgressionState();
        WeaponProgressionEntry entry = data.weaponProgressionState.GetOrCreateEntry(weaponName);
        entry.highestUpgradeLevel = Mathf.Max(entry.highestUpgradeLevel, resultingLevel);

        IReadOnlyList<WeaponMilestoneDefinition> milestones = DefaultMilestones;
        for (int i = 0; i < milestones.Count; i++)
        {
            if (resultingLevel >= milestones[i].level)
                entry.highestMilestoneLevel = Mathf.Max(entry.highestMilestoneLevel, milestones[i].level);
        }

        if (!success)
            entry.failureCount++;

        if (downgraded)
            entry.highestUpgradeLevel = Mathf.Max(entry.highestUpgradeLevel, resultingLevel + 1);
    }

    private static bool IsMilestoneUpgrade(WeaponSO weapon, int targetLevel)
    {
        IReadOnlyList<WeaponMilestoneDefinition> milestones = GetMilestones(weapon);
        for (int i = 0; i < milestones.Count; i++)
        {
            if (milestones[i].level == targetLevel)
                return true;
        }

        return false;
    }

    private static WeaponFailureOutcomePolicy CloneFailurePolicy(WeaponFailureOutcomePolicy source)
    {
        if (source == null)
            return new WeaponFailureOutcomePolicy();

        return new WeaponFailureOutcomePolicy
        {
            failureMode = source.failureMode,
            downgradeStartLevel = source.downgradeStartLevel,
            downgradeChance = source.downgradeChance,
            resourceRefundRatio = source.resourceRefundRatio
        };
    }

    private static List<ProgressionResourceCost> BuildBaseCostList(WeaponUpgradeAttempt attempt)
    {
        List<ProgressionResourceCost> costs = new List<ProgressionResourceCost>();

        if (attempt.goldCost > 0)
        {
            costs.Add(new ProgressionResourceCost
            {
                resourceType = ProgressionResourceType.Gold,
                amount = attempt.goldCost
            });
        }

        if (attempt.additionalCosts != null)
        {
            for (int i = 0; i < attempt.additionalCosts.Count; i++)
            {
                ProgressionResourceCost source = attempt.additionalCosts[i];
                costs.Add(new ProgressionResourceCost
                {
                    resourceType = source.resourceType,
                    amount = source.amount
                });
            }
        }

        return costs;
    }

    private static List<ProgressionResourceCost> BuildCostsWithBooster(WeaponUpgradeAttempt attempt)
    {
        List<ProgressionResourceCost> costs = BuildBaseCostList(attempt);
        if (attempt.usingSuccessBooster && attempt.successBoosterAmount > 0)
        {
            costs.Add(new ProgressionResourceCost
            {
                resourceType = attempt.successBoosterType,
                amount = attempt.successBoosterAmount
            });
        }

        return costs;
    }

    private static bool ShouldDowngradeOnFailure(WeaponUpgradeAttempt attempt, int currentLevel)
    {
        if (attempt.failurePolicy == null)
            return false;

        if (attempt.failurePolicy.failureMode != WeaponUpgradeFailureMode.DropOneLevel)
            return false;

        if (currentLevel < Mathf.Max(0, attempt.failurePolicy.downgradeStartLevel))
            return false;

        if (attempt.failurePolicy.downgradeChance <= 0f)
            return true;

        return UnityEngine.Random.Range(0f, 1f) <= attempt.failurePolicy.downgradeChance;
    }

    private static void ApplyFailureRefund(IReadOnlyList<ProgressionResourceCost> spentCosts, WeaponFailureOutcomePolicy policy)
    {
        if (policy == null || policy.resourceRefundRatio <= 0f || spentCosts == null)
            return;

        for (int i = 0; i < spentCosts.Count; i++)
        {
            ProgressionResourceCost cost = spentCosts[i];
            if (cost == null || cost.amount <= 0)
                continue;

            int refundAmount = Mathf.FloorToInt(cost.amount * Mathf.Clamp01(policy.resourceRefundRatio));
            if (refundAmount <= 0)
                continue;

            ProgressionResourceWalletService.AddPersistentResource(cost.resourceType, refundAmount, false);
        }
    }
}

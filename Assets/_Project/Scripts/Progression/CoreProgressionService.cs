using System;
using System.Collections.Generic;
using UnityEngine;

public enum CoreProgressionCategory
{
    Vitality,
    SecondChanceRecovery,
    RewardControl,
    TempoUtility,
    RunStartTools,
    LegacyGlobalStat
}

public enum CoreLegacyUpgradeType
{
    MaxHealth,
    DamageMultiplier,
    TempoGain
}

[Serializable]
public class CoreUpgradeEntry
{
    public string upgradeId = string.Empty;
    public int level = 0;
}

[Serializable]
public class CoreProgressionState
{
    public List<CoreUpgradeEntry> entries = new List<CoreUpgradeEntry>();

    public int GetLevel(string upgradeId)
    {
        EnsureEntries();
        if (string.IsNullOrWhiteSpace(upgradeId))
            return 0;

        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].upgradeId == upgradeId)
                return Mathf.Max(0, entries[i].level);
        }

        return 0;
    }

    public void SetLevel(string upgradeId, int level)
    {
        EnsureEntries();
        if (string.IsNullOrWhiteSpace(upgradeId))
            return;

        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].upgradeId == upgradeId)
            {
                entries[i].level = Mathf.Max(0, level);
                return;
            }
        }

        entries.Add(new CoreUpgradeEntry
        {
            upgradeId = upgradeId,
            level = Mathf.Max(0, level)
        });
    }

    private void EnsureEntries()
    {
        if (entries == null)
            entries = new List<CoreUpgradeEntry>();
    }
}

[Serializable]
public class CoreUpgradeDefinition
{
    public string upgradeId = string.Empty;
    public string displayName = string.Empty;
    public string description = string.Empty;
    public CoreProgressionCategory category = CoreProgressionCategory.Vitality;
    public bool isLegacyUpgrade = true;
    public int currentLevel = 0;
    public int maxLevel = 0;
    public int nextGoldCost = 0;
}

public static class CoreProgressionService
{
    public const string VitalityLegacyUpgradeId = "core.vitality.legacy_max_health";
    public const string DamageLegacyUpgradeId = "core.legacy.damage_multiplier";
    public const string TempoLegacyUpgradeId = "core.legacy.tempo_gain";

    public static void SyncLegacyCoreState(SaveData data, bool logFallback)
    {
        if (data == null)
            return;

        data.EnsureProgressionState();
        bool hadLegacyValues = data.bonusMaxHealth > 0 || data.bonusDamageMultiplier > 0 || data.bonusTempoGain > 0;
        data.coreProgressionState.SetLevel(VitalityLegacyUpgradeId, data.bonusMaxHealth);
        data.coreProgressionState.SetLevel(DamageLegacyUpgradeId, data.bonusDamageMultiplier);
        data.coreProgressionState.SetLevel(TempoLegacyUpgradeId, data.bonusTempoGain);

        if (logFallback && hadLegacyValues)
            Debug.LogWarning("[CoreProgression] Legacy permanent upgrade fields synchronized into core progression state.");
    }

    public static List<CoreUpgradeDefinition> BuildLegacyDefinitions(UpgradeConfigSO config, SaveData data)
    {
        List<CoreUpgradeDefinition> result = new List<CoreUpgradeDefinition>();
        if (config == null || data == null)
            return result;

        result.Add(new CoreUpgradeDefinition
        {
            upgradeId = VitalityLegacyUpgradeId,
            displayName = "Vitality - Max Health",
            description = "Legacy permanent max health progression.",
            category = CoreProgressionCategory.Vitality,
            isLegacyUpgrade = true,
            currentLevel = data.bonusMaxHealth,
            maxLevel = config.healthMaxLevel,
            nextGoldCost = data.bonusMaxHealth >= config.healthMaxLevel ? 0 : config.GetCost(config.healthBaseCost, config.healthCostPerLevel, data.bonusMaxHealth)
        });

        result.Add(new CoreUpgradeDefinition
        {
            upgradeId = DamageLegacyUpgradeId,
            displayName = "Legacy Global Damage",
            description = "Prototype global damage multiplier track kept for backward compatibility.",
            category = CoreProgressionCategory.LegacyGlobalStat,
            isLegacyUpgrade = true,
            currentLevel = data.bonusDamageMultiplier,
            maxLevel = config.damageMaxLevel,
            nextGoldCost = data.bonusDamageMultiplier >= config.damageMaxLevel ? 0 : config.GetCost(config.damageBaseCost, config.damageCostPerLevel, data.bonusDamageMultiplier)
        });

        result.Add(new CoreUpgradeDefinition
        {
            upgradeId = TempoLegacyUpgradeId,
            displayName = "Legacy Tempo Utility",
            description = "Prototype global tempo gain track kept as a legacy progression branch.",
            category = CoreProgressionCategory.TempoUtility,
            isLegacyUpgrade = true,
            currentLevel = data.bonusTempoGain,
            maxLevel = config.tempoMaxLevel,
            nextGoldCost = data.bonusTempoGain >= config.tempoMaxLevel ? 0 : config.GetCost(config.tempoBaseCost, config.tempoCostPerLevel, data.bonusTempoGain)
        });

        return result;
    }

    public static bool TryPurchaseLegacyUpgrade(CoreLegacyUpgradeType type, UpgradeConfigSO config, out string failureReason)
    {
        failureReason = string.Empty;
        if (SaveManager.Instance == null || config == null)
        {
            failureReason = "Config veya SaveManager eksik.";
            return false;
        }

        SaveData data = SaveManager.Instance.data;
        int currentLevel;
        int maxLevel;
        int cost;

        switch (type)
        {
            case CoreLegacyUpgradeType.MaxHealth:
                currentLevel = data.bonusMaxHealth;
                maxLevel = config.healthMaxLevel;
                cost = config.GetCost(config.healthBaseCost, config.healthCostPerLevel, currentLevel);
                break;
            case CoreLegacyUpgradeType.DamageMultiplier:
                currentLevel = data.bonusDamageMultiplier;
                maxLevel = config.damageMaxLevel;
                cost = config.GetCost(config.damageBaseCost, config.damageCostPerLevel, currentLevel);
                break;
            case CoreLegacyUpgradeType.TempoGain:
                currentLevel = data.bonusTempoGain;
                maxLevel = config.tempoMaxLevel;
                cost = config.GetCost(config.tempoBaseCost, config.tempoCostPerLevel, currentLevel);
                break;
            default:
                failureReason = "Bilinmeyen core upgrade tipi.";
                return false;
        }

        if (currentLevel >= maxLevel)
        {
            failureReason = "Bu upgrade zaten maksimum seviyede.";
            return false;
        }

        if (!SaveManager.Instance.SpendGold(cost))
        {
            failureReason = "Yeterli gold yok.";
            return false;
        }

        switch (type)
        {
            case CoreLegacyUpgradeType.MaxHealth:
                data.bonusMaxHealth++;
                break;
            case CoreLegacyUpgradeType.DamageMultiplier:
                data.bonusDamageMultiplier++;
                break;
            case CoreLegacyUpgradeType.TempoGain:
                data.bonusTempoGain++;
                break;
        }

        SyncLegacyCoreState(data, false);
        SaveManager.Instance.Save();
        return true;
    }
}

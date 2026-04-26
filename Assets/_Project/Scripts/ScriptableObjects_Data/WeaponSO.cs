using UnityEngine;

[CreateAssetMenu(fileName = "New Weapon", menuName = "TempoBlade/Weapon")]
public class WeaponSO : ScriptableObject
{
    public const int MaxUpgradeLevel = 10;

    [Header("Identity")]
    public string weaponName;
    [TextArea] public string description;
    public Sprite icon;
    public WeaponType weaponType = WeaponType.Unknown;

    [Header("Combat Stats (Base - +0 degerleri)")]
    public float damage = 10f;
    public float attackRate = 0.5f;
    public float range = 1.5f;
    public float attackOffset = 1.0f;

    [Header("Combo Sequence")]
    [Tooltip("Kombo adimlari. Bos birakilirsa tek vurus olarak calisir.")]
    public ComboStepData[] comboSteps;

    [Header("Tempo Identity")]
    public TempoIdentity tempoIdentity;

    [Header("Weapon Profiles")]
    public WeaponAttackRhythmProfile attackRhythmProfile = new WeaponAttackRhythmProfile();
    public WeaponRangeProfile rangeProfile = new WeaponRangeProfile();
    public WeaponStaggerProfile staggerProfile = new WeaponStaggerProfile();
    public WeaponRecoveryProfile recoveryProfile = new WeaponRecoveryProfile();
    public WeaponTempoGainStyle tempoGainStyle = new WeaponTempoGainStyle();
    public WeaponRiskProfile riskProfile = new WeaponRiskProfile();

    [Header("Economy")]
    [Tooltip("Bu silahin Hub dukkanindaki altin fiyati. 0 ise satilamaz.")]
    public int price = 200;

    [Header("Legacy Upgrade Config (+1 -> +10)")]
    [Tooltip("Her seviye icin hasar artisi. 10 elemanli dizi: [0]=+1, [1]=+2, ... [9]=+10")]
    public float[] damagePerLevel = new float[10] { 1, 1, 2, 2, 3, 3, 4, 4, 5, 6 };

    [Tooltip("Her seviye icin saldiri hizi iyilesmesi (attackRate azalir). 10 elemanli.")]
    public float[] speedPerLevel = new float[10] { 0.01f, 0.01f, 0.02f, 0.02f, 0.03f, 0.03f, 0.04f, 0.04f, 0.05f, 0.05f };

    [Tooltip("Her seviye icin menzil artisi. 10 elemanli.")]
    public float[] rangePerLevel = new float[10] { 0, 0, 0.1f, 0, 0, 0.1f, 0, 0, 0.2f, 0.1f };

    [Tooltip("Her seviye icin yukseltme maliyeti (gold). 10 elemanli.")]
    public int[] upgradeCosts = new int[10] { 50, 75, 100, 150, 200, 300, 500, 750, 1000, 1500 };

    [Tooltip("Her seviye icin basari orani (0.0 - 1.0). 10 elemanli. 1.0 = %100")]
    public float[] successRates = new float[10] { 1f, 0.95f, 0.85f, 0.7f, 0.55f, 0.4f, 0.25f, 0.15f, 0.05f, 0.03f };

    [Header("Extended Upgrade Data")]
    public WeaponUpgradeScalingData upgradeScalingData = new WeaponUpgradeScalingData();
    public WeaponMilestoneUpgradeData milestoneUpgradeData = new WeaponMilestoneUpgradeData();
    public WeaponUpgradeProgressionData upgradeProgression = new WeaponUpgradeProgressionData();

    [Header("Finisher")]
    public FinisherSO finisher;

    public float GetUpgradedDamage(int level)
    {
        if (upgradeScalingData != null && upgradeScalingData.useOverrideArrays && upgradeScalingData.damagePerLevel != null && upgradeScalingData.damagePerLevel.Length > 0)
        {
            float totalOverride = damage;
            for (int i = 0; i < Mathf.Min(level, MaxUpgradeLevel); i++)
                totalOverride += GetFloatUpgradeValue(upgradeScalingData.damagePerLevel, i, 0f);
            return totalOverride;
        }

        float total = damage;
        for (int i = 0; i < Mathf.Min(level, MaxUpgradeLevel); i++)
            total += GetFloatUpgradeValue(damagePerLevel, i, 0f);
        return total;
    }

    public float GetUpgradedAttackRate(int level)
    {
        if (upgradeScalingData != null && upgradeScalingData.useOverrideArrays && upgradeScalingData.speedPerLevel != null && upgradeScalingData.speedPerLevel.Length > 0)
        {
            float totalOverride = attackRate;
            for (int i = 0; i < Mathf.Min(level, MaxUpgradeLevel); i++)
                totalOverride -= GetFloatUpgradeValue(upgradeScalingData.speedPerLevel, i, 0f);
            return Mathf.Max(0.05f, totalOverride);
        }

        float total = attackRate;
        for (int i = 0; i < Mathf.Min(level, MaxUpgradeLevel); i++)
            total -= GetFloatUpgradeValue(speedPerLevel, i, 0f);
        return Mathf.Max(0.05f, total);
    }

    public float GetUpgradedRange(int level)
    {
        if (upgradeScalingData != null && upgradeScalingData.useOverrideArrays && upgradeScalingData.rangePerLevel != null && upgradeScalingData.rangePerLevel.Length > 0)
        {
            float totalOverride = range;
            for (int i = 0; i < Mathf.Min(level, MaxUpgradeLevel); i++)
                totalOverride += GetFloatUpgradeValue(upgradeScalingData.rangePerLevel, i, 0f);
            return totalOverride;
        }

        float total = range;
        for (int i = 0; i < Mathf.Min(level, MaxUpgradeLevel); i++)
            total += GetFloatUpgradeValue(rangePerLevel, i, 0f);
        return total;
    }

    public int GetUpgradeCost(int currentLevel)
    {
        if (currentLevel >= MaxUpgradeLevel)
            return -1;

        if (upgradeScalingData != null && upgradeScalingData.useOverrideArrays && upgradeScalingData.upgradeCosts != null && upgradeScalingData.upgradeCosts.Length > 0)
        {
            if (currentLevel < upgradeScalingData.upgradeCosts.Length)
                return upgradeScalingData.upgradeCosts[currentLevel];
            int last = upgradeScalingData.upgradeCosts[upgradeScalingData.upgradeCosts.Length - 1];
            return last + 500;
        }

        if (currentLevel < upgradeCosts.Length)
            return upgradeCosts[currentLevel];

        int fallback = upgradeCosts.Length > 0 ? upgradeCosts[upgradeCosts.Length - 1] : 1000;
        return fallback + 500;
    }

    public float GetSuccessRate(int currentLevel)
    {
        if (currentLevel >= MaxUpgradeLevel)
            return 0f;

        if (upgradeScalingData != null && upgradeScalingData.useOverrideArrays && upgradeScalingData.successRates != null && upgradeScalingData.successRates.Length > 0)
        {
            if (currentLevel < upgradeScalingData.successRates.Length)
                return upgradeScalingData.successRates[currentLevel];
            float last = upgradeScalingData.successRates[upgradeScalingData.successRates.Length - 1];
            return Mathf.Max(0.01f, last - 0.02f);
        }

        if (currentLevel < successRates.Length)
            return successRates[currentLevel];
        float fallback = successRates.Length > 0 ? successRates[successRates.Length - 1] : 0.05f;
        return Mathf.Max(0.01f, fallback - 0.02f);
    }

    public string GetDisplayName(int level)
    {
        if (level <= 0)
            return weaponName;

        return weaponName + " +" + level;
    }

    private static float GetFloatUpgradeValue(float[] source, int index, float fallback)
    {
        if (source == null || source.Length == 0)
            return fallback;

        if (index < source.Length)
            return source[index];

        return source[source.Length - 1];
    }
}

public enum TempoIdentity
{
    Balanced,
    Rusher,
    Heavy
}

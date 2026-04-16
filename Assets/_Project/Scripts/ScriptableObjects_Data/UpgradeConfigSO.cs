using UnityEngine;

/// <summary>
/// Hub dukkanindaki tum yukseltmelerin maliyet, artis miktari ve seviye basina
/// maliyet artisini Inspector'dan ayarlamak icin ScriptableObject.
/// 
/// Kullanim: Assets > Create > TempoBlade > UpgradeConfig ile yeni bir asset olustur.
/// ShopUI'daki "upgradeConfig" alanina surekle.
/// </summary>
[CreateAssetMenu(fileName = "UpgradeConfig", menuName = "TempoBlade/UpgradeConfig")]
public class UpgradeConfigSO : ScriptableObject
{
    [Header("Can Yukseltmesi (Max Health)")]
    [Tooltip("Yukseltme basina eklenen can miktari")]
    public float healthPerLevel = 10f;
    [Tooltip("Ilk yukseltmenin altin maliyeti")]
    public int healthBaseCost = 50;
    [Tooltip("Her seviyede maliyete eklenen ekstra altin")]
    public int healthCostPerLevel = 25;
    [Tooltip("Maksimum yukseltme seviyesi")]
    public int healthMaxLevel = 50;

    [Header("Hasar Carpani (Damage Multiplier)")]
    [Tooltip("Yukseltme basina eklenen carpan (0.1 = +%10)")]
    public float damageMultiplierPerLevel = 0.1f;
    [Tooltip("Ilk yukseltmenin altin maliyeti")]
    public int damageBaseCost = 75;
    [Tooltip("Her seviyede maliyete eklenen ekstra altin")]
    public int damageCostPerLevel = 35;
    [Tooltip("Maksimum yukseltme seviyesi")]
    public int damageMaxLevel = 10;

    [Header("Tempo Kazanimi (Tempo Gain Multiplier)")]
    [Tooltip("Yukseltme basina eklenen tempo carpani (0.1 = +%10)")]
    public float tempoGainPerLevel = 0.1f;
    [Tooltip("Ilk yukseltmenin altin maliyeti")]
    public int tempoBaseCost = 100;
    [Tooltip("Her seviyede maliyete eklenen ekstra altin")]
    public int tempoCostPerLevel = 50;
    [Tooltip("Maksimum yukseltme seviyesi")]
    public int tempoMaxLevel = 15;

    // ===================== MALIYET HESAPLAMA =====================

    public int GetCost(int baseCost, int costPerLevel, int currentLevel)
    {
        return baseCost + (costPerLevel * currentLevel);
    }

    public float GetMaxHealth(int bonusLevel, float baseValue = 100f)
    {
        return baseValue + (Mathf.Max(0, bonusLevel) * healthPerLevel);
    }

    public float GetDamageMultiplier(int bonusLevel, float baseValue = 1f)
    {
        return baseValue + (Mathf.Max(0, bonusLevel) * damageMultiplierPerLevel);
    }

    public float GetTempoGainMultiplier(int bonusLevel, float baseValue = 1f)
    {
        return baseValue + (Mathf.Max(0, bonusLevel) * tempoGainPerLevel);
    }

    /// <summary>
    /// Unicode blok karakterleriyle ilerleme çubuğu üretir.
    /// Örn: ████░░░░░░  (4/10)
    /// </summary>
    public static string BuildProgressBar(int current, int max, int barLength = 10)
    {
        if (max <= 0) return "";
        int filled = Mathf.RoundToInt((float)current / max * barLength);
        filled = Mathf.Clamp(filled, 0, barLength);
        return new string('█', filled) + new string('░', barLength - filled);
    }
}

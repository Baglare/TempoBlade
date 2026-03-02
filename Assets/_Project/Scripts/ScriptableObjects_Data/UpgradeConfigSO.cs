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

    [Header("Parry Penceresi (Parry Window)")]
    [Tooltip("ParrySystem'daki baslangic pencere suresi (Inspector ile esit olmali)")]
    public float baseParryWindow = 0.15f;
    [Tooltip("Yukseltme basina parry pencere suresi artisi (saniye, orn: 0.02)")]
    public float parryWindowPerLevel = 0.02f;
    [Tooltip("Ilk yukseltmenin altin maliyeti")]
    public int parryWindowBaseCost = 80;
    [Tooltip("Her seviyede maliyete eklenen ekstra altin")]
    public int parryWindowCostPerLevel = 40;
    [Tooltip("Maksimum yukseltme seviyesi")]
    public int parryWindowMaxLevel = 20;

    [Header("Parry Yenilenme Suresi (Parry Recovery)")]
    [Tooltip("ParrySystem'daki baslangic yenilenme suresi (Inspector ile esit olmali)")]
    public float baseParryRecovery = 0.5f;
    [Tooltip("Yukseltme basina parry recovery azalmasi (saniye, orn: 0.01)")]
    public float parryRecoveryPerLevel = 0.01f;
    [Tooltip("Ilk yukseltmenin altin maliyeti")]
    public int parryRecoveryBaseCost = 60;
    [Tooltip("Her seviyede maliyete eklenen ekstra altin")]
    public int parryRecoveryCostPerLevel = 30;
    [Tooltip("Maksimum yukseltme seviyesi")]
    public int parryRecoveryMaxLevel = 10;

    // ===================== MALIYET HESAPLAMA =====================

    public int GetCost(int baseCost, int costPerLevel, int currentLevel)
    {
        return baseCost + (costPerLevel * currentLevel);
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

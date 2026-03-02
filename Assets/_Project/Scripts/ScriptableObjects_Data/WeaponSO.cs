using UnityEngine;

[CreateAssetMenu(fileName = "New Weapon", menuName = "TempoBlade/Weapon")]
public class WeaponSO : ScriptableObject
{
    [Header("Identity")]
    public string weaponName;
    [TextArea] public string description;
    public Sprite icon;

    [Header("Combat Stats (Base — +0 degerleri)")]
    public float damage = 10f;
    public float attackRate = 0.5f;
    public float range = 1.5f;
    public float attackOffset = 1.0f;

    [Header("Combo Sequence")]
    [Tooltip("Kombo adımları. Boş bırakılırsa tek vuruş olarak çalışır (geriye dönük uyumlu).")]
    public ComboStepData[] comboSteps;

    [Header("Tempo Identity")]
    public TempoIdentity tempoIdentity;

    [Header("Economy")]
    [Tooltip("Bu silahin Hub dukkanindaki altin fiyati. 0 ise satilamaz.")]
    public int price = 200;

    [Header("Upgrade Config (+1 → +9)")]
    [Tooltip("Her seviye icin hasar artisi. 9 elemanli dizi: [0]=+1, [1]=+2, ... [8]=+9")]
    public float[] damagePerLevel = new float[9] { 1, 1, 2, 2, 3, 3, 4, 4, 5 };

    [Tooltip("Her seviye icin saldiri hizi iyilesmesi (attackRate azalir). 9 elemanli.")]
    public float[] speedPerLevel = new float[9] { 0.01f, 0.01f, 0.02f, 0.02f, 0.03f, 0.03f, 0.04f, 0.04f, 0.05f };

    [Tooltip("Her seviye icin menzil artisi. 9 elemanli.")]
    public float[] rangePerLevel = new float[9] { 0, 0, 0.1f, 0, 0, 0.1f, 0, 0, 0.2f };

    [Tooltip("Her seviye icin yukseltme maliyeti (gold). 9 elemanli.")]
    public int[] upgradeCosts = new int[9] { 50, 75, 100, 150, 200, 300, 500, 750, 1000 };

    [Tooltip("Her seviye icin basari orani (0.0 - 1.0). 9 elemanli. 1.0 = %100")]
    public float[] successRates = new float[9] { 1f, 0.95f, 0.85f, 0.7f, 0.55f, 0.4f, 0.25f, 0.15f, 0.05f };

    // ===================== HELPER METODLAR =====================

    /// <summary>
    /// Belirtilen yukseltme seviyesindeki toplam hasar (base + tum upgrade bonuslari).
    /// </summary>
    public float GetUpgradedDamage(int level)
    {
        float total = damage;
        for (int i = 0; i < Mathf.Min(level, 9); i++)
        {
            if (i < damagePerLevel.Length)
                total += damagePerLevel[i];
        }
        return total;
    }

    /// <summary>
    /// Belirtilen yukseltme seviyesindeki saldiri hizi (base - tum upgrade bonuslari).
    /// Daha dusuk = daha hizli.
    /// </summary>
    public float GetUpgradedAttackRate(int level)
    {
        float total = attackRate;
        for (int i = 0; i < Mathf.Min(level, 9); i++)
        {
            if (i < speedPerLevel.Length)
                total -= speedPerLevel[i];
        }
        return Mathf.Max(0.05f, total); // Minimum 0.05s
    }

    /// <summary>
    /// Belirtilen yukseltme seviyesindeki menzil (base + tum upgrade bonuslari).
    /// </summary>
    public float GetUpgradedRange(int level)
    {
        float total = range;
        for (int i = 0; i < Mathf.Min(level, 9); i++)
        {
            if (i < rangePerLevel.Length)
                total += rangePerLevel[i];
        }
        return total;
    }

    /// <summary>
    /// Sonraki seviyeye yukseltme maliyetini dondurur. Max seviyedeyse -1.
    /// </summary>
    public int GetUpgradeCost(int currentLevel)
    {
        if (currentLevel >= 9) return -1;
        if (currentLevel < upgradeCosts.Length)
            return upgradeCosts[currentLevel];
        return 9999;
    }

    /// <summary>
    /// Sonraki seviyeye yukseltme basari oranini dondurur (0.0-1.0).
    /// </summary>
    public float GetSuccessRate(int currentLevel)
    {
        if (currentLevel >= 9) return 0f;
        if (currentLevel < successRates.Length)
            return successRates[currentLevel];
        return 0.05f;
    }

    /// <summary>
    /// Silah adini seviye ile birlikte dondurur. Orn: "Katana +3"
    /// </summary>
    public string GetDisplayName(int level)
    {
        if (level <= 0) return weaponName;
        return weaponName + " +" + level;
    }
}

public enum TempoIdentity
{
    Balanced,
    Rusher,
    Heavy
}

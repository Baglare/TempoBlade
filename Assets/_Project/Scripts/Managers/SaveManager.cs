using UnityEngine;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// Kalici verileri (altin, acik silahlar, yukseltmeler) JSON olarak diske kaydeder ve okur.
/// DontDestroyOnLoad Singleton. Application.persistentDataPath icerisine "save.json" yazar.
/// </summary>
public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    private const string FOLDER_NAME = "TempoBlade";
    private const string SAVE_FILE_NAME = "save.json";

    [Header("Current Save Data (Runtime)")]
    public SaveData data = new SaveData();

    // Kayit dosyasini "Belgelerim/TempoBlade/save.json" yapalim ki bulmasi kolay olsun
    private string SaveDirectory => Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments), FOLDER_NAME);
    private string SaveFilePath => Path.Combine(SaveDirectory, SAVE_FILE_NAME);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);

        // Klasor yoksa olustur
        if (!Directory.Exists(SaveDirectory))
        {
            Directory.CreateDirectory(SaveDirectory);
        }

        Load(); // Oyun acildiginda mevcut kaydi oku
    }

    // ===================== SAVE =====================
    public void Save()
    {
        string json = JsonUtility.ToJson(data, true); // prettyPrint = true
        File.WriteAllText(SaveFilePath, json);
    }

    // ===================== LOAD =====================
    public void Load()
    {
        if (File.Exists(SaveFilePath))
        {
            string json = File.ReadAllText(SaveFilePath);
            data = JsonUtility.FromJson<SaveData>(json);
        }
        else
        {
            data = new SaveData(); // Ilk kez oynaniyor, bos veri olustur
        }
    }

    // ===================== DELETE =====================
    public void DeleteSave()
    {
        if (File.Exists(SaveFilePath))
        {
            File.Delete(SaveFilePath);
        }
        data = new SaveData();
    }

    // ===================== HELPERS =====================
    public void AddGold(int amount)
    {
        data.totalGold += amount;
        Save();
    }

    public bool SpendGold(int amount)
    {
        if (data.totalGold >= amount)
        {
            data.totalGold -= amount;
            Save();
            return true;
        }
        Debug.LogWarning("[SaveManager] Not enough gold! Have: " + data.totalGold + ", Need: " + amount);
        return false;
    }

    public bool IsWeaponUnlocked(string weaponName)
    {
        return data.unlockedWeapons.Contains(weaponName);
    }

    public void UnlockWeapon(string weaponName)
    {
        if (!data.unlockedWeapons.Contains(weaponName))
        {
            data.unlockedWeapons.Add(weaponName);
            Save();
        }
    }
}

/// <summary>
/// Silah yukseltme verisi (silah adi + seviye).
/// </summary>
[System.Serializable]
public class WeaponUpgradeEntry
{
    public string weaponName;
    public int upgradeLevel; // 0 = +0, 1 = +1, ..., 9 = +9
}

/// <summary>
/// Diske kaydedilecek tum kalici veri yapisi.
/// </summary>
[System.Serializable]

public class SaveData
{
    public int totalGold = 0;
    public int totalRunsPlayed = 0;
    public int bestRoomReached = 0;

    // Kalici yukseltme seviyeleri
    public int bonusMaxHealth = 0;
    public int bonusDamageMultiplier = 0;
    public int bonusTempoGain = 0;
    public int bonusParryWindow = 0;
    public int bonusParryRecovery = 0;

    // Acilmis silahlar
    public List<string> unlockedWeapons = new List<string> { "Starting Weapon" };

    // Kusanilan silah
    public string equippedWeaponName = "Starting Weapon";

    // Silah yukseltme seviyeleri (her silah icin ayri)
    public List<WeaponUpgradeEntry> weaponUpgrades = new List<WeaponUpgradeEntry>();

    /// <summary>
    /// Belirtilen silahın yükseltme seviyesini döndürür (0-9).
    /// </summary>
    public int GetWeaponLevel(string weaponName)
    {
        foreach (var entry in weaponUpgrades)
        {
            if (entry.weaponName == weaponName)
                return entry.upgradeLevel;
        }
        return 0;
    }

    /// <summary>
    /// Belirtilen silahın yükseltme seviyesini ayarlar.
    /// </summary>
    public void SetWeaponLevel(string weaponName, int level)
    {
        foreach (var entry in weaponUpgrades)
        {
            if (entry.weaponName == weaponName)
            {
                entry.upgradeLevel = level;
                return;
            }
        }
        // Yeni entry olustur
        weaponUpgrades.Add(new WeaponUpgradeEntry { weaponName = weaponName, upgradeLevel = level });
    }
}

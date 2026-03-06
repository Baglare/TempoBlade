using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// Demirci paneli — Hub'daki interactable ile acilir.
/// Sahip olunan silahlar listelenir, secilen silah +1 → +9 arasi yukseltilebilir.
/// Her seviyenin artisi, maliyeti ve basari orani WeaponSO'dan okunur.
/// </summary>
public class BlacksmithUI : MonoBehaviour
{
    [Header("Silah Listesi (Sol Panel)")]
    [Tooltip("Merkezi silah veritabani (tek SO). Project > Create > TempoBlade > Weapon Database")]
    public WeaponDatabase weaponDatabase;
    [Tooltip("Silah isim butonlarinin olusturulacagi parent")]
    public Transform weaponListParent;
    [Tooltip("Basit text buton prefab'i")]
    public GameObject weaponButtonPrefab;

    [Header("Yukseltme Detay (Sag Panel)")]
    [Tooltip("Silah adi + seviye")]
    public TextMeshProUGUI weaponNameText;
    [Tooltip("Mevcut statlar")]
    public TextMeshProUGUI currentStatsText;
    [Tooltip("Sonraki seviye statlari")]
    public TextMeshProUGUI nextStatsText;
    [Tooltip("Yukseltme maliyeti")]
    public TextMeshProUGUI costText;
    [Tooltip("Basari orani")]
    public TextMeshProUGUI successRateText;
    [Tooltip("Sonuc mesaji (basarili/basarisiz)")]
    public TextMeshProUGUI resultText;
    [Tooltip("Yukselt butonu")]
    public Button upgradeButton;
    [Tooltip("Yukselt butonunun text'i")]
    public TextMeshProUGUI upgradeButtonText;

    [Header("Gold Display")]
    public TextMeshProUGUI goldText;

    // Secili silah
    private WeaponSO selectedWeapon;

    private void OnEnable()
    {
        RefreshAll();
    }

    private void Update()
    {
        // ESC ile demirciyi kapat
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            CloseBlacksmith();
        }
    }

    public void RefreshAll()
    {
        RefreshGold();
        RefreshWeaponList();

        if (selectedWeapon != null)
            ShowUpgradeDetail(selectedWeapon);
        else
            ClearDetail();
    }

    private void RefreshGold()
    {
        if (goldText != null && SaveManager.Instance != null)
            goldText.text = "Altın: " + SaveManager.Instance.data.totalGold;
    }

    /// <summary>
    /// Sahip olunan silahlari listeler.
    /// </summary>
    private void RefreshWeaponList()
    {
        if (weaponListParent == null || weaponButtonPrefab == null || weaponDatabase == null) return;
        if (SaveManager.Instance == null) return;

        foreach (Transform child in weaponListParent)
            Destroy(child.gameObject);

        foreach (WeaponSO weapon in weaponDatabase.weapons)
        {
            if (weapon == null) continue;
            if (!SaveManager.Instance.IsWeaponUnlocked(weapon.weaponName)) continue;

            int level = SaveManager.Instance.data.GetWeaponLevel(weapon.weaponName);

            GameObject btnObj = Instantiate(weaponButtonPrefab, weaponListParent);
            Button btn = btnObj.GetComponent<Button>();
            TextMeshProUGUI btnText = btnObj.GetComponentInChildren<TextMeshProUGUI>();

            if (btnText != null)
                btnText.text = weapon.GetDisplayName(level);

            if (btn != null)
            {
                WeaponSO wpn = weapon;
                btn.onClick.AddListener(() => ShowUpgradeDetail(wpn));
            }
        }
    }

    /// <summary>
    /// Secilen silah icin yukseltme detayini gosterir.
    /// </summary>
    private void ShowUpgradeDetail(WeaponSO weapon)
    {
        if (weapon == null || SaveManager.Instance == null) return;
        selectedWeapon = weapon;

        int level = SaveManager.Instance.data.GetWeaponLevel(weapon.weaponName);
        bool isMaxLevel = level >= 9;

        // Silah adi + seviye
        if (weaponNameText != null)
            weaponNameText.text = weapon.GetDisplayName(level);

        // Mevcut statlar
        if (currentStatsText != null)
        {
            currentStatsText.text =
                "Hasar: " + weapon.GetUpgradedDamage(level).ToString("F1")
                + "\nSaldırı Hızı: " + weapon.GetUpgradedAttackRate(level).ToString("F2")
                + "\nMenzil: " + weapon.GetUpgradedRange(level).ToString("F1");
        }

        // Sonraki seviye statlari
        if (nextStatsText != null)
        {
            if (isMaxLevel)
            {
                nextStatsText.text = "Maksimum Seviye!";
            }
            else
            {
                int nextLevel = level + 1;
                nextStatsText.text = "+" + nextLevel + " Sonrası:"
                    + "\nHasar: " + weapon.GetUpgradedDamage(nextLevel).ToString("F1")
                    + "\nSaldırı Hızı: " + weapon.GetUpgradedAttackRate(nextLevel).ToString("F2")
                    + "\nMenzil: " + weapon.GetUpgradedRange(nextLevel).ToString("F1");
            }
        }

        // Maliyet
        if (costText != null)
        {
            if (isMaxLevel)
                costText.text = "";
            else
                costText.text = "Maliyet: " + weapon.GetUpgradeCost(level) + "g";
        }

        // Basari orani
        if (successRateText != null)
        {
            if (isMaxLevel)
                successRateText.text = "";
            else
                successRateText.text = "Başarı: %" + Mathf.RoundToInt(weapon.GetSuccessRate(level) * 100);
        }

        // Sonuc temizle
        if (resultText != null)
            resultText.text = "";

        // Buton
        if (upgradeButton != null)
        {
            upgradeButton.onClick.RemoveAllListeners();

            if (isMaxLevel)
            {
                if (upgradeButtonText != null)
                    upgradeButtonText.text = "Maksimum Seviye";
                upgradeButton.interactable = false;
            }
            else
            {
                if (upgradeButtonText != null)
                    upgradeButtonText.text = "Yükselt (+" + (level + 1) + ")";
                upgradeButton.interactable = true;

                WeaponSO wpn = weapon;
                upgradeButton.onClick.AddListener(() => AttemptUpgrade(wpn));
            }
        }
    }

    /// <summary>
    /// Yukseltme denemesi — basari oranina gore sonuc belirlenir.
    /// Basarisiz olursa para gider, silah yukselmez.
    /// </summary>
    private void AttemptUpgrade(WeaponSO weapon)
    {
        if (weapon == null || SaveManager.Instance == null) return;

        int level = SaveManager.Instance.data.GetWeaponLevel(weapon.weaponName);
        if (level >= 9) return;

        int cost = weapon.GetUpgradeCost(level);
        float successRate = weapon.GetSuccessRate(level);

        // Yeterli altin var mi?
        if (SaveManager.Instance.data.totalGold < cost)
        {
            if (resultText != null)
                resultText.text = "<color=red>Yeterli altın yok!</color>";
            return;
        }

        // Parayi al (basarili veya basarisiz, para gider)
        SaveManager.Instance.SpendGold(cost);

        // Basari kontrolu
        float roll = Random.Range(0f, 1f);
        if (roll <= successRate)
        {
            // BASARILI
            SaveManager.Instance.data.SetWeaponLevel(weapon.weaponName, level + 1);
            SaveManager.Instance.Save();

            if (resultText != null)
                resultText.text = "<color=green>Yükseltme başarılı! +" + (level + 1) + "</color>";
        }
        else
        {
            // BASARISIZ
            SaveManager.Instance.Save();

            if (resultText != null)
                resultText.text = "<color=red>Yükseltme başarısız! Silah aynı kaldı.</color>";
        }

        // UI guncelle
        RefreshAll();
    }

    private void ClearDetail()
    {
        if (weaponNameText != null) weaponNameText.text = "Bir silah seçin";
        if (currentStatsText != null) currentStatsText.text = "";
        if (nextStatsText != null) nextStatsText.text = "";
        if (costText != null) costText.text = "";
        if (successRateText != null) successRateText.text = "";
        if (resultText != null) resultText.text = "";
        if (upgradeButton != null) upgradeButton.interactable = false;
    }

    // ===================== PANEL KONTROL =====================

    public void CloseBlacksmith()
    {
        if (HubManager.Instance != null)
            HubManager.Instance.CloseBlacksmith();
        else
            gameObject.SetActive(false);
    }
}

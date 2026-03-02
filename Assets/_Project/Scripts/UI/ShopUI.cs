using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Hub dukkan arayuzu. Kalici stat yukseltmeleri ve silah satisi yapar.
/// Tum maliyet/artis degerleri UpgradeConfigSO'dan alinir — Inspector'dan ayarlanabilir.
/// 
/// Silah sistemi: Sol tarafta silah listesi, sag tarafta secilen silah detayi.
/// </summary>
public class ShopUI : MonoBehaviour
{
    [Header("Config")]
    [Tooltip("Yukseltme maliyetlerini ve artis miktarlarini tanimlayan SO asset")]
    public UpgradeConfigSO upgradeConfig;

    [Header("Gold Display")]
    public TextMeshProUGUI goldText;

    [Header("Upgrade Buttons")]
    public Button upgradeHealthButton;
    public Button upgradeDamageButton;
    public Button upgradeTempoButton;
    public Button upgradeParryWindowButton;
    public Button upgradeParryRecoveryButton;

    [Header("Upgrade Info Texts")]
    public TextMeshProUGUI healthInfoText;
    public TextMeshProUGUI damageInfoText;
    public TextMeshProUGUI tempoInfoText;
    public TextMeshProUGUI parryWindowInfoText;
    public TextMeshProUGUI parryRecoveryInfoText;

    [Header("Weapon Shop - Liste (Sol)")]
    [Tooltip("Satisa sunulacak silahlar (WeaponSO listesi)")]
    public WeaponSO[] weaponsForSale;
    [Tooltip("Silah isim butonlarinin oluşturulacağı parent (sol panel icindeki Scroll Content)")]
    public Transform weaponListParent;
    [Tooltip("Basit text buton prefab'i (sadece isim gostermek icin)")]
    public GameObject weaponButtonPrefab;

    [Header("Weapon Shop - Detay (Sag)")]
    [Tooltip("Silah detay paneli (sag taraf). Silah secilince aktif olur.")]
    public GameObject weaponDetailPanel;
    [Tooltip("Secilen silah adi")]
    public TextMeshProUGUI weaponDetailName;
    [Tooltip("Secilen silah statlari (Hasar, Hiz, Menzil, Offset)")]
    public TextMeshProUGUI weaponDetailStats;
    [Tooltip("Secilen silah fiyati (sag alt kose)")]
    public TextMeshProUGUI weaponDetailPrice;
    [Tooltip("Satin Al / Mevcut butonu")]
    public Button weaponBuyButton;
    [Tooltip("Satin Al butonunun text'i")]
    public TextMeshProUGUI weaponBuyButtonText;

    // Su an secili silah
    private WeaponSO selectedWeapon;

    private void OnEnable()
    {
        RefreshUI();
    }

    private void Update()
    {
        // ESC ile dukkani kapat
        if (UnityEngine.InputSystem.Keyboard.current != null &&
            UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            CloseShop();
        }
    }

    /// <summary>
    /// Tum UI elemanlarini gunceller.
    /// </summary>
    public void RefreshUI()
    {
        if (SaveManager.Instance == null)
        {
            Debug.LogWarning("[ShopUI] SaveManager bulunamadi!");
            return;
        }

        if (upgradeConfig == null)
        {
            Debug.LogError("[ShopUI] UpgradeConfig atanmamis! Inspector > ShopUI > Upgrade Config alanina UpgradeConfigSO asset'ini surekle.");
            return;
        }

        SaveData data = SaveManager.Instance.data;

        // Altin goster
        if (goldText != null)
            goldText.text = "Altin: " + data.totalGold;

        // --- Yukseltme bilgileri ---
        const float baseMaxHealth     = 100f;
        const float baseDamageMultiplier = 1.0f;
        const float baseTempoGain     = 1.0f;
        float baseParryWindow   = upgradeConfig.baseParryWindow;
        float baseParryRecovery = upgradeConfig.baseParryRecovery;

        // Can
        RefreshUpgradeSlot(
            infoText:   healthInfoText,
            button:     upgradeHealthButton,
            label:      "Can",
            currentLevel: data.bonusMaxHealth,
            maxLevel:   upgradeConfig.healthMaxLevel,
            currentVal: baseMaxHealth + data.bonusMaxHealth * upgradeConfig.healthPerLevel,
            nextVal:    baseMaxHealth + (data.bonusMaxHealth + 1) * upgradeConfig.healthPerLevel,
            valFormat:  "F0",
            prefix: "", suffix: "",
            cost: upgradeConfig.GetCost(upgradeConfig.healthBaseCost, upgradeConfig.healthCostPerLevel, data.bonusMaxHealth)
        );

        // Hasar Çarpanı
        RefreshUpgradeSlot(
            infoText:   damageInfoText,
            button:     upgradeDamageButton,
            label:      "Hasar Çarpanı",
            currentLevel: data.bonusDamageMultiplier,
            maxLevel:   upgradeConfig.damageMaxLevel,
            currentVal: baseDamageMultiplier + data.bonusDamageMultiplier * upgradeConfig.damageMultiplierPerLevel,
            nextVal:    baseDamageMultiplier + (data.bonusDamageMultiplier + 1) * upgradeConfig.damageMultiplierPerLevel,
            valFormat:  "F2",
            prefix: "x", suffix: "",
            cost: upgradeConfig.GetCost(upgradeConfig.damageBaseCost, upgradeConfig.damageCostPerLevel, data.bonusDamageMultiplier)
        );

        // Tempo Kazanımı
        RefreshUpgradeSlot(
            infoText:   tempoInfoText,
            button:     upgradeTempoButton,
            label:      "Tempo Kazanımı",
            currentLevel: data.bonusTempoGain,
            maxLevel:   upgradeConfig.tempoMaxLevel,
            currentVal: baseTempoGain + data.bonusTempoGain * upgradeConfig.tempoGainPerLevel,
            nextVal:    baseTempoGain + (data.bonusTempoGain + 1) * upgradeConfig.tempoGainPerLevel,
            valFormat:  "F2",
            prefix: "x", suffix: "",
            cost: upgradeConfig.GetCost(upgradeConfig.tempoBaseCost, upgradeConfig.tempoCostPerLevel, data.bonusTempoGain)
        );

        // Parry Penceresi
        RefreshUpgradeSlot(
            infoText:   parryWindowInfoText,
            button:     upgradeParryWindowButton,
            label:      "Parry Penceresi",
            currentLevel: data.bonusParryWindow,
            maxLevel:   upgradeConfig.parryWindowMaxLevel,
            currentVal: baseParryWindow + data.bonusParryWindow * upgradeConfig.parryWindowPerLevel,
            nextVal:    baseParryWindow + (data.bonusParryWindow + 1) * upgradeConfig.parryWindowPerLevel,
            valFormat:  "F3",
            prefix: "", suffix: "s",
            cost: upgradeConfig.GetCost(upgradeConfig.parryWindowBaseCost, upgradeConfig.parryWindowCostPerLevel, data.bonusParryWindow)
        );

        // Parry Yenilenme
        float prCurrent = Mathf.Max(0.01f, baseParryRecovery - data.bonusParryRecovery * upgradeConfig.parryRecoveryPerLevel);
        float prNext    = Mathf.Max(0.01f, baseParryRecovery - (data.bonusParryRecovery + 1) * upgradeConfig.parryRecoveryPerLevel);
        RefreshUpgradeSlot(
            infoText:   parryRecoveryInfoText,
            button:     upgradeParryRecoveryButton,
            label:      "P.Yenilenme",
            currentLevel: data.bonusParryRecovery,
            maxLevel:   upgradeConfig.parryRecoveryMaxLevel,
            currentVal: prCurrent,
            nextVal:    prNext,
            valFormat:  "F3",
            prefix: "", suffix: "s",
            cost: upgradeConfig.GetCost(upgradeConfig.parryRecoveryBaseCost, upgradeConfig.parryRecoveryCostPerLevel, data.bonusParryRecovery)
        );

        // Silah listesini guncelle
        RefreshWeaponList();

        // Secili silah varsa detayini guncelle
        if (selectedWeapon != null)
            ShowWeaponDetail(selectedWeapon);
        else if (weaponDetailPanel != null)
            weaponDetailPanel.SetActive(false);
    }

    // ===================== YARDIMCI: UPGRADE SLOT GUNCELLEME =====================

    /// <summary>
    /// Tek bir upgrade satırının info text'ini ve butonunu günceller.
    /// Max seviyeye ulaşıldıysa butonu devre dışı bırakır ve "MAKS" gösterir.
    /// </summary>
    private void RefreshUpgradeSlot(
        TextMeshProUGUI infoText, Button button,
        string label, int currentLevel, int maxLevel,
        float currentVal, float nextVal, string valFormat,
        string prefix, string suffix, int cost)
    {
        bool isMaxed = currentLevel >= maxLevel;
        string bar   = UpgradeConfigSO.BuildProgressBar(currentLevel, maxLevel);

        if (infoText != null)
        {
            if (isMaxed)
            {
                infoText.text = $"<b>{label}</b>  <color=#FFD700>{bar}</color>  {currentLevel}/{maxLevel}\n"
                              + $"<color=#FFD700>✦ MAKSİMUM SEVİYE ✦</color>";
            }
            else
            {
                string goldColor = SaveManager.Instance != null && SaveManager.Instance.data.totalGold >= cost
                    ? "#FFD700" : "#FF6666";
                infoText.text = $"<b>{label}</b>  {bar}  {currentLevel}/{maxLevel}\n"
                              + $"{prefix}{currentVal.ToString(valFormat)}{suffix}  →  "
                              + $"{prefix}{nextVal.ToString(valFormat)}{suffix}"
                              + $"  <color={goldColor}>[{cost}g]</color>";
            }
        }

        if (button != null)
        {
            button.interactable = !isMaxed;
        }
    }

    // ===================== STAT YUKSELTMELERI =====================

    public void UpgradeHealth()
    {
        if (upgradeConfig == null || SaveManager.Instance == null) return;
        if (SaveManager.Instance.data.bonusMaxHealth >= upgradeConfig.healthMaxLevel) return;
        int cost = upgradeConfig.GetCost(upgradeConfig.healthBaseCost, upgradeConfig.healthCostPerLevel, SaveManager.Instance.data.bonusMaxHealth);
        if (SaveManager.Instance.SpendGold(cost))
        {
            SaveManager.Instance.data.bonusMaxHealth++;
            SaveManager.Instance.Save();
            FindFirstObjectByType<PlayerCombat>()?.RefreshFromSave(); // Slider'ı anında güncelle
            RefreshUI();
        }
    }

    public void UpgradeDamage()
    {
        if (upgradeConfig == null || SaveManager.Instance == null) return;
        if (SaveManager.Instance.data.bonusDamageMultiplier >= upgradeConfig.damageMaxLevel) return;
        int cost = upgradeConfig.GetCost(upgradeConfig.damageBaseCost, upgradeConfig.damageCostPerLevel, SaveManager.Instance.data.bonusDamageMultiplier);
        if (SaveManager.Instance.SpendGold(cost))
        {
            SaveManager.Instance.data.bonusDamageMultiplier++;
            SaveManager.Instance.Save();
            FindFirstObjectByType<PlayerCombat>()?.RefreshFromSave(); // damageMultiplier'ı güncelle
            RefreshUI();
        }
    }

    public void UpgradeTempo()
    {
        if (upgradeConfig == null || SaveManager.Instance == null) return;
        if (SaveManager.Instance.data.bonusTempoGain >= upgradeConfig.tempoMaxLevel) return;
        int cost = upgradeConfig.GetCost(upgradeConfig.tempoBaseCost, upgradeConfig.tempoCostPerLevel, SaveManager.Instance.data.bonusTempoGain);
        if (SaveManager.Instance.SpendGold(cost))
        {
            SaveManager.Instance.data.bonusTempoGain++;
            SaveManager.Instance.Save();
            RefreshUI();
        }
    }

    public void UpgradeParryWindow()
    {
        if (upgradeConfig == null || SaveManager.Instance == null) return;
        if (SaveManager.Instance.data.bonusParryWindow >= upgradeConfig.parryWindowMaxLevel) return;
        int cost = upgradeConfig.GetCost(upgradeConfig.parryWindowBaseCost, upgradeConfig.parryWindowCostPerLevel, SaveManager.Instance.data.bonusParryWindow);
        if (SaveManager.Instance.SpendGold(cost))
        {
            SaveManager.Instance.data.bonusParryWindow++;
            SaveManager.Instance.Save();
            RefreshUI();
        }
    }

    public void UpgradeParryRecovery()
    {
        if (upgradeConfig == null || SaveManager.Instance == null) return;
        if (SaveManager.Instance.data.bonusParryRecovery >= upgradeConfig.parryRecoveryMaxLevel) return;
        int cost = upgradeConfig.GetCost(upgradeConfig.parryRecoveryBaseCost, upgradeConfig.parryRecoveryCostPerLevel, SaveManager.Instance.data.bonusParryRecovery);
        if (SaveManager.Instance.SpendGold(cost))
        {
            SaveManager.Instance.data.bonusParryRecovery++;
            SaveManager.Instance.Save();
            RefreshUI();
        }
    }

    // ===================== SILAH DUKKAN - LISTE =====================

    /// <summary>
    /// Sol taraftaki silah isim listesini olusturur.
    /// Her silah icin bir buton: "Katana", "Kan Katili" gibi.
    /// Tiklaninca sag tarafta detay paneli acilir.
    /// </summary>
    private void RefreshWeaponList()
    {
        if (weaponListParent == null || weaponButtonPrefab == null || weaponsForSale == null) return;

        // Onceki butonlari temizle
        foreach (Transform child in weaponListParent)
        {
            Destroy(child.gameObject);
        }

        foreach (WeaponSO weapon in weaponsForSale)
        {
            if (weapon == null) continue;

            GameObject btnObj = Instantiate(weaponButtonPrefab, weaponListParent);
            Button btn = btnObj.GetComponent<Button>();
            TextMeshProUGUI btnText = btnObj.GetComponentInChildren<TextMeshProUGUI>();

            if (btnText != null)
            {
                btnText.text = weapon.weaponName;
            }

            if (btn != null)
            {
                WeaponSO wpn = weapon; // closure icin local copy
                btn.onClick.AddListener(() => ShowWeaponDetail(wpn));
            }
        }
    }

    // ===================== SILAH DUKKAN - DETAY =====================

    /// <summary>
    /// Sag taraftaki detay panelini secilen silah bilgisiyle doldurur.
    /// </summary>
    private void ShowWeaponDetail(WeaponSO weapon)
    {
        if (weapon == null) return;

        selectedWeapon = weapon;

        // Detay panelini ac
        if (weaponDetailPanel != null)
            weaponDetailPanel.SetActive(true);

        bool isUnlocked = SaveManager.Instance != null && SaveManager.Instance.IsWeaponUnlocked(weapon.weaponName);

        // Silah adi
        if (weaponDetailName != null)
        {
            weaponDetailName.text = isUnlocked
                ? weapon.weaponName + " (ACILDI)"
                : weapon.weaponName;
        }

        // Stat bilgileri
        if (weaponDetailStats != null)
        {
            weaponDetailStats.text =
                "Hasar: " + weapon.damage
                + "\nSaldiri Hizi: " + weapon.attackRate.ToString("F2")
                + "\nMenzil: " + weapon.range.ToString("F1")
                + "\nOffset: " + weapon.attackOffset.ToString("F1");
        }

        // Fiyat (sag alt kose)
        if (weaponDetailPrice != null)
        {
            weaponDetailPrice.text = isUnlocked ? "0g" : weapon.price + "g";
        }

        // Satin Al butonu
        if (weaponBuyButton != null)
        {
            weaponBuyButton.onClick.RemoveAllListeners();

            if (isUnlocked)
            {
                if (weaponBuyButtonText != null)
                    weaponBuyButtonText.text = "Mevcut";
                weaponBuyButton.interactable = false;
            }
            else
            {
                if (weaponBuyButtonText != null)
                    weaponBuyButtonText.text = "Satin Al";
                weaponBuyButton.interactable = true;

                string wpnName = weapon.weaponName;
                int wpnPrice = weapon.price;
                weaponBuyButton.onClick.AddListener(() => BuyWeapon(wpnName, wpnPrice));
            }
        }
    }

    private void BuyWeapon(string weaponName, int price)
    {
        if (SaveManager.Instance == null) return;
        if (SaveManager.Instance.SpendGold(price))
        {
            SaveManager.Instance.UnlockWeapon(weaponName);
            RefreshUI();
        }
    }

    // ===================== PANEL KONTROL =====================

    public void CloseShop()
    {
        if (HubManager.Instance != null)
            HubManager.Instance.CloseShop();
        else
            gameObject.SetActive(false);
    }
}

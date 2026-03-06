using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Istatistik Paneli (I tusu ile acilir).
/// Sol taraf: Oyuncunun tum anlık istatistikleri (can, hasar, hiz, menzil, tempo, parry).
/// Sag taraf:
///   - Hub'da: Silah kuşanma (liste + detay + kuşan butonu)
///   - Gameplay'de: Sadece kuşanılan silahın ikonu ve adı (liste/buton yok)
/// </summary>
public class StatsPanel : MonoBehaviour
{
    [Header("Panel")]
    [Tooltip("I tusu ile acilip kapanacak panel")]
    public GameObject statsPanel;

    [Header("Istatistikler (Sol Taraf)")]
    [Tooltip("Tum istatistikleri gosteren text")]
    public TextMeshProUGUI allStatsText;

    [Header("Silah Kusanma (Sag Taraf)")]
    [Tooltip("Su an kusanilan silah iconu (placeholder)")]
    public Image equippedWeaponIcon;
    [Tooltip("Su an kusanilan silah adi")]
    public TextMeshProUGUI equippedWeaponName;

    [Header("Silah Listesi (Sadece Hub)")]
    [Tooltip("Merkezi silah veritabani")]
    public WeaponDatabase weaponDatabase;
    [Tooltip("Sahip olunan silahlarin listelenecegi parent")]
    public Transform weaponListParent;
    [Tooltip("Silah isim buton prefab'i")]
    public GameObject weaponButtonPrefab;

    [Header("Silah Detay (Sadece Hub)")]
    [Tooltip("Secilen silah detay paneli")]
    public GameObject weaponDetailPanel;
    [Tooltip("Secilen silah adi")]
    public TextMeshProUGUI detailWeaponName;
    [Tooltip("Secilen silah statlari")]
    public TextMeshProUGUI detailWeaponStats;
    [Tooltip("Kusan / Zaten Kusanildi butonu")]
    public Button equipButton;
    [Tooltip("Kusan butonunun text'i")]
    public TextMeshProUGUI equipButtonText;

    [Header("Hub-Only UI Gruplari")]
    [Tooltip("Hub'da gorunen ama Gameplay'de gizlenen UI parent'lari (silah listesi paneli, detay paneli vb.)")]
    public GameObject[] hubOnlyElements;

    [Header("Config")]
    public UpgradeConfigSO upgradeConfig;

    private WeaponSO selectedWeapon;
    private bool isOpen = false;

    private void Update()
    {
        if (Keyboard.current == null) return;

        // ESC ile paneli kapat
        if (isOpen && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ClosePanel();
            return;
        }

        // I tusu ile ac/kapat
        if (Keyboard.current.iKey.wasPressedThisFrame)
        {
            if (isOpen)
                ClosePanel();
            else
                OpenPanel();
        }
    }

    private void OpenPanel()
    {
        if (statsPanel == null) return;

        statsPanel.SetActive(true);
        isOpen = true;

        bool isHub = IsInHub();

        // Hub-only elemanlari goster/gizle
        if (hubOnlyElements != null)
        {
            foreach (var go in hubOnlyElements)
            {
                if (go != null) go.SetActive(isHub);
            }
        }

        RefreshAllStats();
        RefreshEquippedDisplay();

        if (isHub)
        {
            RefreshWeaponList();
            if (weaponDetailPanel != null)
                weaponDetailPanel.SetActive(false);
        }

        // Hareket kilitleme
        SetPlayerMovement(false);
    }

    public void ClosePanel()
    {
        if (statsPanel == null) return;

        statsPanel.SetActive(false);
        isOpen = false;

        SetPlayerMovement(true);
    }

    // ===================== ISTATISTIKLER =====================

    /// <summary>
    /// Sol taraftaki tum istatistikleri gunceller.
    /// </summary>
    private void RefreshAllStats()
    {
        if (allStatsText == null) return;

        PlayerCombat combat = FindFirstObjectByType<PlayerCombat>();
        if (combat == null)
        {
            allStatsText.text = "Oyuncu bulunamadi!";
            return;
        }

        string stats = "";

        SaveData saveData = SaveManager.Instance != null ? SaveManager.Instance.data : null;
        bool hasConfig    = upgradeConfig != null;

        // --- CAN ---
        {
            int lv  = saveData != null ? saveData.bonusMaxHealth : 0;
            int max = hasConfig ? upgradeConfig.healthMaxLevel : 0;
            string bar = hasConfig ? " " + LvBar(lv, max) : "";
            stats += "<b>Can:</b> " + combat.currentHealth.ToString("F0")
                   + " / " + combat.maxHealth.ToString("F0");
            if (lv > 0 && hasConfig)
                stats += " <color=#88ff88>(+" + (lv * upgradeConfig.healthPerLevel).ToString("F0") + ")</color>";
            if (hasConfig)
                stats += $"\n  <size=80%>{bar}  <color=#aaaaaa>Sv.{lv}/{max}</color></size>";
        }

        // --- SILAH STATLARI ---
        if (combat.currentWeapon != null)
        {
            int level = combat.CurrentWeaponLevel;
            stats += "\n\n<b>─── Silah ───</b>";
            stats += "\nSilah: " + combat.currentWeapon.GetDisplayName(level);
            stats += "\nHasar: " + combat.GetEffectiveDamage().ToString("F1");
            stats += "\nSaldırı Hızı: " + combat.GetEffectiveAttackRate().ToString("F2") + "s";
            stats += "\nMenzil: " + combat.GetEffectiveRange().ToString("F1");
        }

        // --- HASAR CARPANI ---
        stats += "\n\n<b>─── Genel ───</b>";
        {
            int lv  = saveData != null ? saveData.bonusDamageMultiplier : 0;
            int max = hasConfig ? upgradeConfig.damageMaxLevel : 0;
            stats += "\nHasar Çarpanı: x" + combat.damageMultiplier.ToString("F2");
            if (lv > 0 && hasConfig)
                stats += " <color=#88ff88>(+" + (lv * upgradeConfig.damageMultiplierPerLevel).ToString("F2") + ")</color>";
            if (hasConfig)
                stats += $"\n  <size=80%>{LvBar(lv, max)}  <color=#aaaaaa>Sv.{lv}/{max}</color></size>";
        }

        // --- TEMPO ---
        if (TempoManager.Instance != null)
        {
            int lv  = saveData != null ? saveData.bonusTempoGain : 0;
            int max = hasConfig ? upgradeConfig.tempoMaxLevel : 0;
            stats += "\nTempo: " + TempoManager.Instance.tempo.ToString("F0")
                   + " / " + TempoManager.Instance.maxTempo.ToString("F0");
            if (hasConfig)
                stats += $"\n  <size=80%>{LvBar(lv, max)}  <color=#aaaaaa>Sv.{lv}/{max}</color></size>";
        }

        // --- PARRY ---
        if (saveData != null && hasConfig)
        {
            float parryWindow   = upgradeConfig.baseParryWindow + saveData.bonusParryWindow   * upgradeConfig.parryWindowPerLevel;
            float parryRecovery = Mathf.Max(0.01f, upgradeConfig.baseParryRecovery - saveData.bonusParryRecovery * upgradeConfig.parryRecoveryPerLevel);

            stats += "\nParry Penceresi: " + parryWindow.ToString("F3") + "s";
            stats += $"\n  <size=80%>{LvBar(saveData.bonusParryWindow, upgradeConfig.parryWindowMaxLevel)}"
                   + $"  <color=#aaaaaa>Sv.{saveData.bonusParryWindow}/{upgradeConfig.parryWindowMaxLevel}</color></size>";

            stats += "\nParry Yenilenme: " + parryRecovery.ToString("F3") + "s";
            stats += $"\n  <size=80%>{LvBar(saveData.bonusParryRecovery, upgradeConfig.parryRecoveryMaxLevel)}"
                   + $"  <color=#aaaaaa>Sv.{saveData.bonusParryRecovery}/{upgradeConfig.parryRecoveryMaxLevel}</color></size>";
        }

        allStatsText.text = stats;
    }

    // ===================== SILAH GORUNTULEME =====================

    /// <summary>
    /// Kusanilan silah ikon ve adini gosterir (Hub + Gameplay).
    /// </summary>
    private void RefreshEquippedDisplay()
    {
        PlayerCombat combat = FindFirstObjectByType<PlayerCombat>();
        if (combat == null || combat.currentWeapon == null) return;

        WeaponSO wpn = combat.currentWeapon;
        int level = combat.CurrentWeaponLevel;

        if (equippedWeaponName != null)
            equippedWeaponName.text = wpn.GetDisplayName(level);

        if (equippedWeaponIcon != null && wpn.icon != null)
            equippedWeaponIcon.sprite = wpn.icon;
    }

    // ===================== SILAH KUSANMA (SADECE HUB) =====================

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

            GameObject btnObj = Instantiate(weaponButtonPrefab, weaponListParent);
            Button btn = btnObj.GetComponent<Button>();
            TextMeshProUGUI btnText = btnObj.GetComponentInChildren<TextMeshProUGUI>();

            int level = SaveManager.Instance.data.GetWeaponLevel(weapon.weaponName);

            if (btnText != null)
                btnText.text = weapon.GetDisplayName(level);

            if (btn != null)
            {
                WeaponSO wpn = weapon;
                btn.onClick.AddListener(() => ShowWeaponDetail(wpn));
            }
        }
    }

    private void ShowWeaponDetail(WeaponSO weapon)
    {
        if (weapon == null) return;
        selectedWeapon = weapon;

        if (weaponDetailPanel != null)
            weaponDetailPanel.SetActive(true);

        int level = SaveManager.Instance != null
            ? SaveManager.Instance.data.GetWeaponLevel(weapon.weaponName)
            : 0;

        if (detailWeaponName != null)
            detailWeaponName.text = weapon.GetDisplayName(level);

        if (detailWeaponStats != null)
        {
            detailWeaponStats.text =
                "Hasar: " + weapon.GetUpgradedDamage(level).ToString("F1")
                + "\nSaldiri Hizi: " + weapon.GetUpgradedAttackRate(level).ToString("F2")
                + "\nMenzil: " + weapon.GetUpgradedRange(level).ToString("F1");
        }

        // Kusan butonu
        PlayerCombat combat = FindFirstObjectByType<PlayerCombat>();
        bool isEquipped = combat != null && combat.currentWeapon != null
            && combat.currentWeapon.weaponName == weapon.weaponName;

        if (equipButton != null)
        {
            equipButton.onClick.RemoveAllListeners();

            if (isEquipped)
            {
                if (equipButtonText != null)
                    equipButtonText.text = "Zaten Kuşanıldı";
                equipButton.interactable = false;
            }
            else
            {
                if (equipButtonText != null)
                    equipButtonText.text = "Kuşan";
                equipButton.interactable = true;

                WeaponSO wpn = weapon;
                equipButton.onClick.AddListener(() => EquipWeapon(wpn));
            }
        }
    }

    private void EquipWeapon(WeaponSO weapon)
    {
        PlayerCombat combat = FindFirstObjectByType<PlayerCombat>();
        if (combat == null) return;

        combat.EquipWeapon(weapon);

        RefreshAllStats();
        RefreshEquippedDisplay();
        RefreshWeaponList();
        ShowWeaponDetail(weapon);
    }

    // ===================== YARDIMCI =====================

    /// <summary>
    /// Küçük progress bar: █░ karakterleri, 8 blok uzunluğunda.
    /// Max'taysa altın renkli tam dolu bar döner.
    /// </summary>
    private static string LvBar(int current, int max)
    {
        if (max <= 0) return "";
        string bar = UpgradeConfigSO.BuildProgressBar(current, max, 8);
        bool isMaxed = current >= max;
        return isMaxed
            ? "<color=#FFD700>" + bar + " MAKS</color>"
            : "<color=#88ff88>" + bar + "</color>";
    }

    private bool IsInHub()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        return sceneName.Contains("Hub") || sceneName.Contains("hub");
    }

    private void SetPlayerMovement(bool canMove)
    {
        PlayerController pc = FindFirstObjectByType<PlayerController>();
        if (pc != null)
            pc.canMove = canMove;
    }
}

using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Istatistik paneli. Hub ve gameplay kullanimi korunur, modal shell runtime'da kurulur.
/// </summary>
public class StatsPanel : MonoBehaviour
{
    [Header("Panel")]
    public GameObject statsPanel;

    [Header("Istatistikler (Sol Taraf)")]
    public TextMeshProUGUI allStatsText;

    [Header("Silah Kusanma (Sag Taraf)")]
    public Image equippedWeaponIcon;
    public TextMeshProUGUI equippedWeaponName;

    [Header("Silah Listesi (Sadece Hub)")]
    public WeaponDatabase weaponDatabase;
    public Transform weaponListParent;
    public GameObject weaponButtonPrefab;

    [Header("Silah Detay (Sadece Hub)")]
    public GameObject weaponDetailPanel;
    public TextMeshProUGUI detailWeaponName;
    public TextMeshProUGUI detailWeaponStats;
    public Button equipButton;
    public TextMeshProUGUI equipButtonText;

    [Header("Hub-Only UI Gruplari")]
    public GameObject[] hubOnlyElements;

    [Header("Config")]
    public UpgradeConfigSO upgradeConfig;

    private const string ModalId = "stats";

    private WeaponSO selectedWeapon;
    private bool isOpen;
    private bool layoutBuilt;
    private Canvas cachedCanvas;

    private void Awake()
    {
        cachedCanvas = GetComponent<Canvas>();
        if (cachedCanvas == null)
            cachedCanvas = GetComponentInParent<Canvas>();
    }

    private void Start()
    {
        EnsureRuntimeLayout();
        if (statsPanel != null)
            statsPanel.SetActive(false);
    }

    private void Update()
    {
        if (Keyboard.current == null) return;

        if (isOpen && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ClosePanel();
            return;
        }

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

        EnsureRuntimeLayout();

        if (!ModalUIManager.Instance.TryOpenModal(ModalId, statsPanel))
            return;

        SetPlayerMovement(false);
        statsPanel.SetActive(true);
        isOpen = true;

        bool isHub = IsInHub();
        if (hubOnlyElements != null)
        {
            foreach (GameObject go in hubOnlyElements)
            {
                if (go != null)
                    go.SetActive(isHub);
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
    }

    public void ClosePanel()
    {
        if (statsPanel == null) return;

        statsPanel.SetActive(false);
        isOpen = false;
        ModalUIManager.Instance.CloseModal(ModalId);
        SetPlayerMovement(true);
    }

    private void RefreshAllStats()
    {
        if (allStatsText == null) return;

        PlayerCombat combat = FindFirstObjectByType<PlayerCombat>();
        if (combat == null)
        {
            allStatsText.text = "Oyuncu bulunamadi!";
            return;
        }

        string stats = string.Empty;
        SaveData saveData = SaveManager.Instance != null ? SaveManager.Instance.data : null;
        bool hasConfig = upgradeConfig != null;

        int healthLevel = saveData != null ? saveData.bonusMaxHealth : 0;
        int healthMax = hasConfig ? upgradeConfig.healthMaxLevel : 0;
        string healthBar = hasConfig ? " " + LvBar(healthLevel, healthMax) : "";
        stats += "<b>Can:</b> " + combat.currentHealth.ToString("F0") + " / " + combat.maxHealth.ToString("F0");
        if (healthLevel > 0 && hasConfig)
            stats += " <color=#88ff88>(+" + (healthLevel * upgradeConfig.healthPerLevel).ToString("F0") + ")</color>";
        if (hasConfig)
            stats += $"\n  <size=80%>{healthBar}  <color=#aaaaaa>Sv.{healthLevel}/{healthMax}</color></size>";

        if (combat.currentWeapon != null)
        {
            int level = combat.CurrentWeaponLevel;
            stats += "\n\n<b>--- Silah ---</b>";
            stats += "\nSilah: " + combat.currentWeapon.GetDisplayName(level);
            stats += "\nHasar: " + combat.GetEffectiveDamage().ToString("F1");
            stats += "\nSaldiri Hizi: " + combat.GetEffectiveAttackRate().ToString("F2") + "s";
            stats += "\nMenzil: " + combat.GetEffectiveRange().ToString("F1");
        }

        stats += "\n\n<b>--- Genel ---</b>";

        int damageLevel = saveData != null ? saveData.bonusDamageMultiplier : 0;
        int damageMax = hasConfig ? upgradeConfig.damageMaxLevel : 0;
        stats += "\nHasar Carpani: x" + combat.damageMultiplier.ToString("F2");
        if (damageLevel > 0 && hasConfig)
            stats += " <color=#88ff88>(+" + (damageLevel * upgradeConfig.damageMultiplierPerLevel).ToString("F2") + ")</color>";
        if (hasConfig)
            stats += $"\n  <size=80%>{LvBar(damageLevel, damageMax)}  <color=#aaaaaa>Sv.{damageLevel}/{damageMax}</color></size>";

        if (TempoManager.Instance != null)
        {
            int tempoLevel = saveData != null ? saveData.bonusTempoGain : 0;
            int tempoMax = hasConfig ? upgradeConfig.tempoMaxLevel : 0;
            stats += "\nTempo: " + TempoManager.Instance.tempo.ToString("F0") + " / " + TempoManager.Instance.maxTempo.ToString("F0");
            if (hasConfig)
                stats += $"\n  <size=80%>{LvBar(tempoLevel, tempoMax)}  <color=#aaaaaa>Sv.{tempoLevel}/{tempoMax}</color></size>";
        }

        allStatsText.text = stats;
    }

    private void RefreshEquippedDisplay()
    {
        PlayerCombat combat = FindFirstObjectByType<PlayerCombat>();
        if (combat == null || combat.currentWeapon == null) return;

        WeaponSO weapon = combat.currentWeapon;
        int level = combat.CurrentWeaponLevel;

        if (equippedWeaponName != null)
            equippedWeaponName.text = weapon.GetDisplayName(level);

        if (equippedWeaponIcon != null && weapon.icon != null)
            equippedWeaponIcon.sprite = weapon.icon;
    }

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
            {
                btnText.text = weapon.GetDisplayName(level);
                ModalUIRuntimeUtility.NormalizeText(btnText, false);
                btnText.color = new Color(0.95f, 0.97f, 1f, 1f);
                btnText.fontSize = 18f;
            }

            if (btn != null)
            {
                ModalUIRuntimeUtility.NormalizeButton(btn, 50f);
                Image image = btn.GetComponent<Image>();
                if (image != null)
                    image.color = new Color(0.16f, 0.20f, 0.28f, 0.98f);

                WeaponSO cachedWeapon = weapon;
                btn.onClick.AddListener(() => ShowWeaponDetail(cachedWeapon));
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
                "Hasar: " + weapon.GetUpgradedDamage(level).ToString("F1") +
                "\nSaldiri Hizi: " + weapon.GetUpgradedAttackRate(level).ToString("F2") +
                "\nMenzil: " + weapon.GetUpgradedRange(level).ToString("F1");
        }

        PlayerCombat combat = FindFirstObjectByType<PlayerCombat>();
        bool isEquipped = combat != null &&
                          combat.currentWeapon != null &&
                          combat.currentWeapon.weaponName == weapon.weaponName;

        if (equipButton != null)
        {
            equipButton.onClick.RemoveAllListeners();

            if (isEquipped)
            {
                if (equipButtonText != null)
                    equipButtonText.text = "Zaten Kusanildi";
                equipButton.interactable = false;
            }
            else
            {
                if (equipButtonText != null)
                    equipButtonText.text = "Kusan";
                equipButton.interactable = true;

                WeaponSO cachedWeapon = weapon;
                equipButton.onClick.AddListener(() => EquipWeapon(cachedWeapon));
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

    private static string LvBar(int current, int max)
    {
        if (max <= 0) return string.Empty;
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

    private void EnsureRuntimeLayout()
    {
        if (layoutBuilt || statsPanel == null)
            return;

        if (cachedCanvas == null)
            cachedCanvas = GetComponent<Canvas>();
        if (cachedCanvas == null)
            cachedCanvas = GetComponentInParent<Canvas>();

        ModalUIRuntimeUtility.EnsureFullscreenCanvas(cachedCanvas);

        RectTransform modalRoot = statsPanel.GetComponent<RectTransform>();
        ModalUIRuntimeUtility.Stretch(modalRoot);

        RectTransform overlay = ModalUIRuntimeUtility.CreateOrGetOverlayRoot(statsPanel.transform, "ModalOverlay");
        RectTransform shell = ModalUIRuntimeUtility.CreateCard(
            overlay,
            "StatsShell",
            new Color(0.06f, 0.09f, 0.13f, 0.98f),
            new Vector2(0.03f, 0.04f),
            new Vector2(0.97f, 0.96f),
            Vector2.zero,
            Vector2.zero);

        RectTransform header = new GameObject("Header", typeof(RectTransform)).GetComponent<RectTransform>();
        header.SetParent(shell, false);
        header.anchorMin = new Vector2(0.03f, 0.90f);
        header.anchorMax = new Vector2(0.97f, 0.97f);
        header.offsetMin = Vector2.zero;
        header.offsetMax = Vector2.zero;

        CreateSectionLabel(header, "ISTATISTIK PANELI", 30f);
        ModalUIRuntimeUtility.CreateCloseButton(header, ClosePanel);

        RectTransform body = new GameObject("Body", typeof(RectTransform), typeof(ResponsiveSplitLayout)).GetComponent<RectTransform>();
        body.SetParent(shell, false);
        body.anchorMin = new Vector2(0.03f, 0.05f);
        body.anchorMax = new Vector2(0.97f, 0.87f);
        body.offsetMin = Vector2.zero;
        body.offsetMax = Vector2.zero;

        ResponsiveSplitLayout split = body.GetComponent<ResponsiveSplitLayout>();
        split.keepFirstSectionWiderOnDesktop = true;
        split.firstSectionFlexibleWidth = 1.2f;
        split.secondSectionFlexibleWidth = 1f;

        RectTransform left = ModalUIRuntimeUtility.CreateSection(body, "StatsSection", ModalUIRuntimeUtility.SectionColor);
        RectTransform right = ModalUIRuntimeUtility.CreateSection(body, "WeaponSection", ModalUIRuntimeUtility.SectionAltColor);

        CreateSectionLabel(left, "Mevcut Durum");
        ModalUIRuntimeUtility.NormalizeText(allStatsText, true, 320f);
        ModalUIRuntimeUtility.Wrap(allStatsText.rectTransform, left, "StatsTextWrap", 320f);

        CreateSectionLabel(right, "Silah");
        RectTransform equippedRow = ModalUIRuntimeUtility.CreateRow(right, "EquippedRow", 92f);
        NormalizeIcon(equippedWeaponIcon);
        ModalUIRuntimeUtility.Wrap(equippedWeaponIcon.rectTransform, equippedRow, "EquippedIconWrap", 84f);
        ModalUIRuntimeUtility.NormalizeText(equippedWeaponName, false, 40f);
        ModalUIRuntimeUtility.Wrap(equippedWeaponName.rectTransform, equippedRow, "EquippedNameWrap", 40f);

        RectTransform listContent = weaponListParent as RectTransform;
        if (listContent != null)
            ModalUIRuntimeUtility.CreateScrollableList(right, "OwnedWeaponsViewport", listContent);

        if (weaponDetailPanel != null)
        {
            LayoutElement detailLayout = weaponDetailPanel.GetComponent<LayoutElement>();
            if (detailLayout == null)
                detailLayout = weaponDetailPanel.AddComponent<LayoutElement>();

            detailLayout.flexibleHeight = 1f;
            detailLayout.minHeight = 220f;
            weaponDetailPanel.transform.SetParent(right, false);
            ModalUIRuntimeUtility.StretchHorizontally(weaponDetailPanel.GetComponent<RectTransform>());
        }

        ModalUIRuntimeUtility.NormalizeText(detailWeaponName, false, 34f);
        ModalUIRuntimeUtility.NormalizeText(detailWeaponStats, true, 90f);
        ModalUIRuntimeUtility.NormalizeText(equipButtonText, false);
        ModalUIRuntimeUtility.NormalizeButton(equipButton, 50f);

        layoutBuilt = true;
    }

    private static void NormalizeIcon(Image image)
    {
        if (image == null)
            return;

        RectTransform rect = image.rectTransform;
        rect.localScale = Vector3.one;

        LayoutElement layout = image.GetComponent<LayoutElement>();
        if (layout == null)
            layout = image.gameObject.AddComponent<LayoutElement>();

        layout.minWidth = 84f;
        layout.minHeight = 84f;
        layout.preferredWidth = 84f;
        layout.preferredHeight = 84f;
        image.preserveAspect = true;
    }

    private static void CreateSectionLabel(RectTransform parent, string title, float fontSize = 22f)
    {
        GameObject go = new GameObject(title.Replace(" ", string.Empty) + "Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI), typeof(LayoutElement));
        go.transform.SetParent(parent, false);

        TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
        text.text = title;
        text.fontSize = fontSize;
        text.fontStyle = FontStyles.Bold;
        text.color = ModalUIRuntimeUtility.HeaderTextColor;
        text.alignment = TextAlignmentOptions.MidlineLeft;

        LayoutElement layout = go.GetComponent<LayoutElement>();
        layout.minHeight = fontSize + 8f;
        layout.flexibleWidth = 1f;

        RectTransform rect = go.GetComponent<RectTransform>();
        ModalUIRuntimeUtility.StretchHorizontally(rect);
    }

    private static void SetPlayerMovement(bool enabled)
    {
        PlayerController pc = FindFirstObjectByType<PlayerController>();
        if (pc == null)
            return;

        pc.canMove = enabled;

        Rigidbody2D rb = pc.GetComponent<Rigidbody2D>();
        if (rb != null && !enabled)
            rb.linearVelocity = Vector2.zero;
    }
}

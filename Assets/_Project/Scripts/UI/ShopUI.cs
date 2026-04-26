using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Hub dukkan arayuzu. Kalici stat yukseltmeleri ve silah satisi yapar.
/// Responsive modal shell runtime'da kurulur, mevcut veri akisi korunur.
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

    [Header("Upgrade Info Texts")]
    public TextMeshProUGUI healthInfoText;
    public TextMeshProUGUI damageInfoText;
    public TextMeshProUGUI tempoInfoText;

    [Header("Weapon Shop - Liste (Sol)")]
    public WeaponSO[] weaponsForSale;
    public Transform weaponListParent;
    public GameObject weaponButtonPrefab;

    [Header("Weapon Shop - Detay (Sag)")]
    public GameObject weaponDetailPanel;
    public TextMeshProUGUI weaponDetailName;
    public TextMeshProUGUI weaponDetailStats;
    public TextMeshProUGUI weaponDetailPrice;
    public Button weaponBuyButton;
    public TextMeshProUGUI weaponBuyButtonText;

    private const string ModalId = "shop";

    private WeaponSO selectedWeapon;
    private Canvas cachedCanvas;
    private bool layoutBuilt;
    private bool isOpen;

    public bool IsOpen => isOpen;

    private void Awake()
    {
        cachedCanvas = GetComponent<Canvas>();
        if (cachedCanvas == null)
            cachedCanvas = GetComponentInParent<Canvas>();
    }

    private void OnEnable()
    {
        EnsureRuntimeLayout();
        RefreshUI();
    }

    private void OnDisable()
    {
        isOpen = false;
    }

    private void Update()
    {
        if (!isOpen)
            return;

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            CloseShop();
    }

    public void OpenPanel()
    {
        EnsureRuntimeLayout();

        if (!ModalUIManager.Instance.TryOpenModal(ModalId, gameObject))
            return;

        SetPlayerMovement(false);
        gameObject.SetActive(true);
        isOpen = true;
        HubInteractable.HideAllPrompts();
        RefreshUI();
    }

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

        if (goldText != null)
            goldText.text = "Altin: " + data.totalGold;

        const float baseMaxHealth = 100f;
        const float baseDamageMultiplier = 1f;
        const float baseTempoGain = 1f;

        RefreshUpgradeSlot(
            infoText: healthInfoText,
            button: upgradeHealthButton,
            label: "Can",
            currentLevel: data.bonusMaxHealth,
            maxLevel: upgradeConfig.healthMaxLevel,
            currentVal: upgradeConfig.GetMaxHealth(data.bonusMaxHealth, baseMaxHealth),
            nextVal: upgradeConfig.GetMaxHealth(data.bonusMaxHealth + 1, baseMaxHealth),
            valFormat: "F0",
            prefix: "",
            suffix: "",
            cost: upgradeConfig.GetCost(upgradeConfig.healthBaseCost, upgradeConfig.healthCostPerLevel, data.bonusMaxHealth)
        );

        RefreshUpgradeSlot(
            infoText: damageInfoText,
            button: upgradeDamageButton,
            label: "Hasar Carpani",
            currentLevel: data.bonusDamageMultiplier,
            maxLevel: upgradeConfig.damageMaxLevel,
            currentVal: upgradeConfig.GetDamageMultiplier(data.bonusDamageMultiplier, baseDamageMultiplier),
            nextVal: upgradeConfig.GetDamageMultiplier(data.bonusDamageMultiplier + 1, baseDamageMultiplier),
            valFormat: "F2",
            prefix: "x",
            suffix: "",
            cost: upgradeConfig.GetCost(upgradeConfig.damageBaseCost, upgradeConfig.damageCostPerLevel, data.bonusDamageMultiplier)
        );

        RefreshUpgradeSlot(
            infoText: tempoInfoText,
            button: upgradeTempoButton,
            label: "Tempo Kazanimi",
            currentLevel: data.bonusTempoGain,
            maxLevel: upgradeConfig.tempoMaxLevel,
            currentVal: upgradeConfig.GetTempoGainMultiplier(data.bonusTempoGain, baseTempoGain),
            nextVal: upgradeConfig.GetTempoGainMultiplier(data.bonusTempoGain + 1, baseTempoGain),
            valFormat: "F2",
            prefix: "x",
            suffix: "",
            cost: upgradeConfig.GetCost(upgradeConfig.tempoBaseCost, upgradeConfig.tempoCostPerLevel, data.bonusTempoGain)
        );

        RefreshWeaponList();

        if (selectedWeapon != null)
            ShowWeaponDetail(selectedWeapon);
        else if (weaponDetailPanel != null)
            weaponDetailPanel.SetActive(false);
    }

    private void RefreshUpgradeSlot(
        TextMeshProUGUI infoText, Button button,
        string label, int currentLevel, int maxLevel,
        float currentVal, float nextVal, string valFormat,
        string prefix, string suffix, int cost)
    {
        bool isMaxed = currentLevel >= maxLevel;
        string bar = UpgradeConfigSO.BuildProgressBar(currentLevel, maxLevel);

        if (infoText != null)
        {
            if (isMaxed)
            {
                infoText.text = $"<b>{label}</b>  <color=#FFD700>{bar}</color>  {currentLevel}/{maxLevel}\n" +
                                "<color=#FFD700>MAKSIMUM SEVIYE</color>";
            }
            else
            {
                string goldColor = SaveManager.Instance != null && SaveManager.Instance.data.totalGold >= cost
                    ? "#FFD700" : "#FF6666";

                infoText.text = $"<b>{label}</b>  {bar}  {currentLevel}/{maxLevel}\n" +
                                $"{prefix}{currentVal.ToString(valFormat)}{suffix}  ->  {prefix}{nextVal.ToString(valFormat)}{suffix}" +
                                $"  <color={goldColor}>[{cost}g]</color>";
            }
        }

        if (button != null)
            button.interactable = !isMaxed;
    }

    public void UpgradeHealth()
    {
        if (upgradeConfig == null || SaveManager.Instance == null) return;

        if (CoreProgressionService.TryPurchaseLegacyUpgrade(CoreLegacyUpgradeType.MaxHealth, upgradeConfig, out string failureReason))
        {
            FindFirstObjectByType<PlayerCombat>()?.RefreshFromSave();
            RefreshUI();
        }
        else if (!string.IsNullOrWhiteSpace(failureReason))
        {
            Debug.LogWarning("[ShopUI] " + failureReason);
        }
    }

    public void UpgradeDamage()
    {
        if (upgradeConfig == null || SaveManager.Instance == null) return;

        if (CoreProgressionService.TryPurchaseLegacyUpgrade(CoreLegacyUpgradeType.DamageMultiplier, upgradeConfig, out string failureReason))
        {
            FindFirstObjectByType<PlayerCombat>()?.RefreshFromSave();
            RefreshUI();
        }
        else if (!string.IsNullOrWhiteSpace(failureReason))
        {
            Debug.LogWarning("[ShopUI] " + failureReason);
        }
    }

    public void UpgradeTempo()
    {
        if (upgradeConfig == null || SaveManager.Instance == null) return;

        if (CoreProgressionService.TryPurchaseLegacyUpgrade(CoreLegacyUpgradeType.TempoGain, upgradeConfig, out string failureReason))
        {
            RefreshUI();
        }
        else if (!string.IsNullOrWhiteSpace(failureReason))
        {
            Debug.LogWarning("[ShopUI] " + failureReason);
        }
    }

    private void RefreshWeaponList()
    {
        if (weaponListParent == null || weaponButtonPrefab == null || weaponsForSale == null)
            return;

        foreach (Transform child in weaponListParent)
            Destroy(child.gameObject);

        foreach (WeaponSO weapon in weaponsForSale)
        {
            if (weapon == null)
                continue;

            GameObject btnObj = Instantiate(weaponButtonPrefab, weaponListParent);
            Button btn = btnObj.GetComponent<Button>();
            TextMeshProUGUI btnText = btnObj.GetComponentInChildren<TextMeshProUGUI>();

            if (btnText != null)
            {
                btnText.text = weapon.weaponName;
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
        if (weapon == null)
            return;

        selectedWeapon = weapon;

        if (weaponDetailPanel != null)
            weaponDetailPanel.SetActive(true);

        bool isUnlocked = SaveManager.Instance != null && SaveManager.Instance.IsWeaponUnlocked(weapon.weaponName);

        if (weaponDetailName != null)
            weaponDetailName.text = isUnlocked ? weapon.weaponName + " (ACILDI)" : weapon.weaponName;

        if (weaponDetailStats != null)
        {
            weaponDetailStats.text =
                "Hasar: " + weapon.damage +
                "\nSaldiri Hizi: " + weapon.attackRate.ToString("F2") +
                "\nMenzil: " + weapon.range.ToString("F1") +
                "\nOffset: " + weapon.attackOffset.ToString("F1");
        }

        if (weaponDetailPrice != null)
            weaponDetailPrice.text = isUnlocked ? "0g" : weapon.price + "g";

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
        if (SaveManager.Instance == null)
            return;

        if (SaveManager.Instance.SpendGold(price))
        {
            SaveManager.Instance.UnlockWeapon(weaponName);
            RefreshUI();
        }
    }

    public void CloseShop()
    {
        isOpen = false;
        ModalUIManager.Instance.CloseModal(ModalId);
        SetPlayerMovement(true);
        gameObject.SetActive(false);
        HubInteractable.ShowAllPrompts();
    }

    private void EnsureRuntimeLayout()
    {
        if (layoutBuilt)
            return;

        if (cachedCanvas == null)
            cachedCanvas = GetComponent<Canvas>();
        if (cachedCanvas == null)
            cachedCanvas = GetComponentInParent<Canvas>();

        ModalUIRuntimeUtility.EnsureFullscreenCanvas(cachedCanvas);
        ModalUIRuntimeUtility.Stretch(GetComponent<RectTransform>());

        RectTransform overlay = ModalUIRuntimeUtility.CreateOrGetOverlayRoot(transform, "ModalOverlay");
        RectTransform shell = ModalUIRuntimeUtility.CreateCard(
            overlay,
            "ShopShell",
            new Color(0.08f, 0.10f, 0.14f, 0.98f),
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

        ModalUIRuntimeUtility.CreateTitle(header, "DUKKAN");
        ModalUIRuntimeUtility.CreateCloseButton(header, CloseShop);

        RectTransform body = new GameObject("Body", typeof(RectTransform), typeof(ResponsiveSplitLayout)).GetComponent<RectTransform>();
        body.SetParent(shell, false);
        body.anchorMin = new Vector2(0.03f, 0.05f);
        body.anchorMax = new Vector2(0.97f, 0.87f);
        body.offsetMin = Vector2.zero;
        body.offsetMax = Vector2.zero;

        ResponsiveSplitLayout split = body.GetComponent<ResponsiveSplitLayout>();
        split.keepFirstSectionWiderOnDesktop = true;
        split.firstSectionFlexibleWidth = 1f;
        split.secondSectionFlexibleWidth = 1.15f;

        RectTransform left = ModalUIRuntimeUtility.CreateSection(body, "UpgradeSection", ModalUIRuntimeUtility.SectionColor);
        RectTransform right = ModalUIRuntimeUtility.CreateSection(body, "WeaponSection", ModalUIRuntimeUtility.SectionAltColor);

        CreateSectionLabel(left, "Kalici Gelisim");
        ModalUIRuntimeUtility.NormalizeText(goldText, false, 28f);
        ModalUIRuntimeUtility.Wrap(goldText.rectTransform, left, "GoldRow", 32f);

        BuildUpgradeCard(left, "Can Gelisimi", healthInfoText, upgradeHealthButton);
        BuildUpgradeCard(left, "Hasar Gelisimi", damageInfoText, upgradeDamageButton);
        BuildUpgradeCard(left, "Tempo Gelisimi", tempoInfoText, upgradeTempoButton);

        CreateSectionLabel(right, "Silah Tezgahi");
        RectTransform listContent = weaponListParent as RectTransform;
        if (listContent != null)
            ModalUIRuntimeUtility.CreateScrollableList(right, "WeaponListViewport", listContent);

        if (weaponDetailPanel != null)
        {
            LayoutElement detailLayout = weaponDetailPanel.GetComponent<LayoutElement>();
            if (detailLayout == null)
                detailLayout = weaponDetailPanel.AddComponent<LayoutElement>();

            detailLayout.flexibleHeight = 1f;
            detailLayout.minHeight = 220f;
            weaponDetailPanel.transform.SetParent(right, false);
            RectTransform detailRect = weaponDetailPanel.GetComponent<RectTransform>();
            ModalUIRuntimeUtility.StretchHorizontally(detailRect);
        }

        ModalUIRuntimeUtility.NormalizeText(weaponDetailName, false, 30f);
        ModalUIRuntimeUtility.NormalizeText(weaponDetailStats, true, 110f);
        ModalUIRuntimeUtility.NormalizeText(weaponDetailPrice, false, 24f);
        ModalUIRuntimeUtility.NormalizeText(weaponBuyButtonText, false);
        ModalUIRuntimeUtility.NormalizeButton(weaponBuyButton, 50f);

        layoutBuilt = true;
    }

    private void BuildUpgradeCard(RectTransform parent, string title, TextMeshProUGUI infoText, Button button)
    {
        RectTransform card = ModalUIRuntimeUtility.CreateSection(parent, title.Replace(" ", string.Empty) + "Card", new Color(1f, 1f, 1f, 0.04f));
        LayoutElement cardLayout = card.GetComponent<LayoutElement>();
        cardLayout.minHeight = 128f;

        CreateSectionLabel(card, title, 19f);
        ModalUIRuntimeUtility.NormalizeText(infoText, true, 70f);
        ModalUIRuntimeUtility.Wrap(infoText.rectTransform, card, title.Replace(" ", string.Empty) + "Info", 76f);
        ModalUIRuntimeUtility.NormalizeButton(button, 50f);
        ModalUIRuntimeUtility.Wrap(button.GetComponent<RectTransform>(), card, title.Replace(" ", string.Empty) + "Button", 50f);
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

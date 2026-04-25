using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Demirci paneli. Responsive modal shell runtime'da kurulur, mevcut upgrade akisi korunur.
/// </summary>
public class BlacksmithUI : MonoBehaviour
{
    [Header("Silah Listesi (Sol Panel)")]
    public WeaponDatabase weaponDatabase;
    public Transform weaponListParent;
    public GameObject weaponButtonPrefab;

    [Header("Yukseltme Detay (Sag Panel)")]
    public TextMeshProUGUI weaponNameText;
    public TextMeshProUGUI currentStatsText;
    public TextMeshProUGUI nextStatsText;
    public TextMeshProUGUI costText;
    public TextMeshProUGUI successRateText;
    public TextMeshProUGUI resultText;
    public Button upgradeButton;
    public TextMeshProUGUI upgradeButtonText;

    [Header("Gold Display")]
    public TextMeshProUGUI goldText;

    private const string ModalId = "blacksmith";

    private WeaponSO selectedWeapon;
    private Canvas cachedCanvas;
    private bool layoutBuilt;
    private bool isOpen;
    private GameObject specializationSection;
    private TextMeshProUGUI milestoneInfoText;
    private TextMeshProUGUI specializationStatusText;
    private Button specializationButtonA;
    private Button specializationButtonB;
    private TextMeshProUGUI specializationButtonAText;
    private TextMeshProUGUI specializationButtonBText;

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
        RefreshAll();
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
            CloseBlacksmith();
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
        RefreshAll();
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
            goldText.text = "Altin: " + SaveManager.Instance.data.totalGold;
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

            int level = SaveManager.Instance.data.GetWeaponLevel(weapon.weaponName);

            GameObject btnObj = Instantiate(weaponButtonPrefab, weaponListParent);
            Button btn = btnObj.GetComponent<Button>();
            TextMeshProUGUI btnText = btnObj.GetComponentInChildren<TextMeshProUGUI>();

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
                    image.color = new Color(0.22f, 0.17f, 0.13f, 0.98f);

                WeaponSO cachedWeapon = weapon;
                btn.onClick.AddListener(() => ShowUpgradeDetail(cachedWeapon));
            }
        }
    }

    private void ShowUpgradeDetail(WeaponSO weapon)
    {
        if (weapon == null || SaveManager.Instance == null) return;
        selectedWeapon = weapon;

        int level = SaveManager.Instance.data.GetWeaponLevel(weapon.weaponName);
        string specializationId = SaveManager.Instance.data.GetWeaponSpecializationChoice(weapon.weaponName);
        WeaponResolvedStats currentStats = WeaponUpgradeResolver.Resolve(weapon, level, specializationId);
        WeaponResolvedStats nextStats = WeaponUpgradeResolver.Resolve(weapon, Mathf.Min(level + 1, WeaponSO.MaxUpgradeLevel), specializationId);
        WeaponMilestoneUpgradeData milestones = WeaponUpgradeResolver.GetMilestones(weapon);
        bool isMaxLevel = level >= WeaponSO.MaxUpgradeLevel;

        if (weaponNameText != null)
            weaponNameText.text = weapon.GetDisplayName(level);

        if (currentStatsText != null)
        {
            currentStatsText.text =
                "Hasar: " + currentStats.damage.ToString("F1") +
                "\nSaldiri Hizi: " + currentStats.attackRate.ToString("F2") +
                "\nMenzil: " + currentStats.range.ToString("F1");
        }

        if (nextStatsText != null)
        {
            if (isMaxLevel)
            {
                nextStatsText.text = "Maksimum Seviye!";
            }
            else
            {
                int nextLevel = level + 1;
                nextStatsText.text = "+" + nextLevel + " Sonrasi:" +
                    "\nHasar: " + nextStats.damage.ToString("F1") +
                    "\nSaldiri Hizi: " + nextStats.attackRate.ToString("F2") +
                    "\nMenzil: " + nextStats.range.ToString("F1");
            }
        }

        if (costText != null)
            costText.text = isMaxLevel ? "" : "Maliyet: " + weapon.GetUpgradeCost(level) + "g";

        if (successRateText != null)
            successRateText.text = isMaxLevel ? "" : "Basari: %" + Mathf.RoundToInt(weapon.GetSuccessRate(level) * 100);

        if (resultText != null)
            resultText.text = "";

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
                    upgradeButtonText.text = "Yukselt (+" + (level + 1) + ")";

                upgradeButton.interactable = true;
                WeaponSO cachedWeapon = weapon;
                upgradeButton.onClick.AddListener(() => AttemptUpgrade(cachedWeapon));
            }
        }

        RefreshMilestoneSection(weapon, level, specializationId, milestones, currentStats);
    }

    private void AttemptUpgrade(WeaponSO weapon)
    {
        if (weapon == null || SaveManager.Instance == null) return;

        int level = SaveManager.Instance.data.GetWeaponLevel(weapon.weaponName);
        if (level >= WeaponSO.MaxUpgradeLevel) return;

        int cost = weapon.GetUpgradeCost(level);
        float successRate = weapon.GetSuccessRate(level);

        if (SaveManager.Instance.data.totalGold < cost)
        {
            if (resultText != null)
                resultText.text = "<color=red>Yeterli altin yok!</color>";
            return;
        }

        SaveManager.Instance.SpendGold(cost);

        if (Random.Range(0f, 1f) <= successRate)
        {
            SaveManager.Instance.data.SetWeaponLevel(weapon.weaponName, level + 1);
            SaveManager.Instance.Save();

            if (resultText != null)
                resultText.text = "<color=green>Yukseltme basarili! +" + (level + 1) + "</color>";
        }
        else
        {
            SaveManager.Instance.Save();

            if (resultText != null)
                resultText.text = "<color=red>Yukseltme basarisiz! Silah ayni kaldi.</color>";
        }

        RefreshAll();
    }

    private void RefreshMilestoneSection(WeaponSO weapon, int level, string specializationId, WeaponMilestoneUpgradeData milestones, WeaponResolvedStats currentStats)
    {
        if (specializationSection == null)
            return;

        specializationSection.SetActive(true);

        string level3Label = string.IsNullOrWhiteSpace(milestones?.level3?.label) ? "+3 Reinforcement" : milestones.level3.label;
        string level6Label = string.IsNullOrWhiteSpace(milestones?.level6?.label) ? "+6 Reinforcement" : milestones.level6.label;

        if (milestoneInfoText != null)
        {
            if (level < 3)
            {
                milestoneInfoText.text = "Yaklasan milestone: +3 -> " + level3Label;
            }
            else if (level < 6)
            {
                milestoneInfoText.text = "Aktif: " + level3Label + "\nSonraki: +6 -> " + level6Label;
            }
            else if (level < WeaponSO.MaxUpgradeLevel)
            {
                milestoneInfoText.text = "Aktif: " + level3Label + " / " + level6Label + "\n+10'da specialization secimi acilir.";
            }
            else
            {
                milestoneInfoText.text = "Milestone: " + currentStats.milestoneLabel;
            }
        }

        WeaponSpecializationChoiceData[] choices = milestones != null ? milestones.level9Choices : null;
        bool hasChoices = choices != null && choices.Length > 0;
        bool pendingChoice = level >= WeaponSO.MaxUpgradeLevel && string.IsNullOrWhiteSpace(specializationId) && hasChoices;

        if (specializationStatusText != null)
        {
            if (level < WeaponSO.MaxUpgradeLevel)
            {
                specializationStatusText.text = "Specialization +10'da acilir.";
            }
            else if (pendingChoice)
            {
                specializationStatusText.text = "Bir specialization sec. Bu secim silaha kaydedilir.";
            }
            else
            {
                specializationStatusText.text = string.IsNullOrWhiteSpace(currentStats.specializationName)
                    ? "Specialization secilmedi."
                    : "Secili specialization: " + currentStats.specializationName;
            }
        }

        ConfigureSpecializationButton(specializationButtonA, specializationButtonAText, weapon, choices, 0, pendingChoice);
        ConfigureSpecializationButton(specializationButtonB, specializationButtonBText, weapon, choices, 1, pendingChoice);
    }

    private void ConfigureSpecializationButton(Button button, TextMeshProUGUI label, WeaponSO weapon, WeaponSpecializationChoiceData[] choices, int index, bool enabled)
    {
        if (button == null)
            return;

        button.onClick.RemoveAllListeners();

        WeaponSpecializationChoiceData choice = choices != null && index < choices.Length ? choices[index] : null;
        bool isValidChoice = choice != null && !string.IsNullOrWhiteSpace(choice.choiceId);
        button.gameObject.SetActive(enabled && isValidChoice);
        button.interactable = enabled && isValidChoice;

        if (label != null && isValidChoice)
            label.text = choice.displayName + "\n<size=70%>" + choice.description + "</size>";

        if (enabled && isValidChoice)
        {
            button.onClick.AddListener(() => SelectSpecialization(weapon, choice.choiceId));
        }
    }

    private void SelectSpecialization(WeaponSO weapon, string choiceId)
    {
        if (weapon == null || SaveManager.Instance == null || string.IsNullOrWhiteSpace(choiceId))
            return;

        SaveManager.Instance.data.SetWeaponSpecializationChoice(weapon.weaponName, choiceId);
        SaveManager.Instance.Save();

        if (resultText != null)
            resultText.text = "<color=green>Specialization secildi.</color>";

        RefreshAll();
    }

    private void ClearDetail()
    {
        if (weaponNameText != null) weaponNameText.text = "Bir silah secin";
        if (currentStatsText != null) currentStatsText.text = "";
        if (nextStatsText != null) nextStatsText.text = "";
        if (costText != null) costText.text = "";
        if (successRateText != null) successRateText.text = "";
        if (resultText != null) resultText.text = "";
        if (upgradeButton != null) upgradeButton.interactable = false;
        if (specializationSection != null) specializationSection.SetActive(false);
    }

    public void CloseBlacksmith()
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
            "BlacksmithShell",
            new Color(0.11f, 0.08f, 0.07f, 0.98f),
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

        CreateSectionLabel(header, "DEMIRCI", 30f);
        ModalUIRuntimeUtility.CreateCloseButton(header, CloseBlacksmith);

        RectTransform body = new GameObject("Body", typeof(RectTransform), typeof(ResponsiveSplitLayout)).GetComponent<RectTransform>();
        body.SetParent(shell, false);
        body.anchorMin = new Vector2(0.03f, 0.05f);
        body.anchorMax = new Vector2(0.97f, 0.87f);
        body.offsetMin = Vector2.zero;
        body.offsetMax = Vector2.zero;

        ResponsiveSplitLayout split = body.GetComponent<ResponsiveSplitLayout>();
        split.keepFirstSectionWiderOnDesktop = false;
        split.secondSectionFlexibleWidth = 1.2f;

        RectTransform left = ModalUIRuntimeUtility.CreateSection(body, "OwnedWeaponsSection", ModalUIRuntimeUtility.SectionColor);
        RectTransform right = ModalUIRuntimeUtility.CreateSection(body, "UpgradeSection", ModalUIRuntimeUtility.SectionAltColor);

        CreateSectionLabel(left, "Silahlar");
        ModalUIRuntimeUtility.NormalizeText(goldText, false, 28f);
        ModalUIRuntimeUtility.Wrap(goldText.rectTransform, left, "GoldRow", 32f);

        RectTransform listContent = weaponListParent as RectTransform;
        if (listContent != null)
            ModalUIRuntimeUtility.CreateScrollableList(left, "WeaponListViewport", listContent);

        CreateSectionLabel(right, "Yukseltme Detayi");
        ModalUIRuntimeUtility.NormalizeText(weaponNameText, false, 34f);
        ModalUIRuntimeUtility.NormalizeText(currentStatsText, true, 84f);
        ModalUIRuntimeUtility.NormalizeText(nextStatsText, true, 84f);
        ModalUIRuntimeUtility.NormalizeText(costText, false, 24f);
        ModalUIRuntimeUtility.NormalizeText(successRateText, false, 24f);
        ModalUIRuntimeUtility.NormalizeText(resultText, true, 44f);
        ModalUIRuntimeUtility.NormalizeText(upgradeButtonText, false);
        ModalUIRuntimeUtility.NormalizeButton(upgradeButton, 52f);

        ModalUIRuntimeUtility.Wrap(weaponNameText.rectTransform, right, "WeaponNameRow", 38f);
        ModalUIRuntimeUtility.Wrap(currentStatsText.rectTransform, right, "CurrentStatsRow", 86f);
        ModalUIRuntimeUtility.Wrap(nextStatsText.rectTransform, right, "NextStatsRow", 86f);

        RectTransform metaRow = ModalUIRuntimeUtility.CreateRow(right, "MetaRow", 32f);
        ModalUIRuntimeUtility.Wrap(costText.rectTransform, metaRow, "CostWrap", 30f);
        ModalUIRuntimeUtility.Wrap(successRateText.rectTransform, metaRow, "SuccessWrap", 30f);

        RectTransform specializationRect = ModalUIRuntimeUtility.CreateSection(right, "SpecializationSection", new Color(0.15f, 0.10f, 0.09f, 0.96f));
        specializationSection = specializationRect.gameObject;
        LayoutElement specializationLayout = specializationSection.GetComponent<LayoutElement>();
        if (specializationLayout != null)
            specializationLayout.minHeight = 148f;

        milestoneInfoText = CreateRuntimeText(specializationRect, "MilestoneInfo", 22f, true);
        specializationStatusText = CreateRuntimeText(specializationRect, "SpecializationStatus", 20f, true);
        RectTransform specButtonsRow = ModalUIRuntimeUtility.CreateRow(specializationRect, "SpecializationButtons", 64f);
        specializationButtonA = CreateRuntimeButton(specButtonsRow, "SpecChoiceA", out specializationButtonAText);
        specializationButtonB = CreateRuntimeButton(specButtonsRow, "SpecChoiceB", out specializationButtonBText);
        specializationSection.SetActive(false);
        ModalUIRuntimeUtility.Wrap(resultText.rectTransform, right, "ResultRow", 48f);
        ModalUIRuntimeUtility.Wrap(upgradeButton.GetComponent<RectTransform>(), right, "UpgradeButtonRow", 52f);

        layoutBuilt = true;
    }

    private static TextMeshProUGUI CreateRuntimeText(RectTransform parent, string name, float fontSize, bool richText)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI), typeof(LayoutElement));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        ModalUIRuntimeUtility.StretchHorizontally(rect);

        TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.enableWordWrapping = true;
        text.richText = richText;
        text.color = new Color(0.92f, 0.93f, 0.98f, 1f);

        LayoutElement layout = go.GetComponent<LayoutElement>();
        layout.minHeight = fontSize + 10f;
        layout.flexibleWidth = 1f;
        return text;
    }

    private static Button CreateRuntimeButton(RectTransform parent, string name, out TextMeshProUGUI label)
    {
        GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(LayoutElement));
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.30f, 0.18f, 0.12f, 0.98f);

        LayoutElement layout = buttonObject.GetComponent<LayoutElement>();
        layout.minHeight = 64f;
        layout.flexibleWidth = 1f;

        GameObject textObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.SetParent(rect, false);
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(10f, 6f);
        textRect.offsetMax = new Vector2(-10f, -6f);

        label = textObject.GetComponent<TextMeshProUGUI>();
        label.alignment = TextAlignmentOptions.Center;
        label.enableWordWrapping = true;
        label.fontSize = 17f;
        label.color = new Color(0.95f, 0.97f, 1f, 1f);
        label.richText = true;

        Button button = buttonObject.GetComponent<Button>();
        ModalUIRuntimeUtility.NormalizeButton(button, 64f);
        return button;
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

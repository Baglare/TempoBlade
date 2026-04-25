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
        bool isMaxLevel = level >= 9;

        if (weaponNameText != null)
            weaponNameText.text = weapon.GetDisplayName(level);

        if (currentStatsText != null)
        {
            currentStatsText.text =
                "Hasar: " + weapon.GetUpgradedDamage(level).ToString("F1") +
                "\nSaldiri Hizi: " + weapon.GetUpgradedAttackRate(level).ToString("F2") +
                "\nMenzil: " + weapon.GetUpgradedRange(level).ToString("F1");
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
                    "\nHasar: " + weapon.GetUpgradedDamage(nextLevel).ToString("F1") +
                    "\nSaldiri Hizi: " + weapon.GetUpgradedAttackRate(nextLevel).ToString("F2") +
                    "\nMenzil: " + weapon.GetUpgradedRange(nextLevel).ToString("F1");
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
    }

    private void AttemptUpgrade(WeaponSO weapon)
    {
        if (weapon == null || SaveManager.Instance == null) return;

        int level = SaveManager.Instance.data.GetWeaponLevel(weapon.weaponName);
        if (level >= 9) return;

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

    private void ClearDetail()
    {
        if (weaponNameText != null) weaponNameText.text = "Bir silah secin";
        if (currentStatsText != null) currentStatsText.text = "";
        if (nextStatsText != null) nextStatsText.text = "";
        if (costText != null) costText.text = "";
        if (successRateText != null) successRateText.text = "";
        if (resultText != null) resultText.text = "";
        if (upgradeButton != null) upgradeButton.interactable = false;
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

        ModalUIRuntimeUtility.Wrap(resultText.rectTransform, right, "ResultRow", 48f);
        ModalUIRuntimeUtility.Wrap(upgradeButton.GetComponent<RectTransform>(), right, "UpgradeButtonRow", 52f);

        layoutBuilt = true;
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

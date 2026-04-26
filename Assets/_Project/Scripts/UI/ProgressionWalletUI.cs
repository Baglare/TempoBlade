using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ProgressionWalletUI : MonoBehaviour
{
    private const string ModalId = "progression_wallet";

    public static ProgressionWalletUI Instance { get; private set; }

    [SerializeField] private Key walletHotkey = Key.O;

    private Canvas cachedCanvas;
    private GameObject panelRoot;
    private TextMeshProUGUI persistentText;
    private TextMeshProUGUI runBankText;
    private bool layoutBuilt;
    private bool isOpen;

    public static void EnsureInstance()
    {
        if (Instance != null)
            return;

        GameObject root = new GameObject("ProgressionWalletUI", typeof(RectTransform), typeof(Canvas));
        DontDestroyOnLoad(root);
        Instance = root.AddComponent<ProgressionWalletUI>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        cachedCanvas = gameObject.GetComponent<Canvas>();
        if (cachedCanvas == null)
            cachedCanvas = gameObject.AddComponent<Canvas>();

        EnsureLayout();
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    private void Update()
    {
        if (Keyboard.current == null)
            return;

        if (!IsInHub())
        {
            if (isOpen)
                ClosePanel();
            return;
        }

        if (isOpen && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ClosePanel();
            return;
        }

        if (Keyboard.current[walletHotkey].wasPressedThisFrame)
        {
            if (isOpen)
                ClosePanel();
            else
                OpenPanel();
        }
    }

    private void OpenPanel()
    {
        EnsureLayout();
        if (panelRoot == null)
            return;

        if (!ModalUIManager.Instance.TryOpenModal(ModalId, panelRoot))
            return;

        RefreshTexts();
        panelRoot.SetActive(true);
        isOpen = true;
    }

    private void ClosePanel()
    {
        if (panelRoot == null)
            return;

        isOpen = false;
        ModalUIManager.Instance.CloseModal(ModalId);
        panelRoot.SetActive(false);
    }

    private void EnsureLayout()
    {
        if (layoutBuilt)
            return;

        ModalUIRuntimeUtility.EnsureFullscreenCanvas(cachedCanvas);

        panelRoot = new GameObject("ProgressionWalletModal", typeof(RectTransform));
        panelRoot.transform.SetParent(transform, false);
        RectTransform rootRect = panelRoot.GetComponent<RectTransform>();
        ModalUIRuntimeUtility.Stretch(rootRect);

        RectTransform overlay = ModalUIRuntimeUtility.CreateOrGetOverlayRoot(panelRoot.transform, "ModalOverlay");
        RectTransform shell = ModalUIRuntimeUtility.CreateCard(
            overlay,
            "WalletShell",
            new Color(0.08f, 0.10f, 0.14f, 0.98f),
            new Vector2(0.22f, 0.18f),
            new Vector2(0.78f, 0.82f),
            Vector2.zero,
            Vector2.zero);

        RectTransform header = new GameObject("Header", typeof(RectTransform)).GetComponent<RectTransform>();
        header.SetParent(shell, false);
        header.anchorMin = new Vector2(0.04f, 0.87f);
        header.anchorMax = new Vector2(0.96f, 0.97f);
        header.offsetMin = Vector2.zero;
        header.offsetMax = Vector2.zero;

        ModalUIRuntimeUtility.CreateTitle(header, "PROGRESSION WALLET");
        ModalUIRuntimeUtility.CreateCloseButton(header, ClosePanel);

        RectTransform body = new GameObject("Body", typeof(RectTransform), typeof(ResponsiveSplitLayout)).GetComponent<RectTransform>();
        body.SetParent(shell, false);
        body.anchorMin = new Vector2(0.04f, 0.06f);
        body.anchorMax = new Vector2(0.96f, 0.84f);
        body.offsetMin = Vector2.zero;
        body.offsetMax = Vector2.zero;

        ResponsiveSplitLayout split = body.GetComponent<ResponsiveSplitLayout>();
        split.keepFirstSectionWiderOnDesktop = false;
        split.firstSectionFlexibleWidth = 1f;
        split.secondSectionFlexibleWidth = 1f;

        RectTransform left = ModalUIRuntimeUtility.CreateSection(body, "PersistentSection", ModalUIRuntimeUtility.SectionColor);
        RectTransform right = ModalUIRuntimeUtility.CreateSection(body, "RunBankSection", ModalUIRuntimeUtility.SectionAltColor);

        CreateSectionLabel(left, "Kalici Kaynaklar");
        persistentText = CreateBodyText(left, "PersistentText");

        CreateSectionLabel(right, "Run Bank");
        runBankText = CreateBodyText(right, "RunBankText");

        layoutBuilt = true;
    }

    private void RefreshTexts()
    {
        if (persistentText != null)
            persistentText.text = BuildWalletBlock(ProgressionResourceWalletService.GetPersistentWallet(), "Kalici");

        if (runBankText != null)
            runBankText.text = BuildWalletBlock(RunResourceBankService.GetRunBank(), "Run");
    }

    private static TextMeshProUGUI CreateBodyText(RectTransform parent, string name)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI), typeof(LayoutElement));
        go.transform.SetParent(parent, false);

        TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
        text.fontSize = 22f;
        text.color = ModalUIRuntimeUtility.BodyTextColor;
        text.alignment = TextAlignmentOptions.TopLeft;
        text.enableWordWrapping = true;

        LayoutElement layout = go.GetComponent<LayoutElement>();
        layout.flexibleWidth = 1f;
        layout.flexibleHeight = 1f;

        RectTransform rect = go.GetComponent<RectTransform>();
        ModalUIRuntimeUtility.StretchHorizontally(rect);
        return text;
    }

    private static void CreateSectionLabel(RectTransform parent, string title)
    {
        GameObject go = new GameObject(title.Replace(" ", string.Empty) + "Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI), typeof(LayoutElement));
        go.transform.SetParent(parent, false);

        TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
        text.text = title;
        text.fontSize = 24f;
        text.fontStyle = FontStyles.Bold;
        text.color = ModalUIRuntimeUtility.HeaderTextColor;
        text.alignment = TextAlignmentOptions.MidlineLeft;

        LayoutElement layout = go.GetComponent<LayoutElement>();
        layout.minHeight = 34f;
        layout.flexibleWidth = 1f;

        RectTransform rect = go.GetComponent<RectTransform>();
        ModalUIRuntimeUtility.StretchHorizontally(rect);
    }

    private static string BuildWalletBlock(PersistentResourceWalletState wallet, string title)
    {
        if (wallet == null)
            return title + ": Veri yok";

        StringBuilder sb = new StringBuilder();
        AppendEntries(sb, wallet.entries);
        return sb.Length == 0 ? "Kayitli kaynak yok." : sb.ToString();
    }

    private static string BuildWalletBlock(RunResourceBankState bank, string title)
    {
        if (bank == null)
            return title + ": Veri yok";

        StringBuilder sb = new StringBuilder();
        AppendEntries(sb, bank.entries);
        if (bank.wasDepositedThisRun)
            sb.AppendLine("Durum: Bu run kaynaklari yatirildi.");
        return sb.Length == 0 ? "Bankalanmis kaynak yok." : sb.ToString().TrimEnd();
    }

    private static void AppendEntries(StringBuilder sb, System.Collections.Generic.List<ProgressionResourceEntry> entries)
    {
        if (entries == null)
            return;

        for (int i = 0; i < entries.Count; i++)
        {
            ProgressionResourceEntry entry = entries[i];
            if (entry == null)
                continue;

            sb.AppendLine(ProgressionResourceUtility.GetDisplayName(entry.resourceType) + ": " + Mathf.Max(0, entry.amount));
        }
    }

    private bool IsInHub()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        return sceneName.Contains("Hub") || sceneName.Contains("hub");
    }
}

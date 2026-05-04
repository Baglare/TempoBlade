using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class SkillTreePanelUI : MonoBehaviour
{
    [Header("Input")]
    public Key toggleKey = Key.Tab;

    [Header("Axis References")]
    public ProgressionAxisSO dashAxis;
    public ProgressionAxisSO parryAxis;
    public ProgressionAxisSO overdriveAxis;
    public ProgressionAxisSO cadenceAxis;

    [Header("Theme")]
    public Color overlayColor = new Color(0.03f, 0.04f, 0.07f, 0.92f);
    public Color panelColor = new Color(0.08f, 0.10f, 0.15f, 0.98f);
    public Color cardColor = new Color(0.11f, 0.14f, 0.20f, 0.98f);
    public Color cardAltColor = new Color(0.09f, 0.12f, 0.17f, 0.98f);
    public Color lockedColor = new Color(0.31f, 0.33f, 0.39f, 1f);
    public Color unlockableColor = new Color(0.92f, 0.74f, 0.26f, 1f);
    public Color unlockedColor = new Color(0.20f, 0.85f, 0.56f, 1f);
    public Color selectedColor = new Color(0.35f, 0.72f, 1f, 1f);
    public Color lineLockedColor = new Color(0.35f, 0.39f, 0.48f, 0.55f);
    public Color lineUnlockedColor = new Color(0.20f, 0.80f, 0.58f, 0.85f);
    public Color textPrimary = new Color(0.96f, 0.97f, 1f, 1f);
    public Color textSecondary = new Color(0.74f, 0.80f, 0.90f, 1f);

    [Header("Layout")]
    public Vector2 nodeSize = new Vector2(82f, 70f);
    public float horizontalSpacing = 18f;
    public float verticalSpacing = 6f;
    public float lineWidth = 3f;

    private Canvas canvas;
    private GameObject panelRoot;
    private RectTransform nodeContainer;
    private RectTransform lineContainer;
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI modeText;
    private TextMeshProUGUI axisStatsText;
    private TextMeshProUGUI axisChoiceText;
    private TextMeshProUGUI selectionTitleText;
    private TextMeshProUGUI selectionStatusText;
    private TextMeshProUGUI selectionDescriptionText;
    private TextMeshProUGUI selectionMetaText;
    private TextMeshProUGUI selectionRequirementText;
    private TextMeshProUGUI treeSummaryText;
    private Image selectionIconFrame;
    private Image selectionIconImage;
    private TextMeshProUGUI selectionIconFallback;
    private Button unlockButton;
    private TextMeshProUGUI unlockButtonLabel;
    private bool isOpen;
    private GameManager.GameState previousGameState = GameManager.GameState.Gameplay;
    private ProgressionAxisSO activeAxis;
    private SkillNodeSO selectedNode;

    private readonly Dictionary<string, NodeSlot> slots = new Dictionary<string, NodeSlot>();
    private readonly Dictionary<string, AxisButtonSlot> axisButtons = new Dictionary<string, AxisButtonSlot>();
    private readonly List<LineConnection> lines = new List<LineConnection>();

    private class NodeSlot
    {
        public SkillNodeSO node;
        public RectTransform rect;
        public Image border;
        public Image fill;
        public Image iconImage;
        public TextMeshProUGUI iconFallback;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI statusText;
        public Image selectionFrame;
        public Button button;
    }

    private class AxisButtonSlot
    {
        public ProgressionAxisSO axis;
        public Image background;
        public TextMeshProUGUI title;
        public TextMeshProUGUI subtitle;
    }

    private class LineConnection
    {
        public RectTransform rect;
        public Image image;
        public string fromId;
        public string toId;
    }

    private static readonly Dictionary<string, Vector2Int> DashGrid = new Dictionary<string, Vector2Int>
    {
        { "dash_t1_ranged_dodge", new Vector2Int(0, 0) },
        { "dash_t1_melee_dodge", new Vector2Int(1, 0) },
        { "dash_t1_tempo_gain", new Vector2Int(3, 0) },
        { "dash_t1_counter", new Vector2Int(0, 1) },
        { "dash_t1_attack_speed", new Vector2Int(3, 1) },
        { "dash_t2_commitment", new Vector2Int(2, 2) },
        { "dash_t2h_hunt_mark", new Vector2Int(0, 3) },
        { "dash_t2h_blind_spot", new Vector2Int(0, 4) },
        { "dash_t2h_hunt_flow", new Vector2Int(1, 4) },
        { "dash_t2h_execute", new Vector2Int(0, 5) },
        { "dash_t2h_hunt_cycle", new Vector2Int(0, 6) },
        { "dash_t2f_flow_mark", new Vector2Int(3, 3) },
        { "dash_t2f_snapback", new Vector2Int(4, 3) },
        { "dash_t2f_chain_bounce", new Vector2Int(3, 4) },
        { "dash_t2f_black_hole", new Vector2Int(3, 5) },
        { "dash_t2f_burst", new Vector2Int(4, 5) }
    };

    private static readonly Dictionary<string, Vector2Int> ParryGrid = new Dictionary<string, Vector2Int>
    {
        { "parry_t1_reflect", new Vector2Int(0, 0) },
        { "parry_t1_perfect_timing", new Vector2Int(3, 0) },
        { "parry_t1_counter_stance", new Vector2Int(0, 1) },
        { "parry_t1_perfect_break", new Vector2Int(3, 1) },
        { "parry_t1_rhythm_return", new Vector2Int(2, 2) },
        { "parry_t2_commitment", new Vector2Int(2, 3) },
        { "parry_t2b_reverse_front", new Vector2Int(0, 4) },
        { "parry_t2b_overdeflect", new Vector2Int(1, 4) },
        { "parry_t2b_suppressive_trace", new Vector2Int(0, 5) },
        { "parry_t2b_fractured_orbit", new Vector2Int(1, 5) },
        { "parry_t2b_feedback", new Vector2Int(0, 6) },
        { "parry_t2p_close_execute", new Vector2Int(3, 4) },
        { "parry_t2p_fine_edge", new Vector2Int(4, 4) },
        { "parry_t2p_heavy_riposte", new Vector2Int(3, 5) },
        { "parry_t2p_rotating_cone", new Vector2Int(4, 5) },
        { "parry_t2p_perfect_cycle", new Vector2Int(4, 6) }
    };

    private static readonly Dictionary<string, Vector2Int> OverdriveGrid = new Dictionary<string, Vector2Int>
    {
        { "overdrive_t1_heat_buildup", new Vector2Int(0, 0) },
        { "overdrive_t1_threshold_burst", new Vector2Int(1, 0) },
        { "overdrive_t1_red_pressure", new Vector2Int(2, 0) },
        { "overdrive_t1_overflow_impulse", new Vector2Int(3, 0) },
        { "overdrive_t1_final_push", new Vector2Int(4, 0) },
        { "overdrive_t2_commitment", new Vector2Int(2, 2) },
        { "overdrive_t2burst_short_circuit", new Vector2Int(0, 3) },
        { "overdrive_t2burst_red_window", new Vector2Int(1, 3) },
        { "overdrive_t2burst_threshold_echo", new Vector2Int(0, 4) },
        { "overdrive_t2burst_pressure_break", new Vector2Int(1, 4) },
        { "overdrive_t2burst_final_flare", new Vector2Int(0, 5) },
        { "overdrive_t2pred_blood_scent", new Vector2Int(3, 3) },
        { "overdrive_t2pred_choking_proximity", new Vector2Int(4, 3) },
        { "overdrive_t2pred_predator_angle", new Vector2Int(3, 4) },
        { "overdrive_t2pred_pack_breaker", new Vector2Int(4, 4) },
        { "overdrive_t2pred_execute_pressure", new Vector2Int(4, 5) }
    };

    private static readonly Dictionary<string, Vector2Int> CadenceGrid = new Dictionary<string, Vector2Int>
    {
        { "cadence_t1_steady_pulse", new Vector2Int(0, 0) },
        { "cadence_t1_transition_rhythm", new Vector2Int(1, 0) },
        { "cadence_t1_soft_fall", new Vector2Int(2, 0) },
        { "cadence_t1_measured_power", new Vector2Int(3, 0) },
        { "cadence_t1_rhythm_shield", new Vector2Int(4, 0) },
        { "cadence_t2_commitment", new Vector2Int(2, 2) },
        { "cadence_t2measured_measure_line", new Vector2Int(0, 3) },
        { "cadence_t2measured_balance_point", new Vector2Int(1, 3) },
        { "cadence_t2measured_timed_accent", new Vector2Int(0, 4) },
        { "cadence_t2measured_recovery_return", new Vector2Int(1, 4) },
        { "cadence_t2measured_perfect_measure", new Vector2Int(0, 5) },
        { "cadence_t2flow_flow_ring", new Vector2Int(3, 3) },
        { "cadence_t2flow_sliding_continuation", new Vector2Int(4, 3) },
        { "cadence_t2flow_wave_bounce", new Vector2Int(3, 4) },
        { "cadence_t2flow_threshold_surf", new Vector2Int(4, 4) },
        { "cadence_t2flow_overflow_harmony", new Vector2Int(4, 5) }
    };

    public bool IsOpen => isOpen;

    private void Start()
    {
        BuildShell();
        SetActiveAxis(GetFirstConfiguredAxis());
        panelRoot.SetActive(false);
        isOpen = false;
    }

    private void OnEnable()
    {
        if (AxisProgressionManager.Instance != null)
        {
            AxisProgressionManager.Instance.OnNodeStatusChanged += HandleNodeStatusChanged;
            AxisProgressionManager.Instance.OnTreeProgressChanged += HandleTreeProgressChanged;
            AxisProgressionManager.Instance.OnInteractionModeChanged += HandleInteractionModeChanged;
        }
    }

    private void OnDisable()
    {
        if (AxisProgressionManager.Instance != null)
        {
            AxisProgressionManager.Instance.OnNodeStatusChanged -= HandleNodeStatusChanged;
            AxisProgressionManager.Instance.OnTreeProgressChanged -= HandleTreeProgressChanged;
            AxisProgressionManager.Instance.OnInteractionModeChanged -= HandleInteractionModeChanged;
        }
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current[toggleKey].wasPressedThisFrame)
            TogglePanel();

        if (isOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            ClosePanel();
    }

    public void TogglePanel()
    {
        if (isOpen)
            ClosePanel();
        else
            OpenPanel();
    }

    private void OpenPanel()
    {
        if (ModalUIManager.HasOpenModal)
            return;

        isOpen = true;
        panelRoot.SetActive(true);
        RefreshAll();

        if (GameManager.Instance != null)
        {
            previousGameState = GameManager.Instance.CurrentState;
            GameManager.Instance.SetState(GameManager.GameState.Paused);
        }
        else
        {
            Time.timeScale = 0f;
        }
    }

    private void ClosePanel()
    {
        isOpen = false;
        panelRoot.SetActive(false);

        if (GameManager.Instance != null)
            GameManager.Instance.SetState(previousGameState);
        else
            Time.timeScale = 1f;
    }

    private void BuildShell()
    {
        GameObject canvasGo = new GameObject("SkillTreeCanvas");
        canvasGo.transform.SetParent(transform, false);
        canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 140;
        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasGo.AddComponent<GraphicRaycaster>();

        panelRoot = CreatePanel(canvasGo.transform, "SkillTreeRoot", overlayColor);
        RectTransform rootRect = panelRoot.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        GameObject panel = CreatePanel(panelRoot.transform, "MainPanel", panelColor);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.02f, 0.03f);
        panelRect.anchorMax = new Vector2(0.98f, 0.97f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        titleText = CreateText(panel.transform, "Title", "SKILL TREE", 32, FontStyles.Bold, textPrimary);
        RectTransform titleRect = titleText.rectTransform;
        titleRect.anchorMin = new Vector2(0.03f, 1f);
        titleRect.anchorMax = new Vector2(0.4f, 1f);
        titleRect.pivot = new Vector2(0f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -18f);
        titleRect.sizeDelta = new Vector2(0f, 38f);

        Button closeButton = CreateButton(panel.transform, "CloseButton", "X", new Vector2(42f, 42f), new Color(0.75f, 0.18f, 0.18f, 0.96f), ClosePanel);
        RectTransform closeRect = closeButton.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(1f, 1f);
        closeRect.anchorMax = new Vector2(1f, 1f);
        closeRect.pivot = new Vector2(1f, 1f);
        closeRect.anchoredPosition = new Vector2(-16f, -16f);

        RectTransform leftSidebar = CreateSection(panel.transform, "LeftSidebar", new Vector2(0.02f, 0.08f), new Vector2(0.23f, 0.90f), cardAltColor);
        RectTransform centerPanel = CreateSection(panel.transform, "CenterPanel", new Vector2(0.25f, 0.08f), new Vector2(0.73f, 0.90f), cardColor);
        RectTransform rightPanel = CreateSection(panel.transform, "RightPanel", new Vector2(0.75f, 0.08f), new Vector2(0.98f, 0.90f), cardAltColor);

        BuildLeftSidebar(leftSidebar);
        BuildCenterPanel(centerPanel);
        BuildRightPanel(rightPanel);
    }

    private void BuildLeftSidebar(RectTransform parent)
    {
        axisChoiceText = CreateText(parent, "AxisChoiceTitle", "AGAC SECIMI", 18, FontStyles.Bold, textPrimary);
        RectTransform axisChoiceRect = axisChoiceText.rectTransform;
        axisChoiceRect.anchorMin = new Vector2(0f, 1f);
        axisChoiceRect.anchorMax = new Vector2(1f, 1f);
        axisChoiceRect.pivot = new Vector2(0.5f, 1f);
        axisChoiceRect.anchoredPosition = new Vector2(0f, -18f);
        axisChoiceRect.offsetMin = new Vector2(18f, 0f);
        axisChoiceRect.offsetMax = new Vector2(-18f, 0f);
        axisChoiceRect.sizeDelta = new Vector2(0f, 24f);

        GameObject axisList = new GameObject("AxisList");
        axisList.transform.SetParent(parent, false);
        RectTransform axisListRect = axisList.AddComponent<RectTransform>();
        axisListRect.anchorMin = new Vector2(0f, 0.62f);
        axisListRect.anchorMax = new Vector2(1f, 0.94f);
        axisListRect.offsetMin = new Vector2(18f, 0f);
        axisListRect.offsetMax = new Vector2(-18f, 0f);
        VerticalLayoutGroup axisLayout = axisList.AddComponent<VerticalLayoutGroup>();
        axisLayout.spacing = 12f;
        axisLayout.childControlWidth = true;
        axisLayout.childControlHeight = true;
        axisLayout.childForceExpandWidth = true;
        axisLayout.childForceExpandHeight = false;
        axisLayout.padding = new RectOffset(0, 0, 0, 0);

        foreach (ProgressionAxisSO axis in GetConfiguredAxes())
        {
            GameObject buttonGo = CreatePanel(axisList.transform, $"{axis.axisId}_Button", new Color(0.16f, 0.19f, 0.27f, 1f));
            RectTransform rect = buttonGo.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.sizeDelta = new Vector2(0f, 72f);
            LayoutElement buttonLayout = buttonGo.AddComponent<LayoutElement>();
            buttonLayout.preferredHeight = 72f;

            Button button = buttonGo.AddComponent<Button>();
            ProgressionAxisSO capturedAxis = axis;
            button.onClick.AddListener(() => SetActiveAxis(capturedAxis));

            TextMeshProUGUI title = CreateText(buttonGo.transform, "Title", axis.displayName.ToUpperInvariant(), 18, FontStyles.Bold, textPrimary);
            RectTransform titleRect = title.rectTransform;
            titleRect.anchorMin = new Vector2(0f, 0.5f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.offsetMin = new Vector2(14f, -2f);
            titleRect.offsetMax = new Vector2(-14f, -6f);

            TextMeshProUGUI subtitle = CreateText(buttonGo.transform, "Subtitle", "", 12, FontStyles.Normal, textSecondary);
            RectTransform subtitleRect = subtitle.rectTransform;
            subtitleRect.anchorMin = new Vector2(0f, 0f);
            subtitleRect.anchorMax = new Vector2(1f, 0.5f);
            subtitleRect.offsetMin = new Vector2(14f, 6f);
            subtitleRect.offsetMax = new Vector2(-14f, -2f);

            axisButtons[axis.axisId] = new AxisButtonSlot
            {
                axis = axis,
                background = buttonGo.GetComponent<Image>(),
                title = title,
                subtitle = subtitle
            };
        }

        axisStatsText = CreateText(parent, "AxisStats", "", 14, FontStyles.Normal, textSecondary);
        RectTransform statsRect = axisStatsText.rectTransform;
        statsRect.anchorMin = new Vector2(0f, 0.38f);
        statsRect.anchorMax = new Vector2(1f, 0.60f);
        statsRect.offsetMin = new Vector2(18f, 0f);
        statsRect.offsetMax = new Vector2(-18f, 0f);
        axisStatsText.alignment = TextAlignmentOptions.TopLeft;

        modeText = CreateText(parent, "ModeText", "", 14, FontStyles.Bold, new Color(0.91f, 0.94f, 0.99f));
        RectTransform modeRect = modeText.rectTransform;
        modeRect.anchorMin = new Vector2(0f, 0.32f);
        modeRect.anchorMax = new Vector2(1f, 0.37f);
        modeRect.offsetMin = new Vector2(18f, 0f);
        modeRect.offsetMax = new Vector2(-18f, 0f);

        treeSummaryText = CreateText(parent, "TreeSummary", "", 13, FontStyles.Normal, textSecondary);
        RectTransform summaryRect = treeSummaryText.rectTransform;
        summaryRect.anchorMin = new Vector2(0f, 0.22f);
        summaryRect.anchorMax = new Vector2(1f, 0.32f);
        summaryRect.offsetMin = new Vector2(18f, 0f);
        summaryRect.offsetMax = new Vector2(-18f, 0f);
        treeSummaryText.alignment = TextAlignmentOptions.TopLeft;

        GameObject utilityList = new GameObject("UtilityList");
        utilityList.transform.SetParent(parent, false);
        RectTransform utilityListRect = utilityList.AddComponent<RectTransform>();
        utilityListRect.anchorMin = new Vector2(0f, 0.02f);
        utilityListRect.anchorMax = new Vector2(1f, 0.18f);
        utilityListRect.offsetMin = new Vector2(18f, 0f);
        utilityListRect.offsetMax = new Vector2(-18f, 0f);
        VerticalLayoutGroup utilityLayout = utilityList.AddComponent<VerticalLayoutGroup>();
        utilityLayout.spacing = 10f;
        utilityLayout.childControlWidth = true;
        utilityLayout.childControlHeight = true;
        utilityLayout.childForceExpandWidth = true;
        utilityLayout.childForceExpandHeight = false;

        CreateUtilityButton(utilityList.transform, "ModeToggle", "MOD DEGISTIR", ToggleMode);
        CreateUtilityButton(utilityList.transform, "RankUp", "AGACA SEVIYE EKLE", AddRankToActiveAxis);
        CreateUtilityButton(utilityList.transform, "RankDown", "AGAC XP AZALT", RemoveRankFromActiveAxis);
        CreateUtilityButton(utilityList.transform, "ResetXp", "AGAC XP SIFIRLA", ResetXpForActiveAxis);
    }

    private void BuildCenterPanel(RectTransform parent)
    {
        TextMeshProUGUI centerTitle = CreateText(parent, "CenterTitle", "PERK HARITASI", 20, FontStyles.Bold, textPrimary);
        RectTransform centerTitleRect = centerTitle.rectTransform;
        centerTitleRect.anchorMin = new Vector2(0f, 1f);
        centerTitleRect.anchorMax = new Vector2(1f, 1f);
        centerTitleRect.offsetMin = new Vector2(20f, 0f);
        centerTitleRect.offsetMax = new Vector2(-20f, 0f);
        centerTitleRect.anchoredPosition = new Vector2(0f, -18f);
        centerTitleRect.sizeDelta = new Vector2(0f, 26f);

        TextMeshProUGUI hintText = CreateText(parent, "HintText", "Bir perk kutusuna tikla. Sag panelden ac/kapat.", 13, FontStyles.Italic, textSecondary);
        RectTransform hintRect = hintText.rectTransform;
        hintRect.anchorMin = new Vector2(0f, 1f);
        hintRect.anchorMax = new Vector2(1f, 1f);
        hintRect.offsetMin = new Vector2(20f, 0f);
        hintRect.offsetMax = new Vector2(-20f, 0f);
        hintRect.anchoredPosition = new Vector2(0f, -46f);
        hintRect.sizeDelta = new Vector2(0f, 18f);

        GameObject mapArea = CreatePanel(parent, "MapArea", new Color(0.07f, 0.09f, 0.13f, 0.92f));
        RectTransform mapRect = mapArea.GetComponent<RectTransform>();
        mapRect.anchorMin = new Vector2(0.02f, 0.03f);
        mapRect.anchorMax = new Vector2(0.98f, 0.94f);
        mapRect.offsetMin = Vector2.zero;
        mapRect.offsetMax = Vector2.zero;

        GameObject lineRoot = new GameObject("LineContainer");
        lineRoot.transform.SetParent(mapArea.transform, false);
        lineContainer = lineRoot.AddComponent<RectTransform>();
        Stretch(lineContainer);

        GameObject nodeRoot = new GameObject("NodeContainer");
        nodeRoot.transform.SetParent(mapArea.transform, false);
        nodeContainer = nodeRoot.AddComponent<RectTransform>();
        Stretch(nodeContainer);
    }

    private void BuildRightPanel(RectTransform parent)
    {
        TextMeshProUGUI infoTitle = CreateText(parent, "InfoTitle", "PERK BILGISI", 20, FontStyles.Bold, textPrimary);
        RectTransform infoTitleRect = infoTitle.rectTransform;
        infoTitleRect.anchorMin = new Vector2(0f, 1f);
        infoTitleRect.anchorMax = new Vector2(1f, 1f);
        infoTitleRect.offsetMin = new Vector2(18f, 0f);
        infoTitleRect.offsetMax = new Vector2(-18f, 0f);
        infoTitleRect.anchoredPosition = new Vector2(0f, -18f);
        infoTitleRect.sizeDelta = new Vector2(0f, 26f);

        GameObject iconFrame = CreatePanel(parent, "SelectedIconFrame", new Color(0.19f, 0.22f, 0.31f, 1f));
        RectTransform iconFrameRect = iconFrame.GetComponent<RectTransform>();
        iconFrameRect.anchorMin = new Vector2(0f, 1f);
        iconFrameRect.anchorMax = new Vector2(0f, 1f);
        iconFrameRect.pivot = new Vector2(0f, 1f);
        iconFrameRect.anchoredPosition = new Vector2(18f, -56f);
        iconFrameRect.sizeDelta = new Vector2(92f, 92f);
        selectionIconFrame = iconFrame.GetComponent<Image>();

        GameObject iconImageGo = new GameObject("IconImage");
        iconImageGo.transform.SetParent(iconFrame.transform, false);
        selectionIconImage = iconImageGo.AddComponent<Image>();
        RectTransform iconImageRect = selectionIconImage.rectTransform;
        iconImageRect.anchorMin = new Vector2(0.1f, 0.1f);
        iconImageRect.anchorMax = new Vector2(0.9f, 0.9f);
        iconImageRect.offsetMin = Vector2.zero;
        iconImageRect.offsetMax = Vector2.zero;
        selectionIconImage.preserveAspect = true;

        selectionIconFallback = CreateText(iconFrame.transform, "IconFallback", "--", 30, FontStyles.Bold, textPrimary);
        Stretch(selectionIconFallback.rectTransform);
        selectionIconFallback.alignment = TextAlignmentOptions.Center;

        selectionTitleText = CreateText(parent, "SelectionTitle", "PERK SEC", 23, FontStyles.Bold, textPrimary);
        RectTransform selectionTitleRect = selectionTitleText.rectTransform;
        selectionTitleRect.anchorMin = new Vector2(0f, 1f);
        selectionTitleRect.anchorMax = new Vector2(1f, 1f);
        selectionTitleRect.offsetMin = new Vector2(124f, 0f);
        selectionTitleRect.offsetMax = new Vector2(-18f, 0f);
        selectionTitleRect.anchoredPosition = new Vector2(0f, -60f);
        selectionTitleRect.sizeDelta = new Vector2(0f, 28f);

        selectionStatusText = CreateText(parent, "SelectionStatus", "", 14, FontStyles.Bold, textSecondary);
        RectTransform selectionStatusRect = selectionStatusText.rectTransform;
        selectionStatusRect.anchorMin = new Vector2(0f, 1f);
        selectionStatusRect.anchorMax = new Vector2(1f, 1f);
        selectionStatusRect.offsetMin = new Vector2(124f, 0f);
        selectionStatusRect.offsetMax = new Vector2(-18f, 0f);
        selectionStatusRect.anchoredPosition = new Vector2(0f, -92f);
        selectionStatusRect.sizeDelta = new Vector2(0f, 20f);

        selectionMetaText = CreateText(parent, "SelectionMeta", "", 14, FontStyles.Normal, textSecondary);
        RectTransform selectionMetaRect = selectionMetaText.rectTransform;
        selectionMetaRect.anchorMin = new Vector2(0f, 1f);
        selectionMetaRect.anchorMax = new Vector2(1f, 1f);
        selectionMetaRect.offsetMin = new Vector2(18f, 0f);
        selectionMetaRect.offsetMax = new Vector2(-18f, 0f);
        selectionMetaRect.anchoredPosition = new Vector2(0f, -164f);
        selectionMetaRect.sizeDelta = new Vector2(0f, 70f);
        selectionMetaText.alignment = TextAlignmentOptions.TopLeft;

        selectionDescriptionText = CreateText(parent, "SelectionDescription", "", 15, FontStyles.Normal, textPrimary);
        RectTransform selectionDescriptionRect = selectionDescriptionText.rectTransform;
        selectionDescriptionRect.anchorMin = new Vector2(0f, 0.38f);
        selectionDescriptionRect.anchorMax = new Vector2(1f, 0.74f);
        selectionDescriptionRect.offsetMin = new Vector2(18f, 0f);
        selectionDescriptionRect.offsetMax = new Vector2(-18f, 0f);
        selectionDescriptionText.alignment = TextAlignmentOptions.TopLeft;

        selectionRequirementText = CreateText(parent, "SelectionRequirements", "", 14, FontStyles.Normal, textSecondary);
        RectTransform selectionRequirementRect = selectionRequirementText.rectTransform;
        selectionRequirementRect.anchorMin = new Vector2(0f, 0.18f);
        selectionRequirementRect.anchorMax = new Vector2(1f, 0.36f);
        selectionRequirementRect.offsetMin = new Vector2(18f, 0f);
        selectionRequirementRect.offsetMax = new Vector2(-18f, 0f);
        selectionRequirementText.alignment = TextAlignmentOptions.TopLeft;

        unlockButton = CreateButton(parent, "UnlockButton", "PERK AC", new Vector2(0f, 0f), new Color(0.20f, 0.60f, 0.34f, 0.98f), HandleSelectedNodeAction);
        RectTransform unlockRect = unlockButton.GetComponent<RectTransform>();
        unlockRect.anchorMin = new Vector2(0f, 0f);
        unlockRect.anchorMax = new Vector2(1f, 0f);
        unlockRect.offsetMin = new Vector2(18f, 18f);
        unlockRect.offsetMax = new Vector2(-18f, 18f);
        unlockRect.sizeDelta = new Vector2(0f, 52f);
        unlockButtonLabel = unlockButton.GetComponentInChildren<TextMeshProUGUI>();
    }

    private RectTransform CreateSection(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Color color)
    {
        GameObject go = CreatePanel(parent, name, color);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return rect;
    }

    private void SetActiveAxis(ProgressionAxisSO axis)
    {
        if (axis == null)
            return;

        activeAxis = axis;
        titleText.text = $"{axis.displayName.ToUpperInvariant()} SKILL TREE";
        selectedNode = null;
        RebuildAxisView();
        SelectNode(GetDefaultSelectedNode());
        RefreshAll();
    }

    private ProgressionAxisSO GetFirstConfiguredAxis()
    {
        if (dashAxis != null) return dashAxis;
        if (parryAxis != null) return parryAxis;
        if (overdriveAxis != null) return overdriveAxis;
        return cadenceAxis;
    }

    private List<ProgressionAxisSO> GetConfiguredAxes()
    {
        List<ProgressionAxisSO> axes = new List<ProgressionAxisSO>();
        if (dashAxis != null) axes.Add(dashAxis);
        if (parryAxis != null) axes.Add(parryAxis);
        if (overdriveAxis != null) axes.Add(overdriveAxis);
        if (cadenceAxis != null) axes.Add(cadenceAxis);
        return axes;
    }

    private void RebuildAxisView()
    {
        slots.Clear();
        lines.Clear();
        ClearChildren(nodeContainer);
        ClearChildren(lineContainer);

        if (activeAxis == null || activeAxis.nodes == null)
            return;

        foreach (SkillNodeSO node in activeAxis.nodes)
        {
            if (node != null)
                BuildNodeSlot(node);
        }

        foreach (SkillNodeSO node in activeAxis.nodes)
        {
            if (node == null || node.prerequisites == null)
                continue;

            foreach (SkillNodeSO prereq in node.prerequisites)
            {
                if (prereq != null)
                    CreateLine(prereq.nodeId, node.nodeId);
            }
        }
    }

    private void BuildNodeSlot(SkillNodeSO node)
    {
        if (!GetCurrentGrid().TryGetValue(node.nodeId, out Vector2Int gridPos))
            return;

        GameObject slotGo = new GameObject($"Node_{node.nodeId}");
        slotGo.transform.SetParent(nodeContainer, false);

        RectTransform rect = slotGo.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = GridToLocal(gridPos);
        rect.sizeDelta = nodeSize;

        Image border = slotGo.AddComponent<Image>();
        border.color = GetNodeTintColor(node) * 0.45f;

        GameObject fillGo = CreatePanel(slotGo.transform, "Fill", new Color(0.09f, 0.11f, 0.16f, 1f));
        RectTransform fillRect = fillGo.GetComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(1f, 1f);
        fillRect.offsetMin = new Vector2(3f, 3f);
        fillRect.offsetMax = new Vector2(-3f, -3f);
        Image fill = fillGo.GetComponent<Image>();

        GameObject selectionGo = CreatePanel(slotGo.transform, "Selection", new Color(0f, 0f, 0f, 0f));
        RectTransform selectionRect = selectionGo.GetComponent<RectTransform>();
        selectionRect.anchorMin = Vector2.zero;
        selectionRect.anchorMax = Vector2.one;
        selectionRect.offsetMin = Vector2.zero;
        selectionRect.offsetMax = Vector2.zero;
        Image selectionFrame = selectionGo.GetComponent<Image>();

        GameObject iconFrame = CreatePanel(fillGo.transform, "IconFrame", new Color(0.15f, 0.18f, 0.25f, 1f));
        RectTransform iconFrameRect = iconFrame.GetComponent<RectTransform>();
        iconFrameRect.anchorMin = new Vector2(0.18f, 0.40f);
        iconFrameRect.anchorMax = new Vector2(0.82f, 0.90f);
        iconFrameRect.offsetMin = Vector2.zero;
        iconFrameRect.offsetMax = Vector2.zero;

        GameObject iconImageGo = new GameObject("IconImage");
        iconImageGo.transform.SetParent(iconFrame.transform, false);
        Image iconImage = iconImageGo.AddComponent<Image>();
        RectTransform iconImageRect = iconImage.GetComponent<RectTransform>();
        iconImageRect.anchorMin = new Vector2(0.1f, 0.1f);
        iconImageRect.anchorMax = new Vector2(0.9f, 0.9f);
        iconImageRect.offsetMin = Vector2.zero;
        iconImageRect.offsetMax = Vector2.zero;
        iconImage.preserveAspect = true;

        TextMeshProUGUI iconFallback = CreateText(iconFrame.transform, "IconFallback", GetNodeBadgeText(node), 22, FontStyles.Bold, textPrimary);
        Stretch(iconFallback.rectTransform);
        iconFallback.alignment = TextAlignmentOptions.Center;

        TextMeshProUGUI nameText = CreateText(fillGo.transform, "Name", node.displayName, 12, FontStyles.Bold, textPrimary);
        RectTransform nameRect = nameText.rectTransform;
        nameRect.anchorMin = new Vector2(0.08f, 0.18f);
        nameRect.anchorMax = new Vector2(0.92f, 0.38f);
        nameRect.offsetMin = Vector2.zero;
        nameRect.offsetMax = Vector2.zero;
        nameText.alignment = TextAlignmentOptions.Center;

        TextMeshProUGUI statusText = CreateText(fillGo.transform, "Status", "", 11, FontStyles.Bold, textSecondary);
        RectTransform statusRect = statusText.rectTransform;
        statusRect.anchorMin = new Vector2(0.08f, 0.03f);
        statusRect.anchorMax = new Vector2(0.92f, 0.15f);
        statusRect.offsetMin = Vector2.zero;
        statusRect.offsetMax = Vector2.zero;
        statusText.alignment = TextAlignmentOptions.Center;

        Button button = slotGo.AddComponent<Button>();
        button.targetGraphic = fill;
        button.onClick.AddListener(() => SelectNode(node));

        slots[node.nodeId] = new NodeSlot
        {
            node = node,
            rect = rect,
            border = border,
            fill = fill,
            iconImage = iconImage,
            iconFallback = iconFallback,
            nameText = nameText,
            statusText = statusText,
            selectionFrame = selectionFrame,
            button = button
        };
    }

    private void CreateLine(string fromId, string toId)
    {
        if (!GetCurrentGrid().ContainsKey(fromId) || !GetCurrentGrid().ContainsKey(toId))
            return;

        GameObject lineGo = new GameObject($"Line_{fromId}_{toId}");
        lineGo.transform.SetParent(lineContainer, false);
        RectTransform rect = lineGo.AddComponent<RectTransform>();
        Image image = lineGo.AddComponent<Image>();
        image.color = lineLockedColor;
        UpdateLinePosition(rect, fromId, toId);
        lines.Add(new LineConnection { rect = rect, image = image, fromId = fromId, toId = toId });
    }

    private void UpdateLinePosition(RectTransform rect, string fromId, string toId)
    {
        Vector2 fromPos = GridToLocal(GetCurrentGrid()[fromId]);
        Vector2 toPos = GridToLocal(GetCurrentGrid()[toId]);
        Vector2 diff = toPos - fromPos;
        float length = diff.magnitude;
        float angle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.anchoredPosition = fromPos;
        rect.sizeDelta = new Vector2(length, lineWidth);
        rect.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    private Vector2 GridToLocal(Vector2Int grid)
    {
        return SkillTreeLayoutUtility.GridToLocal(grid, nodeSize, horizontalSpacing, verticalSpacing);
    }

    private Dictionary<string, Vector2Int> GetCurrentGrid()
    {
        if (activeAxis == parryAxis) return ParryGrid;
        if (activeAxis == overdriveAxis) return OverdriveGrid;
        if (activeAxis == cadenceAxis) return CadenceGrid;
        return DashGrid;
    }

    private Color GetNodeTintColor(SkillNodeSO node)
    {
        if (activeAxis != null && activeAxis.axisColor.a > 0f)
            return activeAxis.axisColor;

        return node != null && node.isCommitmentNode
            ? unlockableColor
            : selectedColor;
    }

    private SkillNodeSO GetDefaultSelectedNode()
    {
        if (selectedNode != null && activeAxis != null && ContainsNode(activeAxis, selectedNode))
            return selectedNode;

        if (activeAxis == null || activeAxis.nodes == null)
            return null;

        AxisProgressionManager manager = AxisProgressionManager.Instance;
        foreach (SkillNodeSO node in activeAxis.nodes)
        {
            if (node == null)
                continue;

            if (manager != null && manager.GetNodeStatus(node) == NodeStatus.Unlocked)
                return node;
        }

        foreach (SkillNodeSO node in activeAxis.nodes)
        {
            if (node == null)
                continue;

            if (manager != null && manager.GetNodeStatus(node) == NodeStatus.Unlockable)
                return node;
        }

        foreach (SkillNodeSO node in activeAxis.nodes)
        {
            if (node != null)
                return node;
        }

        return null;
    }

    private static bool ContainsNode(ProgressionAxisSO axis, SkillNodeSO node)
    {
        if (axis == null || axis.nodes == null || node == null)
            return false;

        foreach (SkillNodeSO axisNode in axis.nodes)
        {
            if (axisNode == node)
                return true;
        }

        return false;
    }

    private void SelectNode(SkillNodeSO node)
    {
        selectedNode = node;
        RefreshSelection();
        RefreshAllSlots();
    }

    private void HandleSelectedNodeAction()
    {
        if (selectedNode == null || AxisProgressionManager.Instance == null)
            return;

        AxisProgressionManager manager = AxisProgressionManager.Instance;
        int result = manager.IsTesterMode
            ? manager.SmartToggleNode(selectedNode)
            : (manager.TryUnlockNode(selectedNode) ? 1 : -2);

        if (result == 1)
            AudioManager.Play(AudioEventId.UIUnlock, gameObject);
        else if (result <= -1)
            AudioManager.Play(AudioEventId.UIFail, gameObject);

        RefreshAll();
    }

    private void RefreshAll()
    {
        RefreshAxisButtons();
        RefreshStatsPanel();
        RefreshAllSlots();
        RefreshSelection();
    }

    private void RefreshAxisButtons()
    {
        AxisProgressionManager manager = AxisProgressionManager.Instance;
        foreach (AxisButtonSlot button in axisButtons.Values)
        {
            bool isActive = button.axis == activeAxis;
            Color accent = button.axis != null ? button.axis.axisColor : selectedColor;
            if (accent.a <= 0f)
                accent = selectedColor;

            button.background.color = isActive
                ? new Color(accent.r * 0.55f, accent.g * 0.55f, accent.b * 0.55f, 1f)
                : new Color(0.16f, 0.19f, 0.27f, 1f);

            int rank = manager != null ? manager.GetTreeRank(button.axis) : 0;
            int points = manager != null ? manager.GetAvailablePerkPoints(button.axis) : 0;
            button.subtitle.text = $"Rank {rank}  |  Perk {points}";
        }
    }

    private void RefreshStatsPanel()
    {
        AxisProgressionManager manager = AxisProgressionManager.Instance;
        if (manager == null || activeAxis == null)
            return;

        int rank = manager.GetTreeRank(activeAxis);
        float xp = manager.GetTreeXp(activeAxis);
        int nextRank = Mathf.Min(manager.GetProgressionConfig().MaxRank, rank + 1);
        int nextRankXp = manager.GetProgressionConfig().GetRequiredXpForRank(nextRank);
        int totalPerkPoints = manager.GetTotalPerkPoints(activeAxis);
        int spentPerkPoints = manager.GetSpentPerkPoints(activeAxis);
        int availablePerkPoints = manager.GetAvailablePerkPoints(activeAxis);
        string route = manager.GetChosenTier2Route(activeAxis);
        string focusState = manager.IsAxisCommitted(activeAxis) ? "Acik" : "Kapali";

        axisStatsText.text =
            $"<b>{activeAxis.displayName}</b>\n" +
            $"Rank: {rank}\n" +
            $"XP: {xp:F0}/{nextRankXp}\n" +
            $"Perk Puanlari: {availablePerkPoints} kalan / {spentPerkPoints} harcanan / {totalPerkPoints} toplam\n" +
            $"Odak: {focusState}\n" +
            $"Secili Yol: {(string.IsNullOrEmpty(route) ? "Yok" : route)}";

        modeText.text = manager.IsTesterMode
            ? "MOD: TESTER"
            : "MOD: NORMAL PROGRESSION";

        treeSummaryText.text = manager.IsTesterMode
            ? "Tester modda XP/rank engeli yok. Focus ve yol kilitleri korunur."
            : "Normal modda rank, perk puani, prerequisite ve odak kilitleri aktif.";
    }

    private void RefreshAllSlots()
    {
        AxisProgressionManager manager = AxisProgressionManager.Instance;
        foreach (NodeSlot slot in slots.Values)
            RefreshSlot(slot, manager);

        foreach (LineConnection line in lines)
        {
            bool fromUnlocked = manager != null &&
                                slots.TryGetValue(line.fromId, out NodeSlot fromSlot) &&
                                manager.IsNodeUnlocked(fromSlot.node);
            bool toUnlocked = manager != null &&
                              slots.TryGetValue(line.toId, out NodeSlot toSlot) &&
                              manager.IsNodeUnlocked(toSlot.node);
            line.image.color = fromUnlocked && toUnlocked ? lineUnlockedColor : lineLockedColor;
        }
    }

    private void RefreshSlot(NodeSlot slot, AxisProgressionManager manager)
    {
        if (slot == null || slot.node == null)
            return;

        NodeStatus status = manager != null ? manager.GetNodeStatus(slot.node) : NodeStatus.Hidden;
        Color tint = GetNodeTintColor(slot.node);
        bool isSelected = selectedNode == slot.node;

        slot.iconImage.enabled = slot.node.icon != null;
        slot.iconImage.sprite = slot.node.icon;
        slot.iconFallback.gameObject.SetActive(slot.node.icon == null);
        slot.iconFallback.text = GetNodeBadgeText(slot.node);

        slot.selectionFrame.color = isSelected ? selectedColor : new Color(0f, 0f, 0f, 0f);

        switch (status)
        {
            case NodeStatus.Unlocked:
                slot.fill.color = new Color(tint.r * 0.22f, tint.g * 0.22f, tint.b * 0.22f, 1f);
                slot.border.color = isSelected ? selectedColor : unlockedColor;
                slot.statusText.text = "ACTIVE";
                slot.statusText.color = unlockedColor;
                break;

            case NodeStatus.Unlockable:
                slot.fill.color = new Color(0.12f, 0.14f, 0.20f, 1f);
                slot.border.color = isSelected ? selectedColor : unlockableColor;
                slot.statusText.text = "UNLOCKABLE";
                slot.statusText.color = unlockableColor;
                break;

            default:
                slot.fill.color = new Color(0.08f, 0.10f, 0.14f, 1f);
                slot.border.color = isSelected ? selectedColor : lockedColor;
                slot.statusText.text = "LOCKED";
                slot.statusText.color = lockedColor;
                break;
        }

        slot.button.interactable = status != NodeStatus.Hidden;
    }

    private void RefreshSelection()
    {
        AxisProgressionManager manager = AxisProgressionManager.Instance;
        if (selectedNode == null)
        {
            selectionTitleText.text = "PERK SEC";
            selectionStatusText.text = "";
            selectionMetaText.text = "Soldaki agaci sec, ortadaki perk kutusuna tikla.";
            selectionDescriptionText.text = "";
            selectionRequirementText.text = "";
            selectionIconImage.enabled = false;
            selectionIconFallback.text = "--";
            unlockButton.interactable = false;
            unlockButtonLabel.text = "PERK SEC";
            return;
        }

        NodeStatus status = manager != null ? manager.GetNodeStatus(selectedNode) : NodeStatus.Hidden;
        selectionTitleText.text = selectedNode.displayName.ToUpperInvariant();
        selectionStatusText.text = GetStatusText(status);
        selectionStatusText.color = GetStatusColor(status);
        selectionMetaText.text = BuildNodeMetaText(selectedNode, manager);
        selectionDescriptionText.text = BuildNodeDescription(selectedNode);
        selectionRequirementText.text = BuildRequirementText(selectedNode, manager);
        selectionIconFrame.color = GetNodeTintColor(selectedNode);
        selectionIconImage.enabled = selectedNode.icon != null;
        selectionIconImage.sprite = selectedNode.icon;
        selectionIconFallback.gameObject.SetActive(selectedNode.icon == null);
        selectionIconFallback.text = GetNodeBadgeText(selectedNode);

        bool canAct = false;
        if (manager != null)
        {
            if (manager.IsTesterMode)
                canAct = status != NodeStatus.Hidden;
            else
                canAct = status == NodeStatus.Unlockable;
        }

        unlockButton.interactable = canAct;
        unlockButtonLabel.text = manager != null && manager.IsTesterMode
            ? (manager.IsNodeUnlocked(selectedNode) ? "PERK KAPAT" : "PERK AC")
            : "PERK AC";
    }

    private static string GetStatusText(NodeStatus status)
    {
        return status switch
        {
            NodeStatus.Unlocked => "ACTIVE",
            NodeStatus.Unlockable => "UNLOCKABLE",
            NodeStatus.VisibleLocked => "LOCKED",
            _ => "HIDDEN"
        };
    }

    private Color GetStatusColor(NodeStatus status)
    {
        return status switch
        {
            NodeStatus.Unlocked => unlockedColor,
            NodeStatus.Unlockable => unlockableColor,
            NodeStatus.VisibleLocked => lockedColor,
            _ => textSecondary
        };
    }

    private string BuildNodeMetaText(SkillNodeSO node, AxisProgressionManager manager)
    {
        ProgressionAxisSO ownerAxis = manager != null && manager.database != null
            ? manager.database.GetAxisForNode(node)
            : activeAxis;

        int requiredRank = manager != null ? manager.GetRequiredRankForNode(node) : node.requiredTreeRank;
        int currentRank = ownerAxis != null && manager != null ? manager.GetTreeRank(ownerAxis) : 0;
        string tierLabel = node.isCommitmentNode ? "Focus / Commitment" : $"Tier {node.tier}";
        string route = string.IsNullOrEmpty(node.regionTag) ? "Genel" : node.regionTag;

        return
            $"<b>{tierLabel}</b>\n" +
            $"Agac: {(ownerAxis != null ? ownerAxis.displayName : "Bilinmiyor")}\n" +
            $"Gerekli Rank: {requiredRank}  |  Mevcut Rank: {currentRank}\n" +
            $"Bolge / Yol: {route}";
    }

    private string BuildNodeDescription(SkillNodeSO node)
    {
        string detailed = GetDetailedNodeDescription(node);
        if (!string.IsNullOrEmpty(detailed))
            return detailed;

        if (!string.IsNullOrWhiteSpace(node.description))
            return node.description;

        if (node.isCommitmentNode)
            return $"{node.displayName}, bu ekseni asil secim haline getirir. Bu odagi actiginda Tier 2 yolu bu agactan ilerler ve karsi odak kilitlenir.";

        if (node.tier == 1)
            return $"{node.displayName}, bu agacin temel oynanis kimligini acan bir Tier 1 perktir. Bagli oldugu prerequisite saglandiginda ve perk puanin yettiginde acilabilir.";

        return $"{node.displayName}, bu agacin Tier 2 uzmanlik katmanina ait bir perktir. Odak acildiktan sonra prerequisite zinciri uzerinden ilerler.";
    }

    private string GetDetailedNodeDescription(SkillNodeSO node)
    {
        if (node == null)
            return "";

        return node.nodeId switch
        {
            "dash_t1_ranged_dodge" => "Dash sirasinda projectile tehditlerini daha guvenli atlatmana odaklanir. Uzak saldiri cizgilerini daha rahat keser ve Dash ekseninin savunma omurgasini acar.",
            "dash_t1_melee_dodge" => "Dash ile yakin dovus saldirilarini riske girip icinden gecmeyi odullendirir. Ozellikle on hatta oynarken aci kazanma ve punish icin temel giris perki olur.",
            "dash_t1_tempo_gain" => "Dash ile dusman baskisina yakin girdiginde tempo uretimini destekler. Dash'i sadece kacis degil, ritim kuran aktif bir aksiyona cevirir.",
            "dash_t1_counter" => "Basarili dodge sonrasinda karsi saldiri penceresi acarak Dash agacinin cezalandirma tarafini devreye alir. Sonraki isabet daha verimli hale gelir.",
            "dash_t1_attack_speed" => "Dash sonrasi gelen hizli follow-up saldiriyi one cikarir. Dogru zamanlamayla saldiri akisini resetler ve baskiyi dusurmene izin vermez.",
            "dash_t2_commitment" => "Dash odagini asil secimine cevirir. Bu node acildiginda Parry odagi ve o eksenin Tier 2 tarafi kilitlenir; Dash odakli build burada kesinlesir.",
            "dash_t2h_hunt_mark" => "Hedefi av olarak isaretleme yolunu acar. Isaretli hedefe baski kurdukca bu yolun diger perkleri daha anlamli hale gelir.",
            "dash_t2h_blind_spot" => "Isaretli hedefin yan ve arka acilarini daha degerli hale getirir. Dash ile aci alip guvenli punish oynamak isteyen stil icin tasarlanmistir.",
            "dash_t2h_hunt_flow" => "Dash + attack akisini isaretlemeye baglar. Zinciri temiz tutarsan av odakli ritim daha stabil ve daha tehditkar ilerler.",
            "dash_t2h_execute" => "Dogru aci ve dogru can esiginde hedefe bitirici baski kurar. Av yolunun odak hedefi eritme aracidir.",
            "dash_t2h_hunt_cycle" => "Av odakli zinciri oda boyunca surdurmeyi odullendirir. Bir hedeften cikip digerine geciste build ritmini dusurmez.",
            "dash_t2f_flow_mark" => "Flow yolunun merkez perki. Dash sonrasi saldiri ile hedefleri akis isareti altina alir ve sonraki sekme/perk etkileşimlerini acar.",
            "dash_t2f_snapback" => "Akis isaretli hedefler uzerinde geri sicrama ve agresif yeniden konumlanma oyunu kurar. Hareketin kendisini hasar akisinin parcasi yapar.",
            "dash_t2f_chain_bounce" => "Isaretlenmis hedefler arasinda sekmeli baski zinciri kurar. Kalabalik icinde dash yolunun akici tarafini buyutur.",
            "dash_t2f_black_hole" => "Akis yolunda dusmanlari tek noktaya cekip sonraki baski araclarini hazirlar. Pozisyonu bozmaktan cok, combo penceresi acmak icin kullanilir.",
            "dash_t2f_burst" => "Sekme ve toplama etkilerinin final patlama odagidir. Akis yolunun kontrollu kaosu burada en yuksek odulunu verir.",
            "parry_t1_reflect" => "Projectile parry/deflect davranisini acan temel perk. Ranged tehditleri sadece engellemek yerine dusmana geri cevirmeni saglar.",
            "parry_t1_perfect_timing" => "Parry penceresinin basina ayrik bir perfect katmani ekler. Dogru milisaniyeyi tuttugunda geri donusler daha sert olur.",
            "parry_t1_counter_stance" => "Basarili parry sonrasinda karsi saldiri penceresi acar. Melee ve ranged parry degerleri burada tek bir punish penceresinde birikir.",
            "parry_t1_perfect_break" => "Perfect melee parry ile dusmanin ritmini bozup agir bir karsilik vermeyi saglar. Moblari stun'a, bosslari ise kisa interrupt'a iter.",
            "parry_t1_rhythm_return" => "Basarili parry sonrasinda recovery'nin bir kismini iade eder. Perfect parry ile bu geri donus daha da verimli olur ve ritim korunur.",
            "parry_t2_commitment" => "Parry odagini asil secimine cevirir. Bu node acildiginda Dash odagi ve onun Tier 2 tarafi kilitlenir; savunma merkezli build burada netlesir.",
            "parry_t2b_reverse_front" => "On kalkan acisini daraltirken arkaya ikinci bir parry yayi ekler. Birden fazla yonden gelen tehditte balistik yolun pozisyon oyununu baslatir.",
            "parry_t2b_overdeflect" => "Geri gonderilen projectile'i hiz, hasar ve delme acisindan guclendirir. Deflect'i sadece savunma degil, aktif hasar araci haline getirir.",
            "parry_t2b_suppressive_trace" => "Deflect edilen projectile'in vurdugu hedefte kisa sureli bozulma yaratir. Cast kesme, windup iptali ve baski kirma bu node'un asil isidir.",
            "parry_t2b_fractured_orbit" => "Ana deflect sonrasinda zayif ikincil sekmeler uretir. Kalabalik ranged senaryolarda parry'nin alani kontrol etmesini saglar.",
            "parry_t2b_feedback" => "Ardisik projectile parry'lerden stack toplayip sonraki deflect'leri daha tehditkar hale getirir. Balistik yolun kartopu perki budur.",
            "parry_t2p_close_execute" => "Mermiyi cok yakindan perfect geri cevirdiginde ranged hedefi infaz etmeye calisir. Boss ve boss-benzeri hedeflerde ise infaz yerine sert baski etkisi verir.",
            "parry_t2p_fine_edge" => "Perfect pencereyi buyuturken normal pencereyi inceltir. Bu yol, daha yuksek odul icin normal parry guvenligini bilerek azaltir.",
            "parry_t2p_heavy_riposte" => "Perfect melee parry'leri daha agir punish'e donusturur. Guard kullanan dusmanlari da daha uzun sure acikta birakir.",
            "parry_t2p_rotating_cone" => "Parry aktifken kisa bir koni sweep'ini tam tur dondurur. Dogru timing ile tek yone degil, etrafindaki tehditlere cevap verme aracidir.",
            "parry_t2p_perfect_cycle" => "Perfect parry zinciri kurmayi odullendirir. Recovery'yi geri alir, counter penceresini tazeler ve riski dogrudan akisa cevirir.",
            "overdrive_t1_heat_buildup" => "Ardisik baskida tempo uretimini kademeli buyutur. Zincir koparsa birikim de duser; bu yuzden surekli basinci sever.",
            "overdrive_t1_threshold_burst" => "Yeni bir tempo esigi asildiginda sonraki vurusun agirligini arttirir. Esik gecislerini sayisal degil, hissedilir patlama anlarina cevirir.",
            "overdrive_t1_red_pressure" => "Yuksek tempoda hasar ve stagger gucunu arttirirken seni daha kirilgan hale getirir. Risk-odul mantiginin temel T1 perki budur.",
            "overdrive_t1_overflow_impulse" => "Maks tempoya yakin oynarken kucuk kesintilere karsi direnc kazandirir. Baskiyi kaybetmeden devam etmeyi kolaylastirir.",
            "overdrive_t1_final_push" => "Kill, agir stun veya guard break sonrasinda bir sonraki darbeyi guclendirir. Momentumun momentumu besledigi yer burasidir.",
            "overdrive_t2_commitment" => "Overdrive odagini asil secimine cevirir. Patlayici yuksek tempo oyunu bu noktadan sonra Cadence'in istikrarli yolunu dislar.",
            "overdrive_t2burst_short_circuit" => "Yeni tempo esigine giris yaptiginda kisa sureli patlama penceresi acar. Burst yolunun kapisi budur.",
            "overdrive_t2burst_red_window" => "Burst penceresi sirasinda hiz, hasar ve stagger gucu yukselir. Karsiliginda tempo daha cabuk akar gider.",
            "overdrive_t2burst_threshold_echo" => "Burst sirasinda kill veya agir baski aldiginda pencereyi uzatir ya da ikinci minik patlama yaratir. Dogru cash-out akisini odullendirir.",
            "overdrive_t2burst_pressure_break" => "Burst sirasindaki heavy ve counter saldirilarin guard/stagger etkisini belirgin buyutur. Tek hedef ustune baski kuran kisa ama sert bir perk.",
            "overdrive_t2burst_final_flare" => "Pencere bitmeden onceki son darbe birikmis gucu tek bir vurus olarak nakde cevirir. Zamanlama dogruysa yolun en yuksek odulunu burada alirsin.",
            "overdrive_t2pred_blood_scent" => "Surekli baski yedigin hedefi av durumuna tasir. Predator yolunun tek hedef odakli takibinin ilk adimidir.",
            "overdrive_t2pred_choking_proximity" => "Av hedefin yakinindayken tempo kaybini azaltir ve baski araclarini daha verimli kullanmana yardim eder. Hedefin ustunden inmemeyi odullendirir.",
            "overdrive_t2pred_predator_angle" => "Avin yan ve arka acilarindan gelen darbeleri daha yikici hale getirir. Pozisyon alan oyuncu burada daha buyuk kazanc alir.",
            "overdrive_t2pred_pack_breaker" => "Av oldugunde baskinin tamamen dusmesini engeller ve uygun hedefe tasir. Tek hedef odakli zinciri oda icinde surdurur.",
            "overdrive_t2pred_execute_pressure" => "Can esigi dusen ava karsi counter/heavy/ozel baski saldirilarini bitirici hale getirir. Boss'ta anlik oldurme degil, cok guclu bitiris baskisi uretir.",
            "cadence_t1_steady_pulse" => "Tempo kaybini yumusatir ve ritmi daha kolay tasimani saglar. Cadence ekseninin temel omurgasi budur.",
            "cadence_t1_transition_rhythm" => "Attack, dash, parry ve skill arasi temiz gecisleri odullendirir. Tek hareket spami yerine bilincli aksiyon akisini one cikarir.",
            "cadence_t1_soft_fall" => "Tempo bir kademe dustugunde bonuslarin bir anda kopmasini engeller. Ritim bozulsa da oyuncuyu tamamen boslukta birakmaz.",
            "cadence_t1_measured_power" => "Ayni tempo bandinda dengeli kaldikca kucuk ama stabil guc olusturur. Patlayici degil, kontrollu avantaj hissi verir.",
            "cadence_t1_rhythm_shield" => "Kucuk hatalarin tempoyu daha az bozmasini saglar. Chip hasar, minik takilmalar ve ritim kirilmalari bu node ile daha az cezalandirir.",
            "cadence_t2_commitment" => "Cadence odagini asil secimine cevirir. Stabil tempo oyunu bu noktadan sonra Overdrive'in patlayici tarafini kilitler.",
            "cadence_t2measured_measure_line" => "Her tempo kademesinde stabil bir bolge tanimlar. O bolgede kaldiginda Cadence bonuslari daha tutarli calisir.",
            "cadence_t2measured_balance_point" => "Stabil bolgede kaldikca precision, counter kalitesi ve kontrol etkileri yavas yavas birikir. Dusuk gurultulu ama yuksek verimli bir perk.",
            "cadence_t2measured_timed_accent" => "Dogru ritim araliklariyla guclendirilmis vurgu darbeleri uretir. Ritimsiz spam yaptiginda bu odulu vermez.",
            "cadence_t2measured_recovery_return" => "Ritim bozuldugunda yeniden dengeye donus suresini kisaltir. Hata sonrasi oyunu toparlamayi hedefler.",
            "cadence_t2measured_perfect_measure" => "Uzun sure bozulmadan oynarsan periyodik olarak guclu ama kontrollu oduller verir. Measured yolun ustalik odagi budur.",
            "cadence_t2flow_flow_ring" => "Farkli aksiyon tiplerini birbirine baglayarak Flow stack uretir. Flow yolunun temel akisini acar.",
            "cadence_t2flow_sliding_continuation" => "Hedef degistirirken veya hareket halindeyken Flow stack'lerinin daha zor dusmesini saglar. Aksiyon zincirini tasimayi kolaylastirir.",
            "cadence_t2flow_wave_bounce" => "Yuksek Flow durumunda saldirilara kucuk yanki/sekme etkileri ekler. Direkt burst degil, akisi odullendiren yan etkidir.",
            "cadence_t2flow_threshold_surf" => "Tempo bir kademe dustugunde bonuslarin bir kismini asagi kademeye tasir. Ritmin cokusunu tamamen durdurmaz ama yumusatir.",
            "cadence_t2flow_overflow_harmony" => "Yeterince uzun Flow zincirlerinde dash/parry/skill recovery iadesi ve hafif tempo geri beslemesi verir. Yolun dans gibi hisseden capstone perki budur.",
            _ => ""
        };
    }

    private string BuildRequirementText(SkillNodeSO node, AxisProgressionManager manager)
    {
        List<string> lines = new List<string>();

        if (node.prerequisites != null && node.prerequisites.Length > 0)
        {
            List<string> prereqLabels = new List<string>();
            foreach (SkillNodeSO prereq in node.prerequisites)
            {
                if (prereq == null)
                    continue;

                bool ok = manager != null && manager.IsNodeUnlocked(prereq);
                prereqLabels.Add(ok
                    ? $"<color=#4DDE8E>{prereq.displayName}</color>"
                    : $"<color=#FF8B78>{prereq.displayName}</color>");
            }

            if (prereqLabels.Count > 0)
                lines.Add($"On Kosullar: {string.Join(", ", prereqLabels)}");
        }

        if (manager != null)
        {
            string blockReason = manager.GetBlockReason(node);
            if (!string.IsNullOrEmpty(blockReason))
                lines.Add($"<color=#FF8B78>{blockReason}</color>");
        }

        return lines.Count > 0
            ? string.Join("\n", lines)
            : "Bu perk icin ek engel yok.";
    }

    private string GetNodeBadgeText(SkillNodeSO node)
    {
        if (node == null || string.IsNullOrWhiteSpace(node.displayName))
            return "--";

        string[] parts = node.displayName.Split(' ');
        if (parts.Length == 1)
            return parts[0].Length >= 2
                ? parts[0].Substring(0, 2).ToUpperInvariant()
                : parts[0].ToUpperInvariant();

        return $"{char.ToUpperInvariant(parts[0][0])}{char.ToUpperInvariant(parts[1][0])}";
    }

    private void ToggleMode()
    {
        if (AxisProgressionManager.Instance == null)
            return;

        AxisProgressionManager.Instance.ToggleInteractionMode();
        RefreshAll();
    }

    private void AddRankToActiveAxis()
    {
        if (AxisProgressionManager.Instance == null || activeAxis == null)
            return;

        if (AxisProgressionManager.Instance.IsTesterMode)
            return;

        AxisProgressionManager.Instance.DebugAddTreeRank(activeAxis);
        RefreshAll();
    }

    private void RemoveRankFromActiveAxis()
    {
        if (AxisProgressionManager.Instance == null || activeAxis == null)
            return;

        if (AxisProgressionManager.Instance.IsTesterMode)
            return;

        AxisProgressionManager.Instance.RemoveTreeRank(activeAxis, 1);
        RefreshAll();
    }

    private void ResetXpForActiveAxis()
    {
        if (AxisProgressionManager.Instance == null || activeAxis == null)
            return;

        if (AxisProgressionManager.Instance.IsTesterMode)
            return;

        AxisProgressionManager.Instance.ResetTreeXp(activeAxis);
        RefreshAll();
    }

    private void HandleNodeStatusChanged(string nodeId, NodeStatus status)
    {
        if (!isOpen)
            return;

        RefreshAll();
    }

    private void HandleTreeProgressChanged(ProgressionAxisSO axis, int rank, float xp)
    {
        if (!isOpen)
            return;

        RefreshAll();
    }

    private void HandleInteractionModeChanged(SkillTreeInteractionMode mode)
    {
        if (!isOpen)
            return;

        RefreshAll();
    }

    private Button CreateUtilityButton(Transform parent, string name, string label, UnityEngine.Events.UnityAction action)
    {
        Button button = CreateButton(parent, name, label, new Vector2(0f, 48f), new Color(0.24f, 0.28f, 0.36f, 1f), action);
        LayoutElement layout = button.gameObject.AddComponent<LayoutElement>();
        layout.preferredHeight = 48f;
        return button;
    }

    private static Button CreateButton(Transform parent, string name, string label, Vector2 size, Color color, UnityEngine.Events.UnityAction onClick)
    {
        GameObject go = CreatePanel(parent, name, color);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = size;
        Button button = go.AddComponent<Button>();
        button.onClick.AddListener(onClick);

        TextMeshProUGUI text = CreateText(go.transform, "Label", label, 14, FontStyles.Bold, Color.white);
        Stretch(text.rectTransform);
        text.alignment = TextAlignmentOptions.Center;
        return button;
    }

    private static GameObject CreatePanel(Transform parent, string name, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        Image image = go.AddComponent<Image>();
        image.color = color;
        return go;
    }

    private static TextMeshProUGUI CreateText(Transform parent, string name, string content, float fontSize, FontStyles style, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = content;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.color = color;
        tmp.richText = true;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void ClearChildren(RectTransform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
            UnityEngine.Object.Destroy(parent.GetChild(i).gameObject);
    }
}

public static class SkillTreeLayoutUtility
{
    private const float DefaultColumns = 5f;
    private const float DefaultRows = 7f;

    public static Vector2 GridToLocal(Vector2Int grid, Vector2 nodeSize, float horizontalSpacing, float verticalSpacing)
    {
        float cellWidth = nodeSize.x + horizontalSpacing;
        float cellHeight = nodeSize.y + verticalSpacing;
        float totalWidth = DefaultColumns * cellWidth;
        float totalHeight = DefaultRows * cellHeight;

        float x = grid.x * cellWidth - totalWidth * 0.5f + cellWidth * 0.5f;
        float y = -grid.y * cellHeight + totalHeight * 0.5f - cellHeight * 0.5f;
        return new Vector2(x, y);
    }
}

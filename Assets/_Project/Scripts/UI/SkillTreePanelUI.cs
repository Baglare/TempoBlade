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

    [Header("Colors")]
    public Color lockedColor = new Color(0.30f, 0.30f, 0.35f, 1f);
    public Color unlockedColor = new Color(0.15f, 0.85f, 0.55f, 1f);
    public Color commitmentColor = new Color(1f, 0.65f, 0.15f, 1f);
    public Color dashT1Color = new Color(0.4f, 0.75f, 1f, 1f);
    public Color hunterColor = new Color(0.9f, 0.35f, 0.2f, 1f);
    public Color flowColor = new Color(0.3f, 0.55f, 1f, 1f);
    public Color parryT1Color = new Color(1f, 0.55f, 0.2f, 1f);
    public Color ballisticColor = new Color(1f, 0.78f, 0.28f, 1f);
    public Color perfectionistColor = new Color(1f, 0.32f, 0.32f, 1f);
    public Color overdriveT1Color = new Color(1f, 0.24f, 0.16f, 1f);
    public Color burstColor = new Color(1f, 0.45f, 0.08f, 1f);
    public Color predatorColor = new Color(0.95f, 0.08f, 0.08f, 1f);
    public Color cadenceT1Color = new Color(0.35f, 0.95f, 0.78f, 1f);
    public Color measuredCadenceColor = new Color(0.30f, 0.75f, 1f, 1f);
    public Color flowCadenceColor = new Color(0.55f, 0.95f, 0.45f, 1f);
    public Color panelBg = new Color(0.08f, 0.08f, 0.12f, 0.96f);
    public Color nodeBg = new Color(0.15f, 0.15f, 0.20f, 1f);
    public Color lineColor = new Color(0.4f, 0.4f, 0.5f, 0.6f);
    public Color lineUnlockedColor = new Color(0.15f, 0.85f, 0.55f, 0.8f);

    [Header("Layout")]
    public float nodeWidth = 155f;
    public float nodeHeight = 48f;
    public float horizontalSpacing = 35f;
    public float verticalSpacing = 62f;
    public float lineWidth = 2.5f;

    private Canvas canvas;
    private GameObject panelRoot;
    private RectTransform nodeContainer;
    private RectTransform lineContainer;
    private RectTransform tierLabelContainer;
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI descText;
    private TextMeshProUGUI warningText;
    private TextMeshProUGUI modeText;
    private TextMeshProUGUI progressText;
    private bool isOpen;
    private ProgressionAxisSO activeAxis;

    private readonly Dictionary<string, NodeSlot> slots = new Dictionary<string, NodeSlot>();
    private readonly List<LineConnection> lines = new List<LineConnection>();

    private class NodeSlot
    {
        public SkillNodeSO node;
        public Image background;
        public Image border;
        public TextMeshProUGUI status;
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
        if (isOpen) ClosePanel();
        else OpenPanel();
    }

    private void OpenPanel()
    {
        isOpen = true;
        panelRoot.SetActive(true);
        RefreshModeAndProgressText();
        RefreshAllSlots();
        Time.timeScale = 0f;
    }

    private void ClosePanel()
    {
        isOpen = false;
        panelRoot.SetActive(false);
        Time.timeScale = 1f;
        if (descText != null)
            descText.text = "";
    }

    private void BuildShell()
    {
        var canvasGO = new GameObject("SkillTreeCanvas");
        canvasGO.transform.SetParent(transform);
        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        canvasGO.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGO.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();

        panelRoot = CreatePanel(canvasGO.transform, "SkillTreePanel", panelBg);
        var panelRect = panelRoot.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = new Vector2(30, 20);
        panelRect.offsetMax = new Vector2(-30, -20);

        titleText = CreateText(panelRoot.transform, "Title", "SKILL TREE", 28, FontStyles.Bold, new Color(0.9f, 0.9f, 0.95f));
        var titleRect = titleText.rectTransform;
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0, -10);
        titleRect.sizeDelta = new Vector2(700, 36);
        titleText.alignment = TextAlignmentOptions.Center;

        warningText = CreateText(panelRoot.transform, "Warning", "", 14, FontStyles.Italic, new Color(1f, 0.8f, 0.3f, 0.7f));
        var warningRect = warningText.rectTransform;
        warningRect.anchorMin = new Vector2(0.5f, 1f);
        warningRect.anchorMax = new Vector2(0.5f, 1f);
        warningRect.pivot = new Vector2(0.5f, 1f);
        warningRect.anchoredPosition = new Vector2(0, -42);
        warningRect.sizeDelta = new Vector2(500, 22);
        warningText.alignment = TextAlignmentOptions.Center;

        var closeBtnGO = CreatePanel(panelRoot.transform, "CloseBtn", new Color(0.8f, 0.2f, 0.2f, 0.9f));
        var closeBtnRect = closeBtnGO.GetComponent<RectTransform>();
        closeBtnRect.anchorMin = new Vector2(1f, 1f);
        closeBtnRect.anchorMax = new Vector2(1f, 1f);
        closeBtnRect.pivot = new Vector2(1f, 1f);
        closeBtnRect.anchoredPosition = new Vector2(-10, -10);
        closeBtnRect.sizeDelta = new Vector2(40, 40);
        closeBtnGO.AddComponent<Button>().onClick.AddListener(ClosePanel);
        var closeLabel = CreateText(closeBtnGO.transform, "CloseLabel", "X", 22, FontStyles.Bold, Color.white);
        Stretch(closeLabel.rectTransform);
        closeLabel.alignment = TextAlignmentOptions.Center;

        CreateAxisButton(panelRoot.transform, "DashTab", "Dash", new Vector2(-210f, -78f), () => SetActiveAxis(dashAxis));
        CreateAxisButton(panelRoot.transform, "ParryTab", "Parry", new Vector2(-70f, -78f), () => SetActiveAxis(parryAxis));
        CreateAxisButton(panelRoot.transform, "OverdriveTab", "Overdrive", new Vector2(70f, -78f), () => SetActiveAxis(overdriveAxis));
        CreateAxisButton(panelRoot.transform, "CadenceTab", "Cadence", new Vector2(210f, -78f), () => SetActiveAxis(cadenceAxis));
        CreateUtilityButton(panelRoot.transform, "ModeToggle", "Mod Degistir", new Vector2(-390f, -78f), () => ToggleMode());
        CreateUtilityButton(panelRoot.transform, "RankDownDebug", "Agac XP Azalt", new Vector2(390f, -118f), () => RemoveRankFromActiveAxis());
        CreateUtilityButton(panelRoot.transform, "XpResetDebug", "Agac XP Sifirla", new Vector2(390f, -158f), () => ResetXpForActiveAxis());
        CreateUtilityButton(panelRoot.transform, "RankDebug", "Agaca Seviye Ekle", new Vector2(390f, -78f), () => AddRankToActiveAxis());

        modeText = CreateText(panelRoot.transform, "ModeText", "", 14, FontStyles.Bold, new Color(0.85f, 0.9f, 1f));
        var modeRect = modeText.rectTransform;
        modeRect.anchorMin = new Vector2(0f, 1f);
        modeRect.anchorMax = new Vector2(0f, 1f);
        modeRect.pivot = new Vector2(0f, 1f);
        modeRect.anchoredPosition = new Vector2(18, -18);
        modeRect.sizeDelta = new Vector2(420, 24);
        modeText.alignment = TextAlignmentOptions.Left;

        progressText = CreateText(panelRoot.transform, "ProgressText", "", 14, FontStyles.Normal, new Color(0.75f, 0.82f, 0.9f));
        var progressRect = progressText.rectTransform;
        progressRect.anchorMin = new Vector2(1f, 1f);
        progressRect.anchorMax = new Vector2(1f, 1f);
        progressRect.pivot = new Vector2(1f, 1f);
        progressRect.anchoredPosition = new Vector2(-60, -58);
        progressRect.sizeDelta = new Vector2(520, 24);
        progressText.alignment = TextAlignmentOptions.Right;

        var nodeContainerGO = new GameObject("NodeContainer");
        nodeContainerGO.transform.SetParent(panelRoot.transform, false);
        nodeContainer = nodeContainerGO.AddComponent<RectTransform>();
        nodeContainer.anchorMin = new Vector2(0.5f, 0.5f);
        nodeContainer.anchorMax = new Vector2(0.5f, 0.5f);
        nodeContainer.pivot = new Vector2(0.5f, 0.5f);
        nodeContainer.anchoredPosition = new Vector2(0, -15);
        nodeContainer.sizeDelta = new Vector2(1000, 620);

        var lineContainerGO = new GameObject("LineContainer");
        lineContainerGO.transform.SetParent(nodeContainer, false);
        lineContainer = lineContainerGO.AddComponent<RectTransform>();
        Stretch(lineContainer);

        var tierContainerGO = new GameObject("TierLabelContainer");
        tierContainerGO.transform.SetParent(nodeContainer, false);
        tierLabelContainer = tierContainerGO.AddComponent<RectTransform>();
        Stretch(tierLabelContainer);

        descText = CreateText(panelRoot.transform, "Description", "", 14, FontStyles.Normal, new Color(0.8f, 0.8f, 0.85f));
        var descRect = descText.rectTransform;
        descRect.anchorMin = new Vector2(0f, 0f);
        descRect.anchorMax = new Vector2(1f, 0f);
        descRect.pivot = new Vector2(0.5f, 0f);
        descRect.anchoredPosition = new Vector2(0, 8);
        descRect.sizeDelta = new Vector2(-90, 48);
        descText.alignment = TextAlignmentOptions.Center;
    }

    private void CreateAxisButton(Transform parent, string name, string label, Vector2 anchoredPosition, UnityEngine.Events.UnityAction onClick)
    {
        var buttonGO = CreatePanel(parent, name, new Color(0.18f, 0.18f, 0.24f, 0.95f));
        var rect = buttonGO.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(120, 34);

        var button = buttonGO.AddComponent<Button>();
        button.onClick.AddListener(onClick);

        var text = CreateText(buttonGO.transform, "Label", label, 16, FontStyles.Bold, Color.white);
        Stretch(text.rectTransform);
        text.alignment = TextAlignmentOptions.Center;
    }

    private void CreateUtilityButton(Transform parent, string name, string label, Vector2 anchoredPosition, UnityEngine.Events.UnityAction onClick)
    {
        var buttonGO = CreatePanel(parent, name, new Color(0.22f, 0.20f, 0.16f, 0.95f));
        var rect = buttonGO.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(155, 34);

        var button = buttonGO.AddComponent<Button>();
        button.onClick.AddListener(onClick);

        var text = CreateText(buttonGO.transform, "Label", label, 13, FontStyles.Bold, Color.white);
        Stretch(text.rectTransform);
        text.alignment = TextAlignmentOptions.Center;
    }

    private void SetActiveAxis(ProgressionAxisSO axis)
    {
        if (axis == null)
            return;

        activeAxis = axis;
        titleText.text = $"{axis.displayName.ToUpperInvariant()} SKILL TREE";
        RefreshModeAndProgressText();
        RebuildAxisView();
    }

    private ProgressionAxisSO GetFirstConfiguredAxis()
    {
        if (dashAxis != null) return dashAxis;
        if (parryAxis != null) return parryAxis;
        if (overdriveAxis != null) return overdriveAxis;
        return cadenceAxis;
    }

    private void RebuildAxisView()
    {
        slots.Clear();
        lines.Clear();
        ClearChildren(nodeContainer, preserve: new HashSet<string> { "LineContainer", "TierLabelContainer" });
        ClearChildren(lineContainer);
        ClearChildren(tierLabelContainer);

        if (activeAxis == null || activeAxis.nodes == null)
            return;

        CreateTierLabels();

        foreach (var node in activeAxis.nodes)
        {
            if (node == null)
                continue;

            BuildNodeSlot(node);
        }

        foreach (var node in activeAxis.nodes)
        {
            if (node == null || node.prerequisites == null)
                continue;

            foreach (var prereq in node.prerequisites)
            {
                if (prereq != null)
                    CreateLine(prereq.nodeId, node.nodeId);
            }
        }

        RefreshAllSlots();
    }

    private void CreateTierLabels()
    {
        string[] labels = { "TIER 1", "TIER 1", "TIER 1", "COMMITMENT", "TIER 2", "TIER 2", "TIER 2" };
        Color tint = GetAxisBaseColor(activeAxis);

        for (int row = 0; row < labels.Length; row++)
        {
            Vector2 pos = GridToLocal(new Vector2Int(-1, row));
            var text = CreateText(tierLabelContainer, $"TierLabel_{row}", labels[row], 12, FontStyles.Bold, tint * 0.7f);
            var rect = text.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(110, 30);
            text.alignment = TextAlignmentOptions.Right;
        }
    }

    private void BuildNodeSlot(SkillNodeSO node)
    {
        if (!GetCurrentGrid().TryGetValue(node.nodeId, out Vector2Int gridPos))
        {
            Debug.LogWarning($"[SkillTreePanelUI] Grid position tanimli degil: {node.nodeId}");
            return;
        }

        Vector2 localPos = GridToLocal(gridPos);
        var slotGO = new GameObject($"Node_{node.nodeId}");
        slotGO.transform.SetParent(nodeContainer, false);

        var rect = slotGO.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = localPos;
        rect.sizeDelta = new Vector2(nodeWidth, nodeHeight);

        var border = slotGO.AddComponent<Image>();
        border.color = GetNodeTintColor(node);

        var inner = CreatePanel(slotGO.transform, "Inner", nodeBg);
        var innerRect = inner.GetComponent<RectTransform>();
        innerRect.anchorMin = Vector2.zero;
        innerRect.anchorMax = Vector2.one;
        innerRect.offsetMin = new Vector2(2, 2);
        innerRect.offsetMax = new Vector2(-2, -2);
        var background = inner.GetComponent<Image>();

        var nameText = CreateText(inner.transform, "Name", node.displayName, 12, FontStyles.Bold, Color.white);
        nameText.alignment = TextAlignmentOptions.Center;
        var nameRect = nameText.rectTransform;
        nameRect.anchorMin = new Vector2(0f, 0.25f);
        nameRect.anchorMax = new Vector2(1f, 1f);
        nameRect.offsetMin = new Vector2(5, 0);
        nameRect.offsetMax = new Vector2(-5, -4);

        var statusText = CreateText(inner.transform, "Status", "LOCKED", 11, FontStyles.Normal, Color.gray);
        statusText.alignment = TextAlignmentOptions.Center;
        var statusRect = statusText.rectTransform;
        statusRect.anchorMin = new Vector2(0f, 0f);
        statusRect.anchorMax = new Vector2(1f, 0.3f);
        statusRect.offsetMin = new Vector2(5, 2);
        statusRect.offsetMax = new Vector2(-5, 0);

        var button = slotGO.AddComponent<Button>();
        button.targetGraphic = border;
        button.onClick.AddListener(() => OnNodeClicked(node));

        var trigger = slotGO.AddComponent<UnityEngine.EventSystems.EventTrigger>();
        var enter = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter };
        enter.callback.AddListener(_ => OnNodeHover(node));
        trigger.triggers.Add(enter);
        var exit = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit };
        exit.callback.AddListener(_ => descText.text = "");
        trigger.triggers.Add(exit);

        slots[node.nodeId] = new NodeSlot
        {
            node = node,
            background = background,
            border = border,
            status = statusText
        };
    }

    private void CreateLine(string fromId, string toId)
    {
        if (!GetCurrentGrid().ContainsKey(fromId) || !GetCurrentGrid().ContainsKey(toId))
            return;

        var lineGO = new GameObject($"Line_{fromId}_{toId}");
        lineGO.transform.SetParent(lineContainer, false);
        var rect = lineGO.AddComponent<RectTransform>();
        var image = lineGO.AddComponent<Image>();
        image.color = lineColor;

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
        float totalCols = 5f;
        float totalRows = 7f;
        float cellW = nodeWidth + horizontalSpacing;
        float cellH = nodeHeight + verticalSpacing;
        float totalW = totalCols * cellW;
        float totalH = totalRows * cellH;

        float x = grid.x * cellW - totalW * 0.5f + cellW * 0.5f;
        float y = -grid.y * cellH + totalH * 0.5f - cellH * 0.5f;
        return new Vector2(x, y);
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
        if (node.isCommitmentNode)
            return commitmentColor;

        string id = node.nodeId;
        if (activeAxis == parryAxis)
        {
            if (id.Contains("_t2b_")) return ballisticColor;
            if (id.Contains("_t2p_")) return perfectionistColor;
            return parryT1Color;
        }

        if (activeAxis == overdriveAxis)
        {
            if (id.Contains("_t2burst_")) return burstColor;
            if (id.Contains("_t2pred_")) return predatorColor;
            return overdriveT1Color;
        }

        if (activeAxis == cadenceAxis)
        {
            if (id.Contains("_t2measured_")) return measuredCadenceColor;
            if (id.Contains("_t2flow_")) return flowCadenceColor;
            return cadenceT1Color;
        }

        if (id.Contains("_t2h_")) return hunterColor;
        if (id.Contains("_t2f_")) return flowColor;
        return dashT1Color;
    }

    private Color GetAxisBaseColor(ProgressionAxisSO axis)
    {
        if (axis == parryAxis) return parryT1Color;
        if (axis == overdriveAxis) return overdriveT1Color;
        if (axis == cadenceAxis) return cadenceT1Color;
        return dashT1Color;
    }

    private void RefreshAllSlots()
    {
        foreach (var slot in slots.Values)
            RefreshSlot(slot);

        foreach (var line in lines)
        {
            bool fromUnlocked = AxisProgressionManager.Instance != null &&
                slots.ContainsKey(line.fromId) &&
                AxisProgressionManager.Instance.IsNodeUnlocked(slots[line.fromId].node);
            bool toUnlocked = AxisProgressionManager.Instance != null &&
                slots.ContainsKey(line.toId) &&
                AxisProgressionManager.Instance.IsNodeUnlocked(slots[line.toId].node);
            line.image.color = (fromUnlocked && toUnlocked) ? lineUnlockedColor : lineColor;
        }
    }

    private void RefreshSlot(NodeSlot slot)
    {
        if (slot == null || slot.node == null)
            return;

        var mgr = AxisProgressionManager.Instance;
        NodeStatus status = mgr != null ? mgr.GetNodeStatus(slot.node) : NodeStatus.Hidden;
        Color tint = GetNodeTintColor(slot.node);

        if (status == NodeStatus.Unlocked)
        {
            slot.background.color = new Color(tint.r * 0.3f, tint.g * 0.3f, tint.b * 0.3f, 1f);
            slot.border.color = tint;
            slot.status.text = "ACTIVE";
            slot.status.color = unlockedColor;
        }
        else if (status == NodeStatus.Unlockable)
        {
            slot.background.color = nodeBg;
            slot.border.color = new Color(tint.r * 0.7f, tint.g * 0.7f, tint.b * 0.5f, 1f);
            slot.status.text = "UNLOCKABLE";
            slot.status.color = new Color(1f, 0.85f, 0.3f);
        }
        else
        {
            slot.background.color = new Color(0.10f, 0.10f, 0.13f, 1f);
            slot.border.color = new Color(tint.r * 0.2f, tint.g * 0.2f, tint.b * 0.2f, 1f);
            slot.status.text = "LOCKED";
            slot.status.color = new Color(0.4f, 0.4f, 0.45f);
        }
    }

    private bool CheckPrereqs(SkillNodeSO node)
    {
        if (node.prerequisites == null || node.prerequisites.Length == 0)
            return true;

        var mgr = AxisProgressionManager.Instance;
        if (mgr == null)
            return false;

        foreach (var prereq in node.prerequisites)
        {
            if (prereq != null && !mgr.IsNodeUnlocked(prereq))
                return false;
        }

        return true;
    }

    private void OnNodeClicked(SkillNodeSO node)
    {
        if (AxisProgressionManager.Instance == null)
            return;

        var manager = AxisProgressionManager.Instance;
        bool testerMode = manager.IsTesterMode;
        if (!testerMode && manager.IsNodeUnlocked(node))
        {
            descText.text = $"<color=#26D98A>{node.displayName} zaten acik.</color>";
            RefreshAllSlots();
            return;
        }

        int result = testerMode ? manager.SmartToggleNode(node) : (manager.TryUnlockNode(node) ? 1 : -2);

        if (result == 1)
        {
            AudioManager.Play(AudioEventId.UIUnlock, gameObject);
            descText.text = $"<color=#26D98A>{node.displayName} acildi.</color>";
        }
        else if (result == 0)
        {
            descText.text = $"<color=#FF8844>{node.displayName} kapatildi.</color>";
        }
        else if (result == -2)
        {
            AudioManager.Play(AudioEventId.UIFail, gameObject);
            descText.text = $"<color=#FF4444>{manager.GetBlockReason(node)}</color>";
        }
        else
        {
            AudioManager.Play(AudioEventId.UIFail, gameObject);
            descText.text = $"<color=#FF4444>On kosullar eksik.</color>";
        }

        RefreshModeAndProgressText();
        RefreshAllSlots();
    }

    private void OnNodeHover(SkillNodeSO node)
    {
        if (node == null)
            return;

        var mgr = AxisProgressionManager.Instance;
        NodeStatus nodeStatus = mgr != null ? mgr.GetNodeStatus(node) : NodeStatus.Hidden;
        bool unlocked = nodeStatus == NodeStatus.Unlocked;
        string status = nodeStatus == NodeStatus.Unlocked
            ? "<color=#26D98A>ACTIVE</color>"
            : nodeStatus == NodeStatus.Unlockable
                ? "<color=#FFD94A>UNLOCKABLE</color>"
                : "<color=#666666>LOCKED</color>";

        string prereqInfo = "";
        if (!unlocked && node.prerequisites != null && node.prerequisites.Length > 0)
        {
            prereqInfo = "\nRequired: ";
            foreach (var prereq in node.prerequisites)
            {
                if (prereq == null) continue;
                bool prereqUnlocked = mgr != null && mgr.IsNodeUnlocked(prereq);
                prereqInfo += prereqUnlocked ? $"<color=#26D98A>{prereq.displayName}</color> " : $"<color=#FF4444>{prereq.displayName}</color> ";
            }
        }

        string blockReason = (!unlocked && mgr != null && nodeStatus == NodeStatus.VisibleLocked)
            ? $"\n<color=#FF6666>{mgr.GetBlockReason(node)}</color>"
            : "";

        string rankInfo = mgr != null && activeAxis != null
            ? $"\nRank: {mgr.GetTreeRank(activeAxis)} / Gerekli: {mgr.GetRequiredRankForNode(node)} | Perk Puani: {mgr.GetAvailablePerkPoints(activeAxis)}/{mgr.GetTotalPerkPoints(activeAxis)}"
            : "";

        descText.text = $"<b>{node.displayName}</b> [{status}]\n{node.description}{rankInfo}{prereqInfo}{blockReason}";
    }

    private void ToggleMode()
    {
        if (AxisProgressionManager.Instance == null)
            return;

        AxisProgressionManager.Instance.ToggleInteractionMode();
        RefreshModeAndProgressText();
        RefreshAllSlots();
    }

    private void AddRankToActiveAxis()
    {
        if (AxisProgressionManager.Instance == null || activeAxis == null)
            return;

        if (AxisProgressionManager.Instance.IsTesterMode)
        {
            descText.text = "<color=#FFAA44>Tester modda XP/rank kullanilmaz.</color>";
            return;
        }

        AxisProgressionManager.Instance.DebugAddTreeRank(activeAxis);
        RefreshModeAndProgressText();
        RefreshAllSlots();
    }

    private void RemoveRankFromActiveAxis()
    {
        if (AxisProgressionManager.Instance == null || activeAxis == null)
            return;

        if (AxisProgressionManager.Instance.IsTesterMode)
        {
            descText.text = "<color=#FFAA44>Tester modda XP/rank kullanilmaz.</color>";
            return;
        }

        AxisProgressionManager.Instance.RemoveTreeRank(activeAxis, 1);
        descText.text = "<color=#FF8844>Agac bir rank geri alindi. Rank'a bagli perkler otomatik kapatildi.</color>";
        RefreshModeAndProgressText();
        RefreshAllSlots();
    }

    private void ResetXpForActiveAxis()
    {
        if (AxisProgressionManager.Instance == null || activeAxis == null)
            return;

        if (AxisProgressionManager.Instance.IsTesterMode)
        {
            descText.text = "<color=#FFAA44>Tester modda XP/rank kullanilmaz.</color>";
            return;
        }

        AxisProgressionManager.Instance.ResetTreeXp(activeAxis);
        descText.text = "<color=#FF6666>Agac XP sifirlandi. Rank'a bagli tum perkler otomatik kapatildi.</color>";
        RefreshModeAndProgressText();
        RefreshAllSlots();
    }

    private void RefreshModeAndProgressText()
    {
        var mgr = AxisProgressionManager.Instance;
        if (mgr == null)
            return;

        if (modeText != null)
            modeText.text = mgr.IsTesterMode
                ? "MOD: TESTER (XP yok, debug unlock)"
                : "MOD: NORMAL PROGRESSION";

        if (warningText != null)
            warningText.text = mgr.IsTesterMode
                ? "[TESTER] Perk ac/kapat serbest; Focus ve yol kilitleri korunur."
                : "[NORMAL] XP/rank/perk puani/prerequisite kurallari aktif.";

        if (progressText != null && activeAxis != null)
        {
            int rank = mgr.GetTreeRank(activeAxis);
            float xp = mgr.GetTreeXp(activeAxis);
            int nextRank = Mathf.Min(mgr.GetProgressionConfig().MaxRank, rank + 1);
            int nextXp = mgr.GetProgressionConfig().GetRequiredXpForRank(nextRank);
            int availablePerkPoints = mgr.GetAvailablePerkPoints(activeAxis);
            int spentPerkPoints = mgr.GetSpentPerkPoints(activeAxis);
            string route = mgr.GetChosenTier2Route(activeAxis);
            string routeText = string.IsNullOrEmpty(route) ? "" : $" | Route: {route}";
            progressText.text = $"Rank {rank} | XP {xp:F0}/{nextXp} | Perk {availablePerkPoints} | Spent {spentPerkPoints}{routeText}";
        }
    }

    private void HandleNodeStatusChanged(string nodeId, NodeStatus status)
    {
        if (!isOpen || !slots.TryGetValue(nodeId, out var slot))
            return;

        RefreshSlot(slot);
        RefreshAllSlots();
    }

    private void HandleTreeProgressChanged(ProgressionAxisSO axis, int rank, float xp)
    {
        if (axis == activeAxis)
            RefreshModeAndProgressText();

        if (isOpen)
            RefreshAllSlots();
    }

    private void HandleInteractionModeChanged(SkillTreeInteractionMode mode)
    {
        RefreshModeAndProgressText();
        if (isOpen)
            RefreshAllSlots();
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void ClearChildren(RectTransform parent, HashSet<string> preserve = null)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.GetChild(i);
            if (preserve != null && preserve.Contains(child.name))
                continue;

            child.gameObject.SetActive(false);
            Object.Destroy(child.gameObject);
        }
    }

    private GameObject CreatePanel(Transform parent, string name, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var image = go.AddComponent<Image>();
        image.color = color;
        go.AddComponent<RectTransform>();
        return go;
    }

    private TextMeshProUGUI CreateText(Transform parent, string name, string content, float fontSize, FontStyles style, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = content;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.color = color;
        tmp.enableAutoSizing = false;
        tmp.overflowMode = TextOverflowModes.Truncate;
        tmp.richText = true;
        return tmp;
    }
}

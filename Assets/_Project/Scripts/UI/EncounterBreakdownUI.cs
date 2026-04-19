using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class EncounterBreakdownUI : MonoBehaviour
{
    public static EncounterBreakdownUI Instance { get; private set; }

    private Canvas canvas;
    private GameObject root;
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI subtitleText;
    private RectTransform contentRoot;
    private Button continueButton;
    private bool isShowing;
    private System.Action closeCallback;

    public static EncounterBreakdownUI EnsureInstance()
    {
        if (Instance != null)
            return Instance;

        GameObject go = new GameObject("EncounterBreakdownUI");
        Instance = go.AddComponent<EncounterBreakdownUI>();
        DontDestroyOnLoad(go);
        return Instance;
    }

    public bool IsShowing => isShowing;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        BuildUi();
        root.SetActive(false);
    }

    public bool Show(EncounterBreakdownSnapshot snapshot, System.Action onClosed)
    {
        if (snapshot == null || snapshot.axisBreakdowns.Count == 0)
            return false;

        closeCallback = onClosed;
        Populate(snapshot);
        isShowing = true;
        root.SetActive(true);

        if (GameManager.Instance != null)
            GameManager.Instance.SetState(GameManager.GameState.Paused);
        else
            Time.timeScale = 0f;

        return true;
    }

    private void Close()
    {
        if (!isShowing)
            return;

        isShowing = false;
        root.SetActive(false);

        if (GameManager.Instance != null)
            GameManager.Instance.SetState(GameManager.GameState.Gameplay);
        else
            Time.timeScale = 1f;

        System.Action callback = closeCallback;
        closeCallback = null;
        callback?.Invoke();
    }

    private void BuildUi()
    {
        GameObject canvasGo = new GameObject("EncounterBreakdownCanvas");
        canvasGo.transform.SetParent(transform, false);
        canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 210;
        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasGo.AddComponent<GraphicRaycaster>();

        root = CreatePanel(canvasGo.transform, "Root", new Color(0.04f, 0.05f, 0.08f, 0.92f));
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        GameObject panel = CreatePanel(root.transform, "Panel", new Color(0.08f, 0.1f, 0.15f, 0.98f));
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(1500f, 860f);

        titleText = CreateText(panel.transform, "Title", "ROOM BREAKDOWN", 34, FontStyles.Bold, Color.white);
        RectTransform titleRect = titleText.rectTransform;
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -28f);
        titleRect.sizeDelta = new Vector2(900f, 42f);
        titleText.alignment = TextAlignmentOptions.Center;

        subtitleText = CreateText(panel.transform, "Subtitle", "", 16, FontStyles.Normal, new Color(0.78f, 0.84f, 0.92f));
        RectTransform subtitleRect = subtitleText.rectTransform;
        subtitleRect.anchorMin = new Vector2(0.5f, 1f);
        subtitleRect.anchorMax = new Vector2(0.5f, 1f);
        subtitleRect.pivot = new Vector2(0.5f, 1f);
        subtitleRect.anchoredPosition = new Vector2(0f, -72f);
        subtitleRect.sizeDelta = new Vector2(1100f, 24f);
        subtitleText.alignment = TextAlignmentOptions.Center;

        GameObject content = CreatePanel(panel.transform, "Content", new Color(0.11f, 0.13f, 0.18f, 0.85f));
        contentRoot = content.GetComponent<RectTransform>();
        contentRoot.anchorMin = new Vector2(0.03f, 0.14f);
        contentRoot.anchorMax = new Vector2(0.97f, 0.88f);
        contentRoot.offsetMin = Vector2.zero;
        contentRoot.offsetMax = Vector2.zero;
        GridLayoutGroup grid = content.AddComponent<GridLayoutGroup>();
        grid.padding = new RectOffset(18, 18, 18, 18);
        grid.spacing = new Vector2(18f, 18f);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 2;
        grid.cellSize = new Vector2(650f, 250f);
        grid.childAlignment = TextAnchor.UpperCenter;

        GameObject continueGo = CreatePanel(panel.transform, "ContinueButton", new Color(0.2f, 0.55f, 0.32f, 0.96f));
        RectTransform continueRect = continueGo.GetComponent<RectTransform>();
        continueRect.anchorMin = new Vector2(0.5f, 0f);
        continueRect.anchorMax = new Vector2(0.5f, 0f);
        continueRect.pivot = new Vector2(0.5f, 0f);
        continueRect.anchoredPosition = new Vector2(0f, 28f);
        continueRect.sizeDelta = new Vector2(260f, 52f);

        continueButton = continueGo.AddComponent<Button>();
        continueButton.onClick.AddListener(Close);

        TextMeshProUGUI continueText = CreateText(continueGo.transform, "ContinueLabel", "CONTINUE", 18, FontStyles.Bold, Color.white);
        Stretch(continueText.rectTransform);
        continueText.alignment = TextAlignmentOptions.Center;
    }

    private void Populate(EncounterBreakdownSnapshot snapshot)
    {
        titleText.text = "ROOM BREAKDOWN";
        subtitleText.text = $"{snapshot.roomName}  |  {snapshot.encounterType}  |  {snapshot.difficulty}";

        ClearChildren(contentRoot);
        foreach (AxisBreakdownSnapshot axis in snapshot.axisBreakdowns)
            BuildAxisCard(axis);
    }

    private void BuildAxisCard(AxisBreakdownSnapshot axis)
    {
        GameObject card = CreatePanel(contentRoot, $"{axis.axisId}_Card", new Color(0.13f, 0.16f, 0.22f, 0.96f));
        RectTransform cardRect = card.GetComponent<RectTransform>();
        cardRect.sizeDelta = new Vector2(650f, 250f);

        TextMeshProUGUI axisTitle = CreateText(card.transform, "AxisTitle", axis.axisDisplayName.ToUpperInvariant(), 24, FontStyles.Bold, Color.white);
        RectTransform titleRect = axisTitle.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(0f, 1f);
        titleRect.pivot = new Vector2(0f, 1f);
        titleRect.anchoredPosition = new Vector2(18f, -14f);
        titleRect.sizeDelta = new Vector2(450f, 32f);

        TextMeshProUGUI affinityText = CreateText(card.transform, "AffinityText", $"Affinity {axis.affinity:P0}  |  XP {axis.xpAwarded:F0}", 18, FontStyles.Bold, axis.receivedXp ? new Color(1f, 0.86f, 0.34f) : new Color(0.78f, 0.82f, 0.9f));
        RectTransform affinityRect = affinityText.rectTransform;
        affinityRect.anchorMin = new Vector2(1f, 1f);
        affinityRect.anchorMax = new Vector2(1f, 1f);
        affinityRect.pivot = new Vector2(1f, 1f);
        affinityRect.anchoredPosition = new Vector2(-18f, -16f);
        affinityRect.sizeDelta = new Vector2(320f, 28f);
        affinityText.alignment = TextAlignmentOptions.Right;

        GameObject barBg = CreatePanel(card.transform, "BarBg", new Color(0.08f, 0.1f, 0.14f, 1f));
        RectTransform barBgRect = barBg.GetComponent<RectTransform>();
        barBgRect.anchorMin = new Vector2(0f, 1f);
        barBgRect.anchorMax = new Vector2(1f, 1f);
        barBgRect.pivot = new Vector2(0.5f, 1f);
        barBgRect.anchoredPosition = new Vector2(0f, -52f);
        barBgRect.offsetMin = new Vector2(18f, 0f);
        barBgRect.offsetMax = new Vector2(-18f, 0f);
        barBgRect.sizeDelta = new Vector2(0f, 12f);

        GameObject barFill = CreatePanel(barBg.transform, "BarFill", axis.receivedXp ? new Color(0.98f, 0.72f, 0.22f, 1f) : new Color(0.35f, 0.68f, 1f, 1f));
        RectTransform barFillRect = barFill.GetComponent<RectTransform>();
        barFillRect.anchorMin = new Vector2(0f, 0f);
        barFillRect.anchorMax = new Vector2(Mathf.Clamp01(axis.affinity), 1f);
        barFillRect.offsetMin = Vector2.zero;
        barFillRect.offsetMax = Vector2.zero;

        TextMeshProUGUI reasonText = CreateText(card.transform, "ReasonText", string.IsNullOrEmpty(axis.xpReason) ? "XP karari icin anlamli veri olusmadi." : axis.xpReason, 14, FontStyles.Italic, new Color(0.84f, 0.87f, 0.92f));
        RectTransform reasonRect = reasonText.rectTransform;
        reasonRect.anchorMin = new Vector2(0f, 1f);
        reasonRect.anchorMax = new Vector2(1f, 1f);
        reasonRect.pivot = new Vector2(0.5f, 1f);
        reasonRect.anchoredPosition = new Vector2(0f, -72f);
        reasonRect.offsetMin = new Vector2(18f, 0f);
        reasonRect.offsetMax = new Vector2(-18f, 0f);
        reasonRect.sizeDelta = new Vector2(0f, 22f);
        reasonText.alignment = TextAlignmentOptions.Left;

        string lines = BuildContributionLines(axis);
        TextMeshProUGUI details = CreateText(card.transform, "Details", lines, 15, FontStyles.Normal, new Color(0.9f, 0.92f, 0.96f));
        RectTransform detailsRect = details.rectTransform;
        detailsRect.anchorMin = new Vector2(0f, 0f);
        detailsRect.anchorMax = new Vector2(1f, 0f);
        detailsRect.pivot = new Vector2(0.5f, 0f);
        detailsRect.anchoredPosition = new Vector2(0f, 18f);
        detailsRect.offsetMin = new Vector2(18f, 0f);
        detailsRect.offsetMax = new Vector2(-18f, 0f);
        detailsRect.sizeDelta = new Vector2(0f, 112f);
        details.alignment = TextAlignmentOptions.TopLeft;
    }

    private string BuildContributionLines(AxisBreakdownSnapshot axis)
    {
        List<string> lines = new List<string>();
        AddLineIfNeeded(lines, axis.axisId, "primary", axis.primary);
        AddLineIfNeeded(lines, axis.axisId, "secondary", axis.secondary);
        AddLineIfNeeded(lines, axis.axisId, "conversion", axis.conversion);
        AddLineIfNeeded(lines, axis.axisId, "utility", axis.utility);
        AddLineIfNeeded(lines, axis.axisId, "fifth", axis.fifth);
        if (axis.penalty > 0.001f)
            lines.Add($"<color=#FF7D6B>{GetLabel(axis.axisId, "penalty")}: -{axis.penalty:F2}</color>");
        lines.Add($"Variety Bonus: x{axis.varietyBonus:F2}");
        return string.Join("\n", lines);
    }

    private void AddLineIfNeeded(List<string> lines, string axisId, string slot, float value)
    {
        if (value <= 0.001f)
            return;

        lines.Add($"{GetLabel(axisId, slot)}: +{value:F2}");
    }

    private string GetLabel(string axisId, string slot)
    {
        return axisId switch
        {
            "axis_dash" => slot switch
            {
                "primary" => "Risk Dash",
                "secondary" => "Close Pressure Dash",
                "conversion" => "Dash Follow-Up",
                "utility" => "Angle Gain",
                "fifth" => "Flow Carry",
                "penalty" => "Panic Dash Penalty",
                _ => slot
            },
            "axis_parry" => slot switch
            {
                "primary" => "Valid Parry",
                "secondary" => "Perfect Parry",
                "conversion" => "Punish Conversion",
                "utility" => "Deflect Value",
                "fifth" => "Control Carry",
                "penalty" => "Parry Spam Penalty",
                _ => slot
            },
            "axis_overdrive" => slot switch
            {
                "primary" => "Threshold Entry",
                "secondary" => "High Tempo Uptime",
                "conversion" => "High Tempo Payoff",
                "utility" => "Heat Chain",
                "fifth" => "Burst Carry",
                "penalty" => "Burnout Penalty",
                _ => slot
            },
            "axis_cadence" => slot switch
            {
                "primary" => "Tempo Retention",
                "secondary" => "Transition Smoothness",
                "conversion" => "Stability",
                "utility" => "Recovery Rhythm",
                "fifth" => "Flow Continuity",
                "penalty" => "Rhythm Break Penalty",
                _ => slot
            },
            _ => slot
        };
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

    private static TextMeshProUGUI CreateText(Transform parent, string name, string content, float size, FontStyles style, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
        text.text = content;
        text.fontSize = size;
        text.fontStyle = style;
        text.color = color;
        text.richText = true;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Overflow;
        return text;
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
            Destroy(parent.GetChild(i).gameObject);
    }
}

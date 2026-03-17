using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class SkillTreeScreenUI : MonoBehaviour
{
    [Header("Panel")]
    public GameObject rootPanel;
    public Key toggleKey = Key.K;

    [Header("Data")]
    public string axisId = "axis_dash";

    [Header("UI")]
    public Transform tier1Parent;
    public Transform tier2Parent;
    public Button nodeButtonPrefab;
    public TextMeshProUGUI nodeDetailsText;
    public TextMeshProUGUI axisTitleText;

    [Header("Detail Actions")]
    public Button unlockSelectedButton;
    public Button deactivateSelectedButton;
    public Button clearSelectionButton;
    public bool allowManualDeactivate = true;

    [Header("Tree Visual")]
    public bool drawConnections = true;
    public RectTransform connectionsParent;
    public Color connectionColor = new Color(1f, 1f, 1f, 0.35f);
    public float connectionThickness = 2f;

    [Header("Layout")]
    public bool autoFitButtonsInColumn = true;
    public float minNodeHeight = 52f;
    public float maxNodeHeight = 88f;

    [Header("Legacy Interaction (Optional)")]
    public bool unlockOnNodeClick = false;
    public bool requireSecondClickToUnlock = false;
    public float unlockConfirmWindow = 1.2f;

    private AxisProgressionManager manager;
    private ProgressionAxisSO axis;
    private bool isOpen;
    private readonly List<Button> spawnedButtons = new List<Button>();
    private readonly List<Image> spawnedConnections = new List<Image>();
    private readonly Dictionary<SkillNodeSO, RectTransform> nodeRects = new Dictionary<SkillNodeSO, RectTransform>();
    private SkillNodeSO selectedNode;
    private SkillNodeSO pendingUnlockNode;
    private float pendingUnlockStartedAt;

    private void Start()
    {
        manager = AxisProgressionManager.Instance;
        if (rootPanel != null) rootPanel.SetActive(false);
        WireActionButtons();
        TryResolveAxis();
    }

    private void Update()
    {
        if (Keyboard.current == null) return;
        if (!Keyboard.current[toggleKey].wasPressedThisFrame) return;
        Toggle();
    }

    public void Toggle()
    {
        if (isOpen) Close();
        else Open();
    }

    public void Open()
    {
        isOpen = true;
        if (rootPanel != null) rootPanel.SetActive(true);
        TryResolveAxis();
        Rebuild();
    }

    public void Close()
    {
        isOpen = false;
        if (rootPanel != null) rootPanel.SetActive(false);
    }

    private void WireActionButtons()
    {
        if (unlockSelectedButton != null)
        {
            unlockSelectedButton.onClick.RemoveAllListeners();
            unlockSelectedButton.onClick.AddListener(OnUnlockSelectedPressed);
        }

        if (deactivateSelectedButton != null)
        {
            deactivateSelectedButton.onClick.RemoveAllListeners();
            deactivateSelectedButton.onClick.AddListener(OnDeactivateSelectedPressed);
        }

        if (clearSelectionButton != null)
        {
            clearSelectionButton.onClick.RemoveAllListeners();
            clearSelectionButton.onClick.AddListener(OnClearSelectionPressed);
        }

        RefreshActionButtons();
    }

    private void TryResolveAxis()
    {
        if (manager == null) manager = AxisProgressionManager.Instance;
        if (manager == null || manager.database == null) return;
        axis = manager.database.GetAxisById(axisId);
        if (axisTitleText != null)
            axisTitleText.text = axis != null ? axis.displayName : axisId;
    }

    public void Rebuild()
    {
        ClearButtons();
        ClearConnections();
        nodeRects.Clear();

        if (manager == null || axis == null || nodeButtonPrefab == null)
        {
            RefreshActionButtons();
            return;
        }

        BuildTier(axis.Tier1Nodes, tier1Parent);
        BuildTier(axis.Tier2Nodes, tier2Parent);

        Canvas.ForceUpdateCanvases();
        DrawConnections();
        RefreshActionButtons();
    }

    private void BuildTier(List<SkillNodeSO> nodes, Transform parent)
    {
        if (nodes == null || parent == null) return;

        float preferredHeight = CalculatePreferredNodeHeight(parent, nodes.Count);

        foreach (SkillNodeSO node in nodes)
        {
            if (node == null) continue;
            Button btn = Instantiate(nodeButtonPrefab, parent);
            spawnedButtons.Add(btn);

            RectTransform btnRect = btn.transform as RectTransform;
            if (btnRect != null)
            {
                nodeRects[node] = btnRect;
                if (autoFitButtonsInColumn)
                {
                    LayoutElement layout = btn.GetComponent<LayoutElement>();
                    if (layout == null) layout = btn.gameObject.AddComponent<LayoutElement>();
                    layout.preferredHeight = preferredHeight;
                    layout.minHeight = preferredHeight;
                    layout.flexibleHeight = 0f;
                }
            }

            NodeStatus status = manager.GetNodeStatus(node);
            TextMeshProUGUI txt = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null)
            {
                txt.text = $"{node.displayName}\n{StatusText(status)}";
                txt.color = StatusColor(status);
            }

            btn.interactable = status != NodeStatus.Hidden;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => OnNodeClicked(node));
        }
    }

    private float CalculatePreferredNodeHeight(Transform parent, int nodeCount)
    {
        if (!autoFitButtonsInColumn || nodeCount <= 0) return maxNodeHeight;

        RectTransform parentRect = parent as RectTransform;
        if (parentRect == null || parentRect.rect.height <= 0f) return maxNodeHeight;

        VerticalLayoutGroup layout = parent.GetComponent<VerticalLayoutGroup>();
        float spacing = layout != null ? layout.spacing : 8f;
        float padding = layout != null ? (layout.padding.top + layout.padding.bottom) : 0f;
        float available = parentRect.rect.height - padding - spacing * (nodeCount - 1);
        float raw = available / nodeCount;
        return Mathf.Clamp(raw, minNodeHeight, maxNodeHeight);
    }

    private void OnNodeClicked(SkillNodeSO node)
    {
        if (node == null || manager == null) return;

        selectedNode = node;
        pendingUnlockNode = null;
        UpdateDetailsForSelectedNode();
        RefreshActionButtons();

        if (!unlockOnNodeClick) return;
        TryUnlockSelectedFromClickFlow();
    }

    private void TryUnlockSelectedFromClickFlow()
    {
        if (selectedNode == null || manager == null) return;
        NodeStatus status = manager.GetNodeStatus(selectedNode);
        if (status != NodeStatus.Unlockable) return;

        if (requireSecondClickToUnlock)
        {
            bool isSamePending = pendingUnlockNode == selectedNode;
            bool withinWindow = (Time.unscaledTime - pendingUnlockStartedAt) <= unlockConfirmWindow;
            if (!isSamePending || !withinWindow)
            {
                pendingUnlockNode = selectedNode;
                pendingUnlockStartedAt = Time.unscaledTime;
                if (nodeDetailsText != null)
                {
                    nodeDetailsText.text =
                        $"{selectedNode.displayName}\n" +
                        $"Tier: {selectedNode.tier}\n" +
                        $"Durum: {StatusText(status)}\n\n" +
                        $"{selectedNode.description}\n\n" +
                        $"Satin almak icin {unlockConfirmWindow:0.0}s icinde tekrar tikla.";
                }
                return;
            }
        }

        if (manager.TryUnlockNode(selectedNode))
        {
            if (SaveManager.Instance != null) SaveManager.Instance.Save();
            pendingUnlockNode = null;
            Rebuild();
            UpdateDetailsForSelectedNode();
        }
    }

    public void OnUnlockSelectedPressed()
    {
        if (selectedNode == null || manager == null) return;
        NodeStatus status = manager.GetNodeStatus(selectedNode);
        if (status != NodeStatus.Unlockable) return;

        if (manager.TryUnlockNode(selectedNode))
        {
            if (SaveManager.Instance != null) SaveManager.Instance.Save();
            Rebuild();
            UpdateDetailsForSelectedNode();
        }
    }

    public void OnDeactivateSelectedPressed()
    {
        if (!allowManualDeactivate) return;
        if (selectedNode == null || manager == null) return;
        NodeStatus status = manager.GetNodeStatus(selectedNode);
        if (status != NodeStatus.Unlocked) return;

        bool locked = manager.TryLockNode(selectedNode, true);
        if (!locked) return;

        if (SaveManager.Instance != null) SaveManager.Instance.Save();
        Rebuild();
        UpdateDetailsForSelectedNode();
    }

    public void OnClearSelectionPressed()
    {
        selectedNode = null;
        pendingUnlockNode = null;
        if (nodeDetailsText != null)
            nodeDetailsText.text = "Bir perk sec.";
        RefreshActionButtons();
    }

    private void UpdateDetailsForSelectedNode()
    {
        if (nodeDetailsText == null) return;

        if (selectedNode == null)
        {
            nodeDetailsText.text = "Bir perk sec.";
            return;
        }

        NodeStatus status = manager != null ? manager.GetNodeStatus(selectedNode) : NodeStatus.Hidden;

        string prereqText = "Yok";
        if (selectedNode.prerequisites != null && selectedNode.prerequisites.Length > 0)
        {
            List<string> names = new List<string>();
            for (int i = 0; i < selectedNode.prerequisites.Length; i++)
            {
                SkillNodeSO prereq = selectedNode.prerequisites[i];
                if (prereq != null) names.Add(prereq.displayName);
            }
            if (names.Count > 0) prereqText = string.Join(", ", names);
        }

        nodeDetailsText.text =
            $"{selectedNode.displayName}\n" +
            $"Tier: {selectedNode.tier}\n" +
            $"Durum: {StatusText(status)}\n" +
            $"Gereklilik: {prereqText}\n\n" +
            $"{selectedNode.description}";
    }

    private void RefreshActionButtons()
    {
        NodeStatus status = NodeStatus.Hidden;
        if (selectedNode != null && manager != null)
            status = manager.GetNodeStatus(selectedNode);

        if (unlockSelectedButton != null)
            unlockSelectedButton.interactable = selectedNode != null && status == NodeStatus.Unlockable;

        if (deactivateSelectedButton != null)
            deactivateSelectedButton.interactable = allowManualDeactivate && selectedNode != null && status == NodeStatus.Unlocked;

        if (clearSelectionButton != null)
            clearSelectionButton.interactable = selectedNode != null;
    }

    private void DrawConnections()
    {
        if (!drawConnections || axis == null || nodeRects.Count == 0) return;

        RectTransform parent = connectionsParent;
        if (parent == null)
        {
            if (rootPanel == null) return;
            parent = rootPanel.transform as RectTransform;
            if (parent == null) return;
        }

        List<SkillNodeSO> allNodes = axis.nodes != null ? new List<SkillNodeSO>(axis.nodes) : new List<SkillNodeSO>();
        if (allNodes.Count == 0)
        {
            if (axis.Tier1Nodes != null) allNodes.AddRange(axis.Tier1Nodes);
            if (axis.Tier2Nodes != null) allNodes.AddRange(axis.Tier2Nodes);
        }

        for (int i = 0; i < allNodes.Count; i++)
        {
            SkillNodeSO node = allNodes[i];
            if (node == null || node.prerequisites == null) continue;
            if (!nodeRects.TryGetValue(node, out RectTransform toRect) || toRect == null) continue;

            for (int p = 0; p < node.prerequisites.Length; p++)
            {
                SkillNodeSO prereq = node.prerequisites[p];
                if (prereq == null) continue;
                if (!nodeRects.TryGetValue(prereq, out RectTransform fromRect) || fromRect == null) continue;
                CreateConnectionLine(parent, fromRect, toRect);
            }
        }
    }

    private void CreateConnectionLine(RectTransform parent, RectTransform from, RectTransform to)
    {
        Vector2 fromPos = GetRectCenterOnParent(from, parent);
        Vector2 toPos = GetRectCenterOnParent(to, parent);

        Vector2 delta = toPos - fromPos;
        float length = delta.magnitude;
        if (length <= 0.01f) return;

        GameObject lineObj = new GameObject("SkillTreeConnection", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        lineObj.transform.SetParent(parent, false);

        Image lineImage = lineObj.GetComponent<Image>();
        lineImage.color = connectionColor;
        lineImage.raycastTarget = false;
        spawnedConnections.Add(lineImage);

        RectTransform lineRect = lineObj.transform as RectTransform;
        if (lineRect == null) return;

        lineRect.anchorMin = new Vector2(0.5f, 0.5f);
        lineRect.anchorMax = new Vector2(0.5f, 0.5f);
        lineRect.pivot = new Vector2(0.5f, 0.5f);
        lineRect.anchoredPosition = (fromPos + toPos) * 0.5f;
        lineRect.sizeDelta = new Vector2(length, connectionThickness);

        float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
        lineRect.localRotation = Quaternion.Euler(0f, 0f, angle);
        lineRect.SetAsFirstSibling();
    }

    private static Vector2 GetRectCenterOnParent(RectTransform rect, RectTransform parent)
    {
        Vector3 world = rect.TransformPoint(rect.rect.center);
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(null, world);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenPoint, null, out Vector2 localPoint);
        return localPoint;
    }

    private void ClearButtons()
    {
        for (int i = 0; i < spawnedButtons.Count; i++)
        {
            if (spawnedButtons[i] != null)
                Destroy(spawnedButtons[i].gameObject);
        }
        spawnedButtons.Clear();
    }

    private void ClearConnections()
    {
        for (int i = 0; i < spawnedConnections.Count; i++)
        {
            if (spawnedConnections[i] != null)
                Destroy(spawnedConnections[i].gameObject);
        }
        spawnedConnections.Clear();
    }

    private static string StatusText(NodeStatus status)
    {
        switch (status)
        {
            case NodeStatus.Unlocked: return "Acilmis";
            case NodeStatus.Unlockable: return "Acilabilir";
            case NodeStatus.VisibleLocked: return "Kilitli";
            default: return "Gizli";
        }
    }

    private static Color StatusColor(NodeStatus status)
    {
        switch (status)
        {
            case NodeStatus.Unlocked: return new Color(0.35f, 1f, 0.35f);
            case NodeStatus.Unlockable: return new Color(1f, 0.95f, 0.4f);
            case NodeStatus.VisibleLocked: return new Color(1f, 0.45f, 0.45f);
            default: return new Color(0.75f, 0.75f, 0.75f);
        }
    }
}

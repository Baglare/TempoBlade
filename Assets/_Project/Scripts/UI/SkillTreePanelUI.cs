using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

/// <summary>
/// Dash Skill Tree görsel paneli.
/// Tab tuşuyla açılır/kapanır. Runtime'da tüm UI hiyerarşisini programatik oluşturur.
/// Deneysel modda: Node'ları tıklayarak serbestçe aç/kapat.
/// </summary>
public class SkillTreePanelUI : MonoBehaviour
{
    [Header("Tuş Ayarı")]
    [Tooltip("Paneli açma/kapama tuşu")]
    public Key toggleKey = Key.Tab;

    [Header("Dash Axis Referansı")]
    [Tooltip("Dash ekseni SO — node listesi buradan okunur")]
    public ProgressionAxisSO dashAxis;

    [Header("Renk Şeması")]
    public Color lockedColor = new Color(0.30f, 0.30f, 0.35f, 1f);
    public Color unlockedColor = new Color(0.15f, 0.85f, 0.55f, 1f);
    public Color commitmentColor = new Color(1f, 0.65f, 0.15f, 1f);
    public Color t1Color = new Color(0.4f, 0.75f, 1f, 1f);
    public Color hunterColor = new Color(0.9f, 0.35f, 0.2f, 1f);
    public Color flowColor = new Color(0.3f, 0.55f, 1f, 1f);
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

    // ═══════════ Runtime ═══════════
    private Canvas _canvas;
    private GameObject _panelRoot;
    private bool _isOpen;
    private readonly Dictionary<string, NodeSlot> _slots = new Dictionary<string, NodeSlot>();
    private readonly List<LineConnection> _lines = new List<LineConnection>();
    private TextMeshProUGUI _titleText;
    private TextMeshProUGUI _descText;

    private class NodeSlot
    {
        public SkillNodeSO node;
        public RectTransform rect;
        public Image bgImage;
        public TextMeshProUGUI label;
        public Image borderImage;
    }

    private class LineConnection
    {
        public RectTransform rect;
        public Image image;
        public string fromId;
        public string toId;
    }

    // ═══════════ LAYOUT TANIMLAMALARI ═══════════
    // Her node'un grid pozisyonu (col, row) — ağaç yapısına uygun

    private static readonly Dictionary<string, Vector2Int> NodeGridPositions = new Dictionary<string, Vector2Int>
    {
        // T1 — Row 0
        { "dash_t1_ranged_dodge",   new Vector2Int(0, 0) },
        { "dash_t1_melee_dodge",    new Vector2Int(1, 0) },
        { "dash_t1_tempo_gain",     new Vector2Int(3, 0) },
        // T1 — Row 1
        { "dash_t1_counter",        new Vector2Int(0, 1) },   // ranged + melee → counter
        { "dash_t1_attack_speed",   new Vector2Int(3, 1) },   // tempo → speed
        // T2 Commitment — Row 2
        { "dash_t2_commitment",     new Vector2Int(2, 2) },
        // T2 Avcı — Row 3-5 (sol taraf)
        { "dash_t2h_hunt_mark",     new Vector2Int(0, 3) },
        { "dash_t2h_blind_spot",    new Vector2Int(0, 4) },
        { "dash_t2h_hunt_flow",     new Vector2Int(1, 4) },
        { "dash_t2h_execute",       new Vector2Int(0, 5) },
        { "dash_t2h_hunt_cycle",    new Vector2Int(0, 6) },
        // T2 Akışçı — Row 3-5 (sağ taraf)
        { "dash_t2f_flow_mark",     new Vector2Int(3, 3) },
        { "dash_t2f_snapback",      new Vector2Int(4, 3) },
        { "dash_t2f_chain_bounce",  new Vector2Int(3, 4) },
        { "dash_t2f_black_hole",    new Vector2Int(3, 5) },
        { "dash_t2f_burst",         new Vector2Int(4, 5) },
    };

    // ═══════════ LIFECYCLE ═══════════

    private void Start()
    {
        BuildUI();
        _panelRoot.SetActive(false);
        _isOpen = false;
    }

    private void OnEnable()
    {
        if (AxisProgressionManager.Instance != null)
            AxisProgressionManager.Instance.OnNodeStatusChanged += HandleNodeStatusChanged;
    }

    private void OnDisable()
    {
        if (AxisProgressionManager.Instance != null)
            AxisProgressionManager.Instance.OnNodeStatusChanged -= HandleNodeStatusChanged;
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current[toggleKey].wasPressedThisFrame)
        {
            TogglePanel();
        }

        // ESC ile kapat
        if (_isOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ClosePanel();
        }
    }

    public void TogglePanel()
    {
        if (_isOpen) ClosePanel();
        else OpenPanel();
    }

    private void OpenPanel()
    {
        _isOpen = true;
        _panelRoot.SetActive(true);
        RefreshAllSlots();
        Time.timeScale = 0f;
    }

    private void ClosePanel()
    {
        _isOpen = false;
        _panelRoot.SetActive(false);
        Time.timeScale = 1f;
        _descText.text = "";
    }

    // ═══════════ UI OLUŞTURMA ═══════════

    private void BuildUI()
    {
        // Canvas
        var canvasGO = new GameObject("SkillTreeCanvas");
        canvasGO.transform.SetParent(transform);
        _canvas = canvasGO.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 100;
        canvasGO.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGO.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();

        // Panel root (arka plan)
        _panelRoot = CreatePanel(canvasGO.transform, "SkillTreePanel", panelBg);
        var panelRect = _panelRoot.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = new Vector2(30, 20);
        panelRect.offsetMax = new Vector2(-30, -20);

        // Başlık
        _titleText = CreateText(_panelRoot.transform, "Title", "⚡ DASH SKILL TREE ⚡",
            28, FontStyle.Bold, new Color(0.9f, 0.9f, 0.95f));
        var titleRect = _titleText.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0, -8);
        titleRect.sizeDelta = new Vector2(600, 35);

        // Deneysel uyarı
        var warnText = CreateText(_panelRoot.transform, "Warning",
            "[DENEYSEL MOD] Tıklayarak perkleri serbestçe aç/kapat",
            14, FontStyle.Italic, new Color(1f, 0.8f, 0.3f, 0.7f));
        var warnRect = warnText.GetComponent<RectTransform>();
        warnRect.anchorMin = new Vector2(0.5f, 1f);
        warnRect.anchorMax = new Vector2(0.5f, 1f);
        warnRect.pivot = new Vector2(0.5f, 1f);
        warnRect.anchoredPosition = new Vector2(0, -38);
        warnRect.sizeDelta = new Vector2(500, 22);

        // Kapatma butonu
        var closeBtnGO = CreatePanel(_panelRoot.transform, "CloseBtn", new Color(0.8f, 0.2f, 0.2f, 0.9f));
        var closeBtnRect = closeBtnGO.GetComponent<RectTransform>();
        closeBtnRect.anchorMin = new Vector2(1f, 1f);
        closeBtnRect.anchorMax = new Vector2(1f, 1f);
        closeBtnRect.pivot = new Vector2(1f, 1f);
        closeBtnRect.anchoredPosition = new Vector2(-10, -10);
        closeBtnRect.sizeDelta = new Vector2(40, 40);
        var closeBtn = closeBtnGO.AddComponent<Button>();
        closeBtn.onClick.AddListener(ClosePanel);
        var closeLabel = CreateText(closeBtnGO.transform, "X", "✕", 22, FontStyle.Bold, Color.white);
        closeLabel.alignment = TextAlignmentOptions.Center;
        closeLabel.GetComponent<RectTransform>().anchorMin = Vector2.zero;
        closeLabel.GetComponent<RectTransform>().anchorMax = Vector2.one;
        closeLabel.GetComponent<RectTransform>().offsetMin = Vector2.zero;
        closeLabel.GetComponent<RectTransform>().offsetMax = Vector2.zero;

        // Node'lar için container (ortalanmış)
        var containerGO = new GameObject("NodeContainer");
        containerGO.transform.SetParent(_panelRoot.transform, false);
        var containerRect = containerGO.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.5f, 0.5f);
        containerRect.anchorMax = new Vector2(0.5f, 0.5f);
        containerRect.pivot = new Vector2(0.5f, 0.5f);
        containerRect.anchoredPosition = new Vector2(0, -10);
        containerRect.sizeDelta = new Vector2(1000, 580);

        // Çizgiler için container (node'ların altında)
        var linesGO = new GameObject("LineContainer");
        linesGO.transform.SetParent(containerRect, false);
        var linesRect = linesGO.AddComponent<RectTransform>();
        linesRect.anchorMin = Vector2.zero;
        linesRect.anchorMax = Vector2.one;
        linesRect.offsetMin = Vector2.zero;
        linesRect.offsetMax = Vector2.zero;

        // Açıklama alanı (alt kısım)
        _descText = CreateText(_panelRoot.transform, "Description", "",
            15, FontStyle.Normal, new Color(0.8f, 0.8f, 0.85f));
        var descRect = _descText.GetComponent<RectTransform>();
        descRect.anchorMin = new Vector2(0f, 0f);
        descRect.anchorMax = new Vector2(1f, 0f);
        descRect.pivot = new Vector2(0.5f, 0f);
        descRect.anchoredPosition = new Vector2(0, 5);
        descRect.sizeDelta = new Vector2(-80, 40);
        _descText.alignment = TextAlignmentOptions.Center;
        _descText.fontSize = 13;

        // Tier label'ları
        CreateTierLabels(containerRect);

        // Node'ları oluştur
        if (dashAxis != null && dashAxis.nodes != null)
        {
            foreach (var node in dashAxis.nodes)
            {
                if (node == null) continue;
                BuildNodeSlot(node, containerRect, linesRect);
            }
        }

        // Çizgileri oluştur (prerequisite bağlantıları)
        if (dashAxis != null && dashAxis.nodes != null)
        {
            foreach (var node in dashAxis.nodes)
            {
                if (node == null || node.prerequisites == null) continue;
                foreach (var prereq in node.prerequisites)
                {
                    if (prereq == null) continue;
                    CreateLine(linesRect, prereq.nodeId, node.nodeId);
                }
            }
        }
    }

    private void CreateTierLabels(RectTransform container)
    {
        string[] labels = { "TIER 1", "TIER 1", "COMMITMENT", "TIER 2", "TIER 2", "TIER 2", "TIER 2" };
        Color[] colors = { t1Color, t1Color, commitmentColor, hunterColor, hunterColor, hunterColor, hunterColor };

        for (int row = 0; row < labels.Length; row++)
        {
            Vector2 pos = GridToLocal(new Vector2Int(-1, row), container);
            var txt = CreateText(container, $"TierLabel_{row}", labels[row],
                12, FontStyle.Bold, colors[row] * 0.6f);
            var r = txt.GetComponent<RectTransform>();
            r.anchorMin = new Vector2(0.5f, 0.5f);
            r.anchorMax = new Vector2(0.5f, 0.5f);
            r.pivot = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = pos;
            r.sizeDelta = new Vector2(100, 30);
            txt.alignment = TextAlignmentOptions.Right;
        }
    }

    private void BuildNodeSlot(SkillNodeSO node, RectTransform container, RectTransform lineContainer)
    {
        if (!NodeGridPositions.TryGetValue(node.nodeId, out Vector2Int gridPos))
        {
            Debug.LogWarning($"[SkillTreePanel] Grid pozisyonu tanımlı değil: {node.nodeId}");
            return;
        }

        Vector2 localPos = GridToLocal(gridPos, container);

        // Arka plan + border
        var slotGO = new GameObject($"Node_{node.nodeId}");
        slotGO.transform.SetParent(container, false);
        var slotRect = slotGO.AddComponent<RectTransform>();
        slotRect.anchorMin = new Vector2(0.5f, 0.5f);
        slotRect.anchorMax = new Vector2(0.5f, 0.5f);
        slotRect.pivot = new Vector2(0.5f, 0.5f);
        slotRect.anchoredPosition = localPos;
        slotRect.sizeDelta = new Vector2(nodeWidth, nodeHeight);

        // Border (outer glow)
        var borderImg = slotGO.AddComponent<Image>();
        borderImg.color = GetNodeTintColor(node);

        // Inner fill
        var innerGO = CreatePanel(slotGO.transform, "Inner", nodeBg);
        var innerRect = innerGO.GetComponent<RectTransform>();
        innerRect.anchorMin = Vector2.zero;
        innerRect.anchorMax = Vector2.one;
        innerRect.offsetMin = new Vector2(2, 2);
        innerRect.offsetMax = new Vector2(-2, -2);
        var innerImg = innerGO.GetComponent<Image>();

        // İsim
        var nameText = CreateText(innerGO.transform, "Name", node.displayName,
            12, FontStyle.Bold, Color.white);
        nameText.alignment = TextAlignmentOptions.Center;
        var nameRect = nameText.GetComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0, 0.25f);
        nameRect.anchorMax = new Vector2(1, 1);
        nameRect.offsetMin = new Vector2(5, 0);
        nameRect.offsetMax = new Vector2(-5, -4);

        // Durum ikonu
        var statusText = CreateText(innerGO.transform, "Status", "🔒",
            11, FontStyle.Normal, Color.gray);
        statusText.alignment = TextAlignmentOptions.Center;
        var statusRect = statusText.GetComponent<RectTransform>();
        statusRect.anchorMin = new Vector2(0, 0);
        statusRect.anchorMax = new Vector2(1, 0.3f);
        statusRect.offsetMin = new Vector2(5, 2);
        statusRect.offsetMax = new Vector2(-5, 0);

        // Button
        var btn = slotGO.AddComponent<Button>();
        btn.targetGraphic = borderImg;

        var slot = new NodeSlot
        {
            node = node,
            rect = slotRect,
            bgImage = innerImg,
            label = statusText,
            borderImage = borderImg
        };
        _slots[node.nodeId] = slot;

        // Tıklama olayı
        btn.onClick.AddListener(() => OnNodeClicked(node));

        // Hover olayları (açıklama gösterme)
        var trigger = slotGO.AddComponent<UnityEngine.EventSystems.EventTrigger>();

        var enterEntry = new UnityEngine.EventSystems.EventTrigger.Entry();
        enterEntry.eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter;
        enterEntry.callback.AddListener((_) => OnNodeHover(node));
        trigger.triggers.Add(enterEntry);

        var exitEntry = new UnityEngine.EventSystems.EventTrigger.Entry();
        exitEntry.eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit;
        exitEntry.callback.AddListener((_) => _descText.text = "");
        trigger.triggers.Add(exitEntry);
    }

    private void CreateLine(RectTransform container, string fromId, string toId)
    {
        if (!NodeGridPositions.ContainsKey(fromId) || !NodeGridPositions.ContainsKey(toId)) return;

        var lineGO = new GameObject($"Line_{fromId}_{toId}");
        lineGO.transform.SetParent(container, false);
        var lineRect = lineGO.AddComponent<RectTransform>();
        var lineImg = lineGO.AddComponent<Image>();
        lineImg.color = lineColor;

        _lines.Add(new LineConnection { rect = lineRect, image = lineImg, fromId = fromId, toId = toId });

        // Çizgi pozisyonunu hesapla
        UpdateLinePosition(lineRect, fromId, toId, container);
    }

    private void UpdateLinePosition(RectTransform lineRect, string fromId, string toId, RectTransform container)
    {
        Vector2 fromPos = GridToLocal(NodeGridPositions[fromId], container);
        Vector2 toPos = GridToLocal(NodeGridPositions[toId], container);

        Vector2 diff = toPos - fromPos;
        float length = diff.magnitude;
        float angle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;

        lineRect.anchorMin = new Vector2(0.5f, 0.5f);
        lineRect.anchorMax = new Vector2(0.5f, 0.5f);
        lineRect.pivot = new Vector2(0f, 0.5f);
        lineRect.anchoredPosition = fromPos;
        lineRect.sizeDelta = new Vector2(length, lineWidth);
        lineRect.localRotation = Quaternion.Euler(0, 0, angle);
    }

    // ═══════════ GRID → LOKAL POZİSYON ═══════════

    private Vector2 GridToLocal(Vector2Int grid, RectTransform container)
    {
        float totalCols = 5;
        float totalRows = 7;

        float cellW = nodeWidth + horizontalSpacing;
        float cellH = nodeHeight + verticalSpacing;

        float totalW = totalCols * cellW;
        float totalH = totalRows * cellH;

        float x = grid.x * cellW - totalW * 0.5f + cellW * 0.5f;
        float y = -grid.y * cellH + totalH * 0.5f - cellH * 0.5f;

        return new Vector2(x, y);
    }

    // ═══════════ NODE RENK KATMANI ═══════════

    private Color GetNodeTintColor(SkillNodeSO node)
    {
        if (node.isCommitmentNode) return commitmentColor;

        string id = node.nodeId;
        if (id.Contains("t1_")) return t1Color;
        if (id.Contains("t2h_")) return hunterColor;
        if (id.Contains("t2f_")) return flowColor;

        return t1Color;
    }

    // ═══════════ SLOT GÜNCELLEME ═══════════

    private void RefreshAllSlots()
    {
        foreach (var kv in _slots)
            RefreshSlot(kv.Value);

        RefreshLines();
    }

    private void RefreshSlot(NodeSlot slot)
    {
        if (slot == null || slot.node == null) return;

        var mgr = AxisProgressionManager.Instance;
        bool unlocked = mgr != null && mgr.IsNodeUnlocked(slot.node);
        bool prereqsMet = mgr != null && CheckPrereqs(slot.node);

        Color tint = GetNodeTintColor(slot.node);

        if (unlocked)
        {
            // AKTİF — parlak kenarlık + koyu iç
            slot.bgImage.color = new Color(tint.r * 0.3f, tint.g * 0.3f, tint.b * 0.3f, 1f);
            slot.borderImage.color = tint;
            slot.label.text = "✅ AKTİF";
            slot.label.color = unlockedColor;
        }
        else if (prereqsMet)
        {
            // AÇILABİLİR — sarımsı kenarlık
            slot.bgImage.color = nodeBg;
            slot.borderImage.color = new Color(tint.r * 0.7f, tint.g * 0.7f, tint.b * 0.5f, 1f);
            slot.label.text = "🔓 AÇILABİLİR";
            slot.label.color = new Color(1f, 0.85f, 0.3f);
        }
        else
        {
            // KİLİTLİ — soluk kenarlık
            slot.bgImage.color = new Color(0.10f, 0.10f, 0.13f, 1f);
            slot.borderImage.color = new Color(tint.r * 0.2f, tint.g * 0.2f, tint.b * 0.2f, 1f);
            slot.label.text = "🔒 KİLİTLİ";
            slot.label.color = new Color(0.4f, 0.4f, 0.45f);
        }
    }

    private bool CheckPrereqs(SkillNodeSO node)
    {
        if (node.prerequisites == null || node.prerequisites.Length == 0) return true;
        var mgr = AxisProgressionManager.Instance;
        if (mgr == null) return false;
        foreach (var prereq in node.prerequisites)
        {
            if (prereq != null && !mgr.IsNodeUnlocked(prereq))
                return false;
        }
        return true;
    }

    private void RefreshLines()
    {
        foreach (var line in _lines)
        {
            bool fromUnlocked = AxisProgressionManager.Instance != null &&
                _slots.ContainsKey(line.fromId) &&
                AxisProgressionManager.Instance.IsNodeUnlocked(_slots[line.fromId].node);
            bool toUnlocked = AxisProgressionManager.Instance != null &&
                _slots.ContainsKey(line.toId) &&
                AxisProgressionManager.Instance.IsNodeUnlocked(_slots[line.toId].node);

            line.image.color = (fromUnlocked && toUnlocked) ? lineUnlockedColor : lineColor;
        }
    }

    // ═══════════ EVENT HANDLER'LAR ═══════════

    private void OnNodeClicked(SkillNodeSO node)
    {
        if (AxisProgressionManager.Instance == null) return;

        int result = AxisProgressionManager.Instance.SmartToggleNode(node);

        if (result == 1)
        {
            _descText.text = $"<color=#26D98A>✅ {node.displayName} açıldı!</color>";
        }
        else if (result == 0)
        {
            _descText.text = $"<color=#FF8844>❌ {node.displayName} kapatıldı</color>";
        }
        else
        {
            // Prerequisite eksik — hangileri gerekiyor göster
            string missing = "";
            if (node.prerequisites != null)
            {
                foreach (var p in node.prerequisites)
                {
                    if (p != null && !AxisProgressionManager.Instance.IsNodeUnlocked(p))
                        missing += p.displayName + ", ";
                }
            }
            if (missing.Length > 2) missing = missing.Substring(0, missing.Length - 2);
            _descText.text = $"<color=#FF4444>⛔ Önce açılması gereken perkler: {missing}</color>";
        }

        RefreshAllSlots();
    }

    private void OnNodeHover(SkillNodeSO node)
    {
        if (node == null) return;

        var mgr = AxisProgressionManager.Instance;
        bool unlocked = mgr != null && mgr.IsNodeUnlocked(node);
        bool prereqsMet = CheckPrereqs(node);

        string status;
        if (unlocked) status = "<color=#26D98A>AKTİF</color>";
        else if (prereqsMet) status = "<color=#FFD94A>AÇILABİLİR</color>";
        else status = "<color=#666>KİLİTLİ</color>";

        string prereqInfo = "";
        if (!unlocked && node.prerequisites != null && node.prerequisites.Length > 0)
        {
            prereqInfo = "\nGerekli: ";
            foreach (var p in node.prerequisites)
            {
                if (p == null) continue;
                bool pUnlocked = mgr != null && mgr.IsNodeUnlocked(p);
                prereqInfo += pUnlocked ? $"<color=#26D98A>{p.displayName}</color>" : $"<color=#FF4444>{p.displayName}</color>";
                prereqInfo += "  ";
            }
        }

        _descText.text = $"<b>{node.displayName}</b>  [{status}]\n{node.description}{prereqInfo}";
    }

    private void HandleNodeStatusChanged(string nodeId, NodeStatus status)
    {
        if (!_isOpen) return;
        if (_slots.TryGetValue(nodeId, out var slot))
            RefreshSlot(slot);
        RefreshLines();
    }

    // ═══════════ YARDIMCI METOTLAR ═══════════

    private GameObject CreatePanel(Transform parent, string name, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        go.AddComponent<RectTransform>();
        return go;
    }

    private TextMeshProUGUI CreateText(Transform parent, string name, string content,
        float fontSize, FontStyle style, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = content;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style == FontStyle.Bold ? TMPro.FontStyles.Bold :
                        style == FontStyle.Italic ? TMPro.FontStyles.Italic : TMPro.FontStyles.Normal;
        tmp.color = color;
        tmp.enableAutoSizing = false;
        tmp.overflowMode = TextOverflowModes.Truncate;
        tmp.richText = true;
        return tmp;
    }
}

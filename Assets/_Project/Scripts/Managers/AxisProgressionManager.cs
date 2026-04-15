using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Eksen Bazlı Progression sisteminin beyni.
/// Kural yorumlama, kilit kontrolü, node açma/kapama, form kaydırma ve build derleme.
/// Singleton olarak çalışır, DontDestroyOnLoad.
/// </summary>
public class AxisProgressionManager : MonoBehaviour
{
    public static AxisProgressionManager Instance { get; private set; }

    private static readonly ExclusiveRouteRule[] ExclusiveRouteRules =
    {
        new ExclusiveRouteRule("dash_t2h_", "dash_t2f_", "Akışçı yolunda açık perk var. Önce onu kapat."),
        new ExclusiveRouteRule("dash_t2f_", "dash_t2h_", "Avcı yolunda açık perk var. Önce onu kapat."),
        new ExclusiveRouteRule("parry_t2b_", "parry_t2p_", "Mükemmeliyetçi yolunda açık perk var. Önce onu kapat."),
        new ExclusiveRouteRule("parry_t2p_", "parry_t2b_", "Balistik yolunda açık perk var. Önce onu kapat."),
        new ExclusiveRouteRule("overdrive_t2burst_", "overdrive_t2pred_", "Predator Overdrive yolunda açık perk var. Önce onu kapat."),
        new ExclusiveRouteRule("overdrive_t2pred_", "overdrive_t2burst_", "Burst Overdrive yolunda açık perk var. Önce onu kapat."),
        new ExclusiveRouteRule("cadence_t2measured_", "cadence_t2flow_", "Flow Cadence yolunda açık perk var. Önce onu kapat."),
        new ExclusiveRouteRule("cadence_t2flow_", "cadence_t2measured_", "Measured Cadence yolunda açık perk var. Önce onu kapat.")
    };

    [Header("Veritabanı")]
    [Tooltip("Tüm eksenleri, karşıt çiftleri ve form overlay'leri barındıran database asset.")]
    public AxisDatabaseSO database;

    [Header("Tree XP / Rank")]
    [Tooltip("Tree XP, affinity agirliklari, rank esikleri ve anti-cheese ayarlari.")]
    public TreeProgressionConfigSO progressionConfig;

    // ═══════════ Runtime State ═══════════
    private readonly HashSet<string> _unlockedNodeIds = new HashSet<string>();
    private readonly HashSet<string> _normalUnlockedNodeIds = new HashSet<string>();
    private readonly HashSet<string> _testerUnlockedNodeIds = new HashSet<string>();
    private readonly Dictionary<string, int> _formAffinities = new Dictionary<string, int>();
    private readonly Dictionary<string, CommitmentState> _axisCommitments = new Dictionary<string, CommitmentState>();
    private readonly Dictionary<string, TreeProgressionRuntime> _treeProgressions = new Dictionary<string, TreeProgressionRuntime>();
    private SkillTreeInteractionMode _interactionMode = SkillTreeInteractionMode.NormalProgression;
    private TreeProgressionConfigSO _runtimeDefaultConfig;

    // ═══════════ Derlenmiş Build ═══════════
    private PlayerBuild _currentBuild = new PlayerBuild();
    public PlayerBuild CurrentBuild => _currentBuild;
    public SkillTreeInteractionMode InteractionMode => _interactionMode;
    public bool IsTesterMode => _interactionMode == SkillTreeInteractionMode.Tester;

    // ═══════════ Events ═══════════
    public event Action<SkillNodeSO> OnNodeUnlocked;
    public event Action<SkillNodeSO> OnNodeBecameLocked;
    public event Action<FormOverlaySO, int, int> OnFormAffinityChanged;
    public event Action<PlayerBuild> OnBuildChanged;
    public event Action<string, NodeStatus> OnNodeStatusChanged;
    public event Action<ProgressionAxisSO, int, float> OnTreeProgressChanged;
    public event Action<SkillTreeInteractionMode> OnInteractionModeChanged;

    // ═══════════ Lifecycle ═══════════

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (progressionConfig == null)
        {
            _runtimeDefaultConfig = ScriptableObject.CreateInstance<TreeProgressionConfigSO>();
            progressionConfig = _runtimeDefaultConfig;
        }
    }

    private void Start()
    {
        // SaveManager varsa otomatik olarak kayıtlı skill state'i yükle
        if (SaveManager.Instance != null)
        {
            LoadFromSave(SaveManager.Instance.data);
            Debug.Log($"[AxisProgression] Save'den yüklendi. Açık node sayısı: {_unlockedNodeIds.Count}");
        }
    }

    /// <summary>
    /// Save verisinden state'i yükler. Oyun başladığında veya save yüklendiğinde çağrılmalıdır.
    /// </summary>
    public void LoadFromSave(SaveData data)
    {
        _unlockedNodeIds.Clear();
        _normalUnlockedNodeIds.Clear();
        _testerUnlockedNodeIds.Clear();
        _formAffinities.Clear();
        _axisCommitments.Clear();
        _treeProgressions.Clear();

        if (data == null) return;

        // Node'lar
        if (data.unlockedSkillNodeIds != null)
        {
            foreach (var id in data.unlockedSkillNodeIds)
                _normalUnlockedNodeIds.Add(id);
        }

        foreach (var id in SkillTreeTesterSaveStore.Load())
            _testerUnlockedNodeIds.Add(id);

        // Form affinities
        if (data.formAffinities != null)
        {
            foreach (var entry in data.formAffinities)
                _formAffinities[entry.formId] = entry.affinity;
        }

        // Axis commitments
        if (data.axisCommitments != null)
        {
            foreach (var entry in data.axisCommitments)
            {
                _axisCommitments[entry.axisId] = new CommitmentState
                {
                    isCommitted = entry.isCommitted,
                    commitmentNodeId = entry.commitmentNodeId,
                    chosenRoute = entry.chosenRoute,
                    highestUnlockedTier = entry.highestUnlockedTier
                };
            }
        }

        if (data.treeProgressions != null)
        {
            foreach (var entry in data.treeProgressions)
            {
                if (entry == null || string.IsNullOrEmpty(entry.axisId))
                    continue;

                _treeProgressions[entry.axisId] = new TreeProgressionRuntime
                {
                    xp = entry.xp,
                    rank = GetProgressionConfig().CalculateRank(entry.xp),
                    chosenTier2Route = entry.chosenTier2Route ?? ""
                };
            }
        }

        // startsUnlocked node'ları otomatik ekle
        if (database != null && database.allAxes != null)
        {
            foreach (var axis in database.allAxes)
            {
                if (axis == null || axis.nodes == null) continue;
                foreach (var node in axis.nodes)
                {
                    if (node != null && node.startsUnlocked)
                        _normalUnlockedNodeIds.Add(node.nodeId);
                }
            }
        }

        _interactionMode = SkillTreeInteractionMode.NormalProgression;
        LoadActiveSetFromBacking();
        RecalculateAllAxisStates();
        RebuildPlayerBuild();
    }

    /// <summary>
    /// Mevcut state'i SaveData'ya yazar.
    /// </summary>
    public void SaveToData(SaveData data)
    {
        if (data == null) return;

        SyncActiveSetToBacking();

        // Node'lar
        data.unlockedSkillNodeIds = new List<string>(_normalUnlockedNodeIds);

        // Form affinities
        data.formAffinities = new List<FormAffinityEntry>();
        foreach (var kv in _formAffinities)
        {
            data.formAffinities.Add(new FormAffinityEntry
            {
                formId = kv.Key,
                affinity = kv.Value
            });
        }

        // Axis commitments
        data.axisCommitments = new List<AxisCommitmentEntry>();
        foreach (var kv in _axisCommitments)
        {
            data.axisCommitments.Add(new AxisCommitmentEntry
            {
                axisId = kv.Key,
                isCommitted = kv.Value.isCommitted,
                commitmentNodeId = kv.Value.commitmentNodeId,
                chosenRoute = kv.Value.chosenRoute,
                highestUnlockedTier = kv.Value.highestUnlockedTier
            });
        }

        data.treeProgressions = new List<TreeProgressionEntry>();
        foreach (var kv in _treeProgressions)
        {
            data.treeProgressions.Add(new TreeProgressionEntry
            {
                axisId = kv.Key,
                xp = kv.Value.xp,
                rank = kv.Value.rank,
                chosenTier2Route = kv.Value.chosenTier2Route
            });
        }
    }

    // ═══════════ Node Status Kontrolü ═══════════

    /// <summary>
    /// Bir node'un mevcut durumunu döndürür.
    /// </summary>
    public NodeStatus GetNodeStatus(SkillNodeSO node)
    {
        if (node == null) return NodeStatus.Hidden;

        // 1. Zaten açık mı?
        if (_unlockedNodeIds.Contains(node.nodeId))
            return NodeStatus.Unlocked;

        // 2. Visibility koşulu karşılanmıyor mu?
        if (!IsNodeVisible(node))
            return NodeStatus.Hidden;

        // 3. Karşıt commitment kilidi var mı?
        if (IsBlockedByCommitment(node))
            return NodeStatus.VisibleLocked;

        // 4. Prerequisite'ler açık değil mi?
        if (!ArePrerequisitesMet(node))
            return NodeStatus.VisibleLocked;

        // 5. T3 form overlay bölge gating?
        if (IsTier2BlockedByMissingCommitment(node))
            return NodeStatus.VisibleLocked;

        if (node.tier >= 3 && !IsFormGatePassed(node))
            return NodeStatus.VisibleLocked;

        // 6. Ayni tier dalinda karsi yol kilidi var mi?
        if (IsPathBlocked(node))
            return NodeStatus.VisibleLocked;

        if (IsRankLocked(node))
            return NodeStatus.VisibleLocked;

        // 7. Hepsi tamam
        return NodeStatus.Unlockable;
    }

    // ═══════════ Node Açma ═══════════

    /// <summary>
    /// Bir node'u açmayı dener. Başarılıysa true döner.
    /// </summary>
    public bool TryUnlockNode(SkillNodeSO node)
    {
        if (node == null) return false;
        if (GetNodeStatus(node) != NodeStatus.Unlockable) return false;

        // State'e ekle
        _unlockedNodeIds.Add(node.nodeId);

        // Commitment kontrolü
        if (node.isCommitmentNode)
        {
            HandleCommitment(node);
        }

        // Axis'in en yüksek tier'ını güncelle
        UpdateAxisHighestTier(node);
        HandleTier2RouteChoice(node);

        // Build yeniden derle
        RebuildPlayerBuild();

        // Events
        OnNodeUnlocked?.Invoke(node);
        OnNodeStatusChanged?.Invoke(node.nodeId, NodeStatus.Unlocked);

        // Karşıt eksende kilitlenen node'lar için status changed eventi
        NotifyAffectedNodes(node);

        SyncActiveSetToBacking();
        AutoSave();
        Debug.Log($"[AxisProgression] Node acildi: {node.displayName} (ID: {node.nodeId})");
        return true;
    }

    // ═══════════ Form Affinity ═══════════

    /// <summary>
    /// Bir form overlay'ın affinity'sini kaydırır.
    /// </summary>
    public void ShiftFormAffinity(FormOverlaySO form, int delta)
    {
        if (form == null || delta == 0) return;

        int oldVal = GetFormAffinity(form);
        int newVal = Mathf.Clamp(oldVal + delta, -form.maxAffinity, form.maxAffinity);

        if (oldVal == newVal) return;

        _formAffinities[form.formId] = newVal;

        // Build yeniden derle (T3 bölge durumları değişmiş olabilir)
        RebuildPlayerBuild();

        OnFormAffinityChanged?.Invoke(form, oldVal, newVal);
        Debug.Log($"[AxisProgression] Form '{form.formId}' affinity: {oldVal} → {newVal}");
    }

    /// <summary>Bir form overlay'ın mevcut affinity değerini döndürür.</summary>
    public int GetFormAffinity(FormOverlaySO form)
    {
        if (form == null) return 0;
        return _formAffinities.TryGetValue(form.formId, out int val) ? val : 0;
    }

    // ═══════════ Build Derleme ═══════════

    /// <summary>
    /// Tüm açık node'ların effect'lerini toplayarak PlayerBuild'i yeniden derler.
    /// </summary>
    public void RebuildPlayerBuild()
    {
        _currentBuild.Clear();

        if (database == null || database.allAxes == null) return;

        foreach (var axis in database.allAxes)
        {
            if (axis == null || axis.nodes == null) continue;
            foreach (var node in axis.nodes)
            {
                if (node != null && _unlockedNodeIds.Contains(node.nodeId) && ShouldApplyNodeToBuild(axis, node))
                {
                    _currentBuild.ApplyNodeEffects(node);
                }
            }
        }

        OnBuildChanged?.Invoke(_currentBuild);
    }

    // ═══════════ Dahili Kural Kontrolleri ═══════════

    private bool IsNodeVisible(SkillNodeSO node)
    {
        switch (node.visibility.rule)
        {
            case VisibilityRule.Always:
                return true;

            case VisibilityRule.RequireFormThreshold:
                var overlay = database?.GetFormOverlayById(node.visibility.formOverlayId);
                if (overlay == null) return true; // overlay tanımlı değilse serbest
                int affinity = GetFormAffinity(overlay);
                switch (node.visibility.requiredDirection)
                {
                    case FormDirection.Positive:
                        return affinity >= node.visibility.formThreshold;
                    case FormDirection.Negative:
                        return affinity <= -node.visibility.formThreshold;
                    case FormDirection.Either:
                        return Mathf.Abs(affinity) >= node.visibility.formThreshold;
                }
                return true;

            case VisibilityRule.RequireAnyUnlockedInRegion:
                return _currentBuild.GetRegionProgress(node.visibility.regionTag) > 0;

            case VisibilityRule.Custom:
                return true; // İleride genişletilebilir

            default:
                return true;
        }
    }

    private bool IsBlockedByCommitment(SkillNodeSO node)
    {
        if (database == null) return false;

        // Bu node hangi eksene ait?
        var ownerAxis = database.GetAxisForNode(node);
        if (ownerAxis == null) return false;

        // Bu eksenin karşıtı var mı?
        var pair = database.GetPairForAxis(ownerAxis);
        if (pair == null) return false;

        var oppositeAxis = pair.GetOpposite(ownerAxis);
        if (oppositeAxis == null) return false;

        // Karşıt eksende commitment yapılmış mı?
        if (_axisCommitments.TryGetValue(oppositeAxis.axisId, out var oppCommitment) && oppCommitment.isCommitted)
        {
            // Karşıt eksende commitment varsa, bu node'un tier'ı commitment tier'ına eşit veya yüksekse kilitli
            // Commitment tier'ı: karşıt taraftaki commitment node'unun tier'ı
            int oppCommitTier = GetCommitmentTier(oppositeAxis, oppCommitment.commitmentNodeId);
            if (node.tier >= oppCommitTier)
            {
                // Ama bu node'un own axis'inde de commitment varsa önceden kazanılmış demektir
                // (zaten Unlocked olarak dönecek, buraya gelmez)
                return true;
            }
        }

        return false;
    }

    private int GetCommitmentTier(ProgressionAxisSO axis, string commitmentNodeId)
    {
        if (axis == null || axis.nodes == null || string.IsNullOrEmpty(commitmentNodeId)) return 99;
        foreach (var node in axis.nodes)
        {
            if (node != null && node.nodeId == commitmentNodeId)
                return node.tier;
        }
        return 99; // Bulunamazsa çok yüksek tier (her şeyi kilitler)
    }

    private bool ArePrerequisitesMet(SkillNodeSO node)
    {
        if (node.prerequisites == null || node.prerequisites.Length == 0)
            return true;

        foreach (var prereq in node.prerequisites)
        {
            if (prereq != null && !_unlockedNodeIds.Contains(prereq.nodeId))
                return false;
        }
        return true;
    }

    private bool IsFormGatePassed(SkillNodeSO node)
    {
        if (string.IsNullOrEmpty(node.regionTag))
            return true; // regionTag yoksa serbest

        if (database == null || database.formOverlays == null)
            return true;

        // Tüm form overlay'lardan bu regionTag için gate kontrolü yap
        foreach (var overlay in database.formOverlays)
        {
            if (overlay == null) continue;
            int affinity = GetFormAffinity(overlay);
            if (!overlay.IsRegionAccessible(node.regionTag, affinity))
                return false;
        }

        return true;
    }

    private void HandleCommitment(SkillNodeSO node)
    {
        var axis = database?.GetAxisForNode(node);
        if (axis == null) return;

        _axisCommitments[axis.axisId] = new CommitmentState
        {
            isCommitted = true,
            commitmentNodeId = node.nodeId,
            chosenRoute = node.regionTag ?? "",
            highestUnlockedTier = node.tier
        };

        Debug.Log($"[AxisProgression] Commitment yapıldı: Eksen '{axis.displayName}', Node '{node.displayName}'");
    }

    private void UpdateAxisHighestTier(SkillNodeSO node)
    {
        var axis = database?.GetAxisForNode(node);
        if (axis == null) return;

        if (_axisCommitments.TryGetValue(axis.axisId, out var state))
        {
            if (node.tier > state.highestUnlockedTier)
            {
                state.highestUnlockedTier = node.tier;
                _axisCommitments[axis.axisId] = state;
            }
        }
        else
        {
            _axisCommitments[axis.axisId] = new CommitmentState
            {
                isCommitted = false,
                commitmentNodeId = "",
                chosenRoute = "",
                highestUnlockedTier = node.tier
            };
        }
    }

    private void NotifyAffectedNodes(SkillNodeSO unlockedNode)
    {
        if (database == null || !unlockedNode.isCommitmentNode) return;

        var axis = database.GetAxisForNode(unlockedNode);
        if (axis == null) return;

        var pair = database.GetPairForAxis(axis);
        if (pair == null) return;

        var opposite = pair.GetOpposite(axis);
        if (opposite == null || opposite.nodes == null) return;

        foreach (var node in opposite.nodes)
        {
            if (node == null) continue;
            var status = GetNodeStatus(node);
            if (status == NodeStatus.VisibleLocked)
            {
                OnNodeBecameLocked?.Invoke(node);
                OnNodeStatusChanged?.Invoke(node.nodeId, NodeStatus.VisibleLocked);
            }
        }
    }

    private void RecalculateAxisState(ProgressionAxisSO axis)
    {
        if (axis == null || string.IsNullOrEmpty(axis.axisId))
            return;

        int highestUnlockedTier = 0;
        SkillNodeSO activeCommitment = null;

        if (axis.nodes != null)
        {
            foreach (var axisNode in axis.nodes)
            {
                if (axisNode == null || !_unlockedNodeIds.Contains(axisNode.nodeId))
                    continue;

                highestUnlockedTier = Mathf.Max(highestUnlockedTier, axisNode.tier);
                if (axisNode.isCommitmentNode)
                    activeCommitment = axisNode;
            }
        }

        if (highestUnlockedTier <= 0 && activeCommitment == null)
        {
            _axisCommitments.Remove(axis.axisId);
            return;
        }

        _axisCommitments[axis.axisId] = new CommitmentState
        {
            isCommitted = activeCommitment != null,
            commitmentNodeId = activeCommitment != null ? activeCommitment.nodeId : "",
            chosenRoute = activeCommitment != null ? (activeCommitment.regionTag ?? "") : "",
            highestUnlockedTier = highestUnlockedTier
        };
    }

    private void NotifyAxisStatuses(ProgressionAxisSO axis, string skipNodeId = null)
    {
        if (axis == null || axis.nodes == null)
            return;

        foreach (var axisNode in axis.nodes)
        {
            if (axisNode == null || axisNode.nodeId == skipNodeId)
                continue;

            OnNodeStatusChanged?.Invoke(axisNode.nodeId, GetNodeStatus(axisNode));
        }
    }

    private void NotifyAxisPairStatuses(ProgressionAxisSO axis, string skipNodeId = null)
    {
        if (axis == null || database == null)
            return;

        NotifyAxisStatuses(axis, skipNodeId);
        NotifyAxisStatuses(database.GetOpposingAxis(axis));
    }

    // ═══════════ Deneysel / Debug Toggle ═══════════

    /// <summary>
    /// [DENEYSEL] Bir node'u açar veya kapatır.
    /// Açarken prerequisite'leri ve yol kısıtlamalarını kontrol eder.
    /// Kapatırken bağımlı node'ları da kapatır.
    /// Döndürülen değer: 1 = açıldı, 0 = kapatıldı, -1 = prerequisite eksik, -2 = yol kısıtlaması.
    /// </summary>
    public int SmartToggleNode(SkillNodeSO node)
    {
        if (node == null) return -1;

        if (_unlockedNodeIds.Contains(node.nodeId))
        {
            // Kapatırken: bu node'a bağımlı açık node'ları da kapat (cascade)
            CascadeLock(node);
            return 0; // kapatıldı
        }
        else
        {
            if (IsBlockedByCommitment(node))
            {
                Debug.Log($"[AxisProgression] Commitment kilidi: {node.displayName}");
                return -2;
            }

            // Açarken: prerequisite kontrolü
            if (!ArePrerequisitesMet(node))
            {
                Debug.Log($"[AxisProgression] Prerequisite eksik: {node.displayName}");
                return -1; // açılamaz
            }

            if (IsTier2BlockedByMissingCommitment(node))
            {
                Debug.Log($"[AxisProgression] Focus eksik: {node.displayName}");
                return -2;
            }

            // Yol kısıtlaması: Avcı vs Akışçı
            if (IsPathBlocked(node))
            {
                Debug.Log($"[AxisProgression] Yol kısıtlaması: {node.displayName}");
                return -2; // karşı yol açık
            }

            ForceUnlockNode(node);
            return 1; // açıldı
        }
    }

    /// <summary>Bu node'un karşı yol kısıtlamasına takılıp takılmadığını kontrol eder.</summary>
    public bool IsPathBlocked(SkillNodeSO node)
    {
        if (node == null) return false;
        if (!TryGetExclusiveRouteRule(node.nodeId, out var rule))
            return false;

        var axis = database?.GetAxisForNode(node);
        if (!IsTesterMode && axis != null)
        {
            var progress = GetTreeProgress(axis);
            if (!string.IsNullOrEmpty(progress.chosenTier2Route) &&
                !node.nodeId.Contains(progress.chosenTier2Route))
                return true;
        }

        return HasAnyUnlockedWithPrefix(rule.opposingPrefix);
    }

    /// <summary>Yol engeli nedenini döndürür (UI'da göstermek için).</summary>
    public string GetBlockReason(SkillNodeSO node)
    {
        if (node == null) return "";
        if (IsBlockedByCommitment(node))
            return GetCommitmentBlockReason(node);
        if (IsTier2BlockedByMissingCommitment(node))
            return "Odak acilmadan Tier 2 yolu acilamaz.";
        if (IsRankLocked(node))
        {
            var axis = database != null ? database.GetAxisForNode(node) : null;
            int requiredRank = GetRequiredRankForNode(node);
            int currentRank = axis != null ? GetTreeRank(axis) : 0;
            return $"Tree Rank {requiredRank} gerekli. Su an: Rank {currentRank}.";
        }
        return TryGetExclusiveRouteRule(node.nodeId, out var rule) ? rule.reason : "";
    }

    private bool HasAnyUnlockedWithPrefix(string prefix)
    {
        foreach (var id in _unlockedNodeIds)
        {
            if (id.Contains(prefix)) return true;
        }
        return false;
    }

    /// <summary>Bu node'u ve bu node'a bağımlı tüm açık node'ları kapatır (cascade).</summary>
    private void CascadeLock(SkillNodeSO node)
    {
        // Önce bu node'a prerequisite olarak bağımlı olan açık node'ları bul ve kapat
        var dependents = GetDependentNodes(node);
        foreach (var dep in dependents)
        {
            if (_unlockedNodeIds.Contains(dep.nodeId))
                CascadeLock(dep); // recursive
        }

        // Sonra kendini kapat
        ForceLockNode(node);
    }

    /// <summary>Verilen node'u prerequisite olarak kullanan tüm node'ları döndürür.</summary>
    private List<SkillNodeSO> GetDependentNodes(SkillNodeSO node)
    {
        var result = new List<SkillNodeSO>();
        if (database == null || database.allAxes == null) return result;

        foreach (var axis in database.allAxes)
        {
            if (axis == null || axis.nodes == null) continue;
            foreach (var n in axis.nodes)
            {
                if (n == null || n.prerequisites == null) continue;
                foreach (var prereq in n.prerequisites)
                {
                    if (prereq != null && prereq.nodeId == node.nodeId)
                    {
                        result.Add(n);
                        break;
                    }
                }
            }
        }
        return result;
    }

    /// <summary>[DENEYSEL] Bir node'u zorunlu olarak açar (tüm kuralları bypass).</summary>
    public void ForceUnlockNode(SkillNodeSO node)
    {
        if (node == null) return;
        if (_unlockedNodeIds.Contains(node.nodeId)) return;

        _unlockedNodeIds.Add(node.nodeId);

        var axis = database?.GetAxisForNode(node);
        RecalculateAxisState(axis);
        HandleTier2RouteChoice(node);

        RebuildPlayerBuild();
        OnNodeUnlocked?.Invoke(node);
        OnNodeStatusChanged?.Invoke(node.nodeId, NodeStatus.Unlocked);
        NotifyAxisPairStatuses(axis, node.nodeId);
        NotifyAffectedNodes(node);
        SyncActiveSetToBacking();
        Debug.Log($"[AxisProgression] UNLOCK: {node.displayName}");
        AutoSave();
    }

    /// <summary>[DENEYSEL] Bir node'u zorunlu olarak kapatır (tüm kuralları bypass).</summary>
    public void ForceLockNode(SkillNodeSO node)
    {
        if (node == null) return;
        if (!_unlockedNodeIds.Contains(node.nodeId)) return;

        _unlockedNodeIds.Remove(node.nodeId);

        var axis = database?.GetAxisForNode(node);
        RecalculateAxisState(axis);

        RebuildPlayerBuild();
        OnNodeBecameLocked?.Invoke(node);
        OnNodeStatusChanged?.Invoke(node.nodeId, NodeStatus.VisibleLocked);
        NotifyAxisPairStatuses(axis, node.nodeId);
        Debug.Log($"[AxisProgression] LOCK: {node.displayName}");
        SyncActiveSetToBacking();
        AutoSave();
    }

    /// <summary>Node açık mı?</summary>
    public bool IsNodeUnlocked(SkillNodeSO node)
    {
        return node != null && _unlockedNodeIds.Contains(node.nodeId);
    }

    public TreeProgressionConfigSO GetProgressionConfig()
    {
        if (progressionConfig != null)
            return progressionConfig;

        if (_runtimeDefaultConfig == null)
            _runtimeDefaultConfig = ScriptableObject.CreateInstance<TreeProgressionConfigSO>();

        progressionConfig = _runtimeDefaultConfig;
        return progressionConfig;
    }

    public void SetInteractionMode(SkillTreeInteractionMode mode)
    {
        if (_interactionMode == mode)
            return;

        SyncActiveSetToBacking();
        _interactionMode = mode;
        LoadActiveSetFromBacking();
        RecalculateAllAxisStates();
        RebuildPlayerBuild();
        NotifyAllNodeStatuses();
        OnInteractionModeChanged?.Invoke(_interactionMode);
    }

    public void ToggleInteractionMode()
    {
        SetInteractionMode(IsTesterMode ? SkillTreeInteractionMode.NormalProgression : SkillTreeInteractionMode.Tester);
    }

    public int GetTreeRank(ProgressionAxisSO axis)
    {
        return axis != null ? GetTreeProgress(axis).rank : 0;
    }

    public float GetTreeXp(ProgressionAxisSO axis)
    {
        return axis != null ? GetTreeProgress(axis).xp : 0f;
    }

    public string GetChosenTier2Route(ProgressionAxisSO axis)
    {
        return axis != null ? GetTreeProgress(axis).chosenTier2Route : "";
    }

    public void AddTreeXp(ProgressionAxisSO axis, float amount)
    {
        if (axis == null || amount <= 0f)
            return;

        TreeProgressionRuntime progress = GetTreeProgress(axis);
        int oldRank = progress.rank;
        progress.xp = Mathf.Max(0f, progress.xp + amount);
        progress.rank = GetProgressionConfig().CalculateRank(progress.xp);

        OnTreeProgressChanged?.Invoke(axis, progress.rank, progress.xp);
        if (oldRank != progress.rank)
            NotifyAxisStatuses(axis);

        AutoSave();
    }

    public void DebugAddTreeRank(ProgressionAxisSO axis)
    {
        if (axis == null)
            return;

        TreeProgressionRuntime progress = GetTreeProgress(axis);
        int nextRank = Mathf.Min(GetProgressionConfig().MaxRank, progress.rank + 1);
        if (nextRank <= progress.rank)
            return;

        progress.xp = Mathf.Max(progress.xp, GetProgressionConfig().GetRequiredXpForRank(nextRank));
        progress.rank = nextRank;
        OnTreeProgressChanged?.Invoke(axis, progress.rank, progress.xp);
        NotifyAxisStatuses(axis);
        AutoSave();
    }

    public bool IsAxisCommitted(ProgressionAxisSO axis)
    {
        return axis != null &&
               _axisCommitments.TryGetValue(axis.axisId, out var state) &&
               state.isCommitted;
    }

    public int GetRequiredRankForNode(SkillNodeSO node)
    {
        if (node == null)
            return 0;

        if (node.requiredTreeRank > 0)
            return node.requiredTreeRank;

        if (node.tier <= 1)
            return Mathf.Clamp(GetTier1Index(node) + 1, 1, 5);

        if (node.isCommitmentNode)
            return 6;

        if (node.tier == 2)
            return Mathf.Clamp(7 + GetTier2RouteIndex(node), 7, 11);

        return 12;
    }

    // ═══════════ Otomatik Kayıt ═══════════

    private void AutoSave()
    {
        SyncActiveSetToBacking();

        if (IsTesterMode)
        {
            SkillTreeTesterSaveStore.Save(_testerUnlockedNodeIds);
            return;
        }

        if (SaveManager.Instance == null) return;
        SaveToData(SaveManager.Instance.data);
        SaveManager.Instance.Save();
        Debug.Log($"[AxisProgression] Otomatik kaydedildi. ({_unlockedNodeIds.Count} node)");
    }

    private void SyncActiveSetToBacking()
    {
        HashSet<string> target = IsTesterMode ? _testerUnlockedNodeIds : _normalUnlockedNodeIds;
        target.Clear();
        foreach (var id in _unlockedNodeIds)
            target.Add(id);
    }

    private void LoadActiveSetFromBacking()
    {
        _unlockedNodeIds.Clear();
        HashSet<string> source = IsTesterMode ? _testerUnlockedNodeIds : _normalUnlockedNodeIds;
        foreach (var id in source)
            _unlockedNodeIds.Add(id);
    }

    private void RecalculateAllAxisStates()
    {
        _axisCommitments.Clear();
        if (database == null || database.allAxes == null)
            return;

        foreach (var axis in database.allAxes)
            RecalculateAxisState(axis);

        if (!IsTesterMode)
            RecalculateTier2RoutesFromActiveNodes();
    }

    private void NotifyAllNodeStatuses()
    {
        if (database == null || database.allAxes == null)
            return;

        foreach (var axis in database.allAxes)
            NotifyAxisStatuses(axis);
    }

    private TreeProgressionRuntime GetTreeProgress(ProgressionAxisSO axis)
    {
        if (axis == null || string.IsNullOrEmpty(axis.axisId))
            return new TreeProgressionRuntime();

        if (!_treeProgressions.TryGetValue(axis.axisId, out var progress))
        {
            progress = new TreeProgressionRuntime();
            _treeProgressions[axis.axisId] = progress;
        }

        progress.rank = GetProgressionConfig().CalculateRank(progress.xp);
        return progress;
    }

    private bool IsRankLocked(SkillNodeSO node)
    {
        if (IsTesterMode || node == null)
            return false;

        var axis = database != null ? database.GetAxisForNode(node) : null;
        if (axis == null)
            return false;

        return GetTreeRank(axis) < GetRequiredRankForNode(node);
    }

    private bool IsTier2BlockedByMissingCommitment(SkillNodeSO node)
    {
        if (node == null || node.tier < 2 || node.isCommitmentNode)
            return false;

        var axis = database != null ? database.GetAxisForNode(node) : null;
        return axis != null && !IsAxisCommitted(axis);
    }

    private bool ShouldApplyNodeToBuild(ProgressionAxisSO axis, SkillNodeSO node)
    {
        if (node == null)
            return false;

        if (node.tier <= 1 || node.isCommitmentNode)
            return true;

        return axis != null && IsAxisCommitted(axis);
    }

    private void HandleTier2RouteChoice(SkillNodeSO node)
    {
        if (IsTesterMode || node == null || node.tier != 2 || node.isCommitmentNode)
            return;

        if (!TryGetExclusiveRouteRule(node.nodeId, out var rule))
            return;

        var axis = database != null ? database.GetAxisForNode(node) : null;
        if (axis == null)
            return;

        TreeProgressionRuntime progress = GetTreeProgress(axis);
        if (string.IsNullOrEmpty(progress.chosenTier2Route))
            progress.chosenTier2Route = rule.ownPrefix;
    }

    private void RecalculateTier2RoutesFromActiveNodes()
    {
        if (database == null || database.allAxes == null)
            return;

        foreach (var axis in database.allAxes)
        {
            if (axis == null || axis.nodes == null)
                continue;

            TreeProgressionRuntime progress = GetTreeProgress(axis);
            if (!string.IsNullOrEmpty(progress.chosenTier2Route))
                continue;

            foreach (var node in axis.nodes)
            {
                if (node == null || !_unlockedNodeIds.Contains(node.nodeId))
                    continue;

                if (node.tier == 2 && !node.isCommitmentNode && TryGetExclusiveRouteRule(node.nodeId, out var rule))
                {
                    progress.chosenTier2Route = rule.ownPrefix;
                    break;
                }
            }
        }
    }

    private int GetTier1Index(SkillNodeSO node)
    {
        var axis = database != null ? database.GetAxisForNode(node) : null;
        if (axis == null || axis.nodes == null)
            return 0;

        int index = 0;
        foreach (var candidate in axis.nodes)
        {
            if (candidate == null || candidate.tier != 1)
                continue;

            if (candidate == node)
                return index;

            index++;
        }

        return 0;
    }

    private int GetTier2RouteIndex(SkillNodeSO node)
    {
        if (node == null || !TryGetExclusiveRouteRule(node.nodeId, out var rule))
            return 0;

        var axis = database != null ? database.GetAxisForNode(node) : null;
        if (axis == null || axis.nodes == null)
            return 0;

        int index = 0;
        foreach (var candidate in axis.nodes)
        {
            if (candidate == null)
                continue;

            if (candidate == node)
                return index;

            if (candidate.nodeId.Contains(rule.ownPrefix))
                index++;
        }

        return index;
    }

    // ═══════════ Debug Yardımcıları ═══════════

    /// <summary>Mevcut durumu Console'a yazdırır.</summary>
    [ContextMenu("Debug: Print State")]
    public void DebugPrintState()
    {
        Debug.Log($"[AxisProgression] Açık Node Sayısı: {_unlockedNodeIds.Count}");
        foreach (var id in _unlockedNodeIds)
            Debug.Log($"  - {id}");

        foreach (var kv in _formAffinities)
            Debug.Log($"  Form '{kv.Key}': {kv.Value}");

        foreach (var kv in _axisCommitments)
            Debug.Log($"  Commitment '{kv.Key}': committed={kv.Value.isCommitted}, node={kv.Value.commitmentNodeId}, route={kv.Value.chosenRoute}");

        Debug.Log(_currentBuild.ToString());
    }

    private static bool TryGetExclusiveRouteRule(string nodeId, out ExclusiveRouteRule rule)
    {
        if (!string.IsNullOrEmpty(nodeId))
        {
            foreach (var candidate in ExclusiveRouteRules)
            {
                if (nodeId.Contains(candidate.ownPrefix))
                {
                    rule = candidate;
                    return true;
                }
            }
        }

        rule = default;
        return false;
    }

    private string GetCommitmentBlockReason(SkillNodeSO node)
    {
        if (database == null || node == null)
            return "Karsi eksende commitment aktif.";

        var ownerAxis = database.GetAxisForNode(node);
        if (ownerAxis == null)
            return "Karsi eksende commitment aktif.";

        var oppositeAxis = database.GetOpposingAxis(ownerAxis);
        if (oppositeAxis == null)
            return "Karsi eksende commitment aktif.";

        return $"{oppositeAxis.displayName} commitment'i acik. Once onu kapat.";
    }
}

// ═══════════ Node Durumu Enum ═══════════

/// <summary>
/// Bir node'un mevcut durumunu temsil eder.
/// </summary>
public enum NodeStatus
{
    Hidden,
    VisibleLocked,
    Unlockable,
    Unlocked
}

// ═══════════ Runtime Commitment State ═══════════

/// <summary>
/// Bir eksenin commitment durumu. Runtime'da kullanılır, save için AxisCommitmentEntry'ye dönüştürülür.
/// </summary>
[System.Serializable]
public class CommitmentState
{
    public bool isCommitted;
    public string commitmentNodeId = "";
    public string chosenRoute = "";
    public int highestUnlockedTier;
}

public class TreeProgressionRuntime
{
    public float xp;
    public int rank;
    public string chosenTier2Route = "";
}

internal readonly struct ExclusiveRouteRule
{
    public readonly string ownPrefix;
    public readonly string opposingPrefix;
    public readonly string reason;

    public ExclusiveRouteRule(string ownPrefix, string opposingPrefix, string reason)
    {
        this.ownPrefix = ownPrefix;
        this.opposingPrefix = opposingPrefix;
        this.reason = reason;
    }
}

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

    [Header("Veritabanı")]
    [Tooltip("Tüm eksenleri, karşıt çiftleri ve form overlay'leri barındıran database asset.")]
    public AxisDatabaseSO database;

    // ═══════════ Runtime State ═══════════
    private readonly HashSet<string> _unlockedNodeIds = new HashSet<string>();
    private readonly Dictionary<string, int> _formAffinities = new Dictionary<string, int>();
    private readonly Dictionary<string, CommitmentState> _axisCommitments = new Dictionary<string, CommitmentState>();

    // ═══════════ Derlenmiş Build ═══════════
    private PlayerBuild _currentBuild = new PlayerBuild();
    public PlayerBuild CurrentBuild => _currentBuild;

    // ═══════════ Events ═══════════
    public event Action<SkillNodeSO> OnNodeUnlocked;
    public event Action<SkillNodeSO> OnNodeBecameLocked;
    public event Action<FormOverlaySO, int, int> OnFormAffinityChanged;
    public event Action<PlayerBuild> OnBuildChanged;
    public event Action<string, NodeStatus> OnNodeStatusChanged;

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
        _formAffinities.Clear();
        _axisCommitments.Clear();

        if (data == null) return;

        // Node'lar
        if (data.unlockedSkillNodeIds != null)
        {
            foreach (var id in data.unlockedSkillNodeIds)
                _unlockedNodeIds.Add(id);
        }

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

        // startsUnlocked node'ları otomatik ekle
        if (database != null && database.allAxes != null)
        {
            foreach (var axis in database.allAxes)
            {
                if (axis == null || axis.nodes == null) continue;
                foreach (var node in axis.nodes)
                {
                    if (node != null && node.startsUnlocked)
                        _unlockedNodeIds.Add(node.nodeId);
                }
            }
        }

        RebuildPlayerBuild();
    }

    /// <summary>
    /// Mevcut state'i SaveData'ya yazar.
    /// </summary>
    public void SaveToData(SaveData data)
    {
        if (data == null) return;

        // Node'lar
        data.unlockedSkillNodeIds = new List<string>(_unlockedNodeIds);

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
        if (node.tier >= 3 && !IsFormGatePassed(node))
            return NodeStatus.VisibleLocked;

        // 6. Hepsi tamam
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

        // Build yeniden derle
        RebuildPlayerBuild();

        // Events
        OnNodeUnlocked?.Invoke(node);
        OnNodeStatusChanged?.Invoke(node.nodeId, NodeStatus.Unlocked);

        // Karşıt eksende kilitlenen node'lar için status changed eventi
        NotifyAffectedNodes(node);

        Debug.Log($"[AxisProgression] Node açıldı: {node.displayName} (ID: {node.nodeId})");
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
                if (node != null && _unlockedNodeIds.Contains(node.nodeId))
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
            // Açarken: prerequisite kontrolü
            if (!ArePrerequisitesMet(node))
            {
                Debug.Log($"[AxisProgression] Prerequisite eksik: {node.displayName}");
                return -1; // açılamaz
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
        string id = node.nodeId;

        // Hunter node açmak istiyorsak → Flow node açık mı?
        if (id.Contains("_t2h_"))
            return HasAnyUnlockedWithPrefix("_t2f_");

        // Flow node açmak istiyorsak → Hunter node açık mı?
        if (id.Contains("_t2f_"))
            return HasAnyUnlockedWithPrefix("_t2h_");

        return false;
    }

    /// <summary>Yol engeli nedenini döndürür (UI'da göstermek için).</summary>
    public string GetBlockReason(SkillNodeSO node)
    {
        if (node == null) return "";
        if (node.nodeId.Contains("_t2h_"))
            return "Akışçı yolunda açık perk var. Önce onu kapat.";
        if (node.nodeId.Contains("_t2f_"))
            return "Avcı yolunda açık perk var. Önce onu kapat.";
        return "";
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
        _unlockedNodeIds.Add(node.nodeId);
        RebuildPlayerBuild();
        OnNodeUnlocked?.Invoke(node);
        OnNodeStatusChanged?.Invoke(node.nodeId, NodeStatus.Unlocked);
        Debug.Log($"[AxisProgression] UNLOCK: {node.displayName}");
        AutoSave();
    }

    /// <summary>[DENEYSEL] Bir node'u zorunlu olarak kapatır (tüm kuralları bypass).</summary>
    public void ForceLockNode(SkillNodeSO node)
    {
        if (node == null) return;
        _unlockedNodeIds.Remove(node.nodeId);
        RebuildPlayerBuild();
        OnNodeBecameLocked?.Invoke(node);
        OnNodeStatusChanged?.Invoke(node.nodeId, NodeStatus.VisibleLocked);
        Debug.Log($"[AxisProgression] LOCK: {node.displayName}");
        AutoSave();
    }

    /// <summary>Node açık mı?</summary>
    public bool IsNodeUnlocked(SkillNodeSO node)
    {
        return node != null && _unlockedNodeIds.Contains(node.nodeId);
    }

    // ═══════════ Otomatik Kayıt ═══════════

    private void AutoSave()
    {
        if (SaveManager.Instance == null) return;
        SaveToData(SaveManager.Instance.data);
        SaveManager.Instance.Save();
        Debug.Log($"[AxisProgression] Otomatik kaydedildi. ({_unlockedNodeIds.Count} node)");
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

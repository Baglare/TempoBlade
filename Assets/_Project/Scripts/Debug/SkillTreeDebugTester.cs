using UnityEngine;

/// <summary>
/// Inspector'dan sağ tıklayarak Skill Tree altyapısını test etmek için debug scripti.
/// Sahneye boş bir objeye ekle, database alanına TestDatabase'i sürükle, sağ tıkla ve test metotlarını çalıştır.
/// </summary>
public class SkillTreeDebugTester : MonoBehaviour
{
    [Header("Veritabanı")]
    [Tooltip("SkillTreeTestSetup ile oluşturulan TestDatabase asset'ini buraya sürükle.")]
    public AxisDatabaseSO testDatabase;

    private AxisProgressionManager _manager;

    private void Awake()
    {
        // Sahnede zaten bir manager varsa onu kullan, yoksa oluştur
        _manager = AxisProgressionManager.Instance;
        if (_manager == null)
        {
            var go = new GameObject("AxisProgressionManager (Debug)");
            _manager = go.AddComponent<AxisProgressionManager>();
        }
    }

    private void Start()
    {
        if (testDatabase == null)
        {
            Debug.LogError("[DebugTester] testDatabase atanmamış! Inspector'dan TestDatabase asset'ini sürükle.");
            return;
        }

        _manager.database = testDatabase;
        _manager.LoadFromSave(new SaveData());

        Debug.Log("<color=cyan>══════════ Skill Tree Debug Tester Hazır ══════════</color>");
        Debug.Log("Sağ tıklayarak test metotlarını çalıştırabilirsin:");
        Debug.Log("  - Test 1: Tüm Node Status'ları Göster");
        Debug.Log("  - Test 2: T1 Node'ları Aç");
        Debug.Log("  - Test 3: Commitment Lock Testi");
        Debug.Log("  - Test 4: Form Gate Testi");
        Debug.Log("  - Test 5: Full Build Çıktısı");
    }

    // ═══════════ TEST 1: Status Raporu ═══════════

    [ContextMenu("Test 1: Tüm Node Status'ları Göster")]
    public void Test_ShowAllStatuses()
    {
        if (!EnsureReady()) return;

        Debug.Log("<color=yellow>═══ TEST 1: Node Status Raporu ═══</color>");

        foreach (var axis in testDatabase.allAxes)
        {
            if (axis == null) continue;
            Debug.Log($"<color=white>── {axis.displayName} ──</color>");
            foreach (var node in axis.nodes)
            {
                if (node == null) continue;
                var status = _manager.GetNodeStatus(node);
                string color = StatusColor(status);
                string commit = node.isCommitmentNode ? " [COMMIT]" : "";
                string region = string.IsNullOrEmpty(node.regionTag) ? "" : $" [{node.regionTag}]";
                Debug.Log($"  <color={color}>T{node.tier} | {status} | {node.displayName}{commit}{region}</color>");
            }
        }
    }

    // ═══════════ TEST 2: T1 Node'ları Aç ═══════════

    [ContextMenu("Test 2: T1 Node'ları Aç (Her İki Eksen)")]
    public void Test_UnlockT1Nodes()
    {
        if (!EnsureReady()) return;

        Debug.Log("<color=yellow>═══ TEST 2: T1 Node'ları Açılıyor ═══</color>");

        foreach (var axis in testDatabase.allAxes)
        {
            if (axis == null) continue;
            foreach (var node in axis.Tier1Nodes)
            {
                bool result = _manager.TryUnlockNode(node);
                Debug.Log($"  TryUnlock '{node.displayName}' → {(result ? "<color=green>BAŞARILI</color>" : "<color=red>BAŞARISIZ</color>")}");
            }
        }

        Test_ShowAllStatuses();
    }

    // ═══════════ TEST 3: Commitment Lock ═══════════

    [ContextMenu("Test 3: Eksen-A Commitment → Eksen-B Kilidi")]
    public void Test_CommitmentLock()
    {
        if (!EnsureReady()) return;

        Debug.Log("<color=yellow>═══ TEST 3: Commitment Lock Testi ═══</color>");

        // Önce Eksen-A T1'leri açık olmalı
        var axisA = testDatabase.GetAxisById("axis_attack");
        if (axisA == null) { Debug.LogError("axis_attack bulunamadı!"); return; }

        foreach (var node in axisA.Tier1Nodes)
            _manager.TryUnlockNode(node);

        // Şimdi Eksen-A commitment node'u aç
        var commitNodes = axisA.CommitmentNodes;
        if (commitNodes.Count == 0) { Debug.LogError("Commitment node bulunamadı!"); return; }

        Debug.Log($"<color=white>Commitment açılıyor: {commitNodes[0].displayName}</color>");
        bool result = _manager.TryUnlockNode(commitNodes[0]);
        Debug.Log($"  Sonuç: {(result ? "<color=green>BAŞARILI</color>" : "<color=red>BAŞARISIZ</color>")}");

        // Eksen-B durumlarını kontrol et
        Debug.Log("<color=white>── Eksen-B Durumları (Commitment Sonrası) ──</color>");
        var axisB = testDatabase.GetAxisById("axis_defense");
        if (axisB == null) return;

        foreach (var node in axisB.nodes)
        {
            if (node == null) continue;
            var status = _manager.GetNodeStatus(node);
            string color = StatusColor(status);
            string expected = "";
            if (node.tier == 1) expected = " ← T1: etkilenmemeli";
            else if (node.isCommitmentNode) expected = " ← COMMIT: KİLİTLİ olmalı!";
            else if (node.tier >= 2) expected = " ← T2+: KİLİTLİ olmalı!";

            Debug.Log($"  <color={color}>T{node.tier} | {status} | {node.displayName}{expected}</color>");
        }
    }

    // ═══════════ TEST 4: Form Gate ═══════════

    [ContextMenu("Test 4: Form Affinity Kaydır (+3) → T3 Bölge Gating")]
    public void Test_FormGating()
    {
        if (!EnsureReady()) return;

        Debug.Log("<color=yellow>═══ TEST 4: Form Gate Testi ═══</color>");

        var form = testDatabase.GetFormOverlayById("form_essence");
        if (form == null) { Debug.LogError("form_essence bulunamadı!"); return; }

        // Önce mevcut T3 durumları
        Debug.Log("<color=white>── Form Affinity: 0 (Başlangıç) ──</color>");
        ShowT3Statuses();

        // Formu +3'e kaydır
        _manager.ShiftFormAffinity(form, 3);
        Debug.Log($"<color=white>── Form Affinity: {_manager.GetFormAffinity(form)} (Kaydırma Sonrası) ──</color>");
        ShowT3Statuses();

        // alpha_path erişilebilir olmalı, beta_path kilitli
        Debug.Log("<color=cyan>Beklenti: alpha_path → Unlockable/VisibleLocked, beta_path → VisibleLocked/Hidden</color>");
    }

    [ContextMenu("Test 4b: Form Affinity Sıfırla ve -3'e Kaydır")]
    public void Test_FormGatingNegative()
    {
        if (!EnsureReady()) return;

        var form = testDatabase.GetFormOverlayById("form_essence");
        if (form == null) return;

        // Formu sıfırla ve -3'e kaydir
        _manager.ShiftFormAffinity(form, -_manager.GetFormAffinity(form)); // sıfırla
        _manager.ShiftFormAffinity(form, -3);

        Debug.Log($"<color=yellow>═══ Form Affinity: {_manager.GetFormAffinity(form)} ═══</color>");
        ShowT3Statuses();
        Debug.Log("<color=cyan>Beklenti: beta_path → Unlockable/VisibleLocked, alpha_path → VisibleLocked/Hidden</color>");
    }

    // ═══════════ TEST 5: Full Build ═══════════

    [ContextMenu("Test 5: Mevcut PlayerBuild Çıktısı")]
    public void Test_PrintBuild()
    {
        if (!EnsureReady()) return;

        Debug.Log("<color=yellow>═══ TEST 5: PlayerBuild ═══</color>");
        Debug.Log(_manager.CurrentBuild.ToString());
    }

    // ═══════════ SIFIRLAMA ═══════════

    [ContextMenu("Sıfırla: Tüm State'i Temizle")]
    public void ResetAll()
    {
        if (!EnsureReady()) return;

        _manager.LoadFromSave(new SaveData());
        Debug.Log("<color=red>═══ TÜM STATE SIFIRLANDI ═══</color>");
        Test_ShowAllStatuses();
    }

    // ═══════════ Yardımcılar ═══════════

    private void ShowT3Statuses()
    {
        foreach (var axis in testDatabase.allAxes)
        {
            if (axis == null) continue;
            foreach (var node in axis.Tier3Nodes)
            {
                if (node == null) continue;
                var status = _manager.GetNodeStatus(node);
                string color = StatusColor(status);
                Debug.Log($"  <color={color}>T3 | {status} | {node.displayName} [{node.regionTag}]</color>");
            }
        }
    }

    private bool EnsureReady()
    {
        if (_manager == null)
        {
            _manager = AxisProgressionManager.Instance;
        }
        if (_manager == null || testDatabase == null)
        {
            Debug.LogError("[DebugTester] Manager veya testDatabase hazır değil!");
            return false;
        }
        if (_manager.database == null)
        {
            _manager.database = testDatabase;
            _manager.LoadFromSave(new SaveData());
        }
        return true;
    }

    private string StatusColor(NodeStatus status)
    {
        return status switch
        {
            NodeStatus.Hidden => "gray",
            NodeStatus.VisibleLocked => "red",
            NodeStatus.Unlockable => "yellow",
            NodeStatus.Unlocked => "green",
            _ => "white"
        };
    }
}

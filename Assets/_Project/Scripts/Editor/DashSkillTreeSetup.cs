#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// Editor menüsünden Dash Skill Tree SO asset'lerini otomatik oluşturur.
/// Menü: TempoBlade > Skill Tree > Dash Ağacı Asset'leri Oluştur
/// </summary>
public static class DashSkillTreeSetup
{
    private const string BasePath = "Assets/_Project/Data/SkillTree_Dash";

    [MenuItem("TempoBlade/Skill Tree/Overdrive ve Cadence Agaclarini Olustur")]
    public static void CreateOverdriveAndCadenceTreeAssets()
    {
        foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
        {
            var setupType = assembly.GetType("OverdriveCadenceSkillTreeSetup");
            var createMethod = setupType?.GetMethod(
                "CreateTrees",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

            if (createMethod == null)
                continue;

            createMethod.Invoke(null, null);
            return;
        }

        Debug.LogError("[DashSkillTreeSetup] OverdriveCadenceSkillTreeSetup bulunamadi. Once Assets > Refresh veya script Reimport yap.");
    }

    [MenuItem("TempoBlade/Skill Tree/Dash Ağacı Asset'leri Oluştur")]
    public static void CreateDashTreeAssets()
    {
        // Klasörleri oluştur
        EnsureFolder(BasePath);
        EnsureFolder(BasePath + "/T1");
        EnsureFolder(BasePath + "/T2_Commitment");
        EnsureFolder(BasePath + "/T2_Hunter");
        EnsureFolder(BasePath + "/T2_Flow");

        // ═══════════ TIER 1 — 5 NODE ═══════════

        var t1_rangedDodge = CreateNode("T1/Dash_T1_RangedDodge", "dash_t1_ranged_dodge",
            "Menzilli Kaçınma", "Dash sırasında projectile saldırılardan güvenli kaçınma sağlar.", 1);
        AddFlagEffect(t1_rangedDodge, EffectKeyRegistry.DashProjectileDodge);

        var t1_meleeDodge = CreateNode("T1/Dash_T1_MeleeDodge", "dash_t1_melee_dodge",
            "Yakın Dövüş Kaçınma", "Dash sırasında melee saldırılardan güvenli kaçınma sağlar.", 1);
        AddFlagEffect(t1_meleeDodge, EffectKeyRegistry.DashMeleeDodge);

        var t1_counter = CreateNode("T1/Dash_T1_Counter", "dash_t1_counter",
            "Karşı Saldırı", "Başarılı dodge sonrası bonus hasarlı karşı saldırı penceresi açar.", 1);
        t1_counter.prerequisites = new SkillNodeSO[] { t1_rangedDodge, t1_meleeDodge };
        AddFlagEffect(t1_counter, EffectKeyRegistry.DashCounter);
        EditorUtility.SetDirty(t1_counter);

        var t1_tempoGain = CreateNode("T1/Dash_T1_TempoGain", "dash_t1_tempo_gain",
            "Tempo Kazancı", "Düşmana yakın biten dash'ler tempo kazandırır.", 1);
        AddFlagEffect(t1_tempoGain, EffectKeyRegistry.DashTempoGain);

        var t1_attackSpeed = CreateNode("T1/Dash_T1_AttackSpeed", "dash_t1_attack_speed",
            "Saldırı Hızı", "Dash sonrası ilk saldırıyı daha hızlı zincirle.", 1);
        t1_attackSpeed.prerequisites = new SkillNodeSO[] { t1_tempoGain };
        AddFlagEffect(t1_attackSpeed, EffectKeyRegistry.DashAttackSpeed);
        EditorUtility.SetDirty(t1_attackSpeed);

        // ═══════════ T2 COMMITMENT ═══════════

        var t2_commitment = CreateNode("T2_Commitment/Dash_T2_Commitment", "dash_t2_commitment",
            "Dash Odağı", "Dash'i ana savunma-saldırı ritmi olarak seçer. Parry zayıflar, Dash güçlenir.", 2);
        t2_commitment.isCommitmentNode = true;
        t2_commitment.prerequisites = new SkillNodeSO[] { t1_counter, t1_attackSpeed };
        AddFlagEffect(t2_commitment, EffectKeyRegistry.DashT2Commitment);
        EditorUtility.SetDirty(t2_commitment);

        // ═══════════ T2 AVCI YOLU — 5 NODE ═══════════

        var t2h_huntMark = CreateNode("T2_Hunter/Dash_T2H_HuntMark", "dash_t2h_hunt_mark",
            "Av İşareti", "Yarı-otomatik hedef takip sistemi. Düşmanları av olarak işaretler.", 2);
        t2h_huntMark.prerequisites = new SkillNodeSO[] { t2_commitment };
        AddFlagEffect(t2h_huntMark, EffectKeyRegistry.DashHuntMark);
        EditorUtility.SetDirty(t2h_huntMark);

        var t2h_blindSpot = CreateNode("T2_Hunter/Dash_T2H_BlindSpot", "dash_t2h_blind_spot",
            "Kör Nokta Baskısı", "Avın ön konisi dışına dash atarak stun ve bonus hasar kazan.", 2);
        t2h_blindSpot.prerequisites = new SkillNodeSO[] { t2h_huntMark };
        AddFlagEffect(t2h_blindSpot, EffectKeyRegistry.DashBlindSpot);
        EditorUtility.SetDirty(t2h_blindSpot);

        var t2h_huntFlow = CreateNode("T2_Hunter/Dash_T2H_HuntFlow", "dash_t2h_hunt_flow",
            "Av Etrafında Akış", "Av yakınındayken dash cooldown daha hızlı yenilenir.", 2);
        t2h_huntFlow.prerequisites = new SkillNodeSO[] { t2h_huntMark };
        AddFlagEffect(t2h_huntFlow, EffectKeyRegistry.DashHuntFlow);
        EditorUtility.SetDirty(t2h_huntFlow);

        var t2h_execute = CreateNode("T2_Hunter/Dash_T2H_Execute", "dash_t2h_execute",
            "İnfaz Dash'i", "Düşük canlı avın arkasından dash ile infaz et.", 2);
        t2h_execute.prerequisites = new SkillNodeSO[] { t2h_blindSpot, t2h_huntFlow };
        AddFlagEffect(t2h_execute, EffectKeyRegistry.DashExecute);
        EditorUtility.SetDirty(t2h_execute);

        var t2h_huntCycle = CreateNode("T2_Hunter/Dash_T2H_HuntCycle", "dash_t2h_hunt_cycle",
            "Av Devri", "Her av düşürdüğünde oda boyu hasar bonusu kazan.", 2);
        t2h_huntCycle.prerequisites = new SkillNodeSO[] { t2h_execute };
        AddFlagEffect(t2h_huntCycle, EffectKeyRegistry.DashHuntCycle);
        EditorUtility.SetDirty(t2h_huntCycle);

        // ═══════════ T2 AKIŞÇI YOLU — 5 NODE ═══════════

        var t2f_flowMark = CreateNode("T2_Flow/Dash_T2F_FlowMark", "dash_t2f_flow_mark",
            "İşaretleme Akışı", "Dash sonrası vurulan düşmanları işaretle, benzersiz işaret başına bonus hasar.", 2);
        t2f_flowMark.prerequisites = new SkillNodeSO[] { t2_commitment };
        AddFlagEffect(t2f_flowMark, EffectKeyRegistry.DashFlowMark);
        EditorUtility.SetDirty(t2f_flowMark);

        var t2f_snapback = CreateNode("T2_Flow/Dash_T2F_Snapback", "dash_t2f_snapback",
            "Geri Sıçrama", "Dash sonrası kısa pencere içinde başlangıç noktasına geri dön.", 2);
        t2f_snapback.prerequisites = new SkillNodeSO[] { t2_commitment };
        AddFlagEffect(t2f_snapback, EffectKeyRegistry.DashSnapback);
        EditorUtility.SetDirty(t2f_snapback);

        var t2f_chain = CreateNode("T2_Flow/Dash_T2F_ChainBounce", "dash_t2f_chain_bounce",
            "Zincir Sekmesi", "İşaretli hedefe vurunca hasar diğer işaretli hedeflere seker.", 2);
        t2f_chain.prerequisites = new SkillNodeSO[] { t2f_flowMark };
        AddFlagEffect(t2f_chain, EffectKeyRegistry.DashChainBounce);
        EditorUtility.SetDirty(t2f_chain);

        var t2f_blackHole = CreateNode("T2_Flow/Dash_T2F_BlackHole", "dash_t2f_black_hole",
            "Kara Delik", "Yeterince hedef işaretlendiğinde hepsini tek noktaya çek.", 2);
        t2f_blackHole.prerequisites = new SkillNodeSO[] { t2f_chain };
        AddFlagEffect(t2f_blackHole, EffectKeyRegistry.DashBlackHole);
        EditorUtility.SetDirty(t2f_blackHole);

        var t2f_burst = CreateNode("T2_Flow/Dash_T2F_Burst", "dash_t2f_burst",
            "Patlama Vuruşu", "Kara Delik sonrası birikimli büyük hasar vuruşu.", 2);
        t2f_burst.prerequisites = new SkillNodeSO[] { t2f_blackHole, t2f_snapback };
        AddFlagEffect(t2f_burst, EffectKeyRegistry.DashBurst);
        EditorUtility.SetDirty(t2f_burst);

        // ═══════════ AXIS SO ═══════════

        var dashAxis = ScriptableObject.CreateInstance<ProgressionAxisSO>();
        dashAxis.axisId = "axis_dash";
        dashAxis.displayName = "Dash Ekseni";
        dashAxis.axisColor = new Color(0.2f, 0.8f, 1f); // Cyan
        dashAxis.nodes = new SkillNodeSO[]
        {
            // T1
            t1_rangedDodge, t1_meleeDodge, t1_counter, t1_tempoGain, t1_attackSpeed,
            // T2 Commitment
            t2_commitment,
            // T2 Avcı
            t2h_huntMark, t2h_blindSpot, t2h_huntFlow, t2h_execute, t2h_huntCycle,
            // T2 Akışçı
            t2f_flowMark, t2f_snapback, t2f_chain, t2f_blackHole, t2f_burst
        };
        SaveAsset(dashAxis, "DashAxis");

        // ═══════════ DATABASE GÜNCELLEME NOTU ═══════════
        // Mevcut AxisDatabaseSO asset'ine bu DashAxis'i manuel eklemen gerekecek.
        // Veya yeni bir database oluşturabilirsin:

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("<color=green>[DashSkillTreeSetup] Tüm Dash Skill Tree SO'ları oluşturuldu!</color>");
        Debug.Log($"  Konum: {BasePath}");
        Debug.Log("  T1: 5 node | T2 Commitment: 1 node | T2 Avcı: 5 node | T2 Akışçı: 5 node = 16 toplam");
        Debug.Log("  <color=yellow>NOT: DashAxis'i mevcut AxisDatabaseSO asset'ine manuel olarak ekleyin.</color>");
    }

    // ═══════════ Yardımcılar ═══════════

    private static SkillNodeSO CreateNode(string subPath, string id, string displayName, string desc, int tier)
    {
        var node = ScriptableObject.CreateInstance<SkillNodeSO>();
        node.nodeId = id;
        node.displayName = displayName;
        node.description = desc;
        node.tier = tier;
        node.visibility = NodeVisibility.AlwaysVisible;
        node.effects = new NodeEffect[0];

        string fullPath = BasePath + "/" + subPath + ".asset";
        AssetDatabase.CreateAsset(node, fullPath);
        return node;
    }

    private static void AddFlagEffect(SkillNodeSO node, string key)
    {
        var list = new List<NodeEffect>(node.effects);
        list.Add(new NodeEffect
        {
            type = EffectType.Flag,
            key = key,
            modifierOp = ModifierOp.Flat,
            numericValue = 0f
        });
        node.effects = list.ToArray();
        EditorUtility.SetDirty(node);
    }

    private static void SaveAsset(ScriptableObject obj, string name)
    {
        string path = BasePath + "/" + name + ".asset";
        AssetDatabase.CreateAsset(obj, path);
    }

    private static void EnsureFolder(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            string parent = Path.GetDirectoryName(path).Replace("\\", "/");
            string folder = Path.GetFileName(path);
            AssetDatabase.CreateFolder(parent, folder);
        }
    }
}
#endif

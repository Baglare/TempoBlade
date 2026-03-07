#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Editor menüsünden örnek Skill Tree SO asset'lerini otomatik oluşturur.
/// Menü: TempoBlade > Skill Tree > Örnek Test Asset'leri Oluştur
/// </summary>
public static class SkillTreeTestSetup
{
    private const string BasePath = "Assets/_Project/Data/SkillTree_Test";

    [MenuItem("TempoBlade/Skill Tree/Örnek Test Asset'leri Oluştur")]
    public static void CreateTestAssets()
    {
        // Klasörleri oluştur
        EnsureFolder(BasePath);
        EnsureFolder(BasePath + "/Nodes_A");
        EnsureFolder(BasePath + "/Nodes_B");

        // ═══════════ EKSEN A — Node'lar ═══════════
        var a_t1_1 = CreateNode("Nodes_A/A_T1_Temel", "a_t1_temel", "Temel Güç", "Eksen-A giriş node'u", 1);
        AddEffect(a_t1_1, EffectType.StatModifier, EffectKeyRegistry.Damage, ModifierOp.Flat, 2f);

        var a_t1_2 = CreateNode("Nodes_A/A_T1_Hiz", "a_t1_hiz", "Hız Artışı", "Hafif hız bonusu", 1);
        AddEffect(a_t1_2, EffectType.StatModifier, EffectKeyRegistry.AttackSpeed, ModifierOp.Percent, 0.05f);

        var a_t2_commit = CreateNode("Nodes_A/A_T2_Commitment", "a_t2_commitment", "Saldırı Odağı", "COMMITMENT: Saldırı yolunu seçer, karşıt eksen kilitlenir", 2);
        a_t2_commit.isCommitmentNode = true;
        a_t2_commit.prerequisites = new SkillNodeSO[] { a_t1_1 };
        AddEffect(a_t2_commit, EffectType.StatModifier, EffectKeyRegistry.Damage, ModifierOp.Percent, 0.15f);

        var a_t2_normal = CreateNode("Nodes_A/A_T2_Normal", "a_t2_normal", "Ek Menzil", "Commitment OLMAYAN T2 node", 2);
        a_t2_normal.prerequisites = new SkillNodeSO[] { a_t1_1 };
        AddEffect(a_t2_normal, EffectType.StatModifier, EffectKeyRegistry.Damage, ModifierOp.Flat, 3f);

        var a_t3_alpha = CreateNode("Nodes_A/A_T3_Alpha", "a_t3_alpha", "Alpha Mutasyon", "Form-A gerektirir (alpha_path)", 3);
        a_t3_alpha.regionTag = "alpha_path";
        a_t3_alpha.prerequisites = new SkillNodeSO[] { a_t2_commit };
        AddEffect(a_t3_alpha, EffectType.Flag, EffectKeyRegistry.DashDamage, ModifierOp.Flat, 0f);

        var a_t3_beta = CreateNode("Nodes_A/A_T3_Beta", "a_t3_beta", "Beta Mutasyon", "Form-B gerektirir (beta_path)", 3);
        a_t3_beta.regionTag = "beta_path";
        a_t3_beta.prerequisites = new SkillNodeSO[] { a_t2_commit };
        AddEffect(a_t3_beta, EffectType.Flag, EffectKeyRegistry.ParryHeal, ModifierOp.Flat, 0f);

        // ═══════════ EKSEN B — Node'lar ═══════════
        var b_t1_1 = CreateNode("Nodes_B/B_T1_Temel", "b_t1_temel", "Temel Savunma", "Eksen-B giriş node'u", 1);
        AddEffect(b_t1_1, EffectType.StatModifier, EffectKeyRegistry.MaxHealth, ModifierOp.Flat, 20f);

        var b_t1_2 = CreateNode("Nodes_B/B_T1_Parry", "b_t1_parry", "Parry Genişleme", "Hafif parry penceresi", 1);
        AddEffect(b_t1_2, EffectType.StatModifier, EffectKeyRegistry.ParryWindow, ModifierOp.Flat, 0.02f);

        var b_t2_commit = CreateNode("Nodes_B/B_T2_Commitment", "b_t2_commitment", "Savunma Odağı", "COMMITMENT: Savunma yolunu seçer, karşıt eksen kilitlenir", 2);
        b_t2_commit.isCommitmentNode = true;
        b_t2_commit.prerequisites = new SkillNodeSO[] { b_t1_1 };
        AddEffect(b_t2_commit, EffectType.StatModifier, EffectKeyRegistry.MaxHealth, ModifierOp.Percent, 0.2f);

        var b_t3_alpha = CreateNode("Nodes_B/B_T3_Alpha", "b_t3_alpha", "Alpha Kalkan", "Form-A gerektirir", 3);
        b_t3_alpha.regionTag = "alpha_path";
        b_t3_alpha.prerequisites = new SkillNodeSO[] { b_t2_commit };
        AddEffect(b_t3_alpha, EffectType.FeatureUnlock, EffectKeyRegistry.WallSlide, ModifierOp.Flat, 0f);

        // ═══════════ EKSEN SO'ları ═══════════
        var axisA = ScriptableObject.CreateInstance<ProgressionAxisSO>();
        axisA.axisId = "axis_attack";
        axisA.displayName = "Eksen-A (Saldırı)";
        axisA.axisColor = new Color(1f, 0.3f, 0.3f);
        axisA.nodes = new SkillNodeSO[] { a_t1_1, a_t1_2, a_t2_commit, a_t2_normal, a_t3_alpha, a_t3_beta };
        SaveAsset(axisA, "Axis_A_Attack");

        var axisB = ScriptableObject.CreateInstance<ProgressionAxisSO>();
        axisB.axisId = "axis_defense";
        axisB.displayName = "Eksen-B (Savunma)";
        axisB.axisColor = new Color(0.3f, 0.5f, 1f);
        axisB.nodes = new SkillNodeSO[] { b_t1_1, b_t1_2, b_t2_commit, b_t3_alpha };
        SaveAsset(axisB, "Axis_B_Defense");

        // ═══════════ KARŞIT ÇİFT ═══════════
        var pair = ScriptableObject.CreateInstance<OpposingPairSO>();
        pair.axisA = axisA;
        pair.axisB = axisB;
        SaveAsset(pair, "Pair_AB");

        // ═══════════ FORM OVERLAY ═══════════
        var form = ScriptableObject.CreateInstance<FormOverlaySO>();
        form.formId = "form_essence";
        form.positiveName = "Form-A (Alpha)";
        form.negativeName = "Form-B (Beta)";
        form.maxAffinity = 5;
        form.regionGates = new RegionGate[]
        {
            new RegionGate { regionTag = "alpha_path", direction = FormDirection.Positive, minimumAffinity = 2 },
            new RegionGate { regionTag = "beta_path",  direction = FormDirection.Negative, minimumAffinity = 2 }
        };
        SaveAsset(form, "Form_Essence");

        // ═══════════ DATABASE ═══════════
        var db = ScriptableObject.CreateInstance<AxisDatabaseSO>();
        db.allAxes = new ProgressionAxisSO[] { axisA, axisB };
        db.opposingPairs = new OpposingPairSO[] { pair };
        db.formOverlays = new FormOverlaySO[] { form };
        SaveAsset(db, "TestDatabase");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("<color=green>[SkillTreeTestSetup] Tüm örnek SO'lar oluşturuldu!</color> " +
                  "Konum: " + BasePath);
        Debug.Log("  2 Eksen (6 + 4 = 10 node), 1 Karşıt Çift, 1 Form Overlay, 1 Database");
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

    private static void AddEffect(SkillNodeSO node, EffectType type, string key, ModifierOp op, float value)
    {
        var list = new System.Collections.Generic.List<NodeEffect>(node.effects);
        list.Add(new NodeEffect
        {
            type = type,
            key = key,
            modifierOp = op,
            numericValue = value
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

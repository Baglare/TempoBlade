#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

public static class DashSkillTreeSetup
{
    private const string BasePath = "Assets/_Project/Data/SkillTree_Dash";

    [MenuItem("Tools/TempoBlade/SkillTree/Create Dash Tree (T1+T2)")]
    public static void CreateDashTree()
    {
        EnsureFolder(BasePath);
        EnsureFolder($"{BasePath}/Nodes");

        // Create config asset
        DashSkillConfigSO config = ScriptableObject.CreateInstance<DashSkillConfigSO>();
        AssetDatabase.CreateAsset(config, $"{BasePath}/DashSkillConfig.asset");

        // Tier 1
        SkillNodeSO t1Ranged = CreateNode("dash_t1_ranged_dodge", "T1 - Menzilli Kacinma", 1);
        SkillNodeSO t1Melee = CreateNode("dash_t1_melee_dodge", "T1 - Yakin Kacinma", 1);
        SkillNodeSO t1Counter = CreateNode("dash_t1_counter", "T1 - Karsi Saldiri", 1);
        SkillNodeSO t1Tempo = CreateNode("dash_t1_tempo_gain", "T1 - Tempo Kazanci", 1);
        SkillNodeSO t1AtkSpeed = CreateNode("dash_t1_attack_speed", "T1 - Saldiri Hizi", 1);

        t1Counter.prerequisites = new[] { t1Ranged, t1Melee };
        t1AtkSpeed.prerequisites = new[] { t1Tempo };

        AddFlag(t1Ranged, EffectKeyRegistry.DashT1RangedDodge);
        AddFlag(t1Melee, EffectKeyRegistry.DashT1MeleeDodge);
        AddFlag(t1Counter, EffectKeyRegistry.DashT1Counter);
        AddFlag(t1Tempo, EffectKeyRegistry.DashT1TempoGain);
        AddFlag(t1AtkSpeed, EffectKeyRegistry.DashT1AttackSpeed);

        // Tier 2 Hunter route
        SkillNodeSO hMark = CreateNode("dash_t2_hunter_mark", "T2 - Av Isareti", 2);
        SkillNodeSO hBlind = CreateNode("dash_t2_hunter_blind", "T2 - Kor Nokta Baskisi", 2);
        SkillNodeSO hFlow = CreateNode("dash_t2_hunter_flow", "T2 - Av Etrafinda Akis", 2);
        SkillNodeSO hExec = CreateNode("dash_t2_hunter_exec", "T2 - Infaz Dashi", 2);
        SkillNodeSO hSucc = CreateNode("dash_t2_hunter_succession", "T2 - Av Devri", 2);

        hMark.isCommitmentNode = true;
        hMark.prerequisites = new[] { t1Counter, t1AtkSpeed };
        hBlind.prerequisites = new[] { hMark };
        hFlow.prerequisites = new[] { hMark };
        hExec.prerequisites = new[] { hBlind, hFlow };
        hSucc.prerequisites = new[] { hExec };

        AddFlag(hMark, EffectKeyRegistry.DashT2Selected);
        AddFlag(hMark, EffectKeyRegistry.DashHunterMark);
        AddFlag(hBlind, EffectKeyRegistry.DashHunterBlindSpot);
        AddFlag(hFlow, EffectKeyRegistry.DashHunterFlow);
        AddFlag(hExec, EffectKeyRegistry.DashHunterExecution);
        AddFlag(hSucc, EffectKeyRegistry.DashHunterSuccession);

        // Tier 2 Flow route
        SkillNodeSO fMark = CreateNode("dash_t2_flow_mark", "T2 - Isaretleme Akisi", 2);
        SkillNodeSO fRebound = CreateNode("dash_t2_flow_rebound", "T2 - Geri Sicrama", 2);
        SkillNodeSO fChain = CreateNode("dash_t2_flow_chain", "T2 - Zincir Sekmesi", 2);
        SkillNodeSO fBlackHole = CreateNode("dash_t2_flow_blackhole", "T2 - Kara Delik", 2);
        SkillNodeSO fBlast = CreateNode("dash_t2_flow_blast", "T2 - Patlama Vurusu", 2);

        fMark.isCommitmentNode = true;
        fMark.prerequisites = new[] { t1Counter, t1AtkSpeed };
        fRebound.prerequisites = new[] { fMark };
        fChain.prerequisites = new[] { fMark };
        fBlackHole.prerequisites = new[] { fChain };
        fBlast.prerequisites = new[] { fBlackHole, fRebound };

        AddFlag(fMark, EffectKeyRegistry.DashT2Selected);
        AddFlag(fMark, EffectKeyRegistry.DashFlowMarkStream);
        AddFlag(fRebound, EffectKeyRegistry.DashFlowRebound);
        AddFlag(fChain, EffectKeyRegistry.DashFlowChain);
        AddFlag(fBlackHole, EffectKeyRegistry.DashFlowBlackHole);
        AddFlag(fBlast, EffectKeyRegistry.DashFlowBlast);

        // Dash axis
        ProgressionAxisSO dashAxis = ScriptableObject.CreateInstance<ProgressionAxisSO>();
        dashAxis.axisId = "axis_dash";
        dashAxis.displayName = "Dash";
        dashAxis.nodes = new[]
        {
            t1Ranged, t1Melee, t1Counter, t1Tempo, t1AtkSpeed,
            hMark, hBlind, hFlow, hExec, hSucc,
            fMark, fRebound, fChain, fBlackHole, fBlast
        };
        AssetDatabase.CreateAsset(dashAxis, $"{BasePath}/Axis_Dash.asset");

        // Minimal parry axis (for commitment lock demonstration)
        SkillNodeSO parryT1 = CreateNode("parry_t1_emergency", "T1 - Acil Parry", 1);
        SkillNodeSO parryT2 = CreateNode("parry_t2_mastery", "T2 - Parry Ustaligi", 2);
        parryT2.prerequisites = new[] { parryT1 };
        parryT2.isCommitmentNode = true;

        ProgressionAxisSO parryAxis = ScriptableObject.CreateInstance<ProgressionAxisSO>();
        parryAxis.axisId = "axis_parry";
        parryAxis.displayName = "Parry";
        parryAxis.nodes = new[] { parryT1, parryT2 };
        AssetDatabase.CreateAsset(parryAxis, $"{BasePath}/Axis_Parry.asset");

        OpposingPairSO pair = ScriptableObject.CreateInstance<OpposingPairSO>();
        pair.axisA = dashAxis;
        pair.axisB = parryAxis;
        AssetDatabase.CreateAsset(pair, $"{BasePath}/Pair_Dash_Parry.asset");

        AxisDatabaseSO db = ScriptableObject.CreateInstance<AxisDatabaseSO>();
        db.allAxes = new[] { dashAxis, parryAxis };
        db.opposingPairs = new[] { pair };
        db.formOverlays = new FormOverlaySO[0];
        AssetDatabase.CreateAsset(db, $"{BasePath}/DashSkillDatabase.asset");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.FocusProjectWindow();
        Selection.activeObject = db;
        Debug.Log("[DashSkillTreeSetup] Dash skill tree assets olusturuldu.");
    }

    private static SkillNodeSO CreateNode(string id, string displayName, int tier)
    {
        SkillNodeSO node = ScriptableObject.CreateInstance<SkillNodeSO>();
        node.nodeId = id;
        node.displayName = displayName;
        node.description = displayName;
        node.tier = tier;
        node.visibility = NodeVisibility.AlwaysVisible;
        node.effects = new NodeEffect[0];

        string path = $"{BasePath}/Nodes/{id}.asset";
        AssetDatabase.CreateAsset(node, path);
        return AssetDatabase.LoadAssetAtPath<SkillNodeSO>(path);
    }

    private static void AddFlag(SkillNodeSO node, string key)
    {
        NodeEffect[] old = node.effects ?? new NodeEffect[0];
        NodeEffect[] next = new NodeEffect[old.Length + 1];
        for (int i = 0; i < old.Length; i++) next[i] = old[i];
        next[old.Length] = new NodeEffect
        {
            type = EffectType.Flag,
            key = key,
            modifierOp = ModifierOp.Flat,
            numericValue = 0f
        };
        node.effects = next;
        EditorUtility.SetDirty(node);
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path)?.Replace("\\", "/");
        string leaf = Path.GetFileName(path);
        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, leaf);
    }
}
#endif


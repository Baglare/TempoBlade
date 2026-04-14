#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class ParrySkillTreeSetup
{
    private const string BasePath = "Assets/_Project/Data/SkillTree_Parry";
    private const string DatabasePath = "Assets/_Project/MainDatabase.asset";
    private const string DashAxisPath = "Assets/_Project/Data/SkillTree_Dash/DashAxis.asset";
    private const string OpposingPairPath = BasePath + "/ParryVsDash.asset";

    [MenuItem("TempoBlade/Skill Tree/Parry Agaci Asset'leri Olustur")]
    public static void CreateParryTreeAssets()
    {
        EnsureFolder(BasePath);
        EnsureFolder(BasePath + "/T1");
        EnsureFolder(BasePath + "/T2_Commitment");
        EnsureFolder(BasePath + "/T2_Ballistic");
        EnsureFolder(BasePath + "/T2_Perfectionist");

        var t1Reflect = CreateOrUpdateNode("T1/Parry_T1_Reflect", "parry_t1_reflect",
            "Yansitma", "Projectile parry/deflect acilir.", 1);
        AddSingleFlagEffect(t1Reflect, EffectKeyRegistry.ParryReflect);

        var t1PerfectTiming = CreateOrUpdateNode("T1/Parry_T1_PerfectTiming", "parry_t1_perfect_timing",
            "Kusursuz Zamanlama", "Parry penceresinin basinda ayri bir perfect parry alt penceresi acilir.", 1);
        AddSingleFlagEffect(t1PerfectTiming, EffectKeyRegistry.ParryPerfectTiming);

        var t1CounterStance = CreateOrUpdateNode("T1/Parry_T1_CounterStance", "parry_t1_counter_stance",
            "Karsilik Durusu", "Basarili parry sonrasinda bonus hasarli counter penceresi acar.", 1,
            t1Reflect);
        AddSingleFlagEffect(t1CounterStance, EffectKeyRegistry.ParryCounterStance);

        var t1PerfectBreak = CreateOrUpdateNode("T1/Parry_T1_PerfectBreak", "parry_t1_perfect_break",
            "Kusursuz Kirilma", "Perfect melee parry agir stun uygular; boss'ta saldiriyi bozar.", 1,
            t1PerfectTiming);
        AddSingleFlagEffect(t1PerfectBreak, EffectKeyRegistry.ParryPerfectBreak);

        var t1RhythmReturn = CreateOrUpdateNode("T1/Parry_T1_RhythmReturn", "parry_t1_rhythm_return",
            "Ritim Iadesi", "Basarili parry recovery'nin bir kismini geri alir; perfect parry ekstra iade ve tempo verir.", 1,
            t1CounterStance, t1PerfectBreak);
        AddSingleFlagEffect(t1RhythmReturn, EffectKeyRegistry.ParryRhythmReturn);

        var t2Commitment = CreateOrUpdateNode("T2_Commitment/Parry_T2_Commitment", "parry_t2_commitment",
            "Parry Odagi", "Parry'i ana savunma ritmi olarak secer. Dash zayiflar, Parry guclenir.", 2,
            t1RhythmReturn);
        t2Commitment.isCommitmentNode = true;
        t2Commitment.regionTag = "parry_commitment";
        AddSingleFlagEffect(t2Commitment, EffectKeyRegistry.ParryT2Commitment);
        EditorUtility.SetDirty(t2Commitment);

        var t2bReverseFront = CreateOrUpdateNode("T2_Ballistic/Parry_T2B_ReverseFront", "parry_t2b_reverse_front",
            "Ters Cephe", "On aci daralir, arka tarafta aynali ikinci bir parry yayi acilir.", 2,
            t2Commitment);
        AddSingleFlagEffect(t2bReverseFront, EffectKeyRegistry.ParryReverseFront);

        var t2bOverdeflect = CreateOrUpdateNode("T2_Ballistic/Parry_T2B_Overdeflect", "parry_t2b_overdeflect",
            "Asiri Sekme", "Geri gonderilen projectile daha hizli, daha guclu ve delici olur.", 2,
            t2Commitment);
        AddSingleFlagEffect(t2bOverdeflect, EffectKeyRegistry.ParryOverdeflect);

        var t2bSuppressiveTrace = CreateOrUpdateNode("T2_Ballistic/Parry_T2B_SuppressiveTrace", "parry_t2b_suppressive_trace",
            "Bastirici Iz", "Deflect projectile vurdugu hedefin aksiyonunu bozar.", 2,
            t2bReverseFront);
        AddSingleFlagEffect(t2bSuppressiveTrace, EffectKeyRegistry.ParrySuppressiveTrace);

        var t2bFracturedOrbit = CreateOrUpdateNode("T2_Ballistic/Parry_T2B_FracturedOrbit", "parry_t2b_fractured_orbit",
            "Kirik Yorunge", "Deflect projectile ilk temas sonrasi zayif sekmeler uretir.", 2,
            t2bOverdeflect);
        AddSingleFlagEffect(t2bFracturedOrbit, EffectKeyRegistry.ParryFracturedOrbit);

        var t2bFeedback = CreateOrUpdateNode("T2_Ballistic/Parry_T2B_Feedback", "parry_t2b_feedback",
            "Geri Besleme", "Projectile parry recovery iadesi ve gecici reflect stack'leri verir.", 2,
            t2bSuppressiveTrace, t2bFracturedOrbit);
        AddSingleFlagEffect(t2bFeedback, EffectKeyRegistry.ParryFeedback);

        var t2pCloseExecute = CreateOrUpdateNode("T2_Perfectionist/Parry_T2P_CloseExecute", "parry_t2p_close_execute",
            "Yakin Infaz", "Cok yakindan yapilan perfect projectile parry, aticiyi infaz eder.", 2,
            t2Commitment);
        AddSingleFlagEffect(t2pCloseExecute, EffectKeyRegistry.ParryCloseExecute);

        var t2pFineEdge = CreateOrUpdateNode("T2_Perfectionist/Parry_T2P_FineEdge", "parry_t2p_fine_edge",
            "Ince Kenar", "Perfect pencere buyur, normal pencere cok daralir.", 2,
            t2Commitment);
        AddSingleFlagEffect(t2pFineEdge, EffectKeyRegistry.ParryFineEdge);

        var t2pHeavyRiposte = CreateOrUpdateNode("T2_Perfectionist/Parry_T2P_HeavyRiposte", "parry_t2p_heavy_riposte",
            "Agir Karsilik", "Perfect melee parry daha agir stun ve guard break uygular.", 2,
            t2pFineEdge);
        AddSingleFlagEffect(t2pHeavyRiposte, EffectKeyRegistry.ParryHeavyRiposte);

        var t2pRotatingCone = CreateOrUpdateNode("T2_Perfectionist/Parry_T2P_RotatingCone", "parry_t2p_rotating_cone",
            "Donen Koni", "Parry aktifken koni aim yonunden baslayip hizla doner.", 2,
            t2pFineEdge);
        AddSingleFlagEffect(t2pRotatingCone, EffectKeyRegistry.ParryRotatingCone);

        var t2pPerfectCycle = CreateOrUpdateNode("T2_Perfectionist/Parry_T2P_PerfectCycle", "parry_t2p_perfect_cycle",
            "Kusursuz Dongu", "Perfect parry recovery'yi buyuk olcude geri alir ve counter penceresini yeniler.", 2,
            t2pHeavyRiposte, t2pRotatingCone);
        AddSingleFlagEffect(t2pPerfectCycle, EffectKeyRegistry.ParryPerfectCycle);

        var axis = LoadOrCreateAsset<ProgressionAxisSO>(BasePath + "/ParryAxis.asset", () =>
        {
            var created = ScriptableObject.CreateInstance<ProgressionAxisSO>();
            created.axisId = "axis_parry";
            created.displayName = "Parry Ekseni";
            created.axisColor = new Color(1f, 0.55f, 0.2f);
            return created;
        });

        axis.axisId = "axis_parry";
        axis.displayName = "Parry Ekseni";
        axis.axisColor = new Color(1f, 0.55f, 0.2f);
        axis.nodes = new[]
        {
            t1Reflect, t1PerfectTiming, t1CounterStance, t1PerfectBreak, t1RhythmReturn,
            t2Commitment,
            t2bReverseFront, t2bOverdeflect, t2bSuppressiveTrace, t2bFracturedOrbit, t2bFeedback,
            t2pCloseExecute, t2pFineEdge, t2pHeavyRiposte, t2pRotatingCone, t2pPerfectCycle
        };
        EditorUtility.SetDirty(axis);

        UpdateDatabase(axis);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("<color=green>[ParrySkillTreeSetup] Parry Skill Tree asset'leri hazirlandi.</color>");
    }

    private static void UpdateDatabase(ProgressionAxisSO parryAxis)
    {
        AxisDatabaseSO database = AssetDatabase.LoadAssetAtPath<AxisDatabaseSO>(DatabasePath);
        if (database == null)
        {
            Debug.LogWarning("[ParrySkillTreeSetup] MainDatabase.asset bulunamadi. ParryAxis elle eklenmeli.");
            return;
        }

        var axisList = new List<ProgressionAxisSO>(database.allAxes ?? new ProgressionAxisSO[0]);
        if (!axisList.Contains(parryAxis))
            axisList.Add(parryAxis);
        database.allAxes = axisList.ToArray();
        EditorUtility.SetDirty(database);

        ProgressionAxisSO dashAxis = AssetDatabase.LoadAssetAtPath<ProgressionAxisSO>(DashAxisPath);
        if (dashAxis == null)
        {
            Debug.LogWarning("[ParrySkillTreeSetup] DashAxis.asset bulunamadi. Opposing pair otomatik kurulamadı.");
            return;
        }

        OpposingPairSO pair = LoadOrCreateAsset<OpposingPairSO>(OpposingPairPath, ScriptableObject.CreateInstance<OpposingPairSO>);
        pair.axisA = dashAxis;
        pair.axisB = parryAxis;
        EditorUtility.SetDirty(pair);

        var pairList = new List<OpposingPairSO>(database.opposingPairs ?? new OpposingPairSO[0]);
        if (!pairList.Contains(pair))
            pairList.Add(pair);
        database.opposingPairs = pairList.ToArray();
        EditorUtility.SetDirty(database);
    }

    private static SkillNodeSO CreateOrUpdateNode(string subPath, string id, string displayName, string description, int tier, params SkillNodeSO[] prerequisites)
    {
        SkillNodeSO node = LoadOrCreateAsset<SkillNodeSO>(BasePath + "/" + subPath + ".asset", ScriptableObject.CreateInstance<SkillNodeSO>);
        node.nodeId = id;
        node.displayName = displayName;
        node.description = description;
        node.tier = tier;
        node.prerequisites = prerequisites;
        node.visibility = NodeVisibility.AlwaysVisible;
        node.effects = node.effects ?? new NodeEffect[0];
        node.startsUnlocked = false;
        EditorUtility.SetDirty(node);
        return node;
    }

    private static void AddSingleFlagEffect(SkillNodeSO node, string key)
    {
        node.effects = new[]
        {
            new NodeEffect
            {
                type = EffectType.Flag,
                key = key,
                modifierOp = ModifierOp.Flat,
                numericValue = 0f
            }
        };
        EditorUtility.SetDirty(node);
    }

    private static T LoadOrCreateAsset<T>(string path, System.Func<T> createFunc) where T : ScriptableObject
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset != null)
            return asset;

        asset = createFunc();
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        string parent = Path.GetDirectoryName(path)?.Replace("\\", "/");
        string folder = Path.GetFileName(path);
        if (!string.IsNullOrEmpty(parent))
            EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, folder);
    }
}
#endif

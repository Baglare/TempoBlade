#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class OverdriveCadenceSkillTreeSetup
{
    private const string OverdrivePath = "Assets/_Project/Data/SkillTree_Overdrive";
    private const string CadencePath = "Assets/_Project/Data/SkillTree_Cadence";
    private const string DatabasePath = "Assets/_Project/MainDatabase.asset";
    private const string OpposingPairPath = "Assets/_Project/Data/SkillTree_Overdrive/OverdriveVsCadence.asset";

    public static void CreateTrees()
    {
        ProgressionAxisSO overdriveAxis = CreateOverdrive();
        ProgressionAxisSO cadenceAxis = CreateCadence();
        UpdateDatabase(overdriveAxis, cadenceAxis);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("<color=green>[OverdriveCadenceSkillTreeSetup] Overdrive ve Cadence asset'leri hazirlandi.</color>");
    }

    private static ProgressionAxisSO CreateOverdrive()
    {
        EnsureTreeFolders(OverdrivePath, "T2_Burst", "T2_Predator");

        var heat = CreateOrUpdateNode(OverdrivePath, "T1/Overdrive_T1_HeatBuildup", "overdrive_t1_heat_buildup",
            "Hararet Birikimi", "Kisa araliklarla yapilan ardisik saldirilar giderek daha fazla tempo uretir.", 1);
        AddFlag(heat, EffectKeyRegistry.OverdriveHeatBuildup);

        var threshold = CreateOrUpdateNode(OverdrivePath, "T1/Overdrive_T1_ThresholdBurst", "overdrive_t1_threshold_burst",
            "Esik Patlamasi", "Yeni bir tempo kademesine gecildiginde sonraki saldiri bonus guc ve sendeletme kazanir.", 1);
        AddFlag(threshold, EffectKeyRegistry.OverdriveThresholdBurst);

        var redPressure = CreateOrUpdateNode(OverdrivePath, "T1/Overdrive_T1_RedPressure", "overdrive_t1_red_pressure",
            "Kizil Basinc", "Yuksek tempoda hasar ve stagger artar; karsiliginda kirilganlik hafif artar.", 1);
        AddFlag(redPressure, EffectKeyRegistry.OverdriveRedPressure);

        var overflow = CreateOrUpdateNode(OverdrivePath, "T1/Overdrive_T1_OverflowImpulse", "overdrive_t1_overflow_impulse",
            "Tasan Durtu", "Maksimum tempoya yakinken kucuk kesintiler ritmi daha zor bozar.", 1);
        AddFlag(overflow, EffectKeyRegistry.OverdriveOverflowImpulse);

        var finalPush = CreateOrUpdateNode(OverdrivePath, "T1/Overdrive_T1_FinalPush", "overdrive_t1_final_push",
            "Son Itki", "Yuksek tempoda kill almak kisa sureli guclendirilmis bir sonraki darbe durumu verir.", 1);
        AddFlag(finalPush, EffectKeyRegistry.OverdriveFinalPush);

        var commitment = CreateOrUpdateNode(OverdrivePath, "T2_Commitment/Overdrive_T2_Commitment", "overdrive_t2_commitment",
            "Overdrive Odagi", "Dusuk ve orta tempoda guvenli oyun zayiflar; yuksek tempodaki Overdrive etkileri guclenir.", 2,
            heat, threshold, redPressure, overflow, finalPush);
        commitment.isCommitmentNode = true;
        commitment.regionTag = "overdrive_commitment";
        AddFlag(commitment, EffectKeyRegistry.OverdriveT2Commitment);

        var shortCircuit = CreateOrUpdateNode(OverdrivePath, "T2_Burst/Overdrive_T2Burst_ShortCircuit", "overdrive_t2burst_short_circuit",
            "Kisa Devre", "Yeni bir tempo esigine girmek kisa sureli bir Overdrive Penceresi acar.", 2, commitment);
        AddFlag(shortCircuit, EffectKeyRegistry.OverdriveShortCircuit);

        var redWindow = CreateOrUpdateNode(OverdrivePath, "T2_Burst/Overdrive_T2Burst_RedWindow", "overdrive_t2burst_red_window",
            "Kizil Pencere", "Overdrive Penceresi sirasinda saldiri hizi, hasar ve stagger artar; tempo daha hizli tukenir.", 2, shortCircuit);
        AddFlag(redWindow, EffectKeyRegistry.OverdriveRedWindow);

        var echo = CreateOrUpdateNode(OverdrivePath, "T2_Burst/Overdrive_T2Burst_ThresholdEcho", "overdrive_t2burst_threshold_echo",
            "Esik Yankisi", "Overdrive Penceresi sirasinda kill veya break pencereyi az miktarda uzatir.", 2, redWindow);
        AddFlag(echo, EffectKeyRegistry.OverdriveThresholdEcho);

        var pressureBreak = CreateOrUpdateNode(OverdrivePath, "T2_Burst/Overdrive_T2Burst_PressureBreak", "overdrive_t2burst_pressure_break",
            "Basinc Kopusu", "Overdrive Penceresi sirasinda heavy/counter saldirilar daha guclu break ve stagger uygular.", 2, redWindow);
        AddFlag(pressureBreak, EffectKeyRegistry.OverdrivePressureBreak);

        var finalFlare = CreateOrUpdateNode(OverdrivePath, "T2_Burst/Overdrive_T2Burst_FinalFlare", "overdrive_t2burst_final_flare",
            "Son Parlama", "Pencere bitmeden yapilan son saldiri guclu bir cash-out darbesine donusur.", 2, echo, pressureBreak);
        AddFlag(finalFlare, EffectKeyRegistry.OverdriveFinalFlare);

        var bloodScent = CreateOrUpdateNode(OverdrivePath, "T2_Predator/Overdrive_T2Pred_BloodScent", "overdrive_t2pred_blood_scent",
            "Kan Kokusu", "Surekli baski goren hedef zamanla av olarak isaretlenir.", 2, commitment);
        AddFlag(bloodScent, EffectKeyRegistry.OverdriveBloodScent);

        var proximity = CreateOrUpdateNode(OverdrivePath, "T2_Predator/Overdrive_T2Pred_ChokingProximity", "overdrive_t2pred_choking_proximity",
            "Bogucu Yakinlik", "Av hedefin yakinindayken tempo kaybi azalir ve baski daha verimli hale gelir.", 2, bloodScent);
        AddFlag(proximity, EffectKeyRegistry.OverdriveChokingProximity);

        var angle = CreateOrUpdateNode(OverdrivePath, "T2_Predator/Overdrive_T2Pred_PredatorAngle", "overdrive_t2pred_predator_angle",
            "Yirtici Aci", "Avin yanindan veya arkasindan yapilan saldirilar bonus stagger ve hasar kazanir.", 2, bloodScent);
        AddFlag(angle, EffectKeyRegistry.OverdrivePredatorAngle);

        var packBreaker = CreateOrUpdateNode(OverdrivePath, "T2_Predator/Overdrive_T2Pred_PackBreaker", "overdrive_t2pred_pack_breaker",
            "Suruyu Yarma", "Av oldugunde agresif durum yakindaki uygun bir hedefe aktarilabilir.", 2, proximity, angle);
        AddFlag(packBreaker, EffectKeyRegistry.OverdrivePackBreaker);

        var executePressure = CreateOrUpdateNode(OverdrivePath, "T2_Predator/Overdrive_T2Pred_ExecutePressure", "overdrive_t2pred_execute_pressure",
            "Infaz Baskisi", "Dusen canli ava karsi counter/heavy saldirilar cok guclu bitirici baski kazanir.", 2, packBreaker);
        AddFlag(executePressure, EffectKeyRegistry.OverdriveExecutePressure);

        var axis = LoadOrCreateAsset<ProgressionAxisSO>(OverdrivePath + "/OverdriveAxis.asset", ScriptableObject.CreateInstance<ProgressionAxisSO>);
        axis.axisId = "axis_overdrive";
        axis.displayName = "Overdrive";
        axis.axisColor = new Color(1f, 0.25f, 0.12f);
        axis.nodes = new[]
        {
            heat, threshold, redPressure, overflow, finalPush,
            commitment,
            shortCircuit, redWindow, echo, pressureBreak, finalFlare,
            bloodScent, proximity, angle, packBreaker, executePressure
        };
        EditorUtility.SetDirty(axis);
        return axis;
    }

    private static ProgressionAxisSO CreateCadence()
    {
        EnsureTreeFolders(CadencePath, "T2_Measured", "T2_Flow");

        var steadyPulse = CreateOrUpdateNode(CadencePath, "T1/Cadence_T1_SteadyPulse", "cadence_t1_steady_pulse",
            "Sabit Nabiz", "Tempo kaybi hissedilir bicimde azalir.", 1);
        AddFlag(steadyPulse, EffectKeyRegistry.CadenceSteadyPulse);

        var transitionRhythm = CreateOrUpdateNode(CadencePath, "T1/Cadence_T1_TransitionRhythm", "cadence_t1_transition_rhythm",
            "Gecis Ritmi", "Farkli aksiyon tipleri arasinda duzgun gecis yapmak tempoyu daha verimli tasir.", 1);
        AddFlag(transitionRhythm, EffectKeyRegistry.CadenceTransitionRhythm);

        var softFall = CreateOrUpdateNode(CadencePath, "T1/Cadence_T1_SoftFall", "cadence_t1_soft_fall",
            "Yumusak Dusus", "Tempo kademesi duserken bonuslar kisa bir tolerans penceresiyle tamamen kopmaz.", 1);
        AddFlag(softFall, EffectKeyRegistry.CadenceSoftFall);

        var measuredPower = CreateOrUpdateNode(CadencePath, "T1/Cadence_T1_MeasuredPower", "cadence_t1_measured_power",
            "Olculu Guc", "Ayni tempo kademesinde dengeli kalindiginda stabil saldiri ve kontrol bonuslari olusur.", 1);
        AddFlag(measuredPower, EffectKeyRegistry.CadenceMeasuredPower);

        var rhythmShield = CreateOrUpdateNode(CadencePath, "T1/Cadence_T1_RhythmShield", "cadence_t1_rhythm_shield",
            "Ritim Kalkani", "Kucuk hatalar, chip hasar ve minor aksakliklar tempoyu daha az bozar.", 1);
        AddFlag(rhythmShield, EffectKeyRegistry.CadenceRhythmShield);

        var commitment = CreateOrUpdateNode(CadencePath, "T2_Commitment/Cadence_T2_Commitment", "cadence_t2_commitment",
            "Cadence Odagi", "Patlayici esik anlari zayiflar; tempo dususu yumusar ve Cadence etkileri daha stabil hale gelir.", 2,
            steadyPulse, transitionRhythm, softFall, measuredPower, rhythmShield);
        commitment.isCommitmentNode = true;
        commitment.regionTag = "cadence_commitment";
        AddFlag(commitment, EffectKeyRegistry.CadenceT2Commitment);

        var measureLine = CreateOrUpdateNode(CadencePath, "T2_Measured/Cadence_T2Measured_MeasureLine", "cadence_t2measured_measure_line",
            "Olcu Cizgisi", "Her tempo kademesinde stabil bolge olusur; bu bolgede Cadence bonuslari daha kararli calisir.", 2, commitment);
        AddFlag(measureLine, EffectKeyRegistry.CadenceMeasureLine);

        var balancePoint = CreateOrUpdateNode(CadencePath, "T2_Measured/Cadence_T2Measured_BalancePoint", "cadence_t2measured_balance_point",
            "Denge Noktasi", "Stabil bolgede kalindikca precision, counter kalitesi ve kontrol etkileri guclenir.", 2, measureLine);
        AddFlag(balancePoint, EffectKeyRegistry.CadenceBalancePoint);

        var timedAccent = CreateOrUpdateNode(CadencePath, "T2_Measured/Cadence_T2Measured_TimedAccent", "cadence_t2measured_timed_accent",
            "Zamanli Vurgu", "Dogru ritim araliklariyla yapilan aksiyonlar guclendirilmis vurgu darbesi uretir.", 2, measureLine);
        AddFlag(timedAccent, EffectKeyRegistry.CadenceTimedAccent);

        var recoveryReturn = CreateOrUpdateNode(CadencePath, "T2_Measured/Cadence_T2Measured_RecoveryReturn", "cadence_t2measured_recovery_return",
            "Geri Toparlanma", "Stabil bolgedeyken ritim bozulursa oyuncu tekrar dengeye daha kolay doner.", 2, balancePoint, timedAccent);
        AddFlag(recoveryReturn, EffectKeyRegistry.CadenceRecoveryReturn);

        var perfectMeasure = CreateOrUpdateNode(CadencePath, "T2_Measured/Cadence_T2Measured_PerfectMeasure", "cadence_t2measured_perfect_measure",
            "Kusursuz Olcu", "Uzun sure ritim bozmadan oynanirsa periyodik kontrollu oduller verir.", 2, recoveryReturn);
        AddFlag(perfectMeasure, EffectKeyRegistry.CadencePerfectMeasure);

        var flowRing = CreateOrUpdateNode(CadencePath, "T2_Flow/Cadence_T2Flow_FlowRing", "cadence_t2flow_flow_ring",
            "Akis Halkasi", "Farkli aksiyon turlerini art arda baglamak Flow stack uretir.", 2, commitment);
        AddFlag(flowRing, EffectKeyRegistry.CadenceFlowRing);

        var sliding = CreateOrUpdateNode(CadencePath, "T2_Flow/Cadence_T2Flow_SlidingContinuation", "cadence_t2flow_sliding_continuation",
            "Kayar Devam", "Hedef degistirirken, bosluk kapatirken veya hareket halindeyken Flow stack'leri daha zor duser.", 2, flowRing);
        AddFlag(sliding, EffectKeyRegistry.CadenceSlidingContinuation);

        var wave = CreateOrUpdateNode(CadencePath, "T2_Flow/Cadence_T2Flow_WaveBounce", "cadence_t2flow_wave_bounce",
            "Dalga Sekmesi", "Yuksek Flow durumunda bazi saldirilar kucuk yankili sekme etkileri uretir.", 2, flowRing);
        AddFlag(wave, EffectKeyRegistry.CadenceWaveBounce);

        var surf = CreateOrUpdateNode(CadencePath, "T2_Flow/Cadence_T2Flow_ThresholdSurf", "cadence_t2flow_threshold_surf",
            "Esik Sorfu", "Tempo kademe dustugunde bonuslarin bir kismi asagi kademeye tasinir.", 2, sliding, wave);
        AddFlag(surf, EffectKeyRegistry.CadenceThresholdSurf);

        var harmony = CreateOrUpdateNode(CadencePath, "T2_Flow/Cadence_T2Flow_OverflowHarmony", "cadence_t2flow_overflow_harmony",
            "Taskin Uyum", "Uzun Flow zincirlerinde recovery iadesi ve kucuk tempo geri beslemesi olusur.", 2, surf);
        AddFlag(harmony, EffectKeyRegistry.CadenceOverflowHarmony);

        var axis = LoadOrCreateAsset<ProgressionAxisSO>(CadencePath + "/CadenceAxis.asset", ScriptableObject.CreateInstance<ProgressionAxisSO>);
        axis.axisId = "axis_cadence";
        axis.displayName = "Cadence";
        axis.axisColor = new Color(0.35f, 0.95f, 0.78f);
        axis.nodes = new[]
        {
            steadyPulse, transitionRhythm, softFall, measuredPower, rhythmShield,
            commitment,
            measureLine, balancePoint, timedAccent, recoveryReturn, perfectMeasure,
            flowRing, sliding, wave, surf, harmony
        };
        EditorUtility.SetDirty(axis);
        return axis;
    }

    private static void UpdateDatabase(ProgressionAxisSO overdriveAxis, ProgressionAxisSO cadenceAxis)
    {
        AxisDatabaseSO database = AssetDatabase.LoadAssetAtPath<AxisDatabaseSO>(DatabasePath);
        if (database == null)
        {
            Debug.LogWarning("[OverdriveCadenceSkillTreeSetup] MainDatabase.asset bulunamadi. Axis'leri elle eklemelisin.");
            return;
        }

        var axes = new List<ProgressionAxisSO>(database.allAxes ?? new ProgressionAxisSO[0]);
        if (!axes.Contains(overdriveAxis)) axes.Add(overdriveAxis);
        if (!axes.Contains(cadenceAxis)) axes.Add(cadenceAxis);
        database.allAxes = axes.ToArray();

        OpposingPairSO pair = LoadOrCreateAsset<OpposingPairSO>(OpposingPairPath, ScriptableObject.CreateInstance<OpposingPairSO>);
        pair.axisA = overdriveAxis;
        pair.axisB = cadenceAxis;
        EditorUtility.SetDirty(pair);

        var pairs = new List<OpposingPairSO>(database.opposingPairs ?? new OpposingPairSO[0]);
        if (!pairs.Contains(pair)) pairs.Add(pair);
        database.opposingPairs = pairs.ToArray();
        EditorUtility.SetDirty(database);
    }

    private static SkillNodeSO CreateOrUpdateNode(string basePath, string subPath, string id, string displayName, string description, int tier, params SkillNodeSO[] prerequisites)
    {
        SkillNodeSO node = LoadOrCreateAsset<SkillNodeSO>(basePath + "/" + subPath + ".asset", ScriptableObject.CreateInstance<SkillNodeSO>);
        node.nodeId = id;
        node.displayName = displayName;
        node.description = description;
        node.tier = tier;
        node.prerequisites = prerequisites;
        node.visibility = NodeVisibility.AlwaysVisible;
        node.startsUnlocked = false;
        node.isCommitmentNode = false;
        node.regionTag = "";
        EditorUtility.SetDirty(node);
        return node;
    }

    private static void AddFlag(SkillNodeSO node, string key)
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

    private static void EnsureTreeFolders(string root, params string[] routeFolders)
    {
        EnsureFolder(root);
        EnsureFolder(root + "/T1");
        EnsureFolder(root + "/T2_Commitment");
        foreach (var routeFolder in routeFolders)
            EnsureFolder(root + "/" + routeFolder);
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

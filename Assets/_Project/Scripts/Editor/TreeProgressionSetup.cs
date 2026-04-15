#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

public static class TreeProgressionSetup
{
    private const string ConfigFolder = "Assets/_Project/Data/SkillTree_Progression";
    private const string ConfigPath = ConfigFolder + "/TreeProgressionConfig.asset";

    [MenuItem("TempoBlade/Skill Tree/Progression Config Olustur ve Rank Gate Ata")]
    public static void CreateProgressionConfigAndRanks()
    {
        if (!Directory.Exists(ConfigFolder))
            Directory.CreateDirectory(ConfigFolder);

        var config = AssetDatabase.LoadAssetAtPath<TreeProgressionConfigSO>(ConfigPath);
        if (config == null)
        {
            config = ScriptableObject.CreateInstance<TreeProgressionConfigSO>();
            AssetDatabase.CreateAsset(config, ConfigPath);
        }

        string[] axisGuids = AssetDatabase.FindAssets("t:ProgressionAxisSO", new[] { "Assets/_Project/Data" });
        foreach (string guid in axisGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var axis = AssetDatabase.LoadAssetAtPath<ProgressionAxisSO>(path);
            if (axis == null || axis.nodes == null)
                continue;

            AssignRanks(axis);
        }

        AxisProgressionManager manager = Object.FindFirstObjectByType<AxisProgressionManager>();
        if (manager != null)
        {
            manager.progressionConfig = config;
            EditorUtility.SetDirty(manager);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[TreeProgressionSetup] Config olusturuldu ve node rank gate degerleri atandi.");
    }

    private static void AssignRanks(ProgressionAxisSO axis)
    {
        int tier1Index = 0;
        if (axis.nodes == null)
            return;

        foreach (var node in axis.nodes)
        {
            if (node == null)
                continue;

            int rank = InferRank(axis, node, ref tier1Index);
            if (node.requiredTreeRank == rank)
                continue;

            node.requiredTreeRank = rank;
            EditorUtility.SetDirty(node);
        }
    }

    private static int InferRank(ProgressionAxisSO axis, SkillNodeSO node, ref int tier1Index)
    {
        if (node.tier <= 1)
        {
            tier1Index++;
            return Mathf.Clamp(tier1Index, 1, 5);
        }

        if (node.isCommitmentNode)
            return 6;

        if (node.tier == 2)
            return Mathf.Clamp(7 + GetRouteIndex(axis, node), 7, 11);

        return 12;
    }

    private static int GetRouteIndex(ProgressionAxisSO axis, SkillNodeSO node)
    {
        string prefix = GetRoutePrefix(node.nodeId);
        if (axis == null || axis.nodes == null || string.IsNullOrEmpty(prefix))
            return 0;

        int index = 0;
        foreach (var candidate in axis.nodes)
        {
            if (candidate == null)
                continue;

            if (candidate == node)
                return index;

            if (!string.IsNullOrEmpty(candidate.nodeId) && candidate.nodeId.Contains(prefix))
                index++;
        }

        return index;
    }

    private static string GetRoutePrefix(string nodeId)
    {
        if (string.IsNullOrEmpty(nodeId))
            return "";

        string[] prefixes =
        {
            "dash_t2h_",
            "dash_t2f_",
            "parry_t2b_",
            "parry_t2p_",
            "overdrive_t2burst_",
            "overdrive_t2pred_",
            "cadence_t2measured_",
            "cadence_t2flow_"
        };

        foreach (string prefix in prefixes)
        {
            if (nodeId.Contains(prefix))
                return prefix;
        }

        return "";
    }
}
#endif


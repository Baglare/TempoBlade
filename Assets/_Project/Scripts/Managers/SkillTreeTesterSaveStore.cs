using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class SkillTreeTesterSaveStore
{
    private const string FolderName = "TempoBlade";
    private const string FileName = "skill_tree_tester.json";

    private static string SaveDirectory => Path.Combine(
        System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments),
        FolderName);

    private static string SaveFilePath => Path.Combine(SaveDirectory, FileName);

    public static HashSet<string> Load()
    {
        if (!File.Exists(SaveFilePath))
            return new HashSet<string>();

        string json = File.ReadAllText(SaveFilePath);
        var data = JsonUtility.FromJson<TesterSkillTreeSave>(json);
        return data != null && data.unlockedNodeIds != null
            ? new HashSet<string>(data.unlockedNodeIds)
            : new HashSet<string>();
    }

    public static void Save(HashSet<string> unlockedNodeIds)
    {
        if (!Directory.Exists(SaveDirectory))
            Directory.CreateDirectory(SaveDirectory);

        var data = new TesterSkillTreeSave
        {
            unlockedNodeIds = unlockedNodeIds != null
                ? new List<string>(unlockedNodeIds)
                : new List<string>()
        };

        File.WriteAllText(SaveFilePath, JsonUtility.ToJson(data, true));
    }
}


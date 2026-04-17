#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

public static class AudioSetupMenu
{
    private const string CatalogFolder = "Assets/_Project/Resources/Audio";
    private const string CatalogPath = CatalogFolder + "/DefaultAudioCueCatalog.asset";

    [MenuItem("TempoBlade/Audio/Varsayilan Audio Catalog Olustur")]
    public static void CreateDefaultCatalog()
    {
        if (!AssetDatabase.IsValidFolder("Assets/_Project/Resources"))
            AssetDatabase.CreateFolder("Assets/_Project", "Resources");

        if (!AssetDatabase.IsValidFolder(CatalogFolder))
            AssetDatabase.CreateFolder("Assets/_Project/Resources", "Audio");

        AudioCueCatalogSO catalog = AssetDatabase.LoadAssetAtPath<AudioCueCatalogSO>(CatalogPath);
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<AudioCueCatalogSO>();
            catalog.cues = BuildDefaultDefinitions();
            AssetDatabase.CreateAsset(catalog, CatalogPath);
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        Selection.activeObject = catalog;
        EditorGUIUtility.PingObject(catalog);
        Debug.Log("[AudioSetup] Default audio catalog hazirlandi: " + CatalogPath);
    }

    private static AudioCueDefinition[] BuildDefaultDefinitions()
    {
        AudioEventId[] ids = (AudioEventId[])System.Enum.GetValues(typeof(AudioEventId));
        AudioCueDefinition[] defs = new AudioCueDefinition[ids.Length - 1];
        int index = 0;

        foreach (AudioEventId id in ids)
        {
            if (id == AudioEventId.None)
                continue;

            defs[index++] = new AudioCueDefinition
            {
                eventId = id,
                clips = new AudioClip[0],
                volume = 1f,
                pitch = 1f,
                pitchRandomRange = 0.05f,
                volumeRandomRange = 0f,
                spatialBlend = GuessSpatialBlend(id),
                minDistance = 1f,
                maxDistance = 14f,
                cooldown = GuessCooldown(id),
                loop = IsLoopEvent(id),
                followTarget = IsLoopEvent(id)
            };
        }

        return defs;
    }

    private static float GuessSpatialBlend(AudioEventId id)
    {
        switch (id)
        {
            case AudioEventId.UIUnlock:
            case AudioEventId.UIFail:
                return 0f;
            default:
                return 0.7f;
        }
    }

    private static float GuessCooldown(AudioEventId id)
    {
        switch (id)
        {
            case AudioEventId.EnemyHurt:
                return 0.05f;
            case AudioEventId.EnemyStun:
                return 0.1f;
            case AudioEventId.ProjectileLaunch:
                return 0.02f;
            case AudioEventId.PlayerHit:
                return 0.03f;
            default:
                return 0f;
        }
    }

    private static bool IsLoopEvent(AudioEventId id)
    {
        return id == AudioEventId.MechanicBlackHoleLoop;
    }
}
#endif

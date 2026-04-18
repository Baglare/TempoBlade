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
                volume = GuessVolume(id),
                pitch = 1f,
                pitchRandomRange = GuessPitchRandomRange(id),
                volumeRandomRange = 0f,
                spatialBlend = GuessSpatialBlend(id),
                minDistance = 1f,
                maxDistance = GuessMaxDistance(id),
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
            case AudioEventId.PlayerDash:
            case AudioEventId.PlayerParryStart:
            case AudioEventId.PlayerParry:
            case AudioEventId.PlayerPerfectParry:
            case AudioEventId.PlayerDeflect:
            case AudioEventId.PlayerParryFail:
            case AudioEventId.PlayerCounter:
            case AudioEventId.UIUnlock:
            case AudioEventId.UIFail:
                return 0f;
            case AudioEventId.PlayerAttack:
            case AudioEventId.PlayerHit:
            case AudioEventId.PlayerWhiff:
            case AudioEventId.PlayerDamageTaken:
            case AudioEventId.PlayerDeath:
            case AudioEventId.PlayerFinisher:
            case AudioEventId.PlayerHeal:
            case AudioEventId.MechanicFlowMark:
            case AudioEventId.MechanicSnapback:
                return 0.2f;
            default:
                return 0.8f;
        }
    }

    private static float GuessCooldown(AudioEventId id)
    {
        switch (id)
        {
            case AudioEventId.PlayerDash:
                return 0.05f;
            case AudioEventId.PlayerParryStart:
            case AudioEventId.PlayerParry:
            case AudioEventId.PlayerParryFail:
            case AudioEventId.PlayerDeflect:
                return 0.03f;
            case AudioEventId.PlayerPerfectParry:
                return 0.04f;
            case AudioEventId.PlayerCounter:
                return 0.04f;
            case AudioEventId.EnemyHurt:
                return 0.05f;
            case AudioEventId.EnemyStun:
                return 0.1f;
            case AudioEventId.ProjectileLaunch:
                return 0.02f;
            case AudioEventId.ProjectileDeflect:
            case AudioEventId.ProjectileHit:
                return 0.03f;
            case AudioEventId.PlayerHit:
                return 0.03f;
            default:
                return 0f;
        }
    }

    private static float GuessVolume(AudioEventId id)
    {
        switch (id)
        {
            case AudioEventId.PlayerPerfectParry:
            case AudioEventId.EnemyBossPhaseTransition:
            case AudioEventId.MechanicBurst:
                return 1f;
            case AudioEventId.UIUnlock:
            case AudioEventId.UIFail:
                return 0.9f;
            case AudioEventId.PlayerParryStart:
            case AudioEventId.PlayerParry:
            case AudioEventId.PlayerDeflect:
            case AudioEventId.PlayerParryFail:
                return 0.95f;
            default:
                return 1f;
        }
    }

    private static float GuessPitchRandomRange(AudioEventId id)
    {
        switch (id)
        {
            case AudioEventId.PlayerPerfectParry:
            case AudioEventId.UIUnlock:
            case AudioEventId.UIFail:
                return 0.03f;
            case AudioEventId.MechanicBlackHoleLoop:
                return 0.01f;
            default:
                return 0.05f;
        }
    }

    private static float GuessMaxDistance(AudioEventId id)
    {
        switch (id)
        {
            case AudioEventId.PlayerDash:
            case AudioEventId.PlayerParryStart:
            case AudioEventId.PlayerParry:
            case AudioEventId.PlayerPerfectParry:
            case AudioEventId.PlayerDeflect:
            case AudioEventId.PlayerParryFail:
            case AudioEventId.PlayerCounter:
            case AudioEventId.UIUnlock:
            case AudioEventId.UIFail:
                return 10f;
            case AudioEventId.MechanicBlackHoleStart:
            case AudioEventId.MechanicBlackHoleLoop:
            case AudioEventId.MechanicBlackHoleEnd:
            case AudioEventId.EnemyBossPhaseTransition:
                return 18f;
            default:
                return 14f;
        }
    }

    private static bool IsLoopEvent(AudioEventId id)
    {
        return id == AudioEventId.MechanicBlackHoleLoop;
    }
}
#endif

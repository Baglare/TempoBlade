using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public enum EliteSpawnMode
{
    NormalOnly = 0,
    UsePrefabDefault = 1,
    ForceElite = 2,
    ChanceBased = 3
}

[CreateAssetMenu(fileName = "New Room", menuName = "TempoBlade/Room")]
public class RoomSO : ScriptableObject
{
    public string roomName;
    
    [Tooltip("Odanin fiziksel geometri ve noktalarini tasiyan Prefab. Uzerinde RoomLayout bileseni olmali.")]
    public GameObject roomPrefab;
    
    [Header("Encounter Config")]
    public List<RoomWave> waves;
    public bool isBossRoom;
    public EncounterType encounterType = EncounterType.Normal;
    public DifficultyTier difficulty = DifficultyTier.Normal;
    
    [Header("Rewards")]
    public int goldReward;
    public WeaponSO possibleWeaponDrop;
}

[System.Serializable]
public class RoomWave
{
    [Tooltip("Wave baslamadan onceki bekleme suresi")]
    public float waveDelay = 1.0f;
    
    [Tooltip("Bu wave icinde cikacak dusman gruplari (Ayni anda cikabilirler)")]
    public List<EnemySpawn> enemyGroups;
}

[System.Serializable]
public class EnemySpawn
{
    public EnemySO enemyType;
    [Tooltip("NormalOnly: her zaman normal. UsePrefabDefault: prefab neyse o. ForceElite: her zaman elite. ChanceBased: yuzdelik olasilikla elite.")]
    public EliteSpawnMode eliteSpawnMode = EliteSpawnMode.NormalOnly;
    [Range(0f, 1f)]
    [Tooltip("ChanceBased modunda elite dogma olasiligi.")]
    public float eliteChance = 0.15f;
    [HideInInspector] public bool isElite;
    [Tooltip("ForceElite veya ChanceBased modunda kullanılacak elite profile. Bos ise prefab ustundeki varsayilan elite profile kullanilir.")]
    public EliteProfileSO eliteProfile;
    public int count;
    [Tooltip("Bu gruptaki dusmanlarin tek tek basilma araligi")]
    public float spawnDelay = 0.5f;

    public bool ShouldSpawnElite()
    {
        if (isElite && eliteSpawnMode == EliteSpawnMode.NormalOnly)
            return true;

        switch (eliteSpawnMode)
        {
            case EliteSpawnMode.UsePrefabDefault:
                return false;
            case EliteSpawnMode.ForceElite:
                return true;
            case EliteSpawnMode.ChanceBased:
                return Random.value <= eliteChance;
            case EliteSpawnMode.NormalOnly:
            default:
                return false;
        }
    }
}

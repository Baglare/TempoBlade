using UnityEngine;
using System.Collections.Generic;

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
    public bool isElite;
    public EliteProfileSO eliteProfile;
    public int count;
    [Tooltip("Bu gruptaki dusmanlarin tek tek basilma araligi")]
    public float spawnDelay = 0.5f;
}

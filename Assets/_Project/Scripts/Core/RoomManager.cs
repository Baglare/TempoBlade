using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class RoomManager : MonoBehaviour
{
    public static RoomManager Instance { get; private set; }

    [Header("Config")]
    public RoomSO currentRoomData;
    
    // Eski Inspector verileri silindi (spawnPoints, portalSpawnPoints)
    // Artik Layout uzerinden okuyacagiz
    private RoomLayout currentRoomLayout;

    [Header("Rewards Setup")]
    [Tooltip("Kapılara rastgele dağıtılacak tüm olası ödül SO'ları (Inspector'dan sürükle)")]
    public RewardDefinitionSO[] possibleRewards;
    [Header("Elite Spawn Debug")]
    public EliteSpawnDebugOverrides eliteSpawnDebugOverrides = new EliteSpawnDebugOverrides();

    [Header("State")]
    public bool isRoomActive = false;
    public int currentWaveIndex = 0;
    public List<GameObject> activeEnemies = new List<GameObject>();
    public EliteSpawnWaveMetrics LastEliteSpawnMetrics { get; private set; } = new EliteSpawnWaveMetrics();

    private readonly EliteSpawnHistoryState eliteSpawnHistory = new EliteSpawnHistoryState();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Found duplicate RoomManager! Destroying this one.");
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // Bolum baslangicinda zamani rutine bindir
        if (GameManager.Instance != null)
            GameManager.Instance.SetState(GameManager.GameState.Gameplay);

        // RunManager'dan siradaki odayi cek (Yoksa Inspector'daki kalir)
        if (RunManager.Instance != null)
        {
            RoomSO nextRoom = RunManager.Instance.GetNextRoom();
            if (nextRoom != null)
            {
                currentRoomData = nextRoom;
            }
        }

        if (currentRoomData != null)
        {
            StartRoom();
        }
    }

    public void StartRoom()
    {
        StopAllCoroutines();
        StartCoroutine(StartRoomRoutine());
    }

    private IEnumerator StartRoomRoutine()
    {
        if (currentRoomData == null)
        {
            Debug.LogError("RoomManager: Current Room Data is MISSING!");
            yield break;
        }

        // --- RESET STATE FOR NEW ROOM ---
        activeEnemies.Clear();
        if (RunManager.Instance != null && RunManager.Instance.roomsCleared <= 0)
            eliteSpawnHistory.Clear();

        // Sahnede önceden yerleştirilmiş düşmanları temizle
        EnemyBase[] existingEnemies = FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);
        foreach (var enemy in existingEnemies) Destroy(enemy.gameObject);
        
        // Eski Oda Geometrisini Sil
        if (currentRoomLayout != null)
        {
            Destroy(currentRoomLayout.gameObject);
            currentRoomLayout = null;
        }
        
        // Yeni Odayi Olustur
        if (currentRoomData.roomPrefab != null)
        {
            GameObject roomObj = Instantiate(currentRoomData.roomPrefab, Vector3.zero, Quaternion.identity);
            currentRoomLayout = roomObj.GetComponent<RoomLayout>();
            
            if (currentRoomLayout == null)
            {
                Debug.LogError($"RoomManager: Odanin ({currentRoomData.roomName}) prefabinda RoomLayout Bileseni YOK!");
            }
            else
            {
                // Oyuncuyu yeni odada doğru pozisyona taşı
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    Vector3 spawnPos = GetDirectionalSpawnPosition(currentRoomLayout);
                    
                    Rigidbody2D pRb = player.GetComponent<Rigidbody2D>();
                    if (pRb != null) pRb.position = spawnPos;
                    player.transform.position = spawnPos;
                }
            }
        }
        else
        {
            Debug.LogWarning($"RoomManager: Odanin ({currentRoomData.roomName}) prefab'i bos. Eski veya bos oda kullaniliyor.");
        }

        isRoomActive = true;
        currentWaveIndex = 0;

        EncounterAffinityManager affinityManager = EncounterAffinityManager.EnsureInstance();
        EncounterBreakdownUI breakdownUi = EncounterBreakdownUI.EnsureInstance();
        if (affinityManager.HasPendingBreakdown())
        {
            bool waitingForBreakdown = true;
            breakdownUi.Show(affinityManager.ConsumePendingBreakdown(), () => waitingForBreakdown = false);
            while (waitingForBreakdown)
                yield return null;
        }

        GameObject telemetryPlayer = GameObject.FindGameObjectWithTag("Player");
        affinityManager.StartEncounter(currentRoomData, telemetryPlayer);
        
        // Eğer odanın wave tanımı yoksa direkt odayı tamamla (Örn: Boş Boss odası test için)
        if (currentRoomData.waves == null || currentRoomData.waves.Count == 0 || currentRoomLayout == null)
        {
            RoomCleared();
        }
        else
        {
            StartCoroutine(SpawnWaveRoutine());
        }
    }

    private IEnumerator SpawnWaveRoutine()
    {
        if (currentRoomData == null || currentWaveIndex >= currentRoomData.waves.Count)
        {
            RoomCleared();
            yield break;
        }

        var currentWave = currentRoomData.waves[currentWaveIndex];
        
        // Eger wave bos ise veya grup yoksa direkt es gec, odayi temizle veya siradakine gec
        if (currentWave == null || currentWave.enemyGroups == null || currentWave.enemyGroups.Count == 0)
        {
            OnEnemyDied(null); // Dummy call to advance
            yield break;
        }

        // Wave ana bekleme suresi (Gruplar basilmadan once)
        yield return new WaitForSeconds(currentWave.waveDelay);

        // Müsait (atanmış) spawn noktalarını bul
        System.Collections.Generic.List<Transform> validSpawnPoints = new System.Collections.Generic.List<Transform>();
        if (currentRoomLayout != null && currentRoomLayout.enemySpawnPoints != null)
        {
            foreach (var point in currentRoomLayout.enemySpawnPoints)
            {
                if (point != null) validSpawnPoints.Add(point);
            }
        }

        if (validSpawnPoints.Count == 0)
        {
            Debug.LogError("RoomManager: Oda RoomLayout icinde hicbir gecerli SpawnPoint atanmamis!");
            yield break;
        }

        List<EliteSpawnPlanEntry> spawnPlan = BuildWaveSpawnPlan(currentWave);
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        PlayerCombat playerCombat = player != null ? player.GetComponent<PlayerCombat>() : null;
        LastEliteSpawnMetrics = EliteSpawnLayer.ApplyConversion(
            currentRoomData,
            currentWave,
            currentWaveIndex,
            spawnPlan,
            playerCombat,
            eliteSpawnHistory,
            eliteSpawnDebugOverrides,
            LastEliteSpawnMetrics);

        for (int i = 0; i < spawnPlan.Count; i++)
        {
            EliteSpawnPlanEntry planEntry = spawnPlan[i];
            if (planEntry == null || planEntry.enemyType == null || planEntry.enemyType.prefab == null)
                continue;

            Transform sp = validSpawnPoints[Random.Range(0, validSpawnPoints.Count)];
            if (planEntry.enemyType.prefab != null)
            {
                GameObject enemy = Instantiate(planEntry.enemyType.prefab, sp.position, Quaternion.identity);
                EnemyBase eb = enemy.GetComponent<EnemyBase>();
                if (eb != null)
                {
                    eb.enemyData = planEntry.enemyType;
                    ApplySpawnPlanElite(eb, planEntry);
                }
                activeEnemies.Add(enemy);
            }

            yield return new WaitForSeconds(planEntry.spawnDelay);
        }
    }

    public void OnEnemyDied(GameObject enemy)
    {
         if (activeEnemies.Contains(enemy))
         {
             activeEnemies.Remove(enemy);
         }

         if (activeEnemies.Count == 0)
         {
             // Wave bitti
             currentWaveIndex++;
             if (currentWaveIndex < currentRoomData.waves.Count)
             {
                 StartCoroutine(SpawnWaveRoutine());
             }
             else
             {
                 RoomCleared();
             }
         }
    }


    /// <summary>
    /// Oyuncunun önceki odadaki kapı yönüne göre yeni odada nerede doğacağını belirler.
    /// Sol kapıdan çıktıysa → sağ kapının entryPoint'i, üst → alt, vs.
    /// </summary>
    private Vector3 GetDirectionalSpawnPosition(RoomLayout layout)
    {
        // Fallback: playerStartPoint veya sıfır
        Vector3 fallback = (layout.playerStartPoint != null) ? layout.playerStartPoint.position : Vector3.zero;

        if (RunManager.Instance == null || RunManager.Instance.lastDoorDirection < 0)
            return fallback;

        if (layout.rewardDoors == null || layout.rewardDoors.Length == 0)
            return fallback;

        // Karşı yönü bul: Left↔Right, Top↔Bottom
        RewardDoor.DoorDirection exitDir = (RewardDoor.DoorDirection)RunManager.Instance.lastDoorDirection;
        RewardDoor.DoorDirection targetDir;

        switch (exitDir)
        {
            case RewardDoor.DoorDirection.Left:   targetDir = RewardDoor.DoorDirection.Right;  break;
            case RewardDoor.DoorDirection.Right:  targetDir = RewardDoor.DoorDirection.Left;   break;
            case RewardDoor.DoorDirection.Top:    targetDir = RewardDoor.DoorDirection.Bottom; break;
            case RewardDoor.DoorDirection.Bottom: targetDir = RewardDoor.DoorDirection.Top;    break;
            default: return fallback;
        }

        // Karşı yöndeki kapıyı bul
        foreach (var door in layout.rewardDoors)
        {
            if (door != null && door.direction == targetDir)
            {
                // entryPoint atanmışsa onu kullan, yoksa kapının pozisyonunu
                return (door.entryPoint != null) ? door.entryPoint.position : door.transform.position;
            }
        }

        // Karşı yönde kapı yoksa fallback
        return fallback;
    }

    private void RoomCleared()
    {
        isRoomActive = false;
        EncounterAffinityManager.EnsureInstance().EndEncounter(currentRoomData);

        // Oda temizlendiğinde haritadaki tüm tuzakları yok et
        TrapArea[] remainingTraps = FindObjectsByType<TrapArea>(FindObjectsSortMode.None);
        foreach (var trap in remainingTraps)
        {
            if (trap != null) Destroy(trap.gameObject);
        }
        
        // 1. Bekleyen odulu ver (Varsa)
        if (RunManager.Instance != null)
        {
            RunManager.Instance.GrantPendingReward();
        }

        // Listedeki son oda temizlendiyse run'i bitir
        if (RunManager.Instance != null && RunManager.Instance.IsRunComplete())
        {
            if (EconomyManager.Instance != null)
                EconomyManager.Instance.DepositRunGold();

            if (GameManager.Instance != null)
                GameManager.Instance.SetState(GameManager.GameState.GameOver);

            return;
        }

        // --- ÖDÜL HAVUZU ---
        if (possibleRewards == null || possibleRewards.Length == 0)
        {
            Debug.LogError("RoomManager: possibleRewards dizisi boş! Lütfen Inspector'dan ödül SO'larını ekleyin.");
            return;
        }

        // Listeye kopyala ve karıştır (Shuffle)
        List<RewardDefinitionSO> availableRewards = new List<RewardDefinitionSO>(possibleRewards);
        for (int i = 0; i < availableRewards.Count; i++)
        {
            RewardDefinitionSO temp = availableRewards[i];
            int randomIndex = Random.Range(i, availableRewards.Count);
            availableRewards[i] = availableRewards[randomIndex];
            availableRewards[randomIndex] = temp;
        }

        // --- Fiziksel Kapılar (RewardDoor) ---
        RewardDoor[] doors = (currentRoomLayout != null) ? currentRoomLayout.rewardDoors : null;
        if (doors != null && doors.Length > 0)
        {
            for (int i = 0; i < doors.Length; i++)
            {
                if (doors[i] == null) continue;
                // Kapı sayısına göre dağıt (Eğer kapı sayısı ödül çeşidinden fazlaysa mod alıp başa döner)
                RewardDefinitionSO reward = availableRewards[i % availableRewards.Count];
                doors[i].Initialize(reward);
                doors[i].Unlock();
            }
        }
        else
        {
            Debug.LogWarning("RoomManager: RoomLayout'ta RewardDoor atanmamış! Kapı açılamıyor.");
        }
    }

    private List<EliteSpawnPlanEntry> BuildWaveSpawnPlan(RoomWave wave)
    {
        List<EliteSpawnPlanEntry> plan = new List<EliteSpawnPlanEntry>();
        if (wave == null || wave.enemyGroups == null)
            return plan;

        for (int groupIndex = 0; groupIndex < wave.enemyGroups.Count; groupIndex++)
        {
            EnemySpawn enemyGroup = wave.enemyGroups[groupIndex];
            if (enemyGroup == null || enemyGroup.enemyType == null || enemyGroup.count <= 0)
                continue;

            for (int instanceIndex = 0; instanceIndex < enemyGroup.count; instanceIndex++)
                plan.Add(new EliteSpawnPlanEntry(enemyGroup, groupIndex, instanceIndex));
        }

        return plan;
    }

    private void ApplySpawnPlanElite(EnemyBase enemyBase, EliteSpawnPlanEntry planEntry)
    {
        if (enemyBase == null)
            return;

        enemyBase.ClearEliteProfile();
        if (planEntry != null && planEntry.isElite && planEntry.eliteProfile != null)
            enemyBase.ApplyEliteProfile(planEntry.eliteProfile);
    }
}

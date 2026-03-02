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

    [Header("State")]
    public bool isRoomActive = false;
    public int currentWaveIndex = 0;
    public List<GameObject> activeEnemies = new List<GameObject>();

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
        if (currentRoomData == null)
        {
            Debug.LogError("RoomManager: Current Room Data is MISSING!");
            return;
        }

        // --- RESET STATE FOR NEW ROOM ---
        StopAllCoroutines();
        activeEnemies.Clear();

        // Sahnede önceden yerleştirilmiş düşmanları ve eski portalları temizle
        EnemyBase[] existingEnemies = FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);
        foreach (var enemy in existingEnemies) Destroy(enemy.gameObject);

        ExitPortal[] existingPortals = FindObjectsByType<ExitPortal>(FindObjectsSortMode.None);
        foreach (var portal in existingPortals) Destroy(portal.gameObject);
        
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
                // (Opsiyonel) Oyuncuyu yeni odanin baslangic noktasina isinla
                if (currentRoomLayout.playerStartPoint != null)
                {
                    GameObject player = GameObject.FindGameObjectWithTag("Player");
                    if (player != null)
                    {
                        // Fizik motoru etkilesimlerini engellememek icin Rigidbody pozisyonunu guncelle
                        Rigidbody2D pRb = player.GetComponent<Rigidbody2D>();
                        if (pRb != null) pRb.position = currentRoomLayout.playerStartPoint.position;
                        player.transform.position = currentRoomLayout.playerStartPoint.position;
                    }
                }
            }
        }
        else
        {
            Debug.LogWarning($"RoomManager: Odanin ({currentRoomData.roomName}) prefab'i bos. Eski veya bos oda kullaniliyor.");
        }

        isRoomActive = true;
        currentWaveIndex = 0;
        
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

        // Bu wave icindeki tum dusman gruplarini don
        foreach (var enemyGroup in currentWave.enemyGroups)
        {
            if (enemyGroup.enemyType == null || enemyGroup.count <= 0) continue;

            for (int i = 0; i < enemyGroup.count; i++)
            {
                // Gecerli noktalardan rastgele sec
                Transform sp = validSpawnPoints[Random.Range(0, validSpawnPoints.Count)];
                
                // Prefab instantiation
                if (enemyGroup.enemyType.prefab != null)
                {
                    GameObject enemy = Instantiate(enemyGroup.enemyType.prefab, sp.position, Quaternion.identity);
                    EnemyBase eb = enemy.GetComponent<EnemyBase>();
                    if (eb != null)
                    {
                        eb.enemyData = enemyGroup.enemyType; // Datayi inject et
                    }
                    activeEnemies.Add(enemy);
                }
                
                // Ayni guptaki dusmanlar arasi bekleme
                yield return new WaitForSeconds(enemyGroup.spawnDelay); 
            }
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

    [Header("Visuals & Portals")]
    public GameObject exitPortalPrefab;
    
    private void RoomCleared()
    {
        isRoomActive = false;

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

        Transform[] pPoints = (currentRoomLayout != null) ? currentRoomLayout.portalSpawnPoints : null;

        // Normal oda — portal cikar
        if (exitPortalPrefab != null && pPoints != null && pPoints.Length > 0)
        {
            int portalCount = Mathf.Min(3, pPoints.Length);
            
            RewardType[] availableRewards = { RewardType.Heal, RewardType.MaxHealth, RewardType.DamageUp, RewardType.TempoBoost, RewardType.Gold };
            
            for (int i = 0; i < availableRewards.Length; i++)
            {
                RewardType temp = availableRewards[i];
                int randomIndex = Random.Range(i, availableRewards.Length);
                availableRewards[i] = availableRewards[randomIndex];
                availableRewards[randomIndex] = temp;
            }

            for (int i = 0; i < portalCount; i++)
            {
                GameObject portalObj = Instantiate(exitPortalPrefab, pPoints[i].position, Quaternion.identity);
                ExitPortal portalScript = portalObj.GetComponent<ExitPortal>();
                
                if (portalScript != null)
                {
                    portalScript.Initialize(availableRewards[i]);
                }
            }
        }
        else
        {
            Debug.LogWarning("RoomManager: Portal Spawn Points NOT assigned in this RoomLayout or ExitPrefab is missing! Cannot spawn doors.");
        }
    }
}

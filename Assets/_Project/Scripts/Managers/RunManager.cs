using UnityEngine;

public class RunManager : MonoBehaviour
{
    public static RunManager Instance { get; private set; }

    [Header("Run State")]
    public int roomsCleared = 0;
    
    [Tooltip("The reward the player chose from the previous room. Will be granted when this room is cleared.")]
    public RewardType pendingReward = RewardType.None;

    [Header("Room Progression")]
    [Tooltip("Odalar bu listedeki sıraya göre oynanır. Son oda temizlendiğinde oyun biter.")]
    public System.Collections.Generic.List<RoomSO> roomSequence;

    public RoomSO GetNextRoom()
    {
        if (roomSequence != null && roomsCleared < roomSequence.Count)
            return roomSequence[roomsCleared];
        return null;
    }

    public bool IsRunComplete()
    {
        return roomSequence == null || roomsCleared >= roomSequence.Count;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
    }

    [Header("Persistent Player Stats")]
    public bool isNewRun = true;
    public float savedMaxHealth = 100f;
    public float savedCurrentHealth = 100f;
    public float savedDamageMultiplier = 1.0f;
    public float savedTempo = 0f;

    // Sahne arasinda oyunculari kaydetmek icin cagrilir
    public void SavePlayerState(PlayerCombat player, TempoManager tempoMgr)
    {
        if (player != null)
        {
            savedMaxHealth = player.maxHealth;
            savedCurrentHealth = player.currentHealth;
            savedDamageMultiplier = player.damageMultiplier;
        }
        if (tempoMgr != null)
        {
            savedTempo = tempoMgr.tempo;
        }
        
        // Save çalışıyorsa, bu artık kesinlikle "Yeni Run" değildir.
        // Odadan odaya geçerken canın sıfırlanmaması (Bug C) için bunu false yapıyoruz.
        isNewRun = false;
    }

    // Oyuncu ölüp oyunu baştan başlattığında (Retry) verileri sıfırlamak için
    public void ResetRunData()
    {
        isNewRun = true;
        savedTempo = 0f;
        roomsCleared = 0;
        pendingReward = RewardType.None;
    }

    /// <summary>
    /// Mevcut odanin RoomSO verisini dondurur (Gold kapi odulu icin goldReward degerine erismek amaciyla).
    /// </summary>
    public RoomSO GetCurrentRoomData()
    {
        if (RoomManager.Instance != null)
            return RoomManager.Instance.currentRoomData;
        return null;
    }

    // Yeni sahne yuklendiginde baslangic degerlerini set etmek icin cagrilir
    public void LoadPlayerState(PlayerCombat player, TempoManager tempoMgr)
    {
        
        // Eğer bu yeni bir run ise (örn. yeni oyuna başlanmış veya öldükten sonra retry atılmışsa)
        // Hardcoded 100f vermek yerine, oyuncunun Unity Inspector'ında ayarlı kendi default değerlerini alıp hafızaya kaydeder.
        if (isNewRun)
        {
            if (player != null)
            {
                // Oyuncunun Inspector'daki default degerlerini al
                savedMaxHealth = player.maxHealth;
                savedDamageMultiplier = player.damageMultiplier;

                // Hub'daki kalici yukseltmeleri uygula (SaveData'dan)
                if (SaveManager.Instance != null)
                {
                    SaveData saveData = SaveManager.Instance.data;
                    savedMaxHealth += saveData.bonusMaxHealth * 10f;                    // Her seviye +10 can
                    savedDamageMultiplier += saveData.bonusDamageMultiplier * 0.1f;     // Her seviye +%10 hasar

                    // Parry yukseltmelerini uygula (UpgradeConfigSO'daki base değerler üzerinden)
                    ParrySystem parry = player.GetComponent<ParrySystem>();
                    if (parry != null)
                    {
                        // UpgradeConfigSO'yu bul
                        var shopUI = FindFirstObjectByType<ShopUI>();
                        UpgradeConfigSO upgradeConfig = shopUI != null ? shopUI.upgradeConfig : null;
                        
                        if (upgradeConfig != null)
                        {
                            parry.parryWindow = upgradeConfig.baseParryWindow + saveData.bonusParryWindow * upgradeConfig.parryWindowPerLevel;
                            parry.parryRecovery = Mathf.Max(0.01f, upgradeConfig.baseParryRecovery - saveData.bonusParryRecovery * upgradeConfig.parryRecoveryPerLevel);
                        }
                        else
                        {
                            // Fallback: configSO bulunamazsa Inspector değerine ekle
                            parry.parryWindow += saveData.bonusParryWindow * 0.02f;
                            parry.parryRecovery = Mathf.Max(0.01f, parry.parryRecovery - saveData.bonusParryRecovery * 0.01f);
                        }
                    }
                }

                savedCurrentHealth = savedMaxHealth; // Yeni runda can full
            }
            if (tempoMgr != null)
            {
                savedTempo = 0f;

                // Hub'daki kalici tempo yukseltmesini uygula
                if (SaveManager.Instance != null)
                {
                    tempoMgr.tempoGainMultiplier = 1f + (SaveManager.Instance.data.bonusTempoGain * 0.1f);
                }
            }
            isNewRun = false;
        }

        if (player != null)
        {
            player.maxHealth = savedMaxHealth;
            player.currentHealth = savedCurrentHealth;
            player.damageMultiplier = savedDamageMultiplier;
            player.UpdateHealthUI();
        }
        if (tempoMgr != null)
        {
            tempoMgr.tempo = savedTempo;
            tempoMgr.InitializeLoadedState();
        }
    }

    // Oyuncu bir portaldan gectiginde cagirilir (RoomExitTrigger icinden)
    public void SetNextRewardContext(RewardType chosenReward)
    {
        pendingReward = chosenReward;
    }

    // Oda temizlendiginde cagirilir (RoomManager icinden)
    public void GrantPendingReward()
    {
        roomsCleared++; // Odayi her turlu bitirdik, sayaci artir
        
        if (pendingReward == RewardType.None) return;


        // TODO: Ileride PlayerStats veya kalici bir veriye islenecek
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            var pCombat = player.GetComponent<PlayerCombat>();
            if (pCombat != null)
            {
                switch (pendingReward)
                {
                    case RewardType.Heal:
                        pCombat.currentHealth = Mathf.Min(pCombat.currentHealth + 20f, pCombat.maxHealth);
                        break;
                    case RewardType.MaxHealth:
                        pCombat.maxHealth += 10f;
                        pCombat.currentHealth += 10f; // Mevcut cani da artir
                        break;
                    case RewardType.DamageUp:
                        pCombat.damageMultiplier += 0.2f; // %20 Hasar Artisi
                        break;
                    case RewardType.TempoBoost:
                        if (TempoManager.Instance != null)
                        {
                            TempoManager.Instance.tempoGainMultiplier += 0.25f; // %25 Fazla Tempo
                        }
                        break;
                    case RewardType.Gold:
                        if (EconomyManager.Instance != null)
                        {
                            // RoomSO'daki goldReward degerini kullan (varsayilan 10)
                            int goldAmount = 10;
                            if (RunManager.Instance != null)
                            {
                                RoomSO currentRoom = RunManager.Instance.GetCurrentRoomData();
                                if (currentRoom != null && currentRoom.goldReward > 0)
                                    goldAmount = currentRoom.goldReward;
                            }
                            EconomyManager.Instance.AddRunGold(goldAmount);
                        }
                        break;
                }
                
                // UI guncellemesi
                pCombat.UpdateHealthUI(); 
            }
        }

        pendingReward = RewardType.None; // Odul verildi, sifirla
    }
}

using UnityEngine;

public class RunManager : MonoBehaviour
{
    public static RunManager Instance { get; private set; }

    [Header("Run State")]
    public int roomsCleared = 0;

    [Tooltip("The reward the player chose from the previous room. Will be granted when this room is cleared.")]
    public RewardDefinitionSO pendingReward;

    [Tooltip("Context metadata for the pending reward. Used by the new reward skeleton while legacy rewards stay intact.")]
    public RunRewardContext pendingRewardContext = new RunRewardContext();

    [Tooltip("Oyuncunun son gectigi kapinin yonu (RewardDoor.DoorDirection int). -1 = ilk oda/belirtilmemis")]
    [HideInInspector] public int lastDoorDirection = -1;

    [Header("Room Progression")]
    [Tooltip("Odalar bu listedeki siraya gore oynanir. Son oda temizlendiginde oyun biter.")]
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
    private RunResourceBankState runResourceBank = new RunResourceBankState();

    public RunModifierContext CurrentRunModifierContext { get; private set; } = PactContractService.BuildDefaultRunModifierContext();

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
            savedTempo = tempoMgr.tempo;

        // Save calisiyorsa, bu artik kesinlikle "Yeni Run" degildir.
        // Odadan odaya gecerken canin sifirlanmamasi icin bunu false yapiyoruz.
        isNewRun = false;
    }

    // Oyuncu olup oyunu bastan baslattiginda (Retry) verileri sifirlamak icin
    public void ResetRunData()
    {
        isNewRun = true;
        savedTempo = 0f;
        roomsCleared = 0;
        pendingReward = null;
        pendingRewardContext = new RunRewardContext();
        runResourceBank = new RunResourceBankState();
        CurrentRunModifierContext = PactContractService.BuildDefaultRunModifierContext();
    }

    /// <summary>
    /// Mevcut odanin RoomSO verisini dondurur.
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
        UpgradeConfigSO upgradeConfig = player != null ? player.upgradeConfig : null;

        // Yeni run baslangicinda kalici meta bonuslar tek kaynak olan UpgradeConfigSO'dan okunur.
        if (isNewRun)
        {
            if (player != null)
            {
                if (SaveManager.Instance != null)
                {
                    SaveData saveData = SaveManager.Instance.data;
                    savedMaxHealth = upgradeConfig != null
                        ? upgradeConfig.GetMaxHealth(saveData.bonusMaxHealth)
                        : player.maxHealth;
                    savedDamageMultiplier = upgradeConfig != null
                        ? upgradeConfig.GetDamageMultiplier(saveData.bonusDamageMultiplier)
                        : player.damageMultiplier;
                }
                else
                {
                    savedMaxHealth = player.maxHealth;
                    savedDamageMultiplier = player.damageMultiplier;
                }

                savedCurrentHealth = savedMaxHealth;
            }

            if (tempoMgr != null)
            {
                savedTempo = 0f;

                if (SaveManager.Instance != null && upgradeConfig != null)
                {
                    SaveData saveData = SaveManager.Instance.data;
                    tempoMgr.tempoGainMultiplier = upgradeConfig.GetTempoGainMultiplier(saveData.bonusTempoGain);
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

    // Oyuncu bir portaldan gectiginde cagrilir (RoomExitTrigger icinden)
    public void SetNextRewardContext(RewardDefinitionSO chosenReward, RunRewardContext rewardContext = null)
    {
        pendingReward = chosenReward;
        pendingRewardContext = rewardContext ?? RunRewardResolver.CreateContext(chosenReward, -1);
    }

    // Oda temizlendiginde cagrilir (RoomManager icinden)
    public void GrantPendingReward()
    {
        roomsCleared++;

        if (pendingReward == null)
            return;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            var pCombat = player.GetComponent<PlayerCombat>();
            if (pCombat != null)
            {
                RunRewardApplier.ApplyReward(pendingReward, pCombat, pendingRewardContext);
                pCombat.UpdateHealthUI();
            }
        }

        pendingReward = null;
        pendingRewardContext = new RunRewardContext();
    }

    public RunResourceBankState GetRunResourceBank()
    {
        if (runResourceBank == null)
            runResourceBank = new RunResourceBankState();

        return runResourceBank;
    }

    public void AddBankedResource(ProgressionResourceType resourceType, int amount)
    {
        if (amount <= 0)
            return;

        GetRunResourceBank().AddAmount(resourceType, amount);
    }

    public void SetRunModifierContext(RunModifierContext context)
    {
        CurrentRunModifierContext = context ?? PactContractService.BuildDefaultRunModifierContext();
    }
}

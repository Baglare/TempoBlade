using UnityEngine;

[DisallowMultipleComponent]
public class MiniBossRoomController : MonoBehaviour
{
    private RoomManager roomManager;
    private RoomSO roomData;
    private RoomLayout roomLayout;
    private MiniBossEncounterSO encounterData;
    private MiniBossPhaseController phaseController;
    private EnemyBase activeMiniBoss;
    private bool completionTriggered;

    public bool IsEncounterActive { get; private set; }
    public MiniBossEncounterSO ActiveEncounter => encounterData;
    public EnemyBase ActiveMiniBoss => activeMiniBoss;

    public void BeginEncounter(RoomManager owner, RoomSO room, RoomLayout layout, MiniBossEncounterSO encounter, bool logDebug)
    {
        roomManager = owner;
        roomData = room;
        roomLayout = layout;
        encounterData = encounter;
        completionTriggered = false;
        IsEncounterActive = false;

        if (encounterData == null || encounterData.miniBossPrefab == null)
        {
            Debug.LogError("[MiniBoss] Encounter data veya prefab eksik. Oda plain exit fallback ile kapatilacak.");
            roomManager?.HandleMiniBossEncounterSetupFailed();
            return;
        }

        Vector3 spawnPosition = ResolveSpawnPosition(layout);
        GameObject miniBossObject = Instantiate(encounterData.miniBossPrefab, spawnPosition, Quaternion.identity);
        activeMiniBoss = miniBossObject.GetComponent<EnemyBase>();
        if (activeMiniBoss == null)
            activeMiniBoss = miniBossObject.GetComponentInChildren<EnemyBase>();

        if (activeMiniBoss == null)
        {
            Debug.LogError($"[MiniBoss] {encounterData.displayName} prefabinda EnemyBase bulunamadi.");
            Destroy(miniBossObject);
            roomManager?.HandleMiniBossEncounterSetupFailed();
            return;
        }

        if (activeMiniBoss.enemyData == null || activeMiniBoss.enemyData.combatClass != EnemyCombatClass.MiniBoss)
            Debug.LogWarning($"[MiniBoss] {encounterData.displayName} icin EnemySO combatClass MiniBoss olmali.");

        activeMiniBoss.SetSuppressDeathRewards(true);
        phaseController = activeMiniBoss.GetComponent<MiniBossPhaseController>();
        if (phaseController == null)
            phaseController = activeMiniBoss.gameObject.AddComponent<MiniBossPhaseController>();
        phaseController.Initialize(activeMiniBoss, encounterData, roomData != null ? roomData.difficulty : DifficultyTier.Normal);

        if (logDebug)
            Debug.Log($"[MiniBoss] Encounter started -> {encounterData.displayName}");

        IsEncounterActive = true;
    }

    private void Update()
    {
        if (!IsEncounterActive || completionTriggered)
            return;

        if (activeMiniBoss == null || activeMiniBoss.CurrentHealth <= 0f)
            CompleteEncounter();
    }

    private void CompleteEncounter()
    {
        if (completionTriggered)
            return;

        completionTriggered = true;
        IsEncounterActive = false;

        bool alreadyClearedThisRun = RunManager.Instance != null &&
                                     RunManager.Instance.HasClearedMiniBossEncounterThisRun(encounterData != null ? encounterData.encounterId : string.Empty);

        MiniBossRewardResolution rewardResolution = MiniBossRewardResolver.Resolve(encounterData, alreadyClearedThisRun);
        rewardResolution.guaranteedResourceCount = MiniBossRewardResolver.ApplyGuaranteedResources(encounterData);

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        PlayerCombat playerCombat = player != null ? player.GetComponent<PlayerCombat>() : null;
        if (rewardResolution.reward != null && playerCombat != null)
        {
            RunRewardApplier.ApplyReward(rewardResolution.reward, playerCombat, rewardResolution.rewardContext);
            playerCombat.UpdateHealthUI();
        }

        if (RunManager.Instance != null && encounterData != null && !string.IsNullOrEmpty(encounterData.encounterId))
            RunManager.Instance.MarkMiniBossEncounterClearedThisRun(encounterData.encounterId);

        Debug.Log($"[MiniBoss] Encounter completed -> {encounterData?.displayName ?? "Unknown"} | Reward: {(rewardResolution.reward != null ? rewardResolution.reward.rewardName : "None")} | GuaranteedResources: {rewardResolution.guaranteedResourceCount}");
        roomManager?.HandleMiniBossEncounterCompleted(encounterData, rewardResolution);
    }

    private Vector3 ResolveSpawnPosition(RoomLayout layout)
    {
        if (layout != null && layout.enemySpawnPoints != null)
        {
            for (int i = 0; i < layout.enemySpawnPoints.Length; i++)
            {
                if (layout.enemySpawnPoints[i] != null)
                    return layout.enemySpawnPoints[i].position;
            }
        }

        return layout != null ? layout.transform.position : Vector3.zero;
    }
}


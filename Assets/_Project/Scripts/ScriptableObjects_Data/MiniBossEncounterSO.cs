using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New MiniBoss Encounter", menuName = "TempoBlade/MiniBoss Encounter")]
public class MiniBossEncounterSO : ScriptableObject
{
    [Header("Identity")]
    public string encounterId = "miniboss_encounter";
    public string displayName = "Mini-Boss Encounter";

    [Header("Mini-Boss")]
    public GameObject miniBossPrefab;
    [Tooltip("Ileride belirli arena/layout filtreleri icin kullanilabilir.")]
    public string arenaTag = "";

    [Header("Phases")]
    public List<MiniBossPhaseThreshold> phaseThresholds = new List<MiniBossPhaseThreshold>();

    [Header("Rewards")]
    public MiniBossRewardProfile rewardProfile;
    public RewardDefinitionSO firstClearReward;
    public RewardDefinitionSO repeatClearReward;

    [Header("Difficulty Scaling")]
    public MiniBossDifficultyScalingProfile difficultyScaling = new MiniBossDifficultyScalingProfile();

    [Header("Support Hooks")]
    public List<EnemySO> allowedSupportEnemies = new List<EnemySO>();

    [Header("Future Hooks")]
    public List<string> pactHookIds = new List<string>();
    public List<string> unlockConditionIds = new List<string>();
}

[Serializable]
public class MiniBossRewardProfile
{
    [Tooltip("Direct first/repeat reward eksikse fallback havuzu olarak kullanilir.")]
    public RewardDefinitionSO[] rewardPool;

    [Tooltip("Run bitmeden once bankalanacak garanti kaynaklar.")]
    public List<ProgressionResourceEntry> guaranteedResources = new List<ProgressionResourceEntry>();

    [Tooltip("Havuzdan secilecek oduller icin alt rarity etiketi.")]
    public RunRewardRarity minimumRarity = RunRewardRarity.Uncommon;
}

[Serializable]
public class MiniBossPhaseThreshold
{
    public string phaseId = "phase_2";
    public string displayName = "Phase 2";

    [Range(0.05f, 0.95f)]
    [Tooltip("Can yuzdesi bu esigin altina indiginde faz degisir.")]
    public float triggerHealthPercent = 0.5f;

    [Header("Combat Hooks")]
    public MiniBossCombatModifierData combatModifiers = new MiniBossCombatModifierData();
    public string behaviorHookId = "";
    public string attackPatternHookId = "";

    [Header("Presentation Hooks")]
    public GameObject phaseChangeVfxPrefab;
    public AudioEventId phaseChangeAudio = AudioEventId.None;

    [Header("Support / Arena Hooks")]
    public bool enableSupportSpawns;
    public bool enableArenaHazards;
}

[Serializable]
public class MiniBossDifficultyScalingProfile
{
    public MiniBossCombatModifierData easy = new MiniBossCombatModifierData();
    public MiniBossCombatModifierData normal = new MiniBossCombatModifierData();
    public MiniBossCombatModifierData hard = new MiniBossCombatModifierData
    {
        healthMultiplier = 1.15f,
        damageMultiplier = 1.12f,
        cooldownMultiplier = 0.94f,
        moveSpeedMultiplier = 1.06f
    };

    public MiniBossCombatModifierData GetForDifficulty(DifficultyTier difficulty)
    {
        return difficulty switch
        {
            DifficultyTier.Easy => easy,
            DifficultyTier.Hard => hard,
            _ => normal
        };
    }
}

[Serializable]
public class MiniBossCombatModifierData
{
    public float healthMultiplier = 1f;
    public float damageMultiplier = 1f;
    public float cooldownMultiplier = 1f;
    public float moveSpeedMultiplier = 1f;

    public static MiniBossCombatModifierData Combine(MiniBossCombatModifierData a, MiniBossCombatModifierData b)
    {
        return new MiniBossCombatModifierData
        {
            healthMultiplier = Mathf.Max(0.01f, (a?.healthMultiplier ?? 1f) * (b?.healthMultiplier ?? 1f)),
            damageMultiplier = Mathf.Max(0.01f, (a?.damageMultiplier ?? 1f) * (b?.damageMultiplier ?? 1f)),
            cooldownMultiplier = Mathf.Max(0.01f, (a?.cooldownMultiplier ?? 1f) * (b?.cooldownMultiplier ?? 1f)),
            moveSpeedMultiplier = Mathf.Max(0.01f, (a?.moveSpeedMultiplier ?? 1f) * (b?.moveSpeedMultiplier ?? 1f))
        };
    }
}


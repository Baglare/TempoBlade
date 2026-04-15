using UnityEngine;

[CreateAssetMenu(fileName = "TreeProgressionConfig", menuName = "TempoBlade/Skill Tree/Tree Progression Config", order = 120)]
public class TreeProgressionConfigSO : ScriptableObject
{
    [Header("Rank XP Thresholds")]
    public int[] rankXpThresholds =
    {
        100, 220, 360, 520, 700, 900, 1150, 1450, 1800, 2200, 2650
    };

    [Header("Room Base XP")]
    public float normalRoomXp = 28f;
    public float eliteRoomXp = 42f;
    public float miniBossRoomXp = 60f;
    public float bossRoomXp = 90f;

    [Header("Difficulty Multipliers")]
    public float easyMultiplier = 0.90f;
    public float normalMultiplier = 1.00f;
    public float hardMultiplier = 1.15f;

    [Header("Affinity XP Formula")]
    [Range(0f, 1f)] public float minimumAffinityForXp = 0.08f;
    [Range(0f, 1f)] public float baseXpFloor = 0.35f;
    [Range(0f, 2f)] public float affinityXpScale = 0.65f;
    [Range(1f, 1.5f)] public float maxVarietyBonus = 1.15f;
    [Tooltip("Weighted score at this value normalizes to 1 affinity.")]
    public float scoreForFullAffinity = 10f;

    [Header("Diminishing Returns")]
    public float repeatedEventWindow = 3.0f;
    public float firstRepeatMultiplier = 1.0f;
    public float secondRepeatMultiplier = 0.6f;
    public float thirdRepeatMultiplier = 0.3f;
    public float furtherRepeatMultiplier = 0.1f;

    [Header("Affinity Weights")]
    public AxisAffinityWeights dashWeights = new AxisAffinityWeights
    {
        axisId = "axis_dash",
        primaryWeight = 0.30f,
        secondaryWeight = 0.20f,
        conversionWeight = 0.25f,
        utilityWeight = 0.15f,
        penaltyWeight = 0.20f
    };

    public AxisAffinityWeights parryWeights = new AxisAffinityWeights
    {
        axisId = "axis_parry",
        primaryWeight = 0.25f,
        secondaryWeight = 0.30f,
        conversionWeight = 0.25f,
        utilityWeight = 0.10f,
        penaltyWeight = 0.20f
    };

    public AxisAffinityWeights overdriveWeights = new AxisAffinityWeights
    {
        axisId = "axis_overdrive",
        primaryWeight = 0.20f,
        secondaryWeight = 0.20f,
        conversionWeight = 0.30f,
        utilityWeight = 0.20f,
        penaltyWeight = 0.25f
    };

    public AxisAffinityWeights cadenceWeights = new AxisAffinityWeights
    {
        axisId = "axis_cadence",
        primaryWeight = 0.20f,
        secondaryWeight = 0.20f,
        conversionWeight = 0.20f,
        utilityWeight = 0.20f,
        fifthWeight = 0.15f,
        penaltyWeight = 0.20f
    };

    [Header("Runtime Sampling")]
    public float highTempoThreshold = 70f;
    public float nearEnemyRadius = 3.0f;
    public float threatRadius = 4.0f;
    public float followUpWindow = 1.25f;
    public float thresholdPayoffWindow = 4.0f;

    public int MaxRank => rankXpThresholds != null ? rankXpThresholds.Length : 0;

    public int GetRequiredXpForRank(int rank)
    {
        if (rank <= 0 || rankXpThresholds == null || rankXpThresholds.Length == 0)
            return 0;

        int index = Mathf.Clamp(rank - 1, 0, rankXpThresholds.Length - 1);
        return rankXpThresholds[index];
    }

    public int CalculateRank(float xp)
    {
        if (rankXpThresholds == null)
            return 0;

        int rank = 0;
        for (int i = 0; i < rankXpThresholds.Length; i++)
        {
            if (xp >= rankXpThresholds[i])
                rank = i + 1;
            else
                break;
        }

        return rank;
    }

    public float GetBaseXp(EncounterType type)
    {
        switch (type)
        {
            case EncounterType.Elite: return eliteRoomXp;
            case EncounterType.MiniBoss: return miniBossRoomXp;
            case EncounterType.Boss: return bossRoomXp;
            default: return normalRoomXp;
        }
    }

    public float GetDifficultyMultiplier(DifficultyTier tier)
    {
        switch (tier)
        {
            case DifficultyTier.Easy: return easyMultiplier;
            case DifficultyTier.Hard: return hardMultiplier;
            default: return normalMultiplier;
        }
    }

    public AxisAffinityWeights GetWeights(string axisId)
    {
        switch (axisId)
        {
            case "axis_parry": return parryWeights;
            case "axis_overdrive": return overdriveWeights;
            case "axis_cadence": return cadenceWeights;
            default: return dashWeights;
        }
    }
}


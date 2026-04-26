using UnityEngine;

public sealed class MiniBossRewardResolution
{
    public RewardDefinitionSO reward;
    public RunRewardContext rewardContext = new RunRewardContext();
    public bool usedFirstClearReward;
    public int guaranteedResourceCount;
}

public static class MiniBossRewardResolver
{
    public static MiniBossRewardResolution Resolve(MiniBossEncounterSO encounter, bool alreadyClearedThisRun)
    {
        MiniBossRewardResolution resolution = new MiniBossRewardResolution
        {
            usedFirstClearReward = !alreadyClearedThisRun
        };

        if (encounter == null)
            return resolution;

        RewardDefinitionSO directReward = !alreadyClearedThisRun ? encounter.firstClearReward : encounter.repeatClearReward;
        if (directReward == null && encounter.rewardProfile != null && encounter.rewardProfile.rewardPool != null && encounter.rewardProfile.rewardPool.Length > 0)
            directReward = ResolveFromPool(encounter.rewardProfile);

        resolution.reward = directReward;
        resolution.rewardContext = directReward != null
            ? RunRewardResolver.CreateContext(directReward, -1)
            : new RunRewardContext();

        return resolution;
    }

    public static int ApplyGuaranteedResources(MiniBossEncounterSO encounter)
    {
        if (encounter == null || encounter.rewardProfile == null || encounter.rewardProfile.guaranteedResources == null)
            return 0;

        int grantsApplied = 0;
        for (int i = 0; i < encounter.rewardProfile.guaranteedResources.Count; i++)
        {
            ProgressionResourceEntry grant = encounter.rewardProfile.guaranteedResources[i];
            if (grant == null || grant.amount <= 0 || RunManager.Instance == null)
                continue;

            RunManager.Instance.AddBankedResource(grant.resourceType, grant.amount);
            grantsApplied++;
        }

        return grantsApplied;
    }

    private static RewardDefinitionSO ResolveFromPool(MiniBossRewardProfile rewardProfile)
    {
        if (rewardProfile == null || rewardProfile.rewardPool == null || rewardProfile.rewardPool.Length == 0)
            return null;

        float totalWeight = 0f;
        RewardDefinitionSO[] pool = rewardProfile.rewardPool;
        float[] weights = new float[pool.Length];

        for (int i = 0; i < pool.Length; i++)
        {
            RewardDefinitionSO reward = pool[i];
            if (reward == null)
            {
                weights[i] = 0f;
                continue;
            }

            if (reward.rarity < rewardProfile.minimumRarity)
            {
                weights[i] = 0f;
                continue;
            }

            RunRewardContext context = RunRewardResolver.CreateContext(reward, -1);
            float weight = RunRewardResolver.ResolveWeight(reward, context);
            weights[i] = weight;
            totalWeight += weight;
        }

        if (totalWeight <= 0.001f)
            return pool[0];

        float roll = Random.value * totalWeight;
        for (int i = 0; i < pool.Length; i++)
        {
            float weight = weights[i];
            if (weight <= 0f)
                continue;

            roll -= weight;
            if (roll <= 0f)
                return pool[i];
        }

        return pool[pool.Length - 1];
    }
}


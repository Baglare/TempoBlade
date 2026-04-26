using System;
using UnityEngine;

public enum RunRewardType
{
    Unspecified,
    Resource,
    Survival,
    Build,
    Risk
}

public enum RunRewardRarity
{
    Common,
    Uncommon,
    Rare,
    Epic
}

[Serializable]
public class RunRewardWeightProfile
{
    public float baseWeight = 1f;
    public float rarityMultiplier = 1f;
    public float contextMultiplier = 1f;
}

[Serializable]
public class RunRewardContext
{
    public int roomIndex = 0;
    public int rewardChoiceIndex = -1;
    public RunRewardType rewardType = RunRewardType.Unspecified;
    public RunRewardRarity rarity = RunRewardRarity.Common;
    public RunModifierContext runModifierContext = new RunModifierContext();
}

public static class RunRewardResolver
{
    public static RunRewardType ResolveRewardType(RewardDefinitionSO reward)
    {
        if (reward == null)
            return RunRewardType.Unspecified;

        if (reward.runRewardType != RunRewardType.Unspecified)
            return reward.runRewardType;

        return reward.category switch
        {
            RewardCategory.Economy => RunRewardType.Resource,
            RewardCategory.Survival => RunRewardType.Survival,
            RewardCategory.Offensive => RunRewardType.Build,
            RewardCategory.Tempo => RunRewardType.Build,
            RewardCategory.Utility => RunRewardType.Risk,
            _ => RunRewardType.Unspecified
        };
    }

    public static float ResolveWeight(RewardDefinitionSO reward, RunRewardContext context)
    {
        if (reward == null)
            return 0f;

        float rarityFactor = reward.rarity switch
        {
            RunRewardRarity.Uncommon => 0.9f,
            RunRewardRarity.Rare => 0.75f,
            RunRewardRarity.Epic => 0.6f,
            _ => 1f
        };

        float baseWeight = reward.weightProfile != null ? reward.weightProfile.baseWeight : 1f;
        float profileRarity = reward.weightProfile != null ? reward.weightProfile.rarityMultiplier : 1f;
        float contextMultiplier = reward.weightProfile != null ? reward.weightProfile.contextMultiplier : 1f;
        float runModifierMultiplier = context != null && context.runModifierContext != null
            ? context.runModifierContext.rewardWeightMultiplier
            : 1f;

        return Mathf.Max(0.01f, baseWeight * rarityFactor * profileRarity * contextMultiplier * runModifierMultiplier);
    }

    public static RunRewardContext CreateContext(RewardDefinitionSO reward, int rewardChoiceIndex)
    {
        return new RunRewardContext
        {
            roomIndex = RunManager.Instance != null ? RunManager.Instance.roomsCleared : 0,
            rewardChoiceIndex = rewardChoiceIndex,
            rewardType = ResolveRewardType(reward),
            rarity = reward != null ? reward.rarity : RunRewardRarity.Common,
            runModifierContext = RunManager.Instance != null
                ? RunManager.Instance.CurrentRunModifierContext
                : PactContractService.BuildDefaultRunModifierContext()
        };
    }
}

public static class RunRewardApplier
{
    public static void ApplyReward(RewardDefinitionSO reward, PlayerCombat player, RunRewardContext context)
    {
        if (reward == null)
        {
            Debug.LogWarning("[RunReward] Reward apply requested with null reward.");
            return;
        }

        if (player == null)
        {
            Debug.LogWarning($"[RunReward] Player missing while applying reward {reward.rewardName}.");
            return;
        }

        reward.GrantReward(player, context);
    }
}

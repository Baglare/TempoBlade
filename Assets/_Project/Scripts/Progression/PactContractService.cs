using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ContractModifierDefinition
{
    public string modifierId = string.Empty;
    public string displayName = string.Empty;
    [TextArea] public string description = string.Empty;
}

[Serializable]
public class PactContractConfig
{
    public List<ContractModifierDefinition> modifiers = new List<ContractModifierDefinition>();
}

[Serializable]
public class PactContractState
{
    public List<string> unlockedContractIds = new List<string>();
    public List<string> activeContractIds = new List<string>();
}

[Serializable]
public class RunModifierContext
{
    public float elitePressureBonus = 0f;
    public float enemyPowerMultiplier = 1f;
    public float healModifier = 1f;
    public float tempoDecayMultiplier = 1f;
    public float rewardWeightMultiplier = 1f;
}

public static class PactContractService
{
    public static void SyncState(SaveData data)
    {
        if (data == null)
            return;

        data.EnsureProgressionState();
        if (data.pactContractState == null)
            data.pactContractState = new PactContractState();

        if (data.pactContractState.unlockedContractIds == null)
            data.pactContractState.unlockedContractIds = new List<string>();

        if (data.pactContractState.activeContractIds == null)
            data.pactContractState.activeContractIds = new List<string>();
    }

    public static RunModifierContext BuildDefaultRunModifierContext()
    {
        return new RunModifierContext
        {
            elitePressureBonus = 0f,
            enemyPowerMultiplier = 1f,
            healModifier = 1f,
            tempoDecayMultiplier = 1f,
            rewardWeightMultiplier = 1f
        };
    }
}

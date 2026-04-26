using System;
using System.Collections.Generic;
using UnityEngine;

public enum ProgressionResourceType
{
    Gold,
    WeaponMaterial,
    SuccessBooster,
    CoreMaterial,
    SpecialTrophy
}

[Serializable]
public class ProgressionResourceEntry
{
    public ProgressionResourceType resourceType = ProgressionResourceType.Gold;
    public int amount = 0;
}

[Serializable]
public class ProgressionResourceCost
{
    public ProgressionResourceType resourceType = ProgressionResourceType.Gold;
    public int amount = 0;
}

[Serializable]
public class PersistentResourceWalletState
{
    public List<ProgressionResourceEntry> entries = new List<ProgressionResourceEntry>();

    public int GetAmount(ProgressionResourceType type)
    {
        EnsureEntries();
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].resourceType == type)
                return Mathf.Max(0, entries[i].amount);
        }

        return 0;
    }

    public void SetAmount(ProgressionResourceType type, int amount)
    {
        EnsureEntries();
        ProgressionResourceEntry entry = GetOrCreateEntry(type);
        entry.amount = Mathf.Max(0, amount);
    }

    public void AddAmount(ProgressionResourceType type, int delta)
    {
        if (delta == 0)
            return;

        EnsureEntries();
        ProgressionResourceEntry entry = GetOrCreateEntry(type);
        entry.amount = Mathf.Max(0, entry.amount + delta);
    }

    public bool RemoveAmount(ProgressionResourceType type, int amount)
    {
        if (amount <= 0)
            return true;

        EnsureEntries();
        ProgressionResourceEntry entry = GetOrCreateEntry(type);
        if (entry.amount < amount)
            return false;

        entry.amount -= amount;
        return true;
    }

    public void EnsureEntryExists(ProgressionResourceType type)
    {
        EnsureEntries();
        GetOrCreateEntry(type);
    }

    private ProgressionResourceEntry GetOrCreateEntry(ProgressionResourceType type)
    {
        EnsureEntries();
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].resourceType == type)
                return entries[i];
        }

        ProgressionResourceEntry created = new ProgressionResourceEntry
        {
            resourceType = type,
            amount = 0
        };
        entries.Add(created);
        return created;
    }

    private void EnsureEntries()
    {
        if (entries == null)
            entries = new List<ProgressionResourceEntry>();
    }
}

[Serializable]
public class RunResourceBankState
{
    public List<ProgressionResourceEntry> entries = new List<ProgressionResourceEntry>();
    public bool wasDepositedThisRun = false;

    public int GetAmount(ProgressionResourceType type)
    {
        EnsureEntries();
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].resourceType == type)
                return Mathf.Max(0, entries[i].amount);
        }

        return 0;
    }

    public void SetAmount(ProgressionResourceType type, int amount)
    {
        EnsureEntries();
        ProgressionResourceEntry entry = GetOrCreateEntry(type);
        entry.amount = Mathf.Max(0, amount);
    }

    public void AddAmount(ProgressionResourceType type, int delta)
    {
        if (delta == 0)
            return;

        EnsureEntries();
        ProgressionResourceEntry entry = GetOrCreateEntry(type);
        entry.amount = Mathf.Max(0, entry.amount + delta);
        wasDepositedThisRun = false;
    }

    public void Clear()
    {
        EnsureEntries();
        entries.Clear();
        wasDepositedThisRun = false;
    }

    public void EnsureEntryExists(ProgressionResourceType type)
    {
        EnsureEntries();
        GetOrCreateEntry(type);
    }

    private ProgressionResourceEntry GetOrCreateEntry(ProgressionResourceType type)
    {
        EnsureEntries();
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].resourceType == type)
                return entries[i];
        }

        ProgressionResourceEntry created = new ProgressionResourceEntry
        {
            resourceType = type,
            amount = 0
        };
        entries.Add(created);
        return created;
    }

    private void EnsureEntries()
    {
        if (entries == null)
            entries = new List<ProgressionResourceEntry>();
    }
}

public static class ProgressionResourceUtility
{
    public static string GetDisplayName(ProgressionResourceType type)
    {
        return type switch
        {
            ProgressionResourceType.Gold => "Gold",
            ProgressionResourceType.WeaponMaterial => "Weapon Material",
            ProgressionResourceType.SuccessBooster => "Tempering Stone",
            ProgressionResourceType.CoreMaterial => "Core Material",
            ProgressionResourceType.SpecialTrophy => "Special Trophy",
            _ => type.ToString()
        };
    }
}

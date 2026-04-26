using System.Collections.Generic;
using UnityEngine;

public static class ProgressionResourceWalletService
{
    public static PersistentResourceWalletState GetPersistentWallet()
    {
        SaveData data = GetSaveData();
        if (data == null)
            return new PersistentResourceWalletState();

        data.EnsureProgressionState();
        return data.persistentResourceWallet;
    }

    public static void SyncLegacyGoldState(SaveData data, bool logFallback)
    {
        if (data == null)
            return;

        data.EnsureProgressionState();
        PersistentResourceWalletState wallet = data.persistentResourceWallet;
        int walletGold = wallet.GetAmount(ProgressionResourceType.Gold);

        if (walletGold <= 0 && data.totalGold > 0)
        {
            wallet.SetAmount(ProgressionResourceType.Gold, data.totalGold);
            if (logFallback)
                Debug.LogWarning("[ProgressionWallet] Legacy totalGold migrated into persistent wallet.");
        }
        else
        {
            data.totalGold = walletGold;
        }
    }

    public static void SyncLegacyGoldMirror(SaveData data)
    {
        if (data == null)
            return;

        data.EnsureProgressionState();
        data.totalGold = data.persistentResourceWallet.GetAmount(ProgressionResourceType.Gold);
    }

    public static int GetPersistentAmount(ProgressionResourceType type)
    {
        return GetPersistentWallet().GetAmount(type);
    }

    public static bool HasPersistentAmount(ProgressionResourceType type, int amount)
    {
        return GetPersistentAmount(type) >= Mathf.Max(0, amount);
    }

    public static void AddPersistentResource(ProgressionResourceType type, int amount, bool saveImmediately = true)
    {
        if (amount <= 0)
            return;

        SaveData data = GetSaveData();
        if (data == null)
        {
            Debug.LogWarning($"[ProgressionWallet] SaveData missing while adding {ProgressionResourceUtility.GetDisplayName(type)}.");
            return;
        }

        data.EnsureProgressionState();
        data.persistentResourceWallet.AddAmount(type, amount);
        SyncLegacyGoldMirror(data);

        if (saveImmediately && SaveManager.Instance != null)
            SaveManager.Instance.Save();
    }

    public static bool SpendPersistentResource(ProgressionResourceType type, int amount, bool saveImmediately = true)
    {
        if (amount <= 0)
            return true;

        SaveData data = GetSaveData();
        if (data == null)
            return false;

        data.EnsureProgressionState();
        if (!data.persistentResourceWallet.RemoveAmount(type, amount))
            return false;

        SyncLegacyGoldMirror(data);

        if (saveImmediately && SaveManager.Instance != null)
            SaveManager.Instance.Save();

        return true;
    }

    public static bool CanAfford(IReadOnlyList<ProgressionResourceCost> costs)
    {
        if (costs == null)
            return true;

        for (int i = 0; i < costs.Count; i++)
        {
            ProgressionResourceCost cost = costs[i];
            if (cost == null || cost.amount <= 0)
                continue;

            if (!HasPersistentAmount(cost.resourceType, cost.amount))
                return false;
        }

        return true;
    }

    public static bool SpendCosts(IReadOnlyList<ProgressionResourceCost> costs, bool saveImmediately = true)
    {
        if (!CanAfford(costs))
            return false;

        if (costs != null)
        {
            for (int i = 0; i < costs.Count; i++)
            {
                ProgressionResourceCost cost = costs[i];
                if (cost == null || cost.amount <= 0)
                    continue;

                SpendPersistentResource(cost.resourceType, cost.amount, false);
            }
        }

        SaveData data = GetSaveData();
        if (data != null)
            SyncLegacyGoldMirror(data);

        if (saveImmediately && SaveManager.Instance != null)
            SaveManager.Instance.Save();

        return true;
    }

    private static SaveData GetSaveData()
    {
        return SaveManager.Instance != null ? SaveManager.Instance.data : null;
    }
}

public static class RunResourceBankService
{
    public static RunResourceBankState GetRunBank()
    {
        if (RunManager.Instance == null)
            return new RunResourceBankState();

        return RunManager.Instance.GetRunResourceBank();
    }

    public static void AddResource(ProgressionResourceType type, int amount)
    {
        if (amount <= 0)
            return;

        if (RunManager.Instance == null)
        {
            Debug.LogWarning($"[RunResourceBank] RunManager missing while adding {ProgressionResourceUtility.GetDisplayName(type)}.");
            return;
        }

        RunManager.Instance.AddBankedResource(type, amount);
        SyncLegacyGoldMirror();
    }

    public static int GetAmount(ProgressionResourceType type)
    {
        return GetRunBank().GetAmount(type);
    }

    public static List<ProgressionResourceEntry> GetEntriesSnapshot()
    {
        RunResourceBankState bank = GetRunBank();
        return bank.entries != null ? new List<ProgressionResourceEntry>(bank.entries) : new List<ProgressionResourceEntry>();
    }

    public static void DepositAllToPersistent()
    {
        if (RunManager.Instance == null)
            return;

        RunResourceBankState bank = RunManager.Instance.GetRunResourceBank();
        if (bank.wasDepositedThisRun)
            return;

        if (bank.entries != null)
        {
            for (int i = 0; i < bank.entries.Count; i++)
            {
                ProgressionResourceEntry entry = bank.entries[i];
                if (entry == null || entry.amount <= 0)
                    continue;

                ProgressionResourceWalletService.AddPersistentResource(entry.resourceType, entry.amount, false);
            }
        }

        bank.wasDepositedThisRun = true;

        if (SaveManager.Instance != null)
            SaveManager.Instance.Save();
    }

    public static void ResetBank()
    {
        if (RunManager.Instance == null)
            return;

        RunManager.Instance.GetRunResourceBank().Clear();
        SyncLegacyGoldMirror();
    }

    public static void SyncLegacyGoldMirror()
    {
        if (EconomyManager.Instance != null)
            EconomyManager.Instance.runGold = GetAmount(ProgressionResourceType.Gold);
    }
}

using UnityEngine;

public sealed class PlayerWeaponRuntime
{
    private readonly PlayerCombat owner;

    public PlayerWeaponRuntime(PlayerCombat owner)
    {
        this.owner = owner;
    }

    public int CurrentWeaponLevel
    {
        get
        {
            if (SaveManager.Instance == null || owner.currentWeapon == null)
                return 0;

            return SaveManager.Instance.data.GetWeaponLevel(owner.currentWeapon.weaponName);
        }
    }

    public string CurrentSpecializationId
    {
        get
        {
            if (SaveManager.Instance == null || owner.currentWeapon == null)
                return string.Empty;

            return SaveManager.Instance.data.GetWeaponSpecializationChoice(owner.currentWeapon.weaponName);
        }
    }

    public WeaponResolvedStats GetResolvedStats()
    {
        return WeaponUpgradeResolver.Resolve(owner.currentWeapon, CurrentWeaponLevel, CurrentSpecializationId);
    }

    public FinisherSO GetActiveFinisher()
    {
        return WeaponUpgradeResolver.ResolveFinisher(owner.currentWeapon, CurrentSpecializationId);
    }

    public WeaponSpecializationChoiceData GetSelectedSpecialization()
    {
        return WeaponUpgradeResolver.GetSelectedSpecialization(owner.currentWeapon, CurrentSpecializationId);
    }

    public void LoadEquippedWeapon()
    {
        if (SaveManager.Instance == null || owner.weaponDatabase == null)
            return;

        string savedName = SaveManager.Instance.data.equippedWeaponName;
        if (string.IsNullOrEmpty(savedName))
            return;

        WeaponSO found = owner.weaponDatabase.GetWeaponByName(savedName);
        if (found != null)
            EquipWeapon(found);
    }

    public void EquipWeapon(WeaponSO weapon)
    {
        if (weapon == null)
            return;

        owner.currentWeapon = weapon;

        if (owner.weaponSpriteRenderer != null)
            owner.weaponSpriteRenderer.sprite = weapon.icon;

        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.data.equippedWeaponName = weapon.weaponName;
            SaveManager.Instance.Save();
        }

        owner.ResetComboState();
    }

    public string BuildDebugSummary(PlayerFinisherController finisherController)
    {
        WeaponResolvedStats stats = GetResolvedStats();
        FinisherSO finisher = GetActiveFinisher();
        string reason = string.Empty;
        bool finisherReady = finisherController != null && finisherController.IsFinisherAvailable(out reason);

        string debug = "\n\n<b>--- Weapon Debug ---</b>";
        debug += "\nType: " + stats.weaponTypeLabel;
        debug += "\nUpgrade: +" + CurrentWeaponLevel;
        debug += "\nMilestone: " + stats.milestoneLabel;
        debug += "\nSpecialization: " + (string.IsNullOrWhiteSpace(stats.specializationName) ? "Yok" : stats.specializationName);
        debug += "\nFinisher: " + (finisher != null ? finisher.displayName : "Eksik");
        debug += "\nFinisher Ready: " + (finisherReady ? "Evet" : "Hayir");

        if (!finisherReady && !string.IsNullOrWhiteSpace(reason))
            debug += "\nFinisher Block: " + reason;

        return debug;
    }
}

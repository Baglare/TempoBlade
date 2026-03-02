using UnityEngine;

/// <summary>
/// Oyundaki tum silahlarin merkezi veritabani.
/// Tek bir SO asset olusturup tum silahlari buraya eklersin.
/// StatsPanel, BlacksmithUI, ShopUI, PlayerCombat gibi scriptler
/// bu asset'i referans alarak tek kaynaktan silah listesine erisir.
///
/// Kullanim:
/// 1. Project panelinde sag tikla > Create > TempoBlade > Weapon Database
/// 2. Tum WeaponSO'lari "weapons" dizisine surekle
/// 3. Bu asset'i ihtiyac duyan scriptlere surekle (tek bir kez)
/// </summary>
[CreateAssetMenu(fileName = "WeaponDatabase", menuName = "TempoBlade/Weapon Database")]
public class WeaponDatabase : ScriptableObject
{
    [Tooltip("Oyundaki tum silahlar (baslangic silahi dahil)")]
    public WeaponSO[] weapons;

    /// <summary>
    /// Silah adina gore WeaponSO bulur. Yoksa null doner.
    /// </summary>
    public WeaponSO GetWeaponByName(string weaponName)
    {
        if (weapons == null) return null;
        foreach (var wpn in weapons)
        {
            if (wpn != null && wpn.weaponName == weaponName)
                return wpn;
        }
        return null;
    }
}

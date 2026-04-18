using UnityEngine;

/// <summary>
/// Parry ile geri gönderilen projectile'ın runtime davranışını tanımlar.
/// Tüm sayısal değerler perk controller tarafından data-driven şekilde doldurulur.
/// </summary>
[System.Serializable]
public struct DeflectContext
{
    public GameObject newOwner;
    public float speedMultiplier;
    public float damageMultiplier;
    public int pierceCount;
    public float suppressDuration;
    public int splitCount;
    public float splitDamageMultiplier;
    public float splitAngleSpread;
    public float splitSpeedMultiplier;
    public bool useSurfaceNormal;
    public Vector2 deflectSurfaceNormal;

    public static DeflectContext Default(GameObject owner)
    {
        return new DeflectContext
        {
            newOwner = owner,
            speedMultiplier = 1f,
            damageMultiplier = 1f,
            pierceCount = 0,
            suppressDuration = 0f,
            splitCount = 0,
            splitDamageMultiplier = 0.5f,
            splitAngleSpread = 20f,
            splitSpeedMultiplier = 1f,
            useSurfaceNormal = false,
            deflectSurfaceNormal = Vector2.up
        };
    }
}

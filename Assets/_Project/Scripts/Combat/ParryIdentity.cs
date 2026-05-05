using UnityEngine;

public enum ParryInteractionType
{
    Melee,
    Projectile
}

public enum ParryIdentityMode
{
    Default,
    ForceParryable,
    Unparryable
}

[DisallowMultipleComponent]
public class ParryIdentity : MonoBehaviour
{
    [Header("Identity")]
    public ParryIdentityMode mode = ParryIdentityMode.Default;
    public bool allowMeleeParry = true;
    public bool allowProjectileParry = true;
    public string debugLabel;

    public bool Allows(ParryInteractionType interactionType, bool defaultValue = true)
    {
        if (mode == ParryIdentityMode.ForceParryable)
            return true;

        if (mode == ParryIdentityMode.Unparryable)
            return false;

        return interactionType == ParryInteractionType.Melee
            ? allowMeleeParry && defaultValue
            : allowProjectileParry && defaultValue;
    }
}

public static class ParryIdentityUtility
{
    public static bool AllowsParry(GameObject sourceObject, ParryInteractionType interactionType, bool defaultValue = true)
    {
        if (sourceObject == null)
            return defaultValue;

        ParryIdentity identity = sourceObject.GetComponentInParent<ParryIdentity>();
        if (identity == null)
            return defaultValue;

        return identity.Allows(interactionType, defaultValue);
    }
}

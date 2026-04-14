using UnityEngine;

/// <summary>
/// Düşmanların parry'e özel reaksiyonlarını özelleştirmesine izin verir.
/// </summary>
public interface IParryReactive
{
    bool AllowParryExecute { get; }
    void OnParryReaction(ParryReactionContext context);
}

[System.Serializable]
public struct ParryReactionContext
{
    public bool isProjectile;
    public bool isPerfect;
    public bool breakGuard;
    public bool interruptOnly;
    public float duration;
    public GameObject instigator;
}

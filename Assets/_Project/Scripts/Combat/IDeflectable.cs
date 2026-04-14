using UnityEngine;

public interface IDeflectable
{
    /// <summary>
    /// Mermiyi verilen context ile sektirir.
    /// </summary>
    void Deflect(DeflectContext context);

    /// <summary>
    /// Merminin o anki sahibi.
    /// </summary>
    GameObject ObjectOwner { get; }

    /// <summary>
    /// Mermiyi ilk atan kaynak.
    /// </summary>
    GameObject SourceOwner { get; }

    /// <summary>
    /// Mermi daha once deflect edildi mi?
    /// </summary>
    bool IsDeflected { get; }
}

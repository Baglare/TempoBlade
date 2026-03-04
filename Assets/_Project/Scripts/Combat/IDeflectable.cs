using UnityEngine;

public interface IDeflectable
{
    /// <summary>
    /// Mermiyi sektiren kisiyi (yeni sahibi) ayarlayarak hedefini tersine cevirir.
    /// </summary>
    void Deflect(GameObject newOwner);

    /// <summary>
    /// Merminin o anki sahibini (kimin firlattigini veya en son kimin sektirdigini) dondurur.
    /// </summary>
    GameObject ObjectOwner { get; }

    /// <summary>
    /// Mermi daha önce sektirilmiş mi? Çift deflect'i engellemek için.
    /// </summary>
    bool IsDeflected { get; }
}

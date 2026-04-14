using UnityEngine;

/// <summary>
/// Başarılı parry sonucunda tetiklenen zengin olay verisi.
/// </summary>
[System.Serializable]
public struct ParryEventData
{
    public bool isRanged;
    public bool isPerfect;
    public GameObject source;
    public Vector2 parryDirection;
    public int blockedCount;
}

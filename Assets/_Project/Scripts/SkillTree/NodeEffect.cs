using UnityEngine;

/// <summary>
/// Bir node'un aktifken uyguladığı tek bir etki.
/// Numeric modifier, boolean flag, feature unlock veya runtime tag olabilir.
/// </summary>
[System.Serializable]
public struct NodeEffect
{
    public EffectType type;

    [Tooltip("Etki anahtarı. EffectKeyRegistry'deki const key'lerden biri olmalı.")]
    public string key;

    [Tooltip("Sadece StatModifier için: Flat veya Percent")]
    public ModifierOp modifierOp;

    [Tooltip("Sadece StatModifier için: sayısal değer")]
    public float numericValue;
}

/// <summary>
/// Node etkisinin türü.
/// </summary>
public enum EffectType
{
    /// <summary>Sayısal stat değişikliği (flat veya percent).</summary>
    StatModifier,
    /// <summary>Boolean bayrak (aktifse true, ör: "parryHeal").</summary>
    Flag,
    /// <summary>Mekanik feature açılımı (ör: "doubleJump").</summary>
    FeatureUnlock,
    /// <summary>Runtime etiketi (koşullu/geçici tag'ler).</summary>
    RuntimeTag
}

/// <summary>
/// StatModifier'ın uygulanma biçimi.
/// </summary>
public enum ModifierOp
{
    /// <summary>Düz toplama (+5, +10 gibi).</summary>
    Flat,
    /// <summary>Yüzde çarpanı (+0.1 = %10 artış gibi).</summary>
    Percent
}

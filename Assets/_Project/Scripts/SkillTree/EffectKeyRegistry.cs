using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Bilinen effect key'lerinin merkezi kaydı.
/// Katı enum değil — yeni key eklemek tek satır. Ama bilinmeyen key'ler uyarı verir.
/// </summary>
public static class EffectKeyRegistry
{
    // ═══════════ Stat Modifier Keys ═══════════
    public const string MaxHealth     = "maxHealth";
    public const string Damage        = "damage";
    public const string AttackSpeed   = "attackSpeed";
    public const string TempoGain     = "tempoGain";
    public const string ParryWindow   = "parryWindow";
    public const string ParryRecovery = "parryRecovery";
    public const string MoveSpeed     = "moveSpeed";
    public const string DashCooldown  = "dashCooldown";

    // ═══════════ Flag Keys ═══════════
    public const string ParryHeal     = "parryHeal";
    public const string DashDamage    = "dashDamage";
    public const string CritOnTempo   = "critOnTempo";

    // ═══════════ Feature Keys ═══════════
    public const string DoubleJump    = "doubleJump";
    public const string WallSlide     = "wallSlide";

    // ═══════════ Validator ═══════════
    private static readonly HashSet<string> _allKeys = new HashSet<string>
    {
        // Stat
        MaxHealth, Damage, AttackSpeed, TempoGain,
        ParryWindow, ParryRecovery, MoveSpeed, DashCooldown,
        // Flag
        ParryHeal, DashDamage, CritOnTempo,
        // Feature
        DoubleJump, WallSlide
    };

    /// <summary>
    /// Verilen key kayıtlı mı?
    /// </summary>
    public static bool IsKnownKey(string key)
    {
        return !string.IsNullOrEmpty(key) && _allKeys.Contains(key);
    }

    /// <summary>
    /// Key bilinmiyorsa Console'a uyarı yazar. Inspector veya runtime'da kullanılır.
    /// </summary>
    public static void WarnIfUnknown(string key, string context = "")
    {
        if (string.IsNullOrEmpty(key)) return;
        if (!_allKeys.Contains(key))
        {
            Debug.LogWarning($"[EffectKeyRegistry] Bilinmeyen key: '{key}' {context}. " +
                             "Bu key EffectKeyRegistry'ye eklenmemiş olabilir.");
        }
    }

    /// <summary>
    /// Yeni bir key'i çalışma zamanında kayıt altına alır (mod desteği veya dinamik ekleme için).
    /// </summary>
    public static void RegisterKey(string key)
    {
        if (!string.IsNullOrEmpty(key))
            _allKeys.Add(key);
    }
}

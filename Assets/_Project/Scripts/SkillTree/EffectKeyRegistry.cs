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
    public const string DashT1RangedDodge = "dash_t1_ranged_dodge";
    public const string DashT1MeleeDodge = "dash_t1_melee_dodge";
    public const string DashT1Counter = "dash_t1_counter";
    public const string DashT1TempoGain = "dash_t1_tempo_gain";
    public const string DashT1AttackSpeed = "dash_t1_attack_speed";
    public const string DashT2Selected = "dash_t2_selected";
    public const string DashHunterMark = "dash_hunter_mark";
    public const string DashHunterBlindSpot = "dash_hunter_blind_spot";
    public const string DashHunterFlow = "dash_hunter_flow";
    public const string DashHunterExecution = "dash_hunter_execution";
    public const string DashHunterSuccession = "dash_hunter_succession";
    public const string DashFlowMarkStream = "dash_flow_mark_stream";
    public const string DashFlowRebound = "dash_flow_rebound";
    public const string DashFlowChain = "dash_flow_chain";
    public const string DashFlowBlackHole = "dash_flow_black_hole";
    public const string DashFlowBlast = "dash_flow_blast";

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
        DashT1RangedDodge, DashT1MeleeDodge, DashT1Counter, DashT1TempoGain, DashT1AttackSpeed,
        DashT2Selected, DashHunterMark, DashHunterBlindSpot, DashHunterFlow, DashHunterExecution,
        DashHunterSuccession, DashFlowMarkStream, DashFlowRebound, DashFlowChain, DashFlowBlackHole, DashFlowBlast,
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

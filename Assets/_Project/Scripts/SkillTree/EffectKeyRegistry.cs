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

    // ═══════════ Dash Perk Keys (T1) ═══════════
    public const string DashProjectileDodge = "dashProjectileDodge";
    public const string DashMeleeDodge      = "dashMeleeDodge";
    public const string DashCounter         = "dashCounter";
    public const string DashTempoGain       = "dashTempoGain";
    public const string DashAttackSpeed     = "dashAttackSpeed";

    // ═══════════ Dash Perk Keys (T2 Commitment) ═══════════
    public const string DashT2Commitment    = "dashT2Commitment";

    // ═══════════ Dash Perk Keys (T2 Avcı) ═══════════
    public const string DashHuntMark        = "dashHuntMark";
    public const string DashBlindSpot       = "dashBlindSpot";
    public const string DashHuntFlow        = "dashHuntFlow";
    public const string DashExecute         = "dashExecute";
    public const string DashHuntCycle       = "dashHuntCycle";

    // ═══════════ Dash Perk Keys (T2 Akışçı) ═══════════
    public const string DashFlowMark        = "dashFlowMark";
    public const string DashSnapback        = "dashSnapback";
    public const string DashChainBounce     = "dashChainBounce";
    public const string DashBlackHole       = "dashBlackHole";
    public const string DashBurst           = "dashBurst";

    // ═══════════ Parry Perk Keys (T1) ═══════════
    public const string ParryReflect          = "parryReflect";
    public const string ParryPerfectTiming    = "parryPerfectTiming";
    public const string ParryCounterStance    = "parryCounterStance";
    public const string ParryPerfectBreak     = "parryPerfectBreak";
    public const string ParryRhythmReturn     = "parryRhythmReturn";

    // ═══════════ Parry Perk Keys (T2 Commitment) ═══════════
    public const string ParryT2Commitment     = "parryT2Commitment";

    // ═══════════ Parry Perk Keys (T2 Balistik) ═══════════
    public const string ParryReverseFront     = "parryReverseFront";
    public const string ParryOverdeflect      = "parryOverdeflect";
    public const string ParrySuppressiveTrace = "parrySuppressiveTrace";
    public const string ParryFracturedOrbit   = "parryFracturedOrbit";
    public const string ParryFeedback         = "parryFeedback";

    // ═══════════ Parry Perk Keys (T2 Mükemmeliyetçi) ═══════════
    public const string ParryCloseExecute     = "parryCloseExecute";
    public const string ParryFineEdge         = "parryFineEdge";
    public const string ParryHeavyRiposte     = "parryHeavyRiposte";
    public const string ParryRotatingCone     = "parryRotatingCone";
    public const string ParryPerfectCycle     = "parryPerfectCycle";

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
        // Dash T1
        DashProjectileDodge, DashMeleeDodge, DashCounter, DashTempoGain, DashAttackSpeed,
        // Dash T2
        DashT2Commitment,
        // Dash T2 Avcı
        DashHuntMark, DashBlindSpot, DashHuntFlow, DashExecute, DashHuntCycle,
        // Dash T2 Akışçı
        DashFlowMark, DashSnapback, DashChainBounce, DashBlackHole, DashBurst,
        // Parry T1
        ParryReflect, ParryPerfectTiming, ParryCounterStance, ParryPerfectBreak, ParryRhythmReturn,
        // Parry T2 Commitment
        ParryT2Commitment,
        // Parry T2 Balistik
        ParryReverseFront, ParryOverdeflect, ParrySuppressiveTrace, ParryFracturedOrbit, ParryFeedback,
        // Parry T2 Mükemmeliyetçi
        ParryCloseExecute, ParryFineEdge, ParryHeavyRiposte, ParryRotatingCone, ParryPerfectCycle,
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

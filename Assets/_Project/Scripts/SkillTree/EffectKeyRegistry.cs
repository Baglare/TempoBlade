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

    // ═══════════ Overdrive Perk Keys (T1) ═══════════
    public const string OverdriveHeatBuildup       = "overdriveHeatBuildup";
    public const string OverdriveThresholdBurst    = "overdriveThresholdBurst";
    public const string OverdriveRedPressure       = "overdriveRedPressure";
    public const string OverdriveOverflowImpulse   = "overdriveOverflowImpulse";
    public const string OverdriveFinalPush         = "overdriveFinalPush";

    // ═══════════ Overdrive Perk Keys (T2 Commitment) ═══════════
    public const string OverdriveT2Commitment      = "overdriveT2Commitment";

    // ═══════════ Overdrive Perk Keys (T2 Burst) ═══════════
    public const string OverdriveShortCircuit      = "overdriveShortCircuit";
    public const string OverdriveRedWindow         = "overdriveRedWindow";
    public const string OverdriveThresholdEcho     = "overdriveThresholdEcho";
    public const string OverdrivePressureBreak     = "overdrivePressureBreak";
    public const string OverdriveFinalFlare        = "overdriveFinalFlare";

    // ═══════════ Overdrive Perk Keys (T2 Predator) ═══════════
    public const string OverdriveBloodScent        = "overdriveBloodScent";
    public const string OverdriveChokingProximity  = "overdriveChokingProximity";
    public const string OverdrivePredatorAngle     = "overdrivePredatorAngle";
    public const string OverdrivePackBreaker       = "overdrivePackBreaker";
    public const string OverdriveExecutePressure   = "overdriveExecutePressure";

    // ═══════════ Cadence Perk Keys (T1) ═══════════
    public const string CadenceSteadyPulse         = "cadenceSteadyPulse";
    public const string CadenceTransitionRhythm    = "cadenceTransitionRhythm";
    public const string CadenceSoftFall            = "cadenceSoftFall";
    public const string CadenceMeasuredPower       = "cadenceMeasuredPower";
    public const string CadenceRhythmShield        = "cadenceRhythmShield";

    // ═══════════ Cadence Perk Keys (T2 Commitment) ═══════════
    public const string CadenceT2Commitment        = "cadenceT2Commitment";

    // ═══════════ Cadence Perk Keys (T2 Measured) ═══════════
    public const string CadenceMeasureLine         = "cadenceMeasureLine";
    public const string CadenceBalancePoint        = "cadenceBalancePoint";
    public const string CadenceTimedAccent         = "cadenceTimedAccent";
    public const string CadenceRecoveryReturn      = "cadenceRecoveryReturn";
    public const string CadencePerfectMeasure      = "cadencePerfectMeasure";

    // ═══════════ Cadence Perk Keys (T2 Flow) ═══════════
    public const string CadenceFlowRing            = "cadenceFlowRing";
    public const string CadenceSlidingContinuation = "cadenceSlidingContinuation";
    public const string CadenceWaveBounce          = "cadenceWaveBounce";
    public const string CadenceThresholdSurf       = "cadenceThresholdSurf";
    public const string CadenceOverflowHarmony     = "cadenceOverflowHarmony";

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
        // Overdrive T1
        OverdriveHeatBuildup, OverdriveThresholdBurst, OverdriveRedPressure, OverdriveOverflowImpulse, OverdriveFinalPush,
        // Overdrive T2
        OverdriveT2Commitment, OverdriveShortCircuit, OverdriveRedWindow, OverdriveThresholdEcho,
        OverdrivePressureBreak, OverdriveFinalFlare, OverdriveBloodScent, OverdriveChokingProximity,
        OverdrivePredatorAngle, OverdrivePackBreaker, OverdriveExecutePressure,
        // Cadence T1
        CadenceSteadyPulse, CadenceTransitionRhythm, CadenceSoftFall, CadenceMeasuredPower, CadenceRhythmShield,
        // Cadence T2
        CadenceT2Commitment, CadenceMeasureLine, CadenceBalancePoint, CadenceTimedAccent,
        CadenceRecoveryReturn, CadencePerfectMeasure, CadenceFlowRing, CadenceSlidingContinuation,
        CadenceWaveBounce, CadenceThresholdSurf, CadenceOverflowHarmony,
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

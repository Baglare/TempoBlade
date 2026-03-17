using UnityEngine;

[CreateAssetMenu(menuName = "TempoBlade/Skill Tree/Dash Skill Config", fileName = "DashSkillConfig", order = 120)]
public class DashSkillConfigSO : ScriptableObject
{
    [System.Serializable]
    public class Tier1RangedDodgeSettings
    {
        public float projectileDodgeWindow = 0.14f;
        public float successDetectDuration = 0.18f;
        public float safeExitPadding = 0.05f;
        public float minThreatDistance = 2.5f;
        public int maxProjectileNegationsPerDash = 1;
    }

    [System.Serializable]
    public class Tier1MeleeDodgeSettings
    {
        public float meleeDodgeWindow = 0.12f;
        public float successDetectDuration = 0.16f;
        public float safeExitPadding = 0.04f;
        public float minThreatDistance = 1.8f;
    }

    [System.Serializable]
    public class Tier1CounterSettings
    {
        public float counterWindow = 1.0f;
        public float counterDamageBonus = 0.35f;
        public float counterStaggerBonus = 0.20f;
        public int maxStoredCharges = 1;
    }

    [System.Serializable]
    public class Tier1TempoGainSettings
    {
        public float tempoPerOffensiveDash = 8f;
        public float offensiveDashDistance = 2.4f;
        public float threatCircleDistance = 2.8f;
        public float internalCooldown = 1.25f;
        public float maxTempoPerDash = 8f;
    }

    [System.Serializable]
    public class Tier1AttackSpeedSettings
    {
        public float fastAttackWindow = 0.75f;
        public float attackSpeedBonus = 0.20f;
        public float recoveryResetRatio = 0.60f;
        public float internalCooldown = 4.0f;
    }

    [System.Serializable]
    public class Tier2GlobalShiftSettings
    {
        public float parryTempoPenalty = 0.30f;
        public float parryCooldownPenalty = 0.20f;
        public float parryWindowPenalty = 0.20f;
        public float dashTempoEfficiencyBonus = 0.25f;
        public float dashCooldownImprovement = 0.20f;
        public float dashWindowQualityBonus = 0.15f;
    }

    [System.Serializable]
    public class HunterMarkSettings
    {
        public float markCooldown = 5.0f;
        public float activeCombatDuration = 1.25f;
        public float randomSelectionRange = 8.0f;
        public float outOfCombatTolerance = 1.5f;
        public float retargetDelay = 0.25f;
        public float retargetRange = 10.0f;
    }

    [System.Serializable]
    public class HunterBlindSpotSettings
    {
        public float frontConeAngle = 110f;
        public float validDashEndDistance = 2.2f;
        public float turnSlow = 0.35f;
        public float turnSlowDuration = 1.25f;
        public float stunDuration = 0.50f;
        public float counterDamageBonus = 0.40f;
    }

    [System.Serializable]
    public class HunterFlowSettings
    {
        public float preyProximity = 3.2f;
        public float dashCooldownRecoveryBonus = 0.30f;
        public float attackSpeedIcdRecoveryBonus = 0.35f;
        public float updateInterval = 0.20f;
    }

    [System.Serializable]
    public class HunterExecutionSettings
    {
        public float executeHealthPercent = 0.18f;
        public float rearConeAngle = 70f;
        public float validEntryDistance = 2.0f;
        public float collisionIgnoreDuration = 0.35f;
        public float inputWindow = 0.20f;
    }

    [System.Serializable]
    public class HunterSuccessionSettings
    {
        public float damagePerPrey = 0.01f;
        public float maxRoomDamageBonus = 0.15f;
        public float retargetDelay = 0.25f;
        public float retargetRange = 10.0f;
    }

    [System.Serializable]
    public class FlowMarkSettings
    {
        public float markWindow = 1.40f;
        public float markDuration = 6.0f;
        public float damagePerUniqueMark = 0.04f;
        public int maxUniqueMarks = 5;
        public float maxDamageBonus = 0.20f;
    }

    [System.Serializable]
    public class FlowReboundSettings
    {
        public float returnWindow = 0.90f;
        public float internalCooldown = 5.0f;
        public float reboundDuration = 0.10f;
        public bool reboundGivesInvulnerability = false;
    }

    [System.Serializable]
    public class FlowChainSettings
    {
        public int maxBounces = 2;
        public float firstBounceRatio = 0.60f;
        public float decayPerBounce = 0.15f;
        public float bounceRange = 5.0f;
    }

    [System.Serializable]
    public class FlowBlackHoleSettings
    {
        public int requiredUniqueMarks = 4;
        public float radius = 4.5f;
        public float pullDuration = 1.0f;
        public float pullStrength = 8.0f;
        public float internalCooldown = 10.0f;
    }

    [System.Serializable]
    public class FlowBlastSettings
    {
        public float blastWindow = 1.20f;
        public float baseDamageMultiplier = 2.20f;
        public float bonusPerConsumedMark = 0.20f;
        public float spreadDamageRatio = 0.80f;
        public bool consumeAllMarks = true;
    }

    [Header("Tier 1")]
    public Tier1RangedDodgeSettings rangedDodge = new Tier1RangedDodgeSettings();
    public Tier1MeleeDodgeSettings meleeDodge = new Tier1MeleeDodgeSettings();
    public Tier1CounterSettings counter = new Tier1CounterSettings();
    public Tier1TempoGainSettings tempoGain = new Tier1TempoGainSettings();
    public Tier1AttackSpeedSettings attackSpeed = new Tier1AttackSpeedSettings();

    [Header("Tier 2 Global Shift")]
    public Tier2GlobalShiftSettings tier2Global = new Tier2GlobalShiftSettings();

    [Header("Tier 2 Hunter Route")]
    public HunterMarkSettings hunterMark = new HunterMarkSettings();
    public HunterBlindSpotSettings hunterBlindSpot = new HunterBlindSpotSettings();
    public HunterFlowSettings hunterFlow = new HunterFlowSettings();
    public HunterExecutionSettings hunterExecution = new HunterExecutionSettings();
    public HunterSuccessionSettings hunterSuccession = new HunterSuccessionSettings();

    [Header("Tier 2 Flow Route")]
    public FlowMarkSettings flowMark = new FlowMarkSettings();
    public FlowReboundSettings flowRebound = new FlowReboundSettings();
    public FlowChainSettings flowChain = new FlowChainSettings();
    public FlowBlackHoleSettings flowBlackHole = new FlowBlackHoleSettings();
    public FlowBlastSettings flowBlast = new FlowBlastSettings();
}


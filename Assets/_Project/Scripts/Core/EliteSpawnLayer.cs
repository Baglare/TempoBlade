using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public enum ElitePressureDebugPreset
{
    None = 0,
    Low = 1,
    Medium = 2,
    High = 3,
    Extreme = 4
}

[Serializable]
public class EliteSpawnConfig
{
    [Header("Enable")]
    public bool enableEliteConversion = true;

    [Header("Pressure")]
    public float basePressure = 0.18f;
    public float waveIndexPressureStep = 0.12f;
    public float runProgressPressureStep = 0.24f;
    public float easyDifficultyModifier = -0.08f;
    public float normalDifficultyModifier = 0f;
    public float hardDifficultyModifier = 0.14f;
    public float eliteEncounterModifier = 0.1f;
    public float miniBossEncounterModifier = 0.16f;
    public float bossEncounterModifier = 0.2f;

    [Header("Budget")]
    public float baseEliteBudget = 0.85f;
    public float budgetPerPressure = 1.75f;

    [Header("Conversion")]
    [Range(0f, 1f)] public float candidateScanBase = 0.45f;
    [Range(0f, 1f)] public float candidateScanPressureScale = 0.28f;
    [Range(0f, 1f)] public float conversionChanceBase = 0.12f;
    [Range(0f, 1f)] public float conversionChancePerPressure = 0.26f;
    [Range(0f, 1f)] public float maxConversionChance = 0.9f;

    [Header("Repeat Damping")]
    [Range(0f, 1f)] public float repeatDampingPerOccurrence = 0.12f;
    [Range(0f, 1f)] public float minimumRepeatWeight = 0.55f;
    public int repeatHistoryDepth = 8;

    [Header("Adaptive")]
    public bool enableAdaptiveModifier = true;
    public float adaptivePressureMagnitude = 0.18f;
}

[Serializable]
public class EliteSpawnDebugOverrides
{
    public bool enablePressureOverride;
    [Range(0f, 3f)] public float pressureOverride = 1f;
    public ElitePressureDebugPreset pressurePreset = ElitePressureDebugPreset.None;
    public EnemySO forceEliteEnemyType;
    public EliteProfileSO forceEliteProfileOverride;
}

[Serializable]
public class EliteSpawnConversionRecord
{
    public string enemyTypeName = "";
    public string eliteProfileName = "";
    public float eliteCost;
    public bool forced;
}

[Serializable]
public class EliteSpawnWaveMetrics
{
    public int waveIndex;
    public int eligibleCount;
    public int evaluatedCount;
    public int conversionCount;
    public int forcedConversionCount;
    public float basePressure;
    public float adaptiveModifier;
    public float effectivePressure;
    public float initialBudget;
    public float totalEliteCost;
    public readonly List<EliteSpawnConversionRecord> conversions = new List<EliteSpawnConversionRecord>();

    public void Reset(int targetWaveIndex)
    {
        waveIndex = targetWaveIndex;
        eligibleCount = 0;
        evaluatedCount = 0;
        conversionCount = 0;
        forcedConversionCount = 0;
        basePressure = 0f;
        adaptiveModifier = 0f;
        effectivePressure = 0f;
        initialBudget = 0f;
        totalEliteCost = 0f;
        conversions.Clear();
    }
}

public sealed class EliteSpawnHistoryState
{
    private readonly List<string> recentEliteKeys = new List<string>();

    public void Clear()
    {
        recentEliteKeys.Clear();
    }

    public float GetRepeatWeight(string eliteKey, EliteSpawnConfig config)
    {
        if (string.IsNullOrEmpty(eliteKey) || config == null || recentEliteKeys.Count == 0)
            return 1f;

        float penalty = 0f;
        int maxDepth = Mathf.Max(1, config.repeatHistoryDepth);
        int recentCount = recentEliteKeys.Count;
        for (int i = 0; i < recentCount; i++)
        {
            int reverseIndex = recentCount - 1 - i;
            if (!string.Equals(recentEliteKeys[reverseIndex], eliteKey, StringComparison.Ordinal))
                continue;

            float recencyWeight = Mathf.Lerp(1f, 0.35f, i / Mathf.Max(1f, maxDepth - 1f));
            penalty += config.repeatDampingPerOccurrence * recencyWeight;
        }

        return Mathf.Clamp(1f - penalty, config.minimumRepeatWeight, 1f);
    }

    public void Record(string eliteKey, EliteSpawnConfig config)
    {
        if (string.IsNullOrEmpty(eliteKey) || config == null)
            return;

        recentEliteKeys.Add(eliteKey);
        int maxDepth = Mathf.Max(1, config.repeatHistoryDepth);
        while (recentEliteKeys.Count > maxDepth)
            recentEliteKeys.RemoveAt(0);
    }
}

public sealed class EliteSpawnPlanEntry
{
    public EnemySO enemyType;
    public EnemySpawn sourceSpawn;
    public int sourceGroupIndex;
    public int instanceIndex;
    public float spawnDelay;
    public EliteProfileSO eliteProfile;
    public bool isElite;

    public EliteSpawnPlanEntry(EnemySpawn source, int groupIndex, int targetInstanceIndex)
    {
        sourceSpawn = source;
        enemyType = source != null ? source.enemyType : null;
        sourceGroupIndex = groupIndex;
        instanceIndex = targetInstanceIndex;
        spawnDelay = source != null ? source.spawnDelay : 0f;
    }

    public void ApplyElite(EliteProfileSO profile)
    {
        eliteProfile = profile;
        isElite = profile != null;
    }
}

public static class EliteSpawnLayer
{
    private sealed class Candidate
    {
        public EliteSpawnPlanEntry entry;
        public EliteProfileSO profile;
        public float cost;
        public float legacyChanceBias;
        public string repeatKey;
        public bool debugForcedType;
    }

    public static EliteSpawnWaveMetrics ApplyConversion(
        RoomSO room,
        RoomWave wave,
        int waveIndex,
        List<EliteSpawnPlanEntry> spawnPlan,
        PlayerCombat playerCombat,
        EliteSpawnHistoryState history,
        EliteSpawnDebugOverrides debugOverrides,
        EliteSpawnWaveMetrics reusableMetrics = null)
    {
        EliteSpawnWaveMetrics metrics = reusableMetrics ?? new EliteSpawnWaveMetrics();
        metrics.Reset(waveIndex);

        if (room == null || wave == null || spawnPlan == null || spawnPlan.Count == 0)
            return metrics;

        EliteSpawnConfig config = room.eliteSpawnConfig ?? new EliteSpawnConfig();
        if (!config.enableEliteConversion)
            return metrics;

        metrics.basePressure = CalculateBasePressure(room, waveIndex, config);
        metrics.adaptiveModifier = CalculateAdaptiveModifier(playerCombat, config);
        metrics.effectivePressure = ResolveEffectivePressure(metrics.basePressure, metrics.adaptiveModifier, debugOverrides);
        metrics.initialBudget = Mathf.Max(0f, config.baseEliteBudget + metrics.effectivePressure * config.budgetPerPressure);

        List<Candidate> candidates = new List<Candidate>();
        float remainingBudget = metrics.initialBudget;

        for (int i = 0; i < spawnPlan.Count; i++)
        {
            EliteSpawnPlanEntry entry = spawnPlan[i];
            if (!TryBuildCandidate(entry, debugOverrides, out Candidate candidate))
                continue;

            metrics.eligibleCount++;

            if (entry.sourceSpawn != null && entry.sourceSpawn.eliteSpawnMode == EliteSpawnMode.ForceElite)
            {
                ApplyConversion(entry, candidate.profile, candidate.cost, true, metrics, config, history);
                remainingBudget = Mathf.Max(0f, remainingBudget - candidate.cost);
                continue;
            }

            if (candidate.debugForcedType)
            {
                ApplyConversion(entry, candidate.profile, candidate.cost, true, metrics, config, history);
                remainingBudget = Mathf.Max(0f, remainingBudget - candidate.cost);
                continue;
            }

            candidates.Add(candidate);
        }

        if (candidates.Count == 0 || remainingBudget <= 0f)
            return metrics;

        ShuffleCandidates(candidates);

        float scanFraction = Mathf.Clamp01(config.candidateScanBase + metrics.effectivePressure * config.candidateScanPressureScale);
        int evaluatedTargetCount = Mathf.Clamp(Mathf.CeilToInt(candidates.Count * scanFraction), 1, candidates.Count);

        for (int i = 0; i < candidates.Count && metrics.evaluatedCount < evaluatedTargetCount; i++)
        {
            Candidate candidate = candidates[i];
            metrics.evaluatedCount++;

            if (remainingBudget + 0.001f < candidate.cost)
                continue;

            float repeatWeight = history != null ? history.GetRepeatWeight(candidate.repeatKey, config) : 1f;
            float conversionChance = Mathf.Clamp01(config.conversionChanceBase + metrics.effectivePressure * config.conversionChancePerPressure);
            if (candidate.legacyChanceBias > 0f)
                conversionChance = Mathf.Max(conversionChance, candidate.legacyChanceBias);
            conversionChance = Mathf.Min(config.maxConversionChance, conversionChance * repeatWeight);

            if (UnityEngine.Random.value > conversionChance)
                continue;

            ApplyConversion(candidate.entry, candidate.profile, candidate.cost, false, metrics, config, history);
            remainingBudget = Mathf.Max(0f, remainingBudget - candidate.cost);
            if (remainingBudget <= 0f)
                break;
        }

        return metrics;
    }

    private static bool TryBuildCandidate(EliteSpawnPlanEntry entry, EliteSpawnDebugOverrides debugOverrides, out Candidate candidate)
    {
        candidate = null;
        if (entry == null || entry.sourceSpawn == null || entry.enemyType == null)
            return false;

        if (!entry.enemyType.eliteEligible)
            return false;

        if (entry.sourceSpawn.eliteSpawnMode == EliteSpawnMode.LegacyPrefabDefault)
            return false;

        bool debugForcedType = debugOverrides != null &&
                               debugOverrides.forceEliteEnemyType != null &&
                               debugOverrides.forceEliteEnemyType == entry.enemyType;

        EliteProfileSO profile = debugForcedType && debugOverrides.forceEliteProfileOverride != null
            ? debugOverrides.forceEliteProfileOverride
            : entry.sourceSpawn.eliteProfile;

        if (profile == null)
            return false;

        candidate = new Candidate
        {
            entry = entry,
            profile = profile,
            cost = CalculateEliteCost(entry.enemyType),
            legacyChanceBias = entry.sourceSpawn.eliteSpawnMode == EliteSpawnMode.ChanceBased
                ? Mathf.Clamp01(entry.sourceSpawn.eliteChance)
                : 0f,
            repeatKey = BuildRepeatKey(profile),
            debugForcedType = debugForcedType
        };
        return true;
    }

    private static void ApplyConversion(
        EliteSpawnPlanEntry entry,
        EliteProfileSO profile,
        float cost,
        bool forced,
        EliteSpawnWaveMetrics metrics,
        EliteSpawnConfig config,
        EliteSpawnHistoryState history)
    {
        if (entry == null || profile == null || metrics == null)
            return;

        entry.ApplyElite(profile);
        metrics.conversionCount++;
        metrics.totalEliteCost += cost;
        if (forced)
            metrics.forcedConversionCount++;

        metrics.conversions.Add(new EliteSpawnConversionRecord
        {
            enemyTypeName = entry.enemyType != null ? entry.enemyType.enemyName : "Enemy",
            eliteProfileName = profile.name,
            eliteCost = cost,
            forced = forced
        });

        history?.Record(BuildRepeatKey(profile), config);
    }

    private static float CalculateBasePressure(RoomSO room, int waveIndex, EliteSpawnConfig config)
    {
        if (config == null)
            return 0f;

        float pressure = config.basePressure;
        pressure += Mathf.Max(0, waveIndex) * config.waveIndexPressureStep;

        if (RunManager.Instance != null && RunManager.Instance.roomSequence != null && RunManager.Instance.roomSequence.Count > 1)
        {
            float runProgress01 = Mathf.Clamp01((float)RunManager.Instance.roomsCleared / (RunManager.Instance.roomSequence.Count - 1));
            pressure += runProgress01 * config.runProgressPressureStep;
        }

        if (room != null)
        {
            switch (room.difficulty)
            {
                case DifficultyTier.Easy: pressure += config.easyDifficultyModifier; break;
                case DifficultyTier.Hard: pressure += config.hardDifficultyModifier; break;
                case DifficultyTier.Normal:
                default: pressure += config.normalDifficultyModifier; break;
            }

            EncounterType encounterType = room.encounterType;
            if (room.isBossRoom && encounterType == EncounterType.Normal)
                encounterType = EncounterType.Boss;

            switch (encounterType)
            {
                case EncounterType.Elite: pressure += config.eliteEncounterModifier; break;
                case EncounterType.MiniBoss: pressure += config.miniBossEncounterModifier; break;
                case EncounterType.Boss: pressure += config.bossEncounterModifier; break;
            }
        }

        return Mathf.Max(0f, pressure);
    }

    private static float CalculateAdaptiveModifier(PlayerCombat playerCombat, EliteSpawnConfig config)
    {
        if (config == null || !config.enableAdaptiveModifier || playerCombat == null || playerCombat.maxHealth <= 0f)
            return 0f;

        float healthRatio = Mathf.Clamp01(playerCombat.currentHealth / playerCombat.maxHealth);
        float centered = Mathf.InverseLerp(0.2f, 0.9f, healthRatio) * 2f - 1f;
        return centered * config.adaptivePressureMagnitude;
    }

    private static float ResolveEffectivePressure(float basePressure, float adaptiveModifier, EliteSpawnDebugOverrides debugOverrides)
    {
        if (debugOverrides != null)
        {
            if (debugOverrides.enablePressureOverride)
                return Mathf.Max(0f, debugOverrides.pressureOverride);

            float presetPressure = ResolvePresetPressure(debugOverrides.pressurePreset);
            if (presetPressure >= 0f)
                return presetPressure;
        }

        return Mathf.Max(0f, basePressure + adaptiveModifier);
    }

    private static float ResolvePresetPressure(ElitePressureDebugPreset preset)
    {
        switch (preset)
        {
            case ElitePressureDebugPreset.Low: return 0.35f;
            case ElitePressureDebugPreset.Medium: return 0.8f;
            case ElitePressureDebugPreset.High: return 1.25f;
            case ElitePressureDebugPreset.Extreme: return 1.8f;
            case ElitePressureDebugPreset.None:
            default:
                return -1f;
        }
    }

    private static float CalculateEliteCost(EnemySO enemyType)
    {
        if (enemyType == null)
            return 1f;

        float healthWeight = Mathf.Clamp(enemyType.maxHealth / 110f, 0.3f, 2f);
        float damageWeight = Mathf.Clamp(enemyType.damage / 16f, 0.2f, 1.6f);
        float rangeWeight = enemyType.attackRange >= 2.5f ? 0.2f : 0f;
        float mobilityWeight = enemyType.moveSpeed >= 4.2f ? 0.15f : 0f;
        float baseCost = 0.45f + healthWeight * 0.45f + damageWeight * 0.3f + rangeWeight + mobilityWeight;
        return Mathf.Max(0.2f, baseCost * Mathf.Max(0.1f, enemyType.eliteCostMultiplier));
    }

    private static string BuildRepeatKey(EliteProfileSO profile)
    {
        if (profile == null)
            return string.Empty;

        return $"{profile.name}:{profile.eliteMechanicType}";
    }

    private static void ShuffleCandidates(List<Candidate> candidates)
    {
        if (candidates == null)
            return;

        for (int i = 0; i < candidates.Count - 1; i++)
        {
            int swapIndex = UnityEngine.Random.Range(i, candidates.Count);
            Candidate temp = candidates[i];
            candidates[i] = candidates[swapIndex];
            candidates[swapIndex] = temp;
        }
    }
}

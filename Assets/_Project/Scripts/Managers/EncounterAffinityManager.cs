using System.Collections.Generic;
using UnityEngine;

public class EncounterAffinityManager : MonoBehaviour
{
    public static EncounterAffinityManager Instance { get; private set; }

    private readonly Dictionary<string, AxisEncounterCounters> axisCounters = new Dictionary<string, AxisEncounterCounters>();
    private readonly Dictionary<string, RepeatedEventState> repeatedEvents = new Dictionary<string, RepeatedEventState>();

    private CombatTelemetryHub telemetry;
    private RoomSO activeRoom;
    private bool isSessionActive;
    private float sampleTimer;
    private float lastThresholdEntryTime = -999f;
    private bool thresholdHadPayoff;
    private float lastTempoDropTime = -999f;
    private TreeProgressionConfigSO fallbackConfig;

    public static EncounterAffinityManager EnsureInstance()
    {
        if (Instance != null)
            return Instance;

        var go = new GameObject("EncounterAffinityManager");
        Instance = go.AddComponent<EncounterAffinityManager>();
        DontDestroyOnLoad(go);
        return Instance;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        if (!isSessionActive || telemetry == null)
            return;

        sampleTimer -= Time.deltaTime;
        if (sampleTimer <= 0f)
        {
            sampleTimer = 0.5f;
            SampleContinuousScores(0.5f);
        }

        var config = GetConfig();
        if (lastThresholdEntryTime > 0f &&
            !thresholdHadPayoff &&
            Time.time - lastThresholdEntryTime > config.thresholdPayoffWindow)
        {
            AddScore("axis_overdrive", ScoreSlot.Penalty, 1f, null, CombatActionType.ThresholdCross);
            thresholdHadPayoff = true;
        }
    }

    public void StartEncounter(RoomSO room, GameObject player)
    {
        EndEncounterIfNeeded(false);

        activeRoom = room;
        isSessionActive = true;
        axisCounters.Clear();
        repeatedEvents.Clear();
        EnsureAxisCounters();

        telemetry = CombatTelemetryHub.EnsureFor(player);
        if (telemetry != null)
        {
            telemetry.OnCombatEvent -= HandleCombatEvent;
            telemetry.OnCombatEvent += HandleCombatEvent;
        }

        sampleTimer = 0.5f;
        lastThresholdEntryTime = -999f;
        thresholdHadPayoff = false;
        lastTempoDropTime = -999f;
    }

    public void EndEncounter(RoomSO room)
    {
        if (!isSessionActive)
            return;

        EndEncounterIfNeeded(true);
    }

    public float GetLastAffinity(string axisId)
    {
        return axisCounters.TryGetValue(axisId, out var counters) ? counters.lastAffinity : 0f;
    }

    private void EndEncounterIfNeeded(bool awardXp)
    {
        if (!isSessionActive)
            return;

        if (telemetry != null)
            telemetry.OnCombatEvent -= HandleCombatEvent;

        SampleContinuousScores(0.5f);

        foreach (var kv in axisCounters)
            kv.Value.lastAffinity = CalculateAffinity(kv.Key, kv.Value);

        if (awardXp)
            AwardEncounterXp();

        isSessionActive = false;
        telemetry = null;
        activeRoom = null;
    }

    private void HandleCombatEvent(CombatTelemetryEvent e)
    {
        if (!isSessionActive || telemetry == null || !telemetry.IsInCombat)
            return;

        if (IsPassiveProgressionTarget(e.target))
            return;

        switch (e.actionType)
        {
            case CombatActionType.Dash:
                HandleDashAction();
                HandleCadenceAction(e.actionType);
                break;

            case CombatActionType.DodgeThreat:
                AddScore("axis_dash", ScoreSlot.Primary, 1.2f, e.source, e.actionType);
                if (telemetry.NearbyEnemyCount > 0)
                    AddScore("axis_dash", ScoreSlot.Secondary, 0.6f, e.source, e.actionType);
                break;

            case CombatActionType.Parry:
                if (e.value > 0f)
                    HandleSuccessfulParry(e);
                else
                    HandleParryStart();
                HandleCadenceAction(e.actionType);
                break;

            case CombatActionType.PerfectParry:
                HandleSuccessfulParry(e);
                AddScore("axis_parry", ScoreSlot.Secondary, 1.2f, e.source, e.actionType);
                HandleCadenceAction(e.actionType);
                break;

            case CombatActionType.ParryFail:
                AddScore("axis_parry", ScoreSlot.Penalty, 1f, null, e.actionType);
                AddScore("axis_cadence", ScoreSlot.Penalty, 0.6f, null, e.actionType);
                break;

            case CombatActionType.Attack:
            case CombatActionType.Skill:
                HandleCadenceAction(e.actionType);
                break;

            case CombatActionType.Hit:
            case CombatActionType.Kill:
                HandleHitOrKill(e);
                break;

            case CombatActionType.DamageTaken:
                AddScore("axis_overdrive", ScoreSlot.Penalty, 0.7f, null, e.actionType);
                AddScore("axis_cadence", ScoreSlot.Penalty, 0.8f, null, e.actionType);
                lastTempoDropTime = Time.time;
                break;

            case CombatActionType.ThresholdCross:
                HandleThresholdCross(e);
                break;

            case CombatActionType.TargetSwitch:
                AddScore("axis_cadence", ScoreSlot.Fifth, 0.5f, e.target != null ? e.target.gameObject : null, e.actionType);
                break;

            case CombatActionType.Whiff:
                AddScore("axis_cadence", ScoreSlot.Penalty, 0.8f, null, e.actionType);
                AddScore("axis_overdrive", ScoreSlot.Penalty, 0.5f, null, e.actionType);
                break;

            case CombatActionType.Deflect:
                AddScore("axis_parry", ScoreSlot.Utility, e.killed ? 1.2f : 0.8f, e.target != null ? e.target.gameObject : null, e.actionType);
                break;
        }
    }

    private void HandleDashAction()
    {
        if (telemetry.IsUnderThreat)
            AddScore("axis_dash", ScoreSlot.Primary, 0.5f, null, CombatActionType.Dash);
        else if (telemetry.NearbyEnemyCount <= 0)
            AddScore("axis_dash", ScoreSlot.Penalty, 0.45f, null, CombatActionType.Dash);

        if (telemetry.NearbyEnemyCount > 0)
            AddScore("axis_dash", ScoreSlot.Secondary, 0.35f, null, CombatActionType.Dash);
    }

    private void HandleParryStart()
    {
        if (!telemetry.IsUnderThreat)
            AddScore("axis_parry", ScoreSlot.Penalty, 0.45f, null, CombatActionType.Parry);
    }

    private void HandleSuccessfulParry(CombatTelemetryEvent e)
    {
        AddScore("axis_parry", ScoreSlot.Primary, 1f, e.source, e.actionType);
        if (e.isRanged)
            AddScore("axis_parry", ScoreSlot.Utility, 0.75f, e.source, e.actionType);
    }

    private void HandleHitOrKill(CombatTelemetryEvent e)
    {
        bool isHighTempo = TempoManager.Instance != null && TempoManager.Instance.tempo >= GetConfig().highTempoThreshold;
        bool afterDash = Time.time - telemetry.LastDashTime <= GetConfig().followUpWindow;
        bool afterParry = Time.time - telemetry.LastParryTime <= GetConfig().followUpWindow;

        if (afterDash)
            AddScore("axis_dash", ScoreSlot.Conversion, e.killed ? 1.2f : 0.8f, e.target != null ? e.target.gameObject : null, e.actionType);

        if (afterDash && e.target != null && IsAtFlankOrRear(e.target))
            AddScore("axis_dash", ScoreSlot.Utility, 0.8f, e.target.gameObject, e.actionType);

        if (afterParry || e.counterMultiplier > 0f)
            AddScore("axis_parry", ScoreSlot.Conversion, e.killed ? 1.3f : 0.9f, e.target != null ? e.target.gameObject : null, e.actionType);

        if (isHighTempo)
        {
            AddScore("axis_overdrive", ScoreSlot.Conversion, e.killed ? 1.5f : 0.55f, e.target != null ? e.target.gameObject : null, e.actionType);
            if (Time.time - lastThresholdEntryTime <= GetConfig().thresholdPayoffWindow)
            {
                AddScore("axis_overdrive", ScoreSlot.Utility, e.killed ? 1.2f : 0.7f, e.target != null ? e.target.gameObject : null, e.actionType);
                thresholdHadPayoff = true;
            }
        }

        if (Time.time - lastTempoDropTime <= 3f)
            AddScore("axis_cadence", ScoreSlot.Utility, 0.55f, e.target != null ? e.target.gameObject : null, e.actionType);
    }

    private void HandleThresholdCross(CombatTelemetryEvent e)
    {
        if (e.value <= (float)TempoManager.TempoTier.T0)
            return;

        AddScore("axis_overdrive", ScoreSlot.Primary, 1f, null, e.actionType);
        lastThresholdEntryTime = Time.time;
        thresholdHadPayoff = false;

        AddScore("axis_cadence", ScoreSlot.Utility, 0.25f, null, e.actionType);
    }

    private void HandleCadenceAction(CombatActionType actionType)
    {
        var actions = telemetry.GetRecentActions();
        if (actions.Count < 2)
            return;

        CombatActionType previous = actions[actions.Count - 2];
        if (previous != actionType && previous != CombatActionType.None)
            AddScore("axis_cadence", ScoreSlot.Secondary, 0.45f, null, actionType);
        else
            AddScore("axis_cadence", ScoreSlot.Penalty, 0.15f, null, actionType);
    }

    private void SampleContinuousScores(float dt)
    {
        if (telemetry == null || !telemetry.IsInCombat)
            return;

        var config = GetConfig();
        if (TempoManager.Instance != null)
        {
            if (TempoManager.Instance.tempo >= config.highTempoThreshold)
                AddScore("axis_overdrive", ScoreSlot.Secondary, dt, null, CombatActionType.None);

            if (telemetry.TempoDecayRateRecent <= 1f && TempoManager.Instance.tempo > 0f)
                AddScore("axis_cadence", ScoreSlot.Primary, dt * 0.6f, null, CombatActionType.None);

            if (telemetry.TimeInCurrentTier >= 2f)
                AddScore("axis_cadence", ScoreSlot.Conversion, dt * 0.5f, null, CombatActionType.None);
        }

        if (Time.time - telemetry.LastDashTime <= config.followUpWindow && telemetry.FlankOrRearTime > 0f)
            AddScore("axis_dash", ScoreSlot.Utility, dt * 0.5f, null, CombatActionType.None);

        if (telemetry.NearbyEnemyCount > 0 && telemetry.TimeNearCurrentTarget > 1f)
            AddScore("axis_cadence", ScoreSlot.Fifth, dt * 0.35f, null, CombatActionType.None);
    }

    private void AwardEncounterXp()
    {
        var manager = AxisProgressionManager.Instance;
        if (manager == null || manager.database == null)
            return;
        if (manager.IsTesterMode)
            return;

        var database = manager.database;
        if (database.opposingPairs == null)
            return;

        foreach (var pair in database.opposingPairs)
        {
            if (pair == null || pair.axisA == null || pair.axisB == null)
                continue;

            ProgressionAxisSO target = ResolveXpTarget(pair.axisA, pair.axisB, manager);
            if (target == null)
                continue;

            float affinity = GetLastAffinity(target.axisId);
            if (affinity < GetConfig().minimumAffinityForXp)
                continue;

            float xp = CalculateXp(target.axisId, affinity);
            manager.AddTreeXp(target, xp);
        }
    }

    private ProgressionAxisSO ResolveXpTarget(ProgressionAxisSO axisA, ProgressionAxisSO axisB, AxisProgressionManager manager)
    {
        bool aCommitted = manager.IsAxisCommitted(axisA);
        bool bCommitted = manager.IsAxisCommitted(axisB);

        if (aCommitted && !bCommitted) return axisA;
        if (bCommitted && !aCommitted) return axisB;

        float aAffinity = GetLastAffinity(axisA.axisId);
        float bAffinity = GetLastAffinity(axisB.axisId);
        return aAffinity >= bAffinity ? axisA : axisB;
    }

    private float CalculateXp(string axisId, float affinity)
    {
        var config = GetConfig();
        EncounterType type = activeRoom != null ? activeRoom.encounterType : EncounterType.Normal;
        if (activeRoom != null && activeRoom.isBossRoom && type == EncounterType.Normal)
            type = EncounterType.Boss;

        DifficultyTier difficulty = activeRoom != null ? activeRoom.difficulty : DifficultyTier.Normal;
        float baseXp = config.GetBaseXp(type);
        float varietyBonus = CalculateVarietyBonus(axisId);
        return baseXp *
               (config.baseXpFloor + config.affinityXpScale * Mathf.Clamp01(affinity)) *
               varietyBonus *
               config.GetDifficultyMultiplier(difficulty);
    }

    private float CalculateAffinity(string axisId, AxisEncounterCounters counters)
    {
        var config = GetConfig();
        AxisAffinityWeights weights = config.GetWeights(axisId);

        float weighted =
            counters.primary * weights.primaryWeight +
            counters.secondary * weights.secondaryWeight +
            counters.conversion * weights.conversionWeight +
            counters.utility * weights.utilityWeight +
            counters.fifth * weights.fifthWeight -
            counters.penalty * weights.penaltyWeight;

        return Mathf.Clamp01(weighted / Mathf.Max(0.01f, config.scoreForFullAffinity));
    }

    private float CalculateVarietyBonus(string axisId)
    {
        if (!axisCounters.TryGetValue(axisId, out var counters))
            return 1f;

        int activeCategories = 0;
        if (counters.primary > 0.1f) activeCategories++;
        if (counters.secondary > 0.1f) activeCategories++;
        if (counters.conversion > 0.1f) activeCategories++;
        if (counters.utility > 0.1f) activeCategories++;
        if (counters.fifth > 0.1f) activeCategories++;

        float t = Mathf.Clamp01(activeCategories / 4f);
        return Mathf.Lerp(1f, GetConfig().maxVarietyBonus, t);
    }

    private void AddScore(string axisId, ScoreSlot slot, float amount, GameObject target, CombatActionType actionType)
    {
        if (amount <= 0f)
            return;

        EnsureAxisCounters();
        if (!axisCounters.TryGetValue(axisId, out var counters))
            return;

        float finalAmount = amount * GetDiminishingMultiplier(axisId, actionType, target);
        switch (slot)
        {
            case ScoreSlot.Primary: counters.primary += finalAmount; break;
            case ScoreSlot.Secondary: counters.secondary += finalAmount; break;
            case ScoreSlot.Conversion: counters.conversion += finalAmount; break;
            case ScoreSlot.Utility: counters.utility += finalAmount; break;
            case ScoreSlot.Fifth: counters.fifth += finalAmount; break;
            case ScoreSlot.Penalty: counters.penalty += finalAmount; break;
        }
    }

    private float GetDiminishingMultiplier(string axisId, CombatActionType actionType, GameObject target)
    {
        if (target == null || actionType == CombatActionType.None)
            return 1f;

        var config = GetConfig();
        string key = $"{axisId}:{actionType}:{target.GetInstanceID()}";
        repeatedEvents.TryGetValue(key, out var state);

        if (Time.time - state.lastTime > config.repeatedEventWindow)
            state.count = 0;

        float multiplier;
        if (state.count <= 0) multiplier = config.firstRepeatMultiplier;
        else if (state.count == 1) multiplier = config.secondRepeatMultiplier;
        else if (state.count == 2) multiplier = config.thirdRepeatMultiplier;
        else multiplier = config.furtherRepeatMultiplier;

        state.count++;
        state.lastTime = Time.time;
        repeatedEvents[key] = state;
        return multiplier;
    }

    private bool IsPassiveProgressionTarget(EnemyBase target)
    {
        return target != null && target.enemyData != null && !target.enemyData.countsForProgression;
    }

    private bool IsAtFlankOrRear(EnemyBase enemy)
    {
        if (enemy == null)
            return false;

        Vector2 enemyForward = enemy.transform.localScale.x >= 0f ? Vector2.right : Vector2.left;
        Vector2 toPlayer = ((Vector2)telemetry.transform.position - (Vector2)enemy.transform.position).normalized;
        return Vector2.Dot(enemyForward, toPlayer) < 0.35f;
    }

    private void EnsureAxisCounters()
    {
        EnsureAxisCounter("axis_dash");
        EnsureAxisCounter("axis_parry");
        EnsureAxisCounter("axis_overdrive");
        EnsureAxisCounter("axis_cadence");
    }

    private void EnsureAxisCounter(string axisId)
    {
        if (!axisCounters.ContainsKey(axisId))
            axisCounters[axisId] = new AxisEncounterCounters();
    }

    private TreeProgressionConfigSO GetConfig()
    {
        if (AxisProgressionManager.Instance != null)
            return AxisProgressionManager.Instance.GetProgressionConfig();

        if (fallbackConfig == null)
            fallbackConfig = ScriptableObject.CreateInstance<TreeProgressionConfigSO>();

        return fallbackConfig;
    }

    private enum ScoreSlot
    {
        Primary,
        Secondary,
        Conversion,
        Utility,
        Fifth,
        Penalty
    }

    private class AxisEncounterCounters
    {
        public float primary;
        public float secondary;
        public float conversion;
        public float utility;
        public float fifth;
        public float penalty;
        public float lastAffinity;
    }

    private struct RepeatedEventState
    {
        public int count;
        public float lastTime;
    }
}

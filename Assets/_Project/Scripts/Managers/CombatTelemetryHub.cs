using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class CombatTelemetryHub : MonoBehaviour
{
    private const int MaxActionHistory = 12;

    private readonly List<CombatActionType> recentActions = new List<CombatActionType>(MaxActionHistory);
    private readonly List<CombatTelemetryEvent> recentEvents = new List<CombatTelemetryEvent>(MaxActionHistory);

    private PlayerController playerController;
    private PlayerCombat playerCombat;
    private ParrySystem parrySystem;
    private Rigidbody2D rb;

    private float sampleTimer;
    private float lastTempo;
    private float tempoGainRecent;
    private float tempoDecayRecent;
    private float lastTierChangeTime;
    private float lastThresholdCrossTime = -999f;
    private int thresholdCrossCountRecent;
    private float thresholdCountTimer;
    private EnemyBase currentTarget;
    private TempoManager.TempoTier lastObservedTier = TempoManager.TempoTier.T0;

    public event Action<CombatTelemetryEvent> OnCombatEvent;

    public float CurrentTempo => TempoManager.Instance != null ? TempoManager.Instance.tempo : 0f;
    public TempoManager.TempoTier CurrentTempoTier => TempoManager.Instance != null ? TempoManager.Instance.CurrentTier : TempoManager.TempoTier.T0;
    public float TimeInCurrentTier => Time.time - lastTierChangeTime;
    public float TimeAboveHighTempo { get; private set; }
    public float LastThresholdCrossTime => lastThresholdCrossTime;
    public int ThresholdCrossCountRecent => thresholdCrossCountRecent;
    public float TempoGainRateRecent => tempoGainRecent;
    public float TempoDecayRateRecent => tempoDecayRecent;
    public bool IsInCombat => RoomManager.Instance != null && RoomManager.Instance.isRoomActive;
    public bool IsUnderThreat { get; private set; }
    public int NearbyEnemyCount { get; private set; }
    public int CurrentTargetId => currentTarget != null ? currentTarget.GetInstanceID() : 0;
    public float TimeNearCurrentTarget { get; private set; }
    public float FlankOrRearTime { get; private set; }
    public float LastDashTime { get; private set; } = -999f;
    public float LastParryTime { get; private set; } = -999f;
    public float LastPerfectParryTime { get; private set; } = -999f;
    public float LastCounterTime { get; private set; } = -999f;
    public float LastHitTime { get; private set; } = -999f;
    public float LastDamageTakenTime { get; private set; } = -999f;
    public float LastKillTime { get; private set; } = -999f;
    public float LastStunTime { get; private set; } = -999f;
    public float LastGuardBreakTime { get; private set; } = -999f;

    public static CombatTelemetryHub EnsureFor(GameObject player)
    {
        if (player == null)
            return null;

        var hub = player.GetComponent<CombatTelemetryHub>();
        if (hub == null)
            hub = player.AddComponent<CombatTelemetryHub>();

        return hub;
    }

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
        playerCombat = GetComponent<PlayerCombat>();
        parrySystem = GetComponent<ParrySystem>();
        rb = GetComponent<Rigidbody2D>();
        lastTempo = CurrentTempo;
        lastObservedTier = CurrentTempoTier;
        lastTierChangeTime = Time.time;
    }

    private void OnEnable()
    {
        Subscribe();
    }

    private void Start()
    {
        Subscribe();
    }

    private void OnDisable()
    {
        if (playerController != null)
            playerController.OnDodgeStarted -= HandleDodgeStarted;

        if (parrySystem != null)
        {
            parrySystem.OnParryStarted -= HandleParryStarted;
            parrySystem.OnParryResolved -= HandleParryResolved;
            parrySystem.OnParryFail -= HandleParryFail;
        }

        if (TempoManager.Instance != null)
        {
            TempoManager.Instance.OnTempoChanged -= HandleTempoChanged;
            TempoManager.Instance.OnTierChanged -= HandleTierChanged;
        }
    }

    private void Update()
    {
        float dt = Time.deltaTime;

        if (TempoManager.Instance != null && TempoManager.Instance.tempo >= GetHighTempoThreshold())
            TimeAboveHighTempo += dt;

        thresholdCountTimer -= dt;
        if (thresholdCountTimer <= 0f)
        {
            thresholdCrossCountRecent = 0;
            thresholdCountTimer = 8f;
        }

        tempoGainRecent = Mathf.MoveTowards(tempoGainRecent, 0f, dt * 4f);
        tempoDecayRecent = Mathf.MoveTowards(tempoDecayRecent, 0f, dt * 4f);

        sampleTimer -= dt;
        if (sampleTimer <= 0f)
        {
            sampleTimer = 0.2f;
            SampleEnemyContext();
        }

        if (currentTarget != null && Vector2.Distance(transform.position, currentTarget.transform.position) <= GetNearEnemyRadius())
            TimeNearCurrentTarget += dt;

        if (currentTarget != null && IsAtFlankOrRear(currentTarget))
            FlankOrRearTime += dt;
    }

    public IReadOnlyList<CombatActionType> GetRecentActions()
    {
        return recentActions;
    }

    public IReadOnlyList<CombatTelemetryEvent> GetRecentEvents()
    {
        return recentEvents;
    }

    public void RecordAction(CombatActionType actionType, GameObject source = null, EnemyBase target = null, float value = 0f)
    {
        PushAction(actionType);
        Emit(new CombatTelemetryEvent
        {
            actionType = actionType,
            time = Time.time,
            source = source,
            target = target,
            value = value
        });
    }

    public void RecordHit(EnemyBase target, bool killed, float attackMultiplier, float counterMultiplier, float damage)
    {
        if (target != null && target != currentTarget)
        {
            currentTarget = target;
            TimeNearCurrentTarget = 0f;
            RecordAction(CombatActionType.TargetSwitch, target.gameObject, target);
        }

        LastHitTime = Time.time;
        PushAction(CombatActionType.Hit);

        if (counterMultiplier > 0f)
            LastCounterTime = Time.time;

        Emit(new CombatTelemetryEvent
        {
            actionType = killed ? CombatActionType.Kill : CombatActionType.Hit,
            time = Time.time,
            source = gameObject,
            target = target,
            killed = killed,
            attackMultiplier = attackMultiplier,
            counterMultiplier = counterMultiplier,
            value = damage
        });

        if (killed)
            LastKillTime = Time.time;
    }

    public void RecordDamageTaken(float amount)
    {
        LastDamageTakenTime = Time.time;
        RecordAction(CombatActionType.DamageTaken, gameObject, null, amount);
    }

    public void RecordDodgeThreat(bool isRanged, UnityEngine.Object threatSource)
    {
        GameObject sourceObject = threatSource as GameObject;
        if (sourceObject == null && threatSource is Component sourceComponent)
            sourceObject = sourceComponent.gameObject;

        Emit(new CombatTelemetryEvent
        {
            actionType = CombatActionType.DodgeThreat,
            time = Time.time,
            source = sourceObject,
            isRanged = isRanged
        });
    }

    public void RecordDeflectHit(EnemyBase target, float damage)
    {
        Emit(new CombatTelemetryEvent
        {
            actionType = CombatActionType.Deflect,
            time = Time.time,
            source = gameObject,
            target = target,
            isRanged = true,
            value = damage
        });
    }

    public void RecordEnemyStun(EnemyBase target, float duration)
    {
        LastStunTime = Time.time;
        Emit(new CombatTelemetryEvent
        {
            actionType = CombatActionType.Stun,
            time = Time.time,
            source = gameObject,
            target = target,
            value = duration
        });
    }

    private void Subscribe()
    {
        if (playerController != null)
        {
            playerController.OnDodgeStarted -= HandleDodgeStarted;
            playerController.OnDodgeStarted += HandleDodgeStarted;
        }

        if (parrySystem != null)
        {
            parrySystem.OnParryStarted -= HandleParryStarted;
            parrySystem.OnParryStarted += HandleParryStarted;
            parrySystem.OnParryResolved -= HandleParryResolved;
            parrySystem.OnParryResolved += HandleParryResolved;
            parrySystem.OnParryFail -= HandleParryFail;
            parrySystem.OnParryFail += HandleParryFail;
        }

        if (TempoManager.Instance != null)
        {
            TempoManager.Instance.OnTempoChanged -= HandleTempoChanged;
            TempoManager.Instance.OnTempoChanged += HandleTempoChanged;
            TempoManager.Instance.OnTierChanged -= HandleTierChanged;
            TempoManager.Instance.OnTierChanged += HandleTierChanged;
        }
    }

    private void HandleDodgeStarted(Vector2 direction)
    {
        LastDashTime = Time.time;
        PushAction(CombatActionType.Dash);
        Emit(new CombatTelemetryEvent
        {
            actionType = CombatActionType.Dash,
            time = Time.time,
            source = gameObject,
            direction = direction
        });
    }

    private void HandleParryStarted(Vector2 direction)
    {
        LastParryTime = Time.time;
        PushAction(CombatActionType.Parry);
        Emit(new CombatTelemetryEvent
        {
            actionType = CombatActionType.Parry,
            time = Time.time,
            source = gameObject,
            direction = direction
        });
    }

    private void HandleParryResolved(ParryEventData data)
    {
        if (data.isPerfect)
            LastPerfectParryTime = Time.time;

        PushAction(data.isPerfect ? CombatActionType.PerfectParry : CombatActionType.Parry);
        Emit(new CombatTelemetryEvent
        {
            actionType = data.isPerfect ? CombatActionType.PerfectParry : CombatActionType.Parry,
            time = Time.time,
            source = data.source,
            isRanged = data.isRanged,
            isPerfect = data.isPerfect,
            direction = data.parryDirection,
            value = data.blockedCount
        });
    }

    private void HandleParryFail()
    {
        PushAction(CombatActionType.ParryFail);
        Emit(new CombatTelemetryEvent
        {
            actionType = CombatActionType.ParryFail,
            time = Time.time,
            source = gameObject
        });
    }

    private void HandleTempoChanged(float newTempo)
    {
        float delta = newTempo - lastTempo;
        if (delta > 0f)
            tempoGainRecent += delta;
        else if (delta < 0f)
            tempoDecayRecent += -delta;

        lastTempo = newTempo;
    }

    private void HandleTierChanged(TempoManager.TempoTier tier)
    {
        lastTierChangeTime = Time.time;
        bool movedUp = (int)tier > (int)lastObservedTier;
        lastObservedTier = tier;
        if (!movedUp)
            return;

        lastThresholdCrossTime = Time.time;
        thresholdCrossCountRecent++;
        thresholdCountTimer = 8f;
        RecordAction(CombatActionType.ThresholdCross, gameObject, null, (float)tier);
    }

    private void SampleEnemyContext()
    {
        NearbyEnemyCount = 0;
        IsUnderThreat = false;

        IReadOnlyList<EnemyBase> enemies = EnemyBase.ActiveEnemies;
        float nearestDist = float.MaxValue;
        EnemyBase nearest = null;

        float nearRadius = GetNearEnemyRadius();
        float threatRadius = GetThreatRadius();
        for (int i = 0; i < enemies.Count; i++)
        {
            EnemyBase enemy = enemies[i];
            if (enemy == null || enemy.HealthPercent <= 0f)
                continue;

            float dist = Vector2.Distance(transform.position, enemy.transform.position);
            if (dist <= nearRadius)
                NearbyEnemyCount++;

            if (dist <= threatRadius)
                IsUnderThreat = true;

            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearest = enemy;
            }
        }

        if (currentTarget == null)
            currentTarget = nearest;
    }

    private bool IsAtFlankOrRear(EnemyBase enemy)
    {
        if (enemy == null)
            return false;

        Vector2 enemyForward = enemy.transform.localScale.x >= 0f ? Vector2.right : Vector2.left;
        Vector2 toPlayer = ((Vector2)transform.position - (Vector2)enemy.transform.position).normalized;
        return Vector2.Dot(enemyForward, toPlayer) < 0.35f;
    }

    private void PushAction(CombatActionType actionType)
    {
        if (recentActions.Count >= MaxActionHistory)
            recentActions.RemoveAt(0);

        recentActions.Add(actionType);
    }

    private void Emit(CombatTelemetryEvent telemetryEvent)
    {
        if (recentEvents.Count >= MaxActionHistory)
            recentEvents.RemoveAt(0);

        recentEvents.Add(telemetryEvent);
        OnCombatEvent?.Invoke(telemetryEvent);
    }

    private float GetHighTempoThreshold()
    {
        var config = AxisProgressionManager.Instance != null ? AxisProgressionManager.Instance.GetProgressionConfig() : null;
        return config != null ? config.highTempoThreshold : 70f;
    }

    private float GetNearEnemyRadius()
    {
        var config = AxisProgressionManager.Instance != null ? AxisProgressionManager.Instance.GetProgressionConfig() : null;
        return config != null ? config.nearEnemyRadius : 3f;
    }

    private float GetThreatRadius()
    {
        var config = AxisProgressionManager.Instance != null ? AxisProgressionManager.Instance.GetProgressionConfig() : null;
        return config != null ? config.threatRadius : 4f;
    }
}

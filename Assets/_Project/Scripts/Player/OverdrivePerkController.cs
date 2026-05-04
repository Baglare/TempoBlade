using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class OverdrivePerkController : MonoBehaviour
{
    [Header("T1: Hararet Birikimi")]
    public float heatChainWindow = 1.1f;
    public int heatMaxStacks = 5;
    public float heatTempoPerStack = 0.45f;

    [Header("T1: Esik Patlamasi")]
    public float thresholdChargeDuration = 4f;
    public float thresholdDamageBonus = 0.25f;
    public float thresholdStunDuration = 0.35f;

    [Header("T1: Kizil Basinc")]
    public TempoManager.TempoTier redPressureMinTier = TempoManager.TempoTier.T2;
    public float redPressureDamageBonus = 0.12f;
    public float redPressureStunBonus = 0.20f;
    public float redPressureIncomingDamageMultiplier = 1.10f;

    [Header("T1: Tasan Durtu")]
    [Range(0f, 100f)] public float overflowTempoThreshold = 85f;
    public float overflowWhiffPenaltyMultiplier = 0.45f;
    public float overflowDamagePenaltyMultiplier = 0.65f;

    [Header("T1: Son Itki")]
    public float finalPushDuration = 3f;
    public float finalPushDamageBonus = 0.20f;

    [Header("T2: Overdrive Odagi")]
    public float focusHighTempoEffectMultiplier = 1.35f;
    public float focusLowTempoTempoGainMultiplier = 0.80f;
    public float focusLowTempoIncomingDamageMultiplier = 1.08f;

    [Header("T2 Burst: Kisa Devre / Kizil Pencere")]
    public float burstWindowDuration = 3f;
    public float burstWindowMaxDuration = 5f;
    public float burstDamageBonus = 0.25f;
    public float burstAttackCooldownMultiplier = 0.82f;
    public float burstTempoDecayMultiplier = 1.45f;

    [Header("T2 Burst: Esik Yankisi / Basinc Kopusu / Son Parlama")]
    public float burstEchoExtension = 0.6f;
    public float burstPressureBreakStun = 0.45f;
    public float finalFlareWindow = 0.55f;
    public float finalFlareDamageBonus = 0.55f;

    [Header("T2 Predator")]
    public int bloodScentHitsToMark = 3;
    public float bloodScentMemory = 3f;
    public float preyMarkerRange = 6f;
    public Color preyMarkerColor = new Color(1f, 0.18f, 0.05f, 1f);
    public float preyProximityRange = 2.8f;
    public float preyTempoDecayMultiplier = 0.75f;
    public float preyDamageBonus = 0.12f;
    public float predatorAngleDamageBonus = 0.18f;
    public float predatorAngleStun = 0.30f;
    public float executeHealthThreshold = 0.25f;
    public float executeDamageBonus = 0.85f;
    public float bossExecuteDamageBonus = 0.30f;
    public float executePressureStun = 0.75f;

    private readonly Dictionary<EnemyBase, MarkPressure> pressure = new Dictionary<EnemyBase, MarkPressure>();
    private Collider2D[] preyTransferHitBuffer = new Collider2D[32];

    private PlayerCombat playerCombat;
    private CadencePerkController cadencePerks;
    private bool hasHeatBuildup;
    private bool hasThresholdBurst;
    private bool hasRedPressure;
    private bool hasOverflowImpulse;
    private bool hasFinalPush;
    private bool hasCommitment;
    private bool hasShortCircuit;
    private bool hasRedWindow;
    private bool hasThresholdEcho;
    private bool hasPressureBreak;
    private bool hasFinalFlare;
    private bool hasBloodScent;
    private bool hasChokingProximity;
    private bool hasPredatorAngle;
    private bool hasPackBreaker;
    private bool hasExecutePressure;

    private int heatStacks;
    private float lastHeatHitTime = -999f;
    private float thresholdChargeTimer;
    private float finalPushTimer;
    private float burstWindowTimer;
    private TempoManager.TempoTier lastTier = TempoManager.TempoTier.T0;
    private EnemyBase currentPrey;
    private bool pendingFinalFlare;

    private struct MarkPressure
    {
        public int hits;
        public float timer;
    }

    private void Awake()
    {
        playerCombat = GetComponent<PlayerCombat>();
        cadencePerks = GetComponent<CadencePerkController>();
    }

    private void OnEnable()
    {
        Subscribe();
    }

    private void Start()
    {
        StartCoroutine(DelayedSync());
    }

    private IEnumerator DelayedSync()
    {
        yield return null;
        Subscribe();
        if (TempoManager.Instance != null)
            lastTier = TempoManager.Instance.CurrentTier;
        if (AxisProgressionManager.Instance != null)
            HandleBuildChanged(AxisProgressionManager.Instance.CurrentBuild);
    }

    private void OnDisable()
    {
        if (AxisProgressionManager.Instance != null)
            AxisProgressionManager.Instance.OnBuildChanged -= HandleBuildChanged;
        if (TempoManager.Instance != null)
            TempoManager.Instance.OnTierChanged -= HandleTierChanged;
        if (TempoManager.Instance != null)
            TempoManager.Instance.SetOverdriveTempoMultipliers(1f, 1f);
        ClearPrey();
    }

    private void Update()
    {
        float dt = Time.deltaTime;

        if (thresholdChargeTimer > 0f) thresholdChargeTimer -= dt;
        if (finalPushTimer > 0f) finalPushTimer -= dt;
        if (burstWindowTimer > 0f) burstWindowTimer -= dt;

        TickMarkPressure(dt);
        ApplyTempoMultipliers();
    }

    private void Subscribe()
    {
        if (AxisProgressionManager.Instance != null)
        {
            AxisProgressionManager.Instance.OnBuildChanged -= HandleBuildChanged;
            AxisProgressionManager.Instance.OnBuildChanged += HandleBuildChanged;
        }

        if (TempoManager.Instance != null)
        {
            TempoManager.Instance.OnTierChanged -= HandleTierChanged;
            TempoManager.Instance.OnTierChanged += HandleTierChanged;
            lastTier = TempoManager.Instance.CurrentTier;
        }
    }

    private void HandleBuildChanged(PlayerBuild build)
    {
        if (build == null) return;

        hasHeatBuildup = build.HasFlag(EffectKeyRegistry.OverdriveHeatBuildup);
        hasThresholdBurst = build.HasFlag(EffectKeyRegistry.OverdriveThresholdBurst);
        hasRedPressure = build.HasFlag(EffectKeyRegistry.OverdriveRedPressure);
        hasOverflowImpulse = build.HasFlag(EffectKeyRegistry.OverdriveOverflowImpulse);
        hasFinalPush = build.HasFlag(EffectKeyRegistry.OverdriveFinalPush);
        hasCommitment = build.HasFlag(EffectKeyRegistry.OverdriveT2Commitment);
        hasShortCircuit = build.HasFlag(EffectKeyRegistry.OverdriveShortCircuit);
        hasRedWindow = build.HasFlag(EffectKeyRegistry.OverdriveRedWindow);
        hasThresholdEcho = build.HasFlag(EffectKeyRegistry.OverdriveThresholdEcho);
        hasPressureBreak = build.HasFlag(EffectKeyRegistry.OverdrivePressureBreak);
        hasFinalFlare = build.HasFlag(EffectKeyRegistry.OverdriveFinalFlare);
        hasBloodScent = build.HasFlag(EffectKeyRegistry.OverdriveBloodScent);
        hasChokingProximity = build.HasFlag(EffectKeyRegistry.OverdriveChokingProximity);
        hasPredatorAngle = build.HasFlag(EffectKeyRegistry.OverdrivePredatorAngle);
        hasPackBreaker = build.HasFlag(EffectKeyRegistry.OverdrivePackBreaker);
        hasExecutePressure = build.HasFlag(EffectKeyRegistry.OverdriveExecutePressure);

        if (!hasBloodScent)
            ClearPrey();
    }

    private void HandleTierChanged(TempoManager.TempoTier tier)
    {
        if ((int)tier > (int)lastTier)
        {
            if (hasThresholdBurst)
                thresholdChargeTimer = thresholdChargeDuration;

            if (hasShortCircuit)
                OpenBurstWindow();
        }

        lastTier = tier;
    }

    public float GetAttackCooldownMultiplier()
    {
        if (hasRedWindow && IsBurstWindowActive)
            return Mathf.Max(0.1f, burstAttackCooldownMultiplier);

        return 1f;
    }

    public float GetGlobalDamageBonus(float attackMultiplier, float totalCounter)
    {
        float bonus = 0f;

        if (hasRedPressure && IsHighTempo())
            bonus += redPressureDamageBonus * FocusMultiplier();

        if (thresholdChargeTimer > 0f)
            bonus += thresholdDamageBonus * FocusMultiplier() * CadenceThresholdMultiplier();

        if (finalPushTimer > 0f)
            bonus += finalPushDamageBonus * FocusMultiplier();

        if (hasRedWindow && IsBurstWindowActive)
            bonus += burstDamageBonus * FocusMultiplier();

        if (hasFinalFlare && IsBurstWindowActive && burstWindowTimer <= finalFlareWindow)
        {
            bonus += finalFlareDamageBonus * FocusMultiplier();
            pendingFinalFlare = true;
        }

        return bonus;
    }

    public float GetTargetDamageBonus(EnemyBase enemy, float attackMultiplier, float totalCounter)
    {
        if (enemy == null)
            return 0f;

        float bonus = 0f;

        if (enemy == currentPrey && hasChokingProximity && IsNear(enemy, preyProximityRange))
            bonus += preyDamageBonus * FocusMultiplier();

        if (enemy == currentPrey && hasPredatorAngle && IsSideOrBackAngle(enemy))
            bonus += predatorAngleDamageBonus * FocusMultiplier();

        if (enemy == currentPrey && hasExecutePressure && enemy.HealthPercent <= executeHealthThreshold &&
            (attackMultiplier >= 1.2f || totalCounter > 0f))
        {
            bool isBoss = enemy.GetComponent<EnemyBoss>() != null;
            bonus += (isBoss ? bossExecuteDamageBonus : executeDamageBonus) * FocusMultiplier();
        }

        return bonus;
    }

    public float GetTempoGainOnHit()
    {
        if (!hasHeatBuildup)
            return 0f;

        if (Time.time - lastHeatHitTime <= heatChainWindow)
            heatStacks = Mathf.Min(heatMaxStacks, heatStacks + 1);
        else
            heatStacks = 1;

        lastHeatHitTime = Time.time;
        float lowTempoPenalty = hasCommitment && !IsHighTempo() ? focusLowTempoTempoGainMultiplier : 1f;
        return Mathf.Max(0, heatStacks - 1) * heatTempoPerStack * FocusMultiplier() * lowTempoPenalty;
    }

    public float ModifyWhiffTempoPenalty(float penalty)
    {
        if (hasOverflowImpulse && IsNearMaxTempo())
            return penalty * overflowWhiffPenaltyMultiplier;

        return penalty;
    }

    public float GetIncomingDamageMultiplier()
    {
        float multiplier = 1f;

        if (hasRedPressure && IsHighTempo())
            multiplier *= redPressureIncomingDamageMultiplier;

        if (hasCommitment && !IsHighTempo())
            multiplier *= focusLowTempoIncomingDamageMultiplier;

        return multiplier;
    }

    public void NotifyEnemyHit(EnemyBase enemy, bool killed, float attackMultiplier, float totalCounter)
    {
        if (enemy == null)
            return;

        if (thresholdChargeTimer > 0f)
        {
            enemy.Stun(thresholdStunDuration * FocusMultiplier() * CadenceThresholdMultiplier());
            thresholdChargeTimer = 0f;
        }

        if (hasRedPressure && IsHighTempo())
            enemy.Stun(redPressureStunBonus * FocusMultiplier());

        if (hasPressureBreak && IsBurstWindowActive && (attackMultiplier >= 1.2f || totalCounter > 0f))
            enemy.Stun(burstPressureBreakStun * FocusMultiplier());

        if (enemy == currentPrey && hasPredatorAngle && IsSideOrBackAngle(enemy))
            enemy.Stun(predatorAngleStun * FocusMultiplier());

        if (enemy == currentPrey && hasExecutePressure && enemy.HealthPercent <= executeHealthThreshold &&
            (attackMultiplier >= 1.2f || totalCounter > 0f))
            enemy.Stun(executePressureStun * FocusMultiplier());

        if (pendingFinalFlare)
        {
            EndBurstWindow();
            pendingFinalFlare = false;
        }

        if (hasBloodScent && !killed)
            RegisterPreyPressure(enemy);

        if (killed)
        {
            if (hasFinalPush && IsHighTempo())
                finalPushTimer = finalPushDuration;

            if (hasThresholdEcho && IsBurstWindowActive)
                burstWindowTimer = Mathf.Min(burstWindowMaxDuration, burstWindowTimer + burstEchoExtension);

            if (enemy == currentPrey)
            {
                ClearPrey();
                if (hasPackBreaker)
                    TransferPrey(enemy.transform.position);
            }
        }
    }

    private void ApplyTempoMultipliers()
    {
        if (TempoManager.Instance == null)
            return;

        float decay = 1f;
        float damagePenalty = 1f;

        if (hasRedWindow && IsBurstWindowActive)
            decay *= burstTempoDecayMultiplier;

        if (hasChokingProximity && currentPrey != null && IsNear(currentPrey, preyProximityRange))
            decay *= preyTempoDecayMultiplier;

        if (hasOverflowImpulse && IsNearMaxTempo())
            damagePenalty *= overflowDamagePenaltyMultiplier;

        TempoManager.Instance.SetOverdriveTempoMultipliers(decay, damagePenalty);
    }

    private void OpenBurstWindow()
    {
        burstWindowTimer = Mathf.Min(burstWindowMaxDuration, Mathf.Max(burstWindowTimer, burstWindowDuration));
    }

    private void EndBurstWindow()
    {
        burstWindowTimer = 0f;
    }

    private bool IsBurstWindowActive => burstWindowTimer > 0f;

    private float FocusMultiplier()
    {
        return hasCommitment && IsHighTempo() ? focusHighTempoEffectMultiplier : 1f;
    }

    private float CadenceThresholdMultiplier()
    {
        return cadencePerks != null ? cadencePerks.GetOverdriveThresholdMultiplier() : 1f;
    }

    private bool IsHighTempo()
    {
        return TempoManager.Instance != null && TempoManager.Instance.CurrentTier >= redPressureMinTier;
    }

    private bool IsNearMaxTempo()
    {
        return TempoManager.Instance != null && TempoManager.Instance.tempo >= overflowTempoThreshold;
    }

    private bool IsNear(EnemyBase enemy, float range)
    {
        return enemy != null && Vector2.Distance(transform.position, enemy.transform.position) <= range;
    }

    private bool IsSideOrBackAngle(EnemyBase enemy)
    {
        if (enemy == null)
            return false;

        Vector2 enemyForward = Vector2.right;
        SpriteRenderer sr = enemy.GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
            enemyForward = sr.flipX ? Vector2.left : Vector2.right;
        else if (enemy.transform.localScale.x < 0f)
            enemyForward = Vector2.left;

        Vector2 toPlayer = ((Vector2)transform.position - (Vector2)enemy.transform.position).normalized;
        return Vector2.Dot(enemyForward, toPlayer) < 0.35f;
    }

    private void RegisterPreyPressure(EnemyBase enemy)
    {
        if (enemy == null)
            return;

        pressure.TryGetValue(enemy, out var mark);
        mark.hits++;
        mark.timer = bloodScentMemory;
        pressure[enemy] = mark;

        if (mark.hits >= bloodScentHitsToMark)
            SetPrey(enemy);
    }

    private void SetPrey(EnemyBase enemy)
    {
        if (enemy == currentPrey)
            return;

        ClearPrey();
        currentPrey = enemy;
        currentPrey.SetPerkMarker(true, preyMarkerColor);
    }

    private void ClearPrey()
    {
        if (currentPrey != null)
            currentPrey.SetPerkMarker(false, preyMarkerColor);
        currentPrey = null;
    }

    private void TransferPrey(Vector3 origin)
    {
        EnemyBase best = DashPerkTargetSelector.SelectClosestEnemy(origin, preyMarkerRange, ref preyTransferHitBuffer, 32);

        if (best != null)
            SetPrey(best);
    }

    private void TickMarkPressure(float dt)
    {
        if (pressure.Count == 0)
            return;

        tempClearList.Clear();
        tempUpdateList.Clear();
        foreach (var kv in pressure)
        {
            var mark = kv.Value;
            mark.timer -= dt;
            if (kv.Key == null || mark.timer <= 0f)
                tempClearList.Add(kv.Key);
            else
                tempUpdateList.Add(new PressureUpdate { enemy = kv.Key, mark = mark });
        }

        foreach (var update in tempUpdateList)
            pressure[update.enemy] = update.mark;

        foreach (var enemy in tempClearList)
            pressure.Remove(enemy);
    }

    private readonly List<EnemyBase> tempClearList = new List<EnemyBase>();
    private readonly List<PressureUpdate> tempUpdateList = new List<PressureUpdate>();

    private struct PressureUpdate
    {
        public EnemyBase enemy;
        public MarkPressure mark;
    }
}

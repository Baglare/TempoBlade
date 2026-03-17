using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DashSkillRuntime : MonoBehaviour
{
    [Header("Config")]
    public DashSkillConfigSO config;

    private PlayerController playerController;
    private PlayerCombat playerCombat;
    private ParrySystem parrySystem;
    private TempoManager tempoManager;

    private float baseParryWindow;
    private float baseParryRecovery;

    // Build flags
    private bool hasRangedDodge;
    private bool hasMeleeDodge;
    private bool hasCounter;
    private bool hasTempoGain;
    private bool hasAttackSpeed;
    private bool hasDashTier2Selected;
    private bool hasHunterMark;
    private bool hasHunterBlindSpot;
    private bool hasHunterFlow;
    private bool hasHunterExecution;
    private bool hasHunterSuccession;
    private bool hasFlowMarkStream;
    private bool hasFlowRebound;
    private bool hasFlowChain;
    private bool hasFlowBlackHole;
    private bool hasFlowBlast;

    // Dash state
    private float dashStartTime = -999f;
    private Vector2 dashStartPos;
    private Vector2 dashEndPos;
    private float projectileInvulnUntil = -999f;
    private float projectileSuccessUntil = -999f;
    private float meleeInvulnUntil = -999f;
    private float meleeSuccessUntil = -999f;
    private int projectileNegationsThisDash = 0;

    // Tier 1 runtime
    private int counterCharges = 0;
    private float counterWindowEnd = -999f;
    private float dashTempoIcdEnd = -999f;
    private float attackSpeedWindowEnd = -999f;
    private float attackSpeedIcdEnd = -999f;

    // Hunter route
    private EnemyBase markedPrey;
    private float nextMarkReadyTime = 0f;
    private float lastMarkRetargetTime = -999f;
    private EnemyBase lastCombatTarget;
    private float lastCombatTargetTime = -999f;
    private float hunterRoomDamageBonus = 0f;
    private float pendingBlindSpotCounterBonus = 0f;
    private float hunterUpdateTimer = 0f;

    // Flow route
    private readonly Dictionary<EnemyBase, float> markedEnemies = new Dictionary<EnemyBase, float>();
    private float flowMarkWindowEnd = -999f;
    private float reboundWindowEnd = -999f;
    private float reboundIcdEnd = -999f;
    private Vector2 reboundReturnPos;
    private float blackHoleIcdEnd = -999f;
    private float blastWindowEnd = -999f;
    private int blastStoredMarkCount = 0;

    // Reusable buffers
    private readonly List<EnemyBase> enemyBuffer = new List<EnemyBase>(32);
    private readonly List<EnemyBase> flowMarkedBuffer = new List<EnemyBase>(16);

    public void Initialize(PlayerController controller, PlayerCombat combat, ParrySystem parry)
    {
        playerController = controller;
        playerCombat = combat;
        parrySystem = parry;
        tempoManager = TempoManager.Instance;

        if (parrySystem != null)
        {
            baseParryWindow = parrySystem.parryWindow;
            baseParryRecovery = parrySystem.parryRecovery;
        }
    }

    private void Awake()
    {
        if (config == null)
        {
            // Fallback: asset atanmamis olsa da sistem default degerlerle calissin.
            config = ScriptableObject.CreateInstance<DashSkillConfigSO>();
        }

        if (playerController == null) playerController = GetComponent<PlayerController>();
        if (playerCombat == null) playerCombat = GetComponent<PlayerCombat>();
        if (parrySystem == null) parrySystem = GetComponent<ParrySystem>();
        tempoManager = TempoManager.Instance;

        if (parrySystem != null)
        {
            baseParryWindow = parrySystem.parryWindow;
            baseParryRecovery = parrySystem.parryRecovery;
        }
    }

    private void Update()
    {
        RefreshBuildFlags();
        ApplyTier2ParryShift();
        CleanupFlowMarks();
        UpdateHunterMarking();
    }

    private void RefreshBuildFlags()
    {
        PlayerBuild build = AxisProgressionManager.Instance != null ? AxisProgressionManager.Instance.CurrentBuild : null;
        if (build == null)
        {
            hasRangedDodge = false;
            hasMeleeDodge = false;
            hasCounter = false;
            hasTempoGain = false;
            hasAttackSpeed = false;
            hasDashTier2Selected = false;
            hasHunterMark = false;
            hasHunterBlindSpot = false;
            hasHunterFlow = false;
            hasHunterExecution = false;
            hasHunterSuccession = false;
            hasFlowMarkStream = false;
            hasFlowRebound = false;
            hasFlowChain = false;
            hasFlowBlackHole = false;
            hasFlowBlast = false;
            return;
        }

        hasRangedDodge = build.HasFlag(EffectKeyRegistry.DashT1RangedDodge);
        hasMeleeDodge = build.HasFlag(EffectKeyRegistry.DashT1MeleeDodge);
        hasCounter = build.HasFlag(EffectKeyRegistry.DashT1Counter);
        hasTempoGain = build.HasFlag(EffectKeyRegistry.DashT1TempoGain);
        hasAttackSpeed = build.HasFlag(EffectKeyRegistry.DashT1AttackSpeed);

        hasDashTier2Selected = build.HasFlag(EffectKeyRegistry.DashT2Selected);
        hasHunterMark = build.HasFlag(EffectKeyRegistry.DashHunterMark);
        hasHunterBlindSpot = build.HasFlag(EffectKeyRegistry.DashHunterBlindSpot);
        hasHunterFlow = build.HasFlag(EffectKeyRegistry.DashHunterFlow);
        hasHunterExecution = build.HasFlag(EffectKeyRegistry.DashHunterExecution);
        hasHunterSuccession = build.HasFlag(EffectKeyRegistry.DashHunterSuccession);

        hasFlowMarkStream = build.HasFlag(EffectKeyRegistry.DashFlowMarkStream);
        hasFlowRebound = build.HasFlag(EffectKeyRegistry.DashFlowRebound);
        hasFlowChain = build.HasFlag(EffectKeyRegistry.DashFlowChain);
        hasFlowBlackHole = build.HasFlag(EffectKeyRegistry.DashFlowBlackHole);
        hasFlowBlast = build.HasFlag(EffectKeyRegistry.DashFlowBlast);
    }

    private void ApplyTier2ParryShift()
    {
        if (parrySystem == null || config == null) return;

        if (!hasDashTier2Selected)
        {
            // Tier2 Dash secilmediyse mevcut degerleri "baseline" olarak koru.
            // Boylece hub upgrade veya run bazli parry ayarlari bozulmaz.
            baseParryWindow = parrySystem.parryWindow;
            baseParryRecovery = parrySystem.parryRecovery;
            return;
        }
        else
        {
            float windowMul = Mathf.Max(0.05f, 1f - config.tier2Global.parryWindowPenalty);
            float recoveryMul = 1f + config.tier2Global.parryCooldownPenalty;
            parrySystem.parryWindow = baseParryWindow * windowMul;
            parrySystem.parryRecovery = baseParryRecovery * recoveryMul;
        }
    }

    public float GetParryTempoMultiplier()
    {
        if (config == null || !hasDashTier2Selected) return 1f;
        return Mathf.Max(0.1f, 1f - config.tier2Global.parryTempoPenalty);
    }

    public float GetDashCooldownMultiplier()
    {
        if (config == null) return 1f;

        float multiplier = 1f;
        if (hasDashTier2Selected)
        {
            multiplier *= Mathf.Max(0.1f, 1f - config.tier2Global.dashCooldownImprovement);
        }

        if (hasHunterFlow && IsNearMarkedPrey())
        {
            multiplier *= Mathf.Max(0.1f, 1f - config.hunterFlow.dashCooldownRecoveryBonus);
        }

        return multiplier;
    }

    public float GetDashWindowQualityMultiplier()
    {
        if (config == null) return 1f;
        if (!hasDashTier2Selected) return 1f;
        return 1f + config.tier2Global.dashWindowQualityBonus;
    }

    public float GetDashTempoEfficiencyMultiplier()
    {
        if (config == null) return 1f;
        if (!hasDashTier2Selected) return 1f;
        return 1f + config.tier2Global.dashTempoEfficiencyBonus;
    }

    public void NotifyDashStarted(Vector2 startPos, Vector2 direction)
    {
        if (config == null) return;

        dashStartTime = Time.time;
        dashStartPos = startPos;
        projectileNegationsThisDash = 0;

        float quality = GetDashWindowQualityMultiplier();

        if (hasRangedDodge)
        {
            projectileInvulnUntil = Time.time + (config.rangedDodge.projectileDodgeWindow * quality) + config.rangedDodge.safeExitPadding;
            projectileSuccessUntil = Time.time + config.rangedDodge.successDetectDuration * quality;
        }
        else
        {
            projectileInvulnUntil = -999f;
            projectileSuccessUntil = -999f;
        }

        if (hasMeleeDodge)
        {
            meleeInvulnUntil = Time.time + (config.meleeDodge.meleeDodgeWindow * quality) + config.meleeDodge.safeExitPadding;
            meleeSuccessUntil = Time.time + config.meleeDodge.successDetectDuration * quality;
        }
        else
        {
            meleeInvulnUntil = -999f;
            meleeSuccessUntil = -999f;
        }

        if (hasFlowMarkStream)
        {
            flowMarkWindowEnd = Time.time + config.flowMark.markWindow;
        }

        if (hasFlowRebound)
        {
            reboundReturnPos = startPos;
            reboundWindowEnd = Time.time + config.flowRebound.returnWindow;
        }
    }

    public void NotifyDashEnded(Vector2 endPos)
    {
        if (config == null) return;

        dashEndPos = endPos;

        if (hasTempoGain && Time.time >= dashTempoIcdEnd)
        {
            if (IsOffensiveDash(endPos))
            {
                float tempoGain = Mathf.Min(config.tempoGain.tempoPerOffensiveDash, config.tempoGain.maxTempoPerDash);
                tempoGain *= GetDashTempoEfficiencyMultiplier();
                if (tempoManager != null) tempoManager.AddTempo(tempoGain);
                dashTempoIcdEnd = Time.time + config.tempoGain.internalCooldown;

                if (hasAttackSpeed)
                {
                    attackSpeedWindowEnd = Time.time + config.attackSpeed.fastAttackWindow;
                }
            }
        }

        if (hasHunterBlindSpot && markedPrey != null)
        {
            TryApplyBlindSpotPressure(markedPrey, endPos);
        }

        if (hasHunterExecution && markedPrey != null)
        {
            TryExecutionDash(markedPrey, endPos);
        }
    }

    public bool TryTriggerRebound()
    {
        if (!hasFlowRebound || config == null) return false;
        if (Time.time > reboundWindowEnd) return false;
        if (Time.time < reboundIcdEnd) return false;
        if (playerController == null) return false;

        Vector2 currentPos = transform.position;
        Vector2 toReturn = reboundReturnPos - currentPos;
        if (toReturn.sqrMagnitude < 0.01f) return false;

        float duration = Mathf.Max(0.02f, config.flowRebound.reboundDuration);
        float speed = toReturn.magnitude / duration;
        playerController.StartExternalDash(
            toReturn.normalized,
            speed,
            duration,
            config.flowRebound.reboundGivesInvulnerability);

        reboundIcdEnd = Time.time + config.flowRebound.internalCooldown;
        reboundWindowEnd = -999f;
        return true;
    }

    public bool TryDodgeProjectile(Vector2 threatPosition)
    {
        if (!hasRangedDodge || config == null) return false;
        if (!IsPlayerCurrentlyDashing()) return false;
        if (Time.time > projectileInvulnUntil) return false;
        if (projectileNegationsThisDash >= config.rangedDodge.maxProjectileNegationsPerDash) return false;

        projectileNegationsThisDash++;
        float distance = Vector2.Distance(transform.position, threatPosition);
        if (Time.time <= projectileSuccessUntil && distance <= config.rangedDodge.minThreatDistance)
        {
            RegisterSuccessfulDodge();
        }
        return true;
    }

    public bool TryDodgeMelee(Vector2 threatPosition)
    {
        if (!hasMeleeDodge || config == null) return false;
        if (!IsPlayerCurrentlyDashing()) return false;
        if (Time.time > meleeInvulnUntil) return false;

        float distance = Vector2.Distance(transform.position, threatPosition);
        if (Time.time <= meleeSuccessUntil && distance <= config.meleeDodge.minThreatDistance)
        {
            RegisterSuccessfulDodge();
        }
        return true;
    }

    private void RegisterSuccessfulDodge()
    {
        if (!hasCounter || config == null) return;
        counterCharges = Mathf.Min(config.counter.maxStoredCharges, counterCharges + 1);
        counterWindowEnd = Time.time + config.counter.counterWindow;
    }

    public bool TryConsumeCounterBonus(out float damageBonus, out float staggerBonus)
    {
        damageBonus = 0f;
        staggerBonus = 0f;
        if (!hasCounter || config == null) return false;
        if (counterCharges <= 0 || Time.time > counterWindowEnd) return false;

        counterCharges--;
        damageBonus = config.counter.counterDamageBonus + pendingBlindSpotCounterBonus;
        staggerBonus = config.counter.counterStaggerBonus;
        pendingBlindSpotCounterBonus = 0f;
        if (counterCharges <= 0)
        {
            counterWindowEnd = -999f;
        }
        return true;
    }

    public float ConsumeAttackSpeedCooldownMultiplier()
    {
        if (!hasAttackSpeed || config == null) return 1f;
        if (Time.time > attackSpeedWindowEnd) return 1f;
        if (Time.time < attackSpeedIcdEnd) return 1f;

        float speedMul = Mathf.Max(0.1f, 1f - config.attackSpeed.attackSpeedBonus);
        float recoveryMul = Mathf.Max(0.1f, 1f - config.attackSpeed.recoveryResetRatio);
        float combined = speedMul * recoveryMul;

        float icd = config.attackSpeed.internalCooldown;
        if (hasHunterFlow && IsNearMarkedPrey())
        {
            icd *= Mathf.Max(0.1f, 1f - config.hunterFlow.attackSpeedIcdRecoveryBonus);
        }
        attackSpeedIcdEnd = Time.time + icd;
        attackSpeedWindowEnd = -999f;
        return combined;
    }

    public float GetPerAttackDamageMultiplier(EnemyBase target)
    {
        if (config == null) return 1f;

        float mult = 1f;
        mult += hunterRoomDamageBonus;

        if (hasFlowMarkStream && target != null && IsEnemyMarked(target))
        {
            int count = Mathf.Min(GetActiveMarkedEnemyCount(), config.flowMark.maxUniqueMarks);
            mult += Mathf.Min(count * config.flowMark.damagePerUniqueMark, config.flowMark.maxDamageBonus);
        }

        if (hasFlowBlast && Time.time <= blastWindowEnd)
        {
            float blastBonus = config.flowBlast.baseDamageMultiplier - 1f;
            blastBonus += blastStoredMarkCount * config.flowBlast.bonusPerConsumedMark;
            mult += Mathf.Max(0f, blastBonus);
        }

        return Mathf.Max(0.1f, mult);
    }

    public void OnPrimaryHitApplied(EnemyBase enemy, float dealtDamage)
    {
        if (enemy == null || config == null) return;

        lastCombatTarget = enemy;
        lastCombatTargetTime = Time.time;

        if (hasFlowMarkStream && Time.time <= flowMarkWindowEnd)
        {
            MarkEnemy(enemy);
        }

        if (hasFlowChain && IsEnemyMarked(enemy))
        {
            ApplyChainBounce(enemy, dealtDamage);
        }

        if (hasFlowBlast && Time.time <= blastWindowEnd)
        {
            ApplyBlastSpread(enemy, dealtDamage);
            blastWindowEnd = -999f;
            if (config.flowBlast.consumeAllMarks)
            {
                markedEnemies.Clear();
            }
            blastStoredMarkCount = 0;
        }

        if (enemy.CurrentHealth <= 0f)
        {
            HandleEnemyKilled(enemy, false);
        }
    }

    private void HandleEnemyKilled(EnemyBase enemy, bool executed)
    {
        if (enemy == null) return;

        if (markedPrey == enemy)
        {
            if (hasHunterSuccession && config != null)
            {
                hunterRoomDamageBonus = Mathf.Min(
                    hunterRoomDamageBonus + config.hunterSuccession.damagePerPrey,
                    config.hunterSuccession.maxRoomDamageBonus);
            }
            markedPrey = null;
            lastMarkRetargetTime = Time.time;
        }

        markedEnemies.Remove(enemy);
    }

    private bool IsOffensiveDash(Vector2 dashEnd)
    {
        float nearest = GetNearestEnemyDistance(dashEnd);
        if (nearest < 0f) return false;
        return nearest <= config.tempoGain.offensiveDashDistance || nearest <= config.tempoGain.threatCircleDistance;
    }

    private void TryApplyBlindSpotPressure(EnemyBase prey, Vector2 endPos)
    {
        if (prey == null || config == null) return;

        float dist = Vector2.Distance(endPos, prey.transform.position);
        if (dist > config.hunterBlindSpot.validDashEndDistance) return;

        Vector2 enemyForward = GetEnemyForward(prey);
        Vector2 toPlayer = ((Vector2)endPos - (Vector2)prey.transform.position).normalized;
        float half = config.hunterBlindSpot.frontConeAngle * 0.5f;
        float angle = Vector2.Angle(enemyForward, toPlayer);
        bool isInFront = angle <= half;
        if (isInFront) return;

        prey.Stun(config.hunterBlindSpot.stunDuration);
        pendingBlindSpotCounterBonus = config.hunterBlindSpot.counterDamageBonus;
    }

    private void TryExecutionDash(EnemyBase prey, Vector2 endPos)
    {
        if (prey == null || config == null) return;
        if (Time.time - dashStartTime > config.hunterExecution.inputWindow) return;

        float hpRatio = prey.MaxHealth > 0.01f ? prey.CurrentHealth / prey.MaxHealth : 1f;
        if (hpRatio > config.hunterExecution.executeHealthPercent) return;

        float dist = Vector2.Distance(endPos, prey.transform.position);
        if (dist > config.hunterExecution.validEntryDistance) return;

        Vector2 enemyForward = GetEnemyForward(prey);
        Vector2 toPlayer = ((Vector2)endPos - (Vector2)prey.transform.position).normalized;
        float rearHalf = config.hunterExecution.rearConeAngle * 0.5f;
        float angleToBack = Vector2.Angle(-enemyForward, toPlayer);
        if (angleToBack > rearHalf) return;

        float killDamage = prey.CurrentHealth + prey.MaxHealth * 2f;
        prey.TakeDamage(killDamage);
        HandleEnemyKilled(prey, true);

        if (playerController != null)
        {
            playerController.SetManualInvulnerability(config.hunterExecution.collisionIgnoreDuration);
        }
    }

    private void UpdateHunterMarking()
    {
        if (!hasHunterMark || config == null) return;

        hunterUpdateTimer -= Time.deltaTime;
        if (hunterUpdateTimer > 0f) return;
        hunterUpdateTimer = config.hunterFlow.updateInterval;

        if (markedPrey != null && markedPrey.CurrentHealth > 0f)
        {
            return;
        }

        if (Time.time < nextMarkReadyTime) return;
        if (Time.time - lastMarkRetargetTime < config.hunterMark.retargetDelay) return;

        EnemyBase candidate = null;
        if (lastCombatTarget != null &&
            lastCombatTarget.CurrentHealth > 0f &&
            Time.time - lastCombatTargetTime <= config.hunterMark.activeCombatDuration &&
            Vector2.Distance(transform.position, lastCombatTarget.transform.position) <= config.hunterMark.retargetRange)
        {
            candidate = lastCombatTarget;
        }
        else
        {
            candidate = PickRandomEnemyInRange(config.hunterMark.randomSelectionRange);
        }

        if (candidate != null)
        {
            markedPrey = candidate;
            nextMarkReadyTime = Time.time + config.hunterMark.markCooldown;
        }
    }

    private EnemyBase PickRandomEnemyInRange(float range)
    {
        enemyBuffer.Clear();
        EnemyBase[] all = FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);
        foreach (var e in all)
        {
            if (e == null || e.CurrentHealth <= 0f) continue;
            if (Vector2.Distance(transform.position, e.transform.position) <= range)
                enemyBuffer.Add(e);
        }

        if (enemyBuffer.Count == 0) return null;
        return enemyBuffer[Random.Range(0, enemyBuffer.Count)];
    }

    private bool IsNearMarkedPrey()
    {
        if (markedPrey == null || config == null) return false;
        return Vector2.Distance(transform.position, markedPrey.transform.position) <= config.hunterFlow.preyProximity;
    }

    private void MarkEnemy(EnemyBase enemy)
    {
        if (enemy == null || config == null) return;
        markedEnemies[enemy] = Time.time + config.flowMark.markDuration;

        if (hasFlowBlackHole && Time.time >= blackHoleIcdEnd && GetActiveMarkedEnemyCount() >= config.flowBlackHole.requiredUniqueMarks)
        {
            TriggerBlackHole(enemy.transform.position);
        }
    }

    private void TriggerBlackHole(Vector2 center)
    {
        if (config == null) return;
        blackHoleIcdEnd = Time.time + config.flowBlackHole.internalCooldown;
        StartCoroutine(BlackHoleRoutine(center));

        if (hasFlowBlast)
        {
            blastStoredMarkCount = GetActiveMarkedEnemyCount();
            blastWindowEnd = Time.time + config.flowBlast.blastWindow;
        }
    }

    private IEnumerator BlackHoleRoutine(Vector2 center)
    {
        float endTime = Time.time + config.flowBlackHole.pullDuration;
        while (Time.time < endTime)
        {
            flowMarkedBuffer.Clear();
            foreach (var kv in markedEnemies)
            {
                if (kv.Key == null || kv.Key.CurrentHealth <= 0f) continue;
                flowMarkedBuffer.Add(kv.Key);
            }

            foreach (var enemy in flowMarkedBuffer)
            {
                float dist = Vector2.Distance(enemy.transform.position, center);
                if (dist > config.flowBlackHole.radius) continue;
                Vector2 dir = (center - (Vector2)enemy.transform.position).normalized;
                enemy.transform.position = Vector2.MoveTowards(
                    enemy.transform.position,
                    center,
                    config.flowBlackHole.pullStrength * Time.deltaTime);

                var rb = enemy.GetComponent<Rigidbody2D>();
                if (rb != null) rb.linearVelocity = dir * config.flowBlackHole.pullStrength;
            }

            yield return null;
        }
    }

    private void ApplyChainBounce(EnemyBase primary, float primaryDamage)
    {
        if (config == null) return;

        flowMarkedBuffer.Clear();
        foreach (var kv in markedEnemies)
        {
            var enemy = kv.Key;
            if (enemy == null || enemy == primary || enemy.CurrentHealth <= 0f) continue;
            if (Vector2.Distance(primary.transform.position, enemy.transform.position) > config.flowChain.bounceRange) continue;
            flowMarkedBuffer.Add(enemy);
        }

        if (flowMarkedBuffer.Count == 0) return;
        int bounceCount = Mathf.Min(config.flowChain.maxBounces, flowMarkedBuffer.Count);
        for (int i = 0; i < bounceCount; i++)
        {
            float ratio = config.flowChain.firstBounceRatio - (i * config.flowChain.decayPerBounce);
            ratio = Mathf.Max(0f, ratio);
            if (ratio <= 0f) break;
            float bounceDamage = primaryDamage * ratio;
            flowMarkedBuffer[i].TakeDamage(bounceDamage);
            if (flowMarkedBuffer[i].CurrentHealth <= 0f)
            {
                HandleEnemyKilled(flowMarkedBuffer[i], false);
            }
        }
    }

    private void ApplyBlastSpread(EnemyBase primary, float primaryDamage)
    {
        if (config == null) return;
        float spreadDamage = primaryDamage * config.flowBlast.spreadDamageRatio;
        foreach (var kv in markedEnemies)
        {
            var enemy = kv.Key;
            if (enemy == null || enemy == primary || enemy.CurrentHealth <= 0f) continue;
            enemy.TakeDamage(spreadDamage);
            if (enemy.CurrentHealth <= 0f)
            {
                HandleEnemyKilled(enemy, false);
            }
        }
    }

    private void CleanupFlowMarks()
    {
        if (markedEnemies.Count == 0) return;
        flowMarkedBuffer.Clear();
        foreach (var kv in markedEnemies)
        {
            if (kv.Key == null || kv.Key.CurrentHealth <= 0f || kv.Value < Time.time)
            {
                flowMarkedBuffer.Add(kv.Key);
            }
        }
        for (int i = 0; i < flowMarkedBuffer.Count; i++)
        {
            markedEnemies.Remove(flowMarkedBuffer[i]);
        }
    }

    private bool IsEnemyMarked(EnemyBase enemy)
    {
        if (enemy == null) return false;
        return markedEnemies.TryGetValue(enemy, out float expiry) && expiry >= Time.time;
    }

    private int GetActiveMarkedEnemyCount()
    {
        int count = 0;
        foreach (var kv in markedEnemies)
        {
            if (kv.Key != null && kv.Key.CurrentHealth > 0f && kv.Value >= Time.time)
                count++;
        }
        return count;
    }

    private bool IsPlayerCurrentlyDashing()
    {
        if (playerController == null) return false;
        return playerController.currentState == PlayerController.PlayerState.Dodging
               || playerController.currentState == PlayerController.PlayerState.DashStriking;
    }

    private Vector2 GetEnemyForward(EnemyBase enemy)
    {
        if (enemy == null) return Vector2.right;
        return enemy.transform.localScale.x >= 0f ? Vector2.right : Vector2.left;
    }

    private float GetNearestEnemyDistance(Vector2 position)
    {
        EnemyBase[] all = FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);
        float best = float.MaxValue;
        bool found = false;
        foreach (var enemy in all)
        {
            if (enemy == null || enemy.CurrentHealth <= 0f) continue;
            float d = Vector2.Distance(position, enemy.transform.position);
            if (d < best)
            {
                best = d;
                found = true;
            }
        }
        return found ? best : -1f;
    }
}

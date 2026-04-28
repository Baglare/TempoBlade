using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class FinisherExecutor
{
    private readonly PlayerCombat owner;
    private readonly PlayerWeaponRuntime weaponRuntime;

    public FinisherExecutor(PlayerCombat owner, PlayerWeaponRuntime weaponRuntime)
    {
        this.owner = owner;
        this.weaponRuntime = weaponRuntime;
    }

    public IEnumerator Execute(FinisherSO finisher, FinisherResolutionResult resolution, Action onComplete)
    {
        PlayerController playerController = owner.GetComponent<PlayerController>();
        Rigidbody2D rb = owner.GetComponent<Rigidbody2D>();
        Vector2 startPosition = owner.transform.position;
        bool madeContact = false;
        IDisposable tempoSuppression = null;

        if (TempoManager.Instance != null)
        {
            if (finisher.disableTempoGainDuringFinisher)
                tempoSuppression = TempoManager.Instance.SuppressPositiveTempoGain("PlayerFinisher");

            if (finisher.tempoCostMode == FinisherTempoCostMode.ResetToZero)
                TempoManager.Instance.ResetTempoToZero();
        }

        owner.SetFinisherActive(true);
        owner.SetFinisherDamageImmune(finisher.playerSafetyMode == FinisherPlayerSafetyMode.InvulnerableDuringAction);
        owner.ResetComboState();

        if (playerController != null)
            playerController.canMove = false;

        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        if (finisher.timeScaleBehavior == FinisherTimeScaleBehavior.ShortSlowMotion)
            yield return ApplyShortSlowMotion(finisher.cameraVfxProfile);

        switch (finisher.executionMode)
        {
            case FinisherExecutionMode.DashThroughMultiHit:
                yield return ExecuteDashThroughMultiHit(finisher, resolution, hit => madeContact |= hit);
                break;

            case FinisherExecutionMode.HeavySlamBurst:
                yield return ExecuteHeavySlamBurst(finisher, resolution, hit => madeContact |= hit);
                break;

            default:
                yield return ExecuteForwardCleave(finisher, resolution, hit => madeContact |= hit);
                break;
        }

        if (finisher.returnBehavior == FinisherReturnBehavior.ReturnToStart)
            yield return MovePlayer(startPosition, 0.08f);
        else if (finisher.returnBehavior == FinisherReturnBehavior.SnapBehindPrimaryTarget && resolution.primaryTarget != null)
            yield return MovePlayer((Vector2)resolution.primaryTarget.transform.position - resolution.aimDirection * 0.55f, 0.05f);

        if (madeContact)
        {
            if (DamagePopupManager.Instance != null)
                DamagePopupManager.Instance.CreateText(owner.transform.position + Vector3.up * 1.4f, finisher.cameraVfxProfile.popupText, finisher.cameraVfxProfile.popupColor, 8f);

            if (finisher.cameraVfxProfile.useHeavyHitStop && HitStopManager.Instance != null)
                HitStopManager.Instance.PlayHeavyHitStop();

            if (CameraShakeManager.Instance != null)
                CameraShakeManager.Instance.ShakeCamera(finisher.cameraVfxProfile.cameraShakeIntensity, finisher.cameraVfxProfile.cameraShakeDuration);
        }
        else if (DamagePopupManager.Instance != null)
        {
            DamagePopupManager.Instance.CreateText(owner.transform.position + Vector3.up * 1.2f, "WHIFF!", Color.gray, 6f);
        }

        owner.SetFinisherDamageImmune(false);
        owner.SetFinisherActive(false);

        if (playerController != null)
            playerController.canMove = true;

        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        tempoSuppression?.Dispose();
        onComplete?.Invoke();
    }

    private IEnumerator ExecuteForwardCleave(FinisherSO finisher, FinisherResolutionResult resolution, Action<bool> registerContact)
    {
        if (finisher.movementBehavior == FinisherMovementBehavior.StepForward)
        {
            Vector2 stepTarget = (Vector2)owner.transform.position + resolution.aimDirection * 0.55f;
            yield return MovePlayer(stepTarget, 0.08f);
        }

        registerContact?.Invoke(ApplyHitSet(finisher, resolution.targets, weaponRuntime.GetResolvedStats(), false));
    }

    private IEnumerator ExecuteHeavySlamBurst(FinisherSO finisher, FinisherResolutionResult resolution, Action<bool> registerContact)
    {
        if (finisher.movementBehavior == FinisherMovementBehavior.StepForward)
        {
            Vector2 stepTarget = (Vector2)owner.transform.position + resolution.aimDirection * 0.42f;
            yield return MovePlayer(stepTarget, 0.1f);
        }

        yield return new WaitForSecondsRealtime(0.04f);
        registerContact?.Invoke(ApplyHitSet(finisher, resolution.targets, weaponRuntime.GetResolvedStats(), true));
    }

    private IEnumerator ExecuteDashThroughMultiHit(FinisherSO finisher, FinisherResolutionResult resolution, Action<bool> registerContact)
    {
        WeaponResolvedStats stats = weaponRuntime.GetResolvedStats();

        if (resolution.primaryTarget == null)
        {
            Vector2 whiffTarget = (Vector2)owner.transform.position + resolution.aimDirection * 1.2f;
            yield return MovePlayer(whiffTarget, 0.12f);
            yield break;
        }

        Vector2 targetPos = resolution.primaryTarget.transform.position;
        Vector2 exitPos = targetPos + resolution.aimDirection * 0.8f;

        yield return MovePlayer(targetPos, 0.08f);

        int hitCount = Mathf.Max(1, finisher.damageProfile.hitCount);
        float delay = Mathf.Max(0.01f, finisher.damageProfile.timeBetweenHits);
        for (int i = 0; i < hitCount; i++)
        {
            FinisherResolvedTarget singleTarget = new FinisherResolvedTarget
            {
                enemy = resolution.primaryTarget,
                combatClass = FinisherTargetResolver.ResolveCombatClass(resolution.primaryTarget),
                distance = Vector2.Distance(owner.transform.position, resolution.primaryTarget.transform.position)
            };

            registerContact?.Invoke(ApplyHitSet(finisher, new List<FinisherResolvedTarget> { singleTarget }, stats, false));
            if (i < hitCount - 1)
                yield return new WaitForSecondsRealtime(delay);
        }

        yield return MovePlayer(exitPos, 0.09f);
    }

    private bool ApplyHitSet(FinisherSO finisher, List<FinisherResolvedTarget> targets, WeaponResolvedStats stats, bool heavyImpact)
    {
        if (targets == null || targets.Count == 0)
            return false;

        bool hitSomeone = false;
        float baseDamage = Mathf.Max(stats.damage, stats.damage * finisher.damageProfile.damageMultiplier + finisher.damageProfile.flatBonusDamage);

        foreach (FinisherResolvedTarget target in targets)
        {
            if (target.enemy == null || target.enemy.CurrentHealth <= 0f)
                continue;

            FinisherEnemyClassRule rule = finisher.enemyClassRuleSet.GetRule(target.combatClass);
            float finalDamage = baseDamage * rule.damageMultiplier;
            if (rule.maxHealthPercentCap > 0f)
                finalDamage = Mathf.Min(finalDamage, target.enemy.MaxHealth * rule.maxHealthPercentCap);

            if (!rule.allowKillingBlow && target.enemy.CurrentHealth - finalDamage <= 0f)
                finalDamage = Mathf.Max(0f, target.enemy.CurrentHealth - 1f);

            if (finalDamage <= 0f)
                continue;

            float beforeHealth = target.enemy.CurrentHealth;
            target.enemy.TakeDamage(BuildFinisherPayload(target.enemy, finalDamage, stats, heavyImpact));
            target.enemy.Stun(rule.pressureStaggerDuration + stats.finisherPressureBonus + (heavyImpact ? 0.08f : 0f));

            bool killed = beforeHealth > 0f && target.enemy.CurrentHealth <= 0f;
            owner.RecordFinisherHit(target.enemy, killed, finalDamage);
            if (DamagePopupManager.Instance != null)
                DamagePopupManager.Instance.CreateHitParticle(target.enemy.transform.position);

            hitSomeone = true;
        }

        return hitSomeone;
    }

    private EnemyDamagePayload BuildFinisherPayload(EnemyBase enemy, float finalDamage, WeaponResolvedStats stats, bool heavyImpact)
    {
        Vector2 hitDirection = Vector2.zero;
        if (enemy != null)
        {
            hitDirection = (Vector2)enemy.transform.position - (Vector2)owner.transform.position;
            if (hitDirection.sqrMagnitude > 0.001f)
                hitDirection.Normalize();
        }

        float stabilityMultiplier = Mathf.Max(0f, stats.stabilityDamageMultiplier) *
                                    Mathf.Max(0f, stats.finisherStabilityMultiplier);

        return new EnemyDamagePayload
        {
            healthDamage = finalDamage,
            stabilityDamage = finalDamage * stabilityMultiplier,
            hasExplicitStabilityDamage = true,
            damageSource = EnemyDamageSource.PlayerFinisher,
            hitDirection = hitDirection,
            instigator = owner.gameObject,
            isFinisher = true,
            isParryCounter = false,
            isDashAttack = false,
            isCritical = true,
            isPerfectTiming = heavyImpact
        };
    }

    private IEnumerator ApplyShortSlowMotion(FinisherCameraVfxProfile profile)
    {
        float previousScale = Time.timeScale;
        Time.timeScale = Mathf.Clamp(profile.slowMotionScale, 0.05f, 1f);
        yield return new WaitForSecondsRealtime(Mathf.Max(0.01f, profile.slowMotionDuration));
        Time.timeScale = previousScale;
    }

    private IEnumerator MovePlayer(Vector2 targetPosition, float duration)
    {
        Vector2 start = owner.transform.position;
        float elapsed = 0f;
        duration = Mathf.Max(0.01f, duration);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            owner.transform.position = Vector2.Lerp(start, targetPosition, t);
            yield return null;
        }

        owner.transform.position = targetPosition;
    }
}

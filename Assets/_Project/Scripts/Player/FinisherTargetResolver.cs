using System.Collections.Generic;
using UnityEngine;

public sealed class FinisherTargetResolver
{
    private const int InitialTargetBufferSize = 64;

    private readonly PlayerCombat owner;
    private readonly List<FinisherResolvedTarget> targets = new();
    private Collider2D[] hitBuffer = new Collider2D[InitialTargetBufferSize];

    public FinisherTargetResolver(PlayerCombat owner)
    {
        this.owner = owner;
    }

    public FinisherResolutionResult Resolve(FinisherSO finisher)
    {
        targets.Clear();

        FinisherResolutionResult result = new FinisherResolutionResult
        {
            aimDirection = owner.CurrentAimDirection.sqrMagnitude > 0.001f ? owner.CurrentAimDirection.normalized : Vector2.right
        };

        if (finisher == null || owner.AttackPoint == null)
            return result;

        float baseRange = owner.GetEffectiveRange();
        float queryRange = Mathf.Max(0.8f, baseRange * finisher.damageProfile.rangeMultiplier + finisher.damageProfile.radiusBonus);
        Vector2 center = owner.AttackPoint.position;
        int hitCount = QueryTargets(center, queryRange);

        EnemyBase bestFrontTarget = null;
        float bestFrontScore = float.MinValue;

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit = hitBuffer[i];
            if (hit == null)
                continue;

            EnemyBase enemy = hit.GetComponentInParent<EnemyBase>();
            if (enemy == null || enemy.CurrentHealth <= 0f || !enemy.gameObject.activeInHierarchy)
                continue;

            Vector2 toEnemy = (Vector2)enemy.transform.position - (Vector2)owner.transform.position;
            float distance = toEnemy.magnitude;
            if (distance <= 0.001f)
                continue;

            Vector2 dir = toEnemy / distance;
            float dot = Vector2.Dot(result.aimDirection, dir);

            bool includeTarget = finisher.targetingMode switch
            {
                FinisherTargetingMode.SelfRadius => true,
                FinisherTargetingMode.FrontArcArea => dot >= 0.25f,
                FinisherTargetingMode.ClosestInFront => dot >= 0.35f,
                _ => false
            };

            if (!includeTarget)
                continue;

            EnemyCombatClass combatClass = ResolveCombatClass(enemy);
            targets.Add(new FinisherResolvedTarget
            {
                enemy = enemy,
                combatClass = combatClass,
                distance = distance
            });

            float score = dot * 3f - distance;
            if (score > bestFrontScore)
            {
                bestFrontScore = score;
                bestFrontTarget = enemy;
            }
        }

        if (finisher.targetingMode == FinisherTargetingMode.ClosestInFront)
        {
            targets.RemoveAll(t => t.enemy != bestFrontTarget);
            result.primaryTarget = bestFrontTarget;
        }
        else
        {
            result.primaryTarget = bestFrontTarget;
        }

        result.targets = new List<FinisherResolvedTarget>(targets);
        result.hitCenter = center;
        result.queryRange = queryRange;
        return result;
    }

    private int QueryTargets(Vector2 center, float queryRange)
    {
        int hitCount = Physics2D.OverlapCircleNonAlloc(center, queryRange, hitBuffer, owner.EnemyLayers);
        while (hitCount >= hitBuffer.Length)
        {
            System.Array.Resize(ref hitBuffer, hitBuffer.Length * 2);
            hitCount = Physics2D.OverlapCircleNonAlloc(center, queryRange, hitBuffer, owner.EnemyLayers);
        }

        return hitCount;
    }

    public static EnemyCombatClass ResolveCombatClass(EnemyBase enemy)
    {
        if (enemy == null)
            return EnemyCombatClass.Normal;

        return enemy.CombatClass;
    }
}

public class FinisherResolutionResult
{
    public List<FinisherResolvedTarget> targets = new();
    public EnemyBase primaryTarget;
    public Vector2 aimDirection;
    public Vector2 hitCenter;
    public float queryRange;
}

public struct FinisherResolvedTarget
{
    public EnemyBase enemy;
    public EnemyCombatClass combatClass;
    public float distance;
}

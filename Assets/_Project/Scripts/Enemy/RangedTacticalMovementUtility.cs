using UnityEngine;

public struct RangedTacticalDecision
{
    public Vector2 moveTarget;
    public bool hasLineOfSight;
    public bool canFireFromCurrent;
    public bool foundBetterPosition;
}

public static class RangedTacticalMovementUtility
{
    public static RangedTacticalDecision EvaluatePosition(
        Transform self,
        Transform player,
        Vector2 firingOrigin,
        float maintainRange,
        float firePermissionRange,
        float localSearchRadius,
        int sampleCount,
        Transform ignoreRoot = null)
    {
        RangedTacticalDecision decision = new RangedTacticalDecision
        {
            moveTarget = self != null ? (Vector2)self.position : Vector2.zero,
            hasLineOfSight = false,
            canFireFromCurrent = false,
            foundBetterPosition = false
        };

        if (self == null || player == null)
            return decision;

        Vector2 selfPos = self.position;
        Vector2 playerPos = player.position;
        float currentDistance = Vector2.Distance(selfPos, playerPos);

        bool hasLos = EnemyLineOfSightUtility.HasLineOfSight(firingOrigin, player, default, ignoreRoot);
        decision.hasLineOfSight = hasLos;
        decision.canFireFromCurrent = hasLos && currentDistance <= firePermissionRange;

        Vector2 bestPoint = selfPos;
        float bestScore = float.MinValue;
        int clampedSamples = Mathf.Max(6, sampleCount);

        for (int i = 0; i < clampedSamples; i++)
        {
            float angle = (360f / clampedSamples) * i;
            Vector2 radial = Quaternion.Euler(0f, 0f, angle) * Vector2.right;
            Vector2 candidate = playerPos + radial * maintainRange;
            EvaluateCandidate(self, player, candidate, firingOrigin, maintainRange, firePermissionRange, ignoreRoot, ref bestPoint, ref bestScore);
        }

        for (int i = 0; i < clampedSamples; i++)
        {
            float angle = (360f / clampedSamples) * i;
            Vector2 radial = Quaternion.Euler(0f, 0f, angle) * Vector2.right;
            Vector2 candidate = selfPos + radial * localSearchRadius;
            EvaluateCandidate(self, player, candidate, firingOrigin, maintainRange, firePermissionRange, ignoreRoot, ref bestPoint, ref bestScore);
        }

        if (bestScore > float.MinValue * 0.5f)
        {
            decision.moveTarget = bestPoint;
            decision.foundBetterPosition = (bestPoint - selfPos).sqrMagnitude > 0.08f;
        }

        return decision;
    }

    public static bool TryFindEscapePoint(
        Transform self,
        Vector2 awayFromPoint,
        float preferredDistance,
        float sectorDegrees,
        int sampleCount,
        Transform ignoreRoot,
        out Vector2 escapePoint)
    {
        escapePoint = self != null ? (Vector2)self.position : Vector2.zero;
        if (self == null)
            return false;

        Vector2 selfPos = self.position;
        Vector2 away = (selfPos - awayFromPoint).normalized;
        if (away.sqrMagnitude <= 0.001f)
            away = Vector2.right;

        float bestScore = float.MinValue;
        int clampedSamples = Mathf.Max(4, sampleCount);
        for (int i = 0; i < clampedSamples; i++)
        {
            float t = clampedSamples == 1 ? 0.5f : i / (float)(clampedSamples - 1);
            float offset = Mathf.Lerp(-sectorDegrees * 0.5f, sectorDegrees * 0.5f, t);
            Vector2 dir = Quaternion.Euler(0f, 0f, offset) * away;
            Vector2 candidate = selfPos + dir * preferredDistance;
            if (!EnemyLineOfSightUtility.IsPointNavigable(candidate, 0.35f, ignoreRoot))
                continue;

            float score = 0f;
            score += Vector2.Distance(candidate, awayFromPoint);
            score -= EnemyLineOfSightUtility.GetObstacleDensity(candidate, 0.6f, ignoreRoot) * 2f;
            if (score <= bestScore)
                continue;

            bestScore = score;
            escapePoint = candidate;
        }

        return bestScore > float.MinValue * 0.5f;
    }

    private static void EvaluateCandidate(
        Transform self,
        Transform player,
        Vector2 candidate,
        Vector2 firingOrigin,
        float maintainRange,
        float firePermissionRange,
        Transform ignoreRoot,
        ref Vector2 bestPoint,
        ref float bestScore)
    {
        if (!EnemyLineOfSightUtility.IsPointNavigable(candidate, 0.32f, ignoreRoot))
            return;

        float distanceToPlayer = Vector2.Distance(candidate, player.position);
        bool hasLos = EnemyLineOfSightUtility.HasLineOfSight(candidate, player, default, ignoreRoot);
        float score = 0f;
        score += hasLos ? 40f : -12f;
        score -= Mathf.Abs(distanceToPlayer - maintainRange) * 4.2f;
        if (distanceToPlayer <= firePermissionRange)
            score += 12f;
        score -= Vector2.Distance(self.position, candidate) * 0.55f;
        score -= EnemyLineOfSightUtility.GetObstacleDensity(candidate, 0.65f, ignoreRoot) * 2.5f;

        if (score <= bestScore)
            return;

        bestScore = score;
        bestPoint = candidate;
    }
}

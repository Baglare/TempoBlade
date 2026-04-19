using System.Collections.Generic;
using UnityEngine;

public static class EnemySupportUtility
{
    public static bool IsPlayerRepeatingBasicAttacks(float window, int requiredCount)
    {
        CombatTelemetryHub telemetry = Object.FindFirstObjectByType<CombatTelemetryHub>();
        if (telemetry == null)
            return false;

        var events = telemetry.GetRecentEvents();
        int attackCount = 0;
        for (int i = events.Count - 1; i >= 0; i--)
        {
            CombatTelemetryEvent combatEvent = events[i];
            if (Time.time - combatEvent.time > window)
                break;

            if (combatEvent.actionType == CombatActionType.Attack)
            {
                attackCount++;
                if (attackCount >= requiredCount)
                    return true;
                continue;
            }

            if (combatEvent.actionType == CombatActionType.Dash ||
                combatEvent.actionType == CombatActionType.Parry ||
                combatEvent.actionType == CombatActionType.PerfectParry ||
                combatEvent.actionType == CombatActionType.Skill)
            {
                return false;
            }
        }

        return false;
    }

    public static void GatherNearbyAllies(Transform origin, float radius, List<EnemyBase> results, EnemyBase exclude = null)
    {
        results.Clear();
        if (origin == null)
            return;

        EnemyBase[] enemies = Object.FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);
        float radiusSqr = radius * radius;

        for (int i = 0; i < enemies.Length; i++)
        {
            EnemyBase enemy = enemies[i];
            if (enemy == null || enemy == exclude || enemy.CurrentHealth <= 0f || !enemy.gameObject.activeInHierarchy)
                continue;

            float distSqr = ((Vector2)enemy.transform.position - (Vector2)origin.position).sqrMagnitude;
            if (distSqr <= radiusSqr)
                results.Add(enemy);
        }
    }

    public static Vector2 FindBestRallyAnchor(Transform self, float searchRadius, float synergyRadius, EnemyBase exclude, out int allyCount)
    {
        allyCount = 0;
        Vector2 bestPosition = self != null ? self.position : Vector2.zero;

        if (self == null)
            return bestPosition;

        EnemyBase[] enemies = Object.FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);
        float bestScore = -1f;
        for (int i = 0; i < enemies.Length; i++)
        {
            EnemyBase candidate = enemies[i];
            if (candidate == null || candidate == exclude || candidate.CurrentHealth <= 0f || !candidate.gameObject.activeInHierarchy)
                continue;

            float selfDistance = Vector2.Distance(self.position, candidate.transform.position);
            if (selfDistance > searchRadius)
                continue;

            int count = CountAlliesInRadius(candidate.transform.position, synergyRadius, exclude);
            float score = count - selfDistance * 0.1f;
            if (score <= bestScore)
                continue;

            bestScore = score;
            allyCount = count;
            bestPosition = candidate.transform.position;
        }

        return bestPosition;
    }

    public static EnemyBase SelectGuardianLinkTarget(Transform self, float searchRadius, EnemyBase exclude)
    {
        EnemyBase[] enemies = Object.FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);
        EnemyBase bestTarget = null;
        float bestScore = float.MinValue;

        for (int i = 0; i < enemies.Length; i++)
        {
            EnemyBase enemy = enemies[i];
            if (enemy == null || enemy == exclude || enemy.CurrentHealth <= 0f || !enemy.gameObject.activeInHierarchy)
                continue;

            float distance = Vector2.Distance(self.position, enemy.transform.position);
            if (distance > searchRadius)
                continue;

            float score = GetGuardianPriorityScore(enemy) - distance * 0.08f;
            if (score <= bestScore)
                continue;

            bestScore = score;
            bestTarget = enemy;
        }

        return bestTarget;
    }

    public static Vector2 FindBestTrapPlacement(Transform self, Transform player, float desiredDistance, int sampleCount, float forwardBias)
    {
        if (self == null || player == null)
            return self != null ? (Vector2)self.position : Vector2.zero;

        Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
        Vector2 playerVelocity = playerRb != null ? playerRb.linearVelocity : Vector2.zero;
        Vector2 moveDir = playerVelocity.sqrMagnitude > 0.04f ? playerVelocity.normalized : Vector2.zero;
        Vector2 fallbackDir = moveDir.sqrMagnitude > 0.001f ? moveDir : ((Vector2)player.position - (Vector2)self.position).normalized;
        if (fallbackDir.sqrMagnitude <= 0.001f)
            fallbackDir = Vector2.right;

        Vector2 best = player.position;
        float bestScore = float.MinValue;
        int clampedSamples = Mathf.Max(4, sampleCount);
        for (int i = 0; i < clampedSamples; i++)
        {
            float angle = (360f / clampedSamples) * i;
            Vector2 radial = Quaternion.Euler(0f, 0f, angle) * fallbackDir;
            Vector2 candidate = (Vector2)player.position + radial * desiredDistance;

            float score = 0f;
            score -= Vector2.Distance(self.position, candidate) * 0.1f;
            score -= Mathf.Abs(Vector2.Distance(player.position, candidate) - desiredDistance);

            if (moveDir.sqrMagnitude > 0.001f)
                score += Mathf.Max(0f, Vector2.Dot((candidate - (Vector2)player.position).normalized, moveDir)) * forwardBias;

            int nearbyAllies = CountAlliesInRadius(candidate, 2.8f, self.GetComponent<EnemyBase>());
            score += nearbyAllies * 0.45f;

            if (score > bestScore)
            {
                bestScore = score;
                best = candidate;
            }
        }

        return best;
    }

    private static int CountAlliesInRadius(Vector2 center, float radius, EnemyBase exclude)
    {
        EnemyBase[] enemies = Object.FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);
        int count = 0;
        float radiusSqr = radius * radius;

        for (int i = 0; i < enemies.Length; i++)
        {
            EnemyBase enemy = enemies[i];
            if (enemy == null || enemy == exclude || enemy.CurrentHealth <= 0f || !enemy.gameObject.activeInHierarchy)
                continue;

            float distSqr = ((Vector2)enemy.transform.position - center).sqrMagnitude;
            if (distSqr <= radiusSqr)
                count++;
        }

        return count;
    }

    public static float GetGuardianPriorityScore(EnemyBase enemy)
    {
        if (enemy is EnemyBoss)
            return 100f;
        if (enemy is EnemyCaster || enemy is EnemyResonator || enemy is EnemyDeadeye)
            return 80f;
        if (enemy is EnemyDuelist || enemy is EnemyAssassin)
            return 60f;
        if (enemy is EnemyWarden)
            return 50f;

        return 30f;
    }
}

using System.Collections.Generic;
using UnityEngine;

public static class EnemySupportUtility
{
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

    private static float GetGuardianPriorityScore(EnemyBase enemy)
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

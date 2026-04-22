using UnityEngine;

public static class EnemySummonHelper
{
    private static EliteProfileSO fallbackEliteWardenProfile;

    public static GameObject SummonTemporaryEnemy(GameObject prefab, Vector3 position, float duration, float alpha, EliteProfileSO eliteProfile = null, EnemyBase summoner = null)
    {
        if (prefab == null)
            return null;

        GameObject instance = Object.Instantiate(prefab, position, Quaternion.identity);
        EliteProfileSO appliedProfile = eliteProfile;
        if (appliedProfile == null && summoner is EnemyWardenLinker && instance.GetComponent<EnemyWarden>() != null)
            appliedProfile = GetFallbackEliteWardenProfile();

        if (appliedProfile != null)
        {
            EnemyBase enemyBase = instance.GetComponent<EnemyBase>();
            if (enemyBase != null)
                enemyBase.ApplyEliteProfile(appliedProfile);
        }

        TemporaryEnemySummon summon = instance.GetComponent<TemporaryEnemySummon>();
        if (summon == null)
            summon = instance.AddComponent<TemporaryEnemySummon>();

        summon.SetSummoner(summoner);
        summon.Configure(duration, alpha);
        return instance;
    }

    private static EliteProfileSO GetFallbackEliteWardenProfile()
    {
        if (fallbackEliteWardenProfile != null)
            return fallbackEliteWardenProfile;

        fallbackEliteWardenProfile = ScriptableObject.CreateInstance<EliteProfileSO>();
        fallbackEliteWardenProfile.name = "Runtime_Fallback_Elite_Warden";
        fallbackEliteWardenProfile.healthMultiplier = 1.28f;
        fallbackEliteWardenProfile.damageMultiplier = 1.14f;
        fallbackEliteWardenProfile.cooldownMultiplier = 0.88f;
        fallbackEliteWardenProfile.moveSpeedMultiplier = 1.02f;
        fallbackEliteWardenProfile.eliteCueColor = new Color(0.35f, 0.95f, 1f, 1f);
        fallbackEliteWardenProfile.eliteMechanicType = EliteMechanicType.WardenLivingDefenceWall;
        return fallbackEliteWardenProfile;
    }
}

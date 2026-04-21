using UnityEngine;

public static class EnemySummonHelper
{
    public static GameObject SummonTemporaryEnemy(GameObject prefab, Vector3 position, float duration, float alpha, EliteProfileSO eliteProfile = null)
    {
        if (prefab == null)
            return null;

        GameObject instance = Object.Instantiate(prefab, position, Quaternion.identity);
        if (eliteProfile != null)
        {
            EnemyBase enemyBase = instance.GetComponent<EnemyBase>();
            if (enemyBase != null)
                enemyBase.ApplyEliteProfile(eliteProfile);
        }

        TemporaryEnemySummon summon = instance.GetComponent<TemporaryEnemySummon>();
        if (summon == null)
            summon = instance.AddComponent<TemporaryEnemySummon>();

        summon.Configure(duration, alpha);
        return instance;
    }
}

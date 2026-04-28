using UnityEngine;

public static class EnemyDamageUtility
{
    public static EnemyDamageResult ApplyDamage(
        EnemyBase enemy,
        float healthDamage,
        EnemyDamageSource source,
        GameObject instigator = null,
        Vector2 hitDirection = default,
        float stabilityMultiplier = 0.65f,
        bool isFinisher = false,
        bool isParryCounter = false,
        bool isDashAttack = false,
        bool isCritical = false,
        bool isPerfectTiming = false)
    {
        if (enemy == null)
            return EnemyDamageResult.Ignored(EnemyDamagePayload.FromHealthDamage(healthDamage, instigator), EnemyCombatClass.Normal);

        EnemyDamagePayload payload = new EnemyDamagePayload
        {
            healthDamage = healthDamage,
            stabilityDamage = Mathf.Max(0f, healthDamage) * Mathf.Max(0f, stabilityMultiplier),
            hasExplicitStabilityDamage = true,
            damageSource = source,
            hitDirection = hitDirection,
            instigator = instigator,
            isFinisher = isFinisher,
            isParryCounter = isParryCounter,
            isDashAttack = isDashAttack,
            isCritical = isCritical,
            isPerfectTiming = isPerfectTiming
        };

        return enemy.TakeDamage(payload);
    }

    public static Vector2 DirectionFromInstigator(EnemyBase enemy, GameObject instigator)
    {
        if (enemy == null || instigator == null)
            return Vector2.zero;

        Vector2 direction = (Vector2)enemy.transform.position - (Vector2)instigator.transform.position;
        return direction.sqrMagnitude > 0.001f ? direction.normalized : Vector2.zero;
    }
}

using UnityEngine;

public struct EnemyPlayerHitResult
{
    public bool isPlayer;
    public bool parried;
    public bool dodged;
    public bool damaged;
    public PlayerController playerController;
}

public static class EnemyPlayerHitUtility
{
    public static EnemyPlayerHitResult ApplyMeleeHit(EnemyBase attacker, Collider2D hit, Vector2 strikeOrigin, float damage)
    {
        EnemyPlayerHitResult result = new EnemyPlayerHitResult();
        if (hit == null || !hit.CompareTag("Player"))
            return result;

        result.isPlayer = true;
        GameObject attackerObject = attacker != null ? attacker.gameObject : null;

        ParrySystem parry = hit.GetComponent<ParrySystem>();
        if (parry != null && parry.TryBlockMelee(strikeOrigin, attackerObject))
        {
            result.parried = true;
            return result;
        }

        PlayerController playerController = hit.GetComponent<PlayerController>();
        result.playerController = playerController;
        if (playerController != null && playerController.IsInvulnerable)
        {
            result.dodged = true;
            hit.GetComponent<DashPerkController>()?.NotifyMeleeDodged(attacker);
            return result;
        }

        IDamageable damageable = hit.GetComponent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(damage);
            result.damaged = true;
        }

        return result;
    }
}

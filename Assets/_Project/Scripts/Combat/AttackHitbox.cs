using UnityEngine;

public class AttackHitbox : MonoBehaviour
{
    [HideInInspector] public EnemyBase owner; // EnemyDummy yerine EnemyBase

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        // Player uzerinde ParrySystem var mi? (Yonlu blok: saldirganin pozisyonuyla kontrol)
        var parry = other.GetComponent<ParrySystem>();
        Vector2 attackerPos = owner != null ? (Vector2)owner.transform.position : (Vector2)transform.position;
        if (parry != null && parry.TryBlockMelee(attackerPos, gameObject))
        {
            var ownCollider = GetComponent<Collider2D>();
            if (ownCollider != null)
                ownCollider.enabled = false;
            return;
        }



        var playerCombat = other.GetComponent<PlayerCombat>();
        var playerController = other.GetComponent<PlayerController>();
        if (playerController != null && playerController.IsInvulnerable)
        {
            other.GetComponent<DashPerkController>()?.NotifyMeleeDodged(this);
            return;
        }

        if (playerCombat != null)
        {
             float dmg = owner != null ? owner.GetEffectiveContactDamage(10f) : 10f;
             playerCombat.TakeDamage(dmg); 
        }
    }
}

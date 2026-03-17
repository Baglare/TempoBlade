using UnityEngine;

public class AttackHitbox : MonoBehaviour
{
    [HideInInspector] public EnemyBase owner; // EnemyDummy yerine EnemyBase

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        // Dash Tier1: Melee Dodge penceresi
        var dashRuntime = other.GetComponent<DashSkillRuntime>();
        if (dashRuntime == null) dashRuntime = other.GetComponentInParent<DashSkillRuntime>();
        Vector2 attackerPos = owner != null ? (Vector2)owner.transform.position : (Vector2)transform.position;
        if (dashRuntime != null && dashRuntime.TryDodgeMelee(attackerPos))
        {
            if (DamagePopupManager.Instance != null)
                DamagePopupManager.Instance.CreateText(other.transform.position + Vector3.up, "DODGE!", Color.cyan, 5f);
            return;
        }

        // Player uzerinde ParrySystem var mi? (Yonlu blok: saldirganin pozisyonuyla kontrol)
        var parry = other.GetComponent<ParrySystem>();
        if (parry == null) parry = other.GetComponentInParent<ParrySystem>();
        if (parry != null && parry.TryBlockMelee(attackerPos))
        {
            // Parry basarili: dusman reaksiyon alabilir
            if (owner != null)
                owner.Stun(1.5f);
            return;
        }



        var playerCombat = other.GetComponent<PlayerCombat>();
        if (playerCombat == null) playerCombat = other.GetComponentInParent<PlayerCombat>();
        if (playerCombat != null)
        {
             float dmg = (owner != null && owner.enemyData != null) ? owner.enemyData.damage : 10f;
             playerCombat.TakeDamage(dmg); 
        }
    }
}

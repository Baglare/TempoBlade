using UnityEngine;

public interface IDamageable
{
    void TakeDamage(float damageAmount);
    void Stun(float duration);
}

using UnityEngine;

public interface IDamageable
{
    void TakeDamage(float damageAmount);
    void Stun(float duration);
}

public interface ICombatTarget
{
    Transform TargetTransform { get; }
    GameObject TargetObject { get; }
    float CurrentHealth { get; }
    float MaxHealth { get; }
    bool IsAlive { get; }
    void TakeDamage(float amount);
}

public interface IRuntimePoolable
{
    void OnSpawnedFromPool();
    void OnReturnedToPool();
}

public interface ICombatFeedbackEmitter
{
    void EmitCombatFeedback(CombatFeedbackEventData data);
}

public struct CombatFeedbackEventData
{
    public GameObject source;
    public GameObject target;
    public Vector3 worldPosition;
    public Vector2 direction;
    public float magnitude;
    public CombatFeedbackEventType type;
}

public enum CombatFeedbackEventType
{
    None,
    AttackStarted,
    AttackHit,
    Parry,
    Dodge,
    Death
}

using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour, IDeflectable
{
    public float speed = 10f;
    public float damage = 10f;
    public float lifeTime = 5f;

    [HideInInspector] public GameObject owner;
    private GameObject sourceOwner;

    public GameObject ObjectOwner => owner;
    public GameObject SourceOwner => sourceOwner;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private bool hasBeenDeflected;
    private float remainingLife;
    private int remainingPierceCount;
    private float suppressDuration;
    private int splitCount;
    private float splitDamageMultiplier = 0.5f;
    private float splitAngleSpread = 20f;
    private float splitSpeedMultiplier = 1f;
    private bool hasSpawnedSplits;
    private readonly HashSet<int> hitTargets = new HashSet<int>();

    public bool IsDeflected => hasBeenDeflected;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        remainingLife = lifeTime;
    }

    private void Update()
    {
        remainingLife -= Time.deltaTime;
        if (remainingLife <= 0f)
            DestroyProjectile();
    }

    public void Launch(Vector2 direction)
    {
        if (sourceOwner == null)
            sourceOwner = owner;

        ApplyVelocity(direction.normalized * speed);
        AudioManager.Play(AudioEventId.ProjectileLaunch, gameObject);
    }

    public void Deflect(DeflectContext context)
    {
        if (hasBeenDeflected)
            return;

        hasBeenDeflected = true;
        sourceOwner = owner != null ? owner : sourceOwner;
        owner = context.newOwner;

        remainingPierceCount = Mathf.Max(0, context.pierceCount);
        suppressDuration = Mathf.Max(0f, context.suppressDuration);
        splitCount = Mathf.Max(0, context.splitCount);
        splitDamageMultiplier = Mathf.Max(0f, context.splitDamageMultiplier);
        splitAngleSpread = Mathf.Max(0f, context.splitAngleSpread);
        splitSpeedMultiplier = Mathf.Max(0.05f, context.splitSpeedMultiplier);

        damage *= Mathf.Max(0f, context.damageMultiplier);

        Vector2 newDirection = ResolveDeflectDirection(context);
        float finalSpeed = speed * Mathf.Max(0.05f, context.speedMultiplier);
        speed = finalSpeed;
        ApplyVelocity(newDirection * finalSpeed);
        AudioManager.Play(AudioEventId.ProjectileDeflect, gameObject);

        remainingLife = Mathf.Max(remainingLife, lifeTime + 2f);
        hitTargets.Clear();

        if (spriteRenderer != null)
            spriteRenderer.color = Color.yellow;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (owner != null && other.gameObject == owner)
            return;

        if (other.gameObject.layer == LayerMask.NameToLayer("Environment"))
        {
            DestroyProjectile();
            return;
        }

        IDamageable damageable = other.GetComponent<IDamageable>();
        if (damageable == null)
            return;

        bool isHittingEnemy = other.CompareTag("Enemy");

        if (isHittingEnemy && !hasBeenDeflected)
            return;

        if (other.CompareTag("Player") && !hasBeenDeflected)
        {
            ParrySystem parry = other.GetComponent<ParrySystem>();
            if (parry != null && parry.TryDeflect(transform.position, gameObject))
            {
                Deflect(BuildDeflectContext(other.gameObject, other.transform.position));
                return;
            }

            var playerController = other.GetComponent<PlayerController>();
            if (playerController != null && playerController.IsInvulnerable)
            {
                other.GetComponent<DashPerkController>()?.NotifyProjectileDodged(this);
                DestroyProjectile();
                return;
            }
        }

        int targetId = other.GetInstanceID();
        if (!hitTargets.Add(targetId))
            return;

        damageable.TakeDamage(damage);
        AudioManager.Play(AudioEventId.ProjectileHit, gameObject, other.transform.position);

        if (hasBeenDeflected && isHittingEnemy)
        {
            EnemyBase enemy = other.GetComponent<EnemyBase>();
            owner?.GetComponent<CombatTelemetryHub>()?.RecordDeflectHit(enemy, damage);
            owner?.GetComponent<ParryPerkController>()?.HandleProjectileHitReaction(other.gameObject);

            if (DamagePopupManager.Instance != null)
                DamagePopupManager.Instance.CreateText(transform.position + Vector3.up, "DEFLECT HIT!", Color.magenta, 8f);

            TrySpawnSplitProjectiles();

            if (remainingPierceCount > 0)
            {
                remainingPierceCount--;
                return;
            }
        }

        DestroyProjectile();
    }

    private DeflectContext BuildDeflectContext(GameObject newOwner, Vector3 deflectOrigin)
    {
        var parryPerks = newOwner.GetComponent<ParryPerkController>();
        DeflectContext context = parryPerks != null ? parryPerks.BuildDeflectContext() : DeflectContext.Default(newOwner);
        Vector2 normal = ((Vector2)transform.position - (Vector2)deflectOrigin).normalized;
        if (normal.sqrMagnitude > 0.001f)
        {
            context.useSurfaceNormal = true;
            context.deflectSurfaceNormal = normal;
        }

        return context;
    }

    private Vector2 ResolveDeflectDirection(DeflectContext context)
    {
        if (context.useSurfaceNormal)
        {
            Vector2 incoming = rb != null && rb.linearVelocity.sqrMagnitude > 0.001f
                ? rb.linearVelocity.normalized
                : Vector2.zero;
            Vector2 surfaceNormal = context.deflectSurfaceNormal.normalized;
            if (incoming.sqrMagnitude > 0.001f && surfaceNormal.sqrMagnitude > 0.001f)
            {
                Vector2 reflected = Vector2.Reflect(incoming, surfaceNormal).normalized;
                if (reflected.sqrMagnitude > 0.001f)
                    return reflected;
            }
        }

        if (sourceOwner != null)
            return ((Vector2)sourceOwner.transform.position - (Vector2)transform.position).normalized;

        if (rb != null && rb.linearVelocity.sqrMagnitude > 0.001f)
            return -rb.linearVelocity.normalized;

        return Vector2.right;
    }

    private void ApplyVelocity(Vector2 velocity)
    {
        if (rb != null)
            rb.linearVelocity = velocity;

        if (velocity.sqrMagnitude > 0.001f)
        {
            float angle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }
    }

    private void TrySpawnSplitProjectiles()
    {
        if (!hasBeenDeflected || hasSpawnedSplits || splitCount <= 0)
            return;

        hasSpawnedSplits = true;

        Vector2 baseVelocity = rb != null && rb.linearVelocity.sqrMagnitude > 0.001f
            ? rb.linearVelocity
            : Vector2.right * speed;

        for (int i = 0; i < splitCount; i++)
        {
            float t = splitCount == 1 ? 0.5f : i / (float)(splitCount - 1);
            float angleOffset = Mathf.Lerp(-splitAngleSpread * 0.5f, splitAngleSpread * 0.5f, t);
            Vector2 splitVelocity = Quaternion.Euler(0f, 0f, angleOffset) * baseVelocity.normalized * (speed * splitSpeedMultiplier);

            GameObject cloneObj = Instantiate(gameObject, transform.position, Quaternion.identity);
            Projectile clone = cloneObj.GetComponent<Projectile>();
            if (clone == null)
                continue;

            clone.ConfigureSplitClone(owner, sourceOwner, damage * splitDamageMultiplier, splitVelocity, suppressDuration);
        }
    }

    private void ConfigureSplitClone(GameObject currentOwner, GameObject originalSourceOwner, float newDamage, Vector2 velocity, float splitSuppressDuration)
    {
        owner = currentOwner;
        sourceOwner = originalSourceOwner;
        damage = newDamage;
        speed = velocity.magnitude;
        remainingLife = Mathf.Max(1f, lifeTime * 0.5f);
        hasBeenDeflected = true;
        remainingPierceCount = 0;
        suppressDuration = splitSuppressDuration;
        splitCount = 0;
        hasSpawnedSplits = true;
        hitTargets.Clear();

        if (spriteRenderer != null)
            spriteRenderer.color = Color.yellow;

        ApplyVelocity(velocity);
    }

    private void DestroyProjectile()
    {
        Destroy(gameObject);
    }
}

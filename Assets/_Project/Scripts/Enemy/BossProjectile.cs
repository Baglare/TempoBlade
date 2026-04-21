using System.Collections.Generic;
using UnityEngine;

public class BossProjectile : MonoBehaviour, IDeflectable
{
    [Header("Projectile Settings")]
    public float speed = 5f;
    public float damage = 15f;
    public float lifeTime = 5f;

    [Header("Deflect Settings")]
    public bool isDeflectable = true;
    public Color normalColor = Color.cyan;
    public Color deflectedColor = Color.yellow;

    [HideInInspector] public GameObject owner;
    private GameObject sourceOwner;

    public GameObject ObjectOwner => owner;
    public GameObject SourceOwner => sourceOwner;
    public bool IsDeflected => hasBeenDeflected;
    public bool CanBeDeflected => true;

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private bool hasBeenDeflected;
    private float remainingLife;
    private int remainingPierceCount;
    private int splitCount;
    private float splitDamageMultiplier = 0.5f;
    private float splitAngleSpread = 20f;
    private float splitSpeedMultiplier = 1f;
    private bool hasSpawnedSplits;
    private readonly HashSet<int> hitTargets = new HashSet<int>();

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponentInChildren<SpriteRenderer>();
        remainingLife = lifeTime;

        if (sr != null)
            sr.color = normalColor;
    }

    private void Update()
    {
        remainingLife -= Time.deltaTime;
        if (remainingLife <= 0f)
            Destroy(gameObject);
    }

    public void Fire(Vector2 direction, GameObject creator = null)
    {
        owner = creator;
        if (sourceOwner == null)
            sourceOwner = creator;

        ApplyVelocity(direction.normalized * speed);
    }

    public void Deflect(DeflectContext context)
    {
        if (!isDeflectable || hasBeenDeflected)
            return;

        hasBeenDeflected = true;
        sourceOwner = owner != null ? owner : sourceOwner;
        owner = context.newOwner;

        remainingPierceCount = Mathf.Max(0, context.pierceCount);
        splitCount = Mathf.Max(0, context.splitCount);
        splitDamageMultiplier = Mathf.Max(0f, context.splitDamageMultiplier);
        splitAngleSpread = Mathf.Max(0f, context.splitAngleSpread);
        splitSpeedMultiplier = Mathf.Max(0.05f, context.splitSpeedMultiplier);

        damage *= Mathf.Max(0f, context.damageMultiplier);
        speed *= Mathf.Max(0.05f, context.speedMultiplier);
        remainingLife = Mathf.Max(remainingLife, lifeTime + 2f);
        hitTargets.Clear();

        Vector2 direction = ResolveDeflectDirection(context);
        ApplyVelocity(direction * speed);

        if (sr != null)
            sr.color = deflectedColor;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (owner != null && other.gameObject == owner)
            return;

        if (other.gameObject.layer == LayerMask.NameToLayer("Environment"))
        {
            Destroy(gameObject);
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
                Destroy(gameObject);
                return;
            }
        }

        int targetId = other.GetInstanceID();
        if (!hitTargets.Add(targetId))
            return;

        damageable.TakeDamage(damage);

        if (hasBeenDeflected && isHittingEnemy)
        {
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

        Destroy(gameObject);
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

        return Vector2.left;
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
            : Vector2.left * speed;

        for (int i = 0; i < splitCount; i++)
        {
            float t = splitCount == 1 ? 0.5f : i / (float)(splitCount - 1);
            float angleOffset = Mathf.Lerp(-splitAngleSpread * 0.5f, splitAngleSpread * 0.5f, t);
            Vector2 splitVelocity = Quaternion.Euler(0f, 0f, angleOffset) * baseVelocity.normalized * (speed * splitSpeedMultiplier);

            GameObject cloneObj = Instantiate(gameObject, transform.position, Quaternion.identity);
            BossProjectile clone = cloneObj.GetComponent<BossProjectile>();
            if (clone == null)
                continue;

            clone.ConfigureSplitClone(owner, sourceOwner, damage * splitDamageMultiplier, splitVelocity);
        }
    }

    private void ConfigureSplitClone(GameObject currentOwner, GameObject originalSourceOwner, float newDamage, Vector2 velocity)
    {
        owner = currentOwner;
        sourceOwner = originalSourceOwner;
        damage = newDamage;
        speed = velocity.magnitude;
        remainingLife = Mathf.Max(1f, lifeTime * 0.5f);
        hasBeenDeflected = true;
        remainingPierceCount = 0;
        splitCount = 0;
        hasSpawnedSplits = true;
        hitTargets.Clear();

        if (sr != null)
            sr.color = deflectedColor;

        ApplyVelocity(velocity);
    }
}

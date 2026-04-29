using UnityEngine;

[DisallowMultipleComponent]
public class ProjectileBurstOnImpact : MonoBehaviour
{
    private static int nextBurstFamilyId = 1;

    private enum BurstMode
    {
        Primary,
        Fragment
    }

    private Projectile projectile;
    private SpriteRenderer spriteRenderer;
    private bool hasTriggered;
    private bool canBeDeflected = true;
    private BurstMode burstMode = BurstMode.Primary;
    private float explosionRadius;
    private float explosionDamageMultiplier;
    private int fragmentCount;
    private float fragmentAngleSpread;
    private float fragmentSpeedMultiplier;
    private float fragmentLifetimeMultiplier;
    private float fragmentDamageMultiplier;
    private float fragmentExplosionRadius;
    private float fragmentExplosionDamageMultiplier;
    private Color burstCueColor = Color.white;
    private AudioEventId impactAudioEvent = AudioEventId.None;
    private const float FragmentSpawnOffsetMultiplier = 0.65f;
    private const float MaxVisualFragmentSpread = 56f;
    private int burstFamilyId;

    public bool CanBeDeflected => canBeDeflected;
    public bool CanApplyDirectHitTo(Collider2D target)
    {
        if (target == null)
            return true;

        if (burstFamilyId <= 0)
            return true;

        return BurstHitRegistry.CanApplyDirectHit(burstFamilyId, target);
    }

    public void RegisterDirectHit(Collider2D target)
    {
        if (target == null || burstFamilyId <= 0)
            return;

        BurstHitRegistry.TryRegisterBurstHit(burstFamilyId, target);
    }

    private void Awake()
    {
        projectile = GetComponent<Projectile>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
        if (spriteRenderer != null)
            WorldSortingUtility.ApplySorting(spriteRenderer, WorldSortingLayers.Projectiles, spriteRenderer.sortingOrder);
    }

    public void ConfigurePrimary(CasterBurstOrbSettings settings, AudioEventId audioEvent, Color fallbackCueColor)
    {
        if (settings == null)
            return;

        hasTriggered = false;
        burstMode = BurstMode.Primary;
        canBeDeflected = true;
        burstFamilyId = nextBurstFamilyId++;
        explosionRadius = Mathf.Max(0f, settings.impactRadius);
        explosionDamageMultiplier = Mathf.Max(0f, settings.impactDamageMultiplier);
        fragmentCount = Mathf.Max(0, settings.fragmentCount);
        fragmentAngleSpread = Mathf.Max(0f, settings.fragmentAngleSpread);
        fragmentSpeedMultiplier = Mathf.Max(0.05f, settings.fragmentSpeedMultiplier);
        fragmentLifetimeMultiplier = Mathf.Max(0.05f, settings.fragmentLifetimeMultiplier);
        fragmentDamageMultiplier = Mathf.Max(0f, settings.fragmentDamageMultiplier);
        fragmentExplosionRadius = Mathf.Max(0f, settings.fragmentExplosionRadius);
        fragmentExplosionDamageMultiplier = Mathf.Max(0f, settings.fragmentExplosionDamageMultiplier);
        burstCueColor = settings.burstCueColor.a > 0f ? settings.burstCueColor : fallbackCueColor;
        impactAudioEvent = audioEvent;

        ApplyVisualTint();
    }

    public void ConfigureFragment(CasterBurstOrbSettings settings, AudioEventId audioEvent, Color fallbackCueColor)
    {
        if (settings == null)
            return;

        hasTriggered = false;
        burstMode = BurstMode.Fragment;
        canBeDeflected = false;
        explosionRadius = Mathf.Max(0f, settings.fragmentExplosionRadius);
        explosionDamageMultiplier = Mathf.Max(0f, settings.fragmentExplosionDamageMultiplier);
        fragmentCount = 0;
        fragmentAngleSpread = 0f;
        fragmentSpeedMultiplier = 1f;
        fragmentLifetimeMultiplier = 1f;
        fragmentDamageMultiplier = 0f;
        fragmentExplosionRadius = 0f;
        fragmentExplosionDamageMultiplier = 0f;
        burstCueColor = settings.burstCueColor.a > 0f ? settings.burstCueColor : fallbackCueColor;
        impactAudioEvent = audioEvent;

        transform.localScale *= 0.7f;
        ApplyVisualTint();
    }

    public bool HandleImpact(Collider2D directHit)
    {
        if (hasTriggered || projectile == null)
            return false;

        hasTriggered = true;

        SpawnCue();
        ApplyExplosionDamage(directHit);

        if (burstMode == BurstMode.Primary && fragmentCount > 0)
            SpawnFragments(directHit);

        return true;
    }

    private void SpawnCue()
    {
        SupportPulseVisualUtility.SpawnPulse(transform.position, 0.15f, Mathf.Max(0.45f, explosionRadius), 0.18f, burstCueColor, 26, 0.07f);
        if (burstMode == BurstMode.Primary && impactAudioEvent != AudioEventId.None)
            AudioManager.Play(impactAudioEvent, gameObject, transform.position);
    }

    private void ApplyExplosionDamage(Collider2D directHit)
    {
        if (explosionRadius <= 0f || explosionDamageMultiplier <= 0f)
            return;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius);
        float explosionDamage = projectile.damage * explosionDamageMultiplier;
        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];
            if (hit == null || hit == directHit)
                continue;

            if (!ShouldDamageTarget(hit))
                continue;

            if (!BurstHitRegistry.TryRegisterBurstHit(burstFamilyId, hit))
                continue;

            IDamageable damageable = hit.GetComponent<IDamageable>();
            if (damageable == null)
                continue;

            if (hit.CompareTag("Player"))
            {
                PlayerController playerController = hit.GetComponent<PlayerController>();
                if (playerController != null && playerController.IsInvulnerable)
                    continue;
            }

            EnemyBase enemy = hit.GetComponent<EnemyBase>();
            if (enemy != null && projectile.IsDeflected)
            {
                EnemyDamageUtility.ApplyDamage(
                    enemy,
                    explosionDamage,
                    EnemyDamageSource.ProjectileDeflect,
                    projectile.ObjectOwner,
                    projectile.CurrentVelocity.sqrMagnitude > 0.001f ? projectile.CurrentVelocity.normalized : Vector2.zero,
                    0.6f,
                    isParryCounter: true,
                    isPerfectTiming: true);
            }
            else
            {
                damageable.TakeDamage(explosionDamage);
            }
        }
    }

    private bool ShouldDamageTarget(Collider2D target)
    {
        if (target == null)
            return false;

        bool damagesEnemies = projectile.IsDeflected || (projectile.ObjectOwner != null && projectile.ObjectOwner.CompareTag("Player"));
        if (damagesEnemies)
            return target.CompareTag("Enemy");

        return target.CompareTag("Player");
    }

    private void SpawnFragments(Collider2D directHit)
    {
        Vector2 baseVelocity = projectile.CurrentVelocity.sqrMagnitude > 0.001f
            ? projectile.CurrentVelocity
            : Vector2.right * projectile.speed;
        Vector2 scatterBaseDirection = ResolveScatterBaseDirection(directHit, baseVelocity.normalized);
        float visualSpread = Mathf.Min(fragmentAngleSpread, MaxVisualFragmentSpread);
        Vector2 tangent = new Vector2(-scatterBaseDirection.y, scatterBaseDirection.x);
        float lateralSpan = Mathf.Max(0.18f, fragmentExplosionRadius * 0.9f);

        for (int i = 0; i < fragmentCount; i++)
        {
            float t = fragmentCount == 1 ? 0.5f : i / (float)(fragmentCount - 1);
            float angleOffset = Mathf.Lerp(-visualSpread * 0.5f, visualSpread * 0.5f, t);
            Vector2 velocity = Quaternion.Euler(0f, 0f, angleOffset) * scatterBaseDirection * (projectile.speed * fragmentSpeedMultiplier);
            float lateralOffset = Mathf.Lerp(-lateralSpan, lateralSpan, t);
            Vector3 spawnPosition = ResolveFragmentSpawnPosition(directHit, scatterBaseDirection, tangent, lateralOffset);

            GameObject cloneObj = Instantiate(gameObject, spawnPosition, Quaternion.identity);
            Projectile cloneProjectile = cloneObj.GetComponent<Projectile>();
            ProjectileBurstOnImpact cloneBurst = cloneObj.GetComponent<ProjectileBurstOnImpact>();
            if (cloneProjectile == null || cloneBurst == null)
                continue;

            cloneBurst.ConfigureFragment(
                new CasterBurstOrbSettings
                {
                    fragmentExplosionRadius = fragmentExplosionRadius,
                    fragmentExplosionDamageMultiplier = fragmentExplosionDamageMultiplier,
                    burstCueColor = burstCueColor
                },
                impactAudioEvent,
                burstCueColor);
            cloneBurst.SetBurstFamilyId(burstFamilyId);

            cloneProjectile.ConfigureSpawnedProjectile(
                projectile.ObjectOwner,
                projectile.SourceOwner,
                projectile.damage * fragmentDamageMultiplier,
                velocity,
                Mathf.Max(0.15f, projectile.lifeTime * fragmentLifetimeMultiplier),
                projectile.IsDeflected);
        }
    }

    private Vector3 ResolveFragmentSpawnPosition(Collider2D directHit, Vector2 scatterBaseDirection, Vector2 tangent, float lateralOffset)
    {
        float spawnOffset = Mathf.Max(0.28f, fragmentExplosionRadius * FragmentSpawnOffsetMultiplier);
        if (directHit == null || scatterBaseDirection.sqrMagnitude <= 0.001f)
            return transform.position + (Vector3)((scatterBaseDirection.normalized * spawnOffset) + (tangent.normalized * lateralOffset));

        float surfaceQueryDistance = Mathf.Max(directHit.bounds.extents.magnitude + spawnOffset, spawnOffset * 2f);
        Vector2 queryPoint = (Vector2)transform.position - (scatterBaseDirection.normalized * surfaceQueryDistance);
        Vector2 surfacePoint = directHit.ClosestPoint(queryPoint);
        return surfacePoint - (scatterBaseDirection.normalized * spawnOffset) + (tangent.normalized * lateralOffset);
    }

    private Vector2 ResolveScatterBaseDirection(Collider2D directHit, Vector2 incomingDirection)
    {
        if (directHit != null && projectile != null && projectile.ObjectOwner != null)
        {
            Vector2 ownerToTarget = ((Vector2)directHit.bounds.center - (Vector2)projectile.ObjectOwner.transform.position).normalized;
            if (ownerToTarget.sqrMagnitude > 0.001f)
                return ownerToTarget;
        }

        if (incomingDirection.sqrMagnitude > 0.001f)
            return incomingDirection.normalized;

        return Vector2.right;
    }

    private void ApplyVisualTint()
    {
        if (spriteRenderer != null)
            spriteRenderer.color = burstCueColor;
    }

    public void SetBurstFamilyId(int familyId)
    {
        burstFamilyId = familyId;
    }
}

internal static class BurstHitRegistry
{
    private struct BurstHitKey
    {
        public int familyId;
        public int targetId;
    }

    private static readonly System.Collections.Generic.Dictionary<BurstHitKey, float> activeHits = new System.Collections.Generic.Dictionary<BurstHitKey, float>();
    private const float RetentionSeconds = 1.5f;

    public static bool TryRegisterBurstHit(int familyId, Collider2D target)
    {
        if (familyId <= 0 || target == null)
            return true;

        CleanupExpired();

        BurstHitKey key = new BurstHitKey
        {
            familyId = familyId,
            targetId = target.GetInstanceID()
        };

        if (activeHits.ContainsKey(key))
            return false;

        activeHits[key] = Time.time + RetentionSeconds;
        return true;
    }

    public static bool CanApplyDirectHit(int familyId, Collider2D target)
    {
        if (familyId <= 0 || target == null)
            return true;

        CleanupExpired();

        BurstHitKey key = new BurstHitKey
        {
            familyId = familyId,
            targetId = target.GetInstanceID()
        };

        return !activeHits.ContainsKey(key);
    }

    private static void CleanupExpired()
    {
        if (activeHits.Count == 0)
            return;

        var expired = new System.Collections.Generic.List<BurstHitKey>();
        foreach (var pair in activeHits)
        {
            if (pair.Value <= Time.time)
                expired.Add(pair.Key);
        }

        for (int i = 0; i < expired.Count; i++)
            activeHits.Remove(expired[i]);
    }
}

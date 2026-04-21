using UnityEngine;

public class UnstableCoreObject : MonoBehaviour, IDamageable
{
    private EliteKamikazeUnstableCoreSettings settings;
    private float baseDamage;
    private float expireTime;
    private bool brokenEarly;
    private bool exploded;
    private SpriteRenderer spriteRenderer;
    private EnemyOverheadMeter overheadMeter;

    public void Configure(EliteKamikazeUnstableCoreSettings coreSettings, float sourceDamage)
    {
        settings = coreSettings;
        baseDamage = sourceDamage;
        expireTime = Time.time + settings.coreLifetime;
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        overheadMeter = GetComponent<EnemyOverheadMeter>();
        if (overheadMeter == null)
            overheadMeter = gameObject.AddComponent<EnemyOverheadMeter>();
        overheadMeter.Configure(settings.coreColor, 0.9f, 0.07f);
        overheadMeter.SetVisible(true);
    }

    private void Update()
    {
        if (exploded || settings == null)
            return;

        float progress = 1f - Mathf.Clamp01((expireTime - Time.time) / Mathf.Max(0.01f, settings.coreLifetime));
        overheadMeter.SetProgress(progress);
        if (spriteRenderer != null)
            spriteRenderer.color = Color.Lerp(settings.coreColor * 0.5f, settings.coreColor, progress);

        if (Time.time >= expireTime)
            Explode(false);
    }

    public void TakeDamage(float amount)
    {
        if (exploded || brokenEarly)
            return;

        brokenEarly = true;
        Invoke(nameof(ExplodeFromBreak), 0.1f);
    }

    public void Stun(float duration) { }

    private void ExplodeFromBreak()
    {
        Explode(true);
    }

    private void Explode(bool weak)
    {
        if (exploded)
            return;

        exploded = true;
        float radius = weak ? settings.brokenExplosionRadius : settings.fullExplosionRadius;
        float damage = baseDamage * (weak ? settings.brokenExplosionDamageMultiplier : settings.fullExplosionDamageMultiplier);
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius);
        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];
            if (!hit.CompareTag("Player"))
                continue;

            PlayerController playerController = hit.GetComponent<PlayerController>();
            if (playerController != null && playerController.IsInvulnerable)
                continue;

            hit.GetComponent<IDamageable>()?.TakeDamage(damage);
        }

        SupportPulseVisualUtility.SpawnPulse(transform.position, 0.2f, radius, 0.28f, settings.coreColor);
        Destroy(gameObject);
    }
}

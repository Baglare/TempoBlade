using UnityEngine;

public class UnstableCoreObject : MonoBehaviour, IDamageable
{
    private static Sprite defaultCoreSprite;
    private EliteKamikazeUnstableCoreSettings settings;
    private float baseDamage;
    private float expireTime;
    private bool brokenEarly;
    private bool exploded;
    private float brokenProgress;
    private SpriteRenderer spriteRenderer;
    private EnemyOverheadMeter overheadMeter;
    private LineRenderer ringRenderer;
    private CircleCollider2D hitCollider;
    private const int RingSegments = 28;

    public void Configure(EliteKamikazeUnstableCoreSettings coreSettings, float sourceDamage)
    {
        settings = coreSettings;
        baseDamage = sourceDamage;
        expireTime = Time.time + settings.coreLifetime;
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (enemyLayer >= 0)
            gameObject.layer = enemyLayer;
        gameObject.tag = "Enemy";
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        if (defaultCoreSprite == null)
            defaultCoreSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        spriteRenderer.sprite = defaultCoreSprite;
        spriteRenderer.drawMode = SpriteDrawMode.Simple;
        spriteRenderer.sortingOrder = 20;
        overheadMeter = GetComponent<EnemyOverheadMeter>();
        if (overheadMeter == null)
            overheadMeter = gameObject.AddComponent<EnemyOverheadMeter>();
        hitCollider = GetComponent<CircleCollider2D>();
        if (hitCollider == null)
            hitCollider = gameObject.AddComponent<CircleCollider2D>();
        hitCollider.radius = Mathf.Max(0.4f, settings.brokenExplosionRadius * 0.33f);
        hitCollider.isTrigger = false;
        ringRenderer = GetComponent<LineRenderer>();
        if (ringRenderer == null)
            ringRenderer = gameObject.AddComponent<LineRenderer>();
        ConfigureRing();
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
        {
            spriteRenderer.color = Color.Lerp(settings.coreColor * 0.5f, settings.coreColor, progress);
            spriteRenderer.transform.localScale = Vector3.one * Mathf.Lerp(0.55f, 0.8f, progress);
        }
        UpdateRing(progress);

        if (Time.time >= expireTime)
            Explode(false);
    }

    public void TakeDamage(float amount)
    {
        if (exploded || brokenEarly)
            return;

        brokenEarly = true;
        brokenProgress = 1f - Mathf.Clamp01((expireTime - Time.time) / Mathf.Max(0.01f, settings.coreLifetime));
        hitCollider.enabled = false;
        Invoke(nameof(ExplodeFromBreak), Mathf.Max(0.2f, settings.earlyBreakDetonationDelay));
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
        float radius;
        float damage;
        if (weak)
        {
            float t = Mathf.Clamp01(brokenProgress);
            radius = Mathf.Lerp(settings.brokenExplosionRadius, settings.fullExplosionRadius, t);
            damage = baseDamage * Mathf.Lerp(settings.brokenExplosionDamageMultiplier, settings.fullExplosionDamageMultiplier, t);
        }
        else
        {
            radius = settings.fullExplosionRadius;
            damage = baseDamage * settings.fullExplosionDamageMultiplier;
        }
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

        float pulseThickness = weak ? Mathf.Lerp(0.16f, 0.24f, Mathf.Clamp01(brokenProgress)) : 0.28f;
        float pulseDuration = weak ? Mathf.Lerp(0.18f, 0.26f, Mathf.Clamp01(brokenProgress)) : 0.34f;
        SupportPulseVisualUtility.SpawnPulse(transform.position, pulseThickness, radius, pulseDuration, settings.coreColor);
        Destroy(gameObject);
    }

    private void ConfigureRing()
    {
        ringRenderer.useWorldSpace = false;
        ringRenderer.loop = true;
        ringRenderer.positionCount = RingSegments;
        ringRenderer.widthMultiplier = 0.08f;
        ringRenderer.startColor = settings.coreColor;
        ringRenderer.endColor = settings.coreColor;
        Shader shader = Shader.Find("Sprites/Default");
        if (shader != null)
            ringRenderer.material = new Material(shader);
    }

    private void UpdateRing(float progress)
    {
        if (ringRenderer == null || settings == null)
            return;

        float radius = Mathf.Lerp(settings.brokenExplosionRadius * 0.18f, settings.brokenExplosionRadius * 0.42f, progress);
        for (int i = 0; i < RingSegments; i++)
        {
            float t = (i / (float)RingSegments) * Mathf.PI * 2f;
            ringRenderer.SetPosition(i, new Vector3(Mathf.Cos(t) * radius, Mathf.Sin(t) * radius, 0f));
        }

        Color color = Color.Lerp(settings.coreColor * 0.45f, settings.coreColor, progress);
        ringRenderer.startColor = color;
        ringRenderer.endColor = color;
        ringRenderer.transform.localScale = Vector3.one * Mathf.Lerp(0.9f, 1.1f, progress);
    }
}

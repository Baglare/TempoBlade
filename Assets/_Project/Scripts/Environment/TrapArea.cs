using UnityEngine;
using System.Collections;

[System.Serializable]
public class TrapRuntimeSettings
{
    public float activationDelayMultiplier = 1f;
    public float speedMultiplierScale = 1f;
    public float slowDurationMultiplier = 1f;
    public float poisonDamageMultiplier = 1f;
    public float indicatorWidthMultiplier = 1f;
    public Color indicatorColor = new Color(1f, 0f, 0f, 0.5f);
}

public class TrapArea : MonoBehaviour
{
    [Header("Trap Settings")]
    public float triggerRadius = 1.5f;
    public float explosionRadius = 1.8f;
    public float activationDelay = 0.3f;

    [Header("Debuff Settings (Patlama TuttuÄŸunda)")]
    [Range(0.1f, 1.0f)]
    public float speedMultiplier = 0.5f;
    public float slowDuration = 3f;
    public float poisonDamagePerTick = 5f;
    public int poisonTicks = 3;
    public float timeBetweenTicks = 1f;

    [Header("Dodge Koruma AyarÄ±")]
    public float dodgeDuration = 0.22f;

    private bool isTriggered;
    private CircleCollider2D triggerCol;
    private LineRenderer lr;
    private TrapRuntimeSettings runtimeSettings;

    private void Start()
    {
        triggerCol = GetComponent<CircleCollider2D>();
        if (triggerCol == null)
            triggerCol = gameObject.AddComponent<CircleCollider2D>();

        triggerCol.isTrigger = true;
        triggerCol.radius = triggerRadius;

        CreateExplosionIndicator();
    }

    private void CreateExplosionIndicator()
    {
        lr = gameObject.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.loop = true;
        lr.positionCount = 100;

        float widthMultiplier = runtimeSettings != null ? Mathf.Max(0.5f, runtimeSettings.indicatorWidthMultiplier) : 1f;
        lr.startWidth = 0.05f * widthMultiplier;
        lr.endWidth = 0.05f * widthMultiplier;

        Color indicatorColor = runtimeSettings != null ? runtimeSettings.indicatorColor : new Color(1f, 0f, 0f, 0.5f);
        lr.startColor = indicatorColor;
        lr.endColor = indicatorColor;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.sortingLayerName = WorldSortingUtility.ResolveLayerName(WorldSortingLayers.GroundVFX);
        lr.sortingOrder = -1;

        DrawCircle(triggerRadius);
    }

    private void DrawCircle(float radius)
    {
        if (lr == null)
            return;

        float angleStep = 360f / lr.positionCount;
        for (int i = 0; i < lr.positionCount; i++)
        {
            float rad = Mathf.Deg2Rad * (i * angleStep);
            float x = Mathf.Cos(rad) * radius;
            float y = Mathf.Sin(rad) * radius;
            lr.SetPosition(i, new Vector3(x, y, 0));
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isTriggered)
            return;

        if (collision.CompareTag("Player"))
            StartCoroutine(ActivationRoutine());
    }

    private IEnumerator ActivationRoutine()
    {
        isTriggered = true;

        SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
            sr.color = Color.red;

        float actualActivationDelay = activationDelay * (runtimeSettings != null ? runtimeSettings.activationDelayMultiplier : 1f);
        actualActivationDelay = Mathf.Max(0.05f, actualActivationDelay);

        float timer = 0f;
        while (timer < actualActivationDelay)
        {
            timer += Time.deltaTime;
            float currentRad = Mathf.Lerp(0f, explosionRadius, timer / actualActivationDelay);
            DrawCircle(currentRad);
            yield return null;
        }

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius);
        bool hitPlayer = false;
        PlayerController player = null;
        IDamageable playerDmg = null;

        foreach (var hit in hits)
        {
            if (!hit.CompareTag("Player"))
                continue;

            player = hit.GetComponent<PlayerController>();
            if (player == null)
                continue;

            float timeSinceDodge = player.GetTimeSinceDodgeStart();
            bool dodgeProtected = player.IsInvulnerable || timeSinceDodge <= (actualActivationDelay + dodgeDuration);
            if (dodgeProtected)
                continue;

            hitPlayer = true;
            playerDmg = hit.GetComponent<IDamageable>();
            break;
        }

        if (sr != null)
            sr.enabled = false;
        if (triggerCol != null)
            triggerCol.enabled = false;

        if (lr != null)
        {
            float shrinkTimer = 0f;
            float shrinkDuration = 0.15f;
            while (shrinkTimer < shrinkDuration)
            {
                shrinkTimer += Time.deltaTime;
                float shrinkRadius = Mathf.Lerp(explosionRadius, 0f, shrinkTimer / shrinkDuration);
                DrawCircle(shrinkRadius);
                yield return null;
            }

            lr.enabled = false;
        }

        if (hitPlayer && player != null)
            StartCoroutine(DebuffRoutine(player, playerDmg));
        else
            Destroy(gameObject);
    }

    private IEnumerator DebuffRoutine(PlayerController player, IDamageable playerDamageable)
    {
        float appliedSpeedMultiplier = speedMultiplier * (runtimeSettings != null ? runtimeSettings.speedMultiplierScale : 1f);
        float appliedSlowDuration = slowDuration * (runtimeSettings != null ? runtimeSettings.slowDurationMultiplier : 1f);
        float appliedPoisonDamage = poisonDamagePerTick * (runtimeSettings != null ? runtimeSettings.poisonDamageMultiplier : 1f);

        if (player != null)
            player.speedMultiplier *= appliedSpeedMultiplier;

        float timer = 0f;
        int ticksDone = 0;
        float nextTickTime = 0f;

        while (timer < appliedSlowDuration || ticksDone < poisonTicks)
        {
            if (ticksDone < poisonTicks && timer >= nextTickTime)
            {
                if (playerDamageable != null)
                    playerDamageable.TakeDamage(appliedPoisonDamage);
                ticksDone++;
                nextTickTime += timeBetweenTicks;
            }

            timer += Time.deltaTime;
            yield return null;
        }

        if (player != null)
            player.speedMultiplier /= appliedSpeedMultiplier;

        Destroy(gameObject);
    }

    public void ApplyRuntimeSettings(TrapRuntimeSettings settings)
    {
        runtimeSettings = settings;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, triggerRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}

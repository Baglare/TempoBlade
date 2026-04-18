using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyResonator : EnemyBase
{
    [Header("Positioning")]
    public float supportSearchRadius = 10f;
    public float desiredSupportDistance = 2.5f;
    public float retreatDistance = 3f;
    public float repositionMoveSpeed = 2.8f;
    public float anchorRefreshInterval = 0.35f;

    [Header("Rally Pulse")]
    public float rallyPulseRadius = 4.5f;
    public float rallyPulseCooldown = 6f;
    public float rallyPulseWindup = 0.45f;
    public float rallyBuffDuration = 4f;
    public float rallyMoveSpeedMultiplier = 1.22f;
    public float rallyAttackSpeedMultiplier = 1.18f;
    public bool rallyIgnoreNextLightStagger = true;
    public Color rallyPulseColor = new Color(0.2f, 1f, 1f, 0.95f);

    [Header("Tempo Static")]
    public float tempoStaticCooldown = 7.5f;
    public float tempoStaticWindup = 0.35f;
    public float tempoStaticDuration = 4f;
    public float tempoStaticRadius = 2.6f;
    public float tempoStaticGainMultiplier = 0.72f;
    public float tempoStaticDecayMultiplier = 1.35f;
    public Color tempoStaticColor = new Color(0.3f, 0.95f, 1f, 0.85f);

    [Header("Sound Burst")]
    public float soundBurstRange = 1.8f;
    public float soundBurstCooldown = 3f;
    public float soundBurstDamage = 6f;
    public float soundBurstKnockback = 9f;
    public float soundBurstStagger = 0.22f;
    public Color soundBurstColor = new Color(1f, 0.55f, 0.2f, 0.9f);

    [Header("Animation")]
    [SerializeField] private float deathAnimDuration = 0.6f;

    private readonly List<EnemyBase> nearbyAllies = new List<EnemyBase>();

    private Transform playerTransform;
    private PlayerController playerController;
    private PlayerCombat playerCombat;
    private Animator animator;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;

    private float nextAnchorRefreshTime;
    private float nextRallyPulseTime;
    private float nextTempoStaticTime;
    private float nextSoundBurstTime;
    private Vector2 currentAnchor;
    private bool isDead;
    private bool isCasting;

    protected override void Start()
    {
        base.Start();
        deathDelay = deathAnimDuration;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
            playerController = player.GetComponent<PlayerController>();
            playerCombat = player.GetComponent<PlayerCombat>();
        }

        animator = GetComponentInChildren<Animator>();
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        currentAnchor = transform.position;
    }

    private void Update()
    {
        if (isDead || isStunned || playerTransform == null)
            return;

        if (Time.time >= nextAnchorRefreshTime)
        {
            currentAnchor = EnemySupportUtility.FindBestRallyAnchor(transform, supportSearchRadius, rallyPulseRadius, this, out _);
            nextAnchorRefreshTime = Time.time + anchorRefreshInterval;
        }

        if (!isCasting && Time.time >= nextSoundBurstTime && Vector2.Distance(transform.position, playerTransform.position) <= soundBurstRange)
        {
            StartCoroutine(SoundBurstRoutine());
            return;
        }

        if (!isCasting && Time.time >= nextRallyPulseTime)
        {
            StartCoroutine(RallyPulseRoutine());
            return;
        }

        if (!isCasting && Time.time >= nextTempoStaticTime)
        {
            StartCoroutine(TempoStaticRoutine());
            return;
        }

        UpdateMovement();
    }

    public override void TakeDamage(float amount)
    {
        if (isDead)
            return;

        if (animator != null)
            animator.SetTrigger("TakeHit");

        base.TakeDamage(amount);
    }

    public override void Stun(float duration)
    {
        if (isDead)
            return;

        StopAllCoroutines();
        isCasting = false;

        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        if (spriteRenderer != null)
            spriteRenderer.color = Color.white;

        base.Stun(duration);
    }

    protected override void OnDeathAnimationStart()
    {
        isDead = true;
        StopAllCoroutines();

        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
            col.enabled = false;

        if (animator != null)
            animator.SetTrigger("Die");
    }

    private void UpdateMovement()
    {
        Vector2 targetPosition = currentAnchor;
        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);
        if (distanceToPlayer <= retreatDistance)
        {
            Vector2 away = ((Vector2)transform.position - (Vector2)playerTransform.position).normalized;
            targetPosition = (Vector2)transform.position + away * desiredSupportDistance;
        }

        Vector2 toTarget = targetPosition - (Vector2)transform.position;
        bool isMoving = toTarget.magnitude > 0.2f;
        if (rb != null)
        {
            rb.linearVelocity = isMoving
                ? toTarget.normalized * repositionMoveSpeed * GetSupportMoveSpeedMultiplier()
                : Vector2.zero;
        }

        if (spriteRenderer != null)
            spriteRenderer.flipX = playerTransform.position.x < transform.position.x;

        if (animator != null)
            animator.SetBool("IsMoving", isMoving);
    }

    private IEnumerator RallyPulseRoutine()
    {
        isCasting = true;
        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        if (spriteRenderer != null)
            spriteRenderer.color = rallyPulseColor;

        yield return new WaitForSeconds(rallyPulseWindup);

        EnemySupportUtility.GatherNearbyAllies(transform, rallyPulseRadius, nearbyAllies, this);
        for (int i = 0; i < nearbyAllies.Count; i++)
        {
            EnemyBase ally = nearbyAllies[i];
            ally?.GetSupportBuffReceiver()?.ApplyRallyBuff(
                rallyBuffDuration,
                rallyMoveSpeedMultiplier,
                rallyAttackSpeedMultiplier,
                rallyIgnoreNextLightStagger);
        }

        SupportPulseVisualUtility.SpawnPulse(transform.position, 0.25f, rallyPulseRadius, 0.32f, rallyPulseColor);
        if (DamagePopupManager.Instance != null)
            DamagePopupManager.Instance.CreateText(transform.position + Vector3.up * 1.8f, "RALLY!", rallyPulseColor, 6f);

        if (spriteRenderer != null)
            spriteRenderer.color = Color.white;

        nextRallyPulseTime = Time.time + rallyPulseCooldown;
        isCasting = false;
    }

    private IEnumerator TempoStaticRoutine()
    {
        isCasting = true;
        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        if (spriteRenderer != null)
            spriteRenderer.color = tempoStaticColor;

        yield return new WaitForSeconds(tempoStaticWindup);

        Vector3 zonePosition = playerTransform.position;
        GameObject zoneObject = new GameObject("TempoStaticZone");
        zoneObject.transform.position = zonePosition;
        TempoStaticZone zone = zoneObject.AddComponent<TempoStaticZone>();
        zone.Configure(tempoStaticDuration, tempoStaticRadius, tempoStaticGainMultiplier, tempoStaticDecayMultiplier);
        SupportPulseVisualUtility.SpawnPulse(zonePosition, 0.25f, tempoStaticRadius, 0.25f, tempoStaticColor);

        if (DamagePopupManager.Instance != null)
            DamagePopupManager.Instance.CreateText(zonePosition + Vector3.up, "STATIC", tempoStaticColor, 5.5f);

        if (spriteRenderer != null)
            spriteRenderer.color = Color.white;

        nextTempoStaticTime = Time.time + tempoStaticCooldown;
        isCasting = false;
    }

    private IEnumerator SoundBurstRoutine()
    {
        isCasting = true;
        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        yield return new WaitForSeconds(0.12f);

        Vector2 toPlayer = (playerTransform.position - transform.position);
        if (toPlayer.magnitude <= soundBurstRange)
        {
            if (playerCombat != null)
                playerCombat.TakeDamage(soundBurstDamage);

            if (playerController != null)
                playerController.ApplyExternalStagger(soundBurstStagger, toPlayer.normalized * soundBurstKnockback);
        }

        SupportPulseVisualUtility.SpawnPulse(transform.position, 0.2f, soundBurstRange, 0.18f, soundBurstColor);
        if (DamagePopupManager.Instance != null)
            DamagePopupManager.Instance.CreateText(transform.position + Vector3.up * 1.4f, "BURST!", soundBurstColor, 5.5f);

        nextSoundBurstTime = Time.time + soundBurstCooldown;
        isCasting = false;
    }
}

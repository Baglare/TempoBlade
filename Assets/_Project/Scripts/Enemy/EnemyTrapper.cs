using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Haritada hizlica rastgele noktalara hereket eden ve tuzak birakan dusman.
/// Tempo arttikca tuzak yerlestirmesi daha akilli hale gelir.
/// </summary>
public class EnemyTrapper : EnemyBase
{
    [System.Serializable]
    public class TrapperTempoConfig
    {
        public TempoTierFloatValue trapSpawnIntervalMultiplier = new TempoTierFloatValue { t0 = 1f, t1 = 0.9f, t2 = 0.9f, t3 = 0.8f };
        public float t1CentralBiasDistance = 1.75f;
        public int t2PlacementSamples = 10;
        public float t2ForwardBias = 4f;
        public float t3SelfRootDuration = 0.3f;
        public TrapRuntimeSettings t3TrapOverrides = new TrapRuntimeSettings
        {
            activationDelayMultiplier = 0.7f,
            speedMultiplierScale = 0.85f,
            slowDurationMultiplier = 1.15f,
            poisonDamageMultiplier = 1.2f,
            indicatorWidthMultiplier = 1.45f,
            indicatorColor = new Color(1f, 0.45f, 0.1f, 0.85f)
        };
    }

    [Header("Trapper - Random Roam")]
    public float moveSpeedModifier = 5f;
    public float roamRadius = 10f;
    private Vector2 targetRoamPos;

    [Header("Trap Spawn")]
    public GameObject trapPrefab;
    public float trapSpawnInterval = 8f;
    public int maxTraps = 5;
    [Tooltip("Mayin cakisma engeli: Yakin mayina bu mesafeden daha yakina kuramazsin")]
    public float minTrapDistance = 2f;

    [Header("Tempo")]
    public TrapperTempoConfig tempoConfig = new TrapperTempoConfig();

    private float nextTrapTime;
    private bool isDead;
    private bool isPlacingTrap;
    private readonly List<TrapArea> activeTraps = new List<TrapArea>();

    private float stuckTimer;
    private Vector2 lastPosition;

    private Transform playerTransform;
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    protected override void Start()
    {
        base.Start();

        playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
        animator = GetComponentInChildren<Animator>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        PickNewRoamPosition();
        lastPosition = transform.position;
        nextTrapTime = Time.time + 2f;
        deathDelay = 1.0f;
    }

    private void Update()
    {
        if (isDead || isStunned)
            return;

        if (!isPlacingTrap)
            UpdateMovement();

        activeTraps.RemoveAll(t => t == null);
        if (!isPlacingTrap && Time.time >= nextTrapTime)
        {
            if (activeTraps.Count < maxTraps)
                StartCoroutine(SpawnTrapRoutine());

            float nextInterval = GetEffectiveCooldownDuration(trapSpawnInterval * tempoConfig.trapSpawnIntervalMultiplier.Evaluate(CurrentTempoTier));
            nextTrapTime = Time.time + Mathf.Max(0.5f, nextInterval);
        }
    }

    public override void TakeDamage(float amount)
    {
        if (isDead)
            return;

        base.TakeDamage(amount);
        if (!isDead && animator != null)
            animator.SetTrigger("TakeHit");
    }

    protected override void OnDeathAnimationStart()
    {
        isDead = true;

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
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
        float speed = GetEffectiveMoveSpeedFromData(moveSpeedModifier);
        float dist = Vector2.Distance(transform.position, targetRoamPos);
        if (dist < 0.5f)
        {
            PickNewRoamPosition();
        }
        else
        {
            transform.position = Vector2.MoveTowards(transform.position, targetRoamPos, speed * Time.deltaTime);

            if (spriteRenderer != null)
                spriteRenderer.flipX = targetRoamPos.x < transform.position.x;

            if (animator != null)
                animator.SetBool("IsMoving", true);
        }

        float movedDist = Vector2.Distance(transform.position, lastPosition);
        if (movedDist < 0.02f)
        {
            stuckTimer += Time.deltaTime;
            if (stuckTimer > 0.4f)
            {
                PickNewRoamPosition();
                stuckTimer = 0f;
            }
        }
        else
        {
            stuckTimer = 0f;
        }

        lastPosition = transform.position;
    }

    private IEnumerator SpawnTrapRoutine()
    {
        Vector2 spawnPosition = ChooseTrapSpawnPosition();
        if (IsTrapTooCloseToExisting(spawnPosition))
        {
            nextTrapTime = Time.time + GetEffectiveCooldownDuration(1f);
            yield break;
        }

        isPlacingTrap = true;
        if (animator != null)
            animator.SetTrigger("Attack");
        if (animator != null)
            animator.SetBool("IsMoving", false);

        if (CurrentTempoTier == TempoManager.TempoTier.T3)
            yield return new WaitForSeconds(tempoConfig.t3SelfRootDuration);

        GameObject spawned = Instantiate(trapPrefab, spawnPosition, Quaternion.identity);
        TrapArea trap = spawned.GetComponent<TrapArea>();
        if (trap != null)
        {
            if (CurrentTempoTier == TempoManager.TempoTier.T3)
                trap.ApplyRuntimeSettings(tempoConfig.t3TrapOverrides);

            activeTraps.Add(trap);
        }

        isPlacingTrap = false;
    }

    private void PickNewRoamPosition()
    {
        int maxAttempts = 15;
        Collider2D myCol = GetComponent<Collider2D>();

        for (int i = 0; i < maxAttempts; i++)
        {
            Vector2 randomDir = Random.insideUnitCircle.normalized;
            float randomDist = Random.Range(2f, roamRadius);
            Vector2 candidate = (Vector2)transform.position + randomDir * randomDist;

            bool wasEnabled = myCol != null && myCol.enabled;
            if (myCol != null)
                myCol.enabled = false;

            RaycastHit2D hit = Physics2D.Raycast(transform.position, randomDir, randomDist);

            if (myCol != null)
                myCol.enabled = wasEnabled;

            if (hit.collider == null || hit.collider.isTrigger)
            {
                targetRoamPos = candidate;
                return;
            }
        }

        targetRoamPos = (Vector2)transform.position + Random.insideUnitCircle.normalized * 2f;
    }

    private Vector2 ChooseTrapSpawnPosition()
    {
        if (playerTransform == null)
            return transform.position;

        if (CurrentTempoTier == TempoManager.TempoTier.T0)
            return transform.position;

        if (CurrentTempoTier == TempoManager.TempoTier.T1)
        {
            Vector2 toPlayer = ((Vector2)playerTransform.position - (Vector2)transform.position).normalized;
            if (toPlayer.sqrMagnitude <= 0.001f)
                return transform.position;

            return (Vector2)transform.position + toPlayer * tempoConfig.t1CentralBiasDistance;
        }

        return EnemySupportUtility.FindBestTrapPlacement(
            transform,
            playerTransform,
            tempoConfig.t1CentralBiasDistance,
            tempoConfig.t2PlacementSamples,
            tempoConfig.t2ForwardBias);
    }

    private bool IsTrapTooCloseToExisting(Vector2 spawnPosition)
    {
        foreach (TrapArea trap in activeTraps)
        {
            if (trap == null)
                continue;

            if (Vector2.Distance(spawnPosition, trap.transform.position) < minTrapDistance)
                return true;
        }

        return false;
    }

    public void DestroyAllTraps()
    {
        foreach (TrapArea trap in activeTraps)
        {
            if (trap != null)
                Destroy(trap.gameObject);
        }

        activeTraps.Clear();
    }
}

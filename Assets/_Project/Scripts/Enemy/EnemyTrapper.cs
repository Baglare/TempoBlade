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
    private float nextTetherTime;
    private bool isDead;
    private bool isPlacingTrap;
    private readonly List<TrapArea> activeTraps = new List<TrapArea>();
    private readonly List<TrapperTetherLink> activeTethers = new List<TrapperTetherLink>();

    private float stuckTimer;
    private float nextEliteRoamRefreshTime;
    private Vector2 lastPosition;

    private Transform playerTransform;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private TrapArea tetherTrapA;
    private TrapArea tetherTrapB;

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

        activeTraps.RemoveAll(t => t == null);
        activeTethers.RemoveAll(t => t == null);

        if (!isPlacingTrap)
            UpdateMovement();

        if (!isPlacingTrap && Time.time >= nextTrapTime)
        {
            if (activeTraps.Count < maxTraps)
                StartCoroutine(SpawnTrapRoutine());

            float nextInterval = GetEffectiveCooldownDuration(trapSpawnInterval * tempoConfig.trapSpawnIntervalMultiplier.Evaluate(CurrentTempoTier));
            nextTrapTime = Time.time + Mathf.Max(0.5f, nextInterval);
        }

        if (!isPlacingTrap &&
            HasEliteMechanic(EliteMechanicType.TrapperTetherTrap) &&
            Time.time >= nextTetherTime)
        {
            EliteTrapperTetherSettings settings = ActiveEliteProfile != null ? ActiveEliteProfile.trapperTetherTrap : null;
            float tetherRange = GetEffectiveTetherSearchRange(settings);
            if (settings != null && activeTraps.Count >= 2 && TryFindBestTetherPair(tetherRange, out tetherTrapA, out tetherTrapB))
            {
                StartCoroutine(CreateTetherRoutine(tetherTrapA, tetherTrapB));
            }
            else
            {
                nextTetherTime = Time.time + 0.45f;
            }
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
        targetRoamPos = GetStrategicMoveTarget();
        Vector2 currentPosition = transform.position;
        if (!EnemyLineOfSightUtility.HasLineOfSight(currentPosition, targetRoamPos, null, transform))
            targetRoamPos = GetStrategicMoveTarget(true);

        float speed = GetEffectiveMoveSpeedFromData(moveSpeedModifier);
        Vector2 moveDirection = (targetRoamPos - currentPosition).normalized;
        Vector2 projectedPosition = currentPosition + moveDirection * Mathf.Max(0.45f, speed * 0.22f);
        if (moveDirection.sqrMagnitude > 0.001f &&
            !EnemyLineOfSightUtility.IsPointNavigable(projectedPosition, 0.26f, transform))
        {
            targetRoamPos = GetStrategicMoveTarget(true);
        }

        float dist = Vector2.Distance(transform.position, targetRoamPos);
        if (dist < 0.5f)
        {
            if (!HasEliteMechanic(EliteMechanicType.TrapperTetherTrap))
                PickNewRoamPosition();
        }
        else
        {
            transform.position = Vector2.MoveTowards(transform.position, targetRoamPos, speed * Time.deltaTime);

            if (spriteRenderer != null)
                UpdateSpriteFacing(spriteRenderer, targetRoamPos.x);

            if (animator != null)
                animator.SetBool("IsMoving", true);
        }

        float movedDist = Vector2.Distance(transform.position, lastPosition);
        if (movedDist < 0.02f)
        {
            stuckTimer += Time.deltaTime;
            if (stuckTimer > 0.4f)
            {
                targetRoamPos = GetStrategicMoveTarget(true);
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
        float moveSpeed = GetEffectiveMoveSpeedFromData(moveSpeedModifier);
        float approachTimeout = Time.time + 1.25f;
        while (Vector2.Distance(transform.position, spawnPosition) > 0.95f && Time.time < approachTimeout)
        {
            transform.position = Vector2.MoveTowards(transform.position, spawnPosition, moveSpeed * Time.deltaTime);
            if (spriteRenderer != null)
                UpdateSpriteFacing(spriteRenderer, spawnPosition.x);
            if (animator != null)
                animator.SetBool("IsMoving", true);
            yield return null;
        }

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

    private IEnumerator CreateTetherRoutine(TrapArea trapA, TrapArea trapB)
    {
        EliteTrapperTetherSettings settings = ActiveEliteProfile != null ? ActiveEliteProfile.trapperTetherTrap : null;
        if (settings == null || trapA == null || trapB == null)
        {
            nextTetherTime = Time.time + 0.8f;
            yield break;
        }

        isPlacingTrap = true;
        Vector2 castAnchor = GetPreferredTetherCastPoint(trapA, trapB);
        while (Vector2.Distance(transform.position, castAnchor) > 0.8f)
        {
            transform.position = Vector2.MoveTowards(transform.position, castAnchor, GetEffectiveMoveSpeedFromData(moveSpeedModifier) * Time.deltaTime);
            if (animator != null)
                animator.SetBool("IsMoving", true);
            if (spriteRenderer != null)
                UpdateSpriteFacing(spriteRenderer, castAnchor.x);
            yield return null;
        }

        if (animator != null)
        {
            animator.SetTrigger("Attack");
            animator.SetBool("IsMoving", false);
        }
        EmitCombatAction(EnemyCombatActionType.Skill);
        yield return new WaitForSeconds(settings.linkWindup);

        GameObject tetherObject = new GameObject("TrapperTetherLink");
        TrapperTetherLink tether = tetherObject.AddComponent<TrapperTetherLink>();
        tether.Configure(trapA, trapB, settings);
        activeTethers.Add(tether);

        isPlacingTrap = false;
        float tetherCooldown = GetEffectiveCooldownDuration(Mathf.Max(1.25f, settings.tetherCooldown));
        nextTetherTime = Time.time + tetherCooldown;
    }

    private Vector2 GetStrategicMoveTarget(bool forceRefresh = false)
    {
        if (!HasEliteMechanic(EliteMechanicType.TrapperTetherTrap) || ActiveEliteProfile == null)
        {
            if (forceRefresh)
                PickNewRoamPosition();
            return targetRoamPos;
        }

        EliteTrapperTetherSettings settings = ActiveEliteProfile.trapperTetherTrap;
        float tetherRange = GetEffectiveTetherSearchRange(settings);
        bool hasTetherOpportunity = activeTraps.Count >= 2 &&
            TryFindBestTetherPair(tetherRange, out tetherTrapA, out tetherTrapB);

        if (hasTetherOpportunity && Time.time >= nextTetherTime)
            return ((Vector2)tetherTrapA.transform.position + (Vector2)tetherTrapB.transform.position) * 0.5f;

        if (forceRefresh || Time.time >= nextEliteRoamRefreshTime || Vector2.Distance(transform.position, targetRoamPos) < 0.45f)
        {
            PickEliteRoamPosition(hasTetherOpportunity);
            nextEliteRoamRefreshTime = Time.time + Random.Range(0.85f, 1.35f);
        }

        return targetRoamPos;
    }

    private bool TryFindBestTetherPair(float searchRadius, out TrapArea trapA, out TrapArea trapB)
    {
        trapA = null;
        trapB = null;
        float bestScore = float.MinValue;
        for (int i = 0; i < activeTraps.Count; i++)
        {
            TrapArea first = activeTraps[i];
            if (first == null)
                continue;

            for (int j = i + 1; j < activeTraps.Count; j++)
            {
                TrapArea second = activeTraps[j];
                if (second == null)
                    continue;
                if (HasExistingTether(first, second))
                    continue;

                float distance = Vector2.Distance(first.transform.position, second.transform.position);
                if (distance > searchRadius)
                    continue;

                Vector2 midpoint = ((Vector2)first.transform.position + (Vector2)second.transform.position) * 0.5f;
                float score = 0f;
                score -= Mathf.Abs(distance - searchRadius * 0.75f);
                score -= Vector2.Distance(transform.position, midpoint) * 0.2f;
                if (playerTransform != null)
                    score -= Vector2.Distance(playerTransform.position, midpoint) * 0.08f;

                if (score <= bestScore)
                    continue;

                bestScore = score;
                trapA = first;
                trapB = second;
            }
        }

        return trapA != null && trapB != null;
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
        return transform.position;
    }

    private void PickEliteRoamPosition(bool tetherOpportunityAvailable)
    {
        Vector2 anchor = tetherOpportunityAvailable ? GetTrapAnchorPoint() : (Vector2)transform.position;
        Vector2 best = anchor;
        float bestScore = float.MinValue;
        for (int i = 0; i < 16; i++)
        {
            Vector2 dir = Quaternion.Euler(0f, 0f, i * 45f + Random.Range(-14f, 14f)) * Vector2.right;
            float distance = tetherOpportunityAvailable ? Random.Range(1.8f, 4.2f) : Random.Range(2.6f, 5.8f);
            Vector2 candidate = anchor + dir.normalized * distance;
            if (!EnemyLineOfSightUtility.IsPointNavigable(candidate, 0.3f, transform))
                continue;
            if (!EnemyLineOfSightUtility.HasLineOfSight((Vector2)transform.position, candidate, null, transform))
                continue;

            float score = Random.Range(-0.35f, 0.35f);
            score -= EnemyLineOfSightUtility.GetObstacleDensity(candidate, 0.7f, transform) * 3.2f;
            if (playerTransform != null)
            {
                float playerDistance = Vector2.Distance(candidate, playerTransform.position);
                if (playerDistance < 3.6f)
                    score -= (3.6f - playerDistance) * 2.4f;
                if (playerDistance > 8.5f)
                    score -= (playerDistance - 8.5f) * 0.35f;

                Vector2 toPlayer = ((Vector2)playerTransform.position - (Vector2)transform.position).normalized;
                if (toPlayer.sqrMagnitude > 0.001f)
                {
                    Vector2 moveDir = (candidate - (Vector2)transform.position).normalized;
                    score -= Mathf.Max(0f, Vector2.Dot(moveDir, toPlayer)) * 1.2f;
                }
            }

            if (tetherOpportunityAvailable)
                score -= Vector2.Distance(candidate, anchor) * 0.08f;

            if (score > bestScore)
            {
                bestScore = score;
                best = candidate;
            }
        }

        targetRoamPos = best;
    }

    private float GetEffectiveTetherSearchRange(EliteTrapperTetherSettings settings)
    {
        float configuredRange = settings != null ? settings.tetherSearchRadius : 6f;
        return Mathf.Max(configuredRange, Mathf.Min(roamRadius + 1.5f, 12f), 9f);
    }

    private Vector2 GetPreferredTetherCastPoint(TrapArea trapA, TrapArea trapB)
    {
        Vector2 firstPosition = trapA.transform.position;
        Vector2 secondPosition = trapB.transform.position;
        float firstDistance = Vector2.Distance(transform.position, firstPosition);
        float secondDistance = Vector2.Distance(transform.position, secondPosition);
        Vector2 primary = firstDistance <= secondDistance ? firstPosition : secondPosition;
        Vector2 secondary = primary == firstPosition ? secondPosition : firstPosition;

        if (EnemyLineOfSightUtility.IsPointNavigable(primary, 0.3f, transform))
            return primary;
        if (EnemyLineOfSightUtility.IsPointNavigable(secondary, 0.3f, transform))
            return secondary;

        return primary;
    }

    private bool HasExistingTether(TrapArea first, TrapArea second)
    {
        for (int i = 0; i < activeTethers.Count; i++)
        {
            TrapperTetherLink tether = activeTethers[i];
            if (tether == null)
                continue;
            if (tether.Connects(first, second))
                return true;
        }

        return false;
    }

    private Vector2 GetTrapAnchorPoint()
    {
        if (activeTraps.Count == 0)
            return transform.position;

        Vector2 sum = Vector2.zero;
        int count = 0;
        for (int i = 0; i < activeTraps.Count; i++)
        {
            TrapArea trap = activeTraps[i];
            if (trap == null)
                continue;
            sum += (Vector2)trap.transform.position;
            count++;
        }

        return count > 0 ? sum / count : (Vector2)transform.position;
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

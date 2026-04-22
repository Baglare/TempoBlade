using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyWardenLinker : EnemyBase
{
    [System.Serializable]
    public class LinkerTempoConfig
    {
        public TempoTierFloatValue guardianLinkWindupMultiplier = new TempoTierFloatValue { t0 = 1f, t1 = 0.88f, t2 = 0.88f, t3 = 0.8f };
        public TempoTierFloatValue summonCooldownMultiplier = new TempoTierFloatValue { t0 = 1f, t1 = 1f, t2 = 0.9f, t3 = 0.85f };
        public TempoTierFloatValue shieldStepMoveMultiplier = new TempoTierFloatValue { t0 = 1f, t1 = 1f, t2 = 1.12f, t3 = 1.18f };
        public float t2GuardianDamageMultiplier = 0.92f;
        public float t2GuardianStaggerMultiplier = 0.9f;
        public float t3GuardianDamageMultiplier = 0.82f;
        public float t3GuardianStaggerMultiplier = 0.82f;
        public float t3HeavyHitHealthFraction = 0.15f;
        public float t3EchoDamageFraction = 0.35f;
        public float t3HeavyStunThreshold = 0.65f;
        public float t3EchoStaggerDuration = 0.32f;
    }

    [Header("Guardian Link")]
    public float guardianSearchRadius = 12f;
    public float guardianLinkCooldown = 6f;
    public float guardianLinkWindup = 0.45f;
    public float guardianLinkDuration = 4.5f;
    public float guardianDamageReductionMultiplier = 0.72f;
    public float guardianStaggerDurationMultiplier = 0.6f;
    public bool guardianIgnoreNextHeavyStagger = true;
    public Color guardianLinkColor = new Color(0.25f, 1f, 0.55f, 0.95f);

    [Header("Warden Call")]
    public GameObject summonedWardenPrefab;
    public EliteProfileSO summonedEliteWardenProfile;
    public float wardenCallCooldown = 12f;
    public float wardenCallWindup = 0.55f;
    public float summonedWardenDuration = 7.5f;
    public float summonedWardenAlpha = 0.72f;
    public float summonOffset = 1.2f;
    public Color summonCueColor = new Color(0.45f, 1f, 1f, 0.95f);

    [Header("Shield Step")]
    public float interceptOffset = 0.45f;
    public float shieldStepMoveSpeed = 4.1f;
    public float shieldStepRefreshInterval = 0.15f;
    public float shieldStepTolerance = 0.22f;
    public float retreatDistance = 2.5f;
    public float maxDistanceFromGuardTarget = 1.75f;

    [Header("Tempo")]
    public LinkerTempoConfig tempoConfig = new LinkerTempoConfig();

    [Header("Animation")]
    [SerializeField] private float deathAnimDuration = 0.6f;

    private Transform playerTransform;
    private Animator animator;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private SupportLinkVisual linkVisual;
    private readonly List<EnemyBase> activeLinks = new List<EnemyBase>();
    private readonly List<SupportLinkVisual> extraLinkVisuals = new List<SupportLinkVisual>();

    private EnemyBase linkedTarget;
    private EnemyBase positioningTarget;
    private float linkEndTime;
    private float nextLinkTime;
    private float nextWardenCallTime;
    private float nextShieldStepRefreshTime;
    private Vector2 currentInterceptPoint;
    private bool isDead;
    private bool isCasting;
    private bool suppressEchoCallbacks;
    private readonly Dictionary<EnemyBase, System.Action<EnemyBase, float>> damageHandlers = new Dictionary<EnemyBase, System.Action<EnemyBase, float>>();
    private readonly Dictionary<EnemyBase, System.Action<EnemyBase, float>> stunHandlers = new Dictionary<EnemyBase, System.Action<EnemyBase, float>>();
    private float lastHeavyLinkHitTime = -999f;

    protected override void Start()
    {
        base.Start();
        deathDelay = deathAnimDuration;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            playerTransform = player.transform;

        animator = GetComponentInChildren<Animator>();
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        linkVisual = GetComponent<SupportLinkVisual>();
        if (linkVisual == null)
            linkVisual = gameObject.AddComponent<SupportLinkVisual>();
    }

    private void Update()
    {
        if (isDead || isStunned || playerTransform == null)
            return;

        if (linkedTarget != null && (!linkedTarget.gameObject.activeInHierarchy || linkedTarget.CurrentHealth <= 0f))
        {
            ApplyDeathStaggerToLinkedTargets();
            ClearLink();
        }

        if (linkedTarget != null && Time.time >= linkEndTime)
            ClearLink();

        for (int i = 0; i < activeLinks.Count; i++)
        {
            EnemyBase activeLink = activeLinks[i];
            if (activeLink == null || !activeLink.gameObject.activeInHierarchy || activeLink.CurrentHealth <= 0f)
            {
                ApplyDeathStaggerToLinkedTargets();
                ClearLink();
                break;
            }
        }

        if (positioningTarget != null && (!positioningTarget.gameObject.activeInHierarchy || positioningTarget.CurrentHealth <= 0f))
            positioningTarget = null;

        if (!isCasting && Time.time >= nextLinkTime)
        {
            StartCoroutine(GuardianLinkRoutine());
            return;
        }

        if (!isCasting && Time.time >= nextWardenCallTime && summonedWardenPrefab != null)
        {
            StartCoroutine(WardenCallRoutine());
            return;
        }

        UpdatePositioning();
    }

    public override void TakeDamage(float amount)
    {
        if (isDead)
            return;

        if (animator != null)
            animator.SetTrigger("TakeHit");

        base.TakeDamage(amount);

        if (HasEliteMechanic(EliteMechanicType.WardenLinkerMultipleLink) && ActiveEliteProfile != null && !suppressEchoCallbacks)
            ChainDamageToOtherLinks(null, amount * ActiveEliteProfile.wardenLinkerMultipleLink.chainDamageFraction);
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
        ClearLink();

        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
            col.enabled = false;

        if (animator != null)
            animator.SetTrigger("Die");
    }

    private IEnumerator GuardianLinkRoutine()
    {
        isCasting = true;
        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        EnemyBase target = EnemySupportUtility.SelectGuardianLinkTarget(transform, guardianSearchRadius, this);
        if (target == null)
        {
            nextLinkTime = Time.time + GetEffectiveCooldownDuration(guardianLinkCooldown);
            isCasting = false;
            yield break;
        }

        List<EnemyBase> desiredTargets = BuildDesiredLinkSet(target);
        int linkCount = Mathf.Max(1, desiredTargets.Count);

        if (spriteRenderer != null)
            spriteRenderer.color = guardianLinkColor;
        EmitCombatAction(EnemyCombatActionType.Skill);

        float windupMultiplier = 1f;
        if (HasEliteMechanic(EliteMechanicType.WardenLinkerMultipleLink) && ActiveEliteProfile != null)
            windupMultiplier += Mathf.Max(0, linkCount - 1) * ActiveEliteProfile.wardenLinkerMultipleLink.extraLinkWindupMultiplierPerLink;
        float windup = GetEffectiveCooldownDuration(guardianLinkWindup * tempoConfig.guardianLinkWindupMultiplier.Evaluate(CurrentTempoTier) * windupMultiplier);
        yield return new WaitForSeconds(Mathf.Max(0.05f, windup));

        ClearLink();
        linkEndTime = Time.time + guardianLinkDuration;

        float damageMultiplier = guardianDamageReductionMultiplier;
        float staggerMultiplier = guardianStaggerDurationMultiplier;
        if (CurrentTempoTier >= TempoManager.TempoTier.T2)
        {
            damageMultiplier *= tempoConfig.t2GuardianDamageMultiplier;
            staggerMultiplier *= tempoConfig.t2GuardianStaggerMultiplier;
        }
        if (CurrentTempoTier == TempoManager.TempoTier.T3)
        {
            damageMultiplier *= tempoConfig.t3GuardianDamageMultiplier;
            staggerMultiplier *= tempoConfig.t3GuardianStaggerMultiplier;
        }

        for (int i = 0; i < desiredTargets.Count; i++)
        {
            EnemyBase nextTarget = desiredTargets[i];
            if (nextTarget == null)
                continue;

            float durationMultiplier = 1f;
            if (HasEliteMechanic(EliteMechanicType.WardenLinkerMultipleLink) && ActiveEliteProfile != null)
                durationMultiplier = Mathf.Max(0.35f, 1f - (i * ActiveEliteProfile.wardenLinkerMultipleLink.extraLinkDurationPenaltyPerLink));

            nextTarget.GetSupportBuffReceiver()?.ApplyGuardianLink(
                guardianLinkDuration * durationMultiplier,
                damageMultiplier,
                staggerMultiplier,
                guardianIgnoreNextHeavyStagger);

            activeLinks.Add(nextTarget);
            SubscribeToLinkedTarget(nextTarget);
            if (i == 0)
            {
                linkedTarget = nextTarget;
                linkVisual.SetTarget(nextTarget.transform);
            }
            else
            {
                SupportLinkVisual extraVisual = GetOrCreateExtraLinkVisual(i - 1);
                extraVisual.SetTarget(nextTarget.transform);
                if (DamagePopupManager.Instance != null)
                    DamagePopupManager.Instance.CreateText(nextTarget.transform.position + Vector3.up * 1.6f, "LINKED", guardianLinkColor, 5.5f);
            }
        }

        SupportPulseVisualUtility.SpawnPulse(transform.position, 0.2f, 1.5f, 0.22f, guardianLinkColor);
        if (DamagePopupManager.Instance != null && linkedTarget != null)
            DamagePopupManager.Instance.CreateText(linkedTarget.transform.position + Vector3.up * 1.6f, "LINKED", guardianLinkColor, 5.5f);

        if (spriteRenderer != null)
            spriteRenderer.color = Color.white;

        float cooldownMultiplier = 1f;
        if (HasEliteMechanic(EliteMechanicType.WardenLinkerMultipleLink) && ActiveEliteProfile != null)
            cooldownMultiplier += Mathf.Max(0, linkCount - 1) * ActiveEliteProfile.wardenLinkerMultipleLink.extraLinkCooldownMultiplierPerLink;
        nextLinkTime = Time.time + GetEffectiveCooldownDuration(guardianLinkCooldown * cooldownMultiplier);
        isCasting = false;
    }

    private IEnumerator WardenCallRoutine()
    {
        isCasting = true;
        if (rb != null)
            rb.linearVelocity = Vector2.zero;
        EmitCombatAction(EnemyCombatActionType.Summon);

        if (spriteRenderer != null)
            spriteRenderer.color = summonCueColor;

        yield return new WaitForSeconds(GetEffectiveCooldownDuration(wardenCallWindup));

        Vector3 summonPosition = transform.position;
        float minRadius = Mathf.Max(0.8f, summonOffset * 1.15f);
        float maxRadius = Mathf.Max(minRadius + 0.25f, summonOffset * 2.25f);
        bool foundPosition = false;
        for (int attempt = 0; attempt < 10; attempt++)
        {
            Vector2 randomDir = Random.insideUnitCircle.normalized;
            if (randomDir.sqrMagnitude <= 0.001f)
                randomDir = Vector2.right;
            float randomRadius = Random.Range(minRadius, maxRadius);
            Vector2 candidate = (Vector2)transform.position + randomDir * randomRadius;
            if (!EnemyLineOfSightUtility.IsPointNavigable(candidate, 0.35f, transform))
                continue;

            summonPosition = candidate;
            foundPosition = true;
            break;
        }

        if (!foundPosition)
            summonPosition = transform.position + (Vector3)(Random.insideUnitCircle.normalized * minRadius);

        EliteProfileSO summonProfile = null;
        if (HasEliteMechanic(EliteMechanicType.WardenLinkerMultipleLink))
            summonProfile = summonedEliteWardenProfile;
        EnemySummonHelper.SummonTemporaryEnemy(summonedWardenPrefab, summonPosition, summonedWardenDuration, summonedWardenAlpha, summonProfile, this);
        SupportPulseVisualUtility.SpawnPulse(summonPosition, 0.2f, 1.1f, 0.25f, summonCueColor);
        if (DamagePopupManager.Instance != null)
            DamagePopupManager.Instance.CreateText(summonPosition + Vector3.up, "WARDEN!", summonCueColor, 6f);

        if (spriteRenderer != null)
            spriteRenderer.color = Color.white;

        float summonCooldown = wardenCallCooldown * tempoConfig.summonCooldownMultiplier.Evaluate(CurrentTempoTier);
        if (HasEliteMechanic(EliteMechanicType.WardenLinkerMultipleLink) && ActiveEliteProfile != null)
            summonCooldown *= ActiveEliteProfile.wardenLinkerMultipleLink.summonCooldownMultiplier;
        nextWardenCallTime = Time.time + Mathf.Max(1f, GetEffectiveCooldownDuration(summonCooldown));
        isCasting = false;
    }

    private void UpdatePositioning()
    {
        if (Time.time >= nextShieldStepRefreshTime)
        {
            currentInterceptPoint = ComputeInterceptPoint();
            nextShieldStepRefreshTime = Time.time + shieldStepRefreshInterval;
        }

        Vector2 toPoint = currentInterceptPoint - (Vector2)transform.position;
        bool isMoving = toPoint.magnitude > shieldStepTolerance;

        if (rb != null)
        {
            float stepSpeed = shieldStepMoveSpeed * tempoConfig.shieldStepMoveMultiplier.Evaluate(CurrentTempoTier);
            if (linkedTarget == null)
                stepSpeed *= 0.92f;
            rb.linearVelocity = isMoving
                ? toPoint.normalized * GetEffectiveMoveSpeed(stepSpeed)
                : Vector2.zero;
        }

        if (spriteRenderer != null)
            UpdateSpriteFacing(spriteRenderer, playerTransform.position.x);

        if (animator != null)
            animator.SetBool("IsMoving", isMoving);
    }

    private Vector2 ComputeInterceptPoint()
    {
        if (linkedTarget != null && linkedTarget.CurrentHealth > 0f)
            positioningTarget = linkedTarget;
        else if (positioningTarget == null || positioningTarget.CurrentHealth <= 0f)
            positioningTarget = EnemySupportUtility.SelectGuardianLinkTarget(transform, guardianSearchRadius, this);

        EnemyBase target = linkedTarget != null && linkedTarget.CurrentHealth > 0f
            ? linkedTarget
            : positioningTarget;

        if (target != null && target.CurrentHealth > 0f)
        {
            Vector2 targetPos = target.transform.position;
            Vector2 toPlayer = (Vector2)playerTransform.position - targetPos;
            if (toPlayer.sqrMagnitude > 0.001f)
            {
                Vector2 awayFromPlayer = (-toPlayer).normalized;
                Vector2 lateral = new Vector2(-toPlayer.y, toPlayer.x).normalized;
                bool targetIsWarden = target is EnemyWarden;
                float sideSign = ((GetInstanceID() & 1) == 0) ? 1f : -1f;
                float supportAngle = targetIsWarden ? 72f : 58f;
                Vector2 supportDirection = (Quaternion.Euler(0f, 0f, supportAngle * sideSign) * awayFromPlayer).normalized;
                float orbitRadius = targetIsWarden
                    ? Mathf.Max(interceptOffset * 2.35f, maxDistanceFromGuardTarget * 0.96f)
                    : Mathf.Max(interceptOffset * 1.75f, maxDistanceFromGuardTarget * 0.82f);
                Vector2 interceptPoint = targetPos + supportDirection * orbitRadius;

                if (!EnemyLineOfSightUtility.IsPointNavigable(interceptPoint, 0.28f, transform))
                {
                    RangedTacticalDecision decision = RangedTacticalMovementUtility.EvaluatePosition(
                        transform,
                        playerTransform,
                        transform.position,
                        maxDistanceFromGuardTarget + 0.8f,
                        guardianSearchRadius,
                        2f,
                        8,
                        transform);
                    if (decision.foundBetterPosition)
                        interceptPoint = decision.moveTarget;
                }

                if (Vector2.Distance(interceptPoint, playerTransform.position) < retreatDistance * 1.1f)
                    interceptPoint = targetPos + awayFromPlayer * Mathf.Max(interceptOffset * 1.8f, maxDistanceFromGuardTarget * 0.88f);

                return interceptPoint;
            }

            return targetPos;
        }

        return transform.position;
    }

    private void ClearLink()
    {
        UnsubscribeAllLinkedTargets();
        linkedTarget = null;
        linkEndTime = 0f;
        activeLinks.Clear();
        if (linkVisual != null)
            linkVisual.SetTarget(null);
        for (int i = 0; i < extraLinkVisuals.Count; i++)
            extraLinkVisuals[i]?.SetTarget(null);
    }

    private void SubscribeToLinkedTarget(EnemyBase target)
    {
        if (target == null)
            return;

        if (damageHandlers.ContainsKey(target))
            return;

        System.Action<EnemyBase, float> damageHandler = (source, amount) => HandleLinkedTargetDamageTaken(source, amount);
        System.Action<EnemyBase, float> stunHandler = (source, duration) => HandleLinkedTargetStunned(source, duration);
        damageHandlers[target] = damageHandler;
        stunHandlers[target] = stunHandler;
        target.OnDamageTakenDetailed += damageHandler;
        target.OnStunnedDetailed += stunHandler;
    }

    private void UnsubscribeAllLinkedTargets()
    {
        foreach (var pair in damageHandlers)
        {
            if (pair.Key != null)
                pair.Key.OnDamageTakenDetailed -= pair.Value;
        }
        foreach (var pair in stunHandlers)
        {
            if (pair.Key != null)
                pair.Key.OnStunnedDetailed -= pair.Value;
        }
        damageHandlers.Clear();
        stunHandlers.Clear();
    }

    private void HandleLinkedTargetDamageTaken(EnemyBase source, float amount)
    {
        if (source == null || suppressEchoCallbacks)
            return;

        if (CurrentTempoTier == TempoManager.TempoTier.T3)
        {
            float heavyThreshold = source.MaxHealth * tempoConfig.t3HeavyHitHealthFraction;
            if (amount >= heavyThreshold)
            {
                lastHeavyLinkHitTime = Time.time;
                ApplyEcho(amount * tempoConfig.t3EchoDamageFraction, true);
            }
        }

        if (HasEliteMechanic(EliteMechanicType.WardenLinkerMultipleLink) && ActiveEliteProfile != null)
            ChainDamageToOtherLinks(source, amount * ActiveEliteProfile.wardenLinkerMultipleLink.chainDamageFraction);
    }

    private void HandleLinkedTargetStunned(EnemyBase source, float duration)
    {
        if (CurrentTempoTier != TempoManager.TempoTier.T3 || suppressEchoCallbacks)
            return;

        if (duration < tempoConfig.t3HeavyStunThreshold)
            return;

        ApplyEcho(0f, true);
    }

    private void ApplyEcho(float damageAmount, bool applyStagger)
    {
        suppressEchoCallbacks = true;
        try
        {
            if (damageAmount > 0f)
                base.TakeDamage(damageAmount);

            if (applyStagger)
                base.Stun(tempoConfig.t3EchoStaggerDuration);
        }
        finally
        {
            suppressEchoCallbacks = false;
        }
    }

    private List<EnemyBase> BuildDesiredLinkSet(EnemyBase primaryTarget)
    {
        List<EnemyBase> result = new List<EnemyBase>();
        if (primaryTarget == null)
            return result;

        result.Add(primaryTarget);
        if (!HasEliteMechanic(EliteMechanicType.WardenLinkerMultipleLink) || ActiveEliteProfile == null)
            return result;

        EliteWardenLinkerMultipleLinkSettings settings = ActiveEliteProfile.wardenLinkerMultipleLink;
        List<EnemyBase> candidates = new List<EnemyBase>();
        EnemyBase[] enemies = FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);
        for (int i = 0; i < enemies.Length; i++)
        {
            EnemyBase enemy = enemies[i];
            if (enemy == null || enemy == this || enemy == primaryTarget || enemy.CurrentHealth <= 0f || !enemy.gameObject.activeInHierarchy)
                continue;
            if (enemy.GetComponent<TemporaryEnemySummon>() != null)
                continue;
            if (Vector2.Distance(primaryTarget.transform.position, enemy.transform.position) > settings.clusterRadius)
                continue;
            candidates.Add(enemy);
        }

        candidates.Sort((a, b) =>
        {
            float aScore = EnemySupportUtility.GetGuardianPriorityScore(a) - Vector2.Distance(transform.position, a.transform.position) * 0.08f;
            float bScore = EnemySupportUtility.GetGuardianPriorityScore(b) - Vector2.Distance(transform.position, b.transform.position) * 0.08f;
            return bScore.CompareTo(aScore);
        });

        while (candidates.Count >= result.Count)
        {
            EnemyBase nextTarget = candidates[result.Count - 1];
            if (nextTarget == null)
                break;
            result.Add(nextTarget);
        }

        return result;
    }

    private SupportLinkVisual GetOrCreateExtraLinkVisual(int index)
    {
        while (extraLinkVisuals.Count <= index)
        {
            GameObject child = new GameObject($"ExtraLink_{extraLinkVisuals.Count}");
            child.transform.SetParent(transform, false);
            SupportLinkVisual visual = child.AddComponent<SupportLinkVisual>();
            extraLinkVisuals.Add(visual);
        }

        return extraLinkVisuals[index];
    }

    private void ChainDamageToOtherLinks(EnemyBase source, float damageAmount)
    {
        if (damageAmount <= 0f)
            return;

        suppressEchoCallbacks = true;
        try
        {
            for (int i = 0; i < activeLinks.Count; i++)
            {
                EnemyBase target = activeLinks[i];
                if (target == null || target == source)
                    continue;
                target.TakeDamage(damageAmount);
            }
        }
        finally
        {
            suppressEchoCallbacks = false;
        }
    }

    private void ApplyDeathStaggerToLinkedTargets()
    {
        if (!HasEliteMechanic(EliteMechanicType.WardenLinkerMultipleLink) || ActiveEliteProfile == null)
            return;

        float staggerDuration = ActiveEliteProfile.wardenLinkerMultipleLink.deathStaggerDuration;
        for (int i = 0; i < activeLinks.Count; i++)
        {
            EnemyBase target = activeLinks[i];
            if (target == null || target == linkedTarget)
                continue;
            target.Stun(staggerDuration);
        }
    }
}

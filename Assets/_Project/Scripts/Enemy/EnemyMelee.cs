using System.Collections;
using UnityEngine;

public class EnemyMelee : EnemyBase
{
    [System.Serializable]
    public class MeleeTempoConfig
    {
        public TempoTierFloatValue moveSpeedMultiplier = new TempoTierFloatValue { t0 = 1f, t1 = 1.08f, t2 = 1.08f, t3 = 1.12f };
        public TempoTierFloatValue attackCooldownMultiplier = new TempoTierFloatValue { t0 = 1f, t1 = 0.92f, t2 = 0.92f, t3 = 0.98f };
        public TempoTierFloatValue recoveryMultiplier = new TempoTierFloatValue { t0 = 1f, t1 = 1f, t2 = 1f, t3 = 1.35f };
        public TempoTierFloatValue stunDurationMultiplier = new TempoTierFloatValue { t0 = 1f, t1 = 1f, t2 = 1f, t3 = 1.35f };
        public TempoTierFloatValue swingArcMultiplier = new TempoTierFloatValue { t0 = 1f, t1 = 1f, t2 = 1f, t3 = 1.28f };
        public int t2ComboHits = 2;
        public int t3MinComboHits = 2;
        public int t3MaxComboHits = 3;
        public float windupTime = 0.35f;
        public float activeFrames = 0.18f;
        public float comboStepGap = 0.12f;
    }

    [Header("Melee Settings")]
    public AttackHitbox hitboxScript;
    public Collider2D hitboxCollider;

    [Header("Arc Visual")]
    [Tooltip("WeaponArcVisual component'i. Enemy altindaki child'a eklenir.")]
    public WeaponArcVisual weaponArcVisual;

    [Header("Tempo")]
    public MeleeTempoConfig tempoConfig = new MeleeTempoConfig();

    private Transform player;
    private bool isAttacking;
    private float baseArcAngle;
    private Vector2 eliteLockedDirection = Vector2.right;

    protected override void Start()
    {
        base.Start();
        var p = FindFirstObjectByType<PlayerController>();
        if (p != null)
            player = p.transform;

        if (hitboxScript != null)
            hitboxScript.owner = this;

        if (hitboxCollider != null && enemyData != null)
        {
            hitboxCollider.enabled = false;
            if (hitboxCollider is BoxCollider2D box)
            {
                box.size = new Vector2(enemyData.attackRange, 1f);
                box.offset = new Vector2(enemyData.attackRange / 2f, 0f);
            }
        }

        if (weaponArcVisual != null && enemyData != null)
        {
            weaponArcVisual.range = enemyData.attackRange;
            float multiplier = Mathf.Max(0.01f, tempoConfig.swingArcMultiplier.Evaluate(CurrentTempoTier));
            baseArcAngle = weaponArcVisual.arcAngle / multiplier;
            ApplyTempoVisuals(CurrentTempoTier);
        }
    }

    protected override void OnTempoTierChanged(TempoManager.TempoTier tier)
    {
        base.OnTempoTierChanged(tier);
        ApplyTempoVisuals(tier);
    }

    private void Update()
    {
        if (weaponArcVisual != null && player != null && enemyData != null)
        {
            Vector2 dirToPlayer = ((Vector2)player.position - (Vector2)transform.position).normalized;
            weaponArcVisual.UpdateVisuals(transform.position, dirToPlayer, isAttacking, false);
        }

        if (isStunned || player == null || isAttacking || enemyData == null)
            return;

        float dist = Vector2.Distance(transform.position, player.position);
        if (dist > enemyData.detectionRange)
            return;

        if (dist > enemyData.attackRange)
        {
            float moveMultiplier = tempoConfig.moveSpeedMultiplier.Evaluate(CurrentTempoTier);
            transform.position = Vector2.MoveTowards(
                transform.position,
                player.position,
                GetEffectiveMoveSpeed(enemyData.moveSpeed * moveMultiplier) * Time.deltaTime);

            Vector3 direction = player.position - transform.position;
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                if (direction.x > 0) sr.flipX = false;
                else if (direction.x < 0) sr.flipX = true;
            }
        }
        else
        {
            StartCoroutine(AttackRoutine());
        }
    }

    private IEnumerator AttackRoutine()
    {
        isAttacking = true;
        EmitCombatAction(EnemyCombatActionType.Attack);

        if (HasEliteMechanic(EliteMechanicType.MeleeRendCombo) && ActiveEliteProfile != null)
        {
            yield return EliteRendComboRoutine();
            isAttacking = false;
            yield break;
        }

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        Color originalColor = sr != null ? sr.color : Color.white;
        float attackSpeedMultiplier = Mathf.Max(0.01f, GetSupportAttackSpeedMultiplier());
        int comboHits = GetComboHitCount();

        for (int i = 0; i < comboHits; i++)
        {
            if (isStunned || player == null)
                break;

            EmitCombatAction(EnemyCombatActionType.Attack, i == comboHits - 1 ? 1.1f : 1f);

            if (sr != null)
                sr.color = Color.yellow;

            AlignAttackToPlayer();

            yield return new WaitForSeconds(tempoConfig.windupTime / attackSpeedMultiplier);

            if (isStunned)
                break;

            if (sr != null)
                sr.color = Color.red;

            if (hitboxCollider != null)
                hitboxCollider.enabled = true;

            yield return new WaitForSeconds(tempoConfig.activeFrames);

            if (hitboxCollider != null)
                hitboxCollider.enabled = false;

            if (sr != null)
                sr.color = originalColor;

            if (i < comboHits - 1)
                yield return new WaitForSeconds(tempoConfig.comboStepGap / attackSpeedMultiplier);
        }

        if (hitboxCollider != null)
            hitboxCollider.enabled = false;

        if (sr != null)
            sr.color = originalColor;

        float cooldown = GetEffectiveCooldownDuration(
                         enemyData.attackCooldown *
                         tempoConfig.attackCooldownMultiplier.Evaluate(CurrentTempoTier) *
                         tempoConfig.recoveryMultiplier.Evaluate(CurrentTempoTier));
        yield return new WaitForSeconds(cooldown / attackSpeedMultiplier);

        isAttacking = false;
    }

    private IEnumerator EliteRendComboRoutine()
    {
        EliteMeleeRendComboSettings settings = ActiveEliteProfile != null ? ActiveEliteProfile.meleeRendCombo : null;
        if (settings == null || player == null)
            yield break;

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        Color originalColor = sr != null ? sr.color : Color.white;
        float attackSpeedMultiplier = Mathf.Max(0.01f, GetSupportAttackSpeedMultiplier());
        eliteLockedDirection = ((Vector2)player.position - (Vector2)transform.position).normalized;
        if (eliteLockedDirection.sqrMagnitude <= 0.001f)
            eliteLockedDirection = transform.localScale.x >= 0f ? Vector2.right : Vector2.left;

        yield return PerformEliteRendHit(settings.firstHitDamageMultiplier, settings.firstWindupRange, settings.hitStaggerDuration, settings.hitKnockback, 1f, originalColor, sr, attackSpeedMultiplier);
        yield return new WaitForSeconds(Random.Range(settings.secondGapRange.x, settings.secondGapRange.y) + settings.reactionGap);
        yield return PerformEliteRendHit(settings.secondHitDamageMultiplier, new Vector2(0.01f, 0.01f), settings.hitStaggerDuration, settings.hitKnockback, 1f, originalColor, sr, attackSpeedMultiplier);
        yield return new WaitForSeconds(Random.Range(settings.thirdGapRange.x, settings.thirdGapRange.y) + settings.reactionGap);
        yield return PerformEliteRendHit(settings.thirdHitDamageMultiplier, new Vector2(0.01f, 0.01f), settings.hitStaggerDuration * 1.15f, settings.hitKnockback * 1.2f, settings.heavyCleaveArcMultiplier, originalColor, sr, attackSpeedMultiplier);

        if (sr != null)
            sr.color = originalColor;

        yield return new WaitForSeconds(GetEffectiveCooldownDuration(settings.recoveryDuration));
    }

    private IEnumerator PerformEliteRendHit(float damageMultiplier, Vector2 windupRange, float staggerDuration, float knockback, float arcMultiplier, Color originalColor, SpriteRenderer sr, float attackSpeedMultiplier)
    {
        if (sr != null)
            sr.color = Color.Lerp(Color.yellow, Color.red, Mathf.Clamp01(damageMultiplier * 0.45f));

        float windup = Random.Range(windupRange.x, windupRange.y) / attackSpeedMultiplier;
        if (windup > 0.01f)
            yield return new WaitForSeconds(windup);

        ResolveEliteRendHit(damageMultiplier, staggerDuration, knockback);

        if (weaponArcVisual != null)
            weaponArcVisual.arcAngle = baseArcAngle * arcMultiplier * tempoConfig.swingArcMultiplier.Evaluate(CurrentTempoTier);

        yield return new WaitForSeconds(0.08f + 0.02f);
        if (weaponArcVisual != null)
            weaponArcVisual.arcAngle = baseArcAngle * tempoConfig.swingArcMultiplier.Evaluate(CurrentTempoTier);

        if (sr != null)
            sr.color = originalColor;
    }

    private void ResolveEliteRendHit(float damageMultiplier, float staggerDuration, float knockback)
    {
        Vector2 strikeOrigin = (Vector2)transform.position + eliteLockedDirection * Mathf.Max(0.6f, enemyData.attackRange * 0.55f);
        float strikeRadius = Mathf.Max(0.6f, enemyData.attackRange * 0.65f);
        Collider2D[] hits = Physics2D.OverlapCircleAll(strikeOrigin, strikeRadius);
        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];
            if (!hit.CompareTag("Player"))
                continue;

            ParrySystem parry = hit.GetComponent<ParrySystem>();
            if (parry != null && parry.TryBlockMelee(strikeOrigin, gameObject))
            {
                Stun(0.45f);
                return;
            }

            PlayerController playerController = hit.GetComponent<PlayerController>();
            if (playerController != null && playerController.IsInvulnerable)
            {
                hit.GetComponent<DashPerkController>()?.NotifyMeleeDodged(this);
                continue;
            }

            PlayerCombat playerCombat = hit.GetComponent<PlayerCombat>();
            if (playerCombat != null)
                playerCombat.TakeDamage(GetEffectiveDamageFromData(enemyData != null ? enemyData.damage : 10f) * damageMultiplier);

            playerController?.ApplyExternalStagger(staggerDuration, eliteLockedDirection * knockback);
        }
    }

    public override void Stun(float duration)
    {
        float finalDuration = duration * tempoConfig.stunDurationMultiplier.Evaluate(CurrentTempoTier);
        base.Stun(finalDuration);
        isAttacking = false;
        if (hitboxCollider != null)
            hitboxCollider.enabled = false;
    }

    private void AlignAttackToPlayer()
    {
        if (player == null || hitboxCollider == null)
            return;

        Vector3 dir = (player.position - transform.position).normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        hitboxCollider.transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private int GetComboHitCount()
    {
        if (CurrentTempoTier == TempoManager.TempoTier.T3)
            return Random.Range(tempoConfig.t3MinComboHits, tempoConfig.t3MaxComboHits + 1);

        if (CurrentTempoTier == TempoManager.TempoTier.T2)
            return Mathf.Max(1, tempoConfig.t2ComboHits);

        return 1;
    }

    private void ApplyTempoVisuals(TempoManager.TempoTier tier)
    {
        if (weaponArcVisual == null)
            return;

        if (baseArcAngle <= 0f)
        {
            float currentMultiplier = Mathf.Max(0.01f, tempoConfig.swingArcMultiplier.Evaluate(CurrentTempoTier));
            baseArcAngle = weaponArcVisual.arcAngle / currentMultiplier;
        }

        float baseAngle = baseArcAngle;
        weaponArcVisual.arcAngle = baseAngle * tempoConfig.swingArcMultiplier.Evaluate(tier);
    }
}

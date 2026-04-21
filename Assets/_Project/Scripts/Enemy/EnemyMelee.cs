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

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        Color originalColor = sr != null ? sr.color : Color.white;
        float attackSpeedMultiplier = Mathf.Max(0.01f, GetSupportAttackSpeedMultiplier());
        int comboHits = GetComboHitCount();

        for (int i = 0; i < comboHits; i++)
        {
            if (isStunned || player == null)
                break;

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

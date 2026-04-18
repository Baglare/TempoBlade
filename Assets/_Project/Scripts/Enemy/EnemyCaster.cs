using UnityEngine;
using System.Collections;

public class EnemyCaster : EnemyBase, IParryReactive
{
    [Header("Caster Settings")]
    public GameObject projectilePrefab;
    public Transform firePoint;
    public float attackRange = 8f;
    public float retreatRange = 5f;
    public float fireRate = 2f;

    [Header("Animation")]
    [SerializeField] private float deathAnimDuration = 0.8f;

    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private float nextFireTime;
    private Transform playerTransform;
    private bool isAttacking;
    private bool isDead;
    private Coroutine suppressRoutine;

    public bool AllowParryExecute => true;

    protected override void Start()
    {
        base.Start();

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            playerTransform = player.transform;

        animator = GetComponentInChildren<Animator>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        deathDelay = deathAnimDuration;
    }

    private void Update()
    {
        if (isDead || isStunned || playerTransform == null)
            return;

        float dist = Vector2.Distance(transform.position, playerTransform.position);
        FacePlayer();

        bool moving = false;
        if (!isAttacking)
        {
            if (dist > attackRange)
            {
                MoveTowards(playerTransform.position);
                moving = true;
            }
            else if (dist < retreatRange)
            {
                Vector2 dir = ((Vector2)transform.position - (Vector2)playerTransform.position).normalized;
                MoveTowards((Vector2)transform.position + dir);
                moving = true;
            }
            else
            {
                StopMovement();
                if (Time.time >= nextFireTime)
                    StartCoroutine(AttackRoutine());
            }
        }

        if (animator != null)
            animator.SetBool("IsMoving", moving);
    }

    private void FacePlayer()
    {
        if (spriteRenderer == null || playerTransform == null)
            return;

        spriteRenderer.flipX = playerTransform.position.x < transform.position.x;
    }

    private void MoveTowards(Vector2 target)
    {
        float speed = (enemyData != null ? enemyData.moveSpeed : 3f) * GetSupportMoveSpeedMultiplier();
        transform.position = Vector2.MoveTowards(transform.position, target, speed * Time.deltaTime);
    }

    private void StopMovement()
    {
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.linearVelocity = Vector2.zero;
    }

    private IEnumerator AttackRoutine()
    {
        isAttacking = true;

        Vector2 aimDirection = Vector2.right;
        if (playerTransform != null && firePoint != null)
            aimDirection = ((Vector2)playerTransform.position - (Vector2)firePoint.position).normalized;

        if (animator != null)
            animator.SetTrigger("Attack");

        if (projectilePrefab != null && firePoint != null)
        {
            GameObject projObj = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
            Projectile proj = projObj.GetComponent<Projectile>();
            if (proj != null)
            {
                proj.owner = gameObject;
                proj.damage = enemyData != null ? enemyData.damage : proj.damage;
                proj.Launch(aimDirection);
            }
        }

        nextFireTime = Time.time + fireRate / Mathf.Max(0.01f, GetSupportAttackSpeedMultiplier());
        isAttacking = false;
        yield break;
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
        isAttacking = false;
        StopAllCoroutines();

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
            col.enabled = false;

        if (animator != null)
            animator.SetTrigger("Die");
    }

    public void OnParryReaction(ParryReactionContext context)
    {
        if (isDead)
            return;

        StopAllCoroutines();
        isAttacking = false;

        if (animator != null)
            animator.SetTrigger("TakeHit");

        if (suppressRoutine != null)
            StopCoroutine(suppressRoutine);

        suppressRoutine = StartCoroutine(SuppressRoutine(Mathf.Max(0.05f, context.duration)));
    }

    private IEnumerator SuppressRoutine(float duration)
    {
        base.Stun(duration);
        nextFireTime = Mathf.Max(nextFireTime, Time.time + duration);
        yield return new WaitForSeconds(duration);
        suppressRoutine = null;
    }
}

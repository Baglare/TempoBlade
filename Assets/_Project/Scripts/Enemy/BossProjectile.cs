using UnityEngine;

public class BossProjectile : MonoBehaviour, IDeflectable
{
    [Header("Projectile Settings")]
    public float speed = 5f;
    public float damage = 15f;
    public float lifeTime = 5f;

    [Header("Deflect Settings")]
    public bool isDeflectable = true;
    public Color normalColor = Color.cyan;
    public Color deflectedColor = Color.yellow;

    [HideInInspector] public GameObject owner;
    public GameObject ObjectOwner => owner;

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private bool hasBeenDeflected = false;
    public bool IsDeflected => hasBeenDeflected;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null) sr.color = normalColor;
    }

    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    public void Fire(Vector2 direction, GameObject creator = null)
    {
        owner = creator;

        if (rb != null)
        {
            rb.linearVelocity = direction.normalized * speed;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        GameObject targetRoot = other.attachedRigidbody != null ? other.attachedRigidbody.gameObject : other.gameObject;

        if (owner != null && (other.gameObject == owner || targetRoot == owner)) return;

        if (other.gameObject.layer == LayerMask.NameToLayer("Environment"))
        {
            Destroy(gameObject);
            return;
        }

        IDamageable damageable = other.GetComponent<IDamageable>();
        if (damageable == null) damageable = other.GetComponentInParent<IDamageable>();
        if (damageable == null) return;

        bool isTargetPlayer = targetRoot.CompareTag("Player") || other.CompareTag("Player");
        bool isTargetEnemy = targetRoot.CompareTag("Enemy") || other.CompareTag("Enemy") ||
                             other.GetComponentInParent<EnemyBase>() != null;

        // Non-deflected projectile can only damage player.
        if (!hasBeenDeflected && !isTargetPlayer) return;
        // Deflected projectile can only damage enemies.
        if (hasBeenDeflected && !isTargetEnemy) return;

        if (isTargetPlayer && !hasBeenDeflected)
        {
            DashSkillRuntime dashRuntime = other.GetComponent<DashSkillRuntime>();
            if (dashRuntime == null) dashRuntime = other.GetComponentInParent<DashSkillRuntime>();
            if (dashRuntime != null && dashRuntime.TryDodgeProjectile(transform.position))
            {
                if (DamagePopupManager.Instance != null)
                    DamagePopupManager.Instance.CreateText(other.transform.position + Vector3.up, "DODGE!", Color.cyan, 5f);
                Destroy(gameObject);
                return;
            }

            ParrySystem parry = other.GetComponent<ParrySystem>();
            if (parry == null) parry = other.GetComponentInParent<ParrySystem>();
            if (parry != null && parry.TryDeflect(transform.position))
            {
                Deflect(other.gameObject);
                if (TempoManager.Instance != null)
                    TempoManager.Instance.AddTempo(10f);
                return;
            }
        }

        float finalDamage = hasBeenDeflected ? damage * 2f : damage;
        damageable.TakeDamage(finalDamage);

        if (hasBeenDeflected && DamagePopupManager.Instance != null && isTargetEnemy)
            DamagePopupManager.Instance.CreateText(transform.position + Vector3.up, "DEFLECT HIT!", Color.magenta, 8f);

        Destroy(gameObject);
    }

    public void Deflect(GameObject newOwner)
    {
        if (!isDeflectable || hasBeenDeflected) return;

        hasBeenDeflected = true;
        owner = newOwner;
        lifeTime += 2f;

        Vector2 currentDir;
        EnemyBoss boss = Object.FindFirstObjectByType<EnemyBoss>();
        if (boss != null)
            currentDir = (boss.transform.position - transform.position).normalized;
        else
            currentDir = rb != null ? -rb.linearVelocity.normalized : Vector2.left;

        if (rb != null)
        {
            rb.linearVelocity = currentDir * (speed * 1.5f);
            float angle = Mathf.Atan2(currentDir.y, currentDir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        if (sr != null) sr.color = deflectedColor;
    }
}

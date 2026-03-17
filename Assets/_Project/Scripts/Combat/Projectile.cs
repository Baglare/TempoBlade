using UnityEngine;

public class Projectile : MonoBehaviour, IDeflectable
{
    public float speed = 10f;
    public float damage = 10f;
    public float lifeTime = 5f;

    [HideInInspector] public GameObject owner;
    public GameObject ObjectOwner => owner;

    private Rigidbody2D rb;
    private bool hasBeenDeflected = false;
    public bool IsDeflected => hasBeenDeflected;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    public void Launch(Vector2 direction)
    {
        if (rb == null) return;
        rb.linearVelocity = direction.normalized * speed;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (owner != null && other.gameObject == owner) return;

        if (other.gameObject.layer == LayerMask.NameToLayer("Environment"))
        {
            DestroyProjectile();
            return;
        }

        IDamageable damageable = other.GetComponent<IDamageable>();
        if (damageable == null) return;

        bool isHittingEnemy = other.CompareTag("Enemy");
        if (isHittingEnemy && !hasBeenDeflected)
            return;

        if (other.CompareTag("Player") && !hasBeenDeflected)
        {
            DashSkillRuntime dashRuntime = other.GetComponent<DashSkillRuntime>();
            if (dashRuntime != null && dashRuntime.TryDodgeProjectile(transform.position))
            {
                if (DamagePopupManager.Instance != null)
                    DamagePopupManager.Instance.CreateText(other.transform.position + Vector3.up, "DODGE!", Color.cyan, 5f);
                DestroyProjectile();
                return;
            }

            ParrySystem parry = other.GetComponent<ParrySystem>();
            if (parry != null && parry.TryDeflect(transform.position))
            {
                Deflect(other.gameObject);
                return;
            }
        }

        damageable.TakeDamage(damage);
        DestroyProjectile();
    }

    public void Deflect(GameObject newOwner)
    {
        if (hasBeenDeflected) return;

        hasBeenDeflected = true;
        if (rb != null)
        {
            rb.linearVelocity = -rb.linearVelocity * 1.5f;
            transform.rotation = Quaternion.Euler(0, 0, transform.rotation.eulerAngles.z + 180f);
        }

        owner = newOwner;
        lifeTime += 2f;

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = Color.yellow;
    }

    private void DestroyProjectile()
    {
        Destroy(gameObject);
    }
}


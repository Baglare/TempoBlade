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
        Destroy(gameObject, lifeTime); // Güvenlik çemberi
    }

    public void Fire(Vector2 direction, GameObject creator = null)
    {
        owner = creator;
        
        if (rb != null)
        {
            rb.linearVelocity = direction.normalized * speed;
            
            // Rotasyon
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Kendi sahibine çarpma (Çıktığı gibi patlamayı önler)
        if (owner != null && other.gameObject == owner) return;

        // Duvarlara çarpma
        if (other.gameObject.layer == LayerMask.NameToLayer("Environment"))
        {
            Destroy(gameObject);
            return;
        }

        // Oyuncuya veya Düşmana (Boss'a) çarpma
        IDamageable damageable = other.GetComponent<IDamageable>();
        if (damageable != null)
        {
            // Dost atesi (Friendly Fire) Kontrolu
            bool isHittingEnemy = other.CompareTag("Enemy");
            
            // Eger oyuncu tarafindan sekmediyse ve bir dusmana (kendisi olmayan) carptiysa es gec (icinden gecsin)
            if (isHittingEnemy && !hasBeenDeflected)
            {
                return; 
            }
            // Eğer Oyuncuya çarptıysa ve Parry yapıyorsa -> DEFLECT (yonlu kontrol)
            if (other.CompareTag("Player") && !hasBeenDeflected)
            {
                ParrySystem parry = other.GetComponent<ParrySystem>();
                if (parry != null && parry.TryDeflect(transform.position)) // Hiz yerine konum veriyoruz
                {
                    Deflect(other.gameObject);

                    // Ekstra tempo
                    if (TempoManager.Instance != null)
                    {
                        TempoManager.Instance.AddTempo(10f);
                    }
                    return;
                }
            }

            // Normal Hasar (Sekmişse Boss'a 2xvurur, sekmemişse oyuncuya 1x)
            float finalDamage = hasBeenDeflected ? damage * 2f : damage;
            damageable.TakeDamage(finalDamage);
            
            if (hasBeenDeflected && DamagePopupManager.Instance != null && other.CompareTag("Enemy"))
            {
                DamagePopupManager.Instance.CreateText(transform.position + Vector3.up, "DEFLECT HIT!", Color.magenta, 8f);
            }
            
            Destroy(gameObject);
        }
    }
    
    public void Deflect(GameObject newOwner)
    {
        if (!isDeflectable || hasBeenDeflected) return;
        
        hasBeenDeflected = true;
        owner = newOwner; // Sahibi artık Player! Boss'a çarpabilir.
        lifeTime += 2f;
        
        // Mermiyi dogrudan Boss'a nisanla
        Vector2 currentDir;
        EnemyBoss boss = Object.FindFirstObjectByType<EnemyBoss>();
        if (boss != null)
        {
             currentDir = (boss.transform.position - transform.position).normalized;
        }
        else
        {
             currentDir = -rb.linearVelocity.normalized; // Boss yoksa geldiği yere dön
        }
        
        if (rb != null)
        {
            rb.linearVelocity = currentDir * (speed * 1.5f); // Geri donen mermi %50 daha hizlidir
            
            // Rotasyonu ayarla
            float angle = Mathf.Atan2(currentDir.y, currentDir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }
        
        // Renk Degisimi
        if (sr != null) sr.color = deflectedColor;
    }
}

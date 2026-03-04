using UnityEngine;

public class Projectile : MonoBehaviour, IDeflectable
{
    public float speed = 10f;
    public float damage = 10f;
    public float lifeTime = 5f;
    
    // Kim sıktı? (Kendini vurmasın)
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
        // Belirli süre sonra yok et (performans)
        Destroy(gameObject, lifeTime);
    }

    public void Launch(Vector2 direction)
    {
        if (rb != null)
        {
            rb.linearVelocity = direction.normalized * speed;
            
            // Merminin yönüne dönmesi için rotasyon ayarı
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Kendine çarpma
        if (owner != null && other.gameObject == owner) return;

        // Duvara çarpma (Layer check is safer than Tag)
        if (other.gameObject.layer == LayerMask.NameToLayer("Environment"))
        {
            DestroyProjectile();
            return;
        }

        // Oyuncuya veya Düşmana çarpma
        IDamageable damageable = other.GetComponent<IDamageable>();
        
        if (damageable != null)
        {
            // Dost atesi (Friendly Fire) Kontrolu
            bool isHittingEnemy = other.CompareTag("Enemy");
            
            // Eger oyuncu tarafindan sekmediyse ve bir dusmana (kendisi olmayan) carptiysa, icinden gec (hicbir sey yapma)
            if (isHittingEnemy && !hasBeenDeflected)
            {
                return;
            }

            // Eğer Oyuncuya çarptıysa ve Parry yapıyorsa -> DEFLECT
            if (other.CompareTag("Player") && !hasBeenDeflected)
            {
                ParrySystem parry = other.GetComponent<ParrySystem>();
                if (parry != null && parry.TryDeflect(transform.position)) // Konum tabanlı parry
                {
                    Deflect(other.gameObject);
                    return; // Zarar verme, yok olma
                }
            }

            // Normal Hasar
            damageable.TakeDamage(damage);
            DestroyProjectile();
        }
    }

    public void Deflect(GameObject newOwner)
    {
        if (hasBeenDeflected) return;
        
        hasBeenDeflected = true;
        // Yönü ters çevir
        if (rb != null)
        {
            rb.linearVelocity = -rb.linearVelocity * 1.5f; // Hızlanarak geri dönsün
            transform.rotation = Quaternion.Euler(0, 0, transform.rotation.eulerAngles.z + 180f);
        }

        // Sahibi değiştir (Artık düşmanı vurabilir)
        owner = newOwner;
        
        // Ömrünü uzat
        lifeTime += 2f; 
        
        // Renk değiştir (Görsel feedback)
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = Color.yellow;
        

    }

    private void DestroyProjectile()
    {
        // İleride buraya patlama efekti (VFX) ekleyeceğiz
        Destroy(gameObject);
    }
}

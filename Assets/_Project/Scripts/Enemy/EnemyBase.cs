using UnityEngine;

public abstract class EnemyBase : MonoBehaviour, IDamageable
{
    [Header("Base Settings")]
    public EnemySO enemyData;

    protected float currentHealth;
    protected bool isStunned;

    protected virtual void Start()
    {
        if (enemyData != null)
            currentHealth = enemyData.maxHealth;
        else
            currentHealth = 100f; // Default

        // Fiziksel kaymayi onlemek icin Drag ekle
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.linearDamping = 10f; // Surtunme (Eski surumlerde .drag, yenilerde .linearDrag)
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        // Tempo görsel efektini otomatik ekle (yoksa)
        if (GetComponent<TempoEnemyEffect>() == null)
            gameObject.AddComponent<TempoEnemyEffect>();
    }

    public virtual void TakeDamage(float damageAmount)
    {
        currentHealth -= damageAmount;


        // Hasar Yazisi (Visual Feedback)
        if (DamagePopupManager.Instance != null)
        {
             // Hafif varyasyonlu pozisyon (ustuste binmesin diye)
             Vector3 randomOffset = new Vector3(Random.Range(-0.3f, 0.3f), 0.5f, 0);
             DamagePopupManager.Instance.Create(transform.position + randomOffset, (int)damageAmount, false);
             
             // Vurus Efekti (Hit Particle)
             DamagePopupManager.Instance.CreateHitParticle(transform.position);
        }
        
        // Beyaz Flash Efekti
        var flash = GetComponent<HitFlash>();
        if (flash != null) flash.Flash();

        Stun(0.2f); // Her vurus hafif sersemletir (Micro-stun)

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public virtual void Stun(float duration)
    {
        if (isStunned) return;
        StartCoroutine(StunRoutine(duration));
    }

    protected virtual System.Collections.IEnumerator StunRoutine(float duration)
    {
        isStunned = true;
        // Visual feedback for stun can go here
        yield return new WaitForSeconds(duration);
        isStunned = false;
    }

    [Header("Effects")]
    [SerializeField] private GameObject deathVFX;

    /// <summary>
    /// Ölüm animasyonu için Destroy gecikmesi (saniye).
    /// Animasyonlu düşmanlar Start()'ta bu değeri kendi clip sürelerine göre ayarlar.
    /// 0 = anında yok ol (varsayılan, animasyonsuz düşmanlar için).
    /// </summary>
    protected float deathDelay = 0f;

    /// <summary>
    /// Die() içinde Destroy çağrılmadan hemen önce tetiklenir.
    /// Override'da: death anim trigger, collider disable, velocity sıfırlama vb. yap.
    /// </summary>
    protected virtual void OnDeathAnimationStart() { }

    protected virtual void Die()
    {
        if (RoomManager.Instance != null)
            RoomManager.Instance.OnEnemyDied(gameObject);

        // --- ALTIN DÜŞÜR ---
        if (enemyData != null && enemyData.goldDrop > 0 && EconomyManager.Instance != null)
        {
            EconomyManager.Instance.AddRunGold(enemyData.goldDrop);
        }

        // Death VFX
        if (deathVFX != null)
        {
            GameObject vfx = Instantiate(deathVFX, transform.position, Quaternion.identity);
            Destroy(vfx, 3f);
        }

        // Animasyon hook'u tetikle, ardından gecikmeli yok et (deathDelay=0 ise anında)
        OnDeathAnimationStart();
        Destroy(gameObject, deathDelay);
    }
}

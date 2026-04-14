using UnityEngine;

public abstract class EnemyBase : MonoBehaviour, IDamageable
{
    [Header("Base Settings")]
    public EnemySO enemyData;
    [Header("Stun Feedback")]
    [SerializeField] protected Color stunTintColor = new Color(1f, 0.55f, 0.15f, 1f);

    protected float currentHealth;
    protected bool isStunned;
    protected SpriteRenderer stunSpriteRenderer;
    protected Color stunOriginalColor = Color.white;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => enemyData != null ? enemyData.maxHealth : 100f;
    public float HealthPercent => MaxHealth > 0f ? currentHealth / MaxHealth : 0f;

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

        stunSpriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (stunSpriteRenderer != null)
            stunOriginalColor = stunSpriteRenderer.color;
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
        if (stunSpriteRenderer != null)
        {
            stunOriginalColor = stunSpriteRenderer.color;
            stunSpriteRenderer.color = stunTintColor;
        }

        yield return new WaitForSeconds(duration);

        if (stunSpriteRenderer != null)
            stunSpriteRenderer.color = stunOriginalColor;

        isStunned = false;
    }

    [Header("Effects")]
    [SerializeField] private GameObject deathVFX;
    private LineRenderer perkMarkerLine;
    private static Material perkMarkerMaterial;

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
    public void SetPerkMarker(bool active, Color color)
    {
        if (!active)
        {
            if (perkMarkerLine != null)
                perkMarkerLine.enabled = false;
            return;
        }

        EnsurePerkMarker();
        perkMarkerLine.enabled = true;
        perkMarkerLine.startColor = color;
        perkMarkerLine.endColor = color;
    }

    private void EnsurePerkMarker()
    {
        if (perkMarkerLine != null) return;

        GameObject markerObj = new GameObject("PerkMarker");
        markerObj.transform.SetParent(transform, false);
        markerObj.transform.localPosition = new Vector3(0f, 1.1f, 0f);

        perkMarkerLine = markerObj.AddComponent<LineRenderer>();
        perkMarkerLine.useWorldSpace = false;
        perkMarkerLine.loop = true;
        perkMarkerLine.positionCount = 20;
        perkMarkerLine.widthMultiplier = 0.04f;
        perkMarkerLine.numCapVertices = 4;
        perkMarkerLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        perkMarkerLine.receiveShadows = false;
        perkMarkerLine.textureMode = LineTextureMode.Stretch;
        perkMarkerLine.alignment = LineAlignment.TransformZ;
        perkMarkerLine.sortingOrder = 100;

        if (perkMarkerMaterial == null)
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null)
                perkMarkerMaterial = new Material(shader);
        }

        if (perkMarkerMaterial != null)
            perkMarkerLine.material = perkMarkerMaterial;

        float radius = 0.12f;
        for (int i = 0; i < perkMarkerLine.positionCount; i++)
        {
            float t = (i / (float)perkMarkerLine.positionCount) * Mathf.PI * 2f;
            perkMarkerLine.SetPosition(i, new Vector3(Mathf.Cos(t) * radius, Mathf.Sin(t) * radius, 0f));
        }
    }
}

using UnityEngine;
using System.Collections;

/// <summary>
/// Yerde deaktif bekleyen, oyuncu yaklaşınca (tetiklenme) 0.3s sonra patlayan mayın (Landmine).
/// </summary>
public class TrapArea : MonoBehaviour
{
    [Header("Trap Settings")]
    [Tooltip("Tetiklenme (Trigger) Alanı Yarıçapı")]
    public float triggerRadius = 1.5f;
    [Tooltip("Patlama (Damage/Debuff) Alanı Yarıçapı")]
    public float explosionRadius = 1.8f;
    [Tooltip("Tetiklendikten sonra patlaması için gereken süre (Dash ile kaçabilmek için)")]
    public float activationDelay = 0.3f;
    
    [Header("Debuff Settings (Patlama Tuttuğunda)")]
    [Tooltip("Hız çarpanı (Örn: 0.5 = %50 yavaşlar)")]
    [Range(0.1f, 1.0f)]
    public float speedMultiplier = 0.5f;
    public float slowDuration = 3f;
    
    public float poisonDamagePerTick = 5f;
    public int poisonTicks = 3;
    public float timeBetweenTicks = 1f;

    private bool isTriggered = false;
    private CircleCollider2D triggerCol;
    private LineRenderer lr;

    private void Start()
    {
        // Kendi trigger collider'ını ayarla (Tetiklenme alanı)
        triggerCol = GetComponent<CircleCollider2D>();
        if (triggerCol == null)
            triggerCol = gameObject.AddComponent<CircleCollider2D>();
            
        triggerCol.isTrigger = true;
        triggerCol.radius = triggerRadius;
        
        // Patlama Çemberini Oyunda Göstermek İçin LineRenderer Ekle
        CreateExplosionIndicator();
    }

    private void CreateExplosionIndicator()
    {
        lr = gameObject.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.loop = true;
        lr.startWidth = 0.05f;
        lr.endWidth = 0.05f;
        lr.positionCount = 100; // Pürüzsüz çember için yeterli

        // Tehlikeli Kırmızı Renk
        lr.startColor = new Color(1f, 0f, 0f, 0.5f);
        lr.endColor = new Color(1f, 0f, 0f, 0.5f);
        lr.material = new Material(Shader.Find("Sprites/Default"));

        lr.sortingOrder = -1;

        // Başlangıçta tetiklenme alanıyla aynı boyutta çiz
        DrawCircle(triggerRadius);
    }

    private void DrawCircle(float radius)
    {
        if (lr == null) return;
        
        float angleStep = 360f / lr.positionCount;
        for (int i = 0; i < lr.positionCount; i++)
        {
            float rad = Mathf.Deg2Rad * (i * angleStep);
            float x = Mathf.Cos(rad) * radius;
            float y = Mathf.Sin(rad) * radius;
            lr.SetPosition(i, new Vector3(x, y, 0));
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isTriggered) return;

        if (collision.CompareTag("Player"))
        {
            StartCoroutine(ActivationRoutine());
        }
    }

    /// <summary>
    /// Tetiklendiğinde delay başlar. Delay boyunca çember merkezden büyür.
    /// Delay bitişinde patlama alanındaki oyuncuyu kontrol eder.
    /// Eğer oyuncu ACTIVATION SÜRESİ İÇİNDE herhangi bir an dodge attıysa, o tuzaktan muaf olur.
    /// </summary>
    // Dodge süresi sabitini dışarıdan eşitlemek için — PlayerController.dodgeDuration ile aynı olmalı
    [Header("Dodge Koruma Ayarı")]
    [Tooltip("PlayerController'daki dodgeDuration ile aynı değer olmalı (varsayılan 0.22f)")]
    public float dodgeDuration = 0.22f;

    private IEnumerator ActivationRoutine()
    {
        isTriggered = true;

        // Tetiklenme anındaki zamanı kaydet
        float activationStartTime = Time.time;
        
        SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null) sr.color = Color.red;

        // 1. Gecikme süresi boyunca çemberin büyüme efekti (Merkezden başlayarak)
        float timer = 0f;
        while (timer < activationDelay)
        {
            timer += Time.deltaTime;
            float currentRad = Mathf.Lerp(0f, explosionRadius, timer / activationDelay);
            DrawCircle(currentRad);
            yield return null;
        }

        // 2. Patlama anı kontrolü
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius);
        bool hitPlayer = false;
        PlayerController player = null;
        IDamageable playerDmg = null;

        foreach (var hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                player = hit.GetComponent<PlayerController>();
                if (player == null) continue;

                // Dodge Koruma Kontrolü:
                // Oyuncu şu an invulnerable mi? → Korunur
                // VEYA son dodge'unu YETERİNCE YAKIN zamanda attı mı?
                // Yani: dodge başlangıcından bu yana geçen süre <= activationDelay + dodgeDuration
                // Bu formül şunu garantiler: oyuncu trapı tetiklediği anda ya da tetiklenmeden
                // kısa süre önce dodge attıysa, dodge'un invuln süresi tuzak patlamasını kapsıyor demektir.
                float timeSinceDodge = player.GetTimeSinceDodgeStart();
                bool dodgeProtected = player.IsInvulnerable || timeSinceDodge <= (activationDelay + dodgeDuration);

                if (dodgeProtected)
                {
                    continue;
                }

                hitPlayer = true;
                playerDmg = hit.GetComponent<IDamageable>();
                break;
            }
        }

        // 3. Görselleri kapat
        if (sr != null) sr.enabled = false;
        if (triggerCol != null) triggerCol.enabled = false;

        // 4. Patlama sonrası çemberin küçülerek kaybolması (shrink efekti)
        // Fade-out shader sorunu yaşandığı için shrink ile çözülüyor
        if (lr != null)
        {
            float shrinkTimer = 0f;
            float shrinkDuration = 0.15f;
            while (shrinkTimer < shrinkDuration)
            {
                shrinkTimer += Time.deltaTime;
                float shrinkRadius = Mathf.Lerp(explosionRadius, 0f, shrinkTimer / shrinkDuration);
                DrawCircle(shrinkRadius);
                yield return null;
            }
            lr.enabled = false;
        }

        // 5. Sonuç
        if (hitPlayer && player != null)
        {
            StartCoroutine(DebuffRoutine(player, playerDmg));
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private IEnumerator DebuffRoutine(PlayerController p, IDamageable pDmg)
    {
        // 1. Slow cezasını ver
        if (p != null) p.speedMultiplier *= speedMultiplier;

        // 2. Zehir hasarı döngüsü
        float timer = 0f;
        int ticksDone = 0;
        float nextTickTime = 0f;

        while (timer < slowDuration || ticksDone < poisonTicks)
        {
            if (ticksDone < poisonTicks && timer >= nextTickTime)
            {
                if (pDmg != null) pDmg.TakeDamage(poisonDamagePerTick);
                ticksDone++;
                nextTickTime += timeBetweenTicks;
            }

            timer += Time.deltaTime;
            yield return null;
        }

        // Cezayı temizle
        if (p != null) p.speedMultiplier /= speedMultiplier;

        Destroy(gameObject);
    }
    
    // Editör üzerinde tetik ve patlama kısımlarını görmek için
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, triggerRadius);
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}

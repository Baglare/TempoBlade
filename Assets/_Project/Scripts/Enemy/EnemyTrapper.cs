using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Haritada sürekli hızlıca rastgele noktalara hareket eden ve arkasında mayın (TrapArea) bırakan düşman.
/// Limitli sayıda tuzak bırakır, böylece harita tıkılmaz.
/// Oda temizlendiğinde bıraktığı tüm tuzaklar otomatik yok olur.
/// </summary>
public class EnemyTrapper : EnemyBase
{
    [Header("Trapper - Random Roam")]
    public float moveSpeedModifier = 5f;     // Hızlı gezecek
    public float roamRadius = 10f;           // Gezme yarıçapı
    private Vector2 targetRoamPos;
    
    [Header("Trap Spawn")]
    public GameObject trapPrefab;            
    public float trapSpawnInterval = 8f;     // Bırakma süresi (örn 8sn)
    public int maxTraps = 5;                 // Haritada aynı anda maksimum olabilecek tuzak sayısı
    [Tooltip("Mayın çakışma engeli: Yakındaki mayına bu mesafeden daha yakına kuramazsın")]
    public float minTrapDistance = 2f;

    private float nextTrapTime;
    private bool isDead = false;
    private List<TrapArea> activeTraps = new List<TrapArea>();
    
    // Sıkışma Algılama
    private float stuckTimer = 0f;
    private Vector2 lastPosition;
    
    // Animasyon Baglantilari
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    protected override void Start()
    {
        base.Start();
        
        animator = GetComponentInChildren<Animator>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        PickNewRoamPosition();
        lastPosition = transform.position;
        
        // İlk tuzak kısa bir sure sonra
        nextTrapTime = Time.time + 2f;
        deathDelay = 1.0f;
    }

    private void Update()
    {
        if (isDead || isStunned) return;

        float speed = enemyData != null ? enemyData.moveSpeed : moveSpeedModifier;

        // Rastgele Noktaya Hareket (Oyuncuyu takmayan Random Roam)
        float dist = Vector2.Distance(transform.position, targetRoamPos);
        if (dist < 0.5f)
        {
            PickNewRoamPosition();
        }
        else
        {
            // Basit, sağlam hareket (transform.MoveTowards) — Rigidbody2D fizik sorunlarından kaçınılır 
            transform.position = Vector2.MoveTowards(transform.position, targetRoamPos, speed * Time.deltaTime);
            
            if (spriteRenderer != null)
                spriteRenderer.flipX = targetRoamPos.x < transform.position.x;
                
            if (animator != null)
                animator.SetBool("IsMoving", true);
        }

        // --- Sıkışma Algılama ---
        float movedDist = Vector2.Distance(transform.position, lastPosition);
        if (movedDist < 0.02f)
        {
            stuckTimer += Time.deltaTime;
            if (stuckTimer > 0.4f)
            {
                PickNewRoamPosition();
                stuckTimer = 0f;
            }
        }
        else
        {
            stuckTimer = 0f;
        }
        lastPosition = transform.position;

        // Listede yokedilmis (patlamis) mayınlar varsa temizle
        activeTraps.RemoveAll(t => t == null);

        // Tuzak Limitine Ulaşmadıysa ve Zamanı Geldiyse Tuzak Kur
        if (Time.time >= nextTrapTime)
        {
            if (activeTraps.Count < maxTraps)
            {
                TrySpawnTrap();
            }
            nextTrapTime = Time.time + trapSpawnInterval;
        }
    }

    private void PickNewRoamPosition()
    {
        int maxAttempts = 15;
        Collider2D myCol = GetComponent<Collider2D>();

        for (int i = 0; i < maxAttempts; i++)
        {
            Vector2 randomDir = Random.insideUnitCircle.normalized;
            float randomDist = Random.Range(2f, roamRadius);
            Vector2 candidate = (Vector2)transform.position + randomDir * randomDist;

            // Raycast ile yolda engel var mı kontrol et
            // Kendi collider'ımızı görmezden gelmek için kendimizi geçici olarak kapatıyoruz
            bool wasEnabled = myCol != null && myCol.enabled;
            if (myCol != null) myCol.enabled = false;

            RaycastHit2D hit = Physics2D.Raycast(transform.position, randomDir, randomDist);

            if (myCol != null) myCol.enabled = wasEnabled;

            // Çarptığı şey yoksa veya trigger ise (mayın gibi) → güvenli
            if (hit.collider == null || hit.collider.isTrigger)
            {
                targetRoamPos = candidate;
                return;
            }
        }

        // Hiçbir yere gidemiyorsa tersine dönmeyi dene
        targetRoamPos = (Vector2)transform.position + Random.insideUnitCircle.normalized * 2f;
    }

    /// <summary>
    /// Mayın spawn etmeden önce yakınlarda başka mayın olup olmadığını kontrol eder.
    /// Eğer çok yakında mayın varsa, spawn süresini 1 saniye erteleyerek geri çekilir.
    /// </summary>
    private void TrySpawnTrap()
    {
        if (trapPrefab == null) return;

        // Yakınlarda başka mayın var mı kontrol et (iç içe geçmeyi engelle)
        foreach (var trap in activeTraps)
        {
            if (trap == null) continue;
            float distToTrap = Vector2.Distance(transform.position, trap.transform.position);
            if (distToTrap < minTrapDistance)
            {
                // Çok yakında mayın var, 1 saniye ertele
                nextTrapTime = Time.time + 1f;
                return;
            }
        }

        // Güvenli, mayını bırak
        if (animator != null) animator.SetTrigger("Attack"); 

        GameObject spawned = Instantiate(trapPrefab, transform.position, Quaternion.identity);
        TrapArea t = spawned.GetComponent<TrapArea>();
        if (t != null)
        {
            activeTraps.Add(t);
        }
    }

    /// <summary>
    /// Tüm aktif tuzakları yok eder. Oda temizlendiğinde çağrılır.
    /// </summary>
    public void DestroyAllTraps()
    {
        foreach (var trap in activeTraps)
        {
            if (trap != null)
            {
                Destroy(trap.gameObject);
            }
        }
        activeTraps.Clear();
    }

    public override void TakeDamage(float amount)
    {
        if (isDead) return;
        base.TakeDamage(amount);

        if (!isDead && animator != null)
            animator.SetTrigger("TakeHit");
    }

    /// <summary>
    /// Ölüm — artık ölüm mayını bırakmıyor, sadece temiz bir ölüm.
    /// Tüm aktif tuzakları da yok eder (oda temizlenmesine katkıda bulunur).
    /// </summary>
    protected override void OnDeathAnimationStart()
    {
        isDead = true;
        
        var rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;

        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        if (animator != null) animator.SetTrigger("Die");
        // Tuzaklar sahada kalır, sadece oda temizlendiğinde (RoomManager) yok olur
    }
}

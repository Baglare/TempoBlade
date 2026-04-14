using System.Collections;
using UnityEngine;

public class EnemyMelee : EnemyBase
{
    [Header("Melee Settings")]
    // public float approachDistance; // Artik SO'dan (enemyData.attackRange) aliyoruz
    public AttackHitbox hitboxScript;
    public Collider2D hitboxCollider;

    [Header("Arc Visual")]
    [Tooltip("WeaponArcVisual component'i. Enemy altındaki child'a eklenir.")]
    public WeaponArcVisual weaponArcVisual;
    
    private Transform player;
    private bool isAttacking;

    protected override void Start()
    {
        base.Start();
        var p = FindFirstObjectByType<PlayerController>();
        if (p != null) player = p.transform;

        if (hitboxScript != null) 
        {
            hitboxScript.owner = this;
        }

        if (hitboxCollider != null && enemyData != null)
        {
            hitboxCollider.enabled = false;

            // Hitbox'i Trait'e gore ayarla (Basit bir yaklasim)
            // Eger BoxCollider2D ise:
            if (hitboxCollider is BoxCollider2D box)
            {
                // Menzil kadar uzaga uzat
                box.size = new Vector2(enemyData.attackRange, 1f);
                box.offset = new Vector2(enemyData.attackRange / 2f, 0f);
            }
        }

        if (weaponArcVisual != null && enemyData != null)
            weaponArcVisual.range = enemyData.attackRange;
    }

    private void Update()
    {
        // Kılıç/yay görselini her frame güncelle (stun olsa bile yön korunur)
        if (weaponArcVisual != null && player != null && enemyData != null)
        {
            Vector2 dirToPlayer = ((Vector2)player.position - (Vector2)transform.position).normalized;
            weaponArcVisual.UpdateVisuals(transform.position, dirToPlayer, isAttacking, false);
        }

        if (isStunned || player == null || isAttacking || enemyData == null) return;

        float dist = Vector2.Distance(transform.position, player.position);

        if (dist > enemyData.detectionRange)
        {
            // Idle
        }
        else if (dist > enemyData.attackRange) // SO'dan gelen veriyi kullan
        {
            transform.position = Vector2.MoveTowards(transform.position, player.position, enemyData.moveSpeed * Time.deltaTime);
            
            // Yone gore donme (Sprite Renderer Flip)
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
        
        // Windup (Telegraph)
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = Color.yellow; // Gecici efekt
        
        // Saldiri yonunu belirle (Oyuncuya don)
        if (player != null && hitboxCollider != null)
        {
            Vector3 dir = (player.position - transform.position).normalized;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            hitboxCollider.transform.rotation = Quaternion.Euler(0, 0, angle);
        }

        float windupTime = 0.4f;
        if (TempoManager.Instance != null)
        {
            var tier = TempoManager.Instance.CurrentTier;
            if (tier == TempoManager.TempoTier.T2) windupTime = 0.6f; // Panik: daha gec saldirir
            if (tier == TempoManager.TempoTier.T3) windupTime = 0.8f;
        }
        yield return new WaitForSeconds(windupTime); // Dinamik Windup suresi

        // Attack
        if (sr != null) sr.color = Color.red;
        if (hitboxCollider != null) hitboxCollider.enabled = true;

        yield return new WaitForSeconds(0.2f); // Active frames

        if (hitboxCollider != null) hitboxCollider.enabled = false;
        if (sr != null) sr.color = Color.white;

        yield return new WaitForSeconds(enemyData.attackCooldown);
        isAttacking = false;
    }

    // EnemyBase'den gelen Metodlar
    public override void Stun(float duration)
    {
        // Temel dusmanlar (Sürü) oyuncu T2 veya T3'teyken panikler ve çok daha uzun sure sersemler.
        float finalDuration = duration;
        if (TempoManager.Instance != null)
        {
            var tier = TempoManager.Instance.CurrentTier;
            if (tier == TempoManager.TempoTier.T2) finalDuration *= 1.5f; // %50 Daha uzun stun
            if (tier == TempoManager.TempoTier.T3) finalDuration *= 2.0f; // 2x Daha uzun stun
        }
        
        base.Stun(finalDuration);
        isAttacking = false;
        if (hitboxCollider != null) hitboxCollider.enabled = false;
    }
}

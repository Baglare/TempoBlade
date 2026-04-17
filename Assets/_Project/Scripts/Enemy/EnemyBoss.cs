using UnityEngine;
using System.Collections;

public class EnemyBoss : EnemyBase, IParryReactive
{
    [Header("Boss Settings")]
    public float phase2HealthThreshold = 0.5f; // %50 Can
    public float moveSpeed = 3f;
    public float dashSpeed = 15f;
    
    [Header("Phase 1: Melee")]
    public float meleeAttackRange = 2f;
    public float meleeAttackCooldown = 0.5f;
    public float meleeWindup = 0.8f;
    public int comboCount = 3;

    [Header("Arc Visual")]
    [Tooltip("WeaponArcVisual component'i. Boss altındaki child'a eklenir. Sadece Phase 1 melee için kullanılır.")]
    public WeaponArcVisual weaponArcVisual;
    
    [Header("Phase 2: Bullet Hell")]
    public GameObject projectilePrefab;
    public Transform firePoint;
    public int bulletBurstCount = 8;
    public float timeBetweenBursts = 0.2f;
    
    // Components
    private Transform player;
    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private float maxHealth;
    
    // State
    private enum BossState { Intro, Idle, Chase, MeleeCombo, SprintDash, PhaseTransition, BulletHell, Stunned, Dead }
    private BossState currentState = BossState.Intro;
    private bool isPhase2 = false;
    private float nextAttackTime;
    
    private bool isMeleeAttacking = false;
    private Coroutine parryInterruptRoutine;

    public bool AllowParryExecute => false;

    protected override void Start()
    {
        base.Start();
        
        // Eger Inspector'dan dusman degeri verilmemisse, boss iicn yuksek bir deger ata
        if (enemyData == null) maxHealth = 1000f; 
        else maxHealth = enemyData.maxHealth;
        
        currentHealth = maxHealth;
        
        GameObject pObj = GameObject.FindGameObjectWithTag("Player");
        if (pObj != null) player = pObj.transform;
        
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponentInChildren<SpriteRenderer>();

        if (weaponArcVisual != null)
            weaponArcVisual.range = meleeAttackRange;

        StartCoroutine(IntroRoutine());
    }

    private void Update()
    {
        // Temel durumlarda veya saldiri halindeyken Update'i kes (Crash/Sonsuz Dongu onlemi)
        if (currentState == BossState.Dead || currentState == BossState.Intro || 
            currentState == BossState.PhaseTransition || currentState == BossState.Stunned ||
            currentState == BossState.MeleeCombo || currentState == BossState.SprintDash ||
            currentState == BossState.BulletHell) return;
            
        // AI Logic
        if (player == null) return;

        // Kılıç/yay görselini Phase 1 melee için güncelle
        if (weaponArcVisual != null && !isPhase2)
        {
            Vector2 dirToPlayer = ((Vector2)player.position - (Vector2)transform.position).normalized;
            weaponArcVisual.UpdateVisuals(transform.position, dirToPlayer, isMeleeAttacking, false);
        }

        FacePlayer();
        
        float dist = Vector2.Distance(transform.position, player.position);
        
        if (Time.time >= nextAttackTime)
        {
            ChooseAttack(dist);
        }
        else if (currentState == BossState.Chase || currentState == BossState.Idle)
        {
            // Hareket Mantigi
            if (dist > meleeAttackRange && !isPhase2)
            {
                currentState = BossState.Chase;
                Vector2 dir = (player.position - transform.position).normalized;
                rb.linearVelocity = dir * moveSpeed;
            }
            else if (isPhase2)
            {
                // Faz 2'de mesafeyi korumaya calis
                if (dist < 4f)
                {
                    Vector2 dir = (transform.position - player.position).normalized;
                    rb.linearVelocity = dir * (moveSpeed * 1.5f); // Kacinma
                }
                else
                {
                    rb.linearVelocity = Vector2.zero;
                    currentState = BossState.Idle; // Kacinma bittiginde dur
                }
            }
            else
            {
                rb.linearVelocity = Vector2.zero;
                currentState = BossState.Idle;
            }
        }
    }
    
    private void FacePlayer()
    {
        if (player == null) return;
        Vector3 sc = transform.localScale;
        sc.x = player.position.x < transform.position.x ? -Mathf.Abs(sc.x) : Mathf.Abs(sc.x);
        transform.localScale = sc;
    }

    private void ChooseAttack(float distToPlayer)
    {
        rb.linearVelocity = Vector2.zero; // Dur
        
        if (!isPhase2)
        {
            // Phase 1 Attacks
            if (distToPlayer <= meleeAttackRange)
            {
                StartCoroutine(MeleeComboRoutine());
            }
            else
            {
                StartCoroutine(SprintDashRoutine());
            }
        }
        else
        {
            // Phase 2 Attacks
            StartCoroutine(BulletBurstRoutine());
        }
    }

    // --- PHASE 1 ATTACKS ---
    private IEnumerator MeleeComboRoutine()
    {
        currentState = BossState.MeleeCombo;
        FacePlayer();
        
        // Tempo bazli hizlanma
        float windup = meleeWindup;
        float cooldown = meleeAttackCooldown;
        if (TempoManager.Instance != null && TempoManager.Instance.CurrentTier >= TempoManager.TempoTier.T2)
        {
            windup *= 0.7f; // %30 daha hizli vurur
            cooldown *= 0.8f;
        }

        for (int i = 0; i < comboCount; i++)
        {
            if (currentHealth <= 0 || isPhase2) yield break; // Boss olurse veya faz gecerse iptal
            
            // Windup: Ozel renk (Sari) - Parry isareti
            if (sr != null) sr.color = Color.yellow;
            yield return new WaitForSeconds(windup);
            
            // Attack Strike (Kirmizi)
            if (sr != null) sr.color = Color.red;
            
            // Kucuk ileri atilma (Lunge)
            if (player != null)
            {
                 Vector2 dir = (player.position - transform.position).normalized;
                 
                 float oldDamping = rb.linearDamping;
                 rb.linearDamping = 0f; // Surtunmeyi gecici olarak kaldir
                 rb.linearVelocity = dir * (moveSpeed * 6f);
                 
                 // Hasari fizik motoruyla (OnCollisionStay2D / OnTriggerStay2D) ver
                 isMeleeAttacking = true;
                 
                 yield return new WaitForSeconds(0.2f);
                 
                 isMeleeAttacking = false;
                 rb.linearDamping = oldDamping; // Surtunmeyi geri ver
            }
            else
            {
                 isMeleeAttacking = true;
                 yield return new WaitForSeconds(0.2f);
                 isMeleeAttacking = false;
            }
            
            // Revert
            if (sr != null) sr.color = Color.white;
            rb.linearVelocity = Vector2.zero;
            
            yield return new WaitForSeconds(0.3f); // Vuruslar arasi bosluk
        }
        
        nextAttackTime = Time.time + cooldown;
        currentState = BossState.Idle;
    }
    
    private IEnumerator SprintDashRoutine()
    {
        currentState = BossState.SprintDash;
        FacePlayer();
        
        // Windup
        if (sr != null) sr.color = new Color(1f, 0.5f, 0f); // Turuncu
        yield return new WaitForSeconds(0.6f);
        
        // Dash!
        if (player != null)
        {
            Vector2 dir = (player.position - transform.position).normalized;
            
            float oldDamping = rb.linearDamping;
            rb.linearDamping = 0f; // Surtunmeyi tamamen sifirla ki uzağa uçabilsin
            
            rb.linearVelocity = dir * dashSpeed;
            
            if (sr != null) sr.color = Color.red;
            
            isMeleeAttacking = true;
            yield return new WaitForSeconds(0.3f); // Dash suresi icinde physics motoru (OnCollisionStay2D) hasari verecek
            isMeleeAttacking = false;
            
            rb.linearDamping = oldDamping; // Surtunmeyi geri ver
            
            rb.linearVelocity = Vector2.zero;
        }
        
        if (sr != null) sr.color = Color.white;
        nextAttackTime = Time.time + meleeAttackCooldown;
        currentState = BossState.Idle;
    }

    private void OnCollisionEnter2D(Collision2D col) { HandleMeleeHit(col.collider); }
    private void OnCollisionStay2D(Collision2D col) { HandleMeleeHit(col.collider); }
    private void OnTriggerEnter2D(Collider2D col) { HandleMeleeHit(col); }
    private void OnTriggerStay2D(Collider2D col) { HandleMeleeHit(col); }

    // Unity fizik sistemi (Radar yerine) eger Boss oyuncuya degerse ve 'isMeleeAttacking' aciksa hasar verir
    private void HandleMeleeHit(Collider2D other)
    {
        if (!isMeleeAttacking) return;
        if (!other.CompareTag("Player")) return;

        var parry = other.GetComponent<ParrySystem>();
        if (parry != null && parry.TryParry(gameObject))
        {
            isMeleeAttacking = false; // Parry yenmisse bu saldiriyi hemen kes
            return;
        }

        var playerCombat = other.GetComponent<PlayerCombat>();
        var playerController = other.GetComponent<PlayerController>();
        if (playerController != null && playerController.IsInvulnerable)
        {
            other.GetComponent<DashPerkController>()?.NotifyMeleeDodged(this);
            return;
        }

        if (playerCombat != null)
        {
             float dmg = enemyData != null ? enemyData.damage : 10f;
             playerCombat.TakeDamage(dmg); 
             // TakeDamage icinde oyuncu i-frame aldigi icin pes pese 100 kere hasar yemeyecektir
        }
    }

    // --- PHASE TRANSITION ---
    private IEnumerator PhaseTransitionRoutine()
    {
        currentState = BossState.PhaseTransition;
        isPhase2 = true;

        // Phase 2'de melee kılıcı/yayı gizle
        if (weaponArcVisual != null)
            weaponArcVisual.SetVisible(false);
        rb.linearVelocity = Vector2.zero;
        
        // Yenilmezlik ver
        
        if (DamagePopupManager.Instance != null)
            DamagePopupManager.Instance.CreateText(transform.position + Vector3.up * 2f, "PHASE 2!", Color.magenta, 10f);
            
        if (HitStopManager.Instance != null)
            HitStopManager.Instance.PlayHeavyHitStop(); // Ekran sert donsun
            
        // Gorsel bi patlama 
        if (sr != null) sr.color = Color.magenta;
        
        // Dalgayi geri it (T3 Final parry gibi yapilabilir)
        
        yield return new WaitForSeconds(2.0f); // Dönüşüm Raconu
        
        if (sr != null) sr.color = Color.white;
        currentState = BossState.Idle;
        nextAttackTime = Time.time + 1f; // Hemen vurmasin
    }

    // --- PHASE 2 ATTACKS ---
    private IEnumerator BulletBurstRoutine()
    {
        currentState = BossState.BulletHell;
        FacePlayer();
        AudioManager.Play(AudioEventId.EnemyBossBulletBurst, gameObject);
        
        if (sr != null) sr.color = Color.cyan;
        yield return new WaitForSeconds(0.5f);
        
        if (projectilePrefab != null && firePoint != null)
        {
            float angleStep = 360f / bulletBurstCount;
            float currentAngle = 0f;
            
            for (int i = 0; i < bulletBurstCount; i++)
            {
                // Mermileri etrafa sac
                float dirX = Mathf.Cos(currentAngle * Mathf.Deg2Rad);
                float dirY = Mathf.Sin(currentAngle * Mathf.Deg2Rad);
                Vector2 bulletDir = new Vector2(dirX, dirY);
                
                // Rotasyon hizalama
                Quaternion bulletRot = Quaternion.Euler(0, 0, currentAngle);
                
                GameObject proj = Instantiate(projectilePrefab, firePoint.position, bulletRot);
                BossProjectile bp = proj.GetComponent<BossProjectile>();
                if (bp != null)
                {
                    bp.Fire(bulletDir, gameObject);
                }
                
                currentAngle += angleStep;
            }
        }
        else
        {
            Debug.LogWarning("Boss Projectile Prefab eksik!");
        }
        
        if (sr != null) sr.color = Color.white;
        
        float cd = timeBetweenBursts;
        if (TempoManager.Instance != null && TempoManager.Instance.CurrentTier >= TempoManager.TempoTier.T2)
        {
            cd *= 0.6f; // Hizli tempo oyuncusuna mermi yagmuru
        }
        
        nextAttackTime = Time.time + cd;
        currentState = BossState.Idle;
    }

    // --- CORE & DAMAGE ---
    private IEnumerator IntroRoutine()
    {
        // Kisa bi baslangic beklemesi
        if (sr != null) sr.color = Color.gray;
        yield return new WaitForSeconds(1.5f);
        if (sr != null) sr.color = Color.white;
        currentState = BossState.Idle;
        nextAttackTime = Time.time + 1.0f;
    }

    public override void TakeDamage(float damageAmount)
    {
        if (currentState == BossState.PhaseTransition || currentState == BossState.Intro || currentState == BossState.Dead) return;
        
        // Hasari Uygula 
        currentHealth -= damageAmount;
        
        // Damage Popup Visuals
        if (DamagePopupManager.Instance != null)
        {
             Vector3 rndOff = new Vector3(Random.Range(-0.3f, 0.3f), 0.5f, 0);
             DamagePopupManager.Instance.Create(transform.position + rndOff, (int)damageAmount, false);
             DamagePopupManager.Instance.CreateHitParticle(transform.position);
        }
        var flash = GetComponent<HitFlash>();
        if (flash != null) flash.Flash();
        
        // Kontrol 1: Faz degisimi
        if (!isPhase2 && currentHealth <= maxHealth * phase2HealthThreshold)
        {
            // Faz 2'ye gectiginde kaza kursununa gitmemesi icin canini tam %50'ye sabitliyoruz.
            // Yoksa oyuncunun asiri guclu bir kombosu Boss'u 1 HP'ye dusurur ve 2. faz baslar baslamaz olur.
            currentHealth = maxHealth * phase2HealthThreshold; 
            AudioManager.Play(AudioEventId.EnemyBossPhaseTransition, gameObject);
            
            StopAllCoroutines();
            rb.linearVelocity = Vector2.zero;
            StartCoroutine(PhaseTransitionRoutine());
            return;
        }
        
        // Kontrol 2: Olum
        if (currentHealth <= 0)
        {
            Die();
            return;
        }
        
        // Eger yasiyorsa ve fazi degismemisse ufak sarsinti
        Stun(0.1f);
    }

    protected override System.Collections.IEnumerator StunRoutine(float duration)
    {
        // Phase 2, Olum, Intro, Gecis VE Saldiri Anlarinda (Super Armor) Boss stun yemez!
        if (currentState == BossState.PhaseTransition || currentState == BossState.Intro || 
            currentState == BossState.Dead || isPhase2 ||
            currentState == BossState.MeleeCombo || currentState == BossState.SprintDash ||
            currentState == BossState.BulletHell) 
            yield break;
        
        BossState prevState = currentState;
        currentState = BossState.Stunned;
        
        // Stun Rengi
        if (sr != null) sr.color = new Color(0.5f, 0f, 0.5f); // Mor saskinlik
        rb.linearVelocity = Vector2.zero;
        
        yield return new WaitForSeconds(duration * 0.5f); // Boss'lar stun'dan daha hizli cikar
        
        if (sr != null) sr.color = Color.white;
        currentState = BossState.Idle;
        nextAttackTime = Time.time + 0.5f; // Kendine gelis
    }

    protected override void Die()
    {
        if (currentState == BossState.Dead) return;
        currentState = BossState.Dead;
        
        StopAllCoroutines();
        rb.linearVelocity = Vector2.zero;
        
        if (DamagePopupManager.Instance != null)
            DamagePopupManager.Instance.CreateText(transform.position + Vector3.up * 2f, "BOSS DEFEATED!", Color.yellow, 15f);
            
        base.Die();
    }

    public void OnParryReaction(ParryReactionContext context)
    {
        if (currentState == BossState.Dead || currentState == BossState.Intro || currentState == BossState.PhaseTransition)
            return;

        if (parryInterruptRoutine != null)
            StopCoroutine(parryInterruptRoutine);

        StopAllCoroutines();
        parryInterruptRoutine = StartCoroutine(ParryInterruptRoutine(Mathf.Max(0.05f, context.duration)));
    }

    private IEnumerator ParryInterruptRoutine(float duration)
    {
        currentState = BossState.Stunned;
        isMeleeAttacking = false;

        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        if (sr != null)
            sr.color = new Color(1f, 0.6f, 0.1f);

        yield return new WaitForSeconds(duration);

        if (currentState != BossState.Dead)
        {
            if (sr != null)
                sr.color = Color.white;

            currentState = BossState.Idle;
            nextAttackTime = Time.time + 0.5f;
        }

        parryInterruptRoutine = null;
    }
}

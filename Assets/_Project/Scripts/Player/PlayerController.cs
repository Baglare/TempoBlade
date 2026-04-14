using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public enum PlayerState
    {
        Idle,
        Moving,
        Dodging,
        Parrying,
        DashStriking
    }

    private ParrySystem parrySystem;
    private PlayerCombat playerCombat;
    private DashPerkController dashPerkController;
    
    [Header("Movement")]
    public float moveSpeed = 6f;
    [HideInInspector] public float speedMultiplier = 1.0f; // Tuzak vb. dış etkenler için

    [Header("Dodge")]
    public float dodgeSpeed = 16f;
    public float dodgeDuration = 0.22f;
    public float dodgeCooldown = 0.45f;
    
    [Header("Dodge Trail (Juice)")]
    public GameObject ghostTrailPrefab;
    public float ghostSpawnDelay = 0.05f;
    private float ghostSpawnTimer;
    public Color ghostColor = new Color(0.5f, 0.5f, 1f, 0.5f); // Yarisaydam Mavi
    
    private System.Collections.Generic.List<GhostTrail> activeTrails = new System.Collections.Generic.List<GhostTrail>();

    private Vector2 lastNonZeroMove = Vector2.right;
    private float dodgeCooldownTimer = 0f;
    private float dodgeTimer = 0f;
    private Vector2 dodgeDir;
    public bool IsInvulnerable { get; set; } = false;
    private float lastDodgeStartTime = -999f;
    private float currentDodgeSpeed;
    private float baseDodgeCooldown;
    private float dashCommitmentDodgeCooldownMultiplier = 1f;
    private float parryCommitmentDodgeCooldownMultiplier = 1f;
    private float lastPerfectParryPopupTime = -999f;

    // --- PERK SİSTEMİ EVENT'LERİ ---
    /// <summary>Dodge başladığında yön bilgisiyle tetiklenir. DashPerkController dinler.</summary>
    public event Action<Vector2> OnDodgeStarted;
    /// <summary>Dodge bittiğinde tetiklenir.</summary>
    public event Action OnDodgeEnded;
    /// <summary>Dodge başlangıç pozisyonu (Geri Sıçrama perki için).</summary>
    public Vector2 DodgeStartPos { get; private set; }

    // --- EXTERNAL DASH (DashStrike kombosu için PlayerCombat tarafından çağrılır) ---
    private Vector2 externalDashDir;
    private float   externalDashSpeed;
    private float   externalDashTimer;
    
    // Timed Dodge kontrolu icin kac saniye once dodge atildigini dondurur
    public float GetTimeSinceDodgeStart()
    {
        return Time.time - lastDodgeStartTime;
    }

    private Rigidbody2D rb;
    private Vector2 moveInput;

    public PlayerState currentState { get; private set; } = PlayerState.Idle;

    [Header("Tempo Debug (Sadece Test Icin)")]
    [Tooltip("Tempoyu istediginiz degere getirmek icin degeri ayarlayip alttaki butonu isaretleyin.")]
    [Range(0f, 100f)]
    public float setTempoValue = 100f;
    public bool applyTempoButton = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        parrySystem = GetComponent<ParrySystem>();
        playerCombat = GetComponent<PlayerCombat>();
        dashPerkController = GetComponent<DashPerkController>();
        baseDodgeCooldown = dodgeCooldown;

        // Otomatik Ayarlar
        if (rb != null)
        {
            rb.gravityScale = 0f; // Yercekimi yok
            rb.constraints = RigidbodyConstraints2D.FreezeRotation; // Donmeyi engelle
        }

        if (parrySystem != null)
        {
            parrySystem.OnParryResolved += HandleParryResolved;
            parrySystem.OnParryFail += HandleParryFail;
        }
    }

    private void OnDestroy()
    {
        if (parrySystem != null)
        {
            parrySystem.OnParryResolved -= HandleParryResolved;
            parrySystem.OnParryFail -= HandleParryFail;
        }
    }

    private void Update()
    {
        if (dashPerkController != null && Keyboard.current != null)
        {
            if (Keyboard.current.leftCtrlKey.wasPressedThisFrame ||
                Keyboard.current.rightCtrlKey.wasPressedThisFrame)
            {
                dashPerkController.TrySnapback();
            }
        }

        // Debug amaciyla inspector'dan tempoyu degistirmek icin
        if (applyTempoButton && TempoManager.Instance != null)
        {
            float diff = setTempoValue - TempoManager.Instance.tempo;
            
            // Kazancliysa decay i yeniler, eksi degere dusuruyorsa eventleri tetikler
            TempoManager.Instance.AddTempo(diff); 
            
            applyTempoButton = false; // "Buton" tiklamasi sonrasi tiki kaldir
        }
    }

    // Hub gibi alanlarda dukkan acikken hareketi kilitlemek icin
    [HideInInspector] public bool canMove = true;

    private void FixedUpdate()
    {
        if (dodgeCooldownTimer > 0f)
        {
            dodgeCooldownTimer -= Time.fixedDeltaTime;
            if (dodgeCooldownTimer <= 0f)
            {
                // Cooldown bitti, tum hayaletleri sil/fade et
                foreach (var trail in activeTrails)
                {
                    if (trail != null) trail.isFading = true;
                }
                activeTrails.Clear(); // Listeyi temizle
            }
        }

        // Hareket kilitliyse dur
        if (!canMove)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (currentState == PlayerState.Dodging)
        {
            UpdateDodge();
            return;
        }

        if (currentState == PlayerState.DashStriking)
        {
            UpdateExternalDash();
            return;
        }

        HandleMovement();
    }


    private void HandleMovement()
    {
        if (currentState == PlayerState.Dodging ||
            currentState == PlayerState.Parrying)
            return;

        if (moveInput.sqrMagnitude > 0.01f)
            lastNonZeroMove = moveInput.normalized;
        float tempoSpeedBonus = TempoManager.Instance != null ? TempoManager.Instance.GetSpeedMultiplier() : 1.0f;
        rb.linearVelocity = moveInput * (moveSpeed * tempoSpeedBonus * speedMultiplier);

        if (moveInput.sqrMagnitude > 0.01f)
        {
            currentState = PlayerState.Moving;
            
            // Yone gore donme (Sprite flip)
            if (moveInput.x > 0)
                transform.localScale = new Vector3(1, 1, 1);
            else if (moveInput.x < 0)
                transform.localScale = new Vector3(-1, 1, 1);
        }
        else
        {
            currentState = PlayerState.Idle;
        }

    }

    // INPUT CALLBACKS (Send Messages Uyumlu)

    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    public void OnAttack(InputValue value)
    {
        if (!value.isPressed) return;
        if (playerCombat == null) return;
        
        // Örn: GameManager durduysa falan buradan kesebiliriz (ileride)
        playerCombat.TryAttack();
    }

    public void OnDodge(InputValue value)
    {
        if (!value.isPressed) return;
        if (currentState == PlayerState.Dodging) return;
        if (dodgeCooldownTimer > 0f) return;

        dodgeDir = (moveInput.sqrMagnitude > 0.01f) ? moveInput.normalized : lastNonZeroMove;
        StartDodge(dodgeDir);
    }

    public void OnParry(InputValue value)
    {
        if (!value.isPressed) return;
        if (parrySystem == null) return;

        // Parry yonunu fare pozisyonundan hesapla
        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(
            UnityEngine.InputSystem.Mouse.current.position.ReadValue());
        mouseWorld.z = 0f;
        Vector2 aimDir = ((Vector2)mouseWorld - (Vector2)transform.position).normalized;
        if (aimDir.sqrMagnitude < 0.001f) aimDir = Vector2.right;

        parrySystem.StartParry(aimDir);
    }

    // --- DODGE LOGIC ---
    private void StartDodge(Vector2 dir)
    {
        currentState = PlayerState.Dodging;
        dodgeTimer = dodgeDuration;
        dodgeDir = dir;

        // NOT: IsInvulnerable artık burada açılmaz.
        // Perk sistemi (DashPerkController) dodge window'a göre açar/kapar.
        lastDodgeStartTime = Time.time;
        DodgeStartPos = rb.position;
        
        currentDodgeSpeed = dodgeSpeed;
        // NOT: Tempo T2/T3 hız bonusu kaldırıldı — skill tree bonusları ile değiştirilecek.
        
        rb.linearVelocity = dodgeDir * currentDodgeSpeed;
        
        OnDodgeStarted?.Invoke(dir);
    }

    private void UpdateDodge()
    {
        dodgeTimer -= Time.fixedDeltaTime;

        rb.linearVelocity = dodgeDir * currentDodgeSpeed;

        // Ghost Trail Spawning
        ghostSpawnTimer -= Time.fixedDeltaTime;
        if (ghostSpawnTimer <= 0f && ghostTrailPrefab != null)
        {
            SpawnGhostTrail();
            ghostSpawnTimer = ghostSpawnDelay;
        }

        if (dodgeTimer <= 0f)
        {
            EndDodge();
        }
    }

    private void SpawnGhostTrail()
    {
        SpriteRenderer mySr = GetComponentInChildren<SpriteRenderer>();
        if (mySr == null || mySr.sprite == null) return;

        GameObject ghostObj = Instantiate(ghostTrailPrefab, mySr.transform.position, mySr.transform.rotation);
        
        // Ayni scale (buyukluk/yon) ile olustur ki dogru tarafa baksin
        ghostObj.transform.localScale = transform.localScale; 
        
        GhostTrail ghost = ghostObj.GetComponent<GhostTrail>();
        if (ghost != null)
        {
            ghost.Setup(mySr.sprite, ghostColor);
            activeTrails.Add(ghost); // Listeye ekle ki sonradan silebilelim
        }
    }

    private void EndDodge()
    {
        IsInvulnerable = false;
        dodgeCooldownTimer = baseDodgeCooldown * dashCommitmentDodgeCooldownMultiplier * parryCommitmentDodgeCooldownMultiplier;

        rb.linearVelocity = Vector2.zero;

        currentState = (moveInput.sqrMagnitude > 0.01f) ? PlayerState.Moving : PlayerState.Idle;
        
        OnDodgeEnded?.Invoke();
    }

    // --- EXTERNAL DASH (DashStrike kombosu) ---
    /// <summary>
    /// PlayerCombat tarafından DashStrike adımında çağrılır.
    /// Dodge cooldown'u ve ghost trail olmaksızın kısa süreliğine i-frame + hareketi uygular.
    /// </summary>
    public void StartExternalDash(Vector2 dir, float speed, float duration)
    {
        if (currentState == PlayerState.Dodging) return; // Aktif dodge'u ezme
        currentState      = PlayerState.DashStriking;
        externalDashDir   = dir;
        externalDashSpeed = speed;
        externalDashTimer = duration;
        IsInvulnerable    = true;
        ghostSpawnTimer   = 0f; // Ghost Trail hemen başlasın
        rb.linearVelocity = dir * speed;
    }

    private void UpdateExternalDash()
    {
        externalDashTimer -= Time.fixedDeltaTime;
        rb.linearVelocity  = externalDashDir * externalDashSpeed;

        // Ghost Trail Spawning (Dodge ile aynı mekanik)
        ghostSpawnTimer -= Time.fixedDeltaTime;
        if (ghostSpawnTimer <= 0f && ghostTrailPrefab != null)
        {
            SpawnGhostTrail();
            ghostSpawnTimer = ghostSpawnDelay;
        }

        if (externalDashTimer <= 0f)
        {
            IsInvulnerable    = false;
            rb.linearVelocity = Vector2.zero;
            currentState = (moveInput.sqrMagnitude > 0.01f) ? PlayerState.Moving : PlayerState.Idle;

            // Dash bitti, trail'leri fade'e al
            foreach (var trail in activeTrails)
            {
                if (trail != null) trail.isFading = true;
            }
            activeTrails.Clear();
        }
    }

    // --- PARRY FEEDBACK ---
    private void HandleParryResolved(ParryEventData data)
    {
        if (data.isPerfect && TempoManager.Instance != null)
        {
            float gain = TempoManager.Instance.gainOnPerfectParry;
            gain *= parrySystem != null ? parrySystem.externalTempoMultiplier : 1f;
            TempoManager.Instance.AddTempo(gain);
        }

        if (HitStopManager.Instance != null)
            HitStopManager.Instance.PlayHitStop();

        if (DamagePopupManager.Instance != null)
        {
            if (data.isPerfect)
            {
                if (!data.isRanged)
                {
                    DamagePopupManager.Instance.CreateText(
                        transform.position + Vector3.up * 1.5f,
                        "PERFECT PARRY!",
                        new Color(1f, 0.9f, 0.25f),
                        6f);
                    lastPerfectParryPopupTime = Time.unscaledTime;
                }
            }
            else if (!data.isRanged && Time.unscaledTime - lastPerfectParryPopupTime > 0.05f)
            {
                DamagePopupManager.Instance.CreateText(
                    transform.position + Vector3.up * 1.5f,
                    "PARRY!",
                    Color.yellow,
                    6f);
            }
        }
            
        // --- T2/T3 Şok Dalgası (Shockwave) ---
        if (data.isPerfect && TempoManager.Instance != null)
        {
            var tier = TempoManager.Instance.CurrentTier;
            if (tier == TempoManager.TempoTier.T2 || tier == TempoManager.TempoTier.T3)
            {
                // Çevredeki düşmanları sersemlet ve küçük hasar ver
                Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 3f);
                foreach(var hit in hits)
                {
                    if (hit.CompareTag("Enemy"))
                    {
                        var enemy = hit.GetComponent<EnemyBase>();
                        if (enemy != null)
                        {
                            enemy.TakeDamage(5f); // Ufak chip damage
                            enemy.Stun(0.5f);     // Sendeletici şok dalgası
                            
                            if (DamagePopupManager.Instance != null)
                                DamagePopupManager.Instance.CreateHitParticle(hit.transform.position);
                        }
                    }
                }
                
                // Oyuncu üstünde görsel
                if (DamagePopupManager.Instance != null)
                    DamagePopupManager.Instance.CreateHitParticle(transform.position);
            }
        }
    }

    private void HandleParryFail()
    {

        if (TempoManager.Instance != null)
            TempoManager.Instance.AddTempo(-6f);
            
        if (DamagePopupManager.Instance != null)
            DamagePopupManager.Instance.CreateText(transform.position + Vector3.up * 1.5f, "FAILED", new Color(1f, 0.5f, 0f), 5f); // Turuncu
    }


    public void OnSimulateHit(InputValue value)
    {
        if (!value.isPressed) return;


        if (parrySystem != null) parrySystem.TryParry();
    }

    public void SetDashCommitmentDodgeCooldownMultiplier(float multiplier)
    {
        dashCommitmentDodgeCooldownMultiplier = Mathf.Max(0.05f, multiplier);
    }

    public void SetParryCommitmentDodgeCooldownMultiplier(float multiplier)
    {
        parryCommitmentDodgeCooldownMultiplier = Mathf.Max(0.05f, multiplier);
    }

    public void SetExternalDodgeCooldownMultiplier(float multiplier)
    {
        SetDashCommitmentDodgeCooldownMultiplier(multiplier);
    }

    public void ReduceDodgeCooldown(float amount)
    {
        if (amount <= 0f) return;
        dodgeCooldownTimer = Mathf.Max(0f, dodgeCooldownTimer - amount);
    }
}

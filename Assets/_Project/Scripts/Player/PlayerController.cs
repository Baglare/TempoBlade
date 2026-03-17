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
    private DashSkillRuntime dashSkillRuntime;
    
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
    public bool IsInvulnerable { get; private set; } = false;
    private float lastDodgeStartTime = -999f;
    private float currentDodgeSpeed;
    private float manualInvulnerableUntil = -999f;

    // --- EXTERNAL DASH (DashStrike kombosu için PlayerCombat tarafından çağrılır) ---
    private Vector2 externalDashDir;
    private float   externalDashSpeed;
    private float   externalDashTimer;
    private bool    externalDashInvulnerable;
    private bool    externalDashCountsAsPerkDash;
    
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
        dashSkillRuntime = GetComponent<DashSkillRuntime>();
        if (dashSkillRuntime == null)
            dashSkillRuntime = gameObject.AddComponent<DashSkillRuntime>();
        dashSkillRuntime.Initialize(this, playerCombat, parrySystem);

        // Otomatik Ayarlar
        if (rb != null)
        {
            rb.gravityScale = 0f; // Yercekimi yok
            rb.constraints = RigidbodyConstraints2D.FreezeRotation; // Donmeyi engelle
        }

        if (parrySystem != null)
        {
            parrySystem.OnParrySuccess += HandleParrySuccess;
            parrySystem.OnParryFail += HandleParryFail;
        }
    }

    private void Update()
    {
        RefreshInvulnerabilityState();

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

        // Hareket kilitliyse dur
        // Not: Dash/Dodge state'leri yukarida ilerlemeye devam etmeli,
        // aksi halde timer donup i-frame sonsuza uzayabilir.
        if (!canMove)
        {
            rb.linearVelocity = Vector2.zero;
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

        if (dashSkillRuntime != null && dashSkillRuntime.TryTriggerRebound())
            return;

        if (currentState == PlayerState.Dodging || currentState == PlayerState.DashStriking) return;
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

        lastDodgeStartTime = Time.time;
        currentDodgeSpeed = dodgeSpeed;
        
        rb.linearVelocity = dodgeDir * currentDodgeSpeed;

        if (dashSkillRuntime != null)
            dashSkillRuntime.NotifyDashStarted(transform.position, dodgeDir);
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
            if (dashSkillRuntime != null)
                dashSkillRuntime.NotifyDashEnded(transform.position);
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
        float cdMul = dashSkillRuntime != null ? dashSkillRuntime.GetDashCooldownMultiplier() : 1f;
        dodgeCooldownTimer = dodgeCooldown * cdMul;

        rb.linearVelocity = Vector2.zero;

        currentState = (moveInput.sqrMagnitude > 0.01f) ? PlayerState.Moving : PlayerState.Idle;
    }

    // --- EXTERNAL DASH (DashStrike kombosu) ---
    /// <summary>
    /// PlayerCombat tarafından DashStrike adımında çağrılır.
    /// Dodge cooldown'u ve ghost trail olmaksızın kısa süreliğine i-frame + hareketi uygular.
    /// </summary>
    public void StartExternalDash(Vector2 dir, float speed, float duration, bool grantInvulnerability = true, bool countsAsPerkDash = false)
    {
        if (currentState == PlayerState.Dodging) return; // Aktif dodge'u ezme
        currentState      = PlayerState.DashStriking;
        externalDashDir   = dir;
        externalDashSpeed = speed;
        externalDashTimer = duration;
        externalDashInvulnerable = grantInvulnerability;
        externalDashCountsAsPerkDash = countsAsPerkDash;
        ghostSpawnTimer   = 0f; // Ghost Trail hemen başlasın
        rb.linearVelocity = dir * speed;
        RefreshInvulnerabilityState();
        if (countsAsPerkDash && dashSkillRuntime != null)
            dashSkillRuntime.NotifyDashStarted(transform.position, dir);
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
            if (externalDashCountsAsPerkDash && dashSkillRuntime != null)
                dashSkillRuntime.NotifyDashEnded(transform.position);

            externalDashInvulnerable = false;
            externalDashCountsAsPerkDash = false;
            rb.linearVelocity = Vector2.zero;
            currentState = (moveInput.sqrMagnitude > 0.01f) ? PlayerState.Moving : PlayerState.Idle;
            RefreshInvulnerabilityState();

            // Dash bitti, trail'leri fade'e al
            foreach (var trail in activeTrails)
            {
                if (trail != null) trail.isFading = true;
            }
            activeTrails.Clear();
        }
    }

    // --- PARRY FEEDBACK ---
    private void HandleParrySuccess(bool isRanged)
    {

        if (TempoManager.Instance != null)
        {
            float gain = TempoManager.Instance.gainOnPerfectParry;
            if (dashSkillRuntime != null)
                gain *= dashSkillRuntime.GetParryTempoMultiplier();
            TempoManager.Instance.AddTempo(gain);
        }

        if (HitStopManager.Instance != null)
            HitStopManager.Instance.PlayHitStop();

        // Melee → "PARRY!", Ranged → popup yok (ParrySystem zaten "DEFLECT!" yazıyor)
        if (!isRanged && DamagePopupManager.Instance != null)
            DamagePopupManager.Instance.CreateText(transform.position + Vector3.up * 1.5f, "PARRY!", Color.yellow, 6f);
            
        // --- T2/T3 Şok Dalgası (Shockwave) ---
        if (TempoManager.Instance != null)
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

    private void RefreshInvulnerabilityState()
    {
        // Guvenlik: manuel i-frame suresi beklenmeyen bir sekilde uzarsa sinirla.
        if (manualInvulnerableUntil > Time.time + 2f)
            manualInvulnerableUntil = Time.time + 2f;

        bool inDashState = currentState == PlayerState.Dodging || currentState == PlayerState.DashStriking;
        bool manual = Time.time < manualInvulnerableUntil && inDashState;
        bool external = currentState == PlayerState.DashStriking && externalDashInvulnerable;
        IsInvulnerable = manual || external;
    }

    public void SetManualInvulnerability(float duration)
    {
        float clampedDuration = Mathf.Clamp(duration, 0f, 1.5f);
        manualInvulnerableUntil = Mathf.Max(manualInvulnerableUntil, Time.time + clampedDuration);
        RefreshInvulnerabilityState();
    }
}

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

        IsInvulnerable = true; 
        lastDodgeStartTime = Time.time;
        
        // T2 ve T3 Tempo'da Dodge menzili (hizi uzerinden) %30 artar
        currentDodgeSpeed = dodgeSpeed;
        if (TempoManager.Instance != null)
        {
            var tier = TempoManager.Instance.CurrentTier;
            if (tier == TempoManager.TempoTier.T2 || tier == TempoManager.TempoTier.T3)
            {
                currentDodgeSpeed *= 1.30f; 
            }
        }
        
        rb.linearVelocity = dodgeDir * currentDodgeSpeed;
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
        dodgeCooldownTimer = dodgeCooldown;

        rb.linearVelocity = Vector2.zero;

        currentState = (moveInput.sqrMagnitude > 0.01f) ? PlayerState.Moving : PlayerState.Idle;
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
    private void HandleParrySuccess(bool isRanged)
    {

        if (TempoManager.Instance != null)
            TempoManager.Instance.AddTempo(TempoManager.Instance.gainOnPerfectParry);

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
}

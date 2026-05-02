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
    public float movementInputDeadzone = 0.01f;
    public bool legacyRootFlipByMovement = false;
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
    private float externalStaggerTimer;
    private Vector2 externalStaggerVelocity;
    private float movementLockTimer;
    private const float ExternalStaggerDecayPerSecond = 18f;

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

    private static readonly RaycastHit2D[] dashBlockCastBuffer = new RaycastHit2D[16];
    private const float DashBlockSkin = 0.04f;

    private Rigidbody2D rb;
    private Collider2D dashProbeCollider;
    private Vector2 moveInput;

    public PlayerState currentState { get; private set; } = PlayerState.Idle;
    public bool IsExternallyStaggered => externalStaggerTimer > 0f;
    public bool IsMovementLocked => movementLockTimer > 0f;
    public Vector2 CurrentMoveInput => moveInput;
    public Vector2 LastMovementFacing => lastNonZeroMove;
    public bool HasMoveInput => moveInput.sqrMagnitude > movementInputDeadzone * movementInputDeadzone;

    [Header("Tempo Debug (Sadece Test Icin)")]
    [Tooltip("Tempoyu istediginiz degere getirmek icin degeri ayarlayip alttaki butonu isaretleyin.")]
    [Range(0f, 100f)]
    public float setTempoValue = 100f;
    public bool applyTempoButton = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        Collider2D[] colliders = GetComponentsInChildren<Collider2D>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null && !colliders[i].isTrigger)
            {
                dashProbeCollider = colliders[i];
                break;
            }
        }
        parrySystem = GetComponent<ParrySystem>();
        playerCombat = GetComponent<PlayerCombat>();
        dashPerkController = GetComponent<DashPerkController>();
        CombatTelemetryHub.EnsureFor(gameObject);
        CombatFeedbackDirector.EnsureFor(gameObject);
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
        if (movementLockTimer > 0f)
            movementLockTimer = Mathf.Max(0f, movementLockTimer - Time.deltaTime);

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
        if (!CanProcessMovement())
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (IsExternallyStaggered)
        {
            UpdateExternalStagger();
            return;
        }

        if (IsMovementLocked)
        {
            rb.linearVelocity = Vector2.zero;
            if (currentState == PlayerState.Moving || currentState == PlayerState.Dodging || currentState == PlayerState.DashStriking)
                currentState = PlayerState.Idle;
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

        if (HasMoveInput)
            lastNonZeroMove = moveInput.normalized;
        float tempoSpeedBonus = TempoManager.Instance != null ? TempoManager.Instance.GetSpeedMultiplier() : 1.0f;
        rb.linearVelocity = moveInput * (moveSpeed * tempoSpeedBonus * speedMultiplier);

        if (HasMoveInput)
        {
            currentState = PlayerState.Moving;

            if (legacyRootFlipByMovement)
            {
                if (moveInput.x > 0)
                    transform.localScale = new Vector3(1, 1, 1);
                else if (moveInput.x < 0)
                    transform.localScale = new Vector3(-1, 1, 1);
            }
        }
        else
        {
            currentState = PlayerState.Idle;
        }

    }

    // INPUT CALLBACKS (Send Messages Uyumlu)

    public void OnMove(InputValue value)
    {
        moveInput = CanProcessMovement() ? value.Get<Vector2>() : Vector2.zero;
    }

    public void OnAttack(InputValue value)
    {
        if (!value.isPressed) return;
        if (!CanProcessCombatInput()) return;
        if (playerCombat == null) return;
        if (IsExternallyStaggered) return;
        if (currentState == PlayerState.Dodging || currentState == PlayerState.DashStriking) return;
        
        // Örn: GameManager durduysa falan buradan kesebiliriz (ileride)
        playerCombat.TryAttack();
    }

    public void OnDodge(InputValue value)
    {
        if (!value.isPressed) return;
        if (!CanProcessCombatInput()) return;
        if (IsExternallyStaggered) return;
        if (IsMovementLocked) return;
        if (currentState == PlayerState.Dodging) return;
        if (dodgeCooldownTimer > 0f) return;

        dodgeDir = HasMoveInput ? moveInput.normalized : lastNonZeroMove;
        StartDodge(dodgeDir);
    }

    public void OnParry(InputValue value)
    {
        if (!value.isPressed) return;
        if (!CanProcessCombatInput()) return;
        if (parrySystem == null) return;
        if (IsExternallyStaggered) return;

        // Parry yonunu fare pozisyonundan hesapla
        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(
            UnityEngine.InputSystem.Mouse.current.position.ReadValue());
        mouseWorld.z = 0f;
        Vector2 aimDir = ((Vector2)mouseWorld - (Vector2)transform.position).normalized;
        if (aimDir.sqrMagnitude < 0.001f) aimDir = Vector2.right;

        parrySystem.StartParry(aimDir);
    }

    private bool CanProcessMovement()
    {
        if (!canMove)
            return false;

        if (ModalUIManager.HasOpenModal)
            return false;

        if (GameManager.Instance != null)
        {
            if (GameManager.Instance.CurrentState == GameManager.GameState.Paused ||
                GameManager.Instance.CurrentState == GameManager.GameState.GameOver)
                return false;
        }

        return true;
    }

    private bool CanProcessCombatInput()
    {
        if (ModalUIManager.HasOpenModal)
            return false;

        if (GameManager.Instance == null)
            return true;

        return GameManager.Instance.CurrentState == GameManager.GameState.Gameplay;
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
        
        rb.linearVelocity = Vector2.zero;
        AudioManager.Play(AudioEventId.PlayerDash, gameObject);
        
        OnDodgeStarted?.Invoke(dir);
    }

    private void UpdateDodge()
    {
        dodgeTimer -= Time.fixedDeltaTime;

        Vector2 desiredVelocity = dodgeDir * currentDodgeSpeed;
        PerformDashStep(desiredVelocity, Time.fixedDeltaTime, true);

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

        currentState = HasMoveInput ? PlayerState.Moving : PlayerState.Idle;
        
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
        rb.linearVelocity = Vector2.zero;
    }

    private void UpdateExternalDash()
    {
        externalDashTimer -= Time.fixedDeltaTime;
        Vector2 desiredVelocity = externalDashDir * externalDashSpeed;
        PerformDashStep(desiredVelocity, Time.fixedDeltaTime, false);

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
            currentState = HasMoveInput ? PlayerState.Moving : PlayerState.Idle;

            // Dash bitti, trail'leri fade'e al
            foreach (var trail in activeTrails)
            {
                if (trail != null) trail.isFading = true;
            }
            activeTrails.Clear();
        }
    }

    private void UpdateExternalStagger()
    {
        externalStaggerTimer -= Time.fixedDeltaTime;
        rb.linearVelocity = externalStaggerVelocity;
        externalStaggerVelocity = Vector2.MoveTowards(
            externalStaggerVelocity,
            Vector2.zero,
            ExternalStaggerDecayPerSecond * Time.fixedDeltaTime);

        if (externalStaggerTimer > 0f)
            return;

        externalStaggerTimer = 0f;
        externalStaggerVelocity = Vector2.zero;
        rb.linearVelocity = Vector2.zero;
        currentState = HasMoveInput ? PlayerState.Moving : PlayerState.Idle;
    }

    public void ApplyExternalStagger(float duration, Vector2 knockbackVelocity)
    {
        if (duration <= 0f && knockbackVelocity.sqrMagnitude <= 0.0001f)
            return;

        externalStaggerTimer = Mathf.Max(externalStaggerTimer, duration);
        externalStaggerVelocity = knockbackVelocity;
        IsInvulnerable = false;
        currentState = PlayerState.Idle;
        rb.linearVelocity = knockbackVelocity;
    }

    public void ApplyMovementLock(float duration, bool cancelDash = true)
    {
        if (duration <= 0f)
            return;

        movementLockTimer = Mathf.Max(movementLockTimer, duration);
        if (!cancelDash)
            return;

        IsInvulnerable = false;
        externalDashTimer = 0f;
        dodgeTimer = 0f;
        rb.linearVelocity = Vector2.zero;
        currentState = PlayerState.Idle;
    }

    public void StopMovementVelocity()
    {
        if (rb != null)
            rb.linearVelocity = Vector2.zero;
    }

    public void EnterDeathState()
    {
        canMove = false;
        moveInput = Vector2.zero;
        IsInvulnerable = true;
        externalDashTimer = 0f;
        dodgeTimer = 0f;
        dodgeCooldownTimer = 0f;
        externalStaggerTimer = 0f;
        externalStaggerVelocity = Vector2.zero;
        movementLockTimer = float.MaxValue;
        currentState = PlayerState.Idle;

        foreach (var trail in activeTrails)
        {
            if (trail != null)
                trail.isFading = true;
        }
        activeTrails.Clear();

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false;
        }

        Collider2D[] colliders = GetComponentsInChildren<Collider2D>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
                colliders[i].enabled = false;
        }
    }

    private void PerformDashStep(Vector2 desiredVelocity, float deltaTime, bool endNormalDodgeOnBlock)
    {
        if (rb == null)
            return;

        rb.linearVelocity = Vector2.zero;

        if (desiredVelocity.sqrMagnitude <= 0.001f || deltaTime <= 0f)
            return;

        Vector2 direction = desiredVelocity.normalized;
        float distance = desiredVelocity.magnitude * deltaTime;
        float allowedDistance = distance;

        if (dashProbeCollider != null && distance > 0.001f)
        {
            ContactFilter2D filter = new ContactFilter2D
            {
                useTriggers = false,
                useLayerMask = false
            };

            int hitCount = dashProbeCollider.Cast(direction, filter, dashBlockCastBuffer, distance + DashBlockSkin);
            for (int i = 0; i < hitCount; i++)
            {
                Collider2D hitCollider = dashBlockCastBuffer[i].collider;
                if (hitCollider == null || hitCollider.transform.root == transform.root)
                    continue;

                bool hitEnemy = hitCollider.GetComponentInParent<EnemyBase>() != null;
                bool hitSolid = !hitCollider.isTrigger;
                if (!hitEnemy && !hitSolid)
                    continue;

                allowedDistance = Mathf.Max(0f, dashBlockCastBuffer[i].distance - DashBlockSkin);
                if (endNormalDodgeOnBlock)
                    dodgeTimer = 0f;
                else
                    externalDashTimer = 0f;
                break;
            }
        }

        if (allowedDistance <= 0f)
            return;

        rb.position += direction * allowedDistance;
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
                            EnemyDamageUtility.ApplyDamage(
                                enemy,
                                5f,
                                EnemyDamageSource.DashAttack,
                                gameObject,
                                EnemyDamageUtility.DirectionFromInstigator(enemy, gameObject),
                                0.6f,
                                isDashAttack: true); // Ufak chip damage
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

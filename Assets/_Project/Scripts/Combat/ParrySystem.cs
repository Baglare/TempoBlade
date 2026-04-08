using UnityEngine;

public class ParrySystem : MonoBehaviour
{
    [Header("Parry Timing")]
    [Tooltip("Parry penceresi (saniye). Orn: 0.15 - 0.20")]
    public float parryWindow   = 0.18f;
    [Tooltip("Parry kacarsa ceza suresi (saniye).")]
    public float parryRecovery = 0.08f;

    [Header("Multi-Block Window Extension")]
    [Tooltip("Her basarili blokta pencere kac saniye uzasin (Zincirleme sekmeler icin)")]
    public float windowExtensionPerBlock = 0.12f;
    [Tooltip("Maksimum parry penceresi suresi (uzama dahil)")]
    public float maxParryWindow          = 0.60f;

    [Header("Directional Parry")]
    [Tooltip("Parry yari acisi (derece). 90 = +/-90 = 180 derece koni")]
    public float parryArcHalfAngle = 90f;

    [Header("Counter Attack")]
    [Tooltip("Parry bittikten sonra karsi saldiri penceresi (s)")]
    public float counterWindowDuration = 0.5f;
    [Tooltip("Her melee bloku icin eklenen karsi saldiri carpani")]
    public float counterBonusPerMelee  = 0.15f;
    [Tooltip("Her ranged deflect icin eklenen karsi saldiri carpani")]
    public float counterBonusPerRanged = 0.10f;

    // ── State ─────────────────────────────────────────────────────────
    public bool IsParryActive         { get; private set; }
    public bool IsCounterWindowActive { get; private set; }
    public bool IsOnCooldown          { get; private set; }

    private float   timer;
    private float   initialWindowDuration;
    private Vector2 parryDirection;
    private int     blockCount;
    private float   accumulatedCounterBonus;
    private float   counterTimer;
    private float   recoveryCooldownTimer;

    // ── DashPerkController Commitment Cezaları ───────────────────
    /// <summary>Parry başarılı bloktan kazandığı tempo çarpanı (1 = normal, 0.7 = %30 azalma).</summary>
    [HideInInspector] public float externalTempoMultiplier = 1f;
    /// <summary>Parry recovery cooldown çarpanı (1 = normal, 1.2 = %20 artış).</summary>
    [HideInInspector] public float externalCooldownMultiplier = 1f;
    /// <summary>Parry pencere süresi çarpanı (1 = normal, 0.8 = %20 daralma).</summary>
    [HideInInspector] public float externalWindowMultiplier = 1f;

    // ── Events ────────────────────────────────────────────────────────
    public System.Action<Vector2> OnParryStarted;           // Parry basladiginda (aim yonuyle)
    public System.Action<bool>  OnParrySuccess;            // Her basarili blokta (bool isRanged)
    public System.Action          OnParryFail;               // Pencere blogsuz kapandi
    public System.Action<float>   OnWindowNormalized;        // UI: 0-1 doluluk (yesil slider)
    public System.Action<float>   OnCounterNormalized;       // UI: 0-1 doluluk (turuncu slider)
    public System.Action          OnCounterWindowStarted;
    public System.Action          OnCounterWindowEnded;

    private PlayerCombat playerCombat;

    private void Start()
    {
        playerCombat = GetComponent<PlayerCombat>();
    }

    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Parry'yi baslatir. Aim yonu PlayerController.OnParry'den gecirilir.
    /// </summary>
    public void StartParry(Vector2 aimDirection)
    {
        // Aktif parry varken veya cooldown'dayken yeni parry başlatma
        if (IsParryActive || IsOnCooldown) return;

        IsParryActive  = true;
        parryDirection = aimDirection;
        blockCount     = 0;
        accumulatedCounterBonus = 0f;

        // Onceki counter window'u iptal et
        if (IsCounterWindowActive)
        {
            IsCounterWindowActive = false;
            OnCounterWindowEnded?.Invoke();
        }

        float tempoSpeed = TempoManager.Instance != null
            ? TempoManager.Instance.GetSpeedMultiplier() : 1f;
        timer = (parryWindow * externalWindowMultiplier) / tempoSpeed;
        initialWindowDuration = timer;

        OnParryStarted?.Invoke(aimDirection);
    }

    private void Update()
    {
        if (IsParryActive)
        {
            PerformActiveParryScan(); // Her frame menzili tara

            timer -= Time.deltaTime;
            float norm = Mathf.Clamp01(timer / Mathf.Max(0.001f, initialWindowDuration));
            OnWindowNormalized?.Invoke(norm);

            if (timer <= 0f)
                CloseParryWindow();
        }

        if (IsCounterWindowActive)
        {
            counterTimer -= Time.deltaTime;
            OnCounterNormalized?.Invoke(Mathf.Clamp01(counterTimer / counterWindowDuration));

            if (counterTimer <= 0f)
            {
                IsCounterWindowActive   = false;
                accumulatedCounterBonus = 0f;
                OnCounterWindowEnded?.Invoke();
            }
        }

        // Recovery cooldown sayacı
        if (IsOnCooldown)
        {
            recoveryCooldownTimer -= Time.deltaTime;
            if (recoveryCooldownTimer <= 0f)
            {
                IsOnCooldown = false;
            }
        }
    }

    // ── NonAlloc Buffer (Sabit boyut, GC sıfır) ──────────────────────
    private static readonly Collider2D[] scanBuffer = new Collider2D[20];

    private void PerformActiveParryScan()
    {
        // Silah menzili tam ucu (Range + Offset) tarariz
        float atkOff = (playerCombat != null && playerCombat.currentWeapon != null) ? playerCombat.currentWeapon.attackOffset : 1.0f;
        float maxRange = playerCombat != null ? playerCombat.GetEffectiveRange() + atkOff : 2.5f;

        // NonAlloc: Sabit buffer'a yaz, yeni dizi yaratma
        int hitCount = Physics2D.OverlapCircleNonAlloc(transform.position, maxRange, scanBuffer);

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit = scanBuffer[i];
            if (hit == null) continue;

            // --- 1) MERMI (PROJECTILE) TARAMASI ---
            if (hit.TryGetComponent<IDeflectable>(out var proj))
            {
                // Sahibi biz miyiz veya zaten sektirilmiş mi?
                if (proj.ObjectOwner == gameObject || proj.IsDeflected) continue;

                Vector2 toProj = (hit.transform.position - transform.position);
                float dist = toProj.magnitude;
                
                if (dist <= maxRange)
                {
                    Vector2 dirToProj = dist > 0.001f ? toProj.normalized : parryDirection;
                    float angle = Vector2.Angle(dirToProj, parryDirection);
                    
                    if (angle <= parryArcHalfAngle)
                    {
                        RegisterBlock(isRanged: true);
                        proj.Deflect(gameObject); 
                        
                        if (TempoManager.Instance != null) TempoManager.Instance.AddTempo(10f);
                        if (DamagePopupManager.Instance != null)
                            DamagePopupManager.Instance.CreateText(hit.transform.position + Vector3.up, "DEFLECT!", Color.cyan, 6f);
                    }
                }
                continue; // Bu collider mermi olduğundan melee kontrolüne geçme
            }
            
            // --- 2) KILIC (MELEE HITBOX) TARAMASI ---
            if (hit.TryGetComponent<AttackHitbox>(out var enemyHitbox))
            {
                if (enemyHitbox.owner == null) continue;

                Vector2 toHitbox = (enemyHitbox.transform.position - transform.position);
                float dist = toHitbox.magnitude;
                
                if (dist <= maxRange)
                {
                    Vector2 dirToHitbox = dist > 0.001f ? toHitbox.normalized : parryDirection;
                    float angle = Vector2.Angle(dirToHitbox, parryDirection);
                    
                    if (angle <= parryArcHalfAngle)
                    {
                        RegisterBlock(isRanged: false);
                        enemyHitbox.owner.Stun(1.5f);
                        
                        if (DamagePopupManager.Instance != null)
                            DamagePopupManager.Instance.CreateHitParticle(enemyHitbox.transform.position);
                            
                        // Hitbox'in o darbesini bitir ki sonraki frame tekrar parry yemesin
                        enemyHitbox.gameObject.SetActive(false); 
                    }
                }
            }
        }
    }

    private void CloseParryWindow()
    {
        IsParryActive = false;

        // Recovery cooldown başlat (parryRecovery süresi boyunca yeni parry atamazsın)
        IsOnCooldown = true;
        recoveryCooldownTimer = parryRecovery * externalCooldownMultiplier;

        if (blockCount > 0)
        {
            IsCounterWindowActive = true;
            counterTimer          = counterWindowDuration;
            OnCounterWindowStarted?.Invoke();
        }
        else
        {
            OnParryFail?.Invoke();
        }
    }

    // ── Dis API ───────────────────────────────────────────────────────

    /// <summary>
    /// Mermi deflect (yonlu). BossProjectile tarafindan merminin world_position'u ile cagrilir.
    /// Pencereyi kapatmaz — dogal olarak solar.
    /// </summary>
    public bool TryDeflect(Vector2 projectileWorldPos)
    {
        if (!IsParryActive) return false;

        Vector2 toProj = projectileWorldPos - (Vector2)transform.position;
        float distance = toProj.magnitude;
        Vector2 projDir = distance > 0.001f ? toProj.normalized : parryDirection;

        // Mermi koni açısı içinde mi? (Hızına değil, fiziksel olarak önümüzde mi çarptı ona bakıyoruz)
        float angle = Vector2.Angle(projDir, parryDirection);
        if (angle > parryArcHalfAngle) return false;

        RegisterBlock(isRanged: true);
        return true;
    }

    /// <summary>
    /// Yakin dovus bloku (yonlu). AttackHitbox / dusman tarafindan saldirganin pozisyonuyla cagrilir.
    /// </summary>
    public bool TryBlockMelee(Vector2 attackerWorldPos)
    {
        if (!IsParryActive) return false;

        Vector2 toAttacker = attackerWorldPos - (Vector2)transform.position;
        float distance = toAttacker.magnitude;
        Vector2 attackerDir = toAttacker.normalized;

        // 1. Koni Açısı Kontrolü (Pasta Dilimi)
        float angle = Vector2.Angle(attackerDir, parryDirection);
        if (angle > parryArcHalfAngle) return false;

        // Mesafeyi Active Scan metodu zaten Hitbox uzerinden hesapliyor.
        // Bu fonksiyona gelenler fiziksel triggerlar oldugu icin ek mesafeye ihtiyac yok.

        RegisterBlock(isRanged: false);
        return true;
    }

    /// <summary>
    /// Yonsuz parry — Kamikaze gibi ozel durumlarda geri donuk uyumluluk icin.
    /// </summary>
    public bool TryParry()
    {
        if (!IsParryActive) return false;
        RegisterBlock(isRanged: false);
        return true;
    }

    /// <summary>
    /// Karsi saldiri carpanini doner (0 = pencere aktif degil).
    /// PlayerCombat.PerformHit tarafindan okunur.
    /// </summary>
    public float GetCounterMultiplier() =>
        IsCounterWindowActive ? accumulatedCounterBonus : 0f;

    /// <summary>
    /// Karsi saldiri penceresini kapatir ve birikimli bonusu sifirlar.
    /// Ilk basarili vurustan sonra PlayerCombat tarafindan cagrilir.
    /// </summary>
    public void ConsumeCounter()
    {
        if (!IsCounterWindowActive) return;
        IsCounterWindowActive   = false;
        accumulatedCounterBonus = 0f;
        counterTimer            = 0f;
        OnCounterWindowEnded?.Invoke();
    }

    // ─────────────────────────────────────────────────────────────────

    private void RegisterBlock(bool isRanged)
    {
        blockCount++;
        accumulatedCounterBonus += isRanged ? counterBonusPerRanged : counterBonusPerMelee;

        // Pencereyi uzat (cap uygula)
        timer = Mathf.Min(timer + windowExtensionPerBlock, maxParryWindow);
        
        // Cok Onemli Bugfix: UI barının ve timer'ın yeni pencereye ayak uydurması icin max threshold'u guncelle
        initialWindowDuration = Mathf.Max(initialWindowDuration, timer);

        if (HitStopManager.Instance != null)
        {
            if (isRanged)
                HitStopManager.Instance.PlayHitStop(0.05f, 0.1f); // Mermi Sektirme 
            else
                HitStopManager.Instance.PlayHitStop(0.08f, 0.05f); // Kılıç Çarpışması (Daha Ağır)
        }

        OnParrySuccess?.Invoke(isRanged);
    }
    
    private void OnDrawGizmosSelected()
    {
        float atkOff = (playerCombat != null && playerCombat.currentWeapon != null) ? playerCombat.currentWeapon.attackOffset : 1.0f;
        float maxRange = playerCombat != null ? playerCombat.GetEffectiveRange() + atkOff : 2.5f;
        Gizmos.color = new Color(0, 1, 1, 0.4f);
        Gizmos.DrawWireSphere(transform.position, maxRange);
    }
}

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Dash Skill Tree perk kontrol sistemi.
/// PlayerBuild flag'lerini dinleyerek aktif perkleri yönetir.
/// Tüm parametreler Inspector'dan ayarlanabilir (data-driven).
/// </summary>
public class DashPerkController : MonoBehaviour
{
    // ═══════════ REFERANSLAR ═══════════
    private PlayerController playerController;
    private PlayerCombat playerCombat;
    private ParrySystem parrySystem;

    // ═══════════ T1: MENZİLLİ KAÇINMA ═══════════
    [Header("=== T1: Menzilli Kaçınma ===")]
    [Tooltip("Dash sırasında projectile dodge penceresi süresi")]
    public float projectileDodgeWindow = 0.14f;
    [Tooltip("Başarılı kaçınma tespit aralığı (dash başlangıcından itibaren)")]
    public float projectileDodgeDetectionRange = 0.18f;
    [Tooltip("Dash sonrası güvenli çıkış payı")]
    public float projectileSafeExitMargin = 0.05f;
    [Tooltip("Başarılı dodge için minimum tehdit yakınlığı")]
    public float projectileThreatDistance = 2.5f;

    // ═══════════ T1: YAKIN DÖVÜŞ KAÇINMA ═══════════
    [Header("=== T1: Yakın Dövüş Kaçınma ===")]
    [Tooltip("Dash sırasında melee dodge penceresi süresi")]
    public float meleeDodgeWindow = 0.12f;
    [Tooltip("Başarılı kaçınma tespit süresi")]
    public float meleeDodgeDetectionRange = 0.16f;
    [Tooltip("Dash sonrası koruma payı")]
    public float meleeSafeExitMargin = 0.04f;
    [Tooltip("Başarılı dodge için saldırı temas mesafesi")]
    public float meleeThreatDistance = 1.8f;

    // ═══════════ T1: KARŞI SALDIRI ═══════════
    [Header("=== T1: Karşı Saldırı ===")]
    [Tooltip("Counter pencere süresi")]
    public float counterWindowDuration = 1.0f;
    [Tooltip("Counter hasar bonusu (0.35 = %35)")]
    public float counterDamageBonus = 0.35f;
    [Tooltip("Counter stagger bonusu")]
    public float counterStaggerBonus = 0.20f;
    [Tooltip("Aynı anda saklanabilen counter charge sayısı")]
    public int maxCounterCharges = 1;

    // ═══════════ T1: TEMPO KAZANCI ═══════════
    [Header("=== T1: Tempo Kazancı ===")]
    [Tooltip("Saldırı Dash'i başına kazanılan tempo")]
    public float tempoPerAggressiveDash = 8f;
    [Tooltip("Saldırı Dash'i tespit mesafesi (düşmana yakınlık)")]
    public float aggressiveDashDetectRange = 2.4f;
    [Tooltip("Tempo kazancı iç cooldown")]
    public float tempoGainInternalCooldown = 1.25f;
    [Tooltip("Tek dash'te max tempo kazanımı")]
    public float tempoGainMax = 8f;

    // ═══════════ T1: SALDIRI HIZI ═══════════
    [Header("=== T1: Saldırı Hızı ===")]
    [Tooltip("Dash sonrası hızlı saldırı penceresi")]
    public float postDashAttackWindow = 0.75f;
    [Tooltip("Saldırı hızı bonusu (0.20 = %20)")]
    public float attackSpeedBonus = 0.20f;
    [Tooltip("Recovery reset oranı (0.60 = %60 recovery atlanır)")]
    public float recoveryResetRatio = 0.60f;
    [Tooltip("Saldırı hızı iç cooldown")]
    public float attackSpeedInternalCooldown = 4.0f;

    // ═══════════ T2: COMMITMENT BONUSLARI (DASH) ═══════════
    [Header("=== T2: Commitment Bonusları (Dash) ===")]
    [Tooltip("Dash ile kazanılan tempo verimliliği bonusu")]
    public float commitDashTempoEfficiency = 0.25f;
    [Tooltip("Dash cooldown iyileşmesi")]
    public float commitDashCooldownReduction = 0.20f;
    [Tooltip("Dodge penceresi genişleme bonusu")]
    public float commitDodgeWindowBonus = 0.15f;

    // ═══════════ T2: COMMITMENT CEZALARI (PARRY) ═══════════
    [Header("=== T2: Commitment Cezaları (Parry) ===")]
    [Tooltip("Parry tempo katkısı azaltması")]
    public float commitParryTempoReduction = 0.30f;
    [Tooltip("Parry cooldown artışı")]
    public float commitParryCooldownPenalty = 0.20f;
    [Tooltip("Parry aktif pencere daralması")]
    public float commitParryWindowReduction = 0.20f;

    // ═══════════ T2 AVCI: AV İŞARETİ ═══════════
    [Header("=== T2 Avcı: Av İşareti ===")]
    public float huntMarkCooldown = 5.0f;
    public float activeCombatDetectionTime = 1.25f;
    public float randomTargetRange = 8.0f;
    public float markIdleTimeout = 1.5f;
    public float newHuntTransitionDelay = 0.25f;
    public float newHuntSearchRange = 10.0f;

    // ═══════════ T2 AVCI: KÖR NOKTA BASKISI ═══════════
    [Header("=== T2 Avcı: Kör Nokta Baskısı ===")]
    public float frontConeAngle = 110f;
    public float blindSpotDashRange = 2.2f;
    public float targetRotationSlow = 0.35f;
    public float rotationSlowDuration = 1.25f;
    public float blindSpotStunDuration = 0.50f;
    public float blindSpotCounterBonus = 0.40f;

    // ═══════════ T2 AVCI: AV ETRAFINDA AKIŞ ═══════════
    [Header("=== T2 Avcı: Av Etrafında Akış ===")]
    public float huntProximityRange = 3.2f;
    public float huntCooldownRegenBonus = 0.30f;
    public float huntAttackSpeedCDBonus = 0.35f;
    public float huntFlowCheckInterval = 0.20f;

    // ═══════════ T2 AVCI: İNFAZ DASH'İ ═══════════
    [Header("=== T2 Avcı: İnfaz Dash'i ===")]
    [Tooltip("İnfaz can eşiği (0.18 = %18)")]
    public float executeHealthThreshold = 0.18f;
    public float executeRearConeAngle = 70f;
    public float executeDashEntryRange = 2.0f;
    public float executeInvulnDuration = 0.35f;
    public float executeEntryWindow = 0.20f;

    // ═══════════ T2 AVCI: AV DEVRİ ═══════════
    [Header("=== T2 Avcı: Av Devri ===")]
    [Tooltip("Av başına hasar bonusu (0.01 = %1)")]
    public float huntKillDamageBonus = 0.01f;
    [Tooltip("Oda boyu maksimum bonus")]
    public float huntKillMaxBonus = 0.15f;

    // ═══════════ T2 AKIŞÇI: İŞARETLEME AKIŞI ═══════════
    [Header("=== T2 Akışçı: İşaretleme Akışı ===")]
    public float flowMarkWindow = 1.40f;
    public float flowMarkDuration = 6.0f;
    [Tooltip("Aktif benzersiz işaret başına hasar bonusu")]
    public float flowMarkDamagePerUnique = 0.04f;
    public int flowMarkMaxUnique = 5;

    // ═══════════ T2 AKIŞÇI: GERİ SIÇRAMA ═══════════
    [Header("=== T2 Akışçı: Geri Sıçrama ===")]
    public float snapbackWindow = 0.90f;
    public float snapbackCooldown = 5.0f;
    public float snapbackDuration = 0.10f;

    // ═══════════ T2 AKIŞÇI: ZİNCİR SEKMESİ ═══════════
    [Header("=== T2 Akışçı: Zincir Sekmesi ===")]
    public int chainBounceMax = 2;
    [Tooltip("İlk sekme hasarı (0.60 = ana vuruşun %60'ı)")]
    public float chainFirstBounceRatio = 0.60f;
    [Tooltip("Her sonraki sekmede azalma")]
    public float chainFalloffPerBounce = 0.15f;
    public float chainBounceRange = 5.0f;

    // ═══════════ T2 AKIŞÇI: KARA DELİK ═══════════
    [Header("=== T2 Akışçı: Kara Delik ===")]
    public int blackHoleMarkThreshold = 4;
    public float blackHoleRadius = 4.5f;
    public float blackHolePullDuration = 1.0f;
    public float blackHoleCooldown = 10.0f;

    // ═══════════ T2 AKIŞÇI: PATLAMA VURUŞU ═══════════
    [Header("=== T2 Akışçı: Patlama Vuruşu ===")]
    public float burstWindow = 1.20f;
    [Tooltip("Ana hedef hasar çarpanı")]
    public float burstMainMultiplier = 2.20f;
    [Tooltip("Tüketilen her işaret başına ek hasar")]
    public float burstPerMarkBonus = 0.20f;
    [Tooltip("Yan hedeflere yayılma hasarı oranı")]
    public float burstSplashRatio = 0.80f;

    // ═══════════ AKTİF PERK FLAG'LERİ ═══════════
    // PlayerBuild'den okunur
    private bool _hasProjectileDodge;
    private bool _hasMeleeDodge;
    private bool _hasCounter;
    private bool _hasTempoGain;
    private bool _hasAttackSpeed;
    private bool _hasT2Commitment;
    // Avcı
    private bool _hasHuntMark;
    private bool _hasBlindSpot;
    private bool _hasHuntFlow;
    private bool _hasExecute;
    private bool _hasHuntCycle;
    // Akışçı
    private bool _hasFlowMark;
    private bool _hasSnapback;
    private bool _hasChainBounce;
    private bool _hasBlackHole;
    private bool _hasBurst;

    // ═══════════ RUNTIME STATE ═══════════

    // -- Dodge Window --
    private bool _isDodging;
    private float _dodgeWindowTimer;
    private float _dodgeElapsed;
    private bool _successfulDodgeThisDash;
    private readonly HashSet<int> _countedDodgedThreats = new HashSet<int>();
    private int _dodgedMeleeCountThisDash;
    private int _dodgedProjectileCountThisDash;
    private float _currentCounterBonus;

    // -- Counter --
    private bool _isCounterWindowActive;
    private float _counterTimer;
    private int _counterCharges;

    // -- Tempo Gain --
    private float _tempoGainCooldownTimer;
    private readonly HashSet<int> _aggressiveZoneIds = new HashSet<int>();
    private float _tempoGainedThisDash;

    // -- Attack Speed --
    private bool _isPostDashAttackReady;
    private float _postDashAttackTimer;
    private float _attackSpeedCooldownTimer;

    // -- Avcı: Av İşareti --
    private EnemyBase _currentHuntTarget;
    private float _huntMarkTimer;
    private float _lastCombatTime;
    private float _huntKillBonusAccumulated;

    // -- Avcı: Kör Nokta --
    private bool _blindSpotTriggered;
    private float _blindSpotBonusTimer;

    // -- Avcı: Hunt Flow --
    private float _huntFlowCheckTimer;

    // -- Akışçı: İşaretleme --
    private Dictionary<EnemyBase, float> _flowMarkedTargets = new Dictionary<EnemyBase, float>();
    private bool _isFlowMarkWindowActive;
    private float _flowMarkWindowTimer;

    // -- Akışçı: Geri Sıçrama --
    private bool _canSnapback;
    private float _snapbackWindowTimer;
    private float _snapbackCooldownTimer;

    // -- Akışçı: Kara Delik --
    private float _blackHoleCooldownTimer;
    private bool _isBlackHoleActive;

    // -- Akışçı: Patlama --
    private bool _isBurstWindowActive;
    private float _burstWindowTimer;

    // ═══════════ PUBLIC API (Diğer sistemler için) ═══════════

    /// <summary>Dash Counter penceresi aktif mi? PlayerCombat.PerformHit okur.</summary>
    public bool IsCounterWindowActive => _isCounterWindowActive;

    /// <summary>Counter hasar çarpanını döndürür. 0 = aktif değil.</summary>
    public float GetCounterMultiplier() => _isCounterWindowActive ? _currentCounterBonus : 0f;

    /// <summary>Counter'ı tüketir (ilk vuruş sonrası).</summary>
    public void ConsumeCounter()
    {
        if (!_isCounterWindowActive) return;
        _counterCharges--;
        if (_counterCharges <= 0)
        {
            _isCounterWindowActive = false;
            _counterTimer = 0f;
            _currentCounterBonus = 0f;
        }
    }

    /// <summary>Post-dash saldırı hızı bonus aktif mi?</summary>
    public bool IsPostDashAttackSpeedActive => _isPostDashAttackReady;

    /// <summary>Post-dash saldırı hızı bonus çarpanı.</summary>
    public float GetAttackSpeedBonus() => _isPostDashAttackReady ? attackSpeedBonus : 0f;

    /// <summary>Post-dash recovery reset oranı.</summary>
    public float GetRecoveryResetRatio() => _isPostDashAttackReady ? recoveryResetRatio : 0f;

    /// <summary>Post-dash saldırı hızını tüketir.</summary>
    public void ConsumeAttackSpeed()
    {
        _isPostDashAttackReady = false;
        _postDashAttackTimer = 0f;
        _attackSpeedCooldownTimer = attackSpeedInternalCooldown;
    }

    /// <summary>Mevcut av hedefini döndürür (Avcı yolu).</summary>
    public EnemyBase CurrentHuntTarget => _currentHuntTarget;

    /// <summary>Av devri birikmiş hasar bonusu.</summary>
    public float HuntKillBonus => _huntKillBonusAccumulated;

    /// <summary>Akışçı işaret sayısı.</summary>
    public int FlowMarkCount => _flowMarkedTargets.Count;

    /// <summary>Patlama vuruşu penceresi aktif mi?</summary>
    public bool IsBurstWindowActive => _isBurstWindowActive;

    public bool NotifyProjectileDodged(Object threatSource)
    {
        return RegisterDodgedThreat(true, threatSource);
    }

    public bool NotifyMeleeDodged(Object threatSource)
    {
        return RegisterDodgedThreat(false, threatSource);
    }

    // ═══════════ LİFECYCLE ═══════════

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
        playerCombat = GetComponent<PlayerCombat>();
        parrySystem = GetComponent<ParrySystem>();
    }

    private void OnEnable()
    {
        if (playerController != null)
        {
            playerController.OnDodgeStarted += HandleDodgeStarted;
            playerController.OnDodgeEnded += HandleDodgeEnded;
        }

        SubscribeToAxisManager();
    }

    private void OnDisable()
    {
        if (playerController != null)
        {
            playerController.OnDodgeStarted -= HandleDodgeStarted;
            playerController.OnDodgeEnded -= HandleDodgeEnded;
        }

        if (AxisProgressionManager.Instance != null)
            AxisProgressionManager.Instance.OnBuildChanged -= HandleBuildChanged;
    }

    private void Start()
    {
        // 1 frame bekle — AxisProgressionManager.Start()'ın LoadFromSave yapmasını garanti et
        StartCoroutine(DelayedBuildSync());
    }

    private System.Collections.IEnumerator DelayedBuildSync()
    {
        // 1 frame bekle (AxisProgressionManager.Start → LoadFromSave tamamlansın)
        yield return null;

        SubscribeToAxisManager();

        if (AxisProgressionManager.Instance != null)
        {
            HandleBuildChanged(AxisProgressionManager.Instance.CurrentBuild);
        }
        else
        {
            Debug.LogWarning("[DashPerkController] AxisProgressionManager bulunamadı! Perkler çalışmayacak.");
        }
    }

    private void SubscribeToAxisManager()
    {
        if (AxisProgressionManager.Instance != null)
        {
            // Çift subscription'ı önle
            AxisProgressionManager.Instance.OnBuildChanged -= HandleBuildChanged;
            AxisProgressionManager.Instance.OnBuildChanged += HandleBuildChanged;
        }
    }

    // ═══════════ BUILD DEĞİŞİKLİĞİ ═══════════

    private void HandleBuildChanged(PlayerBuild build)
    {
        if (build == null) return;

        // T1
        _hasProjectileDodge = build.HasFlag(EffectKeyRegistry.DashProjectileDodge);
        _hasMeleeDodge = build.HasFlag(EffectKeyRegistry.DashMeleeDodge);
        _hasCounter = build.HasFlag(EffectKeyRegistry.DashCounter);
        _hasTempoGain = build.HasFlag(EffectKeyRegistry.DashTempoGain);
        _hasAttackSpeed = build.HasFlag(EffectKeyRegistry.DashAttackSpeed);

        // T2
        _hasT2Commitment = build.HasFlag(EffectKeyRegistry.DashT2Commitment);

        // Avcı
        _hasHuntMark = build.HasFlag(EffectKeyRegistry.DashHuntMark);
        _hasBlindSpot = build.HasFlag(EffectKeyRegistry.DashBlindSpot);
        _hasHuntFlow = build.HasFlag(EffectKeyRegistry.DashHuntFlow);
        _hasExecute = build.HasFlag(EffectKeyRegistry.DashExecute);
        _hasHuntCycle = build.HasFlag(EffectKeyRegistry.DashHuntCycle);

        // Akışçı
        _hasFlowMark = build.HasFlag(EffectKeyRegistry.DashFlowMark);
        _hasSnapback = build.HasFlag(EffectKeyRegistry.DashSnapback);
        _hasChainBounce = build.HasFlag(EffectKeyRegistry.DashChainBounce);
        _hasBlackHole = build.HasFlag(EffectKeyRegistry.DashBlackHole);
        _hasBurst = build.HasFlag(EffectKeyRegistry.DashBurst);

        ResetInactivePerkState();

        // T2 Commitment cezaları → Parry'ye uygula
        ApplyCommitmentModifiers();
    }

    // ═══════════ DODGE EVENT'LERİ ═══════════

    private void HandleDodgeStarted(Vector2 dir)
    {
        _isDodging = true;
        _dodgeElapsed = 0f;
        _successfulDodgeThisDash = false;
        _countedDodgedThreats.Clear();
        _dodgedMeleeCountThisDash = 0;
        _dodgedProjectileCountThisDash = 0;
        _tempoGainedThisDash = 0f;
        RefreshAggressiveZoneIds();

        // Dodge window hesapla (perk'e göre)
        float totalWindow = 0f;
        if (_hasProjectileDodge) totalWindow = Mathf.Max(totalWindow, projectileDodgeWindow);
        if (_hasMeleeDodge) totalWindow = Mathf.Max(totalWindow, meleeDodgeWindow);

        // T2 commitment bonusu
        if (_hasT2Commitment && totalWindow > 0f)
            totalWindow *= (1f + commitDodgeWindowBonus);

        if (totalWindow > 0f)
        {
            _dodgeWindowTimer = totalWindow;
            playerController.IsInvulnerable = true;
        }

    }

    private void HandleDodgeEnded()
    {
        _isDodging = false;

        // Dodge window'u kapat
        if (playerController != null && _dodgeWindowTimer > 0f)
        {
            // Safe exit margin: dodge bittikten sonra kısa bir koruma daha
            float exitMargin = 0f;
            if (_hasProjectileDodge) exitMargin = Mathf.Max(exitMargin, projectileSafeExitMargin);
            if (_hasMeleeDodge) exitMargin = Mathf.Max(exitMargin, meleeSafeExitMargin);

            if (exitMargin > 0f)
            {
                _dodgeWindowTimer = exitMargin;
            }
            else
            {
                playerController.IsInvulnerable = false;
                _dodgeWindowTimer = 0f;
            }
        }

        // Counter penceresi aç — dodge perk'i aktifse her dodge sonrası
        ActivateCounterWindowFromCurrentDash();

        // Post-dash Attack Speed
        if (_hasAttackSpeed && _attackSpeedCooldownTimer <= 0f)
        {
            _isPostDashAttackReady = true;
            _postDashAttackTimer = postDashAttackWindow;
        }

        // Akışçı: İşaretleme penceresi aç
        if (_hasFlowMark)
        {
            _isFlowMarkWindowActive = true;
            _flowMarkWindowTimer = flowMarkWindow;
        }

        if (_hasSnapback && _snapbackCooldownTimer <= 0f)
        {
            _canSnapback = true;
            _snapbackWindowTimer = snapbackWindow;
        }

        // Avcı: Kör Nokta kontrolü
        if (_hasExecute)
        {
            TryExecute();
        }

        if (_hasBlindSpot)
        {
            CheckBlindSpot();
        }
    }

    // ═══════════ UPDATE ═══════════

    private void Update()
    {
        float dt = Time.deltaTime;

        // -- Dodge Window Timer --
        if (_dodgeWindowTimer > 0f)
        {
            _dodgeWindowTimer -= dt;
            if (_dodgeWindowTimer <= 0f && playerController != null)
            {
                playerController.IsInvulnerable = false;
            }
        }

        // -- Dodge Elapsed (başarılı dodge tespiti için) --
        if (_isDodging)
        {
            _dodgeElapsed += dt;
            CheckAggressiveDashEntries();
        }

        // -- Counter Timer --
        if (_isCounterWindowActive)
        {
            _counterTimer -= dt;
            if (_counterTimer <= 0f)
            {
                _isCounterWindowActive = false;
                _counterCharges = 0;
                _currentCounterBonus = 0f;
            }
        }

        // -- Tempo Gain Cooldown --
        if (_tempoGainCooldownTimer > 0f)
            _tempoGainCooldownTimer -= dt;

        // -- Attack Speed Window --
        if (_isPostDashAttackReady)
        {
            _postDashAttackTimer -= dt;
            if (_postDashAttackTimer <= 0f)
            {
                _isPostDashAttackReady = false;
            }
        }

        // -- Attack Speed Internal Cooldown --
        if (_attackSpeedCooldownTimer > 0f)
            _attackSpeedCooldownTimer -= dt;

        // -- Akışçı: İşaretleme penceresi --
        if (_isFlowMarkWindowActive)
        {
            _flowMarkWindowTimer -= dt;
            if (_flowMarkWindowTimer <= 0f)
                _isFlowMarkWindowActive = false;
        }

        // -- Akışçı: Geri sıçrama penceresi --
        if (_canSnapback)
        {
            _snapbackWindowTimer -= dt;
            if (_snapbackWindowTimer <= 0f)
                _canSnapback = false;
        }
        if (_snapbackCooldownTimer > 0f)
            _snapbackCooldownTimer -= dt;

        if (_blindSpotTriggered)
        {
            _blindSpotBonusTimer -= dt;
            if (_blindSpotBonusTimer <= 0f)
            {
                _blindSpotTriggered = false;
                _blindSpotBonusTimer = 0f;
            }
        }

        // -- Akışçı: Kara Delik cooldown --
        if (_blackHoleCooldownTimer > 0f)
            _blackHoleCooldownTimer -= dt;

        // -- Akışçı: Patlama penceresi --
        if (_isBurstWindowActive)
        {
            _burstWindowTimer -= dt;
            if (_burstWindowTimer <= 0f)
                _isBurstWindowActive = false;
        }

        // -- İşaret süreleri temizleme --
        CleanupExpiredMarks();

        // -- Avcı: Av İşareti yönetimi --
        if (_hasHuntMark)
            UpdateHuntMark(dt);

        // -- Avcı: Hunt Flow --
        if (_hasHuntFlow && _currentHuntTarget != null)
            UpdateHuntFlow(dt);
    }

    // ═══════════ T1: BAŞARILI DODGE TESPİTİ ═══════════

    private bool RegisterDodgedThreat(bool isRanged, Object threatSource)
    {
        if (!_isDodging && _dodgeWindowTimer <= 0f) return false;
        if (threatSource == null) return false;
        if (isRanged && !_hasProjectileDodge) return false;
        if (!isRanged && !_hasMeleeDodge) return false;

        int id = threatSource.GetInstanceID();
        if (!_countedDodgedThreats.Add(id)) return false;

        _successfulDodgeThisDash = true;
        if (isRanged) _dodgedProjectileCountThisDash++;
        else _dodgedMeleeCountThisDash++;

        if (!_isDodging)
            ActivateCounterWindowFromCurrentDash();

        ShowDodgePopup("DODGE!");
        return true;
    }

    private void ShowDodgePopup(string text)
    {
        if (DamagePopupManager.Instance != null)
            DamagePopupManager.Instance.CreateText(
                transform.position + Vector3.up * 1.5f, text, Color.cyan, 6f);
    }

    // ═══════════ T1: TEMPO KAZANCI ═══════════

    /// <summary>
    /// Dash BITİŞ noktasında düşman çemberine girmiş mi kontrol eder.
    /// Düşmanın etrafındaki görünmez çembere dash ile girilirse tempo verir.
    /// </summary>
    private void CheckAggressiveDashEntries()
    {
        if (!_hasTempoGain) return;
        if (_tempoGainCooldownTimer > 0f) return;

        Collider2D[] nearby = Physics2D.OverlapCircleAll(transform.position, aggressiveDashDetectRange);
        HashSet<int> insideNow = new HashSet<int>();

        foreach (var col in nearby)
        {
            if (col.CompareTag("Enemy"))
            {
                int id = col.gameObject.GetInstanceID();
                insideNow.Add(id);

                if (!_aggressiveZoneIds.Contains(id))
                    GrantAggressiveDashTempo();
            }
        }

        _aggressiveZoneIds.Clear();
        foreach (int id in insideNow)
            _aggressiveZoneIds.Add(id);
    }

    private void RefreshAggressiveZoneIds()
    {
        _aggressiveZoneIds.Clear();

        Collider2D[] nearby = Physics2D.OverlapCircleAll(transform.position, aggressiveDashDetectRange);
        foreach (var col in nearby)
        {
            if (col.CompareTag("Enemy"))
                _aggressiveZoneIds.Add(col.gameObject.GetInstanceID());
        }
    }

    private void GrantAggressiveDashTempo()
    {
        if (TempoManager.Instance == null) return;

        float remainingCap = tempoGainMax - _tempoGainedThisDash;
        if (remainingCap <= 0f) return;

        float gain = tempoPerAggressiveDash;
        if (_hasT2Commitment) gain *= (1f + commitDashTempoEfficiency);
        gain = Mathf.Min(gain, remainingCap);

        if (gain <= 0f) return;

        TempoManager.Instance.AddTempo(gain);
        _tempoGainCooldownTimer = tempoGainInternalCooldown;
        _tempoGainedThisDash += gain;

        if (DamagePopupManager.Instance != null)
            DamagePopupManager.Instance.CreateText(
                transform.position + Vector3.up * 1.2f,
                $"+{gain:F0} TEMPO", new Color(1f, 0.6f, 0f), 5f);
    }

    // ═══════════ T2: COMMITMENT ═══════════

    private void ApplyCommitmentModifiers()
    {
        if (parrySystem == null) return;

        if (_hasT2Commitment)
        {
            parrySystem.externalTempoMultiplier = 1f - commitParryTempoReduction;
            parrySystem.externalCooldownMultiplier = 1f + commitParryCooldownPenalty;
            parrySystem.externalWindowMultiplier = 1f - commitParryWindowReduction;

            if (playerController != null)
                playerController.SetExternalDodgeCooldownMultiplier(1f - commitDashCooldownReduction);
        }
        else
        {
            parrySystem.externalTempoMultiplier = 1f;
            parrySystem.externalCooldownMultiplier = 1f;
            parrySystem.externalWindowMultiplier = 1f;

            if (playerController != null)
                playerController.SetExternalDodgeCooldownMultiplier(1f);
        }
    }

    // ═══════════ T2 AVCI: AV İŞARETİ ═══════════

    private void UpdateHuntMark(float dt)
    {
        _huntMarkTimer -= dt;

        // Mevcut hedef öldü mü?
        if (_currentHuntTarget != null && _currentHuntTarget.HealthPercent <= 0f)
        {
            OnHuntTargetKilled();
            return;
        }

        // Cooldown dolduğunda yeni hedef seç
        if (_huntMarkTimer <= 0f && _currentHuntTarget == null)
        {
            SelectNewHuntTarget();
            _huntMarkTimer = huntMarkCooldown;
        }
    }

    private void SelectNewHuntTarget()
    {
        Collider2D[] nearby = Physics2D.OverlapCircleAll(transform.position, randomTargetRange);
        EnemyBase closest = null;
        float closestDist = float.MaxValue;

        foreach (var col in nearby)
        {
            if (!col.CompareTag("Enemy")) continue;
            var enemy = col.GetComponent<EnemyBase>();
            if (enemy == null) continue;

            float dist = Vector2.Distance(transform.position, col.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closest = enemy;
            }
        }

        if (_currentHuntTarget != null && _currentHuntTarget != closest)
            _currentHuntTarget.SetPerkMarker(false, Color.red);

        _currentHuntTarget = closest;

        if (_currentHuntTarget != null)
            _currentHuntTarget.SetPerkMarker(true, Color.red);
    }

    private void OnHuntTargetKilled()
    {
        // Av Devri bonusu
        if (_hasHuntCycle)
        {
            _huntKillBonusAccumulated = Mathf.Min(
                _huntKillBonusAccumulated + huntKillDamageBonus,
                huntKillMaxBonus);
        }

        if (_currentHuntTarget != null)
            _currentHuntTarget.SetPerkMarker(false, Color.red);

        _currentHuntTarget = null;
        // Kısa gecikme sonrası yeni hedef aranacak (UpdateHuntMark'ta)
        _huntMarkTimer = newHuntTransitionDelay;
    }

    // ═══════════ T2 AVCI: HUNT FLOW ═══════════

    private void UpdateHuntFlow(float dt)
    {
        _huntFlowCheckTimer -= dt;
        if (_huntFlowCheckTimer > 0f) return;
        _huntFlowCheckTimer = huntFlowCheckInterval;

        if (_currentHuntTarget == null) return;

        float dist = Vector2.Distance(transform.position, _currentHuntTarget.transform.position);
        if (dist <= huntProximityRange)
        {
            if (playerController != null)
                playerController.ReduceDodgeCooldown(dt * huntCooldownRegenBonus);

            if (_attackSpeedCooldownTimer > 0f)
                _attackSpeedCooldownTimer = Mathf.Max(0f, _attackSpeedCooldownTimer - (dt * huntAttackSpeedCDBonus));
        }
    }

    // ═══════════ AKIŞÇI: İŞARETLEME ═══════════

    /// <summary>
    /// Bir düşmana flow mark ekler. PlayerCombat.PerformHit'ten çağrılır.
    /// </summary>
    public bool TryApplyFlowMark(EnemyBase target)
    {
        if (!_hasFlowMark || !_isFlowMarkWindowActive || target == null) return false;

        if (_flowMarkedTargets.ContainsKey(target))
        {
            // Süreyi tazele
            _flowMarkedTargets[target] = Time.time + flowMarkDuration;
            target.SetPerkMarker(true, Color.cyan);
            return true;
        }
        else if (_flowMarkedTargets.Count < flowMarkMaxUnique)
        {
            _flowMarkedTargets[target] = Time.time + flowMarkDuration;
            target.SetPerkMarker(true, Color.cyan);

            // Kara Delik eşik kontrolü
            if (_hasBlackHole && _flowMarkedTargets.Count >= blackHoleMarkThreshold && _blackHoleCooldownTimer <= 0f)
            {
                TriggerBlackHole(target);
            }

            return true;
        }

        return false;
    }

    /// <summary>İşaretli hedef sayısına göre bonus hasar çarpanı.</summary>
    public float GetFlowMarkDamageBonus()
    {
        if (!_hasFlowMark) return 0f;
        return _flowMarkedTargets.Count * flowMarkDamagePerUnique;
    }

    public void ConsumeFlowMarkWindow()
    {
        _isFlowMarkWindowActive = false;
        _flowMarkWindowTimer = 0f;
    }

    /// <summary>Hedef işaretli mi?</summary>
    public bool IsMarked(EnemyBase target)
    {
        return target != null && _flowMarkedTargets.ContainsKey(target);
    }

    private void CleanupExpiredMarks()
    {
        if (_flowMarkedTargets.Count == 0) return;

        var expired = new List<EnemyBase>();
        foreach (var kv in _flowMarkedTargets)
        {
            if (kv.Key == null || Time.time > kv.Value)
                expired.Add(kv.Key);
        }
        foreach (var e in expired)
        {
            if (e != null)
                e.SetPerkMarker(false, Color.cyan);
            _flowMarkedTargets.Remove(e);
        }
    }

    // ═══════════ AKIŞÇI: GERİ SIÇRAMA ═══════════

    /// <summary>
    /// Geri sıçrama tetiklenir. PlayerController.OnDodge input handler'ından çağrılabilir.
    /// </summary>
    public bool TrySnapback()
    {
        if (!_canSnapback || playerController == null) return false;

        _canSnapback = false;
        _snapbackCooldownTimer = snapbackCooldown;

        // Başlangıç pozisyonuna dön
        var rb = playerController.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.position = playerController.DodgeStartPos;
            rb.linearVelocity = Vector2.zero;
        }

        return true;
    }

    // ═══════════ AKIŞÇI: KARA DELİK ═══════════

    private void TriggerBlackHole(EnemyBase lastMarked)
    {
        if (_isBlackHoleActive) return;

        Vector2 center = lastMarked.transform.position;
        StartCoroutine(BlackHoleRoutine(center));
    }

    private System.Collections.IEnumerator BlackHoleRoutine(Vector2 center)
    {
        _isBlackHoleActive = true;
        _blackHoleCooldownTimer = blackHoleCooldown;

        float elapsed = 0f;
        while (elapsed < blackHolePullDuration)
        {
            foreach (var kv in _flowMarkedTargets)
            {
                if (kv.Key == null) continue;
                var rb = kv.Key.GetComponent<Rigidbody2D>();
                if (rb == null) continue;

                Vector2 dir = center - rb.position;
                float dist = dir.magnitude;
                if (dist > 0.5f)
                {
                    float pullForce = (blackHoleRadius / Mathf.Max(dist, 0.5f)) * 8f;
                    rb.AddForce(dir.normalized * pullForce, ForceMode2D.Force);
                }
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        _isBlackHoleActive = false;

        // Patlama penceresi aç
        if (_hasBurst)
        {
            _isBurstWindowActive = true;
            _burstWindowTimer = burstWindow;
        }
    }

    // ═══════════ AKIŞÇI: ZİNCİR SEKMESİ ═══════════

    /// <summary>
    /// İşaretli hedefe vurunca hasarı diğer işaretli hedeflere sektirir.
    /// PlayerCombat.PerformHit'ten çağrılır.
    /// </summary>
    public void TryChainBounce(EnemyBase hitTarget, float baseDamage)
    {
        if (!_hasChainBounce || !IsMarked(hitTarget)) return;

        int bounces = 0;
        float currentDamage = baseDamage * chainFirstBounceRatio;
        HashSet<EnemyBase> alreadyHit = new HashSet<EnemyBase> { hitTarget };

        foreach (var kv in _flowMarkedTargets)
        {
            if (bounces >= chainBounceMax) break;
            if (kv.Key == null || alreadyHit.Contains(kv.Key)) continue;

            float dist = Vector2.Distance(hitTarget.transform.position, kv.Key.transform.position);
            if (dist > chainBounceRange) continue;

            kv.Key.TakeDamage(currentDamage);
            alreadyHit.Add(kv.Key);
            bounces++;
            currentDamage *= (1f - chainFalloffPerBounce);
        }
    }

    // ═══════════ AKIŞÇI: PATLAMA VURUŞU ═══════════

    /// <summary>
    /// Patlama vuruşu. PlayerCombat.PerformHit'ten çağrılır.
    /// Returns true if burst was consumed.
    /// </summary>
    public bool TryBurst(EnemyBase mainTarget, float baseDamage)
    {
        if (!_isBurstWindowActive || mainTarget == null) return false;

        _isBurstWindowActive = false;

        // Ana hedef hasarı
        float mainDmg = baseDamage * burstMainMultiplier;
        mainDmg += _flowMarkedTargets.Count * burstPerMarkBonus * baseDamage;
        mainTarget.TakeDamage(mainDmg);

        // Popup
        if (DamagePopupManager.Instance != null)
            DamagePopupManager.Instance.CreateText(
                mainTarget.transform.position + Vector3.up * 2f,
                "PATLAMA!", new Color(1f, 0.3f, 0f), 8f);

        // Yayılma hasarı
        float splashDmg = mainDmg * burstSplashRatio;
        foreach (var kv in _flowMarkedTargets)
        {
            if (kv.Key == null || kv.Key == mainTarget) continue;
            kv.Key.TakeDamage(splashDmg);
        }

        // İşaretleri temizle
        foreach (var kv in _flowMarkedTargets)
        {
            if (kv.Key != null)
                kv.Key.SetPerkMarker(false, Color.cyan);
        }
        _flowMarkedTargets.Clear();

        return true;
    }

    // ═══════════ T2 AVCI: KÖR NOKTA BASKISI ═══════════

    /// <summary>
    /// Dash bittiğinde av hedefinin kör noktasında mı kontrol eder.
    /// DashPerkController.HandleDodgeEnded'dan çağrılabilir.
    /// </summary>
    public bool CheckBlindSpot()
    {
        if (!_hasBlindSpot || _currentHuntTarget == null) return false;

        Vector2 targetPos = _currentHuntTarget.transform.position;
        Vector2 playerPos = transform.position;
        float dist = Vector2.Distance(playerPos, targetPos);

        if (dist > blindSpotDashRange) return false;

        // Hedefin baktığı yön (localScale.x > 0 = sağ, < 0 = sol)
        Vector2 targetForward = _currentHuntTarget.transform.localScale.x >= 0 ? Vector2.right : Vector2.left;
        Vector2 toPlayer = (playerPos - targetPos).normalized;

        float angle = Vector2.Angle(targetForward, toPlayer);

        // Ön koni dışı = kör nokta
        if (angle > frontConeAngle * 0.5f)
        {
            // Stun uygula
            _currentHuntTarget.Stun(blindSpotStunDuration);
            _blindSpotTriggered = true;
            _blindSpotBonusTimer = counterWindowDuration;

            if (DamagePopupManager.Instance != null)
                DamagePopupManager.Instance.CreateText(
                    targetPos + Vector2.up * 1.5f, "KÖR NOKTA!", Color.yellow, 6f);

            return true;
        }

        return false;
    }

    /// <summary>Kör nokta tetiklendiyse ek counter bonusu.</summary>
    public float GetBlindSpotCounterBonus() => _blindSpotTriggered ? blindSpotCounterBonus : 0f;

    public void ConsumeBlindSpotBonus()
    {
        _blindSpotTriggered = false;
        _blindSpotBonusTimer = 0f;
    }

    // ═══════════ T2 AVCI: İNFAZ DASH'İ ═══════════

    /// <summary>
    /// İnfaz koşullarını kontrol eder. Dash sırasında çağrılır.
    /// </summary>
    public bool TryExecute()
    {
        if (!_hasExecute || _currentHuntTarget == null) return false;

        if (_currentHuntTarget.HealthPercent > executeHealthThreshold)
            return false;

        float dist = Vector2.Distance(transform.position, _currentHuntTarget.transform.position);
        if (dist > executeDashEntryRange)
            return false;

        Vector2 targetForward = _currentHuntTarget.transform.localScale.x >= 0f ? Vector2.right : Vector2.left;
        Vector2 toPlayer = ((Vector2)transform.position - (Vector2)_currentHuntTarget.transform.position).normalized;
        float rearAngle = Vector2.Angle(-targetForward, toPlayer);
        if (rearAngle > executeRearConeAngle * 0.5f)
            return false;

        float lethalDamage = Mathf.Max(_currentHuntTarget.CurrentHealth, _currentHuntTarget.MaxHealth) + 1f;
        _currentHuntTarget.TakeDamage(lethalDamage);

        if (DamagePopupManager.Instance != null)
            DamagePopupManager.Instance.CreateText(
                _currentHuntTarget.transform.position + Vector3.up * 2f,
                "INFAZ!", new Color(1f, 0.2f, 0.2f), 8f);

        if (playerController != null)
            StartCoroutine(TemporaryInvulnerability(executeInvulnDuration));

        if (_currentHuntTarget != null)
            _currentHuntTarget.SetPerkMarker(false, Color.red);

        _currentHuntTarget = null;

        return true;
    }

    // ═══════════ ODA SIFIRLAMA ═══════════

    /// <summary>Oda geçişinde run-local verileri sıfırlar.</summary>
    public void ResetRoomData()
    {
        _huntKillBonusAccumulated = 0f;
        if (_currentHuntTarget != null)
            _currentHuntTarget.SetPerkMarker(false, Color.red);
        foreach (var kv in _flowMarkedTargets)
        {
            if (kv.Key != null)
                kv.Key.SetPerkMarker(false, Color.cyan);
        }
        _flowMarkedTargets.Clear();
        _currentHuntTarget = null;
        _isBlackHoleActive = false;
        _isBurstWindowActive = false;
    }

    private float GetCounterBonusPerMelee()
    {
        return parrySystem != null ? parrySystem.counterBonusPerMelee : 0.15f;
    }

    private float GetCounterBonusPerRanged()
    {
        return parrySystem != null ? parrySystem.counterBonusPerRanged : 0.10f;
    }

    private void ActivateCounterWindowFromCurrentDash()
    {
        if (!_hasCounter || (_dodgedProjectileCountThisDash <= 0 && _dodgedMeleeCountThisDash <= 0))
        {
            _isCounterWindowActive = false;
            _counterTimer = 0f;
            _counterCharges = 0;
            _currentCounterBonus = 0f;
            return;
        }

        _isCounterWindowActive = true;
        _counterTimer = counterWindowDuration;
        _counterCharges = maxCounterCharges;
        _currentCounterBonus =
            (_dodgedMeleeCountThisDash * GetCounterBonusPerMelee()) +
            (_dodgedProjectileCountThisDash * GetCounterBonusPerRanged());
    }

    private void ResetInactivePerkState()
    {
        if (!_hasCounter)
        {
            _isCounterWindowActive = false;
            _counterTimer = 0f;
            _counterCharges = 0;
            _currentCounterBonus = 0f;
        }

        if (!_hasTempoGain)
        {
            _aggressiveZoneIds.Clear();
            _tempoGainedThisDash = 0f;
        }

        if (!_hasAttackSpeed)
        {
            _isPostDashAttackReady = false;
            _postDashAttackTimer = 0f;
            _attackSpeedCooldownTimer = 0f;
        }

        if (!_hasHuntMark)
        {
            if (_currentHuntTarget != null)
                _currentHuntTarget.SetPerkMarker(false, Color.red);
            _currentHuntTarget = null;
            _huntMarkTimer = 0f;
        }

        if (!_hasHuntCycle)
        {
            _huntKillBonusAccumulated = 0f;
        }

        if (!_hasBlindSpot)
        {
            ConsumeBlindSpotBonus();
        }

        if (!_hasFlowMark)
        {
            foreach (var kv in _flowMarkedTargets)
            {
                if (kv.Key != null)
                    kv.Key.SetPerkMarker(false, Color.cyan);
            }
            _flowMarkedTargets.Clear();
            _isFlowMarkWindowActive = false;
            _flowMarkWindowTimer = 0f;
        }

        if (!_hasSnapback)
        {
            _canSnapback = false;
            _snapbackWindowTimer = 0f;
            _snapbackCooldownTimer = 0f;
        }

        if (!_hasBlackHole)
        {
            _isBlackHoleActive = false;
            _blackHoleCooldownTimer = 0f;
        }

        if (!_hasBurst)
        {
            _isBurstWindowActive = false;
            _burstWindowTimer = 0f;
        }
    }

    private System.Collections.IEnumerator TemporaryInvulnerability(float duration)
    {
        if (playerController == null) yield break;

        playerController.IsInvulnerable = true;
        yield return new WaitForSeconds(duration);

        if (playerController != null &&
            playerController.currentState != PlayerController.PlayerState.Dodging &&
            playerController.currentState != PlayerController.PlayerState.DashStriking)
        {
            playerController.IsInvulnerable = false;
        }
    }
}

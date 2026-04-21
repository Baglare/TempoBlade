using System.Collections;
using UnityEngine;

public class PlayerCombat : MonoBehaviour, IDamageable
{
    [Header("Stats")]
    public float maxHealth = 100f;
    public float currentHealth;

    [Header("Weapon")]
    public WeaponSO currentWeapon;
    public Transform attackPoint;
    public LayerMask enemyLayers;

    [Header("Weapon Database")]
    [Tooltip("Merkezi silah veritabani SO. Tek bir yerden tum silahlar cekilir.")]
    public WeaponDatabase weaponDatabase;

    [Header("Weapon Graphics")]
    public SpriteRenderer weaponSpriteRenderer;

    [Header("Arc Visual")]
    [Tooltip("WeaponArcVisual component'i. Player'ın altındaki child'a eklenir.")]
    public WeaponArcVisual weaponArcVisual;

    [Header("Run Modifiers")]
    public float damageMultiplier = 1.0f;

    private float nextAttackTime = 0f;

    // Kısa saldırı animasyon durumu (arc aktif renk için)
    private bool isSwinging;
    private float swingEndTime;

    // DashPerkController referansı (cache)
    private DashPerkController _dashPerks;
    private OverdrivePerkController _overdrivePerks;
    private CadencePerkController _cadencePerks;
    private CombatTelemetryHub _telemetry;

    // ──────────── COMBO STATE ────────────
    private Vector2 currentAimDir = Vector2.right;
    private int   comboIndex = 0;
    private float comboWindowTimer = 0f;
    private bool  isExecutingComboStep = false;

    /// <summary>
    /// (mevcut adım 1-based, toplam adım sayısı).
    /// current == 0 ise kombo sıfırlandı / whiff demektir.
    /// </summary>
    public event System.Action<int, int> OnComboChanged;
    // ─────────────────────────────────────

    public event System.Action<float, float> OnHealthChanged;
    public event System.Action<CounterFeedbackData> OnCounterFeedback;

    /// <summary>
    /// Mevcut silah upgrade seviyesi (SaveData'dan okunur).
    /// </summary>
    public int CurrentWeaponLevel
    {
        get
        {
            if (SaveManager.Instance == null || currentWeapon == null) return 0;
            return SaveManager.Instance.data.GetWeaponLevel(currentWeapon.weaponName);
        }
    }

    /// <summary>Yükseltme dahil efektif hasar.</summary>
    public float GetEffectiveDamage()
    {
        if (currentWeapon == null) return 10f;
        return currentWeapon.GetUpgradedDamage(CurrentWeaponLevel);
    }

    /// <summary>Yükseltme dahil efektif saldırı hızı.</summary>
    public float GetEffectiveAttackRate()
    {
        if (currentWeapon == null) return 0.5f;
        return currentWeapon.GetUpgradedAttackRate(CurrentWeaponLevel);
    }

    /// <summary>Yükseltme dahil efektif menzil.</summary>
    public float GetEffectiveRange()
    {
        if (currentWeapon == null) return 1.5f;
        return currentWeapon.GetUpgradedRange(CurrentWeaponLevel);
    }

    [Header("Upgrade Config")]
    [Tooltip("Hub yukseltme config'i — bonus can/hasar hesaplamak icin gerekli")]
    public UpgradeConfigSO upgradeConfig;

    private void Start()
    {
        // Save'den kusanilan silahi yukle
        LoadEquippedWeapon();

        // Kalici yukseltmeleri uygula (Hub'da satin alinanlar)
        ApplySavedUpgrades();

        // Default baslangic
        currentHealth = maxHealth;

        // RunManager varsa, onceki odadaki ayarlari geri yukle
        if (RunManager.Instance != null && RunManager.Instance.roomsCleared > 0)
        {
            RunManager.Instance.LoadPlayerState(this, TempoManager.Instance);
        }

        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        // DashPerkController referansını cachele
        _dashPerks = GetComponent<DashPerkController>();
        _overdrivePerks = GetComponent<OverdrivePerkController>();
        _cadencePerks = GetComponent<CadencePerkController>();
        _telemetry = CombatTelemetryHub.EnsureFor(gameObject);
    }

    /// <summary>
    /// SaveData'daki kalici yukseltmeleri Player'a uygular.
    /// Hub'da satin alinan bonus can, hasar carpani vb. burada aktif hale gelir.
    /// </summary>
    private void ApplySavedUpgrades()
    {
        if (SaveManager.Instance == null || upgradeConfig == null) return;

        SaveData data = SaveManager.Instance.data;

        maxHealth = upgradeConfig.GetMaxHealth(data.bonusMaxHealth);
        damageMultiplier = upgradeConfig.GetDamageMultiplier(data.bonusDamageMultiplier);
    }

    /// <summary>
    /// Hub'da upgrade satın alınca dışarıdan çağrılır.
    /// maxHealth + damageMultiplier'ı günceller, health slider'ı yeniler.
    /// </summary>
    public void RefreshFromSave()
    {
        ApplySavedUpgrades();
        currentHealth = Mathf.Min(currentHealth, maxHealth); // Taşmayı önle
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    /// <summary>
    /// Save'den equippedWeaponName'i okuyup allWeapons listesinden doğru WeaponSO'yu atar.
    /// </summary>
    public void LoadEquippedWeapon()
    {
        if (SaveManager.Instance == null || weaponDatabase == null) return;

        string savedName = SaveManager.Instance.data.equippedWeaponName;
        if (string.IsNullOrEmpty(savedName)) return;

        WeaponSO found = weaponDatabase.GetWeaponByName(savedName);
        if (found != null)
            EquipWeapon(found);
    }

    /// <summary>
    /// Silah kuşanma (StatsPanel'den çağrılır).
    /// </summary>
    public void EquipWeapon(WeaponSO weapon)
    {
        if (weapon == null) return;
        currentWeapon = weapon;

        if (weaponSpriteRenderer != null)
        {
            weaponSpriteRenderer.sprite = weapon.icon;
        }

        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.data.equippedWeaponName = weapon.weaponName;
            SaveManager.Instance.Save();
        }

        // Silah değişince komboyu sıfırla
        comboIndex       = 0;
        comboWindowTimer = 0f;
        OnComboChanged?.Invoke(0, weapon.comboSteps?.Length ?? 0);
    }

    private void Update()
    {
        // 360 Derece Nisan Alma (Mouse) + Yay Görseli
        Vector2 aimDir = Vector2.right; // Varsayılan yön

        if (attackPoint != null)
        {
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(UnityEngine.InputSystem.Mouse.current.position.ReadValue());
            Vector3 dir = mousePos - transform.position;
            dir.z = 0f;

            if (dir.sqrMagnitude > 0.0001f)
                aimDir = new Vector2(dir.x, dir.y).normalized;

            // Sadece AttackPoint'i dondur, oyuncunun grafigini degil
            float angle = Mathf.Atan2(aimDir.y, aimDir.x) * Mathf.Rad2Deg;
            attackPoint.rotation = Quaternion.Euler(0, 0, angle);

            // AttackPoint pozisyonunu oyuncunun etrafinda tut
            float offset = currentWeapon != null ? currentWeapon.attackOffset : 1.0f;
            attackPoint.position = transform.position + (Vector3)(aimDir * offset);
        }

        currentAimDir = aimDir; // DashStrike adımı için sakla

        // Swing state timeout (arc aktif rengini sıfırla)
        if (isSwinging && Time.time >= swingEndTime)
            isSwinging = false;

        // Saldırı veya parry inputları için ön kontrol
        ParrySystem _ps = GetComponent<ParrySystem>();
        bool _isParrying = _ps != null && _ps.IsParryActive;
        if (_isParrying && _ps != null)
            aimDir = _ps.CurrentParryDirection;

        // Yay ve kılıç görselini güncelle (Hem saldırırken hem parry yaparken yay görünsün)
        if (weaponArcVisual != null)
        {
            float atkOff = currentWeapon != null ? currentWeapon.attackOffset : 1.0f;
            float maxRange = GetEffectiveRange() + atkOff;
            weaponArcVisual.range = maxRange;
            
            // Eğer Parry yapıyorsak koni açısını silaha göre değil Parry ayarlarına (örn 90 derece = 45*2) göre ez
            float overrideAngle = (_isParrying && _ps != null) ? _ps.parryArcHalfAngle * 2f : -1f;
            bool isPerfectWindow = _isParrying && _ps != null && _ps.IsPerfectWindowActive;
            weaponArcVisual.UpdateVisuals(
                transform.position,
                aimDir,
                isSwinging,
                _isParrying,
                overrideAngle,
                -1f,
                false,
                isPerfectWindow);
        }

        // Kombo penceresi sayacı
        comboWindowTimer -= Time.deltaTime;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // COMBO SİSTEMİ
    // ──────────────────────────────────────────────────────────────────────────

    public void TryAttack()
    {
        // --- Cooldown / State Guard ---
        if (isExecutingComboStep) return;

        if (Time.time < nextAttackTime)
        {
            bool canBypassRecovery =
                _dashPerks != null &&
                _dashPerks.IsPostDashAttackSpeedActive;

            if (!canBypassRecovery)
                return;

            float bypassWindow = GetEffectiveAttackRate() * _dashPerks.GetRecoveryResetRatio();
            if ((nextAttackTime - Time.time) > bypassWindow)
                return;

            nextAttackTime = Time.time;
        }

        ParrySystem ps = GetComponent<ParrySystem>();
        if (ps != null && ps.IsParryActive) return;

        AudioManager.Play(AudioEventId.PlayerAttack, gameObject);

        if (_cadencePerks != null)
            _cadencePerks.NotifyAttackAction();
        _telemetry?.RecordAction(CombatActionType.Attack, gameObject);

        ComboStepData[] steps = currentWeapon?.comboSteps;

        // Silahta kombo tanımlı değilse eski tek vuruş davranışı (geriye dönük uyumlu)
        if (steps == null || steps.Length == 0)
        {
            Attack();
            return;
        }

        // Pencere dolmuşsa komboyu sıfırla
        if (comboWindowTimer <= 0f && comboIndex > 0)
        {
            comboIndex = 0;
            OnComboChanged?.Invoke(0, steps.Length);
        }

        ComboStepData step = steps[comboIndex];
        int fired = comboIndex;

        comboIndex++;
        bool isLastStep = (comboIndex >= steps.Length);
        if (isLastStep)
            comboIndex = 0;
        // comboWindowTimer adım bittikten SONRA ExecuteComboStep içinde atanır.
        // Böylece uzun adımlarda (windup + multihit) timer erken bitmez.

        Debug.Log($"[Combo] Adım {fired + 1}/{steps.Length} tetiklendi");
        OnComboChanged?.Invoke(fired + 1, steps.Length);

        // cooldownAfter veya silahın attackRate'inden hangisi büyükse onu kullan
        // Böylece yavaş silahlar (attackRate=1.0s) kombo cooldown'uyla (0.15s) bypass edilemez
        float effectiveCooldown = Mathf.Max(step.cooldownAfter, GetEffectiveAttackRate());
        if (_overdrivePerks != null)
            effectiveCooldown *= _overdrivePerks.GetAttackCooldownMultiplier();
        if (_cadencePerks != null)
            effectiveCooldown *= _cadencePerks.GetAttackCooldownMultiplier();

        // Dash Saldırı Hızı bonusu — post-dash penceresi aktifse cooldown kısalt
        if (_dashPerks != null && _dashPerks.IsPostDashAttackSpeedActive)
        {
            _dashPerks.ConsumeAttackSpeed();
        }

        nextAttackTime = Time.time + effectiveCooldown;
        StartCoroutine(ExecuteComboStep(step, isLastStep));
    }

    private IEnumerator ExecuteComboStep(ComboStepData step, bool isLastStep)
    {
        if (step.isUninterruptible)
            isExecutingComboStep = true;

        if (step.windupTime > 0f)
            yield return new WaitForSeconds(step.windupTime);

        switch (step.type)
        {
            case ComboStepType.Normal:
                PerformHit(step.damageMultiplier, step.rangeBonus);
                break;

            case ComboStepType.MultiHit:
                float dmgPerHit = step.damageMultiplier / Mathf.Max(1, step.hitCount);
                for (int i = 0; i < step.hitCount; i++)
                {
                    PerformHit(dmgPerHit, step.rangeBonus);
                    if (i < step.hitCount - 1)
                        yield return new WaitForSeconds(step.timeBetweenHits);
                }
                break;

            case ComboStepType.DashStrike:
                PlayerController pc = GetComponent<PlayerController>();
                if (_cadencePerks != null)
                    _cadencePerks.NotifyDashAction();
                if (pc != null)
                    pc.StartExternalDash(currentAimDir, step.dashSpeed, step.dashDuration);
                yield return new WaitForSeconds(step.dashDuration);
                PerformHit(step.damageMultiplier, step.rangeBonus);
                break;
        }

        // Adım tamamlandıktan sonra pencereyi başlat.
        // Uzun adımlarda (windup + multihit) timer erken bitmez.
        if (!isLastStep)
            comboWindowTimer = step.comboWindow;
        else
        {
            comboWindowTimer = 0f;
            OnComboChanged?.Invoke(0, currentWeapon?.comboSteps?.Length ?? 0);
        }

        isExecutingComboStep = false;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // HIT DETECTION
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Tek vuruş uygular. Combo adımları ve eski Attack() tarafından çağrılır.
    /// Whiff olursa komboyu sıfırlar ve Tempo cezası uygular.
    /// </summary>
    private void PerformHit(float multiplier, float rangeBonus)
    {
        isSwinging   = true;
        swingEndTime = Time.time + 0.15f;

        // Parry karşı saldırı bonusu (varsa uygula, vuruş sonrası tüket)
        ParrySystem parrySystem = GetComponent<ParrySystem>();
        float counterBonus = parrySystem != null ? parrySystem.GetCounterMultiplier() : 0f;

        // Dash karşı saldırı bonusu (üstüne eklenir)
        float dashCounterBonus = _dashPerks != null ? _dashPerks.GetCounterMultiplier() : 0f;
        float totalCounter = counterBonus + dashCounterBonus;

        // Kör nokta bonusu (Avcı T2)
        float blindSpotBonus = _dashPerks != null ? _dashPerks.GetBlindSpotCounterBonus() : 0f;
        totalCounter += blindSpotBonus;

        // Akışçı: Flow mark hasar bonusu
        float flowMarkBonus = _dashPerks != null ? _dashPerks.GetFlowMarkDamageBonus() : 0f;

        // Av Devri bonusu (Avcı T2)
        float huntBonus = _dashPerks != null ? _dashPerks.HuntKillBonus : 0f;

        float overdriveGlobalBonus = _overdrivePerks != null ? _overdrivePerks.GetGlobalDamageBonus(multiplier, totalCounter) : 0f;
        float cadenceGlobalBonus = _cadencePerks != null ? _cadencePerks.GetGlobalDamageBonus(multiplier, totalCounter) : 0f;

        float range   = GetEffectiveRange() + rangeBonus;
        float baseDmg = GetEffectiveDamage();
        float tempo   = TempoManager.Instance != null ? TempoManager.Instance.GetDamageMultiplier() : 1f;
        float total   = baseDmg * damageMultiplier * tempo * multiplier 
                        * (1f + totalCounter) 
                        * (1f + flowMarkBonus)
                        * (1f + huntBonus)
                        * (1f + overdriveGlobalBonus + cadenceGlobalBonus);

        Collider2D[] hits = Physics2D.OverlapCircleAll(attackPoint.position, range, enemyLayers);
        bool hitAny = false;
        bool flowMarkAppliedThisAttack = false;
        bool perkTempoBonusApplied = false;

        foreach (var col in hits)
        {
            var dmgable = col.GetComponent<IDamageable>();
            if (dmgable != null)
            {
                var enemy = col.GetComponent<EnemyBase>();
                float targetTotal = total;
                if (enemy != null)
                {
                    float overdriveTargetBonus = _overdrivePerks != null ? _overdrivePerks.GetTargetDamageBonus(enemy, multiplier, totalCounter) : 0f;
                    float cadenceTargetBonus = _cadencePerks != null ? _cadencePerks.GetTargetDamageBonus(enemy, multiplier, totalCounter) : 0f;
                    targetTotal *= 1f + overdriveTargetBonus + cadenceTargetBonus;
                }

                float beforeHealth = enemy != null ? enemy.CurrentHealth : 0f;
                dmgable.TakeDamage(targetTotal);

                if (TempoManager.Instance != null)
                {
                    float tempoGain = 2f;
                    if (!perkTempoBonusApplied)
                    {
                        if (_overdrivePerks != null) tempoGain += _overdrivePerks.GetTempoGainOnHit();
                        if (_cadencePerks != null) tempoGain += _cadencePerks.GetTempoGainOnHit();
                        perkTempoBonusApplied = true;
                    }
                    TempoManager.Instance.AddTempo(tempoGain);
                }

                bool killed = enemy != null && beforeHealth > 0f && enemy.CurrentHealth <= 0f;
                _telemetry?.RecordHit(enemy, killed, multiplier, totalCounter, targetTotal);
                if (_overdrivePerks != null) _overdrivePerks.NotifyEnemyHit(enemy, killed, multiplier, totalCounter);
                if (_cadencePerks != null) _cadencePerks.NotifyEnemyHit(enemy, killed, multiplier, totalCounter);
                if (_cadencePerks != null) _cadencePerks.TryWaveBounce(enemy, targetTotal);

                hitAny = true;

                // --- Akışçı Perkleri ---
                if (enemy != null && _dashPerks != null)
                {
                    // İşaretleme Akışı: dash sonrası penceredeyse hedefi işaretle
                    if (_dashPerks.TryApplyFlowMark(enemy))
                        flowMarkAppliedThisAttack = true;

                    // Zincir Sekmesi: işaretli hedefe vurunca hasarı sektirir
                    _dashPerks.TryChainBounce(enemy, total);

                    // Patlama Vuruşu: pencere aktifse büyük hasar
                    _dashPerks.TryBurst(enemy, baseDmg);
                }
            }
            var proj = col.GetComponent<IDeflectable>();
            if (proj != null && proj.CanBeDeflected)
            {
                proj.Deflect(DeflectContext.Default(gameObject));
                if (TempoManager.Instance != null) TempoManager.Instance.AddTempo(10f);
            }
        }

        if (flowMarkAppliedThisAttack && _dashPerks != null)
            _dashPerks.ConsumeFlowMarkWindow();

        if (hitAny)
        {
            if (CameraShakeManager.Instance != null)
                CameraShakeManager.Instance.ShakeCamera(2f, 0.1f);

            if (HitStopManager.Instance != null)
            {
                if (multiplier >= 1.5f) // Çok ağır (Finisher) vuruş
                    HitStopManager.Instance.PlayHeavyHitStop(); // 0.12s donma
                else if (multiplier >= 1.2f) // Ağır vuruş
                    HitStopManager.Instance.PlayHitStop(0.08f, 0.10f); // 0.08s
                else // Standart hafif vuruş
                    HitStopManager.Instance.PlayHitStop(0.04f, 0.15f); // 0.04s
            }

            // Parry karşı saldırı bonusu tüket ve popup göster
            if (counterBonus > 0f && parrySystem != null)
            {
                parrySystem.ConsumeCounter();
                AudioManager.Play(AudioEventId.PlayerCounter, gameObject);
                OnCounterFeedback?.Invoke(new CounterFeedbackData
                {
                    source = CounterFeedbackSource.Parry,
                    multiplier = 1f + counterBonus,
                    worldPosition = transform.position
                });
            }

            // Dash karşı saldırı bonusu tüket ve popup göster
            if (dashCounterBonus > 0f && _dashPerks != null)
            {
                _dashPerks.ConsumeCounter();
                AudioManager.Play(AudioEventId.PlayerCounter, gameObject);
                OnCounterFeedback?.Invoke(new CounterFeedbackData
                {
                    source = CounterFeedbackSource.Dash,
                    multiplier = 1f + dashCounterBonus,
                    worldPosition = transform.position
                });
            }

            if (blindSpotBonus > 0f && _dashPerks != null)
            {
                _dashPerks.ConsumeBlindSpotBonus();
            }
        }

        if (!hitAny) // Whiff → komboyu sıfırla
        {
            AudioManager.Play(AudioEventId.PlayerWhiff, gameObject);
            _telemetry?.RecordAction(CombatActionType.Whiff, gameObject);
            comboIndex       = 0;
            comboWindowTimer = 0f;
            OnComboChanged?.Invoke(0, currentWeapon?.comboSteps?.Length ?? 0);
            if (TempoManager.Instance != null)
            {
                float whiffPenalty = -5f;
                if (_overdrivePerks != null) whiffPenalty = _overdrivePerks.ModifyWhiffTempoPenalty(whiffPenalty);
                if (_cadencePerks != null) whiffPenalty = _cadencePerks.ModifyWhiffTempoPenalty(whiffPenalty);
                TempoManager.Instance.AddTempo(whiffPenalty);
            }
        }
    }

    /// <summary>
    /// Geriye dönük uyumluluk: comboSteps boş silahlar için eski tek vuruş davranışı.
    /// </summary>
    private void Attack()
    {
        float effectiveCooldown = GetEffectiveAttackRate();
        if (_overdrivePerks != null)
            effectiveCooldown *= _overdrivePerks.GetAttackCooldownMultiplier();
        if (_cadencePerks != null)
            effectiveCooldown *= _cadencePerks.GetAttackCooldownMultiplier();

        nextAttackTime = Time.time + effectiveCooldown;
        PerformHit(1f, 0f);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // HASAR ALMA
    // ──────────────────────────────────────────────────────────────────────────

    private float nextDamageTime = 0f;

    public void TakeDamage(float amount)
    {
        // Hasar sinirlandiricisi (I-frame): Ayni anda pes pese hasar yemeyi onlemek icin
        if (Time.time < nextDamageTime) return;

        // Dodge / DashStrike i-frame kontrolu
        PlayerController pc = GetComponent<PlayerController>();
        if (pc != null && pc.IsInvulnerable)
        {
            // Perk sistemi (DashPerkController) tarafından yönetilen dodge window.
            // DashPerkController, dodge sırasında uygun perkler aktifse
            // PlayerController.IsInvulnerable'ı açar/kapar.
            return; // Hasar alma
        }

        if (_overdrivePerks != null)
            amount *= _overdrivePerks.GetIncomingDamageMultiplier();
        if (_cadencePerks != null)
            amount *= _cadencePerks.GetIncomingDamageMultiplier();

        // Hasar alindi, i-frame baslat
        nextDamageTime = Time.time + 0.2f;

        currentHealth -= amount;
        _telemetry?.RecordDamageTaken(amount);
        AudioManager.Play(AudioEventId.PlayerDamageTaken, gameObject);

        OnHealthChanged?.Invoke(currentHealth, maxHealth); // UI Guncelle

        // Hasar yazisini cikar
        if (DamagePopupManager.Instance != null && amount > 0)
        {
            DamagePopupManager.Instance.Create(transform.position + Vector3.up, (int)amount, false);
            DamagePopupManager.Instance.CreateHitParticle(transform.position);
        }

        // Beyaz Flash Efekti
        var flash = GetComponent<HitFlash>();
        if (flash != null) flash.Flash();

        // Hasar yeme sarsintisi
        if (CameraShakeManager.Instance != null && amount > 0)
        {
            CameraShakeManager.Instance.ShakeCamera(6f, 0.25f);
        }

        // Tempo Penalty
        if (TempoManager.Instance != null && amount > 0)
             TempoManager.Instance.ApplyDamagePenalty();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    // RunManager (Odul Sistemi) gibi disaridan cani yenilemek/max cani arttirmak icin
    public void UpdateHealthUI()
    {
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void Heal(float amount)
    {
        if (currentHealth >= maxHealth || amount <= 0) return;

        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        UpdateHealthUI();
        AudioManager.Play(AudioEventId.PlayerHeal, gameObject);

        if (DamagePopupManager.Instance != null && amount >= 1f)
        {
            DamagePopupManager.Instance.CreateText(transform.position + Vector3.up, $"+{amount}", Color.green, 5f);
        }
    }

    public void Stun(float duration)
    {
        // Player stun logic
    }

    public void OnFinisher(UnityEngine.InputSystem.InputValue value)
    {
        if (!value.isPressed) return;
        ExecuteFinisher();
    }

    private void ExecuteFinisher()
    {
        if (TempoManager.Instance == null || TempoManager.Instance.CurrentTier != TempoManager.TempoTier.T3) return;

        AudioManager.Play(AudioEventId.PlayerFinisher, gameObject);

        if (_cadencePerks != null)
            _cadencePerks.NotifySkillAction();
        _telemetry?.RecordAction(CombatActionType.Skill, gameObject);

        // Finisher aktif! Dev hasar ve geniş alan
        float finisherRange = currentWeapon != null ? currentWeapon.range * 2f : 3f;
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, finisherRange, enemyLayers);

        bool hitSomeone = false;
        foreach(var col in hitEnemies)
        {
            if (col.CompareTag("Enemy"))
            {
                var damageable = col.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    float finisherDamage = currentWeapon != null ? currentWeapon.damage * 4f : 100f;
                    var enemyBase = col.GetComponent<EnemyBase>();
                    float beforeHealth = enemyBase != null ? enemyBase.CurrentHealth : 0f;
                    damageable.TakeDamage(finisherDamage);

                    bool killed = enemyBase != null && beforeHealth > 0f && enemyBase.CurrentHealth <= 0f;
                    _telemetry?.RecordHit(enemyBase, killed, 4f, 0f, finisherDamage);
                    if (enemyBase != null) enemyBase.Stun(2.0f);

                    hitSomeone = true;

                    if (DamagePopupManager.Instance != null)
                        DamagePopupManager.Instance.CreateHitParticle(col.transform.position);
                }
            }
        }

        if (hitSomeone)
        {
            if (DamagePopupManager.Instance != null)
                DamagePopupManager.Instance.CreateText(transform.position + Vector3.up * 2f, "FINISHER!", Color.magenta, 8f);

            if (HitStopManager.Instance != null)
                HitStopManager.Instance.PlayHeavyHitStop();

            if (CameraShakeManager.Instance != null)
                CameraShakeManager.Instance.ShakeCamera(10f, 0.4f);

            TempoManager.Instance.tempo = TempoManager.Instance.tier2Start + 9f;
        }
        else
        {
            if (DamagePopupManager.Instance != null)
                DamagePopupManager.Instance.CreateText(transform.position + Vector3.up * 2f, "WHIFF!", Color.gray, 6f);

            TempoManager.Instance.ApplyDamagePenalty();
            TempoManager.Instance.ApplyDamagePenalty();
        }
    }

    void Die()
    {
        AudioManager.Play(AudioEventId.PlayerDeath, gameObject);
        if (GameManager.Instance != null)
            GameManager.Instance.SetState(GameManager.GameState.GameOver);
    }

    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;
        float range = currentWeapon != null ? currentWeapon.range : 0.5f;
        Gizmos.DrawWireSphere(attackPoint.position, range);
    }
}

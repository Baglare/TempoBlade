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
    [Tooltip("WeaponArcVisual component'i. Player'in altindaki child'a eklenir.")]
    public WeaponArcVisual weaponArcVisual;

    [Header("Run Modifiers")]
    public float damageMultiplier = 1.0f;

    [Header("Upgrade Config")]
    [Tooltip("Hub yukseltme config'i - bonus can/hasar hesaplamak icin gerekli")]
    public UpgradeConfigSO upgradeConfig;

    private float nextAttackTime;
    private float nextDamageTime;
    private bool isSwinging;
    private float swingEndTime;
    private bool finisherDamageImmune;
    private bool finisherActive;

    private DashPerkController _dashPerks;
    private OverdrivePerkController _overdrivePerks;
    private CadencePerkController _cadencePerks;
    private CombatTelemetryHub _telemetry;
    private float externalCounterBonus;
    private CounterFeedbackSource externalCounterSource = CounterFeedbackSource.Dash;

    private PlayerWeaponRuntime weaponRuntime;
    private PlayerFinisherController finisherController;

    private Vector2 currentAimDir = Vector2.right;
    private int comboIndex;
    private float comboWindowTimer;
    private bool isExecutingComboStep;

    public event System.Action<int, int> OnComboChanged;
    public event System.Action<float, float> OnHealthChanged;
    public event System.Action<CounterFeedbackData> OnCounterFeedback;

    public Transform AttackPoint => attackPoint;
    public LayerMask EnemyLayers => enemyLayers;
    public Vector2 CurrentAimDirection => currentAimDir;
    public bool IsFinisherActive => finisherActive;

    public int CurrentWeaponLevel
    {
        get
        {
            if (weaponRuntime != null)
                return weaponRuntime.CurrentWeaponLevel;

            if (SaveManager.Instance == null || currentWeapon == null)
                return 0;

            return SaveManager.Instance.data.GetWeaponLevel(currentWeapon.weaponName);
        }
    }

    public float GetEffectiveDamage()
    {
        return currentWeapon == null ? 10f : GetResolvedWeaponStats().damage;
    }

    public float GetEffectiveAttackRate()
    {
        return currentWeapon == null ? 0.5f : GetResolvedWeaponStats().attackRate;
    }

    public float GetEffectiveRange()
    {
        return currentWeapon == null ? 1.5f : GetResolvedWeaponStats().range;
    }

    private void Awake()
    {
        weaponRuntime = new PlayerWeaponRuntime(this);
        finisherController = new PlayerFinisherController(this, weaponRuntime);
    }

    private void Start()
    {
        LoadEquippedWeapon();
        ApplySavedUpgrades();

        currentHealth = maxHealth;

        if (RunManager.Instance != null && RunManager.Instance.roomsCleared > 0)
            RunManager.Instance.LoadPlayerState(this, TempoManager.Instance);

        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        _dashPerks = GetComponent<DashPerkController>();
        _overdrivePerks = GetComponent<OverdrivePerkController>();
        _cadencePerks = GetComponent<CadencePerkController>();
        _telemetry = CombatTelemetryHub.EnsureFor(gameObject);
    }

    private void ApplySavedUpgrades()
    {
        if (SaveManager.Instance == null || upgradeConfig == null)
            return;

        SaveData data = SaveManager.Instance.data;
        maxHealth = upgradeConfig.GetMaxHealth(data.bonusMaxHealth);
        damageMultiplier = upgradeConfig.GetDamageMultiplier(data.bonusDamageMultiplier);
    }

    public void RefreshFromSave()
    {
        ApplySavedUpgrades();
        currentHealth = Mathf.Min(currentHealth, maxHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void LoadEquippedWeapon()
    {
        weaponRuntime?.LoadEquippedWeapon();
    }

    public void EquipWeapon(WeaponSO weapon)
    {
        weaponRuntime?.EquipWeapon(weapon);
    }

    private void Update()
    {
        Vector2 aimDir = Vector2.right;

        if (attackPoint != null && Camera.main != null && UnityEngine.InputSystem.Mouse.current != null)
        {
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(UnityEngine.InputSystem.Mouse.current.position.ReadValue());
            Vector3 dir = mousePos - transform.position;
            dir.z = 0f;

            if (dir.sqrMagnitude > 0.0001f)
                aimDir = new Vector2(dir.x, dir.y).normalized;

            float angle = Mathf.Atan2(aimDir.y, aimDir.x) * Mathf.Rad2Deg;
            attackPoint.rotation = Quaternion.Euler(0f, 0f, angle);

            float offset = currentWeapon != null ? GetResolvedWeaponStats().attackOffset : 1.0f;
            attackPoint.position = transform.position + (Vector3)(aimDir * offset);
        }

        currentAimDir = aimDir;

        if (isSwinging && Time.time >= swingEndTime)
            isSwinging = false;

        ParrySystem parrySystem = GetComponent<ParrySystem>();
        bool isParrying = parrySystem != null && parrySystem.IsParryActive;
        if (isParrying)
            aimDir = parrySystem.CurrentParryDirection;

        if (weaponArcVisual != null)
        {
            float attackOffset = currentWeapon != null ? GetResolvedWeaponStats().attackOffset : 1.0f;
            weaponArcVisual.range = GetEffectiveRange() + attackOffset;

            float overrideAngle = -1f;
            if (isParrying)
                overrideAngle = parrySystem.IsOmniProjectileDeflectActive ? 360f : parrySystem.parryArcHalfAngle * 2f;
            bool isPerfectWindow = isParrying && parrySystem.IsPerfectWindowActive;
            weaponArcVisual.UpdateVisuals(
                transform.position,
                aimDir,
                isSwinging,
                isParrying,
                overrideAngle,
                -1f,
                false,
                isPerfectWindow);
        }

        comboWindowTimer -= Time.deltaTime;
    }

    public void TryAttack()
    {
        if (!CanAcceptCombatInput())
            return;

        if (isExecutingComboStep)
            return;

        bool usedDashRecoveryBypass = false;
        if (Time.time < nextAttackTime)
        {
            bool canBypassRecovery = _dashPerks != null && _dashPerks.IsPostDashAttackSpeedActive;
            if (!canBypassRecovery)
                return;

            float bypassWindow = GetEffectiveAttackRate() * _dashPerks.GetRecoveryResetRatio();
            if ((nextAttackTime - Time.time) > bypassWindow)
                return;

            _dashPerks.ConsumeAttackSpeed();
            nextAttackTime = Time.time;
            usedDashRecoveryBypass = true;
        }

        ParrySystem parrySystem = GetComponent<ParrySystem>();
        if (parrySystem != null && parrySystem.IsParryActive)
            return;

        AudioManager.Play(AudioEventId.PlayerAttack, gameObject);
        if (_cadencePerks != null)
            _cadencePerks.NotifyAttackAction();
        _telemetry?.RecordAction(CombatActionType.Attack, gameObject);

        ComboStepData[] steps = currentWeapon?.comboSteps;
        if (steps == null || steps.Length == 0)
        {
            Attack();
            return;
        }

        if (comboWindowTimer <= 0f && comboIndex > 0)
        {
            comboIndex = 0;
            OnComboChanged?.Invoke(0, steps.Length);
        }

        WeaponResolvedStats stats = GetResolvedWeaponStats();
        ComboStepData step = steps[comboIndex];
        int firedIndex = comboIndex;

        comboIndex++;
        bool isLastStep = comboIndex >= steps.Length;
        if (isLastStep)
            comboIndex = 0;

        Debug.Log($"[Combo] Adim {firedIndex + 1}/{steps.Length} tetiklendi");
        OnComboChanged?.Invoke(firedIndex + 1, steps.Length);

        float effectiveCooldown = Mathf.Max(step.cooldownAfter, stats.attackRate) * stats.recoveryMultiplier;
        if (_overdrivePerks != null)
            effectiveCooldown *= _overdrivePerks.GetAttackCooldownMultiplier();
        if (_cadencePerks != null)
            effectiveCooldown *= _cadencePerks.GetAttackCooldownMultiplier();

        if (!usedDashRecoveryBypass && _dashPerks != null && _dashPerks.IsPostDashAttackSpeedActive)
            _dashPerks.ConsumeAttackSpeed();

        nextAttackTime = Time.time + effectiveCooldown;
        StartCoroutine(ExecuteComboStep(step, isLastStep, stats));
    }

    private IEnumerator ExecuteComboStep(ComboStepData step, bool isLastStep, WeaponResolvedStats stats)
    {
        if (step.isUninterruptible)
            isExecutingComboStep = true;

        float effectiveWindup = step.windupTime * stats.windupMultiplier;
        if (effectiveWindup > 0f)
            yield return new WaitForSeconds(effectiveWindup);

        switch (step.type)
        {
            case ComboStepType.Normal:
                PerformHit(step.damageMultiplier, step.rangeBonus);
                break;

            case ComboStepType.MultiHit:
                float damagePerHit = step.damageMultiplier / Mathf.Max(1, step.hitCount);
                for (int i = 0; i < step.hitCount; i++)
                {
                    PerformHit(damagePerHit, step.rangeBonus);
                    if (i < step.hitCount - 1)
                        yield return new WaitForSeconds(step.timeBetweenHits);
                }
                break;

            case ComboStepType.DashStrike:
                PlayerController playerController = GetComponent<PlayerController>();
                if (_cadencePerks != null)
                    _cadencePerks.NotifyDashAction();
                if (playerController != null)
                    playerController.StartExternalDash(currentAimDir, step.dashSpeed, step.dashDuration);
                yield return new WaitForSeconds(step.dashDuration);
                PerformHit(step.damageMultiplier, step.rangeBonus);
                break;
        }

        if (!isLastStep)
        {
            comboWindowTimer = step.comboWindow * stats.comboWindowMultiplier;
        }
        else
        {
            comboWindowTimer = 0f;
            OnComboChanged?.Invoke(0, currentWeapon?.comboSteps?.Length ?? 0);
        }

        isExecutingComboStep = false;
    }

    private void PerformHit(float multiplier, float rangeBonus)
    {
        WeaponResolvedStats stats = GetResolvedWeaponStats();

        isSwinging = true;
        swingEndTime = Time.time + 0.15f;

        ParrySystem parrySystem = GetComponent<ParrySystem>();
        float counterBonus = parrySystem != null ? parrySystem.GetCounterMultiplier() : 0f;
        float dashCounterBonus = _dashPerks != null ? _dashPerks.GetCounterMultiplier() : 0f;

        float totalCounter = counterBonus + dashCounterBonus;
        float blindSpotBonus = _dashPerks != null ? _dashPerks.GetBlindSpotCounterBonus() : 0f;
        totalCounter += blindSpotBonus;
        totalCounter += externalCounterBonus;
        totalCounter *= stats.counterBonusMultiplier;

        float flowMarkBonus = _dashPerks != null ? _dashPerks.GetFlowMarkDamageBonus() : 0f;
        float huntBonus = _dashPerks != null ? _dashPerks.HuntKillBonus : 0f;
        float overdriveGlobalBonus = _overdrivePerks != null ? _overdrivePerks.GetGlobalDamageBonus(multiplier, totalCounter) : 0f;
        float cadenceGlobalBonus = _cadencePerks != null ? _cadencePerks.GetGlobalDamageBonus(multiplier, totalCounter) : 0f;

        float baseDamage = stats.damage;
        float tempoDamageMultiplier = TempoManager.Instance != null ? TempoManager.Instance.GetDamageMultiplier() : 1f;
        float totalDamage = baseDamage * damageMultiplier * tempoDamageMultiplier * multiplier
            * (1f + totalCounter)
            * (1f + flowMarkBonus)
            * (1f + huntBonus)
            * (1f + overdriveGlobalBonus + cadenceGlobalBonus);

        Collider2D[] hits = Physics2D.OverlapCircleAll(attackPoint.position, stats.range + rangeBonus, enemyLayers);
        bool hitAny = false;
        bool flowMarkAppliedThisAttack = false;
        bool perkTempoBonusApplied = false;

        foreach (Collider2D hit in hits)
        {
            IDamageable damageable = hit.GetComponent<IDamageable>();
            if (damageable != null)
            {
                EnemyBase enemy = hit.GetComponent<EnemyBase>();
                float targetDamage = totalDamage;
                if (enemy != null)
                {
                    float overdriveTargetBonus = _overdrivePerks != null ? _overdrivePerks.GetTargetDamageBonus(enemy, multiplier, totalCounter) : 0f;
                    float cadenceTargetBonus = _cadencePerks != null ? _cadencePerks.GetTargetDamageBonus(enemy, multiplier, totalCounter) : 0f;
                    targetDamage *= 1f + overdriveTargetBonus + cadenceTargetBonus;
                }

                float beforeHealth = enemy != null ? enemy.CurrentHealth : 0f;
                damageable.TakeDamage(targetDamage);

                if (TempoManager.Instance != null)
                {
                    float tempoGain = stats.tempoGainOnHit;
                    if (!perkTempoBonusApplied)
                    {
                        if (_overdrivePerks != null)
                            tempoGain += _overdrivePerks.GetTempoGainOnHit();
                        if (_cadencePerks != null)
                            tempoGain += _cadencePerks.GetTempoGainOnHit();
                        perkTempoBonusApplied = true;
                    }

                    TempoManager.Instance.AddTempo(tempoGain);
                }

                bool killed = enemy != null && beforeHealth > 0f && enemy.CurrentHealth <= 0f;
                _telemetry?.RecordHit(enemy, killed, multiplier, totalCounter, targetDamage);
                if (_overdrivePerks != null)
                    _overdrivePerks.NotifyEnemyHit(enemy, killed, multiplier, totalCounter);
                if (_cadencePerks != null)
                    _cadencePerks.NotifyEnemyHit(enemy, killed, multiplier, totalCounter);
                if (_cadencePerks != null)
                    _cadencePerks.TryWaveBounce(enemy, targetDamage);

                if (enemy != null)
                {
                    float staggerDuration = stats.extraStaggerOnHit;
                    if (multiplier >= stats.heavyHitThreshold)
                        staggerDuration += stats.extraStaggerOnHeavyHit;

                    if (staggerDuration > 0f)
                        enemy.Stun(staggerDuration);
                }

                hitAny = true;

                if (enemy != null && _dashPerks != null)
                {
                    if (_dashPerks.TryApplyFlowMark(enemy))
                        flowMarkAppliedThisAttack = true;

                    _dashPerks.TryChainBounce(enemy, totalDamage);
                    _dashPerks.TryBurst(enemy, baseDamage);
                }
            }

            IDeflectable projectile = hit.GetComponent<IDeflectable>();
            if (projectile != null && projectile.CanBeDeflected)
            {
                projectile.Deflect(DeflectContext.Default(gameObject));
                if (TempoManager.Instance != null)
                    TempoManager.Instance.AddTempo(stats.tempoGainOnProjectileDeflect);
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
                if (multiplier >= 1.5f)
                    HitStopManager.Instance.PlayHeavyHitStop();
                else if (multiplier >= 1.2f)
                    HitStopManager.Instance.PlayHitStop(0.08f, 0.10f);
                else
                    HitStopManager.Instance.PlayHitStop(0.04f, 0.15f);
            }

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
                _dashPerks.ConsumeBlindSpotBonus();

            if (externalCounterBonus > 0f)
            {
                OnCounterFeedback?.Invoke(new CounterFeedbackData
                {
                    source = externalCounterSource,
                    multiplier = 1f + externalCounterBonus,
                    worldPosition = transform.position
                });
                externalCounterBonus = 0f;
            }
        }

        if (!hitAny)
        {
            AudioManager.Play(AudioEventId.PlayerWhiff, gameObject);
            _telemetry?.RecordAction(CombatActionType.Whiff, gameObject);
            ResetComboState();

            if (TempoManager.Instance != null)
            {
                float whiffPenalty = stats.whiffPenalty;
                if (_overdrivePerks != null)
                    whiffPenalty = _overdrivePerks.ModifyWhiffTempoPenalty(whiffPenalty);
                if (_cadencePerks != null)
                    whiffPenalty = _cadencePerks.ModifyWhiffTempoPenalty(whiffPenalty);

                TempoManager.Instance.AddTempo(whiffPenalty);
            }
        }
    }

    private void Attack()
    {
        WeaponResolvedStats stats = GetResolvedWeaponStats();
        float effectiveCooldown = stats.attackRate * stats.recoveryMultiplier;
        if (_overdrivePerks != null)
            effectiveCooldown *= _overdrivePerks.GetAttackCooldownMultiplier();
        if (_cadencePerks != null)
            effectiveCooldown *= _cadencePerks.GetAttackCooldownMultiplier();

        nextAttackTime = Time.time + effectiveCooldown;
        PerformHit(1f, 0f);
    }

    public void TakeDamage(float amount)
    {
        if (finisherDamageImmune)
            return;

        if (Time.time < nextDamageTime)
            return;

        PlayerController playerController = GetComponent<PlayerController>();
        if (playerController != null && playerController.IsInvulnerable)
            return;

        if (_overdrivePerks != null)
            amount *= _overdrivePerks.GetIncomingDamageMultiplier();
        if (_cadencePerks != null)
            amount *= _cadencePerks.GetIncomingDamageMultiplier();

        nextDamageTime = Time.time + 0.2f;

        currentHealth -= amount;
        _telemetry?.RecordDamageTaken(amount);
        AudioManager.Play(AudioEventId.PlayerDamageTaken, gameObject);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (DamagePopupManager.Instance != null && amount > 0f)
        {
            DamagePopupManager.Instance.Create(transform.position + Vector3.up, (int)amount, false);
            DamagePopupManager.Instance.CreateHitParticle(transform.position);
        }

        HitFlash flash = GetComponent<HitFlash>();
        if (flash != null)
            flash.Flash();

        if (CameraShakeManager.Instance != null && amount > 0f)
            CameraShakeManager.Instance.ShakeCamera(6f, 0.25f);

        if (TempoManager.Instance != null && amount > 0f)
            TempoManager.Instance.ApplyDamagePenalty();

        if (currentHealth <= 0f)
            Die();
    }

    public void UpdateHealthUI()
    {
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void Heal(float amount)
    {
        if (currentHealth >= maxHealth || amount <= 0f)
            return;

        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        UpdateHealthUI();
        AudioManager.Play(AudioEventId.PlayerHeal, gameObject);

        if (DamagePopupManager.Instance != null && amount >= 1f)
            DamagePopupManager.Instance.CreateText(transform.position + Vector3.up, $"+{amount}", Color.green, 5f);
    }

    public void Stun(float duration)
    {
    }

    public void OnFinisher(UnityEngine.InputSystem.InputValue value)
    {
        if (!value.isPressed)
            return;
        if (!CanAcceptCombatInput())
            return;

        finisherController?.TryExecute();
    }

    private bool CanAcceptCombatInput()
    {
        if (ModalUIManager.HasOpenModal)
            return false;

        if (finisherController != null && finisherController.IsExecuting)
            return false;

        if (GameManager.Instance == null)
            return true;

        return GameManager.Instance.CurrentState == GameManager.GameState.Gameplay;
    }

    private void Die()
    {
        AudioManager.Play(AudioEventId.PlayerDeath, gameObject);
        if (GameManager.Instance != null)
            GameManager.Instance.SetState(GameManager.GameState.GameOver);
    }

    public void GrantExternalCounterBonus(float bonus, CounterFeedbackSource source)
    {
        if (bonus <= 0f)
            return;

        externalCounterBonus = Mathf.Max(externalCounterBonus, bonus);
        externalCounterSource = source;
    }

    public void ResetComboState()
    {
        comboIndex = 0;
        comboWindowTimer = 0f;
        isExecutingComboStep = false;
        OnComboChanged?.Invoke(0, currentWeapon?.comboSteps?.Length ?? 0);
    }

    public void SetFinisherActive(bool active)
    {
        finisherActive = active;
        if (active)
            nextAttackTime = Time.time + 0.15f;
    }

    public void SetFinisherDamageImmune(bool active)
    {
        finisherDamageImmune = active;
    }

    public void NotifyFinisherSkillTriggered()
    {
        if (_cadencePerks != null)
            _cadencePerks.NotifySkillAction();

        _telemetry?.RecordAction(CombatActionType.Skill, gameObject);
    }

    public void RecordFinisherHit(EnemyBase enemy, bool killed, float damage)
    {
        _telemetry?.RecordHit(enemy, killed, 1f, 0f, damage);

        if (_overdrivePerks != null)
            _overdrivePerks.NotifyEnemyHit(enemy, killed, 1f, 0f);
        if (_cadencePerks != null)
            _cadencePerks.NotifyEnemyHit(enemy, killed, 1f, 0f);
    }

    public string GetWeaponDebugSummary()
    {
        return weaponRuntime != null ? weaponRuntime.BuildDebugSummary(finisherController) : string.Empty;
    }

    private WeaponResolvedStats GetResolvedWeaponStats()
    {
        if (weaponRuntime != null)
            return weaponRuntime.GetResolvedStats();

        return WeaponUpgradeResolver.Resolve(currentWeapon, CurrentWeaponLevel, string.Empty);
    }

    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null)
            return;

        Gizmos.DrawWireSphere(attackPoint.position, currentWeapon != null ? GetEffectiveRange() : 0.5f);
    }
}

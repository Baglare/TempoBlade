using System;
using UnityEngine;

public abstract class EnemyBase : MonoBehaviour, IDamageable
{
    public static event Action<EnemyCombatActionEvent> OnEnemyCombatAction;

    [Header("Base Settings")]
    public EnemySO enemyData;
    [Header("Stun Feedback")]
    [SerializeField] protected Color stunTintColor = new Color(1f, 0.55f, 0.15f, 1f);
    [Header("Facing")]
    [SerializeField] protected float facingTurnDelay = 0.09f;

    protected float currentHealth;
    protected bool isStunned;
    protected SpriteRenderer stunSpriteRenderer;
    protected Color stunOriginalColor = Color.white;
    protected EnemySupportBuffReceiver supportBuffReceiver;
    private Coroutine stunRoutine;
    private float stunEndTime;
    private bool suppressDeathRewards;
    private bool tempoSubscribed;
    private bool startInitialized;
    protected TempoManager.TempoTier currentTempoTier = TempoManager.TempoTier.T0;
    [NonSerialized] private bool isElite;
    [NonSerialized] private EliteProfileSO eliteProfile;
    private int currentFacingSign = 1;
    private int pendingFacingSign = 1;
    private float facingTurnCommitTime;
    private bool facingStateInitialized;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => GetEffectiveMaxHealth(enemyData != null ? enemyData.maxHealth : 100f);
    public float HealthPercent => MaxHealth > 0f ? currentHealth / MaxHealth : 0f;
    public TempoManager.TempoTier CurrentTempoTier => currentTempoTier;
    public bool IsElite => isElite && eliteProfile != null;
    public EliteProfileSO ActiveEliteProfile => eliteProfile;

    public event Action<float> OnDamageTaken;
    public event Action<float> OnStunned;
    public event Action<EnemyBase, float> OnDamageTakenDetailed;
    public event Action<EnemyBase, float> OnStunnedDetailed;

    protected virtual void Awake()
    {
        ClearEliteProfile();
    }

    protected virtual void OnEnable()
    {
        SubscribeTempo();
    }

    protected virtual void OnDisable()
    {
        UnsubscribeTempo();
    }

    protected virtual void Start()
    {
        currentHealth = MaxHealth;

        // Fiziksel kaymayi onlemek icin Drag ekle
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.linearDamping = 10f; // Surtunme (Eski surumlerde .drag, yenilerde .linearDrag)
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        // Tempo görsel efektini otomatik ekle (yoksa)
        if (GetComponent<TempoEnemyEffect>() == null)
            gameObject.AddComponent<TempoEnemyEffect>();

        if (GetComponent<EnemyStateFeedback>() == null)
            gameObject.AddComponent<EnemyStateFeedback>();

        if (GetComponent<EnemySupportBuffReceiver>() == null)
            gameObject.AddComponent<EnemySupportBuffReceiver>();

        supportBuffReceiver = GetComponent<EnemySupportBuffReceiver>();

        stunSpriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (stunSpriteRenderer != null)
            stunOriginalColor = stunSpriteRenderer.color;

        startInitialized = true;
        RefreshElitePresentation();

        currentTempoTier = TempoManager.Instance != null ? TempoManager.Instance.CurrentTier : TempoManager.TempoTier.T0;
        OnTempoTierChanged(currentTempoTier);
        SubscribeTempo();
    }

    public virtual void TakeDamage(float damageAmount)
    {
        if (supportBuffReceiver != null)
            damageAmount = supportBuffReceiver.ModifyIncomingDamage(damageAmount);

        if (damageAmount <= 0f)
            return;

        currentHealth -= damageAmount;
        OnDamageTaken?.Invoke(damageAmount);
        OnDamageTakenDetailed?.Invoke(this, damageAmount);
        AudioManager.Play(AudioEventId.EnemyHurt, gameObject);


        // Hasar Yazisi (Visual Feedback)
        if (DamagePopupManager.Instance != null)
        {
             // Hafif varyasyonlu pozisyon (ustuste binmesin diye)
             Vector3 randomOffset = new Vector3(UnityEngine.Random.Range(-0.3f, 0.3f), 0.5f, 0);
             DamagePopupManager.Instance.Create(transform.position + randomOffset, (int)damageAmount, false);
             
             // Vurus Efekti (Hit Particle)
             DamagePopupManager.Instance.CreateHitParticle(transform.position);
        }
        
        // Beyaz Flash Efekti
        var flash = GetComponent<HitFlash>();
        if (flash != null) flash.Flash();

        Stun(0.2f); // Her vurus hafif sersemletir (Micro-stun)

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public virtual void Stun(float duration)
    {
        if (duration <= 0f) return;

        if (supportBuffReceiver != null)
        {
            if (supportBuffReceiver.TryNegateIncomingStun(duration))
                return;

            duration = supportBuffReceiver.ModifyIncomingStunDuration(duration);
            if (duration <= 0f)
                return;
        }

        AudioManager.Play(AudioEventId.EnemyStun, gameObject);
        OnStunned?.Invoke(duration);
        OnStunnedDetailed?.Invoke(this, duration);

        float requestedEnd = Time.time + duration;
        if (isStunned && requestedEnd <= stunEndTime)
            return;

        if (stunRoutine != null)
            StopCoroutine(stunRoutine);

        stunEndTime = requestedEnd;
        stunRoutine = StartCoroutine(StunRoutine(duration));
    }

    protected virtual System.Collections.IEnumerator StunRoutine(float duration)
    {
        bool wasAlreadyStunned = isStunned;
        isStunned = true;
        if (stunSpriteRenderer != null)
        {
            if (!wasAlreadyStunned)
                stunOriginalColor = stunSpriteRenderer.color;
            stunSpriteRenderer.color = stunTintColor;
        }

        yield return new WaitForSeconds(duration);

        if (stunSpriteRenderer != null)
            stunSpriteRenderer.color = stunOriginalColor;

        isStunned = false;
        stunRoutine = null;
    }

    [Header("Effects")]
    [SerializeField] private GameObject deathVFX;
    private LineRenderer perkMarkerLine;
    private static Material perkMarkerMaterial;

    /// <summary>
    /// Ölüm animasyonu için Destroy gecikmesi (saniye).
    /// Animasyonlu düşmanlar Start()'ta bu değeri kendi clip sürelerine göre ayarlar.
    /// 0 = anında yok ol (varsayılan, animasyonsuz düşmanlar için).
    /// </summary>
    protected float deathDelay = 0f;

    /// <summary>
    /// Die() içinde Destroy çağrılmadan hemen önce tetiklenir.
    /// Override'da: death anim trigger, collider disable, velocity sıfırlama vb. yap.
    /// </summary>
    protected virtual void OnDeathAnimationStart() { }

    protected virtual void Die()
    {
        AudioManager.Play(AudioEventId.EnemyDeath, gameObject);
        if (!suppressDeathRewards && RoomManager.Instance != null)
            RoomManager.Instance.OnEnemyDied(gameObject);

        // --- ALTIN DÜŞÜR ---
        if (!suppressDeathRewards && enemyData != null && enemyData.goldDrop > 0 && EconomyManager.Instance != null)
        {
            EconomyManager.Instance.AddRunGold(enemyData.goldDrop);
        }

        // Death VFX
        if (deathVFX != null)
        {
            GameObject vfx = Instantiate(deathVFX, transform.position, Quaternion.identity);
            Destroy(vfx, 3f);
        }

        // Animasyon hook'u tetikle, ardından gecikmeli yok et (deathDelay=0 ise anında)
        OnDeathAnimationStart();
        Destroy(gameObject, deathDelay);
    }
    public void SetPerkMarker(bool active, Color color)
    {
        if (!active)
        {
            if (perkMarkerLine != null)
                perkMarkerLine.enabled = false;
            return;
        }

        EnsurePerkMarker();
        perkMarkerLine.enabled = true;
        perkMarkerLine.startColor = color;
        perkMarkerLine.endColor = color;
    }

    private void EnsurePerkMarker()
    {
        if (perkMarkerLine != null) return;

        GameObject markerObj = new GameObject("PerkMarker");
        markerObj.transform.SetParent(transform, false);
        markerObj.transform.localPosition = new Vector3(0f, 1.1f, 0f);

        perkMarkerLine = markerObj.AddComponent<LineRenderer>();
        perkMarkerLine.useWorldSpace = false;
        perkMarkerLine.loop = true;
        perkMarkerLine.positionCount = 20;
        perkMarkerLine.widthMultiplier = 0.04f;
        perkMarkerLine.numCapVertices = 4;
        perkMarkerLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        perkMarkerLine.receiveShadows = false;
        perkMarkerLine.textureMode = LineTextureMode.Stretch;
        perkMarkerLine.alignment = LineAlignment.TransformZ;
        perkMarkerLine.sortingOrder = 100;

        if (perkMarkerMaterial == null)
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null)
                perkMarkerMaterial = new Material(shader);
        }

        if (perkMarkerMaterial != null)
            perkMarkerLine.material = perkMarkerMaterial;

        float radius = 0.12f;
        for (int i = 0; i < perkMarkerLine.positionCount; i++)
        {
            float t = (i / (float)perkMarkerLine.positionCount) * Mathf.PI * 2f;
            perkMarkerLine.SetPosition(i, new Vector3(Mathf.Cos(t) * radius, Mathf.Sin(t) * radius, 0f));
        }
    }

    public void SetSuppressDeathRewards(bool suppress)
    {
        suppressDeathRewards = suppress;
    }

    protected virtual void OnTempoTierChanged(TempoManager.TempoTier tier) { }

    private void SubscribeTempo()
    {
        if (tempoSubscribed || TempoManager.Instance == null)
            return;

        TempoManager.Instance.OnTierChanged += HandleTempoTierChanged;
        tempoSubscribed = true;
    }

    private void UnsubscribeTempo()
    {
        if (!tempoSubscribed || TempoManager.Instance == null)
            return;

        TempoManager.Instance.OnTierChanged -= HandleTempoTierChanged;
        tempoSubscribed = false;
    }

    private void HandleTempoTierChanged(TempoManager.TempoTier tier)
    {
        currentTempoTier = tier;
        OnTempoTierChanged(tier);
    }

    public float GetSupportMoveSpeedMultiplier()
    {
        return supportBuffReceiver != null ? supportBuffReceiver.MoveSpeedMultiplier : 1f;
    }

    public float GetSupportAttackSpeedMultiplier()
    {
        return supportBuffReceiver != null ? supportBuffReceiver.AttackSpeedMultiplier : 1f;
    }

    public EnemySupportBuffReceiver GetSupportBuffReceiver()
    {
        return supportBuffReceiver;
    }

    public void ApplyEliteProfile(EliteProfileSO profile)
    {
        float oldMaxHealth = MaxHealth;
        eliteProfile = profile;
        isElite = eliteProfile != null;

        if (startInitialized)
        {
            float healthRatio = oldMaxHealth > 0f ? currentHealth / oldMaxHealth : 1f;
            currentHealth = Mathf.Clamp01(healthRatio) * MaxHealth;
            RefreshElitePresentation();
        }
    }

    public void ClearEliteProfile()
    {
        ApplyEliteProfile(null);
    }

    protected bool HasEliteMechanic(EliteMechanicType mechanicType)
    {
        return IsElite && eliteProfile != null && eliteProfile.HasMechanic(mechanicType);
    }

    protected float GetEffectiveMaxHealth(float baseHealth)
    {
        return baseHealth * GetEliteHealthMultiplier();
    }

    protected float GetEffectiveDamage(float baseDamage)
    {
        return baseDamage * GetEliteDamageMultiplier();
    }

    public float GetEffectiveContactDamage(float fallbackDamage)
    {
        return GetEffectiveDamageFromData(fallbackDamage);
    }

    protected float GetEffectiveDamageFromData(float fallbackDamage)
    {
        float baseDamage = enemyData != null ? enemyData.damage : fallbackDamage;
        return GetEffectiveDamage(baseDamage);
    }

    protected float GetEffectiveMoveSpeed(float baseMoveSpeed)
    {
        return baseMoveSpeed * GetEliteMoveSpeedMultiplier() * GetSupportMoveSpeedMultiplier();
    }

    protected float GetEffectiveMoveSpeedFromData(float fallbackMoveSpeed)
    {
        float baseMoveSpeed = enemyData != null ? enemyData.moveSpeed : fallbackMoveSpeed;
        return GetEffectiveMoveSpeed(baseMoveSpeed);
    }

    protected float GetEffectiveCooldownDuration(float baseDuration)
    {
        return baseDuration * GetEliteCooldownMultiplier();
    }

    protected void PlayEliteCue(Vector3 worldPosition, bool spawnVfx = true, bool playAudio = true)
    {
        if (!IsElite || eliteProfile == null)
            return;

        if (spawnVfx && eliteProfile.eliteVfxPrefab != null)
        {
            GameObject vfx = Instantiate(eliteProfile.eliteVfxPrefab, worldPosition, Quaternion.identity);
            Destroy(vfx, 3f);
        }

        if (playAudio && eliteProfile.eliteAudioEvent != AudioEventId.None)
            AudioManager.Play(eliteProfile.eliteAudioEvent, gameObject, worldPosition);
    }

    private float GetEliteHealthMultiplier()
    {
        return IsElite ? Mathf.Max(0.01f, eliteProfile.healthMultiplier) : 1f;
    }

    private float GetEliteDamageMultiplier()
    {
        return IsElite ? Mathf.Max(0.01f, eliteProfile.damageMultiplier) : 1f;
    }

    private float GetEliteCooldownMultiplier()
    {
        return IsElite ? Mathf.Max(0.01f, eliteProfile.cooldownMultiplier) : 1f;
    }

    private float GetEliteMoveSpeedMultiplier()
    {
        return IsElite ? Mathf.Max(0.01f, eliteProfile.moveSpeedMultiplier) : 1f;
    }

    private void RefreshElitePresentation()
    {
        if (IsElite && eliteProfile != null)
        {
            SetPerkMarker(true, eliteProfile.eliteCueColor);
            return;
        }

        SetPerkMarker(false, Color.clear);
    }

    protected void EmitCombatAction(EnemyCombatActionType actionType, float weight = 1f)
    {
        OnEnemyCombatAction?.Invoke(new EnemyCombatActionEvent
        {
            source = this,
            actionType = actionType,
            weight = Mathf.Max(0f, weight),
            time = Time.time,
            worldPosition = transform.position
        });
    }

    protected void UpdateSpriteFacing(SpriteRenderer targetRenderer, float targetX)
    {
        if (targetRenderer == null)
            return;

        int currentVisualSign = targetRenderer.flipX ? -1 : 1;
        int resolvedSign = ResolveFacingSign(targetX, currentVisualSign);
        targetRenderer.flipX = resolvedSign < 0;
    }

    protected void UpdateScaleFacing(float targetX)
    {
        int currentVisualSign = transform.localScale.x >= 0f ? 1 : -1;
        int resolvedSign = ResolveFacingSign(targetX, currentVisualSign);

        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * resolvedSign;
        transform.localScale = scale;
    }

    private int ResolveFacingSign(float targetX, int currentVisualSign)
    {
        EnsureFacingStateInitialized(currentVisualSign);

        int targetSign = targetX < transform.position.x ? -1 : 1;
        if (targetSign != pendingFacingSign)
        {
            pendingFacingSign = targetSign;
            facingTurnCommitTime = Time.time + Mathf.Max(0f, facingTurnDelay);
        }

        if (Time.time >= facingTurnCommitTime)
            currentFacingSign = pendingFacingSign;

        return currentFacingSign;
    }

    private void EnsureFacingStateInitialized(int currentVisualSign)
    {
        if (facingStateInitialized)
            return;

        currentFacingSign = currentVisualSign >= 0 ? 1 : -1;
        pendingFacingSign = currentFacingSign;
        facingTurnCommitTime = Time.time;
        facingStateInitialized = true;
    }
}

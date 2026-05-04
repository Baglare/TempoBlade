using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class EnemyBase : MonoBehaviour, IDamageable, ICombatTarget
{
    public static event Action<EnemyCombatActionEvent> OnEnemyCombatAction;
    private static readonly List<EnemyBase> activeEnemies = new List<EnemyBase>();

    [Header("Base Settings")]
    public EnemySO enemyData;
    [Header("Stun Feedback")]
    [SerializeField] protected Color stunTintColor = new Color(1f, 0.55f, 0.15f, 1f);
    [Header("Facing")]
    [SerializeField] protected float facingTurnDelay = 0.18f;
    [SerializeField] protected float facingHorizontalDeadzone = 0.28f;

    protected float currentHealth;
    protected bool isStunned;
    protected SpriteRenderer stunSpriteRenderer;
    protected Color stunOriginalColor = Color.white;
    protected EnemySupportBuffReceiver supportBuffReceiver;
    protected EnemyDefenseController defenseController;
    private Coroutine stunRoutine;
    private float stunEndTime;
    private bool hasPendingDamagePayload;
    private EnemyDamagePayload pendingDamagePayload;
    private EnemyDamageResult lastDamageResult;
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
    private float encounterHealthMultiplier = 1f;
    private float encounterDamageMultiplier = 1f;
    private float encounterCooldownMultiplier = 1f;
    private float encounterMoveSpeedMultiplier = 1f;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => GetEffectiveMaxHealth(enemyData != null ? enemyData.maxHealth : 100f);
    public float HealthPercent => MaxHealth > 0f ? currentHealth / MaxHealth : 0f;
    public Transform TargetTransform => transform;
    public GameObject TargetObject => gameObject;
    public bool IsAlive => currentHealth > 0f;
    public TempoManager.TempoTier CurrentTempoTier => currentTempoTier;
    public bool IsElite => isElite && eliteProfile != null;
    public EliteProfileSO ActiveEliteProfile => eliteProfile;
    public EnemyDefenseController Defense => EnsureDefenseController();
    public EnemyCombatClass CombatClass => ResolveCombatClass();
    public virtual bool IsDefenseGuardActive => false;
    public static IReadOnlyList<EnemyBase> ActiveEnemies => activeEnemies;

    public event Action<float> OnDamageTaken;
    public event Action<float> OnStunned;
    public event Action<EnemyBase, float> OnDamageTakenDetailed;
    public event Action<EnemyBase, float> OnStunnedDetailed;
    public event Action<EnemyDamageResult> OnDamageResolved;

    protected virtual void Awake()
    {
        ClearEliteProfile();
    }

    protected virtual void OnEnable()
    {
        RegisterActiveEnemy(this);
        SubscribeTempo();
    }

    protected virtual void OnDisable()
    {
        UnsubscribeTempo();
        UnregisterActiveEnemy(this);
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

        EnsureDefenseController();

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

    public EnemyDamageResult TakeDamage(EnemyDamagePayload payload)
    {
        hasPendingDamagePayload = true;
        pendingDamagePayload = payload;
        lastDamageResult = EnemyDamageResult.Ignored(payload, CombatClass);

        try
        {
            TakeDamage(payload.healthDamage);
            return lastDamageResult;
        }
        finally
        {
            hasPendingDamagePayload = false;
            pendingDamagePayload = default;
        }
    }

    public virtual void TakeDamage(float damageAmount)
    {
        EnemyDamageResult result = ResolveIncomingDamage(damageAmount);
        ApplyResolvedDamage(result, true);
    }

    protected EnemyDamageResult ResolveIncomingDamage(float damageAmount)
    {
        EnemyDamagePayload payload = hasPendingDamagePayload
            ? pendingDamagePayload
            : EnemyDamagePayload.FromHealthDamage(damageAmount);

        payload.healthDamage = damageAmount;
        if (supportBuffReceiver != null)
            payload.healthDamage = supportBuffReceiver.ModifyIncomingDamage(payload.healthDamage);

        bool hasAnyDamage = payload.healthDamage > 0f ||
                            (payload.hasExplicitStabilityDamage && payload.stabilityDamage > 0f);
        if (!hasAnyDamage)
        {
            lastDamageResult = EnemyDamageResult.Ignored(payload, CombatClass);
            return lastDamageResult;
        }

        lastDamageResult = EnsureDefenseController().ResolveDamage(payload);
        OnDamageResolved?.Invoke(lastDamageResult);
        return lastDamageResult;
    }

    protected virtual void ApplyResolvedDamage(EnemyDamageResult result, bool applyDefaultHitStun)
    {
        if (result.ignored)
            return;

        float damageAmount = result.appliedHealthDamage;
        if (damageAmount > 0f)
        {
            currentHealth -= damageAmount;
            OnDamageTaken?.Invoke(damageAmount);
            OnDamageTakenDetailed?.Invoke(this, damageAmount);
            AudioManager.Play(AudioEventId.EnemyHurt, gameObject);
        }

        if (DamagePopupManager.Instance != null && damageAmount > 0f)
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

        if (applyDefaultHitStun && result.shouldInterrupt && !result.didBreak && result.interruptDuration > 0f)
            Stun(result.interruptDuration);

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

    public void SetDefenseGuarding(bool active)
    {
        EnsureDefenseController().SetGuarding(active);
    }

    public void SetDefenseArmorActive(bool active)
    {
        EnsureDefenseController().SetArmorActive(active);
    }

    public virtual Vector2 GetDefenseForward()
    {
        return transform.localScale.x >= 0f ? Vector2.right : Vector2.left;
    }

    public virtual void HandleDefenseBrokenStarted(EnemyDamageResult result)
    {
        if (result.interruptDuration > 0f)
            Stun(result.interruptDuration);
    }

    public virtual void HandleDefenseBrokenEnded(EnemyDamageResult result)
    {
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
            if (defenseController != null)
                defenseController.RefreshFromOwnerData(false);
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
        return baseHealth * GetEliteHealthMultiplier() * encounterHealthMultiplier;
    }

    protected float GetEffectiveDamage(float baseDamage)
    {
        return baseDamage * GetEliteDamageMultiplier() * encounterDamageMultiplier;
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
        return baseMoveSpeed * GetEliteMoveSpeedMultiplier() * encounterMoveSpeedMultiplier * GetSupportMoveSpeedMultiplier();
    }

    protected float GetEffectiveMoveSpeedFromData(float fallbackMoveSpeed)
    {
        float baseMoveSpeed = enemyData != null ? enemyData.moveSpeed : fallbackMoveSpeed;
        return GetEffectiveMoveSpeed(baseMoveSpeed);
    }

    protected float GetEffectiveCooldownDuration(float baseDuration)
    {
        return baseDuration * GetEliteCooldownMultiplier() * encounterCooldownMultiplier;
    }

    public void SetEncounterCombatModifiers(MiniBossCombatModifierData modifiers)
    {
        float oldMaxHealth = MaxHealth;
        encounterHealthMultiplier = Mathf.Max(0.01f, modifiers != null ? modifiers.healthMultiplier : 1f);
        encounterDamageMultiplier = Mathf.Max(0.01f, modifiers != null ? modifiers.damageMultiplier : 1f);
        encounterCooldownMultiplier = Mathf.Max(0.01f, modifiers != null ? modifiers.cooldownMultiplier : 1f);
        encounterMoveSpeedMultiplier = Mathf.Max(0.01f, modifiers != null ? modifiers.moveSpeedMultiplier : 1f);

        if (!startInitialized)
            return;

        float healthRatio = oldMaxHealth > 0f ? currentHealth / oldMaxHealth : 1f;
        currentHealth = Mathf.Clamp01(healthRatio) * MaxHealth;
        if (defenseController != null)
            defenseController.RefreshFromOwnerData(false);
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

    protected EnemyDefenseController EnsureDefenseController()
    {
        if (defenseController != null)
            return defenseController;

        defenseController = GetComponent<EnemyDefenseController>();
        if (defenseController == null)
            defenseController = gameObject.AddComponent<EnemyDefenseController>();

        defenseController.Initialize(this);
        return defenseController;
    }

    private EnemyCombatClass ResolveCombatClass()
    {
        if (this is EnemyBoss || (enemyData != null && enemyData.combatClass == EnemyCombatClass.Boss))
            return EnemyCombatClass.Boss;

        if (enemyData != null && enemyData.combatClass == EnemyCombatClass.MiniBoss)
            return EnemyCombatClass.MiniBoss;

        if (IsElite)
            return EnemyCombatClass.Elite;

        return enemyData != null ? enemyData.combatClass : EnemyCombatClass.Normal;
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

    private static void RegisterActiveEnemy(EnemyBase enemy)
    {
        if (enemy == null || activeEnemies.Contains(enemy))
            return;

        activeEnemies.Add(enemy);
    }

    private static void UnregisterActiveEnemy(EnemyBase enemy)
    {
        if (enemy == null)
            return;

        activeEnemies.Remove(enemy);
    }

    protected float ResolveDirectionalClipLength(DirectionalAnimationState state, float fallback)
    {
        CharacterDirectionalAnimator directionalAnimator = GetComponent<CharacterDirectionalAnimator>();
        if (directionalAnimator == null || directionalAnimator.animationSet == null)
            return fallback;

        DirectionalClipResolution resolution = directionalAnimator.animationSet.ResolveClip(state, directionalAnimator.CurrentDirection);
        if (resolution.clip == null || resolution.clip.length <= 0.01f)
            return fallback;

        return resolution.clip.length;
    }

    protected float ResolveAnimatorClipLength(string[] clipNameTokens, float fallback)
    {
        Animator targetAnimator = GetComponentInChildren<Animator>(true);
        RuntimeAnimatorController controller = targetAnimator != null ? targetAnimator.runtimeAnimatorController : null;
        if (controller == null || clipNameTokens == null || clipNameTokens.Length == 0)
            return fallback;

        float bestLength = 0f;
        AnimationClip[] clips = controller.animationClips;
        for (int i = 0; i < clips.Length; i++)
        {
            AnimationClip clip = clips[i];
            if (clip == null || string.IsNullOrEmpty(clip.name))
                continue;

            for (int j = 0; j < clipNameTokens.Length; j++)
            {
                string token = clipNameTokens[j];
                if (string.IsNullOrEmpty(token))
                    continue;

                if (clip.name.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    bestLength = Mathf.Max(bestLength, clip.length);
                    break;
                }
            }
        }

        return bestLength > 0.01f ? bestLength : fallback;
    }

    protected float ResolveDeathAnimationDelay(float fallback)
    {
        float directionalDelay = ResolveDirectionalClipLength(DirectionalAnimationState.Death, -1f);
        if (directionalDelay > 0.01f)
            return directionalDelay;

        return ResolveAnimatorClipLength(new[] { "Death", "Die" }, fallback);
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

        float deltaX = targetX - transform.position.x;
        if (Mathf.Abs(deltaX) <= Mathf.Max(0f, facingHorizontalDeadzone))
            return currentFacingSign;

        int targetSign = deltaX < 0f ? -1 : 1;
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

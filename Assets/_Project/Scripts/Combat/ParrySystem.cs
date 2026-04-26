using UnityEngine;

public class ParrySystem : MonoBehaviour
{
    private static readonly Collider2D[] projectileScanBuffer = new Collider2D[32];

    [Header("Parry Timing")]
    [Tooltip("Parry penceresi (saniye). Ornek: 0.15 - 0.20")]
    public float parryWindow = 0.18f;
    [Tooltip("Parry kacarsa recovery suresi (saniye).")]
    public float parryRecovery = 0.08f;

    [Header("Multi-Block Window Extension")]
    [Tooltip("Her basarili blokta pencere kac saniye uzasin.")]
    public float windowExtensionPerBlock = 0.12f;
    [Tooltip("Uzama dahil maksimum parry penceresi.")]
    public float maxParryWindow = 0.60f;

    [Header("Directional Parry")]
    [Tooltip("Parry yari acisi (derece). 90 = +/-90 = 180 derece koni.")]
    public float parryArcHalfAngle = 90f;

    [Header("Counter Attack")]
    [Tooltip("Parry bittikten sonra karsi saldiri penceresi (s).")]
    public float counterWindowDuration = 0.5f;
    [Tooltip("Her melee blok icin eklenen karsi saldiri carpani.")]
    public float counterBonusPerMelee = 0.15f;
    [Tooltip("Her ranged deflect icin eklenen karsi saldiri carpani.")]
    public float counterBonusPerRanged = 0.10f;

    [Header("Perk-Gated Runtime")]
    [HideInInspector] public bool allowProjectileDeflect = false;
    [HideInInspector] public bool enablePerfectParry = false;
    [HideInInspector] public bool enableCounterWindow = false;
    [HideInInspector] public float normalWindowMultiplier = 1f;
    [HideInInspector] public float perfectWindowDuration = 0.06f;
    [HideInInspector] public float deflectEdgeThickness = 0.35f;
    [HideInInspector] public bool useDualArc = false;
    [HideInInspector] public float dualArcFrontHalfAngle = 55f;
    [HideInInspector] public float dualArcRearHalfAngle = 55f;
    [HideInInspector] public bool rotateArcWhileActive = false;
    [HideInInspector] public float rotatingArcDegreesPerSecond = 1080f;
    [HideInInspector] public float rotatingArcDuration = 0.18f;
    [HideInInspector] public float projectileWindowExtensionMultiplier = 1f;
    [HideInInspector] public float projectileMaxWindowBonus = 0f;
    [HideInInspector] public bool omniProjectileDeflectWhileActive = false;

    [Header("Projectile Parry Mode")]
    [Tooltip("Cone: tum koni icindeki projectile'lar. EdgeLine: sadece en on cizgiye yakin projectile'lar.")]
    public ProjectileParryMode projectileParryMode = ProjectileParryMode.Cone;

    public bool IsParryActive { get; private set; }
    public bool IsCounterWindowActive { get; private set; }
    public bool IsOnCooldown { get; private set; }
    public bool IsPerfectWindowActive => enablePerfectParry && IsParryActive && parryElapsed <= currentPerfectWindow;
    public float CurrentDeflectRange => GetDeflectRange();
    public bool IsOmniProjectileDeflectActive => allowProjectileDeflect && IsParryActive && omniProjectileDeflectWhileActive;

    private float timer;
    private float initialWindowDuration;
    private float currentWindowExtension;
    private float currentMaxParryWindow;
    private float currentPerfectWindow;
    private float currentProjectileWindowExtensionMultiplier;
    private float currentProjectileMaxWindowBonus;
    private float parryElapsed;
    private Vector2 parryDirection = Vector2.right;
    private int blockCount;
    private float accumulatedCounterBonus;
    private float counterTimer;
    private float recoveryCooldownTimer;
    private float pendingRecoveryReduction;
    private PlayerCombat playerCombat;
    private ParryPerkController parryPerkController;

    // Dash/Parry commitment carpanlari
    private float dashTempoMultiplier = 1f;
    private float dashCooldownMultiplier = 1f;
    private float dashWindowMultiplier = 1f;
    private float parryTempoMultiplier = 1f;
    private float parryCooldownMultiplier = 1f;
    private float parryWindowMultiplier = 1f;

    public float externalTempoMultiplier => dashTempoMultiplier * parryTempoMultiplier;
    public float externalCooldownMultiplier => dashCooldownMultiplier * parryCooldownMultiplier;
    public float externalWindowMultiplier => dashWindowMultiplier * parryWindowMultiplier;

    // Events
    public System.Action<Vector2> OnParryStarted;
    public System.Action<bool> OnParrySuccess;
    public System.Action<ParryEventData> OnParryResolved;
    public System.Action OnParryFail;
    public System.Action<float> OnWindowNormalized;
    public System.Action<float> OnCounterNormalized;
    public System.Action OnCounterWindowStarted;
    public System.Action OnCounterWindowEnded;

    public Vector2 CurrentParryDirection => GetCurrentParryDirection();

    private void Awake()
    {
        playerCombat = GetComponent<PlayerCombat>();
        parryPerkController = GetComponent<ParryPerkController>();
    }

    public void SetDashCommitmentParryMultipliers(float tempoMultiplier, float cooldownMultiplier, float windowMultiplier)
    {
        dashTempoMultiplier = Mathf.Max(0f, tempoMultiplier);
        dashCooldownMultiplier = Mathf.Max(0.05f, cooldownMultiplier);
        dashWindowMultiplier = Mathf.Max(0.05f, windowMultiplier);
    }

    public void SetParryCommitmentMultipliers(float tempoMultiplier, float cooldownMultiplier, float windowMultiplier)
    {
        parryTempoMultiplier = Mathf.Max(0f, tempoMultiplier);
        parryCooldownMultiplier = Mathf.Max(0.05f, cooldownMultiplier);
        parryWindowMultiplier = Mathf.Max(0.05f, windowMultiplier);
    }

    public void StartParry(Vector2 aimDirection)
    {
        if (IsParryActive || IsOnCooldown)
            return;

        if (aimDirection.sqrMagnitude < 0.001f)
            aimDirection = Vector2.right;

        IsParryActive = true;
        parryDirection = aimDirection.normalized;
        blockCount = 0;
        accumulatedCounterBonus = 0f;
        pendingRecoveryReduction = 0f;
        parryElapsed = 0f;

        if (IsCounterWindowActive)
        {
            IsCounterWindowActive = false;
            counterTimer = 0f;
            OnCounterWindowEnded?.Invoke();
        }

        float tempoSpeed = TempoManager.Instance != null ? TempoManager.Instance.GetSpeedMultiplier() : 1f;
        float scaledWindow = (parryWindow * externalWindowMultiplier * normalWindowMultiplier) / Mathf.Max(0.01f, tempoSpeed);

        timer = Mathf.Max(0.01f, scaledWindow);
        initialWindowDuration = timer;
        currentWindowExtension = windowExtensionPerBlock / Mathf.Max(0.01f, tempoSpeed);
        currentMaxParryWindow = (maxParryWindow * externalWindowMultiplier * normalWindowMultiplier) / Mathf.Max(0.01f, tempoSpeed);
        currentPerfectWindow = enablePerfectParry ? perfectWindowDuration / Mathf.Max(0.01f, tempoSpeed) : 0f;
        currentProjectileWindowExtensionMultiplier = Mathf.Max(1f, projectileWindowExtensionMultiplier);
        currentProjectileMaxWindowBonus = (projectileMaxWindowBonus * externalWindowMultiplier * normalWindowMultiplier) / Mathf.Max(0.01f, tempoSpeed);

        AudioManager.Play(AudioEventId.PlayerParryStart, gameObject);
        OnParryStarted?.Invoke(parryDirection);
        OnWindowNormalized?.Invoke(1f);
    }

    private void Update()
    {
        if (IsParryActive)
        {
            parryElapsed += Time.deltaTime;
            timer -= Time.deltaTime;

            if (allowProjectileDeflect)
                PerformActiveProjectileScan();

            float norm = Mathf.Clamp01(timer / Mathf.Max(0.001f, initialWindowDuration));
            OnWindowNormalized?.Invoke(norm);

            if (timer <= 0f)
                CloseParryWindow();
        }

        if (IsCounterWindowActive)
        {
            if (!IsParryActive)
                counterTimer -= Time.deltaTime;

            OnCounterNormalized?.Invoke(Mathf.Clamp01(counterTimer / Mathf.Max(0.001f, counterWindowDuration)));

            if (!IsParryActive && counterTimer <= 0f)
            {
                IsCounterWindowActive = false;
                accumulatedCounterBonus = 0f;
                counterTimer = 0f;
                OnCounterWindowEnded?.Invoke();
            }
        }

        if (IsOnCooldown)
        {
            recoveryCooldownTimer -= Time.deltaTime;
            if (recoveryCooldownTimer <= 0f)
            {
                recoveryCooldownTimer = 0f;
                IsOnCooldown = false;
            }
        }
    }

    public bool TryDeflect(Vector2 projectileWorldPos, GameObject sourceObject = null)
    {
        if (!CanDeflectProjectileAt(projectileWorldPos))
            return false;

        RegisterBlock(true, sourceObject);
        return true;
    }

    public bool TryBlockMelee(Vector2 attackerWorldPos, GameObject sourceObject = null)
    {
        if (!IsParryActive)
            return false;

        Vector2 toAttacker = attackerWorldPos - (Vector2)transform.position;
        Vector2 incomingDir = toAttacker.sqrMagnitude > 0.001f ? toAttacker.normalized : GetCurrentParryDirection();
        if (!IsDirectionParryable(incomingDir))
            return false;

        RegisterBlock(false, sourceObject);
        return true;
    }

    public bool TryParry(GameObject sourceObject = null)
    {
        if (!IsParryActive)
            return false;

        if (sourceObject != null)
            return TryBlockMelee(sourceObject.transform.position, sourceObject);

        RegisterBlock(false, sourceObject);
        return true;
    }

    public float GetCounterMultiplier()
    {
        return IsCounterWindowActive ? accumulatedCounterBonus : 0f;
    }

    public void ConsumeCounter()
    {
        if (!IsCounterWindowActive)
            return;

        IsCounterWindowActive = false;
        accumulatedCounterBonus = 0f;
        counterTimer = 0f;
        OnCounterWindowEnded?.Invoke();
    }

    public void ReduceRecoveryCooldown(float amount)
    {
        if (amount <= 0f)
            return;

        if (IsOnCooldown)
        {
            recoveryCooldownTimer = Mathf.Max(0f, recoveryCooldownTimer - amount);
            if (recoveryCooldownTimer <= 0f)
            {
                recoveryCooldownTimer = 0f;
                IsOnCooldown = false;
            }
            return;
        }

        pendingRecoveryReduction += amount;
    }

    public void RefreshOrExtendCounterWindow(float duration)
    {
        if (!enableCounterWindow || duration <= 0f)
            return;

        if (IsParryActive && !IsCounterWindowActive)
        {
            counterTimer = Mathf.Max(counterTimer, duration);
            return;
        }

        if (!IsCounterWindowActive)
        {
            IsCounterWindowActive = true;
            counterTimer = duration;
            OnCounterWindowStarted?.Invoke();
            return;
        }

        counterTimer = Mathf.Max(counterTimer, duration);
    }

    public bool CanDeflectProjectileAt(Vector2 projectileWorldPos)
    {
        if (!allowProjectileDeflect || !IsParryActive)
            return false;

        float maxRange = GetDeflectRange();
        Vector2 toProjectile = projectileWorldPos - (Vector2)transform.position;
        float distance = toProjectile.magnitude;
        if (distance > maxRange)
            return false;

        if (omniProjectileDeflectWhileActive)
            return true;

        if (projectileParryMode == ProjectileParryMode.EdgeLine)
        {
            float thickness = Mathf.Max(0.01f, deflectEdgeThickness);
            float minRange = Mathf.Max(0f, maxRange - thickness);
            if (distance < minRange)
                return false;
        }

        Vector2 incomingDir = toProjectile.sqrMagnitude > 0.001f ? toProjectile.normalized : GetCurrentParryDirection();
        return IsDirectionParryable(incomingDir);
    }

    public void SetProjectileParryMode(ProjectileParryMode mode)
    {
        projectileParryMode = mode;
    }

    public void ForceResetForBaseParryOnly()
    {
        IsParryActive = false;
        timer = 0f;
        parryElapsed = 0f;
        blockCount = 0;
        initialWindowDuration = 0f;
        currentWindowExtension = 0f;
        currentMaxParryWindow = 0f;
        currentPerfectWindow = 0f;
        OnWindowNormalized?.Invoke(0f);

        if (IsCounterWindowActive)
        {
            IsCounterWindowActive = false;
            accumulatedCounterBonus = 0f;
            counterTimer = 0f;
            OnCounterWindowEnded?.Invoke();
        }
    }

    private void CloseParryWindow()
    {
        IsParryActive = false;
        timer = 0f;
        OnWindowNormalized?.Invoke(0f);

        IsOnCooldown = true;
        recoveryCooldownTimer = Mathf.Max(0f, (parryRecovery * externalCooldownMultiplier) - pendingRecoveryReduction);
        pendingRecoveryReduction = 0f;

        if (blockCount > 0 && enableCounterWindow)
        {
            bool wasActive = IsCounterWindowActive;
            IsCounterWindowActive = true;
            counterTimer = Mathf.Max(counterWindowDuration, counterTimer);
            if (!wasActive)
                OnCounterWindowStarted?.Invoke();
        }
        else if (blockCount == 0)
        {
            AudioManager.Play(AudioEventId.PlayerParryFail, gameObject);
            OnParryFail?.Invoke();
        }
    }

    private void RegisterBlock(bool isRanged, GameObject sourceObject)
    {
        blockCount++;

        if (enableCounterWindow)
        {
            accumulatedCounterBonus += isRanged ? counterBonusPerRanged : counterBonusPerMelee;
            bool wasActive = IsCounterWindowActive;
            IsCounterWindowActive = true;
            counterTimer = Mathf.Max(counterWindowDuration, counterTimer);
            if (!wasActive)
                OnCounterWindowStarted?.Invoke();
        }

        float extension = currentWindowExtension;
        float effectiveMaxWindow = currentMaxParryWindow;
        if (isRanged)
        {
            extension *= currentProjectileWindowExtensionMultiplier;
            effectiveMaxWindow += currentProjectileMaxWindowBonus;
        }

        timer = Mathf.Min(timer + extension, Mathf.Max(initialWindowDuration, effectiveMaxWindow));
        initialWindowDuration = Mathf.Max(initialWindowDuration, timer);

        if (HitStopManager.Instance != null)
        {
            if (isRanged)
                HitStopManager.Instance.PlayHitStop(0.05f, 0.10f);
            else
                HitStopManager.Instance.PlayHitStop(0.08f, 0.05f);
        }

        bool isPerfect = enablePerfectParry && parryElapsed <= currentPerfectWindow;
        Vector2 resolvedDirection = GetCurrentParryDirection();

        if (isRanged)
            AudioManager.Play(AudioEventId.PlayerDeflect, gameObject, transform.position);
        else if (isPerfect)
            AudioManager.Play(AudioEventId.PlayerPerfectParry, gameObject, transform.position);
        else
            AudioManager.Play(AudioEventId.PlayerParry, gameObject, transform.position);

        OnParrySuccess?.Invoke(isRanged);
        OnParryResolved?.Invoke(new ParryEventData
        {
            isRanged = isRanged,
            isPerfect = isPerfect,
            source = sourceObject,
            parryDirection = resolvedDirection,
            blockedCount = blockCount
        });
    }

    private void PerformActiveProjectileScan()
    {
        float maxRange = GetDeflectRange();

        int hitCount = Physics2D.OverlapCircleNonAlloc(transform.position, maxRange, projectileScanBuffer);
        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit = projectileScanBuffer[i];
            if (hit == null)
                continue;

            if (!hit.TryGetComponent<IDeflectable>(out var projectile))
                continue;

            if (projectile.ObjectOwner == gameObject || projectile.IsDeflected || !projectile.CanBeDeflected)
                continue;

            if (!CanDeflectProjectileAt(hit.transform.position))
                continue;

            RegisterBlock(true, hit.gameObject);

            DeflectContext context = parryPerkController != null
                ? parryPerkController.BuildDeflectContext()
                : DeflectContext.Default(gameObject);
            Vector2 surfaceNormal = ((Vector2)hit.transform.position - (Vector2)transform.position).normalized;
            if (surfaceNormal.sqrMagnitude > 0.001f)
            {
                context.useSurfaceNormal = true;
                context.deflectSurfaceNormal = surfaceNormal;
            }
            projectile.Deflect(context);
        }
    }

    private bool IsDirectionParryable(Vector2 incomingDirection)
    {
        float frontHalfAngle = useDualArc ? dualArcFrontHalfAngle : parryArcHalfAngle;
        if (rotateArcWhileActive && IsParryActive)
        {
            if (IsDirectionWithinRotatingSweep(incomingDirection, frontHalfAngle))
                return true;
        }
        else
        {
            Vector2 currentDir = GetCurrentParryDirection();
            if (Vector2.Angle(incomingDirection, currentDir) <= frontHalfAngle)
                return true;
        }

        if (!useDualArc || dualArcRearHalfAngle <= 0f)
            return false;

        if (rotateArcWhileActive && IsParryActive)
            return IsDirectionWithinRotatingSweep(incomingDirection, dualArcRearHalfAngle, true);

        return Vector2.Angle(incomingDirection, -GetCurrentParryDirection()) <= dualArcRearHalfAngle;
    }

    private bool IsDirectionWithinRotatingSweep(Vector2 incomingDirection, float halfAngle, bool useRearArc = false)
    {
        Vector2 baseDir = useRearArc ? -parryDirection : parryDirection;
        Vector2 currentDir = useRearArc ? -GetCurrentParryDirection() : GetCurrentParryDirection();

        float startAngle = Mathf.Atan2(baseDir.y, baseDir.x) * Mathf.Rad2Deg;
        float currentAngle = Mathf.Atan2(currentDir.y, currentDir.x) * Mathf.Rad2Deg;
        float targetAngle = Mathf.Atan2(incomingDirection.y, incomingDirection.x) * Mathf.Rad2Deg;

        float sweepDelta = Mathf.DeltaAngle(startAngle, currentAngle);
        float targetDelta = Mathf.DeltaAngle(startAngle, targetAngle);

        float minSweep = Mathf.Min(0f, sweepDelta) - halfAngle;
        float maxSweep = Mathf.Max(0f, sweepDelta) + halfAngle;

        if (targetDelta >= minSweep && targetDelta <= maxSweep)
            return true;

        return Mathf.Abs(Mathf.DeltaAngle(currentAngle, targetAngle)) <= halfAngle;
    }

    private Vector2 GetCurrentParryDirection()
    {
        if (!IsParryActive || !rotateArcWhileActive)
            return parryDirection;

        float activeWindowDuration = Mathf.Max(0.01f, initialWindowDuration);
        float configuredSweepDuration = rotatingArcDuration > 0.001f
            ? rotatingArcDuration
            : activeWindowDuration;
        float sweepDuration = Mathf.Max(0.01f, Mathf.Min(configuredSweepDuration, activeWindowDuration));
        float normalized = Mathf.Clamp01(parryElapsed / sweepDuration);
        float targetSweepDegrees = Mathf.Max(360f, rotatingArcDegreesPerSecond * sweepDuration);
        float degrees = Mathf.Lerp(0f, targetSweepDegrees, normalized);
        return Quaternion.Euler(0f, 0f, degrees) * parryDirection;
    }

    private float GetDeflectRange()
    {
        float attackOffset = (playerCombat != null && playerCombat.currentWeapon != null) ? playerCombat.currentWeapon.attackOffset : 1f;
        return playerCombat != null ? playerCombat.GetEffectiveRange() + attackOffset : 2.5f;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 1f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, 2.5f);
    }
}

public enum ProjectileParryMode
{
    Cone,
    EdgeLine
}

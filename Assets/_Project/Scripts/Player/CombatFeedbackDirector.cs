using UnityEngine;

[DisallowMultipleComponent]
public class CombatFeedbackDirector : MonoBehaviour
{
    [Header("Popup Anchors")]
    [SerializeField] private float playerPopupHeight = 1.5f;
    [SerializeField] private float counterPopupHeight = 2.1f;
    [SerializeField] private float enemyPopupHeight = 1.4f;

    [Header("Popup Sizes")]
    [SerializeField] private float parryPopupSize = 6f;
    [SerializeField] private float deflectPopupSize = 6f;
    [SerializeField] private float failPopupSize = 5f;
    [SerializeField] private float counterPopupSize = 7f;
    [SerializeField] private float enemyStatePopupSize = 7f;
    [SerializeField] private float executePopupSize = 8f;

    [Header("Dedupe")]
    [SerializeField] private float perfectSuppressWindow = 0.08f;
    [SerializeField] private float normalPopupCooldown = 0.08f;
    [SerializeField] private float failPopupCooldown = 0.16f;
    [SerializeField] private float counterPopupCooldown = 0.08f;
    [SerializeField] private float enemyStatePopupCooldown = 0.08f;
    [SerializeField] private float executeReadyRefreshInterval = 0.05f;

    [Header("Combat Feel V1")]
    [SerializeField] private bool enableSlashVfx = true;
    [SerializeField] private bool enableHitstop = true;
    [SerializeField] private bool enableCameraShake = true;
    [SerializeField] private bool enableHitFlash = true;
    [SerializeField] private bool enableImpactVfx = true;
    [SerializeField] private bool debugCombatFeelLog;

    [Header("Slash / Impact Hooks")]
    [SerializeField] private GameObject slashVfxPrefab;
    [SerializeField] private GameObject impactVfxPrefab;
    [SerializeField] private float slashLifetime = 0.16f;
    [SerializeField] private float impactLifetime = 0.18f;
    [SerializeField] private string vfxSortingLayer = WorldSortingLayers.CharacterVFX;
    [SerializeField] private int slashSortingOrder = 30;
    [SerializeField] private int impactSortingOrder = 35;

    [Header("Hitstop")]
    [SerializeField] private float normalHitstopDuration = 0.075f;
    [SerializeField] private float eliteHitstopDuration = 0.09f;
    [SerializeField] private float bossHitstopDuration = 0.055f;
    [SerializeField, Range(0.01f, 1f)] private float hitstopScale = 0.06f;
    [SerializeField] private float hitstopCooldown = 0.025f;

    [Header("Camera Shake")]
    [SerializeField] private float lightHitShake = 2.35f;
    [SerializeField] private float heavyHitShake = 3.15f;
    [SerializeField] private float eliteHitShake = 3.45f;
    [SerializeField] private float bossHitShake = 2.05f;
    [SerializeField] private float attackShakeDuration = 0.13f;

    private PlayerCombat playerCombat;
    private ParrySystem parrySystem;
    private ParryPerkController parryPerkController;
    private GameObject currentExecuteReadyTarget;
    private float lastPerfectParryPopupTime = -999f;
    private float lastParryPopupTime = -999f;
    private float lastDeflectPopupTime = -999f;
    private float lastFailPopupTime = -999f;
    private float lastCounterPopupTime = -999f;
    private float lastEnemyStatePopupTime = -999f;
    private float lastHitstopTime = -999f;
    private float executeReadyRefreshTimer;

    public static CombatFeedbackDirector EnsureFor(GameObject player)
    {
        if (player == null)
            return null;

        CombatFeedbackDirector director = player.GetComponent<CombatFeedbackDirector>();
        if (director == null)
            director = player.AddComponent<CombatFeedbackDirector>();

        return director;
    }

    private void Awake()
    {
        playerCombat = GetComponent<PlayerCombat>();
        parrySystem = GetComponent<ParrySystem>();
        parryPerkController = GetComponent<ParryPerkController>();
    }

    private void OnEnable()
    {
        Subscribe();
    }

    private void Start()
    {
        Subscribe();
    }

    private void OnDisable()
    {
        if (parrySystem != null)
        {
            parrySystem.OnParryResolved -= HandleParryResolved;
            parrySystem.OnParryFail -= HandleParryFail;
        }

        if (playerCombat != null)
        {
            playerCombat.OnCounterFeedback -= HandleCounterFeedback;
            playerCombat.OnAttackStarted -= HandleAttackStarted;
            playerCombat.OnAttackHit -= HandleAttackHit;
            playerCombat.OnAttackEnded -= HandleAttackEnded;
        }

        if (parryPerkController != null)
            parryPerkController.OnEnemyControlFeedback -= HandleEnemyControlFeedback;

        ClearExecuteReadyTarget();
    }

    private void Update()
    {
        executeReadyRefreshTimer -= Time.deltaTime;
        if (executeReadyRefreshTimer > 0f)
            return;

        executeReadyRefreshTimer = executeReadyRefreshInterval;
        RefreshExecuteReadyTarget();
    }

    private void Subscribe()
    {
        if (parrySystem != null)
        {
            parrySystem.OnParryResolved -= HandleParryResolved;
            parrySystem.OnParryResolved += HandleParryResolved;
            parrySystem.OnParryFail -= HandleParryFail;
            parrySystem.OnParryFail += HandleParryFail;
        }

        if (playerCombat != null)
        {
            playerCombat.OnCounterFeedback -= HandleCounterFeedback;
            playerCombat.OnCounterFeedback += HandleCounterFeedback;
            playerCombat.OnAttackStarted -= HandleAttackStarted;
            playerCombat.OnAttackStarted += HandleAttackStarted;
            playerCombat.OnAttackHit -= HandleAttackHit;
            playerCombat.OnAttackHit += HandleAttackHit;
            playerCombat.OnAttackEnded -= HandleAttackEnded;
            playerCombat.OnAttackEnded += HandleAttackEnded;
        }

        if (parryPerkController != null)
        {
            parryPerkController.OnEnemyControlFeedback -= HandleEnemyControlFeedback;
            parryPerkController.OnEnemyControlFeedback += HandleEnemyControlFeedback;
        }
    }

    private void HandleParryResolved(ParryEventData data)
    {
        if (DamagePopupManager.Instance == null)
            return;

        Vector3 popupPosition = transform.position + Vector3.up * playerPopupHeight;

        if (data.isRanged)
        {
            if (Time.unscaledTime - lastDeflectPopupTime < normalPopupCooldown)
                return;

            lastDeflectPopupTime = Time.unscaledTime;
            DamagePopupManager.Instance.CreateText(
                popupPosition,
                "DEFLECT!",
                Color.cyan,
                deflectPopupSize);
            return;
        }

        if (data.isPerfect)
        {
            if (Time.unscaledTime - lastPerfectParryPopupTime < normalPopupCooldown)
                return;

            lastPerfectParryPopupTime = Time.unscaledTime;
            DamagePopupManager.Instance.CreateText(
                popupPosition,
                "PERFECT PARRY!",
                new Color(1f, 0.9f, 0.25f),
                parryPopupSize);
            return;
        }

        if (Time.unscaledTime - lastPerfectParryPopupTime < perfectSuppressWindow)
            return;

        if (Time.unscaledTime - lastParryPopupTime < normalPopupCooldown)
            return;

        lastParryPopupTime = Time.unscaledTime;
        DamagePopupManager.Instance.CreateText(
            popupPosition,
            "PARRY!",
            Color.yellow,
            parryPopupSize);
    }

    private void HandleParryFail()
    {
        if (DamagePopupManager.Instance == null)
            return;

        if (Time.unscaledTime - lastFailPopupTime < failPopupCooldown)
            return;

        lastFailPopupTime = Time.unscaledTime;
        DamagePopupManager.Instance.CreateText(
            transform.position + Vector3.up * playerPopupHeight,
            "FAILED",
            new Color(1f, 0.5f, 0f),
            failPopupSize);
    }

    private void HandleCounterFeedback(CounterFeedbackData data)
    {
        if (DamagePopupManager.Instance == null)
            return;

        if (Time.unscaledTime - lastCounterPopupTime < counterPopupCooldown)
            return;

        lastCounterPopupTime = Time.unscaledTime;

        string prefix = data.source == CounterFeedbackSource.Parry ? "PARRY COUNTER!" : "DASH COUNTER!";
        Color color = data.source == CounterFeedbackSource.Parry
            ? new Color(1f, 0.85f, 0f)
            : new Color(0f, 0.9f, 1f);

        DamagePopupManager.Instance.CreateText(
            data.worldPosition + Vector3.up * counterPopupHeight,
            $"{prefix} x{data.multiplier:F2}",
            color,
            counterPopupSize);
    }

    private void HandleAttackStarted(PlayerAttackFeedbackData data)
    {
        if (!enableSlashVfx)
            return;

        Vector2 direction = data.direction.sqrMagnitude > 0.001f ? data.direction.normalized : Vector2.right;
        Vector3 origin = data.attackPoint != null ? data.attackPoint.position : data.origin;
        Vector3 position = origin + (Vector3)(direction * Mathf.Max(0.15f, data.range * 0.25f));
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        if (slashVfxPrefab != null)
        {
            GameObject instance = Instantiate(slashVfxPrefab, position, Quaternion.Euler(0f, 0f, angle));
            ApplySorting(instance, slashSortingOrder);
            Destroy(instance, Mathf.Max(0.02f, slashLifetime));
        }
        else
        {
            SpawnPlaceholderSlash(data.origin, direction, Mathf.Max(0.5f, data.range * 0.55f), data.arcAngle);
        }

        if (debugCombatFeelLog)
            Debug.Log($"[CombatFeel] AttackStarted dir={direction} duration={data.duration:F2}");
    }

    private void HandleAttackHit(PlayerAttackHitFeedbackData data)
    {
        if (enableHitFlash)
            TryFlashEnemy(data.enemy);

        if (enableImpactVfx)
            SpawnImpact(data);

        if (enableHitstop)
            TryPlayHitstop(data);

        if (enableCameraShake)
            TryShakeCamera(data);

        if (debugCombatFeelLog)
            Debug.Log($"[CombatFeel] Hit class={data.targetClass} damage={data.healthDamage:F1} heavy={data.isHeavyHit}");
    }

    private void HandleAttackEnded(PlayerAttackFeedbackData data)
    {
        if (debugCombatFeelLog)
            Debug.Log("[CombatFeel] AttackEnded");
    }

    private void TryPlayHitstop(PlayerAttackHitFeedbackData data)
    {
        if (HitStopManager.Instance == null)
            return;

        if (Time.unscaledTime - lastHitstopTime < Mathf.Max(0f, hitstopCooldown))
            return;

        float duration = data.targetClass switch
        {
            EnemyCombatClass.Boss => bossHitstopDuration,
            EnemyCombatClass.MiniBoss => bossHitstopDuration,
            EnemyCombatClass.Elite => eliteHitstopDuration,
            _ => normalHitstopDuration
        };

        if (data.isHeavyHit && data.targetClass == EnemyCombatClass.Normal)
            duration = Mathf.Max(duration, normalHitstopDuration * 1.25f);

        lastHitstopTime = Time.unscaledTime;
        HitStopManager.Instance.PlayHitStop(Mathf.Max(0.005f, duration), hitstopScale);
    }

    private void TryShakeCamera(PlayerAttackHitFeedbackData data)
    {
        if (CameraShakeManager.Instance == null)
            return;

        float strength = data.targetClass switch
        {
            EnemyCombatClass.Boss => bossHitShake,
            EnemyCombatClass.MiniBoss => bossHitShake,
            EnemyCombatClass.Elite => eliteHitShake,
            _ => data.isHeavyHit ? heavyHitShake : lightHitShake
        };

        CameraShakeManager.Instance.ShakeCamera(strength, attackShakeDuration);
    }

    private void TryFlashEnemy(EnemyBase enemy)
    {
        if (enemy == null)
            return;

        HitFlash flash = enemy.GetComponent<HitFlash>();
        if (flash == null)
            flash = enemy.GetComponentInChildren<HitFlash>();

        flash?.Flash();
    }

    private void SpawnImpact(PlayerAttackHitFeedbackData data)
    {
        Vector2 direction = data.direction.sqrMagnitude > 0.001f ? data.direction.normalized : Vector2.right;
        Vector3 position = data.hitPoint;
        if (position == Vector3.zero && data.enemy != null)
            position = data.enemy.transform.position - (Vector3)(direction * 0.2f);

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        if (impactVfxPrefab != null)
        {
            GameObject instance = Instantiate(impactVfxPrefab, position, Quaternion.Euler(0f, 0f, angle));
            ApplySorting(instance, impactSortingOrder);
            Destroy(instance, Mathf.Max(0.02f, impactLifetime));
            return;
        }

        SpawnPlaceholderImpact(position, direction);
    }

    private void SpawnPlaceholderSlash(Vector3 origin, Vector2 direction, float radius, float arcAngle)
    {
        GameObject go = new GameObject("CombatFeel_SlashPlaceholder");
        LineRenderer line = go.AddComponent<LineRenderer>();
        ConfigureLine(line, slashSortingOrder, 0.11f);

        Vector2 normalizedDirection = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector2.right;
        Vector2 tangent = new Vector2(-normalizedDirection.y, normalizedDirection.x);
        Vector3 center = origin + (Vector3)(normalizedDirection * Mathf.Max(0.25f, radius * 0.55f));
        float halfLength = Mathf.Max(0.22f, radius * 0.35f);
        float forwardSkew = Mathf.Max(0.04f, radius * 0.08f);

        line.positionCount = 2;
        line.SetPosition(0, center - (Vector3)(tangent * halfLength) - (Vector3)(normalizedDirection * forwardSkew));
        line.SetPosition(1, center + (Vector3)(tangent * halfLength) + (Vector3)(normalizedDirection * forwardSkew));

        CombatFeelTransientVfx fade = go.AddComponent<CombatFeelTransientVfx>();
        fade.Initialize(line, new Color(1f, 0.72f, 0.22f, 0.85f), slashLifetime);
    }

    private void SpawnPlaceholderImpact(Vector3 position, Vector2 direction)
    {
        GameObject go = new GameObject("CombatFeel_ImpactPlaceholder");
        LineRenderer line = go.AddComponent<LineRenderer>();
        ConfigureLine(line, impactSortingOrder, 0.055f);

        Vector2 normalizedDirection = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector2.right;
        Vector2 tangent = new Vector2(-normalizedDirection.y, normalizedDirection.x);
        Vector3 center = position - (Vector3)(normalizedDirection * 0.04f);
        float mainLength = 0.34f;
        float sideLength = 0.2f;

        line.positionCount = 6;
        line.SetPosition(0, center);
        line.SetPosition(1, center + (Vector3)(normalizedDirection * mainLength));
        line.SetPosition(2, center);
        line.SetPosition(3, center + (Vector3)((normalizedDirection * 0.62f + tangent * 0.38f).normalized * sideLength));
        line.SetPosition(4, center);
        line.SetPosition(5, center + (Vector3)((normalizedDirection * 0.62f - tangent * 0.38f).normalized * sideLength));

        CombatFeelTransientVfx fade = go.AddComponent<CombatFeelTransientVfx>();
        fade.Initialize(line, new Color(1f, 0.72f, 0.18f, 0.9f), impactLifetime);
    }

    private void ConfigureLine(LineRenderer line, int sortingOrder, float width)
    {
        line.useWorldSpace = true;
        line.startWidth = width;
        line.endWidth = width;
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.sortingLayerName = WorldSortingUtility.ResolveLayerName(vfxSortingLayer);
        line.sortingOrder = sortingOrder;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
    }

    private void ApplySorting(GameObject instance, int sortingOrder)
    {
        if (instance == null)
            return;

        Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
            WorldSortingUtility.ApplySorting(renderers[i], vfxSortingLayer, sortingOrder);
    }

    private void HandleEnemyControlFeedback(EnemyControlFeedbackData data)
    {
        if (data.target == null)
            return;

        EnemyStateFeedbackType stateType = EnemyStateFeedbackType.None;
        string text = null;
        Color popupColor = Color.white;
        float popupSize = enemyStatePopupSize;

        switch (data.type)
        {
            case EnemyControlFeedbackType.Stun:
                stateType = EnemyStateFeedbackType.Stun;
                text = "STUN!";
                popupColor = new Color(1f, 0.55f, 0.15f);
                break;
            case EnemyControlFeedbackType.Stagger:
                stateType = EnemyStateFeedbackType.Stagger;
                text = "STAGGER!";
                popupColor = new Color(1f, 0.72f, 0.18f);
                break;
            case EnemyControlFeedbackType.GuardBreak:
                stateType = EnemyStateFeedbackType.GuardBreak;
                text = "GUARD BREAK!";
                popupColor = new Color(1f, 0.32f, 0.18f);
                break;
            case EnemyControlFeedbackType.ExecuteTriggered:
                if (currentExecuteReadyTarget == data.target)
                    ClearExecuteReadyTarget();

                stateType = EnemyStateFeedbackType.Executed;
                text = "EXECUTED!";
                popupColor = new Color(1f, 0.15f, 0.15f);
                popupSize = executePopupSize;
                break;
        }

        EnemyStateFeedback feedback = EnemyStateFeedback.EnsureFor(data.target);
        if (feedback != null && stateType != EnemyStateFeedbackType.None)
            feedback.ShowState(stateType, data.duration);

        if (DamagePopupManager.Instance == null || string.IsNullOrEmpty(text))
            return;

        if (Time.unscaledTime - lastEnemyStatePopupTime < enemyStatePopupCooldown && data.type != EnemyControlFeedbackType.ExecuteTriggered)
            return;

        lastEnemyStatePopupTime = Time.unscaledTime;
        Vector3 popupPosition = data.worldPosition + Vector3.up * enemyPopupHeight;
        DamagePopupManager.Instance.CreateText(popupPosition, text, popupColor, popupSize);
        DamagePopupManager.Instance.CreateHitParticle(data.worldPosition);
    }

    private void RefreshExecuteReadyTarget()
    {
        GameObject nextTarget = null;
        if (parryPerkController != null)
            parryPerkController.TryGetCloseExecuteReadyTarget(out nextTarget);

        if (nextTarget == null && currentExecuteReadyTarget == null)
        {
            currentExecuteReadyTarget = null;
            return;
        }

        if (nextTarget != null && nextTarget == currentExecuteReadyTarget)
            return;

        ClearExecuteReadyTarget();

        if (nextTarget == null)
            return;

        currentExecuteReadyTarget = nextTarget;
        EnemyStateFeedback feedback = EnemyStateFeedback.EnsureFor(currentExecuteReadyTarget);
        if (feedback != null)
            feedback.SetExecuteReady(true);
    }

    private void ClearExecuteReadyTarget()
    {
        if (currentExecuteReadyTarget == null)
            return;

        EnemyStateFeedback feedback = EnemyStateFeedback.EnsureFor(currentExecuteReadyTarget);
        if (feedback != null)
            feedback.SetExecuteReady(false);

        currentExecuteReadyTarget = null;
    }
}

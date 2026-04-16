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
            playerCombat.OnCounterFeedback -= HandleCounterFeedback;

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

using UnityEngine;

public enum DirectionalAnimationBridgeMode
{
    Auto = 0,
    Player = 1,
    Enemy = 2,
    Manual = 3
}

[DisallowMultipleComponent]
public class DirectionalAnimationStateBridge : MonoBehaviour
{
    [Header("Mode")]
    public DirectionalAnimationBridgeMode mode = DirectionalAnimationBridgeMode.Auto;

    [Header("References")]
    public CharacterDirectionalAnimator directionalAnimator;
    public PlayerController playerController;
    public PlayerCombat playerCombat;
    public ParrySystem parrySystem;
    public EnemyBase enemy;

    [Header("Pulse Durations")]
    public float attackPulseDuration = 0.22f;
    public float parryPulseDuration = 0.18f;
    public float dashPulseDuration = 0.22f;
    public float hitPulseDuration = 0.16f;
    public float staggerPulseDuration = 0.35f;
    public float deathPulseDuration = 1f;
    public float movementInputDeadzone = 0.01f;

    [Header("Debug")]
    [SerializeField] private DirectionalAnimationState requestedState = DirectionalAnimationState.Idle;
    [SerializeField] private Vector2 rawMoveInput;
    [SerializeField] private bool hasMoveInput;
    [SerializeField] private bool isMoving;

    private Vector3 lastPosition;
    private float lastPlayerHealth = -1f;
    private bool subscribed;

    private void Awake()
    {
        ResolveReferences();
        lastPosition = transform.position;
        if (playerCombat != null)
            lastPlayerHealth = playerCombat.currentHealth;
    }

    private void OnEnable()
    {
        ResolveReferences();
        Subscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void LateUpdate()
    {
        if (directionalAnimator == null || mode == DirectionalAnimationBridgeMode.Manual)
            return;

        requestedState = ResolveBaseState();
        directionalAnimator.SetBaseState(requestedState);
        lastPosition = transform.position;
    }

    [ContextMenu("Resolve Directional Animation Bridge References")]
    public void ResolveReferences()
    {
        directionalAnimator ??= GetComponent<CharacterDirectionalAnimator>();
        playerController ??= GetComponent<PlayerController>();
        playerCombat ??= GetComponent<PlayerCombat>();
        parrySystem ??= GetComponent<ParrySystem>();
        enemy ??= GetComponent<EnemyBase>();

        if (mode == DirectionalAnimationBridgeMode.Auto)
        {
            if (playerController != null || playerCombat != null)
                mode = DirectionalAnimationBridgeMode.Player;
            else if (enemy != null)
                mode = DirectionalAnimationBridgeMode.Enemy;
        }
    }

    private void Subscribe()
    {
        if (subscribed)
            return;

        if (playerController != null)
        {
            playerController.OnDodgeStarted += HandleDodgeStarted;
            playerController.OnDodgeEnded += HandleDodgeEnded;
        }

        if (playerCombat != null)
            playerCombat.OnHealthChanged += HandlePlayerHealthChanged;

        if (parrySystem != null)
        {
            parrySystem.OnParryStarted += HandleParryStarted;
            parrySystem.OnParryFail += HandleParryEnded;
            parrySystem.OnParryResolved += HandleParryResolved;
        }

        if (enemy != null)
        {
            enemy.OnDamageResolved += HandleEnemyDamageResolved;
            enemy.OnStunnedDetailed += HandleEnemyStunned;
            EnemyBase.OnEnemyCombatAction += HandleEnemyCombatAction;
        }

        subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!subscribed)
            return;

        if (playerController != null)
        {
            playerController.OnDodgeStarted -= HandleDodgeStarted;
            playerController.OnDodgeEnded -= HandleDodgeEnded;
        }

        if (playerCombat != null)
            playerCombat.OnHealthChanged -= HandlePlayerHealthChanged;

        if (parrySystem != null)
        {
            parrySystem.OnParryStarted -= HandleParryStarted;
            parrySystem.OnParryFail -= HandleParryEnded;
            parrySystem.OnParryResolved -= HandleParryResolved;
        }

        if (enemy != null)
        {
            enemy.OnDamageResolved -= HandleEnemyDamageResolved;
            enemy.OnStunnedDetailed -= HandleEnemyStunned;
            EnemyBase.OnEnemyCombatAction -= HandleEnemyCombatAction;
        }

        subscribed = false;
    }

    private DirectionalAnimationState ResolveBaseState()
    {
        if (mode == DirectionalAnimationBridgeMode.Player)
            return ResolvePlayerState();

        if (mode == DirectionalAnimationBridgeMode.Enemy)
            return ResolveEnemyState();

        return DirectionalAnimationState.Idle;
    }

    private DirectionalAnimationState ResolvePlayerState()
    {
        if (playerCombat != null && playerCombat.currentHealth <= 0f)
            return DirectionalAnimationState.Death;

        if (parrySystem != null && parrySystem.IsParryActive)
            return DirectionalAnimationState.Parry;

        if (playerController != null)
        {
            if (playerController.currentState == PlayerController.PlayerState.Dodging ||
                playerController.currentState == PlayerController.PlayerState.DashStriking)
                return DirectionalAnimationState.Dash;
        }

        if (playerCombat != null && playerCombat.IsAttackActionActive)
            return DirectionalAnimationState.Attack;

        rawMoveInput = playerController != null ? playerController.CurrentMoveInput : Vector2.zero;
        hasMoveInput = rawMoveInput.sqrMagnitude > movementInputDeadzone * movementInputDeadzone;
        isMoving = hasMoveInput;
        return isMoving ? DirectionalAnimationState.Move : DirectionalAnimationState.Idle;
    }

    private DirectionalAnimationState ResolveEnemyState()
    {
        if (enemy != null && enemy.CurrentHealth <= 0f)
            return DirectionalAnimationState.Death;

        isMoving = IsMovingByPositionDelta();
        return isMoving ? DirectionalAnimationState.Move : DirectionalAnimationState.Idle;
    }

    private bool IsMovingByPositionDelta()
    {
        float threshold = directionalAnimator != null ? directionalAnimator.movingThreshold : 0.05f;
        Vector2 delta = transform.position - lastPosition;
        return delta.sqrMagnitude > threshold * threshold * Time.deltaTime * Time.deltaTime;
    }

    private void HandleDodgeStarted(Vector2 direction)
    {
        directionalAnimator?.RequestState(DirectionalAnimationState.Dash, dashPulseDuration, 80);
    }

    private void HandleDodgeEnded()
    {
        directionalAnimator?.ClearPulseState(DirectionalAnimationState.Dash);
    }

    private void HandleParryStarted(Vector2 direction)
    {
        directionalAnimator?.RequestState(DirectionalAnimationState.Parry, parryPulseDuration, 70);
    }

    private void HandleParryEnded()
    {
        directionalAnimator?.ClearPulseState(DirectionalAnimationState.Parry);
    }

    private void HandleParryResolved(ParryEventData data)
    {
        directionalAnimator?.ClearPulseState(DirectionalAnimationState.Parry);
    }

    private void HandlePlayerHealthChanged(float current, float max)
    {
        if (lastPlayerHealth >= 0f && current < lastPlayerHealth)
            directionalAnimator?.RequestState(current <= 0f ? DirectionalAnimationState.Death : DirectionalAnimationState.Hit, current <= 0f ? deathPulseDuration : hitPulseDuration, 100);

        lastPlayerHealth = current;
    }

    private void HandleEnemyDamageResolved(EnemyDamageResult result)
    {
        if (result.ignored)
            return;

        if (enemy != null && enemy.CurrentHealth <= 0f)
        {
            directionalAnimator?.RequestState(DirectionalAnimationState.Death, deathPulseDuration, 100);
            return;
        }

        if (result.didBreak)
            directionalAnimator?.RequestState(DirectionalAnimationState.Broken, staggerPulseDuration, 90);
        else if (result.appliedHealthDamage > 0f)
            directionalAnimator?.RequestState(DirectionalAnimationState.Hit, hitPulseDuration, 60);
    }

    private void HandleEnemyStunned(EnemyBase stunnedEnemy, float duration)
    {
        if (stunnedEnemy == enemy)
            directionalAnimator?.RequestState(DirectionalAnimationState.Stagger, Mathf.Max(staggerPulseDuration, duration), 85);
    }

    private void HandleEnemyCombatAction(EnemyCombatActionEvent actionEvent)
    {
        if (actionEvent.source != enemy)
            return;

        DirectionalAnimationState state = DirectionalAnimationState.Special;
        switch (actionEvent.actionType)
        {
            case EnemyCombatActionType.Attack:
                state = DirectionalAnimationState.Attack;
                break;
            case EnemyCombatActionType.Dash:
                state = DirectionalAnimationState.Dash;
                break;
            case EnemyCombatActionType.Cast:
                state = DirectionalAnimationState.Cast;
                break;
            case EnemyCombatActionType.Skill:
            case EnemyCombatActionType.Summon:
                state = DirectionalAnimationState.Special;
                break;
        }

        directionalAnimator?.RequestState(state, attackPulseDuration, 70);
    }
}

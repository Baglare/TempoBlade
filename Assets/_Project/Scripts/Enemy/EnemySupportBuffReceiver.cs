using UnityEngine;

[DisallowMultipleComponent]
public class EnemySupportBuffReceiver : MonoBehaviour
{
    [Header("Rally Visuals")]
    [SerializeField] private Color rallyMarkerColor = new Color(1f, 0.72f, 0.18f, 1f);
    [SerializeField] private Color guardianMarkerColor = new Color(0.25f, 1f, 0.45f, 1f);

    [Header("Stagger Rules")]
    [SerializeField] private float lightStaggerThreshold = 0.3f;
    [SerializeField] private float heavyStaggerThreshold = 0.65f;

    private EnemyBase enemyBase;

    private float rallyEndTime;
    private float rallyMoveSpeedMultiplier = 1f;
    private float rallyAttackSpeedMultiplier = 1f;
    private bool rallyIgnoreNextLightStagger;

    private float guardianEndTime;
    private float guardianDamageTakenMultiplier = 1f;
    private float guardianStaggerDurationMultiplier = 1f;
    private bool guardianIgnoreNextHeavyStagger;

    public float MoveSpeedMultiplier => IsRallyActive ? rallyMoveSpeedMultiplier : 1f;
    public float AttackSpeedMultiplier => IsRallyActive ? rallyAttackSpeedMultiplier : 1f;
    public bool IsRallyActive => Time.time < rallyEndTime;
    public bool IsGuardianLinked => Time.time < guardianEndTime;

    private void Awake()
    {
        enemyBase = GetComponent<EnemyBase>();
    }

    private void Update()
    {
        if (!IsRallyActive)
        {
            rallyMoveSpeedMultiplier = 1f;
            rallyAttackSpeedMultiplier = 1f;
            rallyIgnoreNextLightStagger = false;
        }

        if (!IsGuardianLinked)
        {
            guardianDamageTakenMultiplier = 1f;
            guardianStaggerDurationMultiplier = 1f;
            guardianIgnoreNextHeavyStagger = false;
        }

        RefreshMarker();
    }

    public void ApplyRallyBuff(float duration, float moveSpeedMultiplier, float attackSpeedMultiplier, bool ignoreNextLightStagger)
    {
        rallyEndTime = Mathf.Max(rallyEndTime, Time.time + Mathf.Max(0.05f, duration));
        rallyMoveSpeedMultiplier = Mathf.Max(rallyMoveSpeedMultiplier, moveSpeedMultiplier);
        rallyAttackSpeedMultiplier = Mathf.Max(rallyAttackSpeedMultiplier, attackSpeedMultiplier);
        rallyIgnoreNextLightStagger |= ignoreNextLightStagger;
        RefreshMarker();
    }

    public void ApplyGuardianLink(float duration, float damageTakenMultiplier, float staggerDurationMultiplier, bool ignoreNextHeavyStagger)
    {
        guardianEndTime = Mathf.Max(guardianEndTime, Time.time + Mathf.Max(0.05f, duration));
        guardianDamageTakenMultiplier = Mathf.Min(guardianDamageTakenMultiplier, Mathf.Clamp(damageTakenMultiplier, 0.05f, 1f));
        guardianStaggerDurationMultiplier = Mathf.Min(guardianStaggerDurationMultiplier, Mathf.Clamp(staggerDurationMultiplier, 0.05f, 1f));
        guardianIgnoreNextHeavyStagger |= ignoreNextHeavyStagger;
        RefreshMarker();
    }

    public float ModifyIncomingDamage(float amount)
    {
        if (!IsGuardianLinked)
            return amount;

        return amount * guardianDamageTakenMultiplier;
    }

    public float ModifyIncomingStunDuration(float duration)
    {
        if (!IsGuardianLinked)
            return duration;

        return duration * guardianStaggerDurationMultiplier;
    }

    public bool TryNegateIncomingStun(float duration)
    {
        if (IsRallyActive && rallyIgnoreNextLightStagger && duration <= lightStaggerThreshold)
        {
            rallyIgnoreNextLightStagger = false;
            RefreshMarker();
            return true;
        }

        if (IsGuardianLinked && guardianIgnoreNextHeavyStagger && duration >= heavyStaggerThreshold)
        {
            guardianIgnoreNextHeavyStagger = false;
            RefreshMarker();
            return true;
        }

        return false;
    }

    private void RefreshMarker()
    {
        if (enemyBase == null)
            return;

        if (IsGuardianLinked)
        {
            enemyBase.SetPerkMarker(true, guardianMarkerColor);
            return;
        }

        if (IsRallyActive)
        {
            enemyBase.SetPerkMarker(true, rallyMarkerColor);
            return;
        }

        enemyBase.SetPerkMarker(false, Color.clear);
    }
}

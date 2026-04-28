using UnityEngine;

public enum CounterFeedbackSource
{
    Parry,
    Dash
}

[System.Serializable]
public struct CounterFeedbackData
{
    public CounterFeedbackSource source;
    public float multiplier;
    public Vector3 worldPosition;
}

public enum EnemyControlFeedbackType
{
    Stun,
    Stagger,
    GuardBreak,
    ExecuteTriggered
}

[System.Serializable]
public struct EnemyControlFeedbackData
{
    public GameObject target;
    public EnemyControlFeedbackType type;
    public float duration;
    public bool isBoss;
    public Vector3 worldPosition;
}

public enum EnemyStateFeedbackType
{
    None,
    Stun,
    Stagger,
    GuardBreak,
    Broken,
    Armor,
    Guard,
    ExecuteReady,
    Executed
}

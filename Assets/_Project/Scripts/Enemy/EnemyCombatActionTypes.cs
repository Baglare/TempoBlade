using System;
using UnityEngine;

public enum EnemyCombatActionType
{
    Attack = 0,
    Dash = 1,
    Cast = 2,
    Skill = 3,
    Summon = 4
}

[Serializable]
public struct EnemyCombatActionEvent
{
    public EnemyBase source;
    public EnemyCombatActionType actionType;
    public float weight;
    public float time;
    public Vector3 worldPosition;
}

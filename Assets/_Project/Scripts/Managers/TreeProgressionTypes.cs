using System;
using System.Collections.Generic;
using UnityEngine;

public enum SkillTreeInteractionMode
{
    NormalProgression,
    Tester
}

public enum CombatActionType
{
    None,
    Attack,
    Heavy,
    Dash,
    Parry,
    PerfectParry,
    Counter,
    Skill,
    TargetSwitch,
    Hit,
    DamageTaken,
    Kill,
    Stun,
    GuardBreak,
    ThresholdCross,
    Whiff,
    Deflect,
    DodgeThreat,
    ParryFail
}

public enum EncounterType
{
    Normal,
    Elite,
    MiniBoss,
    Boss
}

public enum DifficultyTier
{
    Easy,
    Normal,
    Hard
}

[Serializable]
public class TreeProgressionEntry
{
    public string axisId;
    public float xp;
    public int rank;
    public string chosenTier2Route = "";
}

[Serializable]
public class TesterSkillTreeSave
{
    public List<string> unlockedNodeIds = new List<string>();
}

public struct CombatTelemetryEvent
{
    public CombatActionType actionType;
    public float time;
    public GameObject source;
    public EnemyBase target;
    public bool isRanged;
    public bool isPerfect;
    public bool killed;
    public float value;
    public float attackMultiplier;
    public float counterMultiplier;
    public Vector2 direction;
}

[Serializable]
public class AxisAffinityWeights
{
    public string axisId;
    [Range(0f, 1f)] public float primaryWeight = 0.3f;
    [Range(0f, 1f)] public float secondaryWeight = 0.2f;
    [Range(0f, 1f)] public float conversionWeight = 0.25f;
    [Range(0f, 1f)] public float utilityWeight = 0.15f;
    [Range(0f, 1f)] public float fifthWeight = 0.15f;
    [Range(0f, 1f)] public float penaltyWeight = 0.2f;
}


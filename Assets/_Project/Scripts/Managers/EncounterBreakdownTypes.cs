using System;
using System.Collections.Generic;

[Serializable]
public class EncounterBreakdownSnapshot
{
    public string roomName = "";
    public EncounterType encounterType = EncounterType.Normal;
    public DifficultyTier difficulty = DifficultyTier.Normal;
    public readonly List<AxisBreakdownSnapshot> axisBreakdowns = new List<AxisBreakdownSnapshot>();
}

[Serializable]
public class AxisBreakdownSnapshot
{
    public string axisId = "";
    public string axisDisplayName = "";
    public float affinity;
    public float xpAwarded;
    public float varietyBonus = 1f;
    public bool receivedXp;
    public string xpReason = "";
    public float primary;
    public float secondary;
    public float conversion;
    public float utility;
    public float fifth;
    public float penalty;
}

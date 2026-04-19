using System;
using UnityEngine;

[Serializable]
public class TempoTierFloatValue
{
    public float t0 = 1f;
    public float t1 = 1f;
    public float t2 = 1f;
    public float t3 = 1f;

    public float Evaluate(TempoManager.TempoTier tier)
    {
        switch (tier)
        {
            case TempoManager.TempoTier.T1:
                return t1;
            case TempoManager.TempoTier.T2:
                return t2;
            case TempoManager.TempoTier.T3:
                return t3;
            default:
                return t0;
        }
    }
}

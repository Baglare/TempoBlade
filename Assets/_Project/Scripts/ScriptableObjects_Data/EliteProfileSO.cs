using System;
using UnityEngine;

[CreateAssetMenu(fileName = "EliteProfile", menuName = "TempoBlade/Enemy/Elite Profile")]
public class EliteProfileSO : ScriptableObject
{
    [Header("General Multipliers")]
    public float healthMultiplier = 1.25f;
    public float damageMultiplier = 1.12f;
    public float cooldownMultiplier = 0.9f;
    public float moveSpeedMultiplier = 1f;

    [Header("Cue")]
    public Color eliteCueColor = new Color(1f, 0.45f, 0.1f, 1f);
    public GameObject eliteVfxPrefab;
    public AudioEventId eliteAudioEvent = AudioEventId.None;

    [Header("Mechanic")]
    public EliteMechanicType eliteMechanicType = EliteMechanicType.None;
    public CasterBurstOrbSettings casterBurstOrb = new CasterBurstOrbSettings();

    public bool HasMechanic(EliteMechanicType mechanicType)
    {
        return eliteMechanicType == mechanicType;
    }
}

[Serializable]
public class CasterBurstOrbSettings
{
    [Range(0f, 1f)] public float burstOrbChance = 0.35f;
    public float impactRadius = 1.35f;
    public float impactDamageMultiplier = 0.65f;
    public int fragmentCount = 4;
    public float fragmentAngleSpread = 80f;
    public float fragmentSpeedMultiplier = 0.9f;
    public float fragmentLifetimeMultiplier = 0.5f;
    public float fragmentDamageMultiplier = 0.4f;
    public float fragmentExplosionRadius = 0.7f;
    public float fragmentExplosionDamageMultiplier = 0.35f;
    public Color burstCueColor = new Color(1f, 0.58f, 0.12f, 1f);
}

public enum EliteMechanicType
{
    None = 0,
    CasterBurstOrb = 1
}

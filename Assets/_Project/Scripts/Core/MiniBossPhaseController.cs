using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class MiniBossPhaseController : MonoBehaviour
{
    private readonly List<MiniBossPhaseThreshold> orderedThresholds = new List<MiniBossPhaseThreshold>();

    private EnemyBase targetEnemy;
    private MiniBossEncounterSO encounterData;
    private MiniBossCombatModifierData difficultyModifiers = new MiniBossCombatModifierData();
    private int nextThresholdIndex;

    public event Action<int, MiniBossPhaseThreshold> OnPhaseChanged;

    public int CurrentPhaseIndex { get; private set; } = 1;
    public MiniBossPhaseThreshold CurrentPhaseData { get; private set; }
    public bool IsInitialized => targetEnemy != null && encounterData != null;

    public void Initialize(EnemyBase enemy, MiniBossEncounterSO encounter, DifficultyTier difficulty)
    {
        targetEnemy = enemy;
        encounterData = encounter;
        CurrentPhaseIndex = 1;
        CurrentPhaseData = null;
        nextThresholdIndex = 0;

        orderedThresholds.Clear();
        if (encounterData != null && encounterData.phaseThresholds != null)
        {
            orderedThresholds.AddRange(encounterData.phaseThresholds);
            orderedThresholds.Sort((a, b) => b.triggerHealthPercent.CompareTo(a.triggerHealthPercent));
        }

        difficultyModifiers = encounterData != null && encounterData.difficultyScaling != null
            ? encounterData.difficultyScaling.GetForDifficulty(difficulty)
            : new MiniBossCombatModifierData();

        ApplyCurrentModifiers();
    }

    private void Update()
    {
        if (!IsInitialized || targetEnemy == null || targetEnemy.CurrentHealth <= 0f || orderedThresholds.Count == 0)
            return;

        while (nextThresholdIndex < orderedThresholds.Count)
        {
            MiniBossPhaseThreshold threshold = orderedThresholds[nextThresholdIndex];
            if (threshold == null || targetEnemy.HealthPercent > threshold.triggerHealthPercent)
                break;

            nextThresholdIndex++;
            CurrentPhaseIndex = Mathf.Max(2, nextThresholdIndex + 1);
            CurrentPhaseData = threshold;
            ApplyCurrentModifiers();
            EmitPhaseChanged(threshold);
        }
    }

    private void ApplyCurrentModifiers()
    {
        if (targetEnemy == null)
            return;

        MiniBossCombatModifierData phaseModifiers = CurrentPhaseData != null ? CurrentPhaseData.combatModifiers : null;
        MiniBossCombatModifierData combined = MiniBossCombatModifierData.Combine(difficultyModifiers, phaseModifiers);
        targetEnemy.SetEncounterCombatModifiers(combined);
    }

    private void EmitPhaseChanged(MiniBossPhaseThreshold threshold)
    {
        if (threshold == null)
            return;

        if (threshold.phaseChangeVfxPrefab != null)
        {
            GameObject vfx = Instantiate(threshold.phaseChangeVfxPrefab, targetEnemy.transform.position, Quaternion.identity);
            Destroy(vfx, 3f);
        }

        if (threshold.phaseChangeAudio != AudioEventId.None)
            AudioManager.Play(threshold.phaseChangeAudio, targetEnemy.gameObject, targetEnemy.transform.position);

        targetEnemy.gameObject.SendMessage("OnMiniBossPhaseChanged", threshold, SendMessageOptions.DontRequireReceiver);
        Debug.Log($"[MiniBoss] Phase changed -> {CurrentPhaseIndex} ({threshold.displayName})");
        OnPhaseChanged?.Invoke(CurrentPhaseIndex, threshold);
    }
}


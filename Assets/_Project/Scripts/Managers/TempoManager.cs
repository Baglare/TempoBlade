using System;
using UnityEngine;

public class TempoManager : MonoBehaviour
{
    public static TempoManager Instance { get; private set; }

    public enum TempoTier { T0, T1, T2, T3 }

    [Serializable]
    public class TempoRegenConfig
    {
        public float healAmount = 0f;
        [Tooltip("0 ise regen kapali")]
        public float healInterval = 0f;
    }

    [Header("HP Regen per Tier (T0, T1, T2, T3)")]
    public TempoRegenConfig[] regenConfigs = new TempoRegenConfig[4];

    [Header("Tempo")]
    [Range(0f, 100f)]
    public float tempo = 0f;
    public float maxTempo = 100f;

    [Header("Run Modifiers")]
    public float tempoGainMultiplier = 1.0f;

    [Header("Tier Thresholds (Non-Linear)")]
    [Tooltip("0-39")] public float tier1Start = 40f;
    [Tooltip("40-69")] public float tier2Start = 70f;
    [Tooltip("90-100")] public float tier3Start = 90f;

    [Header("Gain")]
    public float gainOnPerfectParry = 20f;

    [Header("Decay")]
    public bool enableDecay = true;
    public float decayPerSecond = 6f;
    private float overdriveDecayMultiplier = 1f;
    private float cadenceDecayMultiplier = 1f;
    private float supportZoneGainMultiplier = 1f;
    private float supportZoneDecayMultiplier = 1f;
    private float overdriveDamagePenaltyMultiplier = 1f;
    private float cadenceDamagePenaltyMultiplier = 1f;

    private float decayTimer;
    private float regenTimer;
    private int positiveTempoGainSuppressionCount;

    public TempoTier CurrentTier { get; private set; } = TempoTier.T0;
    public bool IsPositiveTempoGainSuppressed => positiveTempoGainSuppressionCount > 0;

    public Action<TempoTier> OnTierChanged;
    public Action<float> OnTempoChanged;

    private PlayerCombat cachedPlayer;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void InitializeLoadedState()
    {
        OnTempoChanged?.Invoke(tempo);
        CurrentTier = EvaluateTier(tempo);
        ApplyGlobalSpeed(CurrentTier);
        OnTierChanged?.Invoke(CurrentTier);
        decayTimer = GetDecayDelayForTier(CurrentTier);
    }

    public IDisposable SuppressPositiveTempoGain(string reason = "")
    {
        positiveTempoGainSuppressionCount++;
        return new TempoGainSuppressionHandle(this);
    }

    public void ResetTempoToZero()
    {
        SetTempoImmediate(0f, false);
    }

    public void SetTempoImmediate(float value, bool refreshDecayTimer)
    {
        float clamped = Mathf.Clamp(value, 0f, maxTempo);
        bool changed = !Mathf.Approximately(tempo, clamped);
        tempo = clamped;

        if (changed)
            OnTempoChanged?.Invoke(tempo);

        TempoTier newTier = EvaluateTier(tempo);
        if (newTier != CurrentTier)
        {
            CurrentTier = newTier;
            ApplyGlobalSpeed(CurrentTier);
            OnTierChanged?.Invoke(CurrentTier);
        }

        if (refreshDecayTimer)
            decayTimer = GetDecayDelayForTier(CurrentTier);
    }

    public void AddTempo(float amount)
    {
        if (amount > 0f && IsPositiveTempoGainSuppressed)
            return;

        if (amount > 0f)
            decayTimer = GetDecayDelayForTier(CurrentTier);

        float actualAmount = amount > 0f
            ? amount * tempoGainMultiplier * supportZoneGainMultiplier
            : amount;

        SetTempoImmediate(tempo + actualAmount, false);

        if (amount > 0f)
            decayTimer = GetDecayDelayForTier(CurrentTier);
    }

    public void ApplyDamagePenalty()
    {
        float penaltyMultiplier = overdriveDamagePenaltyMultiplier * cadenceDamagePenaltyMultiplier;

        if (CurrentTier == TempoTier.T3)
        {
            float penalty = Mathf.Max(0f, tempo - (tier3Start - 1f));
            SetTempoImmediate(tempo - penalty * penaltyMultiplier, false);
        }
        else if (CurrentTier == TempoTier.T2)
        {
            SetTempoImmediate(Mathf.Clamp(tempo - 15f * penaltyMultiplier, tier1Start, maxTempo), false);
        }
        else if (CurrentTier == TempoTier.T1)
        {
            SetTempoImmediate(tempo - 10f * penaltyMultiplier, false);
        }
        else
        {
            SetTempoImmediate(tempo - 5f * penaltyMultiplier, false);
        }

        decayTimer = GetDecayDelayForTier(EvaluateTier(tempo));
    }

    public void SetOverdriveTempoMultipliers(float decayMultiplier, float damagePenaltyMultiplier)
    {
        overdriveDecayMultiplier = Mathf.Max(0f, decayMultiplier);
        overdriveDamagePenaltyMultiplier = Mathf.Max(0f, damagePenaltyMultiplier);
    }

    public void SetCadenceTempoMultipliers(float decayMultiplier, float damagePenaltyMultiplier)
    {
        cadenceDecayMultiplier = Mathf.Max(0f, decayMultiplier);
        cadenceDamagePenaltyMultiplier = Mathf.Max(0f, damagePenaltyMultiplier);
    }

    public void SetSupportZoneTempoMultipliers(float gainMultiplier, float decayMultiplier)
    {
        supportZoneGainMultiplier = Mathf.Max(0.05f, gainMultiplier);
        supportZoneDecayMultiplier = Mathf.Max(0.05f, decayMultiplier);
    }

    public float GetDamageMultiplier()
    {
        return CurrentTier switch
        {
            TempoTier.T1 => 1.2f,
            TempoTier.T2 => 1.5f,
            TempoTier.T3 => 2.0f,
            _ => 1.0f
        };
    }

    public float GetSpeedMultiplier()
    {
        return CurrentTier switch
        {
            TempoTier.T1 => 1.1f,
            TempoTier.T2 => 1.25f,
            TempoTier.T3 => 1.5f,
            _ => 1.0f
        };
    }

    private float GetDecayDelayForTier(TempoTier tier)
    {
        return tier switch
        {
            TempoTier.T3 => 1.1f,
            TempoTier.T2 => 1.9f,
            TempoTier.T1 => 2.8f,
            _ => 4.0f
        };
    }

    private TempoTier EvaluateTier(float value)
    {
        if (value >= tier3Start) return TempoTier.T3;
        if (value >= tier2Start) return TempoTier.T2;
        if (value >= tier1Start) return TempoTier.T1;
        return TempoTier.T0;
    }

    private void ApplyGlobalSpeed(TempoTier tier)
    {
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameManager.GameState.Gameplay)
            return;

        Time.timeScale = 1.0f;
    }

    private void TryCachePlayer()
    {
        if (cachedPlayer == null)
            cachedPlayer = FindFirstObjectByType<PlayerCombat>();
    }

    private void Update()
    {
        if (!enableDecay)
            return;

        if (regenConfigs != null && regenConfigs.Length > (int)CurrentTier)
        {
            TempoRegenConfig currentConfig = regenConfigs[(int)CurrentTier];
            if (currentConfig.healInterval > 0f && currentConfig.healAmount > 0f)
            {
                regenTimer -= Time.deltaTime;
                if (regenTimer <= 0f)
                {
                    TryCachePlayer();
                    if (cachedPlayer != null)
                        cachedPlayer.Heal(currentConfig.healAmount);

                    regenTimer = currentConfig.healInterval;
                }
            }
            else
            {
                regenTimer = 0f;
            }
        }

        if (decayTimer > 0f)
        {
            decayTimer -= Time.deltaTime;
            return;
        }

        if (tempo > 0f)
            AddTempo(-decayPerSecond * overdriveDecayMultiplier * cadenceDecayMultiplier * supportZoneDecayMultiplier * Time.deltaTime);
    }

    private void ReleasePositiveTempoGainSuppression()
    {
        positiveTempoGainSuppressionCount = Mathf.Max(0, positiveTempoGainSuppressionCount - 1);
    }

    private sealed class TempoGainSuppressionHandle : IDisposable
    {
        private TempoManager owner;

        public TempoGainSuppressionHandle(TempoManager owner)
        {
            this.owner = owner;
        }

        public void Dispose()
        {
            if (owner == null)
                return;

            owner.ReleasePositiveTempoGainSuppression();
            owner = null;
        }
    }
}

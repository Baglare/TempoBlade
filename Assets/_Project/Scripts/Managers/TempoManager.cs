using UnityEngine;

public class TempoManager : MonoBehaviour
{
    public static TempoManager Instance { get; private set; }

    public enum TempoTier { T0, T1, T2, T3 }

    [System.Serializable]
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
    [Tooltip("70-89")] public float tier3Start = 90f; // 90-100 is T3 (Shortest)

    [Header("Gain")]
    public float gainOnPerfectParry = 20f;

    [Header("Decay")]
    public bool enableDecay = true;
    public float decayPerSecond = 6f; // Decay başladıktan sonraki saniyelik düşüş hızı
    
    // Tier bazlı Decay bekleme süreleri
    private float GetDecayDelayForTier(TempoTier tier)
    {
        switch (tier)
        {
            case TempoTier.T3: return 1.1f;
            case TempoTier.T2: return 1.9f;
            case TempoTier.T1: return 2.8f;
            default: return 4.0f; // T0
        }
    }

    private float decayTimer = 0f;
    private float regenTimer = 0f;

    public TempoTier CurrentTier { get; private set; } = TempoTier.T0;

    public System.Action<TempoTier> OnTierChanged;
    public System.Action<float> OnTempoChanged;

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
        // UI ve sistemlerin yeni yuklenen tempo degerine adapte olmasi icin tetikle
        OnTempoChanged?.Invoke(tempo);
        
        CurrentTier = EvaluateTier(tempo);
        ApplyGlobalSpeed(CurrentTier);
        OnTierChanged?.Invoke(CurrentTier);
        
        // Cok hizli bir decay baslamasin diye baslangic suresi ver
        decayTimer = GetDecayDelayForTier(CurrentTier);
    }

    public void AddTempo(float amount)
    {
        // Kazanç varsa süreyi yenile, yoksa elleme (penalty metodu ayrı olacak)
        if (amount > 0)
        {
            decayTimer = GetDecayDelayForTier(CurrentTier);
        }

        float actualAmount = amount > 0 ? amount * tempoGainMultiplier : amount; // Sadece kazanclari carp
        float prevTempo = tempo;
        tempo = Mathf.Clamp(tempo + actualAmount, 0f, maxTempo);

        if (!Mathf.Approximately(prevTempo, tempo))
            OnTempoChanged?.Invoke(tempo);

        var newTier = EvaluateTier(tempo);
        if (newTier != CurrentTier)
        {
            CurrentTier = newTier;
            ApplyGlobalSpeed(CurrentTier);
            OnTierChanged?.Invoke(CurrentTier);
            
            // Eğer tier değiştiyse decay timer'ı yeni tier'e göre yenile (sadece yukarı çıkarken)
            if (amount > 0) decayTimer = GetDecayDelayForTier(CurrentTier);
        }
    }

    // Hasar alındığında çağrılır
    public void ApplyDamagePenalty()
    {
        float prevTempo = tempo;

        if (CurrentTier == TempoTier.T3)
        {
            // T3'te hasar yenirse direkt T2'nin en üstüne (veya biraz altına) çakılır.
            tempo = tier3Start - 1f; // Örn: 89
        }
        else if (CurrentTier == TempoTier.T2)
        {
            // T2'de hasar yenirse sabit bir ceza, ama direkt T1'e çakılmayabilir
            tempo = Mathf.Clamp(tempo - 15f, tier1Start, maxTempo); // T1 eşiğinin altına düşmez
        }
        else if (CurrentTier == TempoTier.T1)
        {
            // T1'de yenirse
            tempo = Mathf.Clamp(tempo - 10f, 0f, maxTempo);
        }
        else
        {
            // T0'da yenirse
            tempo = Mathf.Clamp(tempo - 5f, 0f, maxTempo);
        }

        // Korumayı sıfırlama, hatta hafif afterglow verebiliriz, oyuncu hemen decay yaşamaz
        decayTimer = GetDecayDelayForTier(EvaluateTier(tempo));

        if (!Mathf.Approximately(prevTempo, tempo))
            OnTempoChanged?.Invoke(tempo);

        var newTier = EvaluateTier(tempo);
        if (newTier != CurrentTier)
        {
            CurrentTier = newTier;
            ApplyGlobalSpeed(CurrentTier);
            OnTierChanged?.Invoke(CurrentTier);
        }
    }

    // --- PLAYER STAT BOOSTS ---
    public float GetDamageMultiplier()
    {
        switch (CurrentTier)
        {
            case TempoTier.T1: return 1.2f; // +20% Hasar
            case TempoTier.T2: return 1.5f; // +50% Hasar
            case TempoTier.T3: return 2.0f; // +100% Hasar
            default: return 1.0f;           // Normal
        }
    }

    public float GetSpeedMultiplier()
    {
        switch (CurrentTier)
        {
            case TempoTier.T1: return 1.1f; // +10% Hiz
            case TempoTier.T2: return 1.25f;// +25% Hiz
            case TempoTier.T3: return 1.5f; // +50% Hiz
            default: return 1.0f;           // Normal
        }
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
        // GameManager'in duraklamadigi bir durumdaysak hizi degistir
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameManager.GameState.Gameplay) return;

        switch (tier)
        {
            case TempoTier.T0:
                Time.timeScale = 1.0f;

                break;
            case TempoTier.T1:
                Time.timeScale = 1.1f;

                break;
            case TempoTier.T2:
                Time.timeScale = 1.2f;

                break;
            case TempoTier.T3:
                Time.timeScale = 1.3f;

                break;
        }
    }

    private void Update()
    {
        if (!enableDecay) return;

        // --- HP Regen Logic ---
        if (regenConfigs != null && regenConfigs.Length > (int)CurrentTier)
        {
            var currentConfig = regenConfigs[(int)CurrentTier];
            if (currentConfig.healInterval > 0f && currentConfig.healAmount > 0f)
            {
                regenTimer -= Time.deltaTime;
                if (regenTimer <= 0f)
                {
                    // Oyuncuyu bul ve heal at
                    PlayerCombat player = FindFirstObjectByType<PlayerCombat>();
                    if (player != null)
                    {
                        player.Heal(currentConfig.healAmount);
                    }
                    // Timer'i resetle
                    regenTimer = currentConfig.healInterval;
                }
            }
            else
            {
                // Mevcut Tier'da regen yoksa veya kapatilmissa, timer'i sifirda tut
                regenTimer = 0f;
            }
        }

        // --- Decay Logic ---
        if (decayTimer > 0f)
        {
            decayTimer -= Time.deltaTime;
            return;
        }

        if (tempo > 0f)
            AddTempo(-decayPerSecond * Time.deltaTime);
    }
}

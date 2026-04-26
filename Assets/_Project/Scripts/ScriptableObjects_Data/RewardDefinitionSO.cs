using UnityEngine;

/// <summary>
/// Tum reward ScriptableObject tiplerinin ortak tabani.
/// Legacy reward akisi korunur, yeni run reward metadata'si da burada tasinir.
/// </summary>
public abstract class RewardDefinitionSO : ScriptableObject
{
    [Header("Genel")]
    public string rewardName = "Yeni Odul";

    [Tooltip("Kapi ustunde ve haritada gosterilecek ikon")]
    public Sprite icon;

    [Tooltip("Kapi ustundeki ikon renk tonu")]
    public Color tintColor = Color.white;

    [Header("Kategori")]
    [Tooltip("Legacy reward kategorisi. Yeni reward omurgasi icin fallback olarak kalir.")]
    public RewardCategory category = RewardCategory.Survival;

    [Header("Progression Skeleton Metadata")]
    [Tooltip("Yeni reward iskeleti icindeki tip etiketi. Bos ise legacy kategori fallback kullanilir.")]
    public RunRewardType runRewardType = RunRewardType.Unspecified;

    [Tooltip("Reward rarity etiketi. Bu sprintte metadata olarak tasinir.")]
    public RunRewardRarity rarity = RunRewardRarity.Common;

    [Tooltip("Reward resolver tarafinda kullanilabilecek hafif agirlik metadata'si.")]
    public RunRewardWeightProfile weightProfile = new RunRewardWeightProfile();

    /// <summary>
    /// Legacy reward uygulama giris noktasi.
    /// </summary>
    public abstract void GrantReward(PlayerCombat player);

    /// <summary>
    /// Yeni reward apply omurgasi icin context alabilen adapter.
    /// Reward siniflari isterse override edebilir, override etmezse legacy akisa duser.
    /// </summary>
    public virtual void GrantReward(PlayerCombat player, RunRewardContext context)
    {
        GrantReward(player);
    }

    /// <summary>
    /// UI'da gosterilecek aciklama metni.
    /// </summary>
    public virtual string GetDescription() => rewardName;
}

public enum RewardCategory
{
    Survival,
    Offensive,
    Tempo,
    Economy,
    Utility
}

using UnityEngine;

/// <summary>
/// Tüm ödüllerin ortak arayüzü. Yeni ödül eklemek için bu sınıftan
/// türetip GrantReward'ı override et, sonra Unity'de CreateAssetMenu ile oluştur.
/// Hiçbir switch-case'e dokunmana gerek yok!
/// </summary>
public abstract class RewardDefinitionSO : ScriptableObject
{
    [Header("Genel")]
    public string rewardName = "Yeni Ödül";

    [Tooltip("Kapı üstünde ve haritada gösterilecek ikon")]
    public Sprite icon;

    [Tooltip("Kapı üstündeki ikon renk tonu")]
    public Color tintColor = Color.white;

    [Header("Kategori")]
    [Tooltip("Oyuncuya sunulacak ödül kategorisi (ileride filtreleme için)")]
    public RewardCategory category = RewardCategory.Survival;

    /// <summary>
    /// Ödülü oyuncuya uygular. Her SO kendi mantığını yazar.
    /// </summary>
    public abstract void GrantReward(PlayerCombat player);

    /// <summary>
    /// UI'da gösterilecek açıklama metni.
    /// </summary>
    public virtual string GetDescription() => rewardName;
}

/// <summary>
/// Ödül kategorileri — ileride oynanış seçimi (Hades boon grid) için kullanılacak.
/// </summary>
public enum RewardCategory
{
    Survival,       // Can, kalkan, iyileşme
    Offensive,      // Hasar, hız, kritik
    Tempo,          // Tempo kazanımı, çarpan
    Economy,        // Altın, meta-currency
    Utility         // Hareket, dodge, özel
}

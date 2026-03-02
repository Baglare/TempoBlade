using UnityEngine;

/// <summary>
/// Her Tempo kademesi (T0-T3) icin gorsel tanimlari tutar.
/// Inspector'dan ayarla, runtime'da degismez.
/// Assets > Create > TempoBlade > TempoTierVisual
/// </summary>
[CreateAssetMenu(fileName = "TempoTierVisual", menuName = "TempoBlade/TempoTierVisual")]
public class TempoTierVisualSO : ScriptableObject
{
    [System.Serializable]
    public class TierVisual
    {
        [Header("Player Görseli")]
        [Tooltip("Tier'a özel Animator Controller. null = değişmez (mevcut kalır)")]
        public RuntimeAnimatorController animatorController;

        [Tooltip("Player sprite'ına uygulanacak renk tint")]
        public Color playerTint = Color.white;

        [Header("Düşman Efektleri")]
        [Tooltip("Düşman scale çarpanı (1 = normal, 0.8 = %20 küçült)")]
        [Range(0.3f, 1.5f)]
        public float enemyScaleMultiplier = 1f;

        [Tooltip("Düşman sprite'ına uygulanacak renk tint")]
        public Color enemyTint = Color.white;

        [Tooltip("Düşman üstüne overlay prefab aktif mi? (ör: T3'te neon çarpı)")]
        public bool enemyOverlayEnabled;

        [Tooltip("Düşmana child olarak eklenen overlay prefab (null = yok)")]
        public GameObject enemyOverlayPrefab;

        [Header("UI")]
        [Tooltip("Tempo bar dolgu rengi")]
        public Color uiBarColor = Color.white;

        [Header("Ekran Geçiş Efekti")]
        [Tooltip("Tier'a geçişte ekran flash rengi")]
        public Color transitionFlashColor = new Color(1f, 1f, 1f, 0.3f);

        [Tooltip("Flash süresi (saniye)")]
        public float transitionFlashDuration = 0.15f;

        [Tooltip("Kamera sarsıntı şiddeti (0 = yok)")]
        [Range(0f, 1f)]
        public float cameraShakeIntensity = 0f;
    }

    [Tooltip("T0, T1, T2, T3 sırasıyla 4 eleman")]
    public TierVisual[] tiers = new TierVisual[4]
    {
        new TierVisual(),
        new TierVisual(),
        new TierVisual(),
        new TierVisual()
    };

    /// <summary>
    /// Guvenli tier erisimi. Gecersiz index icin T0 doner.
    /// </summary>
    public TierVisual GetVisual(TempoManager.TempoTier tier)
    {
        int idx = (int)tier;
        if (tiers == null || idx < 0 || idx >= tiers.Length)
            return tiers != null && tiers.Length > 0 ? tiers[0] : new TierVisual();
        return tiers[idx];
    }
}
